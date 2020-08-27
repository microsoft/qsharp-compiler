// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
using YamlDotNet.Serialization;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

#nullable enable

namespace Microsoft.Quantum.Documentation
{
    internal interface IAttributeBuilder<T>
    {
        public IAttributeBuilder<T> AddAttribute(QsDeclarationAttribute attribute);
        public QsNullable<QsLocation> Location { get; }

        public T Build();
    }

    internal class Callable : IAttributeBuilder<QsCallable>
    {
        private QsCallable callable;
        internal Callable(QsCallable callable)
        {
            this.callable = callable;
        }

        public QsNullable<QsLocation> Location => callable.Location;

        public IAttributeBuilder<QsCallable> AddAttribute(QsDeclarationAttribute attribute) =>
            new Callable(callable.AddAttribute(attribute));

        public QsCallable Build() => callable;
    }

    internal class Udt : IAttributeBuilder<QsCustomType>
    {
        private QsCustomType type;
        internal Udt(QsCustomType type)
        {
            this.type = type;
        }

        public QsNullable<QsLocation> Location => type.Location;

        public IAttributeBuilder<QsCustomType> AddAttribute(QsDeclarationAttribute attribute) =>
            new Udt(type.AddAttribute(attribute));

        public QsCustomType Build() => type;
    }

    internal static class Extensions
    {
        internal static IAttributeBuilder<QsCallable> AttributeBuilder(
            this QsCallable callable
        ) => new Callable(callable);
        internal static IAttributeBuilder<QsCustomType> AttributeBuilder(
            this QsCustomType type
        ) => new Udt(type);

        internal static IAttributeBuilder<T> WithAttribute<T>(
            this IAttributeBuilder<T> builder, string @namespace, string name,
            TypedExpression input
        ) =>
            builder.AddAttribute(
                new QsDeclarationAttribute(
                    QsNullable<UserDefinedType>.NewValue(
                        new UserDefinedType(
                            NonNullable<string>.New(@namespace),
                            NonNullable<string>.New(name),
                            QsNullable<Range>.Null
                        )
                    ),
                    input,
                    builder.Location.Item.Offset,
                    QsComments.Empty
                )
            );

        private static IAttributeBuilder<T> WithDocumentationAttribute<T>(
            this IAttributeBuilder<T> builder, string attributeName,
            TypedExpression input
        ) => builder.WithAttribute("Microsoft.Quantum.Documentation", attributeName, input);

        private static TypedExpression AsLiteralExpression(this string literal) =>
            SyntaxGenerator.StringLiteral(
                NonNullable<string>.New(literal),
                ImmutableArray<TypedExpression>.Empty
            );

        internal static IAttributeBuilder<T> MaybeWithSimpleDocumentationAttribute<T>(
            this IAttributeBuilder<T> builder, string attributeName, string? value
        ) =>
            value == null || value.Trim().Length == 0
            ? builder
            : builder.WithDocumentationAttribute(
                attributeName, value.AsLiteralExpression()
            );

        internal static IAttributeBuilder<T> WithListOfDocumentationAttributes<T>(
            this IAttributeBuilder<T> builder, string attributeName, IEnumerable<string> items
        ) =>
            items
            .Aggregate(
                builder,
                (acc, item) => acc.WithDocumentationAttribute(
                    attributeName, item.AsLiteralExpression()
                )
            );

        internal static IAttributeBuilder<T> WithDocumentationAttributesFromDictionary<T>(
            this IAttributeBuilder<T> builder, string attributeName, IDictionary<string, string> items
        ) =>
            items
            .Aggregate(
                builder,
                (acc, item) => acc.WithDocumentationAttribute(
                    attributeName,
                    // The following populates all of the metadata needed for a
                    // Q# literal of type (String, String).
                    new TypedExpression(
                        QsExpressionKind<TypedExpression, Identifier, ResolvedType>.NewValueTuple(
                            ImmutableArray.Create(
                                item.Key.AsLiteralExpression(),
                                item.Value.AsLiteralExpression()
                            )
                        ),
                        ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>.Empty,
                        ResolvedType.New(
                            QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>.NewTupleType(
                                ImmutableArray.Create(
                                    ResolvedType.New(QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>.String),
                                    ResolvedType.New(QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>.String)
                                )
                            )
                        ),
                        new InferredExpressionInformation(false, false),
                        QsNullable<Range>.Null
                    )
                )
            );

