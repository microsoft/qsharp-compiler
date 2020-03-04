using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;

namespace Transformations
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>;

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
    //internal static class HoistContent
    //{
    //    internal static QsCompilation Apply(QsCompilation compilation) => LiftContent.Apply(compilation);

        public class LiftContent : SyntaxTreeTransformation<LiftContent.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation)
            {
                var filter = new LiftContent();

                return new QsCompilation(compilation.Namespaces.Select(ns => filter.Namespaces.OnNamespace(ns)).ToImmutableArray(), compilation.EntryPoints);
            }

            public class CallableDetails
            {
                public QsCallable Callable;
                public QsSpecialization Adjoint;
                public QsSpecialization Controlled;
                public QsSpecialization ControlledAdjoint;
                public QsNullable<ImmutableArray<ResolvedType>> TypeArguments;

                public CallableDetails(QsCallable callable)
                {
                    Callable = callable;
                    Adjoint = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsAdjoint);
                    Controlled = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsControlled);
                    ControlledAdjoint = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsControlledAdjoint);
                    TypeArguments = callable.Signature.TypeParameters.Any(param => param.IsValidName)
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
                public List<QsCallable> GeneratedOperations = null;
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

                private (QsCallable, ResolvedType) GenerateOperation(QsScope contents)
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

                // ToDo: Doc comment
                public (QsCallable, QsStatement) HoistBody(QsScope body)
                {
                    var (targetOp, originalArgumentType) = GenerateOperation(body);
                    var targetOpType = ResolvedType.New(ResolvedTypeKind.NewOperation(
                        Tuple.Create(
                            originalArgumentType,
                            ResolvedType.New(ResolvedTypeKind.UnitType)),
                        targetOp.Signature.Information));

                    var targetTypeArgTypes = CurrentCallable.TypeArguments;
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
                                .Select(type => Tuple.Create(CurrentCallable.Callable.FullName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
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
            }

            public LiftContent() : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.StatementKinds = new StatementKindTransformation(this);
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            public class NamespaceTransformation : NamespaceTransformation<TransformationState>
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
                    // Generated operations list will be populated in the transform
                    SharedState.GeneratedOperations = new List<QsCallable>();
                    return base.OnNamespace(ns)
                        .WithElements(elems => elems.AddRange(SharedState.GeneratedOperations.Select(op => QsNamespaceElement.NewQsCallable(op))));
                }
            }

            protected class StatementKindTransformation : StatementKindTransformation<TransformationState>
            {
                public StatementKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                // ToDo: This logic should be externalized at some point to make the Hoisting more general
                //private bool IsScopeSingleCall(QsScope contents)
                //{
                //    if (contents.Statements.Length != 1) return false;
                //
                //    return contents.Statements[0].Statement is QsStatementKind.QsExpressionStatement expr
                //           && expr.Item.Expression is ExpressionKind.CallLikeExpression call
                //           && !TypedExpression.IsPartialApplication(expr.Item.Expression)
                //           && call.Item1.Expression is ExpressionKind.Identifier;
                //}

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

                public override QsStatementKind OnStatementKind(QsStatementKind kind)
                {
                    SharedState.ContainsHoistParamRef = false; // Every statement kind starts off false
                    return base.OnStatementKind(kind);
                }
            }

            public class ExpressionTransformation : ExpressionTransformation<TransformationState>
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

            public class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
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
        public class UpdateGeneratedOp : SyntaxTreeTransformation<UpdateGeneratedOp.TransformationState>
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

                    // Check if this is a recursive identifier
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
    //}
}
