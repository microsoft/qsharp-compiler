using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.Quantum.QsCompiler.QirGenerator;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler
{
    public class QirGeneratorStep : IRewriteStep
    {
        public class QirConfiguration
        {
            private static readonly ImmutableDictionary<string, string> clangInteropTypeMapping =
                ImmutableDictionary.CreateRange(new Dictionary<string, string>
                {
                    ["Result"] = "class.RESULT",
                    ["Array"] = "struct.quantum::Array",
                    ["Callable"] = "struct.quantum::Callable",
                    ["TuplePointer"] = "struct.quantum::TupleHeader",
                    ["Qubit"] = "class.QUBIT"
                });

            internal readonly ImmutableDictionary<string, string> InteropTypeMapping;

            public readonly string OutputFileName;

            public QirConfiguration(string outputFileName, Dictionary<string, string>? interopTypeMapping = null)
            {
                this.InteropTypeMapping = interopTypeMapping != null
                    ? interopTypeMapping.ToImmutableDictionary()
                    : clangInteropTypeMapping;
                this.OutputFileName = outputFileName;
            }
        }

        private readonly QirConfiguration config;

        private readonly List<IRewriteStep.Diagnostic> diagnostics = new List<IRewriteStep.Diagnostic>();

        public QirGeneratorStep(QirConfiguration configuration)
        {
            this.config = configuration;
        }

        /// <inheritdoc/>
        public string Name => "QIR Generator";

        /// <inheritdoc/>
        public int Priority => 0;

        /// <inheritdoc/>
        public IDictionary<string, string?> AssemblyConstants { get; } = new Dictionary<string, string?>();

        /// <inheritdoc/>
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => this.diagnostics;

        /// <inheritdoc/>
        public bool ImplementsPreconditionVerification => false;

        /// <inheritdoc/>
        public bool ImplementsTransformation => false;

        /// <inheritdoc/>
        public bool ImplementsPostconditionVerification => false;

        /// <inheritdoc/>
        public bool PostconditionVerification(QsCompilation compilation)
        {
            return true;
        }

        /// <inheritdoc/>
        public bool PreconditionVerification(QsCompilation compilation)
        {
            return true;
        }

        /// <inheritdoc/>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            try
            {
                var context = new GenerationContext(compilation, this.config.OutputFileName)
                {
                    ClangTypeMappings = this.config.InteropTypeMapping,
                    GenerateClangWrappers = true
                };
                var transform = new QirTransformation(context);
                context.StartNewModule();

                foreach (var ns in compilation.Namespaces)
                {
                    transform.Namespaces.OnNamespace(ns);
                }

                foreach (var epName in compilation.EntryPoints)
                {
                    context.GenerateEntryPoint(epName);
                }

                context.EndModule();
            }
            catch (Exception ex)
            {
                this.diagnostics.Add(new IRewriteStep.Diagnostic() { Severity = CodeAnalysis.DiagnosticSeverity.Warning, Message = ex.Message });
                File.WriteAllText($"{this.config.OutputFileName}.ll", $"Exception: {ex.Message} at:\n{ex.StackTrace}");
            }

            transformed = compilation;

            return true;
        }
    }
}
