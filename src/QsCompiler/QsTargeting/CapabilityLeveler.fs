// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Targeting.Leveler

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open System.Collections.Immutable
open System.Collections.Generic

type private SpecializationKey =
    {
        QualifiedName : QsQualifiedName
        Kind : QsSpecializationKind
        TypeArgString : string
    }

type private CapabilityLevelManager() =
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

    let SpecToKey (spec : QsSpecialization) =
        SpecInfoToKey spec.Parent spec.Kind spec.TypeArguments

    let StringToTypeArray (ts : string) =
        let lookupString s = 
            match keyTypes.TryGetValue(s) with
            | true, t -> t
            | false, _ -> ResolvedType.New(InvalidType)
        let typeSequence = ts.Split(sep) |> Seq.map lookupString 
        if typeSequence |> Seq.isEmpty
        then Null
        else Value (typeSequence |> ImmutableArray.ToImmutableArray)

    let RecordDependency callerKey calledKey =
        match dependencies.TryGetValue(callerKey) with
        | true, deps -> deps.Add(calledKey) |> ignore
        | false, _ -> let newDeps = new HashSet<SpecializationKey>()
                      newDeps.Add(calledKey) |> ignore
                      dependencies.[callerKey] <- newDeps

    let rec WalkDependencyTree root (accum : HashSet<SpecializationKey>) =
        match dependencies.TryGetValue(root) with
        | true, next -> 
            next |> Seq.fold (fun (a : HashSet<SpecializationKey>) k -> if a.Add(k) then WalkDependencyTree k a else a) accum
        | false, _ -> accum

    member this.GetSpecializationLevel(spec) =
        let key = SpecToKey spec
        match levels.TryGetValue(key) with
        | true, level -> level
        | false, _ -> CapabilityLevel.Unset

    member this.SetSpecializationLevel(spec, level) =
        let key = SpecToKey spec
        levels.[key] <- level

    member this.AddDependency(callerSpec, calledSpec) =
        let callerKey = SpecToKey callerSpec
        let calledKey = SpecToKey calledSpec
        RecordDependency callerKey calledKey

    member this.AddDependency(callerSpec, calledName, calledKind, calledTypeArgs) =
        let callerKey = SpecToKey callerSpec
        let calledKey = SpecInfoToKey calledName calledKind calledTypeArgs
        RecordDependency callerKey calledKey

    member this.AddDependency(callerName, callerKind, callerTypeArgs, calledName, calledKind, calledTypeArgs) =
        let callerKey = SpecInfoToKey callerName callerKind callerTypeArgs
        let calledKey = SpecInfoToKey calledName calledKind calledTypeArgs
        RecordDependency callerKey calledKey

    member this.FlushDependencies(callerSpec) =
        let key = SpecToKey callerSpec
        dependencies.Remove(key) |> ignore

    member this.GetDependencies(callerSpec) =
        let key = SpecToKey callerSpec
        match dependencies.TryGetValue(key) with
        | true, deps -> 
            deps |> Seq.map (fun key -> (key.QualifiedName, key.Kind, key.TypeArgString |> StringToTypeArray))
        | false, _ -> Seq.empty

    member this.GetDependencyTree(callerSpec) =
        let key = SpecToKey callerSpec
        WalkDependencyTree key (new HashSet<SpecializationKey>(key |> Seq.singleton))
        |> Seq.filter (fun k -> k <> key)
        |> Seq.map (fun key -> (key.QualifiedName, key.Kind, key.TypeArgString |> StringToTypeArray))

    member this.GetDependencyLevel(callerSpec) =
        let getLevel k =
            match levels.TryGetValue(k) with
            | true, level -> level
            | false, _ -> CapabilityLevel.Unset
        let key = SpecToKey callerSpec
        let deps = WalkDependencyTree key (new HashSet<SpecializationKey>(key |> Seq.singleton)) 
                   |> Seq.filter (fun k -> k <> key)
        if Seq.isEmpty deps 
        then CapabilityLevel.Unset
        else deps |> Seq.map getLevel |> Seq.max

let private manager = new CapabilityLevelManager()        

let private isResult ex =
    match ex.ResolvedType.Resolution with | Result -> true | _ -> false
        
