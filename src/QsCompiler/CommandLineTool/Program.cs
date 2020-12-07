// Copyright (c) Microsoft Corporation. All rights reserved.
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
        public const int SUCCESS = 0;

        /// <summary>
        /// Return code indicating that the command to the Q# command line compiler was invoked with invalid arguments.
        /// </summary>
        public const int INVALID_ARGUMENTS = -1;

        /// <summary>
        /// Return code indicating that some of the files given to the Q# command line compiler could not be loaded.
        /// </summary>
        public const int UNRESOLVED_FILES = -2;

        /// <summary>
        /// Return code indicating that the compilation using the Q# command line compiler failed due to in build errors.
        /// </summary>
        public const int COMPILATION_ERRORS = -3;

        /// <summary>
        /// Return code indicating that generating the necessary functor support for the built compilation failed.
        /// </summary>
        public const int FUNCTOR_GENERATION_ERRORS = -4;

        /// <summary>
        /// Return code indicating that pre-evaluating the built compilation if possible failed.
        /// </summary>
        public const int PREEVALUATION_ERRORS = -5;

        /// <summary>
        /// Return code indicating that generating a binary file with the content of the built compilation failed.
        /// </summary>
        public const int BINARY_GENERATION_ERRORS = -6;

        /// <summary>
        /// Return code indicating that generating a dll containing the compiled binary failed.
        /// </summary>
        public const int DLL_GENERATION_ERRORS = -7;

        /// <summary>
        /// Return code indicating that generating formatted Q# code based on the built compilation failed.
        /// </summary>
        public const int CODE_GENERATION_ERRORS = -8;

        /// <summary>
        /// Return code indicating that generating documentation for the built compilation failed.
        /// </summary>
        public const int DOC_GENERATION_ERRORS = -9;

        /// <summary>
        /// Return code indicating that invoking the specified compiler plugin(s) failed.
        /// </summary>
        public const int PLUGIN_EXECUTION_ERRORS = -10;

        /// <summary>
        /// Return code indicating that executing a target-specific compilation step failed.
        /// </summary>
        public const int TARGETING_ERRORS = -11;

        /// <summary>
        /// Return code indicating that monomorphizing the compilation failed.
        /// </summary>
        public const int MONOMORPHIZATION_ERRORS = -12;

        /// <summary>
        /// Return code indicating that generating QIR for the built compilation failed.
        /// </summary>
        public const int QIR_GENERATION_ERRORS = -13;

        /// <summary>
        /// Return code indicating that an unexpected exception was thrown when executing the invoked command to the Q# command line compiler.
        /// </summary>
        public const int UNEXPECTED_ERROR = -1000;

        public static int Status(CompilationLoader loaded) =>
            loaded.SourceFileLoading == CompilationLoader.Status.Failed ? UNRESOLVED_FILES :
            loaded.ReferenceLoading == CompilationLoader.Status.Failed ? UNRESOLVED_FILES :
            loaded.Validation == CompilationLoader.Status.Failed ? COMPILATION_ERRORS :
            loaded.FunctorSupport == CompilationLoader.Status.Failed ? FUNCTOR_GENERATION_ERRORS :
            loaded.PreEvaluation == CompilationLoader.Status.Failed ? PREEVALUATION_ERRORS :
            loaded.AllLoadedRewriteSteps == CompilationLoader.Status.Failed ? PLUGIN_EXECUTION_ERRORS :
            loaded.Monomorphization == CompilationLoader.Status.Failed ? MONOMORPHIZATION_ERRORS :
            loaded.TargetSpecificReplacements == CompilationLoader.Status.Failed ? TARGETING_ERRORS :
            loaded.TargetSpecificCompilation == CompilationLoader.Status.Failed ? TARGETING_ERRORS :
            loaded.TargetInstructionInference == CompilationLoader.Status.Failed ? TARGETING_ERRORS :
            loaded.QirGeneration == CompilationLoader.Status.Failed ? QIR_GENERATION_ERRORS :
            loaded.Documentation == CompilationLoader.Status.Failed ? DOC_GENERATION_ERRORS :
            loaded.BinaryFormat == CompilationLoader.Status.Failed ? BINARY_GENERATION_ERRORS :
            loaded.DllGeneration == CompilationLoader.Status.Failed ? DLL_GENERATION_ERRORS :
            loaded.Success ? SUCCESS : UNEXPECTED_ERROR;
    }

    public static class Program
    {
        private static int Run<T>(Func<T, ConsoleLogger, int> compile, T options) where T : Options
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
                return ReturnCode.UNEXPECTED_ERROR;
            }
        }

        public static int Main(string[] args) =>
            Parser.Default
                .ParseArguments<BuildCompilation.BuildOptions, DiagnoseCompilation.DiagnoseOptions, FormatCompilation.FormatOptions>(args)
                .MapResult(
                    (BuildCompilation.BuildOptions opts) => Run((c, o) => BuildCompilation.Run(c, o), opts),
                    (DiagnoseCompilation.DiagnoseOptions opts) => Run((c, o) => DiagnoseCompilation.Run(c, o), opts),
                    (FormatCompilation.FormatOptions opts) => Run((c, o) => FormatCompilation.Run(c, o), opts),
                    errs => ReturnCode.INVALID_ARGUMENTS);
    }
}
