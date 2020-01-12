// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Microsoft.Quantum.QsCompiler.Diagnostics;


namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    public static class BuildCompilation
    {
        [Verb("build", HelpText = "Builds a compilation unit to run on the Q# quantum simulation framework.")]
        public class BuildOptions : Options
        {
            [Usage(ApplicationAlias = "qsCompiler")]
            public static IEnumerable<Example> UsageExamples
            {
                get
                {
                    yield return new Example("***\nCompiling a Q# source file",
                        new BuildOptions { Input = new string[] { "file.qs" } });
                    yield return new Example("***\nCompiling a Q# source file using additional compilation steps defined in a .NET Core dll",
                        new BuildOptions { Input = new string[] { "file.qs" }, Plugins = new string[] { "myCustomStep.dll" } });
                    yield return new Example("***\nCompiling several Q# source files and referenced compiled libraries",
                        new BuildOptions { Input = new string[] { "file1.qs", "file2.qs" }, References = new string[] { "library1.dll", "library2.dll" }});
                    yield return new Example("***\nSetting the output folder for the compilation output",
                        new BuildOptions { Input = new string[] { "file.qs" }, References = new string[] { "library.dll" }, OutputFolder = Path.Combine("obj", "qsharp") });
                }
            }

            [Option('o', "output", Required = false, SetName = CODE_MODE,
            HelpText = "Destination folder where the output of the compilation will be generated.")]
            public string OutputFolder { get; set; }

            [Option("doc", Required = false, SetName = CODE_MODE,
            HelpText = "Destination folder where documentation will be generated.")]
            public string DocFolder { get; set; }

            [Option("proj", Required = false, SetName = CODE_MODE,
            HelpText = "Name of the project (needs to be usable as file name).")]
            public string ProjectName { get; set; }

            [Option("load", Required = false, SetName = CODE_MODE,
            HelpText = "[Experimental feature] Path to the .NET Core dll(s) defining additional transformations to include in the compilation process.")]
            public IEnumerable<string> Plugins { get; set; }

            [Option("trim", Required = false, Default = 1,
            HelpText = "[Experimental feature] Integer indicating how much to simplify the syntax tree by eliminating selective abstractions.")]
            public int TrimLevel { get; set; }

            [Option("emit-dll", Required = false, Default = false, SetName = CODE_MODE,
            HelpText = "Specifies whether the compiler should emit a .NET Core dll containing the compiled Q# code.")]
            public bool EmitDll { get; set; }
        }


        // publicly accessible routines 

        /// <summary>
        /// Builds the compilation for the Q# code or Q# snippet and referenced assemblies defined by the given options.
        /// Returns a suitable error code if one of the compilation or generation steps fails.
        /// Throws an ArgumentNullException if any of the given arguments is null.
        /// </summary>
        public static int Run(BuildOptions options, ConsoleLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            var usesPlugins = options.Plugins != null && options.Plugins.Any();
            var loadOptions = new CompilationLoader.Configuration
            {
                ProjectName = options.ProjectName,
                GenerateFunctorSupport = true,
                SkipSyntaxTreeTrimming = options.TrimLevel == 0,
                AttemptFullPreEvaluation = options.TrimLevel > 1,
                DocumentationOutputFolder = options.DocFolder,
                BuildOutputFolder = options.OutputFolder ?? (usesPlugins ? "." : null),
                DllOutputPath = options.EmitDll ? " " : null, // set to e.g. an empty space to generate the dll in the same location as the .bson file
                RewriteSteps = options.Plugins?.Select(step => (step, (string)null)) ?? ImmutableArray<(string, string)>.Empty,
                EnableAdditionalChecks = false // todo: enable debug mode?
            }; 

            var loaded = new CompilationLoader(options.LoadSourcesOrSnippet(logger), options.References, loadOptions, logger);
            return ReturnCode.Status(loaded);
        }
    }
}
