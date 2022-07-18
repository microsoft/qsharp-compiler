// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.Context

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens

type SyntaxTokenContext =
    {
        Range: Range
        Self: QsNullable<QsFragmentKind>
        Previous: QsNullable<QsFragmentKind>
        Next: QsNullable<QsFragmentKind>
        Parents: QsNullable<QsFragmentKind> []
    }

let private ApplyOrDefaultTo fallback nullable apply =
    match nullable with
    | Null -> fallback
    | Value v -> apply v

/// Verifies that the number of parents in the given context is 0.
/// Returns an array with suitable diagnostics.
let private verifyNamespace (context: SyntaxTokenContext) (documentKind: DocumentKind) =
    match documentKind with
    | NotebookCell -> false, [| (ErrorCode.InvalidNamespaceDeclaration |> Error, context.Range) |]
    | File ->
        if context.Parents.Length = 0 then
            true, [||]
        else
            false, [| (ErrorCode.NotWithinGlobalScope |> Error, context.Range) |]

/// Verifies that the direct parent in the given context is a namespace declaration.
/// Indicates that the fragment is to be excluded from compilation if the direct parent is not a namespace declaration.
/// Returns an array with suitable diagnostics.
let private verifyDeclaration (context: SyntaxTokenContext) (documentKind: DocumentKind) =
    match documentKind with
    | NotebookCell -> true, [||]
    | File ->
        let errMsg = false, [| (ErrorCode.NotWithinNamespace |> Error, context.Range) |]

        let isNamespace =
            function
            | [] -> errMsg
            | parent :: _ ->
                match parent with
                | Value (NamespaceDeclaration _) -> true, [||]
                | Value (InvalidFragment) -> false, [||]
                | _ -> errMsg

        context.Parents |> Array.toList |> isNamespace

/// Verifies that either there is no preceding fragment in the given context,
/// or the preceding fragment is another open directive.
/// Verifies that the direct parent is a namespace declaration.
/// Returns an array with suitable diagnostics.
let private verifyOpenDirective (context: SyntaxTokenContext) (documentKind: DocumentKind) =
    match context.Previous with
    | Value (OpenDirective _)
    | Null -> verifyDeclaration context documentKind
    | Value InvalidFragment -> false, [||]
    | _ -> false, [| (ErrorCode.MisplacedOpenDirective |> Error, context.Range) |] // open directives may only occur at the beginning of a namespace

/// Verifies that the next fragment is either another attribute or a function, operation, or type declaration.
/// Verifies that the direct parent is a namespace declaration.
/// Returns an array with suitable diagnostics.
let private verifyDeclarationAttribute (context: SyntaxTokenContext) (documentKind: DocumentKind) =
    match context.Next with
    | Value (FunctionDeclaration _)
    | Value (OperationDeclaration _)
    | Value (TypeDefinition _)
    | Value (DeclarationAttribute _) -> verifyDeclaration context documentKind
    | Value InvalidFragment -> false, [||]
    | _ -> false, [| (ErrorCode.MisplacedDeclarationAttribute |> Error, context.Range) |]


/// Verifies that the given generator is a valid generator for the callable body -
/// i.e. verifies that the generator is either a user defined implementation, or intrinsic.
/// Does *not* verify whether the symbol tuple for a user defined implementation is correct.
/// Returns an array with suitable diagnostics.
let private checkForInvalidBodyGenerator range =
    function
    | Intrinsic
    | FunctorGenerationDirective InvalidGenerator
    | UserDefinedImplementation _ -> [||]
    | _ -> [| (ErrorCode.InvalidBodyGenerator |> Error, range) |]

/// Verifies that the given generator is a valid generator for the adjoint functor.
/// Does *not* verify whether the symbol tuple for a user defined implementation is correct.
/// Returns an array with suitable diagnostics.
let private checkForInvalidAdjointGenerator range =
    function
    | FunctorGenerationDirective Distribute -> [| (ErrorCode.DistributedAdjointGenerator |> Error, range) |]
    | _ -> [||]

/// Verifies that the given generator is a valid generator for the controlled functor.
/// Does *not* verify whether the symbol tuple for a user defined implementation is correct.
/// Returns an array with suitable diagnostics.
let private checkForInvalidControlledGenerator range =
    function
    | FunctorGenerationDirective SelfInverse -> [| (ErrorCode.SelfControlledGenerator |> Error, range) |]
    | FunctorGenerationDirective Invert -> [| (ErrorCode.InvertControlledGenerator |> Error, range) |]
    | _ -> [||]

