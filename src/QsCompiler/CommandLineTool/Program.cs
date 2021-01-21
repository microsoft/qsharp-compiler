// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using CommandLine;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    public static class ReturnCode
    {
        /// <summary>
        /// Return code indicating that the invoked command to the Q# command line compiler succeeded.
        /// </summary>
        public const int Success = 0;

        /// <summary>
        /// Return code indicating that the command to the Q# command line compiler was invoked with invalid arguments.
        /// </summary>
        public const int InvalidArguments = -1;

        /// <summary>
        /// Return code indicating that some of the files given to the Q# command line compiler could not be loaded.
        /// </summary>
        public const int UnresolvedFiles = -2;

        /// <summary>
        /// Return code indicating that the compilation using the Q# command line compiler failed due to in build errors.
        /// </summary>
        public const int CompilationErrors = -3;

        /// <summary>
        /// Return code indicating that generating the necessary functor support for the built compilation failed.
        /// </summary>
        public const int FunctorGenerationErrors = -4;

        /// <summary>
        /// Return code indicating that pre-evaluating the built compilation if possible failed.
        /// </summary>
        public const int PreevaluationErrors = -5;

        /// <summary>
        /// Return code indicating that generating a binary file with the content of the built compilation failed.
        /// </summary>
        public const int BinaryGenerationErrors = -6;

        /// <summary>
        /// Return code indicating that generating a dll containing the compiled binary failed.
        /// </summary>
        public const int DllGenerationErrors = -7;

        /// <summary>
        /// Return code indicating that generating formatted Q# code based on the built compilation failed.
        /// </summary>
        public const int CodeGenerationErrors = -8;

        /// <summary>
        /// Return code indicating that generating documentation for the built compilation failed.
        /// </summary>
        public const int DocGenerationErrors = -9;

        /// <summary>
        /// Return code indicating that invoking the specified compiler plugin(s) failed.
        /// </summary>
        public const int PluginExecutionErrors = -10;

        /// <summary>
        /// Return code indicating that an unexpected exception was thrown when executing the invoked command to the Q# command line compiler.
        /// </summary>
        public const int UnexpectedError = -1000;

        public static int Status(CompilationLoader loaded) =>
            loaded.SourceFileLoading == CompilationLoader.Status.Failed ? UnresolvedFiles :
            loaded.ReferenceLoading == CompilationLoader.Status.Failed ? UnresolvedFiles :
            loaded.Validation == CompilationLoader.Status.Failed ? CompilationErrors :
            loaded.FunctorSupport == CompilationLoader.Status.Failed ? FunctorGenerationErrors :
            loaded.PreEvaluation == CompilationLoader.Status.Failed ? PreevaluationErrors :
            loaded.Documentation == CompilationLoader.Status.Failed ? DocGenerationErrors :
            loaded.BinaryFormat == CompilationLoader.Status.Failed ? BinaryGenerationErrors :
            loaded.DllGeneration == CompilationLoader.Status.Failed ? DllGenerationErrors :
            loaded.AllLoadedRewriteSteps == CompilationLoader.Status.Failed ? PluginExecutionErrors :
            loaded.Success ? Success : UnexpectedError;
    }

    public static class Program
    {
        private static int Run<T>(Func<T, ConsoleLogger, int> compile, T options)
            where T : Options
        {
            var logger = options.GetLogger();
            try
            {
                CompilationLoader.LoadAssembly = path =>
                    LoadContext.LoadAssembly(path, options.PackageLoadFallbackFolders?.ToArray());

                var result = compile(options, logger);
                logger.ReportSummary(result);
                return result;
            }
            catch (Exception ex)
            {
                logger.Verbosity = DiagnosticSeverity.Hint;
                logger.Log(ErrorCode.UnexpectedCommandLineCompilerException, Enumerable.Empty<string>());
                logger.Log(ex);
                return ReturnCode.UnexpectedError;
            }
        }

        public static int Main(string[] args) =>
            Parser.Default
                .ParseArguments<BuildCompilation.BuildOptions, DiagnoseCompilation.DiagnoseOptions, FormatCompilation.FormatOptions>(args)
                .MapResult(
                    (BuildCompilation.BuildOptions opts) => Run((c, o) => BuildCompilation.Run(c, o), opts),
                    (DiagnoseCompilation.DiagnoseOptions opts) => Run((c, o) => DiagnoseCompilation.Run(c, o), opts),
                    (FormatCompilation.FormatOptions opts) => Run((c, o) => FormatCompilation.Run(c, o), opts),
                    errs => ReturnCode.InvalidArguments);
    }
}
