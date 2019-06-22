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

        [Option('v', "verbose", Required = false, Default = false,
        HelpText = "Specifies whether to compile in verbose mode.")]
        public bool Verbose { get; set; }

        [Option('i', "input", Required = true, SetName = CODE_MODE,
        HelpText = "Q# code or name of the Q# file to compile.")]
        public IEnumerable<string> Input { get; set; }

        [Option('s', "snippet", Required = true, SetName = SNIPPET_MODE,
        HelpText = "Q# snippet to compile - i.e. Q# code occuring within an operation or function declaration.")]
        public string CodeSnippet { get; set; }

        [Option('f', "withinFunction", Required = false, Default = false, SetName = SNIPPET_MODE,
        HelpText = "Specifies whether a given Q# snipped occurs within a function")]
        public bool WithinFunction { get; set; }

        [Option('r', "references", Required = false, Default = new string[0],
        HelpText = "Referenced binaries to include in the compilation.")]
        public IEnumerable<string> References { get; set; }

        [Option('n', "noWarn", Required = false, Default = new int[0],
        HelpText = "Warnings with the given code(s) will be ignored.")]
        public IEnumerable<int> NoWarn { get; set; }

        [Option("format", Required = false, Default = LogFormat.Default,
        HelpText = "Specifies the output format of the command line compiler.")]
        public LogFormat OutputFormat { get; set; }


        // routines related to logging 

        /// If a logger is given, logs the options as CommandLineArguments Information before returning the printed string. 
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

        /// Given a LogFormat, returns a suitable routing for formatting diagnostics.
        internal static Func<Diagnostic, string> LoggingFormat(LogFormat format)
        {
            switch (format)
            {
                case LogFormat.MsBuild: return Formatting.MsBuildFormat;
                case LogFormat.Default: return Formatting.HumanReadableFormat;
                default: throw new NotImplementedException("unknown output format for logger");
            }
        }

        /// Creates a suitable logger for the given command line options, 
        /// logging the given arguments if the verbosity is high enough.
        public ConsoleLogger GetLogger(DiagnosticSeverity minimumVerbosity = DiagnosticSeverity.Warning)
        {
            var logger = new ConsoleLogger(
                LoggingFormat(this.OutputFormat),
                this.Verbose ? DiagnosticSeverity.Hint : minimumVerbosity,
                this.NoWarn,
                this.CodeSnippet != null ? -2 : 0);
            this.Print(logger);
            return logger;
        }


        // routines related to processing snippets

        /// text document identifier used to identify the code snippet in diagnostic mode
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

        /// name of the namespace within which code snippets are compiled
        private const string SNIPPET_NAMESPACE = "_CODE_SNIPPET_NS_";
        /// name of the callable within which code snippets are compiled
        private const string SNIPPET_CALLABLE = "_CODE_SNIPPET_CALLABLE_";

        /// wraps the given content into a namespace and callable that maps Unit to Unit
        public static string AsSnippet(string content, bool inFunction = false) =>
            $"{Declarations.Namespace} {SNIPPET_NAMESPACE} {{ \n " +
            $"{(inFunction ? Declarations.Function : Declarations.Operation)} {SNIPPET_CALLABLE} () : {Types.Unit} {{ \n" +
            $"{content} \n" + // no indentation such that position info is accurate
            $"}} \n" +
            $"}}";

        /// helper function that returns true if the given file id is the one a compilation unit manager would assign to the code snipped 
        public static bool IsCodeSnippet(NonNullable<string> fileId) =>
            fileId.Value == SNIPPET_FILE_ID.Value;


        /// Returns a function that given a routine for loading files from disk, 
        /// return an enumerable with all text document identifiers and the corresponding file content 
        /// for the source code or Q# snippet specified by the given options. 
        /// If both the Input and the CodeSnippet property are set, or none of these properties is set in the given options, 
        /// logs a suitable error and returns and empty dictionary.
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
