// Copyright (c) Microsoft Corporation.
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
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.Transformations.FunctorGeneration
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, string, ResolvedType>>;

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

            public TransformationsState(QsFunctor functor) => this.FunctorToApply = functor;
        }

        public ApplyFunctorToOperationCalls(QsFunctor functor)
        : base(new TransformationsState(functor))
        {
            if (functor.IsControlled)
            {
                this.Statements = new BasicTransformations.AddVariableDeclarations<TransformationsState>(this, ControlQubitsDeclaration);
            }
            this.StatementKinds = new IgnoreOuterBlockInConjugations<TransformationsState>(this);
            this.ExpressionKinds = new ExpressionKindTransformation(this);
            this.Types = new TypeTransformation<TransformationsState>(this, TransformationOptions.Disabled);
        }

        // static methods for convenience

        private static readonly string ControlQubitsName = InternalUse.ControlQubitsName;

        private static readonly TypedExpression ControlQubits =
            SyntaxGenerator.ImmutableQubitArrayWithName(ControlQubitsName);

        private static readonly LocalVariableDeclaration<string> ControlQubitsDeclaration =
            new LocalVariableDeclaration<string>(
                ControlQubitsName,
                ControlQubits.ResolvedType,
                ControlQubits.InferredInformation,
                QsNullable<Position>.Null,
                Range.Zero);

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
            public override ExpressionKind OnOperationCall(TypedExpression method, TypedExpression arg)
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
    [Obsolete("AddVariableDeclarations should not be used directly and will be made internal in a future release.")]
    public class AddVariableDeclarations<T>
    : StatementTransformation<T>
    {
        private readonly IEnumerable<LocalVariableDeclaration<string>> addedVariableDeclarations;

        public AddVariableDeclarations(SyntaxTreeTransformation<T> parent, params LocalVariableDeclaration<string>[] addedVars)
        : base(parent) =>
            this.addedVariableDeclarations = addedVars;

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
        public IgnoreOuterBlockInConjugations(SyntaxTreeTransformation<T> parent, TransformationOptions? options = null)
        : base(parent, options ?? TransformationOptions.Default)
        {
        }

        public IgnoreOuterBlockInConjugations(T sharedInternalState, TransformationOptions? options = null)
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
    /// Scope transformation that splits any nested operation calls into separate statements so
    /// that they can be properly reversed. This is necessary to avoid out of order execution of the
    /// automatically generated adjoint. It is safe to do because an adjointable operation must return
    /// Unit, so any nested calls can be replaced by Unit and those calls moved to separate,
    /// ordered statements.
    /// </summary>
    internal class ExtractNestedOperationCalls
    : SyntaxTreeTransformation<ExtractNestedOperationCalls.TransformationsState>
    {
        internal class TransformationsState
        {
            /// <summary>
            /// Accumulates statements that have been lifted from the current statement.
            /// </summary>
            public List<QsStatement> AdditionalStatements = new List<QsStatement>();

            /// <summary>
            /// Tracks the current expression that is being evaluated, keeping previous
            /// expressions in the stack so that we can return to those when leaving a nested
            /// evaluation.
            /// </summary>
            public Stack<TypedExpression> CurrentExpression = new Stack<TypedExpression>();

            /// <summary>
            /// Allows us to remember the current statement location, and use that for the generated
            /// statements that get added when extracting nested expressions.
            /// </summary>
            public QsNullable<QsLocation> StatementLocation = QsNullable<QsLocation>.Null;
        }

        public ExtractNestedOperationCalls()
        : base(new TransformationsState())
        {
            this.Statements = new StatementTransformation(this);
            this.Expressions = new ExpressionTransformation(this);
            this.ExpressionKinds = new ExpressionKindTransformation(this);
            this.Types = new TypeTransformation<TransformationsState>(this, TransformationOptions.Disabled);
        }

        private class StatementTransformation
        : StatementTransformation<TransformationsState>
        {
            public StatementTransformation(SyntaxTreeTransformation<TransformationsState> parent)
            : base(parent)
            {
            }

            public override QsScope OnScope(QsScope scope)
            {
                var statements = new List<QsStatement>();
                foreach (var statement in scope.Statements)
                {
                    this.SharedState.StatementLocation = statement.Location;
                    var transformed = this.OnStatement(statement);
                    this.SharedState.StatementLocation = QsNullable<QsLocation>.Null;
                    statements.AddRange(this.SharedState.AdditionalStatements);
                    this.SharedState.AdditionalStatements.Clear();
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
        }

        private class ExpressionTransformation
        : ExpressionTransformation<TransformationsState>
        {
            public ExpressionTransformation(SyntaxTreeTransformation<TransformationsState> parent)
            : base(parent, TransformationOptions.Default)
            {
            }

            public override TypedExpression OnTypedExpression(TypedExpression ex)
            {
                this.SharedState.CurrentExpression.Push(ex);
                var newEx = base.OnTypedExpression(ex);
                this.SharedState.CurrentExpression.Pop();
                return newEx;
            }
        }

        private class ExpressionKindTransformation
        : ExpressionKindTransformation<TransformationsState>
        {
            public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationsState> parent)
            : base(parent)
            {
            }

            public override ExpressionKind OnOperationCall(TypedExpression method, TypedExpression arg)
            {
                // An operation in an adjoint scope must return Unit, so extract the operation to be an
                // an additional statement and then replace it with Unit.
                var curExpression = this.SharedState.CurrentExpression.Peek();
                this.SharedState.AdditionalStatements.Add(
                    new QsStatement(
                        QsStatementKind.NewQsExpressionStatement(
                            new TypedExpression(
                                base.OnOperationCall(method, arg),
                                curExpression.TypeParameterResolutions.Select(x => Tuple.Create(x.Key.Item1, x.Key.Item2, x.Value)).ToImmutableArray(),
                                curExpression.ResolvedType,
                                curExpression.InferredInformation,
                                curExpression.Range)),
                        LocalDeclarations.Empty,
                        this.SharedState.StatementLocation,
                        QsComments.Empty));

                return ExpressionKind.UnitValue;
            }
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
                    if (this.subSelector?.SharedState.SatisfiesCondition ?? false)
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
        /// </summary>
        /// <exception cref="InvalidOperationException">Encountered a while-loop.</exception>
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
