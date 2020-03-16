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
        public string Name => "ClassicallyControlled";
        public int Priority => 10; // Not used for built-in transformations like this
        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => null;

        public bool ImplementsTransformation => true;
        public bool ImplementsPreconditionVerification => true;
        public bool ImplementsPostconditionVerification => false;

        public ClassicallyControlled()
        {
            AssemblyConstants = new Dictionary<string, string>();
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            transformed = ReplaceClassicalControl.Apply(compilation);
            return true;
        }

        public bool PreconditionVerification(QsCompilation compilation)
        {
            var controlNs = compilation.Namespaces
                .FirstOrDefault(ns => ns.Name.Equals(BuiltIn.ClassicallyControlledNamespace));

            if (controlNs == null)
            {
                return false;
            }

            var providedOperations = new QsNamespace[] { controlNs }.Callables().Select(c => c.FullName.Name);
            var requiredBuiltIns = new List<NonNullable<string>>()
            {
                BuiltIn.ApplyIfZero.FullName.Name,
                BuiltIn.ApplyIfZeroA.FullName.Name,
                BuiltIn.ApplyIfZeroC.FullName.Name,
                BuiltIn.ApplyIfZeroCA.FullName.Name,

                BuiltIn.ApplyIfOne.FullName.Name,
                BuiltIn.ApplyIfOneA.FullName.Name,
                BuiltIn.ApplyIfOneC.FullName.Name,
                BuiltIn.ApplyIfOneCA.FullName.Name,

                BuiltIn.ApplyIfElseR.FullName.Name,
                BuiltIn.ApplyIfElseRA.FullName.Name,
                BuiltIn.ApplyIfElseRC.FullName.Name,
                BuiltIn.ApplyIfElseRCA.FullName.Name
            };

            return requiredBuiltIns.All(builtIn => providedOperations.Any(provided => provided.Equals(builtIn)));
        }

        public bool PostconditionVerification(QsCompilation compilation)
        {
            throw new System.NotImplementedException();
        }
    }
}
