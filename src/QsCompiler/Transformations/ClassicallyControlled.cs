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
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlled
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, string, ResolvedType>>;

    /// <summary>
    /// This transformation works in three passes.
    /// 1st Pass: Reshape conditional statements to replace Elif's and top level OR and AND conditions
    /// with equivalent nested if-else statements.
    /// 2st Pass: Lift the contents of conditional statements into separate operations, where possible.
    /// 3nd Pass: Convert conditional statements into interface calls, where possible.
    /// This relies on global callables being the only things that have type parameters.
    /// </summary>
    public static class ReplaceClassicalControl
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            compilation = RestructureConditions.Apply(compilation);
            compilation = LiftConditionBlocks.Apply(compilation);
            return ConvertConditions.Apply(compilation);
        }

        private class RestructureConditions : SyntaxTreeTransformation
        {
            public static QsCompilation Apply(QsCompilation compilation) =>
                new RestructureConditions().OnCompilation(compilation);

            private RestructureConditions()
                : base()
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation(this);
                this.Expressions = new ExpressionTransformation(this, TransformationOptions.Disabled);
                this.Types = new TypeTransformation(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : Core.NamespaceTransformation
            {
                public NamespaceTransformation(SyntaxTreeTransformation parent)
                    : base(parent)
                {
                }

                public override QsCallable OnFunction(QsCallable c) => c; // Prevent anything in functions from being considered
            }

            private class StatementTransformation : Core.StatementTransformation
            {
                public StatementTransformation(SyntaxTreeTransformation parent)
                    : base(parent)
                {
                }

                // Condition Reshaping Logic

                /// <summary>
                /// Converts if-elif-else structures to nested if-else structures.
                /// </summary>
                private (bool, QsConditionalStatement) ProcessElif(QsConditionalStatement conditionStatment)
                {
                    if (conditionStatment.ConditionalBlocks.Length < 2)
                    {
                        return (false, conditionStatment);
                    }

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
                    if (conditionStatment.ConditionalBlocks.Length != 1)
                    {
                        return (false, conditionStatment);
                    }

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
                    if (conditionStatment.ConditionalBlocks.Length != 1)
                    {
                        return (false, conditionStatment);
                    }

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

                private (bool, QsConditionalStatement) ProcessNOT(QsConditionalStatement conditionStatement)
                {
                    // This method expects elif blocks to have been abstracted out
                    if (conditionStatement.ConditionalBlocks.Length != 1)
                    {
                        return (false, conditionStatement);
                    }

                    var (condition, block) = conditionStatement.ConditionalBlocks[0];

                    if (condition.Expression is ExpressionKind.NOT notCondition)
                    {
                        if (conditionStatement.Default.IsValue)
                        {
                            return (true, new QsConditionalStatement(
                                ImmutableArray.Create(Tuple.Create(notCondition.Item, conditionStatement.Default.Item)),
                                QsNullable<QsPositionedBlock>.NewValue(block)));
                        }
                        else
                        {
                            var emptyScope = new QsScope(
                                ImmutableArray<QsStatement>.Empty,
                                LocalDeclarations.Empty);
                            var newConditionalBlock = new QsPositionedBlock(
                                    emptyScope,
                                    QsNullable<QsLocation>.Null,
                                    QsComments.Empty);
                            return (true, new QsConditionalStatement(
                                ImmutableArray.Create(Tuple.Create(notCondition.Item, newConditionalBlock)),
                                QsNullable<QsPositionedBlock>.NewValue(block)));
                        }
                    }
                    else
                    {
                        return (false, conditionStatement);
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
                        (_, stm) = this.ProcessElif(stm);
                        bool wasOrProcessed, wasAndProcessed, wasNotProcessed;
                        do
                        {
                            (wasOrProcessed, stm) = this.ProcessOR(stm);
                            (wasAndProcessed, stm) = this.ProcessAND(stm);
                            (wasNotProcessed, stm) = this.ProcessNOT(stm);
                        }
                        while (wasOrProcessed || wasAndProcessed || wasNotProcessed);

                        return new QsStatement(
                            QsStatementKind.NewQsConditionalStatement(stm),
                            statement.SymbolDeclarations,
                            statement.Location,
                            statement.Comments);
                    }

                    return statement;
                }

                public override QsScope OnScope(QsScope scope)
                {
                    var parentSymbols = this.OnLocalDeclarations(scope.KnownSymbols);
                    var statements = new List<QsStatement>();

                    foreach (var statement in scope.Statements)
                    {
                        if (statement.Statement is QsStatementKind.QsConditionalStatement)
                        {
                            var stm = this.ReshapeConditional(statement);
                            stm = this.OnStatement(stm);
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

        private class ConvertConditions : SyntaxTreeTransformation<ConvertConditions.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation) =>
                new ConvertConditions(compilation).OnCompilation(compilation);

            public class TransformationState
            {
                public readonly QsCompilation Compilation;

                public TransformationState(QsCompilation compilation)
                {
                    this.Compilation = compilation;
                }
            }

            private ConvertConditions(QsCompilation compilation)
                : base(new TransformationState(compilation))
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation(this);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                public override QsCallable OnFunction(QsCallable c) => c; // Prevent anything in functions from being considered
            }

            private class StatementTransformation : StatementTransformation<TransformationState>
            {
                public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                /// <summary>
                /// Checks if the scope is valid for conversion to an operation call from the conditional control API.
                /// It is valid if there is exactly one statement in it and that statement is a call like expression
                /// statement. If valid, returns the identifier of the call like expression and the arguments of the
                /// call like expression, otherwise returns null.
                /// </summary>
                [return: NotNullIfNotNull("scope")]
                private (TypedExpression Id, TypedExpression Args)? IsValidScope(QsScope? scope)
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
                        var newCallIdentifier = call.Item1;
                        var callTypeArguments = expr.Item.TypeParameterResolutions;

                        // This relies on anything having type parameters must be a global callable.
                        if (newCallIdentifier.Expression is ExpressionKind.Identifier id
                            && id.Item1 is Identifier.GlobalCallable global
                            && callTypeArguments.Any())
                        {
                            // We are dissolving the application of arguments here, so the call's type argument
                            // resolutions have to be moved to the 'identifier' sub expression.
                            var combination = new TypeResolutionCombination(expr.Item);
                            var combinedTypeArguments = combination.CombinedResolutionDictionary.FilterByOrigin(global.Item);
                            QsCompilerError.Verify(combination.IsValid, "failed to combine type parameter resolution");

                            var globalCallable = this.SharedState.Compilation.Namespaces
                                .Where(ns => ns.Name.Equals(global.Item.Namespace))
                                .Callables()
                                .FirstOrDefault(c => c.FullName.Name.Equals(global.Item.Name));

                            QsCompilerError.Verify(globalCallable != null, $"Could not find the global reference {global.Item}.");

                            var callableTypeParameters = globalCallable.Signature.TypeParameters.Select(param =>
                            {
                                var name = param as QsLocalSymbol.ValidName;
                                QsCompilerError.Verify(!(name is null), "Invalid type parameter name.");
                                return name;
                            });

                            newCallIdentifier = new TypedExpression(
                                ExpressionKind.NewIdentifier(
                                    id.Item1,
                                    QsNullable<ImmutableArray<ResolvedType>>.NewValue(
                                        callableTypeParameters
                                        .Select(x => combinedTypeArguments[Tuple.Create(global.Item, x.Item)]).ToImmutableArray())),
                                TypedExpression.AsTypeArguments(combinedTypeArguments),
                                call.Item1.ResolvedType,
                                call.Item1.InferredInformation,
                                call.Item1.Range);
                        }

                        return (newCallIdentifier, call.Item2);
                    }

                    return null;
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
                        QsNullable<Range>.Null);

                // Condition Converting Logic

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
                        QsNullable<Range>.Null);

                    // Creates type resolutions for the call expression
                    var opTypeArgResolutions = typeArgs
                        .SelectMany(x =>
                            x.Resolution is ResolvedTypeKind.TupleType tup
                            ? tup.Item
                            : ImmutableArray.Create(x))
                        .SelectNotNull(x => (x.Resolution as ResolvedTypeKind.TypeParameter)?.Item)
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
                        QsNullable<Range>.Null);
                }

                /// <summary>
                /// Creates an operation call from the conditional control API for non-literal Result comparisons.
                /// The equalityScope and inequalityScope cannot both be null.
                /// </summary>
                private TypedExpression? CreateApplyConditionallyExpression(TypedExpression conditionExpr1, TypedExpression conditionExpr2, QsScope? equalityScope, QsScope? inequalityScope)
                {
                    QsCompilerError.Verify(equalityScope != null || inequalityScope != null, $"Cannot have null for both equality and inequality scopes when creating ApplyConditionally expressions.");

                    var equalityInfo = this.IsValidScope(equalityScope);
                    var inequalityInfo = this.IsValidScope(inequalityScope);

                    if (!equalityInfo.HasValue && equalityScope != null)
                    {
                        return null; // ToDo: Diagnostic message - equality block exists, but is not valid
                    }

                    if (!inequalityInfo.HasValue && inequalityScope != null)
                    {
                        return null; // ToDo: Diagnostic message - inequality block exists, but is not valid
                    }

                    equalityInfo ??= LiftConditionBlocks.GetNoOp();
                    inequalityInfo ??= LiftConditionBlocks.GetNoOp();

                    // Get characteristic properties from global id
                    var props = ImmutableHashSet<OpProperty>.Empty;
                    if (equalityInfo.Value.Id.ResolvedType.Resolution is ResolvedTypeKind.Operation op)
                    {
                        props = op.Item2.Characteristics.GetProperties();
                        if (inequalityInfo.HasValue && inequalityInfo.Value.Id.ResolvedType.Resolution is ResolvedTypeKind.Operation defaultOp)
                        {
                            props = props.Intersect(defaultOp.Item2.Characteristics.GetProperties());
                        }
                    }

                    var controlOpInfo = (props.Contains(OpProperty.Adjointable), props.Contains(OpProperty.Controllable)) switch
                    {
                        (true, true) => BuiltIn.ApplyConditionallyCA,
                        (true, false) => BuiltIn.ApplyConditionallyA,
                        (false, true) => BuiltIn.ApplyConditionallyC,
                        (false, false) => BuiltIn.ApplyConditionally
                    };

                    // Takes a single TypedExpression of type Result and puts in into a
                    // value array expression with the given expression as its only item.
                    static TypedExpression BoxResultInArray(TypedExpression expression) =>
                        new TypedExpression(
                            ExpressionKind.NewValueArray(ImmutableArray.Create(expression)),
                            TypeArgsResolution.Empty,
                            ResolvedType.New(ResolvedTypeKind.NewArrayType(ResolvedType.New(ResolvedTypeKind.Result))),
                            new InferredExpressionInformation(false, expression.InferredInformation.HasLocalQuantumDependency),
                            QsNullable<Range>.Null);

                    var equality = this.CreateValueTupleExpression(equalityInfo.Value.Id, equalityInfo.Value.Args);
                    var inequality = this.CreateValueTupleExpression(inequalityInfo.Value.Id, inequalityInfo.Value.Args);
                    var controlArgs = this.CreateValueTupleExpression(
                        BoxResultInArray(conditionExpr1),
                        BoxResultInArray(conditionExpr2),
                        equality,
                        inequality);
                    var targetArgsTypes = ImmutableArray.Create(equalityInfo.Value.Args.ResolvedType, inequalityInfo.Value.Args.ResolvedType);

                    return this.CreateControlCall(controlOpInfo, props, controlArgs, targetArgsTypes);
                }

                /// <summary>
                /// Creates an operation call from the conditional control API for Result literal comparisons.
                /// </summary>
                private TypedExpression? CreateApplyIfExpression(QsResult result, TypedExpression conditionExpression, QsScope conditionScope, QsScope? defaultScope)
                {
                    var conditionInfo = this.IsValidScope(conditionScope);
                    var defaultInfo = this.IsValidScope(defaultScope);

                    BuiltIn controlOpInfo;
                    TypedExpression controlArgs;
                    ImmutableArray<ResolvedType> targetArgsTypes;

                    var props = ImmutableHashSet<OpProperty>.Empty;

                    if (conditionInfo.HasValue)
                    {
                        // Get characteristic properties from global id
                        if (conditionInfo.Value.Id.ResolvedType.Resolution is ResolvedTypeKind.Operation op)
                        {
                            props = op.Item2.Characteristics.GetProperties();
                            if (defaultInfo.HasValue && defaultInfo.Value.Id.ResolvedType.Resolution is ResolvedTypeKind.Operation defaultOp)
                            {
                                props = props.Intersect(defaultOp.Item2.Characteristics.GetProperties());
                            }
                        }

                        (bool adj, bool ctl) = (props.Contains(OpProperty.Adjointable), props.Contains(OpProperty.Controllable));

                        if (defaultInfo.HasValue)
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
                                (this.CreateValueTupleExpression(
                                    conditionExpression,
                                    this.CreateValueTupleExpression(zeroId, zeroArgs),
                                    this.CreateValueTupleExpression(oneId, oneArgs)),

                                ImmutableArray.Create(zeroArgs.ResolvedType, oneArgs.ResolvedType));

                            (controlArgs, targetArgsTypes) = (result == QsResult.Zero)
                                ? GetArgs(conditionInfo.Value.Id, conditionInfo.Value.Args, defaultInfo.Value.Id, defaultInfo.Value.Args)
                                : GetArgs(defaultInfo.Value.Id, defaultInfo.Value.Args, conditionInfo.Value.Id, conditionInfo.Value.Args);
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

                            controlArgs = this.CreateValueTupleExpression(
                                conditionExpression,
                                this.CreateValueTupleExpression(conditionInfo.Value.Id, conditionInfo.Value.Args));

                            targetArgsTypes = ImmutableArray.Create(conditionInfo.Value.Args.ResolvedType);
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

                    return this.CreateControlCall(controlOpInfo, props, controlArgs, targetArgsTypes);
                }

                /// <summary>
                /// Takes an expression that is the call to a conditional control API operation and the original statement,
                /// and creates a statement from the given expression.
                /// </summary>
                private QsStatement CreateControlStatement(QsStatement statement, TypedExpression? callExpression)
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
                    var condition = this.IsConditionWithSingleBlock(statement);

                    if (condition.HasValue)
                    {
                        if (this.IsConditionedOnResultLiteralExpression(condition.Value.Condition, out var literal, out var conditionExpression))
                        {
                            return this.CreateControlStatement(statement, this.CreateApplyIfExpression(literal, conditionExpression, condition.Value.Body, condition.Value.Default));
                        }
                        else if (this.IsConditionedOnResultEqualityExpression(condition.Value.Condition, out var lhsConditionExpression, out var rhsConditionExpression))
                        {
                            return this.CreateControlStatement(statement, this.CreateApplyConditionallyExpression(lhsConditionExpression, rhsConditionExpression, condition.Value.Body, condition.Value.Default));
                        }
                        else if (this.IsConditionedOnResultInequalityExpression(condition.Value.Condition, out lhsConditionExpression, out rhsConditionExpression))
                        {
                            // The scope arguments are reversed to account for the negation of the NEQ
                            return this.CreateControlStatement(statement, this.CreateApplyConditionallyExpression(lhsConditionExpression, rhsConditionExpression, condition.Value.Default, condition.Value.Body));
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

                // Condition Checking Logic

                /// <summary>
                /// Checks if the statement is a condition statement that only has one conditional block in it (default blocks are optional).
                /// If it is, returns the condition, the body of the conditional block, and, optionally, the body of the
                /// default block, otherwise returns null. If there is no default block, the last value of the return tuple will be null.
                /// </summary>
                private (TypedExpression Condition, QsScope Body, QsScope? Default)? IsConditionWithSingleBlock(QsStatement statement)
                {
                    if (statement.Statement is QsStatementKind.QsConditionalStatement condition && condition.Item.ConditionalBlocks.Length == 1)
                    {
                        return (condition.Item.ConditionalBlocks[0].Item1, condition.Item.ConditionalBlocks[0].Item2.Body, condition.Item.Default.ValueOr(null)?.Body);
                    }

                    return null;
                }

                /// <summary>
                /// Checks if the expression is an equality or inequality comparison where one side is a
                /// Result literal. If it is, returns true along with the Result literal and the other
                /// expression in the (in)equality, otherwise returns false with nulls. If it is an
                /// inequality, the returned result value will be the opposite of the result literal found.
                /// </summary>
                private bool IsConditionedOnResultLiteralExpression(
                    TypedExpression expression,
                    [NotNullWhen(true)] out QsResult? literal,
                    [NotNullWhen(true)] out TypedExpression? conditionExpression)
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
                        static QsResult FlipResult(QsResult result) => result.IsZero ? QsResult.One : QsResult.Zero;

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
                private bool IsConditionedOnResultEqualityExpression(
                    TypedExpression expression,
                    [NotNullWhen(true)] out TypedExpression? lhs,
                    [NotNullWhen(true)] out TypedExpression? rhs)
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
                private bool IsConditionedOnResultInequalityExpression(
                    TypedExpression expression,
                    [NotNullWhen(true)] out TypedExpression? lhs,
                    [NotNullWhen(true)] out TypedExpression? rhs)
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

                public override QsScope OnScope(QsScope scope)
                {
                    var parentSymbols = this.OnLocalDeclarations(scope.KnownSymbols);
                    var statements = new List<QsStatement>();

                    foreach (var statement in scope.Statements)
                    {
                        if (statement.Statement is QsStatementKind.QsConditionalStatement)
                        {
                            var stm = this.OnStatement(statement);
                            stm = this.ConvertConditionalToControlCall(stm);
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
        public static QsCompilation Apply(QsCompilation compilation) =>
            new LiftContent().OnCompilation(compilation);

        /// <summary>
        /// Gets an identifier and argument tuple for the built-in operation NoOp.
        /// </summary>
        internal static (TypedExpression Id, TypedExpression Args) GetNoOp()
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
                QsNullable<Range>.Null);
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
                QsNullable<Range>.Null);

            return (identifier, args);
        }

        private class LiftContent : ContentLifting.LiftContent<LiftContent.TransformationState>
        {
            internal class TransformationState : ContentLifting.LiftContent.TransformationState
            {
                internal bool IsConditionLiftable = false;
            }

            public LiftContent()
                : base(new TransformationState())
            {
                this.StatementKinds = new StatementKindTransformation(this);
            }

            private new class StatementKindTransformation : ContentLifting.LiftContent<TransformationState>.StatementKindTransformation
            {
                public StatementKindTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                private bool IsScopeSingleCall(QsScope contents)
                {
                    if (contents.Statements.Length != 1)
                    {
                        return false;
                    }

                    return contents.Statements[0].Statement is QsStatementKind.QsExpressionStatement expr
                           && expr.Item.Expression is ExpressionKind.CallLikeExpression call
                           && call.Item1.ResolvedType.Resolution.IsOperation
                           && call.Item1.Expression is ExpressionKind.Identifier
                           && !TypedExpression.IsPartialApplication(expr.Item.Expression);
                }

                public override QsStatementKind OnConditionalStatement(QsConditionalStatement stm)
                {
                    var contextIsConditionLiftable = this.SharedState.IsConditionLiftable;
                    this.SharedState.IsConditionLiftable = true;

                    var newConditionBlocks = new List<Tuple<TypedExpression, QsPositionedBlock>>();
                    var generatedOperations = new List<QsCallable>();
                    foreach (var conditionBlock in stm.ConditionalBlocks)
                    {
                        var contextValidScope = this.SharedState.IsValidScope;
                        var contextParams = this.SharedState.GeneratedOpParams;

                        this.SharedState.IsValidScope = true;
                        this.SharedState.GeneratedOpParams = conditionBlock.Item2.Body.KnownSymbols.Variables;

                        var (expr, block) = this.OnPositionedBlock(QsNullable<TypedExpression>.NewValue(conditionBlock.Item1), conditionBlock.Item2);

                        // ToDo: Reduce the number of unnecessary generated operations by generalizing
                        // the condition logic for the conversion and using that condition here
                        // var (isExprCondition, _, _) = IsConditionedOnResultLiteralExpression(expr.Item);

                        if (block.Body.Statements.Length == 0)
                        {
                            // This is an empty scope, so it can just be treated as a call to NoOp.
                            var (id, args) = GetNoOp();
                            var callExpression = new TypedExpression(
                                ExpressionKind.NewCallLikeExpression(id, args),
                                TypeArgsResolution.Empty,
                                ResolvedType.New(ResolvedTypeKind.UnitType),
                                new InferredExpressionInformation(false, true),
                                QsNullable<Range>.Null);
                            var callStatement = new QsStatement(
                                QsStatementKind.NewQsExpressionStatement(callExpression),
                                LocalDeclarations.Empty,
                                QsNullable<QsLocation>.Null,
                                QsComments.Empty);
                            newConditionBlocks.Add(Tuple.Create(
                                expr.Item,
                                new QsPositionedBlock(
                                    new QsScope(
                                        ImmutableArray.Create(callStatement),
                                        LocalDeclarations.Empty),
                                    block.Location,
                                    block.Comments)));
                        }
                        else if (this.IsScopeSingleCall(block.Body))
                        {
                            newConditionBlocks.Add(Tuple.Create(expr.Item, block));
                        }
                        else
                        {
                            // Lift the scope to its own operation
                            if (this.SharedState.LiftBody(block.Body, out var callable, out var call))
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
                                this.SharedState.IsConditionLiftable = false;
                            }
                        }

                        this.SharedState.GeneratedOpParams = contextParams;
                        this.SharedState.IsValidScope = contextValidScope;

                        if (!this.SharedState.IsConditionLiftable)
                        {
                            break;
                        }
                    }

                    var newDefault = QsNullable<QsPositionedBlock>.Null;
                    if (this.SharedState.IsConditionLiftable && stm.Default.IsValue)
                    {
                        var contextValidScope = this.SharedState.IsValidScope;
                        var contextParams = this.SharedState.GeneratedOpParams;

                        this.SharedState.IsValidScope = true;
                        this.SharedState.GeneratedOpParams = stm.Default.Item.Body.KnownSymbols.Variables;

                        var (_, block) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, stm.Default.Item);

                        if (this.IsScopeSingleCall(block.Body))
                        {
                            newDefault = QsNullable<QsPositionedBlock>.NewValue(block);
                        }
                        // ToDo: We may want to prevent empty blocks from getting lifted
                        // else if (block.Body.Statements.Length > 0)
                        else
                        {
                            // Lift the scope to its own operation
                            if (this.SharedState.LiftBody(block.Body, out var callable, out var call))
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
                                this.SharedState.IsConditionLiftable = false;
                            }
                        }

                        this.SharedState.GeneratedOpParams = contextParams;
                        this.SharedState.IsValidScope = contextValidScope;
                    }

                    if (this.SharedState.IsConditionLiftable)
                    {
                        this.SharedState.GeneratedOperations?.AddRange(generatedOperations);
                    }

                    var rtrn = this.SharedState.IsConditionLiftable
                        ? QsStatementKind.NewQsConditionalStatement(
                          new QsConditionalStatement(newConditionBlocks.ToImmutableArray(), newDefault))
                        : QsStatementKind.NewQsConditionalStatement(
                          new QsConditionalStatement(stm.ConditionalBlocks, stm.Default));

                    this.SharedState.IsConditionLiftable = contextIsConditionLiftable;

                    return rtrn;
                }
            }
        }
    }
}
