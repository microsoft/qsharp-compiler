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
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;


namespace Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlled
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>;

    /// <summary>
    /// This transformation works in two passes.
    /// 1st Pass: Hoist the contents of conditional statements into separate operations, where possible.
    /// 2nd Pass: On the way down the tree, reshape conditional statements to replace Elif's and
    /// top level OR and AND conditions with equivalent nested if-else statements. One the way back up
    /// the tree, convert conditional statements into ApplyIf calls, where possible.
    /// This relies on anything having type parameters must be a global callable.
    /// </summary>
    public static class ReplaceClassicalControl
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            compilation = HoistTransformation.Apply(compilation);

            return ConvertConditions.Apply(compilation);
        }

        private class ConvertConditions : SyntaxTreeTransformation<ConvertConditions.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation)
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
                        var combinedTypeArguments = Utils.GetCombinedTypeResolution(callTypeArguments, idTypeArguments);

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

                #region Apply If

                private TypedExpression CreateApplyIfExpression(QsResult result, TypedExpression conditionExpression, QsScope conditionScope, QsScope defaultScope)
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

                            var (zeroOpArg, oneOpArg) = (result == QsResult.Zero)
                                ? (Utils.CreateValueTupleExpression(condId, condArgs), Utils.CreateValueTupleExpression(defaultId, defaultArgs))
                                : (Utils.CreateValueTupleExpression(defaultId, defaultArgs), Utils.CreateValueTupleExpression(condId, condArgs));

                            controlArgs = Utils.CreateValueTupleExpression(conditionExpression, zeroOpArg, oneOpArg);

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

                            controlArgs = Utils.CreateValueTupleExpression(
                                conditionExpression,
                                Utils.CreateValueTupleExpression(condId, condArgs));

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
                    var controlOpId = Utils.CreateIdentifierExpression(
                        Identifier.NewGlobalCallable(new QsQualifiedName(controlOpInfo.Namespace, controlOpInfo.Name)),
                        targetArgs
                            .Zip(controlOpInfo.TypeParameters, (type, param) => Tuple.Create(new QsQualifiedName(controlOpInfo.Namespace, controlOpInfo.Name), param, type))
                            .ToImmutableArray(),
                        Utils.GetOperationType(props, controlArgs.ResolvedType));

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

                    return Utils.CreateCallLikeExpression(controlOpId, controlArgs, opTypeArgResolutions);
                }

                private QsStatement CreateApplyIfStatement(QsStatement statement, QsResult result, TypedExpression conditionExpression, QsScope conditionScope, QsScope defaultScope)
                {
                    var controlCall = CreateApplyIfExpression(result, conditionExpression, conditionScope, defaultScope);

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

                #endregion

                #region Condition Reshaping Logic

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

                #endregion

                #region Condition Checking Logic

                private (bool, TypedExpression, QsScope, QsScope) IsConditionWithSingleBlock(QsStatement statement)
                {
                    if (statement.Statement is QsStatementKind.QsConditionalStatement cond && cond.Item.ConditionalBlocks.Length == 1)
                    {
                        return (true, cond.Item.ConditionalBlocks[0].Item1, cond.Item.ConditionalBlocks[0].Item2.Body, cond.Item.Default.ValueOr(null)?.Body);
                    }

                    return (false, null, null, null);
                }

                private (bool, QsResult, TypedExpression) IsConditionedOnResultLiteralExpression(TypedExpression expression)
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

                            var (isCondition, cond, conditionScope, defaultScope) = IsConditionWithSingleBlock(stm);

                            if (isCondition)
                            {
                                /*ToDo: this could be a separate function.*/
                                var (isCompareLiteral, literal, nonLiteral) = IsConditionedOnResultLiteralExpression(cond);
                                if (isCompareLiteral)
                                {
                                    statements.Add(CreateApplyIfStatement(stm, literal, nonLiteral, conditionScope, defaultScope));
                                }
                                else
                                {
                                    statements.Add(stm);
                                }
                                /**/
                            }
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


    /// <summary>
    /// Transformation handling the first pass task of hoisting of the contents of conditional statements.
    /// If blocks are first validated to see if they can safely be hoisted into their own operation.
    /// Validation requirements are that there are no return statements and that there are no set statements
    /// on mutables declared outside the block. Setting mutables declared inside the block is valid.
    /// If the block is valid, and there is more than one statement in the block, a new operation with the
    /// block's contents is generated, having all the same type parameters as the calling context
    /// and all known variables at the start of the block become parameters to the new operation.
    /// The contents of the conditional block are then replaced with a call to the new operation with all
    /// the type parameters and known variables being forwarded to the new operation as arguments.
    /// </summary>
    internal static class HoistTransformation // this class can be made public once its functionality is no longer tied to the classically-controlled transformation 
    {
        internal static QsCompilation Apply(QsCompilation compilation) => HoistContents.Apply(compilation);

        private class HoistContents : SyntaxTreeTransformation<HoistContents.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation)
            {
                var filter = new HoistContents();

                return new QsCompilation(compilation.Namespaces.Select(ns => filter.Namespaces.OnNamespace(ns)).ToImmutableArray(), compilation.EntryPoints);
            }

            public class CallableDetails
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

            public class TransformationState
            {
                public bool IsValidScope = true;
                public List<QsCallable> ControlOperations = null;
                public ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> CurrentHoistParams =
                    ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty;
                public bool ContainsHoistParamRef = false;

                public CallableDetails CurrentCallable = null;
                public bool InBody = false;
                public bool InAdjoint = false;
                public bool InControlled = false;
                public bool InWithinBlock = false;

                private (ResolvedSignature, IEnumerable<QsSpecialization>) MakeSpecializations(
                    QsQualifiedName callableName, ResolvedType argsType, SpecializationImplementation bodyImplementation)
                {
                    QsSpecialization MakeSpec(QsSpecializationKind kind, ResolvedSignature signature, SpecializationImplementation impl) =>
                        new QsSpecialization(
                            kind,
                            callableName,
                            ImmutableArray<QsDeclarationAttribute>.Empty,
                            CurrentCallable.Callable.SourceFile,
                            QsNullable<QsLocation>.Null,
                            QsNullable<ImmutableArray<ResolvedType>>.Null,
                            signature,
                            impl,
                            ImmutableArray<string>.Empty,
                            QsComments.Empty);

                    var adj = CurrentCallable.Adjoint;
                    var ctl = CurrentCallable.Controlled;
                    var ctlAdj = CurrentCallable.ControlledAdjoint;

                    bool addAdjoint = false;
                    bool addControlled = false;

                    if (InWithinBlock)
                    {
                        addAdjoint = true;
                        addControlled = false;
                    }
                    else if (InBody)
                    {
                        if (adj != null && adj.Implementation is SpecializationImplementation.Generated adjGen) addAdjoint = adjGen.Item.IsInvert;
                        if (ctl != null && ctl.Implementation is SpecializationImplementation.Generated ctlGen) addControlled = ctlGen.Item.IsDistribute;
                        if (ctlAdj != null && ctlAdj.Implementation is SpecializationImplementation.Generated ctlAdjGen)
                        {
                            addAdjoint = addAdjoint || ctlAdjGen.Item.IsInvert && ctl.Implementation.IsGenerated;
                            addControlled = addControlled || ctlAdjGen.Item.IsDistribute && adj.Implementation.IsGenerated;
                        }
                    }
                    else if (ctlAdj != null && ctlAdj.Implementation is SpecializationImplementation.Generated gen)
                    {
                        addControlled = InAdjoint && gen.Item.IsDistribute;
                        addAdjoint = InControlled && gen.Item.IsInvert;
                    }

                    var props = new List<OpProperty>();
                    if (addAdjoint) props.Add(OpProperty.Adjointable);
                    if (addControlled) props.Add(OpProperty.Controllable);
                    var newSig = new ResolvedSignature(
                        CurrentCallable.Callable.Signature.TypeParameters,
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

                public (QsCallable, ResolvedType) GenerateOperation(QsScope contents)
                {
                    var newName = UniqueVariableNames.PrependGuid(CurrentCallable.Callable.FullName);

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
                        CurrentCallable.Callable.SourceFile,
                        QsNullable<QsLocation>.Null,
                        signature,
                        parameters,
                        specializations.ToImmutableArray(),
                        ImmutableArray<string>.Empty,
                        QsComments.Empty);

                    var updatedCallable = UpdateGeneratedOp.Apply(controlCallable, knownVariables, CurrentCallable.Callable.FullName, newName);

                    return (updatedCallable, signature.ArgumentType);
                }
            }

            private HoistContents() : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.StatementKinds = new StatementKindTransformation(this);
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override QsCallable OnCallableDeclaration(QsCallable c)
                {
                    SharedState.CurrentCallable = new CallableDetails(c);
                    return base.OnCallableDeclaration(c);
                }

                public override QsSpecialization OnBodySpecialization(QsSpecialization spec)
                {
                    SharedState.InBody = true;
                    var rtrn = base.OnBodySpecialization(spec);
                    SharedState.InBody = false;
                    return rtrn;
                }

                public override QsSpecialization OnAdjointSpecialization(QsSpecialization spec)
                {
                    SharedState.InAdjoint = true;
                    var rtrn = base.OnAdjointSpecialization(spec);
                    SharedState.InAdjoint = false;
                    return rtrn;
                }

                public override QsSpecialization OnControlledSpecialization(QsSpecialization spec)
                {
                    SharedState.InControlled = true;
                    var rtrn = base.OnControlledSpecialization(spec);
                    SharedState.InControlled = false;
                    return rtrn;
                }

                public override QsCallable OnFunction(QsCallable c) => c; // Prevent anything in functions from being hoisted

                public override QsNamespace OnNamespace(QsNamespace ns)
                {
                    // Control operations list will be populated in the transform
                    SharedState.ControlOperations = new List<QsCallable>();
                    return base.OnNamespace(ns)
                        .WithElements(elems => elems.AddRange(SharedState.ControlOperations.Select(op => QsNamespaceElement.NewQsCallable(op))));
                }
            }

            private class StatementKindTransformation : StatementKindTransformation<TransformationState>
            {
                public StatementKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                private (QsCallable, QsStatement) HoistBody(QsScope body)
                {
                    var (targetOp, originalArgumentType) = SharedState.GenerateOperation(body);
                    var targetOpType = ResolvedType.New(ResolvedTypeKind.NewOperation(
                        Tuple.Create(
                            originalArgumentType,
                            ResolvedType.New(ResolvedTypeKind.UnitType)),
                        targetOp.Signature.Information));

                    var targetTypeArgTypes = SharedState.CurrentCallable.TypeParamTypes;
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

                    var knownSymbols = body.KnownSymbols.Variables;

                    TypedExpression targetArgs = null;
                    if (knownSymbols.Any())
                    {
                        targetArgs = Utils.CreateValueTupleExpression(knownSymbols.Select(var => Utils.CreateIdentifierExpression(
                            Identifier.NewLocalVariable(var.VariableName),
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
                                .Select(type => Tuple.Create(SharedState.CurrentCallable.Callable.FullName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
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

                // ToDo: This logic should be externalized at some point to make the Hoisting more general
                private bool IsScopeSingleCall(QsScope contents)
                {
                    if (contents.Statements.Length != 1) return false;

                    return contents.Statements[0].Statement is QsStatementKind.QsExpressionStatement expr
                           && expr.Item.Expression is ExpressionKind.CallLikeExpression call
                           && !TypedExpression.IsPartialApplication(expr.Item.Expression)
                           && call.Item1.Expression is ExpressionKind.Identifier;
                }

                public override QsStatementKind OnConjugation(QsConjugation stm)
                {
                    var superInWithinBlock = SharedState.InWithinBlock;
                    SharedState.InWithinBlock = true;
                    var (_, outer) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, stm.OuterTransformation);
                    SharedState.InWithinBlock = superInWithinBlock;

                    var (_, inner) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, stm.InnerTransformation);

                    return QsStatementKind.NewQsConjugation(new QsConjugation(outer, inner));
                }

                public override QsStatementKind OnReturnStatement(TypedExpression ex)
                {
                    SharedState.IsValidScope = false;
                    return base.OnReturnStatement(ex);
                }

                public override QsStatementKind OnValueUpdate(QsValueUpdate stm)
                {
                    // If lhs contains an identifier found in the scope's known variables (variables from the super-scope), the scope is not valid
                    var lhs = this.Expressions.OnTypedExpression(stm.Lhs);

                    if (SharedState.ContainsHoistParamRef)
                    {
                        SharedState.IsValidScope = false;
                    }

                    var rhs = this.Expressions.OnTypedExpression(stm.Rhs);
                    return QsStatementKind.NewQsValueUpdate(new QsValueUpdate(lhs, rhs));
                }

                public override QsStatementKind OnConditionalStatement(QsConditionalStatement stm)
                {
                    var contextValidScope = SharedState.IsValidScope;
                    var contextHoistParams = SharedState.CurrentHoistParams;

                    var isHoistValid = true;

                    var newConditionBlocks = new List<Tuple<TypedExpression, QsPositionedBlock>>();
                    var generatedOperations = new List<QsCallable>();
                    foreach (var condBlock in stm.ConditionalBlocks)
                    {
                        SharedState.IsValidScope = true;
                        SharedState.CurrentHoistParams = condBlock.Item2.Body.KnownSymbols.IsEmpty
                        ? ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty
                        : condBlock.Item2.Body.KnownSymbols.Variables;

                        var (expr, block) = this.OnPositionedBlock(QsNullable<TypedExpression>.NewValue(condBlock.Item1), condBlock.Item2);

                        // ToDo: Reduce the number of unnecessary generated operations by generalizing
                        // the condition logic for the conversion and using that condition here
                        //var (isExprCond, _, _) = IsConditionedOnResultLiteralExpression(expr.Item);

                        if (block.Body.Statements.Length > 0 /*&& isExprCond*/ && SharedState.IsValidScope && !IsScopeSingleCall(block.Body)) // if sub-scope is valid, hoist content
                        {
                            // Hoist the scope to its own operation
                            var (callable, call) = HoistBody(block.Body);
                            block = new QsPositionedBlock(
                                new QsScope(ImmutableArray.Create(call), block.Body.KnownSymbols),
                                block.Location,
                                block.Comments);
                            newConditionBlocks.Add(Tuple.Create(expr.Item, block));
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
                        SharedState.IsValidScope = true;
                        SharedState.CurrentHoistParams = stm.Default.Item.Body.KnownSymbols.IsEmpty
                            ? ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty
                            : stm.Default.Item.Body.KnownSymbols.Variables;

                        var (_, block) = this.OnPositionedBlock(QsNullable<TypedExpression>.Null, stm.Default.Item);
                        if (block.Body.Statements.Length > 0 && SharedState.IsValidScope && !IsScopeSingleCall(block.Body)) // if sub-scope is valid, hoist content
                        {
                            // Hoist the scope to its own operation
                            var (callable, call) = HoistBody(block.Body);
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
                        SharedState.ControlOperations.AddRange(generatedOperations);
                    }

                    SharedState.CurrentHoistParams = contextHoistParams;
                    SharedState.IsValidScope = contextValidScope;

                    return isHoistValid
                        ? QsStatementKind.NewQsConditionalStatement(
                          new QsConditionalStatement(newConditionBlocks.ToImmutableArray(), newDefault))
                        : QsStatementKind.NewQsConditionalStatement(
                          new QsConditionalStatement(stm.ConditionalBlocks, stm.Default));
                }

                public override QsStatementKind OnStatementKind(QsStatementKind kind)
                {
                    SharedState.ContainsHoistParamRef = false; // Every statement kind starts off false
                    return base.OnStatementKind(kind);
                }
            }

            private class ExpressionTransformation : ExpressionTransformation<TransformationState>
            {
                public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override TypedExpression OnTypedExpression(TypedExpression ex)
                {
                    var contextContainsHoistParamRef = SharedState.ContainsHoistParamRef;
                    SharedState.ContainsHoistParamRef = false;
                    var rtrn = base.OnTypedExpression(ex);

                    // If the sub context contains a reference, then the super context contains a reference,
                    // otherwise return the super context to its original value
                    if (!SharedState.ContainsHoistParamRef)
                    {
                        SharedState.ContainsHoistParamRef = contextContainsHoistParamRef;
                    }

                    return rtrn;
                }
            }

            private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    if (sym is Identifier.LocalVariable local &&
                    SharedState.CurrentHoistParams.Any(param => param.VariableName.Equals(local.Item)))
                    {
                        SharedState.ContainsHoistParamRef = true;
                    }
                    return base.OnIdentifier(sym, tArgs);
                }
            }
        }

        /// <summary>
        /// Transformation that updates the contents of newly generated operations by:
        /// 1. Rerouting the origins of type parameter references to the new operation
        /// 2. Changes the IsMutable info on variable that used to be mutable, but are now immutable params to the operation
        /// </summary>
        private class UpdateGeneratedOp : SyntaxTreeTransformation<UpdateGeneratedOp.TransformationState>
        {
            public static QsCallable Apply(QsCallable qsCallable, ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> parameters, QsQualifiedName oldName, QsQualifiedName newName)
            {
                var filter = new UpdateGeneratedOp(parameters, oldName, newName);

                return filter.Namespaces.OnCallableDeclaration(qsCallable);
            }

            public class TransformationState
            {
                public bool IsRecursiveIdentifier = false;
                public readonly ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> Parameters;
                public readonly QsQualifiedName OldName;
                public readonly QsQualifiedName NewName;

                public TransformationState(ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> parameters, QsQualifiedName oldName, QsQualifiedName newName)
                {
                    Parameters = parameters;
                    OldName = oldName;
                    NewName = newName;
                }
            }

            private UpdateGeneratedOp(ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> parameters, QsQualifiedName oldName, QsQualifiedName newName)
            : base(new TransformationState(parameters, oldName, newName))
            {
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation(this);
            }

            private class ExpressionTransformation : ExpressionTransformation<TransformationState>
            {
                public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> OnTypeParamResolutions(ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> typeParams)
                {
                    // Prevent keys from having their names updated
                    return typeParams.ToImmutableDictionary(kvp => kvp.Key, kvp => this.Types.OnType(kvp.Value));
                }

                public override TypedExpression OnTypedExpression(TypedExpression ex)
                {
                    // Checks if expression is mutable identifier that is in parameter list
                    if (ex.InferredInformation.IsMutable &&
                        ex.Expression is ExpressionKind.Identifier id &&
                        id.Item1 is Identifier.LocalVariable variable &&
                        SharedState.Parameters.Any(x => x.VariableName.Equals(variable)))
                    {
                        // Set the mutability to false
                        ex = new TypedExpression(
                            ex.Expression,
                            ex.TypeArguments,
                            ex.ResolvedType,
                            new InferredExpressionInformation(false, ex.InferredInformation.HasLocalQuantumDependency),
                            ex.Range);
                    }

                    // Prevent IsRecursiveIdentifier from propagating beyond the typed expression it is referring to
                    var isRecursiveIdentifier = SharedState.IsRecursiveIdentifier;
                    var rtrn = base.OnTypedExpression(ex);
                    SharedState.IsRecursiveIdentifier = isRecursiveIdentifier;
                    return rtrn;
                }
            }

            private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    var rtrn = base.OnIdentifier(sym, tArgs);

                    // Then check if this is a recursive identifier
                    // In this context, that is a call back to the original callable from the newly generated operation
                    if (sym is Identifier.GlobalCallable callable && SharedState.OldName.Equals(callable.Item))
                    {
                        // Setting this flag will prevent the rerouting logic from processing the resolved type of the recursive identifier expression.
                        // This is necessary because we don't want any type parameters from the original callable from being rerouted to the new generated
                        // operation's type parameters in the definition of the identifier.
                        SharedState.IsRecursiveIdentifier = true;
                    }
                    return rtrn;
                }
            }

            private class TypeTransformation : TypeTransformation<TransformationState>
            {
                public TypeTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
                {
                    // Reroute a type parameter's origin to the newly generated operation
                    if (!SharedState.IsRecursiveIdentifier && SharedState.OldName.Equals(tp.Origin))
                    {
                        tp = new QsTypeParameter(SharedState.NewName, tp.TypeName, tp.Range);
                    }

                    return base.OnTypeParameter(tp);
                }
            }
        }
    }
}
