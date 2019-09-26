// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Microsoft.Quantum.QsCompiler
{
    /// <summary>
    /// The sole purpose of this module is to generate a C# class that explicitly uses referenced Q# content. 
    /// This is a hack to force that these references are not dropped upon Emit due to being unused. 
    /// This is needed (only) if we want to build dlls using the command line compiler without relying on the dotnet core project system.
    /// </summary>
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
            var aliases = references
                .Select(reference => reference.Properties.Aliases.FirstOrDefault(alias => alias.StartsWith(AssemblyConstants.QSHARP_REFERENCE))) // FIXME: improve...
                .Where(alias => alias != null);
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
                            AliasQualifiedName(IdentifierName(alias), IdentifierName(AssemblyConstants.METADATA_NAMESPACE)),
                            IdentifierName(AssemblyConstants.METADATA_TYPE)
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
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(AssemblyConstants.DEPENDENCIES_FIELD))
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
                ClassDeclaration(AssemblyConstants.METADATA_TYPE)
                    .WithModifiers(
                        TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    )
                    .WithMembers(
                        SingletonList<MemberDeclarationSyntax>(metadataField)
                    );
            var namespaceDef =
                NamespaceDeclaration(IdentifierName(AssemblyConstants.METADATA_NAMESPACE))
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
