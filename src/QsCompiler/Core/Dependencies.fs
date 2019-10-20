// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTokens
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

    /// returns the set of namespaces that is automatically opened for each compilation
    static member NamespacesToAutoOpen = ImmutableHashSet.Create (BuiltIn.CoreNamespace)

    /// returns true if the given attribute is an entry point attribute
    static member internal IsEntryPointAttribute (att : QsDeclarationAttribute) = att.TypeId |> function 
        | Value tId -> tId.Namespace.Value = BuiltIn.EntryPoint.Namespace.Value && tId.Name.Value = BuiltIn.EntryPoint.Name.Value
        | Null -> false

    static member CheckForDeprecation (fullName : QsQualifiedName, range) (attributes : ImmutableArray<QsDeclarationAttribute>) = 
        let asDeprecatedAttribute (att : QsDeclarationAttribute) = 
            match att.TypeId, att.Argument.Expression with 
            | Value tId, StringLiteral (str, args) when // FIXME: ERROR MESSAGE IF THERE IS NO SUBSTITUTE GIVEN...
                tId.Namespace.Value = BuiltIn.Deprecated.Namespace.Value && tId.Name.Value = BuiltIn.Deprecated.Name.Value &&
                args.Length = 0 && not (String.IsNullOrWhiteSpace str.Value) -> Some str.Value
            | _ -> None
        let deprecatedWarning args = [| range |> QsCompilerDiagnostic.Warning (WarningCode.UseOfDeprecatedCallableOrType, args) |]
        match attributes |> Seq.choose asDeprecatedAttribute |> Seq.tryHead with
        | Some redirect -> deprecatedWarning [sprintf "%s.%s" fullName.Namespace.Value fullName.Name.Value; redirect] 
        | None -> [| |]


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


    // "weak dependencies" in other namespaces (e.g. things used for code actions)

    static member IndexRange = {
        Name = "IndexRange" |> NonNullable<string>.New
        Namespace = BuiltIn.StandardArrayNamespace
        TypeParameters = ImmutableArray.Empty
    }
