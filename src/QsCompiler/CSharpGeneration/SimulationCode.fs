// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.CsharpGeneration

#nowarn "46" // Backticks removed by Fantomas: https://github.com/fsprojects/fantomas/issues/2034

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open System.Reflection
open System.Text.RegularExpressions

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Formatting

open Microsoft.Quantum.RoslynWrapper
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations


/// ---------------------------------------------------------------------------
/// The code generation for the simulation runtime. C# code
/// for Quantum simulation is generated using the Roslyn compiler.
/// It uses BrightSword's (John Azariah's) F# wrapper for easier readability.
/// ---------------------------------------------------------------------------
module SimulationCode =
    open System.Globalization

    type CodegenContext with
        member this.setCallable(op: QsCallable) =
            { this with current = (Some op.FullName); signature = (Some op.Signature) }

        member this.setUdt(udt: QsCustomType) =
            { this with current = (Some udt.FullName) }

    let autoNamespaces =
        [
            "System"
            "Microsoft.Quantum.Core"
            "Microsoft.Quantum.Intrinsic"
            "Microsoft.Quantum.Simulation.Core"
        ]

    let autoNamespacesWithInterfaces =
        [
            "System"
            "Microsoft.Quantum.Core"
            "Microsoft.Quantum.Intrinsic"
            "Microsoft.Quantum.Intrinsic.Interfaces"
            "Microsoft.Quantum.Simulation.Core"
        ]

    let funcsAsProps =
        [
            ("Length", { Namespace = "Microsoft.Quantum.Core"; Name = "Length" })
            ("Start", { Namespace = "Microsoft.Quantum.Core"; Name = "RangeStart" })
            ("End", { Namespace = "Microsoft.Quantum.Core"; Name = "RangeEnd" })
            ("Step", { Namespace = "Microsoft.Quantum.Core"; Name = "RangeStep" })
        ]

    let internal userDefinedName (parent: QsQualifiedName option) name =
        let isReserved =
            match name with
            | "Data"
            | "Deconstruct"
            | "Info"
            | "Run" -> true
            | _ -> Regex.IsMatch(name, @"^Item\d+$") || parent |> Option.exists (fun current' -> name = current'.Name)

        if isReserved then name + "__" else name

    let isCurrentOp context n =
        match context.current with
        | None -> false
        | Some name -> name = n

    let prependNamespaceString (name: QsQualifiedName) =
        name.Namespace.Replace(".", "__") + "__" + name.Name

    let isConcreteIntrinsic (context: CodegenContext) =
        match context.assemblyConstants.TryGetValue AssemblyConstants.GenerateConcreteIntrinsic with
        | true, "false" -> false
        | true, _ -> true
        | false, _ -> false

    let needsFullPath context (op: QsQualifiedName) =
        let hasMultipleDefinitions () =
            if context.byName.ContainsKey op.Name then context.byName.[op.Name].Length > 1 else false

        let sameNamespace =
            match context.current with
            | None -> false
            | Some n -> n.Namespace = op.Namespace

        let namespaces = if isConcreteIntrinsic context then autoNamespacesWithInterfaces else autoNamespaces

        if sameNamespace then false
        elif hasMultipleDefinitions () then true
        else not (namespaces |> List.contains op.Namespace)

    let getOpName context n =
        if isCurrentOp context n then Directives.Self
        elif needsFullPath context n then prependNamespaceString n
        else n.Name + "__"

    let getTypeParameters types =
        let findAll (t: ResolvedType) =
            t.ExtractAll (fun item ->
                match item.Resolution with
                | QsTypeKind.TypeParameter tp -> seq { tp }
                | _ -> Seq.empty)

        types |> Seq.collect findAll |> Seq.distinctBy (fun tp -> tp.Origin, tp.TypeName) |> Seq.toList

    let getAllItems itemBase t =
        let rec getItems (acc: Queue<ExpressionSyntax>) current =
            function
            | Tuple ts ->
                ts |> Seq.iteri (fun i x -> getItems acc (current <|.|> ident ("Item" + (i + 1).ToString())) x)
            | _ -> acc.Enqueue current

        let items = Queue()
        getItems items itemBase t
        items

    let hasTypeParameters types = not (getTypeParameters types).IsEmpty

    let justTheName context (n: QsQualifiedName) =
        let name = userDefinedName None n.Name
        if needsFullPath context n then n.Namespace + "." + name else name

    let isGeneric context (n: QsQualifiedName) =
        if context.allCallables.ContainsKey n then
            let signature = context.allCallables.[n].Signature
            not signature.TypeParameters.IsEmpty
        else
            false

    let findUdt context (name: QsQualifiedName) = context.allUdts.[name]

    let isUdt context (name: QsQualifiedName) = context.allUdts.TryGetValue name

    let inAndOutputType (qsharpType: ResolvedType) =
        match qsharpType.Resolution with
        | QsTypeKind.Operation ((tIn, tOut), _) -> (tIn, tOut)
        | QsTypeKind.Function (tIn, tOut) -> (tIn, tOut)
        // TODO: Diagnostics
        | _ -> failwith "Invalid ResolvedType for callable definition"

    let hasAdjointControlled functors =
        let oneFunctor (adj, ctrl) f =
            match f with
            | QsFunctor.Adjoint -> (true, ctrl)
            | QsFunctor.Controlled -> (adj, true)

        match functors with
        | Value fs -> fs |> Seq.fold oneFunctor (false, false)
        // TODO: Diagnostics
        | Null -> (true, true)

    // Maps Q# types to their corresponding Roslyn type
    let rec roslynTypeName context (qsharpType: ResolvedType) : string =
        match qsharpType.Resolution with
        | QsTypeKind.UnitType -> "QVoid"
        | QsTypeKind.Int -> "Int64"
        | QsTypeKind.BigInt -> "System.Numerics.BigInteger"
        | QsTypeKind.Double -> "Double"
        | QsTypeKind.Bool -> "Boolean"
        | QsTypeKind.String -> "String"
        | QsTypeKind.Qubit -> "Qubit"
        | QsTypeKind.Result -> "Result"
        | QsTypeKind.Pauli -> "Pauli"
        | QsTypeKind.Range -> "QRange"
        | QsTypeKind.ArrayType arrayType -> sprintf "IQArray<%s>" (arrayType |> roslynTypeName context)
        | QsTypeKind.TupleType tupleType -> tupleType |> roslynTupleTypeName context
        | QsTypeKind.UserDefinedType name -> justTheName context (QsQualifiedName.New(name.Namespace, name.Name))
        | QsTypeKind.Operation (_, functors) -> roslynCallableInterfaceName functors.Characteristics
        | QsTypeKind.Function _ -> roslynCallableInterfaceName ResolvedCharacteristics.Empty
        | QsTypeKind.TypeParameter t -> t |> roslynTypeParameterName
        | QsTypeKind.MissingType -> "object"
        // TODO: diagnostics
        | QsTypeKind.InvalidType -> ""

    and roslynTupleTypeName context tupleTypes =
        tupleTypes |> Seq.map (roslynTypeName context) |> String.concat "," |> sprintf "(%s)"

    and roslynTypeParameterName (t: QsTypeParameter) = sprintf "__%s__" t.TypeName

    and roslynCallableInterfaceName characteristics =
        let (adj, ctrl) = characteristics.SupportedFunctors |> hasAdjointControlled

        match (adj, ctrl) with
        | (true, true) -> "IUnitary"
        | (true, false) -> "IAdjointable"
        | (false, true) -> "IControllable"
        | _ -> "ICallable"

    and roslynCallableTypeName context (name: QsQualifiedName) =
        if not (context.allCallables.ContainsKey name) then
            userDefinedName None name.Name
        else
            let signature = context.allCallables.[name].Signature
            let tIn = signature.ArgumentType
            let tOut = signature.ReturnType
            let baseInterface = roslynCallableInterfaceName signature.Information.Characteristics

            if isGeneric context name then
                baseInterface
            else
                match baseInterface with
                | "ICallable" ->
                    sprintf "%s<%s, %s>" baseInterface (roslynTypeName context tIn) (roslynTypeName context tOut)
                | _ -> sprintf "%s<%s>" baseInterface (roslynTypeName context tIn)

    let isTuple =
        function
        | QsTypeKind.TupleType _ -> true
        | _ -> false

    let isCallable (qsharpType: ResolvedType) =
        match qsharpType.Resolution with
        | QsTypeKind.Operation _
        | QsTypeKind.Function _ -> true
        | _ -> false

    let tupleBaseClassName context qsharpType =
        let baseType = (roslynTypeName context qsharpType)
        sprintf "QTuple<%s>" baseType

    let udtBaseClassName context qsharpType =
        let baseType = (roslynTypeName context qsharpType)
        sprintf "UDTBase<%s>" baseType

    // Top-level and public for testing
    let floatToString (f: double) =
        sprintf "%sD" (f.ToString("R", CultureInfo.InvariantCulture))

    let mutable private count = 0

    let private nextArgName () =
        count <- count + 1
        sprintf "__arg%d__" count

    type ExpressionSeeker(parent: SyntaxTreeTransformation<HashSet<QsQualifiedName>>) =
        inherit ExpressionTransformation<HashSet<QsQualifiedName>>(parent, TransformationOptions.NoRebuild)

        override this.OnTypedExpression ex =
            match ex.Expression with
            | Identifier (id, _) ->
                match id with
                | GlobalCallable name -> this.SharedState.Add name |> ignore
                | _ -> ()
            | _ -> ()

            base.OnTypedExpression ex

    /// Used to discover which operations are used by a certain code block.
    type StatementKindSeeker(parent: SyntaxTreeTransformation<HashSet<QsQualifiedName>>) =
        inherit StatementKindTransformation<HashSet<QsQualifiedName>>(parent, TransformationOptions.NoRebuild)

        let ALLOCATE = { Name = "Allocate"; Namespace = "Microsoft.Quantum.Intrinsic" }
        let RELEASE = { Name = "Release"; Namespace = "Microsoft.Quantum.Intrinsic" }
        let BORROW = { Name = "Borrow"; Namespace = "Microsoft.Quantum.Intrinsic" }
        let RETURN = { Name = "Return"; Namespace = "Microsoft.Quantum.Intrinsic" }

        override this.OnAllocateQubits node =
            this.SharedState.Add ALLOCATE |> ignore
            this.SharedState.Add RELEASE |> ignore
            base.OnAllocateQubits node

        override this.OnBorrowQubits node =
            this.SharedState.Add BORROW |> ignore
            this.SharedState.Add RETURN |> ignore
            base.OnBorrowQubits node

    /// Used to discover which operations are used by a certain code block.
    type OperationsSeeker() as this =
        inherit SyntaxTreeTransformation<HashSet<QsQualifiedName>>(HashSet(), TransformationOptions.NoRebuild)

        do
            this.StatementKinds <- StatementKindSeeker this
            this.Expressions <- ExpressionSeeker this
            this.Types <- TypeTransformation<HashSet<QsQualifiedName>>(this, TransformationOptions.Disabled)

    type SyntaxBuilder(context) as this =
        inherit SyntaxTreeTransformation(TransformationOptions.NoRebuild)

        do
            this.Namespaces <- NamespaceBuilder this
            this.Statements <- StatementBlockBuilder this
            this.StatementKinds <- StatementBuilder(this, context)
            this.Expressions <- ExpressionTransformation(this, TransformationOptions.Disabled)
            this.Types <- TypeTransformation(this, TransformationOptions.Disabled)

        member val DeclarationsInStatement = LocalDeclarations.Empty with get, set
        member val DeclarationsInScope = LocalDeclarations.Empty with get, set

        member val BuiltStatements = [] with get, set

        member val StartLine = None with get, set
        member val LineNumber = None with get, set

    /// Used to generate the list of statements that implement a Q# operation specialization.
    and StatementBlockBuilder(parent: SyntaxBuilder) =
        inherit StatementTransformation(parent, TransformationOptions.NoRebuild)

        override this.OnScope(scope: QsScope) =
            parent.DeclarationsInScope <- scope.KnownSymbols
            base.OnScope scope

        override this.OnStatement(node: QsStatement) =
            match node.Location with
            | Value loc ->
                let current = loc.Offset.Line
                parent.LineNumber <- parent.StartLine |> Option.map (fun start -> start + current + 1) // The Q# compiler reports 0-based line numbers.
            | Null -> parent.LineNumber <- None // auto-generated statement; the line number will be set to the specialization declaration

            parent.DeclarationsInStatement <- node.SymbolDeclarations

            parent.DeclarationsInScope <-
                LocalDeclarations.Concat parent.DeclarationsInScope parent.DeclarationsInStatement // only fine because/if a new statement transformation is created for every block!

            base.OnStatement node

    /// Used to generate the statements that implement a Q# operation specialization.
    and StatementBuilder(parent: SyntaxBuilder, context) =
        inherit StatementKindTransformation(parent, TransformationOptions.NoRebuild)

        let withLineNumber s =
            // add a line directive if the operation specifies the source file and a line number
            match context.fileName, parent.LineNumber with
            | Some _, Some ln when ln = 0 -> ``#line hidden`` <| s
            | Some n, Some ln -> ``#line`` ln n s
            | Some n, None ->
                parent.StartLine
                |> function
                    | Some ln -> ``#line`` (ln + 1) n s // we need 1-based line numbers here, and startLine is zero-based
                    | None -> s
            | _ -> s

        let QArrayType =
            function
            | ArrayType b -> generic "QArray" ``<<`` [ roslynTypeName context b ] ``>>`` |> Some
            | _ -> None

        let (|Property|_|) =
            function
            | CallLikeExpression (op: TypedExpression, args) ->
                match op.Expression with
                | Identifier (id, _) ->
                    match id with
                    | GlobalCallable n ->
                        funcsAsProps |> List.tryPick (fun (prop, f) -> if (n = f) then Some(args, prop) else None)
                    | _ -> None
                | _ -> None
            | _ -> None

        let (|NewUdt|_|) =
            function
            | CallLikeExpression (op: TypedExpression, args) ->
                match op.Expression with
                | Identifier (id, _) ->
                    match id with
                    | GlobalCallable n when isUdt context n |> fst -> Some(n, args)
                    | _ -> None
                | _ -> None
            | _ -> None

        let (|PartialApplication|_|) expression =
            match expression with
            | CallLikeExpression (op, args) when TypedExpression.IsPartialApplication expression -> Some(op, args)
            | _ -> None

        let addStatement s =
            parent.BuiltStatements <- parent.BuiltStatements @ [ withLineNumber s ]

        // Builds Roslyn code for a Q# expression
        let rec buildExpression (ex: TypedExpression) =
            match ex.Expression with
            // TODO: Diagnostics
            | InvalidExpr -> failwith "Can't generate code for invalid expression."
            | Lambda _ -> failwith "Can't generate code for un-lifted lambda."
            | UnitValue -> (ident "QVoid") <|.|> (ident "Instance")
            | IntLiteral i -> literal i
            | BigIntLiteral b ->
                invoke
                    (ident "System.Numerics.BigInteger.Parse")
                    ``(``
                    [ literal (b.ToString("R", CultureInfo.InvariantCulture)) ]
                    ``)``
            // otherwise converts x.0 to int, see https://stackoverflow.com/questions/24299692/why-is-a-round-trip-conversion-via-a-string-not-safe-for-a-double
            | DoubleLiteral f -> ident (floatToString f) :> ExpressionSyntax
            | BoolLiteral b -> upcast (if b then ``true`` else ``false``)
            | ResultLiteral r ->
                let name =
                    match r with
                    | Zero -> "Zero"
                    | One -> "One"

                ident "Result" <|.|> ident name
            | PauliLiteral p ->
                let name =
                    match p with
                    | PauliI -> "PauliI"
                    | PauliX -> "PauliX"
                    | PauliY -> "PauliY"
                    | PauliZ -> "PauliZ"

                ident "Pauli" <|.|> ident name
            | Identifier (id, _) -> buildId id
            | StringLiteral (s, e) -> buildInterpolatedString s e
            | RangeLiteral (r, e) -> buildRange r e
            | NEG n -> ``-`` (buildExpression n)
            | NOT r -> !(buildExpression r)
            | BNOT i -> Expressions.``~~~`` (buildExpression i)
            | ADD (l, r) -> buildAddExpr ex.ResolvedType l r
            // We use the Pow extension method from Microsoft.Quantum.Simulation.Core for all valid combinations of types.
            | POW (l, r) -> invoke ((buildExpression l) <|.|> (ident "Pow")) ``(`` [ (buildExpression r) ] ``)``
            | SUB (l, r) -> ``((`` ((buildExpression l) <-> (buildExpression r)) ``))``
            | MUL (l, r) -> ``((`` ((buildExpression l) <*> (buildExpression r)) ``))``
            | DIV (l, r) -> ``((`` ((buildExpression l) </> (buildExpression r)) ``))``
            | MOD (l, r) -> ``((`` ((buildExpression l) <%> (buildExpression r)) ``))``
            | EQ (l, r) -> ``((`` ((buildExpression l) .==. (buildExpression r)) ``))``
            | NEQ (l, r) -> ``((`` ((buildExpression l) .!=. (buildExpression r)) ``))``
            | AND (l, r) -> ``((`` ((buildExpression l) .&&. (buildExpression r)) ``))``
            | OR (l, r) -> ``((`` ((buildExpression l) .||. (buildExpression r)) ``))``
            | BOR (l, r) -> ``((`` ((buildExpression l) .|||. (buildExpression r)) ``))``
            | BAND (l, r) -> ``((`` ((buildExpression l) .&&&. (buildExpression r)) ``))``
            | BXOR (l, r) -> ``((`` ((buildExpression l) .^^^. (buildExpression r)) ``))``
            | LSHIFT (l, r) -> ``((`` ((buildExpression l) .<<<. (cast "int" (buildExpression r))) ``))``
            | RSHIFT (l, r) -> ``((`` ((buildExpression l) .>>>. (cast "int" (buildExpression r))) ``))``
            | LT (l, r) -> ``((`` ((buildExpression l) .<. (buildExpression r)) ``))``
            | LTE (l, r) -> ``((`` ((buildExpression l) .<=. (buildExpression r)) ``))``
            | GT (l, r) -> ``((`` ((buildExpression l) .>. (buildExpression r)) ``))``
            | GTE (l, r) -> ``((`` ((buildExpression l) .>=. (buildExpression r)) ``))``
            | CONDITIONAL (c, t, f) -> ``((`` (buildConditional c t f) ``))``
            | CopyAndUpdate (l, i, r) -> buildCopyAndUpdateExpression (l, i, r)
            | UnwrapApplication e -> (buildExpression e) <|.|> (ident "Data")
            | ValueTuple vs -> buildTuple vs
            | NamedItem (ex, acc) -> buildNamedItem ex acc
            | ArrayItem (a, i) -> buildArrayItem a i
            | ValueArray elems -> buildValueArray ex.ResolvedType elems
            | SizedArray (value, size) -> buildSizedArray value size
            | NewArray (t, expr) -> buildNewArray t expr
            | AdjointApplication op -> (buildExpression op) <|.|> (ident "Adjoint")
            | ControlledApplication op -> (buildExpression op) <|.|> (ident "Controlled")
            | Property (elem, prop) -> (buildExpression elem) <|.|> (ident prop)
            | PartialApplication (op, args) -> buildPartial ex.ResolvedType ex.TypeParameterResolutions op args // needs to be before NewUdt!
            | NewUdt (udt, args) -> buildNewUdt udt args // needs to be before CallLikeExpression!
            | CallLikeExpression (op, args) -> buildApply ex.ResolvedType op args
            | MissingExpr -> ident "_" :> ExpressionSyntax

        and captureExpression (ex: TypedExpression) =
            match ex.Expression with
            | Identifier (s, _) when ex.InferredInformation.IsMutable ->
                match ex.ResolvedType.Resolution with
                | QsTypeKind.ArrayType _ -> invoke (buildId s <|?.|> (ident "Copy")) ``(`` [] ``)``
                | _ -> buildExpression ex
            | _ -> buildExpression ex

        and buildNamedItem ex acc =
            match acc with
            | LocalVariable name ->
                let name' =
                    match ex.ResolvedType.Resolution with
                    | UserDefinedType udt ->
                        name |> userDefinedName (Some { Namespace = udt.Namespace; Name = udt.Name })
                    | _ -> name

                buildExpression ex <|.|> ident name'
            | _ ->
                // TODO: Diagnostics
                failwith "Invalid identifier for named item"

        and buildAddExpr (exType: ResolvedType) lhs rhs =
            match exType.Resolution |> QArrayType with
            | Some arrType -> arrType <.> (ident "Add", [ buildExpression lhs; buildExpression rhs ])
            | _ -> ``((`` ((buildExpression lhs) <+> (buildExpression rhs)) ``))``

        and buildInterpolatedString (s: string) (exs: ImmutableArray<TypedExpression>) =
            if exs.Length <> 0 then
                let exprs = exs |> Seq.map buildExpression |> Seq.toList
                invoke (ident "String.Format") ``(`` (literal s :: exprs) ``)``
            else
                literal s

        and buildId id : ExpressionSyntax =
            match id with
            | LocalVariable n -> n |> ident :> ExpressionSyntax
            | GlobalCallable n -> getOpName context n |> ident :> ExpressionSyntax
            | InvalidIdentifier ->
                // TODO: Diagnostics
                failwith "Received InvalidIdentifier"

        and buildCopyAndUpdateExpression (lhsEx: TypedExpression, accEx: TypedExpression, rhsEx) =
            match lhsEx.ResolvedType.Resolution |> QArrayType with
            | Some arrayType ->
                let lhsAsQArray = ``new`` arrayType ``(`` [ buildExpression lhsEx ] ``)``
                lhsAsQArray <.> (ident "Modify", [ buildExpression accEx; captureExpression rhsEx ]) // in-place modification
            | _ ->
                lhsEx.ResolvedType.Resolution
                |> function
                    | UserDefinedType udt ->
                        let name = QsQualifiedName.New(udt.Namespace, udt.Name)
                        let decl = findUdt context name

                        let isUserDefinedType =
                            function
                            | UserDefinedType _ -> true
                            | _ -> false

                        let getItemName =
                            function
                            | Identifier (LocalVariable id, Null) -> id
                            // TODO: Diagnostics
                            | _ ->
                                failwith
                                    "item access expression in copy-and-update expression for user defined type is not a suitable identifier"

                        let updatedItems = new Dictionary<string, ExpressionSyntax>()

                        let rec aggregate (lhs: TypedExpression) =
                            match lhs.Expression with
                            | CopyAndUpdate (l, i, r) when l.ResolvedType.Resolution |> isUserDefinedType ->
                                let lhs = aggregate l // need to recur first, or make sure key is not already in dictionary
                                updatedItems.[getItemName i.Expression] <- captureExpression r
                                lhs
                            | _ -> lhs

                        let lhs = aggregate lhsEx |> buildExpression
                        updatedItems.[getItemName accEx.Expression] <- captureExpression rhsEx // needs to be after aggregate

                        let root = lhs <|.|> (ident "Data")
                        let items = getAllItems root decl.Type

                        let rec buildArg =
                            function
                            | QsTuple args -> args |> Seq.map buildArg |> Seq.toList |> tuple
                            | QsTupleItem (Named item) ->
                                updatedItems.TryGetValue item.VariableName
                                |> function
                                    | true, rhs ->
                                        items.Dequeue() |> ignore
                                        rhs
                                    | _ -> items.Dequeue()
                            | QsTupleItem _ -> items.Dequeue()

                        ``new`` (``type`` [ justTheName context name ]) ``(`` [ buildArg decl.TypeItems ] ``)``
                    | _ ->
                        failwith
                            "copy-and-update expressions are currently only supported for arrays and user defined types"

        and buildTuple many : ExpressionSyntax =
            many |> Seq.map captureExpression |> Seq.toList |> tuple // captured since we rely on the native C# tuples

        and buildPartial (partialType: ResolvedType) typeParamResolutions opEx args =
            let (pIn, pOut) = inAndOutputType partialType // The type of the operation constructed by partial application
            let (oIn, _) = inAndOutputType opEx.ResolvedType // The type of the operation accepting the partial tuples.

            let buildPartialMapper () = // may only be executed if there are no more type parameters to be resolved
                let argName = nextArgName ()
                let items = getAllItems (ident argName) pIn

                let rec argMapping (expr: TypedExpression) =
                    let rec buildMissing =
                        function
                        | Tuple ts -> ts |> Seq.toList |> List.map buildMissing |> tuple
                        | _ -> items.Dequeue()

                    match expr with
                    | Missing -> buildMissing expr.ResolvedType
                    | Tuple vt ->
                        match expr.ResolvedType with
                        | Tuple ts when ts.Length = vt.Length ->
                            vt
                            |> Seq.zip ts
                            |> Seq.toList
                            |> List.map (fun (t, v) -> argMapping { v with ResolvedType = t })
                            |> tuple
                        // TODO: Diagnostics.
                        | _ -> failwith "invalid input to code gen in partial application"
                    | Item ex -> captureExpression ex
                    // TODO: Diagnostics.
                    | _ -> failwith "partial application contains an error expression"

                let resolvedOrigInputT = ResolvedType.ResolveTypeParameters typeParamResolutions oIn

                let mapper =
                    [
                        ``() =>`` [ argName ] (argMapping { args with ResolvedType = resolvedOrigInputT })
                    ]

                ``new``
                    (generic
                        "Func"
                        ``<<``
                        [ (roslynTypeName context pIn); (roslynTypeName context resolvedOrigInputT) ]
                        ``>>``)
                    ``(``
                    mapper
                    ``)``

            // Checks if the expression still has type parameters.
            // If it does, we can't create the PartialMapper at compile time
            // so we just build a partial-tuple and let it be resolved at runtime.
            let op = buildExpression opEx
            let values = if hasTypeParameters [ pIn; pOut ] then captureExpression args else buildPartialMapper ()
            op <.> (ident "Partial", [ values ])

        and buildNewUdt n args =
            ``new`` (``type`` [ justTheName context n ]) ``(`` [ args |> captureExpression ] ``)``

        and buildApply returnType op args =
            // Checks if the expression points to a non-generic user-defined callable.
            // Because these have fully-resolved types in the runtime,
            // they don't need to have the return type explicitly in the apply.
            let isNonGenericCallable () =
                match op.Expression with
                | Identifier (_, Value tArgs) when tArgs.Length > 0 -> false
                | Identifier (id, _) ->
                    match id with
                    | GlobalCallable n ->
                        let sameName =
                            match context.current with
                            | None -> false
                            | Some name -> n = name

                        if sameName then // when called recursively, we always need to specify the return type.
                            false
                        else
                            not (hasTypeParameters [ op.ResolvedType ])
                    | _ -> false
                | _ -> false

            let useReturnType =
                match returnType.Resolution with
                | QsTypeKind.UnitType -> false
                | _ -> not (isNonGenericCallable ())

            let apply =
                if useReturnType then
                    (ident (sprintf "Apply<%s>" (roslynTypeName context returnType)))
                else
                    (ident "Apply")

            buildExpression op <.> (apply, [ args |> captureExpression ]) // we need to capture to guarantee that the result accurately reflects any indirect binding of arguments

        and buildConditional c t f =
            let cond = c |> buildExpression
            let whenTrue = t |> captureExpression
            let whenFalse = f |> captureExpression
            ``?`` cond (whenTrue, whenFalse)

        and buildRange lhs rEnd =
            let args =
                lhs.Expression
                |> function
                    | RangeLiteral (start, step) ->
                        [ (buildExpression start); (buildExpression step); (buildExpression rEnd) ]
                    | _ -> [ (buildExpression lhs); (buildExpression rEnd) ]

            ``new`` (``type`` [ "QRange" ]) ``(`` args ``)``

        and buildValueArray at elems =
            match at.Resolution |> QArrayType with
            | Some arrayType -> ``new`` arrayType ``(`` (elems |> Seq.map captureExpression |> Seq.toList) ``)``
            // TODO: diagnostics.
            | _ -> failwith ""

        and buildSizedArray value size =
            let valueName = nextArgName ()
            var valueName (``:=`` (buildExpression value)) |> ``#line hidden`` |> addStatement
            let valueId = { value with Expression = Identifier(LocalVariable valueName, Null); Range = Null }
            let supplier = ``() =>`` [] (captureExpression valueId) :> ExpressionSyntax
            ident "QArray" <.> (ident "Filled", [ supplier; buildExpression size ])

        and buildNewArray b count =
            let arrayType = (ArrayType b |> QArrayType).Value
            arrayType <.> (ident "Create", [ count |> buildExpression ])

        and buildArrayItem a i =
            match i.ResolvedType.Resolution with
            | Range -> invoke ((buildExpression a) <|.|> (ident "Slice")) ``(`` [ (buildExpression i) ] ``)``
            | _ -> item (buildExpression a) [ (buildExpression i) ]

        let buildBlock (block: QsScope) =
            let builder = new SyntaxBuilder(context)
            builder.StartLine <- parent.StartLine
            builder.Statements.OnScope block |> ignore
            builder.BuiltStatements

        let buildSymbolTuple buildTuple buildSymbol symbol =
            let rec buildOne =
                function
                // TODO: Diagnostics
                | InvalidItem -> failwith ("InvalidItem received")
                | VariableName one -> one |> buildSymbol
                | VariableNameTuple many -> many |> Seq.map buildOne |> Seq.toList |> buildTuple
                | DiscardedItem -> "_" |> buildSymbol
            // While _ inside C# tuple destructs will properly discard the assignment,
            // _ can also be used as variable name in C# where a repeated usage will lead to a compilation error.
            // We hence auto-generate a name for discarded Q# bindings.
            match symbol with
            | DiscardedItem -> nextArgName () |> buildSymbol
            | _ -> buildOne symbol

        let buildSymbolNames buildName =
            buildSymbolTuple (String.concat "," >> sprintf "(%s)") buildName

        /// returns true if a value of this type contains any arrays
        /// -> in particular, this does not include the in- and output type of callables
        let rec containsArrays (t: ResolvedType) =
            match t.Resolution with
            | TupleType ts -> ts |> Seq.exists containsArrays
            | ArrayType _ -> true
            | _ -> false // no need to check types within callables

        /// returns true if the given expression initializes a new QArray instance
        let rec isArrayInit ex =
            match ex.Expression with
            | CopyAndUpdate _
            | NewArray _
            | ADD _
            | ValueArray _ -> true
            | CONDITIONAL (_, l, r) -> isArrayInit l && isArrayInit r
            | _ -> false

        override _.OnExpressionStatement(node: TypedExpression) =
            buildExpression node |> statement |> addStatement
            QsExpressionStatement node

        override _.OnReturnStatement(node: TypedExpression) =
            buildExpression node |> Some |> ``return`` |> addStatement
            QsReturnStatement node

        override _.OnVariableDeclaration(node: QsBinding<TypedExpression>) =
            let bindsArrays = node.Rhs.ResolvedType |> containsArrays
            let rhs = node.Rhs |> captureExpression

            let buildBinding buildName =
                let lhs = node.Lhs |> buildSymbolNames buildName

                if bindsArrays then // we need to cast to the correct type here (in particular to IQArray for arrays)
                    let t = roslynTypeName context node.Rhs.ResolvedType
                    var lhs (``:=`` <| cast t rhs) |> addStatement
                else
                    var lhs (``:=`` <| rhs) |> addStatement

            match node.Kind with
            | MutableBinding ->
                match node.Lhs with

                // no need to insert a destructing statement first
                | VariableName varName ->
                    match node.Rhs.ResolvedType.Resolution |> QArrayType with
                    | Some _ when isArrayInit node.Rhs -> // avoid unnecessary copies on construction
                        var varName (``:=`` <| rhs) |> addStatement
                    | Some arrType -> // we need to make sure to bind to a new QArray instance here
                        let qArray = ``new`` arrType ``(`` [ rhs ] ``)``
                        var varName (``:=`` <| qArray) |> addStatement
                    | _ -> buildBinding id

                // we first need to destruct here, and then make sure all QArrays are built
                | VariableNameTuple _ when bindsArrays ->
                    // insert a destructing statement
                    let prefix = nextArgName ()
                    let imName = sprintf "%s%s__" prefix
                    buildBinding imName

                    // build the actual binding, making sure all necessary QArrays instances are created
                    for localVar in parent.DeclarationsInStatement.Variables do
                        let varName = localVar.VariableName

                        match localVar.Type.Resolution |> QArrayType with
                        | Some arrType ->
                            let qArray = ``new`` arrType ``(`` [ ident (imName varName) ] ``)``
                            var varName (``:=`` <| qArray) |> addStatement
                        | _ -> var varName (``:=`` <| ident (imName varName)) |> addStatement
                | _ -> buildBinding id
            | _ -> buildBinding id

            QsVariableDeclaration node

        override _.OnValueUpdate(node: QsValueUpdate) =
            let rec varNames onTuple onItem (ex: TypedExpression) =
                match ex.Expression with
                | MissingExpr -> onItem "_"
                | Identifier (LocalVariable id, Null) -> onItem id
                | ValueTuple vs -> vs |> Seq.map (varNames onTuple onItem) |> onTuple
                // TODO: diagnostics.
                | _ -> failwith "unexpected expression in lhs of value update"

            let lhs, rhs = buildExpression node.Lhs, captureExpression node.Rhs

            match node.Lhs.Expression with
            | MissingExpr -> var (nextArgName ()) (``:=`` <| buildExpression node.Rhs) |> addStatement

            // no need to insert a destructing statement first
            | Identifier (LocalVariable id, Null) ->
                let matchesIdentifier (ex: TypedExpression) =
                    match ex.Expression with
                    | Identifier (LocalVariable rhsId, Null) when rhsId = id -> true
                    | _ -> false

                let isArray =
                    function
                    | ArrayType _ -> true
                    | _ -> false

                match node.Rhs.Expression with
                | CopyAndUpdate (l, a, r) when l |> matchesIdentifier && l.ResolvedType.Resolution |> isArray -> // we do an in-place modification in this case
                    let access, rhs = buildExpression a, captureExpression r
                    (buildExpression l) <.> (ident "Modify", [ access; rhs ]) |> statement |> addStatement
                | _ when node.Rhs |> matchesIdentifier -> () // unnecessary statement
                | _ ->
                    node.Rhs.ResolvedType.Resolution
                    |> QArrayType
                    |> function
                        | Some _ when isArrayInit node.Rhs -> // avoid unnecessary copies here
                            lhs <-- rhs |> statement |> addStatement
                        | Some arrType -> // we need to make sure to bind to a new QArray instance here
                            let qArray = ``new`` arrType ``(`` [ rhs ] ``)``
                            lhs <-- qArray |> statement |> addStatement
                        | _ -> lhs <-- rhs |> statement |> addStatement

            // we first need to destruct here, and then make sure all QArrays are built
            | _ when containsArrays node.Rhs.ResolvedType ->
                // insert a destructing statement
                let prefix = nextArgName ()

                let imName name =
                    if name = "_" then name else sprintf "%s%s__" prefix name

                let tempBinding = varNames (fun ids -> String.Join(",", ids) |> sprintf "(%s)") imName node.Lhs
                var tempBinding (``:=`` <| rhs) |> addStatement

                // build the actual binding, making sure all necessary QArrays instances are created
                let ids = varNames (Seq.collect id) (fun id -> seq { if id <> "_" then yield id }) node.Lhs

                for id in ids do
                    let decl = parent.DeclarationsInScope.Variables |> Seq.tryFind (fun d -> d.VariableName = id)

                    match decl |> Option.map (fun d -> d.Type.Resolution |> QArrayType) |> Option.flatten with
                    | Some arrType -> // we need to make sure to create a new QArray instance here
                        let qArray = ``new`` arrType ``(`` [ imName id |> ident ] ``)``
                        (ident id) <-- qArray |> statement |> addStatement
                    | _ -> (ident id) <-- (imName id |> ident) |> statement |> addStatement

            | _ -> lhs <-- rhs |> statement |> addStatement

            QsValueUpdate node

        override _.OnConditionalStatement(node: QsConditionalStatement) =
            let all = node.ConditionalBlocks
            let (cond, thenBlock) = all.[0]
            let cond = cond |> buildExpression
            let thenBlock = thenBlock.Body |> buildBlock

            let others =
                [
                    for i in 1 .. all.Length - 1 ->
                        let (cond, block) = all.[i]
                        cond |> buildExpression, block.Body |> buildBlock
                ]

            let elseBlock =
                match node.Default with
                | Null -> None
                | Value block -> ``else`` (buildBlock block.Body) |> Some

            ``if`` ``(`` cond ``)`` thenBlock (``elif`` others elseBlock) |> addStatement
            QsConditionalStatement node

        override _.OnForStatement(node: QsForStatement) =
            let sym = node.LoopItem |> fst |> buildSymbolNames id
            let range = node.IterationValues |> captureExpression
            let body = node.Body |> buildBlock
            foreach ``(`` sym ``in`` range ``)`` body |> addStatement
            QsForStatement node

        override _.OnWhileStatement(node: QsWhileStatement) =
            let cond = node.Condition |> buildExpression
            let body = node.Body |> buildBlock
            ``while`` ``(`` cond ``)`` body |> addStatement
            QsWhileStatement node

        override _.OnRepeatStatement rs =
            let buildTest test fixup =
                let condition = buildExpression test
                let thens = [ break ]
                let elses = buildBlock fixup
                ``if`` ``(`` condition ``)`` thens (Some(``else`` elses))

            ``while``
                ``(``
                ``true``
                ``)``
                ((buildBlock rs.RepeatBlock.Body) @ [ buildTest rs.SuccessCondition rs.FixupBlock.Body ])
            |> addStatement

            QsRepeatStatement rs

        override _.OnQubitScope(using: QsQubitScope) =
            let (alloc, release) =
                match using.Kind with
                | Allocate -> "Allocate__", "Release__"
                | Borrow -> "Borrow__", "Return__"

            let rec removeDiscarded sym =
                match sym with
                | VariableName _ -> sym
                | DiscardedItem -> nextArgName () |> VariableName
                | VariableNameTuple many ->
                    many |> Seq.map removeDiscarded |> ImmutableArray.CreateRange |> VariableNameTuple
                | InvalidItem -> failwith ("InvalidItem received")

            let rec buildInitializeExpression (exp: ResolvedInitializer) =
                match exp.Resolution with
                | SingleQubitAllocation -> ((ident alloc) <.> (ident "Apply", []))
                | QubitRegisterAllocation e -> ((ident alloc) <.> (ident "Apply", [ (buildExpression e) ]))
                | QubitTupleAllocation many -> many |> Seq.map buildInitializeExpression |> List.ofSeq |> tuple
                // todo: diagnostics
                | InvalidInitializer -> failwith ("InvalidInitializer received")

            let rec buildReleaseExpression (symbol, expr: ResolvedInitializer) : StatementSyntax list =
                let currentLine = parent.LineNumber
                parent.LineNumber <- Some 0

                let buildOne sym =
                    (ident release) <.> (ident "Apply", [ (ident (sym)) ]) |> (statement >> withLineNumber)

                let rec buildDeconstruct sym (rhs: ResolvedInitializer) =
                    match rhs.Resolution with
                    | SingleQubitAllocation -> [ buildOne sym ]
                    | QubitRegisterAllocation _ -> [ buildOne sym ]
                    | QubitTupleAllocation aa ->
                        aa
                        |> Seq.mapi (fun i e -> buildDeconstruct (sprintf "%s.Item%d" sym (i + 1)) e)
                        |> Seq.toList
                        |> List.concat
                    | InvalidInitializer -> failwith ("InvalidInitializer received")

                let releases =
                    match (symbol, expr.Resolution) with
                    | VariableName one, SingleQubitAllocation -> [ buildOne one ]
                    | VariableName one, QubitRegisterAllocation _ -> [ buildOne one ]
                    | VariableName one, QubitTupleAllocation _ -> (buildDeconstruct one expr)
                    | VariableNameTuple ss, QubitTupleAllocation aa ->
                        Seq.zip ss aa |> Seq.map buildReleaseExpression |> Seq.toList |> List.concat
                    | _ -> failwith ("InvalidItem received")

                parent.LineNumber <- currentLine
                releases

            let symbols = removeDiscarded using.Binding.Lhs
            let deallocationFlagName = nextArgName ()
            let deallocationFlagIdentifier = ident deallocationFlagName

            // allocations and deallocations
            let lhs = symbols |> buildSymbolNames id
            let rhs = using.Binding.Rhs |> buildInitializeExpression
            let allocation = var lhs (``:=`` <| rhs) |> withLineNumber
            let deallocation = buildReleaseExpression (symbols, using.Binding.Rhs)

            // To force that exceptions thrown during the execution of the allocation scope take precedence over the ones thrown upon release
            // we catch all exceptions in a variable and throw after releaseing if necessary.

            // Indicates if deallocation is needed. It is not needed when exception is thrown.
            let deallocationFlagDeclaration =
                ``typed var`` "bool" deallocationFlagName (``:=`` ``true`` |> Some) |> ``#line hidden``
                :> StatementSyntax

            let catch =
                let setFlagToFalse = deallocationFlagIdentifier <-- ``false`` |> statement
                catch None [ setFlagToFalse; throw None ] // use standard mechanism to rethrow the exception by using "throw;"

            let finallyBlock = [ ``if`` ``(`` deallocationFlagIdentifier ``)`` deallocation None ]

            let body =
                ``try``
                    (buildBlock using.Body)
                    [ catch |> ``#line hidden`` ]
                    (``finally`` finallyBlock |> ``#line hidden`` |> Some)

            let statements = [ allocation; deallocationFlagDeclaration; body ]

            // Put all statements into their own brackets so variable names have their own context.
            // Make sure the brackets get #line hidden:
            let currentLine = parent.LineNumber
            parent.LineNumber <- parent.LineNumber |> Option.map (fun _ -> 0)
            ``{{`` statements ``}}`` |> addStatement
            parent.LineNumber <- currentLine
            QsQubitScope using

        override _.OnFailStatement fs =
            let failException = ``new`` (``type`` [ "ExecutionFailException" ]) ``(`` [ (buildExpression fs) ] ``)``
            addStatement (throw <| Some failException)
            QsFailStatement fs

    and NamespaceBuilder(parent: SyntaxBuilder) =
        inherit NamespaceTransformation(parent, TransformationOptions.NoRebuild)

        override this.OnSpecializationDeclaration(sp: QsSpecialization) =
            count <- 0

            match sp.Location with
            | Value location -> parent.StartLine <- Some location.Offset.Line
            | Null -> parent.StartLine <- None // TODO: we may need to have the means to know which original declaration the code came from

            base.OnSpecializationDeclaration sp

    let operationDependencies (od: QsCallable) =
        let seeker = new OperationsSeeker()
        seeker.Namespaces.OnCallableDeclaration od |> ignore
        seeker.SharedState |> Seq.toList

    let getTypeOfOp context (n: QsQualifiedName) =
        let name =
            let sameNamespace =
                match context.current with
                | None -> false
                | Some o -> o.Namespace = n.Namespace

            let opName =
                if sameNamespace then
                    userDefinedName None n.Name
                else
                    "global::" + n.Namespace + "." + userDefinedName None n.Name

            if isGeneric context n then
                let signature = context.allCallables.[n].Signature
                let count = signature.TypeParameters.Length
                sprintf "%s<%s>" opName (String.replicate (count - 1) ",")
            else
                opName

        invoke (ident "typeof") ``(`` [ ident name ] ``)``

    /// Returns the list of statements of the contructor's body for the given operation.
    let buildInit context (operations: QsQualifiedName list) =
        let parameters = []

        let body =
            let buildOne n =
                let name = getOpName context n
                let lhs = ident "this" <|.|> ident name

                let rhs =
                    if (isCurrentOp context n) && not (isGeneric context n) then
                        "this" |> ident :> ExpressionSyntax
                    else
                        let signature = roslynCallableTypeName context n

                        let factoryGet =
                            (ident "this" <|.|> ident "__Factory__" <|.|> (generic "Get" ``<<`` [ signature ] ``>>``))

                        (invoke factoryGet ``(`` [ (getTypeOfOp context n) ] ``)``)

                statement (lhs <-- rhs)

            operations |> List.map buildOne

        method "void" "__Init__" ``<<`` [] ``>>`` ``(`` parameters ``)`` [ ``public``; ``override`` ] ``{`` body ``}``
        :> MemberDeclarationSyntax

    /// Returns the constructor for the given operation.
    let buildConstructor context name : MemberDeclarationSyntax =
        constructor name ``(`` [ ("m", ``type`` "IOperationFactory") ] ``)`` ``:`` [ "m" ] [ ``public`` ] ``{`` [] ``}``
        :> MemberDeclarationSyntax

    /// Returns the constructor for the given intrinsic operation.
    let buildIntrinsicConstructor context name : MemberDeclarationSyntax =
        constructor
            name
            ``(``
            [ ("m", ``type`` "IOperationFactory") ]
            ``)``
            ``:``
            [ "m" ]
            [ ``public`` ]
            ``{``
            [ (ident "this") <|.|> (ident "Impl") <-- (ident "m") |> statement ]
            ``}``
        :> MemberDeclarationSyntax

    /// For each Operation used in the given OperationDeclartion, returns
    /// a Property that returns an instance of the operation by calling the
    /// IOperationFactory
    let buildOpsProperties context (operations: QsQualifiedName list) : MemberDeclarationSyntax list =
        let getCallableAccess qualifiedName =
            match context.allCallables.TryGetValue qualifiedName with
            | true, callable -> Some callable.Access
            | false, _ -> None

        let getPropertyModifiers qualifiedName =
            // Use the right accessibility for the property depending on the accessibility of the callable.
            // Note: In C#, "private protected" is the intersection of protected and internal.
            match getCallableAccess qualifiedName |> Option.defaultValue Public with
            | Public -> [ protected ]
            | Internal -> [ ``private``; protected ]

        let buildOne qualifiedName =
            /// eg:
            /// protected opType opName { get; }
            let signature = roslynCallableTypeName context qualifiedName
            let name = getOpName context qualifiedName
            let modifiers = getPropertyModifiers qualifiedName
            prop signature name modifiers :> MemberDeclarationSyntax

        operations |> List.map buildOne

    /// Returns a static property of type OperationInfo using the operation's input and output types.
    let buildOperationInfoProperty (globalContext: CodegenContext) operationInput operationOutput operationName =
        let propertyType =
            match globalContext.ProcessorArchitecture with
            | target when target = AssemblyConstants.HoneywellProcessor ->
                sprintf "HoneywellEntryPointInfo<%s, %s>" operationInput operationOutput
            | target when target = AssemblyConstants.IonQProcessor ->
                sprintf "IonQEntryPointInfo<%s, %s>" operationInput operationOutput
            | target when target = AssemblyConstants.QCIProcessor ->
                sprintf "QCIEntryPointInfo<%s, %s>" operationInput operationOutput
            | target when target = AssemblyConstants.QuantinuumProcessor ->
                sprintf "QuantinuumEntryPointInfo<%s, %s>" operationInput operationOutput
            | _ -> sprintf "EntryPointInfo<%s, %s>" operationInput operationOutput

        let operationType = simpleBase operationName
        let newInstanceArgs = [ invoke (ident "typeof") ``(`` [ operationType.Type ] ``)`` ]
        let newInstance = ``new`` (``type`` [ propertyType ]) ``(`` newInstanceArgs ``)``

        ``property-arrow_get`` propertyType "Info" [ ``public``; ``static`` ] get (``=>`` newInstance)
        :> MemberDeclarationSyntax

    let buildSpecializationBody (context: CodegenContext) (op: QsCallable) (sp: QsSpecialization) =
        let getInputVarWithInit args =
            let inData = ident "__in__"

            let name =
                function
                | ValidName n -> n
                | InvalidName -> ""

            let rec buildVariableName =
                function
                | QsTupleItem one -> one.VariableName |> name
                | QsTuple many -> "(" + (many |> Seq.map buildVariableName |> String.concat ",") + ")"

            match args with
            | QsTupleItem one -> (one.VariableName |> name, [])
            | QsTuple many ->
                if many.Length = 0 then ("__in__", [])
                elif many.Length = 1 then ("__in__", [ var (buildVariableName many.[0]) (``:=`` <| inData) ])
                else ("__in__", [ var (buildVariableName args) (``:=`` <| inData) ])

        match sp.Implementation with
        | Provided (args, _) ->
            let argName, argsInit = getInputVarWithInit args

            let statements =
                let builder = new SyntaxBuilder(context)
                builder.Namespaces.OnSpecializationDeclaration sp |> ignore
                builder.BuiltStatements

            let ret =
                match sp.Signature.ReturnType.Resolution with
                | QsTypeKind.UnitType ->
                    [
                        ``return`` (Some((ident "QVoid") <|.|> (ident "Instance"))) |> ``#line hidden``
                    ]
                | _ -> []

            Some(``() => {}`` [ argName ] (argsInit @ statements @ ret) :> ExpressionSyntax)
        | Generated SelfInverse ->
            let adjointedBodyName =
                match sp.Kind with
                | QsAdjoint -> "__Body__"
                | QsControlledAdjoint -> "__ControlledBody__"
                //TODO: diagnostics.
                | _ -> "__Body__"

            Some(ident adjointedBodyName :> ExpressionSyntax)
        | Intrinsic when isConcreteIntrinsic context ->
            // Add in the control qubits parameter when dealing with a controlled spec
            let args =
                match sp.Kind with
                | QsControlled
                | QsControlledAdjoint ->
                    let ctlVar =
                        let name = ValidName("__controlQubits__")
                        let varType = Qubit |> ResolvedType.New |> ArrayType |> ResolvedType.New
                        let info = InferredExpressionInformation.New(false, false)
                        let pos = QsNullable<Position>.Null
                        let range = Range.Zero

                        {
                            VariableName = name
                            Type = varType
                            InferredInformation = info
                            Position = pos
                            Range = range
                        }

                    match op.ArgumentTuple with
                    | QsTuple many when many.Length = 1 -> QsTuple(ImmutableArray.Create(QsTupleItem(ctlVar), many.[0]))
                    | _ -> QsTuple(ImmutableArray.Create(QsTupleItem(ctlVar), op.ArgumentTuple))
                | _ -> op.ArgumentTuple

            let argName, argsInit = getInputVarWithInit args

            let specCall =
                match sp.Kind with
                | QsBody -> ""
                | QsAdjoint -> "Adjoint"
                | QsControlled -> "Controlled"
                | QsControlledAdjoint -> "ControlledAdjoint"
                + "Body"

            let name =
                function
                | ValidName n -> ident n
                | InvalidName -> ident ""

            let rec argsToVars =
                function
                | QsTupleItem one -> [ one.VariableName |> name ]
                | QsTuple many -> many |> Seq.map argsToVars |> List.concat

            let callExp =
                ``((`` (cast ("IIntrinsic" + (userDefinedName None op.FullName.Name)) (ident "Impl")) ``))``
                <.> (ident specCall, argsToVars args)

            let statements =
                match sp.Signature.ReturnType.Resolution with
                | QsTypeKind.UnitType ->
                    [
                        callExp |> statement
                        ``return`` (Some((ident "QVoid") <|.|> (ident "Instance"))) |> ``#line hidden``
                    ]
                | _ -> [ ``return`` (Some callExp) ]

            Some(``() => {}`` [ argName ] (argsInit @ statements) :> ExpressionSyntax)
        | _ -> None

    let buildSpecialization context (op: QsCallable) (sp: QsSpecialization) : (PropertyDeclarationSyntax * _) option =
        let inType = roslynTypeName context sp.Signature.ArgumentType
        let outType = roslynTypeName context sp.Signature.ReturnType
        let propertyType = "Func<" + inType + ", " + outType + ">"

        let bodyName =
            match sp.Kind with
            | QsBody -> "Body"
            | QsAdjoint -> "Adjoint"
            | QsControlled -> "Controlled"
            | QsControlledAdjoint -> "ControlledAdjoint"

        let body = buildSpecializationBody context op sp

        let attributes =
            match sp.Location with
            | Null -> []
            | Value location ->
                [
                    // since the line numbers throughout the generated code are 1-based, let's also choose them 1-based here
                    let startLine = location.Offset.Line + 1

                    let endLine =
                        match context.declarationPositions.TryGetValue sp.Source.AssemblyOrCodeFile with
                        | true, startPositions ->
                            let index = startPositions.IndexOf location.Offset
                            if index + 1 >= startPositions.Count then -1 else startPositions.[index + 1].Line + 1
                        //TODO: diagnostics.
                        | false, _ -> startLine

                    attribute
                        None
                        (ident "SourceLocation")
                        [
                            literal sp.Source.AssemblyOrCodeFile
                            ident "OperationFunctor" <|.|> ident bodyName
                            literal startLine
                            literal endLine
                        ]
                ]

        match body with
        | Some body ->
            let bodyName = if bodyName = "Body" then "__Body__" else "__" + bodyName + "Body__"
            let impl = ``property-arrow_get`` propertyType bodyName [ ``public``; ``override`` ] get (``=>`` body)
            Some(impl, attributes)
        | None -> None

    /// Returns a flat list (name, type) with all the named parameters of a DeconstructedTuple
    let flatArgumentsList context args =
        let rec flatOne found =
            function
            | QsTupleItem one ->
                match one.VariableName with
                | ValidName n -> found @ [ n, one.Type |> roslynTypeName context ]
                | InvalidName -> found
            | QsTuple many -> many |> Seq.fold flatOne found

        args |> flatOne []

    /// Maps the name and type of each named item in the argument tuple.
    let internal mapArgumentTuple mapping context arguments (argumentType: ResolvedType) =
        let rec buildTuple =
            function
            | QsTupleItem one ->
                match one.VariableName with
                | ValidName n -> mapping (n, roslynTypeName context one.Type) :> ExpressionSyntax
                | InvalidName -> mapping ("", roslynTypeName context one.Type) :> ExpressionSyntax
            | QsTuple many -> many |> Seq.map buildTuple |> List.ofSeq |> tuple

        if isTuple argumentType.Resolution then
            buildTuple arguments
        else
            match flatArgumentsList context arguments with
            | [] -> ident "QVoid" <|.|> ident "Instance"
            | [ name, typeName ] -> mapping (name, typeName) :> ExpressionSyntax
            | flatArgs -> flatArgs |> List.map mapping |> tuple

    let buildRun context className arguments argumentType returnType : MemberDeclarationSyntax =
        let inType = roslynTypeName context argumentType
        let outType = roslynTypeName context returnType

        let task = sprintf "System.Threading.Tasks.Task<%s>" outType
        let flatArgs = arguments |> flatArgumentsList context
        let opFactoryTypes = [ className; inType; outType ]

        let uniqueArgName = "__m__"
        let runArgs = mapArgumentTuple (fst >> ident) context arguments argumentType

        let body =
            [
                ``return`` (Some((ident uniqueArgName) <.> (generic "Run" ``<<`` opFactoryTypes ``>>``, [ runArgs ])))
            ]

        let args =
            (param uniqueArgName ``of`` (``type`` "IOperationFactory"))
            :: (flatArgs |> List.map (fun (name, roslynType) -> (param name ``of`` (``type`` roslynType))))

        method task "Run" ``<<`` [] ``>>`` ``(`` args ``)`` [ ``public``; ``static`` ] ``{`` body ``}``
        :> MemberDeclarationSyntax

    let findUdtBase context n =
        let udt = findUdt context n
        udt.Type

    let rec canHaveQubits context (qsharpType: ResolvedType) =
        match qsharpType.Resolution with
        | QsTypeKind.Qubit -> true
        | QsTypeKind.ArrayType at -> canHaveQubits context at
        | QsTypeKind.TupleType tt -> tt |> Seq.fold (fun state m -> state || canHaveQubits context m) false
        | QsTypeKind.UserDefinedType n ->
            QsQualifiedName.New(n.Namespace, n.Name) |> findUdtBase context |> canHaveQubits context
        | QsTypeKind.Operation _
        | QsTypeKind.Function _ -> true
        | QsTypeKind.TypeParameter _ -> true
        | _ -> false

    let findQubitFields context (qsharpType: ResolvedType) =
        let item_n n = ident (sprintf "Item%d" (n + 1))

        let rec buildSimpleTerm current nullable (t: ResolvedType) =
            match t.Resolution with
            | QsTypeKind.Qubit -> [ t, current ]
            | QsTypeKind.Operation _
            | QsTypeKind.Function _
            | QsTypeKind.TypeParameter _
            | QsTypeKind.ArrayType _ -> if canHaveQubits context t then [ t, current ] else []
            | QsTypeKind.UserDefinedType n ->
                QsQualifiedName.New(n.Namespace, n.Name)
                |> findUdtBase context
                |> buildSimpleTerm (current <|?.|> (ident "Data")) false
            | QsTypeKind.TupleType tt ->
                let buildOne j t =
                    if nullable then
                        buildSimpleTerm (current <|?.|> (item_n j)) false t
                    else
                        buildSimpleTerm (current <|.|> (item_n j)) false t

                tt |> Seq.mapi buildOne |> List.concat
            | _ -> []

        match qsharpType.Resolution with
        | QsTypeKind.TupleType many ->
            many |> Seq.mapi (fun j -> buildSimpleTerm (ident "Data" <|.|> item_n j) false) |> List.concat
        | one -> qsharpType |> buildSimpleTerm (ident "Data") true

    let areAllQubitArgs (argsTypes: ResolvedType list) =
        let isOne =
            function
            | QsTypeKind.Qubit -> true
            | _ -> false

        argsTypes |> List.fold (fun st t -> st && isOne t.Resolution) true

    let buildQubitsField context (qsharpType: ResolvedType) =
        let fields = qsharpType |> findQubitFields context
        let (fieldTypes, fieldPaths) = fields |> List.unzip

        if areAllQubitArgs fieldTypes then
            let buildOne path = ``yield return`` path

            match fieldPaths with
            | [] ->
                ``property-arrow_get``
                    "System.Collections.Generic.IEnumerable<Qubit>"
                    "IApplyData.Qubits"
                    []
                    get
                    (``=>`` ``null``)
            | _ ->
                ``property-get``
                    "System.Collections.Generic.IEnumerable<Qubit>"
                    "IApplyData.Qubits"
                    []
                    get
                    (fieldPaths |> List.map buildOne)
        else
            // this implementation is a workaround for the .NET Core issue discussed here:
            // https://github.com/microsoft/qsharp-runtime/issues/116
            let mutable count = 0

            let nextName () =
                count <- count + 1
                sprintf "__temp%d__" count

            let mutable items = []

            for (t, token) in fields do
                match t.Resolution with
                | QsTypeKind.Function _
                | QsTypeKind.Operation _
                | QsTypeKind.ArrayType _
                | QsTypeKind.UserDefinedType _
                | QsTypeKind.Qubit ->
                    let qs = ``((`` (cast "IApplyData" token) ``))`` <|?.|> ident "Qubits"
                    items <- (null, qs) :: items
                | _ ->
                    let id = nextName ()
                    let decl = var id (``:=`` token)
                    let qs = (ident id) <?.> (ident "GetQubits", [])
                    items <- (decl, qs) :: items

            items <- items |> List.rev

            let statements =
                match fields with
                | [] -> [ ``return`` (Some ``null``) ]
                | [ _ ] ->
                    [
                        let (decl, qs) = items.Head
                        if decl <> null then yield decl
                        yield ``return`` (snd items.Head |> Some)
                    ]
                | _ ->
                    [
                        for (decl, _) in items do
                            if decl <> null then yield decl
                        let qs = (ident "Qubit" <.> (ident "Concat", items |> List.map snd))
                        yield ``return`` (Some qs)
                    ]

            ``property-get`` "System.Collections.Generic.IEnumerable<Qubit>" "IApplyData.Qubits" [] get statements
        :> MemberDeclarationSyntax
        |> List.singleton

    let buildName name =
        ``property-arrow_get`` "String" "ICallable.Name" [] get (``=>`` (literal name)) :> MemberDeclarationSyntax

    let buildImpl name =
        propg "IOperationFactory" "Impl" [ ``private``; protected ] :> MemberDeclarationSyntax

    let buildFullName (name: QsQualifiedName) =
        let fqn =
            let ns = name.Namespace
            let n = name.Name
            if ns = "" then n else ns + "." + n

        ``property-arrow_get`` "String" "ICallable.FullName" [] get (``=>`` (literal fqn)) :> MemberDeclarationSyntax

    let outputHelperInterface = "Xunit.Abstractions.ITestOutputHelper"
    let testOutputHandle = "Output"

    let buildOutput () =
        [
            propg outputHelperInterface testOutputHandle [ ``internal`` ] :> MemberDeclarationSyntax
        ]

    let buildUnitTest (targetName: QsQualifiedName) opName opStart opSourceFile =
        let sim = ident "sim"
        let baseSim = ident "baseSim"
        let disposeSim = ident "disposeSim"
        let ``this.Output`` = ident "this" <|.|> ident "Output"
        let ``sim.OnLog`` = baseSim <|.|> ident "OnLog"
        let Execute = generic "Execute" ``<<`` [ opName; "QVoid"; "QVoid" ] ``>>``

        let simCond =
            sim |> ``is assign`` "Microsoft.Quantum.Simulation.Common.SimulatorBase" baseSim
            .&&. ``this.Output``
            .!=. ``null``

        let getSimulator = var "sim" (``:=`` <| ``new`` (ident <| targetName.ToString()) ``(`` [] ``)``)

        let assignLogEvent =
            ``if`` ``(`` simCond ``)`` [ ``sim.OnLog`` <+=> (``this.Output`` <|.|> ident "WriteLine") ] None

        let ``sim.Execute`` = sim <.> (Execute, [ ident "QVoid" <|.|> ident "Instance" ]) |> statement

        let disposeOfRun =
            ``if``
                ``(``
                (sim |> ``is assign`` "IDisposable" disposeSim)
                ``)``
                [ disposeSim <.> ((ident "Dispose"), []) |> statement ]
                None

        let errMsg = literal "Q# Test failed. For details see the Standard output below."

        let tryRunCatch =
            ``try``
                [ ``sim.Execute`` ]
                [
                    catch
                        None
                        [
                            (ident "Xunit.Assert") <.> ((ident "True"), [ ``false`` :> ExpressionSyntax; errMsg ])
                            |> (statement >> ``#line`` (opStart + 1) opSourceFile)
                        ]
                ]
                (Some(``finally`` [ disposeOfRun ]))

        attributes
            [
                attribute None (ident "Xunit.Fact") []
                attribute None (ident "Xunit.Trait") [ literal "Target"; literal targetName.Name ]
                attribute None (ident "Xunit.Trait") [ literal "Name"; literal opName ]
            ]
            (method
                "void"
                opName
                ``<<``
                []
                ``>>``
                ``(``
                []
                ``)``
                [ ``public`` ]
                ``{``
                [ getSimulator; assignLogEvent; tryRunCatch ]
                ``}``
             |> ``with trivia`` (``#lineNr`` (opStart + 1) opSourceFile)) // we need 1-based line numbers here, and opStart is zero-based

    let buildDataWrapper context name qsharpType =
        let buildDataClass =
            let buildValueTupleConstructor =
                let args = [ ("data", ``type`` (roslynTypeName context qsharpType)) ]

                constructor name ``(`` args ``)`` ``:`` [ "data" ] [ ``public`` ] ``{`` [] ``}``
                :> MemberDeclarationSyntax

            let baseClass = simpleBase (tupleBaseClassName context qsharpType)
            let modifiers = [ ``public`` ]
            let constructors = [ buildValueTupleConstructor ]
            let qubitsField = buildQubitsField context qsharpType

            ``class``
                name
                ``<<``
                []
                ``>>``
                ``:``
                (Some baseClass)
                ``,``
                [ simpleBase "IApplyData" ]
                modifiers
                ``{``
                (constructors @ qubitsField)
                ``}``
            :> MemberDeclarationSyntax

        let buildMethod t body =
            let baseType = (roslynTypeName context t)
            let args = [ (param "data" ``of`` (``type`` (roslynTypeName context t))) ]

            arrow_method
                "IApplyData"
                (sprintf "__Data%s__" name)
                ``<<``
                []
                ``>>``
                ``(``
                args
                ``)``
                [ ``public``; ``override`` ]
                (Some(``=>`` body))
            :> MemberDeclarationSyntax

        match qsharpType.Resolution with
        | QsTypeKind.UnitType
        | QsTypeKind.Qubit
        | QsTypeKind.UserDefinedType _
        | QsTypeKind.ArrayType _ -> (ident "data") |> buildMethod qsharpType, None
        | QsTypeKind.TupleType vt ->
            (``new`` (``type`` name) ``(`` [ ident "data" ] ``)``) |> buildMethod qsharpType, (Some buildDataClass)
        | _ ->
            (``new`` (generic "QTuple" ``<<`` [ roslynTypeName context qsharpType ] ``>>``) ``(`` [ ident "data" ] ``)``)
            |> buildMethod qsharpType,
            None

    let typeParametersNames signature =
        // TODO Diagnostics
        let name =
            function
            | ValidName n -> sprintf "__%s__" n
            | InvalidName -> "__"

        signature.TypeParameters |> Seq.map name |> Seq.sort |> Seq.toList

    let findClassName (op: QsCallable) =
        let name = userDefinedName None op.FullName.Name
        let typeParameters = typeParametersNames op.Signature

        let nonGeneric =
            if typeParameters.IsEmpty then name else sprintf "%s<%s>" name (String.Join(",", typeParameters))

        (name, nonGeneric)

    let isIntrinsic op =
        let isBody (sp: QsSpecialization) =
            match sp.Kind with
            | QsBody when sp.Implementation <> Intrinsic -> true
            | _ -> false

        not (op.Specializations |> Seq.exists isBody)

    let isFunction (op: QsCallable) =
        match op.Kind with
        | Function -> true
        | _ -> false

    let buildTestClass (testTargets: QsQualifiedName list) (targetName: QsQualifiedName) opName (op: QsCallable) =
        let className =
            let requiresQualification = (testTargets |> List.filter (fun t -> t.Name = targetName.Name)).Length > 1

            if requiresQualification then
                sprintf "%s_%s" (targetName.Namespace.Replace('.', '_')) targetName.Name
            else
                targetName.Name

        let constructors =
            [
                constructor
                    className
                    ``(``
                    [ (testOutputHandle, ``type`` outputHelperInterface) ]
                    ``)``
                    ``:``
                    []
                    [ ``public`` ]
                    ``{``
                    [
                        ident "this" <|.|> ident testOutputHandle <-- ident testOutputHandle |> statement
                    ]
                    ``}``
                :> MemberDeclarationSyntax
            ]

        let properties = buildOutput ()

        let methods =
            match op.Location with
            | Value location ->
                [
                    buildUnitTest targetName opName location.Offset.Line op.Source.AssemblyOrCodeFile
                ]
            // TODO: diagnostics
            | Null -> failwith "missing location for unit test"


        ``class``
            className
            ``<<``
            []
            ``>>``
            ``:``
            None
            ``,``
            []
            [ ``public`` ]
            ``{``
            (constructors @ properties @ methods)
            ``}``

    let private classAccess =
        function
        | Public -> ``public``
        | Internal -> ``internal``

    // Builds the .NET class for the given operation.
    let buildOperationClass (globalContext: CodegenContext) (op: QsCallable) =
        let context = globalContext.setCallable op
        let (name, nonGenericName) = findClassName op
        let opNames = operationDependencies op
        let inType = op.Signature.ArgumentType |> roslynTypeName context
        let outType = op.Signature.ReturnType |> roslynTypeName context
        let opIsIntrinsic = isIntrinsic op
        let isConcreteIntrinsic = opIsIntrinsic && isConcreteIntrinsic context

        let constructors =
            [
                ((if isConcreteIntrinsic then buildIntrinsicConstructor else buildConstructor) context name)
            ]

        let properties =
            let opProperties = buildOpsProperties context opNames

            [
                yield buildName name
                yield buildFullName context.current.Value
                if globalContext.entryPoints |> Seq.contains op.FullName then
                    yield buildOperationInfoProperty globalContext inType outType nonGenericName
                if isConcreteIntrinsic then yield buildImpl name
                yield! opProperties
            ]

        let baseOp =
            if isFunction op then
                "Function"
            else
                let (adj, ctrl) = op.Signature.Information.Characteristics.SupportedFunctors |> hasAdjointControlled

                match (adj, ctrl) with
                | (false, false) -> "Operation"
                | (true, false) -> "Adjointable"
                | (false, true) -> "Controllable"
                | (true, true) -> "Unitary"

        let typeArgsInterface =
            if (baseOp = "Operation" || baseOp = "Function") then [ inType; outType ] else [ inType ]

        let typeParameters = typeParametersNames op.Signature
        let baseClass = genericBase baseOp ``<<`` typeArgsInterface ``>>``

        let bodies, attr =
            op.Specializations
            |> Seq.map (buildSpecialization context op)
            |> Seq.choose id
            |> Seq.toList
            |> List.map (fun (x, y) -> (x :> MemberDeclarationSyntax, y))
            |> List.unzip

        let inData = (buildDataWrapper context "In" op.Signature.ArgumentType)
        let outData = (buildDataWrapper context "Out" op.Signature.ReturnType)

        let defaultTargetNs = "Microsoft.Quantum.Simulation.Simulators"

        let testTargets =
            op.Attributes
            |> SymbolResolution.TryFindTestTargets
            |> Seq.filter (String.IsNullOrWhiteSpace >> not)
            |> Seq.map (function
                | x when x.Contains(".") ->
                    let indexOfDot = x.LastIndexOf('.')
                    { Namespace = x.Substring(0, indexOfDot); Name = x.Substring(indexOfDot + 1) }
                | str -> { Namespace = defaultTargetNs; Name = str })
            |> Seq.sort
            |> Seq.toList

        let unitTests =
            [
                for targetName in testTargets do
                    buildTestClass testTargets targetName name op :> MemberDeclarationSyntax
            ]

        let innerClasses = ([ inData |> snd; outData |> snd ] |> List.choose id) @ unitTests

        let methods =
            [
                opNames |> buildInit context
                inData |> fst
                outData |> fst
                buildRun context nonGenericName op.ArgumentTuple op.Signature.ArgumentType op.Signature.ReturnType
            ]

        let modifiers =
            let access = classAccess op.Access

            if opIsIntrinsic && not isConcreteIntrinsic then
                [ access; ``abstract``; partial ]
            else
                [ access; partial ]

        attributes
            (attr |> List.concat)
            (``class``
                name
                ``<<``
                typeParameters
                ``>>``
                ``:``
                (Some baseClass)
                ``,``
                [ simpleBase "ICallable" ]
                modifiers
                ``{``
                (constructors @ innerClasses @ properties @ bodies @ methods)
                ``}``)

    let isUDTDeclaration =
        function
        | QsCustomType udt -> Some udt
        | _ -> None

    let isCallableDeclaration =
        function
        | QsCallable c -> Some c
        | _ -> None

    let buildUdtClass (globalContext: CodegenContext) (udt: QsCustomType) =
        let context = globalContext.setUdt udt
        let name = userDefinedName None udt.FullName.Name
        let qsharpType = udt.Type

        let buildEmptyConstructor =
            let defaultValue =
                roslynTypeName context qsharpType
                |> sprintf "global::Microsoft.Quantum.Simulation.Core.Default.OfType<%s>()"

            constructor name ``(`` [] ``)`` ``:`` [ defaultValue ] [ ``public`` ] ``{`` [] ``}``
            :> MemberDeclarationSyntax

        let buildBaseTupleConstructor =
            let baseTupleType = (roslynTypeName context qsharpType)
            let args = [ ("data", ``type`` baseTupleType) ]
            constructor name ``(`` args ``)`` ``:`` [ "data" ] [ ``public`` ] ``{`` [] ``}`` :> MemberDeclarationSyntax

        let buildNamedItemFields =
            let produceProperty (decl: LocalVariableDeclaration<_>) valueExpr =
                ``property-arrow_get``
                    (roslynTypeName context decl.Type)
                    (userDefinedName context.current decl.VariableName)
                    [ ``public`` ]
                    get
                    (``=>`` valueExpr)
                :> MemberDeclarationSyntax

            let rec buildProps current =
                function
                | QsTuple items ->
                    items
                    |> Seq.mapi (fun i x -> buildProps (current <|.|> ident ("Item" + (i + 1).ToString())) x)
                    |> Seq.collect id
                | QsTupleItem (Anonymous _) -> Seq.empty
                | QsTupleItem (Named decl) -> seq { yield produceProperty decl current }
            // UDT types are packaged differently if there is one constituent type, or many.
            // This function handles that difference in packaging.
            let rec readType typeItem =
                match typeItem with
                | QsTuple items when items.IsEmpty -> Seq.empty
                | QsTuple items when items.Length = 1 -> items |> Seq.head |> readType
                | QsTuple _ -> buildProps (ident "Data") typeItem
                | QsTupleItem (Anonymous _) -> Seq.empty
                | QsTupleItem (Named decl) -> seq { yield produceProperty decl (ident "Data") }

            readType udt.TypeItems |> Seq.toList

        let buildItemFields =
            let buildOne i t =
                ``property-arrow_get``
                    (roslynTypeName context t)
                    (sprintf "Item%d" (i + 1))
                    [ ``public`` ]
                    get
                    (``=>`` (ident "Data" <|.|> ident (sprintf "Item%d" (i + 1))))
                :> MemberDeclarationSyntax

            match qsharpType.Resolution with
            | QsTypeKind.TupleType many -> many |> Seq.mapi buildOne |> List.ofSeq
            | _ -> []

        let buildDeconstruct =
            let body =
                let buildOne i t =
                    let lhs = ident (sprintf "item%d" (i + 1))
                    let rhs = ident "Data" <|.|> ident (sprintf "Item%d" (i + 1))
                    statement (lhs <-- rhs)

                match qsharpType.Resolution with
                | QsTypeKind.TupleType many -> many |> Seq.mapi buildOne |> List.ofSeq
                | _ -> []

            let parameters =
                let buildOneParameter i t =
                    let paramType = t |> roslynTypeName context
                    ``out param`` (sprintf "item%d" (i + 1)) ``of`` (``type`` paramType)

                match qsharpType.Resolution with
                | QsTypeKind.TupleType many -> many |> Seq.mapi buildOneParameter |> List.ofSeq
                | _ -> []

            method "void" "Deconstruct" ``<<`` [] ``>>`` ``(`` parameters ``)`` [ ``public`` ] ``{`` body ``}``
            :> MemberDeclarationSyntax

        let baseClassName = udtBaseClassName context qsharpType
        let baseClass = simpleBase baseClassName
        let modifiers = [ classAccess udt.Access ]
        let interfaces = [ simpleBase "IApplyData" ]
        let constructors = [ buildEmptyConstructor; buildBaseTupleConstructor ]
        let qubitsField = buildQubitsField context qsharpType
        let itemFields = buildNamedItemFields @ buildItemFields
        let allFields = itemFields @ qubitsField
        let allMethods = [ buildDeconstruct ]

        ``class``
            name
            ``<<``
            []
            ``>>``
            ``:``
            (Some baseClass)
            ``,``
            interfaces
            modifiers
            ``{``
            (constructors @ allFields @ allMethods)
            ``}``
        :> MemberDeclarationSyntax

    // Generates the code for all the elements of the given namespace.
    let buildNamespace globalContext (nsName, localElements) =
        let buildOne =
            function
            | QsCallable op when op.Kind = TypeConstructor -> None
            | QsCustomType udt -> udt |> buildUdtClass globalContext |> Some
            | QsCallable op -> op |> buildOperationClass globalContext |> Some

        let members = localElements |> List.map buildOne |> List.choose id

        ``#line hidden`` <| ``namespace`` nsName ``{`` [] (members) ``}`` :> MemberDeclarationSyntax

    type AttributeGenerator() =
        inherit NamespaceTransformation(TransformationOptions.NoRebuild)

        let mutable attributes = []

        let GenerateAndAdd attrName json =
            let attr = attribute (Some assembly) (ident attrName) [ literal json ]
            attributes <- attr :: attributes

        member internal this.Apply(elements: IEnumerable<QsNamespaceElement>) =
            attributes <- []

            for element in elements do
                base.OnNamespaceElement element |> ignore

            attributes |> List.rev

        override this.OnSpecializationDeclaration(spec: QsSpecialization) =
            (SpecializationDeclarationHeader.New spec).ToJson() |> GenerateAndAdd "SpecializationDeclaration"
            spec

        override this.OnCallableDeclaration(callable: QsCallable) =
            (CallableDeclarationHeader.New callable).ToJson() |> GenerateAndAdd "CallableDeclaration"
            base.OnCallableDeclaration callable

        override this.OnTypeDeclaration(qsType: QsCustomType) =
            (TypeDeclarationHeader.New qsType).ToJson() |> GenerateAndAdd "TypeDeclaration"
            qsType


    let buildDeclarationAttributes elements =
        let generator = new AttributeGenerator()
        generator.Apply elements

    // Returns only those namespaces and their elements that are defined for the given file.
    let findLocalElements selector (fileName: string) syntaxTree =
        syntaxTree
        |> Seq.map (fun ns ->
            (ns.Name, (FilterBySourceFile.Apply(ns, fileName)).Elements |> Seq.choose selector |> Seq.toList))
        |> Seq.sortBy fst
        |> Seq.filter (fun (_, elements) -> not elements.IsEmpty)
        |> Seq.toList

    /// The comment that is displayed at the top of generated files.
    let internal autogenComment =
        [
            "//------------------------------------------------------------------------------"
            "// <auto-generated>                                                             "
            "//     This code was generated by a tool.                                       "
            "//     Changes to this file may cause incorrect behavior and will be lost if    "
            "//     the code is regenerated.                                                 "
            "// </auto-generated>                                                            "
            "//------------------------------------------------------------------------------"
        ]

    // Builds the C# syntaxTree for the Q# elements defined in the given file.
    let buildSyntaxTree localElements (context: CodegenContext) =
        let namespaces = if isConcreteIntrinsic context then autoNamespacesWithInterfaces else autoNamespaces
        let usings = namespaces |> List.map (fun ns -> using ns)
        let attributes = localElements |> List.map (snd >> buildDeclarationAttributes) |> List.concat
        let namespaces = localElements |> List.map (buildNamespace context)

        ``compilation unit`` attributes usings namespaces
        // We add a "pragma warning disable 1591" since we don't generate doc comments in our C# code.
        |> pragmaDisableWarning 1591
        |> pragmaDisableWarning 162 // unreachable code
        |> pragmaDisableWarning 436 // shadowing existing classes from references
        |> ``with leading comments`` autogenComment

    // Helper method that takes a SyntaxTree, adds trivia (formatting)
    // and returns it as a string
    let formatSyntaxTree tree =
        try
            let ws = new AdhocWorkspace()
            let formattedRoot = Formatter.Format(tree, ws)
            formattedRoot.ToFullString()
        with
        | :? ReflectionTypeLoadException as l ->
            let msg = l.LoaderExceptions |> Array.fold (fun msg e -> msg + ";" + e.Message) ""
            failwith msg

    /// Builds the SyntaxTree for callables and types loaded via test names,
    /// formats it and returns it as a string.
    /// Returns null if no elements have been loaded via test name.
    let loadedViaTestNames dllName (globalContext: CodegenContext) =
        let isLoadedViaTestName nsElement =
            if globalContext.ExposeReferencesViaTestNames then
                let asOption =
                    function
                    | Value _ -> Some nsElement
                    | _ -> None

                match nsElement with
                | QsCallable c -> SymbolResolution.TryGetTestName c.Attributes
                | QsCustomType t -> SymbolResolution.TryGetTestName t.Attributes
                |> asOption
            else
                None

        let context = { globalContext with fileName = Some dllName }
        let localElements = findLocalElements isLoadedViaTestName dllName context.allQsElements

        let getNameCollisions (_, elems: QsNamespaceElement list) =
            let tryGetCollision =
                function
                | QsCustomType t ->
                    match SymbolResolution.TryGetOriginalName t.Attributes with
                    | Value origName ->
                        match context.allUdts.TryGetValue origName with
                        | true, collision ->
                            if context.GenerateCodeForSource collision.Source.AssemblyOrCodeFile then
                                None
                            else
                                Some(origName.Namespace, QsCustomType collision)
                        | _ -> None
                    | Null -> None
                | QsCallable c ->
                    match SymbolResolution.TryGetOriginalName c.Attributes with
                    | Value origName ->
                        match context.allCallables.TryGetValue origName with
                        | true, collision ->
                            if context.GenerateCodeForSource collision.Source.AssemblyOrCodeFile then
                                None
                            else
                                Some(origName.Namespace, QsCallable collision)
                        | _ -> None
                    | Null -> None

            elems |> List.choose tryGetCollision

        if localElements.Any() then
            let collisions =
                (localElements |> Seq.collect getNameCollisions).ToLookup(fst, snd)
                |> Seq.map (fun g -> g.Key, g |> Seq.toList)
                |> Seq.toList

            buildSyntaxTree (localElements @ collisions) context |> formatSyntaxTree
        else
            null

    /// Main entry method for a CodeGenerator.
    /// Builds the SyntaxTree for the given Q# syntax tree, formats it and returns it as a string.
    /// Omits code generation for intrinsic callables in references.
    let generate fileName globalContext =
        let isIntrinsic =
            function
            | QsCallable c -> c.Signature.Information.InferredInformation.IsIntrinsic
            | QsCustomType _ -> false

        let filterIntrinsics (ns, elems) =
            ns, elems |> List.filter (not << isIntrinsic)

        let context = { globalContext with fileName = Some fileName }

        let localElements =
            let elements = findLocalElements Some fileName context.allQsElements
            if fileName.EndsWith ".dll" then elements |> List.map filterIntrinsics else elements

        buildSyntaxTree localElements context |> formatSyntaxTree
