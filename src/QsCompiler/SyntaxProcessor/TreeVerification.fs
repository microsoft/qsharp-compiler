// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.SyntaxTree

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


// routines for verifying statement blocks

/// Returns a boolean indicating whether all code paths within the given scope contain a return or fail statement,
/// as well as an array with diagnostics that require a scope-wise verification.
/// Such diagnostics include in particular warnings for all statement that will never be executed, and errors for misplaced returns statements
/// Throws an ArgumentException if the statements contain no location information.
let AllPathsReturnValueOrFail body =
    let diagnostics = ResizeArray<_>()

    let addDiagnostic diag (stm: QsStatement) =
        stm.Location
        |> function
        | Null ->
            ArgumentException "no location set for the given statement"
            |> raise
        | Value loc -> loc.Offset + loc.Range |> diag |> diagnostics.Add

    // generate an error for every return within a using or borrowing block that is not executed as the last statement of a particular path
    let returnsWithinQubitScope = new List<QsStatement>()

    let errorOnCollectedReturns () =
        if returnsWithinQubitScope.Any() then
            for stm in returnsWithinQubitScope do
                stm
                |> addDiagnostic (QsCompilerDiagnostic.Error(ErrorCode.InvalidReturnWithinAllocationScope, []))

            returnsWithinQubitScope.Clear()

    let rec checkReturnStatements withinQubitScope (scope: QsScope) =
        let delayAddingReturns block = // returns all newly detected return statements instead of directly adding them to returnsWithinQubitScope
            let initialReturns = new List<_>(returnsWithinQubitScope)
            checkReturnStatements withinQubitScope block

            let remaining, added =
                returnsWithinQubitScope
                |> Seq.toList
                |> List.partition initialReturns.Contains

            returnsWithinQubitScope.Clear()
            returnsWithinQubitScope.AddRange remaining
            added

        for statement in scope.Statements do
            errorOnCollectedReturns ()

            match statement.Statement with
            | QsStatementKind.QsReturnStatement _ -> if withinQubitScope then returnsWithinQubitScope.Add statement
            | QsStatementKind.QsQubitScope statement ->
                checkReturnStatements true statement.Body
                if not withinQubitScope then returnsWithinQubitScope.Clear()
            | QsStatementKind.QsForStatement statement -> checkReturnStatements withinQubitScope statement.Body
            | QsStatementKind.QsWhileStatement statement -> checkReturnStatements withinQubitScope statement.Body
            | QsStatementKind.QsConjugation statement ->
                let added =
                    statement.OuterTransformation.Body
                    |> delayAddingReturns

                checkReturnStatements withinQubitScope statement.InnerTransformation.Body
                returnsWithinQubitScope.AddRange added
                errorOnCollectedReturns () // returns within any of the two blocks are necessariliy followed by a statement
            | QsStatementKind.QsRepeatStatement statement ->
                let added =
                    statement.RepeatBlock.Body |> delayAddingReturns

                checkReturnStatements withinQubitScope statement.FixupBlock.Body
                errorOnCollectedReturns () // returns within the fixup block are necessarily followed by a statement
                returnsWithinQubitScope.AddRange added
            | QsStatementKind.QsConditionalStatement statement ->
                let added = new List<_>()

                for (_, case) in statement.ConditionalBlocks do
                    case.Body |> delayAddingReturns |> added.AddRange

                match statement.Default with
                | Value block -> checkReturnStatements withinQubitScope block.Body
                | Null -> ()

                returnsWithinQubitScope.AddRange added
            | QsStatementKind.QsExpressionStatement _
            | QsStatementKind.QsFailStatement _
            | QsStatementKind.QsValueUpdate _
            | QsStatementKind.QsVariableDeclaration _
            | QsStatementKind.EmptyStatement -> ()

    // returns true if all paths in the given scope contain a terminating (i.e. return or fail) statement
    let rec checkTermination (scope: QsScope) =
        let isNonTerminatingStatement (qsStatement: QsStatement) =
            match qsStatement.Statement with
            | QsStatementKind.QsReturnStatement _
            | QsStatementKind.QsFailStatement _ -> false
            | QsStatementKind.QsForStatement _ // it is not immediately obvious whether or not the body will get executed, hence non-terminating
            | QsStatementKind.QsWhileStatement _ -> true // same here
            | QsStatementKind.QsQubitScope statement -> checkTermination statement.Body |> not
            | QsStatementKind.QsConjugation statement ->
                checkTermination statement.OuterTransformation.Body
                |> not
                && checkTermination statement.InnerTransformation.Body
                   |> not
            | QsStatementKind.QsRepeatStatement statement ->
                checkTermination statement.FixupBlock.Body
                |> ignore // only here to give warnings for unreachable code

                checkTermination statement.RepeatBlock.Body |> not
            | QsStatementKind.QsConditionalStatement statement ->
                let returns =
                    statement.ConditionalBlocks
                    |> Seq.map (fun (_, case) -> checkTermination case.Body)
                    |> Seq.toList

                match statement.Default with
                | Value block ->
                    checkTermination block.Body |> not
                    || returns |> List.contains false
                | Null -> true
            | QsStatementKind.QsExpressionStatement _
            | QsStatementKind.QsFailStatement _
            | QsStatementKind.QsValueUpdate _
            | QsStatementKind.QsVariableDeclaration _
            | QsStatementKind.EmptyStatement -> true

        let returnOrFailAndAfter =
            Seq.toList
            <| scope.Statements.SkipWhile isNonTerminatingStatement

        if returnOrFailAndAfter.Length <> 0 then
            let unreachable =
                returnOrFailAndAfter.[0].Statement
                |> function
                | QsStatementKind.QsRepeatStatement statement ->
                    statement.FixupBlock.Body.Statements.Concat(returnOrFailAndAfter.Skip(1))
                | _ -> returnOrFailAndAfter.Skip(1)

            for statement in unreachable do
                statement
                |> addDiagnostic (QsCompilerDiagnostic.Warning(WarningCode.UnreachableCode, []))

            true
        else
            false

    checkReturnStatements false body
    checkTermination body, diagnostics.ToArray()


