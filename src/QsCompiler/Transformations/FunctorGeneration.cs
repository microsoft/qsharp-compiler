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


namespace Microsoft.Quantum.QsCompiler.Transformations.FunctorGeneration
{
    /// <summary>
    /// Scope transformation that replaces each operation call within a given scope
    /// with a call to the operation after application of the functor given on initialization. 
    /// The default values used for auto-generation will be used for the additional functor arguments.  
    /// </summary>
    public class ApplyFunctorToOperationCalls : 
        ScopeTransformation<ExpressionTransformation <ApplyFunctorToOperationCalls.ApplyToExpressionKind>>
    {
        public ApplyFunctorToOperationCalls(QsFunctor functor) :
            base(new ExpressionTransformation<ApplyToExpressionKind>(e => new ApplyToExpressionKind(e, functor))) { }

        private static readonly TypedExpression ControlQubits =
            SyntaxGenerator.ImmutableQubitArrayWithName(NonNullable<string>.New(InternalUse.ControlQubitsName));

        public static readonly Func<QsScope, QsScope> ApplyAdjoint =
            new ApplyFunctorToOperationCalls(QsFunctor.Adjoint).Transform;

        public static readonly Func<QsScope, QsScope> ApplyControlled =
            new ApplyFunctorToOperationCalls(QsFunctor.Controlled).Transform;


        // helper class

        /// <summary>
        /// Replaces each operation call with a call to the operation after application of the given functor. 
        /// The default values used for auto-generation will be used for the additional functor arguments.  
        /// </summary>
        public class ApplyToExpressionKind : 
            ExpressionKindTransformation<Core.ExpressionTransformation> 
        {
            public readonly QsFunctor FunctorToApply;
            public ApplyToExpressionKind(Core.ExpressionTransformation expression, QsFunctor functor) : 
                base(expression) =>
                this.FunctorToApply = functor ?? throw new ArgumentNullException(nameof(functor));

            public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> onOperationCall(TypedExpression method, TypedExpression arg)
            {
                if (this.FunctorToApply.IsControlled)
                {
                    method = SyntaxGenerator.ControlledOperation(method);
                    arg = SyntaxGenerator.ArgumentWithControlQubits(arg, ControlQubits);
                }
                else if (this.FunctorToApply.IsAdjoint)
                {
                    method = SyntaxGenerator.AdjointOperation(method);
                }
                else throw new NotImplementedException("unsupported functor");
                return base.onOperationCall(method, arg);
            }
        }
    }


