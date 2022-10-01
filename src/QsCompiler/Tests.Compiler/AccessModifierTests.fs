// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.AccessModifierTests

open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open System.IO
open Xunit

let private diagnostics =
    TestUtils.buildFiles
        "TestCases"
        [ "AccessModifiers.qs" ]
        [ File.ReadAllLines("ReferenceTargets.txt")[2] ]
        None
        TestUtils.Library
    |> Diagnostics.byDeclaration

let private assertDiagnostics name expected =
    diagnostics[QsQualifiedName.New("Microsoft.Quantum.Testing.AccessModifiers", name)]
    |> Diagnostics.assertMatches expected

[<Fact>]
let callables () =
    assertDiagnostics "CallableUseOK" []
    assertDiagnostics "CallableReferenceInternalInaccessible" [ Error ErrorCode.InaccessibleCallable, None ]

[<Fact>]
let types () =
    assertDiagnostics "TypeUseOK" (Seq.replicate 3 (Warning WarningCode.DeprecatedNewArray, None))

    assertDiagnostics
        "TypeReferenceInternalInaccessible"
        [
            Error ErrorCode.InaccessibleType, None
            Warning WarningCode.DeprecatedNewArray, None
        ]

    assertDiagnostics "TypeConstructorReferenceInternalInaccessible" [ Error ErrorCode.InaccessibleCallable, None ]

[<Fact>]
let callableSignatures () =
    assertDiagnostics "CallableLeaksInternalTypeIn1" [ Error ErrorCode.TypeLessAccessibleThanParentCallable, None ]
    assertDiagnostics "CallableLeaksInternalTypeIn2" [ Error ErrorCode.TypeLessAccessibleThanParentCallable, None ]
    assertDiagnostics "CallableLeaksInternalTypeIn3" [ Error ErrorCode.TypeLessAccessibleThanParentCallable, None ]
    assertDiagnostics "CallableLeaksInternalTypeOut1" [ Error ErrorCode.TypeLessAccessibleThanParentCallable, None ]
    assertDiagnostics "CallableLeaksInternalTypeOut2" [ Error ErrorCode.TypeLessAccessibleThanParentCallable, None ]
    assertDiagnostics "CallableLeaksInternalTypeOut3" [ Error ErrorCode.TypeLessAccessibleThanParentCallable, None ]
    assertDiagnostics "InternalCallableInternalTypeOK" []

[<Fact>]
let underlyingTypes =
    assertDiagnostics "PublicTypeLeaksInternalType1" [ Error ErrorCode.TypeLessAccessibleThanParentType, None ]
    assertDiagnostics "PublicTypeLeaksInternalType2" [ Error ErrorCode.TypeLessAccessibleThanParentType, None ]
    assertDiagnostics "PublicTypeLeaksInternalType3" [ Error ErrorCode.TypeLessAccessibleThanParentType, None ]
    assertDiagnostics "InternalTypeInternalTypeOK" []
