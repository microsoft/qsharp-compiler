// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
using YamlDotNet.Serialization;

namespace Microsoft.Quantum.Documentation
{
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    // The next several types allow for adding attributes to
    // callables and UDTs with a single API, so that the extension
    // methods in this file can be written once for both kinds of
    // AST items.
    internal interface IAttributeBuilder<T>
    {
        public IAttributeBuilder<T> AddAttribute(QsDeclarationAttribute attribute);

        public T Build();
    }

    internal class Callable : IAttributeBuilder<QsCallable>
    {
        private readonly QsCallable callable;

        internal Callable(QsCallable callable)
        {
            this.callable = callable;
        }

        public IAttributeBuilder<QsCallable> AddAttribute(QsDeclarationAttribute attribute) =>
            new Callable(this.callable.AddAttribute(attribute));

        public QsCallable Build() => this.callable;
    }

    internal class Udt : IAttributeBuilder<QsCustomType>
    {
        private QsCustomType type;

        internal Udt(QsCustomType type)
        {
            this.type = type;
        }

        public IAttributeBuilder<QsCustomType> AddAttribute(QsDeclarationAttribute attribute) =>
            new Udt(this.type.AddAttribute(attribute));

        public QsCustomType Build() => this.type;
    }

    internal static class Extensions
    {
        internal static IAttributeBuilder<QsCallable> AttributeBuilder(
            this QsCallable callable) =>
                new Callable(callable);

        internal static IAttributeBuilder<QsCustomType> AttributeBuilder(
            this QsCustomType type) =>
                new Udt(type);

        /// <summary>
        ///      Given an attribute builder, returns a new copy of that builder
        ///      with one additional attribute.
        /// </summary>
        /// <typeparam name="T">The type of AST item decorated by the attribute builder.</typeparam>
        /// <param name="builder">The attribute builder to which the new attribute should be added.</param>
        /// <param name="namespace">The Q# namespace containing the new attribute.</param>
        /// <param name="name">The name of the Q# UDT for the new attribute.</param>
        /// <param name="input">The input to the Q# UDT's type constructor function for the given attribute.</param>
        /// <returns>A new attribute builder with the new attribute added.</returns>
        internal static IAttributeBuilder<T> WithAttribute<T>(
            this IAttributeBuilder<T> builder,
            string @namespace,
            string name,
            TypedExpression input) =>
                builder.AddAttribute(
                    AttributeUtils.BuildAttribute(
                        new QsQualifiedName(@namespace, name), input));

        private static IAttributeBuilder<T> WithDocumentationAttribute<T>(
            this IAttributeBuilder<T> builder,
            string attributeName,
            TypedExpression input) =>
                builder.WithAttribute(
                    "Microsoft.Quantum.Documentation",
                    attributeName,
                    input);

        private static TypedExpression AsLiteralExpression(this string literal) =>
            SyntaxGenerator.StringLiteral(literal, ImmutableArray<TypedExpression>.Empty);

        /// <summary>
        ///      Given an attribute builder, either returns it unmodified,
        ///      or returns a new copy of the attribute builder with a new
        ///      string-valued documentation attribute added.
        /// </summary>
        /// <typeparam name="T">The type of AST item decorated by the attribute builder.</typeparam>
        /// <param name="builder">The attribute builder to which the new attribute should be added.</param>
        /// <param name="attributeName">The name of the Q# UDT for the new attribute.</param>
        /// <param name="value">The value of the new attribute to be added, or <c>null</c> if no attribute is to be added.</param>
        /// <returns>A new attribute builder with the new attribute added, or <paramref name="builder"/> if <paramref name="value"/> is <c>null</c>.</returns>
        internal static IAttributeBuilder<T> MaybeWithSimpleDocumentationAttribute<T>(
            this IAttributeBuilder<T> builder, string attributeName, string? value) =>
                value == null || value.Trim().Length == 0
                ? builder
                : builder.WithDocumentationAttribute(
                    attributeName, value.AsLiteralExpression());

        /// <summary>
        ///      Given an attribute builder, returns a new attribute builder with
        ///      an attribute added for each string in a given collection.
        /// </summary>
        /// <typeparam name="T">The type of AST item decorated by the attribute builder.</typeparam>
        /// <param name="builder">The attribute builder to which the new attributes should be added.</param>
        /// <param name="attributeName">The name of the Q# UDT for the new attributes.</param>
        /// <param name="items">The values of the new attributes to be added.</param>
        /// <returns>A new attribute builder with the one new attribute added for each element of <paramref name="items"/>.</returns>
        internal static IAttributeBuilder<T> WithListOfDocumentationAttributes<T>(
            this IAttributeBuilder<T> builder,
            string attributeName,
            IEnumerable<string> items) =>
                items
                .Aggregate(
                    builder,
                    (acc, item) => acc.WithDocumentationAttribute(
                        attributeName, item.AsLiteralExpression()));

