// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Targeting.CapabilityLeveler

open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open System.Collections.Immutable
open System.Collections.Generic
open System
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput

type internal SpecializationKey =
    {
        QualifiedName : QsQualifiedName
        Kind : QsSpecializationKind
        TypeArgString : string
    }

type CapabilityLevelManager() =
    let sep = '|'
    let levels = new Dictionary<SpecializationKey, CapabilityLevel>()
    let dependencies = new Dictionary<SpecializationKey, HashSet<SpecializationKey>>()
    let keyTypes = new Dictionary<string, ResolvedType>()

    let SpecInfoToKey name kind (types : QsNullable<ImmutableArray<ResolvedType>>) =
        let ResolvedTypeToString rt =
            let exprTransformer = new ExpressionToQs()
            let transformer = new ExpressionTypeToQs(exprTransformer)
            transformer.Apply(rt)
        let typeArgs = types.ValueOr (new ImmutableArray<ResolvedType>())
                       |> Seq.map (fun t -> (t, ResolvedTypeToString t))
        typeArgs |> Seq.iter (fun (t, s) -> keyTypes.[s] <- t)
        let typeArgString = typeArgs
                            |> Seq.map snd
                            |> String.concat (sep.ToString())
        { QualifiedName = name; Kind = kind; TypeArgString = typeArgString }

    let StringToTypeArray (ts : string) =
        let lookupString s = 
            match keyTypes.TryGetValue(s) with
            | true, t -> t
            | false, _ -> ResolvedType.New(InvalidType)
        let typeSequence = ts.Split(sep) |> Seq.map lookupString 
        if typeSequence |> Seq.isEmpty
        then Null
        else Value (typeSequence |> ImmutableArray.ToImmutableArray)

    let rec WalkDependencyTree root (accum : HashSet<SpecializationKey>) =
        match dependencies.TryGetValue(root) with
        | true, next -> 
            next |> Seq.fold (fun (a : HashSet<SpecializationKey>) k -> if a.Add(k) then WalkDependencyTree k a else a) accum
        | false, _ -> accum

    member this.GetSpecializationLevel(name, kind, types) =
        let key = SpecInfoToKey name kind types
        match levels.TryGetValue(key) with
        | true, level -> level
        | false, _ -> CapabilityLevel.Unset

    member this.SetSpecializationLevel(name, kind, types, level) =
        let key = SpecInfoToKey name kind types
        levels.[key] <- level

    member this.AddDependency(callerName, callerKind, callerTypes, calledName, calledKind, calledTypes) =
        let callerKey = SpecInfoToKey callerName callerKind callerTypes
        let calledKey = SpecInfoToKey calledName calledKind calledTypes
        match dependencies.TryGetValue(callerKey) with
        | true, deps -> deps.Add(calledKey) |> ignore
        | false, _ -> let newDeps = new HashSet<SpecializationKey>()
                      newDeps.Add(calledKey) |> ignore
                      dependencies.[callerKey] <- newDeps

    member this.FlushDependencies(callerName, callerKind, callerTypes) =
        let key = SpecInfoToKey callerName callerKind callerTypes
        dependencies.Remove(key) |> ignore

    member this.GetDependencies(callerName, callerKind, callerTypes) =
        let key = SpecInfoToKey callerName callerKind callerTypes
        match dependencies.TryGetValue(key) with
        | true, deps -> 
            deps |> Seq.map (fun key -> (key.QualifiedName, key.Kind, key.TypeArgString |> StringToTypeArray))
        | false, _ -> Seq.empty

    member this.GetDependencyTree(callerName, callerKind, callerTypes) =
        let key = SpecInfoToKey callerName callerKind callerTypes
        WalkDependencyTree key (new HashSet<SpecializationKey>(key |> Seq.singleton))
        |> Seq.filter (fun k -> k <> key)
        |> Seq.map (fun key -> (key.QualifiedName, key.Kind, key.TypeArgString |> StringToTypeArray))

    member this.GetDependencyLevel(callerName, callerKind, callerTypes) =
        let getLevel k =
            match levels.TryGetValue(k) with
            | true, level -> level
            | false, _ -> CapabilityLevel.Unset
        let key = SpecInfoToKey callerName callerKind callerTypes
        let deps = WalkDependencyTree key (new HashSet<SpecializationKey>(key |> Seq.singleton)) 
                   |> Seq.filter (fun k -> k <> key)
        if Seq.isEmpty deps 
        then CapabilityLevel.Unset
        else deps |> Seq.map getLevel |> Seq.max

