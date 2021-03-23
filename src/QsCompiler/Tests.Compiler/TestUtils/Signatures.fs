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
    Array.concat [ _BaseTypes; udts ] |> Seq.map (fun (k, v) -> k, ResolvedType.New v) |> dict

let private _DefaultTypes = _MakeTypeMap [||]

let private _MakeSig input (typeMap: IDictionary<string, ResolvedType>) =
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

let private _MakeSignatures sigs =
    sigs |> Seq.map (fun (types, case) -> Seq.map (fun _sig -> _MakeSig _sig types) case) |> Seq.toArray

let _MakeTypeParam originNs originName paramName =
    originName + "." + paramName,
    {
        Origin = { Namespace = originNs; Name = originName }
        TypeName = paramName
        Range = Null
    }
    |> TypeParameter

/// For all given namespaces in checkedNamespaces, checks that there are exactly
/// the callables specified with targetSignatures in the given compilation.
let public SignatureCheck checkedNamespaces targetSignatures compilation =

    let getNs targetNs =
        match Seq.tryFind (fun (ns: QsNamespace) -> ns.Name = targetNs) compilation.Namespaces with
        | Some ns -> ns
        | None -> sprintf "Expected but did not find namespace: %s" targetNs |> failwith

    let mutable callableSigs =
        checkedNamespaces
        |> Seq.map (fun checkedNs -> getNs checkedNs)
        |> SyntaxTreeExtensions.Callables
        |> Seq.map (fun call ->
            (call.FullName,
             StripPositionInfo.Apply call.Signature.ArgumentType,
             StripPositionInfo.Apply call.Signature.ReturnType))

    let doesCallMatchSig call signature =
        let (call_fullName: QsQualifiedName), call_argType, call_rtrnType = call
        let (sig_fullName: QsQualifiedName), sig_argType, sig_rtrnType = signature

        call_fullName.Namespace = sig_fullName.Namespace
        && call_fullName.Name.EndsWith sig_fullName.Name
        && call_argType = sig_argType
        && call_rtrnType = sig_rtrnType

    let makeArgsString (args: ResolvedType) =
        match args.Resolution with
        | QsTypeKind.UnitType -> "()"
        | _ -> args |> SyntaxTreeToQsharp.Default.ToCode

    let removeAt i lst =
        Seq.append <| Seq.take i lst <| Seq.skip (i + 1) lst

    (*Tests that all target signatures are present*)
    for targetSig in targetSignatures do
        let sig_fullName, sig_argType, sig_rtrnType = targetSig

        callableSigs
        |> Seq.tryFindIndex (fun callSig -> doesCallMatchSig callSig targetSig)
        |> (fun x ->
            Assert.True
                (x <> None,
                 sprintf
                     "Expected but did not find: %s.*%s %s : %A"
                     sig_fullName.Namespace
                     sig_fullName.Name
                     (makeArgsString sig_argType)
                     sig_rtrnType.Resolution)

            callableSigs <- removeAt x.Value callableSigs)

    (*Tests that *only* targeted signatures are present*)
    for callSig in callableSigs do
        let sig_fullName, sig_argType, sig_rtrnType = callSig

        failwith
            (sprintf
                "Found unexpected callable: %O %s : %A"
                 sig_fullName
                 (makeArgsString sig_argType)
                 sig_rtrnType.Resolution)

/// Names of several testing namespaces
let public MonomorphizationNS = "Microsoft.Quantum.Testing.Monomorphization"
let public GenericsNS = "Microsoft.Quantum.Testing.Generics"
let public IntrinsicResolutionNS = "Microsoft.Quantum.Testing.IntrinsicResolution"
let public ClassicalControlNS = "Microsoft.Quantum.Testing.ClassicalControl"
let public InternalRenamingNS = "Microsoft.Quantum.Testing.InternalRenaming"
let public CycleDetectionNS = "Microsoft.Quantum.Testing.CycleDetection"
let public PopulateCallGraphNS = "Microsoft.Quantum.Testing.PopulateCallGraph"
let public SyntaxTreeTrimmingNS = "Microsoft.Quantum.Testing.SyntaxTreeTrimming"

