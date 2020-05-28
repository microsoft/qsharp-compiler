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
open Xunit.Abstractions


type LocalVerificationTests (output:ITestOutputHelper) =
    inherit CompilerTests(
        CompilerTests.Compile "TestCases" [
            "General.qs"; "LocalVerification.qs"; "Types.qs"; 
            Path.Combine ("LinkingTests", "Core.qs");
        ] [], output)

    member private this.Expect name (diag : IEnumerable<DiagnosticItem>) = 
        let ns = "Microsoft.Quantum.Testing.LocalVerification" |> NonNullable<_>.New
        let name = name |> NonNullable<_>.New
        this.Verify (QsQualifiedName.New (ns, name), diag)

    
    [<Fact>]
    member this.``Variable declarations`` () = 
        this.Expect "VariableDeclaration1"  []
        this.Expect "VariableDeclaration2"  []
        this.Expect "VariableDeclaration3"  []
        this.Expect "VariableDeclaration4"  []
        this.Expect "VariableDeclaration5"  []
        this.Expect "VariableDeclaration6"  []
        this.Expect "VariableDeclaration7"  []
        this.Expect "VariableDeclaration8"  []
        this.Expect "VariableDeclaration9"  [Error ErrorCode.SymbolTupleShapeMismatch]
        this.Expect "VariableDeclaration10" [Error ErrorCode.SymbolTupleShapeMismatch]


    [<Fact>]
    member this.``Copy-and-update arrays`` () = 
        this.Expect "CopyAndUpdateArray1"  []
        this.Expect "CopyAndUpdateArray2"  []
        this.Expect "CopyAndUpdateArray3"  []
        this.Expect "CopyAndUpdateArray4"  []
        this.Expect "CopyAndUpdateArray5"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "CopyAndUpdateArray6"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "CopyAndUpdateArray7"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "CopyAndUpdateArray8"  [Error ErrorCode.ConstrainsTypeParameter]
        this.Expect "CopyAndUpdateArray9"  []
        this.Expect "CopyAndUpdateArray10" []
        this.Expect "CopyAndUpdateArray11" []
        this.Expect "CopyAndUpdateArray12" []
        this.Expect "CopyAndUpdateArray13" []
        this.Expect "CopyAndUpdateArray14" []
        this.Expect "CopyAndUpdateArray15" [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "CopyAndUpdateArray16" [Error ErrorCode.ConstrainsTypeParameter]


    [<Fact>]
    member this.``Update-and-reassign arrays`` () = 
        this.Expect "UpdateAndReassign1"  []
        this.Expect "UpdateAndReassign2"  []
        this.Expect "UpdateAndReassign3"  []
        this.Expect "UpdateAndReassign4"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "UpdateAndReassign5"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "UpdateAndReassign6"  []
        this.Expect "UpdateAndReassign7"  []
        this.Expect "UpdateAndReassign8"  []
        this.Expect "UpdateAndReassign9"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "UpdateAndReassign10" [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]


    [<Fact>]
    member this.``Apply-and-reassign`` () = 
        this.Expect "ApplyAndReassign1"   []
        this.Expect "ApplyAndReassign2"   []
        this.Expect "ApplyAndReassign3"   []
        this.Expect "ApplyAndReassign4"   []
        this.Expect "ApplyAndReassign5"   []
        this.Expect "ApplyAndReassign6"   [Error ErrorCode.ExpectingBoolExpr]
        this.Expect "ApplyAndReassign7"   [Error ErrorCode.ArgumentMismatchInBinaryOp; Error ErrorCode.ArgumentMismatchInBinaryOp]
        this.Expect "ApplyAndReassign8"   [Error ErrorCode.UpdateOfImmutableIdentifier]
        this.Expect "ApplyAndReassign9"   [Error ErrorCode.UpdateOfArrayItemExpr]
        this.Expect "ApplyAndReassign10"  [Error ErrorCode.UpdateOfArrayItemExpr]


    [<Fact>]
    member this.``Named type item access`` () =
        this.Expect "ItemAccess1"  [Error ErrorCode.ExpectingUserDefinedType]
        this.Expect "ItemAccess2"  [Error ErrorCode.UnknownItemName]
        this.Expect "ItemAccess3"  []
        this.Expect "ItemAccess4"  []
        this.Expect "ItemAccess5"  [Error ErrorCode.ArgumentMismatchInBinaryOp; Error ErrorCode.ArgumentMismatchInBinaryOp]
        this.Expect "ItemAccess6"  []
        this.Expect "ItemAccess7"  []
        this.Expect "ItemAccess8"  []
        this.Expect "ItemAccess9"  []
        this.Expect "ItemAccess10" []
        this.Expect "ItemAccess11" []
        this.Expect "ItemAccess12" [Error ErrorCode.OperationCallOutsideOfOperation; Error ErrorCode.OperationCallOutsideOfOperation]
        this.Expect "ItemAccess13" [Error ErrorCode.MissingFunctorForAutoGeneration; Error ErrorCode.MissingFunctorForAutoGeneration]
        this.Expect "ItemAccess14" []
        this.Expect "ItemAccess15" []
        this.Expect "ItemAccess16" []
        this.Expect "ItemAccess17" []
        this.Expect "ItemAccess18" []
        this.Expect "ItemAccess19" []
        this.Expect "ItemAccess20" []


    [<Fact>]
    member this.``Named type item update`` () =
        this.Expect "ItemUpdate1"  []
        this.Expect "ItemUpdate2"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "ItemUpdate3"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "ItemUpdate4"  []
        this.Expect "ItemUpdate5"  [Error ErrorCode.UpdateOfImmutableIdentifier]
        this.Expect "ItemUpdate6"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "ItemUpdate7"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "ItemUpdate8"  [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr; Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "ItemUpdate9"  []
        this.Expect "ItemUpdate10" [Error ErrorCode.InvalidIdentifierExprInUpdate; Error ErrorCode.ExcessContinuation]
        this.Expect "ItemUpdate11" [Error ErrorCode.UpdateOfArrayItemExpr]
        this.Expect "ItemUpdate12" [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr; Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "ItemUpdate13" []
        this.Expect "ItemUpdate14" [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "ItemUpdate15" [Error ErrorCode.TypeMismatchInCopyAndUpdateExpr]
        this.Expect "ItemUpdate16" [Error ErrorCode.MissingFunctorForAutoGeneration; Error ErrorCode.MissingFunctorForAutoGeneration]
        this.Expect "ItemUpdate17" [Error ErrorCode.MissingFunctorForAutoGeneration; Error ErrorCode.ValueUpdateWithinAutoInversion]
        this.Expect "ItemUpdate18" [Error ErrorCode.MissingFunctorForAutoGeneration]
        this.Expect "ItemUpdate19" [Error ErrorCode.MissingFunctorForAutoGeneration]
        this.Expect "ItemUpdate20" []
        this.Expect "ItemUpdate21" []
        this.Expect "ItemUpdate22" []


    [<Fact>]
    member this.``Open-ended ranges`` () = 
        this.Expect "ValidArraySlice1" []
        this.Expect "ValidArraySlice2" []
        this.Expect "ValidArraySlice3" []
        this.Expect "ValidArraySlice4" []
        this.Expect "ValidArraySlice5" []
        this.Expect "ValidArraySlice6" []
        this.Expect "ValidArraySlice7" []
        this.Expect "ValidArraySlice8" []
        this.Expect "ValidArraySlice9" []

        this.Expect "InvalidArraySlice1" [Error ErrorCode.ItemAccessForNonArray]
        this.Expect "InvalidArraySlice2" [Error ErrorCode.ItemAccessForNonArray]
        this.Expect "InvalidArraySlice3" [Error ErrorCode.ItemAccessForNonArray]
        this.Expect "InvalidArraySlice4" [Error ErrorCode.ItemAccessForNonArray]
        this.Expect "InvalidArraySlice5" [Error ErrorCode.ItemAccessForNonArray]
        this.Expect "InvalidArraySlice6" [Error ErrorCode.ItemAccessForNonArray]
        this.Expect "InvalidArraySlice7" [Error ErrorCode.ItemAccessForNonArray]
        this.Expect "InvalidArraySlice8" [Error ErrorCode.ItemAccessForNonArray]
        this.Expect "InvalidArraySlice9" [Error ErrorCode.ItemAccessForNonArray]


    [<Fact>]
    member this.``Conjugation verification`` () =
        this.Expect "ValidConjugation1"   []
        this.Expect "ValidConjugation2"   []
        this.Expect "ValidConjugation3"   []
        this.Expect "ValidConjugation4"   []
        this.Expect "ValidConjugation5"   []
        this.Expect "ValidConjugation6"   []
        this.Expect "ValidConjugation7"   []
        this.Expect "ValidConjugation8"   []
        this.Expect "InvalidConjugation1" [Error ErrorCode.InvalidReassignmentInApplyBlock]
        this.Expect "InvalidConjugation2" [Error ErrorCode.InvalidReassignmentInApplyBlock]
        this.Expect "InvalidConjugation3" [Error ErrorCode.InvalidReassignmentInApplyBlock]
        this.Expect "InvalidConjugation4" [Error ErrorCode.InvalidReassignmentInApplyBlock]
        this.Expect "InvalidConjugation5" [Error ErrorCode.InvalidReassignmentInApplyBlock]
        this.Expect "InvalidConjugation6" [Error ErrorCode.InvalidReassignmentInApplyBlock]
        this.Expect "InvalidConjugation7" [Error ErrorCode.InvalidReassignmentInApplyBlock]
        this.Expect "InvalidConjugation8" [Error ErrorCode.InvalidReassignmentInApplyBlock]


    [<Fact>]
    member this.``Deprecation warnings`` () =
        this.Expect "DeprecatedType"               []
        this.Expect "RenamedType"                  []
        this.Expect "DuplicateDeprecateAttribute1" [Warning WarningCode.DuplicateAttribute]
        this.Expect "DuplicateDeprecateAttribute2" [Warning WarningCode.DuplicateAttribute]

        this.Expect "DeprecatedTypeConstructor"    [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "RenamedTypeConstructor"       [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "UsingDeprecatedCallable"      [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "UsingRenamedCallable"         [Warning WarningCode.DeprecationWithRedirect]
                                                   
        this.Expect "DeprecatedItemType1"          [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "DeprecatedItemType2"          [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "RenamedItemType1"             [Warning WarningCode.DeprecationWithRedirect]
        this.Expect "RenamedItemType2"             [Warning WarningCode.DeprecationWithRedirect]
                                                   
        this.Expect "UsingDeprecatedAttribute1"    [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "UsingDeprecatedAttribute2"    [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "UsingDeprecatedAttribute3"    [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "UsingRenamedAttribute1"       [Warning WarningCode.DeprecationWithRedirect]
        this.Expect "UsingRenamedAttribute2"       [Warning WarningCode.DeprecationWithRedirect]
        this.Expect "UsingRenamedAttribute3"       [Warning WarningCode.DeprecationWithRedirect]
                                                   
        this.Expect "UsingDeprecatedType1"         [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "UsingDeprecatedType2"         [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "UsingDeprecatedType3"         [Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "UsingDeprecatedType4"         [Warning WarningCode.DeprecationWithoutRedirect; Warning WarningCode.DeprecationWithoutRedirect]
        this.Expect "UsingDeprecatedType5"         [Warning WarningCode.DeprecationWithoutRedirect; Warning WarningCode.DeprecationWithoutRedirect]
                                                   
        this.Expect "UsingRenamedType1"            [Warning WarningCode.DeprecationWithRedirect]
        this.Expect "UsingRenamedType2"            [Warning WarningCode.DeprecationWithRedirect]
        this.Expect "UsingRenamedType3"            [Warning WarningCode.DeprecationWithRedirect]
        this.Expect "UsingRenamedType4"            [Warning WarningCode.DeprecationWithRedirect; Warning WarningCode.DeprecationWithRedirect]
        this.Expect "UsingRenamedType5"            [Warning WarningCode.DeprecationWithRedirect; Warning WarningCode.DeprecationWithRedirect]


    [<Fact>]
    member this.``Unit test attributes`` () = 
        this.Expect "ValidTestAttribute1"    []
        this.Expect "ValidTestAttribute2"    []
        this.Expect "ValidTestAttribute3"    []
        this.Expect "ValidTestAttribute4"    []
        this.Expect "ValidTestAttribute5"    []
        this.Expect "ValidTestAttribute6"    []
        this.Expect "ValidTestAttribute7"    []
        this.Expect "ValidTestAttribute8"    []
        this.Expect "ValidTestAttribute9"    []
        this.Expect "ValidTestAttribute10"   []
        this.Expect "ValidTestAttribute11"   []
        this.Expect "ValidTestAttribute12"   []
        this.Expect "ValidTestAttribute13"   []
        this.Expect "ValidTestAttribute14"   []
        this.Expect "ValidTestAttribute15"   [Warning WarningCode.DuplicateAttribute]
        this.Expect "ValidTestAttribute16"   [Error ErrorCode.MissingType]
        this.Expect "ValidTestAttribute17"   []
        this.Expect "ValidTestAttribute18"   []
        this.Expect "ValidTestAttribute19"   [Warning WarningCode.DuplicateAttribute]
        this.Expect "ValidTestAttribute20"   []
                                             
        this.Expect "InvalidTestAttribute1"  [Error ErrorCode.InvalidTestAttributePlacement]
        this.Expect "InvalidTestAttribute2"  [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidTestAttribute3"  [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidTestAttribute4"  [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidTestAttribute5"  [Error ErrorCode.InvalidTestAttributePlacement; Warning WarningCode.TypeParameterNotResolvedByArgument]
        this.Expect "InvalidTestAttribute6"  [Error ErrorCode.InvalidTestAttributePlacement; Warning WarningCode.TypeParameterNotResolvedByArgument]
        this.Expect "InvalidTestAttribute7"  [Error ErrorCode.InvalidTestAttributePlacement]
        this.Expect "InvalidTestAttribute8"  [Error ErrorCode.InvalidTestAttributePlacement]
        this.Expect "InvalidTestAttribute9"  [Error ErrorCode.InvalidTestAttributePlacement]
        this.Expect "InvalidTestAttribute10" [Error ErrorCode.InvalidTestAttributePlacement]
        this.Expect "InvalidTestAttribute11" [Error ErrorCode.InvalidTestAttributePlacement]
        this.Expect "InvalidTestAttribute12" [Error ErrorCode.InvalidExecutionTargetForTest]
        this.Expect "InvalidTestAttribute13" [Error ErrorCode.InvalidExecutionTargetForTest]
        this.Expect "InvalidTestAttribute14" [Error ErrorCode.InvalidExecutionTargetForTest]
        this.Expect "InvalidTestAttribute15" [Error ErrorCode.InvalidExecutionTargetForTest; Warning WarningCode.DuplicateAttribute]
        this.Expect "InvalidTestAttribute16" [Error ErrorCode.InvalidTestAttributePlacement; Error ErrorCode.UnknownType]
        this.Expect "InvalidTestAttribute17" [Error ErrorCode.InvalidExecutionTargetForTest; Error ErrorCode.AttributeArgumentTypeMismatch]
        this.Expect "InvalidTestAttribute18" [Error ErrorCode.InvalidExecutionTargetForTest; Error ErrorCode.MissingAttributeArgument]
        this.Expect "InvalidTestAttribute19" [Error ErrorCode.InvalidExecutionTargetForTest]
        this.Expect "InvalidTestAttribute20" [Error ErrorCode.InvalidExecutionTargetForTest]
        this.Expect "InvalidTestAttribute21" [Error ErrorCode.InvalidExecutionTargetForTest]
        this.Expect "InvalidTestAttribute22" [Error ErrorCode.InvalidExecutionTargetForTest]