type private CapabilityInfoHolder(spec) =
    let mutable localLevel = CapabilityLevel.Minimal

    member this.LocalLevel with get() = localLevel and set(n) = if n > localLevel then localLevel <- n

    member this.Specialization with get() = spec

type private ExpressionKindLeveler(holder : CapabilityInfoHolder) =
    inherit ExpressionKindTransformation()

    let exprXformer = new ExpressionLeveler(holder)
    let mutable inCall = false
    let mutable adjoint = false
    let mutable controlled = false
    let mutable isSimpleResultTest = true

    member this.IsSimpleResultTest with get() = isSimpleResultTest and set(value) = isSimpleResultTest <- value

    member private this.HandleCallable method arg =
        inCall <- true
        adjoint <- false
        controlled <- false
        let method = this.ExpressionTransformation method
        inCall <- false
        let arg = this.ExpressionTransformation arg
        CallLikeExpression(method, arg)

    override this.ExpressionTransformation x = exprXformer.Transform x
    override this.TypeTransformation x = x

    override this.onOperationCall(method, arg) =
        this.HandleCallable method arg

    override this.onFunctionCall(method, arg) =
        this.HandleCallable method arg

    override this.onAdjointApplication(ex) =
        adjoint <- true
        base.onAdjointApplication(ex)

    override this.onControlledApplication(ex) =
        controlled <- true
        base.onControlledApplication(ex)

    override this.onIdentifier(sym, typeArgs) =
        match sym with
        | GlobalCallable(name) ->
            if inCall
            then
                let kind = match adjoint, controlled with
                           | false, false -> QsBody
                           | false, true  -> QsControlled
                           | true,  false -> QsAdjoint
                           | true,  true  -> QsControlledAdjoint
                manager.AddDependency(holder.Specialization, name, kind, typeArgs)
            else
                // The callable is being used in a non-call context, such as being
                /// assigned to a variable or passed as an argument to another callable,
                // which means it could get a functor applied at some later time.
                // We're conservative and add all 4 possible kinds.
                manager.AddDependency(holder.Specialization, name, QsBody, typeArgs)
                manager.AddDependency(holder.Specialization, name, QsControlled, typeArgs)
                manager.AddDependency(holder.Specialization, name, QsAdjoint, typeArgs)
                manager.AddDependency(holder.Specialization, name, QsControlledAdjoint, typeArgs)
        | _ -> ()
        base.onIdentifier(sym, typeArgs)

    override this.Transform(kind) =
        match kind with 
        | UnwrapApplication _
        | ValueTuple _
        | ArrayItem _
        | NamedItem _
        | ValueArray _
        | NewArray _
        | IntLiteral _
        | BigIntLiteral _
        | DoubleLiteral _
        | BoolLiteral _
        | StringLiteral _
        | RangeLiteral _
        | CopyAndUpdate _
        | CONDITIONAL _
        | NEQ _
        | LT _ 
        | LTE _
        | GT _ 
        | GTE _
        | AND _
        | ADD _
        | SUB _
        | MUL _
        | DIV _
        | POW _
        | MOD _
        | LSHIFT _
        | RSHIFT _
        | BXOR _ 
        | BOR _
        | BAND _ 
        | NOT _
        | NEG _
        | BNOT _ -> 
            holder.LocalLevel <- CapabilityLevel.Medium
        | OR _  ->
            isSimpleResultTest <- false
        | EQ (ex1, ex2) -> 
            if not (isResult ex1 && isResult ex2)
            then isSimpleResultTest <- false
        | _ -> ()
        base.Transform(kind)

and private ExpressionLeveler(holder : CapabilityInfoHolder) =
    inherit ExpressionTransformation()

    let kindXformer = new ExpressionKindLeveler(holder)

    override this.Kind = upcast kindXformer

    member this.IsSimpleResultTest with get() = kindXformer.IsSimpleResultTest 
                                    and set(v) = kindXformer.IsSimpleResultTest <- v