/// Expected callable signatures to be found when running Monomorphization tests
let public MonomorphizationSignatures =
    [| // Test Case 1
        (_DefaultTypes,
         [|
             MonomorphizationNS, "Test1", [||], "Unit"
             GenericsNS, "Test1Main", [||], "Unit"
             GenericsNS, "Test2Main", [||], "Unit"
             GenericsNS, "Test3Main", [||], "Unit"
             GenericsNS, "Test4Main", [||], "Unit"

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
        (_DefaultTypes,
         [|
             MonomorphizationNS, "Test2", [||], "Unit"
             GenericsNS, "Test1Main", [||], "Unit"
             GenericsNS, "Test2Main", [||], "Unit"
             GenericsNS, "Test3Main", [||], "Unit"
             GenericsNS, "Test4Main", [||], "Unit"

             GenericsNS, "ArrayGeneric", [| "Qubit"; "String" |], "Int"
             GenericsNS, "ArrayGeneric", [| "Qubit"; "Int" |], "Int"
             GenericsNS, "GenericCallsGeneric", [| "Qubit"; "Int" |], "Unit"
         |])
        // Test Case 3
        (_DefaultTypes,
         [|
             MonomorphizationNS, "Test3", [||], "Unit"
             GenericsNS, "Test1Main", [||], "Unit"
             GenericsNS, "Test2Main", [||], "Unit"
             GenericsNS, "Test3Main", [||], "Unit"
             GenericsNS, "Test4Main", [||], "Unit"

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
        (_DefaultTypes,
         [|
             MonomorphizationNS, "Test4", [||], "Unit"
             GenericsNS, "Test1Main", [||], "Unit"
             GenericsNS, "Test2Main", [||], "Unit"
             GenericsNS, "Test3Main", [||], "Unit"
             GenericsNS, "Test4Main", [||], "Unit"

             GenericsNS, "_GenericCallsSelf", [||], "Unit"
             GenericsNS, "_GenericCallsSelf2", [| "Double" |], "Unit"
         |])
    |]
    |> _MakeSignatures

let private _IntrinsicResolutionTypes =
    _MakeTypeMap [| "TestType",
                    {
                        Namespace = IntrinsicResolutionNS
                        Name = "TestType"
                        Range = Null
                    }
                    |> UserDefinedType |]

/// Expected callable signatures to be found when running Intrinsic Resolution tests
let public IntrinsicResolutionSignatures =
    [|
        (_DefaultTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest1", [||], "Unit"
             IntrinsicResolutionNS, "LocalIntrinsic", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "Unit"
             IntrinsicResolutionNS, "EnvironmentIntrinsic", [||], "Unit"
         |])
        (_IntrinsicResolutionTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest2", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "TestType"
             IntrinsicResolutionNS, "TestType", [||], "TestType"
         |])
        (_IntrinsicResolutionTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest3", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "TestType"
             IntrinsicResolutionNS, "TestType", [||], "TestType"
         |])
        (_IntrinsicResolutionTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest4", [||], "Unit"
             IntrinsicResolutionNS, "Override", [| "TestType" |], "Unit"
             IntrinsicResolutionNS, "TestType", [||], "TestType"
         |])
        (_DefaultTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest5", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "Unit"
         |])
        (_DefaultTypes,
         [|
             IntrinsicResolutionNS, "IntrinsicResolutionTest6", [||], "Unit"
             IntrinsicResolutionNS, "Override", [||], "Unit"
         |])
    |]
    |> _MakeSignatures

let private _TypeParameterTypes =
    _MakeTypeMap [| _MakeTypeParam ClassicalControlNS "Bar" "Q"
                    _MakeTypeParam ClassicalControlNS "Bar" "W" |]

