// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.Signatures

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open System.Collections.Generic
open System.Collections.Immutable
open Xunit

let private baseTypes =
    [|
        "Unit", UnitType
        "Int", Int
        "Double", Double
        "String", String
        "Result", Result
        "Qubit", Qubit
    |]

let private makeTypeParam originNs originName paramName =
    originName + "." + paramName,
    QsTypeParameter.New({ Namespace = originNs; Name = originName }, paramName) |> TypeParameter

let private makeArrayType baseTypeName baseType =
    baseTypeName + "[]", ResolvedType.New baseType |> ArrayType

let private makeTupleType tupleItems =
    let names, types = List.unzip tupleItems

    names
    |> String.concat ", "
    |> fun x -> "(" + x + ")", types |> List.map ResolvedType.New |> (fun x -> x.ToImmutableArray() |> TupleType)

let private makeTypeMap extraTypes =
    Array.concat [ baseTypes; extraTypes ] |> Seq.map (fun (k, v) -> k, ResolvedType.New v) |> dict

let private defaultTypes = makeTypeMap [||]

let private makeSig input (typeMap: IDictionary<string, ResolvedType>) =
    let ns, name, args, rtrn = input
    let fullName = { Namespace = ns; Name = name }

    let argType =
        if Array.isEmpty args then
            typeMap.["Unit"]
        else
            args
            |> Seq.map (fun typ -> typeMap.[typ])
            |> ImmutableArray.ToImmutableArray
            |> QsTypeKind.TupleType
            |> ResolvedType.New

    let returnType = typeMap.[rtrn]
    (fullName, argType, returnType)

let private makeSignatures sigs =
    sigs |> Seq.map (fun (types, case) -> Seq.map (fun _sig -> makeSig _sig types) case) |> Seq.toArray

/// For all given namespaces in checkedNamespaces, checks that there are exactly
/// the callables specified with targetSignatures in the given compilation.
let public SignatureCheck checkedNamespaces targetSignatures compilation =

    let getNs targetNs =
        match Seq.tryFind (fun (ns: QsNamespace) -> ns.Name = targetNs) compilation.Namespaces with
        | Some ns -> ns
        | None -> sprintf "Expected but did not find namespace: %s" targetNs |> failwith

    let mutable callableSigs =
        checkedNamespaces
        |> Seq.map getNs
        |> SyntaxTreeExtensions.Callables
        |> Seq.map (fun call ->
            (call.FullName,
             StripPositionInfo.Apply call.Signature.ArgumentType,
             StripPositionInfo.Apply call.Signature.ReturnType))

    let doesCallMatchSig call signature =
        let (callFullName: QsQualifiedName), callArgType, callRtrnType = call
        let (sigFullName: QsQualifiedName), sigArgType, sigRtrnType = signature

        callFullName.Namespace = sigFullName.Namespace
        && callFullName.Name.EndsWith sigFullName.Name
        && callArgType = sigArgType
        && callRtrnType = sigRtrnType

    let makeArgsString (args: ResolvedType) =
        match args.Resolution with
        | QsTypeKind.UnitType -> "()"
        | _ -> args |> SyntaxTreeToQsharp.Default.ToCode

    let removeAt i lst =
        Seq.append <| Seq.take i lst <| Seq.skip (i + 1) lst

    (*Tests that all target signatures are present*)
    for targetSig in targetSignatures do
        let sigFullName, sigArgType, sigRtrnType = targetSig

        callableSigs
        |> Seq.tryFindIndex (fun callSig -> doesCallMatchSig callSig targetSig)
        |> (fun x ->
            Assert.True(
                x <> None,
                sprintf
                    "Expected but did not find: %s.*%s %s : %A"
                    sigFullName.Namespace
                    sigFullName.Name
                    (makeArgsString sigArgType)
                    sigRtrnType.Resolution
            )

            callableSigs <- removeAt x.Value callableSigs)

    (*Tests that *only* targeted signatures are present*)
    for callSig in callableSigs do
        let sigFullName, sigArgType, sigRtrnType = callSig

        failwith (
            sprintf
                "Found unexpected callable: %O %s : %A"
                sigFullName
                (makeArgsString sigArgType)
                sigRtrnType.Resolution
        )

