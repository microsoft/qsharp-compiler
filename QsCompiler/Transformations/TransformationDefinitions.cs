// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations
{
    // syntax tree transformations

    public class SyntaxTreeTransformation<S> :
        Core.SyntaxTreeTransformation
        where S : Core.ScopeTransformation
    {
        public readonly S _Scope;
        public override Core.ScopeTransformation Scope => this._Scope ?? base.Scope;

        public SyntaxTreeTransformation(S scope) :
            base() =>
            this._Scope = scope;
    }


    // scope transformations

    /// Base class for all StatementKindTransformations
    public class StatementKindTransformation<S> :
        Core.StatementKindTransformation
        where S : Core.ScopeTransformation
    {
        public readonly S _Scope;

        public StatementKindTransformation(S scope) :
            base(true) =>
            this._Scope = scope ?? throw new ArgumentNullException(nameof(scope));

        public override QsScope ScopeTransformation(QsScope value) =>
            this._Scope.Transform(value);

        public override TypedExpression ExpressionTransformation(TypedExpression value) =>
            this._Scope.Expression.Transform(value);

        public override ResolvedType TypeTransformation(ResolvedType value) =>
            this._Scope.Expression.Type.Transform(value);

        public override QsNullable<QsLocation> LocationTransformation(QsNullable<QsLocation> value) =>
            this._Scope.onLocation(value);
    }

    /// Base class for all ScopeTransformations.
    public class ScopeTransformation<K, E> : 
        Core.ScopeTransformation
        where K : Core.StatementKindTransformation
        where E : Core.ExpressionTransformation
    {
        public readonly K _StatementKind;
        private readonly Core.StatementKindTransformation DefaultStatementKind;
        public override Core.StatementKindTransformation StatementKind => _StatementKind ?? DefaultStatementKind;

        public readonly E _Expression;
        private readonly Core.ExpressionTransformation DefaultExpression;
        public override Core.ExpressionTransformation Expression => _Expression ?? DefaultExpression;

        public ScopeTransformation(Func<ScopeTransformation<K, E>, K> statementKind, E expression) :
            base(expression != null) // default kind transformations are enabled only if there are expression transformations
        {
            this.DefaultStatementKind = base.StatementKind;
            this._StatementKind = statementKind?.Invoke(this);

            this.DefaultExpression = new NoExpressionTransformations(); // disable by default
            this._Expression = expression;
        }
    }

    /// Given an expression transformation, Transform applies the given transformation to all expressions in a scope. 
    public class ScopeTransformation<E> : 
        ScopeTransformation<Core.StatementKindTransformation, E>
        where E : Core.ExpressionTransformation
    {
        public ScopeTransformation(E expression) : 
            base(null, expression) { }
    }

    /// Does not do any transformations, and can be use as no-op if a ScopeTransformation is required as argument. 
    public class NoScopeTransformations : 
        ScopeTransformation<Core.ExpressionTransformation>
    {
        public NoScopeTransformations() : 
            base(null) { }

        public override QsScope Transform(QsScope scope) => scope;
    }


    // expression transformations

    /// Base class for all ExpressionTypeTransformations
    public class ExpressionTypeTransformation<E> :
        Core.ExpressionTypeTransformation
        where E : Core.ExpressionTransformation
    {
        public readonly E _Expression;

        public ExpressionTypeTransformation(E expression) :
            base(true) =>
            this._Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// Base class for all ExpressionKindTransformations
    public class ExpressionKindTransformation<E> :
        Core.ExpressionKindTransformation
        where E : Core.ExpressionTransformation
    {
        public readonly E _Expression;

        public ExpressionKindTransformation(E expression) :
            base(true) =>
            this._Expression = expression ?? throw new ArgumentNullException(nameof(expression));

        public override TypedExpression ExpressionTransformation(TypedExpression value) =>
            this._Expression.Transform(value);

        public override ResolvedType TypeTransformation(ResolvedType value) =>
            this._Expression.Type.Transform(value);
    }

    /// Base class for all ExpressionTransformations.
    public class ExpressionTransformation<K, T> : 
        Core.ExpressionTransformation
        where K : Core.ExpressionKindTransformation
        where T : Core.ExpressionTypeTransformation
    {
        public readonly K _Kind;
        private readonly Core.ExpressionKindTransformation DefaultKind;
        public override Core.ExpressionKindTransformation Kind => _Kind ?? DefaultKind;

        public readonly T _Type;
        private readonly Core.ExpressionTypeTransformation DefaultType;
        public override Core.ExpressionTypeTransformation Type => _Type ?? DefaultType;

        public ExpressionTransformation(Func<ExpressionTransformation<K, T>, K> kind, Func<ExpressionTransformation<K, T>, T> type) : 
            base(false) // disable transformations by default
        {
            this.DefaultKind = base.Kind;
            this._Kind = kind?.Invoke(this);

            this.DefaultType = new Core.ExpressionTypeTransformation(false); // disabled by default
            this._Type = type?.Invoke(this);
        }
    }

    /// Given an expression kind transformation, Transform applies the given transformation to the Kind of every expression. 
    public class ExpressionTransformation<K> :
        ExpressionTransformation<K, Core.ExpressionTypeTransformation>
        where K : Core.ExpressionKindTransformation
    {
        public ExpressionTransformation(Func<ExpressionTransformation<K, Core.ExpressionTypeTransformation>, K> kind) : 
            base(kind, null) { }
    }

    /// ExpressionTransformation where expression kind transformations are set to their default -
    /// i.e. subexpressions are walked, but no transformation is done on the kind itself.
    public class DefaultExpressionTransformation :
        ExpressionTransformation<ExpressionKindTransformation<DefaultExpressionTransformation>>
    {
        public DefaultExpressionTransformation() :
            base(e => new ExpressionKindTransformation<DefaultExpressionTransformation>(e as DefaultExpressionTransformation))
        { }
    }

    /// Disables all expression transformations, and can be use as no-op if an ExpressionTransformation is required as argument. 
    public class NoExpressionTransformations : 
        ExpressionTransformation<Core.ExpressionKindTransformation>
    {
        public NoExpressionTransformations() : 
            base(null) { }

        public override TypedExpression Transform(TypedExpression ex) => ex;
    }
}

