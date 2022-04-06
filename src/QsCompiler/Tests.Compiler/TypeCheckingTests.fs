// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Immutable
open System.IO
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

/// Tests for type checking of Q# programs.
module TypeCheckingTests =
    let private compilation =
        CompilerTests.Compile(
            "TestCases",
            [
                Path.Combine("LinkingTests", "Core.qs")
                "General.qs"
                "TypeChecking.qs"
                "Types.qs"
            ]
        )

    /// The compiled type-checking tests.
    let private tests = CompilerTests compilation

    let private ns = "Microsoft.Quantum.Testing.TypeChecking"

    /// <summary>
    /// Asserts that the declaration with the given <paramref name="name"/> has the given
    /// <paramref name="diagnostics"/>.
    /// </summary>
    let internal expect name diagnostics =
        tests.VerifyDiagnostics(QsQualifiedName.New(ns, name), diagnostics)

    let private allValid name count =
        for i = 1 to count do
            expect (sprintf "%s%i" name i) []

    let private findSpecScope name kind =
        let callable = compilation.Callables.[QsQualifiedName.New(ns, name)]
        let spec = callable.Specializations |> Seq.find (fun s -> s.Kind = kind)

        match spec.Implementation with
        | Provided (_, scope) -> scope
        | _ -> failwith "Missing specialization implementation."

    let private getOperationType (type_: ResolvedType) =
        match type_.Resolution with
        | QsTypeKind.Operation (io, info) -> io, info
        | _ -> failwith "Not an operation type."

    [<Fact>]
    let ``Supports integral operators`` () =
        allValid "Integral" 2
        expect "IntegralInvalid1" (Error ErrorCode.ExpectingIntegralExpr |> List.replicate 4)
        expect "IntegralInvalid2" (Error ErrorCode.TypeIntersectionMismatch |> List.replicate 4)

    [<Fact>]
    let ``Supports iteration`` () =
        allValid "Iterable" 2
        expect "IterableInvalid1" [ Error ErrorCode.ExpectingIterableExpr ]
        expect "IterableInvalid2" [ Error ErrorCode.ExpectingIterableExpr ]

    [<Fact>]
    let ``Supports numeric operators`` () =
        allValid "Numeric" 3
        expect "NumericInvalid1" (Error ErrorCode.InvalidTypeInArithmeticExpr |> List.replicate 4)
        expect "NumericInvalid2" (Error ErrorCode.InvalidTypeInArithmeticExpr |> List.replicate 4)

    [<Fact>]
    let ``Supports power operator`` () =
        expect "Power1" []
        expect "Power2" []
        expect "Power3" []
        expect "Power4" []
        expect "Power5" []
        expect "PowerInvalid1" [ Error ErrorCode.TypeMismatch ]
        expect "PowerInvalid2" [ Error ErrorCode.TypeMismatch ]
        expect "PowerInvalid3" [ Error ErrorCode.TypeMismatch ]
        expect "PowerInvalid4" [ Error ErrorCode.TypeMismatch ]
        expect "PowerInvalid5" [ Error ErrorCode.TypeMismatch ]

    [<Fact>]
    let ``Supports the semigroup operator`` () =
        allValid "Semigroup" 5
        expect "SemigroupInvalid1" [ Error ErrorCode.InvalidTypeForConcatenation ]
        expect "SemigroupInvalid2" [ Error ErrorCode.InvalidTypeForConcatenation ]

    [<Fact>]
    let ``Supports the unwrap operator`` () =
        expect "Unwrap1" []
        expect "UnwrapInvalid1" [ Error ErrorCode.ExpectingUserDefinedType ]
        expect "UnwrapInvalid2" [ Error ErrorCode.ExpectingUserDefinedType ]

    [<Fact>]
    let ``Supports indexed types`` () =
        expect "Indexed1" []
        expect "Indexed2" []
        expect "Indexed3" []
        expect "Indexed4" []
        expect "IndexedInvalid1" [ Error ErrorCode.ItemAccessForNonArray ]
        expect "IndexedInvalid2" [ Error ErrorCode.ItemAccessForNonArray ]
        expect "IndexedInvalid3" [ Error ErrorCode.InvalidArrayItemIndex ]
        expect "IndexedInvalid4" [ Error ErrorCode.InvalidArrayItemIndex ]
        expect "IndexedInvalid5" [ Error ErrorCode.UnknownIdentifier ]
        expect "IndexedInvalid5" [ Error ErrorCode.UnknownIdentifier ]
        expect "IndexedInvalid6" [ Error ErrorCode.UnknownIdentifier ]

    [<Fact>]
    let ``Supports sized array literals`` () =
        allValid "SizedArray" 4
        expect "SizedArrayInvalid1" [ Error ErrorCode.TypeMismatch ]
        expect "SizedArrayInvalid2" [ Error ErrorCode.TypeMismatch ]
        expect "SizedArrayInvalid3" [ Error ErrorCode.TypeMismatch ]

    [<Fact>]
    let ``Supports lambda expressions`` () =
        allValid "Lambda" 28
        expect "LambdaInvalid1" [ Error ErrorCode.TypeMismatchInReturn ]
        expect "LambdaInvalid2" [ Error ErrorCode.TypeMismatchInReturn ]
        expect "LambdaInvalid3" (Error ErrorCode.InfiniteType |> List.replicate 2)
        expect "LambdaInvalid4" [ Error ErrorCode.MutableClosure ]
        expect "LambdaInvalid5" [ Error ErrorCode.MutableClosure; Error ErrorCode.MutableClosure ]
        expect "LambdaInvalid6" [ Error ErrorCode.LocalVariableAlreadyExists ]
        expect "LambdaInvalid7" [ Error ErrorCode.LocalVariableAlreadyExists ]

    [<Fact>]
    let ``Operation lambda with non-unit return (1)`` () =
        let scope = findSpecScope "Lambda17" QsBody
        let lambda = Seq.exactlyOne scope.Statements.[0].SymbolDeclarations.Variables
        let (_, output), info = getOperationType lambda.Type
        Assert.Equal(Int, output.Resolution)
        Assert.Empty(info.Characteristics.GetProperties())

    [<Fact>]
    let ``Operation lambda with non-unit return (2)`` () =
        let scope = findSpecScope "Lambda18" QsBody
        let lambda = Seq.exactlyOne scope.Statements.[0].SymbolDeclarations.Variables
        let (_, output), info = getOperationType lambda.Type

        let outputs =
            match output.Resolution with
            | TupleType ts -> ts |> Seq.map (fun t -> t.Resolution)
            | _ -> failwith "Not a tuple type."

        Assert.Equal([ UnitType; Int ], outputs)
        Assert.Empty(info.Characteristics.GetProperties())

    [<Fact>]
    let ``Operation lambda with non-unit return (3)`` () =
        let scope = findSpecScope "Lambda19" QsBody
        let lambda = Seq.exactlyOne scope.Statements.[0].SymbolDeclarations.Variables
        let (_, output), info = getOperationType lambda.Type
        Assert.Equal(Int, output.Resolution)
        Assert.Empty(info.Characteristics.GetProperties())

    [<Fact>]
    let ``Operation lambda characteristics intersection (1)`` () =
        let scope = findSpecScope "Lambda20" QsBody
        let lambda = Seq.exactlyOne scope.Statements.[0].SymbolDeclarations.Variables
        let _, info = getOperationType lambda.Type
        let properties = info.Characteristics.GetProperties()
        Assert.True(properties.SetEquals(ImmutableHashSet.Create Adjointable), "Set not equal to Adj.")

    [<Fact>]
    let ``Operation lambda characteristics intersection (2)`` () =
        let scope = findSpecScope "Lambda21" QsBody
        let lambda = Seq.exactlyOne scope.Statements.[0].SymbolDeclarations.Variables
        let _, info = getOperationType lambda.Type
        let properties = info.Characteristics.GetProperties()
        Assert.True(properties.SetEquals(ImmutableHashSet.Create Controllable), "Set not equal to Ctl.")

    [<Fact>]
    let ``Operation lambda characteristics intersection (3)`` () =
        let scope = findSpecScope "Lambda22" QsBody
        let lambda = Seq.exactlyOne scope.Statements.[0].SymbolDeclarations.Variables
        let _, info = getOperationType lambda.Type
        Assert.Empty(info.Characteristics.GetProperties())