/// Names of several testing namespaces
let public MonomorphizationNS = "Microsoft.Quantum.Testing.Monomorphization"
let public GenericsNS = "Microsoft.Quantum.Testing.Generics"
let public IntrinsicResolutionNS = "Microsoft.Quantum.Testing.IntrinsicResolution"
let public ClassicalControlNS = "Microsoft.Quantum.Testing.ClassicalControl"
let public LambdaLiftingNS = "Microsoft.Quantum.Testing.LambdaLifting"
let public OutputRecordingNS = "Microsoft.Quantum.Testing.OutputRecording"
let public InternalRenamingNS = "Microsoft.Quantum.Testing.InternalRenaming"
let public CycleDetectionNS = "Microsoft.Quantum.Testing.CycleDetection"
let public PopulateCallGraphNS = "Microsoft.Quantum.Testing.PopulateCallGraph"
let public SyntaxTreeTrimmingNS = "Microsoft.Quantum.Testing.SyntaxTreeTrimming"

let private defaultWithQubitArray = makeTypeMap [| makeArrayType "Qubit" Qubit |]

/// Expected callable signatures to be found when running Monomorphization tests
let public MonomorphizationSignatures =
    [| // Test Case 1
        (defaultTypes,
         [|
             MonomorphizationNS, "Test1", [||], "Unit"
             GenericsNS, "Test1Main", [||], "Unit"

             GenericsNS, "BasicGeneric", [| "Double"; "Int" |], "Unit"
             GenericsNS, "BasicGeneric", [| "String"; "String" |], "Unit"
             GenericsNS, "BasicGeneric", [| "Unit"; "Unit" |], "Unit"
             GenericsNS, "BasicGeneric", [| "String"; "Double" |], "Unit"
             GenericsNS, "BasicGeneric", [| "Int"; "Double" |], "Unit"
             GenericsNS, "NoArgsGeneric", [||], "Double"
             GenericsNS, "ReturnGeneric", [| "Double"; "String"; "Int" |], "Int"
             GenericsNS, "ReturnGeneric", [| "String"; "Int"; "String" |], "String"
         |])
        // Test Case 2
        (defaultTypes,
         [|
             MonomorphizationNS, "Test2", [||], "Unit"
             GenericsNS, "Test2Main", [||], "Unit"

             GenericsNS, "ArrayGeneric", [| "Qubit"; "String" |], "Int"
             GenericsNS, "ArrayGeneric", [| "Qubit"; "Int" |], "Int"
             GenericsNS, "GenericCallsGeneric", [| "Qubit"; "Int" |], "Unit"
         |])
        // Test Case 3
        (defaultWithQubitArray,
         [|
             MonomorphizationNS, "Test3", [||], "Unit"
             GenericsNS, "Test3Main", [||], "Unit"

             GenericsNS, "GenericCallsSpecializations", [| "Double"; "String"; "Qubit[]" |], "Unit"
             GenericsNS, "GenericCallsSpecializations", [| "Double"; "String"; "Double" |], "Unit"
             GenericsNS, "GenericCallsSpecializations", [| "String"; "Int"; "Unit" |], "Unit"

             GenericsNS, "BasicGeneric", [| "Double"; "String" |], "Unit"
             GenericsNS, "BasicGeneric", [| "String"; "Qubit[]" |], "Unit"
             GenericsNS, "BasicGeneric", [| "String"; "Int" |], "Unit"
             GenericsNS, "BasicGeneric", [| "Qubit[]"; "Qubit[]" |], "Unit"
             GenericsNS, "BasicGeneric", [| "Qubit[]"; "Double" |], "Unit"
             GenericsNS, "BasicGeneric", [| "Qubit[]"; "Unit" |], "Unit"
             GenericsNS, "BasicGeneric", [| "String"; "Double" |], "Unit"
             GenericsNS, "BasicGeneric", [| "Int"; "Unit" |], "Unit"

             GenericsNS, "ArrayGeneric", [| "Qubit"; "Double" |], "Int"
             GenericsNS, "ArrayGeneric", [| "Qubit"; "Qubit[]" |], "Int"
             GenericsNS, "ArrayGeneric", [| "Qubit"; "Unit" |], "Int"
         |])
        // Test Case 4
        (defaultTypes,
         [|
             MonomorphizationNS, "Test4", [||], "Unit"
             GenericsNS, "Test4Main", [||], "Unit"

             GenericsNS, "_GenericCallsSelf", [||], "Unit"
             GenericsNS, "_GenericCallsSelf2", [| "Double" |], "Unit"
         |])
    |]
    |> makeSignatures

