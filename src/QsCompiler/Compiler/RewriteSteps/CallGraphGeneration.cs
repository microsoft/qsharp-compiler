// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Inspects the relationships between callables to populate the compilation's Call Graph field.
    /// </summary>
    internal class CallGraphGeneration : IRewriteStep
    {
        public string Name => "CallGraphGeneration";

        public int Priority => RewriteStepPriorities.CallGraphGeneration;

        public IDictionary<string, string> AssemblyConstants { get; }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => null;

        public bool ImplementsPreconditionVerification => false;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        public CallGraphGeneration()
        {
            this.AssemblyConstants = new Dictionary<string, string>();
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = new QsCompilation(compilation.Namespaces, compilation.EntryPoints, QsNullable<ICallGraph>.NewValue(BuildCallGraph.Apply(compilation)));
            return true;
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