/// Verifies that the given generator is a valid generator for the controlled adjoint functor.
/// Does *not* verify whether the symbol tuple for a user defined implementation is correct.
/// Returns an array with suitable diagnostics.
let private checkForInvalidControlledAdjointGenerator _ =
    function
    | _ -> [||]

/// Verifies that the direct parent of the given context is either an operation or a function declaration.
/// If the direct parent is a function, verifies that the given specialization is a body specialization.
/// Assuming Self of the given context is a specialization, verifies its generator.
/// Returns an array with suitable diagnostics
let private verifySpecialization (context: SyntaxTokenContext) =
    let checkGenerator =
        function // don't invalidate even if the generator is incorrect
        | BodyDeclaration gen -> true, gen.Generator |> checkForInvalidBodyGenerator gen.RangeOrDefault
        | AdjointDeclaration gen -> true, gen.Generator |> checkForInvalidAdjointGenerator gen.RangeOrDefault
        | ControlledDeclaration gen -> true, gen.Generator |> checkForInvalidControlledGenerator gen.RangeOrDefault
        | ControlledAdjointDeclaration gen ->
            true, gen.Generator |> checkForInvalidControlledAdjointGenerator gen.RangeOrDefault
        | _ -> ArgumentException "not a specialization" |> raise

    let checkForNotValidInFunctionAndGenerator =
        function
        | AdjointDeclaration _ -> false, [| (ErrorCode.AdjointDeclInFunction |> Error, context.Range) |]
        | ControlledDeclaration _ -> false, [| (ErrorCode.ControlledDeclInFunction |> Error, context.Range) |]
        | ControlledAdjointDeclaration _ ->
            false, [| (ErrorCode.ControlledAdjointDeclInFunction |> Error, context.Range) |]
        | decl -> checkGenerator decl

    let NullOr = ApplyOrDefaultTo (false, [||]) context.Self // empty fragments can be excluded from the compilation

    let errMsg = false, [| (ErrorCode.NotWithinCallable |> Error, context.Range) |]

    let isCallable =
        function
        | [] -> errMsg
        | parent :: _ ->
            match parent with
            | Value (OperationDeclaration _) -> NullOr checkGenerator
            | Value (FunctionDeclaration _) -> NullOr checkForNotValidInFunctionAndGenerator
            | Value (InvalidFragment) -> false, [||]
            | _ -> errMsg

    context.Parents |> Array.toList |> isCallable


/// Verifies that either a specialization declaration, or a callable declaration is a closer parent of the given context
/// than any type declaration, namespace declaration or an open directive.
/// If the closest declaration is a function, verifies that Self may occur within functions.
/// Returns an array with suitable diagnostics.
let private verifyStatement (context: SyntaxTokenContext) =
    let checkForNotValidInFunction =
        function
        | UsingBlockIntro _ -> false, [| (ErrorCode.UsingInFunction |> Error, context.Range) |]
        | BorrowingBlockIntro _ -> false, [| (ErrorCode.BorrowingInFunction |> Error, context.Range) |]
        | RepeatIntro _ ->
            // NOTE: if repeat is excluded, exlude the UntilSuccess below!
            true, [| (WarningCode.DeprecatedRUSloopInFunction |> Warning, context.Range) |]
        | UntilSuccess _ ->
            // no need to raise an error - the error comes either from the preceding repeat or because the latter is missing
            true, [||]
        | WithinBlockIntro _ -> false, [| (ErrorCode.ConjugationWithinFunction |> Error, context.Range) |]
        | ApplyBlockIntro _ ->
            // no need to raise an error - the error comes either from the preceding within or because the latter is missing
            false, [||]
        | _ -> true, [||]

    let checkForNotValidInOperation =
        function
        | WhileLoopIntro _ -> false, [| (ErrorCode.WhileLoopInOperation |> Error, context.Range) |]
        | _ -> true, [||]

    let checkForNotValidInApply =
        function
        | ReturnStatement _ -> false, [| (ErrorCode.ReturnFromWithinApplyBlock |> Error, context.Range) |]
        | _ -> true, [||]

    let NullOr = ApplyOrDefaultTo (false, [||]) context.Self // empty fragments can be excluded from the compilation

    let notWithinSpecialization = false, [| (ErrorCode.NotWithinSpecialization |> Error, context.Range) |]

    let rec isStatementScope =
        function
        | [] -> notWithinSpecialization
        | parent :: tail ->
            match parent with
            // (potentially) valid parents for statements
            | Value (ApplyBlockIntro _) -> NullOr checkForNotValidInApply
            | Value (FunctionDeclaration _) -> NullOr checkForNotValidInFunction
            | Value (OperationDeclaration _) -> NullOr checkForNotValidInOperation
            | Value (BodyDeclaration _)
            | Value (AdjointDeclaration _)
            | Value (ControlledDeclaration _)
            | Value (ControlledAdjointDeclaration _) ->
                let isInFunction =
                    if tail.Length = 0 then
                        false
                    else
                        match tail.Head with
                        | Value (FunctionDeclaration _) -> true
                        | _ -> false

                if isInFunction then NullOr checkForNotValidInFunction else true, [||]
            // because we are doing a recursion we need to break if we reach an "invalid" parent
            | Value (NamespaceDeclaration _)
            | Value (TypeDefinition _)
            | Value (OpenDirective _) -> notWithinSpecialization
            | Value (InvalidFragment) -> false, [||]
            | _ -> tail |> isStatementScope

    context.Parents |> Array.toList |> isStatementScope

