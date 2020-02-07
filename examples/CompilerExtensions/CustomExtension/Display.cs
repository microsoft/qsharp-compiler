// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.Demos.CompilerExtensions.Demo
{
    /// <summary>
    /// Provides a compilation step that can be executed during the compilation of a Q# project.  
    /// The compilation step does not implement any transformations, 
    /// but instead merely displays the current state of the Q# compilation as source code. 
    /// If no editor is found to open a txt file, the output is logged to the console.  
    /// </summary>
    public class DisplayQsharpCode : IRewriteStep
    {
        public DisplayQsharpCode() =>
            this.AssemblyConstants = new Dictionary<string, string>(); // will be populated by the Q# compiler

        public string Name => "DisplayQsharpCode";
        public int Priority => 0; // only compared within this dll

        public IDictionary<string, string> AssemblyConstants { get; }
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => null; // this compilation step does not generate any diagnostics

        public bool ImplementsPreconditionVerification => true;
        public bool ImplementsTransformation => false;
        public bool ImplementsPostconditionVerification => false;

        public bool PreconditionVerification(QsCompilation compilation)
        {
            var display = new Display(compilation);
            display.Show();
            return true;
        }

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed) =>
            throw new NotImplementedException();

        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();
    }
}