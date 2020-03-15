﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        public class BuildOptions : CompilationOptions
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

            [Option("response-files", Required = true, SetName = RESPONSE_FILES,
            HelpText = "Response file(s) providing command arguments. Required only if no other arguments are specified. Non-default values for options specified via command line take precedence.")]
            public IEnumerable<string> ResponseFiles { get; set; }

            [Option('o', "output", Required = false, SetName = CODE_MODE,
            HelpText = "Destination folder where the output of the compilation will be generated.")]
            public string OutputFolder { get; set; }

            [Option("doc", Required = false, SetName = CODE_MODE,
            HelpText = "Destination folder where documentation will be generated.")]
            public string DocFolder { get; set; }

            [Option("proj", Required = false, SetName = CODE_MODE,
            HelpText = "Name of the project (needs to be usable as file name).")]
            public string ProjectName { get; set; }

            [Option("emit-dll", Required = false, Default = false, SetName = CODE_MODE,
            HelpText = "Specifies whether the compiler should emit a .NET Core dll containing the compiled Q# code.")]
            public bool EmitDll { get; set; }

            [Option('p', "perf", Required = false, SetName = CODE_MODE,
            HelpText = "Destination folder where the output of the performance assessment will be generated.")]
            public string PerfFolder { get; set; }


            /// <summary>
            /// Reads the content of all specified response files and processes it using FromResponseFiles. 
            /// Updates the options accordingly, prioritizing already specified non-default values over the values from response-files.
            /// Returns false if the content of the specified response-files could not be processed. 
            /// </summary>
            internal bool IncorporateResponseFiles()
            {
                while (this.ResponseFiles != null && this.ResponseFiles.Any())
                {
                    var fromResponseFiles = FromResponseFiles(this.ResponseFiles);
                    if (fromResponseFiles == null) return false;

                    this.CodeSnippet ??= fromResponseFiles.CodeSnippet;
                    this.DocFolder ??= fromResponseFiles.DocFolder;
                    this.EmitDll = this.EmitDll || fromResponseFiles.EmitDll;
                    this.Input = (this.Input ?? new string[0]).Concat(fromResponseFiles.Input ?? new string[0]);
                    this.NoWarn = (this.NoWarn ?? new int[0]).Concat(fromResponseFiles.NoWarn ?? new int[0]);
                    this.OutputFolder ??= fromResponseFiles.OutputFolder;
                    this.OutputFormat = this.OutputFormat != DefaultOptions.OutputFormat ? this.OutputFormat : fromResponseFiles.OutputFormat;
                    this.PackageLoadFallbackFolders = (this.PackageLoadFallbackFolders ?? new string[0]).Concat(fromResponseFiles.PackageLoadFallbackFolders ?? new string[0]);
                    this.PerfFolder ??= fromResponseFiles.PerfFolder;
                    this.Plugins = (this.Plugins ?? new string[0]).Concat(fromResponseFiles.Plugins ?? new string[0]);
                    this.ProjectName ??= fromResponseFiles.ProjectName;
                    this.References = (this.References ?? new string[0]).Concat(fromResponseFiles.References ?? new string[0]);
                    this.ResponseFiles = fromResponseFiles.ResponseFiles;
                    this.TargetPackage ??= fromResponseFiles.TargetPackage;
                    this.TrimLevel = this.TrimLevel != DefaultOptions.TrimLevel ? this.TrimLevel : fromResponseFiles.TrimLevel;
                    this.Verbosity = this.Verbosity != DefaultOptions.Verbosity ? this.Verbosity : fromResponseFiles.Verbosity;
                    this.WithinFunction = this.WithinFunction || fromResponseFiles.WithinFunction;
                }
                return true;
            }
        }

        /// <summary>
        /// Given a string representing the command line arguments, splits them into a suitable string array. 
        /// </summary>
        private static IEnumerable<string> SplitCommandLineArguments(string commandLine)
        {
            var parmChars = commandLine?.ToCharArray() ?? new char[0];
            var inQuote = false;
            for (int index = 0; index < parmChars.Length; index++)
            {
                var precededByBackslash = index > 0 && parmChars[index - 1] == '\\';
                var ignoreIfQuote = inQuote && precededByBackslash;
                if (parmChars[index] == '"' && !ignoreIfQuote) inQuote = !inQuote;
                if (inQuote && parmChars[index] == '\n') parmChars[index] = ' ';
                if (!inQuote && !precededByBackslash && Char.IsWhiteSpace(parmChars[index])) parmChars[index] = '\n';
            }
            return (new string(parmChars))
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(arg => arg.Trim('"')); 
        }

        /// <summary>
        /// Reads the content off all given response files and tries to parse their concatenated content as command line arguments. 
        /// Logs a suitable exceptions and returns null if the parsing fails. 
        /// Throws an ArgumentNullException if the given sequence of responseFiles is null. 
        /// </summary>
        private static BuildOptions FromResponseFiles(IEnumerable<string> responseFiles)
        {
            if (responseFiles == null) throw new ArgumentNullException(nameof(responseFiles));
            var commandLine = String.Join(" ", responseFiles.Select(File.ReadAllText));
            var args = SplitCommandLineArguments(commandLine);
            var parsed = Parser.Default.ParseArguments<BuildOptions>(args);
            return parsed.MapResult(
                (BuildOptions opts) => opts,
                (errs => 
                { 
                    HelpText.AutoBuild(parsed);
                    return null;
                })
            );
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
            if (!options.IncorporateResponseFiles()) return ReturnCode.INVALID_ARGUMENTS;

            var usesPlugins = options.Plugins != null && options.Plugins.Any();
            var loadOptions = new CompilationLoader.Configuration
            {
                ProjectName = options.ProjectName,
                TargetPackageAssembly = options.GetTargetPackageAssemblyPath(logger),
                GenerateFunctorSupport = true,
                SkipSyntaxTreeTrimming = options.TrimLevel == 0,
                ConvertClassicalControl = options.TrimLevel >= 2,
                AttemptFullPreEvaluation = options.TrimLevel > 2,
                DocumentationOutputFolder = options.DocFolder,
                BuildOutputFolder = options.OutputFolder ?? (usesPlugins ? "." : null),
                DllOutputPath = options.EmitDll ? " " : null, // set to e.g. an empty space to generate the dll in the same location as the .bson file
                RewriteSteps = options.Plugins?.Select(step => (step, (string)null)) ?? ImmutableArray<(string, string)>.Empty,
                EnableAdditionalChecks = false // todo: enable debug mode?
            }; 

            if (options.PerfFolder != null)
            {
                CompilationLoader.CompilationTaskEvent += CompilationTracker.OnCompilationTaskEvent;
            }

            var loaded = new CompilationLoader(options.LoadSourcesOrSnippet(logger), options.References, loadOptions, logger);
            if (options.PerfFolder != null)
            {
                try
                {
                    CompilationTracker.PublishResults(options.PerfFolder);
                }
                catch (Exception ex)
                {
                    logger.Log(ErrorCode.PublishingPerfResultsFailed, new string[]{ options.PerfFolder });
                    logger.Log(ex);
                }
            }

            return ReturnCode.Status(loaded);
        }
    }
}
