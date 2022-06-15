namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Use this module to specify the syntax for a <code>namespace</code>
/// </summary>
[<AutoOpen>]
module NamespaceDeclaration =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax


    let private setUsings usings (nd: NamespaceDeclarationSyntax) =
        usings |> SyntaxFactory.List |> nd.WithUsings

    let private setMembers members (nd: NamespaceDeclarationSyntax) =
        members |> (Seq.toArray >> SyntaxFactory.List) |> nd.WithMembers

    /// This function creates a 'namespace' with the given name and contents:
    ///
    /// ##### Parameters
    /// 1. **namespaceName** : `string` : The name of the namespace to be created
    /// 1. ``{`` : white noise : ignored - use the ``{`` constant for visual structure
    /// 1. **usings** : `string seq` : The namespaces to reference within this namespace
    /// 1. **members** : `MemberDeclaration seq` : the members of this namespace. Typically `class` and `interface`
    /// 1. ``}`` : white noise : ignored - use the ``}`` constant for visual structure
    ///
    /// ##### Returns
    /// A `namespace` object
    ///
    /// ##### Usage
    ///  ```
    ///      ``namespace`` "Foo"
    ///          ``{``
    ///              [ ``using`` "System" ]
    ///              [ c ]
    ///          ``}``
    ///  ```
    ///  will result in a namespace definition which will generate code similar to
    ///  ```
    ///      namespace Foo
    ///      {
    ///          using System;
    ///
    ///          class C {...}
    ///      }
    ///  ```
    ///
    /// ##### Notes
    /// * The `System` namespace is always included by default
    /// * You may pass a sequence of these namespaces to ``compilation unit`` and generate code from it
    ///
    let ``namespace`` namespaceName ``{`` usings members ``}`` =
        namespaceName
        |> (ident >> SyntaxFactory.NamespaceDeclaration)
        |> setUsings usings
        |> setMembers members


    let ``using`` (typeName: string) =
        typeName |> (ident >> SyntaxFactory.UsingDirective)

    let ``alias`` (aliasName: string) (typeName: string) =
        SyntaxFactory.UsingDirective(SyntaxFactory.NameEquals(aliasName), SyntaxFactory.IdentifierName(typeName))
