// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations.Conjugations
{
    //public class InlineConjugations :
    //    SyntaxTreeTransformation<InlineConjugateStatements>
    //{
    //    public InlineConjugations() :
    //        base(new InlineConjugateStatements())
    //    { }
    //
    //    private static readonly Func<QsScope, QsScope> SkipConjugates =
    //        new SkipConjugateStatements().Transform;
    //
    //    private QsSpecialization TransformFunctionSpecialization(QsSpecialization spec) =>
    //        spec.Implementation is SpecializationImplementation.Provided impl
    //            ? spec.WithImplementation(SpecializationImplementation.NewProvided(impl.Item1, SkipConjugates(impl.Item2)))
    //            : spec;
    //
    //    public override QsCallable onFunction(QsCallable c) => 
    //        c.WithSpecializations(specs => specs.Select(TransformFunctionSpecialization).ToImmutableArray());
    //
    //    public override QsCallable onTypeConstructor(QsCallable c) =>
    //        c.WithSpecializations(specs => specs.Select(TransformFunctionSpecialization).ToImmutableArray());
    //}

    // FIXME: WE NEED TO RESOLVE TO UNIQUE VARIABLE NAMES!

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
                    // FIXME: WE NEED TO RESOLVE TO UNIQUE VARIABLE NAMES!

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
    /// Applying the transformation generates a new scope without any conjugate-statements. 
    /// Conjugate-statements will be silently eliminated without replacement or further verification. 
    /// </summary>
    public class SkipConjugateStatements :
        ScopeTransformation<StatementKindTransformation<SkipConjugateStatements>, NoExpressionTransformations>
    {
        public SkipConjugateStatements() :
            base(s => new StatementKindTransformation<SkipConjugateStatements>(s as SkipConjugateStatements), new NoExpressionTransformations())
        { }

        public static Func<QsScope, QsScope> Apply =
            new SkipConjugateStatements().Transform;

        public override QsScope Transform(QsScope scope) =>
            base.Transform(new QsScope(scope.Statements.Where(s => !s.Statement.IsQsConjugateStatement).ToImmutableArray(), scope.KnownSymbols));
    }
}


