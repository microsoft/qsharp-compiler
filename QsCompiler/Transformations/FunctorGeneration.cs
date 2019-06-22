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
    /// Scope transformation that replaces each operation call within a given scope
    /// with a call to the operation after application of the functor given on initialization. 
    /// The default values used for auto-generation will be used for the additional functor arguments.  
    public class ApplyFunctorToOperationCalls : 
        ScopeTransformation<ExpressionTransformation <ApplyFunctorToOperationCalls.ApplyToExpressionKind>>
    {
        public ApplyFunctorToOperationCalls(QsFunctor functor) :
            base(new ExpressionTransformation<ApplyToExpressionKind>(e => new ApplyToExpressionKind(e, functor))) { }

        private static readonly TypedExpression ControlQubits =
            SyntaxGenerator.ImmutableQubitArrayWithName(NonNullable<string>.New(InternalUse.ControlQubitsName));


        // helper classes

        /// Replaces each operation call with a call to the operation after application of the given functor. 
        /// The default values used for auto-generation will be used for the additional functor arguments.  
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


    /// Scope transformation that reverses the order of execution for operation calls within a given scope.
    /// Note that the transformed scope is only guaranteed to be valid if operation calls only occur within expression statements! 
    /// Otherwise the transformation will succeed, but the generated scope is not necessarily valid. 
    /// Throws an InvalidOperationException if the scope to transform contains while-loops. 
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
            var topStatements = new List<QsStatement>();
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


        // helper classes

        /// Helper class for the scope transformation that reverses the order of all operation calls.
        /// Throws an InvalidOperationException upon while-loops. 
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


    /// Applying the transformation sets all location information to Null.
    public class StripLocationInformation :
        ScopeTransformation<StatementKindTransformation<StripLocationInformation>,NoExpressionTransformations>
    {
        public StripLocationInformation() :
            base(s => new StatementKindTransformation<StripLocationInformation>(s as StripLocationInformation), new NoExpressionTransformations())
        { }

        public override QsNullable<QsLocation> onLocation(QsNullable<QsLocation> loc) =>
            QsNullable<QsLocation>.Null;
    }
}


