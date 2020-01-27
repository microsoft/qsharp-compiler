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
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>;

    // This transformation works in two passes.
    // 1st Pass: Hoist the contents of conditional statements into separate operations, where possible.
    // 2nd Pass: On the way down the tree, reshape conditional statements to replace Elif's and
    // top level OR and AND conditions with equivalent nested if-else statements. One the way back up
    // the tree, convert conditional statements into ApplyIf calls, where possible.
    public class ClassicallyControlledTransformation
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            var filter = new ClassicallyControlledSyntax();

            compilation = HoistTransformation.Apply(compilation);

            return new QsCompilation(compilation.Namespaces.Select(ns => filter.Transform(ns)).ToImmutableArray(), compilation.EntryPoints);
        }

        private static TypedExpression CreateIdentifierExpression(Identifier id,
            TypeArgsResolution typeArgsMapping, ResolvedType resolvedType) =>
            new TypedExpression
            (
                ExpressionKind.NewIdentifier(
                    id,
                    typeArgsMapping.Any()
                    ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(typeArgsMapping
                        .Select(argMapping => argMapping.Item3) // This should preserve the order of the type args
                        .ToImmutableArray())
                    : QsNullable<ImmutableArray<ResolvedType>>.Null),
                typeArgsMapping,
                resolvedType,
                new InferredExpressionInformation(false, false),
                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
            );

        private static TypedExpression CreateValueTupleExpression(params TypedExpression[] expressions) =>
            new TypedExpression
            (
                ExpressionKind.NewValueTuple(expressions.ToImmutableArray()),
                TypeArgsResolution.Empty,
                ResolvedType.New(ResolvedTypeKind.NewTupleType(expressions.Select(expr => expr.ResolvedType).ToImmutableArray())),
                new InferredExpressionInformation(false, false),
                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
            );

        private static (bool, QsResult, TypedExpression) IsConditionedOnResultLiteralExpression(TypedExpression expression)
        {
            if (expression.Expression is ExpressionKind.EQ eq)
            {
                if (eq.Item1.Expression is ExpressionKind.ResultLiteral exp1)
                {
                    return (true, exp1.Item, eq.Item2);
                }
                else if (eq.Item2.Expression is ExpressionKind.ResultLiteral exp2)
                {
                    return (true, exp2.Item, eq.Item1);
                }
            }

            return (false, null, null);
        }

        private static (bool, QsResult, TypedExpression, QsScope, QsScope) IsConditionedOnResultLiteralStatement(QsStatement statement)
        {
            if (statement.Statement is QsStatementKind.QsConditionalStatement cond)
            {
                if (cond.Item.ConditionalBlocks.Length == 1 && (cond.Item.ConditionalBlocks[0].Item1.Expression is ExpressionKind.EQ expression))
                {
                    var scope = cond.Item.ConditionalBlocks[0].Item2.Body;
                    var defaultScope = cond.Item.Default.ValueOr(null)?.Body;

                    var (success, literal, expr) = IsConditionedOnResultLiteralExpression(cond.Item.ConditionalBlocks[0].Item1);

                    if (success)
                    {
                        return (true, literal, expr, scope, defaultScope);
                    }
                }
            }

            return (false, null, null, null, null);
        }

        private ClassicallyControlledTransformation() { }

        private class ClassicallyControlledSyntax : SyntaxTreeTransformation<ClassicallyControlledScope>
        {
            public ClassicallyControlledSyntax(ClassicallyControlledScope scope = null) : base(scope ?? new ClassicallyControlledScope()) { }

            public override QsCallable onFunction(QsCallable c) => c; // Prevent anything in functions from being considered
        }

        private class ClassicallyControlledScope : ScopeTransformation<NoExpressionTransformations>
        {
            public ClassicallyControlledScope(NoExpressionTransformations expr = null) : base(expr ?? new NoExpressionTransformations()) { }

            private TypeArgsResolution GetCombinedType(TypeArgsResolution outer, TypeArgsResolution inner)
            {
                var result = new List<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>();
                var outerDict = outer.ToDictionary(x => (x.Item1, x.Item2), x => x.Item3);

                return inner.Select(innerRes =>
                {
                    if (innerRes.Item3.Resolution is ResolvedTypeKind.TypeParameter typeParam &&
                        outerDict.ContainsKey((typeParam.Item.Origin, typeParam.Item.TypeName)))
                    {
                        var outerRes = outerDict[(typeParam.Item.Origin, typeParam.Item.TypeName)];
                        return Tuple.Create(innerRes.Item1, innerRes.Item2, outerRes);
                    }
                    else
                    {
                        return innerRes;
                    }
                }).ToImmutableArray();
            }

            private (bool, TypedExpression, TypedExpression) IsValidScope(QsScope scope)
            {
                // if the scope has exactly one statement in it and that statement is a call like expression statement
                if (scope != null && scope.Statements.Length == 1 &&
                    scope.Statements[0].Statement is QsStatementKind.QsExpressionStatement expr &&
                    expr.Item.ResolvedType.Resolution.IsUnitType && expr.Item.Expression is ExpressionKind.CallLikeExpression call)
                {
                    // We are dissolving the application of arguments here, so the call's type argument
                    // resolutions have to be moved to the 'identifier' sub expression.

                    var callTypeArguments = expr.Item.TypeArguments;
                    var idTypeArguments = call.Item1.TypeArguments;
                    var combinedTypeArguments = GetCombinedType(callTypeArguments, idTypeArguments);

                    var newExpr1 = call.Item1;
                    // ToDo: shouldn't rely on expr1 being identifier
                    if (combinedTypeArguments.Any() && newExpr1.Expression is ExpressionKind.Identifier id)
                    {
                        newExpr1 = new TypedExpression(
                            ExpressionKind.NewIdentifier(
                                id.Item1,
                                QsNullable<ImmutableArray<ResolvedType>>.NewValue(combinedTypeArguments
                                    .Select(arg => arg.Item3)
                                    .ToImmutableArray())),
                            combinedTypeArguments,
                            call.Item1.ResolvedType,
                            call.Item1.InferredInformation,
                            call.Item1.Range);
                    }

                    return (true, newExpr1, call.Item2);
                }

                return (false, null, null);
            }

            private TypedExpression CreateApplyIfCall(TypedExpression id, TypedExpression args, TypeArgsResolution typeRes) =>
                new TypedExpression
                (
                    ExpressionKind.NewCallLikeExpression(id, args),
                    typeRes,
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

            private static ResolvedType GetApplyIfResolvedType(BuiltIn builtIn, IEnumerable<OpProperty> props, ResolvedType argumentType)
            {
                var characteristics = new CallableInformation(
                    ResolvedCharacteristics.FromProperties(props),
                    InferredCallableInformation.NoInformation);

                return ResolvedType.New(ResolvedTypeKind.NewOperation(
                    Tuple.Create(argumentType, ResolvedType.New(ResolvedTypeKind.UnitType)),
                    characteristics));
            }

            private TypedExpression GetApplyIfExpression(QsResult result, TypedExpression conditionExpression, QsScope conditionScope, QsScope defaultScope)
            {
                var (isCondValid, condId, condArgs) = IsValidScope(conditionScope);
                var (isDefaultValid, defaultId, defaultArgs) = IsValidScope(defaultScope);

                BuiltIn controlOpInfo;
                TypedExpression controlArgs;
                ImmutableArray<ResolvedType> targetArgs;

                var props = ImmutableHashSet<OpProperty>.Empty;

                if (isCondValid)
                {
                    // Get characteristic properties from global id
                    if (condId.ResolvedType.Resolution is ResolvedTypeKind.Operation op)
                    {
                        props = op.Item2.Characteristics.GetProperties();
                    }

                    (bool adj, bool ctl) = (props.Contains(OpProperty.Adjointable), props.Contains(OpProperty.Controllable));

                    if (isDefaultValid)
                    {
                        if (adj && ctl)
                        {
                            controlOpInfo = BuiltIn.ApplyIfElseCA;
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

                        var (zeroOpArg, oneOpArg) = (result == QsResult.Zero)
                            ? (CreateValueTupleExpression(condId, condArgs), CreateValueTupleExpression(defaultId, defaultArgs))
                            : (CreateValueTupleExpression(defaultId, defaultArgs), CreateValueTupleExpression(condId, condArgs));

                        controlArgs = CreateValueTupleExpression(conditionExpression, zeroOpArg, oneOpArg);

                        targetArgs = ImmutableArray.Create(condArgs.ResolvedType, defaultArgs.ResolvedType);
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
                            CreateValueTupleExpression(condId, condArgs));

                        targetArgs = ImmutableArray.Create(condArgs.ResolvedType);
                    }
                    else
                    {
                        return null; // ToDo: Diagnostic message - default body exists, but is not valid
                    }

                }
                else
                {
                    return null; // ToDo: Diagnostic message - cond body not valid
                }

                // Build the surrounding apply-if call
                var controlOpId = CreateIdentifierExpression(
                    Identifier.NewGlobalCallable(new QsQualifiedName(controlOpInfo.Namespace, controlOpInfo.Name)),
                    targetArgs
                        .Zip(controlOpInfo.TypeParameters, (type, param) => Tuple.Create(new QsQualifiedName(controlOpInfo.Namespace, controlOpInfo.Name), param, type))
                        .ToImmutableArray(),
                    GetApplyIfResolvedType(controlOpInfo, props, controlArgs.ResolvedType));

                // Creates identity resolutions for the call expression
                var opTypeArgResolutions = targetArgs
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

                return CreateApplyIfCall(controlOpId, controlArgs, opTypeArgResolutions);
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

                    // Prevent _IsRecursiveIdentifier from propagating beyond the typed expression it is referring to
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
                    // In this context, that is a call back to the original callable from the newly generated operation
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
                    // Reroute a type parameter's origin to the newly generated operation
                    if (!_super._IsRecursiveIdentifier && _super._OldName.Equals(tp.Origin))
                    {
                        tp = new QsTypeParameter(_super._NewName, tp.TypeName, tp.Range);
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
            private ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> _CurrentHoistParams =
                ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty;
            private bool _ContainsHoistParamRef = false;

            private class CallableDetails
            {
                public QsCallable Callable;
                public QsSpecialization Adjoint;
                public QsSpecialization Controlled;
                public QsSpecialization ControlledAdjoint;
                public QsNullable<ImmutableArray<ResolvedType>> TypeParamTypes;

                public CallableDetails(QsCallable callable)
                {
                    Callable = callable;
                    Adjoint = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsAdjoint);
                    Controlled = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsControlled);
                    ControlledAdjoint = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsControlledAdjoint);
                    TypeParamTypes = callable.Signature.TypeParameters.Any(param => param.IsValidName)
                    ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(callable.Signature.TypeParameters
                        .Where(param => param.IsValidName)
                        .Select(param =>
                            ResolvedType.New(ResolvedTypeKind.NewTypeParameter(new QsTypeParameter(
                                callable.FullName,
                                ((QsLocalSymbol.ValidName)param).Item,
                                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                        ))))
                        .ToImmutableArray())
                    : QsNullable<ImmutableArray<ResolvedType>>.Null;
                }
            }

            private CallableDetails _CurrentCallable = null;
            private bool _InBody = false;
            private bool _InAdjoint = false;
            private bool _InControlled = false;

            private bool _InWithinBlock = false;

            public static QsCompilation Apply(QsCompilation compilation)
            {
                var filter = new HoistSyntax(new HoistTransformation());

                return new QsCompilation(compilation.Namespaces.Select(ns => filter.Transform(ns)).ToImmutableArray(), compilation.EntryPoints);
            }

            private (ResolvedSignature, IEnumerable<QsSpecialization>) MakeSpecializations(QsQualifiedName callableName, ResolvedType argsType, SpecializationImplementation bodyImplementation)
            {
                QsSpecialization MakeSpec(QsSpecializationKind kind, ResolvedSignature signature, SpecializationImplementation impl) =>
                    new QsSpecialization(
                        kind,
                        callableName,
                        ImmutableArray<QsDeclarationAttribute>.Empty,
                        _CurrentCallable.Callable.SourceFile,
                        QsNullable<QsLocation>.Null,
                        QsNullable<ImmutableArray<ResolvedType>>.Null,
                        signature,
                        impl,
                        ImmutableArray<string>.Empty,
                        QsComments.Empty);

                var adj = _CurrentCallable.Adjoint;
                var ctl = _CurrentCallable.Controlled;
                var ctlAdj = _CurrentCallable.ControlledAdjoint;

                bool addAdjoint = false;
                bool addControlled = false;

                if (_InWithinBlock) addAdjoint = true;

                if (_InBody)
                {
                    if (adj != null && adj.Implementation is SpecializationImplementation.Generated adjGen) addAdjoint = addAdjoint || adjGen.Item.IsInvert;
                    if (ctl != null && ctl.Implementation is SpecializationImplementation.Generated ctlGen) addControlled = ctlGen.Item.IsDistribute;
                    if (ctlAdj != null && ctlAdj.Implementation is SpecializationImplementation.Generated ctlAdjGen)
                    {
                        addAdjoint = addAdjoint || ctlAdjGen.Item.IsInvert && ctl.Implementation.IsGenerated;
                        addControlled = addControlled || ctlAdjGen.Item.IsDistribute && adj.Implementation.IsGenerated;
                    }
                }
                else if (ctlAdj != null && ctlAdj.Implementation is SpecializationImplementation.Generated gen)
                {
                    addControlled = _InAdjoint && gen.Item.IsDistribute;
                    addAdjoint = _InControlled && gen.Item.IsInvert;
                }

                var props = new List<OpProperty>();
                if (addAdjoint) props.Add(OpProperty.Adjointable);
                if (addControlled) props.Add(OpProperty.Controllable);
                var newSig = new ResolvedSignature(
                    _CurrentCallable.Callable.Signature.TypeParameters,
                    argsType,
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    new CallableInformation(ResolvedCharacteristics.FromProperties(props), InferredCallableInformation.NoInformation));

                var controlledSig = new ResolvedSignature(
                    newSig.TypeParameters,
                    ResolvedType.New(ResolvedTypeKind.NewTupleType(ImmutableArray.Create(
                        ResolvedType.New(ResolvedTypeKind.NewArrayType(ResolvedType.New(ResolvedTypeKind.Qubit))),
                        newSig.ArgumentType))),
                    newSig.ReturnType,
                    newSig.Information);

                var specializations = new List<QsSpecialization>() { MakeSpec(QsSpecializationKind.QsBody, newSig, bodyImplementation) };

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

            private (QsCallable, ResolvedType) GenerateOperation(QsScope contents)
            {
                var newName = new QsQualifiedName(
                    _CurrentCallable.Callable.FullName.Namespace,
                    NonNullable<string>.New("_" + Guid.NewGuid().ToString("N") + "_" + _CurrentCallable.Callable.FullName.Name.Value));

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
                if (knownVariables.Length == 1)
                {
                    paramTypes = knownVariables.First().Type;
                }
                else if (knownVariables.Length > 1)
                {
                    paramTypes = ResolvedType.New(ResolvedTypeKind.NewTupleType(knownVariables
                        .Select(var => var.Type)
                        .ToImmutableArray()));
                }

                var (signature, specializations) = MakeSpecializations(newName, paramTypes, SpecializationImplementation.NewProvided(parameters, contents));

                var controlCallable = new QsCallable(
                    QsCallableKind.Operation,
                    newName,
                    ImmutableArray<QsDeclarationAttribute>.Empty,
                    _CurrentCallable.Callable.SourceFile,
                    QsNullable<QsLocation>.Null,
                    signature,
                    parameters,
                    specializations.ToImmutableArray(),
                    ImmutableArray<string>.Empty,
                    QsComments.Empty);

                var updatedCallable = UpdateGeneratedOpTransformation.Apply(controlCallable, knownVariables, _CurrentCallable.Callable.FullName, newName);

                return (updatedCallable, signature.ArgumentType);
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
                    _super._CurrentCallable = new CallableDetails(c);
                    return base.onCallableImplementation(c);
                }

                public override QsSpecialization onBodySpecialization(QsSpecialization spec)
                {
                    _super._InBody = true;
                    var rtrn = base.onBodySpecialization(spec);
                    _super._InBody = false;
                    return rtrn;
                }

                public override QsSpecialization onAdjointSpecialization(QsSpecialization spec)
                {
                    _super._InAdjoint = true;
                    var rtrn = base.onAdjointSpecialization(spec);
                    _super._InAdjoint = false;
                    return rtrn;
                }

                public override QsSpecialization onControlledSpecialization(QsSpecialization spec)
                {
                    _super._InControlled = true;
                    var rtrn = base.onControlledSpecialization(spec);
                    _super._InControlled = false;
                    return rtrn;
                }

                public override QsCallable onFunction(QsCallable c) => c; // Prevent anything in functions from being hoisted

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

                private (QsCallable, QsStatement) HoistIfContents(QsScope contents)
                {
                    var (targetOp, originalArgumentType) = _super.GenerateOperation(contents);
                    var targetOpType = ResolvedType.New(ResolvedTypeKind.NewOperation(
                        Tuple.Create(
                            originalArgumentType,
                            ResolvedType.New(ResolvedTypeKind.UnitType)),
                        targetOp.Signature.Information));

                    var targetTypeArgTypes = _super._CurrentCallable.TypeParamTypes;
                    var targetOpId = new TypedExpression
                    (
                        ExpressionKind.NewIdentifier(Identifier.NewGlobalCallable(targetOp.FullName), targetTypeArgTypes),
                        targetTypeArgTypes.IsNull
                            ? TypeArgsResolution.Empty
                            : targetTypeArgTypes.Item
                                .Select(type => Tuple.Create(targetOp.FullName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
                                .ToImmutableArray(),
                        targetOpType,
                        new InferredExpressionInformation(false, false),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                    );

                    var knownSymbols = contents.KnownSymbols.Variables;

                    TypedExpression targetArgs = null;
                    if (knownSymbols.Any())
                    {
                        targetArgs = CreateValueTupleExpression(knownSymbols.Select(var => CreateIdentifierExpression(
                            Identifier.NewLocalVariable(var.VariableName),
                            // ToDo: may need to be more careful here with the type argument mapping on the identifiers
                            TypeArgsResolution.Empty,
                            var.Type))
                            .ToArray());
                    }
                    else
                    {
                        targetArgs = new TypedExpression
                        (
                            ExpressionKind.UnitValue,
                            TypeArgsResolution.Empty,
                            ResolvedType.New(ResolvedTypeKind.UnitType),
                            new InferredExpressionInformation(false, false),
                            QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                        );
                    }

                    var call = new TypedExpression
                    (
                        ExpressionKind.NewCallLikeExpression(targetOpId, targetArgs),
                        targetTypeArgTypes.IsNull
                            ? TypeArgsResolution.Empty
                            : targetTypeArgTypes.Item
                                .Select(type => Tuple.Create(_super._CurrentCallable.Callable.FullName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
                                .ToImmutableArray(),
                        ResolvedType.New(ResolvedTypeKind.UnitType),
                        new InferredExpressionInformation(false, true),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
                    );

                    return (targetOp, new QsStatement(
                        QsStatementKind.NewQsExpressionStatement(call),
                        LocalDeclarations.Empty,
                        QsNullable<QsLocation>.Null,
                        QsComments.Empty));
                }

                private bool IsScopeSingleCall(QsScope contents)
                {
                    if (contents.Statements.Length != 1) return false;
                    
                    return contents.Statements[0].Statement is QsStatementKind.QsExpressionStatement expr &&
                            expr.Item.Expression is ExpressionKind.CallLikeExpression;
                }

                public override QsStatementKind onConjugation(QsConjugation stm)
                {
                    var superInWithinBlock = _super._InWithinBlock;
                    _super._InWithinBlock = true;
                    var (_, outer) = this.onPositionedBlock(null, stm.OuterTransformation); // ToDo: null is probably bad here
                    _super._InWithinBlock = superInWithinBlock;

                    var (_, inner) = this.onPositionedBlock(null, stm.InnerTransformation); // ToDo: null is probably bad here

                    return QsStatementKind.NewQsConjugation(new QsConjugation(outer, inner));
                }

                public override QsStatementKind onReturnStatement(TypedExpression ex)
                {
                    _super._IsValidScope = false;
                    return base.onReturnStatement(ex);
                }

                public override QsStatementKind onValueUpdate(QsValueUpdate stm)
                {
                    // If lhs contains an identifier found in the scope's known variables (variables from the super-scope), the scope is not valid
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

                    var isHoistValid = true;

                    var newConditionBlocks = new List<Tuple<TypedExpression, QsPositionedBlock>>();
                    var generatedOperations = new List<QsCallable>();
                    foreach (var condBlock in stm.ConditionalBlocks)
                    {
                        _super._IsValidScope = true;
                        _super._CurrentHoistParams = condBlock.Item2.Body.KnownSymbols.IsEmpty
                        ? ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty
                        : condBlock.Item2.Body.KnownSymbols.Variables;

                        var (expr, block) = this.onPositionedBlock(condBlock.Item1, condBlock.Item2);

                        // ToDo: Reduce the number of unnecessary generated operations by generalizing
                        // the condition logic for the conversion and using that condition here
                        //var (isExprCond, _, _) = IsConditionedOnResultLiteralExpression(expr.Value); // ToDo: .Value may not be needed in the future

                        if (block.Body.Statements.Length > 0 /*&& isExprCond*/ && _super._IsValidScope && !IsScopeSingleCall(block.Body)) // if sub-scope is valid, hoist content
                        {
                            // Hoist the scope to its own operation
                            var (callable, call) = HoistIfContents(block.Body);
                            block = new QsPositionedBlock(
                                new QsScope(ImmutableArray.Create(call), block.Body.KnownSymbols),
                                block.Location,
                                block.Comments);
                            newConditionBlocks.Add(Tuple.Create(expr.Value,block));
                            generatedOperations.Add(callable);
                        }
                        else
                        {
                            isHoistValid = false;
                            break;
                        }
                    }

                    var newDefault = QsNullable<QsPositionedBlock>.Null;
                    if (isHoistValid && stm.Default.IsValue)
                    {
                        _super._IsValidScope = true;
                        _super._CurrentHoistParams = stm.Default.Item.Body.KnownSymbols.IsEmpty
                            ? ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty
                            : stm.Default.Item.Body.KnownSymbols.Variables;

                        var (_, block) = this.onPositionedBlock(null, stm.Default.Item); // ToDo: null is probably bad here
                        if (block.Body.Statements.Length > 0 && _super._IsValidScope && !IsScopeSingleCall(block.Body)) // if sub-scope is valid, hoist content
                        {
                            // Hoist the scope to its own operation
                            var (callable, call) = HoistIfContents(block.Body);
                            block = new QsPositionedBlock(
                                new QsScope(ImmutableArray.Create(call), block.Body.KnownSymbols),
                                block.Location,
                                block.Comments);
                            newDefault = QsNullable<QsPositionedBlock>.NewValue(block);
                            generatedOperations.Add(callable);
                        }
                        else
                        {
                            isHoistValid = false;
                        }
                    }

                    if (isHoistValid)
                    {
                        _super._ControlOperations.AddRange(generatedOperations);
                    }

                    _super._CurrentHoistParams = contextHoistParams;
                    _super._IsValidScope = contextValidScope;

                    return isHoistValid
                        ? QsStatementKind.NewQsConditionalStatement(
                          new QsConditionalStatement(newConditionBlocks.ToImmutableArray(), newDefault))
                        : QsStatementKind.NewQsConditionalStatement(
                          new QsConditionalStatement(stm.ConditionalBlocks, stm.Default));
                }

                public override QsStatementKind Transform(QsStatementKind kind)
                {
                    _super._ContainsHoistParamRef = false; // Every statement kind starts off false
                    return base.Transform(kind);
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
