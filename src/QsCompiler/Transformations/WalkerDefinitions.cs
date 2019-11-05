// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations
{
    // syntax tree walkers

    public class SyntaxTreeWalker<S> :
        Core.SyntaxTreeWalker
        where S : Core.ScopeWalker
    {
        public readonly S _Scope;
        public override Core.ScopeWalker Scope => this._Scope ?? base.Scope;

        public SyntaxTreeWalker(S scope) :
            base() =>
            this._Scope = scope;
    }


    // scope walkers

    /// <summary>
    /// Base class for all StatementKindWalkers.
    /// </summary>
    public class StatementKindWalker<S> :
        Core.StatementKindWalker
        where S : Core.ScopeWalker
    {
        public readonly S _Scope;

        public StatementKindWalker(S scope) :
            base(true) =>
            this._Scope = scope ?? throw new ArgumentNullException(nameof(scope));

        public override void ScopeWalker(QsScope value) =>
            this._Scope.Walk(value);

        public override void ExpressionWalker(TypedExpression value) =>
            this._Scope.Expression.Walk(value);

        public override void TypeWalker(ResolvedType value) =>
            this._Scope.Expression.Type.Walk(value);

        public override void LocationWalker(QsNullable<QsLocation> value) =>
            this._Scope.onLocation(value);
    }

    /// <summary>
    /// Base class for all ScopeWalkers.
    /// </summary>
    public class ScopeWalker<K, E> :
        Core.ScopeWalker
        where K : Core.StatementKindWalker
        where E : Core.ExpressionWalker
    {
        public readonly K _StatementKind;
        private readonly Core.StatementKindWalker DefaultStatementKind;
        public override Core.StatementKindWalker StatementKind => _StatementKind ?? DefaultStatementKind;

        public readonly E _Expression;
        private readonly Core.ExpressionWalker DefaultExpression;
        public override Core.ExpressionWalker Expression => _Expression ?? DefaultExpression;

        public ScopeWalker(Func<ScopeWalker<K, E>, K> statementKind, E expression) :
            base(expression != null) // default kind Walkers are enabled only if there are expression Walkers
        {
            this.DefaultStatementKind = base.StatementKind;
            this._StatementKind = statementKind?.Invoke(this);

            this.DefaultExpression = new NoExpressionWalkers(); // disable by default
            this._Expression = expression;
        }
    }

    /// <summary>
    /// Given an expression Walker, Walk applies the given Walker to all expressions in a scope. 
    /// </summary>
    public class ScopeWalker<E> :
        ScopeWalker<Core.StatementKindWalker, E>
        where E : Core.ExpressionWalker
    {
        public ScopeWalker(E expression) :
            base(null, expression)
        { }
    }

    /// <summary>
    /// Does not do any Walkers, and can be use as no-op if a ScopeWalker is required as argument. 
    /// </summary>
    public class NoScopeWalkers :
        ScopeWalker<Core.ExpressionWalker>
    {
        public NoScopeWalkers() :
            base(null)
        { }

        public override void Walk(QsScope scope) {}
    }


    // expression Walkers

    /// <summary>
    /// Base class for all ExpressionTypeWalkers.
    /// </summary>
    public class ExpressionTypeWalker<E> :
        Core.ExpressionTypeWalker
        where E : Core.ExpressionWalker
    {
        public readonly E _Expression;

        public ExpressionTypeWalker(E expression) :
            base(true) =>
            this._Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <summary>
    /// Base class for all ExpressionKindWalkers.
    /// </summary>
    public class ExpressionKindWalker<E> :
        Core.ExpressionKindWalker
        where E : Core.ExpressionWalker
    {
        public readonly E _Expression;

        public ExpressionKindWalker(E expression) :
            base(true) =>
            this._Expression = expression ?? throw new ArgumentNullException(nameof(expression));

        public override void ExpressionWalker(TypedExpression value) =>
            this._Expression.Walk(value);

        public override void TypeWalker(ResolvedType value) =>
            this._Expression.Type.Walk(value);
    }

    /// <summary>
    /// Base class for all ExpressionWalkers.
    /// </summary>
    public class ExpressionWalker<K, T> :
        Core.ExpressionWalker
        where K : Core.ExpressionKindWalker
        where T : Core.ExpressionTypeWalker
    {
        public readonly K _Kind;
        private readonly Core.ExpressionKindWalker DefaultKind;
        public override Core.ExpressionKindWalker Kind => _Kind ?? DefaultKind;

        public readonly T _Type;
        private readonly Core.ExpressionTypeWalker DefaultType;
        public override Core.ExpressionTypeWalker Type => _Type ?? DefaultType;

        public ExpressionWalker(Func<ExpressionWalker<K, T>, K> kind, Func<ExpressionWalker<K, T>, T> type) :
            base(false) // disable Walkers by default
        {
            this.DefaultKind = base.Kind;
            this._Kind = kind?.Invoke(this);

            this.DefaultType = new Core.ExpressionTypeWalker(false); // disabled by default
            this._Type = type?.Invoke(this);
        }
    }

    /// <summary>
    /// Given an expression kind Walker, Walk applies the given Walker to the Kind of every expression. 
    /// </summary>
    public class ExpressionWalker<K> :
        ExpressionWalker<K, Core.ExpressionTypeWalker>
        where K : Core.ExpressionKindWalker
    {
        public ExpressionWalker(Func<ExpressionWalker<K, Core.ExpressionTypeWalker>, K> kind) :
            base(kind, null)
        { }
    }

    /// <summary>
    /// ExpressionWalker where expression kind Walkers are set to their default -
    /// i.e. subexpressions are walked, but no Walker is done on the kind itself.
    /// </summary>
    public class DefaultExpressionWalker :
        ExpressionWalker<ExpressionKindWalker<DefaultExpressionWalker>>
    {
        public DefaultExpressionWalker() :
            base(e => new ExpressionKindWalker<DefaultExpressionWalker>(e as DefaultExpressionWalker))
        { }
    }

    /// <summary>
    /// Disables all expression Walkers, and can be use as no-op if an ExpressionWalker is required as argument.
    /// </summary>
    public class NoExpressionWalkers :
        ExpressionWalker<Core.ExpressionKindWalker>
    {
        public NoExpressionWalkers() :
            base(null)
        { }

        public override void Walk(TypedExpression ex) { }
    }
}

