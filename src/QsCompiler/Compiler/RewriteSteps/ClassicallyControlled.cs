// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlled;

namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    /// <summary>
    /// Replaces if-statements with the corresponding calls to built-in quantum operations if possible.
    /// </summary>
    internal class ClassicallyControlled : IRewriteStep
    {
        public string Name => "Classically Controlled";

        public int Priority => RewriteStepPriorities.ControlFlowSubstitutions;

        public IDictionary<string, string?> AssemblyConstants { get; }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => Enumerable.Empty<IRewriteStep.Diagnostic>();

        public bool ImplementsPreconditionVerification => true;

        public bool ImplementsTransformation => true;

        public bool ImplementsPostconditionVerification => false;

        public ClassicallyControlled()
        {
            this.AssemblyConstants = new Dictionary<string, string?>();
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            var classicallyControlledRequired = ImmutableHashSet.Create(
                BuiltIn.ApplyIfZero.FullName,
                BuiltIn.ApplyIfZeroA.FullName,
                BuiltIn.ApplyIfZeroC.FullName,
                BuiltIn.ApplyIfZeroCA.FullName,
                BuiltIn.ApplyIfOne.FullName,
                BuiltIn.ApplyIfOneA.FullName,
                BuiltIn.ApplyIfOneC.FullName,
                BuiltIn.ApplyIfOneCA.FullName,
                BuiltIn.ApplyIfElseR.FullName,
                BuiltIn.ApplyIfElseRA.FullName,
                BuiltIn.ApplyIfElseRC.FullName,
                BuiltIn.ApplyIfElseRCA.FullName);

            if (!this.CheckForRequired(compilation, BuiltIn.ClassicallyControlledNamespace, classicallyControlledRequired))
            {
                return false;
            }

            var cannonRequired = ImmutableHashSet.Create(
                BuiltIn.NoOp.FullName);

            if (!this.CheckForRequired(compilation, BuiltIn.CanonNamespace, cannonRequired))
            {
                return false;
            }

            return true;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = ReplaceClassicalControl.Apply(compilation);
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }

        private bool CheckForRequired(QsCompilation compilation, string namespaceName, ImmutableHashSet<QsQualifiedName> requiredBuiltIns)
        {
            var builtInNs = compilation.Namespaces
                .FirstOrDefault(ns => ns.Name.Equals(namespaceName));

            if (builtInNs == null)
            {
                return false;
            }

            var providedOperations = new QsNamespace[] { builtInNs }
                .Callables()
                .Select(c => c.FullName)
                .ToImmutableHashSet();

            return requiredBuiltIns.IsSubsetOf(providedOperations);
        }
    }
}
