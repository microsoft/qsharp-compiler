// Copyright (c) Microsoft Corporation. All rights reserved.
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
    : SyntaxTreeTransformation<GetSourceFiles.TransformationState>
    {
        public class TransformationState
        {
            internal readonly HashSet<string> SourceFiles = new HashSet<string>();
        }

        private GetSourceFiles()
        : base(new TransformationState(), TransformationOptions.NoRebuild)
        {
            this.Namespaces = new NamespaceTransformation(this);
            this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.Disabled);
            this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
            this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
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
                filter.Namespaces.OnNamespace(ns);
            }
            return filter.SharedState.SourceFiles.ToImmutableHashSet();
        }

        /// <summary>
        /// Returns a hash set containing all source files in the given namespace(s).
        /// </summary>
        public static ImmutableHashSet<string> Apply(params QsNamespace[] namespaces) =>
            Apply((IEnumerable<QsNamespace>)namespaces);

        // helper classes

        private class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec) // short cut to avoid further evaluation
            {
                this.OnSource(spec.Source);
                return spec;
            }

            public override Source OnSource(Source source)
            {
                this.SharedState.SourceFiles.Add(source.AssemblyOrCode);
                return base.OnSource(source);
            }
        }
    }

    /// <summary>
    /// Calling Transform on a syntax tree returns a new tree that only contains the type and callable declarations
    /// that are defined in the source file with the identifier given upon initialization.
    /// The transformation also ensures that the elements in each namespace are ordered according to
    /// the location at which they are defined in the file. Auto-generated declarations will be ordered alphabetically.
    /// </summary>
    public class FilterBySourceFile
    : SyntaxTreeTransformation<FilterBySourceFile.TransformationState>
    {
        public class TransformationState
        {
            internal readonly Func<string, bool> Predicate;
            internal readonly List<(int?, QsNamespaceElement)> Elements =
                new List<(int?, QsNamespaceElement)>();

            public TransformationState(Func<string, bool> predicate) =>
                this.Predicate = predicate;
        }

        public FilterBySourceFile(Func<string, bool> predicate)
        : base(new TransformationState(predicate))
        {
            this.Namespaces = new NamespaceTransformation(this);
            this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.Disabled);
            this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
            this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
        }

        // static methods for convenience

        public static QsNamespace Apply(QsNamespace ns, Func<string, bool> predicate)
        {
            var filter = new FilterBySourceFile(predicate);
            return filter.Namespaces.OnNamespace(ns);
        }

        public static QsNamespace Apply(QsNamespace ns, params string[] fileIds)
        {
            var sourcesToKeep = fileIds.ToImmutableHashSet();
            return Apply(ns, sourcesToKeep.Contains);
        }

        // helper classes

        public class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent)
            {
            }

            // TODO: these overrides needs to be adapted once we support external specializations

            /// <inheritdoc/>
            public override QsCustomType OnTypeDeclaration(QsCustomType t)
            {
                if (this.SharedState.Predicate(t.Source.AssemblyOrCode))
                {
                    this.SharedState.Elements.Add((t.Location.IsValue ? t.Location.Item.Offset.Line : (int?)null, QsNamespaceElement.NewQsCustomType(t)));
                }
                return t;
            }

            /// <inheritdoc/>
            public override QsCallable OnCallableDeclaration(QsCallable c)
            {
                if (this.SharedState.Predicate(c.Source.AssemblyOrCode))
                {
                    this.SharedState.Elements.Add((c.Location.IsValue ? c.Location.Item.Offset.Line : (int?)null, QsNamespaceElement.NewQsCallable(c)));
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
                this.SharedState.Elements.Clear();
                base.OnNamespace(ns);
                this.SharedState.Elements.Sort(SortComparison);
                return new QsNamespace(ns.Name, this.SharedState.Elements.Select(e => e.Item2).ToImmutableArray(), ns.Documentation);
            }
        }
    }

    /// <summary>
    /// Class that allows to transform scopes by keeping only statements whose expressions satisfy a certain criterion.
    /// Calling Transform will build a new Scope that contains only the statements for which the fold of a given condition
    /// over all contained expressions evaluates to true.
    /// If evaluateOnSubexpressions is set to true, the fold is evaluated on all subexpressions as well.
    /// </summary>
    public class SelectByFoldingOverExpressions
    : SyntaxTreeTransformation<SelectByFoldingOverExpressions.TransformationState>
    {
        public class TransformationState
        : FoldOverExpressions<TransformationState, bool>.IFoldingState
        {
            /// <inheritdoc/>
            public bool Recur { get; }

            public readonly bool Seed;

            internal readonly Func<TypedExpression, bool> Condition;
            internal readonly Func<bool, bool, bool> ConstructFold;

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
        : base(new TransformationState(condition, fold, seed, evaluateOnSubexpressions))
        {
            this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            this.Expressions = new FoldOverExpressions<TransformationState, bool>(this);
            this.Statements = new StatementTransformation<SelectByFoldingOverExpressions>(
                state => new SelectByFoldingOverExpressions(state.Condition, state.ConstructFold, state.Seed, state.Recur),
                this);
        }

        // helper classes

        public class StatementTransformation<TSelector>
        : Core.StatementTransformation<TransformationState> where TSelector : SelectByFoldingOverExpressions
        {
            protected TSelector? SubSelector;
            protected readonly Func<TransformationState, TSelector> CreateSelector;

            /// <summary>
            /// The given function for creating a new subselector is expected to initialize a new internal state with the same configurations as the one given upon construction.
            /// Upon initialization, the FoldResult of the internal state should be set to the specified seed rather than the FoldResult of the given constructor argument.
            /// </summary>
            public StatementTransformation(Func<TransformationState, TSelector> createSelector, SyntaxTreeTransformation<TransformationState> parent)
            : base(parent) =>
                this.CreateSelector = createSelector;

            /// <inheritdoc/>
            public override QsStatement OnStatement(QsStatement stm)
            {
                this.SubSelector = this.CreateSelector(this.SharedState);
                var loc = this.SubSelector.Statements.OnLocation(stm.Location);
                var stmKind = this.SubSelector.StatementKinds.OnStatementKind(stm.Statement);
                var varDecl = this.SubSelector.Statements.OnLocalDeclarations(stm.SymbolDeclarations);
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
    : ExpressionTransformation<TState> where TState : FoldOverExpressions<TState, TResult>.IFoldingState
    {
        public interface IFoldingState
        {
            public bool Recur { get; }

            public TResult Fold(TypedExpression ex, TResult current);

            public TResult FoldResult { get; set; }
        }

        public FoldOverExpressions(SyntaxTreeTransformation<TState> parent)
        : base(parent, TransformationOptions.NoRebuild)
        {
        }

        public FoldOverExpressions(TState state)
        : base(state)
        {
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
    /// Upon transformation, applies the specified action to each expression and subexpression.
    /// The action to apply is specified upon construction, and will be applied before recurring into subexpressions.
    /// The transformation merely walks expressions and rebuilding is disabled.
    /// </summary>
    public class TypedExpressionWalker<T>
    : ExpressionTransformation<T>
    {
        public TypedExpressionWalker(Action<TypedExpression> onExpression, SyntaxTreeTransformation<T> parent)
        : base(parent, TransformationOptions.NoRebuild) =>
            this.OnExpression = onExpression;

        public TypedExpressionWalker(Action<TypedExpression> onExpression, T internalState)
        : base(internalState, TransformationOptions.NoRebuild) =>
            this.OnExpression = onExpression;

        public readonly Action<TypedExpression> OnExpression;

        /// <inheritdoc/>
        public override TypedExpression OnTypedExpression(TypedExpression ex)
        {
            this.OnExpression(ex);
            return base.OnTypedExpression(ex);
        }
    }

    /// <summary>
    /// Adds the given variable declarations to the list of defined variables for each scope.
    /// </summary>
    internal class AddVariableDeclarations<T>
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
}
