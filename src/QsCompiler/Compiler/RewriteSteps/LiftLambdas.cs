// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.LiftLambdas;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Replaces lambda expressions with the corresponding calls to generated callables if possible.
    /// </summary>
    internal class LiftLambdas : IRewriteStep
    {
        public string Name => "Lift Lambdas";

        public int Priority => RewriteStepPriorities.LambdaExpressionElimination;

        public IDictionary<string, string?> AssemblyConstants { get; }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => false;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        public LiftLambdas()
        {
            this.AssemblyConstants = new Dictionary<string, string?>();
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = LiftLambdaExpressions.Apply(compilation);
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
