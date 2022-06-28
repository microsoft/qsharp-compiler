// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    /// <summary>
    /// Transformation class used to generate QIR.
    /// </summary>
    public class Generator : SyntaxTreeTransformation<GenerationContext>, IDisposable
    {
        /// <summary>
        /// The compilation unit for which QIR is generated.
        /// </summary>
        public QsCompilation Compilation { get; }

        /// <summary>
        /// The runtime library that is used during QIR generation.
        /// It is automatically populated with the functions that are expected
        /// to be supported by the runtime according to the QIR specs.
        /// </summary>
        public FunctionLibrary RuntimeLibrary { get; }

        /// <summary>
        /// The quantum instruction set that is used during QIR generation.
        /// It is automatically populated with the callables that are
        /// declared as intrinsic in the compilation.
        /// </summary>
        public FunctionLibrary QuantumInstructionSet { get; }

        /// <summary>
        /// Instantiates a transformation capable of emitting QIR for the given compilation.
        /// </summary>
        /// <param name="compilation">The compilation for which to generate QIR</param>
        public Generator(QsCompilation compilation, TargetCapability? capability = null)
        : base(
            new GenerationContext(
                compilation.Namespaces,
                compilation.EntryPoints.Length == 0,
                capability ?? TargetCapabilityModule.Top),
            TransformationOptions.NoRebuild)
        {
            this.Compilation = compilation;
            var publicAPI = ImmutableHashSet.CreateRange(
                this.SharedState.IsLibrary // it would be nicer to not have 4 cases...
                ? compilation.InteroperableSurface(includeReferences: false)
                    .Where(this.SharedState.TargetQirProfile ? this.BasicApiSurface : _ => true)
                : this.SharedState.TargetQirProfile ? compilation.EntryPoints : ImmutableHashSet<QsQualifiedName>.Empty);

            this.Namespaces = new QirNamespaceTransformation(this, TransformationOptions.NoRebuild, publicAPI);
            this.StatementKinds = new QirStatementKindTransformation(this, TransformationOptions.NoRebuild);
            this.Expressions = new QirExpressionTransformation(this, TransformationOptions.NoRebuild);
            this.ExpressionKinds = new QirExpressionKindTransformation(this, TransformationOptions.NoRebuild);
            this.Types = new TypeTransformation<GenerationContext>(this, TransformationOptions.Disabled);

            // needs to be *after* the proper subtransformations are set
            this.SharedState.SetTransformation(this, out var runtimeLibrary, out var quantumInstructionSet);
            this.RuntimeLibrary = runtimeLibrary;
            this.QuantumInstructionSet = quantumInstructionSet;
            this.SharedState.InitializeRuntimeLibrary();
            this.SharedState.RegisterQuantumInstructionSet();
        }

        /// <summary>
        /// Returns true if the callable with the give name does not take arguments or return values
        /// that require support for composite types (including callable types) or require a classical runtime.
        /// Qubits and results are assumed to not require special runtime support.
        /// </summary>
        private bool BasicApiSurface(QsQualifiedName callableName)
        {
            bool ContainsCompositeType(ResolvedType t) =>
                t.Resolution.IsArrayType ||
                t.Resolution.IsTupleType || t.Resolution.IsUserDefinedType ||
                t.Resolution.IsOperation || t.Resolution.IsFunction;

            bool RequiresClassicalRuntime(ResolvedType t) =>
                t.Resolution.IsBigInt || t.Resolution.IsString;

            bool IsSimpleSignature(ResolvedSignature sig) =>
                !(ContainsCompositeType(sig.ArgumentType) || RequiresClassicalRuntime(sig.ArgumentType)) &&
                !(ContainsCompositeType(sig.ReturnType) || RequiresClassicalRuntime(sig.ReturnType));

            return this.SharedState.TryGetGlobalCallable(callableName, out var callable)
                && IsSimpleSignature(callable.Signature);
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

            // TODO: get rid of entry point and interop wrappers
            // (requires simulator update to align in- and output processing)
            // Update the generation of the entry point attribute in the Namespace transformation accordingly.
            if (!this.SharedState.TargetQirProfile)
            {
                foreach (var epName in this.Compilation.EntryPoints)
                {
                    this.SharedState.CreateInteropFriendlyWrapper(epName);
                    this.SharedState.CreateEntryPoint(epName);
                }
            }

            this.SharedState.GenerateRequiredFunctions();
        }

        /// <summary>
        /// Writes the current content to the output file.
        /// Call <see cref="Apply"/> first to populate the generator.
        /// </summary>
        /// <param name="fileName">The file to which the output is written.</param>
        /// <param name="emitBitcode">False if the file should be human readable, true if the file should contain bitcode.</param>
        /// <param name="overwrite">Whether or not to overwrite a file if it already exists.</param>
        /// <returns>True if the file has been created successfully, and false otherwise.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a file with the given name already exists, but <paramref name="overwrite"/> has been set to false.
        /// </exception>
        public bool Emit(string fileName, bool emitBitcode = false, bool overwrite = true, Action<string>? onError = null)
        {
            if (!overwrite && File.Exists(fileName))
            {
                throw new InvalidOperationException($"The file \"{fileName}\" already exist(s).");
            }

            var valid = this.SharedState.Module.Verify(out string validationErrors);
            if (!valid)
            {
                onError?.Invoke(validationErrors);
            }

            // We generate human readable LLVM IR if the IR is invalid,
            // independent on whether bitcode has been requested or not.
            if (valid && emitBitcode)
            {
                this.SharedState.Module.WriteToFile(fileName);
                return true;
            }
            else
            {
                if (!this.SharedState.Module.WriteToTextFile(fileName, out string fileErrors))
                {
                    onError?.Invoke(fileErrors);
                    return false;
                }

                return valid;
            }
        }

        /// <summary>
        /// Creates a string with the current QIR.
        /// Call <see cref="Apply"/> first to populate the generator.
        /// </summary>
        /// <returns>True if the QIR passes basic LLVM validation.</returns>
        public bool EmitLlvmIR(out string llvmIR, Action<string>? onError = null)
        {
            var valid = this.SharedState.Module.Verify(out string validationErrors);
            if (!valid)
            {
                onError?.Invoke(validationErrors);
            }

            llvmIR = this.SharedState.Module.WriteToString();
            return valid;
        }

        public void Dispose() =>
            this.SharedState.Dispose();
    }
}