    /// <summary>
    /// Scope transformation that reverses the order of execution for operation calls within a given scope.
    /// Note that the transformed scope is only guaranteed to be valid if operation calls only occur within expression statements! 
    /// Otherwise the transformation will succeed, but the generated scope is not necessarily valid. 
    /// Throws an InvalidOperationException if the scope to transform contains while-loops. 
    /// </summary>
    public class ReverseOrderOfOperationCalls :
        SelectByAllContainedExpressions<ReverseOrderOfOperationCalls.ReverseLoops>
    {
        public ReverseOrderOfOperationCalls() :
            base(ex => !ex.InferredInformation.HasLocalQuantumDependency, false, s => new ReverseLoops(s as ReverseOrderOfOperationCalls)) // no need to evaluate subexpressions
        { }

        protected override SelectByFoldingOverExpressions<ReverseLoops> GetSubSelector() =>
            new ReverseOrderOfOperationCalls();

        public override QsScope Transform(QsScope scope)
        {
            var topStatements = ImmutableArray.CreateBuilder<QsStatement>();
            var bottomStatements = new List<QsStatement>();
            foreach (var statement in scope.Statements)
            {
                var transformed = this.onStatement(statement);
                if (this.SubSelector.SatisfiesCondition) topStatements.Add(statement);
                else bottomStatements.Add(transformed);
            }
            bottomStatements.Reverse();
            return new QsScope(topStatements.Concat(bottomStatements).ToImmutableArray(), scope.KnownSymbols);
        }


        // helper class

        /// <summary>
        /// Helper class for the scope transformation that reverses the order of all operation calls.
        /// Throws an InvalidOperationException upon while-loops. 
        /// </summary>
        public class ReverseLoops : 
            StatementKindTransformation<ReverseOrderOfOperationCalls>
        {
            internal ReverseLoops(ReverseOrderOfOperationCalls scope) :
                base(scope) { }

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


    /// <summary>
    /// Scope transformation that inlines all conjugate-statements, thus eliminating them from a given scope.
    /// The generation of the adjoint for the outer block needed for conjugation is subject to the same limitation as any adjoint auto-generation. 
    /// In particular, it is only guaranteed to be valid if operation calls only occur within expression statements, and 
    /// throws an InvalidOperationException if the outer block contains while-loops. 
    /// </summary>
    public class InlineConjugateStatements :
        ScopeTransformation<StatementKindTransformation<InlineConjugateStatements>, NoExpressionTransformations>
    {
        public InlineConjugateStatements() :
            base(s => new StatementKindTransformation<InlineConjugateStatements>(s as InlineConjugateStatements), new NoExpressionTransformations())
        { }

        public static Func<QsScope, QsScope> Apply =
            new InlineConjugateStatements().Transform;

        public override QsScope Transform(QsScope scope)
        {
            var statements = ImmutableArray.CreateBuilder<QsStatement>();
            foreach (var statement in scope.Statements)
            {
                if (statement.Statement is QsStatementKind.QsConjugateStatement conjStm)
                {
                    var outer = this.Transform(conjStm.Item.OuterTransformation.Body);
                    var inner = this.Transform(conjStm.Item.InnerTransformation.Body);
                    var adjOuter = outer.GenerateAdjoint();

                    statements.AddRange(outer.Statements);
                    statements.AddRange(inner.Statements);
                    statements.AddRange(adjOuter.Statements);
                }
                else statements.Add(this.onStatement(statement));
            }
            return new QsScope(statements.ToImmutableArray(), scope.KnownSymbols);
        }
    }


    /// <summary>
    /// Applying the transformation sets all location information to Null.
    /// </summary>
    public class StripLocationInformation :
        ScopeTransformation<StatementKindTransformation<StripLocationInformation>, NoExpressionTransformations>
    {
        public StripLocationInformation() :
            base(s => new StatementKindTransformation<StripLocationInformation>(s as StripLocationInformation), new NoExpressionTransformations())
        { }

        public override QsNullable<QsLocation> onLocation(QsNullable<QsLocation> loc) =>
            QsNullable<QsLocation>.Null;

        public static readonly Func<QsScope, QsScope> Apply =
            new StripLocationInformation().Transform;
    }


    public static class AutoGeneration
    {
        /// <summary>
        /// Given the body of an operation, auto-generates the (content of the) adjoint specialization, 
        /// under the assumption that operation calls may only ever occur within expression statements, 
        /// and while-loops cannot occur within operations. 
        /// </summary>
        public static QsScope GenerateAdjoint(this QsScope scope)
        {
            scope = ApplyFunctorToOperationCalls.ApplyAdjoint(scope);
            scope = new ReverseOrderOfOperationCalls().Transform(scope);
            return StripLocationInformation.Apply(scope);
        }

        /// <summary>
        /// Given the body of an operation, auto-generates the (content of the) controlled specialization 
        /// using the default name for control qubits.
        /// </summary>
        public static QsScope GenerateControlled(this QsScope scope)
        {
            scope = ApplyFunctorToOperationCalls.ApplyControlled(scope);
            return StripLocationInformation.Apply(scope);
        }

        /// <summary>
        /// Eliminates all conjugate-statements from the given scope by replacing them with the corresponding implementations (i.e. inlining them). 
        /// The generation of the adjoint for the outer block needed for conjugation is subject to the same limitation as any adjoint auto-generation. 
        /// In particular, it is only guaranteed to be valid if operation calls only occur within expression statements, and 
        /// throws an InvalidOperationException if the outer block contains while-loops. 
        /// </summary>
        public static QsScope InlineConjugations(this QsScope scope) =>
            InlineConjugateStatements.Apply(scope); 
    }
}


