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
                    yield return new Example("***\nCompiling a Q# source file and calling the C# generation on the compiled binary",
                        new BuildOptions { Input = new string[] { "file.qs" }, Targets = new string[] { "path/To/Microsoft.Quantum.CsharpGeneration.dll" } });
                    yield return new Example("***\nCompiling several Q# source files and referenced compiled libraries",
                        new BuildOptions { Input = new string[] { "file1.qs", "file2.qs" }, References = new string[] { "library1.dll", "library2.dll" }});
                    yield return new Example("***\nSetting the output folder for the compilation output",
                        new BuildOptions { Input = new string[] { "file.qs" }, References = new string[] { "library.dll" }, OutputFolder = Path.Combine("obj", "qsharp") });
                }
            }

            [Option('t', "target", Required = false, SetName = CODE_MODE,
            HelpText = "Path to the dotnet core app(s) to call for processing the compiled binary.")]
            public IEnumerable<string> Targets { get; set; }

            [Option('o', "output", Required = false, SetName = CODE_MODE,
            HelpText = "Destination folder where the output of the compilation will be generated.")]
            public string OutputFolder { get; set; }

            [Option("doc", Required = false, SetName = CODE_MODE,
            HelpText = "Destination folder where documentation will be generated.")]
            public string DocFolder { get; set; }

            [Option("proj", Required = false, SetName = CODE_MODE,
            HelpText = "Name of the project (needs to be usable as file name).")]
            public string ProjectName { get; set; }

            [Option("trim", Required = false, Default = 1,
            HelpText = "[Experimental feature] Integer indicating how much to simplify the syntax tree by eliminating selective abstractions.")]
            public int TrimLevel { get; set; }
        }


        // publicly accessible routines 

        /// <summary>
        /// Builds the compilation for the Q# code or Q# snippet and referenced assemblies defined by the given options.
        /// Invokes all specified targets (dotnet core apps) with suitable TargetOptions,
        /// that in particular specify the path to the compiled binary as input and the same output folder, verbosity, and suppressed warnings as the given options.
        /// The output folder is set to the current directory if one or more targets have been specified but the output folder was left unspecified.
        /// Returns a suitable error code if one of the compilation or generation steps fails.
        /// </summary>
        /// <exception cref="ArgumentNullException">If any of the given arguments is null.</exception>
        /// </summary>
        public static int Run(BuildOptions options, ConsoleLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            var specifiesTargets = options.Targets != null && options.Targets.Any();
            var loadOptions = new CompilationLoader.Configuration
            {
                ProjectName = options.ProjectName,
                GenerateFunctorSupport = true,
                SkipSyntaxTreeTrimming = options.TrimLevel == 0,
                AttemptFullPreEvaluation = options.TrimLevel > 1,
                DocumentationOutputFolder = options.DocFolder,
                BuildOutputFolder = options.OutputFolder ?? (specifiesTargets ? "." : null),
                DllOutputPath = " ", // generating the dll in the same location as the .bson file
                RewriteSteps = options.Targets?.Select(target => (target, (IRewriteStepOptions)null)) ?? ImmutableArray<(string, IRewriteStepOptions)>.Empty
            }; 

            var loaded = new CompilationLoader(options.LoadSourcesOrSnippet(logger), options.References, loadOptions, logger);
            return ReturnCode.Status(loaded);
        }
    }
}
