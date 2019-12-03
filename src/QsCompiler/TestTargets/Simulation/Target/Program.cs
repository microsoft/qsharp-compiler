// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
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
        public CsharpGeneration() =>
            this.AssemblyConstants = new Dictionary<string, string>();

        public string Name => "CsharpGeneration";
        public int Priority => 0;
        public IDictionary<string, string> AssemblyConstants { get; }

        public bool ImplementsTransformation => true;
        public bool ImplementsPreconditionVerification => false;
        public bool ImplementsPostconditionVerification => false;

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            var success = true;
            var outputFolder = this.AssemblyConstants.TryGetValue("OutputPath", out var path) ? path : null; // TODO: Replace string with AssemblyConstant.OutputPath
            var allSources = GetSourceFiles.Apply(compilation.Namespaces) // also generate the code for referenced libraries...
                // ... except when they are one of the packages that currently still already contains the C# code (temporary workaround):
                .Where(s => !Path.GetFileName(s.Value).StartsWith("Microsoft.Quantum")); 
            foreach (var source in allSources)
            {
                var content = SimulationCode.generate(source, CodegenContext.Create(compilation.Namespaces));
                try { CompilationLoader.GeneratedFile(source, outputFolder ?? this.Name, ".g.cs", content); }
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
