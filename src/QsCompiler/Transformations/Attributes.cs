using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.Transformations
{
    using AttributeId = QsNullable<UserDefinedType>;
    using CallablePredicate = Func<QsCallable, bool>;

    /// <summary>
    /// Contains tools for building and adding attributes to an existing Q# compilation.
    /// </summary>
    public static class AttributeUtils
    {
        private static AttributeId BuildId(QsQualifiedName name) =>
            name != null
            ? AttributeId.NewValue(new UserDefinedType(name.Namespace, name.Name, QsNullable<Range>.Null))
            : AttributeId.Null;

        // public static methods

        /// <summary>
        /// Returns a Q# attribute with the given name and argument that can be attached to a declaration.
        /// The attribute id is set to Null if the given name is null.
        /// The attribute argument is set to an invalid expression if the given argument is null.
        /// </summary>
        public static QsDeclarationAttribute BuildAttribute(QsQualifiedName name, TypedExpression arg) =>
            new QsDeclarationAttribute(BuildId(name), arg ?? SyntaxGenerator.InvalidExpression, Position.Zero, QsComments.Empty);

        /// <summary>
        /// Builds a string literal with the given content that can be used as argument to a Q# attribute.
        /// The value of the string literal is set to the empty string if the given content is null.
        /// </summary>
        public static TypedExpression StringArgument(string content) =>
            SyntaxGenerator.StringLiteral(content, ImmutableArray<TypedExpression>.Empty);

        /// <summary>
        /// Builds an attribute argument with the given string valued tuple items.
        /// If a given string is null, the value of the corresponding item is set to the empty string.
        /// If no items are given, a suitable argument of type unit is returned.
        /// </summary>
        public static TypedExpression StringArguments(params string[] items) =>
            items == null || items.Length == 0 ? SyntaxGenerator.UnitValue :
            items.Length == 1 ? StringArgument(items.Single()) :
            SyntaxGenerator.TupleLiteral(items.Select(StringArgument));

        /// <summary>
        /// Adds the given attribute to all callables in the given compilation that satisfy the given predicate
        /// - if the predicate is specified and not null.
        /// </summary>
        public static QsCompilation AddToCallables(QsCompilation compilation, QsDeclarationAttribute attribute, CallablePredicate? predicate = null) =>
            new AddAttributes(new[] { (attribute, predicate) }).OnCompilation(compilation);

        /// <summary>
        /// Adds the given attribute(s) to all callables in the given compilation that satisfy the given predicate
        /// - if the predicate is specified and not null.
        /// </summary>
        public static QsCompilation AddToCallables(QsCompilation compilation, params (QsDeclarationAttribute, CallablePredicate?)[] attributes) =>
            new AddAttributes(attributes).OnCompilation(compilation);

        /// <summary>
        /// Adds the given attribute(s) to all callables in the given compilation.
        /// </summary>
        public static QsCompilation AddToCallables(QsCompilation compilation, params QsDeclarationAttribute[] attributes) =>
            new AddAttributes(attributes.Select(att => (att, (CallablePredicate?)null))).OnCompilation(compilation);

        /// <summary>
        /// Adds the given attribute to all callables in the given namespace that satisfy the given predicate
        /// - if the predicate is specified and not null.
        /// </summary>
        public static QsNamespace AddToCallables(QsNamespace ns, QsDeclarationAttribute attribute, CallablePredicate? predicate = null) =>
            new AddAttributes(new[] { (attribute, predicate) }).Namespaces.OnNamespace(ns);

        /// <summary>
        /// Adds the given attribute(s) to all callables in the given namespace that satisfy the given predicate
        /// - if the predicate is specified and not null.
        /// </summary>
        public static QsNamespace AddToCallables(QsNamespace ns, params (QsDeclarationAttribute, CallablePredicate?)[] attributes) =>
            new AddAttributes(attributes).Namespaces.OnNamespace(ns);

        /// <summary>
        /// Adds the given attribute(s) to all callables in the given namespace.
        /// </summary>
        public static QsNamespace AddToCallables(QsNamespace ns, params QsDeclarationAttribute[] attributes) =>
            new AddAttributes(attributes.Select(att => (att, (CallablePredicate?)null))).Namespaces.OnNamespace(ns);

        // private transformation class(es)

        /// <summary>
        /// Transformation to add attributes to an existing compilation.
        /// </summary>
        private class AddAttributes
        : Core.SyntaxTreeTransformation<AddAttributes.TransformationState>
        {
            internal class TransformationState
            {
                internal readonly ImmutableArray<(QsDeclarationAttribute, CallablePredicate)> AttributeSelection;

                internal TransformationState(IEnumerable<(QsDeclarationAttribute, CallablePredicate)> selections) =>
                    this.AttributeSelection = selections.ToImmutableArray();
            }

            internal AddAttributes(IEnumerable<(QsDeclarationAttribute, CallablePredicate?)> attributes)
            : base(new TransformationState(attributes.Select(entry => (entry.Item1, entry.Item2 ?? (_ => true)))))
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new Core.StatementTransformation<TransformationState>(this, Core.TransformationOptions.Disabled);
                this.StatementKinds = new Core.StatementKindTransformation<TransformationState>(this, Core.TransformationOptions.Disabled);
                this.Expressions = new Core.ExpressionTransformation<TransformationState>(this, Core.TransformationOptions.Disabled);
                this.ExpressionKinds = new Core.ExpressionKindTransformation<TransformationState>(this, Core.TransformationOptions.Disabled);
                this.Types = new Core.TypeTransformation<TransformationState>(this, Core.TransformationOptions.Disabled);
            }

            // helper classes

            private class NamespaceTransformation
            : Core.NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(AddAttributes parent)
                : base(parent)
                {
                }

                public override QsCallable OnCallableDeclaration(QsCallable c) =>
                    c.AddAttributes(this.SharedState.AttributeSelection
                        .Where(entry => entry.Item2(c))
                        .Select(entry => entry.Item1));
            }
        }
    }
}
