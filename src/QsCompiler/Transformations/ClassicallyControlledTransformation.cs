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
        private List<QsCallable> _ControlOperators;
        private QsCallable _CurrentCallable = null;

        public static QsCompilation Apply(QsCompilation compilation)
        {
            var filter = new ClassicallyControlledSyntax(new ClassicallyControlledTransformation());

            return new QsCompilation(compilation.Namespaces.Select(ns => filter.Transform(ns)).ToImmutableArray(), compilation.EntryPoints);
        }

        private ClassicallyControlledTransformation() { }

        private class ClassicallyControlledSyntax : SyntaxTreeTransformation<ClassicallyControlledScope>
        {
            private ClassicallyControlledTransformation _super;

            public ClassicallyControlledSyntax(ClassicallyControlledTransformation super, ClassicallyControlledScope scope = null) : base(scope ?? new ClassicallyControlledScope(super))
            {
                _super = super;
            }

            public override QsCallable onCallableImplementation(QsCallable c)
            {
                _super._CurrentCallable = c;
                return base.onCallableImplementation(c);
            }

            public override QsNamespace Transform(QsNamespace ns)
            {
                // Control operators list will be populated in the transform
                _super._ControlOperators = new List<QsCallable>();
                return base.Transform(ns)
                    .WithElements(elems => elems.AddRange(_super._ControlOperators.Select(op => QsNamespaceElement.NewQsCallable(op))));
            }
        }

        private class ClassicallyControlledScope : ScopeTransformation<NoExpressionTransformations>
        {
            private ClassicallyControlledTransformation _super;

            public ClassicallyControlledScope(ClassicallyControlledTransformation super, NoExpressionTransformations expr = null)
                : base (expr ?? new NoExpressionTransformations()) { _super = super; }

            private (bool, QsResult, TypedExpression, QsScope, QsScope) IsConditionedOnResultLiteralStatement(QsStatementKind statement)
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

            private bool AreSimpleCallStatements(IEnumerable<QsStatement> stmts) =>
                stmts.Select(s => IsSimpleCallStatement(s.Statement).Item1).All(b => b);

            private (bool, TypedExpression, TypedExpression) IsSimpleCallStatement(QsStatementKind statement)
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

            private TypedExpression CreateIdentifierExpression(Identifier id,
                QsNullable<ImmutableArray<ResolvedType>> typeParams, ResolvedType resolvedType) =>
                new TypedExpression
                (
                    ExpressionKind.NewIdentifier(id, typeParams),
                    ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty,
                    resolvedType,
                    new InferredExpressionInformation(false, false),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                );

            private TypedExpression CreateValueTupleExpression(params TypedExpression[] expressions) =>
                new TypedExpression
                (
                    ExpressionKind.NewValueTuple(expressions.ToImmutableArray()),
                    ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty,
                    ResolvedType.New(ResolvedTypeKind.NewTupleType(expressions.Select(expr => expr.ResolvedType).ToImmutableArray())),
                    new InferredExpressionInformation(false, false),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                );

            private TypedExpression CreateApplyIfCall(TypedExpression id, TypedExpression args, BuiltIn controlOp, ResolvedType opTypeParamResolution) =>
                new TypedExpression
                (
                    ExpressionKind.NewCallLikeExpression(id, args),
                    ImmutableArray.Create(Tuple.Create(new QsQualifiedName(controlOp.Namespace, controlOp.Name), controlOp.TypeParameters.First(), opTypeParamResolution)),
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    new InferredExpressionInformation(false, true),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                );

            private QsStatement CreateApplyIfStatement(QsResult result, TypedExpression conditionExpression, QsStatement s)
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

            private QsStatement CreateApplyIfStatement(QsStatement statement, QsResult result, TypedExpression conditionExpression, QsScope contents)
            {
                // Hoist the scope to its own operator
                var targetName = GenerateControlOperator(contents, statement.Comments);
                var targetOpType = ResolvedType.New(ResolvedTypeKind.NewOperation(
                    Tuple.Create(
                        ResolvedType.New(ResolvedTypeKind.UnitType), // ToDo: add generic params to operator
                        ResolvedType.New(ResolvedTypeKind.UnitType)), // ToDo: something has to be done to allow for mutables in sub-scopes
                    CallableInformation.NoInformation
                    ));
                var targetOpId = CreateIdentifierExpression(
                    Identifier.NewGlobalCallable(new QsQualifiedName(targetName.Namespace, targetName.Name)),
                    QsNullable<ImmutableArray<ResolvedType>>.Null, // ToDo: allow for type params to be passed in from super-scope
                    targetOpType);
                var targetArgs = CreateValueTupleExpression(); // ToDo: add generic params to operator

                // Build the surrounding apply-if call
                var (controlOp, controlOpType) = (result == QsResult.One)
                    ? (BuiltIn.ApplyIfOne, BuiltIn.ApplyIfOneResolvedType)
                    : (BuiltIn.ApplyIfZero, BuiltIn.ApplyIfZeroResolvedType);
                var controlOpId = CreateIdentifierExpression(
                    Identifier.NewGlobalCallable(new QsQualifiedName(controlOp.Namespace, controlOp.Name)),
                    QsNullable<ImmutableArray<ResolvedType>>.Null,
                    controlOpType);
                var controlArgs = CreateValueTupleExpression(conditionExpression, CreateValueTupleExpression(targetOpId, targetArgs));
                
                var controlCall = CreateApplyIfCall(controlOpId, controlArgs, controlOp, targetArgs.ResolvedType);

                return new QsStatement(
                    QsStatementKind.NewQsExpressionStatement(controlCall),
                    statement.SymbolDeclarations,
                    QsNullable<QsLocation>.Null,
                    statement.Comments);
            }

            private static int _varCount = 0;

            private (QsStatement, TypedExpression) CreateNewConditionVariable(TypedExpression value, QsStatement condStatement)
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

            private QsQualifiedName GenerateControlOperator(QsScope contents, QsComments comments)
            {
                var newName = new QsQualifiedName(
                            _super._CurrentCallable.FullName.Namespace,
                            NonNullable<string>.New("_" + Guid.NewGuid().ToString("N") + "_" + _super._CurrentCallable.FullName.Name.Value));

                var parameters = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>
                    .NewQsTuple(ImmutableArray<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>>.Empty); // ToDo: add generic params to operator

                var paramTypes = ResolvedType.New(ResolvedTypeKind.NewTupleType(ImmutableArray<ResolvedType>.Empty)); // ToDo: add generic params to operator

                var signature = new ResolvedSignature(
                        ImmutableArray<QsLocalSymbol>.Empty,
                        paramTypes,
                        ResolvedType.New(ResolvedTypeKind.UnitType),
                        CallableInformation.NoInformation);

                var spec = new QsSpecialization(
                    QsSpecializationKind.QsBody,
                    newName,
                    ImmutableArray<QsDeclarationAttribute>.Empty,
                    _super._CurrentCallable.SourceFile,
                    new QsLocation(Tuple.Create(0, 0), Tuple.Create(QsPositionInfo.Zero, QsPositionInfo.Zero)), //ToDo
                    QsNullable<ImmutableArray<ResolvedType>>.Null,
                    signature,
                    SpecializationImplementation.NewProvided(parameters, contents),
                    ImmutableArray<string>.Empty,
                    comments);

                var controlCallable = new QsCallable(
                    QsCallableKind.Operation,
                    newName,
                    ImmutableArray<QsDeclarationAttribute>.Empty,
                    _super._CurrentCallable.SourceFile,
                    new QsLocation(Tuple.Create(0, 0), Tuple.Create(QsPositionInfo.Zero, QsPositionInfo.Zero)), //ToDo
                    signature,
                    parameters,
                    ImmutableArray.Create(spec), //ToDo: account for ctrl and adjt
                    ImmutableArray<string>.Empty,
                    comments);

                _super._ControlOperators.Add(controlCallable);

                return newName;
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

                        //statements.Add(CreateApplyIfStatement(statement, result, conditionExpression, conditionScope));

                        foreach (var stmt in conditionScope.Statements)
                        {
                            statements.Add(CreateApplyIfStatement(result, conditionExpression, stmt));
                        }

                        if (defaultScope != null)
                        {
                            //statements.Add(CreateApplyIfStatement(statement, result.IsOne ? QsResult.Zero : QsResult.One, conditionExpression, defaultScope));

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