        internal static IAttributeBuilder<T> WithDocumentationAttributesFromDictionary<T>(
            this IAttributeBuilder<T> builder,
            string attributeName,
            IDictionary<string, string> items) =>
                items
                .Aggregate(
                    builder,
                    (acc, item) => acc.WithDocumentationAttribute(
                        attributeName,
                        // The following populates all of the metadata needed for a
                        // Q# literal of type (String, String).
                        SyntaxGenerator.TupleLiteral(
                            ImmutableArray.Create(
                                item.Key.AsLiteralExpression(),
                                item.Value.AsLiteralExpression()))));

        internal static string ToSyntax(this QsCustomType type) =>
            SyntaxTreeToQsharp.Default.ToCode(type);

        internal static string ToSyntax(this ResolvedCharacteristics characteristics) =>
            characteristics.SupportedFunctors switch
            {
                { IsNull: true } => "",
                { Item: { Count: 0 } } => "",
                // Be sure to add the leading space before is!
                { Item: var functors } => $" is {string.Join(" + ", functors.Select(functor => functor.ToSyntax()))}"
            };

        internal static string ToSyntax(this QsFunctor functor) =>
            functor.Tag switch
            {
                QsFunctor.Tags.Adjoint => "Adj",
                QsFunctor.Tags.Controlled => "Ctl",
                _ => "__invalid__"
            };

        internal static string ToSyntax(this QsCallable callable)
        {
            var signature = SyntaxTreeToQsharp.DeclarationSignature(
                callable, SyntaxTreeToQsharp.Default.ToCode);

            // The signature provided by SyntaxTreeToQsharp doesn't include
            // the characteristics (e.g.: `is Adj + Ctl`) for the callable, so
            // we add that here.            };
            return $"{signature}{callable.Signature.Information.Characteristics.ToSyntax()}";
        }

        internal static string MaybeWithSection(this string document, string name, string? contents) =>
            contents == null || contents.Trim().Length == 0
            ? document
            : document.WithSection(name, contents);

        internal static string WithSection(this string document, string name, string contents) =>
            $"{document}\n\n## {name}\n\n{contents}";

        internal static string WithYamlHeader(this string document, object header) =>
            $"---\n{new SerializerBuilder().Build().Serialize(header)}---\n{document}";

        internal static bool IsDeprecated(this QsCallable callable, out string? replacement)
        {
            var redirect = SymbolResolution.TryFindRedirect(callable.Attributes);
            if (redirect.IsNull)
            {
                replacement = null;
                return false;
            }
            else
            {
                replacement = redirect.Item;
                return true;
            }
        }

        internal static bool IsDeprecated(this QsCustomType type, [NotNullWhen(true)] out string? replacement)
        {
            var redirect = SymbolResolution.TryFindRedirect(type.Attributes);
            if (redirect.IsNull)
            {
                replacement = null;
                return false;
            }
            else
            {
                replacement = redirect.Item;
                return true;
            }
        }

        internal static Dictionary<string, ResolvedType> ToDictionaryOfDeclarations(this QsTuple<LocalVariableDeclaration<QsLocalSymbol>> items) =>
            items.InputDeclarations().ToDictionary(
                declaration => declaration.Item1,
                declaration => declaration.Item2);

        internal static Dictionary<string, ResolvedType> ToDictionaryOfDeclarations(this QsTuple<QsTypeItem> typeItems) =>
            typeItems.TypeDeclarations().ToDictionary(
                declaration => declaration.Item1,
                declaration => declaration.Item2);

        internal static List<(string, ResolvedType)> TypeDeclarations(this QsTuple<QsTypeItem> typeItems) => typeItems switch
            {
                QsTuple<QsTypeItem>.QsTuple tuple =>
                    tuple.Item.SelectMany(
                        item => item.TypeDeclarations())
                    .ToList(),
                QsTuple<QsTypeItem>.QsTupleItem item => item.Item switch
                    {
                        QsTypeItem.Anonymous _ => new List<(string, ResolvedType)>(),
                        QsTypeItem.Named { Item: var named } =>
                            new List<(string, ResolvedType)>
                            {
                                (named.VariableName, named.Type)
                            },
                        _ => throw new ArgumentException($"Type item {item} is neither anonymous nor named.", nameof(typeItems)),
                    },
                _ => throw new ArgumentException($"Type items {typeItems} aren't a tuple of type items.", nameof(typeItems)),
            };

