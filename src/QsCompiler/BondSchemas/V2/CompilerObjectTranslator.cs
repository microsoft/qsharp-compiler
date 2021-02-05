// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bond;
using Microsoft.Quantum.QsCompiler.BondSchemas.V1;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.V2
{
    /// <summary>
    /// This class translates Bond schema objects to C# compiler objects.
    /// </summary>
    internal static class CompilerObjectTranslator
    {
        /// <summary>
        /// Creates a C# QsCompilation compiler object from a Bond schema QsCompilation object.
        /// </summary>
        public static SyntaxTree.QsCompilation CreateQsCompilation(
            QsCompilation bondCompilation) =>
                new SyntaxTree.QsCompilation(
                    namespaces: bondCompilation.Namespaces.Select(n => n.ToCompilerObject()).ToImmutableArray(),
                    entryPoints: bondCompilation.EntryPoints.Select(e => e.ToCompilerObject()).ToImmutableArray());

        internal static SyntaxTree.QsNamespace ToCompilerObject(
            this QsNamespace bondQsNamespace) =>
                new SyntaxTree.QsNamespace(
                    name: bondQsNamespace.Name,
                    elements: bondQsNamespace.Elements.Select(e => e.ToCompilerObject()).ToImmutableArray(),
                    documentation: bondQsNamespace.Documentation.ToCompilerObject());

        internal static ILookup<string, ImmutableArray<string>> ToCompilerObject(
            this LinkedList<IBonded<QsSourceFileDocumentation>> documentation)
        {
            var deserializedDocumentation = documentation.Select(d => d.Deserialize());
            return deserializedDocumentation.ToLookup(
                p => p.FileName,
                p => p.DocumentationItems.ToImmutableArray());
        }
    }
}
