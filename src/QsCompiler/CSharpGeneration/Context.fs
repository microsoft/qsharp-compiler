// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.CsharpGeneration

open System.Collections.Generic
open System.Collections.Immutable
open System.Linq

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


module internal DeclarationLocations =

    type TransformationState() =

        member val internal CurrentSource = null with get, set
        member val internal DeclarationLocations = new List<string * Position>()

    type NamespaceTransformation(parent: SyntaxTreeTransformation<TransformationState>) =
        inherit NamespaceTransformation<TransformationState>(parent)

        override this.OnSource file =
            this.SharedState.CurrentSource <- Source.assemblyOrCodeFile file
            file

        override this.OnLocation sourceLocation =
            match sourceLocation with
            | Value (loc: QsLocation) when this.SharedState.CurrentSource <> null ->
                this.SharedState.DeclarationLocations.Add(this.SharedState.CurrentSource, loc.Offset)
            | _ -> ()

            sourceLocation


    type internal SyntaxTreeTransformation private (_private_) =
        inherit SyntaxTreeTransformation<TransformationState>(new TransformationState(), TransformationOptions.NoRebuild)

        new() as this =
            new SyntaxTreeTransformation("_private_")
            then
                this.Namespaces <- new NamespaceTransformation(this)

                this.Statements <-
                    new StatementTransformation<TransformationState>(this, TransformationOptions.Disabled)

                this.Expressions <-
                    new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled)

                this.Types <- TypeTransformation<TransformationState>(this, TransformationOptions.Disabled)

        member this.DeclarationLocations = this.SharedState.DeclarationLocations.ToLookup(fst, snd)

    let Accumulate (syntaxTree: IEnumerable<QsNamespace>) =
        let walker = new SyntaxTreeTransformation()

        for ns in syntaxTree do
            walker.Namespaces.OnNamespace ns |> ignore

        walker.DeclarationLocations


type CodegenContext =
    {
        assemblyConstants: IDictionary<string, string>
        allQsElements: IEnumerable<QsNamespace>
        allUdts: ImmutableDictionary<QsQualifiedName, QsCustomType>
        allCallables: ImmutableDictionary<QsQualifiedName, QsCallable>
        declarationPositions: ImmutableDictionary<string, ImmutableSortedSet<Position>>
        byName: ImmutableDictionary<string, (string * QsCallable) list>
        current: QsQualifiedName option
        signature: ResolvedSignature option
        fileName: string option
        entryPoints: IEnumerable<QsQualifiedName>
    }
    static member public Create(syntaxTree, assemblyConstants) =
        let udts = GlobalTypeResolutions syntaxTree
        let callables = GlobalCallableResolutions syntaxTree
        let positionInfos = DeclarationLocations.Accumulate syntaxTree

        let callablesByName =
            let result = new Dictionary<string, (string * QsCallable) list>()

            syntaxTree
            |> Seq.collect
                (fun ns ->
                    ns.Elements
                    |> Seq.choose
                        (function
                        | QsCallable c -> Some(ns, c)
                        | _ -> None))
            |> Seq.iter
                (fun (ns: QsNamespace, c: QsCallable) ->
                    if result.ContainsKey c.FullName.Name then
                        result.[c.FullName.Name] <- (ns.Name, c) :: (result.[c.FullName.Name])
                    else
                        result.[c.FullName.Name] <- [ ns.Name, c ])

            result.ToImmutableDictionary()

        {
            assemblyConstants = assemblyConstants
            allQsElements = syntaxTree
            byName = callablesByName
            allUdts = udts
            allCallables = callables
            declarationPositions =
                positionInfos.ToImmutableDictionary((fun g -> g.Key), (fun g -> g.ToImmutableSortedSet()))
            current = None
            fileName = None
            signature = None
            entryPoints = ImmutableArray.Empty
        }

    static member public Create(compilation: QsCompilation, assemblyConstants) =
        { CodegenContext.Create(compilation.Namespaces, assemblyConstants) with entryPoints = compilation.EntryPoints }

    static member public Create(compilation: QsCompilation) =
        CodegenContext.Create(compilation, ImmutableDictionary.Empty)

    static member public Create(syntaxTree: ImmutableArray<QsNamespace>) =
        CodegenContext.Create(syntaxTree, ImmutableDictionary.Empty)

    member public this.ProcessorArchitecture =
        match this.assemblyConstants.TryGetValue AssemblyConstants.ProcessorArchitecture with
        | true, name -> name
        | false, _ -> null

    member public this.ExecutionTarget =
        match this.assemblyConstants.TryGetValue AssemblyConstants.ExecutionTarget with
        | true, name -> name
        | false, _ -> null

    member public this.AssemblyName =
        match this.assemblyConstants.TryGetValue AssemblyConstants.AssemblyName with
        | true, name -> name
        | false, _ -> null

    member public this.ExposeReferencesViaTestNames =
        match this.assemblyConstants.TryGetValue AssemblyConstants.ExposeReferencesViaTestNames with
        | true, propVal -> propVal = "true"
        | false, _ -> false

    member internal this.GenerateCodeForSource(fileName: string) =
        let targetsQuantumProcessor =
            match this.assemblyConstants.TryGetValue AssemblyConstants.ProcessorArchitecture with
            | true, target ->
                target = AssemblyConstants.HoneywellProcessor
                || target = AssemblyConstants.IonQProcessor
                || target = AssemblyConstants.QCIProcessor
                || target = "MicrosoftSimulator" // ToDo: We need to have an assembly constant for this.
            | _ -> false

        not (fileName.EndsWith ".dll") || targetsQuantumProcessor
