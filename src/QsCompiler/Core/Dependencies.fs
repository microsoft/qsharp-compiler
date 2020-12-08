// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System
open System.Collections.Immutable

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


type BuiltInKind =
    | Attribute
    | Function of TypeParameters: ImmutableArray<string>
    | Operation of TypeParameters: ImmutableArray<string> * IsSelfAdjoint: bool


type BuiltIn =
    {
      /// contains the fully qualified name of the built-in
      FullName: QsQualifiedName
      /// contains the specific kind of built-in this is, as well as information specific to that kind
      Kind: BuiltInKind }

    static member CanonNamespace = "Microsoft.Quantum.Canon"

    static member ClassicallyControlledNamespace =
        "Microsoft.Quantum.Simulation.QuantumProcessor.Extensions"

    static member CoreNamespace = "Microsoft.Quantum.Core"
    static member DiagnosticsNamespace = "Microsoft.Quantum.Diagnostics"
    static member IntrinsicNamespace = "Microsoft.Quantum.Intrinsic"
    static member StandardArrayNamespace = "Microsoft.Quantum.Arrays"
    static member TargetingNamespace = "Microsoft.Quantum.Targeting"

    /// Returns the set of namespaces that is automatically opened for each compilation.
    static member NamespacesToAutoOpen =
        ImmutableHashSet.Create(BuiltIn.CoreNamespace)

    /// Returns the set of callables that rewrite steps take dependencies on.
    /// These should be non-Generic callables only.
    static member RewriteStepDependencies =
        ImmutableHashSet.Create(BuiltIn.RangeReverse.FullName, BuiltIn.Length.FullName)

    /// Returns true if the given attribute marks the corresponding declaration as entry point.
    static member MarksEntryPoint(att: QsDeclarationAttribute) =
        att.TypeId
        |> function
        | Value tId ->
            tId.Namespace = BuiltIn.EntryPoint.FullName.Namespace && tId.Name = BuiltIn.EntryPoint.FullName.Name
        | Null -> false

    /// Returns true if the given attribute marks the corresponding declaration as deprecated.
    static member MarksDeprecation(att: QsDeclarationAttribute) =
        att.TypeId
        |> function
        | Value tId ->
            tId.Namespace = BuiltIn.Deprecated.FullName.Namespace && tId.Name = BuiltIn.Deprecated.FullName.Name
        | Null -> false

    /// Returns true if the given attribute marks the corresponding declaration as unit test.
    static member MarksTestOperation(att: QsDeclarationAttribute) =
        att.TypeId
        |> function
        | Value tId -> tId.Namespace = BuiltIn.Test.FullName.Namespace && tId.Name = BuiltIn.Test.FullName.Name
        | Null -> false

    /// Returns true if the given attribute defines an alternative name that may be used when loading a type or callable for testing purposes.
    static member internal DefinesNameForTesting(att: QsDeclarationAttribute) =
        att.TypeId
        |> function
        | Value tId ->
            tId.Namespace = BuiltIn.EnableTestingViaName.FullName.Namespace
            && tId.Name = BuiltIn.EnableTestingViaName.FullName.Name
        | Null -> false

    /// Returns true if the given attribute indicates that the type or callable has been loaded via an alternative name for testing purposes.
    static member internal DefinesLoadedViaTestNameInsteadOf(att: QsDeclarationAttribute) =
        att.TypeId
        |> function
        | Value tId ->
            tId.Namespace = GeneratedAttributes.Namespace
            && tId.Name = GeneratedAttributes.LoadedViaTestNameInsteadOf
        | Null -> false

    /// Returns the required runtime capability if the sequence of attributes contains at least one valid instance of
    /// the RequiresCapability attribute.
    static member TryGetRequiredCapability attributes =
        let isCapability udt =
            BuiltIn.RequiresCapability.FullName = { Namespace = udt.Namespace
                                                    Name = udt.Name }

        let extractString =
            function
            | StringLiteral (str, _) -> Value str
            | _ -> Null

        let capability attribute =
            match attribute.TypeId, attribute.Argument.Expression with
            | Value udt, ValueTuple items when isCapability udt && not items.IsEmpty ->
                items.[0].Expression |> extractString |> QsNullable<_>.Bind RuntimeCapability.TryParse
            | _ -> Null

        let capabilities =
            attributes |> QsNullable<_>.Choose capability

        if Seq.isEmpty capabilities
        then Null
        else capabilities |> Seq.reduce RuntimeCapability.Combine |> Value

    // dependencies in Microsoft.Quantum.Core

    static member Length =
        { FullName =
              { Name = "Length"
                Namespace = BuiltIn.CoreNamespace }
          Kind = Function(TypeParameters = ImmutableArray.Create "T") }

    static member RangeReverse =
        { FullName =
              { Name = "RangeReverse"
                Namespace = BuiltIn.CoreNamespace }
          Kind = Function(TypeParameters = ImmutableArray.Empty) }

    static member Attribute =
        { FullName =
              { Name = "Attribute"
                Namespace = BuiltIn.CoreNamespace }
          Kind = Attribute }

    static member EntryPoint =
        { FullName =
              { Name = "EntryPoint"
                Namespace = BuiltIn.CoreNamespace }
          Kind = Attribute }

    static member Deprecated =
        { FullName =
              { Name = "Deprecated"
                Namespace = BuiltIn.CoreNamespace }
          Kind = Attribute }

    static member RequiresCapability =
        { FullName =
              { Name = "RequiresCapability"
                Namespace = BuiltIn.TargetingNamespace }
          Kind = Attribute }

    // dependencies in Microsoft.Quantum.Diagnostics

    static member Test =
        { FullName =
              { Name = "Test"
                Namespace = BuiltIn.DiagnosticsNamespace }
          Kind = Attribute }

    static member EnableTestingViaName =
        { FullName =
              { Name = "EnableTestingViaName"
                Namespace = BuiltIn.DiagnosticsNamespace }
          Kind = Attribute }

    // dependencies in Microsoft.Quantum.Canon

    static member NoOp =
        { FullName =
              { Name = "NoOp"
                Namespace = BuiltIn.CanonNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false) }

    // dependencies in Microsoft.Quantum.Simulation.QuantumProcessor.Extensions

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit), 'T) , (('U => Unit), 'U)) => Unit)
    static member ApplyConditionally =
        { FullName =
              { Name = "ApplyConditionally"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false) }

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit is Adj), 'T) , (('U => Unit is Adj), 'U)) => Unit is Adj)
    static member ApplyConditionallyA =
        { FullName =
              { Name = "ApplyConditionallyA"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false) }

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit is Ctl), 'T) , (('U => Unit is Ctl), 'U)) => Unit is Ctl)
    static member ApplyConditionallyC =
        { FullName =
              { Name = "ApplyConditionallyC"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false) }

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit is Adj + Ctl), 'T) , (('U => Unit is Adj + Ctl), 'U)) => Unit is Adj + Ctl)
    static member ApplyConditionallyCA =
        { FullName =
              { Name = "ApplyConditionallyCA"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false) }

    // This is expected to have type <'T>((Result, (('T => Unit), 'T)) => Unit)
    static member ApplyIfZero =
        { FullName =
              { Name = "ApplyIfZero"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false) }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj), 'T)) => Unit is Adj)
    static member ApplyIfZeroA =
        { FullName =
              { Name = "ApplyIfZeroA"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false) }

    // This is expected to have type <'T>((Result, (('T => Unit is Ctl), 'T)) => Unit is Ctl)
    static member ApplyIfZeroC =
        { FullName =
              { Name = "ApplyIfZeroC"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false) }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj + Ctl), 'T)) => Unit is Adj + Ctl)
    static member ApplyIfZeroCA =
        { FullName =
              { Name = "ApplyIfZeroCA"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false) }

    // This is expected to have type <'T>((Result, (('T => Unit), 'T)) => Unit)
    static member ApplyIfOne =
        { FullName =
              { Name = "ApplyIfOne"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false) }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj), 'T)) => Unit is Adj)
    static member ApplyIfOneA =
        { FullName =
              { Name = "ApplyIfOneA"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false) }

    // This is expected to have type <'T>((Result, (('T => Unit is Ctl), 'T)) => Unit is Ctl)
    static member ApplyIfOneC =
        { FullName =
              { Name = "ApplyIfOneC"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false) }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj + Ctl), 'T)) => Unit is Adj + Ctl)
    static member ApplyIfOneCA =
        { FullName =
              { Name = "ApplyIfOneCA"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false) }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit), 'T), (('U => Unit), 'U)) => Unit)
    static member ApplyIfElseR =
        { FullName =
              { Name = "ApplyIfElseR"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false) }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Adj), 'T), (('U => Unit is Adj), 'U)) => Unit is Adj)
    static member ApplyIfElseRA =
        { FullName =
              { Name = "ApplyIfElseRA"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false) }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Ctl), 'T), (('U => Unit is Ctl), 'U)) => Unit is Ctl)
    static member ApplyIfElseRC =
        { FullName =
              { Name = "ApplyIfElseRC"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false) }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Adj + Ctl), 'T), (('U => Unit is Adj + Ctl), 'U)) => Unit is Adj + Ctl)
    static member ApplyIfElseRCA =
        { FullName =
              { Name = "ApplyIfElseRCA"
                Namespace = BuiltIn.ClassicallyControlledNamespace }
          Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false) }

    // dependencies in other namespaces (e.g. things used for code actions)

    static member IndexRange =
        { FullName =
              { Name = "IndexRange"
                Namespace = BuiltIn.StandardArrayNamespace }
          Kind = Function(TypeParameters = ImmutableArray.Create "TElement") }
