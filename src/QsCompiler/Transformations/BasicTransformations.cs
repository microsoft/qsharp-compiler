// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations
{
    // syntax tree transformations

    public class GetSourceFiles :
        SyntaxTreeTransformation<NoScopeTransformations>
    {
        /// <summary>
        /// Returns a hash set containing all source files in the given namespace(s).
        /// Throws an ArgumentNullException if any of the given namespaces is null. 
        /// </summary>
        public static ImmutableHashSet<NonNullable<string>> Apply(params QsNamespace[] namespaces)
        {
            if (namespaces == null || namespaces.Contains(null)) throw new ArgumentNullException(nameof(namespaces));
            var filter = new GetSourceFiles();
            foreach(var ns in namespaces) filter.Transform(ns);
            return filter.SourceFiles.ToImmutableHashSet();
        }

        private readonly HashSet<NonNullable<string>> SourceFiles;
        private GetSourceFiles() :
            base(new NoScopeTransformations()) =>
            this.SourceFiles = new HashSet<NonNullable<string>>();

        public override QsSpecialization onSpecializationImplementation(QsSpecialization spec) // short cut to avoid further evaluation
        {
            this.onSourceFile(spec.SourceFile);
            return spec;
        }

        public override NonNullable<string> onSourceFile(NonNullable<string> f)
        {
            this.SourceFiles.Add(f);
            return base.onSourceFile(f);
        }
    }

    /// <summary>
    /// Calling Transform on a syntax tree returns a new tree that only contains the type and callable declarations
    /// that are defined in the source file with the identifier given upon initialization. 
    /// The transformation also ensures that the elements in each namespace are ordered according to 
    /// the location at which they are defined in the file. 
    /// </summary>
    public class FilterBySourceFile :
        SyntaxTreeTransformation<NoScopeTransformations>
    {
        public static QsNamespace Apply(QsNamespace ns, NonNullable<string> fileId)
        {
            if (ns == null) throw new ArgumentNullException(nameof(ns));
            var filter = new FilterBySourceFile(fileId);
            return filter.Transform(ns); 
        }

        private readonly List<(int, QsNamespaceElement)> Elements;
        private readonly Func<NonNullable<string>, bool> IsInSource;

        public FilterBySourceFile(NonNullable<string> fileId) :
            base(new NoScopeTransformations())
        {
            this.IsInSource = s => s.Value == fileId.Value;
            this.Elements = new List<(int, QsNamespaceElement)>();
        }

        private QsCallable AddCallableIfInSource(QsCallable c)
        {
            if (IsInSource(c.SourceFile))
            { Elements.Add((c.Location.Offset.Item1, QsNamespaceElement.NewQsCallable(c))); }
            return c;
        }

        private QsCustomType AddTypeIfInSource(QsCustomType t)
        {
            if (IsInSource(t.SourceFile))
            { Elements.Add((t.Location.Offset.Item1, QsNamespaceElement.NewQsCustomType(t))); }
            return t;
        }

        // TODO: these transformations needs to be adapted once we support external specializations
        public override QsCustomType onType(QsCustomType t) => AddTypeIfInSource(t);
        public override QsCallable onCallableImplementation(QsCallable c) => AddCallableIfInSource(c);

        public override QsNamespace Transform(QsNamespace ns)
        {
            this.Elements.Clear();
            base.Transform(ns);
            this.Elements.Sort((x, y) => x.Item1 - y.Item1);
            return new QsNamespace(ns.Name, this.Elements.Select(e => e.Item2).ToImmutableArray(), ns.Documentation);
        }
    }


    // scope transformations

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
        protected readonly Func<TypedExpression, bool> Condition;
        protected readonly Func<bool, bool, bool> Fold;
        private readonly bool Seed;

        protected SelectByFoldingOverExpressions<K> SubSelector;
        protected virtual SelectByFoldingOverExpressions<K> GetSubSelector() =>
            new SelectByFoldingOverExpressions<K>(this.Condition, this.Fold, this.Seed, this._Expression.recur, null);

        public bool SatisfiesCondition => this._Expression.Result;

        public SelectByFoldingOverExpressions(
            Func<TypedExpression, bool> condition, Func<bool, bool, bool> fold, bool seed, bool evaluateOnSubexpressions = true,
            Func<ScopeTransformation<K, FoldOverExpressions<bool>>, K> statementKind = null) :
            base(statementKind, new FoldOverExpressions<bool>((ex, current) => fold(condition(ex), current), seed, recur: evaluateOnSubexpressions))
        {
            this.Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            this.Fold = fold ?? throw new ArgumentNullException(nameof(condition));
            this.Seed = seed;
        }

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


    // expression transformations

    /// <summary>
    /// Class that evaluates a fold on Transform. 
    /// If recur is set to true on initialization (default value), 
    /// the fold function given on initialization is applied to all subexpressions as well as the expression itself -
    /// i.e. the fold it take starting on inner expressions (from the inside out). 
    /// Otherwise the set Action is only applied to the expression itself. 
    /// The result of the fold is accessible via the Result property. 
    /// </summary>
    public class FoldOverExpressions<T> :
        ExpressionTransformation<ExpressionKindTransformation<FoldOverExpressions<T>>>
    {
        private static readonly Func<
            ExpressionTransformation<ExpressionKindTransformation<FoldOverExpressions<T>>, Core.ExpressionTypeTransformation>, 
            ExpressionKindTransformation<FoldOverExpressions<T>>> InitializeKind =
            e => new ExpressionKindTransformation<FoldOverExpressions<T>>(e as FoldOverExpressions<T>);

        internal readonly bool recur;
        public readonly Func<TypedExpression, T, T> Fold; 
        public T Result { get; set; }

        public FoldOverExpressions(Func<TypedExpression, T, T> fold, T seed, bool recur = true) :
            base(recur ? InitializeKind : null) // we need to enable expression kind transformations in order to walk subexpressions
        {
            this.Fold = fold ?? throw new ArgumentNullException(nameof(fold));
            this.Result = seed;
            this.recur = recur;
        }

        public override TypedExpression Transform(TypedExpression ex)
        {
            ex = recur ? base.Transform(ex) : ex;
            this.Result = Fold(ex, this.Result);
            return ex;
        }
    }
}

