using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations
{
    using CallablePredicate = Func<QsCallable, bool>;
    using AttributeId = QsNullable<UserDefinedType>;
    using QsRangeInfo = QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>;


    /// <summary>
    /// Contains tools for building and adding attributes to an existing Q# compilation. 
    /// </summary>
    public static class AttributeUtils
    {
        private static AttributeId BuildId(QsQualifiedName name) =>
            name != null
            ? AttributeId.NewValue(new UserDefinedType(name.Namespace, name.Name, QsRangeInfo.Null))
            : AttributeId.Null;

        // public static methods

        /// <summary>
        /// Returns a Q# attribute with the given name and argument that can be attached to a declaration. 
        /// The attribute id is set to Null if the given name is null. 
        /// The attribute argument is set to an invalid expression if the given argument is null.
        /// </summary>
        public static QsDeclarationAttribute BuildAttribute(QsQualifiedName name, TypedExpression arg) =>
            new QsDeclarationAttribute(BuildId(name), arg ?? SyntaxGenerator.InvalidExpression, null, QsComments.Empty);

        /// <summary>
        /// Builds a string literal with the given content that can be used as argument to a Q# attribute.
        /// The value of the string literal is set to the empty string if the given content is null.
        /// </summary>
        public static TypedExpression StringArgument(string content) =>
            SyntaxGenerator.StringLiteral(NonNullable<string>.New(content ?? ""), ImmutableArray<TypedExpression>.Empty);

        /// <summary>
        /// Adds the given attribute to all callables in the given compilation that satisfy the given predicate 
        /// - if the predicate is specified and not null.
        /// Throws an ArgumentNullException if the given attribute or compilation is null.
        /// </summary>
        public static QsCompilation AddToCallables(QsCompilation compilation, QsDeclarationAttribute attribute, CallablePredicate predicate = null) =>
            new AddAttributes(new[] { (attribute, predicate) }).Apply(compilation);

        /// <summary>
        /// Adds the given attribute(s) to all callables in the given compilation that satisfy the given predicate 
        /// - if the predicate is specified and not null.
        /// Throws an ArgumentNullException if one of the given attributes or the compilation is null.
        /// </summary>
        public static QsCompilation AddToCallables(QsCompilation compilation, params (QsDeclarationAttribute, CallablePredicate)[] attributes) =>
            new AddAttributes(attributes).Apply(compilation);

        /// <summary>
        /// Adds the given attribute(s) to all callables in the given compilation.
        /// Throws an ArgumentNullException if one of the given attributes or the compilation is null.
        /// </summary>
        public static QsCompilation AddToCallables(QsCompilation compilation, params QsDeclarationAttribute[] attributes) =>
            new AddAttributes(attributes.Select(att => (att, (CallablePredicate)null))).Apply(compilation);

        /// <summary>
        /// Adds the given attribute to all callables in the given namespace that satisfy the given predicate 
        /// - if the predicate is specified and not null.
        /// Throws an ArgumentNullException if the given attribute or namespace is null.
        /// </summary>
        public static QsNamespace AddToCallables(QsNamespace ns, QsDeclarationAttribute attribute, CallablePredicate predicate = null) =>
            new AddAttributes(new[] { (attribute, predicate) }).Namespaces.OnNamespace(ns);

        /// <summary>
        /// Adds the given attribute(s) to all callables in the given namespace that satisfy the given predicate 
        /// - if the predicate is specified and not null.
        /// Throws an ArgumentNullException if one of the given attributes or the namespace is null.
        /// </summary>
        public static QsNamespace AddToCallables(QsNamespace ns, params (QsDeclarationAttribute, CallablePredicate)[] attributes) =>
            new AddAttributes(attributes).Namespaces.OnNamespace(ns);

        /// <summary>
        /// Adds the given attribute(s) to all callables in the given namespace.
        /// Throws an ArgumentNullException if one of the given attributes or the namespace is null.
        /// </summary>
        public static QsNamespace AddToCallables(QsNamespace ns, params QsDeclarationAttribute[] attributes) =>
            new AddAttributes(attributes.Select(att => (att, (CallablePredicate)null))).Namespaces.OnNamespace(ns);


        // private transformation class(es)

        /// <summary>
        /// Transformation to add attributes to an existing compilation.
        /// </summary>
        private class AddAttributes
        : Core.SyntaxTreeTransformation<AddAttributes.TransformationState>
        {
            internal class TransformationState
            {
                internal readonly ImmutableArray<(QsDeclarationAttribute, Func<QsCallable, bool>)> AttributeSelection;

                /// <exception cref="ArgumentNullException">Thrown when the given selection is null.</exception>
                internal TransformationState(IEnumerable<(QsDeclarationAttribute, Func<QsCallable, bool>)> selections) =>
                    this.AttributeSelection = selections?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(selections));
            }

            internal AddAttributes(IEnumerable<(QsDeclarationAttribute, CallablePredicate)> attributes)
            : base(new TransformationState(attributes?.Select(entry => (entry.Item1, entry.Item2 ?? (_ => true)))))
            {
                if (attributes == null || attributes.Any(entry => entry.Item1 == null)) throw new ArgumentNullException(nameof(attributes));
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
                { }

                public override QsCallable OnCallableDeclaration(QsCallable c) =>
                    c.AddAttributes(SharedState.AttributeSelection
                        .Where(entry => entry.Item2(c))
                        .Select(entry => entry.Item1));
            }
        }
    }
}
