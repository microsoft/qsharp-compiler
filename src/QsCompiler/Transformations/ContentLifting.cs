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


namespace Microsoft.Quantum.QsCompiler.Transformations.ContentLifting
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>;

    /// <summary>
    /// Transformation handling the task of lifting the contents of code blocks into generated operations.
    /// The transformation provides validation to see if any given block can safely be lifted into its own operation.
    /// Validation requirements are that there are no return statements and that there are no set statements
    /// on mutables declared outside the block. Setting mutables declared inside the block is valid.
    /// A block can be checked by setting the SharedState.IsValidScope to true before traversing the scope,
    /// then checking the SharedState.IsValidScope after traversal. Blocks should be validated before calling
    /// the SharedState.LiftBody function, which will generate a new operation with the block's contents,
    /// having all the same type parameters as the calling context and all known variables at the start of
    /// the block become parameters to the new operation. A call to the new operation is also returned with
    /// all the type parameters and known variables being forwarded to the new operation as arguments.
    /// </summary>
    public class LiftContent : SyntaxTreeTransformation<LiftContent.TransformationState>
    {
        internal class CallableDetails
        {
            internal readonly QsCallable Callable;
            internal readonly QsSpecialization Adjoint;
            internal readonly QsSpecialization Controlled;
            internal readonly QsSpecialization ControlledAdjoint;
            internal readonly QsNullable<ImmutableArray<ResolvedType>> TypeParameters;

            internal CallableDetails(QsCallable callable)
            {
                Callable = callable;
                // ToDo: this may need to be adapted once we support type specializations
                Adjoint = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsAdjoint);
                Controlled = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsControlled);
                ControlledAdjoint = callable.Specializations.FirstOrDefault(spec => spec.Kind == QsSpecializationKind.QsControlledAdjoint);
                // ToDo: this may need to be per-specialization
                TypeParameters = callable.Signature.TypeParameters.Any(param => param.IsValidName)
                ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(callable.Signature.TypeParameters
                    .Where(param => param.IsValidName)
                    .Select(param =>
                        ResolvedType.New(ResolvedTypeKind.NewTypeParameter(new QsTypeParameter(
                            callable.FullName,
                            ((QsLocalSymbol.ValidName)param).Item,
                            QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null))))
                    .ToImmutableArray())
                : QsNullable<ImmutableArray<ResolvedType>>.Null;
            }
        }

        public class TransformationState
        {
            // ToDo: It should be possible to make these three properties private, 
            // if we absorb the corresponding logic into LiftBody. 
            public bool IsValidScope = true;
            internal bool ContainsParamRef = false;
            internal ImmutableArray<LocalVariableDeclaration<NonNullable<string>>> GeneratedOpParams =
                ImmutableArray<LocalVariableDeclaration<NonNullable<string>>>.Empty;

            internal CallableDetails CurrentCallable = null;

            protected internal bool InBody = false;
            protected internal bool InAdjoint = false;
            protected internal bool InControlled = false;
            protected internal bool InControlledAdjoint = false;
            protected internal bool InWithinBlock = false;

            public List<QsCallable> GeneratedOperations = null;

            private (ResolvedSignature, IEnumerable<QsSpecialization>) MakeSpecializations(
                QsQualifiedName callableName, ResolvedType paramsType, SpecializationImplementation bodyImplementation)
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
                bool isSelfAdjoint = false;

                if (InWithinBlock)
                {
                    addAdjoint = true;
                    addControlled = false;
                }
                else if (InBody)
                {
                    if (adj != null && adj.Implementation is SpecializationImplementation.Generated adjGen)
                    {
                        addAdjoint = adjGen.Item.IsInvert;
                        isSelfAdjoint = adjGen.Item.IsSelfInverse;
                    }
                    if (ctl != null && ctl.Implementation is SpecializationImplementation.Generated ctlGen) addControlled = ctlGen.Item.IsDistribute;
                    if (ctlAdj != null && ctlAdj.Implementation is SpecializationImplementation.Generated ctlAdjGen)
                    {
                        addAdjoint = addAdjoint || ctlAdjGen.Item.IsInvert && ctl.Implementation.IsGenerated;
                        addControlled = addControlled || ctlAdjGen.Item.IsDistribute && adj.Implementation.IsGenerated;
                        isSelfAdjoint = isSelfAdjoint || ctlAdjGen.Item.IsSelfInverse;
                    }
                }
                else if (ctlAdj != null && ctlAdj.Implementation is SpecializationImplementation.Generated gen)
                {
                    addControlled = InAdjoint && gen.Item.IsDistribute;
                    addAdjoint = InControlled && gen.Item.IsInvert;
                    isSelfAdjoint = gen.Item.IsSelfInverse;
                }

                var props = new List<OpProperty>();
                if (addAdjoint) props.Add(OpProperty.Adjointable);
                if (addControlled) props.Add(OpProperty.Controllable);
                var newSig = new ResolvedSignature(
                    CurrentCallable.Callable.Signature.TypeParameters,
                    paramsType,
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    new CallableInformation(ResolvedCharacteristics.FromProperties(props), new InferredCallableInformation(isSelfAdjoint, false)));

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

                var knownVariables = contents.KnownSymbols.Variables;

                var parameters = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.NewQsTuple(knownVariables
                    .Select(var => QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.NewQsTupleItem(new LocalVariableDeclaration<QsLocalSymbol>(
                        QsLocalSymbol.NewValidName(var.VariableName),
                        var.Type,
                        new InferredExpressionInformation(false, false),
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

                var generatedCallable = new QsCallable(
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

                // Change the origin of all type parameter references to use the new name and make all variables immutable
                generatedCallable = UpdateGeneratedOp.Apply(generatedCallable, knownVariables, CurrentCallable.Callable.FullName, newName);

                return (generatedCallable, signature.ArgumentType);
            }

            /// <summary>
            /// Generates a new operation with the body's contents, having all the same type parameters
            /// as the current callable and all known variables at the start of the block become
            /// parameters to the new operation. The generated operation is returned, along with a call
            /// to the new operation is also returned with all the type parameters and known
            /// variables being forwarded to the new operation as arguments.
            /// 
            /// The given body should be validated with the SharedState.IsValidScope before using this function.
            /// </summary>
            public (QsCallable, QsStatement) LiftBody(QsScope body)
            {
                var (generatedOp, originalArgumentType) = GenerateOperation(body);
                var generatedOpType = ResolvedType.New(ResolvedTypeKind.NewOperation(
                    Tuple.Create(
                        originalArgumentType,
                        ResolvedType.New(ResolvedTypeKind.UnitType)),
                    generatedOp.Signature.Information));

                // Foreword the type parameters of the parent callable to the type arguments of the call to the generated operation.
                var typeArguments = CurrentCallable.TypeParameters;
                var generatedOpId = new TypedExpression(
                    ExpressionKind.NewIdentifier(
                        Identifier.NewGlobalCallable(generatedOp.FullName),
                        typeArguments),
                    typeArguments.IsNull
                        ? TypeArgsResolution.Empty
                        : typeArguments.Item
                            .Select(type => Tuple.Create(generatedOp.FullName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
                            .ToImmutableArray(),
                    generatedOpType,
                    new InferredExpressionInformation(false, false),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);

                var knownSymbols = body.KnownSymbols.Variables;
                TypedExpression arguments = null;
                if (knownSymbols.Any())
                {
                    var argumentArray = knownSymbols
                        .Select(var => new TypedExpression(
                            ExpressionKind.NewIdentifier(
                                Identifier.NewLocalVariable(var.VariableName),
                                QsNullable<ImmutableArray<ResolvedType>>.Null),
                                TypeArgsResolution.Empty,
                                var.Type,
                                var.InferredInformation,
                                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null))
                        .ToImmutableArray();

                    arguments = new TypedExpression(
                        ExpressionKind.NewValueTuple(argumentArray),
                        TypeArgsResolution.Empty,
                        ResolvedType.New(ResolvedTypeKind.NewTupleType(argumentArray.Select(expr => expr.ResolvedType).ToImmutableArray())),
                        new InferredExpressionInformation(false, argumentArray.Any(exp => exp.InferredInformation.HasLocalQuantumDependency)),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);
                }
                else
                {
                    arguments = new TypedExpression(
                        ExpressionKind.UnitValue,
                        TypeArgsResolution.Empty,
                        ResolvedType.New(ResolvedTypeKind.UnitType),
                        new InferredExpressionInformation(false, false),
                        QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);
                }

                var call = new TypedExpression(
                    ExpressionKind.NewCallLikeExpression(generatedOpId, arguments),
                    typeArguments.IsNull
                        ? TypeArgsResolution.Empty
                        : typeArguments.Item
                            .Select(type => Tuple.Create(CurrentCallable.Callable.FullName, ((ResolvedTypeKind.TypeParameter)type.Resolution).Item.TypeName, type))
                            .ToImmutableArray(),
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    new InferredExpressionInformation(false, true),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);

                return (generatedOp, new QsStatement(
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

        protected class NamespaceTransformation : NamespaceTransformation<TransformationState>
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

            public override QsSpecialization OnControlledAdjointSpecialization(QsSpecialization spec)
            {
                SharedState.InControlledAdjoint = true;
                var rtrn = base.OnControlledAdjointSpecialization(spec);
                SharedState.InControlledAdjoint = false;
                return rtrn;
            }

            public override QsCallable OnFunction(QsCallable c) => c; // Prevent anything in functions from being lifted

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

                if (SharedState.ContainsParamRef)
                {
                    SharedState.IsValidScope = false;
                }

                var rhs = this.Expressions.OnTypedExpression(stm.Rhs);
                return QsStatementKind.NewQsValueUpdate(new QsValueUpdate(lhs, rhs));
            }

            public override QsStatementKind OnStatementKind(QsStatementKind kind)
            {
                SharedState.ContainsParamRef = false; // Every statement kind starts off false
                return base.OnStatementKind(kind);
            }
        }

        protected class ExpressionTransformation : ExpressionTransformation<TransformationState>
        {
            public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override TypedExpression OnTypedExpression(TypedExpression ex)
            {
                var contextContainsParamRef = SharedState.ContainsParamRef;
                SharedState.ContainsParamRef = false;
                var rtrn = base.OnTypedExpression(ex);

                // If the sub context contains a reference, then the super context contains a reference,
                // otherwise return the super context to its original value
                SharedState.ContainsParamRef |= contextContainsParamRef;

                return rtrn;
            }
        }

        protected class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
        {
            public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

            public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.LocalVariable local &&
                SharedState.GeneratedOpParams.Any(param => param.VariableName.Equals(local.Item)))
                {
                    SharedState.ContainsParamRef = true;
                }
                return base.OnIdentifier(sym, tArgs);
            }
        }

        /// <summary>
        /// Transformation that updates the contents of newly generated operations by:
        /// 1. Rerouting the origins of type parameter references to the new operation
        /// 2. Changes the IsMutable and HasLocalQuantumDependency info on parameter references to be false
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
                    if ((ex.InferredInformation.IsMutable || ex.InferredInformation.HasLocalQuantumDependency)
                        && ex.Expression is ExpressionKind.Identifier id
                        && id.Item1 is Identifier.LocalVariable variable
                        && SharedState.Parameters.Any(x => x.VariableName.Equals(variable)))
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
    }
}