let private intrinsicResolutionTypes =
    [|
        "TestType", UserDefinedType.New(IntrinsicResolutionNS, "TestType") |> UserDefinedType
    |]
    |> makeTypeMap

/// Expected callable signatures to be found when running Intrinsic Resolution tests
let public IntrinsicResolutionSignatures =
    [|
        (defaultTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest1", [||], "Unit"
             IntrinsicResolutionNS, "LocalIntrinsic", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "Unit"
             IntrinsicResolutionNS, "EnvironmentIntrinsic", [||], "Unit"
         |])
        (intrinsicResolutionTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest2", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "TestType"
             IntrinsicResolutionNS, "TestType", [||], "TestType"
         |])
        (intrinsicResolutionTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest3", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "TestType"
             IntrinsicResolutionNS, "TestType", [||], "TestType"
         |])
        (intrinsicResolutionTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest4", [||], "Unit"
             IntrinsicResolutionNS, "Override", [| "TestType" |], "Unit"
             IntrinsicResolutionNS, "TestType", [||], "TestType"
         |])
        (defaultTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest5", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "Unit"
         |])
        (defaultTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest6", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "Unit"
         |])
    |]
    |> makeSignatures

let private typeParameterTypes =
    makeTypeMap [| makeTypeParam ClassicalControlNS "Bar" "Q"
                   makeTypeParam ClassicalControlNS "Bar" "W" |]

let private defaultWithOperation =
    makeTypeMap [| "SubOp1Type[]",
                   ((ResolvedType.New UnitType, ResolvedType.New UnitType),
                    {
                        Characteristics = ResolvedCharacteristics.Empty
                        InferredInformation = InferredCallableInformation.NoInformation
                    })
                   |> QsTypeKind.Operation
                   |> ResolvedType.New
                   |> ArrayType |]

