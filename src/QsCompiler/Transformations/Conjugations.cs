// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;


namespace Microsoft.Quantum.QsCompiler.Transformations.Conjugations
{
    /// <summary>
    /// Scope transformation that inlines all conjugate-statements, thus eliminating them from a given scope.
    /// The generation of the adjoint for the outer block needed for conjugation is subject to the same limitation as any adjoint auto-generation. 
    /// In particular, it is only guaranteed to be valid if operation calls only occur within expression statements, and 
    /// throws an InvalidOperationException if the outer block contains while-loops. 
    /// </summary>
    public class InlineConjugateStatements 
        : ScopeTransformation<StatementKindTransformation<InlineConjugateStatements>, NoExpressionTransformations>
    {
        private readonly Func<QsScope, QsScope> ResolveNames;

        public InlineConjugateStatements()
            : base(s => new StatementKindTransformation<InlineConjugateStatements>(s as InlineConjugateStatements), new NoExpressionTransformations()) =>
            this.ResolveNames = new UniqueVariableNames().Transform;

        public override QsScope Transform(QsScope scope)
        {
            var statements = ImmutableArray.CreateBuilder<QsStatement>();
            foreach (var statement in scope.Statements)
            {
                if (statement.Statement is QsStatementKind.QsConjugateStatement conjStm)
                {
                    // since we are eliminating scopes, 
                    // we need to make sure that the variables defined within the inlined scopes do not clash with other defined variables.
                    var outer = ResolveNames(this.Transform(conjStm.Item.OuterTransformation.Body));
                    var inner = ResolveNames(this.Transform(conjStm.Item.InnerTransformation.Body));
                    var adjOuter = outer.GenerateAdjoint(); // will add a unique name wrapper

                    statements.AddRange(outer.Statements);
                    statements.AddRange(inner.Statements);
                    statements.AddRange(adjOuter.Statements);
                }
                else statements.Add(this.onStatement(statement));
            }
            return new QsScope(statements.ToImmutableArray(), scope.KnownSymbols);
        }
    }
}


