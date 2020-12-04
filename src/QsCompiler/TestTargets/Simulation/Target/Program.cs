// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;

namespace Microsoft.Quantum.QsCompiler.Testing.Simulation
{
    /// <summary>
    /// This project serves as example for defining a rewrite step that can integrated into the compilation process
    /// by given it as target to the Q# command line compiler (via -t path/To/Simulation.dll).
    /// Any class in this dll that implements the IRewriteStep interface will be detected during compilation,
    /// and its transformation and verfication step (if implemented) will be executed.
    /// </summary>
    public class CsharpGeneration : IRewriteStep
    {
        /// <inheritdoc/>
        public string Name => "CsharpGeneration";

        /// <inheritdoc/>
        public int Priority => 0;

        /// <inheritdoc/>
        public IDictionary<string, string?> AssemblyConstants { get; } = new Dictionary<string, string?>();

        /// <inheritdoc/>
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics { get; private set; } =
            Enumerable.Empty<IRewriteStep.Diagnostic>();

        /// <inheritdoc/>
        public bool ImplementsTransformation => true;

        /// <inheritdoc/>
        public bool ImplementsPreconditionVerification => false;

        /// <inheritdoc/>
        public bool ImplementsPostconditionVerification => false;

        /// <inheritdoc/>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            // random "diagnostic" to check if diagnostics loading works
            this.GeneratedDiagnostics = new List<IRewriteStep.Diagnostic>()
            {
                new IRewriteStep.Diagnostic
                {
                    Severity = CodeAnalysis.DiagnosticSeverity.Info,
                    Message = "Invokation of the Q# compiler extension for C# generation to demonstrate execution on the simulation framework.",
                }
            };

            var success = true;
            var outputFolder = this.AssemblyConstants.TryGetValue(ReservedKeywords.AssemblyConstants.OutputPath, out var path) ? path : null;
            var allSources = GetSourceFiles.Apply(compilation.Namespaces) // also generate the code for referenced libraries...
                // ... except when they are one of the packages that currently still already contains the C# code (temporary workaround):
                .Where(s => !Path.GetFileName(s).StartsWith("Microsoft.Quantum"));
            foreach (var source in allSources)
            {
                var content = SimulationCode.generate(source, CodegenContext.Create(compilation.Namespaces));
                try
                {
                    CompilationLoader.GeneratedFile(source, outputFolder ?? this.Name, ".g.cs", content);
                }
                catch
                {
                    success = false;
                }
            }
            transformed = compilation;
            return success;
        }

        /// <inheritdoc/>
        public bool PreconditionVerification(QsCompilation compilation) =>
            // todo: we should implement this and check for conjugations and invalid pieces
            throw new System.NotImplementedException();

        /// <inheritdoc/>
        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new System.NotImplementedException();
    }
}
