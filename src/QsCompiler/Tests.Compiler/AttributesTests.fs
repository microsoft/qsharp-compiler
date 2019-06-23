// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.AttributesTests

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open System.Reflection
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens 
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit


let simpleSignature argType rType props = {
    TypeParameters = ImmutableArray.Empty
    ArgumentType = argType |> ResolvedType.New
    ReturnType = rType |> ResolvedType.New
    Information = CallableInformation.New(ResolvedCharacteristics.FromProperties props, InferredCallableInformation.NoInformation)
}

let varDecl name t (s, e) = {
    VariableName = name |> NonNullable<string>.New |> ValidName
    Type = t |> ResolvedType.New
    InferredInformation = InferredExpressionInformation.New (false, false)
    Position = Null
    Range = {Line = 1; Column = s}, {Line = 1; Column = e}
}

let tupleIntIntType = TupleType ([Int |> ResolvedType.New; Int |> ResolvedType.New].ToImmutableArray())
let intIntTypeItems = 
    let intItem = Int |> ResolvedType.New |> Anonymous |> QsTupleItem 
    [intItem; intItem].ToImmutableArray() |> QsTuple
let qualifiedName ns name = {Namespace = ns |> NonNullable<string>.New; Name = name |> NonNullable<string>.New}
let udt name = 
    let range = ({Line = 5; Column = 10}, {Line = 5; Column = 10}) |> Value
    let fullName = qualifiedName "Microsoft.Quantum" name 
    {Namespace = fullName.Namespace; Name = fullName.Name; Range = range} |> UserDefinedType
let udtPair = udt "Pair"


[<Fact>]
let ``specialization declaration serialization`` () =
    let testOne (decl:SpecializationDeclarationHeader) =
        let json = decl.ToJson()
        let built, header = SpecializationDeclarationHeader.FromJson json
        Assert.True(built)
        Assert.Equal(decl, header)

    let qualifiedName ns name = {Namespace = ns |> NonNullable<string>.New; Name = name |> NonNullable<string>.New}
    {
        Kind            = QsSpecializationKind.QsBody
        TypeArguments   = Null
        Information     = CallableInformation.NoInformation
        Parent          = qualifiedName "Microsoft.Quantum" "emptyFunction"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (4,43)
        HeaderRange     = {Line = 1; Column = 1}, {Line = 1; Column = 5}
        Documentation   = ImmutableArray.Empty
    } 
    |> testOne

    {
        Kind            = QsSpecializationKind.QsBody
        TypeArguments   = Null
        Information     = CallableInformation.New(ResolvedCharacteristics.FromProperties [Adjointable; Controllable], InferredCallableInformation.NoInformation)
        Parent          = qualifiedName "Microsoft.Quantum" "emptyOperation"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (5,39)
        HeaderRange     = {Line = 1; Column = 1}, {Line = 1; Column = 5}
        Documentation   = [ "Line one"; "Line two" ] |> ImmutableArray.CreateRange
    } 
    |> testOne

    {
        Kind            = QsSpecializationKind.QsBody
        TypeArguments   = Null
        Information     = CallableInformation.New(ResolvedCharacteristics.Empty, InferredCallableInformation.New (intrinsic = true))
        Parent          = qualifiedName "Microsoft.Quantum" "Pair"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (5,4)
        HeaderRange     = {Line = 1; Column = 9}, {Line = 1; Column = 13}
        Documentation   = ImmutableArray.Empty
    }
    |> testOne

    {
        Kind            = QsSpecializationKind.QsBody
        TypeArguments   = Null
        Information     = CallableInformation.New(ResolvedCharacteristics.Empty, InferredCallableInformation.New (intrinsic = true))
        Parent          = qualifiedName "Microsoft.Quantum" "Unused"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (6,4)
        HeaderRange     = {Line = 1; Column = 9}, {Line = 1; Column = 15}
        Documentation   = ImmutableArray.Empty
    }
    |> testOne
    

