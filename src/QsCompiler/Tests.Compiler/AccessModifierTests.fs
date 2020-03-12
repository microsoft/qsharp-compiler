﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit
open System.IO
open System.Linq


type AccessModifierTests (output) =
    inherit CompilerTests (CompilerTests.Compile "TestCases"
                                                 ["AccessModifiers.qs"]
                                                 [File.ReadAllLines("ReferenceTargets.txt").ElementAt(1)],
                           output)

    member private this.Expect name (diagnostics : IEnumerable<DiagnosticItem>) =
        let ns = "Microsoft.Quantum.Testing.AccessModifiers" |> NonNullable<_>.New
        let name = name |> NonNullable<_>.New
        this.Verify (QsQualifiedName.New (ns, name), diagnostics)

    // Note: Since internal declarations in references are renamed, the error codes will be for unidentified callables
    // and types instead of inaccessible ones.

    [<Fact>]
    member this.``Callables`` () =
        this.Expect "CallableUseOK" []
        this.Expect "CallableReferenceInternalInaccessible" [Error ErrorCode.UnknownIdentifier]

    [<Fact>]
    member this.``Types`` () =
        this.Expect "TypeUseOK" []
        this.Expect "TypeReferenceInternalInaccessible" [Error ErrorCode.UnknownType]
        this.Expect "TypeConstructorReferenceInternalInaccessible" [Error ErrorCode.UnknownIdentifier]

    [<Fact>]
    member this.``Callable signatures`` () =
        this.Expect "CallableLeaksInternalTypeIn1" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "CallableLeaksInternalTypeIn2" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "CallableLeaksInternalTypeIn3" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "CallableLeaksInternalTypeOut1" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "CallableLeaksInternalTypeOut2" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "CallableLeaksInternalTypeOut3" [Error ErrorCode.TypeLessAccessibleThanParentCallable]
        this.Expect "InternalCallableInternalTypeOK" []

    [<Fact>]
    member this.``Underlying types`` () =
        this.Expect "PublicTypeLeaksInternalType1" [Error ErrorCode.TypeLessAccessibleThanParentType]
        this.Expect "PublicTypeLeaksInternalType2" [Error ErrorCode.TypeLessAccessibleThanParentType]
        this.Expect "PublicTypeLeaksInternalType3" [Error ErrorCode.TypeLessAccessibleThanParentType]
        this.Expect "InternalTypeInternalTypeOK" []
