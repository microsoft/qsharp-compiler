// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.CallAnalyzer

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

let referenceReason (name: string) (range: _ QsNullable) (header: SpecializationDeclarationHeader) diagnostic =
    let warningCode =
        match diagnostic.Diagnostic with
        | Error ErrorCode.UnsupportedResultComparison -> Some WarningCode.UnsupportedResultComparison
        | Error ErrorCode.ResultComparisonNotInOperationIf -> Some WarningCode.ResultComparisonNotInOperationIf
        | Error ErrorCode.ReturnInResultConditionedBlock -> Some WarningCode.ReturnInResultConditionedBlock
        | Error ErrorCode.SetInResultConditionedBlock -> Some WarningCode.SetInResultConditionedBlock
        | Error ErrorCode.UnsupportedCallableCapability -> Some WarningCode.UnsupportedCallableCapability
        | _ -> None

    let args =
        seq {
            name
            header.Source.CodeFile
            string (diagnostic.Range.Start.Line + 1)
            string (diagnostic.Range.Start.Column + 1)
            yield! diagnostic.Arguments
        }

    Option.map (fun code -> QsCompilerDiagnostic.Warning(code, args) (range.ValueOr Range.Zero)) warningCode

type CallPattern =
    {
        Name: QsQualifiedName
        Range: Range QsNullable
    }

    interface IPattern with
        member _.Capability _ = InvalidOperationException() |> raise // TODO

        member pattern.Diagnose context =
            let parentNS = context.Symbols.Parent.Namespace
            let parentFile = context.Symbols.SourceFile

            match context.Globals.TryGetCallable pattern.Name (parentNS, parentFile) with
            | Found declaration ->
                let capability =
                    (SymbolResolution.TryGetRequiredCapability declaration.Attributes).ValueOr RuntimeCapability.Base

                if context.Capability.Implies capability then
                    None
                else
                    let args = [ pattern.Name.Name; string capability; context.ProcessorArchitecture ]
                    let range = pattern.Range.ValueOr Range.Zero
                    QsCompilerDiagnostic.Error(ErrorCode.UnsupportedCallableCapability, args) range |> Some
            | _ -> None

        member pattern.Explain context =
            context.Globals.ImportedSpecializations pattern.Name
            |> Seq.collect (fun (header, impl) ->
                match impl with
                | Provided (_, scope) ->
                    CallPattern.AnalyzeScope(scope, CallPattern.AnalyzeAllShallow)
                    |> Seq.choose (fun p -> p.Diagnose context)
                    |> Seq.map (fun d ->
                        header.Location
                        |> QsNullable<_>.Map (fun l -> { d with Range = l.Offset + d.Range })
                        |> QsNullable.defaultValue d)
                    |> Seq.choose (referenceReason pattern.Name.Name pattern.Range header)
                | _ -> Seq.empty)

    static member AnalyzeShallow(action: SyntaxTreeTransformation -> _) =
        let transformation = LocationTrackingTransformation TransformationOptions.NoRebuild
        let patterns = ResizeArray()

        transformation.Expressions <-
            { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
                member _.OnTypedExpression expression =
                    match expression.Expression with
                    | Identifier (GlobalCallable name, _) ->
                        let range = QsNullable.Map2(+) transformation.Offset expression.Range
                        patterns.Add { Name = name; Range = range }
                    | _ -> ()

                    base.OnTypedExpression expression
            }

        action transformation
        Seq.map (fun p -> p :> IPattern) patterns

    static member AnalyzeSyntax action =
        Seq.collect
            ((|>) action)
            [
                ResultAnalyzer.analyze
                StatementAnalyzer.analyze
                TypeAnalyzer.analyze
                ArrayAnalyzer.analyze
            ]

    static member AnalyzeAllShallow action =
        Seq.append (CallPattern.AnalyzeSyntax action) (CallPattern.AnalyzeShallow action)

    static member AnalyzeScope(scope, analyzer: Analyzer) =
        analyzer (fun transformation -> transformation.Statements.OnScope scope |> ignore)

let analyzeSyntax = CallPattern.AnalyzeSyntax

let analyzeAllShallow = CallPattern.AnalyzeAllShallow

let analyzeScope scope analyzer =
    CallPattern.AnalyzeScope(scope, analyzer)
