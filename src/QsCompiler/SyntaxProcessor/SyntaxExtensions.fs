// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

[<System.Runtime.CompilerServices.Extension>]
module Microsoft.Quantum.QsCompiler.SyntaxProcessing.SyntaxExtensions

open System
open System.Collections.Immutable
open System.Linq
open System.Runtime.CompilerServices
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Documentation
open Microsoft.Quantum.QsCompiler.Documentation.BuiltIn
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput


// utils for providing information for editor commands based on syntax tokens

let rec private collectWith collector (exs : 'a seq, ts : QsType seq) : QsSymbol list * QsType list * QsExpression list =
    let (varFromExs, tsFromExs, bsFromExs) = [ for item in exs do yield collector item ] |> List.unzip3
    let fromTs  = ts |> Seq.collect TypeNameSymbols |> Seq.toList
    varFromExs |> List.concat, (fromTs :: tsFromExs) |> List.concat, bsFromExs |> List.concat

and private TypeNameSymbols (t : QsType) = t.Type |> function
    | QsTypeKind.UnitType              _ -> [t]
    | QsTypeKind.Int                   _ -> [t]
    | QsTypeKind.BigInt                _ -> [t]
    | QsTypeKind.Double                _ -> [t]
    | QsTypeKind.Bool                  _ -> [t]
    | QsTypeKind.String                _ -> [t]
    | QsTypeKind.Qubit                 _ -> [t]
    | QsTypeKind.Result                _ -> [t]
    | QsTypeKind.Pauli                 _ -> [t]
    | QsTypeKind.Range                 _ -> [t]
    | QsTypeKind.ArrayType             b -> TypeNameSymbols b
    | QsTypeKind.TupleType         items -> ([], items) |> collectWith SymbolsFromExpr |> fun (_,r,_) -> r
    | QsTypeKind.UserDefinedType       _ -> [t]
    | QsTypeKind.TypeParameter         _ -> [t]
    | QsTypeKind.Operation ((it, ot), _) -> ([], [it; ot]) |> collectWith SymbolsFromExpr |> fun (_,r,_) -> r
    | QsTypeKind.Function       (it, ot) -> ([], [it; ot]) |> collectWith SymbolsFromExpr |> fun (_,r,_) -> r
    | QsTypeKind.MissingType           _ -> []
    | QsTypeKind.InvalidType           _ -> []

and private VariablesInInitializer item = item.Initializer |> function
    | QsInitializerKind.SingleQubitAllocation -> [], [], []
    | QsInitializerKind.QubitRegisterAllocation ex -> ex |> SymbolsFromExpr
    | QsInitializerKind.QubitTupleAllocation items -> collectWith VariablesInInitializer (items, [])
    | QsInitializerKind.InvalidInitializer -> [], [], []

and private SymbolsFromExpr item : QsSymbol list * QsType list * QsExpression list = item.Expression |> function
    | QsExpressionKind.UnitValue                              -> [], [], [item]
    | QsExpressionKind.Identifier (id, typeArgs)              -> [id], typeArgs.ValueOr ImmutableArray.Empty |> Seq.collect TypeNameSymbols |> Seq.toList, []
    | QsExpressionKind.CallLikeExpression (lhs, rhs)          -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.UnwrapApplication ex                   -> ([ex], [])                 |> collectWith SymbolsFromExpr
    | QsExpressionKind.AdjointApplication ex                  -> ([ex], [])                 |> collectWith SymbolsFromExpr
    | QsExpressionKind.ControlledApplication ex               -> ([ex], [])                 |> collectWith SymbolsFromExpr
    | QsExpressionKind.ValueTuple values                      -> (values, [])               |> collectWith SymbolsFromExpr
    | QsExpressionKind.IntLiteral _                           -> [], [], [item]
    | QsExpressionKind.BigIntLiteral _                        -> [], [], [item]
    | QsExpressionKind.DoubleLiteral _                        -> [], [], [item]
    | QsExpressionKind.BoolLiteral _                          -> [], [], [item]
    | QsExpressionKind.StringLiteral (_, i) when i.Length = 0 -> [], [], [item] // todo: remove once overlapping items in SymbolInformation are handled
    | QsExpressionKind.StringLiteral (_, inner)               -> (inner, [])                |> collectWith SymbolsFromExpr
    | QsExpressionKind.ResultLiteral _                        -> [], [], [item]
    | QsExpressionKind.PauliLiteral _                         -> [], [], [item]
    | QsExpressionKind.RangeLiteral (lhs,rhs)                 -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.NewArray (t, idx)                      -> ([idx], [t])               |> collectWith SymbolsFromExpr
    | QsExpressionKind.ValueArray values                      -> (values, [])               |> collectWith SymbolsFromExpr
    | QsExpressionKind.ArrayItem (arr, ex)                    -> ([arr; ex], [])            |> collectWith SymbolsFromExpr
    | QsExpressionKind.NamedItem (ex, acc)                    -> ([ex], [])                 |> collectWith SymbolsFromExpr // TODO: process accessor
    | QsExpressionKind.NEG ex                                 -> ([ex], [])                 |> collectWith SymbolsFromExpr
    | QsExpressionKind.NOT ex                                 -> ([ex], [])                 |> collectWith SymbolsFromExpr
    | QsExpressionKind.BNOT ex                                -> ([ex], [])                 |> collectWith SymbolsFromExpr
    | QsExpressionKind.ADD    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.SUB    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.MUL    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.DIV    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.MOD    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.POW    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.EQ     (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.NEQ    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.LT     (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.LTE    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.GT     (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.GTE    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.AND    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.OR     (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.BOR    (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.BAND   (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.BXOR   (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.LSHIFT (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.RSHIFT (lhs,rhs)                       -> ([lhs; rhs], [])           |> collectWith SymbolsFromExpr
    | QsExpressionKind.CopyAndUpdate (lhs, acc, rhs)          -> ([lhs; rhs; acc], [])      |> collectWith SymbolsFromExpr
    | QsExpressionKind.CONDITIONAL (cond, case1, case2)       -> ([cond; case1; case2], []) |> collectWith SymbolsFromExpr
    | QsExpressionKind.MissingExpr                            -> [], [], [item]
    | QsExpressionKind.InvalidExpr                            -> [], [], [item]

let private AttributeAsCallExpr (sym : QsSymbol, ex : QsExpression) = 
    let combinedRange = QsNullable.Map2 Range.Span sym.Range ex.Range
    let id = {Expression = QsExpressionKind.Identifier (sym, Null); Range = sym.Range}
    {Expression = QsExpressionKind.CallLikeExpression(id, ex); Range = combinedRange}

let rec private SymbolDeclarations (sym : QsSymbol) =
    match sym.Symbol with
    | SymbolTuple items -> [ for item in items do yield item ] |> List.collect SymbolDeclarations
    | InvalidSymbol | MissingSymbol -> []
    | _ -> [sym]

let private SymbolsInGenerator (gen : QsSpecializationGenerator) =
    match gen.Generator with
    | UserDefinedImplementation sym -> sym |> SymbolDeclarations
    | _ -> []

let private SymbolsInArgumentTuple (declName, argTuple) =
    let recur extract items =
        let extracted : (QsSymbol list * QsType list) list = items |> List.map extract
        let getSeq select = [for seq in extracted do for item in select seq do yield item]
        getSeq fst, getSeq snd
    let rec extract = function
        | QsTuple items -> [for item in items do yield item] |> recur extract
        | QsTupleItem (sym, t) -> sym |> SymbolDeclarations, t |> TypeNameSymbols
    let decl, types = argTuple |> extract
    List.concat [declName |> SymbolDeclarations; decl], ([], types, [])

let private SymbolsInCallableDeclaration (name : QsSymbol, signature : CallableSignature) =
    let symDecl, (vars, types, exs) = SymbolsInArgumentTuple (name, signature.Argument)
    let typeParams =
        let build (sym : QsSymbol) = {Type = QsTypeKind.TypeParameter sym; Range = sym.Range}
        [for param in signature.TypeParameters do yield build param]
    symDecl, (vars, List.concat [typeParams; types; signature.ReturnType |> TypeNameSymbols], exs)

type SymbolInformation = {
    DeclaredSymbols : ImmutableHashSet<QsSymbol>
    UsedVariables : ImmutableHashSet<QsSymbol>
    UsedTypes : ImmutableHashSet<QsType>
    UsedLiterals : ImmutableHashSet<QsExpression>
}
    with
    static member internal New (decl : QsSymbol list, (vars : QsSymbol list, ts : QsType list, ex : QsExpression list)) = {
        DeclaredSymbols = decl.ToImmutableHashSet();
        UsedVariables = vars.ToImmutableHashSet();
        UsedTypes = ts.ToImmutableHashSet();
        UsedLiterals = ex.ToImmutableHashSet()}

[<Extension>]
let public SymbolInformation fragmentKind =
    let chooseValues = QsNullable<_>.Choose id >> Seq.toList
    let addVariable var (syms, ts, exs) = var :: syms, ts, exs
    fragmentKind |> function
    | QsFragmentKind.ExpressionStatement                 ex -> [],                        ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.ReturnStatement                     ex -> [],                        ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.FailStatement                       ex -> [],                        ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.MutableBinding               (sym, ex) -> sym |> SymbolDeclarations, ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.ImmutableBinding             (sym, ex) -> sym |> SymbolDeclarations, ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.ValueUpdate                 (lhs, rhs) -> [],                        ([lhs;rhs], [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.IfClause                            ex -> [],                        ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.ElifClause                          ex -> [],                        ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.ElseClause                             -> [],                        ([]       , [], [])
    | QsFragmentKind.ForLoopIntro                 (sym, ex) -> sym |> SymbolDeclarations, ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.WhileLoopIntro                      ex -> [],                        ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.RepeatIntro                            -> [],                        ([]       , [], [])
    | QsFragmentKind.UntilSuccess                    (ex,_) -> [],                        ([ex]     , [])       |> collectWith SymbolsFromExpr
    | QsFragmentKind.WithinBlockIntro                       -> [],                        ([]       , [], [])
    | QsFragmentKind.ApplyBlockIntro                        -> [],                        ([]       , [], [])
    | QsFragmentKind.UsingBlockIntro            (sym, init) -> sym |> SymbolDeclarations, init                  |> VariablesInInitializer
    | QsFragmentKind.BorrowingBlockIntro        (sym, init) -> sym |> SymbolDeclarations, init                  |> VariablesInInitializer
    | QsFragmentKind.BodyDeclaration                    gen -> gen |> SymbolsInGenerator, ([], [], [])
    | QsFragmentKind.AdjointDeclaration                 gen -> gen |> SymbolsInGenerator, ([], [], [])
    | QsFragmentKind.ControlledDeclaration              gen -> gen |> SymbolsInGenerator, ([], [], [])
    | QsFragmentKind.ControlledAdjointDeclaration       gen -> gen |> SymbolsInGenerator, ([], [], [])
    | QsFragmentKind.OperationDeclaration (_, n, signature) -> (n, signature)                                   |> SymbolsInCallableDeclaration
    | QsFragmentKind.FunctionDeclaration  (_, n, signature) -> (n, signature)                                   |> SymbolsInCallableDeclaration
    | QsFragmentKind.TypeDefinition             (_, sym, t) -> (sym, t)                                         |> SymbolsInArgumentTuple
    | QsFragmentKind.DeclarationAttribute         (sym, ex) -> [], ([AttributeAsCallExpr (sym, ex)], [])        |> collectWith SymbolsFromExpr |> addVariable sym
    | QsFragmentKind.NamespaceDeclaration               sym -> sym |> SymbolDeclarations, ([], [], [])
    | QsFragmentKind.OpenDirective          (nsName, alias) -> [alias] |> chooseValues,   ([nsName], [], [])
    | QsFragmentKind.InvalidFragment                      _ -> [],                        ([], [], [])
    |> SymbolInformation.New

let rec private ExpressionsInInitializer item = item.Initializer |> function
    | QsInitializerKind.QubitRegisterAllocation ex -> seq {yield ex}
    | QsInitializerKind.QubitTupleAllocation items -> items |> Seq.collect ExpressionsInInitializer
    | _                                            -> Seq.empty

[<Extension>]
let public CallExpressions fragmentKind =
    let isCallExpression (ex : QsExpression) = ex.Expression |> function | CallLikeExpression _ -> seq {yield ex} | _ -> Enumerable.Empty()
    let callExpressions (ex : QsExpression) = (ex.ExtractAll isCallExpression).ToImmutableArray()
    fragmentKind |> function
    | QsFragmentKind.ExpressionStatement              ex
    | QsFragmentKind.ReturnStatement                  ex
    | QsFragmentKind.FailStatement                    ex
    | QsFragmentKind.MutableBinding              (_, ex)
    | QsFragmentKind.ImmutableBinding            (_, ex)
    | QsFragmentKind.ValueUpdate                 (_, ex)
    | QsFragmentKind.IfClause                         ex
    | QsFragmentKind.ElifClause                       ex
    | QsFragmentKind.ForLoopIntro                (_, ex)
    | QsFragmentKind.WhileLoopIntro                   ex
    | QsFragmentKind.UntilSuccess                (ex, _) -> callExpressions ex
    | QsFragmentKind.UsingBlockIntro           (_, init)
    | QsFragmentKind.BorrowingBlockIntro       (_, init) -> (ExpressionsInInitializer init |> Seq.collect callExpressions).ToImmutableArray()
    | QsFragmentKind.DeclarationAttribute      (sym, ex) -> AttributeAsCallExpr (sym, ex) |> ImmutableArray.Create
    | _                                                  -> ImmutableArray.Empty

let private tryResolveWith resolve extract (currentNS, source) = function
    | QsSymbolKind.Symbol sym ->
        try resolve sym (currentNS, source) |> function
            | Found decl -> Some decl, Some sym
            | _ -> None, Some sym
        with | :? ArgumentException -> None, Some sym
    | QsSymbolKind.QualifiedSymbol (ns, sym) ->
        try extract {Namespace = ns; Name = sym} (currentNS, source) |> function
            | Found decl -> Some decl, Some sym
            | _ -> None, None
        with | :? ArgumentException -> None, None
    | _ -> None, None

let private globalTypeResolution (symbolTable : NamespaceManager) (currentNS, source) (udt : QsSymbol) =
    tryResolveWith symbolTable.TryResolveAndGetType symbolTable.TryGetType (currentNS, source) udt.Symbol

let private globalCallableResolution (symbolTable : NamespaceManager) (currentNS, source) (qsSym : QsSymbol) =
    tryResolveWith symbolTable.TryResolveAndGetCallable symbolTable.TryGetCallable (currentNS, source) qsSym.Symbol

let private newLine = "    \n" // spaces first here so it will work with markdown as well
let private withNewLine line = sprintf "%s%s" line newLine

/// Converts the first character of the string to uppercase.
let private toUpperFirst (s : string) =
    s.[0..0].ToUpper() + s.[1..]

let private AsDocComment (doc : string seq) =
    if doc = null then null
    elif doc.Any() then
        let minAmountOfLeadingWS = doc |> Seq.map (fun line -> line.Length - line.TrimStart().Length) |> Seq.min
        new DocComment(doc |> Seq.map (fun line -> line.Substring(minAmountOfLeadingWS).TrimEnd()))
    else new DocComment(doc)

[<Extension>]
let public ParameterDescription (doc : string seq) paramName =
    let docComment = AsDocComment doc
    if docComment <> null then
        match docComment.Input.TryGetValue paramName with
        | true, description -> if String.IsNullOrWhiteSpace description then "" else description
        | false, _ -> ""
    else ""

[<Extension>]
let public PrintSummary (doc : string seq) markdown =
    let docComment = AsDocComment doc
    if docComment <> null then
        let info = if markdown then docComment.FullSummary else docComment.ShortSummary
        if String.IsNullOrWhiteSpace info then "" else sprintf "%s%s%s" newLine newLine info
    else ""

let private namespaceDocumentation (docs : ILookup<_, ImmutableArray<_>>, markdown) =
    // FIXME: currently all documentation for a namespace is simply given in concatenated form here
    let allDoc = docs.SelectMany(fun entry -> entry.SelectMany(fun d -> d.AsEnumerable())) // the key is the source file
    PrintSummary allDoc markdown

/// Adds a string describing the modifiers in front of the string describing a kind of declaration.
let private showModifiers kind modifiers =
    match modifiers.Access with
    | DefaultAccess -> kind
    | Internal -> "internal " + kind

type private TName () =
    inherit SyntaxTreeToQsharp.TypeTransformation()
    override this.OnCharacteristicsExpression characteristics =
        if characteristics.AreInvalid then this.Output <- "?"; characteristics
        else base.OnCharacteristicsExpression characteristics
    override this.OnInvalidType() =
        this.Output <- "?"
        InvalidType
    override this.OnUserDefinedType udt =
        this.Output <- udt.Name
        UserDefinedType udt
    member this.Apply t =
        this.OnType t |> ignore
        this.Output
let private TypeString = new TName()
let private TypeName = TypeString.Apply
let private CharacteristicsAnnotation (ex, format) =
    let charEx = SyntaxTreeToQsharp.CharacteristicsExpression ex
    if String.IsNullOrWhiteSpace charEx then "" else sprintf "is %s" charEx |> format

[<Extension>]
let public TypeInfo (symbolTable : NamespaceManager) (currentNS, source) (qsType : QsType) markdown =
    let udtInfo udt =
        match udt |> globalTypeResolution symbolTable (currentNS, source) with
        | Some decl, _ ->
            let kind = showModifiers "user-defined type" decl.Modifiers |> toUpperFirst
            let name = decl.QualifiedName.Name |> withNewLine
            let ns = sprintf "Namespace: %s" decl.QualifiedName.Namespace |> withNewLine
            let info = sprintf "Underlying type: %s" (TypeName decl.Type)
            let doc = PrintSummary decl.Documentation markdown
            sprintf "%s %s%s%s%s" kind name ns info doc
        | None, Some sym -> sprintf "Type %s" sym
        | _ -> "?"
    let typeParamName onUnknown (sym : QsSymbol) =
        let name = sym.Symbol |> function
            | Symbol name -> name
            | _ -> null
        if String.IsNullOrWhiteSpace name then onUnknown else sprintf "'%s" name

    let rec typeName tKind =
        let udtName udt =
            match udt |> globalTypeResolution symbolTable (currentNS, source) with
            | Some _, Some sym -> sym // hovering over the udt will show its namespace and source
            | _ -> "?"
        let characteristics ex = CharacteristicsAnnotation (QsType.TryResolve ex, sprintf " %s")
        let inner (ts : ImmutableArray<QsType>) = [for t in ts do yield t.Type] |> List.map typeName
        match tKind with
        | QsTypeKind.UserDefinedType udt          -> udt |> udtName
        | QsTypeKind.ArrayType b                  -> sprintf "%s[]" (b.Type |> typeName)
        | QsTypeKind.TupleType ts                 -> sprintf "(%s)" (ts |> inner |> String.concat ", ")
        | QsTypeKind.TypeParameter p              -> p |> typeParamName "?"
        | QsTypeKind.Operation ((it, ot), cs)     -> sprintf "(%s => %s%s)" (it.Type |> typeName) (ot.Type |> typeName) (cs |> characteristics)
        | QsTypeKind.Function (it, ot)            -> sprintf "(%s -> %s)" (it.Type |> typeName) (ot.Type |> typeName)
        | QsTypeKind.UnitType                     -> QsTypeKind.UnitType    |> ResolvedType.New |> TypeName
        | QsTypeKind.Int                          -> QsTypeKind.Int         |> ResolvedType.New |> TypeName
        | QsTypeKind.BigInt                       -> QsTypeKind.BigInt      |> ResolvedType.New |> TypeName
        | QsTypeKind.Double                       -> QsTypeKind.Double      |> ResolvedType.New |> TypeName
        | QsTypeKind.Bool                         -> QsTypeKind.Bool        |> ResolvedType.New |> TypeName
        | QsTypeKind.String                       -> QsTypeKind.String      |> ResolvedType.New |> TypeName
        | QsTypeKind.Qubit                        -> QsTypeKind.Qubit       |> ResolvedType.New |> TypeName
        | QsTypeKind.Result                       -> QsTypeKind.Result      |> ResolvedType.New |> TypeName
        | QsTypeKind.Pauli                        -> QsTypeKind.Pauli       |> ResolvedType.New |> TypeName
        | QsTypeKind.Range                        -> QsTypeKind.Range       |> ResolvedType.New |> TypeName
        | QsTypeKind.MissingType                  -> QsTypeKind.MissingType |> ResolvedType.New |> TypeName
        | QsTypeKind.InvalidType                  -> QsTypeKind.InvalidType |> ResolvedType.New |> TypeName

    let doc = PrintSummary qsType.Documentation markdown
    match qsType.Type with
    | QsTypeKind.UserDefinedType udt -> udtInfo udt
    | QsTypeKind.TypeParameter p     -> sprintf "Type parameter %s%s" (p |> typeParamName "") doc
    | _                              -> sprintf "Built-in type %s%s" (typeName qsType.Type) doc

let private printCallableKind = function
    | QsCallableKind.Function -> "function"
    | QsCallableKind.Operation -> "operation"
    | QsCallableKind.TypeConstructor -> "type constructor"

[<Extension>]
let public PrintArgumentTuple item =
    SyntaxTreeToQsharp.ArgumentTuple (item, new Func<_,_>(TypeName)) // note: needs to match the corresponding part of the output constructed by PrintSignature below!

[<Extension>]
let public PrintSignature (header : CallableDeclarationHeader) =
    let callable =
        QsCallable.New header.Kind (header.SourceFile, Null) (header.QualifiedName,
                                                              header.Attributes,
                                                              header.Modifiers,
                                                              header.ArgumentTuple,
                                                              header.Signature,
                                                              ImmutableArray.Empty,
                                                              ImmutableArray.Empty,
                                                              QsComments.Empty)
    let signature = SyntaxTreeToQsharp.DeclarationSignature (callable, new Func<_,_>(TypeName))
    let annotation = CharacteristicsAnnotation (header.Signature.Information.Characteristics, sprintf "%s%s" newLine)
    sprintf "%s%s" signature annotation

[<Extension>]
let public VariableInfo (symbolTable : NamespaceManager) (locals : LocalDeclarations) (currentNS, source) (qsSym : QsSymbol) markdown =
    match qsSym |> globalCallableResolution symbolTable (currentNS, source) with
    | Some decl, _ ->
        let kind = showModifiers (printCallableKind decl.Kind) decl.Modifiers |> toUpperFirst
        let nameAndSignature = PrintSignature decl |> withNewLine
        let ns = sprintf "Namespace: %s" decl.QualifiedName.Namespace
        let doc = PrintSummary decl.Documentation markdown
        sprintf "%s %s%s%s" kind nameAndSignature ns doc
    | None, Some sym ->
        let localVars = locals.AsVariableLookup()
        if localVars.ContainsKey sym then
            let decl = localVars.[sym]
            let kind = if decl.InferredInformation.IsMutable then "Mutable" else "Immutable"
            sprintf "%s variable %s%sType: %s" kind sym newLine (TypeName decl.Type)
        else symbolTable.Documentation().TryGetValue(sym) |> function
            | true, docs -> sprintf "Namespace %s%s" sym (namespaceDocumentation (docs, markdown))
            | false,_ -> sprintf "Variable %s" sym
    | _ -> qsSym.Symbol |> function
        | QualifiedSymbol (ns, sym) -> sprintf "%s.%s" ns sym
        | _ -> "Unknown symbol"

[<Extension>]
let public DeclarationInfo symbolTable (locals : LocalDeclarations) (currentNS, source) (qsSym : QsSymbol) markdown =
    match qsSym.Symbol with
    | QsSymbolKind.Symbol name ->
        match locals.AsVariableLookup().TryGetValue name with
        | true, decl ->
            let kind = if decl.InferredInformation.IsMutable then "a mutable" else "an immutable"
            let name = name |> withNewLine
            let info = sprintf "Type: %s" (TypeName decl.Type)
            sprintf "Declaration of %s variable %s%s" kind name info
        | false, _ ->
        match qsSym |> globalTypeResolution symbolTable (currentNS, source) with // needs to be before querying callables
        | Some decl, _ ->
            let kind = showModifiers "user-defined type" decl.Modifiers
            let name = decl.QualifiedName.Name |> withNewLine
            let ns = sprintf "Namespace: %s" decl.QualifiedName.Namespace |> withNewLine
            let info = sprintf "Underlying type: %s" (decl.Type |> TypeName)
            let doc = PrintSummary decl.Documentation markdown
            sprintf "Declaration of %s %s%s%s%s" kind name ns info doc
        | None, _ ->
        match qsSym |> globalCallableResolution symbolTable (currentNS, source) with
        | Some decl, _ ->
            let kind = showModifiers (printCallableKind decl.Kind) decl.Modifiers
            let name = decl.QualifiedName.Name |> withNewLine
            let ns = sprintf "Namespace: %s" decl.QualifiedName.Namespace |> withNewLine
            let input = sprintf "Input type: %s" (decl.Signature.ArgumentType |> TypeName) |> withNewLine
            let output = sprintf "Output type: %s" (decl.Signature.ReturnType |> TypeName) |> withNewLine
            let functorSupport characteristics =
                let charEx = SyntaxTreeToQsharp.CharacteristicsExpression characteristics
                if String.IsNullOrWhiteSpace charEx then "(None)" else charEx
            let fs = sprintf "Supported functors: %s" (decl.Signature.Information.Characteristics |> functorSupport)
            let doc = PrintSummary decl.Documentation markdown
            sprintf "Declaration of %s %s%s%s%s%s%s" kind name ns input output fs doc
        | None, _ ->
        match symbolTable.Documentation().TryGetValue name with
        | true, docs -> sprintf "Declaration of a partial namespace %s%s" name (namespaceDocumentation (docs, markdown))
        | false, _ -> sprintf "Symbol declaration %s" name
    | QsSymbolKind.MissingSymbol -> "Discarded symbol assignment"
    | QsSymbolKind.OmittedSymbols -> "Omitted symbols representing the argument of the top level declaration"
    | _ -> "Invalid symbol declaration"

[<Extension>]
let public LiteralInfo (ex : QsExpression) markdown =
    ex.Expression |> function
    | QsExpressionKind.UnitValue                     -> sprintf "Built-in Unit literal %s"   (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.IntLiteral _                  -> sprintf "Built-in Int literal %s"    (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.BigIntLiteral _               -> sprintf "Built-in BigInt literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.DoubleLiteral _               -> sprintf "Built-in Double literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.BoolLiteral _                 -> sprintf "Built-in Bool literal %s"   (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.StringLiteral _               -> sprintf "Built-in String literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.ResultLiteral _               -> sprintf "Built-in Result literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.PauliLiteral _                -> sprintf "Build-in Pauli literal %s"  (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.MissingExpr                   -> "Omitted argument used within partial applications"
    | QsExpressionKind.InvalidExpr                   -> "?"
    | _                                              -> QsCompilerError.Raise "no string defined for given expression in query to LiteralInfo"; ""


// extensions providing information for editor commands

/// Returns all of the operation characteristics found in the given type.
[<Extension>]
let rec ExtractCharacteristics (qsType : QsType) =
    let extract (t : QsType) =
        match t.Type with
        | QsTypeKind.Operation ((_,_), characteristics) -> seq {yield characteristics}
        | _ -> Seq.empty
    qsType.ExtractAll extract

[<Extension>]
let public LocalVariable (locals : LocalDeclarations) (qsSym : QsSymbol) =
    match qsSym.Symbol with
    | QsSymbolKind.Symbol sym ->
        match locals.AsVariableLookup().TryGetValue sym with
        | true, decl ->
            let noPositionInfoException = ArgumentException "no position information available for local variable"
            let position = decl.Position.ValueOrApply (fun () -> noPositionInfoException |> raise)
            (sym, position, decl.Range) |> Value
        | false, _ -> Null
    | _ -> Null

[<Extension>]
let public VariableDeclaration (symbolTable : NamespaceManager) (locals : LocalDeclarations) (currentNS, source) (qsSym : QsSymbol) =
    match qsSym |> globalCallableResolution symbolTable (currentNS, source) with
    | Some decl, Some _ -> decl.Location |> QsNullable<_>.Map (fun loc -> decl.SourceFile, loc.Offset, loc.Range)
    | _ -> LocalVariable locals qsSym |> QsNullable<_>.Map (fun (_, pos, range) -> source, pos, range)

[<Extension>]
let public TypeDeclaration (symbolTable : NamespaceManager) (currentNS, source) (qsType : QsType) =
    match qsType.Type with
    | QsTypeKind.UserDefinedType udt ->
        match udt |> globalTypeResolution symbolTable (currentNS, source) with
        | Some decl, _ -> decl.Location |> QsNullable<_>.Map (fun loc -> decl.SourceFile, loc.Offset, loc.Range)
        | _ -> Null
    | _ -> Null

[<Extension>]
let public SymbolDeclaration (symbolTable : NamespaceManager) (locals : LocalDeclarations) (currentNS, source) (qsSym : QsSymbol) =
    match qsSym.Symbol with
    | QsSymbolKind.Symbol _ ->
        match qsSym |> globalTypeResolution symbolTable (currentNS, source) with // needs to be first
        | Some decl, _ -> decl.Location |> QsNullable<_>.Map (fun loc -> decl.SourceFile, loc.Offset, loc.Range)
        | None, _ ->
        match qsSym |> globalCallableResolution symbolTable (currentNS, source) with
        | Some decl, _ -> decl.Location |> QsNullable<_>.Map (fun loc -> decl.SourceFile, loc.Offset, loc.Range)
        | _ -> LocalVariable locals qsSym |> QsNullable<_>.Map (fun (_, pos, range) -> source, pos, range)
    | _ -> Null
