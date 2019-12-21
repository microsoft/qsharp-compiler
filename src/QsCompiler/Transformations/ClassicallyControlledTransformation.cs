// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.FSharp.Core;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlledTransformation
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal static class Helper
    {
        public static TypedExpression CreateIdentifierExpression(Identifier id,
            QsNullable<ImmutableArray<ResolvedType>> typeParams, ResolvedType resolvedType) =>
            new TypedExpression
            (
                ExpressionKind.NewIdentifier(id, typeParams),
                ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty,
                resolvedType,
                new InferredExpressionInformation(false, false),
                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
            );

        public static TypedExpression CreateValueTupleExpression(params TypedExpression[] expressions) =>
            new TypedExpression
            (
                ExpressionKind.NewValueTuple(expressions.ToImmutableArray()),
                ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty,
                ResolvedType.New(ResolvedTypeKind.NewTupleType(expressions.Select(expr => expr.ResolvedType).ToImmutableArray())),
                new InferredExpressionInformation(false, false),
                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
            );
    }

    // This transformation works in two passes.
    // 1st Pass: Hoist the contents of conditional statements into separate operations, where possible.
    // 2nd Pass: On the way down the tree, reshape conditional statements to replace Elif's and
    // top level OR and AND conditions with equivalent nested if-else statements. One the way back up
    // the tree, convert conditional statements into ApplyIf calls, where possible.
    public class ClassicallyControlledTransformation
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            var filter = new SyntaxTreeTransformation<ClassicallyControlledScope>(new ClassicallyControlledScope());

            compilation = HoistTransformation.Apply(compilation);

            return new QsCompilation(compilation.Namespaces.Select(ns => filter.Transform(ns)).ToImmutableArray(), compilation.EntryPoints);
        }

        private ClassicallyControlledTransformation() { } 

        private class ClassicallyControlledScope : ScopeTransformation<NoExpressionTransformations>
        {
            public ClassicallyControlledScope(NoExpressionTransformations expr = null) : base (expr ?? new NoExpressionTransformations()) {}

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

            private (bool, TypedExpression, TypedExpression) IsValidScope(QsScope scope)
            {
                if (scope != null && scope.Statements.Length == 1)
                {
                    if (scope.Statements[0].Statement is QsStatementKind.QsExpressionStatement expr)
                    {
                        var returnType = expr.Item.ResolvedType;

                        if (returnType.Resolution.IsUnitType && expr.Item.Expression is ExpressionKind.CallLikeExpression call)
                        {
                            return (true, call.Item1, call.Item2);
                        }
                    }
                }

                return (false, null, null);
            }

            private TypedExpression CreateApplyIfCall(TypedExpression id, TypedExpression args, BuiltIn controlOp, IEnumerable<ResolvedType> opTypeParamResolutions) =>
                new TypedExpression
                (
                    ExpressionKind.NewCallLikeExpression(id, args),
                    opTypeParamResolutions
                        .Zip(controlOp.TypeParameters, (type, param) => Tuple.Create(new QsQualifiedName(controlOp.Namespace, controlOp.Name), param, type))
                        .ToImmutableArray(),
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    new InferredExpressionInformation(false, true),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                );

            private QsStatement CreateApplyIfStatement(QsStatement statement, QsResult result, TypedExpression conditionExpression, QsScope conditionScope, QsScope defaultScope)
            {
                var controlCall = GetApplyIfExpression(result, conditionExpression, conditionScope, defaultScope);

                if (controlCall != null)
                {
                    return new QsStatement(
                        QsStatementKind.NewQsExpressionStatement(controlCall),
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

            private TypedExpression GetApplyIfExpression(QsResult result, TypedExpression conditionExpression, QsScope conditionScope, QsScope defaultScope)
            {
                var (isCondValid, condId, condArgs) = IsValidScope(conditionScope);
                var (isDefaultValid, defaultId, defaultArgs) = IsValidScope(defaultScope);

                BuiltIn controlOpInfo;
                ResolvedType controlOpType;
                TypedExpression controlArgs;
                ImmutableArray<ResolvedType> targetArgs;
                if (isCondValid && isDefaultValid)
                {
                    controlOpInfo = BuiltIn.ApplyIfElseR;
                    controlOpType = BuiltIn.ApplyIfElseRResolvedType;

                    controlArgs = Helper.CreateValueTupleExpression(
                        conditionExpression,
                        Helper.CreateValueTupleExpression(condId, condArgs),
                        Helper.CreateValueTupleExpression(defaultId, defaultArgs));

                    targetArgs = ImmutableArray.Create(condArgs.ResolvedType, defaultArgs.ResolvedType);
                }
                else if (isCondValid && defaultScope == null)
                {
                    (controlOpInfo, controlOpType) = (result == QsResult.One)
                    ? (BuiltIn.ApplyIfOne, BuiltIn.ApplyIfOneResolvedType)
                    : (BuiltIn.ApplyIfZero, BuiltIn.ApplyIfZeroResolvedType);

                    controlArgs = Helper.CreateValueTupleExpression(
                        conditionExpression,
                        Helper.CreateValueTupleExpression(condId, condArgs));

                    targetArgs = ImmutableArray.Create(condArgs.ResolvedType);
                }
                else
                {
                    return null;
                }

                // Build the surrounding apply-if call
                var controlOpId = Helper.CreateIdentifierExpression(
                    Identifier.NewGlobalCallable(new QsQualifiedName(controlOpInfo.Namespace, controlOpInfo.Name)),
                    QsNullable<ImmutableArray<ResolvedType>>.Null,
                    controlOpType);

                return CreateApplyIfCall(controlOpId, controlArgs, controlOpInfo, targetArgs);
            }

            private (bool, QsConditionalStatement) ProcessElif(QsConditionalStatement cond)
            {
                if (cond.ConditionalBlocks.Length < 2) return (false, cond);

                var subCond = new QsConditionalStatement(cond.ConditionalBlocks.RemoveAt(0), cond.Default);
                var secondCondBlock = cond.ConditionalBlocks[1].Item2;

                var subIfStatment = new QsStatement
                (
                    QsStatementKind.NewQsConditionalStatement(subCond),
                    LocalDeclarations.Empty,
                    secondCondBlock.Location,
                    secondCondBlock.Comments
                );

                var newDefault = QsNullable<QsPositionedBlock>.NewValue(new QsPositionedBlock(
                    new QsScope(ImmutableArray.Create(subIfStatment), secondCondBlock.Body.KnownSymbols),
                    secondCondBlock.Location,
                    QsComments.Empty));

                return (true, new QsConditionalStatement(ImmutableArray.Create(cond.ConditionalBlocks[0]), newDefault));
            }

            private (bool, QsConditionalStatement) ProcessOR(QsConditionalStatement cond)
            {
                // This method expects elif blocks to have been abstracted out
                if (cond.ConditionalBlocks.Length != 1) return (false, cond);

                var (condition, block) = cond.ConditionalBlocks[0];

                if (condition.Expression is ExpressionKind.OR orCond)
                {
                    var subCond = new QsConditionalStatement(ImmutableArray.Create(Tuple.Create(orCond.Item2, block)), cond.Default);
                    var subIfStatment = new QsStatement
                    (
                        QsStatementKind.NewQsConditionalStatement(subCond),
                        LocalDeclarations.Empty,
                        block.Location,
                        QsComments.Empty
                    );
                    var newDefault = QsNullable<QsPositionedBlock>.NewValue(new QsPositionedBlock(
                        new QsScope(ImmutableArray.Create(subIfStatment), block.Body.KnownSymbols),
                        block.Location,
                        QsComments.Empty));

                    return (true, new QsConditionalStatement(ImmutableArray.Create(Tuple.Create(orCond.Item1, block)), newDefault));
                }
                else
                {
                    return (false, cond);
                }
            }

            private (bool, QsConditionalStatement) ProcessAND(QsConditionalStatement cond)
            {
                // This method expects elif blocks to have been abstracted out
                if (cond.ConditionalBlocks.Length != 1) return (false, cond);

                var (condition, block) = cond.ConditionalBlocks[0];

                if (condition.Expression is ExpressionKind.AND andCond)
                {
                    var subCond = new QsConditionalStatement(ImmutableArray.Create(Tuple.Create(andCond.Item2, block)), cond.Default);
                    var subIfStatment = new QsStatement
                    (
                        QsStatementKind.NewQsConditionalStatement(subCond),
                        LocalDeclarations.Empty,
                        block.Location,
                        QsComments.Empty
                    );
                    var newBlock = new QsPositionedBlock(
                        new QsScope(ImmutableArray.Create(subIfStatment), block.Body.KnownSymbols),
                        block.Location,
                        QsComments.Empty);

                    return (true, new QsConditionalStatement(ImmutableArray.Create(Tuple.Create(andCond.Item1, newBlock)), cond.Default));
                }
                else
                {
                    return (false, cond);
                }
            }

            private QsStatement ReshapeConditional(QsStatement statement)
            {
                if (statement.Statement is QsStatementKind.QsConditionalStatement cond)
                {
                    var stm = cond.Item;
                    (_, stm) = ProcessElif(stm);
                    bool wasOrProcessed, wasAndProcessed;
                    do
                    {
                        (wasOrProcessed, stm) = ProcessOR(stm);
                        (wasAndProcessed, stm) = ProcessAND(stm);
                    } while (wasOrProcessed || wasAndProcessed);

                    return new QsStatement
                    (
                        QsStatementKind.NewQsConditionalStatement(stm),
                        statement.SymbolDeclarations,
                        statement.Location,
                        statement.Comments
                    );
                }
                return statement;
            }

            public override QsScope Transform(QsScope scope)
            {
                var parentSymbols = this.onLocalDeclarations(scope.KnownSymbols);
                var statements = new List<QsStatement>();
                
                foreach (var statement in scope.Statements)
                {
                    if (statement.Statement is QsStatementKind.QsConditionalStatement)
                    {
                        var stm = ReshapeConditional(statement);
                        stm = this.onStatement(stm);

                        var (isCondition, result, conditionExpression, conditionScope, defaultScope) = IsConditionedOnResultLiteralStatement(stm);

                        if (isCondition)
                        {
                            statements.Add(CreateApplyIfStatement(stm, result, conditionExpression, conditionScope, defaultScope));
                        }
                        else
                        {
                            statements.Add(this.onStatement(stm));
                        }
                    }
                    else
                    {
                        statements.Add(this.onStatement(statement));
                    }
                }
                
                return new QsScope(statements.ToImmutableArray(), parentSymbols);
            }
        }

        // Transformation that updates the contents of newly generated operations by:
        // 1. Rerouting the origins of type parameter references to the new operation
        // 2. Changes the IsMutable info on variable that used to be mutable, but are now immutable params to the operation
        private class UpdateGeneratedOpTransformation
        {
            private bool _IsRecursiveIdentifier = false;
            private ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> _Parameters;
            private QsQualifiedName _OldName;
            private QsQualifiedName _NewName;

            public static QsCallable Apply(QsCallable qsCallable, ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> parameters, QsQualifiedName oldName, QsQualifiedName newName)
            {
                var filter = new SyntaxTreeTransformation<ScopeTransformation<UpdateGeneratedOpExpression>>(
                    new ScopeTransformation<UpdateGeneratedOpExpression>(
                        new UpdateGeneratedOpExpression(
                            new UpdateGeneratedOpTransformation(parameters, oldName, newName))));

                return filter.onCallableImplementation(qsCallable);
            }

            private UpdateGeneratedOpTransformation(ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> parameters, QsQualifiedName oldName, QsQualifiedName newName)
            {
                _Parameters = parameters;
                _OldName = oldName;
                _NewName = newName;
            }

            private class UpdateGeneratedOpExpression : ExpressionTransformation<Core.ExpressionKindTransformation, UpdateGeneratedOpExpressionType>
            {
                private UpdateGeneratedOpTransformation _super;

                public UpdateGeneratedOpExpression(UpdateGeneratedOpTransformation super) :
                    base(expr => new UpdateGeneratedOpExpressionKind(super, expr as UpdateGeneratedOpExpression),
                         expr => new UpdateGeneratedOpExpressionType(super, expr as UpdateGeneratedOpExpression))
                { _super = super; }

                public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> onTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
                {
                    // Prevent keys from having their names updated
                    return typeParams.ToImmutableDictionary(kvp => kvp.Key, kvp => this.Type.Transform(kvp.Value));
                }

                public override TypedExpression Transform(TypedExpression ex)
                {
                    // Checks if expression is mutable identifier that is in parameter list
                    if (ex.InferredInformation.IsMutable &&
                        ex.Expression is ExpressionKind.Identifier id &&
                        id.Item1 is Identifier.LocalVariable variable &&
                        _super._Parameters.Any(x => x.VariableName.Equals(variable)))
                    {
                        // Set the mutability to false
                        ex = new TypedExpression(
                            ex.Expression,
                            ex.TypeArguments,
                            ex.ResolvedType,
                            new InferredExpressionInformation(false, ex.InferredInformation.HasLocalQuantumDependency),
                            ex.Range);
                    }

                    // prevent _IsRecursiveIdentifier from propagating beyond the typed expression it is referring to
                    var isRecursiveIdentifier = _super._IsRecursiveIdentifier;
                    var rtrn = base.Transform(ex);
                    _super._IsRecursiveIdentifier = isRecursiveIdentifier;
                    return rtrn;
                }
            }

            private class UpdateGeneratedOpExpressionKind : ExpressionKindTransformation<UpdateGeneratedOpExpression>
            {
                private UpdateGeneratedOpTransformation _super;

                public UpdateGeneratedOpExpressionKind(UpdateGeneratedOpTransformation super, UpdateGeneratedOpExpression expr) : base(expr) { _super = super; }

                public override ExpressionKind onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
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

            private class UpdateGeneratedOpExpressionType : ExpressionTypeTransformation<UpdateGeneratedOpExpression>
            {
                private UpdateGeneratedOpTransformation _super;

                public UpdateGeneratedOpExpressionType(UpdateGeneratedOpTransformation super, UpdateGeneratedOpExpression expr) : base(expr) { _super = super; }

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

        // Transformation handling the first pass task of hoisting of the contents of conditional statements.
        // If blocks are first validated to see if they can safely be hoisted into their own operation.
        // Validation requirements are that there are no return statements and that there are no set statements
        // on mutables declared outside the block. Setting mutables declared inside the block is valid.
        // If the block is valid, and there is more than one statement in the block, a new operation with the
        // block's contents is generated, having all the same type parameters as the calling context
        // and all known variables at the start of the block become parameters to the new operation.
        // The contents of the conditional block are then replaced with a call to the new operation with all
        // the type parameters and known variables being forwarded to the new operation as arguments.
        private class HoistTransformation
        {
            private bool _IsValidScope = true;
            private List<QsCallable> _ControlOperations;
            private QsCallable _CurrentCallable = null;
            private ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> _CurrentHoistParams =
                ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty;
            private bool _ContainsHoistParamRef = false; // ToDo: May need to explicitly reset this value after every statement.

            public static QsCompilation Apply(QsCompilation compilation)
            {
                var filter = new HoistSyntax(new HoistTransformation());

                return new QsCompilation(compilation.Namespaces.Select(ns => filter.Transform(ns)).ToImmutableArray(), compilation.EntryPoints);
            }

            private (QsQualifiedName, ResolvedType) GenerateOperation(QsScope contents)
            {
                var newName = new QsQualifiedName(
                    _CurrentCallable.FullName.Namespace,
                    NonNullable<string>.New("_" + Guid.NewGuid().ToString("N") + "_" + _CurrentCallable.FullName.Name.Value));

                var knownVariables = contents.KnownSymbols.IsEmpty
                    ? ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty
                    : contents.KnownSymbols.Variables;

                var parameters = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.NewQsTuple(knownVariables
                    .Select(var => QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.NewQsTupleItem(new LocalVariableDeclaration<QsLocalSymbol>(
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
                    _CurrentCallable.Signature.TypeParameters,
                    paramTypes,
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    CallableInformation.NoInformation);

                var spec = new QsSpecialization(
                    QsSpecializationKind.QsBody,
                    newName,
                    ImmutableArray<QsDeclarationAttribute>.Empty,
                    _CurrentCallable.SourceFile,
                    QsNullable<QsLocation>.Null,
                    QsNullable<ImmutableArray<ResolvedType>>.Null,
                    signature,
                    SpecializationImplementation.NewProvided(parameters, contents),
                    ImmutableArray<string>.Empty,
                    QsComments.Empty);

                var controlCallable = new QsCallable(
                    QsCallableKind.Operation,
                    newName,
                    ImmutableArray<QsDeclarationAttribute>.Empty,
                    _CurrentCallable.SourceFile,
                    QsNullable<QsLocation>.Null,
                    signature,
                    parameters,
                    ImmutableArray.Create(spec), //ToDo: account for ctrl and adjt
                    ImmutableArray<string>.Empty,
                    QsComments.Empty);

                var reroutedCallable = UpdateGeneratedOpTransformation.Apply(controlCallable, knownVariables, _CurrentCallable.FullName, newName);
                _ControlOperations.Add(reroutedCallable);

                return (newName, paramTypes);
            }

            private HoistTransformation() { }

            private class HoistSyntax : SyntaxTreeTransformation<ScopeTransformation<HoistStatementKind, HoistExpression>>
            {
                private HoistTransformation _super;

                public HoistSyntax(HoistTransformation super, ScopeTransformation<HoistStatementKind, HoistExpression> scope = null) :
                    base(scope ?? new ScopeTransformation<HoistStatementKind, HoistExpression>(
                        scopeTransform => new HoistStatementKind(super, scopeTransform),
                        new HoistExpression(super)))
                { _super = super; }

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

            private class HoistStatementKind : StatementKindTransformation<ScopeTransformation<HoistStatementKind, HoistExpression>>
            {
                private HoistTransformation _super;

                public HoistStatementKind(HoistTransformation super, ScopeTransformation<HoistStatementKind, HoistExpression> scope) : base(scope) { _super = super; }

                private QsStatement HoistIfContents(QsScope contents)
                {
                    var (targetName, targetParamType) = _super.GenerateOperation(contents);
                    var targetOpType = ResolvedType.New(ResolvedTypeKind.NewOperation(
                        Tuple.Create(
                            targetParamType,
                            ResolvedType.New(ResolvedTypeKind.UnitType)), // ToDo: something has to be done to allow for mutables in sub-scopes
                        CallableInformation.NoInformation));

                    var targetTypeArgTypes = GetTypeParamTypesFromCallable(_super._CurrentCallable);
                    var targetOpId = new TypedExpression
                    (
                        ExpressionKind.NewIdentifier(Identifier.NewGlobalCallable(targetName), targetTypeArgTypes),
                        targetTypeArgTypes.IsNull
                            ? ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty
                            : targetTypeArgTypes.Item
                                .Select(type => Tuple.Create(targetName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
                                .ToImmutableArray(),
                        targetOpType,
                        new InferredExpressionInformation(false, false),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                    );

                    var knownSymbols = contents.KnownSymbols.IsEmpty
                        ? ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty
                        : contents.KnownSymbols.Variables;

                    TypedExpression targetArgs = null;
                    if (knownSymbols.Any())
                    {
                        targetArgs = Helper.CreateValueTupleExpression(knownSymbols.Select(var => Helper.CreateIdentifierExpression(
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

                    var call = new TypedExpression
                    (
                        ExpressionKind.NewCallLikeExpression(targetOpId, targetArgs),
                        // All type params are resolved on the Identifier
                        ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty,
                        ResolvedType.New(ResolvedTypeKind.UnitType),
                        new InferredExpressionInformation(false, true),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                    );

                    return new QsStatement(
                        QsStatementKind.NewQsExpressionStatement(call),
                        LocalDeclarations.Empty,
                        QsNullable<QsLocation>.Null,
                        QsComments.Empty);
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

                public override QsStatementKind onReturnStatement(TypedExpression ex)
                {
                    _super._IsValidScope = false;
                    return base.onReturnStatement(ex);
                }

                public override QsStatementKind onValueUpdate(QsValueUpdate stm)
                {
                    // If lhs contains an identifier found in the scope's known variables, the scope is not valid
                    var lhs = this.ExpressionTransformation(stm.Lhs);

                    if (_super._ContainsHoistParamRef)
                    {
                        _super._IsValidScope = false;
                    }

                    var rhs = this.ExpressionTransformation(stm.Rhs);
                    return QsStatementKind.NewQsValueUpdate(new QsValueUpdate(lhs, rhs));
                }

                public override QsStatementKind onConditionalStatement(QsConditionalStatement stm)
                {
                    // ToDo: Revisit this method when the F# Option type has been removed from the onPositionBlock function.

                    var contextValidScope = _super._IsValidScope;
                    var contextHoistParams = _super._CurrentHoistParams;

                    var newConditionBlocks = stm.ConditionalBlocks
                        .Select(condBlock =>
                        {
                            _super._IsValidScope = true;
                            _super._CurrentHoistParams = condBlock.Item2.Body.KnownSymbols.IsEmpty
                            ? ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty
                            : condBlock.Item2.Body.KnownSymbols.Variables;

                            var (expr, block) = this.onPositionedBlock(condBlock.Item1, condBlock.Item2);
                            if (_super._IsValidScope && block.Body.Statements.Length > 1) // if sub-scope is valid, hoist content
                            {
                                // Hoist the scope to its own operation
                                var call = HoistIfContents(block.Body);
                                block = new QsPositionedBlock(
                                    new QsScope(ImmutableArray.Create(call), block.Body.KnownSymbols),
                                    block.Location,
                                    block.Comments);
                            }
                            return Tuple.Create(expr.Value, block); // ToDo: .Value may be unnecessary in the future
                        }).ToImmutableArray();

                    var newDefault = QsNullable<QsPositionedBlock>.Null;
                    if (stm.Default.IsValue)
                    {
                        _super._IsValidScope = true;
                        _super._CurrentHoistParams = stm.Default.Item.Body.KnownSymbols.IsEmpty
                            ? ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty
                            : stm.Default.Item.Body.KnownSymbols.Variables;

                        var (_, block) = this.onPositionedBlock(null, stm.Default.Item); // ToDo: null is probably bad here
                        if (_super._IsValidScope && block.Body.Statements.Length > 1) // if sub-scope is valid, hoist content
                        {
                            // Hoist the scope to its own operation
                            var call = HoistIfContents(block.Body);
                            block = new QsPositionedBlock(
                                new QsScope(ImmutableArray.Create(call), block.Body.KnownSymbols),
                                block.Location,
                                block.Comments);
                        }
                        newDefault = QsNullable<QsPositionedBlock>.NewValue(block);
                    }

                    _super._CurrentHoistParams = contextHoistParams;
                    _super._IsValidScope = contextValidScope;

                    return QsStatementKind.NewQsConditionalStatement(
                        new QsConditionalStatement(newConditionBlocks, newDefault));
                }
            }

            private class HoistExpression : ExpressionTransformation<HoistExpressionKind>
            {
                private HoistTransformation _super;

                public HoistExpression(HoistTransformation super) :
                    base(expr => new HoistExpressionKind(super, expr as HoistExpression))
                { _super = super; }

                public override TypedExpression Transform(TypedExpression ex)
                {
                    var contextContainsHoistParamRef = _super._ContainsHoistParamRef;
                    _super._ContainsHoistParamRef = false;
                    var rtrn = base.Transform(ex);

                    // If the sub context contains a reference, then the super context contains a reference,
                    // otherwise return the super context to its original value
                    if (!_super._ContainsHoistParamRef)
                    {
                        _super._ContainsHoistParamRef = contextContainsHoistParamRef;
                    }

                    return rtrn;
                }
            }

            private class HoistExpressionKind : ExpressionKindTransformation<HoistExpression>
            {
                private HoistTransformation _super;

                public HoistExpressionKind(HoistTransformation super, HoistExpression expr) : base(expr) { _super = super; }

                public override ExpressionKind onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    if (sym is Identifier.LocalVariable local &&
                        _super._CurrentHoistParams.Any(param => param.VariableName.Equals(local.Item)))
                    {
                        _super._ContainsHoistParamRef = true;
                    }
                    return base.onIdentifier(sym, tArgs);
                }
            }
        }
    }
}
