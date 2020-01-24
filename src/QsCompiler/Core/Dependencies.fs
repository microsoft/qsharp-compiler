// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.SyntaxTokens


type BuiltIn = {
    /// contains the name of the callable
    Name : NonNullable<string>
    /// contains the name of the namespace in which the callable is defined
    Namespace : NonNullable<string>
    /// contains the names of the type parameters without the leading tick (')
    TypeParameters : ImmutableArray<NonNullable<string>>
}
    with 
    static member CoreNamespace = NonNullable<string>.New "Microsoft.Quantum.Core"
    static member IntrinsicNamespace = NonNullable<string>.New "Microsoft.Quantum.Intrinsic"
    static member StandardArrayNamespace = NonNullable<string>.New "Microsoft.Quantum.Arrays"
    static member DiagnosticsNamespace = NonNullable<string>.New "Microsoft.Quantum.Diagnostics"
    static member ClassicallyControlledNamespace = NonNullable<string>.New "Microsoft.Quantum.ClassicallyControlled"

    /// Returns the set of namespaces that is automatically opened for each compilation.
    static member NamespacesToAutoOpen = ImmutableHashSet.Create (BuiltIn.CoreNamespace)

    /// Returns all valid targets for executing Q# code.
    static member ValidExecutionTargets = 
        // Note: If this is adapted, then the error message for InvalidExecutionTargetForTest needs to be adapted as well.
        ["QuantumSimulator"; "ToffoliSimulator"; "ResourcesEstimator"] 
        |> ImmutableHashSet.CreateRange

    /// Returns true if the given attribute marks the corresponding declaration as entry point. 
    static member MarksEntryPoint (att : QsDeclarationAttribute) = att.TypeId |> function 
        | Value tId -> tId.Namespace.Value = BuiltIn.EntryPoint.Namespace.Value && tId.Name.Value = BuiltIn.EntryPoint.Name.Value
        | Null -> false

    /// Returns true if the given attribute marks the corresponding declaration as deprecated. 
    static member MarksDeprecation (att : QsDeclarationAttribute) = att.TypeId |> function 
        | Value tId -> tId.Namespace.Value = BuiltIn.Deprecated.Namespace.Value && tId.Name.Value = BuiltIn.Deprecated.Name.Value
        | Null -> false

    /// Returns true if the given attribute marks the corresponding declaration as unit test. 
    static member MarksTestOperation (att : QsDeclarationAttribute) = att.TypeId |> function 
        | Value tId -> tId.Namespace.Value = BuiltIn.Test.Namespace.Value && tId.Name.Value = BuiltIn.Test.Name.Value
        | Null -> false


    // hard dependencies in Microsoft.Quantum.Core

    static member Length = {
        Name = "Length" |> NonNullable<string>.New
        Namespace = BuiltIn.CoreNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    static member RangeReverse = {
        Name = "RangeReverse" |> NonNullable<string>.New
        Namespace = BuiltIn.CoreNamespace
        TypeParameters = ImmutableArray.Empty    
    }

    static member Attribute = {
        Name = "Attribute" |> NonNullable<string>.New
        Namespace = BuiltIn.CoreNamespace
        TypeParameters = ImmutableArray.Empty
    }

    static member EntryPoint = {
        Name = "EntryPoint" |> NonNullable<string>.New
        Namespace = BuiltIn.CoreNamespace
        TypeParameters = ImmutableArray.Empty
    }

    static member Deprecated = {
        Name = "Deprecated" |> NonNullable<string>.New
        Namespace = BuiltIn.CoreNamespace
        TypeParameters = ImmutableArray.Empty
    }

    static member Test = {
        Name = "Test" |> NonNullable<string>.New
        Namespace = BuiltIn.DiagnosticsNamespace
        TypeParameters = ImmutableArray.Empty
    }

    // This is expected to have type <'T>((Result, (('T => Unit), 'T)) => Unit)
    static member ApplyIfZero = {
        Name = "ApplyIfZero" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj), 'T)) => Unit is Adj)
    static member ApplyIfZeroA = {
        Name = "ApplyIfZeroA" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Ctl), 'T)) => Unit is Ctl)
    static member ApplyIfZeroC = {
        Name = "ApplyIfZeroC" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj + Ctl), 'T)) => Unit is Adj + Ctl)
    static member ApplyIfZeroCA = {
        Name = "ApplyIfZeroCA" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T>((Result, (('T => Unit), 'T)) => Unit)
    static member ApplyIfOne = {
        Name = "ApplyIfOne" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj), 'T)) => Unit is Adj)
    static member ApplyIfOneA = {
        Name = "ApplyIfOneA" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Ctl), 'T)) => Unit is Ctl)
    static member ApplyIfOneC = {
        Name = "ApplyIfOneC" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj + Ctl), 'T)) => Unit is Adj + Ctl)
    static member ApplyIfOneCA = {
        Name = "ApplyIfOneCA" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit), 'T), (('U => Unit), 'U)) => Unit)
    static member ApplyIfElseR = {
        Name = "ApplyIfElseR" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Adj), 'T), (('U => Unit is Adj), 'U)) => Unit is Adj)
    static member ApplyIfElseRA = {
        Name = "ApplyIfElseRA" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Ctl), 'T), (('U => Unit is Ctl), 'U)) => Unit is Ctl)
    static member ApplyIfElseRC = {
        Name = "ApplyIfElseRC" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New)
    }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Adj + Ctl), 'T), (('U => Unit is Adj + Ctl), 'U)) => Unit is Adj + Ctl)
    static member ApplyIfElseCA = {
        Name = "ApplyIfElseCA" |> NonNullable<string>.New
        Namespace = BuiltIn.ClassicallyControlledNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New, "U" |> NonNullable<string>.New)
    }

    // "weak dependencies" in other namespaces (e.g. things used for code actions)

    static member IndexRange = {
        Name = "IndexRange" |> NonNullable<string>.New
        Namespace = BuiltIn.StandardArrayNamespace
        TypeParameters = ImmutableArray.Empty
    }
