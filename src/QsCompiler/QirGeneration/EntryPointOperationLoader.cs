// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.QIR.BondSchemas;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    public static class EntryPointOperationLoader
    {
        public static IList<EntryPointOperation> LoadEntryPointOperations(FileInfo assemblyFileInfo)
        {
            if (!AssemblyLoader.LoadReferencedAssembly(assemblyFileInfo.FullName, out var compilation))
            {
                throw new ArgumentException("Unable to read the Q# syntax tree from the given DLL.");
            }
            return GenerateEntryPointOperations(compilation);
        }

        private static IList<EntryPointOperation> GenerateEntryPointOperations(QsCompilation compilation)
        {
            return compilation.EntryPoints.Select(ep => new EntryPointOperation()
            {
                Name = NameGeneration.InteropFriendlyWrapperName(ep)
            }).ToList();
        }
    }
}
