using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization;
using Microsoft.Quantum.QsCompiler.Transformations.MonomorphizationValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler.RewriteSteps
{
    internal class MonomorphizationRewriteStep : IRewriteStep
    {
        public string Name { get; }

        public int Priority { get; }

        public string OutputFolder { get; set; }

        public bool ImplementsTransformation { get; }

        public bool ImplementsPreconditionVerification { get; }

        public bool ImplementsPostconditionVerification { get; }

        public MonomorphizationRewriteStep()
        {
            Name = "Monomorphization";
            Priority = 10; // Not used for hard-coded transformations like this
            OutputFolder = null;
            ImplementsTransformation = true;
            ImplementsPreconditionVerification = true;
            ImplementsPostconditionVerification = true;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = MonomorphizationTransformation.Apply(compilation);
            return true;
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            return compilation != null && compilation.EntryPoints.Any();
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            try
            {
                MonomorphizationValidationTransformation.Apply(compilation);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