type internal CapabilityInfoHolder(context : QsNamespace seq) =
    let mutable localLevel = CapabilityLevel.Minimal
    let mutable calledLevel = CapabilityLevel.Minimal
    let callables = context |> GlobalCallableResolutions

    member this.GetCallableLevel(name, modifier, types) =
        match callables.TryGetValue(name) with
        | true, callable -> 
            callable.Specializations |> Seq.filter (fun spec -> spec.Kind = modifier && spec.TypeArguments = types)
                                     |> Seq.map (fun a -> a.RequiredCapability)
                                     |> Seq.tryExactlyOne
        | false, _ -> None

    member this.LocalLevel with get() = localLevel and set(n) = if n > localLevel then localLevel <- n
    member this.CalledLevel with get() = calledLevel and set(n) = if n > calledLevel then calledLevel <- n

type internal ExpressionKindLeveler(holder : CapabilityInfoHolder) =
    inherit ExpressionKindTransformation()

    let exprXformer = new ExpressionLeveler(holder)

    override this.ExpressionTransformation x = exprXformer.Transform x
    override this.TypeTransformation x = x

    override this.onOperationCall(ex1, ex2) =
        base.onOperationCall(ex1, ex2)

and internal ExpressionLeveler(holder : CapabilityInfoHolder) =
    inherit ExpressionTransformation()

    let kindXformer = new ExpressionKindLeveler(holder)

and internal StatementLeveler(holder : CapabilityInfoHolder) =
    inherit StatementKindTransformation()

    let scopeXformer = new ScopeLeveler(holder)
    let exprXformer = new ExpressionLeveler(holder)

    override this.ScopeTransformation x = scopeXformer.Transform x
    override this.ExpressionTransformation x = exprXformer.Transform x
    override this.TypeTransformation x = x
    override this.LocationTransformation x = x

and internal ScopeLeveler(holder : CapabilityInfoHolder) =
    inherit ScopeTransformation()

    override this.StatementKind = upcast new StatementLeveler(holder)

type TreeLeveler(context : QsNamespace seq) =
    inherit SyntaxTreeTransformation()

    let mutable currentCallableLevel = None : (CapabilityLevel * CapabilityLevel) option

    let checkForLevelAttributes (attrs : QsDeclarationAttribute seq) =
        let isLevelAttribute (a : QsDeclarationAttribute) = 
            match a.TypeId with
            | Value udt -> udt.Namespace = BuiltIn.LevelAttribute.Namespace && udt.Name = BuiltIn.LevelAttribute.Name
            | Null -> false
        let getLevelFromArgument (a : QsDeclarationAttribute) =
            match a.Argument.Expression with
            | IntLiteral n -> let level = enum<CapabilityLevel> (int n)
                              Some (level, level)
            | _ -> None
        let levels = attrs |> Seq.filter isLevelAttribute |> List.ofSeq
        match levels with
        | [ level ] -> getLevelFromArgument level
        | [] -> None
        | _ -> None

    let transformSpecialization (spec : QsSpecialization) =
        let computeLevelFromImplementation () =
            match spec.Implementation with
            | SpecializationImplementation.Provided (_, code) -> 
                // Compute the capability levels by walking the code
                let holder = new CapabilityInfoHolder(context)
                let xform = new ScopeLeveler(holder)
                xform.Transform code |> ignore
                Some (holder.LocalLevel, holder.CalledLevel)
            | SpecializationImplementation.Intrinsic -> 
                Some (CapabilityLevel.Minimal, CapabilityLevel.Minimal)
            | SpecializationImplementation.External -> 
                None
            | SpecializationImplementation.Generated _ -> 
                // TODO: Find the "base" specialization and use it's levels
                Some (CapabilityLevel.Unset, CapabilityLevel.Unset)
        // If there is a Level attribute for this specific specialization, use it; otherwise,
        // use the level from Level attribute for the containing callable, if any; as a last
        // resort, check the actual implementation.
        let local, called = spec.Attributes 
                            |> checkForLevelAttributes 
                            |> Option.orElse currentCallableLevel
                            |> Option.orElseWith computeLevelFromImplementation
                            |> Option.defaultValue (CapabilityLevel.Unset, CapabilityLevel.Unset)
        { spec with LocalRequiredCapability = local; RequiredCapability = called }
    
    override this.onSpecializationImplementation spec = 
        transformSpecialization spec

    override this.beforeCallable callable =
        currentCallableLevel <- checkForLevelAttributes callable.Attributes
        base.beforeCallable callable
        