/// Expected callable signatures to be found when running Classical Control tests
let public ClassicalControlSignatures =
    [| // Basic Lift
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit" // The original operation
             ClassicalControlNS, "_Foo", [||], "Unit" // The generated operation
         |])
        // Lift Loops
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
         |])
        // Don't Lift Single Call
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Lift Single Non-Call
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
         |])
        // Don't Lift Return Statements
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // All-Or-None Lifting
        (defaultTypes,
         [|
             ClassicalControlNS, "IfInvalid", [||], "Unit"
             ClassicalControlNS, "ElseInvalid", [||], "Unit"
             ClassicalControlNS, "BothInvalid", [||], "Unit"
         |])
        // ApplyIfZero And ApplyIfOne
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Apply If Zero Else One
        (defaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Apply If One Else Zero
        (defaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // If Elif
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // And Condition
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Or Condition
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Don't Lift Functions
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "SubFunc1", [||], "Unit"
             ClassicalControlNS, "SubFunc2", [||], "Unit"
             ClassicalControlNS, "SubFunc3", [||], "Unit"
         |])
        // Lift Self-Contained Mutable
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
         |])
        // Don't Lift General Mutable
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Generics Support
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
         |])
        // Adjoint Support
        (defaultTypes,
         [|
             ClassicalControlNS, "Provided", [||], "Unit"
             ClassicalControlNS, "Self", [||], "Unit"
             ClassicalControlNS, "Invert", [||], "Unit"
             ClassicalControlNS, "_Provided", [||], "Unit"
             ClassicalControlNS, "_Provided", [||], "Unit"
             ClassicalControlNS, "_Self", [||], "Unit"
             ClassicalControlNS, "_Invert", [||], "Unit"
         |])
        // Controlled Support
        (defaultTypes,
         [|
             ClassicalControlNS, "Provided", [||], "Unit"
             ClassicalControlNS, "Distribute", [||], "Unit"
             ClassicalControlNS, "_Provided", [||], "Unit"
             ClassicalControlNS, "_Provided", [||], "Unit"
             ClassicalControlNS, "_Distribute", [||], "Unit"
         |])
        // Controlled Adjoint Support - Provided
        (defaultTypes,
         [|
             ClassicalControlNS, "ProvidedBody", [||], "Unit"
             ClassicalControlNS, "ProvidedAdjoint", [||], "Unit"
             ClassicalControlNS, "ProvidedControlled", [||], "Unit"
             ClassicalControlNS, "ProvidedAll", [||], "Unit"

             ClassicalControlNS, "_ProvidedBody", [||], "Unit"
             ClassicalControlNS, "_ProvidedBody", [||], "Unit"

             ClassicalControlNS, "_ProvidedAdjoint", [||], "Unit"
             ClassicalControlNS, "_ProvidedAdjoint", [||], "Unit"
             ClassicalControlNS, "_ProvidedAdjoint", [||], "Unit"

             ClassicalControlNS, "_ProvidedControlled", [||], "Unit"
             ClassicalControlNS, "_ProvidedControlled", [||], "Unit"
             ClassicalControlNS, "_ProvidedControlled", [||], "Unit"

             ClassicalControlNS, "_ProvidedAll", [||], "Unit"
             ClassicalControlNS, "_ProvidedAll", [||], "Unit"
             ClassicalControlNS, "_ProvidedAll", [||], "Unit"
             ClassicalControlNS, "_ProvidedAll", [||], "Unit"
         |])
        // Controlled Adjoint Support - Distribute
        (defaultTypes,
         [|
             ClassicalControlNS, "DistributeBody", [||], "Unit"
             ClassicalControlNS, "DistributeAdjoint", [||], "Unit"
             ClassicalControlNS, "DistributeControlled", [||], "Unit"
             ClassicalControlNS, "DistributeAll", [||], "Unit"

             ClassicalControlNS, "_DistributeBody", [||], "Unit"

             ClassicalControlNS, "_DistributeAdjoint", [||], "Unit"
             ClassicalControlNS, "_DistributeAdjoint", [||], "Unit"

             ClassicalControlNS, "_DistributeControlled", [||], "Unit"
             ClassicalControlNS, "_DistributeControlled", [||], "Unit"

             ClassicalControlNS, "_DistributeAll", [||], "Unit"
             ClassicalControlNS, "_DistributeAll", [||], "Unit"
             ClassicalControlNS, "_DistributeAll", [||], "Unit"
         |])
        // Controlled Adjoint Support - Invert
        (defaultTypes,
         [|
             ClassicalControlNS, "InvertBody", [||], "Unit"
             ClassicalControlNS, "InvertAdjoint", [||], "Unit"
             ClassicalControlNS, "InvertControlled", [||], "Unit"
             ClassicalControlNS, "InvertAll", [||], "Unit"

             ClassicalControlNS, "_InvertBody", [||], "Unit"

             ClassicalControlNS, "_InvertAdjoint", [||], "Unit"
             ClassicalControlNS, "_InvertAdjoint", [||], "Unit"

             ClassicalControlNS, "_InvertControlled", [||], "Unit"
             ClassicalControlNS, "_InvertControlled", [||], "Unit"

             ClassicalControlNS, "_InvertAll", [||], "Unit"
             ClassicalControlNS, "_InvertAll", [||], "Unit"
             ClassicalControlNS, "_InvertAll", [||], "Unit"
         |])
        // Controlled Adjoint Support - Self
        (defaultTypes,
         [|
             ClassicalControlNS, "SelfBody", [||], "Unit"
             ClassicalControlNS, "SelfControlled", [||], "Unit"

             ClassicalControlNS, "_SelfBody", [||], "Unit"

             ClassicalControlNS, "_SelfControlled", [||], "Unit"
             ClassicalControlNS, "_SelfControlled", [||], "Unit"
         |])
        // Within Block Support
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
         |])
        // Arguments Partially Resolve Type Parameters
        (typeParameterTypes,
         [|
             ClassicalControlNS, "Bar", [| "Bar.Q"; "Bar.W" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Lift Functor Application
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
         |])
        // Lift Partial Application
        (defaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Int"; "Double" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
         |])
        // Lift Array Item Call
        (defaultWithOperation,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "SubOp1Type[]" |], "Unit"
         |])
        // Lift One Not Both
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
         |])
        // Apply Conditionally
        (defaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Apply Conditionally With NoOp
        (defaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Inequality with ApplyConditionally
        (defaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Inequality with Apply If One Else Zero
        (defaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Inequality with Apply If Zero Else One
        (defaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Inequality with ApplyIfOne
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Inequality with ApplyIfZero
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Literal on the Left
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Simple NOT condition
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Outer NOT condition
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Nested NOT condition
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // One-sided NOT condition
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Don't Lift Classical Conditions
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Mutables with Nesting Lift Both
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Mutables with Nesting Lift Outer
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Mutables with Nesting Lift Neither
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Mutables with Classic Nesting Lift Inner
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [||], "Unit"
         |])
        // Mutables with Classic Nesting Lift Outer
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Int" |], "Unit"
         |])
        // Mutables with Classic Nesting Lift Outer With More Classic
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Int" |], "Unit"
         |])
        // Mutables with Classic Nesting Lift Middle
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Int" |], "Unit"
         |])
        // Nested Invalid Lifting
        (defaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Mutables with Classic Nesting Elif
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Int"; "Qubit" |], "Unit"
             ClassicalControlNS, "_Foo", [| "Int" |], "Unit"
             ClassicalControlNS, "_Foo", [| "Int" |], "Unit"
         |])
        // Mutables with Classic Nesting Elif Lift First
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Int" |], "Unit"
         |])
        // NOT Condition Retains Used Variables
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result"; "Result" |], "Unit"
             ClassicalControlNS, "_Foo", [| "Result"; "Result" |], "Unit"
             ClassicalControlNS, "_Foo", [| "Result"; "Result" |], "Unit"
         |])
        // Minimal Parameter Capture
        (defaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Int"; "Double"; "String"; "Double" |], "Unit"
         |])
    |]
    |> makeSignatures

