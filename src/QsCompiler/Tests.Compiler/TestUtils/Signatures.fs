// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.Signatures

open System.Collections.Generic
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

let private _BaseTypes =
    [|
        "Unit", UnitType
        "Int", Int
        "Double", Double
        "String", String
        "Result", Result
        "Qubit", Qubit
        "Qubit[]", ResolvedType.New Qubit |> ArrayType
    |]

let private _MakeTypeMap udts =
    Array.concat
        [
            _BaseTypes
            udts
        ]
    |> Seq.map (fun (k, v) -> k, ResolvedType.New v) |> dict

let private _DefaultTypes = _MakeTypeMap [||]

let private _MakeSig input (typeMap : IDictionary<string,ResolvedType> ) =
    let ns, name, args, rtrn = input
    let fullName = { Namespace = ns; Name = name }
    let argType =
        if Array.isEmpty args then
            typeMap.["Unit"]
        else
            args |> Seq.map (fun typ -> typeMap.[typ]) |> ImmutableArray.ToImmutableArray |> QsTypeKind.TupleType |> ResolvedType.New
    let returnType = typeMap.[rtrn]
    (fullName, argType, returnType)

let private _MakeSignatures sigs =
    sigs
    |> Seq.map (fun (types, case) -> Seq.map (fun _sig -> _MakeSig _sig types) case)
    |> Seq.toArray

let _MakeTypeParam originNs originName paramName =
    originName + "." + paramName, {
        Origin = {
            Namespace = originNs
            Name = originName
        }
        TypeName = paramName
        Range = Null
    } |> TypeParameter

/// For all given namespaces in checkedNamespaces, checks that there are exactly
/// the callables specified with targetSignatures in the given compilation.
let public SignatureCheck checkedNamespaces targetSignatures compilation =

    let getNs targetNs =
        match Seq.tryFind (fun (ns : QsNamespace) -> ns.Name = targetNs) compilation.Namespaces with
        | Some ns -> ns
        | None -> sprintf "Expected but did not find namespace: %s" targetNs |> failwith

    let mutable callableSigs =
        checkedNamespaces
        |> Seq.map (fun checkedNs -> getNs checkedNs)
        |> SyntaxExtensions.Callables
        |> Seq.map (fun call -> (call.FullName, StripPositionInfo.Apply call.Signature.ArgumentType, StripPositionInfo.Apply call.Signature.ReturnType))

    let doesCallMatchSig call signature =
        let (call_fullName : QsQualifiedName), call_argType, call_rtrnType = call
        let (sig_fullName : QsQualifiedName), sig_argType, sig_rtrnType = signature

        call_fullName.Namespace = sig_fullName.Namespace &&
        call_fullName.Name.EndsWith sig_fullName.Name &&
        call_argType = sig_argType &&
        call_rtrnType = sig_rtrnType

    let makeArgsString (args : ResolvedType) =
        match args.Resolution with
        | QsTypeKind.UnitType -> "()"
        | _ -> args |> SyntaxTreeToQsharp.Default.ToCode

    let removeAt i lst =
        Seq.append
        <| Seq.take i lst
        <| Seq.skip (i+1) lst

    (*Tests that all target signatures are present*)
    for targetSig in targetSignatures do
        let sig_fullName, sig_argType, sig_rtrnType = targetSig
        callableSigs
        |> Seq.tryFindIndex (fun callSig -> doesCallMatchSig callSig targetSig)
        |> (fun x ->
                Assert.True (x <> None, sprintf "Expected but did not find: %s.*%s %s : %A" sig_fullName.Namespace sig_fullName.Name (makeArgsString sig_argType) sig_rtrnType.Resolution)
                callableSigs <- removeAt x.Value callableSigs
           )

    (*Tests that *only* targeted signatures are present*)
    for callSig in callableSigs do
        let sig_fullName, sig_argType, sig_rtrnType = callSig
        failwith (sprintf "Found unexpected callable: %O %s : %A" sig_fullName (makeArgsString sig_argType) sig_rtrnType.Resolution)

/// Names of several testing namespaces
let public MonomorphizationNs = "Microsoft.Quantum.Testing.Monomorphization"
let public GenericsNs = "Microsoft.Quantum.Testing.Generics"
let public IntrinsicResolutionNs = "Microsoft.Quantum.Testing.IntrinsicResolution"
let public ClassicalControlNs = "Microsoft.Quantum.Testing.ClassicalControl"
let public InternalRenamingNs = "Microsoft.Quantum.Testing.InternalRenaming"
let public CycleDetectionNS = "Microsoft.Quantum.Testing.CycleDetection"
let public PopulateCallGraphNS = "Microsoft.Quantum.Testing.PopulateCallGraph"

