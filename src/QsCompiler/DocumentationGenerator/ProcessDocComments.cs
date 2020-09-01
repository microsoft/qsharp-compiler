// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Newtonsoft.Json.Linq;

using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

#nullable enable

namespace Microsoft.Quantum.Documentation
{
    public class ProcessDocComments
    : SyntaxTreeTransformation<ProcessDocComments.TransformationState>
    {
        public class TransformationState
        { }

        private readonly DocumentationWriter? writer;

        public ProcessDocComments(
            string? outputPath = null,
            string? packageName = null
        )
        : base(new TransformationState())
        {
            writer = outputPath == null
                     ? null
                     : new DocumentationWriter(outputPath, packageName);

            // We provide our own custom namespace transformation, and expression kind transformation.
            this.Namespaces = new ProcessDocComments.NamespaceTransformation(this, writer);
        }

        public QsCompilation OnCompilation(QsCompilation compilation) =>
            new QsCompilation(
                compilation.Namespaces
                    .Select(this.Namespaces.OnNamespace)
                    .ToImmutableArray(),
                compilation.EntryPoints
            );

        private class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            private DocumentationWriter? writer;
            internal NamespaceTransformation(ProcessDocComments parent, DocumentationWriter? writer)
            : base(parent)
            { this.writer = writer; }

            public override QsCustomType OnTypeDeclaration(QsCustomType type)
            {
                type = base.OnTypeDeclaration(type);
                // If the UDT didn't come from a Q# source file, then it
                // came in from a reference, and shouldn't be documented in this
                // project.
                if (!type.SourceFile.Value.EndsWith(".qs")) return type;

                var isDeprecated = type.IsDeprecated(out var replacement);
                var docComment = new DocComment(
                    type.Documentation, type.FullName.Name.Value,
                    deprecated: isDeprecated,
                    replacement: replacement
                );

                writer?.WriteOutput(type, docComment)?.Wait();

                return type
                    .AttributeBuilder()
                        .MaybeWithSimpleDocumentationAttribute("Summary", docComment.Summary)
                        .MaybeWithSimpleDocumentationAttribute("Description", docComment.Description)
                        .MaybeWithSimpleDocumentationAttribute("Remarks", docComment.Remarks)
                        .MaybeWithSimpleDocumentationAttribute("References", docComment.References)
                        .WithListOfDocumentationAttributes("SeeAlso", docComment.SeeAlso)
                        .WithListOfDocumentationAttributes("Example", docComment.Examples)
                        .WithDocumentationAttributesFromDictionary("NamedItem", docComment.NamedItems)
                    .Build();
            }

            public override QsCallable OnCallableDeclaration(QsCallable callable)
            {
                callable = base.OnCallableDeclaration(callable);
                // If the callable didn't come from a Q# source file, then it
                // came in from a reference, and shouldn't be documented in this
                // project.
                if (!callable.SourceFile.Value.EndsWith(".qs")) return callable;

                var isDeprecated = callable.IsDeprecated(out var replacement);
                var docComment = new DocComment(
                    callable.Documentation, callable.FullName.Name.Value,
                    deprecated: isDeprecated,
                    replacement: replacement
                );

                writer?.WriteOutput(callable, docComment)?.Wait();

                return callable
                    .AttributeBuilder()
                        .MaybeWithSimpleDocumentationAttribute("Summary", docComment.Summary)
                        .MaybeWithSimpleDocumentationAttribute("Description", docComment.Description)
                        .MaybeWithSimpleDocumentationAttribute("Remarks", docComment.Remarks)
                        .MaybeWithSimpleDocumentationAttribute("References", docComment.References)
                        .WithListOfDocumentationAttributes("SeeAlso", docComment.SeeAlso)
                        .WithListOfDocumentationAttributes("Example", docComment.Examples)
                        .WithDocumentationAttributesFromDictionary("Input", docComment.Input)
                        .MaybeWithSimpleDocumentationAttribute("Output", docComment.Output)
                        .WithDocumentationAttributesFromDictionary("TypeParameter", docComment.TypeParameters)
                    .Build();
            }
        }
    }
}
