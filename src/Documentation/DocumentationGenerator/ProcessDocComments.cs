// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.Documentation.Linting;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using DiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

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
        {
        }

        /// <summary>
        ///     An event that is raised on diagnostics about documentation
        ///     writing (e.g., if an I/O problem prevents writing to disk).
        /// </summary>
        public event Action<IRewriteStep.Diagnostic>? OnDiagnostic;

        internal DocumentationWriter Writer { get; }

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
        /// <param name="lintingRules">
        ///     A dictionary of named linting rules that will be applied to
        ///     each different callable and UDT definition to yield additional
        ///     diagnostics, as well as the severity that will be applied to
        ///     each such diagnostic.
        /// </param>
        public ProcessDocComments(
            string outputPath,
            string? packageName = null,
            IDictionary<string, (DiagnosticSeverity, IDocumentationLintingRule)>? lintingRules = null)
        : base(new TransformationState(), TransformationOptions.Disabled)
        {
            this.Writer = new DocumentationWriter(outputPath, packageName);

            this.Writer.OnDiagnostic += diagnostic =>
                this.OnDiagnostic?.Invoke(diagnostic);

            // We provide our own custom namespace transformation, and expression kind transformation.
            this.Namespaces = new NamespaceTransformation(this, this.Writer, lintingRules);
        }

        private class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            private readonly DocumentationWriter? writer;

            private readonly ImmutableDictionary<string, (DiagnosticSeverity, IDocumentationLintingRule)> lintingRules;

            internal NamespaceTransformation(
                ProcessDocComments parent,
                DocumentationWriter? writer,
                IDictionary<string, (DiagnosticSeverity, IDocumentationLintingRule)>? lintingRules = null)
            : base(parent)
            {
                this.writer = writer;
                this.lintingRules = lintingRules?.ToImmutableDictionary()
                                    ?? ImmutableDictionary<string, (DiagnosticSeverity, IDocumentationLintingRule)>.Empty;
            }

            public override QsNamespace OnNamespace(QsNamespace ns)
            {
                ns = base.OnNamespace(ns);
                if (ns.Elements.Any(element => element.IsInCompilationUnit()))
                {
                    // Concatenate everything into one documentation comment.
                    var comment = new DocComment(
                        ns.Documentation.SelectMany(group => group).SelectMany(comments => comments));
                    if (ns.Elements.Any(element => element switch
                        {
                            QsNamespaceElement.QsCallable { Item: var callable } => callable.Access.IsPublic,
                            QsNamespaceElement.QsCustomType { Item: var type } => type.Access.IsPublic,
                            _ => false,
                        }))
                    {
                        this.writer?.WriteOutput(ns, comment)?.Wait();
                    }
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
                    type.Documentation,
                    type.FullName.Name,
                    deprecated: isDeprecated,
                    replacement: replacement);

                this.lintingRules.InvokeRules(
                    rule => rule.OnTypeDeclaration(type, docComment),
                    (this.Transformation as ProcessDocComments)?.OnDiagnostic
                    ?? ((_) => { }));

                if (type.Access.IsPublic)
                {
                    this.writer?.WriteOutput(type, docComment)?.Wait();
                }

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
                    callable.Documentation,
                    callable.FullName.Name,
                    deprecated: isDeprecated,
                    replacement: replacement);

                this.lintingRules.InvokeRules(
                    rule => rule.OnCallableDeclaration(callable, docComment),
                    (this.Transformation as ProcessDocComments)?.OnDiagnostic
                    ?? ((_) => { }));

                if (callable.Access.IsPublic)
                {
                    this.writer?.WriteOutput(callable, docComment)?.Wait();
                }

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
