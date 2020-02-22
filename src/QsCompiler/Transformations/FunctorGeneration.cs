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
    /// </summary>
    public class ApplyFunctorToOperationCalls :
        SyntaxTreeTransformation<ApplyFunctorToOperationCalls.TransformationsState>
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
            this.StatementKinds = new IgnoreOuterBlockInConjugations<TransformationsState>(this);
            this.ExpressionKinds = new ExpressionKindTransformation(this);
        }


        // static methods for convenience

        private static readonly TypedExpression ControlQubits =
            SyntaxGenerator.ImmutableQubitArrayWithName(NonNullable<string>.New(InternalUse.ControlQubitsName));

        public static readonly Func<QsScope, QsScope> ApplyAdjoint =
            new ApplyFunctorToOperationCalls(QsFunctor.Adjoint).Statements.Transform;

        public static readonly Func<QsScope, QsScope> ApplyControlled =
            new ApplyFunctorToOperationCalls(QsFunctor.Controlled).Statements.Transform;


        // helper classes

        /// <summary>
        /// Replaces each operation call with a call to the operation after application of the given functor. 
        /// The default values used for auto-generation will be used for the additional functor arguments.  
        /// </summary>
        public class ExpressionKindTransformation :
            Core.ExpressionKindTransformation<TransformationsState>
        {
            public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationsState> parent)
                : base(parent)
            { }

            public ExpressionKindTransformation(QsFunctor functor)
                : base(new TransformationsState(functor))
            { }


            public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> onOperationCall(TypedExpression method, TypedExpression arg)
            {
                if (this.Transformation.InternalState.FunctorToApply.IsControlled)
                {
                    method = SyntaxGenerator.ControlledOperation(method);
                    arg = SyntaxGenerator.ArgumentWithControlQubits(arg, ControlQubits);
                }
                else if (this.Transformation.InternalState.FunctorToApply.IsAdjoint)
                {
                    method = SyntaxGenerator.AdjointOperation(method);
                }
                else throw new NotImplementedException("unsupported functor");
                return base.onOperationCall(method, arg);
            }
        }
    }


    /// <summary>
    /// Ensures that the outer block of conjugations is ignored during transformation. 
    /// </summary>
    public class IgnoreOuterBlockInConjugations<T> :
        Core.StatementKindTransformation<T>
    {
        public IgnoreOuterBlockInConjugations(SyntaxTreeTransformation<T> parent)
            : base(parent)
        { }

        public IgnoreOuterBlockInConjugations(T sharedInternalState)
            : base(sharedInternalState)
        { }

        public override QsStatementKind onConjugation(QsConjugation stm)
        {
            var inner = stm.InnerTransformation;
            var innerLoc = this.Transformation.Statements.onLocation(inner.Location);
            var transformedInner = new QsPositionedBlock(this.Transformation.Statements.Transform(inner.Body), innerLoc, inner.Comments);
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
    internal class ReverseOrderOfOperationCalls :
        SelectByAllContainedExpressions 
    {
        public ReverseOrderOfOperationCalls() :
            base(ex => !ex.InferredInformation.HasLocalQuantumDependency, false) // no need to evaluate subexpressions
        { 
            this.StatementKinds = new ReverseLoops(this);
            this.Statements = new StatementTransformation(this);
        }


        // helper classes

        private class StatementTransformation
            : StatementTransformation<ReverseOrderOfOperationCalls>
        {
            public StatementTransformation(ReverseOrderOfOperationCalls parent)
                : base(state => new ReverseOrderOfOperationCalls(), parent)
            { }

            public override QsScope Transform(QsScope scope)
            {
                var topStatements = ImmutableArray.CreateBuilder<QsStatement>();
                var bottomStatements = new List<QsStatement>();
                foreach (var statement in scope.Statements)
                {
                    var transformed = this.onStatement(statement);
                    if (this.SubSelector.InternalState.SatisfiesCondition) topStatements.Add(statement);
                    else bottomStatements.Add(transformed);
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
        private class ReverseLoops : 
            IgnoreOuterBlockInConjugations<TransformationState>
        {
            internal ReverseLoops(ReverseOrderOfOperationCalls parent) :
                base(parent) 
            { }

            public override QsStatementKind onForStatement(QsForStatement stm)
            {
                var reversedIterable = SyntaxGenerator.ReverseIterable(stm.IterationValues);
                stm = new QsForStatement(stm.LoopItem, reversedIterable, stm.Body);
                return base.onForStatement(stm);
            }

            public override QsStatementKind onWhileStatement(QsWhileStatement stm) =>
                throw new InvalidOperationException("cannot reverse while-loops");
        }
    }
}


