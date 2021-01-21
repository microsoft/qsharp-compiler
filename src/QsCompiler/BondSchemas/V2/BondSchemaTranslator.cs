// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
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

        public static QsNamespace ToBondSchema(this SyntaxTree.QsNamespace qsNamespace) =>
            new QsNamespace
            {
                Name = qsNamespace.Name,
                Elements = qsNamespace.Elements.Select(e => e.ToBondSchema()).ToList(),
                // TODO: Implement.
                //Documentation = qsNamespace.Documentation.ToQsSourceFileDocumentationList()
            };
    }
}
