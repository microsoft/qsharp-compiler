﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree


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

    /// Returns the set of namespaces that is automatically opened for each compilation.
    static member NamespacesToAutoOpen = ImmutableHashSet.Create (BuiltIn.CoreNamespace)

    /// Returns all valid targets for executing Q# code.
    static member ValidExecutionTargets = 
        // Note: If this is adapted, then the error message for InvalidExecutionTargetForTest needs to be adapted as well.
        ["QuantumSimulator"; "TraceSimulator"; "ToffoliSimulator"] 
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
        | Value tId -> tId.Namespace.Value = BuiltIn.TestOperation.Namespace.Value && tId.Name.Value = BuiltIn.TestOperation.Name.Value
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

    static member TestOperation = {
        Name = "TestOperation" |> NonNullable<string>.New
        Namespace = BuiltIn.DiagnosticsNamespace
        TypeParameters = ImmutableArray.Empty
    }


    // "weak dependencies" in other namespaces (e.g. things used for code actions)

    static member IndexRange = {
        Name = "IndexRange" |> NonNullable<string>.New
        Namespace = BuiltIn.StandardArrayNamespace
        TypeParameters = ImmutableArray.Empty
    }
