// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;


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
        [Option("trim", Required = false, Default = DefaultOptions.TrimLevel, SetName = CODE_MODE,
        HelpText = "[Experimental feature] Integer indicating how much to simplify the syntax tree by eliminating selective abstractions.")]
        public int TrimLevel { get; set; }

        [Option("load", Required = false, SetName = CODE_MODE,
        HelpText = "[Experimental feature] Path to the .NET Core dll(s) defining additional transformations to include in the compilation process.")]
        public IEnumerable<string> Plugins { get; set; }

        [Option("target-package", Required = false, SetName = CODE_MODE,
        HelpText = "Path to the NuGet package containing target specific information and implementations.")]
        public string TargetPackage { get; set; }

        [Option("load-test-names", Required = false, Default = false, SetName = CODE_MODE,
        HelpText = "Specifies whether public types and callables declared in referenced assemblies are exposed via their test name defined by the corresponding attribute.")]
        public bool ExposeReferencesViaTestNames { get; set; }

        [Option("assembly-properties", Required = false, SetName = CODE_MODE,
        HelpText = "Additional properties to populate the AssemblyConstants dictionary with. Each item is expected to be of the form \"key:value\".")]
        public IEnumerable<string> AdditionalAssemblyProperties { get; set; }

        [Option("runtime", Required = false, SetName = CODE_MODE, 
        HelpText = "Specifies the classical capabilites of the runtime. Determines what QIR profile to compile to.")]
        public AssemblyConstants.RuntimeCapabilities RuntimeCapabilites { get; set; }

        /// <summary>
        /// Returns a dictionary with the specified assembly properties as out parameter. 
        /// Returns a boolean indicating whether all specified properties were successfully added.
        /// </summary>
        internal bool ParseAssemblyProperties(out Dictionary<string, string> parsed)
        {
            var success = true;
            parsed = new Dictionary<string, string>();
            foreach (var keyValue in this.AdditionalAssemblyProperties ?? new string[0])
            {
                var pieces = keyValue?.Split(":");
                var valid = pieces != null && pieces.Length == 2;
                success = valid && parsed.TryAdd(pieces[0].Trim().Trim('"'), pieces[1].Trim().Trim('"')) && success;
            }
            return success;
        }

        /// <summary>
        /// Returns null if TargetPackage is null or empty, and 
        /// returns the path to the assembly containing target specific implementations otherwise.
        /// If a logger is specified, logs suitable diagnostics if a TargetPackages is not null or empty,
        /// but no path to the target package assembly could be determined. 
        /// This may be the case if no directory at the TargetPackage location exists, or if its files can't be accessed, 
        /// or more than one dll matches the pattern by which the target package assembly is identified.
        /// </summary>
        public string GetTargetPackageAssemblyPath(ILogger logger = null)
        {
            if (String.IsNullOrEmpty(this.TargetPackage)) return null;
            try
            {
                // Disclaimer: we may revise that in the future.
                var targetPackageAssembly = Directory.GetFiles(this.TargetPackage, "*Intrinsics.dll", SearchOption.AllDirectories).SingleOrDefault();
                if (targetPackageAssembly != null) return targetPackageAssembly;
            }
            catch (Exception ex)
            {
                if (Directory.Exists(this.TargetPackage)) logger?.Log(ex);
                else logger?.Log(ErrorCode.CouldNotFindTargetPackage, new[] { this.TargetPackage });
            }

            logger?.Log(ErrorCode.CouldNotFindTargetPackageAssembly, new[] { this.TargetPackage });
            return null;
        }
    }


    public class Options
    {
        public enum LogFormat
        {
            Default,
            MsBuild
        }

        // Note: items in one set are mutually exclusive with items from other sets
        protected const string CODE_MODE = "codeMode";
        protected const string SNIPPET_MODE = "snippetMode";
        protected const string RESPONSE_FILES = "responseFiles";

        [Option('v', "verbosity", Required = false, Default = DefaultOptions.Verbosity,
        HelpText = "Specifies the verbosity of the logged output. Valid values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].")]
        public string Verbosity { get; set; }

        [Option("format", Required = false, Default = DefaultOptions.OutputFormat,
        HelpText = "Specifies the output format of the command line compiler.")]
        public LogFormat OutputFormat { get; set; }

        [Option('i', "input", Required = true, SetName = CODE_MODE,
        HelpText = "Q# code or name of the Q# file to compile.")]
        public IEnumerable<string> Input { get; set; }

        [Option('s', "snippet", Required = true, SetName = SNIPPET_MODE,
        HelpText = "Q# snippet to compile - i.e. Q# code occuring within an operation or function declaration.")]
        public string CodeSnippet { get; set; }

        [Option('f', "within-function", Required = false, Default = false, SetName = SNIPPET_MODE,
        HelpText = "Specifies whether a given Q# snipped occurs within a function")]
        public bool WithinFunction { get; set; }

        [Option('r', "references", Required = false, Default = new string[0],
        HelpText = "Referenced binaries to include in the compilation.")]
        public IEnumerable<string> References { get; set; }

        [Option('n', "no-warn", Required = false, Default = new int[0],
        HelpText = "Warnings with the given code(s) will be ignored.")]
        public IEnumerable<int> NoWarn { get; set; }

        [Option("package-load-fallback-folders", Required = false, SetName = CODE_MODE,
        HelpText = "Specifies the directories the compiler will search when a compiler dependency could not be found.")]
        public IEnumerable<string> PackageLoadFallbackFolders { get; set; }


        /// <summary>
        /// Updates the settings that can be used independent on the other arguments according to the setting in the given options.
        /// Already specified non-default values are prioritized over the values in the given options, 
        /// unless overwriteNonDefaultValues is set to true. Sequences are merged. 
        /// </summary>
        internal void UpdateSetIndependentSettings(Options updates, bool overwriteNonDefaultValues = false)
        {
            this.Verbosity = overwriteNonDefaultValues || this.Verbosity == DefaultOptions.Verbosity ? updates.Verbosity : this.Verbosity;
            this.OutputFormat = overwriteNonDefaultValues || this.OutputFormat == DefaultOptions.OutputFormat ? updates.OutputFormat : this.OutputFormat;
            this.NoWarn = (this.NoWarn ?? new int[0]).Concat(updates.NoWarn ?? new int[0]);
            this.References = (this.References ?? new string[0]).Concat(updates.References ?? new string[0]);
        }


        // routines related to logging 

        /// <summary>
        /// If a logger is given, logs the options as CommandLineArguments Information before returning the printed string. 
        /// </summary>
        public string[] Print(ILogger logger = null)
        {
            string value(PropertyInfo p)
            {
                var v = p.GetValue(this);
                return v is String[] a 
                    ? String.Join(';', a)
                    : v?.ToString() ?? "(null)";
            }

            var props = this.GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(OptionAttribute)));
            var msg = props.Select(p => $"{p.Name}: {value(p)}").ToArray();
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
        /// Creates a suitable logger for the given command line options, 
        /// logging the given arguments if the verbosity is high enough.
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
            this.Print(logger);
            return logger;
        }


        // routines related to processing snippets

        /// <summary>
        /// text document identifier used to identify the code snippet in diagnostic mode
        /// </summary>
        private static readonly Uri SNIPPET_FILE_URI = new Uri(Path.GetFullPath("__CODE_SNIPPET__.qs"));
        private static NonNullable<string> SNIPPET_FILE_ID
        {
            get
            {
                QsCompilerError.Verify(
                    CompilationUnitManager.TryGetFileId(SNIPPET_FILE_URI, out NonNullable<string> id),
                    "invalid code snippet id");
                return id;
            }
        }

        /// <summary>
        /// name of the namespace within which code snippets are compiled
        /// </summary>
        private const string SNIPPET_NAMESPACE = "_CODE_SNIPPET_NS_";
        /// <summary>
        /// name of the callable within which code snippets are compiled
        /// </summary>
        private const string SNIPPET_CALLABLE = "_CODE_SNIPPET_CALLABLE_";

        /// <summary>
        /// wraps the given content into a namespace and callable that maps Unit to Unit
        /// </summary>
        public static string AsSnippet(string content, bool inFunction = false) =>
            $"{Declarations.Namespace} {SNIPPET_NAMESPACE} {{ \n " +
            $"{(inFunction ? Declarations.Function : Declarations.Operation)} {SNIPPET_CALLABLE} () : {Types.Unit} {{ \n" +
            $"{content} \n" + // no indentation such that position info is accurate
            $"}} \n" +
            $"}}";

        /// <summary>
        /// Helper function that returns true if the given file id is consistent with the one for a code snippet. 
        /// </summary>
        public static bool IsCodeSnippet(NonNullable<string> fileId) =>
            fileId.Value == SNIPPET_FILE_ID.Value;


        /// <summary>
        /// Returns a function that given a routine for loading files from disk, 
        /// return an enumerable with all text document identifiers and the corresponding file content 
        /// for the source code or Q# snippet specified by the given options. 
        /// If both the Input and the CodeSnippet property are set, or none of these properties is set in the given options, 
        /// logs a suitable error and returns and empty dictionary.
        /// </summary>
        internal CompilationLoader.SourceLoader LoadSourcesOrSnippet (ILogger logger) => loadFromDisk =>
        {
            bool inputIsEmptyOrNull = this.Input == null || !this.Input.Any();
            if (this.CodeSnippet == null && !inputIsEmptyOrNull)
            { return loadFromDisk(this.Input); }
            else if (this.CodeSnippet != null && inputIsEmptyOrNull)
            { return new Dictionary<Uri, string> { { SNIPPET_FILE_URI, AsSnippet(this.CodeSnippet, this.WithinFunction) } }.ToImmutableDictionary(); }

            if (inputIsEmptyOrNull) logger?.Log(ErrorCode.MissingInputFileOrSnippet, Enumerable.Empty<string>());
            else logger?.Log(ErrorCode.SnippetAndInputArguments, Enumerable.Empty<string>());
            return ImmutableDictionary<Uri, string>.Empty;
        };
    }
}
