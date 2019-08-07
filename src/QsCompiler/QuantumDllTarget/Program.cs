// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.Quantum.QsCompiler
{
    class Program
    {

        public static void Main(string[] args) =>
            CommandLineApplication.Execute<Program>(args);

        [Option("-v|--verbose", Description = "Specifies whether to execute in verbose mode.")]
        public bool Verbose { get; set; }

        [Required]
        [Option("-i|--input", Description = "Path to the Q# binary file(s) to process.")]
        public IEnumerable<string> Input { get; set; }

        [Option("-o|--output", Description = "Destination folder where the process output will be generated.")]
        public string OutputFolder { get; set; }

        [Option("-l|--log", Description = "Destination folder where the process log will be generated.")]
        public string LogFolder { get; set; }

        [Option("-n|--noWarn", Description = "Warnings with the given code(s) will be ignored.")]
        public IEnumerable<int> NoWarn { get; set; } = new int[0];

        private readonly StreamWriter LogStream;

        public Program()
        {
            LogStream = new StreamWriter(File.OpenWrite(@"dll-targeting.log"));
        }

        private void Log(string message)
        {
            LogStream.WriteLine(message);
            LogStream.Flush();
        }

        private T LoggingExceptions<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Log("[EXCEPTION] " + ex.ToString());
                throw ex;
            }
        }

        private void LoggingExceptions(Action func)
        {
            try
            {
                func();
            }
            catch (Exception ex)
            {
                Log("[EXCEPTION] " + ex.ToString());
                throw ex;
            }
        }

        void OnExecute() => LoggingExceptions(EncapsulateTarget);

        internal CodeAnalysis.SyntaxTree GenerateAssemblyMetadata(
            Compilation compilation,
            IEnumerable<MetadataReference> references
        )
        {
            var tree = MetadataGeneration.GenerateAssemblyMetadata(compilation, references, Log);
            Log($"Creating syntax tree:\n{tree.GetRoot().NormalizeWhitespace().ToFullString()}");
            return tree;
        }

        void EncapsulateTarget()
        {
            // Begin by setting the various paths that we need.
            var inputPath = Input.Single();
            var inputFile = Path.GetFileName(inputPath);
            var assemblyName = Path.ChangeExtension(inputFile, ".dll");

            var outputPath =
                Path.Join(
                    OutputFolder
                    ?? Directory.GetCurrentDirectory(),
                    assemblyName
                );

            Log($"Loading AST from {inputFile} and encapsulating into {outputPath}.");

            // Next, we need to load the various namespaces from the AST
            // that we were given, and then walk through them to find what
            // assemblies were references.
            // Our goal is to turn these references into references we can
            // emit into our new assembly.

            var references =
                // We first use the Q# compiler library to deserialize
                // the serialized AST into an enumerable of namespaces,
                // then passing that to the source files transformer in the
                // Q# compiler.
                GetSourceFiles.Apply(
                    CompilationLoader
                        .ReadBinary(inputPath)
                        .ToArray()
                )
                // Resolve nonnullable wrappers.
                .Select(sourceFile => sourceFile.Value)
                // Strip out Q# source files.
                .Where(sourceFile => sourceFile.EndsWith(".dll"))
                // Fix / vs \ by going through Uri and back.
                .Select(sourceFile =>
                    new Uri(sourceFile).LocalPath
                )
                // Finally, we each source location into a metadata reference.
                // In doing so, we assign an alias to each reference so that
                // we can look up its Metadata object.
                .Select(
                    (sourceFile, idx) => MetadataReference
                        .CreateFromFile(
                            Path.GetFullPath(sourceFile)
                        )
                        .WithAliases(
                            new string[] {$"reference{idx}"}
                        )
                );

            foreach (var reference in references)
            {
                Log($"Found reference: {reference.Display}");
            }

            // We then use the assembly name and references to create a new
            // compilation object.
            var compilation = CSharpCompilation.Create(
                assemblyName,
                references:
                    references.Concat(
                        // We will get a warning if System.Object can't be found as a reference.
                        new MetadataReference[]
                        {
                            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                        }
                    ),
                options: new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary

                )
            );

            // After we have the compilation, we can use it to look up metadata
            // from referenced assemblies and generate the right syntax tree.
            compilation = compilation.AddSyntaxTrees(
                GenerateAssemblyMetadata(compilation, references)
            );

            // Finally, we can emit the assembly to a file.
            using (var outputStream = File.OpenWrite(outputPath))
            {
                var result = compilation.Emit(
                    outputStream,
                    options: 
                        new EmitOptions()
                            .WithIncludePrivateMembers(true),       
                    manifestResources:
                        new ResourceDescription[]
                        {
                            new ResourceDescription(
                                WellKnown.AST_RESOURCE_NAME,
                                () => File.OpenRead(inputPath),
                                true
                            )
                        }
                );

                // Before completing, we write out any diagnostics generated
                // by the emit process.
                foreach (var diagnostic in result.Diagnostics)
                {
                    Log($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }
            }
        }
    }
}
