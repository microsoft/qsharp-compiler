// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.Experimental;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.Documentation
{
    public class DocumentationGenerationStep : IRewriteStep
    {
        private readonly List<IRewriteStep.Diagnostic> Diagnostics;

        public DocumentationGenerationStep()
        {
            this.AssemblyConstants = new Dictionary<string, string>(); // will be populated by the Q# compiler
            this.Diagnostics = new List<IRewriteStep.Diagnostic>(); // collects diagnostics that will be displayed to the user
        }

        public string Name => "DocumentationGeneration";
        public int Priority => 0; // only compared within this dll

        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => this.Diagnostics;

        public bool ImplementsPreconditionVerification => true;
        public bool ImplementsTransformation => true;
        public bool ImplementsPostconditionVerification => false;


        public bool PreconditionVerification(QsCompilation compilation)
        {
            var preconditionPassed = true; // nothing to check
            if (preconditionPassed)
            {
                // Diagnostics with severity Info or lower usually won't be displayed to the user.
                // If the severity is Error or Warning the diagnostic is shown to the user like any other compiler diagnostic,
                // and if the Source property is set to the absolute path of an existing file,
                // the user will be directed to the file when double clicking the diagnostics.
                this.Diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Info,
                    Message = $"Precondition for {this.Name} was {(preconditionPassed ? "satisfied" : "not satisfied")}.",
                    Stage = IRewriteStep.Stage.PreconditionVerification
                });

                foreach (var item in AssemblyConstants)
                {
                    this.Diagnostics.Add(new IRewriteStep.Diagnostic
                    {
                        Severity = DiagnosticSeverity.Info,
                        Message = $"Got assembly constant \"{item.Key}\" = \"{item.Value}\".",
                        Stage = IRewriteStep.Stage.PreconditionVerification
                    });
                }
            }
            return preconditionPassed;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = new ProcessDocComments(
                AssemblyConstants.TryGetValue("OutputPath", out var path)
                ? path
                : null
            ).OnCompilation(compilation);
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();
    }
}
