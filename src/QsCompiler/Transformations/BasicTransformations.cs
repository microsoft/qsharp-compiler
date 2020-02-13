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



    public class __SelectByFoldingOverExpressions__ :
        QsSyntaxTreeTransformation<__SelectByFoldingOverExpressions__.TransformationState>
    {
        public class TransformationState : FoldingState<bool>
        {
            private readonly Func<TypedExpression, bool> Condition;
            private readonly Func<bool, bool, bool> ConstructFold;
            private readonly bool Seed;

            public bool SatisfiesCondition => this.FoldResult;

            public TransformationState(Func<TypedExpression, bool> condition, Func<bool, bool, bool> fold, bool seed, bool recur = true)
                : base((ex, current) => fold(condition(ex), current), seed, recur: recur)
            { 
                this.Condition = condition ?? throw new ArgumentNullException(nameof(condition));
                this.ConstructFold = fold ?? throw new ArgumentNullException(nameof(condition));
                this.Seed = seed;
            }
        }


        public __SelectByFoldingOverExpressions__(Func<TypedExpression, bool> condition, Func<bool, bool, bool> fold, bool seed, bool evaluateOnSubexpressions = true)
            : base(new TransformationState(condition, fold, seed, evaluateOnSubexpressions))
        { 
        
        }

        public override Core.ExpressionTransformation<TransformationState> NewExpressionTransformation() =>
            new __FoldOverExpressions__<TransformationState, bool>(this); 
    }

    /// <summary>
    /// Class that allows to transform scopes by keeping only statements whose expressions satisfy a certain criterion. 
    /// Calling Transform will build a new Scope that contains only the statements for which the fold of a given condition 
    /// over all contained expressions evaluates to true. 
    /// If evaluateOnSubexpressions is set to true, the fold is evaluated on all subexpressions as well. 
    /// </summary>
    public class SelectByFoldingOverExpressions<K> :
        ScopeTransformation<K, FoldOverExpressions<bool>>
        where K : Core.StatementKindTransformation
    {

        protected SelectByFoldingOverExpressions<K> SubSelector;
        protected virtual SelectByFoldingOverExpressions<K> GetSubSelector() =>
            new SelectByFoldingOverExpressions<K>(this.Condition, this.Fold, this.Seed, this._Expression.recur, null);

        protected new Core.StatementKindTransformation _StatementKind => base.StatementKind; 
        public override Core.StatementKindTransformation StatementKind
        {
            get
            {
                this.SubSelector = GetSubSelector();
                return this.SubSelector._StatementKind; // don't spawn the next one
            }
        }

        public override QsStatement onStatement(QsStatement stm)
        {
            stm = base.onStatement(stm);
            this._Expression.Result = this.Fold(this._Expression.Result, this.SubSelector._Expression.Result);
            return stm;
        }

        public override QsScope Transform(QsScope scope)
        {
            var statements = new List<QsStatement>();
            foreach (var statement in scope.Statements)
            {
                // StatementKind.Transform sets a new Subselector that walks all expressions contained in statement, 
                // and sets its satisfiesCondition to true if one of them satisfies the condition given on initialization
                var transformed = this.onStatement(statement);
                if (this.SubSelector.SatisfiesCondition) statements.Add(transformed);
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
    public class SelectByAnyContainedExpression<K> :
        SelectByFoldingOverExpressions<K>
        where K : Core.StatementKindTransformation
    {
        private readonly Func<SelectByFoldingOverExpressions<K>, K> GetStatementKind;
        protected override SelectByFoldingOverExpressions<K> GetSubSelector() =>
            new SelectByAnyContainedExpression<K>(this.Condition, this._Expression.recur, this.GetStatementKind);

        public SelectByAnyContainedExpression(
            Func<TypedExpression, bool> condition, bool evaluateOnSubexpressions = true,
            Func<SelectByFoldingOverExpressions<K>, K> statementKind = null) :
                base(condition, (a, b) => a || b, false, evaluateOnSubexpressions, s => statementKind(s as SelectByFoldingOverExpressions<K>)) =>
                this.GetStatementKind = statementKind;
    }

    /// <summary>
    /// Class that allows to transform scopes by keeping only statements whose expressions satisfy a certain criterion. 
    /// Calling Transform will build a new Scope that contains only the statements 
    /// for which all contained expressions or subexpressions satisfy the condition given on initialization.
    /// Note that subexpressions will only be verified if evaluateOnSubexpressions is set to true (default value).
    /// </summary>
    public class SelectByAllContainedExpressions<K> :
        SelectByFoldingOverExpressions<K>
        where K : Core.StatementKindTransformation
    {
        private readonly Func<SelectByFoldingOverExpressions<K>, K> GetStatementKind;
        protected override SelectByFoldingOverExpressions<K> GetSubSelector() =>
            new SelectByAllContainedExpressions<K>(this.Condition, this._Expression.recur, this.GetStatementKind);

        public SelectByAllContainedExpressions(
            Func<TypedExpression, bool> condition, bool evaluateOnSubexpressions = true,
            Func<SelectByFoldingOverExpressions<K>, K> statementKind = null) :
                base(condition, (a, b) => a && b, true, evaluateOnSubexpressions, s => statementKind(s as SelectByFoldingOverExpressions<K>)) =>
                this.GetStatementKind = statementKind;
    }


    /// <summary>
    /// ... 
    /// </summary>
    public class FoldingState<T> // FIXME: MAKE THIS AN INTERFACE within FoldOverExpressions
    {
        public readonly bool Recur; // FIXME: REMOVE THAT
        public readonly Func<TypedExpression, T, T> Fold;
        public T FoldResult { get; set; }

        public FoldingState(Func<TypedExpression, T, T> fold, T seed, bool recur = true)
        {
            this.Recur = recur;
            this.Fold = fold ?? throw new ArgumentNullException(nameof(fold));
            this.FoldResult = seed;
        }
    }

    /// <summary>
    /// Class that evaluates a fold on upon transforming an expression. 
    /// If recur is set to true in the internal state of the transformation, 
    /// the fold function given on initialization is applied to all subexpressions as well as the expression itself -
    /// i.e. the fold it take starting on inner expressions (from the inside out). 
    /// Otherwise the specified folder is only applied to the expression itself. 
    /// The result of the fold is accessible via the FoldResult property in the internal state of the transformation. 
    /// </summary>
    public class __FoldOverExpressions__<T, S> :
        Core.ExpressionTransformation<T>
        where T : FoldingState<S>
    {
        public __FoldOverExpressions__(QsSyntaxTreeTransformation<T> parent)
            : base(parent)
        { }

        public __FoldOverExpressions__(T state)
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

