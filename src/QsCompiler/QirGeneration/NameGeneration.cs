// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Targeting;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    public static class NameGeneration
    {
        /// <summary>
        /// Cleans a namespace name by replacing periods with double underscores.
        /// </summary>
        /// <param name="namespaceName">The namespace name to clean</param>
        /// <returns>The cleaned name</returns>
        internal static string FlattenNamespaceName(string namespaceName) =>
            namespaceName.Replace(".", "__");

        /// <summary>
        /// The name of the interop-friendly wrapper function for the callable.
        /// </summary>
        public static string InteropFriendlyWrapperName(QsQualifiedName fullName) =>
            $"{FlattenNamespaceName(fullName.Namespace)}__{fullName.Name}__Interop";

        /// <summary>
        /// The name of the entry point function calling into the body of the callable with the given name.
        /// </summary>
        public static string EntryPointName(QsQualifiedName fullName) =>
            $"{FlattenNamespaceName(fullName.Namespace)}__{fullName.Name}";

        /// <summary>
        /// Generates a mangled name for a callable specialization.
        /// QIR mangled names are the namespace name, with periods replaced by double underscores, followed
        /// by a double underscore and the callable name, then another double underscore and the name of the
        /// callable kind ("body", "adj", "ctl", or "ctladj").
        /// </summary>
        /// <param name="fullName">The callable's qualified name.</param>
        /// <param name="kind">The specialization kind.</param>
        /// <returns>The mangled name for the specialization.</returns>
        public static string FunctionName(QsQualifiedName fullName, QsSpecializationKind kind)
        {
            var suffix = InferTargetInstructions.SpecializationSuffix(kind).ToLowerInvariant();
            return $"{FlattenNamespaceName(fullName.Namespace)}__{fullName.Name}{suffix}";
        }

        /// <summary>
        /// Generates a mangled name for a callable specialization wrapper.
        /// Wrapper names are the mangled specialization name followed by double underscore and "wrapper".
        /// </summary>
        /// <param name="fullName">The callable's qualified name</param>
        /// <param name="kind">The specialization kind.</param>
        /// <returns>The mangled name for the wrapper.</returns>
        public static string FunctionWrapperName(QsQualifiedName fullName, QsSpecializationKind kind) =>
            $"{FunctionName(fullName, kind)}__wrapper";

        /// <returns>
        /// Returns true and the target instruction name for the callable as out parameter
        /// if a target instruction exists for the callable.
        /// Returns false otherwise.
        /// </returns>
        internal static bool TryGetTargetInstructionName(QsCallable callable, [MaybeNullWhen(false)] out string instructionName)
        {
            if (SymbolResolution.TryGetTargetInstructionName(callable.Attributes) is var att && att.IsValue)
            {
                instructionName = att.Item;
                return true;
            }
            else
            {
                instructionName = null;
                return false;
            }
        }
    }
}
