// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Targeting.Leveler

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

// Some useful utility routines
let private isResult ex =
    match ex.ResolvedType.Resolution with | Result -> true | _ -> false
        
let private isQubitType (t : ResolvedType) =
    match t.Resolution with | Qubit -> true | _ -> false
        
let private isQubitArray ex =
    match ex.ResolvedType.Resolution with | ArrayType t when isQubitType t -> true | _ -> false
        
/// Tiny wrapper around a mutable capability level.
/// The reason for using this class, rather than a simple ref, is the logic in the setter;
/// that allows us to always just set the value, rather than have the test logic throughout the code.
type private CapabilityInfoHolder() =
    let mutable localLevel = CapabilityLevel.Minimal

    member this.LocalLevel with get() = localLevel and set(n) = if n > localLevel then localLevel <- n

/// Walker for setting capability levels based on expression details.
type private ExpressionKindLeveler(exprXformer : ExpressionLeveler, holder : CapabilityInfoHolder) =
    inherit ExpressionKindWalker()

    let mutable isSimpleResultTest = true

    member this.IsSimpleResultTest with get() = isSimpleResultTest and set(value) = isSimpleResultTest <- value

    override this.ExpressionWalker x = exprXformer.Walk x
    override this.TypeWalker x = ()

    override this.Walk(kind) =
        match kind with 
        | UnwrapApplication _
        | ValueTuple _
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
        | ArrayItem (arr, idx) ->
            if not (isQubitArray arr)
            then holder.LocalLevel <- CapabilityLevel.Medium
        | _ -> ()
        base.Walk(kind)

/// Walker for setting capability levels based on expressions.
and private ExpressionLeveler(holder : CapabilityInfoHolder) as this =
    inherit ExpressionWalker()

    let kindXformer = new ExpressionKindLeveler(this, holder)

    override this.Kind = upcast kindXformer

    member this.IsSimpleResultTest with get() = kindXformer.IsSimpleResultTest 
                                    and set(v) = kindXformer.IsSimpleResultTest <- v

/// Walker for setting capability levels based on statements.
type private StatementLeveler(scopeXformer : ScopeLeveler, holder : CapabilityInfoHolder) =
    inherit StatementKindWalker()

    let exprXformer = new ExpressionLeveler(holder)

    override this.ScopeWalker x = scopeXformer.Walk x

    override this.ExpressionWalker x = 
        exprXformer.IsSimpleResultTest <- true
        exprXformer.Walk x
    override this.TypeWalker x = ()
    override this.LocationWalker x = ()

    override this.onConditionalStatement(stm) =
        let processCase (condition, block : QsPositionedBlock) =
            this.ExpressionWalker condition
            if not exprXformer.IsSimpleResultTest 
            then holder.LocalLevel <- CapabilityLevel.Medium
            else holder.LocalLevel <- CapabilityLevel.Basic
            this.ScopeWalker block.Body
        stm.ConditionalBlocks |> Seq.iter processCase
        stm.Default |> QsNullable<_>.Iter (fun b -> this.onPositionedBlock (None, b))

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

/// Walker for setting capability levels based on scopes.
and private ScopeLeveler(holder : CapabilityInfoHolder) as this =
    inherit ScopeWalker()

    let kindXformer = new StatementLeveler(this, holder)

    override this.StatementKind = upcast kindXformer

    member this.Holder with get() = holder

/// This syntax tree transformer fills in the CapabilityLevel fields in specializations,
/// based on information gathered by the associated scope and other walkers.
type TreeLeveler() =
    inherit SyntaxTreeTransformation()

    // Checks to see if a callable or specialization has a developer-specified capability level,
    // which overrides the level computed from the code if it is present.
    let checkForLevelAttributes (attrs : QsDeclarationAttribute seq) =
        let isLevelAttribute (a : QsDeclarationAttribute) = 
            match a.TypeId with
            | Value udt -> udt.Namespace = BuiltIn.LevelAttribute.Namespace && udt.Name = BuiltIn.LevelAttribute.Name
            | Null -> false
        let getLevelFromArgument (a : QsDeclarationAttribute) =
            match a.Argument.Expression with
            | IntLiteral n -> let level = enum<CapabilityLevel> (int n)
                              Some level
            | ValueTuple args -> if args.Length = 1
                                 then
                                    match args.[0].Expression with
                                    | IntLiteral n -> let level = enum<CapabilityLevel> (int n)
                                                      Some level
                                    | _ -> None
                                 else None
            | _ -> None
        let levels = attrs |> Seq.filter isLevelAttribute |> List.ofSeq
        match levels with
        | [ level ] -> getLevelFromArgument level
        | [] -> None
        | _ -> None

    let mutable currentOperationLevel = None : CapabilityLevel option

    override this.Scope with get() = new ScopeTransformation()

    override this.beforeCallable(c) =
        currentOperationLevel <- c.Attributes |> checkForLevelAttributes
        let result = base.beforeCallable(c)
        result

    override this.onSpecializationImplementation(s) =
        let codeLevel = match s.Implementation with
                        | Intrinsic -> CapabilityLevel.Minimal
                        | External -> CapabilityLevel.Unset
                        | Generated _ -> CapabilityLevel.Unset
    // TODO: For generated specializations, we need to find the appropriate "body" declaration
    // and copy the required capability from that to the generated specialization.
    // If the body hasn't been processed yet, then process it right away, use the result here, 
    // and remember the result for later use so we don't compute it twice.
                        | Provided (_, scope) ->
                            let holder = new CapabilityInfoHolder()
                            let xform = new ScopeLeveler(holder)
                            xform.Walk(scope)
                            holder.LocalLevel
        let level = s.Attributes |> checkForLevelAttributes 
                                 |> Option.orElse currentOperationLevel
                                 |> Option.defaultValue codeLevel
        if level <> s.Signature.Information.InferredInformation.RequiredCapabilityLevel
        then { s with Signature = { s.Signature with Information = { s.Signature.Information with InferredInformation = { s.Signature.Information.InferredInformation with RequiredCapabilityLevel = level } } }}
        else s
