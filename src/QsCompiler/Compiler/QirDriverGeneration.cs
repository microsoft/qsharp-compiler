// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint;
using Microsoft.Quantum.QsCompiler.Templates;

namespace Microsoft.Quantum.QsCompiler
{
    internal static class QirDriverGeneration
    {
        public static void GenerateQirDriverCpp(EntryPointOperation entryPointOperation, Stream stream)
        {
            QirDriverCpp qirDriverCpp = new QirDriverCpp(entryPointOperation);
            var cppSource = qirDriverCpp.TransformText();
            stream.Write(Encoding.UTF8.GetBytes(cppSource));
            stream.Flush();
            stream.Position = 0;
        }
    }
}
