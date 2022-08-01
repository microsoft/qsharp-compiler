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

let private attributeAsCallExpr (sym: QsSymbol, ex: QsExpression) =
    let combinedRange = QsNullable.Map2 Range.Span sym.Range ex.Range
    let id = { Expression = QsExpressionKind.Identifier(sym, Null); Range = sym.Range }
    { Expression = QsExpressionKind.CallLikeExpression(id, ex); Range = combinedRange }

// TODO: RELEASE 2022-05: Remove SymbolInformation.
[<Obsolete "Replaced by SymbolOccurrence.">]
type SymbolInformation =
    {
        DeclaredSymbols: ImmutableHashSet<QsSymbol>
        UsedVariables: ImmutableHashSet<QsSymbol>
        UsedTypes: ImmutableHashSet<QsType>
        UsedLiterals: ImmutableHashSet<QsExpression>
    }

// TODO: RELEASE 2022-05: Remove SymbolInformation.
[<Extension>]
[<Obsolete "Replaced by SymbolOccurrence.InFragment.">]
let public SymbolInformation fragmentKind =
    let occurrences = SymbolOccurrence.inFragment fragmentKind

    let tryDeclaration =
        function
        | Declaration s -> Some s
        | _ -> None

    let tryVariable =
        function
        | UsedVariable s -> Some s
        | _ -> None

    let tryType =
        function
        | UsedType t -> Some t
        | _ -> None

    let tryLiteral =
        function
        | UsedLiteral e -> Some e
        | _ -> None

    {
        DeclaredSymbols = Seq.choose tryDeclaration occurrences |> ImmutableHashSet.CreateRange
        UsedVariables = Seq.choose tryVariable occurrences |> ImmutableHashSet.CreateRange
        UsedTypes = Seq.choose tryType occurrences |> ImmutableHashSet.CreateRange
        UsedLiterals = Seq.choose tryLiteral occurrences |> ImmutableHashSet.CreateRange
    }

let rec private expressionsInInitializer item =
    match item.Initializer with
    | QsInitializerKind.QubitRegisterAllocation ex -> seq { yield ex }
    | QsInitializerKind.QubitTupleAllocation items -> items |> Seq.collect expressionsInInitializer
    | _ -> Seq.empty

[<Extension>]
let public CallExpressions fragmentKind =
    let isCallExpression (ex: QsExpression) =
        match ex.Expression with
        | CallLikeExpression _ -> seq { yield ex }
        | _ -> Enumerable.Empty()

    let callExpressions (ex: QsExpression) =
        (ex.ExtractAll isCallExpression).ToImmutableArray()

    match fragmentKind with
    | QsFragmentKind.ExpressionStatement ex
    | QsFragmentKind.ReturnStatement ex
    | QsFragmentKind.FailStatement ex
    | QsFragmentKind.MutableBinding (_, ex)
    | QsFragmentKind.ImmutableBinding (_, ex)
    | QsFragmentKind.ValueUpdate (_, ex)
    | QsFragmentKind.IfClause ex
    | QsFragmentKind.ElifClause ex
    | QsFragmentKind.ForLoopIntro (_, ex)
    | QsFragmentKind.WhileLoopIntro ex
    | QsFragmentKind.UntilSuccess (ex, _) -> callExpressions ex
    | QsFragmentKind.UsingBlockIntro (_, init)
    | QsFragmentKind.BorrowingBlockIntro (_, init) ->
        (expressionsInInitializer init |> Seq.collect callExpressions).ToImmutableArray()
    | QsFragmentKind.DeclarationAttribute (sym, ex) -> attributeAsCallExpr (sym, ex) |> ImmutableArray.Create
    | _ -> ImmutableArray.Empty

let private tryResolveWith resolve extract (currentNS, source) =
    function
    | QsSymbolKind.Symbol sym ->
        try
            match resolve sym (currentNS, source) with
            | Found decl -> Some decl, Some sym
            | _ -> None, Some sym
        with
        | :? ArgumentException -> None, Some sym
    | QsSymbolKind.QualifiedSymbol (ns, sym) ->
        try
            match extract { Namespace = ns; Name = sym } (currentNS, source) with
            | Found decl -> Some decl, Some sym
            | _ -> None, None
        with
        | :? ArgumentException -> None, None
    | _ -> None, None

