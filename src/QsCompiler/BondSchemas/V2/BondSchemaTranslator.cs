// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bond;
using Microsoft.Quantum.QsCompiler.BondSchemas.V1;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.V2
{
    /// <summary>
    /// This class translates compiler objects to Bond schema objects.
    /// </summary>
    internal static class BondSchemaTranslator
    {
        /// <summary>
        /// Creates a Bond schema QsCompilation object from a QsCompilation compiler object.
        /// </summary>
        public static QsCompilation CreateBondCompilation(SyntaxTree.QsCompilation qsCompilation) =>
            new QsCompilation
            {
                Namespaces = qsCompilation.Namespaces.Select(n => n.ToBondSchema()).ToList(),
                EntryPoints = qsCompilation.EntryPoints.Select(e => e.ToBondSchema()).ToList()
            };

        internal static QsNamespace ToBondSchema(this SyntaxTree.QsNamespace qsNamespace) =>
            new QsNamespace
            {
                Name = qsNamespace.Name,
                Elements = qsNamespace.Elements.Select(e => e.ToBondSchema()).ToList(),
                Documentation = qsNamespace.Documentation.ToQsSourceFileDocumentationList()
            };

        internal static LinkedList<IBonded<QsSourceFileDocumentation>> ToQsSourceFileDocumentationList(
            this ILookup<string, ImmutableArray<string>> qsDocumentation)
        {
            var documentationList = new LinkedList<IBonded<QsSourceFileDocumentation>>();
            foreach (var qsSourceFileDocumentation in qsDocumentation)
            {
                foreach (var items in qsSourceFileDocumentation)
                {
                    var qsDocumentationItem = new QsSourceFileDocumentation
                    {
                        FileName = qsSourceFileDocumentation.Key,
                        DocumentationItems = items.ToList()
                    };

                    documentationList.AddLast(new Bonded<QsSourceFileDocumentation>(qsDocumentationItem));
                }
            }

            return documentationList;
        }
    }
}
