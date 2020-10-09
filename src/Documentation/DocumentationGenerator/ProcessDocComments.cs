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
    /// <summary>
    ///     A syntax tree transformation that parses documentation comments,
    ///     saving documentation content back to the syntax tree as attributes,
    ///     and writing formatted documentation content out to Markdown files.
    /// </summary>
    public class ProcessDocComments
    : SyntaxTreeTransformation<ProcessDocComments.TransformationState>
    {
        public class TransformationState
        { }

        private readonly DocumentationWriter? writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessDocComments"/> class.
        /// </summary>
        /// <param name="outputPath">
        ///     The path to which documentation files should be written.
        /// </param>
        /// <param name="packageName">
        ///     The name of the NuGet package being documented, or <c>null</c>
        ///     if the documentation to be written by this object does not
        ///     relate to a particular package.
        /// </param>
        public ProcessDocComments(
            string? outputPath = null,
            string? packageName = null
        )
        : base(new TransformationState())
        {
            this.writer = outputPath == null
                          ? null
                          : new DocumentationWriter(outputPath, packageName);

            // We provide our own custom namespace transformation, and expression kind transformation.
            this.Namespaces = new ProcessDocComments.NamespaceTransformation(this, this.writer);
        }

        private class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            private DocumentationWriter? writer;

            internal NamespaceTransformation(ProcessDocComments parent, DocumentationWriter? writer)
            : base(parent)
            { this.writer = writer; }

            public override QsNamespace OnNamespace(QsNamespace ns)
            {
                ns = base.OnNamespace(ns);
                if (ns.Elements.Any(element => element.IsInCompilationUnit()))
                {
                    // Concatenate everything into one documentation comment.
                    var comment = new DocComment(
                        ns.Documentation.SelectMany(group => group).SelectMany(comments => comments)
                    );
                    writer?.WriteOutput(ns, comment)?.Wait();
                }

                return ns;
            }

            public override QsCustomType OnTypeDeclaration(QsCustomType type)
            {
                type = base.OnTypeDeclaration(type);
                // If the UDT didn't come from a Q# source file, then it
                // came in from a reference, and shouldn't be documented in this
                // project.
                if (!type.IsInCompilationUnit())
                {
                    return type;
                }

                var isDeprecated = type.IsDeprecated(out var replacement);
                var docComment = new DocComment(
                    type.Documentation, type.FullName.Name.Value,
                    deprecated: isDeprecated,
                    replacement: replacement
                );

                this.writer?.WriteOutput(type, docComment)?.Wait();

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
                if (!callable.IsInCompilationUnit())
                {
                    return callable;
                }

                var isDeprecated = callable.IsDeprecated(out var replacement);
                var docComment = new DocComment(
                    callable.Documentation, callable.FullName.Name.Value,
                    deprecated: isDeprecated,
                    replacement: replacement
                );

                this.writer?.WriteOutput(callable, docComment)?.Wait();

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