let private globalTypeResolution (symbolTable: NamespaceManager) (currentNS, source) (udt: QsSymbol) =
    tryResolveWith symbolTable.TryResolveAndGetType symbolTable.TryGetType (currentNS, source) udt.Symbol

let private globalCallableResolution (symbolTable: NamespaceManager) (currentNS, source) (qsSym: QsSymbol) =
    tryResolveWith symbolTable.TryResolveAndGetCallable symbolTable.TryGetCallable (currentNS, source) qsSym.Symbol

let private newLine = "    \n" // spaces first here so it will work with markdown as well
let private withNewLine line = sprintf "%s%s" line newLine

/// Converts the first character of the string to uppercase.
let private toUpperFirst (s: string) = s.[0..0].ToUpper() + s.[1..]

let private asDocComment (doc: string seq) =
    if isNull doc then
        null
    elif doc.Any() then
        let minAmountOfLeadingWS = doc |> Seq.map (fun line -> line.Length - line.TrimStart().Length) |> Seq.min
        DocComment(doc |> Seq.map (fun line -> line.Substring(minAmountOfLeadingWS).TrimEnd()))
    else
        DocComment(doc)

[<Extension>]
let public ParameterDescription (doc: string seq) paramName =
    let docComment = asDocComment doc

    if not (isNull docComment) then
        match docComment.Input.TryGetValue paramName with
        | true, description -> if String.IsNullOrWhiteSpace description then "" else description
        | false, _ -> ""
    else
        ""

[<Extension>]
let public PrintSummary (doc: string seq) markdown =
    let docComment = asDocComment doc

    if not (isNull docComment) then
        let info = if markdown then docComment.FullSummary else docComment.ShortSummary
        if String.IsNullOrWhiteSpace info then "" else sprintf "%s%s%s" newLine newLine info
    else
        ""

let private namespaceDocumentation (docs: ILookup<_, ImmutableArray<_>>, markdown) =
    // FIXME: currently all documentation for a namespace is simply given in concatenated form here
    let allDoc = docs.SelectMany(fun entry -> entry.SelectMany(fun d -> d.AsEnumerable())) // the key is the source file
    PrintSummary allDoc markdown

/// Attaches the access modifier to the declaration kind string.
let private showAccess kind =
    function
    | Public -> kind
    | Internal -> ReservedKeywords.Declarations.Internal + " " + kind

type private TName() =
    inherit SyntaxTreeToQsharp.TypeTransformation()

    override this.OnCharacteristicsExpression characteristics =
        if characteristics.AreInvalid then
            this.Output <- "?"
            characteristics
        else
            base.OnCharacteristicsExpression characteristics

    override this.OnInvalidType() =
        this.Output <- "?"
        InvalidType

    override this.OnUserDefinedType udt =
        this.Output <- udt.Name
        UserDefinedType udt

    member this.Apply t =
        this.OnType t |> ignore
        this.Output

let private typeString = TName()
let private typeName = typeString.Apply

let private characteristicsAnnotation (ex, format) =
    let charEx = SyntaxTreeToQsharp.CharacteristicsExpression ex
    if String.IsNullOrWhiteSpace charEx then "" else sprintf "is %s" charEx |> format

