// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using QsAssemblyConstants = Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants;

namespace Microsoft.Quantum.Documentation
{
    /// <summary>
    ///     Rewrite step that generates API documentation from documentation
    ///     comments in the Q# source files being compiled.
    /// </summary>
    public class DocumentationGeneration : IRewriteStep
    {
        private string docsOutputPath = "";
        private readonly List<IRewriteStep.Diagnostic> diagnostics;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DocumentationGeneration"/> class.
        /// </summary>
        public DocumentationGeneration()
        {
            this.diagnostics = new List<IRewriteStep.Diagnostic>(); // collects diagnostics that will be displayed to the user
        }

        /// <inheritdoc/>
        public string Name => "DocumentationGeneration";

        /// <inheritdoc/>
        public int Priority => 0; // only compared within this dll

        /// <inheritdoc/>
        public IDictionary<string, string?> AssemblyConstants { get; } = new Dictionary<string, string?>();

        /// <inheritdoc/>
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => this.diagnostics;

        /// <inheritdoc/>
        public bool ImplementsPreconditionVerification => true;

        /// <inheritdoc/>
        public bool ImplementsTransformation => true;

        /// <inheritdoc/>
        public bool ImplementsPostconditionVerification => false;

        /// <inheritdoc/>
        public bool PreconditionVerification(QsCompilation compilation)
        {
            if (!this.AssemblyConstants.TryGetValue(QsAssemblyConstants.DocsOutputPath, out var path))
            {
                this.diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = $"Documentation generation was enabled, but precondition for {this.Name} was not satisfied: Missing assembly property {QsAssemblyConstants.DocsOutputPath}.",
                    Stage = IRewriteStep.Stage.PreconditionVerification,
                });
                return false;
            }

            if (string.IsNullOrEmpty(path))
            {
                this.diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = $"Documentation generation was enabled, but precondition for {this.Name} was not satisfied: Assembly property {QsAssemblyConstants.DocsOutputPath} was found, but was empty.",
                    Stage = IRewriteStep.Stage.PreconditionVerification,
                });
                return false;
            }

            this.docsOutputPath = path;

            // Diagnostics with severity Info or lower usually won't be displayed to the user.
            // If the severity is Error or Warning the diagnostic is shown to the user like any other compiler diagnostic,
            // and if the Source property is set to the absolute path of an existing file,
            // the user will be directed to the file when double clicking the diagnostics.
            this.diagnostics.Add(new IRewriteStep.Diagnostic
            {
                Severity = DiagnosticSeverity.Info,
                Message = $"Documentation generation was enabled, and precondition for {this.Name} was satisfied, writing docs to {this.docsOutputPath}.",
                Stage = IRewriteStep.Stage.PreconditionVerification,
            });

            return true;
        }

        /// <inheritdoc/>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            var docProcessor = new ProcessDocComments(
                this.docsOutputPath,
                this.AssemblyConstants.TryGetValue(QsAssemblyConstants.DocsPackageId, out var packageName)
                ? packageName
                : null);

            docProcessor.OnDiagnostic += diagnostic =>
            {
                this.diagnostics.Add(diagnostic);
            };

            transformed = docProcessor.OnCompilation(compilation);
            return true;
        }

        /// <inheritdoc/>
        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();
    }
}
