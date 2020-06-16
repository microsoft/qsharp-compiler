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
    public static class Attributes
    {
        private static AttributeId BuildId(QsQualifiedName name) =>
            AttributeId.NewValue(new UserDefinedType(name.Namespace, name.Name, QsRangeInfo.Null));

        // public static methods

        public static QsDeclarationAttribute BuildAttribute(QsQualifiedName name, TypedExpression arg) =>
            new QsDeclarationAttribute(BuildId(name), arg, null, QsComments.Empty);

        public static TypedExpression StringArgument(string target) =>
            SyntaxGenerator.StringLiteral(NonNullable<string>.New(target), ImmutableArray<TypedExpression>.Empty);

        public static QsCompilation AddToCallables(QsCompilation compilation, QsDeclarationAttribute attribute, CallablePredicate predicate = null) =>
            new AddAttributes(new[] { (attribute, predicate) }).Apply(compilation);

        public static QsCompilation AddToCallables(QsCompilation compilation, params (QsDeclarationAttribute, CallablePredicate)[] attributes) =>
            new AddAttributes(attributes).Apply(compilation);

        public static QsCompilation AddToCallables(QsCompilation compilation, params QsDeclarationAttribute[] attributes) =>
            new AddAttributes(attributes.Select(att => (att, (CallablePredicate)null))).Apply(compilation);

        public static QsNamespace AddToCallables(QsNamespace ns, QsDeclarationAttribute attribute, CallablePredicate predicate = null) =>
            new AddAttributes(new[] { (attribute, predicate) }).Namespaces.OnNamespace(ns);

        public static QsNamespace AddToCallables(QsNamespace ns, params (QsDeclarationAttribute, CallablePredicate)[] attributes) =>
            new AddAttributes(attributes).Namespaces.OnNamespace(ns);

        public static QsNamespace AddToCallables(QsNamespace ns, params QsDeclarationAttribute[] attributes) =>
            new AddAttributes(attributes.Select(att => (att, (CallablePredicate)null))).Namespaces.OnNamespace(ns);


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