[<Fact>]
let ``callable declaration serialization`` () =
    let testOne (decl:CallableDeclarationHeader) =
        let json = decl.ToJson()
        let built, header = CallableDeclarationHeader.FromJson json
        Assert.True(built)
        Assert.Equal(decl, header)
        
    {
        Kind            = QsCallableKind.TypeConstructor
        QualifiedName   = qualifiedName "Microsoft.Quantum" "Pair"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (2,4)
        SymbolRange     = {Line = 1; Column = 9}, {Line = 1; Column = 13}
        ArgumentTuple   = [varDecl "__Item1__" Int (1,1) |> QsTupleItem; varDecl "__Item2__" Int (1,1) |> QsTupleItem].ToImmutableArray() |> QsTuple
        Signature       = simpleSignature tupleIntIntType udtPair [] 
        Documentation   = ImmutableArray.Create("type constructor for user defined type") 
    } 
    |> testOne

    {
        Kind            = QsCallableKind.Function
        QualifiedName   = qualifiedName "Microsoft.Quantum" "emptyFunction"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (4,4)
        SymbolRange     = {Line = 1; Column = 10}, {Line = 1; Column = 23} 
        ArgumentTuple   = [ varDecl "p" udtPair (25,26) |> QsTupleItem].ToImmutableArray() |> QsTuple
        Signature       = simpleSignature udtPair UnitType []
        Documentation   = ImmutableArray.Empty
    } 
    |> testOne

    {
        Kind            = QsCallableKind.Operation
        QualifiedName   = qualifiedName "Microsoft.Quantum" "emptyOperation"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (5,4)
        SymbolRange     = {Line = 1; Column = 11}, {Line = 1; Column = 25}
        ArgumentTuple   = [].ToImmutableArray() |> QsTuple
        Signature       = simpleSignature UnitType UnitType [Adjointable; Controllable]
        Documentation   = ImmutableArray.Empty
    } 
    |> testOne

    {
        Kind            = QsCallableKind.TypeConstructor
        QualifiedName   = qualifiedName "Microsoft.Quantum" "Unused"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (3,4)
        SymbolRange     = {Line = 1; Column = 9}, {Line = 1; Column = 15}
        ArgumentTuple   = [varDecl "__Item1__" Int (1,1) |> QsTupleItem; varDecl "__Item2__" Int (1,1) |> QsTupleItem].ToImmutableArray() |> QsTuple
        Signature       = simpleSignature tupleIntIntType (udt "Unused") []
        Documentation   = ImmutableArray.Create("type constructor for user defined type")
    } 
    |> testOne

    
[<Fact>]
let ``type declaration serialization`` () =
    let testOne (decl:TypeDeclarationHeader) =
        let json = decl.ToJson()
        let built, header = TypeDeclarationHeader.FromJson json
        Assert.True(built)
        Assert.Equal(decl, header)
    {
        QualifiedName   = qualifiedName "Microsoft.Quantum" "Pair"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (2,4)
        SymbolRange     = {Line = 1; Column = 9}, {Line = 1; Column = 13}
        Type            = tupleIntIntType |> ResolvedType.New
        TypeItems       = intIntTypeItems
        Documentation   = ImmutableArray.Empty
    }
    |> testOne

    {
        QualifiedName   = qualifiedName "Microsoft.Quantum" "Unused"
        SourceFile      = "%%%" |> NonNullable<string>.New
        Position        = (3,4)
        SymbolRange     = {Line = 1; Column = 9}, {Line = 1; Column = 15}
        Type            = tupleIntIntType |> ResolvedType.New
        TypeItems       = intIntTypeItems
        Documentation   = ImmutableArray.Empty
    }
    |> testOne
    
