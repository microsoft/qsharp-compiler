// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler
{
    public interface IRewriteStepOptions
    {
        string OutputFolder => null;
    }

    public interface IRewriteStep
    {
        public string Name { get; }
        public int Priority { get; }
        public IRewriteStepOptions Options { get; set; }

        public bool ImplementsTransformation { get; }
        public bool ImplementsPreconditionVerification { get; }
        public bool ImplementsPostconditionVerification { get; }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed);
        public bool PreconditionVerification(QsCompilation compilation);
        public bool PostconditionVerification(QsCompilation compilation);
    }

}
