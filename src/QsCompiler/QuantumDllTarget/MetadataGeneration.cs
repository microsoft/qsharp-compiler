using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System;

namespace Microsoft.Quantum.QsCompiler
{
    public static class MetadataGeneration
    {
        public static ArrayTypeSyntax WithOmittedRankSpecifiers(this ArrayTypeSyntax syntax) =>
            syntax
            .WithRankSpecifiers(
                SingletonList<ArrayRankSpecifierSyntax>(
                    ArrayRankSpecifier(
                        SingletonSeparatedList<ExpressionSyntax>(
                            OmittedArraySizeExpression()
                        )
                    )
                )
            );

        internal static CodeAnalysis.SyntaxTree GenerateAssemblyMetadata(
            Compilation compilation,
            IEnumerable<MetadataReference> references,
            Action<string> log = null
        )
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
                aliases
                .Select(
                    alias =>
                        TypeOfExpression(
                            QualifiedName(
                                AliasQualifiedName(
                                    IdentifierName(alias),
                                    IdentifierName(WellKnown.METADATA_NAMESPACE)
                                ),
                                IdentifierName(WellKnown.METADATA_TYPE)
                            )
                        )
                );
            foreach (var node in metadataTypeNodes)
            {
                if (log != null) {log(node.NormalizeWhitespace().ToFullString());}
            }
            var dependenciesInitializer =
                ArrayCreationExpression(
                    ArrayType(typeName)
                    .WithOmittedRankSpecifiers()
                )
                .WithInitializer(
                    InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SeparatedList<ExpressionSyntax>(
                            metadataTypeNodes
                        )
                    )
                );
            var metadataField =
                FieldDeclaration(
                    VariableDeclaration(
                        ArrayType(typeName)
                            .WithOmittedRankSpecifiers()
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

            var tree = CSharpSyntaxTree.Create(
                CompilationUnit()
                    .WithExterns(
                        List<ExternAliasDirectiveSyntax>(
                            aliases
                                .Select(
                                    alias =>
                                        ExternAliasDirective(
                                            Identifier(alias)
                                        )
                                )
                        )
                    )
                    .WithMembers(
                        SingletonList<MemberDeclarationSyntax>(
                            namespaceDef
                        )
                    )
            );

            return tree;
        }
    }
}