/// Verifies that the preceding fragment in the given context is an if- or elif-clause.
/// Returns an array with suitable diagnostics.
let private precededByIfOrElif context =
    match context.Previous with
    | Value (IfClause _)
    | Value (ElifClause _) -> verifyStatement context
    | Value InvalidFragment -> false, [||]
    | _ -> false, [| (ErrorCode.MissingPrecedingIfOrElif |> Error, context.Range) |]

/// Verifies that the following fragment in the given context is an until-clause.
/// Returns an array with suitable diagnostics.
let private followedByUntil context =
    match context.Next with
    | Value (UntilSuccess _) -> verifyStatement context
    | Value InvalidFragment -> false, [||]
    | _ -> false, [| (ErrorCode.MissingContinuationUntil |> Error, context.Range) |]

/// Verifies that the preceding fragment in the given context is a repeat-block intro.
/// Returns an array with suitable diagnostics.
let private precededByRepeat context =
    match context.Previous with
    | Value RepeatIntro -> verifyStatement context
    | Value InvalidFragment -> false, [||]
    | _ -> false, [| (ErrorCode.MissingPrecedingRepeat |> Error, context.Range) |]

/// Verifies that the preceding fragment in the given context is a within-block intro.
/// Returns an array with suitable diagnostics.
let private precededByWithin context =
    match context.Previous with
    | Value WithinBlockIntro -> verifyStatement context
    | Value InvalidFragment -> false, [||]
    | _ -> false, [| (ErrorCode.MissingPrecedingWithin |> Error, context.Range) |]

/// Verifies that the following fragment in the given context is an apply-block intro.
/// Returns an array with suitable diagnostics.
let private followedByApply context =
    match context.Next with
    | Value ApplyBlockIntro -> verifyStatement context
    | Value InvalidFragment -> false, [||]
    | _ -> false, [| (ErrorCode.MissingContinuationApply |> Error, context.Range) |]

type ContextVerification = delegate of SyntaxTokenContext * DocumentKind -> (bool * QsCompilerDiagnostic [])