// routines for checking user defined types for cycles

/// Given an immutable array of all defined types with their underlying resolved type,
/// as well as their location (the file they are declared in and the position where the declaration starts),
/// verifies that the defined types do not have circular dependencies.
/// Ignores any usage of a user defined type that is not listed in the given array of types.
/// Returns a lookup that contains the generated diagnostics and their positions for each file.
/// Throws an ArgumentException if the location for a generated diagnostic cannot be determined.
let CheckDefinedTypesForCycles (definitions: ImmutableArray<TypeDeclarationHeader>) =
    let diagnostics = List<_>()

    let getLocation (header: TypeDeclarationHeader) =
        header.Location.ValueOrApply(fun _ ->
            ArgumentException "The given type header contains no location information."
            |> raise)

    // for each defined type build a list of all user defined types it contains, and one with all types it is contained in (convenient for sorting later)
    let containedTypes =
        List.init definitions.Length (fun _ -> List<int>())

    let containedIn =
        List.init definitions.Length (fun _ -> List<int>()) // convenient

    let updateContainedReferences (rootIndex: int option) (source, udt) =
        match definitions
              |> Seq.tryFindIndex (fun header -> header.QualifiedName = udt) with
        | None -> []
        | Some typeIndex ->
            let header = definitions.[typeIndex]

            match rootIndex with
            | None -> [ header |> (fun header -> header.Type) ]
            | Some parent ->
                if typeIndex <> parent then
                    if not (containedTypes.[parent].Contains typeIndex)
                    then containedTypes.[parent].Add typeIndex

                    if not (containedIn.[typeIndex].Contains parent)
                    then containedIn.[typeIndex].Add parent
                else
                    (source,
                     (getLocation header).Range
                     |> QsCompilerDiagnostic.Error(ErrorCode.TypeCannotContainItself, []))
                    |> diagnostics.Add

                []

    let getTypes location (vtype: ResolvedType) (rootIndex: int option) =
        match vtype.Resolution with
        | QsTypeKind.ArrayType a -> [ a ]
        | QsTypeKind.Function (it, ot)
        | QsTypeKind.Operation ((it, ot), _) -> [ it; ot ]
        | QsTypeKind.TupleType vtypeList -> vtypeList |> Seq.toList
        | QsTypeKind.UserDefinedType udt ->
            updateContainedReferences rootIndex (location, QsQualifiedName.New(udt.Namespace, udt.Name))
        | _ -> []

    let walk_udts () = // builds up containedTypes and containedIn
        definitions
        |> Seq.iteri (fun typeIndex header ->
            let queue = Queue()

            let parent =
                (header.QualifiedName.Namespace,
                 header.QualifiedName.Name,
                 header.Location
                 |> QsNullable<_>.Map(fun loc -> loc.Range))
                |> UserDefinedType.New
                |> UserDefinedType
                |> ResolvedType.New

            for entry in getTypes ((getLocation header).Offset, header.SourceFile) parent None do
                queue.Enqueue entry

            let rec search () =
                if queue.Count <> 0 then
                    let ctypes =
                        getTypes ((getLocation header).Offset, header.SourceFile) (queue.Dequeue()) (Some typeIndex)

                    for entry in ctypes do
                        queue.Enqueue entry

                    search ()

            search ())

    walk_udts ()

    // search the graph defined by contained_types for loops (complexity N^2 with BFS/DFS; -> better option (O(N)): sort topologically)
    // (i.e. reconstruct the ordering in which types would have to be defined if everything needs to be resolved before being used)
    let queue = Queue()

    containedIn
    |> List.iteri (fun i x -> if x.Count = 0 then queue.Enqueue i)

    let rec order () =
        if queue.Count <> 0 then
            let current_node = queue.Dequeue()

            for child in containedTypes.[current_node] do
                containedIn.[child].Remove current_node |> ignore
                if containedIn.[child].Count = 0 then queue.Enqueue child

            order ()

    order ()

    let remaining =
        containedIn
        |> List.mapi (fun i x -> (i, x))
        |> List.filter (fun x -> (snd x).Count <> 0)

    if remaining.Length <> 0 then
        for (udtIndex, _) in remaining do
            let udt = definitions.[udtIndex]
            let loc = getLocation udt

            ((loc.Offset, udt.SourceFile),
             loc.Range
             |> QsCompilerDiagnostic.Error(ErrorCode.TypeIsPartOfCyclicDeclaration, []))
            |> diagnostics.Add

    diagnostics.ToLookup
        (fst >> snd,
         (fun ((position, _), diagnostic) ->
             { diagnostic with
                   QsCompilerDiagnostic.Range = position + diagnostic.Range }))
