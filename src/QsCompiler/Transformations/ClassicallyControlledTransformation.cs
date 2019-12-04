// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlledTransformation
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    public class ClassicallyControlledTransformation
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            var filter = new ClassicallyControlledSyntax();

            return new QsCompilation(compilation.Namespaces.Select(ns => filter.Transform(ns)).ToImmutableArray(), compilation.EntryPoints);
        }

        private class ClassicallyControlledSyntax : SyntaxTreeTransformation<ClassicallyControlledScope>
        {
            public ClassicallyControlledSyntax(ClassicallyControlledScope scope = null) : base(scope ?? new ClassicallyControlledScope()) { }
        }

        private class ClassicallyControlledScope : ScopeTransformation<NoExpressionTransformations>
        {
            public ClassicallyControlledScope(NoExpressionTransformations expr = null) : base (expr ?? new NoExpressionTransformations()) { }

            public (bool, QsResult, TypedExpression, QsScope, QsScope) IsConditionedOnResultLiteralStatement(QsStatementKind statement)
            {
                if (statement is QsStatementKind.QsConditionalStatement cond)
                {
                    if (cond.Item.ConditionalBlocks.Length == 1 && (cond.Item.ConditionalBlocks[0].Item1.Expression is ExpressionKind.EQ expression))
                    {
                        var scope = cond.Item.ConditionalBlocks[0].Item2.Body;
                        var defaultScope = cond.Item.Default.ValueOr(null)?.Body;

                        if (expression.Item1.Expression is ExpressionKind.ResultLiteral exp1)
                        {
                            return (true, exp1.Item, expression.Item2, scope, defaultScope);
                        }
                        else if (expression.Item2.Expression is ExpressionKind.ResultLiteral exp2)
                        {
                            return (true, exp2.Item, expression.Item1, scope, defaultScope);
                        }
                    }
                }

                return (false, null, null, null, null);
            }

            public bool AreSimpleCallStatements(IEnumerable<QsStatement> stmts) =>
                stmts.Select(s => IsSimpleCallStatement(s.Statement).Item1).All(b => b);

            public (bool, TypedExpression, TypedExpression) IsSimpleCallStatement(QsStatementKind statement)
            {
                if (statement is QsStatementKind.QsExpressionStatement expr)
                {
                    var returnType = expr.Item.ResolvedType;

                    if (returnType.Resolution.IsUnitType && expr.Item.Expression is ExpressionKind.CallLikeExpression call)
                    {
                        return (true, call.Item1, call.Item2);
                    }
                }

                return (false, null, null);
            }

            public TypedExpression CreateIdentifierExpression(Identifier id,
                QsNullable<ImmutableArray<ResolvedType>> typeParams, ResolvedType resolvedType) =>
                new TypedExpression
                (
                    ExpressionKind.NewIdentifier(id, typeParams),
                    ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty,
                    resolvedType,
                    new InferredExpressionInformation(false, false),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                );

            public TypedExpression CreateValueTupleExpression(params TypedExpression[] expressions) =>
                new TypedExpression
                (
                    ExpressionKind.NewValueTuple(expressions.ToImmutableArray()),
                    ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty,
                    ResolvedType.New(ResolvedTypeKind.NewTupleType(expressions.Select(expr => expr.ResolvedType).ToImmutableArray())),
                    new InferredExpressionInformation(false, false),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                );

            public TypedExpression CreateApplyIfCall(TypedExpression id, TypedExpression args, BuiltIn controlOp, ResolvedType opTypeParamResolution) =>
                new TypedExpression
                (
                    ExpressionKind.NewCallLikeExpression(id, args),
                    ImmutableArray.Create(Tuple.Create(new QsQualifiedName(controlOp.Namespace, controlOp.Name), controlOp.TypeParameters.First(), opTypeParamResolution)),
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    new InferredExpressionInformation(false, true),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                );

            public QsStatement CreateApplyIfStatement(QsResult result, TypedExpression conditionExpression, QsStatement s)
            {
                var (_, op, originalArgs) = IsSimpleCallStatement(s.Statement);

                var (controlOp, opType) = (result == QsResult.One)
                    ? (BuiltIn.ApplyIfOne, BuiltIn.ApplyIfOneResolvedType)
                    : (BuiltIn.ApplyIfZero, BuiltIn.ApplyIfZeroResolvedType);
                var applyIfOp = Identifier.NewGlobalCallable(new QsQualifiedName(controlOp.Namespace, controlOp.Name)) as Identifier.GlobalCallable;
                
                var id = CreateIdentifierExpression(applyIfOp, QsNullable<ImmutableArray<ResolvedType>>.Null, opType);
                var originalCall = CreateValueTupleExpression(op, originalArgs);
                var args = CreateValueTupleExpression(conditionExpression, originalCall);
                
                var call = CreateApplyIfCall(id, args, controlOp, originalArgs.ResolvedType);

                return new QsStatement(QsStatementKind.NewQsExpressionStatement(call), s.SymbolDeclarations, s.Location, s.Comments);
            }

            private static int _varCount = 0;

            public (QsStatement, TypedExpression) CreateNewConditionVariable(TypedExpression value, QsStatement condStatement)
            {
                _varCount++;
                var name = NonNullable<string>.New($"__classic_ctrl{_varCount}__");

                // The typed expression with the identifier of the variable we just created:
                var idExpression = CreateIdentifierExpression(Identifier.NewLocalVariable(name), QsNullable<ImmutableArray<ResolvedType>>.Null, value.ResolvedType);

                // The actual binding statement:
                var binding = new QsBinding<TypedExpression>(QsBindingKind.ImmutableBinding, SymbolTuple.NewVariableName(name), value);
                var symbDecl = new LocalDeclarations(condStatement.SymbolDeclarations.Variables.Add(new LocalVariableDeclaration<NonNullable<string>>
                    (
                        name,
                        value.ResolvedType,
                        new InferredExpressionInformation(false, false),
                        condStatement.Location.IsValue
                            ? QsNullable<Tuple<int, int>>.NewValue(condStatement.Location.Item.Offset)
                            : QsNullable<Tuple<int, int>>.Null,
                        condStatement.Location.IsValue
                            ? condStatement.Location.Item.Range
                            : Tuple.Create(QsPositionInfo.Zero, QsPositionInfo.Zero)
                    )));
                var stmt = new QsStatement(QsStatementKind.NewQsVariableDeclaration(binding), symbDecl, condStatement.Location, condStatement.Comments);
                
                return (stmt, idExpression);
            }

            public override QsScope Transform(QsScope scope)
            {
                scope = base.Transform(scope); // process sub-scopes first

                var statements = ImmutableArray.CreateBuilder<QsStatement>();
                foreach (var statement in scope.Statements)
                {
                    var (isCondition, result, conditionExpression, conditionScope, defaultScope) = IsConditionedOnResultLiteralStatement(statement.Statement);

                    if (isCondition && AreSimpleCallStatements(conditionScope.Statements) && (defaultScope == null || AreSimpleCallStatements(defaultScope.Statements)))
                    {
                        // The condition must be an identifier, otherwise we'll call it multiple times.
                        // If not, create a new variable and use that:
                        if (!(conditionExpression.Expression is ExpressionKind.Identifier))
                        {
                            var (letStmt, idExpression) = CreateNewConditionVariable(conditionExpression, statement);
                            statements.Add(letStmt);
                            conditionExpression = idExpression;
                        }

                        foreach (var stmt in conditionScope.Statements)
                        {
                            statements.Add(CreateApplyIfStatement(result, conditionExpression, stmt));
                        }

                        if (defaultScope != null)
                        {
                            foreach (var stmt in defaultScope.Statements)
                            {
                                statements.Add(CreateApplyIfStatement(result.IsOne ? QsResult.Zero : QsResult.One, conditionExpression, stmt));
                            }
                        }
                    }
                    else
                    {
                        statements.Add(this.onStatement(statement));
                    }
                }

                return new QsScope(statements.ToImmutableArray(), scope.KnownSymbols);
            }
        }
    }
}
