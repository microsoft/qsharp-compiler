// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.IntrinsicMappingTransformation;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class IntrinsicMapping : IRewriteStep
    {
        public string Name { get; }
        public int Priority { get; }
        public string OutputFolder { get; set; }

        public bool ImplementsTransformation { get; }
        public bool ImplementsPreconditionVerification { get; }
        public bool ImplementsPostconditionVerification { get; }

        private QsCompilation Envrionment { get; }

        public IntrinsicMapping(QsCompilation environment)
        {
            Name = "IntrinsicMapping";
            Priority = 10; // Not used for hard-coded transformations like this
            OutputFolder = null;
            ImplementsTransformation = true;
            ImplementsPreconditionVerification = false;
            ImplementsPostconditionVerification = false;

            Envrionment = environment;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = IntrinsicMappingTransformation.Apply(this.Envrionment, compilation);
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