let private withTupleTypes =
    makeTypeMap [| makeTupleType [ "Double", Double
                                   "String", String
                                   "Result", Result ]
                   makeTupleType [ "Double", Double
                                   "String", String ]
                   makeTupleType [ "Int", Int
                                   "Double", Double ] |]

let private withLambdaOperation =
    makeTypeMap [| "Unit => Int",
                   ((ResolvedType.New UnitType, ResolvedType.New Int),
                    {
                        Characteristics = ResolvedCharacteristics.Empty
                        InferredInformation = InferredCallableInformation.NoInformation
                    })
                   |> QsTypeKind.Operation |]

/// Expected callable signatures to be found when running Lambda Lifting tests
let public LambdaLiftingSignatures =
    [| // Basic Lift
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Int" // The generated operation
         |])
        // Without Return Value
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Unit" // The generated operation
         |])
        // Call Valued Callable
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "Bar", [||], "Int"
             LambdaLiftingNS, "_Foo", [||], "Int" // The generated operation
         |])
        // Call Unit Callable
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "Bar", [||], "Unit"
             LambdaLiftingNS, "_Foo", [||], "Unit" // The generated operation
         |])
        // Call Valued Callable Recursive
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Int" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Int" // The generated operation
         |])
        // Call Unit Callable Recursive
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Unit" // The generated operation
         |])
        // Use Closure
        (withTupleTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [| "Double"; "String"; "Result"; "Unit" |], "(Double, String, Result)" // The generated operation
         |])
        // With Lots of Params
        (withTupleTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [| "Int" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "Int" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "Int"; "Double" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "Int"; "Double"; "String" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "Int"; "(Double, String)" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "Int"; "Double"; "String" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "(Int, Double)"; "String" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "Int"; "Double"; "String" |], "Unit"
         |])
        // Use Closure With Params
        (withTupleTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [| "Double"; "String"; "Result"; "Int" |], "(Double, String, Result)"
             LambdaLiftingNS, "_Foo", [| "Double"; "String"; "Result"; "Int" |], "(Double, String, Result)"
             LambdaLiftingNS, "_Foo", [| "Double"; "String"; "Result"; "Int"; "Double" |], "(Double, String, Result)"
             LambdaLiftingNS,
             "_Foo",
             [| "Double"; "String"; "Result"; "Int"; "Double"; "String" |],
             "(Double, String, Result)"
             LambdaLiftingNS,
             "_Foo",
             [| "Double"; "String"; "Result"; "Int"; "(Double, String)" |],
             "(Double, String, Result)"
             LambdaLiftingNS,
             "_Foo",
             [| "Double"; "String"; "Result"; "Int"; "Double"; "String" |],
             "(Double, String, Result)"
             LambdaLiftingNS,
             "_Foo",
             [| "Double"; "String"; "Result"; "(Int, Double)"; "String" |],
             "(Double, String, Result)"
             LambdaLiftingNS,
             "_Foo",
             [| "Double"; "String"; "Result"; "Int"; "Double"; "String" |],
             "(Double, String, Result)"
         |])
        // Function Lambda
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Int"
         |])
        // With Type Parameters
        (defaultTypes,
         [|
         // This check is done manually in the test, but we have an empty entry here to
         // keep the index numbers of this list consistent with the test case numbers.
         |])
        // With Nested Lambda Call
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Int"
             LambdaLiftingNS, "_Foo", [||], "Int"
         |])
        // With Nested Lambda
        (withLambdaOperation,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Unit => Int"
             LambdaLiftingNS, "_Foo", [||], "Int"
         |])
        // Functor Support Basic Return
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Int"
             LambdaLiftingNS, "_Foo", [||], "Unit"
         |])
        // Functor Support Call
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit"
             LambdaLiftingNS, "BarInt", [||], "Int"
             LambdaLiftingNS, "Bar", [||], "Unit"
             LambdaLiftingNS, "BarAdj", [||], "Unit"
             LambdaLiftingNS, "BarCtl", [||], "Unit"
             LambdaLiftingNS, "BarAdjCtl", [||], "Unit"

             LambdaLiftingNS, "_Foo", [||], "Int"
             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_Foo", [||], "Unit"
         |])
        // Functor Support Lambda Call
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit"
             LambdaLiftingNS, "BarAdj", [||], "Unit"
             LambdaLiftingNS, "BarCtl", [||], "Unit"
             LambdaLiftingNS, "BarAdjCtl", [||], "Unit"

             LambdaLiftingNS, "_Foo", [||], "Int"
             LambdaLiftingNS, "_Foo", [||], "Int"
             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_Foo", [||], "Unit"
         |])
        // Functor Support Recursive
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit"
             LambdaLiftingNS, "FooAdj", [||], "Unit"
             LambdaLiftingNS, "FooCtl", [||], "Unit"
             LambdaLiftingNS, "FooAdjCtl", [||], "Unit"

             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_FooAdj", [||], "Unit"
             LambdaLiftingNS, "_FooCtl", [||], "Unit"
             LambdaLiftingNS, "_FooAdjCtl", [||], "Unit"
         |])
        // With Missing Params
        (withTupleTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation

             LambdaLiftingNS, "_Foo", [||], "Unit"
             LambdaLiftingNS, "_Foo", [| "Int" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "(Int, Double)" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "Int"; "Double" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "String"; "(Int, Double)" |], "Unit"
             LambdaLiftingNS, "_Foo", [| "String"; "Int"; "Double" |], "Unit"
         |])
        // Use Parameter Single
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [| "Int" |], "Int" // The generated operation
         |])
        // Use Parameter Tuple
        (withTupleTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [| "Double"; "Int" |], "(Int, Double)" // The generated operation
         |])
        // Use Parameter and Closure
        (withTupleTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [| "Int"; "Double" |], "(Int, Double)" // The generated operation
         |])
        // Use Parameter with Missing Params
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [| "Int"; "Result"; "String" |], "Int" // The generated operation
         |])
        // Multiple Lambdas in One Expression
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [| "Int" |], "Int"
             LambdaLiftingNS, "_Foo", [| "Int" |], "Int"
         |])
        // Function Without Return Value
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Unit" // The generated operation
         |])
        // Return Unit-Typed Expression
        (defaultTypes,
         [|
             LambdaLiftingNS, "Foo", [||], "Unit" // The original operation
             LambdaLiftingNS, "_Foo", [||], "Unit" // The generated operation
         |])
    |]
    |> makeSignatures

