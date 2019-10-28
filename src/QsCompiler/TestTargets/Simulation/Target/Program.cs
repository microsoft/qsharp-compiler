// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Quantum.QsCompiler.CommandLineCompiler;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Newtonsoft.Json.Bson;


namespace Microsoft.Quantum.QsCompiler.Testing.Simulation
{
    /// TODO BE REMOVED!
    public class QsCompilation
    {
        public ImmutableArray<QsNamespace> Namespaces;
        ImmutableArray<QsQualifiedName> EntryPoints;
    }

    /// <summary>
    /// This executable server as example program for defining an executable 
    /// that can be given as target to the Q# command line compiler (via -t path/To/Simulation.dll). 
    /// The given target is expected to process the command line options defined by TargetOptions, 
    /// and is invoked as a final step during compilation. 
    /// </summary>
    public static class Program
    {
        /// TODO BE REMOVED!
        private static IEnumerable<QsNamespace> ReadBinary(string file)
        {

            using var stream = new MemoryStream(File.ReadAllBytes(Path.GetFullPath(file)));
            using var reader = new BsonDataReader(stream);
            reader.ReadRootValueAsArray = false;
            return Json.Serializer.Deserialize<QsCompilation>(reader).Namespaces;
        }


        private static void GenerateFromBinary(string outputFolder, string pathToBinary)
        {
            /// TODO: TO BE REPLACED BY CompilationLoader.ReadBinary
            var syntaxTree = ReadBinary(pathToBinary).ToArray();
            var allSources = GetSourceFiles.Apply(syntaxTree); // also generate the code for referenced libraries
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
