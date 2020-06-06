// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTree


type BuiltInKind =
    | Attribute
    | Function of TypeParameters : ImmutableArray<NonNullable<string>>
    | Operation of TypeParameters : ImmutableArray<NonNullable<string>> * IsSelfAdjoint : bool


type BuiltIn = {
    /// contains the fully qualified name of the built-in
    FullName : QsQualifiedName
    /// contains the specific kind of built-in this is, as well as information specific to that kind
    Kind : BuiltInKind
}
    with

    static member CoreNamespace = NonNullable<string>.New "Microsoft.Quantum.Core"
    static member CanonNamespace = NonNullable<string>.New "Microsoft.Quantum.Canon"
    static member IntrinsicNamespace = NonNullable<string>.New "Microsoft.Quantum.Intrinsic"
    static member StandardArrayNamespace = NonNullable<string>.New "Microsoft.Quantum.Arrays"
    static member DiagnosticsNamespace = NonNullable<string>.New "Microsoft.Quantum.Diagnostics"
    static member ClassicallyControlledNamespace = NonNullable<string>.New "Microsoft.Quantum.Simulation.QuantumProcessor.Extensions"

    /// Returns the set of namespaces that is automatically opened for each compilation.
    static member NamespacesToAutoOpen = ImmutableHashSet.Create (BuiltIn.CoreNamespace)

    /// Returns the set of callables that rewrite steps take dependencies on.
    /// These should be non-Generic callables only.
    static member RewriteStepDependencies =
        ImmutableHashSet.Create (
            BuiltIn.RangeReverse.FullName,
            BuiltIn.Length.FullName
    )

    /// Returns true if the given attribute marks the corresponding declaration as entry point.
    static member MarksEntryPoint (att : QsDeclarationAttribute) = att.TypeId |> function
        | Value tId -> tId.Namespace.Value = BuiltIn.EntryPoint.FullName.Namespace.Value && tId.Name.Value = BuiltIn.EntryPoint.FullName.Name.Value
        | Null -> false

    /// Returns true if the given attribute marks the corresponding declaration as deprecated.
    static member MarksDeprecation (att : QsDeclarationAttribute) = att.TypeId |> function
        | Value tId -> tId.Namespace.Value = BuiltIn.Deprecated.FullName.Namespace.Value && tId.Name.Value = BuiltIn.Deprecated.FullName.Name.Value
        | Null -> false

    /// Returns true if the given attribute marks the corresponding declaration as unit test.
    static member MarksTestOperation (att : QsDeclarationAttribute) = att.TypeId |> function
        | Value tId -> tId.Namespace.Value = BuiltIn.Test.FullName.Namespace.Value && tId.Name.Value = BuiltIn.Test.FullName.Name.Value
        | Null -> false

    /// Returns true if the given attribute defines an alternative name that may be used when loading a type or callable for testing purposes.
    static member internal DefinesNameForTesting (att : QsDeclarationAttribute) = att.TypeId |> function
        | Value tId -> tId.Namespace.Value = BuiltIn.EnableTestingViaName.FullName.Namespace.Value && tId.Name.Value = BuiltIn.EnableTestingViaName.FullName.Name.Value
        | Null -> false

    /// Returns true if the given attribute indicates that the type or callable has been loaded via an alternative name for testing purposes.
    static member internal DefinesLoadedViaTestNameInsteadOf (att : QsDeclarationAttribute) = att.TypeId |> function
        | Value tId -> tId.Namespace.Value = GeneratedAttributes.Namespace && tId.Name.Value = GeneratedAttributes.LoadedViaTestNameInsteadOf
        | Null -> false


    // dependencies in Microsoft.Quantum.Core

    static member Length = {
        FullName = {Name = "Length" |> NonNullable<string>.New; Namespace = BuiltIn.CoreNamespace}
        Kind = Function (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New))
    }

    static member RangeReverse = {
        FullName = {Name = "RangeReverse" |> NonNullable<string>.New; Namespace = BuiltIn.CoreNamespace}
        Kind = Function (TypeParameters = ImmutableArray.Empty)
    }

    static member Attribute = {
        FullName = {Name = "Attribute" |> NonNullable<string>.New; Namespace = BuiltIn.CoreNamespace}
        Kind = Attribute
    }

    static member EntryPoint = {
        FullName = {Name = "EntryPoint" |> NonNullable<string>.New; Namespace = BuiltIn.CoreNamespace}
        Kind = Attribute
    }

    static member Deprecated = {
        FullName = {Name = "Deprecated" |> NonNullable<string>.New; Namespace = BuiltIn.CoreNamespace}
        Kind = Attribute
    }

    // dependencies in Microsoft.Quantum.Diagnostics

    static member Test = {
        FullName = {Name = "Test" |> NonNullable<string>.New; Namespace = BuiltIn.DiagnosticsNamespace}
        Kind = Attribute
    }

    static member EnableTestingViaName = {
        FullName = {Name = "EnableTestingViaName" |> NonNullable<string>.New; Namespace = BuiltIn.DiagnosticsNamespace}
        Kind = Attribute
    }

    // dependencies in Microsoft.Quantum.Canon

    static member NoOp = {
        FullName = {Name = "NoOp" |> NonNullable<string>.New; Namespace = BuiltIn.CanonNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // dependencies in Microsoft.Quantum.Simulation.QuantumProcessor.Extensions

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit), 'T) , (('U => Unit), 'U)) => Unit)
    static member ApplyConditionally = {
        FullName = {Name = "ApplyConditionally" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit is Adj), 'T) , (('U => Unit is Adj), 'U)) => Unit is Adj)
    static member ApplyConditionallyA = {
        FullName = {Name = "ApplyConditionallyA" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit is Ctl), 'T) , (('U => Unit is Ctl), 'U)) => Unit is Ctl)
    static member ApplyConditionallyC = {
        FullName = {Name = "ApplyConditionallyC" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit is Adj + Ctl), 'T) , (('U => Unit is Adj + Ctl), 'U)) => Unit is Adj + Ctl)
    static member ApplyConditionallyCA = {
        FullName = {Name = "ApplyConditionallyCA" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T>((Result, (('T => Unit), 'T)) => Unit)
    static member ApplyIfZero = {
        FullName = {Name = "ApplyIfZero" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj), 'T)) => Unit is Adj)
    static member ApplyIfZeroA = {
        FullName = {Name = "ApplyIfZeroA" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Ctl), 'T)) => Unit is Ctl)
    static member ApplyIfZeroC = {
        FullName = {Name = "ApplyIfZeroC" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj + Ctl), 'T)) => Unit is Adj + Ctl)
    static member ApplyIfZeroCA = {
        FullName = {Name = "ApplyIfZeroCA" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T>((Result, (('T => Unit), 'T)) => Unit)
    static member ApplyIfOne = {
        FullName = {Name = "ApplyIfOne" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj), 'T)) => Unit is Adj)
    static member ApplyIfOneA = {
        FullName = {Name = "ApplyIfOneA" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Ctl), 'T)) => Unit is Ctl)
    static member ApplyIfOneC = {
        FullName = {Name = "ApplyIfOneC" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj + Ctl), 'T)) => Unit is Adj + Ctl)
    static member ApplyIfOneCA = {
        FullName = {Name = "ApplyIfOneCA" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit), 'T), (('U => Unit), 'U)) => Unit)
    static member ApplyIfElseR = {
        FullName = {Name = "ApplyIfElseR" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Adj), 'T), (('U => Unit is Adj), 'U)) => Unit is Adj)
    static member ApplyIfElseRA = {
        FullName = {Name = "ApplyIfElseRA" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Ctl), 'T), (('U => Unit is Ctl), 'U)) => Unit is Ctl)
    static member ApplyIfElseRC = {
        FullName = {Name = "ApplyIfElseRC" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Adj + Ctl), 'T), (('U => Unit is Adj + Ctl), 'U)) => Unit is Adj + Ctl)
    static member ApplyIfElseRCA = {
        FullName = {Name = "ApplyIfElseRCA" |> NonNullable<string>.New; Namespace = BuiltIn.ClassicallyControlledNamespace}
        Kind = Operation (TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New), IsSelfAdjoint = false)
    }

    // dependencies in other namespaces (e.g. things used for code actions)

    static member IndexRange = {
        FullName = {Name = "IndexRange" |> NonNullable<string>.New; Namespace = BuiltIn.StandardArrayNamespace}
        Kind = Function (TypeParameters = ImmutableArray.Create("TElement" |> NonNullable<string>.New))
    }
