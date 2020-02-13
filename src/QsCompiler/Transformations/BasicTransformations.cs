// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;


namespace Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations
{
    public class GetSourceFiles :
        QsSyntaxTreeTransformation<GetSourceFiles.TransformationState>
    {
        public class TransformationState
        {
            internal readonly HashSet<NonNullable<string>> SourceFiles = 
                new HashSet<NonNullable<string>>();
        }


        private GetSourceFiles() :
            base(new TransformationState())
        { }

        public override NamespaceTransformation<TransformationState> NewNamespaceTransformation() =>
            new NamespaceTransformation(this);


        // static methods for convenience

        /// <summary>
        /// Returns a hash set containing all source files in the given namespaces.
        /// Throws an ArgumentNullException if the given sequence or any of the given namespaces is null. 
        /// </summary>
        public static ImmutableHashSet<NonNullable<string>> Apply(IEnumerable<QsNamespace> namespaces)
        {
            if (namespaces == null || namespaces.Contains(null)) throw new ArgumentNullException(nameof(namespaces));
            var filter = new GetSourceFiles();
            foreach (var ns in namespaces) filter.Namespaces.Transform(ns);
            return filter.InternalState.SourceFiles.ToImmutableHashSet();
        }

        /// <summary>
        /// Returns a hash set containing all source files in the given namespace(s).
        /// Throws an ArgumentNullException if any of the given namespaces is null. 
        /// </summary>
        public static ImmutableHashSet<NonNullable<string>> Apply(params QsNamespace[] namespaces) =>
            Apply((IEnumerable<QsNamespace>)namespaces);


        // helper classes

        private class NamespaceTransformation :
            NamespaceTransformation<TransformationState>
        {

            public NamespaceTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            public override QsSpecialization onSpecializationImplementation(QsSpecialization spec) // short cut to avoid further evaluation
            {
                this.onSourceFile(spec.SourceFile);
                return spec;
            }

            public override NonNullable<string> onSourceFile(NonNullable<string> f)
            {
                this.Transformation.InternalState.SourceFiles.Add(f);
                return base.onSourceFile(f);
            }
        }
    }


