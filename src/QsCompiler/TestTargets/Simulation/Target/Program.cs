// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using CommandLine;
using Microsoft.Quantum.QsCompiler.CommandLineCompiler;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;


namespace Microsoft.Quantum.QsCompiler.Testing.Simulation
{
    /// <summary>
    /// This executable server as example program for defining an executable 
    /// that can be given as target to the Q# command line compiler (via -t path/To/Simulation.dll). 
    /// The given target is expected to process the command line options defined by TargetOptions, 
    /// and is invoked as a final step during compilation. 
    /// </summary>
    public static class Program
    {
        private static void GenerateFromBinary(string outputFolder, string pathToBinary)
        {
            var syntaxTree = CompilationLoader.ReadBinary(pathToBinary).ToArray();
            var allSources = GetSourceFiles.Apply(syntaxTree).Where(file => file.Value.EndsWith(".qs"));
            foreach (var source in allSources)
            {
                var content = SimulationCode.generate(source, syntaxTree);
                CompilationLoader.GeneratedFile(source, outputFolder, ".g.cs", content);
            }
        }

        static void Main(string[] args) => 
            Parser.Default.ParseArguments<TargetOptions>(args).WithParsed(options =>
            {
                foreach (var binary in options.Input)
                { GenerateFromBinary(options.OutputFolder, binary); }
            });
    }
}
