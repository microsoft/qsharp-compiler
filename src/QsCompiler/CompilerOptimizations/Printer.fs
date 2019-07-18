module Microsoft.Quantum.QsCompiler.CompilerOptimization.Printer

open System
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

open Utils


// Pretty-print various syntax elements


let rec printExpr (expr: Expr): string =
    match expr with
    | Identifier (LocalVariable a, _) -> "LocalVariable " + a.Value
    | Identifier (GlobalCallable a, _) -> "GlobalCallable " + a.Name.Value
    | Identifier (InvalidIdentifier, _) -> "__invalid__"
    | CallLikeExpression (f, x) -> "(" + (printExpr f.Expression) + " of " + (printExpr x.Expression) + ")"
    | ValueTuple a -> "(" + String.Join(", ", a |> Seq.map (fun x -> printExpr x.Expression)) + ")"
    | ValueArray a -> "[" + String.Join(", ", a |> Seq.map (fun x -> printExpr x.Expression)) + "]"
    | RangeLiteral (a, b) -> (printExpr a.Expression) + ".." + (printExpr b.Expression)
    | CONDITIONAL (a, b, c) -> sprintf "(%O ? %O | %O)" (printExpr a.Expression) (printExpr b.Expression) (printExpr c.Expression)
    | CopyAndUpdate (a, b, c) -> sprintf "(%O w/= %O <- %O)" (printExpr a.Expression) (printExpr b.Expression) (printExpr c.Expression)
    | ArrayItem (a, b) -> sprintf "%O[%O]" (printExpr a.Expression) (printExpr b.Expression)
    | UnwrapApplication a -> "(Unwrap " + (printExpr a.Expression) + ")"
    | AdjointApplication a -> "(Adjoint " + (printExpr a.Expression) + ")"
    | ControlledApplication a -> "(Controlled " + (printExpr a.Expression) + ")"
    | ADD (a, b) -> "(" + (printExpr a.Expression) + " + " + (printExpr b.Expression) + ")"
    | SUB (a, b) -> "(" + (printExpr a.Expression) + " - " + (printExpr b.Expression) + ")"
    | MUL (a, b) -> "(" + (printExpr a.Expression) + " * " + (printExpr b.Expression) + ")"
    | DIV (a, b) -> "(" + (printExpr a.Expression) + " / " + (printExpr b.Expression) + ")"
    | EQ (a, b) -> "(" + (printExpr a.Expression) + " = " + (printExpr b.Expression) + ")"
    | GT (a, b) -> "(" + (printExpr a.Expression) + " > " + (printExpr b.Expression) + ")"
    | GTE (a, b) -> "(" + (printExpr a.Expression) + " >= " + (printExpr b.Expression) + ")"
    | LT (a, b) -> "(" + (printExpr a.Expression) + " < " + (printExpr b.Expression) + ")"
    | AND (a, b) -> "(" + (printExpr a.Expression) + " and " + (printExpr b.Expression) + ")"
    | OR (a, b) -> "(" + (printExpr a.Expression) + " or " + (printExpr b.Expression) + ")"
    | BAND (a, b) -> "(" + (printExpr a.Expression) + " & " + (printExpr b.Expression) + ")"
    | BOR (a, b) -> "(" + (printExpr a.Expression) + " | " + (printExpr b.Expression) + ")"
    | StringLiteral (a, b) -> "\"" + a.Value + "\""
    | a -> a.ToString()


let rec printInitializer (kind: InitKind): string =
    match kind with
    | SingleQubitAllocation -> "Qubit()"
    | QubitRegisterAllocation a -> sprintf "new Qubit[%O]" (printExpr a.Expression)
    | QubitTupleAllocation a -> "(" + String.Join(", ", a |> Seq.map (fun x -> printInitializer x.Resolution)) + ")"
    | InvalidInitializer -> "__invalid__"


