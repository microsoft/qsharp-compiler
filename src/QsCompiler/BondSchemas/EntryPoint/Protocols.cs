// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Bond;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint
{
    public static class Protocols
    {
        public static void SerializeToJson(EntryPointOperation entryPoint, Stream stream)
        {
            var writer = new SimpleJsonWriter(stream);
            var serializer = new Serializer<SimpleJsonWriter>(typeof(EntryPointOperation), true);
            serializer.Serialize(entryPoint, writer);
            writer.Flush();
            stream.Flush();
            stream.Position = 0;
        }
    }
}