[<Extension>]
let public TypeInfo (symbolTable: NamespaceManager) (currentNS, source) (qsType: QsType) markdown =
    let udtInfo udt =
        match udt |> globalTypeResolution symbolTable (currentNS, source) with
        | Some decl, _ ->
            let kind = showAccess "user-defined type" decl.Access |> toUpperFirst
            let name = decl.QualifiedName.Name |> withNewLine
            let ns = sprintf "Namespace: %s" decl.QualifiedName.Namespace |> withNewLine
            let info = sprintf "Underlying type: %s" (typeName decl.Type)
            let doc = PrintSummary decl.Documentation markdown
            sprintf "%s %s%s%s%s" kind name ns info doc
        | None, Some sym -> sprintf "Type %s" sym
        | _ -> "?"

    let typeParamName onUnknown (sym: QsSymbol) =
        let name =
            match sym.Symbol with
            | Symbol name -> name
            | _ -> null

        if String.IsNullOrWhiteSpace name then onUnknown else sprintf "'%s" name

    let rec typeKindName tKind =
        let udtName udt =
            match udt |> globalTypeResolution symbolTable (currentNS, source) with
            | Some _, Some sym -> sym // hovering over the udt will show its namespace and source
            | _ -> "?"

        let characteristics ex =
            characteristicsAnnotation (QsType.TryResolve ex, sprintf " %s")

        let inner (ts: ImmutableArray<QsType>) =
            [
                for t in ts do
                    yield t.Type
            ]
            |> List.map typeKindName

        match tKind with
        | QsTypeKind.UserDefinedType udt -> udt |> udtName
        | QsTypeKind.ArrayType b -> sprintf "%s[]" (b.Type |> typeKindName)
        | QsTypeKind.TupleType ts -> sprintf "(%s)" (ts |> inner |> String.concat ", ")
        | QsTypeKind.TypeParameter p -> p |> typeParamName "?"
        | QsTypeKind.Operation ((it, ot), cs) ->
            sprintf "(%s => %s%s)" (it.Type |> typeKindName) (ot.Type |> typeKindName) (cs |> characteristics)
        | QsTypeKind.Function (it, ot) -> sprintf "(%s -> %s)" (it.Type |> typeKindName) (ot.Type |> typeKindName)
        | QsTypeKind.UnitType -> QsTypeKind.UnitType |> ResolvedType.New |> typeName
        | QsTypeKind.Int -> QsTypeKind.Int |> ResolvedType.New |> typeName
        | QsTypeKind.BigInt -> QsTypeKind.BigInt |> ResolvedType.New |> typeName
        | QsTypeKind.Double -> QsTypeKind.Double |> ResolvedType.New |> typeName
        | QsTypeKind.Bool -> QsTypeKind.Bool |> ResolvedType.New |> typeName
        | QsTypeKind.String -> QsTypeKind.String |> ResolvedType.New |> typeName
        | QsTypeKind.Qubit -> QsTypeKind.Qubit |> ResolvedType.New |> typeName
        | QsTypeKind.Result -> QsTypeKind.Result |> ResolvedType.New |> typeName
        | QsTypeKind.Pauli -> QsTypeKind.Pauli |> ResolvedType.New |> typeName
        | QsTypeKind.Range -> QsTypeKind.Range |> ResolvedType.New |> typeName
        | QsTypeKind.MissingType -> QsTypeKind.MissingType |> ResolvedType.New |> typeName
        | QsTypeKind.InvalidType -> QsTypeKind.InvalidType |> ResolvedType.New |> typeName

    let doc = PrintSummary qsType.Documentation markdown

    match qsType.Type with
    | QsTypeKind.UserDefinedType udt -> udtInfo udt
    | QsTypeKind.TypeParameter p -> sprintf "Type parameter %s%s" (p |> typeParamName "") doc
    | _ -> sprintf "Built-in type %s%s" (typeKindName qsType.Type) doc

let private printCallableKind =
    function
    | QsCallableKind.Function -> "function"
    | QsCallableKind.Operation -> "operation"
    | QsCallableKind.TypeConstructor -> "type constructor"

[<Extension>]
let public PrintArgumentTuple item =
    SyntaxTreeToQsharp.ArgumentTuple(item, new Func<_, _>(typeName)) // note: needs to match the corresponding part of the output constructed by PrintSignature below!

[<Extension>]
let public PrintSignature (header: CallableDeclarationHeader) =
    let callable =
        QsCallable.New
            header.Kind
            (header.Source, Null)
            (header.QualifiedName,
             header.Attributes,
             header.Access,
             header.ArgumentTuple,
             header.Signature,
             ImmutableArray.Empty,
             ImmutableArray.Empty,
             QsComments.Empty)

    let signature = SyntaxTreeToQsharp.DeclarationSignature(callable, new Func<_, _>(typeName))

    let annotation =
        characteristicsAnnotation (header.Signature.Information.Characteristics, sprintf "%s%s" newLine)

    sprintf "%s%s" signature annotation