[<Literal>]
let CALLABLE_1 = "{\"Kind\":{\"Case\":\"TypeConstructor\"},\"QualifiedName\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\"},\"SourceFile\":\"%%%\",\"Position\":{\"Item1\":2,\"Item2\":4},\"SymbolRange\":{\"Item1\":{\"Line\":1,\"Column\":9},\"Item2\":{\"Line\":1,\"Column\":13}},\"ArgumentTuple\":{\"Case\":\"QsTuple\",\"Fields\":[[{\"Case\":\"QsTupleItem\",\"Fields\":[{\"VariableName\":{\"Case\":\"ValidName\",\"Fields\":[\"__Item1__\"]},\"Type\":{\"Case\":\"Int\"},\"InferredInformation\":{\"IsMutable\":false,\"HasLocalQuantumDependency\":false},\"Position\":{\"Case\":\"Null\"},\"Range\":{\"Item1\":{\"Line\":1,\"Column\":1},\"Item2\":{\"Line\":1,\"Column\":1}}}]},{\"Case\":\"QsTupleItem\",\"Fields\":[{\"VariableName\":{\"Case\":\"ValidName\",\"Fields\":[\"__Item2__\"]},\"Type\":{\"Case\":\"Int\"},\"InferredInformation\":{\"IsMutable\":false,\"HasLocalQuantumDependency\":false},\"Position\":{\"Case\":\"Null\"},\"Range\":{\"Item1\":{\"Line\":1,\"Column\":1},\"Item2\":{\"Line\":1,\"Column\":1}}}]}]]},\"Signature\":{\"TypeParameters\":[],\"ArgumentType\":{\"Case\":\"TupleType\",\"Fields\":[[{\"Case\":\"Int\"},{\"Case\":\"Int\"}]]},\"ReturnType\":{\"Case\":\"UserDefinedType\",\"Fields\":[{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\",\"Range\":{\"Case\":\"Null\"}}]},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":true}}},\"Documentation\":[\"type constructor for user defined type\"]}"
[<Literal>]
let TYPE_1 = "{\"QualifiedName\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\"},\"SourceFile\":\"%%%\",\"Position\":{\"Item1\":2,\"Item2\":4},\"SymbolRange\":{\"Item1\":{\"Line\":1,\"Column\":9},\"Item2\":{\"Line\":1,\"Column\":13}},\"Type\":{\"Case\":\"TupleType\",\"Fields\":[[{\"Case\":\"Int\"},{\"Case\":\"Int\"}]]},\"TypeItems\":{\"Case\":\"QsTuple\",\"Fields\":[[{\"Case\":\"QsTupleItem\",\"Fields\":[{\"Case\":\"Anonymous\",\"Fields\":[{\"Case\":\"Int\"}]}]},{\"Case\":\"QsTupleItem\",\"Fields\":[{\"Case\":\"Anonymous\",\"Fields\":[{\"Case\":\"Int\"}]}]}]]},\"Documentation\":[]}"
[<Literal>]
let CALLABLE_2 = "{\"Kind\":{\"Case\":\"Function\"},\"QualifiedName\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"emptyFunction\"},\"SourceFile\":\"%%%\",\"Position\":{\"Item1\":4,\"Item2\":4},\"SymbolRange\":{\"Item1\":{\"Line\":1,\"Column\":10},\"Item2\":{\"Line\":1,\"Column\":23}},\"ArgumentTuple\":{\"Case\":\"QsTuple\",\"Fields\":[[{\"Case\":\"QsTupleItem\",\"Fields\":[{\"VariableName\":{\"Case\":\"ValidName\",\"Fields\":[\"p\"]},\"Type\":{\"Case\":\"UserDefinedType\",\"Fields\":[{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\"}]},\"InferredInformation\":{\"IsMutable\":false,\"HasLocalQuantumDependency\":false},\"Position\":{\"Case\":\"Null\"},\"Range\":{\"Item1\":{\"Line\":1,\"Column\":25},\"Item2\":{\"Line\":1,\"Column\":26}}}]}]]},\"Signature\":{\"TypeParameters\":[],\"ArgumentType\":{\"Case\":\"UserDefinedType\",\"Fields\":[{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"Pair\"}]},\"ReturnType\":{\"Case\":\"UnitType\"},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":false}}},\"Documentation\":[]}"
[<Literal>]
let SPECIALIZATION_1 = "{\"Kind\":{\"Case\":\"QsBody\"},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":false}},\"Parent\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"emptyFunction\"},\"SourceFile\":\"%%%\",\"Position\":{\"Item1\":4,\"Item2\":43},\"HeaderRange\":{\"Item1\":{\"Line\":1,\"Column\":1},\"Item2\":{\"Line\":1,\"Column\":5}},\"Documentation\":[]}"
[<Literal>]
let CALLABLE_3 = "{\"Kind\":{\"Case\":\"Operation\"},\"QualifiedName\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"emptyOperation\"},\"SourceFile\":\"%%%\",\"Position\":{\"Item1\":5,\"Item2\":4},\"SymbolRange\":{\"Item1\":{\"Line\":1,\"Column\":11},\"Item2\":{\"Line\":1,\"Column\":25}},\"ArgumentTuple\":{\"Case\":\"QsTuple\",\"Fields\":[[]]},\"Signature\":{\"TypeParameters\":[],\"ArgumentType\":{\"Case\":\"UnitType\"},\"ReturnType\":{\"Case\":\"UnitType\"},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":false}}},\"Documentation\":[]}"
[<Literal>]
let SPECIALIZATION_3 = "{\"Kind\":{\"Case\":\"QsBody\"},\"Information\":{\"Characteristics\":{\"Case\":\"EmptySet\"},\"InferredInformation\":{\"IsSelfAdjoint\":false,\"IsIntrinsic\":false}},\"Parent\":{\"Namespace\":\"Microsoft.Quantum\",\"Name\":\"emptyOperation\"},\"SourceFile\":\"%%%\",\"Position\":{\"Item1\":5,\"Item2\":39},\"HeaderRange\":{\"Item1\":{\"Line\":1,\"Column\":1},\"Item2\":{\"Line\":1,\"Column\":5}},\"Documentation\":[]}"

    
[<Fact>]
let ``attribute reader`` () =
    let path = Assembly.GetExecutingAssembly().Location
    let uri = new Uri(path)
    let attrs = AttributeReader.GetQsCompilerAttributes uri |> Seq.toArray
    Assert.Equal(6, attrs.Length)

    [
        CALLABLE_1
        TYPE_1
        CALLABLE_2
        SPECIALIZATION_1
        CALLABLE_3
        SPECIALIZATION_3
    ] 
    |> List.iteri (fun i expected -> 
        let struct (_, actual) = attrs.[i]
        Assert.Equal (expected, actual)
    )


