// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.CodeAnalysis.Emit;


namespace Microsoft.Quantum.QsCompiler
{
    class Program
    {
        public static void Main(string[] args) =>
            CommandLineApplication.Execute<Program>(args);

        void EncapsulateTarget(string inputFile)
        {
            var assemblyName = Path.ChangeExtension(inputFile, ".dll");
            var outputPath = Path.Join(Directory.GetCurrentDirectory(), assemblyName);

            // We turn each dll reference into references we can emit into our new assembly.
            var referencePaths = GetSourceFiles.Apply(CompilationLoader.ReadBinary(inputFile))
                .Select(sourceFile => sourceFile.Value)
                .Where(sourceFile => sourceFile.EndsWith(".dll"));

            // If System.Object can't be found as a reference a warning is generated. 
            // To avoid that warning, we package that reference as well. 
            var systemObjRef = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var references =
                referencePaths
                // Fix / vs \ by going through Uri and back.
                .Select(dllFile => new Uri(dllFile).LocalPath)
                // Finally, we each source location into a metadata reference.
                // In doing so, we assign an alias to each reference so that
                // we can look up its Metadata object.
                .Select((dllFile, idx) =>
                    MetadataReference.CreateFromFile(Path.GetFullPath(dllFile))
                        .WithAliases(new string[] { $"reference{idx}" })) // convenient alias for that reference
                .Append(systemObjRef);

            var tree = MetadataGeneration.GenerateAssemblyMetadata(references);
            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary)
            );

            // Finally, we can emit the assembly to a file.
            var astResource = new ResourceDescription(WellKnown.AST_RESOURCE_NAME, () => File.OpenRead(inputFile), true);
            using (var outputStream = File.OpenWrite(outputPath))
            {
                var result = compilation.Emit(outputStream,
                    options: new EmitOptions(includePrivateMembers: true),       
                    manifestResources: new ResourceDescription[] { astResource }
                );

                //foreach (var diagnostic in result.Diagnostics)
                //{ Log($"{diagnostic.Id}: {diagnostic.GetMessage()}"); }
            }
        }
    }
}
