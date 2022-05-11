// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    /// <summary>
    /// Default values for command line options if nothing is specified.
    /// </summary>
    internal static class DefaultOptions
    {
        public const string Verbosity = "normal";
        public const Options.LogFormat OutputFormat = Options.LogFormat.Default;
        public const int TrimLevel = 1;
    }

    public class CompilationOptions : Options
    {
        [Option(
            "trim",
            Required = false,
            Default = DefaultOptions.TrimLevel,
            SetName = CodeMode,
            HelpText = "[Experimental feature] Integer indicating how much to simplify the syntax tree by eliminating selective abstractions.")]
        public int TrimLevel { get; set; }

        [Option(
            "load",
            Required = false,
            SetName = CodeMode,
            HelpText = "Path to the .NET Core dll(s) defining additional transformations to include in the compilation process.")]
        public IEnumerable<string>? Plugins { get; set; }

        [Option(
            "target-specific-decompositions",
            Required = false,
            SetName = CodeMode,
            HelpText = "[Experimental feature] Path to the .NET Core dll(s) containing target specific implementations.")]
        public IEnumerable<string>? TargetSpecificDecompositions { get; set; }

        [Option(
            "load-test-names",
            Required = false,
            Default = false,
            SetName = CodeMode,
            HelpText = "Specifies whether public types and callables declared in referenced assemblies are exposed via their test name defined by the corresponding attribute.")]
        public bool ExposeReferencesViaTestNames { get; set; }

        [Option(
            "assembly-properties",
            Required = false,
            SetName = CodeMode,
            HelpText = "Additional properties to populate the AssemblyConstants dictionary with. Each item is expected to be of the form \"key:value\".")]
        public IEnumerable<string>? AdditionalAssemblyProperties { get; set; }

        [Option(
            "skip-monomorphization",
            Required = false,
            Default = false,
            HelpText = "Specifies whether to omit monomorphizing the AST for executables.")]
        public bool SkipMonomorphization { get; set; }

        [Option(
            "force-rewrite-step-execution",
            Required = false,
            Default = false,
            HelpText = "Specifies whether to execute rewrite steps even if their precondition is not satisfied. If the parameter is set to true and the precondition is not satisfied, the transformation will be executed, its output will be ignored and the compilation fails.")]
        public bool ForceRewriteStepExecution { get; set; }

        [Option(
            "runtime",
            Required = false,
            SetName = CodeMode,
            HelpText = "Specifies the classical capabilites of the runtime. Determines what QIR profile to compile to.")]
        public string? RuntimeCapabilityName { get; set; }

        /// <summary>
        /// The parsed <see cref="RuntimeCapabilityName"/>.
        /// </summary>
        internal RuntimeCapability RuntimeCapability =>
            RuntimeCapability.Parse(this.RuntimeCapabilityName) ?? RuntimeCapabilityModule.Top;

        [Option(
            "build-exe",
            Required = false,
            Default = false,
            SetName = CodeMode,
            HelpText = "Specifies whether to build a Q# command line application.")]
        public bool MakeExecutable { get; set; }

        [Option(
            "package-load-fallback-folders",
            Required = false,
            SetName = CodeMode,
            HelpText = "Specifies the directories the compiler will search when a compiler dependency could not be found.")]
        public IEnumerable<string>? PackageLoadFallbackFolders { get; set; }

        [Option(
            "llvm-libs",
            Required = false,
            SetName = CodeMode,
            HelpText = "Specifies the folder where the llvm libraries can be found.")]
        public string? LlvmLibs { get; set; }

        /// <summary>
        /// Returns a dictionary with the specified assembly properties as out parameter.
        /// Returns a boolean indicating whether all specified properties were successfully added.
        /// </summary>
        internal bool ParseAssemblyProperties(ILogger? logger, [MaybeNullWhen(false)] out Dictionary<string, string> asmProperties)
        {
            asmProperties = null;
            var parsed = new Dictionary<string, string>();

            foreach (var keyValue in this.AdditionalAssemblyProperties ?? Array.Empty<string>())
            {
                // NB: We use `count: 2` here to ensure that assembly constants can contain colons.
                var pieces = keyValue?.Split(":", count: 2);
                if (pieces is null || pieces.Length != 2)
                {
                    logger?.Log(WarningCode.InvalidAssemblyProperties, Array.Empty<string>());
                    return false;
                }

                var (key, value) = (pieces[0].Trim().Trim('"'), pieces[1].Trim().Trim('"'));
                if (string.IsNullOrWhiteSpace(key))
                {
                    logger?.Log(WarningCode.InvalidAssemblyProperties, Array.Empty<string>());
                    return false;
                }

                if (!parsed.TryAdd(key, value))
                {
                    logger?.Log(WarningCode.DuplicateAssemblyProperty, new[] { key, value });
                    parsed[key] = value;
                }
            }

            asmProperties = parsed;
            return true;
        }

        /// <summary>
        /// Configures the LoadingContext for this assembly depending on the defined options.
        /// </summary>
        internal void SetupLoadingContext()
        {
            var current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var fallbackFolders = this.PackageLoadFallbackFolders?.Prepend(current);

            CompilationLoader.LoadAssembly = path =>
                LoadContext.LoadAssembly(path, fallbackFolders?.ToArray());

            var llvmLibs = this.LlvmLibs != null
                ? LoadContext.ResolveFromPaths("libLLVM", new[] { this.LlvmLibs })
                : null;
            if (llvmLibs != null)
            {
                LoadContext.UnmanagedDllPaths.Add(llvmLibs);
            }
        }
    }

    public class Options
    {
        public enum LogFormat
        {
            Default,
            MsBuild,
        }

        // Note: items in one set are mutually exclusive with items from other sets
        protected const string CodeMode = "codeMode";
        protected const string SnippetMode = "snippetMode";
        protected const string ResponseFiles = "responseFiles";

        [Option(
            'v',
            "verbosity",
            Required = false,
            Default = DefaultOptions.Verbosity,
            HelpText = "Specifies the verbosity of the logged output. Valid values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].")]
        public string? Verbosity { get; set; }

        [Option(
            "format",
            Required = false,
            Default = DefaultOptions.OutputFormat,
            HelpText = "Specifies the output format of the command line compiler.")]
        public LogFormat OutputFormat { get; set; }

        [Option(
            'i',
            "input",
            Required = true,
            SetName = CodeMode,
            HelpText = "Q# code or name of the Q# file to compile.")]
        public IEnumerable<string>? Input { get; set; }

        [Option(
            's',
            "snippet",
            Required = true,
            SetName = SnippetMode,
            HelpText = "Q# snippet to compile - i.e. Q# code occuring within an operation or function declaration.")]
        public string? CodeSnippet { get; set; }

        [Option(
            'f',
            "within-function",
            Required = false,
            Default = false,
            SetName = SnippetMode,
            HelpText = "Specifies whether a given Q# snipped occurs within a function")]
        public bool WithinFunction { get; set; }

        [Option(
            'r',
            "references",
            Required = false,
            Default = new string[0],
            HelpText = "Referenced binaries to include in the compilation.")]
        public IEnumerable<string>? References { get; set; }

        [Option(
            'n',
            "no-warn",
            Required = false,
            Default = new int[0],
            HelpText = "Warnings with the given code(s) will be ignored.")]
        public IEnumerable<int>? NoWarn { get; set; }

        /// <summary>
        /// Updates the settings that can be used independent on the other arguments according to the setting in the given options.
        /// Already specified non-default values are prioritized over the values in the given options,
        /// unless overwriteNonDefaultValues is set to true. Sequences are merged.
        /// </summary>
        internal void UpdateSetIndependentSettings(Options updates, bool overwriteNonDefaultValues = false)
        {
            this.Verbosity = overwriteNonDefaultValues || this.Verbosity == DefaultOptions.Verbosity ? updates.Verbosity : this.Verbosity;
            this.OutputFormat = overwriteNonDefaultValues || this.OutputFormat == DefaultOptions.OutputFormat ? updates.OutputFormat : this.OutputFormat;
            this.NoWarn = (this.NoWarn ?? Array.Empty<int>()).Concat(updates.NoWarn ?? Array.Empty<int>());
            this.References = (this.References ?? Array.Empty<string>()).Concat(updates.References ?? Array.Empty<string>());
        }

        // routines related to logging

        /// <summary>
        /// If a logger is given, logs the options as CommandLineArguments Information before returning the printed string.
        /// </summary>
        public string[] Print(ILogger? logger = null)
        {
            string Value(PropertyInfo p)
            {
                var v = p.GetValue(this);
                return v is string[] a
                    ? string.Join(';', a)
                    : v?.ToString() ?? "(null)";
            }

            var props = this.GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(OptionAttribute)));
            var msg = props.Select(p => $"{p.Name}: {Value(p)}").ToArray();
            logger?.Log(InformationCode.CommandLineArguments, Enumerable.Empty<string>(), messageParam: Formatting.Indent(msg).ToArray());
            return msg;
        }

        /// <summary>
        /// Given a LogFormat, returns a suitable routing for formatting diagnostics.
        /// </summary>
        internal static Func<Diagnostic, string> LoggingFormat(LogFormat format) =>
            format switch
            {
                LogFormat.MsBuild => Formatting.MsBuildFormat,
                LogFormat.Default => Formatting.HumanReadableFormat,
                _ => throw new NotImplementedException("unknown output format for logger"),
            };

        /// <summary>
        /// Creates a suitable logger for the given command line options.
        /// </summary>
        public ConsoleLogger GetLogger(DiagnosticSeverity defaultVerbosity = DiagnosticSeverity.Warning)
        {
            var verbosity =
                "detailed".Equals(this.Verbosity, StringComparison.InvariantCultureIgnoreCase) ||
                "d".Equals(this.Verbosity, StringComparison.InvariantCultureIgnoreCase) ||
                "diagnostic".Equals(this.Verbosity, StringComparison.InvariantCultureIgnoreCase) ||
                "diag".Equals(this.Verbosity, StringComparison.InvariantCultureIgnoreCase)
                ? DiagnosticSeverity.Hint :
                "quiet".Equals(this.Verbosity, StringComparison.InvariantCultureIgnoreCase) ||
                "q".Equals(this.Verbosity, StringComparison.InvariantCultureIgnoreCase)
                ? DiagnosticSeverity.Error :
                defaultVerbosity;
            var logger = new ConsoleLogger(
                LoggingFormat(this.OutputFormat),
                verbosity,
                this.NoWarn,
                this.CodeSnippet != null ? -2 : 0);
            return logger;
        }

        // routines related to processing snippets

        /// <summary>
        /// text document identifier used to identify the code snippet in diagnostic mode
        /// </summary>
        private static readonly Uri SnippetFileUri = new Uri(Path.GetFullPath("__CODE_SNIPPET__.qs"));

        private static string SnippetFileId =>
            QsCompilerError.RaiseOnFailure(
                () => CompilationUnitManager.GetFileId(SnippetFileUri),
                "invalid code snippet id");

        /// <summary>
        /// name of the namespace within which code snippets are compiled
        /// </summary>
        private const string SnippetNamespace = "CODE_SNIPPET_NS";

        /// <summary>
        /// name of the callable within which code snippets are compiled
        /// </summary>
        private const string SnippetCallable = "CODE_SNIPPET_CALLABLE";

        /// <summary>
        /// wraps the given content into a namespace and callable that maps Unit to Unit
        /// </summary>
        public static string AsSnippet(string content, bool inFunction = false) =>
            $"{Declarations.Namespace} {SnippetNamespace} {{ \n " +
            $"{(inFunction ? Declarations.Function : Declarations.Operation)} {SnippetCallable} () : {Types.Unit} {{ \n" +
            $"{content} \n" + // no indentation such that position info is accurate
            $"}} \n" +
            $"}}";

        /// <summary>
        /// Helper function that returns true if the given file id is consistent with the one for a code snippet.
        /// </summary>
        public static bool IsCodeSnippet(string fileId) => fileId == SnippetFileId;

        /// <summary>
        /// Returns a function that given a routine for loading files from disk,
        /// return an enumerable with all text document identifiers and the corresponding file content
        /// for the source code or Q# snippet specified by the given options.
        /// If both the Input and the CodeSnippet property are set, or none of these properties is set in the given options,
        /// logs a suitable error and returns and empty dictionary.
        /// </summary>
        internal CompilationLoader.SourceLoader LoadSourcesOrSnippet(ILogger logger) => loadFromDisk =>
        {
            var input = this.Input ?? Enumerable.Empty<string>();
            if (this.CodeSnippet == null && input.Any())
            {
                return loadFromDisk(input);
            }
            else if (this.CodeSnippet != null && !input.Any())
            {
                return new Dictionary<Uri, string> { { SnippetFileUri, AsSnippet(this.CodeSnippet, this.WithinFunction) } }.ToImmutableDictionary();
            }

            if (!input.Any())
            {
                logger?.Log(ErrorCode.MissingInputFileOrSnippet, Enumerable.Empty<string>());
            }
            else
            {
                logger?.Log(ErrorCode.SnippetAndInputArguments, Enumerable.Empty<string>());
            }

            return ImmutableDictionary<Uri, string>.Empty;
        };
    }
}
