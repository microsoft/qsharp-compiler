// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

module TypeCheckingTests =
    let private tests =
        CompilerTests.Compile("TestCases", [ "General.qs"; "TypeChecking.qs"; "Types.qs" ]) |> CompilerTests

    let internal expect name diagnostics =
        let ns = "Microsoft.Quantum.Testing.TypeChecking"
        tests.VerifyDiagnostics(QsQualifiedName.New(ns, name), diagnostics)

    [<Fact>]
    let ``Integer size in array constructor`` () = expect "SizedArray1" []

    [<Fact>]
    let ``Negative integer size in array constructor`` () = expect "SizedArray2" []

    [<Fact>]
    let ``Type variable in array constructor`` () = expect "SizedArray3" []

    [<Fact>]
    let ``Double size in array constructor`` () =
        expect "SizedArrayInvalid1" [ Error ErrorCode.ExpectingIntExpr ]

    [<Fact>]
    let ``String size in array constructor`` () =
        expect "SizedArrayInvalid2" [ Error ErrorCode.ExpectingIntExpr ]

    [<Fact>]
    let ``Tuple size in array constructor`` () =
        expect "SizedArrayInvalid3" [ Error ErrorCode.ExpectingIntExpr ]

type TypeCheckingTests() =
    member private this.Expect name diagnostics =
        TypeCheckingTests.expect name diagnostics


    [<Fact>]
    member this.Variance() =
        this.Expect "Variance1" []
        this.Expect "Variance2" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "Variance3" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "Variance4" [ Error ErrorCode.ArrayBaseTypeMismatch ]
        this.Expect "Variance5" [ Error ErrorCode.ArrayBaseTypeMismatch ]
        this.Expect "Variance6" [ Error ErrorCode.ArrayBaseTypeMismatch ]
        this.Expect "Variance7" []
        this.Expect "Variance8" []
        this.Expect "Variance9" [ Error ErrorCode.CallableTypeInputTypeMismatch ]
        this.Expect "Variance10" [ Error ErrorCode.CallableTypeInputTypeMismatch ]
        this.Expect "Variance11" [ Error ErrorCode.CallableTypeInputTypeMismatch ]
        this.Expect "Variance12" [ Error ErrorCode.CallableTypeInputTypeMismatch ]
        this.Expect "Variance13" [ Error ErrorCode.CallableTypeInputTypeMismatch ]
        this.Expect "Variance14" [ Error ErrorCode.CallableTypeInputTypeMismatch ]


    [<Fact>]
    member this.``Common base type``() =
        this.Expect "CommonBaseType1" []

        this.Expect
            "CommonBaseType2"
            [
                Error ErrorCode.ArgumentMismatchInBinaryOp
                Error ErrorCode.ArgumentMismatchInBinaryOp
            ]

        this.Expect
            "CommonBaseType3"
            [
                Error ErrorCode.ArgumentMismatchInBinaryOp
                Error ErrorCode.ArgumentMismatchInBinaryOp
            ]

        this.Expect "CommonBaseType4" [ Error ErrorCode.TypeMismatchInReturn ]
        this.Expect "CommonBaseType5" []
        this.Expect "CommonBaseType6" []
        this.Expect "CommonBaseType7" []
        this.Expect "CommonBaseType8" [ Error ErrorCode.TypeMismatchInReturn ]
        this.Expect "CommonBaseType9" []
        this.Expect "CommonBaseType10" []
        this.Expect "CommonBaseType11" [ Error ErrorCode.TypeMismatchInReturn ]

        this.Expect
            "CommonBaseType12"
            [
                Error ErrorCode.ArgumentMismatchInBinaryOp
                Error ErrorCode.ArgumentMismatchInBinaryOp
            ]

        this.Expect "CommonBaseType13" []
        this.Expect "CommonBaseType14" []
        this.Expect "CommonBaseType15" [ Error ErrorCode.TypeMismatchInReturn ]
        this.Expect "CommonBaseType16" [ Error ErrorCode.MultipleTypesInArray ]
        this.Expect "CommonBaseType17" []
        this.Expect "CommonBaseType18" []

        this.Expect
            "CommonBaseType19"
            [
                Warning WarningCode.TypeParameterNotResolvedByArgument
                Warning WarningCode.TypeParameterNotResolvedByArgument
                Warning WarningCode.ReturnTypeNotResolvedByArgument
            ]

        this.Expect "CommonBaseType20" [ Error ErrorCode.MultipleTypesInArray ]
        this.Expect "CommonBaseType21" []
        this.Expect "CommonBaseType22" []
        this.Expect "CommonBaseType23" []
        this.Expect "CommonBaseType24" []
        this.Expect "CommonBaseType25" [ Error ErrorCode.MultipleTypesInArray ]


    [<Fact>]
    member this.``Equality comparison``() =
        this.Expect "UnitEquality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "UnitInequality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "IntEquality" []
        this.Expect "IntInequality" []
        this.Expect "BigIntEquality" []
        this.Expect "BigIntInequality" []
        this.Expect "DoubleEquality" []
        this.Expect "DoubleInequality" []
        this.Expect "BoolEquality" []
        this.Expect "BoolInequality" []
        this.Expect "StringEquality" []
        this.Expect "StringInequality" []
        this.Expect "QubitEquality" []
        this.Expect "QubitInequality" []
        this.Expect "ResultEquality" []
        this.Expect "ResultInequality" []
        this.Expect "PauliEquality" []
        this.Expect "PauliInequality" []
        this.Expect "RangeEquality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "RangeInequality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "ArrayEquality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "ArrayInequality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "TupleEquality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "TupleInequality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "UDTEquality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "UDTInequality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "GenericEquality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "GenericInequality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "OperationEquality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "OperationInequality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "FunctionEquality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]
        this.Expect "FunctionInequality" [ Error ErrorCode.InvalidTypeInEqualityComparison ]

        this.Expect
            "InvalidTypeEquality"
            [
                Error ErrorCode.InvalidUseOfUnderscorePattern
                Error ErrorCode.InvalidUseOfUnderscorePattern
            ]

        this.Expect
            "InvalidTypeInequality"
            [
                Error ErrorCode.InvalidUseOfUnderscorePattern
                Error ErrorCode.InvalidUseOfUnderscorePattern
            ]

        this.Expect
            "NoCommonBaseEquality"
            [
                Error ErrorCode.ArgumentMismatchInBinaryOp
                Error ErrorCode.ArgumentMismatchInBinaryOp
            ]

        this.Expect
            "NoCommonBaseInequality"
            [
                Error ErrorCode.ArgumentMismatchInBinaryOp
                Error ErrorCode.ArgumentMismatchInBinaryOp
            ]


    [<Fact>]
    member this.``Argument matching``() =
        this.Expect "MatchArgument1" []
        this.Expect "MatchArgument2" []
        this.Expect "MatchArgument3" []
        this.Expect "MatchArgument4" []
        this.Expect "MatchArgument5" []
        this.Expect "MatchArgument6" []
        this.Expect "MatchArgument7" []
        this.Expect "MatchArgument8" []
        this.Expect "MatchArgument9" [ Error ErrorCode.ArgumentTypeMismatch ]

        this.Expect
            "MatchArgument10"
            [
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
            ]

        this.Expect "MatchArgument11" []
        this.Expect "MatchArgument12" []
        this.Expect "MatchArgument13" []
        this.Expect "MatchArgument14" []
        this.Expect "MatchArgument15" []
        this.Expect "MatchArgument16" []
        this.Expect "MatchArgument17" []
        this.Expect "MatchArgument18" []
        this.Expect "MatchArgument19" [ Error ErrorCode.ArgumentTupleShapeMismatch ]


    [<Fact>]
    member this.``Partial application``() =
        this.Expect "PartialApplication1" []
        this.Expect "PartialApplication2" [ Error ErrorCode.ArgumentTupleShapeMismatch ]
        this.Expect "PartialApplication3" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "PartialApplication4" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "PartialApplication5" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "PartialApplication6" []
        this.Expect "PartialApplication7" [ Error ErrorCode.ArgumentTupleShapeMismatch ]
        this.Expect "PartialApplication8" [ Error ErrorCode.ArgumentTupleShapeMismatch ]
        this.Expect "PartialApplication9" []
        this.Expect "PartialApplication10" []
        this.Expect "PartialApplication11" []
        this.Expect "PartialApplication12" []
        this.Expect "PartialApplication13" []
        this.Expect "PartialApplication14" []

        this.Expect
            "PartialApplication15"
            [ Error ErrorCode.ArgumentTypeMismatch; Error ErrorCode.ArgumentTypeMismatch ]

        this.Expect "PartialApplication16" [ Error ErrorCode.ArgumentTypeMismatch ]
        this.Expect "PartialApplication17" []
        this.Expect "PartialApplication18" [ Error ErrorCode.OperationCallOutsideOfOperation ]
        this.Expect "PartialApplication19" []
        this.Expect "PartialApplication20" []
        this.Expect "PartialApplication21" []
        this.Expect "PartialApplication22" [ Error ErrorCode.MissingFunctorForAutoGeneration ]
        this.Expect "PartialApplication23" []
        this.Expect "PartialApplication24" [ Error ErrorCode.InvalidControlledApplication ]
        this.Expect "PartialApplication25" [ Error ErrorCode.InvalidControlledApplication ]
        this.Expect "PartialApplication26" [ Error ErrorCode.InvalidAdjointApplication ]
        this.Expect "PartialApplication27" [ Error ErrorCode.InvalidAdjointApplication ]
        this.Expect "PartialApplication28" [ Error ErrorCode.TypeMismatchInReturn ]
        this.Expect "PartialApplication29" []
        this.Expect "PartialApplication30" []


    [<Fact>]
    member this.``Named type items``() =
        this.Expect "NamedItems1" []
        this.Expect "NamedItems2" []
        this.Expect "NamedItems3" []
        this.Expect "NamedItems4" []
        this.Expect "NamedItems5" []
        this.Expect "NamedItems6" []
        this.Expect "NamedItems7" []
        this.Expect "NamedItems8" []
        this.Expect "NamedItems9" [ Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket ]
        this.Expect "NamedItems10" [ Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket ]
        this.Expect "NamedItems11" [ Error ErrorCode.NamedItemAlreadyExists ]
        this.Expect "NamedItems12" [ Error ErrorCode.NamedItemAlreadyExists ]
        this.Expect "NamedItems13" [ Error ErrorCode.NamedItemAlreadyExists ]
        this.Expect "NamedItems14" [ Error ErrorCode.NamedItemAlreadyExists ]

        this.Expect "TupleType1" []
        this.Expect "TupleType2" []
        this.Expect "TupleType3" [ Warning WarningCode.DeprecatedUnitType ]
        this.Expect "TupleType4" [ Warning WarningCode.ExcessComma ]
        this.Expect "TupleType5" [ Error ErrorCode.ExcessContinuation ]
        this.Expect "TupleType6" [ Error ErrorCode.ExcessContinuation ]

        this.Expect "OpType1" []
        this.Expect "OpType2" []
        this.Expect "OpType3" []
        this.Expect "OpType4" []
        this.Expect "OpType5" []
        this.Expect "OpType6" [ Error ErrorCode.ExcessContinuation ]
        this.Expect "OpType7" [ Error ErrorCode.ExcessContinuation ]
        this.Expect "OpType8" []
        this.Expect "OpType9" []
        this.Expect "OpType10" []
        this.Expect "OpType11" []
        this.Expect "OpType12" []
        this.Expect "OpType13" [ Error ErrorCode.ExcessContinuation ]
        this.Expect "OpType14" [ Error ErrorCode.ExcessContinuation ]
        this.Expect "OpType15" []
        this.Expect "OpType16" [ Error ErrorCode.InvalidUdtItemNameDeclaration; Error ErrorCode.InvalidType ]

        this.Expect
            "OpType17"
            [
                Error ErrorCode.InvalidUdtItemNameDeclaration
                Error ErrorCode.InvalidType
                Error ErrorCode.InvalidUdtItemDeclaration
            ]

        this.Expect
            "OpType18"
            [
                Error ErrorCode.InvalidUdtItemNameDeclaration
                Error ErrorCode.InvalidType
                Error ErrorCode.InvalidUdtItemDeclaration
            ]

        this.Expect "FctType1" []
        this.Expect "FctType2" []
        this.Expect "FctType3" []
        this.Expect "FctType4" []
        this.Expect "FctType5" []
        this.Expect "FctType6" []
        this.Expect "FctType7" []
        this.Expect "FctType8" []

        this.Expect "ArrayType1" []
        this.Expect "ArrayType2" []
        this.Expect "ArrayType3" [ Error ErrorCode.UnknownType; Error ErrorCode.ExcessContinuation ]
        this.Expect "ArrayType4" []
        this.Expect "ArrayType5" []
        this.Expect "ArrayType6" [ Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket ]
        this.Expect "ArrayType7" [ Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket ]
        this.Expect "ArrayType8" []
        this.Expect "ArrayType9" []
        this.Expect "ArrayType10" []
        this.Expect "ArrayType11" []
        this.Expect "ArrayType12" []
        this.Expect "ArrayType13" []
        this.Expect "ArrayType14" []
        this.Expect "ArrayType15" []
        this.Expect "ArrayType16" [ Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket ]
        this.Expect "ArrayType17" [ Error ErrorCode.MissingLTupleBracket; Error ErrorCode.MissingRTupleBracket ]
        this.Expect "ArrayType18" []
        this.Expect "ArrayType19" []
        this.Expect "ArrayType20" []
