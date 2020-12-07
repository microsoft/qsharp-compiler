// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open System.IO
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit


type AccessModifierTests () =
    inherit CompilerTests
        (CompilerTests.Compile ("TestCases", ["AccessModifiers.qs"], [File.ReadAllLines("ReferenceTargets.txt").[1]]))

    member private this.Expect name (diagnostics : IEnumerable<DiagnosticItem>) =
        let ns = "Microsoft.Quantum.Testing.AccessModifiers"
        this.VerifyDiagnostics (QsQualifiedName.New (ns, name), diagnostics)

    [<Fact>]
    member this.``Callables`` () =
        this.Expect "CallableUseOK" []
        this.Expect "CallableReferenceInternalInaccessible" [Error ErrorCode.InaccessibleCallable]

    [<Fact>]
    member this.``Types`` () =
        this.Expect "TypeUseOK" []
        this.Expect "TypeReferenceInternalInaccessible" [Error ErrorCode.InaccessibleType]
        this.Expect "TypeConstructorReferenceInternalInaccessible" [Error ErrorCode.InaccessibleCallable]

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
