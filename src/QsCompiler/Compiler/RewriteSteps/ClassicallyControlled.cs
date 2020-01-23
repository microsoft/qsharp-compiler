// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlledTransformation;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class ClassicallyControlled : IRewriteStep
    {
        public string Name => "ClassicallyControlled";
        public int Priority => 10; // Not used for built-in transformations like this
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => null;

        public bool ImplementsTransformation => true;
        public bool ImplementsPreconditionVerification => false;
        public bool ImplementsPostconditionVerification => false;

        public ClassicallyControlled()
        {
            AssemblyConstants = new Dictionary<string, string>();
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = ClassicallyControlledTransformation.Apply(compilation);
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
