// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
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
    : SyntaxTreeTransformation<InlineConjugations.TransformationState>
    {
        public class TransformationState
        {
            public bool Success { get; internal set; }

            internal readonly Action<Exception>? OnException;

            internal Func<QsScope, QsScope> ResolveNames =
                new UniqueVariableNames().Statements.OnScope;

            public void Reset() =>
                this.ResolveNames = new UniqueVariableNames().Statements.OnScope;

            public TransformationState(Action<Exception>? onException = null)
            {
                this.Success = true;
                this.OnException = onException;
            }
        }

        public InlineConjugations(Action<Exception>? onException = null)
        : base(new TransformationState(onException))
        {
            this.Namespaces = new NamespaceTransformation(this);
            this.Statements = new StatementTransformation(this);
            this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
            this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
        }

        // helper classes

        private class StatementTransformation
        : StatementTransformation<TransformationState>
        {
            public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent)
            {
            }

            public override QsScope OnScope(QsScope scope)
            {
                var statements = ImmutableArray.CreateBuilder<QsStatement>();
                foreach (var statement in scope.Statements)
                {
                    if (statement.Statement is QsStatementKind.QsConjugation conj)
                    {
                        // since we are eliminating scopes,
                        // we need to make sure that the variables defined within the inlined scopes do not clash with other defined variables.
                        var outer = this.SharedState.ResolveNames(this.OnScope(conj.Item.OuterTransformation.Body));
                        var inner = this.SharedState.ResolveNames(this.OnScope(conj.Item.InnerTransformation.Body));
                        var adjOuter = outer.GenerateAdjoint(); // will add a unique name wrapper

                        statements.AddRange(outer.Statements);
                        statements.AddRange(inner.Statements);
                        statements.AddRange(adjOuter.Statements);
                    }
                    else
                    {
                        statements.Add(this.OnStatement(statement));
                    }
                }
                return new QsScope(statements.ToImmutableArray(), scope.KnownSymbols);
            }
        }

        private class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent)
            {
            }

            public override Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope> OnProvidedImplementation(
                QsTuple<LocalVariableDeclaration<QsLocalSymbol>> argTuple, QsScope body)
            {
                this.SharedState.Reset();
                try
                {
                    body = this.Transformation.Statements.OnScope(body);
                }
                catch (Exception ex)
                {
                    this.SharedState.OnException?.Invoke(ex);
                    this.SharedState.Success = false;
                }
                return new Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope>(argTuple, body);
            }
        }
    }
}
