// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;


namespace Microsoft.Quantum.QsCompiler.Transformations.Conjugations
{
    /// <summary>
    /// Syntax tree transformation that inlines all conjugations, thus eliminating them from a given scope.
    /// All exception thrown during transformation are caught and the action given upon instantiation - if any - is called upon them. 
    /// The syntax tree is left unchanged if an exception occurs. The Success property is true if and only if no exceptions occurred. 
    /// The generation of the adjoint for the outer block is subject to the same limitation as any adjoint auto-generation. 
    /// In particular, it is only guaranteed to be valid if operation calls only occur within expression statements, and 
    /// throws an InvalidOperationException if the outer block contains while-loops. 
    /// </summary>
    public class InlineConjugations
        : SyntaxTreeTransformation<InlineConjugationStatements>
    {
        public bool Success { get; private set; }
        private readonly Action<Exception> OnException;

        public InlineConjugations(Action<Exception> onException = null)
            : base(new InlineConjugationStatements())
        {
            this.Success = true;
            this.OnException = onException;
        }

        public override Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope> onProvidedImplementation
            (QsTuple<LocalVariableDeclaration<QsLocalSymbol>> argTuple, QsScope body)
        {
            this._Scope.Reset();
            try { body = this._Scope.Transform(body); }
            catch (Exception ex)
            {
                this.OnException?.Invoke(ex);
                this.Success = false;
            }
            return new Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope>(argTuple, body);
        }
    }

    /// <summary>
    /// Scope transformation that inlines all conjugations, thus eliminating them from a given scope.
    /// The generation of the adjoint for the outer block is subject to the same limitation as any adjoint auto-generation. 
    /// In particular, it is only guaranteed to be valid if operation calls only occur within expression statements, and 
    /// throws an InvalidOperationException if the outer block contains while-loops. 
    /// </summary>
    public class InlineConjugationStatements 
        : ScopeTransformation<StatementKindTransformation<InlineConjugationStatements>, NoExpressionTransformations>
    {
        private Func<QsScope, QsScope> ResolveNames;
        internal void Reset() => this.ResolveNames = new UniqueVariableNames().Transform;

        public InlineConjugationStatements()
            : base(s => new StatementKindTransformation<InlineConjugationStatements>(s as InlineConjugationStatements), new NoExpressionTransformations()) =>
            this.ResolveNames = new UniqueVariableNames().Transform;

        public override QsScope Transform(QsScope scope)
        {
            var statements = ImmutableArray.CreateBuilder<QsStatement>();
            foreach (var statement in scope.Statements)
            {
                if (statement.Statement is QsStatementKind.QsConjugation conj)
                {
                    // since we are eliminating scopes, 
                    // we need to make sure that the variables defined within the inlined scopes do not clash with other defined variables.
                    var outer = ResolveNames(this.Transform(conj.Item.OuterTransformation.Body));
                    var inner = ResolveNames(this.Transform(conj.Item.InnerTransformation.Body));
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


