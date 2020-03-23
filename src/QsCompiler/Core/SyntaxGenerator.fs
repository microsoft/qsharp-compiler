﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System
open System.Collections.Immutable
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


// transformations used to strip range information for auto-generated syntax

type private StripPositionInfoFromType (parent : StripPositionInfo) =
    inherit TypeTransformation(parent)
    override this.OnRangeInformation _ = Null

and private StripPositionInfoFromExpression (parent : StripPositionInfo) =
    inherit ExpressionTransformation(parent)
    override this.OnRangeInformation _ = Null

and private StripPositionInfoFromStatement(parent : StripPositionInfo) =
    inherit StatementTransformation(parent)
    override this.OnLocation _ = Null

and private StripPositionInfoFromNamespace(parent : StripPositionInfo) =
    inherit NamespaceTransformation(parent)
    override this.OnLocation _ = Null

and public StripPositionInfo private (_internal_) =
    inherit SyntaxTreeTransformation()
    static let defaultInstance = new StripPositionInfo()

    new () as this =
        StripPositionInfo("_internal_") then
            this.Types <- new StripPositionInfoFromType(this)
            this.Expressions <- new StripPositionInfoFromExpression(this)
            this.Statements <- new StripPositionInfoFromStatement(this)
            this.Namespaces <- new StripPositionInfoFromNamespace(this)

    static member public Default = defaultInstance
    static member public Apply t = defaultInstance.Types.OnType t
    static member public Apply e = defaultInstance.Expressions.OnTypedExpression e
    static member public Apply s = defaultInstance.Statements.OnScope s
    static member public Apply a = defaultInstance.Namespaces.OnNamespace a


