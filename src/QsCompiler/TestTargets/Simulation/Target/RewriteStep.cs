// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.Testing.Simulation
{
    /// <summary>
    /// This project serves as example for defining a rewrite step that can integrated into the compilation process
    /// by given it as target to the Q# command line compiler (via -t path/To/Simulation.dll).
    /// Any class in this dll that implements the IRewriteStep interface will be detected during compilation,
    /// and its transformation and verification step (if implemented) will be executed.
    /// </summary>
    public class CSharpGeneration : Emitter, IRewriteStep
    {
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics { get; private set; } =
            Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => true;

        public bool PreconditionVerification(QsCompilation compilation)
        {
            // random "diagnostic" to check if diagnostics loading works
            this.GeneratedDiagnostics = new List<IRewriteStep.Diagnostic>()
            {
                new IRewriteStep.Diagnostic
                {
                    Severity = CodeAnalysis.DiagnosticSeverity.Info,
                    Message = "Invokation of the Q# compiler extension for C# generation to demonstrate execution on the simulation framework.",
                },
            };

            // This ensures that we are regenerating the C# code for references.
            var props = ((IRewriteStep)this).AssemblyConstants;
            if (!props.TryGetValue(AssemblyConstants.ProcessorArchitecture, out var arch) ||
                arch?.Trim().ToLower() == "unspecified")
            {
                props[AssemblyConstants.ProcessorArchitecture] = AssemblyConstants.MicrosoftSimulator;
            }

            return true;
        }
    }
}