let private _DefaultWithOperation =
    _MakeTypeMap [| "SubOp1Type[]",
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
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit" // The original operation
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit" // The generated operation
         |])
        // Lift Loops
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Don't Lift Single Call
        (_DefaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Lift Single Non-Call
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Don't Lift Return Statements
        (_DefaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // All-Or-None Lifting
        (_DefaultTypes,
         [|
             ClassicalControlNS, "IfInvalid", [||], "Unit"
             ClassicalControlNS, "ElseInvalid", [||], "Unit"
             ClassicalControlNS, "BothInvalid", [||], "Unit"
         |])
        // ApplyIfZero And ApplyIfOne
        (_DefaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Apply If Zero Else One
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Apply If One Else Zero
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // If Elif
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // And Condition
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Or Condition
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Don't Lift Functions
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "SubFunc1", [||], "Unit"
             ClassicalControlNS, "SubFunc2", [||], "Unit"
             ClassicalControlNS, "SubFunc3", [||], "Unit"
         |])
        // Lift Self-Contained Mutable
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Don't Lift General Mutable
        (_DefaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Generics Support
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Adjoint Support
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Provided", [||], "Unit"
             ClassicalControlNS, "Self", [||], "Unit"
             ClassicalControlNS, "Invert", [||], "Unit"
             ClassicalControlNS, "_Provided", [| "Result" |], "Unit"
             ClassicalControlNS, "_Provided", [| "Result" |], "Unit"
             ClassicalControlNS, "_Self", [| "Result" |], "Unit"
             ClassicalControlNS, "_Invert", [| "Result" |], "Unit"
         |])
        // Controlled Support
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Provided", [||], "Unit"
             ClassicalControlNS, "Distribute", [||], "Unit"
             ClassicalControlNS, "_Provided", [| "Result" |], "Unit"
             ClassicalControlNS, "_Provided", [| "Result"; "Qubit[]"; "Unit" |], "Unit"
             ClassicalControlNS, "_Distribute", [| "Result" |], "Unit"
         |])
        // Controlled Adjoint Support - Provided
        (_DefaultTypes,
         [|
             ClassicalControlNS, "ProvidedBody", [||], "Unit"
             ClassicalControlNS, "ProvidedAdjoint", [||], "Unit"
             ClassicalControlNS, "ProvidedControlled", [||], "Unit"
             ClassicalControlNS, "ProvidedAll", [||], "Unit"

             ClassicalControlNS, "_ProvidedBody", [| "Result" |], "Unit"
             ClassicalControlNS, "_ProvidedBody", [| "Result"; "Qubit[]"; "Unit" |], "Unit"

             ClassicalControlNS, "_ProvidedAdjoint", [| "Result" |], "Unit"
             ClassicalControlNS, "_ProvidedAdjoint", [| "Result" |], "Unit"
             ClassicalControlNS, "_ProvidedAdjoint", [| "Result"; "Qubit[]"; "Unit" |], "Unit"

             ClassicalControlNS, "_ProvidedControlled", [| "Result" |], "Unit"
             ClassicalControlNS, "_ProvidedControlled", [| "Result"; "Qubit[]"; "Unit" |], "Unit"
             ClassicalControlNS, "_ProvidedControlled", [| "Result"; "Qubit[]"; "Unit" |], "Unit"

             ClassicalControlNS, "_ProvidedAll", [| "Result" |], "Unit"
             ClassicalControlNS, "_ProvidedAll", [| "Result" |], "Unit"
             ClassicalControlNS, "_ProvidedAll", [| "Result"; "Qubit[]"; "Unit" |], "Unit"
             ClassicalControlNS, "_ProvidedAll", [| "Result"; "Qubit[]"; "Unit" |], "Unit"
         |])
        // Controlled Adjoint Support - Distribute
        (_DefaultTypes,
         [|
             ClassicalControlNS, "DistributeBody", [||], "Unit"
             ClassicalControlNS, "DistributeAdjoint", [||], "Unit"
             ClassicalControlNS, "DistributeControlled", [||], "Unit"
             ClassicalControlNS, "DistributeAll", [||], "Unit"

             ClassicalControlNS, "_DistributeBody", [| "Result" |], "Unit"

             ClassicalControlNS, "_DistributeAdjoint", [| "Result" |], "Unit"
             ClassicalControlNS, "_DistributeAdjoint", [| "Result" |], "Unit"

             ClassicalControlNS, "_DistributeControlled", [| "Result" |], "Unit"
             ClassicalControlNS, "_DistributeControlled", [| "Result"; "Qubit[]"; "Unit" |], "Unit"

             ClassicalControlNS, "_DistributeAll", [| "Result" |], "Unit"
             ClassicalControlNS, "_DistributeAll", [| "Result" |], "Unit"
             ClassicalControlNS, "_DistributeAll", [| "Result"; "Qubit[]"; "Unit" |], "Unit"
         |])
        // Controlled Adjoint Support - Invert
        (_DefaultTypes,
         [|
             ClassicalControlNS, "InvertBody", [||], "Unit"
             ClassicalControlNS, "InvertAdjoint", [||], "Unit"
             ClassicalControlNS, "InvertControlled", [||], "Unit"
             ClassicalControlNS, "InvertAll", [||], "Unit"

             ClassicalControlNS, "_InvertBody", [| "Result" |], "Unit"

             ClassicalControlNS, "_InvertAdjoint", [| "Result" |], "Unit"
             ClassicalControlNS, "_InvertAdjoint", [| "Result" |], "Unit"

             ClassicalControlNS, "_InvertControlled", [| "Result" |], "Unit"
             ClassicalControlNS, "_InvertControlled", [| "Result"; "Qubit[]"; "Unit" |], "Unit"

             ClassicalControlNS, "_InvertAll", [| "Result" |], "Unit"
             ClassicalControlNS, "_InvertAll", [| "Result" |], "Unit"
             ClassicalControlNS, "_InvertAll", [| "Result"; "Qubit[]"; "Unit" |], "Unit"
         |])
        // Controlled Adjoint Support - Self
        (_DefaultTypes,
         [|
             ClassicalControlNS, "SelfBody", [||], "Unit"
             ClassicalControlNS, "SelfControlled", [||], "Unit"

             ClassicalControlNS, "_SelfBody", [| "Result" |], "Unit"

             ClassicalControlNS, "_SelfControlled", [| "Result" |], "Unit"
             ClassicalControlNS, "_SelfControlled", [| "Result"; "Qubit[]"; "Unit" |], "Unit"
         |])
        // Within Block Support
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Arguments Partially Resolve Type Parameters
        (_TypeParameterTypes,
         [|
             ClassicalControlNS, "Bar", [| "Bar.Q"; "Bar.W" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Lift Functor Application
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Lift Partial Application
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Int"; "Double" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Lift Array Item Call
        (_DefaultWithOperation,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "SubOp1Type[]"; "Result" |], "Unit"
         |])
        // Lift One Not Both
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Apply Conditionally
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Apply Conditionally With NoOp
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Inequality with ApplyConditionally
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Inequality with Apply If One Else Zero
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Inequality with Apply If Zero Else One
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Bar", [| "Result" |], "Unit"
             ClassicalControlNS, "Foo", [||], "Unit"
         |])
        // Inequality with ApplyIfOne
        (_DefaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Inequality with ApplyIfZero
        (_DefaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Literal on the Left
        (_DefaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Simple NOT condition
        (_DefaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
        // Outer NOT condition
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // Nested NOT condition
        (_DefaultTypes,
         [|
             ClassicalControlNS, "Foo", [||], "Unit"
             ClassicalControlNS, "_Foo", [| "Result" |], "Unit"
         |])
        // One-sided NOT condition
        (_DefaultTypes, [| ClassicalControlNS, "Foo", [||], "Unit" |])
    |]
    |> _MakeSignatures

let private _SyntaxTreeTrimmingTypes =
    let UsedUDT =
        "UsedUDT",
        {
            Namespace = SyntaxTreeTrimmingNS
            Name = "UsedUDT"
            Range = Null
        }
        |> UserDefinedType
    let UnusedUDT =
        "UnusedUDT",
        {
            Namespace = SyntaxTreeTrimmingNS
            Name = "UnusedUDT"
            Range = Null
        }
        |> UserDefinedType
    _MakeTypeMap [| UsedUDT; UnusedUDT |]

let public SyntaxTreeTrimmingSignatures =
    [| // Trimmer Removes Unused Callables
        (_DefaultTypes,
         [|
             SyntaxTreeTrimmingNS, "Main", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedOp", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedFunc", [||], "Unit"
         |])
        // Trimmer Keeps UDTs
        (_SyntaxTreeTrimmingTypes,
         [|
             SyntaxTreeTrimmingNS, "Main", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedUDT", [| "Int" |], "UsedUDT"
             SyntaxTreeTrimmingNS, "UnusedUDT", [| "String" |], "UnusedUDT"
         |])
        // Trimmer Keeps Intrinsics When Told
        (_DefaultTypes,
         [|
             SyntaxTreeTrimmingNS, "Main", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedIntrinsic", [||], "Unit"
             SyntaxTreeTrimmingNS, "UnusedIntrinsic", [||], "Unit"
         |])
        // Trimmer Removes Intrinsics When Told
        (_DefaultTypes,
         [|
             SyntaxTreeTrimmingNS, "Main", [||], "Unit"
             SyntaxTreeTrimmingNS, "UsedIntrinsic", [||], "Unit"
         |])
    |]
    |> _MakeSignatures
