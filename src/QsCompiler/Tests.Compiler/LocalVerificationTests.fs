// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open System.IO
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit


type LocalVerificationTests() =
    inherit CompilerTests(CompilerTests.Compile
                              ("TestCases",
                               [
                                   "General.qs"
                                   "LocalVerification.qs"
                                   "Types.qs"
                                   Path.Combine("LinkingTests", "Core.qs")
                                   Path.Combine("StringParsingTests", "StringParsing.qs")
                                   Path.Combine("StringParsingTests", "StringInterpolation.qs")
                               ]))

    member private this.Expect name (diag: IEnumerable<DiagnosticItem>) =
        let ns = "Microsoft.Quantum.Testing.LocalVerification"
        this.VerifyDiagnostics(QsQualifiedName.New(ns, name), diag)


    [<Fact>]
    member this.``Type argument inference``() =
        this.Expect "TypeArgumentsInference1" [ Error ErrorCode.UnresolvedTypeParameterForRecursiveCall ]
        this.Expect "TypeArgumentsInference2" [ Error ErrorCode.UnresolvedTypeParameterForRecursiveCall ]

        this.Expect
            "TypeArgumentsInference3"
            [
                Error ErrorCode.UnresolvedTypeParameterForRecursiveCall
                Error ErrorCode.MultipleTypesInArray
            ]

        this.Expect
            "TypeArgumentsInference4"
            [
                Error ErrorCode.UnresolvedTypeParameterForRecursiveCall
                Error ErrorCode.MultipleTypesInArray
            ]

        this.Expect
            "TypeArgumentsInference5"
            [
                Error ErrorCode.UnresolvedTypeParameterForRecursiveCall
                Error ErrorCode.UnresolvedTypeParameterForRecursiveCall
            ]

        this.Expect "TypeArgumentsInference6" [ Error ErrorCode.UnresolvedTypeParameterForRecursiveCall ]
        this.Expect "TypeArgumentsInference7" [ Error ErrorCode.UnresolvedTypeParameterForRecursiveCall ]

        this.Expect
            "TypeArgumentsInference8"
            [
                Error ErrorCode.UnresolvedTypeParameterForRecursiveCall
                Error ErrorCode.UnresolvedTypeParameterForRecursiveCall
            ]

        this.Expect
            "TypeArgumentsInference9"
            [
                Error ErrorCode.UnresolvedTypeParameterForRecursiveCall
                Error ErrorCode.UnresolvedTypeParameterForRecursiveCall
            ]

        this.Expect "TypeArgumentsInference10" []
        this.Expect "TypeArgumentsInference11" []
        this.Expect "TypeArgumentsInference12" []
        this.Expect "TypeArgumentsInference13" []
        this.Expect "TypeArgumentsInference14" []
        this.Expect "TypeArgumentsInference15" []
        this.Expect "TypeArgumentsInference16" []
        this.Expect "TypeArgumentsInference17" []
        this.Expect "TypeArgumentsInference18" []
        this.Expect "TypeArgumentsInference19" []
        this.Expect "TypeArgumentsInference20" []
        this.Expect "TypeArgumentsInference21" [ Error ErrorCode.ConstrainsTypeParameter ]
        this.Expect "TypeArgumentsInference22" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "TypeArgumentsInference23" []
        this.Expect "TypeArgumentsInference24" []
        this.Expect "TypeArgumentsInference25" []
        this.Expect "TypeArgumentsInference26" []
        this.Expect "TypeArgumentsInference27" [ Error ErrorCode.ConstrainsTypeParameter ]
        this.Expect "TypeArgumentsInference28" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "TypeArgumentsInference29" [ Error ErrorCode.InvalidCyclicTypeParameterResolution ]

        this.Expect
            "TypeArgumentsInference30"
            [
                Error ErrorCode.TypeParameterResConflictWithTypeArgument
                Error ErrorCode.InvalidCyclicTypeParameterResolution
            ]

        this.Expect
            "TypeArgumentsInference31"
            [
                Error ErrorCode.TypeParameterResConflictWithTypeArgument
                Error ErrorCode.InvalidCyclicTypeParameterResolution
            ]

        this.Expect "TypeArgumentsInference32" [ Error ErrorCode.ConstrainsTypeParameter ]
        this.Expect "TypeArgumentsInference33" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "TypeArgumentsInference34" []
        this.Expect "TypeArgumentsInference35" []
        this.Expect "TypeArgumentsInference36" []
        this.Expect "TypeArgumentsInference37" []
        this.Expect "TypeArgumentsInference38" [ Error ErrorCode.ConstrainsTypeParameter ]
        this.Expect "TypeArgumentsInference39" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "TypeArgumentsInference40" []
        this.Expect "TypeArgumentsInference41" [ Error ErrorCode.TypeParameterResConflictWithTypeArgument ]
        this.Expect "TypeArgumentsInference42" [ Error ErrorCode.TypeParameterResConflictWithTypeArgument ]


    [<Fact>]
    member this.``Variable declarations``() =
        this.Expect "VariableDeclaration1" []
        this.Expect "VariableDeclaration2" []
        this.Expect "VariableDeclaration3" []
        this.Expect "VariableDeclaration4" []
        this.Expect "VariableDeclaration5" []
        this.Expect "VariableDeclaration6" []
        this.Expect "VariableDeclaration7" []
        this.Expect "VariableDeclaration8" []
        this.Expect "VariableDeclaration9" [ Error ErrorCode.SymbolTupleShapeMismatch ]
        this.Expect "VariableDeclaration10" [ Error ErrorCode.SymbolTupleShapeMismatch ]
        this.Expect "VariableDeclaration11" [ Error ErrorCode.InvalidUseOfTypeParameterizedObject ]
        this.Expect "VariableDeclaration12" [ Error ErrorCode.InvalidUseOfTypeParameterizedObject ]
        this.Expect "VariableDeclaration13" [ Error ErrorCode.ConstrainsTypeParameter ]

        this.Expect
            "VariableDeclaration14"
            [
                Error ErrorCode.InvalidUseOfTypeParameterizedObject
                Error ErrorCode.InvalidUseOfTypeParameterizedObject
            ]

        this.Expect "VariableDeclaration15" [ Error ErrorCode.InvalidUseOfTypeParameterizedObject ]
        this.Expect "VariableDeclaration16" [ Error ErrorCode.InvalidUseOfTypeParameterizedObject ]
        this.Expect "VariableDeclaration17" [ Error ErrorCode.InvalidUseOfTypeParameterizedObject ]

        this.Expect
            "VariableDeclaration18"
            [
                Error ErrorCode.InvalidUseOfTypeParameterizedObject
                Error ErrorCode.MultipleTypesInArray
            ]

        this.Expect "VariableDeclaration19" [ Error ErrorCode.InvalidUseOfTypeParameterizedObject ]
        this.Expect "VariableDeclaration20" [ Error ErrorCode.ConstrainsTypeParameter ]
        this.Expect "VariableDeclaration21" []
        this.Expect "VariableDeclaration22" []
        this.Expect "VariableDeclaration23" []
        this.Expect "VariableDeclaration24" [ Error ErrorCode.UnknownIdentifier ]
        this.Expect "VariableDeclaration25" []
        this.Expect "VariableDeclaration26" []
        this.Expect "VariableDeclaration27" []
        this.Expect "VariableDeclaration28" [ Error ErrorCode.ExpectingOpeningBracketOrSemicolon ]
        this.Expect "VariableDeclaration29" [ Error ErrorCode.UnknownIdentifier ]
        this.Expect "VariableDeclaration30" []
        this.Expect "VariableDeclaration31" [ Error ErrorCode.ExpectingOpeningBracketOrSemicolon ]
        this.Expect "VariableDeclaration32" [ Error ErrorCode.UnknownIdentifier ]


    [<Fact>]
    member this.``Copy-and-update arrays``() =
        this.Expect "CopyAndUpdateArray1" []
        this.Expect "CopyAndUpdateArray2" []
        this.Expect "CopyAndUpdateArray3" []
        this.Expect "CopyAndUpdateArray4" []
        this.Expect "CopyAndUpdateArray5" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "CopyAndUpdateArray6" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "CopyAndUpdateArray7" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "CopyAndUpdateArray8" [ Error ErrorCode.ConstrainsTypeParameter ]
        this.Expect "CopyAndUpdateArray9" []
        this.Expect "CopyAndUpdateArray10" []
        this.Expect "CopyAndUpdateArray11" []
        this.Expect "CopyAndUpdateArray12" []
        this.Expect "CopyAndUpdateArray13" []
        this.Expect "CopyAndUpdateArray14" []
        this.Expect "CopyAndUpdateArray15" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "CopyAndUpdateArray16" [ Error ErrorCode.ConstrainsTypeParameter ]


    [<Fact>]
    member this.``Update-and-reassign arrays``() =
        this.Expect "UpdateAndReassign1" []
        this.Expect "UpdateAndReassign2" []
        this.Expect "UpdateAndReassign3" []
        this.Expect "UpdateAndReassign4" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "UpdateAndReassign5" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "UpdateAndReassign6" []
        this.Expect "UpdateAndReassign7" []
        this.Expect "UpdateAndReassign8" []
        this.Expect "UpdateAndReassign9" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "UpdateAndReassign10" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]


    [<Fact>]
    member this.``Apply-and-reassign``() =
        this.Expect "ApplyAndReassign1" []
        this.Expect "ApplyAndReassign2" []
        this.Expect "ApplyAndReassign3" []
        this.Expect "ApplyAndReassign4" []
        this.Expect "ApplyAndReassign5" []
        this.Expect "ApplyAndReassign6" [ Error ErrorCode.ExpectingBoolExpr ]

        this.Expect
            "ApplyAndReassign7"
            [
                Error ErrorCode.ArgumentMismatchInBinaryOp
                Error ErrorCode.ArgumentMismatchInBinaryOp
            ]

        this.Expect "ApplyAndReassign8" [ Error ErrorCode.UpdateOfImmutableIdentifier ]
        this.Expect "ApplyAndReassign9" [ Error ErrorCode.UpdateOfArrayItemExpr ]
        this.Expect "ApplyAndReassign10" [ Error ErrorCode.UpdateOfArrayItemExpr ]
        this.Expect "ApplyAndReassign11" [ Error ErrorCode.InvalidUseOfReservedKeyword ]


    [<Fact>]
    member this.``Named type item access``() =
        this.Expect "ItemAccess1" [ Error ErrorCode.ExpectingUserDefinedType ]
        this.Expect "ItemAccess2" [ Error ErrorCode.UnknownItemName ]
        this.Expect "ItemAccess3" []
        this.Expect "ItemAccess4" []

        this.Expect
            "ItemAccess5"
            [
                Error ErrorCode.ArgumentMismatchInBinaryOp
                Error ErrorCode.ArgumentMismatchInBinaryOp
            ]

        this.Expect "ItemAccess6" []
        this.Expect "ItemAccess7" []
        this.Expect "ItemAccess8" []
        this.Expect "ItemAccess9" []
        this.Expect "ItemAccess10" []
        this.Expect "ItemAccess11" []

        this.Expect
            "ItemAccess12"
            [
                Error ErrorCode.OperationCallOutsideOfOperation
                Error ErrorCode.OperationCallOutsideOfOperation
            ]

        this.Expect
            "ItemAccess13"
            [
                Error ErrorCode.MissingFunctorForAutoGeneration
                Error ErrorCode.MissingFunctorForAutoGeneration
            ]

        this.Expect "ItemAccess14" []
        this.Expect "ItemAccess15" []
        this.Expect "ItemAccess16" []
        this.Expect "ItemAccess17" []
        this.Expect "ItemAccess18" []
        this.Expect "ItemAccess19" []
        this.Expect "ItemAccess20" []


    [<Fact>]
    member this.``Named type item update``() =
        this.Expect "ItemUpdate1" []
        this.Expect "ItemUpdate2" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "ItemUpdate3" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "ItemUpdate4" []
        this.Expect "ItemUpdate5" [ Error ErrorCode.UpdateOfImmutableIdentifier ]
        this.Expect "ItemUpdate6" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "ItemUpdate7" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]

        this.Expect
            "ItemUpdate8"
            [
                Error ErrorCode.TypeMismatchInCopyAndUpdateExpr
                Error ErrorCode.TypeMismatchInCopyAndUpdateExpr
            ]

        this.Expect "ItemUpdate9" []

        this.Expect
            "ItemUpdate10"
            [
                Error ErrorCode.InvalidIdentifierExprInUpdate
                Error ErrorCode.ExcessContinuation
            ]

        this.Expect "ItemUpdate11" [ Error ErrorCode.UpdateOfArrayItemExpr ]

        this.Expect
            "ItemUpdate12"
            [
                Error ErrorCode.TypeMismatchInCopyAndUpdateExpr
                Error ErrorCode.TypeMismatchInCopyAndUpdateExpr
            ]

        this.Expect "ItemUpdate13" []
        this.Expect "ItemUpdate14" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]
        this.Expect "ItemUpdate15" [ Error ErrorCode.TypeMismatchInCopyAndUpdateExpr ]

        this.Expect
            "ItemUpdate16"
            [
                Error ErrorCode.MissingFunctorForAutoGeneration
                Error ErrorCode.MissingFunctorForAutoGeneration
            ]

        this.Expect
            "ItemUpdate17"
            [
                Error ErrorCode.MissingFunctorForAutoGeneration
                Error ErrorCode.ValueUpdateWithinAutoInversion
            ]

        this.Expect "ItemUpdate18" [ Error ErrorCode.MissingFunctorForAutoGeneration ]
        this.Expect "ItemUpdate19" [ Error ErrorCode.MissingFunctorForAutoGeneration ]
        this.Expect "ItemUpdate20" []
        this.Expect "ItemUpdate21" []
        this.Expect "ItemUpdate22" []


    [<Fact>]
    member this.``Open-ended ranges``() =
        this.Expect "ValidArraySlice1" []
        this.Expect "ValidArraySlice2" []
        this.Expect "ValidArraySlice3" []
        this.Expect "ValidArraySlice4" []
        this.Expect "ValidArraySlice5" []
        this.Expect "ValidArraySlice6" []
        this.Expect "ValidArraySlice7" []
        this.Expect "ValidArraySlice8" []
        this.Expect "ValidArraySlice9" []

        this.Expect "InvalidArraySlice1" [ Error ErrorCode.ItemAccessForNonArray ]
        this.Expect "InvalidArraySlice2" [ Error ErrorCode.ItemAccessForNonArray ]
        this.Expect "InvalidArraySlice3" [ Error ErrorCode.ItemAccessForNonArray ]
        this.Expect "InvalidArraySlice4" [ Error ErrorCode.ItemAccessForNonArray ]
        this.Expect "InvalidArraySlice5" [ Error ErrorCode.ItemAccessForNonArray ]
        this.Expect "InvalidArraySlice6" [ Error ErrorCode.ItemAccessForNonArray ]
        this.Expect "InvalidArraySlice7" [ Error ErrorCode.ItemAccessForNonArray ]
        this.Expect "InvalidArraySlice8" [ Error ErrorCode.ItemAccessForNonArray ]
        this.Expect "InvalidArraySlice9" [ Error ErrorCode.ItemAccessForNonArray ]


    [<Fact>]
    member this.``Conjugation verification``() =
        this.Expect "ValidConjugation1" []
        this.Expect "ValidConjugation2" []
        this.Expect "ValidConjugation3" []
        this.Expect "ValidConjugation4" []
        this.Expect "ValidConjugation5" []
        this.Expect "ValidConjugation6" []
        this.Expect "ValidConjugation7" []
        this.Expect "ValidConjugation8" []

        this.Expect "InvalidConjugation1" [ Error ErrorCode.InvalidReassignmentInApplyBlock ]
        this.Expect "InvalidConjugation2" [ Error ErrorCode.InvalidReassignmentInApplyBlock ]
        this.Expect "InvalidConjugation3" [ Error ErrorCode.InvalidReassignmentInApplyBlock ]
        this.Expect "InvalidConjugation4" [ Error ErrorCode.InvalidReassignmentInApplyBlock ]
        this.Expect "InvalidConjugation5" [ Error ErrorCode.InvalidReassignmentInApplyBlock ]
        this.Expect "InvalidConjugation6" [ Error ErrorCode.InvalidReassignmentInApplyBlock ]
        this.Expect "InvalidConjugation7" [ Error ErrorCode.InvalidReassignmentInApplyBlock ]
        this.Expect "InvalidConjugation8" [ Error ErrorCode.InvalidReassignmentInApplyBlock ]


    [<Fact>]
    member this.``Deprecation warnings``() =
        this.Expect "DeprecatedType" []
        this.Expect "RenamedType" []
        this.Expect "DeprecatedCallable" []
        this.Expect "DuplicateDeprecateAttribute1" [ Warning WarningCode.DuplicateAttribute ]
        this.Expect "DuplicateDeprecateAttribute2" [ Warning WarningCode.DuplicateAttribute ]

        this.Expect "DeprecatedTypeConstructor" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "RenamedTypeConstructor" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "UsingDeprecatedCallable" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "UsingRenamedCallable" [ Warning WarningCode.DeprecationWithRedirect ]

        this.Expect "DeprecatedItemType1" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "DeprecatedItemType2" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "RenamedItemType1" [ Warning WarningCode.DeprecationWithRedirect ]
        this.Expect "RenamedItemType2" [ Warning WarningCode.DeprecationWithRedirect ]

        this.Expect "UsingDeprecatedAttribute1" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "UsingDeprecatedAttribute2" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "UsingDeprecatedAttribute3" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "UsingRenamedAttribute1" [ Warning WarningCode.DeprecationWithRedirect ]
        this.Expect "UsingRenamedAttribute2" [ Warning WarningCode.DeprecationWithRedirect ]
        this.Expect "UsingRenamedAttribute3" [ Warning WarningCode.DeprecationWithRedirect ]

        this.Expect "NestedDeprecatedCallable" []
        this.Expect "DeprecatedAttributeInDeprecatedCallable" []
        this.Expect "DeprecatedTypeInDeprecatedCallable" []
        this.Expect "UsingNestedDeprecatedCallable" [ Warning WarningCode.DeprecationWithRedirect ]
        this.Expect "UsingDepAttrInDepCall" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "UsingDepTypeInDepCall" [ Warning WarningCode.DeprecationWithoutRedirect ]

        this.Expect "UsingDeprecatedType1" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "UsingDeprecatedType2" [ Warning WarningCode.DeprecationWithoutRedirect ]
        this.Expect "UsingDeprecatedType3" [ Warning WarningCode.DeprecationWithoutRedirect ]

        this.Expect
            "UsingDeprecatedType4"
            [
                Warning WarningCode.DeprecationWithoutRedirect
                Warning WarningCode.DeprecationWithoutRedirect
            ]

        this.Expect
            "UsingDeprecatedType5"
            [
                Warning WarningCode.DeprecationWithoutRedirect
                Warning WarningCode.DeprecationWithoutRedirect
            ]

        this.Expect "UsingRenamedType1" [ Warning WarningCode.DeprecationWithRedirect ]
        this.Expect "UsingRenamedType2" [ Warning WarningCode.DeprecationWithRedirect ]
        this.Expect "UsingRenamedType3" [ Warning WarningCode.DeprecationWithRedirect ]

        this.Expect
            "UsingRenamedType4"
            [
                Warning WarningCode.DeprecationWithRedirect
                Warning WarningCode.DeprecationWithRedirect
            ]

        this.Expect
            "UsingRenamedType5"
            [
                Warning WarningCode.DeprecationWithRedirect
                Warning WarningCode.DeprecationWithRedirect
            ]


    [<Fact>]
    member this.``Unit test attributes``() =
        this.Expect "ValidTestAttribute1" []
        this.Expect "ValidTestAttribute2" []
        this.Expect "ValidTestAttribute3" []
        this.Expect "ValidTestAttribute4" []
        this.Expect "ValidTestAttribute5" []
        this.Expect "ValidTestAttribute6" []
        this.Expect "ValidTestAttribute7" []
        this.Expect "ValidTestAttribute8" []
        this.Expect "ValidTestAttribute9" []
        this.Expect "ValidTestAttribute10" []
        this.Expect "ValidTestAttribute11" []
        this.Expect "ValidTestAttribute12" []
        this.Expect "ValidTestAttribute13" []
        this.Expect "ValidTestAttribute14" []
        this.Expect "ValidTestAttribute15" [ Warning WarningCode.DuplicateAttribute ]
        this.Expect "ValidTestAttribute16" [ Error ErrorCode.MissingType ]
        this.Expect "ValidTestAttribute17" []
        this.Expect "ValidTestAttribute18" []
        this.Expect "ValidTestAttribute19" [ Warning WarningCode.DuplicateAttribute ]
        this.Expect "ValidTestAttribute20" []

        this.Expect "InvalidTestAttribute1" [ Error ErrorCode.InvalidTestAttributePlacement ]
        this.Expect "InvalidTestAttribute2" [ Error ErrorCode.MisplacedDeclarationAttribute ]
        this.Expect "InvalidTestAttribute3" [ Error ErrorCode.MisplacedDeclarationAttribute ]
        this.Expect "InvalidTestAttribute4" [ Error ErrorCode.MisplacedDeclarationAttribute ]

        this.Expect
            "InvalidTestAttribute5"
            [
                Error ErrorCode.InvalidTestAttributePlacement
                Warning WarningCode.TypeParameterNotResolvedByArgument
            ]

        this.Expect
            "InvalidTestAttribute6"
            [
                Error ErrorCode.InvalidTestAttributePlacement
                Warning WarningCode.TypeParameterNotResolvedByArgument
            ]

        this.Expect "InvalidTestAttribute7" [ Error ErrorCode.InvalidTestAttributePlacement ]
        this.Expect "InvalidTestAttribute8" [ Error ErrorCode.InvalidTestAttributePlacement ]
        this.Expect "InvalidTestAttribute9" [ Error ErrorCode.InvalidTestAttributePlacement ]
        this.Expect "InvalidTestAttribute10" [ Error ErrorCode.InvalidTestAttributePlacement ]
        this.Expect "InvalidTestAttribute11" [ Error ErrorCode.InvalidTestAttributePlacement ]
        this.Expect "InvalidTestAttribute12" [ Error ErrorCode.InvalidExecutionTargetForTest ]
        this.Expect "InvalidTestAttribute13" [ Error ErrorCode.InvalidExecutionTargetForTest ]
        this.Expect "InvalidTestAttribute14" [ Error ErrorCode.InvalidExecutionTargetForTest ]

        this.Expect
            "InvalidTestAttribute15"
            [
                Error ErrorCode.InvalidExecutionTargetForTest
                Warning WarningCode.DuplicateAttribute
            ]

        this.Expect
            "InvalidTestAttribute16"
            [ Error ErrorCode.InvalidTestAttributePlacement; Error ErrorCode.UnknownType ]

        this.Expect
            "InvalidTestAttribute17"
            [
                Error ErrorCode.InvalidExecutionTargetForTest
                Error ErrorCode.AttributeArgumentTypeMismatch
            ]

        this.Expect
            "InvalidTestAttribute18"
            [
                Error ErrorCode.InvalidExecutionTargetForTest
                Error ErrorCode.MissingAttributeArgument
            ]

        this.Expect "InvalidTestAttribute19" [ Error ErrorCode.InvalidExecutionTargetForTest ]
        this.Expect "InvalidTestAttribute20" [ Error ErrorCode.InvalidExecutionTargetForTest ]
        this.Expect "InvalidTestAttribute21" [ Error ErrorCode.InvalidExecutionTargetForTest ]
        this.Expect "InvalidTestAttribute22" [ Error ErrorCode.InvalidExecutionTargetForTest ]


    [<Fact>]
    member this.``Parentheses in statements``() =
        this.Expect "ParensIf" []
        this.Expect "NoParensIf" []
        this.Expect "ParensElif" []
        this.Expect "NoParensElif" []
        this.Expect "ParensFor" [ Warning WarningCode.DeprecatedTupleBrackets ]
        this.Expect "NoParensFor" []
        this.Expect "ParensWhile" []
        this.Expect "NoParensWhile" []
        this.Expect "ParensUntil" []
        this.Expect "NoParensUntil" []
        this.Expect "ParensUntilFixup" []
        this.Expect "NoParensUntilFixup" []
        this.Expect "ParensUse" [ Warning WarningCode.DeprecatedTupleBrackets ]
        this.Expect "NoParensUse" []
        this.Expect "ParensBorrow" [ Warning WarningCode.DeprecatedTupleBrackets ]
        this.Expect "NoParensBorrow" []

    [<Fact>]
    member this.``String Parsing``() =
        this.Expect "StringParsingTest1" []
        this.Expect "StringParsingTest2" []
        this.Expect "StringParsingTest3" []
        this.Expect "StringParsingTest4" []
        this.Expect "StringParsingTest5" []
        this.Expect "StringParsingTest6" []
        this.Expect "StringParsingTest7" []

        this.Expect "MultiLineStringTest1" []
        this.Expect "MultiLineStringTest2" []
        this.Expect "MultiLineStringTest3" []
        this.Expect "MultiLineStringTest4" []
        this.Expect "MultiLineStringTest5" []
        this.Expect "MultiLineStringTest6" []
        this.Expect "MultiLineStringTest7" []
        this.Expect "MultiLineStringTest8" []
        this.Expect "MultiLineStringTest9" [ Error ErrorCode.ExcessContinuation ]

        this.Expect "StringInterpolationTest1" []
        this.Expect "StringInterpolationTest2" []

        this.Expect "StringInterpolationSimpleStringTest1" []
        this.Expect "StringInterpolationSimpleStringTest2" []
        this.Expect "StringInterpolationSimpleStringTest3" []
        this.Expect "StringInterpolationSimpleStringTest4" []

        this.Expect "StringInterpolationQuoteTest1" []
        this.Expect "StringInterpolationQuoteTest2" []
        this.Expect "StringInterpolationQuoteTest3" []
        this.Expect "StringInterpolationQuoteTest4" []

        this.Expect "StringInterpolationSemicolonTest1" []
        this.Expect "StringInterpolationSemicolonTest2" []
        this.Expect "StringInterpolationSemicolonTest3" []
        this.Expect "StringInterpolationSemicolonTest4" []

        this.Expect "StringInterpolationDollarSignTest1" []
        this.Expect "StringInterpolationDollarSignTest2" []
        this.Expect "StringInterpolationDollarSignTest3" []
        this.Expect "StringInterpolationDollarSignTest4" []
        this.Expect "StringInterpolationDollarSignTest5" []
        this.Expect "StringInterpolationDollarSignTest6" []

        this.Expect "StringInterpolationOpenBraceTest1" []
        this.Expect "StringInterpolationOpenBraceTest2" []
        this.Expect "StringInterpolationOpenBraceTest3" []
        this.Expect "StringInterpolationOpenBraceTest4" []
        this.Expect "StringInterpolationOpenBraceTest5" []
        this.Expect "StringInterpolationOpenBraceTest6" []

        this.Expect "StringInterpolationWithCommentTest1" []
        this.Expect "StringInterpolationWithCommentTest2" []
        this.Expect "StringInterpolationWithCommentTest3" []
        this.Expect "StringInterpolationWithCommentTest4" []
        this.Expect "StringInterpolationWithCommentTest5" []
        this.Expect "StringInterpolationWithCommentTest6" []
        this.Expect "StringInterpolationWithCommentTest7" []
        this.Expect "StringInterpolationWithCommentTest8" []


    [<Fact>]
    member this.``Nested Interpolation Strings``() =
        this.Expect "StringNestedInterpolationTest1" [ Error ErrorCode.InvalidCharacterInInterpolatedArgument ]
        this.Expect "StringNestedInterpolationTest2" [ Error ErrorCode.InvalidCharacterInInterpolatedArgument ]
        this.Expect "StringNestedInterpolationTest3" [ Error ErrorCode.InvalidCharacterInInterpolatedArgument ]

    [<Fact>]
    member this.``Deprecated qubit allocation keywords``() =
        this.Expect "DeprecatedUsingKeyword" [ Warning WarningCode.DeprecatedQubitBindingKeyword ]

        this.Expect
            "DeprecatedUsingKeywordParens"
            [
                Warning WarningCode.DeprecatedQubitBindingKeyword
                Warning WarningCode.DeprecatedTupleBrackets
            ]

        this.Expect "DeprecatedBorrowingKeyword" [ Warning WarningCode.DeprecatedQubitBindingKeyword ]

        this.Expect
            "DeprecatedBorrowingKeywordParens"
            [
                Warning WarningCode.DeprecatedQubitBindingKeyword
                Warning WarningCode.DeprecatedTupleBrackets
            ]
