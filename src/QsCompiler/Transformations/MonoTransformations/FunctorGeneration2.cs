// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if MONO

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
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.Transformations.FunctorGeneration
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;

    /// <summary>
    /// Scope transformation that replaces each operation call within a given scope
    /// with a call to the operation after application of the functor given on initialization.
    /// The default values used for auto-generation will be used for the additional functor arguments.
    /// Additional functor arguments will be added to the list of defined variables for each scope.
    /// </summary>
    public class ApplyFunctorToOperationCalls
    : MonoTransformation
    {
        public QsFunctor FunctorToApply { get; }

        public ApplyFunctorToOperationCalls(QsFunctor functor)
        : base()
        {
            this.FunctorToApply = functor;
        }

        /* static methods for convenience */

        private static readonly string ControlQubitsName = InternalUse.ControlQubitsName;

        private static readonly TypedExpression ControlQubits =
            SyntaxGenerator.ImmutableQubitArrayWithName(ControlQubitsName);

        private static readonly LocalVariableDeclaration<string, ResolvedType> ControlQubitsDeclaration =
            new LocalVariableDeclaration<string, ResolvedType>(
                ControlQubitsName,
                ControlQubits.ResolvedType,
                ControlQubits.InferredInformation,
                QsNullable<Position>.Null,
                Range.Zero);

        public static readonly Func<QsScope, QsScope> ApplyAdjoint =
            new ApplyFunctorToOperationCalls(QsFunctor.Adjoint).OnScope;

        public static readonly Func<QsScope, QsScope> ApplyControlled =
            new ApplyFunctorToOperationCalls(QsFunctor.Controlled).OnScope;

        /* overrides */

        /// <inheritdoc/>
        public override ExpressionKind OnOperationCall(TypedExpression method, TypedExpression arg)
        {
            // Replaces each operation call with a call to the operation after application of the given functor.
            // The default values used for auto-generation will be used for the additional functor arguments.
            if (this.FunctorToApply.IsControlled)
            {
                method = SyntaxGenerator.ControlledOperation(method);
                arg = SyntaxGenerator.ArgumentWithControlQubits(arg, ControlQubits);
            }
            else if (this.FunctorToApply.IsAdjoint)
            {
                method = SyntaxGenerator.AdjointOperation(method);
            }
            else
            {
                throw new NotImplementedException("unsupported functor");
            }

            return base.OnOperationCall(method, arg);
        }

        /// <inheritdoc/>
        public override LocalDeclarations OnLocalDeclarations(LocalDeclarations decl)
        {
            if (this.FunctorToApply.IsControlled)
            {
                return base.OnLocalDeclarations(new LocalDeclarations(decl.Variables.Add(ControlQubitsDeclaration)));
            }
            else
            {
                return base.OnLocalDeclarations(decl);
            }
        }

        /// <inheritdoc/>
        public override QsStatementKind OnConjugation(QsConjugation stm)
        {
            var inner = stm.InnerTransformation;
            var innerLoc = this.OnRelativeLocation(inner.Location);
            var transformedInner = new QsPositionedBlock(this.OnScope(inner.Body), innerLoc, inner.Comments);
            return QsStatementKind.NewQsConjugation(new QsConjugation(stm.OuterTransformation, transformedInner));
        }
    }

    /// <summary>
    /// Scope transformation that splits any nested operation calls into separate statements so
    /// that they can be properly reversed. This is necessary to avoid out of order execution of the
    /// automatically generated adjoint. It is safe to do because an adjointable operation must return
    /// Unit, so any nested calls can be replaced by Unit and those calls moved to separate,
    /// ordered statements.
    /// </summary>
    internal class ExtractNestedOperationCalls
    : MonoTransformation
    {
        /// <summary>
        /// Accumulates statements that have been lifted from the current statement.
        /// </summary>
        public List<QsStatement> AdditionalStatements { get; set; } = new List<QsStatement>();

        /// <summary>
        /// Tracks the current expression that is being evaluated, keeping previous
        /// expressions in the stack so that we can return to those when leaving a nested
        /// evaluation.
        /// </summary>
        public Stack<TypedExpression> CurrentExpression { get; set; } = new Stack<TypedExpression>();

        /// <summary>
        /// Allows us to remember the current statement location, and use that for the generated
        /// statements that get added when extracting nested expressions.
        /// </summary>
        public QsNullable<QsLocation> StatementLocation { get; set; } = QsNullable<QsLocation>.Null;

        public ExtractNestedOperationCalls()
        : base()
        {
        }

        /* overrides */

        public override QsScope OnScope(QsScope scope)
        {
            var statements = new List<QsStatement>();
            foreach (var statement in scope.Statements)
            {
                this.StatementLocation = statement.Location;
                var transformed = this.OnStatement(statement);
                this.StatementLocation = QsNullable<QsLocation>.Null;
                statements.AddRange(this.AdditionalStatements);
                this.AdditionalStatements.Clear();
                if (!(transformed.Statement is QsStatementKind.QsExpressionStatement expr &&
                    expr.Item.Expression == ExpressionKind.UnitValue))
                {
                    // Only add statements that are not free-floating Unit, which could have
                    // been left behind by expression transformation.
                    statements.Add(transformed);
                }
            }

            return new QsScope(statements.ToImmutableArray(), scope.KnownSymbols);
        }

        public override TypedExpression OnTypedExpression(TypedExpression ex)
        {
            this.CurrentExpression.Push(ex);
            var newEx = base.OnTypedExpression(ex);
            this.CurrentExpression.Pop();
            return newEx;
        }

        public override ExpressionKind OnOperationCall(TypedExpression method, TypedExpression arg)
        {
            // An operation in an adjoint scope must return Unit, so extract the operation to be an
            // an additional statement and then replace it with Unit.
            var curExpression = this.CurrentExpression.Peek();
            this.AdditionalStatements.Add(
                new QsStatement(
                    QsStatementKind.NewQsExpressionStatement(
                        new TypedExpression(
                            base.OnOperationCall(method, arg),
                            curExpression.TypeParameterResolutions.Select(x => Tuple.Create(x.Key.Item1, x.Key.Item2, x.Value)).ToImmutableArray(),
                            curExpression.ResolvedType,
                            curExpression.InferredInformation,
                            curExpression.Range)),
                    LocalDeclarations.Empty,
                    this.StatementLocation,
                    QsComments.Empty));

            return ExpressionKind.UnitValue;
        }
    }

    /// <summary>
    /// Scope transformation that reverses the order of execution for operation calls within a given scope,
    /// unless these calls occur within the outer block of a conjugation. Outer transformations of conjugations are left unchanged.
    /// Note that the transformed scope is only guaranteed to be valid if operation calls only occur within expression statements!
    /// Otherwise the transformation will succeed, but the generated scope is not necessarily valid.
    /// </summary>
    /// <exception cref="InvalidOperationException">The scope to transform contains while-loops.</exception>
    internal class ReverseOrderOfOperationCalls
    : SelectByAllContainedExpressions
    {
        public ReverseOrderOfOperationCalls()
        : base(ex => !ex.InferredInformation.HasLocalQuantumDependency, false) // no need to evaluate subexpressions
        {
        }

        /* overrides */

        public override QsScope OnScope(QsScope scope)
        {
            var topStatements = ImmutableArray.CreateBuilder<QsStatement>();
            var bottomStatements = new List<QsStatement>();
            foreach (var statement in scope.Statements)
            {
                var transformed = this.OnStatement(statement);
                if (this.SubSelector?.SharedState.SatisfiesCondition ?? false)
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

        public override QsStatementKind OnForStatement(QsForStatement stm)
        {
            var reversedIterable = SyntaxGenerator.ReverseIterable(stm.IterationValues);
            stm = new QsForStatement(stm.LoopItem, reversedIterable, stm.Body);
            return base.OnForStatement(stm);
        }

        public override QsStatementKind OnWhileStatement(QsWhileStatement stm) =>
            throw new InvalidOperationException("cannot reverse while-loops");

        /// <inheritdoc/>
        public override QsStatementKind OnConjugation(QsConjugation stm)
        {
            var inner = stm.InnerTransformation;
            var innerLoc = this.OnRelativeLocation(inner.Location);
            var transformedInner = new QsPositionedBlock(this.OnScope(inner.Body), innerLoc, inner.Comments);
            return QsStatementKind.NewQsConjugation(new QsConjugation(stm.OuterTransformation, transformedInner));
        }
    }
}

#endif
