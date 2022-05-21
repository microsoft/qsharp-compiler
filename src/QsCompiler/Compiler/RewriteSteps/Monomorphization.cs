// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation;
using Microsoft.Quantum.QsCompiler.Transformations.SyntaxTreeTrimming;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Replaces all type parametrized callables with concrete instantiations, dropping any unused callables.
    /// </summary>
    internal class Monomorphization : IRewriteStep
    {
        private readonly bool monomorphizeIntrinsics;
        private readonly bool trimTree;

        public string Name => "Monomorphization";

        public int Priority => RewriteStepPriorities.TypeParameterElimination;

        public IDictionary<string, string?> AssemblyConstants { get; }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => false;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => true;

        /// <summary>
        /// Constructor for the Monomorphization Rewrite Step.
        /// </summary>
        /// <param name="monomorphizeIntrinsics">When true, intrinsics will be monomorphized as part of the rewrite step.</param>
        public Monomorphization(bool monomorphizeIntrinsics = false, bool trimTree = true)
        {
            this.trimTree = trimTree;
            this.monomorphizeIntrinsics = monomorphizeIntrinsics;
            this.AssemblyConstants = new Dictionary<string, string?>();
        }

        public bool PreconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = Monomorphize.Apply(compilation, this.monomorphizeIntrinsics);
            if (this.trimTree)
            {
                transformed = TrimSyntaxTree.Apply(transformed, !this.monomorphizeIntrinsics);
            }

            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            try
            {
                ValidateMonomorphization.Apply(compilation, allowTypeParametersForIntrinsics: !this.monomorphizeIntrinsics);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