type private StatementLeveler(holder : CapabilityInfoHolder) =
    inherit StatementKindTransformation()

    let scopeXformer = new ScopeLeveler(holder)
    let exprXformer = new ExpressionLeveler(holder)

    override this.ScopeTransformation x = scopeXformer.Transform x
    override this.ExpressionTransformation x = 
        exprXformer.IsSimpleResultTest <- true
        exprXformer.Transform x
    override this.TypeTransformation x = x
    override this.LocationTransformation x = x

    override this.onConditionalStatement(stm) =
        let processCase (condition, block : QsPositionedBlock) =
            let expr = this.ExpressionTransformation condition
            if not exprXformer.IsSimpleResultTest 
            then holder.LocalLevel <- CapabilityLevel.Medium
            else holder.LocalLevel <- CapabilityLevel.Basic
            let body = this.ScopeTransformation block.Body
            expr, QsPositionedBlock.New block.Comments block.Location body
        let cases = stm.ConditionalBlocks |> Seq.map processCase
        let defaultCase = stm.Default |> QsNullable<_>.Map (fun b -> this.onPositionedBlock (None, b) |> snd)
        QsConditionalStatement.New (cases, defaultCase) |> QsConditionalStatement

    override this.onRepeatStatement(s) =
        holder.LocalLevel <- CapabilityLevel.Advanced
        base.onRepeatStatement(s)

    override this.onWhileStatement(s) =
        holder.LocalLevel <- CapabilityLevel.Advanced
        base.onWhileStatement(s)

    override this.onValueUpdate(s) =
        holder.LocalLevel <- CapabilityLevel.Advanced
        base.onValueUpdate(s)

    override this.onVariableDeclaration(s) =
        if s.Kind = QsBindingKind.MutableBinding 
        then holder.LocalLevel <- CapabilityLevel.Advanced
        else
            if not (isResult s.Rhs)
            then holder.LocalLevel <- CapabilityLevel.Advanced
        base.onVariableDeclaration(s)

and private ScopeLeveler(holder : CapabilityInfoHolder) =
    inherit ScopeTransformation()

    override this.StatementKind = upcast new StatementLeveler(holder)

let ProcessSpecialization(callable : QsCallable, spec : QsSpecialization) =
    let checkForLevelAttributes (attrs : QsDeclarationAttribute seq) =
        let isLevelAttribute (a : QsDeclarationAttribute) = 
            match a.TypeId with
            | Value udt -> udt.Namespace = BuiltIn.LevelAttribute.Namespace && udt.Name = BuiltIn.LevelAttribute.Name
            | Null -> false
        let getLevelFromArgument (a : QsDeclarationAttribute) =
            match a.Argument.Expression with
            | IntLiteral n -> let level = enum<CapabilityLevel> (int n)
                              Some level
            | _ -> None
        let levels = attrs |> Seq.filter isLevelAttribute |> List.ofSeq
        match levels with
        | [ level ] -> getLevelFromArgument level
        | [] -> None
        | _ -> None
    let computeLevelFromImplementation () =
        match spec.Implementation with
        | SpecializationImplementation.Provided (_, code) -> 
            // Compute the capability levels by walking the code
            let holder = new CapabilityInfoHolder(spec)
            let xform = new ScopeLeveler(holder)
            xform.Transform code |> ignore
            Some holder.LocalLevel
        | SpecializationImplementation.Intrinsic -> 
            Some CapabilityLevel.Minimal
        | SpecializationImplementation.External -> 
            None
        | SpecializationImplementation.Generated _ -> 
            // TODO: Find the "base" specialization and use it's level
            Some CapabilityLevel.Unset
    // We always have to walk the implementation in order to reset call dependencies,
    // even if the local level is set by an attribute.
    manager.FlushDependencies(spec)
    let levelFromImplementation = computeLevelFromImplementation()
    // Now we can get the max level of called routines
    let calledLevel = manager.GetDependencyLevel(spec)

    // If there is a Level attribute for this specific specialization, use it; otherwise,
    // use the level from Level attribute for the containing callable, if any; as a last
    // resort, check the actual implementation.
    let localLevel = checkForLevelAttributes spec.Attributes
                     |> Option.orElse (checkForLevelAttributes callable.Attributes)
                     |> Option.orElse levelFromImplementation
                     |> Option.defaultValue CapabilityLevel.Unset
    manager.SetSpecializationLevel(spec, localLevel)

    (localLevel, calledLevel)

        
