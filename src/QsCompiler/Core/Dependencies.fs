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

    /// Returns the set of namespaces that is automatically opened for each compilation.
    static member NamespacesToAutoOpen = ImmutableHashSet.Create (BuiltIn.CoreNamespace)

    /// Returns true if any of the given attributes indicates an entry point.
    static member IndicateEntryPoint attributes = 
        attributes |> Seq.exists (fun att -> att.TypeId |> function 
        | Value tId -> tId.Namespace.Value = BuiltIn.EntryPoint.Namespace.Value && tId.Name.Value = BuiltIn.EntryPoint.Name.Value
        | Null -> false)

    /// Generates a suitable deprecation warning at the given range for the type or callable with the given name based on the given attributes.
    /// If several attributes indicate deprecation, a redirection is suggested based on the first deprecation attribute. 
    static member IndicateDeprecation (fullName : QsQualifiedName, range) attributes = 
        let asDeprecatedAttribute (att : QsDeclarationAttribute) = 
            let rec unwrap = function  
                | ValueTuple vs when vs.Length = 1 -> vs.[0].Expression |> unwrap
                | argEx -> argEx
            match att.TypeId, unwrap att.Argument.Expression with 
            | Value tId, StringLiteral (str, _) when 
                tId.Namespace.Value = BuiltIn.Deprecated.Namespace.Value && tId.Name.Value = BuiltIn.Deprecated.Name.Value -> Some str.Value
            | _ -> None
        let usedName = sprintf "%s.%s" fullName.Namespace.Value fullName.Name.Value
        match attributes |> Seq.choose asDeprecatedAttribute |> Seq.tryHead with
        | Some redirect when String.IsNullOrWhiteSpace redirect -> [| range |> QsCompilerDiagnostic.Warning (WarningCode.DeprecationWithoutRedirect, [usedName]) |]
        | Some redirect -> [| range |> QsCompilerDiagnostic.Warning (WarningCode.DeprecationWithRedirect, [usedName; redirect]) |]
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
