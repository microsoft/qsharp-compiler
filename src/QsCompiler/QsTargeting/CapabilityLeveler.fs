// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Targeting.CapabilityLeveler

open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxTokens

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

    member this.LocalLevel with get() = localLevel and set(n) = if int n > int localLevel then localLevel <- n
    member this.CalledLevel with get() = calledLevel and set(n) = if int n > int calledLevel then calledLevel <- n

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
        
