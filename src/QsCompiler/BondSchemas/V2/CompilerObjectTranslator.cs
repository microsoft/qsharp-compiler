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
    internal static class CompilerObjectTranslator
    {

        /// <summary>
        /// Creates a C# QsCompilation compiler object from a Bond schema QsCompilation object.
        /// </summary>
        /// //
        public static SyntaxTree.QsCompilation CreateQsCompilation(
            QsCompilation bondCompilation,
            Protocols.Option[]? options = null) =>
                new SyntaxTree.QsCompilation(
                    namespaces: bondCompilation.Namespaces.Select(n => n.ToCompilerObject(options)).ToImmutableArray(),
                    entryPoints: bondCompilation.EntryPoints.Select(e => e.ToCompilerObject()).ToImmutableArray());

        internal static SyntaxTree.QsNamespace ToCompilerObject(
            this QsNamespace bondQsNamespace,
            Protocols.Option[]? options = null) =>
                new SyntaxTree.QsNamespace(
                    name: bondQsNamespace.Name,
                    elements: bondQsNamespace.Elements.Select(e => e.ToCompilerObject()).ToImmutableArray(),
                    documentation: bondQsNamespace.Documentation.ToCompilerObject(options));

        internal static ILookup<string, ImmutableArray<string>> ToCompilerObject(
            this LinkedList<IBonded<QsSourceFileDocumentation>> documentation,
            Protocols.Option[]? options = null)
        {
            IEnumerable<QsSourceFileDocumentation> deserializedDocumentation;
            if ((options != null) &&
                options.Contains(Protocols.Option.ExcludeNamespaceDocumentation))
            {
                deserializedDocumentation = Enumerable.Empty<QsSourceFileDocumentation>();
            }
            else
            {
                deserializedDocumentation = documentation.Select(d => d.Deserialize());
            }

            return deserializedDocumentation.ToLookup(
                p => p.FileName,
                p => p.DocumentationItems.ToImmutableArray());
        }
    }
}
