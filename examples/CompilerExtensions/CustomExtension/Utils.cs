// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;


namespace Microsoft.Quantum.Demos.CompilerExtensions.Demo
{
    /// <summary>
    /// Used to display the current state of a Q# compilation as source code.
    /// </summary>
    internal class Display
    {
        private readonly QsCompilation Compilation;
        private readonly ImmutableHashSet<string> SourceFiles;
        private readonly ImmutableDictionary<string, ImmutableArray<(string, string)>> Imports;

        internal Display(QsCompilation compilation)
        {
            this.Compilation = compilation;
            this.SourceFiles = GetSourceFiles.Apply(compilation.Namespaces);
            this.Imports = compilation.Namespaces.ToImmutableDictionary(ns => ns.Name, _ => ImmutableArray<(string, string)>.Empty);
        }

        /// <summary>
        /// Extracts the compiled data structures for all type and callables defined in the specified sourceFile.
        /// Generates the corresponding Q# code, which represents the implementation after compilation opposed to the original source code.
        /// Displays the generated code using the default editor for text files.
        /// If no editor is found to open a text file, the output is logged to the console.
        /// If no sourceFile is specified, then the implementation for the compiled types and callables defined in all source files are displayed to the user.
        /// </summary>
        public void Show(string sourceFile = null)
        {
            var filesToShow = sourceFile == null
                ? this.SourceFiles.Where(f => f.EndsWith(".qs")).OrderBy(f => f).Select(f => (f, this.Imports)).ToArray()
                : new[] { (sourceFile, this.Imports) };

            SyntaxTreeToQsharp.Apply(out List<ImmutableDictionary<string, string>> generated, this.Compilation.Namespaces, filesToShow);
            var code = generated.SelectMany((namespaces, sourceIndex) =>
                namespaces.Values
                .Prepend($"// Compiled Q# code from file \"{filesToShow[sourceIndex].Item1}\":")
                .Select(nsCode => $"{nsCode}{Environment.NewLine}"));

            try
            {
                var tempFile = Path.GetTempFileName() + ".txt";
                File.WriteAllLines(tempFile, code);
                if (Environment.OSVersion.Platform != PlatformID.Win32NT) Process.Start(tempFile);
                else Process.Start("notepad.exe", tempFile);
            }
            catch
            {
                Console.WriteLine($"{Environment.NewLine}******");
                Console.Write(String.Join(Environment.NewLine, code));
                Console.WriteLine("******");
            }
        }

    }
}