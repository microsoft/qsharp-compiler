// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.FunctorGeneration
{
    /// <summary>
    /// Scope transformation that replaces each operation call within a given scope
    /// with a call to the operation after application of the functor given on initialization.
    /// The default values used for auto-generation will be used for the additional functor arguments.
    /// Additional functor arguments will be added to the list of defined variables for each scope.
    /// </summary>
    public class ApplyFunctorToOperationCalls
    : SyntaxTreeTransformation<ApplyFunctorToOperationCalls.TransformationsState>
    {
        public class TransformationsState
        {
            public readonly QsFunctor FunctorToApply;

            public TransformationsState(QsFunctor functor) =>
                this.FunctorToApply = functor ?? throw new ArgumentNullException(nameof(functor));
        }

        public ApplyFunctorToOperationCalls(QsFunctor functor)
        : base(new TransformationsState(functor))
        {
            if (functor.IsControlled)
            {
                this.Statements = new AddVariableDeclarations<TransformationsState>(this, ControlQubitsDeclaration);
            }
            this.StatementKinds = new IgnoreOuterBlockInConjugations<TransformationsState>(this);
            this.ExpressionKinds = new ExpressionKindTransformation(this);
            this.Types = new TypeTransformation<TransformationsState>(this, TransformationOptions.Disabled);
        }

        // static methods for convenience

        private static readonly NonNullable<string> ControlQubitsName =
            NonNullable<string>.New(InternalUse.ControlQubitsName);

        private static readonly TypedExpression ControlQubits =
            SyntaxGenerator.ImmutableQubitArrayWithName(ControlQubitsName);

        private static readonly LocalVariableDeclaration<NonNullable<string>> ControlQubitsDeclaration =
            new LocalVariableDeclaration<NonNullable<string>>(
                ControlQubitsName,
                ControlQubits.ResolvedType,
                ControlQubits.InferredInformation,
                QsNullable<Tuple<int, int>>.Null,
                QsCompilerDiagnostic.DefaultRange);

        public static readonly Func<QsScope, QsScope> ApplyAdjoint =
            new ApplyFunctorToOperationCalls(QsFunctor.Adjoint).Statements.OnScope;

        public static readonly Func<QsScope, QsScope> ApplyControlled =
            new ApplyFunctorToOperationCalls(QsFunctor.Controlled).Statements.OnScope;

        // helper classes

        /// <summary>
        /// Replaces each operation call with a call to the operation after application of the given functor.
        /// The default values used for auto-generation will be used for the additional functor arguments.
        /// </summary>
        public class ExpressionKindTransformation
        : ExpressionKindTransformation<TransformationsState>
        {
            public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationsState> parent)
            : base(parent)
            {
            }

            public ExpressionKindTransformation(QsFunctor functor)
            : base(new TransformationsState(functor))
            {
            }

            /// <inheritdoc/>
            public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> OnOperationCall(TypedExpression method, TypedExpression arg)
            {
                if (this.SharedState.FunctorToApply.IsControlled)
                {
                    method = SyntaxGenerator.ControlledOperation(method);
                    arg = SyntaxGenerator.ArgumentWithControlQubits(arg, ControlQubits);
                }
                else if (this.SharedState.FunctorToApply.IsAdjoint)
                {
                    method = SyntaxGenerator.AdjointOperation(method);
                }
                else
                {
                    throw new NotImplementedException("unsupported functor");
                }
                return base.OnOperationCall(method, arg);
            }
        }
    }

    /// <summary>
    /// Adds the given variable declarations to the list of defined variables for each scope.
    /// </summary>
    public class AddVariableDeclarations<T>
    : StatementTransformation<T>
    {
        private readonly IEnumerable<LocalVariableDeclaration<NonNullable<string>>> addedVariableDeclarations;

        public AddVariableDeclarations(SyntaxTreeTransformation<T> parent, params LocalVariableDeclaration<NonNullable<string>>[] addedVars)
        : base(parent) =>
            this.addedVariableDeclarations = addedVars ?? throw new ArgumentNullException(nameof(addedVars));

        /// <inheritdoc/>
        public override LocalDeclarations OnLocalDeclarations(LocalDeclarations decl) =>
            base.OnLocalDeclarations(new LocalDeclarations(decl.Variables.AddRange(this.addedVariableDeclarations)));
    }

    /// <summary>
    /// Ensures that the outer block of conjugations is ignored during transformation.
    /// </summary>
    public class IgnoreOuterBlockInConjugations<T>
    : StatementKindTransformation<T>
    {
        public IgnoreOuterBlockInConjugations(SyntaxTreeTransformation<T> parent, TransformationOptions options = null)
        : base(parent, options ?? TransformationOptions.Default)
        {
        }

        public IgnoreOuterBlockInConjugations(T sharedInternalState, TransformationOptions options = null)
        : base(sharedInternalState, options ?? TransformationOptions.Default)
        {
        }

        /// <inheritdoc/>
        public override QsStatementKind OnConjugation(QsConjugation stm)
        {
            var inner = stm.InnerTransformation;
            var innerLoc = this.Transformation.Statements.OnLocation(inner.Location);
            var transformedInner = new QsPositionedBlock(this.Transformation.Statements.OnScope(inner.Body), innerLoc, inner.Comments);
            return QsStatementKind.NewQsConjugation(new QsConjugation(stm.OuterTransformation, transformedInner));
        }
    }

    /// <summary>
    /// Scope transformation that reverses the order of execution for operation calls within a given scope,
    /// unless these calls occur within the outer block of a conjugation. Outer transformations of conjugations are left unchanged.
    /// Note that the transformed scope is only guaranteed to be valid if operation calls only occur within expression statements!
    /// Otherwise the transformation will succeed, but the generated scope is not necessarily valid.
    /// Throws an InvalidOperationException if the scope to transform contains while-loops.
    /// </summary>
    internal class ReverseOrderOfOperationCalls
    : SelectByAllContainedExpressions
    {
        public ReverseOrderOfOperationCalls()
        : base(ex => !ex.InferredInformation.HasLocalQuantumDependency, false) // no need to evaluate subexpressions
        {
            // Do *not* disable transformations; the base class takes care of that!
            this.StatementKinds = new ReverseLoops(this);
            this.Statements = new StatementTransformation(this);
        }

        // helper classes

        private class StatementTransformation
        : StatementTransformation<ReverseOrderOfOperationCalls>
        {
            public StatementTransformation(ReverseOrderOfOperationCalls parent)
            : base(state => new ReverseOrderOfOperationCalls(), parent)
            {
            }

            public override QsScope OnScope(QsScope scope)
            {
                var topStatements = ImmutableArray.CreateBuilder<QsStatement>();
                var bottomStatements = new List<QsStatement>();
                foreach (var statement in scope.Statements)
                {
                    var transformed = this.OnStatement(statement);
                    if (this.SubSelector.SharedState.SatisfiesCondition)
                    {
                        topStatements.Add(statement);
                    }
                    else
                    {
                        bottomStatements.Add(transformed);
                    }
                }
                bottomStatements.Reverse();
                return new QsScope(topStatements.Concat(bottomStatements).ToImmutableArray(), scope.KnownSymbols);
            }
        }

        /// <summary>
        /// Helper class to reverse the order of all operation calls
        /// unless these calls occur within the outer block of a conjugation.
        /// Outer transformations of conjugations are left unchanged.
        /// Throws an InvalidOperationException upon while-loops.
        /// </summary>
        private class ReverseLoops
        : IgnoreOuterBlockInConjugations<TransformationState>
        {
            internal ReverseLoops(ReverseOrderOfOperationCalls parent)
            : base(parent)
            {
            }

            public override QsStatementKind OnForStatement(QsForStatement stm)
            {
                var reversedIterable = SyntaxGenerator.ReverseIterable(stm.IterationValues);
                stm = new QsForStatement(stm.LoopItem, reversedIterable, stm.Body);
                return base.OnForStatement(stm);
            }

            public override QsStatementKind OnWhileStatement(QsWhileStatement stm) =>
                throw new InvalidOperationException("cannot reverse while-loops");
        }
    }
}
