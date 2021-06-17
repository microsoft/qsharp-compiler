// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization;
using Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Replaces all type parametrized callables with concrete instantiations, dropping any unused callables.
    /// </summary>
    internal class Monomorphization : IRewriteStep
    {
        private readonly bool monomorphizeIntrinsics;
        private readonly bool isLibrary;

        public string Name => "Monomorphization";

        public int Priority => RewriteStepPriorities.TypeParameterElimination;

        public IDictionary<string, string?> AssemblyConstants { get; }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => true;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => true;

        /// <summary>
        /// Constructor for the Monomorphization Rewrite Step.
        /// </summary>
        /// <param name="monomorphizeIntrinsics">When true, intrinsics will be monomorphized as part of the rewrite step.</param>
        /// <param name="isLibrary">When true, each public, non-generic callable will be treated as an entry point in the call graph.</param>
        public Monomorphization(bool monomorphizeIntrinsics = false, bool isLibrary = false)
        {
            this.monomorphizeIntrinsics = monomorphizeIntrinsics;
            this.AssemblyConstants = new Dictionary<string, string?>();
            this.isLibrary = isLibrary;
        }

        public bool PreconditionVerification(QsCompilation compilation) => compilation.EntryPoints.Any() || this.isLibrary;

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = Monomorphize.Apply(new QsCompilation(compilation.Namespaces, this.AugmentedEntryPoints(compilation)), this.monomorphizeIntrinsics);
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

        private ImmutableArray<QsQualifiedName> AugmentedEntryPoints(QsCompilation compilation)
        {
            // If this compilation is for a library project, there are no defined entry points. Instead,
            // treat every public, non-generic callable as a possible entry point into the library.
            return this.isLibrary ?
                compilation.Namespaces.GlobalCallableResolutions()
                    .Where(g => g.Value.Source.AssemblyFile.IsNull && g.Value.Signature.TypeParameters.IsEmpty && g.Value.Access.IsPublic)
                    .Select(e => e.Key)
                    .ToImmutableArray() :
                compilation.EntryPoints;
        }
    }
}
