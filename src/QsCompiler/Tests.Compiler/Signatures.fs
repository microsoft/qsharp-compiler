// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open System.Collections.Immutable
open Xunit

[<AbstractClass; Sealed>]
type Signatures private () =
    
    static member private _TypeMap =
        [|
            "Unit", QsTypeKind.UnitType;
            "Int", QsTypeKind.Int;
            "Double", QsTypeKind.Double;
            "String", QsTypeKind.String;
            "Qubit", QsTypeKind.Qubit;
            "Qubit[]", ResolvedType.New QsTypeKind.Qubit |> QsTypeKind.ArrayType;
        |] 
        |> Seq.map (fun (k, v) -> k, ResolvedType.New v) |> dict
    
    static member private _MakeSig input =
        let ns, name, args, rtrn = input
        let fullName = { Namespace = NonNullable<string>.New ns; Name = NonNullable<string>.New name }
        let argType =
            if Array.isEmpty args then
                Signatures._TypeMap.["Unit"]
            else
                args |> Seq.map (fun typ -> Signatures._TypeMap.[typ]) |> ImmutableArray.ToImmutableArray |> QsTypeKind.TupleType |> ResolvedType.New
        let returnType = Signatures._TypeMap.[rtrn]
        (fullName, argType, returnType)

    /// For all given namespaces in checkedNamespaces, checks that there are exactly
    /// the callables specified with targetSignatures in the given compilation.
    static member public SignatureCheck checkedNamespaces targetSignatures compilation =

        let getNs targetNs =
            match Seq.tryFind (fun (ns : QsNamespace) -> ns.Name.Value = targetNs) compilation.Namespaces with
            | Some ns -> ns
            | None -> sprintf "Expected but did not find namespace: %s" targetNs |> failwith

        let callableSigs =
            checkedNamespaces
            |> Seq.map (fun checkedNs -> getNs checkedNs)
            |> SyntaxExtensions.Callables
            |> Seq.map (fun call -> (call.FullName, call.Signature.ArgumentType, call.Signature.ReturnType))

        let doesCallMatchSig call signature =
            let (call_fullName : QsQualifiedName), call_argType, call_rtrnType = call
            let (sig_fullName : QsQualifiedName), sig_argType, sig_rtrnType = signature

            call_fullName.Namespace.Value = sig_fullName.Namespace.Value &&
            call_fullName.Name.Value.EndsWith sig_fullName.Name.Value &&
            call_argType = sig_argType &&
            call_rtrnType = sig_rtrnType

        let makeArgsString (args : ResolvedType) =
            match args.Resolution with
            | QsTypeKind.UnitType -> "()"
            | _ -> args |> (ExpressionToQs () |> ExpressionTypeToQs).Apply

        (*Tests that all target signatures are present*)
        for targetSig in targetSignatures do
            let sig_fullName, sig_argType, sig_rtrnType = targetSig
            callableSigs
            |> Seq.exists (fun callSig -> doesCallMatchSig callSig targetSig)
            |> (fun x -> Assert.True (x, sprintf "Expected but did not find: %s.%s %s : %A" sig_fullName.Namespace.Value sig_fullName.Name.Value (makeArgsString sig_argType) sig_rtrnType.Resolution))

        (*Tests that *only* targeted signatures are present*)
        for callSig in callableSigs do
            let sig_fullName, sig_argType, sig_rtrnType = callSig
            targetSignatures
            |> Seq.exists (fun targetSig -> doesCallMatchSig callSig targetSig)
            |> (fun x -> Assert.True (x, sprintf "Found unexpected callable: %s.%s %s : %A" sig_fullName.Namespace.Value sig_fullName.Name.Value (makeArgsString sig_argType) sig_rtrnType.Resolution))

    /// Names of several testing namespaces
    static member public MonomorphizationNs = "Microsoft.Quantum.Testing.Monomorphization"
    static member public GenericsNs = "Microsoft.Quantum.Testing.Generics"
    static member public IntrinsicResolutionNs = "Microsoft.Quantum.Testing.IntrinsicResolution"

    /// Expected callable signatures to be found when running Monomorphization tests
    static member public MonomorphizationSignatures =
        [|
            [| (*Test Case 1*)
                Signatures.MonomorphizationNs, "Test1", [||], "Unit";
                Signatures.GenericsNs, "Test1Main", [||], "Unit";

                Signatures.GenericsNs, "BasicGeneric", [|"Double"; "Int"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"String"; "String"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"Unit"; "Unit"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"String"; "Double"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"Int"; "Double"|], "Unit";
                Signatures.GenericsNs, "NoArgsGeneric", [||], "Double";
                Signatures.GenericsNs, "ReturnGeneric", [|"Double"; "String"; "Int"|], "Int";
                Signatures.GenericsNs, "ReturnGeneric", [|"String"; "Int"; "String"|], "String";
            |];
            [| (*Test Case 2*)
                Signatures.MonomorphizationNs, "Test2", [||], "Unit";
                Signatures.GenericsNs, "Test2Main", [||], "Unit";

                Signatures.GenericsNs, "ArrayGeneric", [|"Qubit"; "String"|], "Int";
                Signatures.GenericsNs, "ArrayGeneric", [|"Qubit"; "Int"|], "Int";
                Signatures.GenericsNs, "GenericCallsGeneric", [|"Qubit"; "Int"|], "Unit";
            |];
            [| (*Test Case 3*)
                Signatures.MonomorphizationNs, "Test3", [||], "Unit";
                Signatures.GenericsNs, "Test3Main", [||], "Unit";

                Signatures.GenericsNs, "GenericCallsSpecializations", [|"Double"; "String"; "Qubit[]"|], "Unit";
                Signatures.GenericsNs, "GenericCallsSpecializations", [|"Double"; "String"; "Double"|], "Unit";
                Signatures.GenericsNs, "GenericCallsSpecializations", [|"String"; "Int"; "Unit"|], "Unit";

                Signatures.GenericsNs, "BasicGeneric", [|"Qubit[]"; "Qubit[]"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"String"; "Qubit[]"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"Double"; "String"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"Qubit[]"; "Double"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"String"; "Double"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"Qubit[]"; "Unit"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"Int"; "Unit"|], "Unit";
                Signatures.GenericsNs, "BasicGeneric", [|"String"; "Int"|], "Unit";

                Signatures.GenericsNs, "ArrayGeneric", [|"Qubit"; "Qubit[]"|], "Int";
                Signatures.GenericsNs, "ArrayGeneric", [|"Qubit"; "Double"|], "Int";
                Signatures.GenericsNs, "ArrayGeneric", [|"Qubit"; "Unit"|], "Int";
            |]
        |]
        |> Seq.map (fun case -> Seq.map (fun _sig -> Signatures._MakeSig _sig) case)
        |> Seq.toArray

    /// Expected callable signatures to be found when running Intrinsic Resolution tests
    static member public IntrinsicResolutionSignatures =
        [|
            [|
                Signatures.IntrinsicResolutionNs, "IntrinsicResolutionTest", [||], "Unit";
                Signatures.IntrinsicResolutionNs, "LocalIntrinsic", [||], "Unit";
                Signatures.IntrinsicResolutionNs, "Override", [||], "Unit";
                Signatures.IntrinsicResolutionNs, "EnvironmentIntrinsic", [||], "Unit";
            |];
        |]
        |> Seq.map (fun case -> Seq.map (fun _sig -> Signatures._MakeSig _sig) case)
        |> Seq.toArray