// These attributes are used to test the AttributeReader.
[<assembly: Microsoft.Quantum.Simulation.Core.CallableDeclaration(CALLABLE_1)>]
[<assembly: Microsoft.Quantum.Simulation.Core.TypeDeclaration(TYPE_1)>]
[<assembly: Microsoft.Quantum.Simulation.Core.CallableDeclaration(CALLABLE_2)>]
[<assembly: Microsoft.Quantum.Simulation.Core.SpecializationDeclaration(SPECIALIZATION_1)>]
[<assembly: Attributes.CallableDeclaration(CALLABLE_3)>]
[<assembly: Attributes.SpecializationDeclaration(SPECIALIZATION_3)>]
do ()


[<Fact>]
let ``referenced declarations`` () =
    let path = Assembly.GetExecutingAssembly().Location
    let uri = new Uri(path)
    let attrs = AttributeReader.GetQsCompilerAttributes uri
    match References.TryInitializeFrom [KeyValuePair(uri, attrs.ToImmutableArray())] with 
    | built, refs, errs -> 
        Assert.True(built)
        Assert.Empty(errs)
        Assert.True(refs.Declarations.Count = 1); 
        let declarations = refs.Declarations.Single().Value
        Assert.Equal(3, declarations.Callables.Length)
        Assert.Equal(1, declarations.Types.Length)
        Assert.Equal(2, declarations.Specializations.Length)
        
