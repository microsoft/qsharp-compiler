// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Microsoft.Quantum.QsCompiler
{
    public static class WellKnown
    {
        public const string AST_RESOURCE_NAME = "__qsharp_data__.bson";
        public const string METADATA_NAMESPACE = "__qsharp__";
        public const string METADATA_TYPE = "Metadata";
        public const string DEPENDENCIES_FIELD = "Dependencies";
    }

    internal static class MetadataGeneration
    {
        public static ArrayTypeSyntax WithOmittedRankSpecifiers(this ArrayTypeSyntax syntax) =>
            syntax.WithRankSpecifiers(
                SingletonList(
                    ArrayRankSpecifier(
                        SingletonSeparatedList<ExpressionSyntax>(
                            OmittedArraySizeExpression()
                        )
                    )
                )
            );

        internal static CodeAnalysis.SyntaxTree GenerateAssemblyMetadata(IEnumerable<MetadataReference> references)
        {
            var aliases = references.Select(reference => reference.Properties.Aliases.First());
            var typeName =
                QualifiedName(
                    AliasQualifiedName(
                        IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                        IdentifierName("System")
                    ),
                    IdentifierName("Type")
                );
            var metadataTypeNodes =
                aliases.Select(alias =>
                    TypeOfExpression(
                        QualifiedName(
                            AliasQualifiedName(IdentifierName(alias), IdentifierName(WellKnown.METADATA_NAMESPACE)),
                            IdentifierName(WellKnown.METADATA_TYPE)
                        )
                    )
                );
            var dependenciesInitializer =
                ArrayCreationExpression(
                    ArrayType(typeName).WithOmittedRankSpecifiers()
                )
                .WithInitializer(
                    InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SeparatedList<ExpressionSyntax>(metadataTypeNodes)
                    )
                );
            var metadataField =
                FieldDeclaration(
                    VariableDeclaration(
                        ArrayType(typeName).WithOmittedRankSpecifiers()
                    )
                    .WithVariables(
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(Identifier(WellKnown.DEPENDENCIES_FIELD))
                            .WithInitializer(
                                EqualsValueClause(dependenciesInitializer)
                            )
                        )
                    )
                )
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.ReadOnlyKeyword)
                    )
                );
            var classDef =
                ClassDeclaration(WellKnown.METADATA_TYPE)
                    .WithModifiers(
                        TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    )
                    .WithMembers(
                        SingletonList<MemberDeclarationSyntax>(metadataField)
                    );
            var namespaceDef =
                NamespaceDeclaration(IdentifierName(WellKnown.METADATA_NAMESPACE))
                    .WithMembers(
                        SingletonList<MemberDeclarationSyntax>(classDef)
                    );

            var compilation =
                CompilationUnit()
                    .WithExterns(
                        List(aliases.Select(Identifier).Select(ExternAliasDirective))
                    )
                    .WithMembers(
                        SingletonList<MemberDeclarationSyntax>(namespaceDef)
                    );

            return CSharpSyntaxTree.Create(compilation);
        }
    }
}