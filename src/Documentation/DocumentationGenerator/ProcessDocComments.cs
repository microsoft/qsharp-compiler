// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

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

        internal readonly DocumentationWriter? Writer;

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
            string? packageName = null)
        : base(new TransformationState(), TransformationOptions.Disabled)
        {
            this.Writer = outputPath == null
                          ? null
                          : new DocumentationWriter(outputPath, packageName);

            if (this.Writer != null)
            {
                this.Writer.OnDiagnostic += diagnostic =>
                    this.OnDiagnostic?.Invoke(diagnostic);
            }

            // We provide our own custom namespace transformation, and expression kind transformation.
            this.Namespaces = new NamespaceTransformation(this, this.Writer);
        }

        private class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            private DocumentationWriter? writer;

            internal NamespaceTransformation(ProcessDocComments parent, DocumentationWriter? writer)
            : base(parent)
            {
                this.writer = writer;
            }

            private void ValidateNames(
                string symbolName,
                string nameKind,
                Func<string, bool> isNameValid,
                IEnumerable<string> actualNames,
                Range? range = null,
                string? source = null)
            {
                foreach (var name in actualNames)
                {
                    if (!isNameValid(name))
                    {
                        (this.Transformation as ProcessDocComments)?.OnDiagnostic?.Invoke(
                            new IRewriteStep.Diagnostic
                            {
                                Message = $"When documenting {symbolName}, found documentation for {nameKind} {name}, but no such {nameKind} exists.",
                                Severity = CodeAnalysis.DiagnosticSeverity.Warning,
                                Range = range,
                                Source = source,
                                Stage = IRewriteStep.Stage.Transformation,
                            });
                    }
                }
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
                            QsNamespaceElement.QsCallable { Item: var callable } => callable.Visibility.IsPublic,
                            QsNamespaceElement.QsCustomType { Item: var type } => type.Visibility.IsPublic,
                            _ => false
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

                // Validate named item names.
                var inputDeclarations = type.TypeItems.ToDictionaryOfDeclarations();
                this.ValidateNames(
                    $"{type.FullName.Namespace}.{type.FullName.Name}",
                    "named item",
                    name => inputDeclarations.ContainsKey(name),
                    docComment.Input.Keys,
                    range: null, // TODO: provide more exact locations once supported by DocParser.
                    source: type.Source.AssemblyOrCodeFile);

                if (type.Visibility.IsPublic)
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
                var callableName =
                    $"{callable.FullName.Namespace}.{callable.FullName.Name}";

                // Validate input and type parameter names.
                var inputDeclarations = callable.ArgumentTuple.ToDictionaryOfDeclarations();
                this.ValidateNames(
                    callableName,
                    "input",
                    name => inputDeclarations.ContainsKey(name),
                    docComment.Input.Keys,
                    range: null, // TODO: provide more exact locations once supported by DocParser.
                    source: callable.Source.AssemblyOrCodeFile);
                this.ValidateNames(
                    callableName,
                    "type parameter",
                    name => callable.Signature.TypeParameters.Any(
                        typeParam =>
                            typeParam is QsLocalSymbol.ValidName validName &&
                            validName.Item == name.TrimStart('\'')),
                    docComment.TypeParameters.Keys,
                    range: null, // TODO: provide more exact locations once supported by DocParser.
                    source: callable.Source.AssemblyOrCodeFile);

                if (callable.Visibility.IsPublic)
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