/// Expected callable signatures to be found when running Monomorphization tests
let public MonomorphizationSignatures =
    [|
        (_DefaultTypes, [| (*Test Case 1*)
            MonomorphizationNs, "Test1", [||], "Unit"
            GenericsNs, "Test1Main", [||], "Unit"

            GenericsNs, "BasicGeneric", [|"Double"; "Int"|], "Unit"
            GenericsNs, "BasicGeneric", [|"String"; "String"|], "Unit"
            GenericsNs, "BasicGeneric", [|"Unit"; "Unit"|], "Unit"
            GenericsNs, "BasicGeneric", [|"String"; "Double"|], "Unit"
            GenericsNs, "BasicGeneric", [|"Int"; "Double"|], "Unit"
            GenericsNs, "NoArgsGeneric", [||], "Double"
            GenericsNs, "ReturnGeneric", [|"Double"; "String"; "Int"|], "Int"
            GenericsNs, "ReturnGeneric", [|"String"; "Int"; "String"|], "String"
        |])
        (_DefaultTypes, [| (*Test Case 2*)
            MonomorphizationNs, "Test2", [||], "Unit"
            GenericsNs, "Test2Main", [||], "Unit"

            GenericsNs, "ArrayGeneric", [|"Qubit"; "String"|], "Int"
            GenericsNs, "ArrayGeneric", [|"Qubit"; "Int"|], "Int"
            GenericsNs, "GenericCallsGeneric", [|"Qubit"; "Int"|], "Unit"
        |])
        (_DefaultTypes, [| (*Test Case 3*)
            MonomorphizationNs, "Test3", [||], "Unit"
            GenericsNs, "Test3Main", [||], "Unit"

            GenericsNs, "GenericCallsSpecializations", [|"Double"; "String"; "Qubit[]"|], "Unit"
            GenericsNs, "GenericCallsSpecializations", [|"Double"; "String"; "Double"|], "Unit"
            GenericsNs, "GenericCallsSpecializations", [|"String"; "Int"; "Unit"|], "Unit"

            GenericsNs, "BasicGeneric", [|"String"; "Qubit[]"|], "Unit"
            GenericsNs, "BasicGeneric", [|"String"; "Int"|], "Unit"

            GenericsNs, "ArrayGeneric", [|"Qubit"; "Double"|], "Int"
        |])
        (_DefaultTypes, [| (*Test Case 4*)
            MonomorphizationNs, "Test4", [||], "Unit"
            GenericsNs, "Test4Main", [||], "Unit"
            GenericsNs, "_GenericCallsSelf", [||], "Unit"
            GenericsNs, "_GenericCallsSelf2", [|"Double"|], "Unit"
        |])
    |]
    |> _MakeSignatures

let private _IntrinsicResolutionTypes = _MakeTypeMap [|
        "TestType", {Namespace = "Microsoft.Quantum.Testing.IntrinsicResolution"; Name = "TestType"; Range = Null} |> UserDefinedType
    |]

/// Expected callable signatures to be found when running Intrinsic Resolution tests
let public IntrinsicResolutionSignatures =
    [|
        (_DefaultTypes, [|
            IntrinsicResolutionNs, "IntrinsicResolutionTest1", [||], "Unit"
            IntrinsicResolutionNs, "LocalIntrinsic", [||], "Unit"
            IntrinsicResolutionNs, "Override", [||], "Unit"
            IntrinsicResolutionNs, "EnvironmentIntrinsic", [||], "Unit"
        |])
        (_IntrinsicResolutionTypes, [|
            IntrinsicResolutionNs, "IntrinsicResolutionTest2", [||], "Unit"
            IntrinsicResolutionNs, "Override", [||], "TestType"
            IntrinsicResolutionNs, "TestType", [||], "TestType"
        |])
        (_IntrinsicResolutionTypes, [|
            IntrinsicResolutionNs, "IntrinsicResolutionTest3", [||], "Unit"
            IntrinsicResolutionNs, "Override", [||], "TestType"
            IntrinsicResolutionNs, "TestType", [||], "TestType"
        |])
        (_IntrinsicResolutionTypes, [|
            IntrinsicResolutionNs, "IntrinsicResolutionTest4", [||], "Unit"
            IntrinsicResolutionNs, "Override", [|"TestType"|], "Unit"
            IntrinsicResolutionNs, "TestType", [||], "TestType"
        |])
        (_DefaultTypes, [|
            IntrinsicResolutionNs, "IntrinsicResolutionTest5", [||], "Unit"
            IntrinsicResolutionNs, "Override", [||], "Unit"
        |])
        (_DefaultTypes, [|
            IntrinsicResolutionNs, "IntrinsicResolutionTest6", [||], "Unit"
            IntrinsicResolutionNs, "Override", [||], "Unit"
        |])
    |]
    |> _MakeSignatures

