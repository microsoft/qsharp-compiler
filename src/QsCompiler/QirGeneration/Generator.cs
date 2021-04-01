// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    /// <summary>
    /// Transformation class used to generate QIR.
    /// </summary>
    public class Generator : SyntaxTreeTransformation<GenerationContext>
    {
        /// <summary>
        /// The compilation unit for which QIR is generated.
        /// </summary>
        public readonly QsCompilation Compilation;

        /// <summary>
        /// The runtime library that is used during QIR generation.
        /// It is automatically populated with the functions that are expected
        /// to be supported by the runtime according to the QIR specs.
        /// </summary>
        public readonly FunctionLibrary RuntimeLibrary;

        /// <summary>
        /// The quantum instruction set that is used during QIR generation.
        /// It is automatically populated with the callables that are
        /// declared as intrinsic in the compilation.
        /// </summary>
        public readonly FunctionLibrary QuantumInstructionSet;

        /// <summary>
        /// Instantiates a transformation capable of emitting QIR for the given compilation.
        /// </summary>
        /// <param name="compilation">The compilation for which to generate QIR</param>
        public Generator(QsCompilation compilation)
        : base(new GenerationContext(compilation.Namespaces), TransformationOptions.NoRebuild)
        {
            this.Compilation = compilation;

            this.Namespaces = new QirNamespaceTransformation(this, TransformationOptions.NoRebuild);
            this.StatementKinds = new QirStatementKindTransformation(this, TransformationOptions.NoRebuild);
            this.Expressions = new QirExpressionTransformation(this, TransformationOptions.NoRebuild);
            this.ExpressionKinds = new QirExpressionKindTransformation(this, TransformationOptions.NoRebuild);
            this.Types = new QirTypeTransformation(this, TransformationOptions.NoRebuild);

            // needs to be *after* the proper subtransformations are set
            this.SharedState.SetTransformation(this, out this.RuntimeLibrary, out this.QuantumInstructionSet);
            this.SharedState.InitializeRuntimeLibrary();
            this.SharedState.RegisterQuantumInstructionSet();
        }

        /// <summary>
        /// Constructs the QIR for the compilation, including interop-friendly functions for entry points.
        /// Does not emit anything; use <see cref="Emit"/> to output the constructed QIR.
        /// </summary>
        public void Apply()
        {
            foreach (var ns in this.Compilation.Namespaces)
            {
                this.Namespaces.OnNamespace(ns);
            }

            foreach (var epName in this.Compilation.EntryPoints)
            {
                this.SharedState.CreateInteropFriendlyWrapper(epName);
                this.SharedState.CreateEntryPoint(epName);
            }
        }

        /// <summary>
        /// Writes the current content to the output file.
        /// </summary>
        /// <param name="fileName">
        /// The file to which the output is written. The file extension is replaced with a .ll extension.
        /// </param>
        /// <param name="overwrite">Whether or not to overwrite a file if it already exists.</param>
        public void Emit(string fileName, bool overwrite = true) =>
            this.SharedState.Emit(fileName, overwrite: overwrite);
    }
}