    /// <summary>
    /// Calling Transform on a syntax tree returns a new tree that only contains the type and callable declarations
    /// that are defined in the source file with the identifier given upon initialization. 
    /// The transformation also ensures that the elements in each namespace are ordered according to 
    /// the location at which they are defined in the file. Auto-generated declarations will be ordered alphabetically.
    /// </summary>
    public class FilterBySourceFile :
        QsSyntaxTreeTransformation<FilterBySourceFile.TransformationState>
    { 
        public class TransformationState 
        {
            internal readonly Func<NonNullable<string>, bool> Predicate;
            internal readonly List<(int?, QsNamespaceElement)> Elements =
                new List<(int?, QsNamespaceElement)>();

            public TransformationState(Func<NonNullable<string>, bool> predicate) => 
                this.Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }


        public FilterBySourceFile(Func<NonNullable<string>, bool> predicate)
            : base(new TransformationState(predicate))
        { }

        public override NamespaceTransformation<TransformationState> NewNamespaceTransformation() =>
            new NamespaceTransformation(this);


        // static methods for convenience

        public static QsNamespace Apply(QsNamespace ns, Func<NonNullable<string>, bool> predicate)
        {
            if (ns == null) throw new ArgumentNullException(nameof(ns));
            var filter = new FilterBySourceFile(predicate);
            return filter.Namespaces.Transform(ns);
        }

        public static QsNamespace Apply(QsNamespace ns, params NonNullable<string>[] fileIds)
        {
            var sourcesToKeep = fileIds.Select(f => f.Value).ToImmutableHashSet();
            return Apply(ns, s => sourcesToKeep.Contains(s.Value));
        }


        // helper classes 

        public class NamespaceTransformation : 
            NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            // TODO: these overrides needs to be adapted once we support external specializations

            public override QsCustomType onType(QsCustomType t)
            {
                if (this.Transformation.InternalState.Predicate(t.SourceFile))
                { this.Transformation.InternalState.Elements.Add((t.Location.IsValue ? t.Location.Item.Offset.Item1 : (int?)null, QsNamespaceElement.NewQsCustomType(t))); }
                return t;
            }

            public override QsCallable onCallableImplementation(QsCallable c) 
            {
                if (this.Transformation.InternalState.Predicate(c.SourceFile))
                { this.Transformation.InternalState.Elements.Add((c.Location.IsValue ? c.Location.Item.Offset.Item1 : (int?)null, QsNamespaceElement.NewQsCallable(c))); }
                return c;
            }

            public override QsNamespace Transform(QsNamespace ns)
            {
                static int SortComparison((int?, QsNamespaceElement) x, (int?, QsNamespaceElement) y)
                {
                    if (x.Item1.HasValue && y.Item1.HasValue) return Comparer<int>.Default.Compare(x.Item1.Value, y.Item1.Value);
                    if (!x.Item1.HasValue && !y.Item1.HasValue) return Comparer<string>.Default.Compare(x.Item2.GetFullName().ToString(), y.Item2.GetFullName().ToString());
                    return x.Item1.HasValue ? -1 : 1;
                }
                this.Transformation.InternalState.Elements.Clear();
                base.Transform(ns);
                this.Transformation.InternalState.Elements.Sort(SortComparison);
                return new QsNamespace(ns.Name, this.Transformation.InternalState.Elements.Select(e => e.Item2).ToImmutableArray(), ns.Documentation);
            }
        }
    }


    /// <summary>
    /// Class that allows to transform scopes by keeping only statements whose expressions satisfy a certain criterion. 
    /// Calling Transform will build a new Scope that contains only the statements for which the fold of a given condition 
    /// over all contained expressions evaluates to true. 
    /// If evaluateOnSubexpressions is set to true, the fold is evaluated on all subexpressions as well. 
    /// </summary>
    public class SelectByFoldingOverExpressions :
        QsSyntaxTreeTransformation<SelectByFoldingOverExpressions.TransformationState>
    {
        public class TransformationState : FoldOverExpressions<TransformationState, bool>.IFoldingState
        {
            public bool Recur { get; }
            public readonly bool Seed;

            internal readonly Func<TypedExpression, bool> Condition;
            internal readonly Func<bool, bool, bool> ConstructFold;

            public bool Fold(TypedExpression ex, bool current) =>
                this.ConstructFold(this.Condition(ex), current);

            public bool FoldResult { get; set; }
            public bool SatisfiesCondition => this.FoldResult;

            public TransformationState(Func<TypedExpression, bool> condition, Func<bool, bool, bool> fold, bool seed, bool recur = true)
            {
                this.Recur = recur;
                this.Seed = seed;
                this.FoldResult = seed;
                this.Condition = condition ?? throw new ArgumentNullException(nameof(condition));
                this.ConstructFold = fold ?? throw new ArgumentNullException(nameof(fold));
            }
        }


        public SelectByFoldingOverExpressions(Func<TypedExpression, bool> condition, Func<bool, bool, bool> fold, bool seed, bool evaluateOnSubexpressions = true)
            : base(new TransformationState(condition, fold, seed, evaluateOnSubexpressions))
        { }

        public override Core.ExpressionTransformation<TransformationState> NewExpressionTransformation() =>
            new FoldOverExpressions<TransformationState, bool>(this);

        public override Core.StatementTransformation<TransformationState> NewStatementTransformation() =>
            new StatementTransformation<SelectByFoldingOverExpressions>(
                state => new SelectByFoldingOverExpressions(state.Condition, state.ConstructFold, state.Seed, state.Recur), 
                this);


        // helper classes

        public class StatementTransformation<P> : 
            Core.StatementTransformation<TransformationState>
            where P : SelectByFoldingOverExpressions
        {
            protected P SubSelector;
            protected readonly Func<TransformationState, P> CreateSelector;

            /// <summary>
            /// The given function for creating a new subselector is expected to initialize a new internal state with the same configurations as the one given upon construction.
            /// Upon initialization, the FoldResult of the internal state should be set to the specified seed rather than the FoldResult of the given constructor argument. 
            /// </summary>
            public StatementTransformation(Func<TransformationState, P> createSelector, QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent) =>
                this.CreateSelector = createSelector ?? throw new ArgumentNullException(nameof(createSelector));

            public override QsStatement onStatement(QsStatement stm)
            {
                this.SubSelector = this.CreateSelector(this.Transformation.InternalState);
                var loc = this.SubSelector.Statements.onLocation(stm.Location);
                var stmKind = this.SubSelector.StatementKinds.Transform(stm.Statement);
                var varDecl = this.SubSelector.Statements.onLocalDeclarations(stm.SymbolDeclarations);
                this.Transformation.InternalState.FoldResult = this.Transformation.InternalState.ConstructFold(
                    this.Transformation.InternalState.FoldResult, this.SubSelector.InternalState.FoldResult);
                return new QsStatement(stmKind, varDecl, loc, stm.Comments);
            }

            public override QsScope Transform(QsScope scope)
            {
                var statements = new List<QsStatement>();
                foreach (var statement in scope.Statements)
                {
                    // StatementKind.Transform sets a new Subselector that walks all expressions contained in statement, 
                    // and sets its satisfiesCondition to true if one of them satisfies the condition given on initialization
                    var transformed = this.onStatement(statement);
                    if (this.SubSelector.InternalState.SatisfiesCondition) statements.Add(transformed);
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
    public class SelectByAnyContainedExpression :
        SelectByFoldingOverExpressions
    {
        public SelectByAnyContainedExpression(Func<TypedExpression, bool> condition, bool evaluateOnSubexpressions = true)
            : base(condition, (a, b) => a || b, false, evaluateOnSubexpressions)
        { }
    }

    /// <summary>
    /// Class that allows to transform scopes by keeping only statements whose expressions satisfy a certain criterion. 
    /// Calling Transform will build a new Scope that contains only the statements 
    /// for which all contained expressions or subexpressions satisfy the condition given on initialization.
    /// Note that subexpressions will only be verified if evaluateOnSubexpressions is set to true (default value).
    /// </summary>
    public class SelectByAllContainedExpressions :
        SelectByFoldingOverExpressions
    {
        public SelectByAllContainedExpressions(Func<TypedExpression, bool> condition, bool evaluateOnSubexpressions = true)
            : base(condition, (a, b) => a && b, true, evaluateOnSubexpressions)
        { }
    }


    /// <summary>
    /// Class that evaluates a fold on upon transforming an expression. 
    /// If recur is set to true in the internal state of the transformation, 
    /// the fold function given on initialization is applied to all subexpressions as well as the expression itself -
    /// i.e. the fold it take starting on inner expressions (from the inside out). 
    /// Otherwise the specified folder is only applied to the expression itself. 
    /// The result of the fold is accessible via the FoldResult property in the internal state of the transformation. 
    /// </summary>
    public class FoldOverExpressions<T, S> :
        Core.ExpressionTransformation<T>
        where T : FoldOverExpressions<T,S>.IFoldingState
    {
        public interface IFoldingState
        {
            public bool Recur { get; }
            public S Fold(TypedExpression ex, S current);
            public S FoldResult { get; set; }
        }


        public FoldOverExpressions(QsSyntaxTreeTransformation<T> parent)
            : base(parent)
        { }

        public FoldOverExpressions(T state)
            : base(state)
        { }


        public override TypedExpression Transform(TypedExpression ex)
        {
            ex = this.Transformation.InternalState.Recur ? base.Transform(ex) : ex;
            this.Transformation.InternalState.FoldResult = this.Transformation.InternalState.Fold(ex, this.Transformation.InternalState.FoldResult);
            return ex;
        }
    }
}

