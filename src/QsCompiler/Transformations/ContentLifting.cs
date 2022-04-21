// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.Transformations.ContentLifting
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ParameterTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, string, ResolvedType>>;

    /// <summary>
    /// Static class to for holding the LiftOperationBody and the LiftFunctionBody static functions.
    /// </summary>
    internal static class LiftContent
    {
        private static IEnumerable<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>> FlattenParamTuple(ParameterTuple parameters)
        {
            if (parameters is ParameterTuple.QsTupleItem item)
            {
                return new[] { item.Item };
            }
            else if (parameters is ParameterTuple.QsTuple tuple)
            {
                return tuple.Item.SelectMany(FlattenParamTuple);
            }

            return ImmutableArray<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>.Empty;
        }

        private static ResolvedType ExtractParamType(ParameterTuple parameters)
        {
            if (parameters is ParameterTuple.QsTupleItem item)
            {
                return ResolvedType.New(item.Item.Type.Resolution);
            }
            else if (parameters is ParameterTuple.QsTuple tuple)
            {
                if (tuple.Item.Length == 0)
                {
                    return ResolvedType.New(ResolvedTypeKind.UnitType);
                }
                else if (tuple.Item.Length == 1)
                {
                    return ExtractParamType(tuple.Item[0]);
                }
                else
                {
                    return ResolvedType.New(ResolvedTypeKind.NewTupleType(tuple.Item.Select(ExtractParamType).ToImmutableArray()));
                }
            }

            return ResolvedType.New(ResolvedTypeKind.UnitType);
        }

        private static ParameterTuple ConcatParams(ParameterTuple first, ParameterTuple second)
        {
            var firstItems =
                first is ParameterTuple.QsTuple firstTup
                ? firstTup.Item
                : new[] { first }.ToImmutableArray();

            var secondItems =
                second is ParameterTuple.QsTuple secondTup
                ? secondTup.Item
                : new[] { second }.ToImmutableArray();

            return ParameterTuple.NewQsTuple(firstItems.Concat(secondItems).ToImmutableArray());
        }

        private static (ResolvedSignature, IEnumerable<QsSpecialization>) MakeSpecializations(
                QsCallable callingCallable,
                QsQualifiedName callableName,
                ResolvedType paramsType,
                SpecializationImplementation bodyImplementation,
                CallableInformation callableInformation,
                bool isFunction,
                ResolvedType returnType)
        {
            QsSpecialization MakeSpec(QsSpecializationKind kind, ResolvedSignature signature, SpecializationImplementation impl) =>
                new QsSpecialization(
                    kind,
                    callableName,
                    ImmutableArray<QsDeclarationAttribute>.Empty,
                    callingCallable.Source,
                    QsNullable<QsLocation>.Null,
                    QsNullable<ImmutableArray<ResolvedType>>.Null,
                    signature,
                    impl,
                    ImmutableArray<string>.Empty,
                    QsComments.Empty);

            // If we are making the body of a function, we can skip the rest of this function.
            if (isFunction)
            {
                var funcSig = new ResolvedSignature(
                    callingCallable.Signature.TypeParameters,
                    paramsType,
                    returnType,
                    CallableInformation.NoInformation);

                var funcSpecializations = new List<QsSpecialization>() { MakeSpec(QsSpecializationKind.QsBody, funcSig, bodyImplementation) };

                return (funcSig, funcSpecializations);
            }

            var newSig = new ResolvedSignature(
                callingCallable.Signature.TypeParameters,
                paramsType,
                returnType,
                callableInformation);

            var controlledSig = new ResolvedSignature(
                newSig.TypeParameters,
                ResolvedType.New(ResolvedTypeKind.NewTupleType(ImmutableArray.Create(
                    ResolvedType.New(ResolvedTypeKind.NewArrayType(ResolvedType.New(ResolvedTypeKind.Qubit))),
                    newSig.ArgumentType))),
                newSig.ReturnType,
                newSig.Information);

            var specializations = new List<QsSpecialization>() { MakeSpec(QsSpecializationKind.QsBody, newSig, bodyImplementation) };

            var addAdjoint = false;
            var addControlled = false;
            if (callableInformation.Characteristics.SupportedFunctors.IsValue)
            {
                addAdjoint = callableInformation.Characteristics.SupportedFunctors.Item.Contains(QsFunctor.Adjoint);
                addControlled = callableInformation.Characteristics.SupportedFunctors.Item.Contains(QsFunctor.Controlled);
            }

            if (addAdjoint)
            {
                specializations.Add(MakeSpec(
                    QsSpecializationKind.QsAdjoint,
                    newSig,
                    SpecializationImplementation.NewGenerated(QsGeneratorDirective.Invert)));
            }

            if (addControlled)
            {
                specializations.Add(MakeSpec(
                    QsSpecializationKind.QsControlled,
                    controlledSig,
                    SpecializationImplementation.NewGenerated(QsGeneratorDirective.Distribute)));
            }

            if (addAdjoint && addControlled)
            {
                specializations.Add(MakeSpec(
                    QsSpecializationKind.QsControlledAdjoint,
                    controlledSig,
                    SpecializationImplementation.NewGenerated(QsGeneratorDirective.Distribute)));
            }

            return (newSig, specializations);
        }

        private static (QsCallable, ResolvedType) GenerateCallable(
            QsCallable callingCallable,
            QsScope contents,
            ParameterTuple parameters,
            CallableInformation callableInformation,
            bool isFunction,
            ResolvedType returnType)
        {
            var newName = NameGenerator.GenerateCallableName(callingCallable.FullName);
            var paramTypes = ExtractParamType(parameters);

            // Update the scope to have Known Symbols equal to its parameter list
            var newContents = new QsScope(
                contents.Statements,
                new LocalDeclarations(FlattenParamTuple(parameters)
                    .Select(decl => new LocalVariableDeclaration<string, ResolvedType>(
                        ((QsLocalSymbol.ValidName)decl.VariableName).Item,
                        decl.Type,
                        decl.InferredInformation,
                        decl.Position,
                        decl.Range))
                    .ToImmutableArray()));

            var (signature, specializations) = MakeSpecializations(callingCallable, newName, paramTypes, SpecializationImplementation.NewProvided(parameters, newContents), callableInformation, isFunction, returnType);

            var generatedCallable = new QsCallable(
                isFunction
                    ? QsCallableKind.Function
                    : QsCallableKind.Operation,
                newName,
                ImmutableArray<QsDeclarationAttribute>.Empty,
                Access.Internal,
                callingCallable.Source,
                QsNullable<QsLocation>.Null,
                signature,
                parameters,
                specializations.ToImmutableArray(),
                ImmutableArray<string>.Empty,
                QsComments.Empty);

            // Change the origin of all type parameter references to use the new name and make all variables immutable
            generatedCallable = UpdateGeneratedCallable.Apply(generatedCallable, parameters, callingCallable.FullName, newName);

            // We want to use the non-updated param and return types here so that they refer to
            // the original callable's type parameters. We do this because that is what they will
            // need to be for the call expression.
            var generatedCallableCallType = ResolvedType.New(isFunction
                ? ResolvedTypeKind.NewFunction(paramTypes, returnType)
                : ResolvedTypeKind.NewOperation(
                    Tuple.Create(paramTypes, returnType),
                    generatedCallable.Signature.Information));

            return (generatedCallable, generatedCallableCallType);
        }

        private static bool LiftBody(
                QsCallable callingCallable,
                QsScope body,
                ParameterTuple? additionalParameters,
                CallableInformation callableInformation,
                bool isFunction,
                bool isReturnAllowed,
                [NotNullWhen(true)] out TypedExpression? callExpression,
                [NotNullWhen(true)] out QsCallable? callable)
        {
            if (!LiftValidationWalker.Apply(body, isReturnAllowed, out var usedSymbols, out var returnType))
            {
                callable = null;
                callExpression = null;
                return false;
            }

            var hasAdditionalParams = true;
            if (additionalParameters is null)
            {
                additionalParameters = ParameterTuple.NewQsTuple(ImmutableArray<ParameterTuple>.Empty);
                hasAdditionalParams = false;
            }
            else if (additionalParameters is ParameterTuple.QsTuple tuple && tuple.Item.Length == 0)
            {
                hasAdditionalParams = false;
            }

            var parameters = ParameterTuple.NewQsTuple(usedSymbols
                .Select(var => ParameterTuple.NewQsTupleItem(new LocalVariableDeclaration<QsLocalSymbol, ResolvedType>(
                    QsLocalSymbol.NewValidName(var.VariableName),
                    var.Type,
                    new InferredExpressionInformation(false, false),
                    var.Position,
                    var.Range)))
                .ToImmutableArray());
            if (hasAdditionalParams)
            {
                parameters = ConcatParams(parameters, additionalParameters);
            }

            var (generatedCallable, generatedCallableCallType) = GenerateCallable(callingCallable, body, parameters, callableInformation, isFunction, returnType);

            // Forward the type parameters of the parent callable to the type arguments of the call to the generated callable.
            // Future: this may need to be per-specialization when type-specific specializations are implemented
            var typeArguments = callingCallable.Signature.TypeParameters.Any(param => param.IsValidName)
            ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(callingCallable.Signature.TypeParameters
                .Where(param => param.IsValidName)
                .Select(param =>
                    ResolvedType.New(ResolvedTypeKind.NewTypeParameter(QsTypeParameter.New(
                        callingCallable.FullName,
                        ((QsLocalSymbol.ValidName)param).Item))))
                .ToImmutableArray())
            : QsNullable<ImmutableArray<ResolvedType>>.Null;

            var generatedCallableId = new TypedExpression(
                ExpressionKind.NewIdentifier(
                    Identifier.NewGlobalCallable(generatedCallable.FullName),
                    typeArguments),
                typeArguments.IsNull
                    ? TypeArgsResolution.Empty
                    : typeArguments.Item
                        .Select(type => Tuple.Create(generatedCallable.FullName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
                        .ToImmutableArray(),
                generatedCallableCallType,
                new InferredExpressionInformation(false, false),
                QsNullable<Range>.Null);

            TypedExpression arguments = new TypedExpression(
                ExpressionKind.UnitValue,
                TypeArgsResolution.Empty,
                ResolvedType.New(ResolvedTypeKind.UnitType),
                new InferredExpressionInformation(false, false),
                QsNullable<Range>.Null);

            if (usedSymbols.Any() || hasAdditionalParams)
            {
                var argumentArray = usedSymbols
                    .Select(var => new TypedExpression(
                        ExpressionKind.NewIdentifier(
                            Identifier.NewLocalVariable(var.VariableName),
                            QsNullable<ImmutableArray<ResolvedType>>.Null),
                        TypeArgsResolution.Empty,
                        var.Type,
                        var.InferredInformation,
                        QsNullable<Range>.Null))
                    .ToImmutableArray();

                if (additionalParameters.IsQsTupleItem)
                {
                    argumentArray = argumentArray.Add(new TypedExpression(
                        ExpressionKind.MissingExpr,
                        TypeArgsResolution.Empty,
                        ResolvedType.New(ResolvedTypeKind.MissingType),
                        new InferredExpressionInformation(false, false),
                        QsNullable<Range>.Null));
                }
                else if (additionalParameters is ParameterTuple.QsTuple tup)
                {
                    argumentArray = argumentArray
                        .Concat(tup.Item
                            .Select(_ => new TypedExpression(
                                ExpressionKind.MissingExpr,
                                TypeArgsResolution.Empty,
                                ResolvedType.New(ResolvedTypeKind.MissingType),
                                new InferredExpressionInformation(false, false),
                                QsNullable<Range>.Null)))
                        .ToImmutableArray();
                }

                if (argumentArray.Length == 1)
                {
                    arguments = argumentArray.First();
                }
                else
                {
                    arguments = new TypedExpression(
                        ExpressionKind.NewValueTuple(argumentArray),
                        TypeArgsResolution.Empty,
                        ResolvedType.New(ResolvedTypeKind.NewTupleType(argumentArray.Select(expr => expr.ResolvedType).ToImmutableArray())),
                        new InferredExpressionInformation(false, usedSymbols.Any(exp => exp.InferredInformation.HasLocalQuantumDependency)),
                        QsNullable<Range>.Null);
                }
            }

            // If there are additional parameters, the call expression will be a partial application with
            // missing arguments for each of the top-level additional parameters ('top-level' meaning that
            // sub-tuples in the parameters will not be broken into individual missing arguments for each of
            // their elements, but will just get one missing argument for the whole sub-tuple). For this
            // case the return type needs to be rewritten appropriately to a callable type for the partial
            // application.
            if (hasAdditionalParams)
            {
                var additionalParamsType = ExtractParamType(additionalParameters);

                // The return type is a callable that takes the additional parameters and returns the original return type.
                returnType = ResolvedType.New(isFunction
                    ? ResolvedTypeKind.NewFunction(additionalParamsType, returnType)
                    : ResolvedTypeKind.NewOperation(
                        Tuple.Create(additionalParamsType, returnType),
                        generatedCallable.Signature.Information));
            }

            // set output parameters
            callable = generatedCallable;
            callExpression = new TypedExpression(
                ExpressionKind.NewCallLikeExpression(generatedCallableId, arguments),
                typeArguments.IsNull
                    ? TypeArgsResolution.Empty
                    : typeArguments.Item
                        .Select(type => Tuple.Create(generatedCallable.FullName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
                        .ToImmutableArray(),
                returnType,
                new InferredExpressionInformation(false, usedSymbols.Any(exp => exp.InferredInformation.HasLocalQuantumDependency)),
                QsNullable<Range>.Null);

            return true;
        }

        /// <summary>
        /// Generates a new operation with the body's contents. All the known variables at the
        /// start of the block that get used in the body will become parameters to the new
        /// operation, and the operation will have all the valid type parameters of the calling
        /// context as type parameters. Any additional parameters specified will be at the end
        /// of the parameter list and will have their tuple structuring preserved. Additional
        /// parameters will be part of the generated operation's parameters even if they are
        /// not used in the body's contents. If return values are allowed, the generated
        /// operation will have a return type determined on return statements found
        /// in the body's contents. Otherwise the operation will return the unit type value
        /// and return statements will be considered invalid.
        /// If the body is valid to be lifted, 'true' is returned, and a call expression to
        /// the new operation is returned as an out-parameter with all the type parameters and
        /// used variables being forwarded to the new operation as arguments. Any additional
        /// parameters specified for the operation will be represented in the argument list
        /// as missing parameters for the call expression. The generated operation is also
        /// returned as an out-parameter.
        /// If the body is not valid to be lifted, 'false' is returned and the out-parameters are null.
        /// </summary>
        public static bool LiftOperationBody(
                QsCallable callingCallable,
                QsScope body,
                ParameterTuple? additionalParameters,
                CallableInformation callableInformation,
                bool isReturnAllowed,
                [NotNullWhen(true)] out TypedExpression? callExpression,
                [NotNullWhen(true)] out QsCallable? callable)
        {
            return LiftBody(callingCallable, body, additionalParameters, callableInformation, false, isReturnAllowed, out callExpression, out callable);
        }

        /// <summary>
        /// Generates a new function with the body's contents. All the known variables at the
        /// start of the block that get used in the body will become parameters to the new
        /// function, and the function will have all the valid type parameters of the calling
        /// context as type parameters. Any additional parameters specified will be at the end
        /// of the parameter list and will have their tuple structuring preserved. Additional
        /// parameters will be part of the generated function's parameters even if they are
        /// not used in the body's contents. If return values are allowed, the generated
        /// function will have a return type determined on return statements found
        /// in the body's contents. Otherwise the function will return the unit type value
        /// and return statements will be considered invalid.
        /// If the body is valid to be lifted, 'true' is returned, and a call expression to
        /// the new function is returned as an out-parameter with all the type parameters and
        /// used variables being forwarded to the new function as arguments. Any additional
        /// parameters specified for the function will be represented in the argument list
        /// as missing parameters for the call expression. The generated function is also
        /// returned as an out-parameter.
        /// If the body is not valid to be lifted, 'false' is returned and the out-parameters are null.
        /// </summary>
        public static bool LiftFunctionBody(
                QsCallable callingCallable,
                QsScope body,
                ParameterTuple? additionalParameters,
                bool isReturnAllowed,
                [NotNullWhen(true)] out TypedExpression? callExpression,
                [NotNullWhen(true)] out QsCallable? callable)
        {
            return LiftBody(callingCallable, body, additionalParameters, CallableInformation.NoInformation, true, isReturnAllowed, out callExpression, out callable);
        }

        /// <summary>
        /// Transformation that updates the contents of newly generated callables by:
        /// 1. Rerouting the origins of type parameter references to the new callable
        /// 2. Changes the IsMutable and HasLocalQuantumDependency info on parameter references to be false
        /// </summary>
        private class UpdateGeneratedCallable : SyntaxTreeTransformation<UpdateGeneratedCallable.TransformationState>
        {
            public static QsCallable Apply(QsCallable qsCallable, ParameterTuple parameters, QsQualifiedName oldName, QsQualifiedName newName)
            {
                var filter = new UpdateGeneratedCallable(parameters, oldName, newName);

                return filter.Namespaces.OnCallableDeclaration(qsCallable);
            }

            public class TransformationState
            {
                public bool IsRecursiveIdentifier { get; set; } = false;

                public ImmutableHashSet<string> ParameterNames { get; }

                public QsQualifiedName OldName { get; }

                public QsQualifiedName NewName { get; }

                public TransformationState(ParameterTuple parameters, QsQualifiedName oldName, QsQualifiedName newName)
                {
                    this.ParameterNames = FlattenParamTuple(parameters)
                        .Where(x => x.VariableName.IsValidName)
                        .Select(x => ((QsLocalSymbol.ValidName)x.VariableName).Item)
                        .ToImmutableHashSet();
                    this.OldName = oldName;
                    this.NewName = newName;
                }
            }

            private UpdateGeneratedCallable(ParameterTuple parameters, QsQualifiedName oldName, QsQualifiedName newName)
            : base(new TransformationState(parameters, oldName, newName))
            {
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation(this);
            }

            private class ExpressionTransformation : ExpressionTransformation<TransformationState>
            {
                public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                public override ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType> OnTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType> typeParams)
                {
                    // Prevent keys from having their names updated
                    return typeParams.ToImmutableDictionary(kvp => kvp.Key, kvp => this.Types.OnType(kvp.Value));
                }

                public override TypedExpression OnTypedExpression(TypedExpression ex)
                {
                    // Checks if expression is mutable identifier that is in parameter list
                    if ((ex.InferredInformation.IsMutable || ex.InferredInformation.HasLocalQuantumDependency)
                        && ex.Expression is ExpressionKind.Identifier id
                        && id.Item1 is Identifier.LocalVariable variable
                        && this.SharedState.ParameterNames.Contains(variable.Item))
                    {
                        // Set the mutability to false
                        ex = new TypedExpression(
                            ex.Expression,
                            ex.TypeArguments,
                            ex.ResolvedType,
                            new InferredExpressionInformation(false, false), // parameter references cannot be mutable or have local quantum dependency
                            ex.Range);
                    }

                    // Prevent IsRecursiveIdentifier from propagating beyond the typed expression it is referring to
                    var isRecursiveIdentifier = this.SharedState.IsRecursiveIdentifier;
                    var rtrn = base.OnTypedExpression(ex);
                    this.SharedState.IsRecursiveIdentifier = isRecursiveIdentifier;
                    return rtrn;
                }
            }

            private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    var rtrn = base.OnIdentifier(sym, tArgs);

                    // Check if this is a recursive identifier
                    // In this context, that is a call back to the original callable from the newly generated callable
                    if (sym is Identifier.GlobalCallable callable && this.SharedState.OldName.Equals(callable.Item))
                    {
                        // Setting this flag will prevent the rerouting logic from processing the resolved type of the recursive identifier expression.
                        // This is necessary because we don't want any type parameters from the original callable from being rerouted to the new generated
                        // callable's type parameters in the definition of the identifier.
                        this.SharedState.IsRecursiveIdentifier = true;
                    }

                    return rtrn;
                }
            }

            private class TypeTransformation : TypeTransformation<TransformationState>
            {
                public TypeTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp) =>

                    // Reroute a type parameter's origin to the newly generated callable
                    !this.SharedState.IsRecursiveIdentifier && this.SharedState.OldName.Equals(tp.Origin)
                        ? base.OnTypeParameter(tp.With(this.SharedState.NewName))
                        : base.OnTypeParameter(tp);
            }
        }

        /// <summary>
        /// Walker that determines if the given scope is valid for lifting by checking if there are any
        /// return statements or update statements on mutables defined prior to the scope. This will
        /// also check all of the used variables in the scope to determine the minimum parameter set
        /// for the generated callable. If the scope is not valid, the walker will not report any
        /// used variables. The return type reported is the unit type.
        /// When isReturnsAllowed is set to true, then the scope is not valid if there are no return
        /// statements or if the return statements do not agree on a return type, and the walker will
        /// report a return type based on the return statements.
        /// </summary>
        private class LiftValidationWalker : SyntaxTreeTransformation<LiftValidationWalker.TransformationState>
        {
            public static bool Apply(
                QsScope content,
                bool isReturnsAllowed,
                out ImmutableArray<LocalVariableDeclaration<string, ResolvedType>> usedSymbols,
                out ResolvedType returnType)
            {
                usedSymbols = ImmutableArray<LocalVariableDeclaration<string, ResolvedType>>.Empty;
                returnType = ResolvedType.New(ResolvedTypeKind.UnitType);

                var filter = new LiftValidationWalker(content, isReturnsAllowed);
                filter.Statements.OnScope(content);

                if (!filter.SharedState.IsValidScope
                    || (isReturnsAllowed && filter.SharedState.ReturnType == null))
                {
                    return false;
                }

                if (isReturnsAllowed && filter.SharedState.ReturnType != null)
                {
                    returnType = filter.SharedState.ReturnType;
                }

                usedSymbols = filter.SharedState.SuperContextSymbols
                    .Where(symbol => symbol.Used)
                    .Select(symbol => symbol.Variable)
                    .ToImmutableArray();

                return true;
            }

            public class TransformationState
            {
                public bool IsValidScope { get; set; } = true;

                public bool InValueUpdate { get; set; } = false;

                public ResolvedType? ReturnType { get; set; } = null;

                public bool IsReturnAllowed { get; private set; } = false;

                public List<(LocalVariableDeclaration<string, ResolvedType> Variable, bool Used)> SuperContextSymbols { get; set; } =
                    new List<(LocalVariableDeclaration<string, ResolvedType> Variable, bool Used)>();

                public TransformationState(bool isReturnAllowed)
                {
                    this.IsReturnAllowed = isReturnAllowed;
                }
            }

            private LiftValidationWalker(QsScope content, bool isReturnAllowed)
            : base(new TransformationState(isReturnAllowed))
            {
                this.SharedState.SuperContextSymbols = content.KnownSymbols.Variables
                    .Select(name => (Variable: name, Used: false))
                    .ToList();

                this.Namespaces = new NamespaceTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.StatementKinds = new StatementKindTransformation(this);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class StatementKindTransformation : StatementKindTransformation<TransformationState>
            {
                public StatementKindTransformation(SyntaxTreeTransformation<TransformationState> parent)
                : base(parent, TransformationOptions.NoRebuild)
                {
                }

                private ResolvedType ValidateAndCombineReturnTypes(ResolvedType type1, ResolvedType type2)
                {
                    var (typeKind1, typeKind2) = (type1.Resolution, type2.Resolution);

                    if (typeKind1 is ResolvedTypeKind.ArrayType arrayType1)
                    {
                        if (typeKind2 is ResolvedTypeKind.ArrayType arrayType2)
                        {
                            return ResolvedType.New(ResolvedTypeKind.NewArrayType(
                                this.ValidateAndCombineReturnTypes(arrayType1.Item, arrayType2.Item)));
                        }

                        this.SharedState.IsValidScope = false;
                        return type1;
                    }
                    else if (typeKind1 is ResolvedTypeKind.TupleType tupleType1)
                    {
                        if (typeKind2 is ResolvedTypeKind.TupleType tupleType2 && tupleType1.Item.Length == tupleType2.Item.Length)
                        {
                            return ResolvedType.New(ResolvedTypeKind.NewTupleType(
                                tupleType1.Item
                                    .Zip(tupleType2.Item, (x, y) => this.ValidateAndCombineReturnTypes(x, y))
                                    .ToImmutableArray()));
                        }

                        this.SharedState.IsValidScope = false;
                        return type1;
                    }
                    else if (typeKind1 is ResolvedTypeKind.Operation opType1)
                    {
                        if (typeKind2 is ResolvedTypeKind.Operation opType2)
                        {
                            var subTypes = Tuple.Create(
                                    this.ValidateAndCombineReturnTypes(opType1.Item1.Item1, opType2.Item1.Item1),
                                    this.ValidateAndCombineReturnTypes(opType1.Item1.Item2, opType2.Item1.Item2));
                            var commonInfo = CallableInformation.Common(new[] { opType1.Item2, opType2.Item2 });

                            return ResolvedType.New(ResolvedTypeKind.NewOperation(subTypes, commonInfo));
                        }

                        this.SharedState.IsValidScope = false;
                        return type1;
                    }
                    else if (typeKind1 is ResolvedTypeKind.Function funcType1)
                    {
                        if (typeKind2 is ResolvedTypeKind.Function funcType2)
                        {
                            return ResolvedType.New(ResolvedTypeKind.NewFunction(
                                this.ValidateAndCombineReturnTypes(funcType1.Item1, funcType2.Item1),
                                this.ValidateAndCombineReturnTypes(funcType1.Item2, funcType2.Item2)));
                        }

                        this.SharedState.IsValidScope = false;
                        return type1;
                    }
                    else
                    {
                        if (!typeKind1.Equals(typeKind2))
                        {
                            this.SharedState.IsValidScope = false;
                        }

                        return ResolvedType.New(type1.Resolution);
                    }
                }

                /// <inheritdoc/>
                public override QsStatementKind OnReturnStatement(TypedExpression ex)
                {
                    if (!this.SharedState.IsReturnAllowed)
                    {
                        this.SharedState.IsValidScope = false;
                    }
                    else
                    {
                        if (this.SharedState.ReturnType == null)
                        {
                            this.SharedState.ReturnType = ResolvedType.New(ex.ResolvedType.Resolution);
                        }
                        else
                        {
                            this.SharedState.ReturnType = this.ValidateAndCombineReturnTypes(this.SharedState.ReturnType, ex.ResolvedType);
                        }
                    }

                    return base.OnReturnStatement(ex);
                }

                /// <inheritdoc/>
                public override QsStatementKind OnValueUpdate(QsValueUpdate stm)
                {
                    // If lhs contains an identifier found in the scope's known variables (variables from the super-scope), the scope is not valid
                    this.SharedState.InValueUpdate = true;
                    var lhs = this.Expressions.OnTypedExpression(stm.Lhs);
                    this.SharedState.InValueUpdate = false;
                    var rhs = this.Expressions.OnTypedExpression(stm.Rhs);
                    return QsStatementKind.NewQsValueUpdate(new QsValueUpdate(lhs, rhs));
                }
            }

            private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent, TransformationOptions.NoRebuild)
                {
                }

                /// <inheritdoc/>
                public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    if (sym is Identifier.LocalVariable local)
                    {
                        if (this.SharedState.InValueUpdate
                            && this.SharedState.SuperContextSymbols.Any(symbol => symbol.Variable.VariableName.Equals(local.Item)))
                        {
                            this.SharedState.IsValidScope = false;
                        }

                        this.SharedState.SuperContextSymbols =
                            this.SharedState.SuperContextSymbols
                            .Select(symbol => (symbol.Variable, symbol.Used || symbol.Variable.VariableName.Equals(local.Item)))
                            .ToList();
                    }

                    return base.OnIdentifier(sym, tArgs);
                }
            }
        }
    }

    /// <summary>
    /// Static class for containing the transformation state class for the <see cref="LiftContentUsingContext{T}"/>.
    /// </summary>
    internal static class LiftContentUsingContext
    {
        public class TransformationState
        {
            internal CallableDetails? CurrentCallable { get; set; } = null;

            protected internal bool InBody { get; set; } = false;

            protected internal bool InAdjoint { get; set; } = false;

            protected internal bool InControlled { get; set; } = false;

            protected internal bool InControlledAdjoint { get; set; } = false;

            protected internal bool InWithinBlock { get; set; } = false;

            private CallableInformation DetermineCallableInformation()
            {
                if (this.CurrentCallable is null)
                {
                    return CallableInformation.NoInformation;
                }

                var adj = this.CurrentCallable.Adjoint;
                var ctl = this.CurrentCallable.Controlled;
                var ctlAdj = this.CurrentCallable.ControlledAdjoint;

                bool addAdjoint = false;
                bool addControlled = false;
                bool isSelfAdjoint = false;

                if (this.InWithinBlock)
                {
                    addAdjoint = true;
                    addControlled = false;
                }
                else if (this.InBody)
                {
                    if (adj != null && adj.Implementation is SpecializationImplementation.Generated adjGen)
                    {
                        addAdjoint = adjGen.Item.IsInvert;
                        isSelfAdjoint = adjGen.Item.IsSelfInverse;
                    }

                    if (ctl != null && ctl.Implementation is SpecializationImplementation.Generated ctlGen)
                    {
                        addControlled = ctlGen.Item.IsDistribute;
                    }

                    if (ctlAdj != null && ctlAdj.Implementation is SpecializationImplementation.Generated ctlAdjGen)
                    {
                        addAdjoint = addAdjoint || (ctlAdjGen.Item.IsInvert && (ctl?.Implementation.IsGenerated ?? true));
                        addControlled = addControlled || (ctlAdjGen.Item.IsDistribute && (adj?.Implementation.IsGenerated ?? true));
                        isSelfAdjoint = isSelfAdjoint || ctlAdjGen.Item.IsSelfInverse;
                    }
                }
                else if (ctlAdj != null && ctlAdj.Implementation is SpecializationImplementation.Generated gen)
                {
                    addControlled = this.InAdjoint && gen.Item.IsDistribute;
                    addAdjoint = this.InControlled && gen.Item.IsInvert;
                    isSelfAdjoint = gen.Item.IsSelfInverse;
                }

                var props = new List<OpProperty>();
                if (addAdjoint)
                {
                    props.Add(OpProperty.Adjointable);
                }

                if (addControlled)
                {
                    props.Add(OpProperty.Controllable);
                }

                return new CallableInformation(ResolvedCharacteristics.FromProperties(props), new InferredCallableInformation(isSelfAdjoint, false));
            }

            /// <summary>
            /// Generates a new callable with the body's contents using the context of the body's
            /// original location. If the body was originally in a function, the generated
            /// callable will be a function, otherwise it will be an operation with the functor
            /// support determined based on the context. All the known variables at the start of
            /// the block that get used in the body will become parameters to the new callable,
            /// and the callable will have all the valid type parameters of the calling context
            /// as type parameters. Any additional parameters specified will be at the end of
            /// the parameter list and will have their tuple structuring preserved. Additional
            /// parameters will be part of the generated callable's parameters even if they are
            /// not used in the body's contents. If return values are allowed, the generated
            /// callable will have a return type determined on return statements found in the
            /// body's contents. Otherwise the callable will return the unit type value and
            /// return statements will be considered invalid.
            /// If the body is valid to be lifted, 'true' is returned, and
            /// a call expression to the new callable is returned as an out-parameter with all
            /// the type parameters and used variables being forwarded to the new callable as
            /// arguments. Any additional parameters specified for the callable will be
            /// represented in the argument list as missing parameters for the call expression.
            /// The generated callable is also returned as an out-parameter.
            /// If the body is not valid to be lifted, 'false' is returned and the out-parameters are null.
            /// </summary>
            public bool LiftBody(
                QsScope body,
                ParameterTuple? additionalParameters,
                bool isReturnAllowed,
                [NotNullWhen(true)] out TypedExpression? callExpression,
                [NotNullWhen(true)] out QsCallable? callable)
            {
                if (this.CurrentCallable is null)
                {
                    callExpression = null;
                    callable = null;
                    return false;
                }

                if (this.CurrentCallable.Callable.Kind.IsFunction)
                {
                    return LiftContent.LiftFunctionBody(this.CurrentCallable.Callable, body, additionalParameters, isReturnAllowed, out callExpression, out callable);
                }
                else
                {
                    return LiftContent.LiftOperationBody(this.CurrentCallable.Callable, body, additionalParameters, this.DetermineCallableInformation(), isReturnAllowed, out callExpression, out callable);
                }
            }
        }

        internal class CallableDetails
        {
            internal QsCallable Callable { get; }

            internal QsSpecialization? Adjoint { get; }

            internal QsSpecialization? Controlled { get; }

            internal QsSpecialization? ControlledAdjoint { get; }

            internal QsNullable<ImmutableArray<ResolvedType>> TypeParameters { get; }

            internal CallableDetails(QsCallable callable)
            {
                this.Callable = callable;

                // Future: this may need to be adapted once we support type specializations
                this.Adjoint = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsAdjoint);
                this.Controlled = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsControlled);
                this.ControlledAdjoint = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsControlledAdjoint);

                // Future: this may need to be per-specialization when type-specific specializations are implemented
                this.TypeParameters = callable.Signature.TypeParameters.Any(param => param.IsValidName)
                ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(callable.Signature.TypeParameters
                    .Where(param => param.IsValidName)
                    .Select(param =>
                        ResolvedType.New(ResolvedTypeKind.NewTypeParameter(QsTypeParameter.New(
                            callable.FullName,
                            ((QsLocalSymbol.ValidName)param).Item))))
                    .ToImmutableArray())
                : QsNullable<ImmutableArray<ResolvedType>>.Null;
            }
        }
    }

    /// <summary>
    /// This transformation provides the basis for lifters that use the context to generated
    /// callables from code blocks. More specific lifter transformations can inherit from
    /// the classes found here. It provides the logic for accumulating context information
    /// that is used when calling
    /// <see cref="LiftContentUsingContext.TransformationState.LiftBody"/>.
    /// To properly inherit from this transformation, the derived transformation should inherit
    /// this class, its transformation state class should inherit from
    /// <see cref="LiftContentUsingContext.TransformationState"/>, and new namespace transformation
    /// and new statement kind transformations are needed, they should inherit from
    /// <see cref="LiftContentUsingContext{T}.NamespaceTransformation"/> and
    /// <see cref="LiftContentUsingContext{T}.StatementKindTransformation"/> respectively.
    /// </summary>
    internal class LiftContentUsingContext<T> : SyntaxTreeTransformation<T>
        where T : LiftContentUsingContext.TransformationState
    {
        protected LiftContentUsingContext(T state)
            : base(state)
        {
            this.Namespaces = new NamespaceTransformation(this);
            this.StatementKinds = new StatementKindTransformation(this);
            this.Types = new TypeTransformation<T>(this, TransformationOptions.Disabled);
        }

        protected class NamespaceTransformation : NamespaceTransformation<T>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<T> parent)
                : base(parent)
            {
            }

            /// <inheritdoc/>
            public override QsCallable OnCallableDeclaration(QsCallable c)
            {
                this.SharedState.CurrentCallable = new LiftContentUsingContext.CallableDetails(c);
                return base.OnCallableDeclaration(c);
            }

            /// <inheritdoc/>
            public override QsSpecialization OnBodySpecialization(QsSpecialization spec)
            {
                this.SharedState.InBody = true;
                var rtrn = base.OnBodySpecialization(spec);
                this.SharedState.InBody = false;
                return rtrn;
            }

            /// <inheritdoc/>
            public override QsSpecialization OnAdjointSpecialization(QsSpecialization spec)
            {
                this.SharedState.InAdjoint = true;
                var rtrn = base.OnAdjointSpecialization(spec);
                this.SharedState.InAdjoint = false;
                return rtrn;
            }

            /// <inheritdoc/>
            public override QsSpecialization OnControlledSpecialization(QsSpecialization spec)
            {
                this.SharedState.InControlled = true;
                var rtrn = base.OnControlledSpecialization(spec);
                this.SharedState.InControlled = false;
                return rtrn;
            }

            /// <inheritdoc/>
            public override QsSpecialization OnControlledAdjointSpecialization(QsSpecialization spec)
            {
                this.SharedState.InControlledAdjoint = true;
                var rtrn = base.OnControlledAdjointSpecialization(spec);
                this.SharedState.InControlledAdjoint = false;
                return rtrn;
            }
        }

        protected class StatementKindTransformation : StatementKindTransformation<T>
        {
            public StatementKindTransformation(SyntaxTreeTransformation<T> parent)
                : base(parent)
            {
            }

            /// <inheritdoc/>
            public override QsStatementKind OnConjugation(QsConjugation stm)
            {
                var superInWithinBlock = this.SharedState.InWithinBlock;
                this.SharedState.InWithinBlock = true;
                var (_, outer) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, stm.OuterTransformation);
                this.SharedState.InWithinBlock = superInWithinBlock;

                var (_, inner) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, stm.InnerTransformation);

                return QsStatementKind.NewQsConjugation(new QsConjugation(outer, inner));
            }
        }
    }
}
