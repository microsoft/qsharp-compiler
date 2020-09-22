using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Quantum.QsCompiler.QirGenerator;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler
{
    public class QirGeneratorStep : IRewriteStep
    {
        public class QirConfiguration
        {
            public Dictionary<string, string> EntryPointTypeMappings { get; private set; }

            public string OutputFileName { get; private set; }

            public QirConfiguration(string outputFileName, Dictionary<string, string> entryPointTypeMappings)
            {
                if (entryPointTypeMappings != null)
                {
                    this.EntryPointTypeMappings = new Dictionary<string, string>(entryPointTypeMappings);
                }
                else
                {
                    this.EntryPointTypeMappings = new Dictionary<string, string>();
                }
                this.OutputFileName = outputFileName;
            }
        }

        public string Name => "QIR Generator";

        public int Priority => 0;

        private readonly Dictionary<string, string> assemblyConstants = new Dictionary<string, string>();
        public IDictionary<string, string> AssemblyConstants => assemblyConstants;

        private readonly List<IRewriteStep.Diagnostic> diags = new List<IRewriteStep.Diagnostic>();
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => diags;

        public bool ImplementsPreconditionVerification => false;

        public bool ImplementsTransformation => false;

        public bool ImplementsPostconditionVerification => false;

        private readonly string outputFileName;

        private readonly Dictionary<string, string> clangTypeMappings;

        public QirGeneratorStep(string file)
        {
            outputFileName = file;

            // Map from QIR type names to clang-generated C type names
            clangTypeMappings = new Dictionary<string, string>
            {
                // HACK for right now
                // TODO: Figure out a way to perform proper configuration
                ["Result"] = "class.RESULT",
                ["Array"] = "struct.quantum::Array",
                ["Callable"] = "struct.quantum::Callable",
                ["TuplePointer"] = "struct.quantum::TupleHeader",
                ["Qubit"] = "class.QUBIT"
            };
        }

        public QirGeneratorStep(QirConfiguration config)
        {
            outputFileName = config.OutputFileName;
            clangTypeMappings = config.EntryPointTypeMappings;
        }


        public bool PostconditionVerification(QsCompilation compilation)
        {
            return true;
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            return true;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            try
            {
                if (compilation == null) throw new ArgumentNullException(nameof(compilation));

                var context = new GenerationContext(compilation, outputFileName)
                { ClangTypeMappings = clangTypeMappings, GenerateClangWrappers = true };
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
                diags.Add(new IRewriteStep.Diagnostic() { Severity = CodeAnalysis.DiagnosticSeverity.Warning, Message = ex.Message });
                File.WriteAllText($"{outputFileName}.ll", $"Exception: {ex.Message} at:\n{ex.StackTrace}");
            }

            transformed = compilation;

            return true;
        }
    }
}
