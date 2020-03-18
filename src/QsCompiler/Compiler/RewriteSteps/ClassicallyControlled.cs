// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlled;


namespace Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps
{
    internal class ClassicallyControlled : IRewriteStep
    {
        public string Name => "Classically Controlled";
        public int Priority => 10; // Not used for built-in transformations like this
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => null;

        public bool ImplementsPreconditionVerification => true;
        public bool ImplementsTransformation => true;
        public bool ImplementsPostconditionVerification => false;

        public ClassicallyControlled()
        {
            AssemblyConstants = new Dictionary<string, string>();
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            var controlNs = compilation.Namespaces
                .FirstOrDefault(ns => ns.Name.Equals(BuiltIn.ClassicallyControlledNamespace));

            if (controlNs == null)
            {
                return false;
            }

            var providedOperations = new QsNamespace[] { controlNs }
                .Callables()
                .Select(c => c.FullName)
                .ToHashSet();
            var requiredBuiltIns = new HashSet<QsQualifiedName>()
            {
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
                BuiltIn.ApplyIfElseRCA.FullName
            };

            return requiredBuiltIns.IsSubsetOf(providedOperations);
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
    }
}