type TypeCheckingTests() =
    member private this.Expect name diagnostics =
        TypeCheckingTests.expect name diagnostics


    [<Fact>]
    member this.Variance() =
        this.Expect "Variance1" (Warning WarningCode.DeprecatedNewArray |> List.replicate 5)
        this.Expect "Variance2" [ Error ErrorCode.TypeMismatch; Warning WarningCode.DeprecatedNewArray ]
        this.Expect "Variance3" [ Error ErrorCode.TypeMismatch; Warning WarningCode.DeprecatedNewArray ]
        this.Expect "Variance4" [ Error ErrorCode.TypeMismatch; Warning WarningCode.DeprecatedNewArray ]
        this.Expect "Variance5" [ Error ErrorCode.TypeMismatch; Warning WarningCode.DeprecatedNewArray ]
        this.Expect "Variance6" [ Error ErrorCode.TypeMismatch; Warning WarningCode.DeprecatedNewArray ]

        this.Expect
            "Variance7"
            [
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
            ]

        this.Expect
            "Variance8"
            [
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
                Error ErrorCode.AmbiguousTypeParameterResolution
            ]

        this.Expect "Variance9" [ Error ErrorCode.TypeMismatch ]
        this.Expect "Variance10" [ Error ErrorCode.TypeMismatch ]
        this.Expect "Variance11" [ Error ErrorCode.TypeMismatch ]
        this.Expect "Variance12" [ Error ErrorCode.TypeMismatch ]
        this.Expect "Variance13" [ Error ErrorCode.TypeMismatch ]
        this.Expect "Variance14" [ Error ErrorCode.TypeMismatch ]


    [<Fact>]
    member this.``Common base type``() =
        this.Expect "CommonBaseType1" (Warning WarningCode.DeprecatedNewArray |> List.replicate 2)

        this.Expect
            "CommonBaseType2"
            [
                Error ErrorCode.TypeIntersectionMismatch
                Warning WarningCode.DeprecatedNewArray
                Warning WarningCode.DeprecatedNewArray
            ]

        this.Expect
            "CommonBaseType3"
            [
                Error ErrorCode.TypeIntersectionMismatch
                Warning WarningCode.DeprecatedNewArray
                Warning WarningCode.DeprecatedNewArray
            ]

        this.Expect
            "CommonBaseType4"
            [
                Error ErrorCode.TypeMismatchInReturn
                Warning WarningCode.DeprecatedNewArray
                Warning WarningCode.DeprecatedNewArray
            ]

        this.Expect "CommonBaseType5" []
        this.Expect "CommonBaseType6" []
        this.Expect "CommonBaseType7" []
        this.Expect "CommonBaseType8" [ Error ErrorCode.TypeMismatchInReturn ]
        this.Expect "CommonBaseType9" []
        this.Expect "CommonBaseType10" []
        this.Expect "CommonBaseType11" [ Error ErrorCode.TypeMismatchInReturn ]
        this.Expect "CommonBaseType12" [ Error ErrorCode.TypeIntersectionMismatch ]
        this.Expect "CommonBaseType13" []
        this.Expect "CommonBaseType14" []
        this.Expect "CommonBaseType15" [ Error ErrorCode.TypeMismatchInReturn ]
        this.Expect "CommonBaseType16" [ Error ErrorCode.TypeIntersectionMismatch ]
        this.Expect "CommonBaseType17" []
        this.Expect "CommonBaseType18" []

        this.Expect
            "CommonBaseType19"
            [
                Warning WarningCode.UnusedTypeParam
                Warning WarningCode.DeprecatedNewArray
                Warning WarningCode.DeprecatedNewArray
            ]

        this.Expect
            "CommonBaseType20"
            [
                Error ErrorCode.TypeIntersectionMismatch
                Warning WarningCode.DeprecatedNewArray
                Warning WarningCode.DeprecatedNewArray
            ]

        this.Expect
            "CommonBaseType21"
            [
                Warning WarningCode.DeprecatedNewArray
                Warning WarningCode.DeprecatedNewArray
            ]

        this.Expect "CommonBaseType22" []
        this.Expect "CommonBaseType23" []
        this.Expect "CommonBaseType24" []
        this.Expect "CommonBaseType25" [ Error ErrorCode.TypeIntersectionMismatch ]


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

        this.Expect "NoCommonBaseEquality" [ Error ErrorCode.TypeIntersectionMismatch ]
        this.Expect "NoCommonBaseInequality" [ Error ErrorCode.TypeIntersectionMismatch ]


    [<Fact>]
    member this.``Argument matching``() =
        this.Expect "MatchArgument1" []
        this.Expect "MatchArgument2" []
        this.Expect "MatchArgument3" (Warning WarningCode.DeprecatedNewArray |> List.replicate 2)
        this.Expect "MatchArgument4" []
        this.Expect "MatchArgument5" []
        this.Expect "MatchArgument6" []
        this.Expect "MatchArgument7" []
        this.Expect "MatchArgument8" []
        this.Expect "MatchArgument9" [ Error ErrorCode.TypeMismatch ]
        this.Expect "MatchArgument10" [ Error ErrorCode.TypeMismatch; Error ErrorCode.TypeMismatchInReturn ]
        this.Expect "MatchArgument11" []
        this.Expect "MatchArgument12" []
        this.Expect "MatchArgument13" []
        this.Expect "MatchArgument14" []
        this.Expect "MatchArgument15" []
        this.Expect "MatchArgument16" []
        this.Expect "MatchArgument17" []
        this.Expect "MatchArgument18" []
        this.Expect "MatchArgument19" [ Error ErrorCode.TypeMismatch ]


    [<Fact>]
    member this.``Partial application``() =
        this.Expect "PartialApplication1" []
        this.Expect "PartialApplication2" [ Error ErrorCode.TypeMismatch; Error ErrorCode.TypeMismatch ]
        this.Expect "PartialApplication3" [ Error ErrorCode.TypeMismatch ]
        this.Expect "PartialApplication4" [ Error ErrorCode.TypeMismatch ]
        this.Expect "PartialApplication5" [ Error ErrorCode.TypeMismatch ]
        this.Expect "PartialApplication6" []

        this.Expect
            "PartialApplication7"
            [
                Error ErrorCode.TypeMismatch
                Error ErrorCode.TypeMismatch
                Error ErrorCode.TypeMismatch
            ]

        this.Expect "PartialApplication8" [ Error ErrorCode.TypeMismatch; Error ErrorCode.TypeMismatch ]
        this.Expect "PartialApplication9" []
        this.Expect "PartialApplication10" []
        this.Expect "PartialApplication11" []
        this.Expect "PartialApplication12" []
        this.Expect "PartialApplication13" []
        this.Expect "PartialApplication14" []
        this.Expect "PartialApplication15" [ Error ErrorCode.TypeMismatch; Error ErrorCode.TypeMismatch ]
        this.Expect "PartialApplication16" [ Error ErrorCode.TypeMismatch ]
        this.Expect "PartialApplication17" []
        this.Expect "PartialApplication18" [ Error ErrorCode.OperationCallOutsideOfOperation ]
        this.Expect "PartialApplication19" []
        this.Expect "PartialApplication20" []
        this.Expect "PartialApplication21" []
        this.Expect "PartialApplication22" [ Error ErrorCode.MissingFunctorForAutoGeneration ]
        this.Expect "PartialApplication23" []

        this.Expect
            "PartialApplication24"
            [
                Error ErrorCode.InvalidControlledApplication
                Error ErrorCode.TypeMismatch
                Error ErrorCode.TypeMismatch
            ]

        this.Expect
            "PartialApplication25"
            [
                Error ErrorCode.InvalidControlledApplication
                Error ErrorCode.TypeMismatch
                Error ErrorCode.TypeMismatch
            ]

        this.Expect
            "PartialApplication26"
            [
                Error ErrorCode.InvalidAdjointApplication
                Error ErrorCode.TypeMismatch
                Error ErrorCode.TypeMismatch
                Error ErrorCode.TypeMismatch
            ]

        this.Expect
            "PartialApplication27"
            [
                Error ErrorCode.InvalidAdjointApplication
                Error ErrorCode.TypeMismatch
                Error ErrorCode.TypeMismatch
                Error ErrorCode.TypeMismatch
            ]

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
