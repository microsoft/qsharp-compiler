// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit


type AccessModifierTests (output) =
    inherit CompilerTests (CompilerTests.Compile "TestCases" ["AccessModifiers.qs"], output)

    member private this.Expect name (diagnostics : IEnumerable<DiagnosticItem>) = 
        let ns = "Microsoft.Quantum.Testing.TypeChecking" |> NonNullable<_>.New
        let name = name |> NonNullable<_>.New
        this.Verify (QsQualifiedName.New (ns, name), diagnostics)

    [<Fact>]
    member this.``Callables with access modifiers`` () =
        this.Expect "CallableUseOK" []
        this.Expect "CallableUnqualifiedUsePrivateInaccessible" [Error ErrorCode.InaccessibleCallable]
        this.Expect "CallableQualifiedUsePrivateInaccessible" [Error ErrorCode.InaccessibleCallableInNamespace]

    [<Fact>]
    member this.``Types with access modifiers`` () =
        this.Expect "TypeUseOK" []
        this.Expect "TypeUnqualifiedUsePrivateInaccessible" [Error ErrorCode.InaccessibleType]
        this.Expect "TypeQualifiedUsePrivateInaccessible" [Error ErrorCode.InaccessibleTypeInNamespace]

    [<Fact>]
    member this.``Callable signatures`` () =
        this.Expect "PublicCallableLeaksPrivateTypeIn" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "PublicCallableLeaksPrivateTypeOut" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "InternalCallableLeaksPrivateTypeIn" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "InternalCallableLeaksPrivateTypeOut" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "CallablePrivateTypeOK" []
        this.Expect "CallableLeaksInternalTypeIn" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "CallableLeaksInternalTypeOut" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "InternalCallableInternalTypeOK" []
        this.Expect "PrivateCallableInternalTypeOK" []

    [<Fact>]
    member this.``Underlying types`` () =
        this.Expect "PublicTypeLeaksPrivateType" [Error ErrorCode.TypeLessAccessibleThanParentType]
        this.Expect "InternalTypeLeaksPrivateType" [Error ErrorCode.TypeLessAccessibleThanParentType]
        this.Expect "PrivateTypePrivateTypeOK" []
        this.Expect "PublicTypeLeaksInternalType" [Error ErrorCode.TypeLessAccessibleThanParentType]
        this.Expect "InternalTypeInternalTypeOK" []
        this.Expect "PrivateTypeInternalTypeOK" []