let rec printStm (indent: int) (stm: QsStatementKind): string =
    let ws = String.replicate indent "    "
    match stm with
    | QsReturnStatement a ->
        ws + sprintf "return %O;" (printExpr a.Expression)
    | QsFailStatement a ->
        ws + sprintf "fail %O;" (printExpr a.Expression)
    | QsVariableDeclaration a ->
        ws + sprintf "let %O = %O;" (a.Lhs |> StringTuple.fromSymbolTuple) (printExpr a.Rhs.Expression)
    | QsValueUpdate a ->
        ws + sprintf "set %O = %O;" (printExpr a.Lhs.Expression) (printExpr a.Rhs.Expression)
    | QsConditionalStatement a ->
        let condBlocks = ws + "if " + String.Join(" elif ", a.ConditionalBlocks |> Seq.map (fun (c, b) -> (printExpr c.Expression) + " " + (printScope indent b.Body)))
        match a.Default with
        | Value x -> condBlocks + " else " + (printScope indent x.Body)
        | Null -> condBlocks
    | QsForStatement a ->
        ws + sprintf "for %O in %O %O" (a.LoopItem |> fst |> StringTuple.fromSymbolTuple) (printExpr a.IterationValues.Expression) (printScope indent a.Body)
    | QsWhileStatement a ->
        ws + sprintf "while %O %O" (printExpr a.Condition.Expression) (printScope indent a.Body)
    | QsRepeatStatement a ->
        ws + "repeat " + (printScope indent a.RepeatBlock.Body) + "\n" +
        ws + "until " + (printExpr a.SuccessCondition.Expression) + "\n" +
        ws + "fixup " + (printScope indent a.FixupBlock.Body)
    | QsScopeStatement a ->
        ws + printScope indent a.Body
    | QsExpressionStatement a ->
        ws + printExpr a.Expression
    | QsQubitScope a ->
        ws + sprintf "%O (%O = %O) %O" a.Kind (StringTuple.fromSymbolTuple a.Binding.Lhs) (printInitializer a.Binding.Rhs.Resolution) (printScope indent a.Body)


and printScope (indent: int) (scope: QsScope): string =
    if scope.Statements.IsEmpty then "{ EmptyScope }"
    else
        let ws = String.replicate indent "    "
        sprintf "{\n" + (String.Join("", scope.Statements |> Seq.map (fun x -> printStm (indent+1) x.Statement + "\n"))) + ws + "}"


let printSpecialization (s: QsSpecialization): string =
    let impl =
        match s.Implementation with 
        | Provided (a, b) ->
            sprintf "%O %O" (StringTuple.fromQsTuple a) (printScope 2 b)
        | _ -> s.Implementation.ToString()
    sprintf "        %O %O\n" s.Kind impl


let rec printTypeKind (kind: TypeKind): string =
    match kind with
    | UserDefinedType a -> a.Name.Value
    | TypeParameter a -> a.TypeName.Value
    | TypeKind.Function (a, b) -> sprintf "(%O -> %O)" (printTypeKind a.Resolution) (printTypeKind b.Resolution)
    | TypeKind.Operation ((a, b), _) -> sprintf "(%O => %O)" (printTypeKind a.Resolution) (printTypeKind b.Resolution)
    | TupleType a -> "(" + String.Join(", ", a |> Seq.map (fun x -> printTypeKind x.Resolution)) + ")"
    | ArrayType a -> (printTypeKind a.Resolution) + "[]"
    | _ -> kind.ToString()


let printNamespaceElem (elem: QsNamespaceElement): string =
    match elem with
    | QsCallable a ->
        sprintf
            "%O %O %O : %O {\n%O    }"
            a.Kind
            a.FullName.Name.Value
            (StringTuple.fromQsTuple a.ArgumentTuple)
            (printTypeKind a.Signature.ReturnType.Resolution)
            (String.Join("", a.Specializations |> Seq.map printSpecialization |> List.ofSeq |> List.sort))
    | QsCustomType a ->
        sprintf "newtype %O = %O;" a.FullName.Name.Value (printTypeKind a.Type.Resolution)


let printNamespace (ns: QsNamespace): string =
    sprintf "namespace %O {\n    %O\n}" ns.Name.Value (String.Join("\n\n    ", ns.Elements |> Seq.map printNamespaceElem |> List.ofSeq |> List.sort))