/// Verifies that Self is valid within the given context -
/// i.e. that it is indeed preceded and followed by suitable fragments (if required), and that it has suitable parents.
/// Returns true if Self is sufficiently correct to be included in the compilation, and false otherwise.
/// In particular, specialization declarations with invalid generators are *not* marked as to be excluded.
/// Marked as excluded, on the other hand, are invalid or empty fragments.
let VerifySyntaxTokenContext =
    new ContextVerification(fun context documentKind ->
        match context.Self with
        | Null -> false, [||]
        | Value kind ->
            match kind with
            | ExpressionStatement _ -> verifyStatement context
            | ReturnStatement _ -> verifyStatement context
            | FailStatement _ -> verifyStatement context
            | ImmutableBinding _ -> verifyStatement context
            | MutableBinding _ -> verifyStatement context
            | ValueUpdate _ -> verifyStatement context
            | IfClause _ -> verifyStatement context
            | ElifClause _ -> precededByIfOrElif context
            | ElseClause _ -> precededByIfOrElif context
            | ForLoopIntro _ -> verifyStatement context
            | WhileLoopIntro _ -> verifyStatement context
            | RepeatIntro _ -> followedByUntil context
            | UntilSuccess _ -> precededByRepeat context
            | WithinBlockIntro _ -> followedByApply context
            | ApplyBlockIntro _ -> precededByWithin context
            | UsingBlockIntro _ -> verifyStatement context
            | BorrowingBlockIntro _ -> verifyStatement context
            | BodyDeclaration _ -> verifySpecialization context
            | AdjointDeclaration _ -> verifySpecialization context
            | ControlledDeclaration _ -> verifySpecialization context
            | ControlledAdjointDeclaration _ -> verifySpecialization context
            | OperationDeclaration _ -> verifyDeclaration context documentKind
            | FunctionDeclaration _ -> verifyDeclaration context documentKind
            | TypeDefinition _ -> verifyDeclaration context documentKind
            | OpenDirective _ -> verifyOpenDirective context documentKind
            | DeclarationAttribute _ -> verifyDeclarationAttribute context documentKind
            | NamespaceDeclaration _ -> verifyNamespace context documentKind
            | InvalidFragment _ -> false, [||]
        |> fun (kind, tuple) -> kind, tuple |> Array.map (fun (x, y) -> QsCompilerDiagnostic.New (x, []) y))

let private mergeMaps onDuplicateKey =
    fun map key value ->
        match Map.tryFind key map with
        | Some value' -> Map.add key (onDuplicateKey value value') map
        | None -> Map.add key value map
    |> Map.fold

let rec internal freeVariables e =
    let merge = mergeMaps (@)

    match e.Expression with
    | Identifier ({ Symbol = Symbol name }, _) -> Map.add name [ e.Range ] Map.empty
    | UnitValue
    | IntLiteral _
    | BigIntLiteral _
    | DoubleLiteral _
    | BoolLiteral _
    | ResultLiteral _
    | PauliLiteral _
    | MissingExpr
    | InvalidExpr
    | Identifier _ -> Map.empty
    | NewArray (_, e)
    | NamedItem (e, _)
    | NEG e
    | NOT e
    | BNOT e
    | UnwrapApplication e
    | AdjointApplication e
    | ControlledApplication e -> freeVariables e
    | RangeLiteral (e1, e2)
    | ArrayItem (e1, e2)
    | ADD (e1, e2)
    | SUB (e1, e2)
    | MUL (e1, e2)
    | DIV (e1, e2)
    | MOD (e1, e2)
    | POW (e1, e2)
    | EQ (e1, e2)
    | NEQ (e1, e2)
    | LT (e1, e2)
    | LTE (e1, e2)
    | GT (e1, e2)
    | GTE (e1, e2)
    | AND (e1, e2)
    | OR (e1, e2)
    | BOR (e1, e2)
    | BAND (e1, e2)
    | BXOR (e1, e2)
    | LSHIFT (e1, e2)
    | RSHIFT (e1, e2)
    | CopyAndUpdate (e1, _, e2)
    | CallLikeExpression (e1, e2)
    | SizedArray (e1, e2) -> freeVariables e1 |> merge (freeVariables e2)
    | CONDITIONAL (e1, e2, e3) -> freeVariables e1 |> merge (freeVariables e2) |> merge (freeVariables e3)
    | ValueTuple es
    | StringLiteral (_, es)
    | ValueArray es -> es |> Seq.map freeVariables |> Seq.fold merge Map.empty
    | Lambda lambda ->
        let validVariable (decl: SyntaxTree.LocalVariableDeclaration<SyntaxTree.QsLocalSymbol, _>) =
            match decl.VariableName with
            | SyntaxTree.QsLocalSymbol.ValidName name -> Some name
            | SyntaxTree.QsLocalSymbol.InvalidName -> None

        let bindings = lambda.ArgumentTuple.Items |> Seq.choose validVariable |> Set
        freeVariables lambda.Body |> Map.filter (fun name _ -> Set.contains name bindings |> not)
