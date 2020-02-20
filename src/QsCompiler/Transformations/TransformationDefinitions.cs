// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations
{
    // syntax tree transformations

    public class SyntaxTreeTransformation<S> :
        Core.NamespaceTransformationBase
        where S : Core.StatementTransformationBase
    {
        public readonly S _Scope;

        public override StatementTransformationBase Statements => this._Scope ?? base.Statements;

        public SyntaxTreeTransformation(S scope) :
            base() =>
            this._Scope = scope;
    }


    // scope transformations

    /// <summary>
    /// Base class for all StatementKindTransformations.
    /// </summary>
    public class StatementKindTransformation<S> :
        Core.StatementKindTransformationBase
        where S : Core.StatementTransformationBase
    {
        public readonly S _Scope;

        public StatementKindTransformation(S scope) :
            base() =>
            this._Scope = scope ?? throw new ArgumentNullException(nameof(scope));

        public override StatementTransformationBase Statements => this._Scope;
        public override ExpressionTransformationBase Expressions => this._Scope.Expressions;
    }

    /// <summary>
    /// Base class for all ScopeTransformations.
    /// </summary>
    public class ScopeTransformation<K, E> : 
        Core.StatementTransformationBase
        where K : Core.StatementKindTransformationBase
        where E : Core.ExpressionTransformationBase
    {
        public readonly K _StatementKind;
        private readonly Core.StatementKindTransformationBase DefaultStatementKind;
        public override StatementKindTransformationBase StatementKinds => _StatementKind ?? DefaultStatementKind;

        public readonly E _Expression;
        private readonly Core.ExpressionTransformationBase DefaultExpression;
        public override ExpressionTransformationBase Expressions => _Expression ?? DefaultExpression;

        public ScopeTransformation(Func<ScopeTransformation<K, E>, K> statementKind, E expression) :
            base() // default kind transformations are enabled only if there are expression transformations
        {
            this.DefaultStatementKind = base.StatementKinds;
            this._StatementKind = statementKind?.Invoke(this);

            this.DefaultExpression = new NoExpressionTransformations(); // disable by default
            this._Expression = expression;
        }
    }

    /// <summary>
    /// Given an expression transformation, Transform applies the given transformation to all expressions in a scope. 
    /// </summary>
    public class ScopeTransformation<E> : 
        ScopeTransformation<Core.StatementKindTransformationBase, E>
        where E : Core.ExpressionTransformationBase
    {
        public ScopeTransformation(E expression) : 
            base(null, expression) { }
    }

    /// <summary>
    /// Does not do any transformations, and can be use as no-op if a ScopeTransformation is required as argument. 
    /// </summary>
    public class NoScopeTransformations : 
        ScopeTransformation<Core.ExpressionTransformationBase>
    {
        public NoScopeTransformations() : 
            base(null) { }

        public override QsScope Transform(QsScope scope) => scope;
    }


    // expression transformations

    /// <summary>
    /// Base class for all ExpressionTypeTransformations.
    /// </summary>
    public class ExpressionTypeTransformation<E> :
        Core.TypeTransformationBase
        where E : Core.ExpressionTransformationBase
    {
        public readonly E _Expression;

        public ExpressionTypeTransformation(E expression) :
            base() =>
            this._Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <summary>
    /// Base class for all ExpressionKindTransformations.
    /// </summary>
    public class ExpressionKindTransformation<E> :
        Core.ExpressionKindTransformationBase
        where E : Core.ExpressionTransformationBase
    {
        public readonly E _Expression;

        public ExpressionKindTransformation(E expression) :
            base() =>
            this._Expression = expression ?? throw new ArgumentNullException(nameof(expression));

        public override Core.ExpressionTransformationBase Expressions =>
            this._Expression;

        public override Core.TypeTransformationBase Types =>
            this._Expression.Types;
    }

    /// <summary>
    /// Base class for all ExpressionTransformations.
    /// </summary>
    public class ExpressionTransformation<K, T> : 
        Core.ExpressionTransformationBase
        where K : Core.ExpressionKindTransformationBase
        where T : Core.TypeTransformationBase
    {
        public readonly K _Kind;
        private readonly Core.ExpressionKindTransformationBase DefaultKind;
        public override Core.ExpressionKindTransformationBase ExpressionKinds => _Kind ?? DefaultKind;

        public readonly T _Type;
        private readonly Core.TypeTransformationBase DefaultType;
        public override Core.TypeTransformationBase Types => _Type ?? DefaultType;

        public ExpressionTransformation(Func<ExpressionTransformation<K, T>, K> kind, Func<ExpressionTransformation<K, T>, T> type) : 
            base() // disable transformations by default
        {
            this.DefaultKind = base.ExpressionKinds;
            this._Kind = kind?.Invoke(this);

            this.DefaultType = new Core.TypeTransformationBase(); // disabled by default
            this._Type = type?.Invoke(this);
        }
    }

    /// <summary>
    /// Given an expression kind transformation, Transform applies the given transformation to the Kind of every expression. 
    /// </summary>
    public class ExpressionTransformation<K> :
        ExpressionTransformation<K, TypeTransformationBase>
        where K : Core.ExpressionKindTransformationBase
    {
        public ExpressionTransformation(Func<ExpressionTransformation<K, TypeTransformationBase>, K> kind) : 
            base(kind, null) { }
    }

    /// <summary>
    /// ExpressionTransformation where expression kind transformations are set to their default -
    /// i.e. subexpressions are walked, but no transformation is done on the kind itself.
    /// </summary>
    public class DefaultExpressionTransformation :
        ExpressionTransformation<ExpressionKindTransformation<DefaultExpressionTransformation>>
    {
        public DefaultExpressionTransformation() :
            base(e => new ExpressionKindTransformation<DefaultExpressionTransformation>(e as DefaultExpressionTransformation))
        { }
    }

    /// <summary>
    /// Disables all expression transformations, and can be use as no-op if an ExpressionTransformation is required as argument.
    /// </summary>
    public class NoExpressionTransformations :
        ExpressionTransformation<ExpressionKindTransformationBase>
    {
        public NoExpressionTransformations() : 
            base(null) { }

        public override TypedExpression Transform(TypedExpression ex) => ex;
    }
}