        internal static string ToSyntax(this ResolvedType type) =>
            SyntaxTreeToQsharp.Default.ToCode(type);

        internal static string ToSyntax(this QsTypeItem item) =>
            item switch
            {
                QsTypeItem.Anonymous anon => anon.Item.ToSyntax(),
                QsTypeItem.Named named => $"{named.Item.VariableName.Value} : {named.Item.Type.ToSyntax()}"
            };
        
        internal static string ToSyntax(this QsTuple<QsTypeItem> items) =>
            items switch
            {
                QsTuple<QsTypeItem>.QsTuple tuple => $@"({
                    String.Join(", ", tuple.Item.Select(innerItem => innerItem.ToSyntax()))
                })",
                QsTuple<QsTypeItem>.QsTupleItem item => item.Item.ToSyntax()
            };

        internal static string ToSyntax(this QsTuple<LocalVariableDeclaration<QsLocalSymbol>> items) =>
            items switch
            {
                QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.QsTuple tuple => $@"({
                    String.Join(", ", tuple.Item.Select(innerItem => innerItem.ToSyntax()))
                })",
                QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.QsTupleItem item => item.Item.ToSyntax()
            };

        internal static string ToSyntax(this LocalVariableDeclaration<QsLocalSymbol> symbol) =>
            $@"{symbol.VariableName switch
            {
                QsLocalSymbol.ValidName name => name.Item.Value,
                _ => "{{invalid}}"
            }} : {symbol.Type.ToSyntax()}";

        internal static string ToSyntax(this QsCustomType type) =>
            $@"newtype {type.FullName.Name.Value} = {
                String.Join(",", type.TypeItems.ToSyntax())
            };";

        internal static string ToSyntax(this ResolvedCharacteristics characteristics) =>
            characteristics.SupportedFunctors.ValueOr(null) switch
            {
                null => "",
                { Count: 0 } => "",
                var functors => $@" is {String.Join(" + ", 
                    functors.Select(functor => functor.Tag switch
                    {
                        QsFunctor.Tags.Adjoint => "Adj",
                        QsFunctor.Tags.Controlled => "Ctl"
                    })
                )}"
            };

        internal static string ToSyntax(this QsCallable callable)
        {
            var kind = callable.Kind.Tag switch
            {
                QsCallableKind.Tags.Function => "function",
                QsCallableKind.Tags.Operation => "operation",
                QsCallableKind.Tags.TypeConstructor => "function"
            };
            var modifiers = callable.Modifiers.Access.Tag switch
            {
                AccessModifier.Tags.DefaultAccess => "",
                AccessModifier.Tags.Internal => "internal "
            };
            var typeParameters = callable.Signature.TypeParameters switch
            {
                { Length: 0 } => "",
                var typeParams => $@"<{
                    String.Join(", ", typeParams.Select(
                        param => param switch
                        {
                            QsLocalSymbol.ValidName name => $"'{name.Item.Value}",
                            _ => "{invalid}"
                        }
                    ))
                }>"
            };
            var input = callable.ArgumentTuple.ToSyntax();
            var output = callable.Signature.ReturnType.ToSyntax();
            var characteristics = callable.Signature.Information.Characteristics.ToSyntax();
            return $"{modifiers}{kind} {callable.FullName.Name.Value}{typeParameters}{input} : {output}{characteristics}";
        }

        internal static string MaybeWithSection(this string document, string name, string? contents) =>
            contents == null || contents.Trim().Length == 0
            ? document
            : $"{document}\n\n## {name}\n\n{contents}";

        internal static string WithYamlHeader(this string document, object header) =>
            $"---\n{new SerializerBuilder().Build().Serialize(header)}---\n{document}";
    }

}
