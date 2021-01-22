// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.BondSchemas.V1;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.V2
{
    internal static class CompilerObjectTranslator
    {

        /// <summary>
        /// Creates a C# QsCompilation compiler object from a Bond schema QsCompilation object.
        /// </summary>
        /// //
        public static SyntaxTree.QsCompilation CreateQsCompilation(QsCompilation bondCompilation) =>
            new SyntaxTree.QsCompilation(
                namespaces: bondCompilation.Namespaces.Select(n => n.ToCompilerObject()).ToImmutableArray(),
                entryPoints: bondCompilation.EntryPoints.Select(e => e.ToCompilerObject()).ToImmutableArray());

        public static SyntaxTree.QsNamespace ToCompilerObject(this QsNamespace bondQsNamespace) =>
            new SyntaxTree.QsNamespace(
                name: bondQsNamespace.Name,
                elements: bondQsNamespace.Elements.Select(e => e.ToCompilerObject()).ToImmutableArray(),
                // TODO: Implement.
                documentation: default);
    }
}
