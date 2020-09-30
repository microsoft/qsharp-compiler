// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    public static class CompilerObjectTranslator
    {
        public static SyntaxTree.QsCompilation CreateQsCompilation(QsCompilation bondCompilation) =>
            new SyntaxTree.QsCompilation(
                namespaces: bondCompilation.Namespaces.Select(n => n.ToCompilerObject()).ToImmutableArray(),
                // TODO: Implement EntryPoints.
                entryPoints: Array.Empty<SyntaxTree.QsQualifiedName>().ToImmutableArray());

        private static DataTypes.Position ToCompilerObject(this Position position) =>
            DataTypes.Position.Create(
                line: position.Line,
                column: position.Column);

        private static DataTypes.Range ToCompilerObject(this Range range) =>
            DataTypes.Range.Create(
                start: range.Start.ToCompilerObject(),
                end: range.End.ToCompilerObject());

        private static SyntaxTree.QsCallable ToCompilerObject(this QsCallable bondQsCallable) =>
            new SyntaxTree.QsCallable(
                kind: bondQsCallable.Kind.ToCompilerObject(),
                fullName: bondQsCallable.FullName.ToCompilerObject(),
                attributes: bondQsCallable.Attributes.Select(a => a.ToCompilerObject()).ToImmutableArray(),
                modifiers: bondQsCallable.Modifiers.ToCompilerObject(),
                sourceFile: bondQsCallable.SourceFile.ToNonNullable(),
                location: bondQsCallable.Location.ToQsNullable(),
                signature: bondQsCallable.Signature.ToCompilerObject(),
                // TODO: Implement ArgumentTuple.
                argumentTuple: default,
                specializations: bondQsCallable.Specializations.Select(s => s.ToCompilerObject()).ToImmutableArray(),
                documentation: bondQsCallable.Documentation.ToImmutableArray(),
                comments: bondQsCallable.Comments.ToCompilerObject());

        private static SyntaxTree.QsCallableKind ToCompilerObject(this QsCallableKind bondQsCallableKind) =>
            bondQsCallableKind switch
            {
                QsCallableKind.Operation => SyntaxTree.QsCallableKind.Operation,
                QsCallableKind.Function => SyntaxTree.QsCallableKind.Function,
                QsCallableKind.TypeConstructor => SyntaxTree.QsCallableKind.TypeConstructor,
                _ => throw new ArgumentException($"Unsupported Bond QsCallableKind: {bondQsCallableKind}")
            };

        private static SyntaxTree.QsComments ToCompilerObject(this QsComments bondQsComments) =>
            new SyntaxTree.QsComments(
                openingComments: bondQsComments.OpeningComments.ToImmutableArray(),
                closingComments: bondQsComments.ClosingComments.ToImmutableArray());

        private static SyntaxTree.QsCustomType ToCompilerObject(this QsCustomType bondQsCustomType) =>
            new SyntaxTree.QsCustomType(
                fullName: bondQsCustomType.FullName.ToCompilerObject(),
                // TODO: Implement Attributes.
                attributes: Array.Empty<SyntaxTree.QsDeclarationAttribute>().ToImmutableArray(),
                modifiers: bondQsCustomType.Modifiers.ToCompilerObject(),
                sourceFile: bondQsCustomType.SourceFile.ToNonNullable(),
                location: bondQsCustomType.Location.ToQsNullable(),
                // TODO: Implement Type.
                type: default,
                // TODO: Implement TypeItems.
                typeItems: default,
                documentation: bondQsCustomType.Documentation.ToImmutableArray(),
                comments: bondQsCustomType.Comments.ToCompilerObject());

        private static SyntaxTree.QsDeclarationAttribute ToCompilerObject(this QsDeclarationAttribute bondQsDeclarationAttribute) =>
            new SyntaxTree.QsDeclarationAttribute(
                typeId: bondQsDeclarationAttribute.TypeId.ToQsNullable(),
                // TODO: Implement Argument.
                argument: default,
                offset: bondQsDeclarationAttribute.Offset.ToCompilerObject(),
                comments: bondQsDeclarationAttribute.Comments.ToCompilerObject());

        private static SyntaxTree.QsLocalSymbol ToCompilerObject(this QsLocalSymbol bondQsLocalSymbol) =>
            bondQsLocalSymbol.Kind switch
            {
                QsLocalSymbolKind.ValidName => SyntaxTree.QsLocalSymbol.NewValidName(bondQsLocalSymbol.Name.ToNonNullable()),
                QsLocalSymbolKind.InvalidName => SyntaxTree.QsLocalSymbol.InvalidName,
                _ => throw new ArgumentException($"Unsupported QsLocalSymbolKind: {bondQsLocalSymbol.Kind}")
            };

        private static SyntaxTree.QsLocation ToCompilerObject(this QsLocation bondQsLocation) =>
            new SyntaxTree.QsLocation(
                offset: bondQsLocation.Offset.ToCompilerObject(),
                range: bondQsLocation.Range.ToCompilerObject());

        private static SyntaxTree.QsNamespace ToCompilerObject(this QsNamespace bondQsNamespace) =>
            new SyntaxTree.QsNamespace(
                name: bondQsNamespace.Name.ToNonNullable(),
                elements: bondQsNamespace.Elements.Select(e => e.ToCompilerObject()).ToImmutableArray(),
                documentation: bondQsNamespace.Documentation.ToLookup(
                    p => p.FileName.ToNonNullable(),
                    p => p.DocumentationItems.ToImmutableArray()));

        private static SyntaxTree.QsNamespaceElement ToCompilerObject(this QsNamespaceElement bondQsNamespaceElement)
        {
            if (bondQsNamespaceElement.Kind == QsNamespaceElementKind.QsCallable)
            {
                return SyntaxTree.QsNamespaceElement.NewQsCallable(bondQsNamespaceElement.Callable.ToCompilerObject());
            }
            else if (bondQsNamespaceElement.Kind == QsNamespaceElementKind.QsCustomType)
            {
                return SyntaxTree.QsNamespaceElement.NewQsCustomType(bondQsNamespaceElement.CustomType.ToCompilerObject());
            }
            else
            {
                throw new ArgumentException($"Unsupported kind: {bondQsNamespaceElement.Kind}");
            }
        }

        private static SyntaxTree.QsQualifiedName ToCompilerObject(this QsQualifiedName bondQsQualifiedName) =>
            new SyntaxTree.QsQualifiedName(
                @namespace: bondQsQualifiedName.Namespace.ToNonNullable(),
                name: bondQsQualifiedName.Name.ToNonNullable());

        private static SyntaxTree.QsSpecialization ToCompilerObject(this QsSpecialization bondQsSpecialization) =>
            new SyntaxTree.QsSpecialization(
                // TODO: Implement Kind.
                kind: default,
                // TODO: Implement Parent.
                parent: default,
                // TODO: Implement Attributes.
                attributes: Array.Empty<SyntaxTree.QsDeclarationAttribute>().ToImmutableArray(),
                // TODO: Implement SourceFile.
                sourceFile: default,
                // TODO: Implement Location.
                location: default,
                // TODO: Implement TypeArguments.
                typeArguments: default,
                // TODO: Implement Signature.
                signature: default,
                // TODO: Implement Implementation.
                implementation: default,
                // TODO: Implement Documentation.
                documentation: Array.Empty<string>().ToImmutableArray(),
                // TODO: Implement Comments.
                comments: default);

        private static SyntaxTree.ResolvedSignature ToCompilerObject(this ResolvedSignature bondResolvedSignature) =>
            new SyntaxTree.ResolvedSignature(
                typeParameters: bondResolvedSignature.TypeParameters.Select(tp => tp.ToCompilerObject()).ToImmutableArray(),
                // Implement ArgumentType
                argumentType: default,
                // Implement ReturnType
                returnType: default,
                // Implement Information
                information: default);

        private static SyntaxTree.UserDefinedType ToCompilerObject(this UserDefinedType userDefinedType) =>
            new SyntaxTree.UserDefinedType(
                @namespace: userDefinedType.Namespace.ToNonNullable(),
                name: userDefinedType.Name.ToNonNullable(),
                range: userDefinedType.Range.ToQsNullable());

        private static SyntaxTokens.AccessModifier ToCompilerObject(this AccessModifier accessModifier) =>
            accessModifier switch
            {
                AccessModifier.DefaultAccess => SyntaxTokens.AccessModifier.DefaultAccess,
                AccessModifier.Internal => SyntaxTokens.AccessModifier.Internal,
                _ => throw new ArgumentException($"Unsupported AccessModifier: {accessModifier}")
            };

        private static SyntaxTokens.Modifiers ToCompilerObject(this Modifiers modifiers) =>
            new SyntaxTokens.Modifiers(
                access: modifiers.Access.ToCompilerObject());

        private static NonNullable<string> ToNonNullable(this string str) =>
            NonNullable<string>.New(str);

        private static QsNullable<DataTypes.Range> ToQsNullable(this Range range) =>
            range != null ?
                range.ToCompilerObject().ToQsNullable() :
                QsNullable<DataTypes.Range>.Null;

        private static QsNullable<DataTypes.Range> ToQsNullable(this DataTypes.Range range) =>
            QsNullable<DataTypes.Range>.NewValue(range);

        private static QsNullable<SyntaxTree.QsLocation> ToQsNullable(this QsLocation qsLocation) =>
            qsLocation != null ?
                qsLocation.ToCompilerObject().ToQsNullable() :
                QsNullable<SyntaxTree.QsLocation>.Null;

        private static QsNullable<SyntaxTree.QsLocation> ToQsNullable(this SyntaxTree.QsLocation qsLocation) =>
            QsNullable<SyntaxTree.QsLocation>.NewValue(qsLocation);

        private static QsNullable<SyntaxTree.UserDefinedType> ToQsNullable(this UserDefinedType userDefinedType) =>
            userDefinedType != null ?
                userDefinedType.ToCompilerObject().ToQsNullable() :
                QsNullable<SyntaxTree.UserDefinedType>.Null;

        private static QsNullable<SyntaxTree.UserDefinedType> ToQsNullable(this SyntaxTree.UserDefinedType userDefinedType) =>
            QsNullable<SyntaxTree.UserDefinedType>.NewValue(userDefinedType);
    }
}
