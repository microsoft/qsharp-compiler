// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Compilation = Microsoft.Quantum.QsCompiler.CompilationBuilder.CompilationUnitManager.Compilation;
using SourceFileLoader = System.Func<System.Collections.Generic.IEnumerable<string>, System.Collections.Immutable.ImmutableDictionary<System.Uri, string>>;

namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    public static class FormatCompilation
    {
        [Verb("format", HelpText = "Generates formatted Q# code.")]
        public class FormatOptions : Options
        {
            // TODO: Disabling nullable annotations is a workaround for
            // https://github.com/commandlineparser/commandline/issues/136.
#nullable disable annotations
            [Usage(ApplicationAlias = "qsCompiler")]
            public static IEnumerable<Example> UsageExamples
            {
                get
                {
                    yield return new Example(
                        "***\nFormat Q# source files in place",
                        new FormatOptions { Input = new string[] { "file1.qs", "file2.qs" }, OutputFolder = " " });
                    yield return new Example(
                        "***\nFormat Q# source files that depend on referenced libraries and setting the output folder where the formatted files will be generated",
                        new FormatOptions { Input = new string[] { "file.qs" }, References = new string[] { "library1.dll", "library2.dll" }, OutputFolder = Path.Combine("src", "FormattedFiles") });
                }
            }

            [Option(
                'o',
                "output",
                Required = true,
                SetName = CODE_MODE,
                HelpText = "Destination folder where the formatted files will be generated.")]
            public string OutputFolder { get; set; }
#nullable restore annotations
        }

        /// <summary>
        /// Regex that matches anything within array brackets.
        /// </summary>
        public static readonly Regex WithinArrayBrackets =
            new Regex(@"\[(?:[^\[\]]|(?<ctr>\[)|(?<-ctr>\]))*(?(ctr)(?!))\]");

        /// <summary>
        /// Replaces all semicolons that occur within array brackets in the given string with commas.
        /// </summary>
        public static string UpdateArrayLiterals(string fileContent)
        {
            string ReplaceSemicolons(Match match) => match.Value.Replace(';', ',');
            return WithinArrayBrackets.Replace(fileContent, ReplaceSemicolons);
        }

        /// <summary>
        /// Generates formatted Q# code based on the part of the syntax tree that corresponds to each file in the given compilation.
        /// If the id of a file is consistent with the one assigned to a code snippet,
        /// strips the lines of code that correspond to the wrapping defined by WrapSnippet.
        /// </summary>
        /// <exception cref="ArgumentException">This is not possible because the given syntax tree is inconsistent with that wrapping.</exception>
        private static IEnumerable<string> GenerateQsCode(Compilation compilation, string file, ILogger logger)
        {
            if (Options.IsCodeSnippet(file))
            {
                var subtree = compilation.SyntaxTree.Values.Select(ns => FilterBySourceFile.Apply(ns, file)).Where(ns => ns.Elements.Any());
                return DiagnoseCompilation.StripSnippetWrapping(subtree).Select(SyntaxTreeToQsharp.Default.ToCode);
            }
            else
            {
                var imports = compilation.SyntaxTree.Values
                    .ToImmutableDictionary(ns => ns.Name, ns => compilation.OpenDirectives(file, ns.Name).ToImmutableArray());
                var success = SyntaxTreeToQsharp.Apply(out List<ImmutableDictionary<string, string>> generated, compilation.SyntaxTree.Values, (file, imports));
                if (!success)
                {
                    logger?.Log(WarningCode.UnresolvedItemsInGeneratedQs, Enumerable.Empty<string>(), file);
                }

                return generated.Single().Select(entry =>
                {
                    var nsComments = compilation.NamespaceComments(file, entry.Key);
                    string FormatComments(IEnumerable<string> comments) =>
                        string.Join(
                            Environment.NewLine,
                            comments.Select(line => line.Trim()).Select(line => string.IsNullOrWhiteSpace(line) ? "" : $"// {line}"))
                        .Trim();
                    var leadingComments = entry.Value.StartsWith("///")
                        ? $"{FormatComments(nsComments.OpeningComments)}{Environment.NewLine}"
                        : FormatComments(nsComments.OpeningComments);
                    var trailingComments = FormatComments(nsComments.ClosingComments);

                    var code = new string[] { leadingComments, entry.Value, trailingComments }.Where(s => !string.IsNullOrWhiteSpace(s));
                    return string.Join(Environment.NewLine, code);
                });
            }
        }

        /// <summary>
        /// Generates formatted Q# code for the file with the given uri based on the syntax tree in the given compilation.
        /// If the id of the file is consistent with the one assigned to a code snippet,
        /// logs the generated code using the given logger.
        /// Creates a file containing the generated code in the given output folder otherwise.
        /// Returns true if the generation succeeded, and false if an exception was thrown.
        /// </summary>
        private static bool GenerateFormattedQsFile(Compilation compilation, string fileName, string? outputFolder, ILogger logger)
        {
            var code = Enumerable.Empty<string>();
            try
            {
                code = code.Concat(GenerateQsCode(compilation, fileName, logger).Where(c => !string.IsNullOrWhiteSpace(c)));
            }
            catch (Exception ex)
            {
                logger?.Log(ErrorCode.QsGenerationFailed, Enumerable.Empty<string>(), fileName);
                logger?.Log(ex);
                return false;
            }

            if (Options.IsCodeSnippet(fileName))
            {
                code = new string[] { "" }.Concat(Formatting.Indent(code.ToArray()));
                logger?.Log(InformationCode.GeneratedQsCode, Enumerable.Empty<string>(), messageParam: code.ToArray());
            }
            else
            {
                var content = string.Join(Environment.NewLine, code.Select(block => $"{block}{Environment.NewLine}{Environment.NewLine}"));
                CompilationLoader.GeneratedFile(fileName, outputFolder ?? "FormattedFiles", ".qs", content);
            }
            return true;
        }

        /// <summary>
        /// Builds the compilation for the Q# code or Q# snippet and referenced assemblies defined by the given options.
        /// Generates formatted Q# code for each source file in the compilation.
        /// Returns a suitable error code if some of the source files or references could not be found or loaded, or if the Q# generation failed.
        /// Compilation errors are not reflected in the return code, but are logged using the given logger.
        /// </summary>
        public static int Run(FormatOptions options, ConsoleLogger logger)
        {
            ImmutableDictionary<Uri, string> LoadSources(SourceFileLoader loadFromDisk) =>
                options.LoadSourcesOrSnippet(logger)(loadFromDisk)
                    .ToImmutableDictionary(entry => entry.Key, entry => UpdateArrayLiterals(entry.Value)); // manually replace array literals

            // no rewrite steps, no generation
            var loaded =
                new CompilationLoader(LoadSources, options.References ?? Enumerable.Empty<string>(), logger: logger);
            if (ReturnCode.Status(loaded) == ReturnCode.UNRESOLVED_FILES)
            {
                return ReturnCode.UNRESOLVED_FILES; // ignore compilation errors
            }
            else if (loaded.VerifiedCompilation is null)
            {
                logger.Log(ErrorCode.QsGenerationFailed, Enumerable.Empty<string>());
                return ReturnCode.CODE_GENERATION_ERRORS;
            }

            // TODO: a lot of the formatting logic defined here and also in the routines above
            // is supposed to move into the compilation manager in order to be available for the language server to provide formatting
            var success = true;
            foreach (var file in loaded.VerifiedCompilation.SourceFiles)
            {
                var verbosity = logger.Verbosity;
                if (Options.IsCodeSnippet(file) && logger.Verbosity < DiagnosticSeverity.Information)
                {
                    logger.Verbosity = DiagnosticSeverity.Information;
                }
                if (!GenerateFormattedQsFile(loaded.VerifiedCompilation, file, options.OutputFolder, logger))
                {
                    success = false;
                }
                logger.Verbosity = verbosity;
            }
            return success ? ReturnCode.SUCCESS : ReturnCode.CODE_GENERATION_ERRORS;
        }
    }
}
