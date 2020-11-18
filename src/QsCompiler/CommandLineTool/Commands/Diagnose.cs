// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using static Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants;
using Compilation = Microsoft.Quantum.QsCompiler.CompilationBuilder.CompilationUnitManager.Compilation;

namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    public static class DiagnoseCompilation
    {
        [Verb("diagnose", HelpText = "Generates intermediate representations of the code to help diagnose issues.")]
        public class DiagnoseOptions : CompilationOptions
        {
            [Usage(ApplicationAlias = "qsCompiler")]
            public static IEnumerable<Example> UsageExamples
            {
                get
                {
                    yield return new Example(
                        "***\nCompiling a Q# snippet and getting the syntax tree",
                        new DiagnoseOptions { CodeSnippet = "let a = 0;", PrintSyntaxTree = true });
                    yield return new Example(
                        "***\nCompiling a Q# snippet and getting the tokenization",
                        new DiagnoseOptions { CodeSnippet = "let a = 0;", PrintTokenization = true });
                    yield return new Example(
                        "***\nCompiling a file containing Q# code and getting the syntax tree",
                        new DiagnoseOptions { Input = new string[] { Path.Combine("path", "to", "file.qs") }, PrintSyntaxTree = true });
                    yield return new Example(
                        "***\nCompiling a file containing Q# code and getting the Q# representation of the syntax tree",
                        new DiagnoseOptions { Input = new string[] { Path.Combine("path", "to", "file.qs") }, PrintCompiledCode = true });
                }
            }

            [Option(
                "text",
                Required = false,
                Default = false,
                HelpText = "Specifies whether to print the text representation of the code in memory.")]
            public bool PrintTextRepresentation { get; set; }

            [Option(
                "tokenization",
                Required = false,
                Default = false,
                HelpText = "Specifies whether to print the tokenization of the code.")]
            public bool PrintTokenization { get; set; }

            [Option(
                "tree",
                Required = false,
                Default = false,
                HelpText = "Specifies whether to print the serialization of the built syntax tree.")]
            public bool PrintSyntaxTree { get; set; }

            [Option(
                "code",
                Required = false,
                Default = false,
                HelpText = "Specifies whether to print the Q# code generated based on the built syntax tree.")]
            public bool PrintCompiledCode { get; set; }
        }

        /// <summary>
        /// Logs the content of each file in the given compilation as Information using the given logger.
        /// If the id of a file is consistent with the one assigned to a code snippet,
        /// strips the lines of code that correspond to the wrapping defined by WrapSnippet.
        /// </summary>
        private static void PrintFileContentInMemory(Compilation compilation, ILogger logger)
        {
            foreach (var file in compilation.SourceFiles)
            {
                IEnumerable<string> inMemory = compilation.FileContent[file];
                var stripWrapping = Options.IsCodeSnippet(file);
                QsCompilerError.Verify(
                    !stripWrapping || inMemory.Count() >= 4,
                    "expecting at least four lines of code for the compilation of a code snippet");

                if (stripWrapping)
                {
                    inMemory = inMemory.Skip(2).Take(inMemory.Count() - 4);
                }
                logger.Log(
                    InformationCode.FileContentInMemory,
                    Enumerable.Empty<string>(),
                    stripWrapping ? null : file,
                    messageParam: $"{Environment.NewLine}{string.Concat(inMemory)}");
            }
        }

        /// <summary>
        /// Logs the tokenization of each file in the given compilation as Information using the given logger.
        /// If the id of a file is consistent with the one assigned to a code snippet,
        /// strips the tokens that correspond to the wrapping defined by WrapSnippet.
        /// </summary>
        private static void PrintContentTokenization(Compilation compilation, ILogger logger)
        {
            foreach (var file in compilation.SourceFiles)
            {
                var tokenization = compilation.Tokenization[file].Select(tokens => tokens.Select(token => token.Kind));
                var stripWrapping = Options.IsCodeSnippet(file);
                QsCompilerError.Verify(
                    !stripWrapping || tokenization.Count() >= 4,
                    "expecting at least four lines of code for the compilation of a code snippet");

                if (stripWrapping)
                {
                    tokenization = tokenization.Skip(2).Take(tokenization.Count() - 4).ToImmutableArray();
                }
                var serialization = tokenization
                    .Select(line => line.Select(item => JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented)))
                    .Zip(
                        Enumerable.Range(1, tokenization.Count()),
                        (ts, i) => ts.Any() ? $"\n[ln {i}]: \n{string.Join("\n", ts)} \n" : "");

                serialization = new string[] { "" }.Concat(serialization);
                logger.Log(
                    InformationCode.BuiltTokenization,
                    Enumerable.Empty<string>(),
                    stripWrapping ? null : file,
                    messageParam: serialization.ToArray());
            }
        }

        /// <summary>
        /// Logs the part of the given evaluated syntax tree that corresponds to each file
        /// in the given compilation as Information using the given logger.
        /// If the given evaluated tree is null, queries the tree contained in the given compilation instead.
        /// If the id of a file is consistent with the one assigned to a code snippet,
        /// strips the lines of code that correspond to the wrapping defined by WrapSnippet.
        /// </summary>
        /// <exception cref="ArgumentException">This is not possible because the given syntax tree is inconsistent with that wrapping.</exception>
        private static void PrintSyntaxTree(IEnumerable<QsNamespace>? evaluatedTree, Compilation compilation, ILogger logger)
        {
            evaluatedTree ??= compilation.SyntaxTree.Values;

            foreach (var file in compilation.SourceFiles)
            {
                var stripWrapping = Options.IsCodeSnippet(file);
                var subtree = evaluatedTree.Select(ns => FilterBySourceFile.Apply(ns, file)).Where(ns => ns.Elements.Any());

                void PrintTree(string serialization) => logger.Log(
                    InformationCode.BuiltSyntaxTree,
                    Enumerable.Empty<string>(),
                    stripWrapping ? null : file,
                    messageParam: new string[] { "", serialization });

                if (!stripWrapping)
                {
                    PrintTree(JsonConvert.SerializeObject(subtree, Newtonsoft.Json.Formatting.Indented));
                }
                else
                {
                    PrintTree(JsonConvert.SerializeObject(StripSnippetWrapping(subtree), Newtonsoft.Json.Formatting.Indented));
                }
            }
        }

        /// <summary>
        /// Logs the generated Q# code for the part of the given evaluated syntax tree that corresponds to each file
        /// in the given compilation as Information using the given logger.
        /// If the given evaluated tree is null, queries the tree contained in the given compilation instead.
        /// If the id of a file is consistent with the one assigned to a code snippet,
        /// strips the lines of code that correspond to the wrapping defined by WrapSnippet.
        /// </summary>
        /// <exception cref="ArgumentException">This is not possible because the given syntax tree is inconsistent with that wrapping.</exception>
        private static void PrintGeneratedQs(IEnumerable<QsNamespace>? evaluatedTree, Compilation compilation, ILogger logger)
        {
            evaluatedTree ??= compilation.SyntaxTree.Values;

            foreach (var file in compilation.SourceFiles)
            {
                if (Options.IsCodeSnippet(file))
                {
                    var subtree = evaluatedTree.Select(ns => FilterBySourceFile.Apply(ns, file)).Where(ns => ns.Elements.Any());
                    var code = new string[] { "" }.Concat(StripSnippetWrapping(subtree).Select(SyntaxTreeToQsharp.Default.ToCode));
                    logger.Log(InformationCode.FormattedQsCode, Enumerable.Empty<string>(), messageParam: code.ToArray());
                }
                else
                {
                    var imports = evaluatedTree.ToImmutableDictionary(ns => ns.Name, ns => compilation.OpenDirectives(file, ns.Name).ToImmutableArray());
                    SyntaxTreeToQsharp.Apply(out var generated, evaluatedTree, (file, imports));
                    var code = new string[] { "" }.Concat(generated.Single().Values.Select(nsCode => $"{nsCode}{Environment.NewLine}"));
                    logger.Log(InformationCode.FormattedQsCode, Enumerable.Empty<string>(), file, messageParam: code.ToArray());
                }
            }
        }

        // publicly accessible methods

        /// <summary>
        /// Strips the namespace and callable declaration that is consistent with the wrapping defined by WrapSnippet.
        /// </summary>
        /// <exception cref="ArgumentException">This is not possible because the given syntax tree is inconsistent with that wrapping.</exception>
        public static IEnumerable<QsStatement> StripSnippetWrapping(IEnumerable<QsNamespace> syntaxTree)
        {
            var incorrectWrapperException = new ArgumentException("syntax tree does not reflect the expected wrapper");
            if (syntaxTree.Count() != 1 || syntaxTree.Single().Elements.Count() != 1)
            {
                throw incorrectWrapperException;
            }
            if (syntaxTree.Single().Elements.Single() is QsNamespaceElement.QsCallable callable &&
                callable.Item.Specializations.Count() == 1 &&
                callable.Item.Specializations.Single().Implementation is SpecializationImplementation.Provided impl)
            {
                return impl.Item2.Statements;
            }
            else
            {
                throw incorrectWrapperException;
            }
        }

        /// <summary>
        /// Builds the compilation for the Q# code or Q# snippet and referenced assemblies defined by the given options.
        /// Prints the data structures requested by the given options using the given logger.
        /// Returns a suitable error code if one of the compilation or generation steps fails.
        /// </summary>
        public static int Run(DiagnoseOptions options, ConsoleLogger logger)
        {
            if (!options.ParseAssemblyProperties(out var assemblyConstants))
            {
                logger.Log(WarningCode.InvalidAssemblyProperties, Array.Empty<string>());
            }

            var loadOptions = new CompilationLoader.Configuration
            {
                AssemblyConstants = assemblyConstants,
                TargetPackageAssemblies = options.TargetSpecificDecompositions ?? Enumerable.Empty<string>(),
                RuntimeCapability = options.RuntimeCapability,
                SkipMonomorphization = options.RuntimeCapability == RuntimeCapability.FullComputation,
                GenerateFunctorSupport = true,
                SkipSyntaxTreeTrimming = options.TrimLevel == 0,
                AttemptFullPreEvaluation = options.TrimLevel > 2,
                IsExecutable = options.MakeExecutable,
                RewriteStepAssemblies = options.Plugins?.Select(step => (step, (string?)null)) ?? ImmutableArray<(string, string)>.Empty,
                EnableAdditionalChecks = true,
                ExposeReferencesViaTestNames = options.ExposeReferencesViaTestNames
            };
            var loaded = new CompilationLoader(
                options.LoadSourcesOrSnippet(logger),
                options.References ?? Enumerable.Empty<string>(),
                loadOptions,
                logger);
            if (loaded.VerifiedCompilation == null)
            {
                return ReturnCode.Status(loaded);
            }

            if (logger.Verbosity < DiagnosticSeverity.Information)
            {
                logger.Verbosity = DiagnosticSeverity.Information;
            }
            if (options.PrintTextRepresentation)
            {
                PrintFileContentInMemory(loaded.VerifiedCompilation, logger);
            }
            if (options.PrintTokenization)
            {
                PrintContentTokenization(loaded.VerifiedCompilation, logger);
            }
            if (options.PrintSyntaxTree)
            {
                PrintSyntaxTree(loaded.CompilationOutput?.Namespaces, loaded.VerifiedCompilation, logger);
            }
            if (options.PrintCompiledCode)
            {
                PrintGeneratedQs(loaded.CompilationOutput?.Namespaces, loaded.VerifiedCompilation, logger);
            }
            return ReturnCode.Status(loaded);
        }
    }
}