[<Extension>]
let public VariableInfo
    (symbolTable: NamespaceManager)
    (locals: LocalDeclarations)
    (currentNS, source)
    (qsSym: QsSymbol)
    markdown
    =
    match qsSym |> globalCallableResolution symbolTable (currentNS, source) with
    | Some decl, _ ->
        let kind = showAccess (printCallableKind decl.Kind) decl.Access |> toUpperFirst
        let nameAndSignature = PrintSignature decl |> withNewLine
        let ns = sprintf "Namespace: %s" decl.QualifiedName.Namespace
        let doc = PrintSummary decl.Documentation markdown
        sprintf "%s %s%s%s" kind nameAndSignature ns doc
    | None, Some sym ->
        let localVars = locals.AsVariableLookup()

        if localVars.ContainsKey sym then
            let decl = localVars.[sym]
            let kind = if decl.InferredInformation.IsMutable then "Mutable" else "Immutable"
            sprintf "%s variable %s%sType: %s" kind sym newLine (typeName decl.Type)
        else
            match symbolTable.Documentation().TryGetValue(sym) with
            | true, docs -> sprintf "Namespace %s%s" sym (namespaceDocumentation (docs, markdown))
            | false, _ -> sprintf "Variable %s" sym
    | _ ->
        match qsSym.Symbol with
        | QualifiedSymbol (ns, sym) -> sprintf "%s.%s" ns sym
        | _ -> "Unknown symbol"

[<Extension>]
let public DeclarationInfo symbolTable (locals: LocalDeclarations) (currentNS, source) (qsSym: QsSymbol) markdown =
    match qsSym.Symbol with // needs to be before querying callables
    | QsSymbolKind.Symbol name ->
        match locals.AsVariableLookup().TryGetValue name with // needs to be before querying callables
        | true, decl ->
            let kind = if decl.InferredInformation.IsMutable then "a mutable" else "an immutable"
            let name = name |> withNewLine
            let info = sprintf "Type: %s" (typeName decl.Type)
            sprintf "Declaration of %s variable %s%s" kind name info
        | false, _ ->
            match qsSym |> globalTypeResolution symbolTable (currentNS, source) with // needs to be before querying callables
            | Some decl, _ ->
                let kind = showAccess "user-defined type" decl.Access
                let name = decl.QualifiedName.Name |> withNewLine
                let ns = sprintf "Namespace: %s" decl.QualifiedName.Namespace |> withNewLine
                let info = sprintf "Underlying type: %s" (decl.Type |> typeName)
                let doc = PrintSummary decl.Documentation markdown
                sprintf "Declaration of %s %s%s%s%s" kind name ns info doc
            | None, _ ->
                match qsSym |> globalCallableResolution symbolTable (currentNS, source) with
                | Some decl, _ ->
                    let kind = showAccess (printCallableKind decl.Kind) decl.Access
                    let name = decl.QualifiedName.Name |> withNewLine
                    let ns = sprintf "Namespace: %s" decl.QualifiedName.Namespace |> withNewLine
                    let input = sprintf "Input type: %s" (decl.Signature.ArgumentType |> typeName) |> withNewLine
                    let output = sprintf "Output type: %s" (decl.Signature.ReturnType |> typeName) |> withNewLine

                    let functorSupport characteristics =
                        let charEx = SyntaxTreeToQsharp.CharacteristicsExpression characteristics
                        if String.IsNullOrWhiteSpace charEx then "(None)" else charEx

                    let fs =
                        sprintf "Supported functors: %s" (decl.Signature.Information.Characteristics |> functorSupport)

                    let doc = PrintSummary decl.Documentation markdown
                    sprintf "Declaration of %s %s%s%s%s%s%s" kind name ns input output fs doc
                | None, _ ->
                    match symbolTable.Documentation().TryGetValue name with
                    | true, docs ->
                        sprintf "Declaration of a partial namespace %s%s" name (namespaceDocumentation (docs, markdown))
                    | false, _ -> sprintf "Symbol declaration %s" name
    | QsSymbolKind.MissingSymbol -> "Discarded symbol assignment"
    | QsSymbolKind.OmittedSymbols -> "Omitted symbols representing the argument of the top level declaration"
    | _ -> "Invalid symbol declaration"