        internal static List<(string, ResolvedType)> InputDeclarations(this QsTuple<LocalVariableDeclaration<QsLocalSymbol>> items) => items switch
            {
                QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.QsTuple tuple =>
                    tuple.Item.SelectMany(
                        item => item.InputDeclarations())
                    .ToList(),
                QsTuple<LocalVariableDeclaration<QsLocalSymbol>>.QsTupleItem item =>
                    new List<(string, ResolvedType)>
                    {
                        (
                            item.Item.VariableName switch
                            {
                                QsLocalSymbol.ValidName name => name.Item,
                                _ => "__invalid__",
                            },
                            item.Item.Type)
                    },
                _ => throw new Exception()
            };

        internal static string ToMarkdownLink(this ResolvedType type) => type.Resolution switch
            {
                ResolvedTypeKind.ArrayType array => $"{array.Item.ToMarkdownLink()}[]",
                ResolvedTypeKind.Function function =>
                    $"{function.Item1.ToMarkdownLink()} -> {function.Item2.ToMarkdownLink()}",
                ResolvedTypeKind.Operation operation =>
                    $@"{operation.Item1.Item1.ToMarkdownLink()} => {operation.Item1.Item2.ToMarkdownLink()} {
                        operation.Item2.Characteristics.ToSyntax()}",
                ResolvedTypeKind.TupleType tuple => "(" + string.Join(
                    ",", tuple.Item.Select(ToMarkdownLink)) + ")",
                ResolvedTypeKind.UserDefinedType udt => udt.Item.ToMarkdownLink(),
                ResolvedTypeKind.TypeParameter typeParam =>
                    $"'{typeParam.Item.TypeName}",
                _ => type.Resolution.Tag switch
                    {
                        ResolvedTypeKind.Tags.BigInt => "[BigInt](xref:microsoft.quantum.lang-ref.bigint)",
                        ResolvedTypeKind.Tags.Bool => "[Bool](xref:microsoft.quantum.lang-ref.bool)",
                        ResolvedTypeKind.Tags.Double => "[Double](xref:microsoft.quantum.lang-ref.double)",
                        ResolvedTypeKind.Tags.Int => "[Int](xref:microsoft.quantum.lang-ref.int)",
                        ResolvedTypeKind.Tags.Pauli => "[Pauli](xref:microsoft.quantum.lang-ref.pauli)",
                        ResolvedTypeKind.Tags.Qubit => "[Qubit](xref:microsoft.quantum.lang-ref.qubit)",
                        ResolvedTypeKind.Tags.Range => "[Range](xref:microsoft.quantum.lang-ref.range)",
                        ResolvedTypeKind.Tags.String => "[String](xref:microsoft.quantum.lang-ref.string)",
                        ResolvedTypeKind.Tags.UnitType => "[Unit](xref:microsoft.quantum.lang-ref.unit)",
                        ResolvedTypeKind.Tags.InvalidType => "__invalid__",
                        _ => $"__invalid<{type.Resolution.ToString()}>__",
                    },
            };

        internal static string ToMarkdownLink(this UserDefinedType type) =>
            $"[{type.Name}](xref:{type.Namespace}.{type.Name})";

        internal static bool IsInCompilationUnit(this QsNamespaceElement element) =>
            element switch
            {
                QsNamespaceElement.QsCallable callable => callable.Item.IsInCompilationUnit(),
                QsNamespaceElement.QsCustomType type => type.Item.IsInCompilationUnit(),
                _ => false,
            };

        internal static bool IsInCompilationUnit(this QsCallable callable) => !callable.Source.IsReference;

        internal static bool IsInCompilationUnit(this QsCustomType type) => !type.Source.IsReference;

        internal static QsCustomType WithoutDocumentationAndComments(this QsCustomType type) =>
            new QsCustomType(
                fullName: type.FullName,
                attributes: type.Attributes,
                modifiers: type.Modifiers,
                source: type.Source,
                location: type.Location,
                type: type.Type,
                typeItems: type.TypeItems,
                documentation: ImmutableArray<string>.Empty,
                comments: QsComments.Empty);
    }
}
