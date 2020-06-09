// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;


namespace Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlled
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>;

    /// <summary>
    /// This transformation works in two passes.
    /// 1st Pass: Lift the contents of conditional statements into separate operations, where possible.
    /// 2nd Pass: On the way down the tree, reshape conditional statements to replace Elif's and
    /// top level OR and AND conditions with equivalent nested if-else statements. One the way back up
    /// the tree, convert conditional statements into interface calls, where possible.
    /// This relies on anything having type parameters must be a global callable.
    /// </summary>
    public static class ReplaceClassicalControl
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            compilation = LiftConditionBlocks.Apply(compilation);

            return ConvertConditions.Apply(compilation);
        }

        private class ConvertConditions : SyntaxTreeTransformation<ConvertConditions.TransformationState>
        {
            public new static QsCompilation Apply(QsCompilation compilation)
            {
                var filter = new ConvertConditions(compilation);

                return new QsCompilation(compilation.Namespaces.Select(ns => filter.Namespaces.OnNamespace(ns)).ToImmutableArray(), compilation.EntryPoints);
            }

            public class TransformationState
            {
                public readonly QsCompilation Compilation;

                public TransformationState(QsCompilation compilation)
                {
                    Compilation = compilation;
                }
            }

            private ConvertConditions(QsCompilation compilation) : base(new TransformationState(compilation))
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation(this);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override QsCallable OnFunction(QsCallable c) => c; // Prevent anything in functions from being considered
            }

            private class StatementTransformation : StatementTransformation<TransformationState>
            {
                public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                /// <summary>
                /// Get the combined type resolutions for a pair of nested resolutions,
                /// resolving references in the inner resolutions to the outer resolutions.
                /// </summary>
                private TypeArgsResolution GetCombinedTypeResolution(TypeArgsResolution outer, TypeArgsResolution inner)
                {
                    var outerDict = outer.ToDictionary(x => (x.Item1, x.Item2), x => x.Item3);
                    return inner.Select(innerRes =>
                    {
                        if (innerRes.Item3.Resolution is ResolvedTypeKind.TypeParameter typeParam &&
                            outerDict.TryGetValue((typeParam.Item.Origin, typeParam.Item.TypeName), out var outerRes))
                        {
                            outerDict.Remove((typeParam.Item.Origin, typeParam.Item.TypeName));
                            return Tuple.Create(innerRes.Item1, innerRes.Item2, outerRes);
                        }
                        else
                        {
                            return innerRes;
                        }
                    })
                    .Concat(outerDict.Select(x => Tuple.Create(x.Key.Item1, x.Key.Item2, x.Value))).ToImmutableArray();
                }

                /// <summary>
                /// Checks if the scope is valid for conversion to an operation call from the conditional control API.
                /// It is valid if there is exactly one statement in it and that statement is a call like expression statement.
                /// If valid, returns true with the identifier of the call like expression and the arguments of the
                /// call like expression, otherwise returns false with nulls.
                /// </summary>
                private (bool, TypedExpression, TypedExpression) IsValidScope(QsScope scope)
                {
                    // if the scope has exactly one statement in it and that statement is a call like expression statement
                    if (scope != null
                        && scope.Statements.Length == 1
                        && scope.Statements[0].Statement is QsStatementKind.QsExpressionStatement expr
                        && expr.Item.ResolvedType.Resolution.IsUnitType
                        && expr.Item.Expression is ExpressionKind.CallLikeExpression call
                        && !TypedExpression.IsPartialApplication(expr.Item.Expression)
                        && call.Item1.Expression is ExpressionKind.Identifier)
                    {
                        // We are dissolving the application of arguments here, so the call's type argument
                        // resolutions have to be moved to the 'identifier' sub expression.

                        var callTypeArguments = expr.Item.TypeArguments;
                        var idTypeArguments = call.Item1.TypeArguments;
                        var combinedTypeArguments = GetCombinedTypeResolution(callTypeArguments, idTypeArguments);

                        // This relies on anything having type parameters must be a global callable.
                        var newCallIdentifier = call.Item1;
                        if (combinedTypeArguments.Any()
                            && newCallIdentifier.Expression is ExpressionKind.Identifier id
                            && id.Item1 is Identifier.GlobalCallable global)
                        {
                            var globalCallable = SharedState.Compilation.Namespaces
                                .Where(ns => ns.Name.Equals(global.Item.Namespace))
                                .Callables()
                                .FirstOrDefault(c => c.FullName.Name.Equals(global.Item.Name));

                            QsCompilerError.Verify(globalCallable != null, $"Could not find the global reference {global.Item.Namespace.Value + "." + global.Item.Name.Value}");

                            var callableTypeParameters = globalCallable.Signature.TypeParameters
                                .Select(x => x as QsLocalSymbol.ValidName);

                            QsCompilerError.Verify(callableTypeParameters.All(x => x != null), $"Invalid type parameter names.");

                            newCallIdentifier = new TypedExpression(
                                ExpressionKind.NewIdentifier(
                                    id.Item1,
                                    QsNullable<ImmutableArray<ResolvedType>>.NewValue(
                                        callableTypeParameters
                                        .Select(x => combinedTypeArguments.First(y => y.Item2.Equals(x.Item)).Item3).ToImmutableArray())),
                                combinedTypeArguments,
                                call.Item1.ResolvedType,
                                call.Item1.InferredInformation,
                                call.Item1.Range);
                        }

                        return (true, newCallIdentifier, call.Item2);
                    }

                    return (false, null, null);
                }

                /// <summary>
                /// Gets an identifier and argument tuple for the built-in operation NoOp.
                /// </summary>
                private (TypedExpression, TypedExpression) GetNoOp()
                {
                    var opInfo = BuiltIn.NoOp;

                    var properties = new[] { OpProperty.Adjointable, OpProperty.Controllable };
                    var characteristics = new CallableInformation(
                        ResolvedCharacteristics.FromProperties(properties),
                        new InferredCallableInformation(((BuiltInKind.Operation)opInfo.Kind).IsSelfAdjoint, false));

                    var unitType = ResolvedType.New(ResolvedTypeKind.UnitType);
                    var operationType = ResolvedType.New(ResolvedTypeKind.NewOperation(
                            Tuple.Create(unitType, unitType),
                            characteristics));

                    var args = new TypedExpression(
                        ExpressionKind.UnitValue,
                        TypeArgsResolution.Empty,
                        unitType,
                        new InferredExpressionInformation(false, false),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);
                    var typeArgs = ImmutableArray.Create(unitType);

                    var identifier = new TypedExpression(
                        ExpressionKind.NewIdentifier(
                            Identifier.NewGlobalCallable(opInfo.FullName),
                            QsNullable<ImmutableArray<ResolvedType>>.NewValue(typeArgs)),
                        typeArgs
                            .Zip(((BuiltInKind.Operation)opInfo.Kind).TypeParameters, (type, param) => Tuple.Create(opInfo.FullName, param, type))
                            .ToImmutableArray(),
                        operationType,
                        new InferredExpressionInformation(false, false),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);

                    return (identifier, args);
                }

                /// <summary>
                /// Creates a value tuple expression containing the given expressions.
                /// </summary>
                private TypedExpression CreateValueTupleExpression(params TypedExpression[] expressions) =>
                    new TypedExpression(
                        ExpressionKind.NewValueTuple(expressions.ToImmutableArray()),
                        TypeArgsResolution.Empty,
                        ResolvedType.New(ResolvedTypeKind.NewTupleType(expressions.Select(expr => expr.ResolvedType).ToImmutableArray())),
                        new InferredExpressionInformation(false, expressions.Any(exp => exp.InferredInformation.HasLocalQuantumDependency)),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);

                #region Condition Converting Logic

                /// <summary>
                /// Creates an operation call from the conditional control API, given information
                /// about which operation to call and with what arguments.
                /// </summary>
                private TypedExpression CreateControlCall(BuiltIn opInfo, IEnumerable<OpProperty> properties, TypedExpression args, IEnumerable<ResolvedType> typeArgs)
                {
                    var characteristics = new CallableInformation(
                        ResolvedCharacteristics.FromProperties(properties),
                        new InferredCallableInformation(((BuiltInKind.Operation)opInfo.Kind).IsSelfAdjoint, false));

                    var unitType = ResolvedType.New(ResolvedTypeKind.UnitType);
                    var operationType = ResolvedType.New(ResolvedTypeKind.NewOperation(
                        Tuple.Create(args.ResolvedType, unitType),
                        characteristics));

                    // Build the surrounding control call
                    var identifier = new TypedExpression(
                        ExpressionKind.NewIdentifier(
                            Identifier.NewGlobalCallable(opInfo.FullName),
                            typeArgs.Any()
                            ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(typeArgs.ToImmutableArray())
                            : QsNullable<ImmutableArray<ResolvedType>>.Null),
                        typeArgs
                            .Zip(((BuiltInKind.Operation)opInfo.Kind).TypeParameters, (type, param) => Tuple.Create(opInfo.FullName, param, type))
                            .ToImmutableArray(),
                        operationType,
                        new InferredExpressionInformation(false, false),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);

                    // Creates type resolutions for the call expression
                    var opTypeArgResolutions = typeArgs
                        .SelectMany(x =>
                            x.Resolution is ResolvedTypeKind.TupleType tup
                            ? tup.Item
                            : ImmutableArray.Create(x))
                        .Where(x => x.Resolution.IsTypeParameter)
                        .Select(x => (x.Resolution as ResolvedTypeKind.TypeParameter).Item)
                        .GroupBy(x => (x.Origin, x.TypeName))
                        .Select(group =>
                        {
                            var typeParam = group.First();
                            return Tuple.Create(typeParam.Origin, typeParam.TypeName, ResolvedType.New(ResolvedTypeKind.NewTypeParameter(typeParam)));
                        })
                        .ToImmutableArray();

                    return new TypedExpression(
                        ExpressionKind.NewCallLikeExpression(identifier, args),
                        opTypeArgResolutions,
                        unitType,
                        new InferredExpressionInformation(false, true),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);
                }

                /// <summary>
                /// Creates an operation call from the conditional control API for non-literal Result comparisons.
                /// The equalityScope and inequalityScope cannot both be null.
                /// </summary>
                private TypedExpression CreateApplyConditionallyExpression(TypedExpression conditionExpr1, TypedExpression conditionExpr2, QsScope equalityScope, QsScope inequalityScope)
                {
                    QsCompilerError.Verify(equalityScope != null || inequalityScope != null, $"Cannot have null for both equality and inequality scopes when creating ApplyConditionally expressions.");

                    var (isEqualityValid, equalityId, equalityArgs) = IsValidScope(equalityScope);
                    var (isInequaltiyValid, inequalityId, inequalityArgs) = IsValidScope(inequalityScope);

                    if (!isEqualityValid && equalityScope != null)
                    {
                        return null; // ToDo: Diagnostic message - equality block exists, but is not valid
                    }

                    if (!isInequaltiyValid && inequalityScope != null)
                    {
                        return null; // ToDo: Diagnostic message - inequality block exists, but is not valid
                    }

                    if (equalityScope == null)
                    {
                        (equalityId, equalityArgs) = GetNoOp();
                    }
                    else if (inequalityScope == null)
                    {
                        (inequalityId, inequalityArgs) = GetNoOp();
                    }

                    // Get characteristic properties from global id
                    var props = ImmutableHashSet<OpProperty>.Empty;
                    if (equalityId.ResolvedType.Resolution is ResolvedTypeKind.Operation op)
                    {
                        props = op.Item2.Characteristics.GetProperties();
                        if (inequalityId != null && inequalityId.ResolvedType.Resolution is ResolvedTypeKind.Operation defaultOp)
                        {
                            props = props.Intersect(defaultOp.Item2.Characteristics.GetProperties());
                        }
                    }

                    BuiltIn controlOpInfo;
                    (bool adj, bool ctl) = (props.Contains(OpProperty.Adjointable), props.Contains(OpProperty.Controllable));
                    if (adj && ctl)
                    {
                        controlOpInfo = BuiltIn.ApplyConditionallyCA;
                    }
                    else if (adj)
                    {
                        controlOpInfo = BuiltIn.ApplyConditionallyA;
                    }
                    else if (ctl)
                    {
                        controlOpInfo = BuiltIn.ApplyConditionallyC;
                    }
                    else
                    {
                        controlOpInfo = BuiltIn.ApplyConditionally;
                    }

                    // Takes a single TypedExpression of type Result and puts in into a
                    // value array expression with the given expression as its only item.
                    TypedExpression BoxResultInArray(TypedExpression expression) =>
                        new TypedExpression(
                            ExpressionKind.NewValueArray(ImmutableArray.Create(expression)),
                            TypeArgsResolution.Empty,
                            ResolvedType.New(ResolvedTypeKind.NewArrayType(ResolvedType.New(ResolvedTypeKind.Result))),
                            new InferredExpressionInformation(false, expression.InferredInformation.HasLocalQuantumDependency),
                            QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);

                    var equality = CreateValueTupleExpression(equalityId, equalityArgs);
                    var inequality = CreateValueTupleExpression(inequalityId, inequalityArgs);
                    var controlArgs = CreateValueTupleExpression(
                        BoxResultInArray(conditionExpr1),
                        BoxResultInArray(conditionExpr2),
                        equality,
                        inequality);
                    var targetArgsTypes = ImmutableArray.Create(equalityArgs.ResolvedType, inequalityArgs.ResolvedType);

                    return CreateControlCall(controlOpInfo, props, controlArgs, targetArgsTypes);
                }

                /// <summary>
                /// Creates an operation call from the conditional control API for Result literal comparisons.
                /// </summary>
                private TypedExpression CreateApplyIfExpression(QsResult result, TypedExpression conditionExpression, QsScope conditionScope, QsScope defaultScope)
                {
                    var (isConditionValid, conditionId, conditionArgs) = IsValidScope(conditionScope);
                    var (isDefaultValid, defaultId, defaultArgs) = IsValidScope(defaultScope);

                    BuiltIn controlOpInfo;
                    TypedExpression controlArgs;
                    ImmutableArray<ResolvedType> targetArgsTypes;

                    var props = ImmutableHashSet<OpProperty>.Empty;

                    if (isConditionValid)
                    {
                        // Get characteristic properties from global id
                        if (conditionId.ResolvedType.Resolution is ResolvedTypeKind.Operation op)
                        {
                            props = op.Item2.Characteristics.GetProperties();
                            if (defaultId != null && defaultId.ResolvedType.Resolution is ResolvedTypeKind.Operation defaultOp)
                            {
                                props = props.Intersect(defaultOp.Item2.Characteristics.GetProperties());
                            }
                        }

                        (bool adj, bool ctl) = (props.Contains(OpProperty.Adjointable), props.Contains(OpProperty.Controllable));

                        if (isDefaultValid)
                        {
                            if (adj && ctl)
                            {
                                controlOpInfo = BuiltIn.ApplyIfElseRCA;
                            }
                            else if (adj)
                            {
                                controlOpInfo = BuiltIn.ApplyIfElseRA;
                            }
                            else if (ctl)
                            {
                                controlOpInfo = BuiltIn.ApplyIfElseRC;
                            }
                            else
                            {
                                controlOpInfo = BuiltIn.ApplyIfElseR;
                            }

                            (TypedExpression, ImmutableArray<ResolvedType>) GetArgs(TypedExpression zeroId, TypedExpression zeroArgs, TypedExpression oneId, TypedExpression oneArgs) =>
                            (
                                CreateValueTupleExpression(
                                    conditionExpression,
                                    CreateValueTupleExpression(zeroId, zeroArgs),
                                    CreateValueTupleExpression(oneId, oneArgs)),

                                ImmutableArray.Create(zeroArgs.ResolvedType, oneArgs.ResolvedType)
                            );

                            (controlArgs, targetArgsTypes) = (result == QsResult.Zero)
                                ? GetArgs(conditionId, conditionArgs, defaultId, defaultArgs)
                                : GetArgs(defaultId, defaultArgs, conditionId, conditionArgs);
                        }
                        else if (defaultScope == null)
                        {
                            if (adj && ctl)
                            {
                                controlOpInfo = (result == QsResult.Zero)
                                ? BuiltIn.ApplyIfZeroCA
                                : BuiltIn.ApplyIfOneCA;
                            }
                            else if (adj)
                            {
                                controlOpInfo = (result == QsResult.Zero)
                                ? BuiltIn.ApplyIfZeroA
                                : BuiltIn.ApplyIfOneA;
                            }
                            else if (ctl)
                            {
                                controlOpInfo = (result == QsResult.Zero)
                                ? BuiltIn.ApplyIfZeroC
                                : BuiltIn.ApplyIfOneC;
                            }
                            else
                            {
                                controlOpInfo = (result == QsResult.Zero)
                                ? BuiltIn.ApplyIfZero
                                : BuiltIn.ApplyIfOne;
                            }

                            controlArgs = CreateValueTupleExpression(
                                conditionExpression,
                                CreateValueTupleExpression(conditionId, conditionArgs));

                            targetArgsTypes = ImmutableArray.Create(conditionArgs.ResolvedType);
                        }
                        else
                        {
                            return null; // ToDo: Diagnostic message - default block exists, but is not valid
                        }

                    }
                    else
                    {
                        return null; // ToDo: Diagnostic message - condition block not valid
                    }

                    return CreateControlCall(controlOpInfo, props, controlArgs, targetArgsTypes);
                }

                /// <summary>
                /// Takes an expression that is the call to a conditional control API operation and the original statement,
                /// and creates a statement from the given expression.
                /// </summary>
                private QsStatement CreateControlStatement(QsStatement statement, TypedExpression callExpression)
                {
                    if (callExpression != null)
                    {
                        return new QsStatement(
                            QsStatementKind.NewQsExpressionStatement(callExpression),
                            statement.SymbolDeclarations,
                            QsNullable<QsLocation>.Null,
                            statement.Comments);
                    }
                    else
                    {
                        // ToDo: add diagnostic message here
                        return statement; // If the blocks can't be converted, return the original
                    }
                }

                /// <summary>
                /// Converts a conditional statement to an operation call from the conditional control API.
                /// </summary>
                private QsStatement ConvertConditionalToControlCall(QsStatement statement)
                {
                    var (isCondition, condition, conditionScope, defaultScope) = IsConditionWithSingleBlock(statement);

                    if (isCondition)
                    {
                        if (IsConditionedOnResultLiteralExpression(condition, out var literal, out var conditionExpression))
                        {
                            return CreateControlStatement(statement, CreateApplyIfExpression(literal, conditionExpression, conditionScope, defaultScope));
                        }
                        else if (IsConditionedOnResultEqualityExpression(condition, out var lhsConditionExpression, out var rhsConditionExpression))
                        {
                            return CreateControlStatement(statement, CreateApplyConditionallyExpression(lhsConditionExpression, rhsConditionExpression, conditionScope, defaultScope));
                        }
                        else if (IsConditionedOnResultInequalityExpression(condition, out lhsConditionExpression, out rhsConditionExpression))
                        {
                            // The scope arguments are reversed to account for the negation of the NEQ
                            return CreateControlStatement(statement, CreateApplyConditionallyExpression(lhsConditionExpression, rhsConditionExpression, defaultScope, conditionScope));
                        }

                        // ToDo: Diagnostic message
                        return statement; // The condition does not fit a supported format.
                    }
                    else
                    {
                        // ToDo: Diagnostic message
                        return statement; // The reshaping of the conditional did not succeed.
                    }
                }

                #endregion

                #region Condition Checking Logic

                /// <summary>
                /// Checks if the statement is a condition statement that only has one conditional block in it (default blocks are optional).
                /// If it is, returns true along with the condition, the body of the conditional block, and, optionally, the body of the
                /// default block, otherwise returns false with nulls. If there is no default block, the last value of the return tuple will be null.
                /// </summary>
                private (bool, TypedExpression, QsScope, QsScope) IsConditionWithSingleBlock(QsStatement statement)
                {
                    if (statement.Statement is QsStatementKind.QsConditionalStatement condition && condition.Item.ConditionalBlocks.Length == 1)
                    {
                        return (true, condition.Item.ConditionalBlocks[0].Item1, condition.Item.ConditionalBlocks[0].Item2.Body, condition.Item.Default.ValueOr(null)?.Body);
                    }

                    return (false, null, null, null);
                }

                /// <summary>
                /// Checks if the expression is an equality or inequality comparison where one side is a
                /// Result literal. If it is, returns true along with the Result literal and the other
                /// expression in the (in)equality, otherwise returns false with nulls. If it is an
                /// inequality, the returned result value will be the opposite of the result literal found.
                /// </summary>
                private bool IsConditionedOnResultLiteralExpression(TypedExpression expression, out QsResult literal, out TypedExpression conditionExpression)
                {
                    literal = null;
                    conditionExpression = null;

                    if (expression.Expression is ExpressionKind.EQ eq)
                    {
                        if (eq.Item1.Expression is ExpressionKind.ResultLiteral literal1)
                        {
                            literal = literal1.Item;
                            conditionExpression = eq.Item2;
                            return true;
                        }
                        else if (eq.Item2.Expression is ExpressionKind.ResultLiteral literal2)
                        {
                            literal = literal2.Item;
                            conditionExpression = eq.Item1;
                            return true;
                        }
                    }
                    else if (expression.Expression is ExpressionKind.NEQ neq)
                    {
                        QsResult FlipResult(QsResult result) => result.IsZero ? QsResult.One : QsResult.Zero;

                        if (neq.Item1.Expression is ExpressionKind.ResultLiteral literal1)
                        {
                            literal = FlipResult(literal1.Item);
                            conditionExpression = neq.Item2;
                            return true;
                        }
                        else if (neq.Item2.Expression is ExpressionKind.ResultLiteral literal2)
                        {
                            literal = FlipResult(literal2.Item);
                            conditionExpression = neq.Item1;
                            return true;
                        }
                    }

                    return false;
                }

                /// <summary>
                /// Checks if the expression is an equality comparison between two Result-typed expressions.
                /// If it is, returns true along with the two expressions, otherwise returns false with nulls.
                /// </summary>
                private bool IsConditionedOnResultEqualityExpression(TypedExpression expression, out TypedExpression lhs, out TypedExpression rhs)
                {
                    lhs = null;
                    rhs = null;

                    if (expression.Expression is ExpressionKind.EQ eq
                        && eq.Item1.ResolvedType.Resolution == ResolvedTypeKind.Result
                        && eq.Item2.ResolvedType.Resolution == ResolvedTypeKind.Result)
                    {
                        lhs = eq.Item1;
                        rhs = eq.Item2;
                        return true;
                    }

                    return false;
                }

                /// <summary>
                /// Checks if the expression is an inequality comparison between two Result-typed expressions.
                /// If it is, returns true along with the two expressions, otherwise returns false with nulls.
                /// </summary>
                private bool IsConditionedOnResultInequalityExpression(TypedExpression expression, out TypedExpression lhs, out TypedExpression rhs)
                {
                    lhs = null;
                    rhs = null;

                    if (expression.Expression is ExpressionKind.NEQ neq
                        && neq.Item1.ResolvedType.Resolution == ResolvedTypeKind.Result
                        && neq.Item2.ResolvedType.Resolution == ResolvedTypeKind.Result)
                    {
                        lhs = neq.Item1;
                        rhs = neq.Item2;
                        return true;
                    }

                    return false;
                }

                #endregion

                #region Condition Reshaping Logic

                /// <summary>
                /// Converts if-elif-else structures to nested if-else structures.
                /// </summary>
                private (bool, QsConditionalStatement) ProcessElif(QsConditionalStatement conditionStatment)
                {
                    if (conditionStatment.ConditionalBlocks.Length < 2) return (false, conditionStatment);

                    var subCondition = new QsConditionalStatement(conditionStatment.ConditionalBlocks.RemoveAt(0), conditionStatment.Default);
                    var secondConditionBlock = conditionStatment.ConditionalBlocks[1].Item2;
                    var subIfStatment = new QsStatement(
                        QsStatementKind.NewQsConditionalStatement(subCondition),
                        LocalDeclarations.Empty,
                        secondConditionBlock.Location,
                        secondConditionBlock.Comments);
                    var newDefault = QsNullable<QsPositionedBlock>.NewValue(new QsPositionedBlock(
                        new QsScope(ImmutableArray.Create(subIfStatment), secondConditionBlock.Body.KnownSymbols),
                        secondConditionBlock.Location,
                        QsComments.Empty));

                    return (true, new QsConditionalStatement(ImmutableArray.Create(conditionStatment.ConditionalBlocks[0]), newDefault));
                }

                /// <summary>
                /// Converts conditional statements whose top-most condition is an OR.
                /// Creates a nested structure without the top-most OR.
                /// </summary>
                private (bool, QsConditionalStatement) ProcessOR(QsConditionalStatement conditionStatment)
                {
                    // This method expects elif blocks to have been abstracted out
                    if (conditionStatment.ConditionalBlocks.Length != 1) return (false, conditionStatment);

                    var (condition, block) = conditionStatment.ConditionalBlocks[0];

                    if (condition.Expression is ExpressionKind.OR orCondition)
                    {
                        var subCondition = new QsConditionalStatement(ImmutableArray.Create(Tuple.Create(orCondition.Item2, block)), conditionStatment.Default);
                        var subIfStatment = new QsStatement(
                            QsStatementKind.NewQsConditionalStatement(subCondition),
                            LocalDeclarations.Empty,
                            block.Location,
                            QsComments.Empty);
                        var newDefault = QsNullable<QsPositionedBlock>.NewValue(new QsPositionedBlock(
                            new QsScope(ImmutableArray.Create(subIfStatment), block.Body.KnownSymbols),
                            block.Location,
                            QsComments.Empty));

                        return (true, new QsConditionalStatement(ImmutableArray.Create(Tuple.Create(orCondition.Item1, block)), newDefault));
                    }
                    else
                    {
                        return (false, conditionStatment);
                    }
                }

                /// <summary>
                /// Converts conditional statements whose top-most condition is an AND.
                /// Creates a nested structure without the top-most AND.
                /// </summary>
                private (bool, QsConditionalStatement) ProcessAND(QsConditionalStatement conditionStatment)
                {
                    // This method expects elif blocks to have been abstracted out
                    if (conditionStatment.ConditionalBlocks.Length != 1) return (false, conditionStatment);

                    var (condition, block) = conditionStatment.ConditionalBlocks[0];

                    if (condition.Expression is ExpressionKind.AND andCondition)
                    {
                        var subCondition = new QsConditionalStatement(ImmutableArray.Create(Tuple.Create(andCondition.Item2, block)), conditionStatment.Default);
                        var subIfStatment = new QsStatement(
                            QsStatementKind.NewQsConditionalStatement(subCondition),
                            LocalDeclarations.Empty,
                            block.Location,
                            QsComments.Empty);
                        var newBlock = new QsPositionedBlock(
                            new QsScope(ImmutableArray.Create(subIfStatment), block.Body.KnownSymbols),
                            block.Location,
                            QsComments.Empty);

                        return (true, new QsConditionalStatement(ImmutableArray.Create(Tuple.Create(andCondition.Item1, newBlock)), conditionStatment.Default));
                    }
                    else
                    {
                        return (false, conditionStatment);
                    }
                }

                /// <summary>
                /// Converts conditional statements to nested structures so they do not
                /// have elif blocks or top-most OR or AND conditions.
                /// </summary>
                private QsStatement ReshapeConditional(QsStatement statement)
                {
                    if (statement.Statement is QsStatementKind.QsConditionalStatement condition)
                    {
                        var stm = condition.Item;
                        (_, stm) = ProcessElif(stm);
                        bool wasOrProcessed, wasAndProcessed;
                        do
                        {
                            (wasOrProcessed, stm) = ProcessOR(stm);
                            (wasAndProcessed, stm) = ProcessAND(stm);
                        } while (wasOrProcessed || wasAndProcessed);

                        return new QsStatement(
                            QsStatementKind.NewQsConditionalStatement(stm),
                            statement.SymbolDeclarations,
                            statement.Location,
                            statement.Comments);
                    }
                    return statement;
                }

                #endregion

                public override QsScope OnScope(QsScope scope)
                {
                    var parentSymbols = this.OnLocalDeclarations(scope.KnownSymbols);
                    var statements = new List<QsStatement>();

                    foreach (var statement in scope.Statements)
                    {
                        if (statement.Statement is QsStatementKind.QsConditionalStatement)
                        {
                            var stm = ReshapeConditional(statement);
                            stm = this.OnStatement(stm);
                            stm = ConvertConditionalToControlCall(stm);

                            statements.Add(stm);
                        }
                        else
                        {
                            statements.Add(this.OnStatement(statement));
                        }
                    }

                    return new QsScope(statements.ToImmutableArray(), parentSymbols);
                }
            }
        }
    }

    internal static class LiftConditionBlocks
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            var filter = new LiftContent();

            return new QsCompilation(compilation.Namespaces.Select(ns => filter.Namespaces.OnNamespace(ns)).ToImmutableArray(), compilation.EntryPoints);
        }

        private class LiftContent : ContentLifting.LiftContent<LiftContent.TransformationState>
        {
            internal class TransformationState : ContentLifting.LiftContent.TransformationState
            {
                internal bool IsConditionLiftable = false;
            }

            public LiftContent() : base(new TransformationState())
            {
                this.StatementKinds = new StatementKindTransformation(this);
            }

            private new class StatementKindTransformation : ContentLifting.LiftContent<TransformationState>.StatementKindTransformation
            {
                public StatementKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                private bool IsScopeSingleCall(QsScope contents)
                {
                    if (contents.Statements.Length != 1) return false;

                    return contents.Statements[0].Statement is QsStatementKind.QsExpressionStatement expr
                           && expr.Item.Expression is ExpressionKind.CallLikeExpression call
                           && call.Item1.ResolvedType.Resolution.IsOperation
                           && call.Item1.Expression is ExpressionKind.Identifier
                           && !TypedExpression.IsPartialApplication(expr.Item.Expression);
                }

                public override QsStatementKind OnConditionalStatement(QsConditionalStatement stm)
                {
                    var contextIsConditionLiftable = SharedState.IsConditionLiftable;
                    SharedState.IsConditionLiftable = true;

                    var newConditionBlocks = new List<Tuple<TypedExpression, QsPositionedBlock>>();
                    var generatedOperations = new List<QsCallable>();
                    foreach (var conditionBlock in stm.ConditionalBlocks)
                    {
                        var contextValidScope = SharedState.IsValidScope;
                        var contextParams = SharedState.GeneratedOpParams;

                        SharedState.IsValidScope = true;
                        SharedState.GeneratedOpParams = conditionBlock.Item2.Body.KnownSymbols.Variables;

                        var (expr, block) = this.OnPositionedBlock(QsNullable<TypedExpression>.NewValue(conditionBlock.Item1), conditionBlock.Item2);

                        // ToDo: Reduce the number of unnecessary generated operations by generalizing
                        // the condition logic for the conversion and using that condition here
                        //var (isExprCondition, _, _) = IsConditionedOnResultLiteralExpression(expr.Item);

                        if (IsScopeSingleCall(block.Body))
                        {
                            newConditionBlocks.Add(Tuple.Create(expr.Item, block));
                        }
                        // ToDo: We may want to prevent empty blocks from getting lifted
                        else //if(block.Body.Statements.Length > 0)
                        {
                            // Lift the scope to its own operation
                            if (SharedState.LiftBody(block.Body, out var callable, out var call))
                            {
                                block = new QsPositionedBlock(
                                    new QsScope(ImmutableArray.Create(call), block.Body.KnownSymbols),
                                    block.Location,
                                    block.Comments);
                                newConditionBlocks.Add(Tuple.Create(expr.Item, block));
                                generatedOperations.Add(callable);
                            }
                            else
                            {
                                SharedState.IsConditionLiftable = false;
                            }
                        }

                        SharedState.GeneratedOpParams = contextParams;
                        SharedState.IsValidScope = contextValidScope;

                        if (!SharedState.IsConditionLiftable) break;
                    }

                    var newDefault = QsNullable<QsPositionedBlock>.Null;
                    if (SharedState.IsConditionLiftable && stm.Default.IsValue)
                    {
                        var contextValidScope = SharedState.IsValidScope;
                        var contextParams = SharedState.GeneratedOpParams;

                        SharedState.IsValidScope = true;
                        SharedState.GeneratedOpParams = stm.Default.Item.Body.KnownSymbols.Variables;

                        var (_, block) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, stm.Default.Item);

                        if (IsScopeSingleCall(block.Body))
                        {
                            newDefault = QsNullable<QsPositionedBlock>.NewValue(block);
                        }
                        // ToDo: We may want to prevent empty blocks from getting lifted
                        else //if(block.Body.Statements.Length > 0)
                        {
                            // Lift the scope to its own operation
                            if (SharedState.LiftBody(block.Body, out var callable, out var call))
                            {
                                block = new QsPositionedBlock(
                                    new QsScope(ImmutableArray.Create(call), block.Body.KnownSymbols),
                                    block.Location,
                                    block.Comments);
                                newDefault = QsNullable<QsPositionedBlock>.NewValue(block);
                                generatedOperations.Add(callable);
                            }
                            else
                            {
                                SharedState.IsConditionLiftable = false;
                            }
                        }

                        SharedState.GeneratedOpParams = contextParams;
                        SharedState.IsValidScope = contextValidScope;
                    }

                    if (SharedState.IsConditionLiftable)
                    {
                        SharedState.GeneratedOperations.AddRange(generatedOperations);
                    }

                    var rtrn = SharedState.IsConditionLiftable
                        ? QsStatementKind.NewQsConditionalStatement(
                          new QsConditionalStatement(newConditionBlocks.ToImmutableArray(), newDefault))
                        : QsStatementKind.NewQsConditionalStatement(
                          new QsConditionalStatement(stm.ConditionalBlocks, stm.Default));

                    SharedState.IsConditionLiftable = contextIsConditionLiftable;

                    return rtrn;
                }
            }
        }
    }
}
