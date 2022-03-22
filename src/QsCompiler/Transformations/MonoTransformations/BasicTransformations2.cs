// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations
{
    public class GetSourceFiles
    : MonoTransformation
    {
        private HashSet<string> SourceFiles { get; } = new HashSet<string>();

        private GetSourceFiles()
        : base(TransformationOptions.NoRebuild)
        {
        }

        // static methods for convenience

        /// <summary>
        /// Returns a hash set containing all source files in the given namespaces.
        /// </summary>
        public static ImmutableHashSet<string> Apply(IEnumerable<QsNamespace> namespaces)
        {
            var filter = new GetSourceFiles();
            foreach (var ns in namespaces)
            {
                filter.OnNamespace(ns);
            }

            return filter.SourceFiles.ToImmutableHashSet();
        }

        /// <summary>
        /// Returns a hash set containing all source files in the given namespace(s).
        /// </summary>
        public static ImmutableHashSet<string> Apply(params QsNamespace[] namespaces) =>
            Apply((IEnumerable<QsNamespace>)namespaces);

        /* overrides */

        public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec) // short cut to avoid further evaluation
        {
            this.OnSource(spec.Source);
            return spec;
        }

        public override Source OnSource(Source source)
        {
            this.SourceFiles.Add(source.AssemblyOrCodeFile);
            return base.OnSource(source);
        }
    }

    /// <summary>
    /// Calling Transform on a syntax tree returns a new tree that only contains the type and callable declarations
    /// that are defined in the source file with the identifier given upon initialization.
    /// The transformation also ensures that the elements in each namespace are ordered according to
    /// the location at which they are defined in the file. Auto-generated declarations will be ordered alphabetically.
    /// </summary>
    public class FilterBySourceFile
    : MonoTransformation
    {
        private Func<string, bool> Predicate { get; }

        private List<(int?, QsNamespaceElement)> Elements { get; } =
            new List<(int?, QsNamespaceElement)>();

        public FilterBySourceFile(Func<string, bool> predicate)
        : base()
        {
            this.Predicate = predicate;
        }

        /* static methods for convenience */

        public static QsNamespace Apply(QsNamespace ns, Func<string, bool> predicate)
        {
            var filter = new FilterBySourceFile(predicate);
            return filter.OnNamespace(ns);
        }

        public static QsNamespace Apply(QsNamespace ns, params string[] fileIds)
        {
            var sourcesToKeep = fileIds.ToImmutableHashSet();
            return Apply(ns, sourcesToKeep.Contains);
        }

        /* overrides */

        // TODO: these overrides needs to be adapted once we support external specializations

        /// <inheritdoc/>
        public override QsCustomType OnTypeDeclaration(QsCustomType t)
        {
            if (this.Predicate(t.Source.AssemblyOrCodeFile))
            {
                this.Elements.Add((t.Location.IsValue ? t.Location.Item.Offset.Line : (int?)null, QsNamespaceElement.NewQsCustomType(t)));
            }

            return t;
        }

        /// <inheritdoc/>
        public override QsCallable OnCallableDeclaration(QsCallable c)
        {
            if (this.Predicate(c.Source.AssemblyOrCodeFile))
            {
                this.Elements.Add((c.Location.IsValue ? c.Location.Item.Offset.Line : (int?)null, QsNamespaceElement.NewQsCallable(c)));
            }

            return c;
        }

        /// <inheritdoc/>
        public override QsNamespace OnNamespace(QsNamespace ns)
        {
            static int SortComparison((int?, QsNamespaceElement) x, (int?, QsNamespaceElement) y)
            {
                if (x.Item1.HasValue && y.Item1.HasValue)
                {
                    return Comparer<int>.Default.Compare(x.Item1.Value, y.Item1.Value);
                }

                if (!x.Item1.HasValue && !y.Item1.HasValue)
                {
                    return Comparer<string>.Default.Compare(x.Item2.GetFullName().ToString(), y.Item2.GetFullName().ToString());
                }

                return x.Item1.HasValue ? -1 : 1;
            }

            this.Elements.Clear();
            base.OnNamespace(ns);
            this.Elements.Sort(SortComparison);
            return new QsNamespace(ns.Name, this.Elements.Select(e => e.Item2).ToImmutableArray(), ns.Documentation);
        }
    }

    /// <summary>
    /// Class that allows to transform scopes by keeping only statements whose expressions satisfy a certain criterion.
    /// Calling Transform will build a new Scope that contains only the statements for which the fold of a given condition
    /// over all contained expressions evaluates to true.
    /// If evaluateOnSubexpressions is set to true, the fold is evaluated on all subexpressions as well.
    /// </summary>
    public class SelectByFoldingOverExpressions
    : MonoTransformation
    {
        public class TransformationState
        : FoldOverExpressions<TransformationState, bool>.IFoldingState
        {
            /// <inheritdoc/>
            public bool Recur { get; }

            public bool Seed { get; }

            internal Func<TypedExpression, bool> Condition { get; }

            internal Func<bool, bool, bool> ConstructFold { get; }

            /// <inheritdoc/>
            public bool Fold(TypedExpression ex, bool current) =>
                this.ConstructFold(this.Condition(ex), current);

            /// <inheritdoc/>
            public bool FoldResult { get; set; }

            public bool SatisfiesCondition => this.FoldResult;

            public TransformationState(Func<TypedExpression, bool> condition, Func<bool, bool, bool> fold, bool seed, bool recur = true)
            {
                this.Recur = recur;
                this.Seed = seed;
                this.FoldResult = seed;
                this.Condition = condition;
                this.ConstructFold = fold;
            }
        }

        public SelectByFoldingOverExpressions(Func<TypedExpression, bool> condition, Func<bool, bool, bool> fold, bool seed, bool evaluateOnSubexpressions = true)
        : base()
        {
            this.SharedState = new TransformationState(condition, fold, seed, evaluateOnSubexpressions);

            this.CreateSelector = state => new SelectByFoldingOverExpressions(state.Condition, state.ConstructFold, state.Seed, state.Recur);
        }

        public TransformationState SharedState { get; private set; }

        /// <inheritdoc/>
        public override TypedExpression OnTypedExpression(TypedExpression ex)
        {
            ex = this.SharedState.Recur ? base.OnTypedExpression(ex) : ex;
            this.SharedState.FoldResult = this.SharedState.Fold(ex, this.SharedState.FoldResult);
            return ex;
        }

        /* Statement Transformation */

        protected SelectByFoldingOverExpressions? SubSelector { get; set; }

        protected Func<TransformationState, SelectByFoldingOverExpressions> CreateSelector { get; }

        /// <inheritdoc/>
        public override QsStatement OnStatement(QsStatement stm)
        {
            this.SubSelector = this.CreateSelector(this.SharedState);
            var loc = this.SubSelector.OnRelativeLocation(stm.Location);
            var stmKind = this.SubSelector.OnStatementKind(stm.Statement);
            var varDecl = this.SubSelector.OnLocalDeclarations(stm.SymbolDeclarations);
            this.SharedState.FoldResult = this.SharedState.ConstructFold(
                this.SharedState.FoldResult, this.SubSelector.SharedState.FoldResult);
            return new QsStatement(stmKind, varDecl, loc, stm.Comments);
        }

        /// <inheritdoc/>
        public override QsScope OnScope(QsScope scope)
        {
            var statements = new List<QsStatement>();
            foreach (var statement in scope.Statements)
            {
                // StatementKind.Transform sets a new Subselector that walks all expressions contained in statement,
                // and sets its satisfiesCondition to true if one of them satisfies the condition given on initialization
                var transformed = this.OnStatement(statement);
                if (this.SubSelector?.SharedState.SatisfiesCondition ?? false)
                {
                    statements.Add(transformed);
                }
            }

            return new QsScope(statements.ToImmutableArray(), scope.KnownSymbols);
        }
    }

    /// <summary>
    /// Class that allows to transform scopes by keeping only statements that contain certain expressions.
    /// Calling Transform will build a new Scope that contains only the statements
    /// which contain an expression or subexpression (only if evaluateOnSubexpressions is set to true)
    /// that satisfies the condition given on initialization.
    /// </summary>
    public class SelectByAnyContainedExpression
    : SelectByFoldingOverExpressions
    {
        public SelectByAnyContainedExpression(Func<TypedExpression, bool> condition, bool evaluateOnSubexpressions = true)
        : base(condition, (a, b) => a || b, false, evaluateOnSubexpressions)
        {
        }
    }

    /// <summary>
    /// Class that allows to transform scopes by keeping only statements whose expressions satisfy a certain criterion.
    /// Calling Transform will build a new Scope that contains only the statements
    /// for which all contained expressions or subexpressions satisfy the condition given on initialization.
    /// Note that subexpressions will only be verified if evaluateOnSubexpressions is set to true (default value).
    /// </summary>
    public class SelectByAllContainedExpressions
    : SelectByFoldingOverExpressions
    {
        public SelectByAllContainedExpressions(Func<TypedExpression, bool> condition, bool evaluateOnSubexpressions = true)
        : base(condition, (a, b) => a && b, true, evaluateOnSubexpressions)
        {
        }
    }

    /// <summary>
    /// Class that evaluates a fold on upon transforming an expression.
    /// If recur is set to true in the internal state of the transformation,
    /// the fold function given on initialization is applied to all subexpressions as well as the expression itself -
    /// i.e. the fold it take starting on inner expressions (from the inside out).
    /// Otherwise the specified folder is only applied to the expression itself.
    /// The result of the fold is accessible via the FoldResult property in the internal state of the transformation.
    /// The transformation itself merely walks expressions and rebuilding is disabled.
    /// </summary>
    public class FoldOverExpressions<TState, TResult>
    : MonoTransformation
        where TState : FoldOverExpressions<TState, TResult>.IFoldingState
    {
        public interface IFoldingState
        {
            public bool Recur { get; }

            public TResult Fold(TypedExpression ex, TResult current);

            public TResult FoldResult { get; set; }
        }

        public TState SharedState { get; private set; }

        public FoldOverExpressions(TState state)
        : base(TransformationOptions.NoRebuild)
        {
            this.SharedState = state;
        }

        /// <inheritdoc/>
        public override TypedExpression OnTypedExpression(TypedExpression ex)
        {
            ex = this.SharedState.Recur ? base.OnTypedExpression(ex) : ex;
            this.SharedState.FoldResult = this.SharedState.Fold(ex, this.SharedState.FoldResult);
            return ex;
        }
    }

    /// <summary>
    /// Adds the given variable declarations to the list of defined variables for each scope.
    /// </summary>
    internal class AddVariableDeclarations<T>
    : MonoTransformation
    {
        private readonly IEnumerable<LocalVariableDeclaration<string, ResolvedType>> addedVariableDeclarations;

        public AddVariableDeclarations(params LocalVariableDeclaration<string, ResolvedType>[] addedVars)
        : base() =>
            this.addedVariableDeclarations = addedVars;

        /// <inheritdoc/>
        public override LocalDeclarations OnLocalDeclarations(LocalDeclarations decl) =>
            base.OnLocalDeclarations(new LocalDeclarations(decl.Variables.AddRange(this.addedVariableDeclarations)));
    }
}