module SyntaxGenerator =

    /// Matches only if the string consists of a fully qualified name and nothing else.
    let internal FullyQualifiedName =
        new Regex(@"^[\p{L}_][\p{L}\p{Nd}_]*(\.[\p{L}_][\p{L}\p{Nd}_]*)+$")


    // literal expressions

    /// Builds an immutable typed expression of the given kind and type kind,
    /// setting the quantum dependency to the given value and assuming no type parameter resolutions.
    /// Sets the range information for the built expression to Null.
    let private AutoGeneratedExpression kind exTypeKind qDep =
        let noInferredInfo = InferredExpressionInformation.New (false, quantumDep = qDep)
        TypedExpression.New (kind, ImmutableDictionary.Empty, exTypeKind |> ResolvedType.New, noInferredInfo, QsRangeInfo.Null)

    /// Creates a typed expression that corresponds to a Unit value.
    /// Sets the range information for the built expression to Null.
    let UnitValue =
        AutoGeneratedExpression UnitValue QsTypeKind.UnitType false

    /// Creates a typed expression that corresponds to an Int literal with the given value.
    /// Sets the range information for the built expression to Null.
    let IntLiteral v =
        AutoGeneratedExpression (IntLiteral v) QsTypeKind.Int false

    /// Creates a typed expression that corresponds to a BigInt literal with the given value.
    /// Sets the range information for the built expression to Null.
    let BigIntLiteral (v : int) =
        AutoGeneratedExpression (BigIntLiteral (bigint v)) QsTypeKind.BigInt false

    /// Creates a typed expression that corresponds to a Double literal with the given value.
    /// Sets the range information for the built expression to Null.
    let DoubleLiteral v =
        AutoGeneratedExpression (DoubleLiteral v) QsTypeKind.Double false

    /// Creates a typed expression that corresponds to a String literal with the given value and interpolation arguments.
    /// Sets the range information for the built expression to Null.
    let StringLiteral (s, interpolArgs) =
        AutoGeneratedExpression (StringLiteral (s, interpolArgs)) QsTypeKind.String false

    /// Creates a typed expression that corresponds to a Range literal with the given left hand side and right hand side.
    /// Sets the range information for the built expression to Null.
    /// Does *not* verify the given left and right hand side.
    let RangeLiteral (lhs, rhs) =
        AutoGeneratedExpression (RangeLiteral (lhs, rhs)) QsTypeKind.Range false


    // utils to for building typed expressions and iterable inversions

    /// Creates a typed expression corresponding to a call to a non-type-parametrized callable.
    /// Does *not* verify whether the given lhs and rhs besides
    /// throwing an ArgumentException if the type of the given lhs is valid but not a Function or Operation type.
    let private CallNonGeneric (lhs : TypedExpression, rhs : TypedExpression) =
        let kind = CallLikeExpression (lhs, rhs)
        let quantumDep = lhs.InferredInformation.HasLocalQuantumDependency || rhs.InferredInformation.HasLocalQuantumDependency
        match lhs.ResolvedType.Resolution with
        | QsTypeKind.InvalidType            -> AutoGeneratedExpression kind InvalidType quantumDep
        | QsTypeKind.Operation ((_, ot), _)
        | QsTypeKind.Function (_,ot)        -> AutoGeneratedExpression kind ot.Resolution quantumDep
        | _ -> ArgumentException "given lhs is not callable" |> raise

    /// Given a typed expression of type range,
    /// creates a typed expression that when executed will generate the reverse sequence for the given range.
    /// Assumes that the RangeReverse function is part of the standard library.
    /// Throws an ArgumentException if the given expression is of a valid type but not of type Range.
    let private ReverseRange (ex : TypedExpression) =
        let buildCallToReverse ex =
            let kind = Identifier.GlobalCallable BuiltIn.RangeReverse.FullName
            let exTypeKind = QsTypeKind.Function (QsTypeKind.Range |> ResolvedType.New, QsTypeKind.Range |> ResolvedType.New)
            let reverse = AutoGeneratedExpression (QsExpressionKind.Identifier (kind, Null)) exTypeKind false
            CallNonGeneric (reverse, ex)
        match ex.ResolvedType.Resolution with
        | QsTypeKind.InvalidType
        | QsTypeKind.Range _     -> buildCallToReverse ex
        | _ -> ArgumentException "given expression is not a range" |> raise

    /// Builds a range expression for the given lhs and rhs of the range operator.
    /// Does *not* verify the type of the given lhs or rhs.
    let private RangeExpression (lhs : TypedExpression, rhs : TypedExpression) =
        let kind = QsExpressionKind.RangeLiteral (lhs, rhs)
        let quantumDep = lhs.InferredInformation.HasLocalQuantumDependency || rhs.InferredInformation.HasLocalQuantumDependency
        AutoGeneratedExpression kind QsTypeKind.Range quantumDep

    /// Creates a typed expression that corresponds to a call to the Length function.
    /// The Length function needs to be part of the QsCore library, and its type parameter name needs to match the one here.
    /// Throws an ArgumentException if the given expression is not of type array or of invalid type.
    let private Length (ex : TypedExpression) =
        let callableName = BuiltIn.Length.FullName
        let kind = Identifier.GlobalCallable callableName
        let typeParameterName =
            match BuiltIn.Length.Kind with
            | BuiltInKind.Function typeParams -> typeParams.[0]
            | _ -> ArgumentException "Length is expected to be a function" |> raise
        let typeParameter = QsTypeParameter.New (callableName, typeParameterName, Null)
        let genArrayType = QsTypeKind.ArrayType (QsTypeKind.TypeParameter typeParameter |> ResolvedType.New) |> ResolvedType.New
        let exTypeKind = QsTypeKind.Function (genArrayType, QsTypeKind.Int |> ResolvedType.New)
        let length = AutoGeneratedExpression (QsExpressionKind.Identifier (kind, Null)) exTypeKind false
        let callToLength tpRes =
            let resolutions = (seq { yield (typeParameter.Origin, typeParameter.TypeName, tpRes) }).ToImmutableArray()
            {CallNonGeneric (length, ex) with TypeArguments = resolutions}
        match ex.ResolvedType.Resolution with
        | ArrayType b -> callToLength b
        | InvalidType -> callToLength ex.ResolvedType
        | _ -> ArgumentException "the given expression is not of array type" |> raise

    /// Creates a typed expression that corresponds to subtracting one from the call to the Length function.
    /// Sets any range information in the built expression to Null, including inner expressions.
    /// Throws an ArgumentException if the given expression is not of type array or of invalid type.
    let LengthMinusOne (ex : TypedExpression) =
        let callToLength = ex |> StripPositionInfo.Apply |> Length
        let kind = QsExpressionKind.SUB (callToLength, IntLiteral 1L)
        AutoGeneratedExpression kind QsTypeKind.Int callToLength.InferredInformation.HasLocalQuantumDependency

    /// Given a typed expression of array type,
    /// creates a typed expression that when evaluated returns a new array with the order of the elements reversed.
    /// Throws an ArgumentException if the given expression is not of type array or of invalid type.
    let private ReverseArray (ex : TypedExpression) =
        let built =
            let reversingRange = RangeExpression (RangeExpression (LengthMinusOne ex, IntLiteral -1L), IntLiteral 0L)
            let kind = ArrayItem (ex, reversingRange)
            AutoGeneratedExpression kind ex.ResolvedType.Resolution ex.InferredInformation.HasLocalQuantumDependency
        match ex.ResolvedType.Resolution with
        | ArrayType _
        | InvalidType -> built
        | _ -> ArgumentException "the given expression is not of array type" |> raise

    /// Given a typed expression of a type that supports iteration,
    /// creates a typed expression that when evaluated returns the reversed sequence.
    /// Throws an ArgumentException if the given expression is of a valid type but not either of type Range or of array type.
    let ReverseIterable (ex : TypedExpression) =
        let ex = StripPositionInfo.Apply ex
        match ex.ResolvedType.Resolution with
        | QsTypeKind.Range -> ReverseRange ex
        | QsTypeKind.ArrayType _ -> ReverseArray ex
        | QsTypeKind.InvalidType -> ex
        | _ -> ArgumentException "the given expression is not iterable" |> raise

    /// Returns a boolean expression that evaluates to true if the given expression is negative.
    /// Returns an invalid expression of type Bool if the given expression is invalid.
    /// Throws an ArgumentException if the type of the given expression does not support arithmetic.
    let IsNegative (ex : TypedExpression) =
        let kind = ex.ResolvedType.Resolution |> function
            | Int -> LT (ex, IntLiteral 0L)
            | BigInt -> LT (ex, BigIntLiteral 0)
            | Double -> LT (ex, DoubleLiteral 0.)
            | InvalidType -> InvalidExpr
            | _ -> ArgumentException "the type of the given expression does not support arithmetic operations" |> raise
        AutoGeneratedExpression kind Bool ex.InferredInformation.HasLocalQuantumDependency


    // utils related to building and generating functor specializations

    let private QubitArray = Qubit |> ResolvedType.New |> ArrayType |> ResolvedType.New

    /// Given a QsTuple, recursively extracts and returns all of its items.
    let ExtractItems (this : QsTuple<_>) =
        this.Items.ToImmutableArray()

    /// Strips all range information from the given signature.
    let WithoutRangeInfo (signature : ResolvedSignature) =
        let argType = signature.ArgumentType |> StripPositionInfo.Apply
        let returnType = signature.ReturnType |> StripPositionInfo.Apply
        ResolvedSignature.New ((argType, returnType), signature.Information, signature.TypeParameters)

    /// Given the resolved argument type of an operation, returns the argument type of its controlled version.
    let AddControlQubits (argT : ResolvedType) =
        [QubitArray; argT].ToImmutableArray() |> TupleType |> ResolvedType.New

    /// Given a resolved signature, returns the corresponding signature for the controlled version.
    let BuildControlled (this : ResolvedSignature) =
        { this with ArgumentType = this.ArgumentType |> AddControlQubits }

    /// Given an argument tuple of a callable, the name and the range of the control qubits symbol, as well as the position offset for that range,
    /// builds and returns the argument tuple for the controlled specialization.
    /// Throws an ArgumentException if the given argument tuple is not a QsTuple.
    let WithControlQubits arg offset (name, symRange : QsNullable<_>) =
        let range = symRange.ValueOr QsCompilerDiagnostic.DefaultRange
        let ctlQs = LocalVariableDeclaration.New false ((offset, range), name, QubitArray, false)
        let unitArg =
            let argName = NonNullable<_>.New InternalUse.UnitArgument |> ValidName;
            let unitT = UnitType |> ResolvedType.New
            LocalVariableDeclaration.New false ((offset,range), argName, unitT, false) |> QsTupleItem // the range is not accurate here, but also irrelevant
        match arg with
        | QsTuple ts when ts.Length = 0 -> [ctlQs |> QsTupleItem; unitArg].ToImmutableArray()
        | QsTuple ts when ts.Length = 1 -> [ctlQs |> QsTupleItem; ts.[0]].ToImmutableArray()
        | QsTuple _                     -> [ctlQs |> QsTupleItem; arg].ToImmutableArray()
        | _                             -> ArgumentException "expecting the given argument tuple to be a QsTuple" |> raise
        |> QsTuple

    /// Given a typed expression that is used as argument to an operation and a typed expression for the control qubits,
    /// combines them to a suitable argument for the controlled version of the originally called operation under the assumption that the argument was correct.
    /// The range information for the built expression is set to Null.
    /// Throws an ArgumentException if the given expression for the control qubits is a valid type but not of type Qubit[].
    let ArgumentWithControlQubits (arg : TypedExpression) (ctlQs : TypedExpression) =
        let isInvalid = function
            | Tuple _ | Item _ | Missing -> false
            | _ -> true
        if ctlQs.ResolvedType.Resolution <> QubitArray.Resolution && not (ctlQs.ResolvedType |> isInvalid) then
            new ArgumentException "expression for the control qubits is valid but not of type Qubit[]" |> raise
        let buildControlledArgument orig =
            let kind = QsExpressionKind.ValueTuple ([ctlQs; orig].ToImmutableArray())
            let quantumDep = orig.InferredInformation.HasLocalQuantumDependency || ctlQs.InferredInformation.HasLocalQuantumDependency
            let exInfo = InferredExpressionInformation.New (isMutable = false, quantumDep = quantumDep)
            TypedExpression.New (kind, orig.TypeParameterResolutions, AddControlQubits orig.ResolvedType, exInfo, QsRangeInfo.Null)
        buildControlledArgument arg

    /// Returns the name of the control qubits
    /// if the given argument tuple is consistent with the argument tuple of a controlled specialization.
    /// Return null otherwise, or if the name of the control qubits is invalid.
    let ControlledFunctorArgument arg =
        let getItemName = function
        | QsTupleItem (item : LocalVariableDeclaration<QsLocalSymbol>) ->
            item.VariableName |> function
            | ValidName name -> name.Value
            | InvalidName -> null
        | _ -> null
        match arg with
        | QsTuple ts when ts.Length = 2 -> ts.[0] |> getItemName
        | _ -> null

    /// Creates a typed expression that corresponds to an immutable local variable with the given name
    /// that contains an expression of type Qubit[] and has no quantum dependencies.
    let ImmutableQubitArrayWithName name =
        let kind = (Identifier.LocalVariable name, Null) |> QsExpressionKind.Identifier
        let exTypeKind = QsTypeKind.ArrayType (QsTypeKind.Qubit |> ResolvedType.New)
        AutoGeneratedExpression kind exTypeKind false

    /// Given a typed expression of operation type, creates a typed expression corresponding to a Controlled application on that operation.
    /// Creates an expression of invalid type if the given expression is invalid.
    /// Blindly builds the controlled application if the characteristics of the given operation are invalid.
    /// Throws an ArgumentException if the given expression is valid but not of an operation type, or if it does not support the Controlled functor.
    let ControlledOperation (ex : TypedExpression) =
        let ex = StripPositionInfo.Apply ex
        let kind = QsExpressionKind.ControlledApplication ex
        let built exTypeKind = AutoGeneratedExpression kind exTypeKind ex.InferredInformation.HasLocalQuantumDependency
        let ctlOpType ((it, ot), opInfo) = QsTypeKind.Operation ((AddControlQubits it, ot), opInfo)
        match ex.ResolvedType.Resolution with
        | QsTypeKind.InvalidType -> built InvalidType
        | QsTypeKind.Operation ((it,ot), opInfo) when opInfo.Characteristics.AreInvalid -> ctlOpType ((it, ot), opInfo) |> built
        | QsTypeKind.Operation ((it,ot), opInfo) -> opInfo.Characteristics.SupportedFunctors |> function
            | Value functors when functors.Contains Controlled -> ctlOpType ((it, ot), opInfo) |> built
            | _ -> ArgumentException "given expression does not correspond to a suitable operation" |> raise
        | _ -> ArgumentException "given expression does not correspond to a suitable operation" |> raise

    /// Given a typed expression of operation type, creates a typed expression corresponding to a Adjoint application on that operation.
    /// Creates an expression of invalid type if the given expression is invalid.
    /// Blindly builds the adjoint application if the characteristics of the given operation are invalid.
    /// Throws an ArgumentException if the given expression is valid but not of an operation type, or if it does not support the Adjoint functor.
    let AdjointOperation (ex : TypedExpression) =
        let ex = StripPositionInfo.Apply ex
        let kind = QsExpressionKind.AdjointApplication (ex)
        let built = AutoGeneratedExpression kind ex.ResolvedType.Resolution ex.InferredInformation.HasLocalQuantumDependency
        match ex.ResolvedType.Resolution with
        | QsTypeKind.InvalidType -> built
        | QsTypeKind.Operation (_, opInfo) when opInfo.Characteristics.AreInvalid -> built
        | QsTypeKind.Operation (_, opInfo) -> opInfo.Characteristics.SupportedFunctors |> function
            | Value functors when functors.Contains Adjoint -> built
            | _ -> ArgumentException "given expression does not correspond to a suitable operation" |> raise
        | _ -> ArgumentException "given expression does not correspond to a suitable operation" |> raise