let private syntaxTreeTrimmingTypes =
    let usedUdt = "UsedUDT", UserDefinedType.New(SyntaxTreeTrimmingNS, "UsedUDT") |> UserDefinedType
    let unusedUdt = "UnusedUDT", UserDefinedType.New(SyntaxTreeTrimmingNS, "UnusedUDT") |> UserDefinedType
    makeTypeMap [| usedUdt; unusedUdt |]

let public SyntaxTreeTrimmingSignatures =
    [| // Trimmer Removes Unused Callables
        (defaultTypes,
         [|
             SyntaxTreeTrimmingNS, "Main", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedOp", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedFunc", [||], "Unit"
         |])
        // Trimmer Keeps UDTs
        (syntaxTreeTrimmingTypes,
         [|
             SyntaxTreeTrimmingNS, "Main", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedUDT", [| "Int" |], "UsedUDT"
         |])
        // Trimmer Keeps Intrinsics When Told
        (defaultTypes,
         [|
             SyntaxTreeTrimmingNS, "Main", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedIntrinsic", [||], "Unit"
             SyntaxTreeTrimmingNS, "UnusedIntrinsic", [||], "Unit"
         |])
        // Trimmer Removes Intrinsics When Told
        (defaultTypes,
         [|
             SyntaxTreeTrimmingNS, "Main", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedIntrinsic", [||], "Unit"
         |])
    |]
    |> makeSignatures