[<Extension>]
let public LiteralInfo (ex: QsExpression) markdown =
    match ex.Expression with
    | QsExpressionKind.UnitValue -> sprintf "Built-in Unit literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.IntLiteral _ -> sprintf "Built-in Int literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.BigIntLiteral _ -> sprintf "Built-in BigInt literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.DoubleLiteral _ -> sprintf "Built-in Double literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.BoolLiteral _ -> sprintf "Built-in Bool literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.StringLiteral _ -> sprintf "Built-in String literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.ResultLiteral _ -> sprintf "Built-in Result literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.PauliLiteral _ -> sprintf "Build-in Pauli literal %s" (PrintSummary ex.Documentation markdown)
    | QsExpressionKind.MissingExpr -> "Omitted argument used within partial applications"
    | QsExpressionKind.InvalidExpr -> "?"
    | _ ->
        QsCompilerError.Raise "no string defined for given expression in query to LiteralInfo"
        ""


// extensions providing information for editor commands

/// Returns all of the operation characteristics found in the given type.
[<Extension>]
let rec ExtractCharacteristics (qsType: QsType) =
    let extract (t: QsType) =
        match t.Type with
        | QsTypeKind.Operation ((_, _), characteristics) -> seq { yield characteristics }
        | _ -> Seq.empty

    qsType.ExtractAll extract

[<Extension>]
let public LocalVariable (locals: LocalDeclarations) (qsSym: QsSymbol) =
    match qsSym.Symbol with
    | QsSymbolKind.Symbol sym ->
        match locals.AsVariableLookup().TryGetValue sym with
        | true, decl ->
            let noPositionInfoException = ArgumentException "no position information available for local variable"
            let position = decl.Position.ValueOrApply(fun () -> noPositionInfoException |> raise)
            (sym, position, decl.Range) |> Value
        | false, _ -> Null
    | _ -> Null

[<Extension>]
let public VariableDeclaration
    (symbolTable: NamespaceManager)
    (locals: LocalDeclarations)
    (currentNS, source)
    (qsSym: QsSymbol)
    =
    match qsSym |> globalCallableResolution symbolTable (currentNS, source) with
    | Some decl, Some _ ->
        decl.Location
        |> QsNullable<_>.Map (fun loc -> Source.assemblyOrCodeFile decl.Source, loc.Offset, loc.Range)
    | _ -> LocalVariable locals qsSym |> QsNullable<_>.Map (fun (_, pos, range) -> source, pos, range)

[<Extension>]
let public TypeDeclaration (symbolTable: NamespaceManager) (currentNS, source) (qsType: QsType) =
    match qsType.Type with
    | QsTypeKind.UserDefinedType udt ->
        match udt |> globalTypeResolution symbolTable (currentNS, source) with
        | Some decl, _ ->
            decl.Location
            |> QsNullable<_>.Map (fun loc -> Source.assemblyOrCodeFile decl.Source, loc.Offset, loc.Range)
        | _ -> Null
    | _ -> Null

[<Extension>]
let public SymbolDeclaration
    (symbolTable: NamespaceManager)
    (locals: LocalDeclarations)
    (currentNS, source)
    (qsSym: QsSymbol)
    =
    match qsSym.Symbol with // needs to be first
    | QsSymbolKind.Symbol _ ->
        match qsSym |> globalTypeResolution symbolTable (currentNS, source) with // needs to be first
        | Some decl, _ ->
            decl.Location
            |> QsNullable<_>.Map (fun loc -> Source.assemblyOrCodeFile decl.Source, loc.Offset, loc.Range)
        | None, _ ->
            match qsSym |> globalCallableResolution symbolTable (currentNS, source) with
            | Some decl, _ ->
                decl.Location
                |> QsNullable<_>.Map (fun loc -> Source.assemblyOrCodeFile decl.Source, loc.Offset, loc.Range)
            | _ -> LocalVariable locals qsSym |> QsNullable<_>.Map (fun (_, pos, range) -> source, pos, range)
    | _ -> Null
