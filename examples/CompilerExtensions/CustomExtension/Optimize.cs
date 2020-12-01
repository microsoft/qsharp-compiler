// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.Experimental;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.Demos.CompilerExtensions.Demo
{
    /// <summary>
    /// Provides a compilation step that can be executed during the compilation of a Q# project.
    /// The compilation step executes selective optimizations provided in the Microsoft.Quantum.QsCompiler.Experimental namespace.
    /// It adds a comment to each callable that lists all identifiers that were used within it prior to applying the optimizations.
    /// </summary>
    public class CustomCompilerExtension : IRewriteStep
    {
        private readonly List<IRewriteStep.Diagnostic> Diagnostics;

        public CustomCompilerExtension()
        {
            this.AssemblyConstants = new Dictionary<string, string>(); // will be populated by the Q# compiler
            this.Diagnostics = new List<IRewriteStep.Diagnostic>(); // collects diagnostics that will be displayed to the user
        }


        public string Name => "CustomCompilerExtension";
        public int Priority => 1; // only compared within this dll

        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => this.Diagnostics;

        public bool ImplementsPreconditionVerification => true;
        public bool ImplementsTransformation => true;
        public bool ImplementsPostconditionVerification => false;


        public bool PreconditionVerification(QsCompilation compilation)
        {
            var preconditionPassed = true; // nothing to check
            if (preconditionPassed)
            {
                // Diagnostics with severity Info or lower usually won't be displayed to the user.
                // If the severity is Error or Warning the diagnostic is shown to the user like any other compiler diagnostic,
                // and if the Source property is set to the absolute path of an existing file,
                // the user will be directed to the file when double clicking the diagnostics.
                this.Diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Info,
                    Message = $"Precondition for {this.Name} was {(preconditionPassed ? "satisfied" : "not satisfied")}.",
                    Stage = IRewriteStep.Stage.PreconditionVerification
                });
            }
            return preconditionPassed;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            // Defines the transformations to execute in each iteration spawned during PreEvaluation.
            // The PreEvaluation invokes these transformations as long as the previous invocation resulted in a non-trivial change.
            static TransformationBase[] Script(ImmutableDictionary<QsQualifiedName, QsCallable> callables) =>
                new TransformationBase[]
                {
                    new CallableInlining(callables),
                    new ConstantPropagation(callables),
                    //new VariableRemoval(),
                    //new StatementRemoval(true),
                };

            transformed = new ListIdentifiers().OnCompilation(compilation); // adds a comment with all used identifiers to each callable
            transformed = PreEvaluation.WithScript(Script, transformed); // applies the specified optimizations
            return true;
        }

        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();
    }
}
