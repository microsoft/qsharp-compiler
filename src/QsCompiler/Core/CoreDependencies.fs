// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes


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

    static member NamespacesToAutoOpen = ImmutableHashSet.Create (BuiltIn.CoreNamespace)


    static member Length = {
        Name = "Length" |> NonNullable<string>.New
        Namespace = BuiltIn.CoreNamespace
        TypeParameters = ImmutableArray.Create("T" |> NonNullable<string>.New)
    }

    static member IndexRange = {
        Name = "IndexRange" |> NonNullable<string>.New
        Namespace = BuiltIn.StandardArrayNamespace
        TypeParameters = ImmutableArray.Empty
    }

    static member Attribute = {
        Name = "Attribute" |> NonNullable<string>.New
        Namespace = BuiltIn.CoreNamespace
        TypeParameters = ImmutableArray.Empty
    }

