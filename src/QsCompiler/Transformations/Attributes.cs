﻿using System;
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
    using AttributeSelection = IEnumerable<(QsDeclarationAttribute, Func<QsCallable, bool>)>;


    public class Attributes
    : Core.SyntaxTreeTransformation<AttributeSelection>
    {
        private readonly AttributeSelection AttributesToAdd;

        private static AttributeId BuildId(QsQualifiedName name) =>
            AttributeId.NewValue(new UserDefinedType(name.Namespace, name.Name, QsRangeInfo.Null));

        private Attributes(params (QsDeclarationAttribute, CallablePredicate)[] attributes)
        : base(attributes)
        {
            if (attributes == null || attributes.Any(entry => entry.Item1 == null)) throw new ArgumentNullException(nameof(attributes));
            this.AttributesToAdd = attributes.Select(entry => (entry.Item1, entry.Item2 ?? (_ => true))).ToImmutableArray();

            this.Namespaces = new NamespaceTransformation(this);
            this.Statements = new Core.StatementTransformation<AttributeSelection>(this, Core.TransformationOptions.Disabled);
            this.StatementKinds = new Core.StatementKindTransformation<AttributeSelection>(this, Core.TransformationOptions.Disabled);
            this.Expressions = new Core.ExpressionTransformation<AttributeSelection>(this, Core.TransformationOptions.Disabled);
            this.ExpressionKinds = new Core.ExpressionKindTransformation<AttributeSelection>(this, Core.TransformationOptions.Disabled);
            this.Types = new Core.TypeTransformation<AttributeSelection>(this, Core.TransformationOptions.Disabled);
        }


        // public static methods

        public static QsDeclarationAttribute BuildAttribute(QsQualifiedName name, TypedExpression arg) =>
            new QsDeclarationAttribute(BuildId(name), arg, null, QsComments.Empty); // FIXME: should not be null!

        public static TypedExpression StringArgument(string target) =>
            SyntaxGenerator.StringLiteral(NonNullable<string>.New(target), ImmutableArray<TypedExpression>.Empty);

        public static QsCompilation AddToCallables(QsCompilation compilation, params (QsDeclarationAttribute, CallablePredicate)[] attributes) =>
            new Attributes(attributes).Apply(compilation);

        public static QsNamespace AddToCallables(QsNamespace ns, params (QsDeclarationAttribute, CallablePredicate)[] attributes) =>
            new Attributes(attributes).Namespaces.OnNamespace(ns);

        public static QsNamespace AddToCallables(QsNamespace ns, params QsDeclarationAttribute[] attributes) =>
            new Attributes(attributes.Select(att => (att, (CallablePredicate)null)).ToArray()).Namespaces.OnNamespace(ns);


        // helper classes

        private class NamespaceTransformation
        : Core.NamespaceTransformation<AttributeSelection>
        {
            public NamespaceTransformation(Attributes parent)
            : base(parent)
            { }

            public override QsCallable OnCallableDeclaration(QsCallable c) =>
                c.AddAttributes(SharedState
                    .Where(entry => entry.Item2(c))
                    .Select(entry => entry.Item1));
        }
    }
}
