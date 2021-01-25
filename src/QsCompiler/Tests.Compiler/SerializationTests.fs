// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// mock-up for the purpose of testing
namespace Microsoft.Quantum.Simulation.Core

open System

[<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
type CallableDeclarationAttribute(serialization: string) =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
type TypeDeclarationAttribute(serialization: string) =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
type SpecializationDeclarationAttribute(serialization: string) =
    inherit Attribute()

// mock-up for the purpose of testing
namespace Microsoft.Quantum.QsCompiler.Attributes

open System

[<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
type CallableDeclarationAttribute(serialization: string) =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
type TypeDeclarationAttribute(serialization: string) =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
type SpecializationDeclarationAttribute(serialization: string) =
    inherit Attribute()


namespace Microsoft.Quantum.QsCompiler.Testing

module SerializationTests =

    open System
    open System.Collections.Immutable
    open System.Reflection
    open Microsoft.Quantum.QsCompiler
    open Microsoft.Quantum.QsCompiler.DataTypes
    open Microsoft.Quantum.QsCompiler.SyntaxExtensions
    open Microsoft.Quantum.QsCompiler.SyntaxTokens
    open Microsoft.Quantum.QsCompiler.SyntaxTree
    open Xunit


    let simpleSignature argType rType props =
        {
            TypeParameters = ImmutableArray.Empty
            ArgumentType = argType |> ResolvedType.New
            ReturnType = rType |> ResolvedType.New
            Information =
                CallableInformation.New
                    (ResolvedCharacteristics.FromProperties props, InferredCallableInformation.NoInformation)
        }

    let varDecl name t (s, e) =
        {
            VariableName = ValidName name
            Type = t |> ResolvedType.New
            InferredInformation = InferredExpressionInformation.New(false, false)
            Position = Null
            Range = Range.Create (Position.Create 0 s) (Position.Create 0 e)
        }

    let tupleIntIntType = TupleType([ Int |> ResolvedType.New; Int |> ResolvedType.New ].ToImmutableArray())

    let intIntTypeItems =
        let intItem = Int |> ResolvedType.New |> Anonymous |> QsTupleItem
        [ intItem; intItem ].ToImmutableArray() |> QsTuple

    let qualifiedName ns name = { Namespace = ns; Name = name }

    let udt name =
        let range = Range.Create (Position.Create 4 9) (Position.Create 4 9) |> Value
        let fullName = qualifiedName "Microsoft.Quantum" name

        {
            Namespace = fullName.Namespace
            Name = fullName.Name
            Range = range
        }
        |> UserDefinedType

    let udtPair = udt "Pair"


    [<Fact>]
    let ``specialization declaration serialization`` () =
        let testOne (decl: SpecializationDeclarationHeader) =
            let json = decl.ToJson()
            let built, header = SpecializationDeclarationHeader.FromJson json
            Assert.True(built)
            Assert.Equal(decl, header)

        let qualifiedName ns name = { Namespace = ns; Name = name }

        {
            Kind = QsSpecializationKind.QsBody
            TypeArguments = Null
            Information = CallableInformation.NoInformation
            Parent = qualifiedName "Microsoft.Quantum" "emptyFunction"
            Attributes = ImmutableArray.Empty
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 4 43 |> DeclarationHeader.Offset.Defined
            HeaderRange = Range.Create (Position.Create 0 0) (Position.Create 0 4) |> DeclarationHeader.Range.Defined
            Documentation = ImmutableArray.Empty
        }
        |> testOne

        {
            Kind = QsSpecializationKind.QsBody
            TypeArguments = Null
            Information =
                CallableInformation.New
                    (ResolvedCharacteristics.FromProperties [ Adjointable
                                                              Controllable ],
                     InferredCallableInformation.NoInformation)
            Parent = qualifiedName "Microsoft.Quantum" "emptyOperation"
            Attributes = ImmutableArray.Empty
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 5 39 |> DeclarationHeader.Offset.Defined
            HeaderRange = Range.Create (Position.Create 0 0) (Position.Create 0 4) |> DeclarationHeader.Range.Defined
            Documentation = [ "Line one"; "Line two" ] |> ImmutableArray.CreateRange
        }
        |> testOne

        {
            Kind = QsSpecializationKind.QsBody
            TypeArguments = Null
            Information =
                CallableInformation.New
                    (ResolvedCharacteristics.Empty, InferredCallableInformation.New(intrinsic = true))
            Parent = qualifiedName "Microsoft.Quantum" "Pair"
            Attributes = ImmutableArray.Empty
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 5 4 |> DeclarationHeader.Offset.Defined
            HeaderRange = Range.Create (Position.Create 0 8) (Position.Create 0 12) |> DeclarationHeader.Range.Defined
            Documentation = ImmutableArray.Empty
        }
        |> testOne

        {
            Kind = QsSpecializationKind.QsBody
            TypeArguments = Null
            Information =
                CallableInformation.New
                    (ResolvedCharacteristics.Empty, InferredCallableInformation.New(intrinsic = true))
            Parent = qualifiedName "Microsoft.Quantum" "Unused"
            Attributes = ImmutableArray.Empty
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 6 4 |> DeclarationHeader.Offset.Defined
            HeaderRange = Range.Create (Position.Create 0 8) (Position.Create 0 14) |> DeclarationHeader.Range.Defined
            Documentation = ImmutableArray.Empty
        }
        |> testOne


    [<Fact>]
    let ``callable declaration serialization`` () =
        let testOne (decl: CallableDeclarationHeader) =
            let json = decl.ToJson()
            let built, header = CallableDeclarationHeader.FromJson json
            Assert.True(built)
            Assert.Equal(decl, header)

        {
            Kind = QsCallableKind.TypeConstructor
            QualifiedName = qualifiedName "Microsoft.Quantum" "Pair"
            Attributes = ImmutableArray.Empty
            Visibility = Public
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 2 4 |> DeclarationHeader.Offset.Defined
            SymbolRange = Range.Create (Position.Create 0 8) (Position.Create 0 12) |> DeclarationHeader.Range.Defined
            ArgumentTuple =
                [
                    varDecl "__Item1__" Int (1, 1) |> QsTupleItem
                    varDecl "__Item2__" Int (1, 1) |> QsTupleItem
                ]
                    .ToImmutableArray()
                |> QsTuple
            Signature = simpleSignature tupleIntIntType udtPair []
            Documentation = ImmutableArray.Create("type constructor for user defined type")
        }
        |> testOne

        {
            Kind = QsCallableKind.Function
            QualifiedName = qualifiedName "Microsoft.Quantum" "emptyFunction"
            Attributes = ImmutableArray.Empty
            Visibility = Public
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 4 4 |> DeclarationHeader.Offset.Defined
            SymbolRange = Range.Create (Position.Create 0 9) (Position.Create 0 22) |> DeclarationHeader.Range.Defined
            ArgumentTuple = [ varDecl "p" udtPair (25, 26) |> QsTupleItem ].ToImmutableArray() |> QsTuple
            Signature = simpleSignature udtPair UnitType []
            Documentation = ImmutableArray.Empty
        }
        |> testOne

        {
            Kind = QsCallableKind.Operation
            QualifiedName = qualifiedName "Microsoft.Quantum" "emptyOperation"
            Attributes = ImmutableArray.Empty
            Visibility = Public
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 5 4 |> DeclarationHeader.Offset.Defined
            SymbolRange = Range.Create (Position.Create 0 10) (Position.Create 0 24) |> DeclarationHeader.Range.Defined
            ArgumentTuple = [].ToImmutableArray() |> QsTuple
            Signature = simpleSignature UnitType UnitType [ Adjointable; Controllable ]
            Documentation = ImmutableArray.Empty
        }
        |> testOne

        {
            Kind = QsCallableKind.TypeConstructor
            QualifiedName = qualifiedName "Microsoft.Quantum" "Unused"
            Attributes = ImmutableArray.Empty
            Visibility = Public
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 3 4 |> DeclarationHeader.Offset.Defined
            SymbolRange = Range.Create (Position.Create 0 8) (Position.Create 0 14) |> DeclarationHeader.Range.Defined
            ArgumentTuple =
                [
                    varDecl "__Item1__" Int (1, 1) |> QsTupleItem
                    varDecl "__Item2__" Int (1, 1) |> QsTupleItem
                ]
                    .ToImmutableArray()
                |> QsTuple
            Signature = simpleSignature tupleIntIntType (udt "Unused") []
            Documentation = ImmutableArray.Create("type constructor for user defined type")
        }
        |> testOne


    [<Fact>]
    let ``type declaration serialization`` () =
        let testOne (decl: TypeDeclarationHeader) =
            let json = decl.ToJson()
            let built, header = TypeDeclarationHeader.FromJson json
            Assert.True(built)
            Assert.Equal(decl, header)

        {
            QualifiedName = qualifiedName "Microsoft.Quantum" "Pair"
            Attributes = ImmutableArray.Empty
            Visibility = Public
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 2 4 |> DeclarationHeader.Offset.Defined
            SymbolRange = Range.Create (Position.Create 0 8) (Position.Create 0 12) |> DeclarationHeader.Range.Defined
            Type = tupleIntIntType |> ResolvedType.New
            TypeItems = intIntTypeItems
            Documentation = ImmutableArray.Empty
        }
        |> testOne

        {
            QualifiedName = qualifiedName "Microsoft.Quantum" "Unused"
            Attributes = ImmutableArray.Empty
            Visibility = Public
            Source = { CodeFile = "Test.qs"; AssemblyFile = Null }
            Position = Position.Create 3 4 |> DeclarationHeader.Offset.Defined
            SymbolRange = Range.Create (Position.Create 0 8) (Position.Create 0 14) |> DeclarationHeader.Range.Defined
            Type = tupleIntIntType |> ResolvedType.New
            TypeItems = intIntTypeItems
            Documentation = ImmutableArray.Empty
        }
        |> testOne

    [<Literal>]
    let CALLABLE_1 =
        "{\"Kind\":{\"Case\":\"TypeConstructor\"},\"QualifiedName\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\"},\"Attributes\":[],\"Modifiers\":{\"Access\":{\"Case\":\"DefaultAccess\"}},\"SourceFile\":\"Test.qs\",\"Position\":{\"Item1\":2,\"Item2\":4},\"SymbolRange\":{\"Item1\":{\"Line\":1,\"Column\":9},\"Item2\":{\"Line\":1,\"Column\":13}},\"ArgumentTuple\":{\"Case\":\"QsTuple\",\"Fields\":[[{\"Case\":\"QsTupleItem\",\"Fields\":[{\"VariableName\":{\"Case\":\"ValidName\",\"Fields\":[\"__Item1__\"]},\"Type\":{\"Case\":\"Int\"},\"InferredInformation\":{\"IsMutable\":false,\"HasLocalQuantumDependency\":false},\"Position\":{\"Case\":\"Null\"},\"Range\":{\"Item1\":{\"Line\":1,\"Column\":1},\"Item2\":{\"Line\":1,\"Column\":1}}}]},{\"Case\":\"QsTupleItem\",\"Fields\":[{\"VariableName\":{\"Case\":\"ValidName\",\"Fields\":[\"__Item2__\"]},\"Type\":{\"Case\":\"Int\"},\"InferredInformation\":{\"IsMutable\":false,\"HasLocalQuantumDependency\":false},\"Position\":{\"Case\":\"Null\"},\"Range\":{\"Item1\":{\"Line\":1,\"Column\":1},\"Item2\":{\"Line\":1,\"Column\":1}}}]}]]},\"Signature\":{\"TypeParameters\":[],\"ArgumentType\":{\"Case\":\"TupleType\",\"Fields\":[[{\"Case\":\"Int\"},{\"Case\":\"Int\"}]]},\"ReturnType\":{\"Case\":\"UserDefinedType\",\"Fields\":[{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\",\"Range\":{\"Case\":\"Null\"}}]},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":true}}},\"Documentation\":[\"type constructor for user defined type\"]}"

    [<Literal>]
    let TYPE_1 =
        "{\"QualifiedName\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\"},\"Attributes\":[],\"Modifiers\":{\"Access\":{\"Case\":\"DefaultAccess\"}},\"SourceFile\":\"Test.qs\",\"Position\":{\"Item1\":2,\"Item2\":4},\"SymbolRange\":{\"Item1\":{\"Line\":1,\"Column\":9},\"Item2\":{\"Line\":1,\"Column\":13}},\"Type\":{\"Case\":\"TupleType\",\"Fields\":[[{\"Case\":\"Int\"},{\"Case\":\"Int\"}]]},\"TypeItems\":{\"Case\":\"QsTuple\",\"Fields\":[[{\"Case\":\"QsTupleItem\",\"Fields\":[{\"Case\":\"Anonymous\",\"Fields\":[{\"Case\":\"Int\"}]}]},{\"Case\":\"QsTupleItem\",\"Fields\":[{\"Case\":\"Anonymous\",\"Fields\":[{\"Case\":\"Int\"}]}]}]]},\"Documentation\":[]}"

    [<Literal>]
    let CALLABLE_2 =
        "{\"Kind\":{\"Case\":\"Function\"},\"QualifiedName\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"emptyFunction\"},\"Attributes\":[],\"Modifiers\":{\"Access\":{\"Case\":\"DefaultAccess\"}},\"SourceFile\":\"Test.qs\",\"Position\":{\"Item1\":4,\"Item2\":4},\"SymbolRange\":{\"Item1\":{\"Line\":1,\"Column\":10},\"Item2\":{\"Line\":1,\"Column\":23}},\"ArgumentTuple\":{\"Case\":\"QsTuple\",\"Fields\":[[{\"Case\":\"QsTupleItem\",\"Fields\":[{\"VariableName\":{\"Case\":\"ValidName\",\"Fields\":[\"p\"]},\"Type\":{\"Case\":\"UserDefinedType\",\"Fields\":[{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\",\"Range\":{\"Case\":\"Null\"}}]},\"InferredInformation\":{\"IsMutable\":false,\"HasLocalQuantumDependency\":false},\"Position\":{\"Case\":\"Null\"},\"Range\":{\"Item1\":{\"Line\":1,\"Column\":25},\"Item2\":{\"Line\":1,\"Column\":26}}}]}]]},\"Signature\":{\"TypeParameters\":[],\"ArgumentType\":{\"Case\":\"UserDefinedType\",\"Fields\":[{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\",\"Range\":{\"Case\":\"Null\"}}]},\"ReturnType\":{\"Case\":\"UnitType\"},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":false}}},\"Documentation\":[]}"

    [<Literal>]
    let SPECIALIZATION_1 =
        "{\"Kind\":{\"Case\":\"QsBody\"},\"TypeArguments\":{\"Case\":\"Null\"},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":false}},\"Parent\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"emptyFunction\"},\"Attributes\":[],\"SourceFile\":\"Test.qs\",\"Position\":{\"Item1\":4,\"Item2\":43},\"HeaderRange\":{\"Item1\":{\"Line\":1,\"Column\":1},\"Item2\":{\"Line\":1,\"Column\":5}},\"Documentation\":[]}"

    [<Literal>]
    let CALLABLE_3 =
        "{\"Kind\":{\"Case\":\"Operation\"},\"QualifiedName\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"emptyOperation\"},\"Attributes\":[],\"Modifiers\":{\"Access\":{\"Case\":\"DefaultAccess\"}},\"SourceFile\":\"Test.qs\",\"Position\":{\"Item1\":5,\"Item2\":4},\"SymbolRange\":{\"Item1\":{\"Line\":1,\"Column\":11},\"Item2\":{\"Line\":1,\"Column\":25}},\"ArgumentTuple\":{\"Case\":\"QsTuple\",\"Fields\":[[]]},\"Signature\":{\"TypeParameters\":[],\"ArgumentType\":{\"Case\":\"UnitType\"},\"ReturnType\":{\"Case\":\"UnitType\"},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":false}}},\"Documentation\":[]}"

    [<Literal>]
    let SPECIALIZATION_3 =
        "{\"Kind\":{\"Case\":\"QsBody\"},\"TypeArguments\":{\"Case\":\"Null\"},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":false}},\"Parent\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"emptyOperation\"},\"Attributes\":[],\"SourceFile\":\"Test.qs\",\"Position\":{\"Item1\":5,\"Item2\":39},\"HeaderRange\":{\"Item1\":{\"Line\":1,\"Column\":1},\"Item2\":{\"Line\":1,\"Column\":5}},\"Documentation\":[]}"


    [<Fact>]
    let ``attribute reader`` () =
        let dllUri = Assembly.GetExecutingAssembly().Location |> Uri
        let mutable attrs = null
        let loadedFromResource = AssemblyLoader.LoadReferencedAssembly(dllUri, &attrs, false)

        Assert.False
            (loadedFromResource,
             "loading should indicate failure when headers are loaded based on attributes rather than resources")

        let callables = attrs.Callables |> Seq.map (fun c -> c.ToJson()) |> Seq.toList
        let types = attrs.Types |> Seq.map (fun t -> t.ToJson()) |> Seq.toList
        let specs = attrs.Specializations |> Seq.map (fun s -> (s.ToTuple() |> fst).ToJson()) |> Seq.toList

        let AssertEqual expected got =
            List.zip expected got |> List.iter Assert.Equal

        AssertEqual [ CALLABLE_1; CALLABLE_2; CALLABLE_3 ] callables
        AssertEqual [ SPECIALIZATION_1; SPECIALIZATION_3 ] specs
        AssertEqual [ TYPE_1 ] types


    // These attributes are used to test the AttributeReader.
    [<assembly: Microsoft.Quantum.Simulation.Core.CallableDeclaration(CALLABLE_1)>]
    [<assembly: Microsoft.Quantum.Simulation.Core.TypeDeclaration(TYPE_1)>]
    [<assembly: Microsoft.Quantum.Simulation.Core.CallableDeclaration(CALLABLE_2)>]
    [<assembly: Microsoft.Quantum.Simulation.Core.SpecializationDeclaration(SPECIALIZATION_1)>]
    [<assembly: Attributes.CallableDeclaration(CALLABLE_3)>]
    [<assembly: Attributes.SpecializationDeclaration(SPECIALIZATION_3)>]
    do ()
