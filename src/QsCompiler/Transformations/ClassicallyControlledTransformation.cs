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
    using ParamTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;

    public class ClassicallyControlledTransformation
    {
        private List<QsCallable> _ControlOperations;
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
                // Control operations list will be populated in the transform
                _super._ControlOperations = new List<QsCallable>();
                return base.Transform(ns)
                    .WithElements(elems => elems.AddRange(_super._ControlOperations.Select(op => QsNamespaceElement.NewQsCallable(op))));
            }
        }

        private class ClassicallyControlledScope : ScopeTransformation<NoExpressionTransformations>
        {
            private ClassicallyControlledTransformation _super;

            public ClassicallyControlledScope(ClassicallyControlledTransformation super, NoExpressionTransformations expr = null)
                : base (expr ?? new NoExpressionTransformations()) { _super = super; }

            private QsStatement MakeNestedIfs(QsStatement originalStatment, QsStatementKind.QsConditionalStatement condStatmentKind)
            {
                var cond = condStatmentKind.Item;

                if (cond.ConditionalBlocks.Length == 1)
                {
                    return new QsStatement
                    (
                        condStatmentKind,
                        originalStatment.SymbolDeclarations,
                        originalStatment.Location,
                        originalStatment.Comments
                    );
                }

                var subIfKind = (QsStatementKind.QsConditionalStatement)QsStatementKind.NewQsConditionalStatement(
                    new QsConditionalStatement(cond.ConditionalBlocks.RemoveAt(0), cond.Default));

                var subIfStatment = MakeNestedIfs(originalStatment, subIfKind);

                var secondCondBlock = cond.ConditionalBlocks[1].Item2;
                var newDefault = QsNullable<QsPositionedBlock>.NewValue(new QsPositionedBlock(
                    new QsScope(ImmutableArray.Create(subIfStatment), secondCondBlock.Body.KnownSymbols),
                    secondCondBlock.Location,
                    secondCondBlock.Comments));

                return new QsStatement
                (
                    QsStatementKind.NewQsConditionalStatement(new QsConditionalStatement(ImmutableArray.Create(cond.ConditionalBlocks[0]), newDefault)),
                    originalStatment.SymbolDeclarations,
                    originalStatment.Location,
                    originalStatment.Comments
                );
            }

            private QsStatement MakeNestedIfs(QsStatement originalStatment)
            {
                if (originalStatment.Statement is QsStatementKind.QsConditionalStatement cond)
                {
                    var nested = MakeNestedIfs(originalStatment, cond);
                    var nestedKind = ((QsStatementKind.QsConditionalStatement)nested.Statement).Item;
                    if (nestedKind.Default.IsValue)
                    {
                        var newDefault = QsNullable<QsPositionedBlock>.NewValue(new QsPositionedBlock(
                            this.Transform(nestedKind.Default.Item.Body),
                            nestedKind.Default.Item.Location,
                            nestedKind.Default.Item.Comments));

                        nested = new QsStatement
                        (
                            QsStatementKind.NewQsConditionalStatement(new QsConditionalStatement(nestedKind.ConditionalBlocks, newDefault)),
                            nested.SymbolDeclarations,
                            nested.Location,
                            nested.Comments
                        );
                    }

                    return nested;
                }

                return originalStatment;
            }

            private (bool, QsResult, TypedExpression, QsScope, QsScope) IsConditionedOnResultLiteralStatement(QsStatement statement)
            {
                if (statement.Statement is QsStatementKind.QsConditionalStatement cond)
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

            private QsStatement CreateApplyIfStatement(QsStatement statement, QsResult result, TypedExpression conditionExpression, QsScope contents)
            {
                // Hoist the scope to its own operation
                var (targetName, targetParamType) = GenerateControlOperation(contents, statement.Comments);
                var targetOpType = ResolvedType.New(ResolvedTypeKind.NewOperation(
                    Tuple.Create(
                        targetParamType,
                        ResolvedType.New(ResolvedTypeKind.UnitType)), // ToDo: something has to be done to allow for mutables in sub-scopes
                    CallableInformation.NoInformation));

                var targetTypeParamTypes = GetTypeParamTypesFromCallable(_super._CurrentCallable);
                var targetOpId = new TypedExpression
                (
                    ExpressionKind.NewIdentifier(Identifier.NewGlobalCallable(targetName), targetTypeParamTypes),
                    targetTypeParamTypes.IsNull
                        ? ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty
                        : targetTypeParamTypes.Item
                            .Select(type => Tuple.Create(targetName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
                            .ToImmutableArray(),
                    targetOpType,
                    new InferredExpressionInformation(false, false),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                );

                TypedExpression targetArgs = null;
                if (contents.KnownSymbols.Variables.Any())
                {
                    targetArgs = CreateValueTupleExpression(contents.KnownSymbols.Variables.Select(var => CreateIdentifierExpression(
                        Identifier.NewLocalVariable(var.VariableName),
                        QsNullable<ImmutableArray<ResolvedType>>.Null,
                        var.Type))
                        .ToArray());
                }
                else
                {
                    targetArgs = new TypedExpression
                    (
                        ExpressionKind.UnitValue,
                        ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty,
                        ResolvedType.New(ResolvedTypeKind.UnitType),
                        new InferredExpressionInformation(false, false),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                    );
                }

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
                var idExpression = CreateIdentifierExpression(
                    Identifier.NewLocalVariable(name),
                    QsNullable<ImmutableArray<ResolvedType>>.Null,
                    value.ResolvedType);

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

            private QsNullable<ImmutableArray<ResolvedType>> GetTypeParamTypesFromCallable(QsCallable callable)
            {
                if (callable.Signature.TypeParameters.Any(param => param.IsValidName))
                {
                    return QsNullable<ImmutableArray<ResolvedType>>.NewValue(callable.Signature.TypeParameters
                    .Where(param => param.IsValidName)
                    .Select(param =>
                        ResolvedType.New(ResolvedTypeKind.NewTypeParameter(new QsTypeParameter(
                            callable.FullName,
                            ((QsLocalSymbol.ValidName)param).Item,
                            QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                    ))))
                    .ToImmutableArray());
                }
                else
                {
                    return QsNullable<ImmutableArray<ResolvedType>>.Null;
                }
            }
            
            private (QsQualifiedName, ResolvedType) GenerateControlOperation(QsScope contents, QsComments comments)
            {
                var newName = new QsQualifiedName(
                    _super._CurrentCallable.FullName.Namespace,
                    NonNullable<string>.New("_" + Guid.NewGuid().ToString("N") + "_" + _super._CurrentCallable.FullName.Name.Value));

                var knownVariables = contents.KnownSymbols.Variables;

                var parameters = ParamTuple.NewQsTuple(knownVariables
                    .Select(var => ParamTuple.NewQsTupleItem(new LocalVariableDeclaration<QsLocalSymbol>(
                        QsLocalSymbol.NewValidName(var.VariableName),
                        var.Type,
                        var.InferredInformation,
                        var.Position,
                        var.Range)))
                    .ToImmutableArray());

                var paramTypes = ResolvedType.New(ResolvedTypeKind.UnitType);
                if (knownVariables.Any())
                {
                    paramTypes = ResolvedType.New(ResolvedTypeKind.NewTupleType(knownVariables
                        .Select(var => var.Type)
                        .ToImmutableArray()));
                }

                var signature = new ResolvedSignature(
                    _super._CurrentCallable.Signature.TypeParameters,
                    paramTypes,
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    CallableInformation.NoInformation);

                var spec = new QsSpecialization(
                    QsSpecializationKind.QsBody,
                    newName,
                    ImmutableArray<QsDeclarationAttribute>.Empty,
                    _super._CurrentCallable.SourceFile,
                    QsNullable<QsLocation>.Null,
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
                    QsNullable<QsLocation>.Null,
                    signature,
                    parameters,
                    ImmutableArray.Create(spec), //ToDo: account for ctrl and adjt
                    ImmutableArray<string>.Empty,
                    comments);

                var reroutedCallable = RerouteTypeParamOriginTransformation.Apply(controlCallable, _super._CurrentCallable.FullName, newName);
                _super._ControlOperations.Add(reroutedCallable);

                return (newName, paramTypes);
            }

            public override QsScope Transform(QsScope scope)
            {
                scope = base.Transform(scope); // process sub-scopes first

                var statements = ImmutableArray.CreateBuilder<QsStatement>();
                foreach (var s in scope.Statements)
                {
                    var statement = MakeNestedIfs(s);
                    var (isCondition, result, conditionExpression, conditionScope, defaultScope) = IsConditionedOnResultLiteralStatement(statement);

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

                        statements.Add(CreateApplyIfStatement(statement, result, conditionExpression, conditionScope));

                        if (defaultScope != null)
                        {
                            statements.Add(CreateApplyIfStatement(statement, result.IsOne ? QsResult.Zero : QsResult.One, conditionExpression, defaultScope));
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

        private class RerouteTypeParamOriginTransformation
        {
            private bool _IsRecursiveIdentifier = false;
            private QsQualifiedName _OldName;
            private QsQualifiedName _NewName;

            public static QsCallable Apply(QsCallable qsCallable, QsQualifiedName oldName, QsQualifiedName newName)
            {
                var filter = new SyntaxTreeTransformation<ScopeTransformation<RerouteTypeParamOriginExpression>>(
                    new ScopeTransformation<RerouteTypeParamOriginExpression>(
                        new RerouteTypeParamOriginExpression(
                            new RerouteTypeParamOriginTransformation(oldName, newName))));

                return filter.onCallableImplementation(qsCallable);
            }

            private RerouteTypeParamOriginTransformation(QsQualifiedName oldName, QsQualifiedName newName) { _OldName = oldName; _NewName = newName; }

            private class RerouteTypeParamOriginExpression : ExpressionTransformation<Core.ExpressionKindTransformation, RerouteTypeParamOriginExpressionType>
            {
                private RerouteTypeParamOriginTransformation _super;

                public RerouteTypeParamOriginExpression(RerouteTypeParamOriginTransformation super) :
                    base(expr => new RerouteTypeParamOriginExpressionKind(super, expr as RerouteTypeParamOriginExpression),
                         expr => new RerouteTypeParamOriginExpressionType(super, expr as RerouteTypeParamOriginExpression))
                { _super = super; }

                public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> onTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
                {
                    return typeParams.ToImmutableDictionary(kvp => kvp.Key, kvp => this.Type.Transform(kvp.Value));
                }

                public override TypedExpression Transform(TypedExpression ex)
                {
                    // prevent _IsRecursiveIdentifier from propagating beyond the typed expression it is referring to
                    var isRecursiveIdentifier = _super._IsRecursiveIdentifier;
                    var rtrn = base.Transform(ex);
                    _super._IsRecursiveIdentifier = isRecursiveIdentifier;
                    return rtrn;
                }
            }

            private class RerouteTypeParamOriginExpressionKind : ExpressionKindTransformation<RerouteTypeParamOriginExpression>
            {
                private RerouteTypeParamOriginTransformation _super;

                public RerouteTypeParamOriginExpressionKind(RerouteTypeParamOriginTransformation super, RerouteTypeParamOriginExpression expr) : base(expr) { _super = super; }

                public override ExpressionKind onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    // Process the identifier (including its type arguments)
                    var rtrn = base.onIdentifier(sym, tArgs);

                    // Then check if this is a recursive identifier
                    if (sym is Identifier.GlobalCallable callable && _super._OldName.Equals(callable.Item))
                    {
                        // Setting this flag will prevent the rerouting logic from processing the resolved type of the recursive identifier expression.
                        // This is necessary because we don't want any type parameters from the original callable from being rerouted to the new generated
                        // operation's type parameters in the definition of the identifier.
                        _super._IsRecursiveIdentifier = true;
                    }
                    return rtrn;
                }
            }

            private class RerouteTypeParamOriginExpressionType : ExpressionTypeTransformation<RerouteTypeParamOriginExpression>
            {
                private RerouteTypeParamOriginTransformation _super;

                public RerouteTypeParamOriginExpressionType(RerouteTypeParamOriginTransformation super, RerouteTypeParamOriginExpression expr) : base(expr) { _super = super; }

                public override ResolvedTypeKind onTypeParameter(QsTypeParameter tp)
                {
                    if (!_super._IsRecursiveIdentifier && _super._OldName.Equals(tp.Origin))
                    {
                        tp = new QsTypeParameter
                        (
                            _super._NewName,
                            tp.TypeName,
                            tp.Range
                        );
                    }

                    return base.onTypeParameter(tp);
                }
            }
        }
    }
}
