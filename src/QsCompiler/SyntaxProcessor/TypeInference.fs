// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open System.Collections.Immutable
open System.Collections.Generic

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxProcessing.VerificationTools
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Utils

/// used for type matching arguments in call-like expressions
type internal Variance =
    | Covariant
    | Contravariant
    | Invariant

let private isSubsetOf info1 info2 =
    info1.Characteristics.GetProperties().IsSubsetOf(info2.Characteristics.GetProperties())

// TODO
let private greatestSubtype = List.head

type Constraint = | Concatenates

type InferenceContext(origin) =
    let mutable count = 0

    let substitutions = Dictionary()

    let constraints = Dictionary()

    let fresh () =
        let name = sprintf "__t%d__" count
        count <- count + 1

        {
            Origin = origin
            TypeName = name
            Range = Null
        }

    let bind param typeKind =
        match substitutions.TryGetValue param |> tryOption with
        | Some v -> substitutions.[param] <- typeKind :: v
        | None -> substitutions.[param] <- [ typeKind ]

    member internal context.Substitutions = substitutions |> Seq.map (fun item -> item.Key, item.Value) |> Map.ofSeq

    member context.Fresh() =
        fresh () |> TypeParameter |> ResolvedType.New

    member context.Unify(left: ResolvedType, right: ResolvedType) =
        // TODO: Make sure type parameters are actually placeholders created by this context and not foralls.
        match left.Resolution, right.Resolution with
        | TypeParameter param, resolution
        | resolution, TypeParameter param -> bind param resolution
        | ArrayType item1, ArrayType item2 -> context.Unify(item1, item2)
        | TupleType items1, TupleType items2 -> Seq.zip items1 items2 |> Seq.iter context.Unify
        | QsTypeKind.Operation ((in1, out1), info1), QsTypeKind.Operation ((in2, out2), info2) when isSubsetOf
                                                                                                        info1
                                                                                                        info2 ->
            // TODO: Variance.
            [ in1, in2; out1, out2 ] |> List.iter context.Unify
        | QsTypeKind.Function (in1, out1), QsTypeKind.Function (in2, out2) ->
            // TODO: Variance.
            [ in1, in2; out1, out2 ] |> List.iter context.Unify
        | _ -> failwithf "Cannot unify %A <: %A" left.Resolution right.Resolution

    member context.Constrain(resolvedType: ResolvedType, ``constraint``) =
        match resolvedType.Resolution with
        | TypeParameter param -> constraints.Add(param, ``constraint``)
        | _ ->
            match ``constraint`` with
            | Concatenates ->
                if resolvedType.supportsConcatenation |> Option.isSome |> not
                then failwithf "%A cannot concatenate" resolvedType.Resolution

    member context.Resolve typeKind =
        match typeKind with
        | TypeParameter param ->
            let t =
                substitutions.TryGetValue param
                |> tryOption
                |> Option.map (List.map context.Resolve >> greatestSubtype)
                |> Option.defaultValue typeKind

            match constraints.TryGetValue param |> tryOption with
            | Some Concatenates ->
                if (ResolvedType.New t).supportsConcatenation |> Option.isSome |> not
                then failwithf "%A cannot concatenate" t
            | None -> ()

            t
        | ArrayType array -> context.Resolve array.Resolution |> ResolvedType.New |> ArrayType
        | TupleType tuple ->
            tuple
            |> Seq.map (fun item -> context.Resolve item.Resolution |> ResolvedType.New)
            |> ImmutableArray.CreateRange
            |> TupleType
        | QsTypeKind.Operation ((inType, outType), info) ->
            let inType = context.Resolve inType.Resolution |> ResolvedType.New
            let outType = context.Resolve outType.Resolution |> ResolvedType.New
            QsTypeKind.Operation((inType, outType), info)
        | QsTypeKind.Function (inType, outType) ->
            let inType = context.Resolve inType.Resolution |> ResolvedType.New
            let outType = context.Resolve outType.Resolution |> ResolvedType.New
            QsTypeKind.Function(inType, outType)
        | _ -> typeKind

module InferenceContext =
    [<CompiledName "Resolver">]
    let resolver (context: InferenceContext) =
        printfn "%A" context.Substitutions

        let types =
            { new TypeTransformation() with
                member this.OnTypeParameter param = TypeParameter param |> context.Resolve
            }

        SyntaxTreeTransformation(Types = types)
