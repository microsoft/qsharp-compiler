// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;


namespace Microsoft.Quantum.QsCompiler.Testing.Simulation
{
    /// <summary>
    /// This project serves as example for defining a rewrite step that can integrated into the compilation process
    /// by given it as target to the Q# command line compiler (via -t path/To/Simulation.dll). 
    /// Any class in this dll that implements the IRewriteStep interface will be detected during compilation, 
    /// and its transformation and verfication step (if implemented) will be executed. 
    /// </summary>
    public class CsharpGeneration : IRewriteStep
    {
        public string Name => "CsharpGeneration";
        public int Priority => 0;
        public IRewriteStepOptions Options { get; set; }

        public bool ImplementsTransformation => true;
        public bool ImplementsPreconditionVerification => false;
        public bool ImplementsPostconditionVerification => false;

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            var success = true;
            var outputFolder = this.Options?.OutputFolder ?? this.Name;
            var allSources = GetSourceFiles.Apply(compilation.Namespaces); // also generate the code for referenced libraries
            foreach (var source in allSources)
            {
                var content = SimulationCode.generate(source, compilation.Namespaces);
                try { CompilationLoader.GeneratedFile(source, outputFolder, ".g.cs", content); }
                catch { success = false; }
            }
            transformed = compilation;
            return success;
        }

        public bool PreconditionVerification(QsCompilation compilation) =>
            // todo: we should implement this and check for conjugations and invalid pieces
            throw new System.NotImplementedException();

        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new System.NotImplementedException();
    }
}