let private _TypeParameterTypes = _MakeTypeMap [|
    _MakeTypeParam ClassicalControlNs "Bar" "Q"
    _MakeTypeParam ClassicalControlNs "Bar" "W"
|]

let private _DefaultWithOperation = _MakeTypeMap [|
    "SubOp1Type[]", ((ResolvedType.New UnitType, ResolvedType.New UnitType), {
        Characteristics = ResolvedCharacteristics.Empty
        InferredInformation = InferredCallableInformation.NoInformation
    }) |> QsTypeKind.Operation |> ResolvedType.New |> ArrayType
|]

/// Expected callable signatures to be found when running Classical Control tests
let public ClassicalControlSignatures =
    [|
        (_DefaultTypes, [| // Basic Lift
            ClassicalControlNs, "Foo", [||], "Unit"; // The original operation
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"; // The generated operation
        |])
        (_DefaultTypes, [| // Lift Loops
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Don't Lift Single Call
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Lift Single Non-Call
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Don't Lift Return Statements
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // All-Or-None Lifting
            ClassicalControlNs, "IfInvalid", [||], "Unit"
            ClassicalControlNs, "ElseInvalid", [||], "Unit"
            ClassicalControlNs, "BothInvalid", [||], "Unit"
        |])
        (_DefaultTypes, [| // ApplyIfZero And ApplyIfOne
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Apply If Zero Else One
            ClassicalControlNs, "Bar", [|"Result"|], "Unit"
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Apply If One Else Zero
            ClassicalControlNs, "Bar", [|"Result"|], "Unit"
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // If Elif
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // And Condition
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Or Condition
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Don't Lift Functions
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "SubFunc1", [||], "Unit"
            ClassicalControlNs, "SubFunc2", [||], "Unit"
            ClassicalControlNs, "SubFunc3", [||], "Unit"
        |])
        (_DefaultTypes, [| // Lift Self-Contained Mutable
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Don't Lift General Mutable
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Generics Support
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Adjoint Support
            ClassicalControlNs, "Provided", [||], "Unit"
            ClassicalControlNs, "Self", [||], "Unit"
            ClassicalControlNs, "Invert", [||], "Unit"
            ClassicalControlNs, "_Provided", [|"Result"|], "Unit"
            ClassicalControlNs, "_Provided", [|"Result"|], "Unit"
            ClassicalControlNs, "_Self", [|"Result"|], "Unit"
            ClassicalControlNs, "_Invert", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Controlled Support
            ClassicalControlNs, "Provided", [||], "Unit"
            ClassicalControlNs, "Distribute", [||], "Unit"
            ClassicalControlNs, "_Provided", [|"Result"|], "Unit"
            ClassicalControlNs, "_Provided", [|"Result";"Qubit[]";"Unit"|], "Unit"
            ClassicalControlNs, "_Distribute", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Controlled Adjoint Support - Provided
            ClassicalControlNs, "ProvidedBody", [||], "Unit"
            ClassicalControlNs, "ProvidedAdjoint", [||], "Unit"
            ClassicalControlNs, "ProvidedControlled", [||], "Unit"
            ClassicalControlNs, "ProvidedAll", [||], "Unit"

            ClassicalControlNs, "_ProvidedBody", [|"Result"|], "Unit"
            ClassicalControlNs, "_ProvidedBody", [|"Result";"Qubit[]";"Unit"|], "Unit"

            ClassicalControlNs, "_ProvidedAdjoint", [|"Result"|], "Unit"
            ClassicalControlNs, "_ProvidedAdjoint", [|"Result"|], "Unit"
            ClassicalControlNs, "_ProvidedAdjoint", [|"Result";"Qubit[]";"Unit"|], "Unit"

            ClassicalControlNs, "_ProvidedControlled", [|"Result"|], "Unit"
            ClassicalControlNs, "_ProvidedControlled", [|"Result";"Qubit[]";"Unit"|], "Unit"
            ClassicalControlNs, "_ProvidedControlled", [|"Result";"Qubit[]";"Unit"|], "Unit"

            ClassicalControlNs, "_ProvidedAll", [|"Result"|], "Unit"
            ClassicalControlNs, "_ProvidedAll", [|"Result"|], "Unit"
            ClassicalControlNs, "_ProvidedAll", [|"Result";"Qubit[]";"Unit"|], "Unit"
            ClassicalControlNs, "_ProvidedAll", [|"Result";"Qubit[]";"Unit"|], "Unit"
        |])
        (_DefaultTypes, [| // Controlled Adjoint Support - Distribute
            ClassicalControlNs, "DistributeBody", [||], "Unit"
            ClassicalControlNs, "DistributeAdjoint", [||], "Unit"
            ClassicalControlNs, "DistributeControlled", [||], "Unit"
            ClassicalControlNs, "DistributeAll", [||], "Unit"

            ClassicalControlNs, "_DistributeBody", [|"Result"|], "Unit"

            ClassicalControlNs, "_DistributeAdjoint", [|"Result"|], "Unit"
            ClassicalControlNs, "_DistributeAdjoint", [|"Result"|], "Unit"

            ClassicalControlNs, "_DistributeControlled", [|"Result"|], "Unit"
            ClassicalControlNs, "_DistributeControlled", [|"Result";"Qubit[]";"Unit"|], "Unit"

            ClassicalControlNs, "_DistributeAll", [|"Result"|], "Unit"
            ClassicalControlNs, "_DistributeAll", [|"Result"|], "Unit"
            ClassicalControlNs, "_DistributeAll", [|"Result";"Qubit[]";"Unit"|], "Unit"
        |])
        (_DefaultTypes, [| // Controlled Adjoint Support - Invert
            ClassicalControlNs, "InvertBody", [||], "Unit"
            ClassicalControlNs, "InvertAdjoint", [||], "Unit"
            ClassicalControlNs, "InvertControlled", [||], "Unit"
            ClassicalControlNs, "InvertAll", [||], "Unit"

            ClassicalControlNs, "_InvertBody", [|"Result"|], "Unit"

            ClassicalControlNs, "_InvertAdjoint", [|"Result"|], "Unit"
            ClassicalControlNs, "_InvertAdjoint", [|"Result"|], "Unit"

            ClassicalControlNs, "_InvertControlled", [|"Result"|], "Unit"
            ClassicalControlNs, "_InvertControlled", [|"Result";"Qubit[]";"Unit"|], "Unit"

            ClassicalControlNs, "_InvertAll", [|"Result"|], "Unit"
            ClassicalControlNs, "_InvertAll", [|"Result"|], "Unit"
            ClassicalControlNs, "_InvertAll", [|"Result";"Qubit[]";"Unit"|], "Unit"
        |])
        (_DefaultTypes, [| // Controlled Adjoint Support - Self
            ClassicalControlNs, "SelfBody", [||], "Unit"
            ClassicalControlNs, "SelfControlled", [||], "Unit"

            ClassicalControlNs, "_SelfBody", [|"Result"|], "Unit"

            ClassicalControlNs, "_SelfControlled", [|"Result"|], "Unit"
            ClassicalControlNs, "_SelfControlled", [|"Result";"Qubit[]";"Unit"|], "Unit"
        |])
        (_DefaultTypes, [| // Within Block Support
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_TypeParameterTypes, [| // Arguments Partially Resolve Type Parameters
            ClassicalControlNs, "Bar", [|"Bar.Q";"Bar.W"|], "Unit"
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Lift Functor Application
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Lift Partial Application
            ClassicalControlNs, "Bar", [|"Int";"Double"|], "Unit"
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultWithOperation, [| // Lift Array Item Call
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"SubOp1Type[]";"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Lift One Not Both
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Apply Conditionally
            ClassicalControlNs, "Bar", [|"Result"|], "Unit"
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Apply Conditionally With NoOp
            ClassicalControlNs, "Bar", [|"Result"|], "Unit"
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Inequality with ApplyConditionally
            ClassicalControlNs, "Bar", [|"Result"|], "Unit"
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Inequality with Apply If One Else Zero
            ClassicalControlNs, "Bar", [|"Result"|], "Unit"
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Inequality with Apply If Zero Else One
            ClassicalControlNs, "Bar", [|"Result"|], "Unit"
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Inequality with ApplyIfOne
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Inequality with ApplyIfZero
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Literal on the Left
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Simple NOT condition
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
        (_DefaultTypes, [| // Outer NOT condition
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // Nested NOT condition
            ClassicalControlNs, "Foo", [||], "Unit"
            ClassicalControlNs, "_Foo", [|"Result"|], "Unit"
        |])
        (_DefaultTypes, [| // One-sided NOT condition
            ClassicalControlNs, "Foo", [||], "Unit"
        |])
    |]
    |> _MakeSignatures
