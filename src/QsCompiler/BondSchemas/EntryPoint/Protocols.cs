// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Bond;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint
{
    /// <summary>
    /// This class provides methods for serialization/deserialization of objects in the Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint namespace.
    /// </summary>
    public static class Protocols
    {
        /// <summary>
        /// Deserializes an EntryPointOperation object from its JSON representation.
        /// </summary>
        public static EntryPointOperation DeserializeFromJson(Stream stream)
        {
            var reader = new SimpleJsonReader(stream);
            var deserializer = new Deserializer<SimpleJsonReader>(typeof(EntryPointOperation));
            var entryPoint = deserializer.Deserialize<EntryPointOperation>(reader);
            return entryPoint;
        }

        /// <summary>
        /// Serializes an EntryPointOperation object to its JSON representation.
        /// </summary>
        public static void SerializeToJson(EntryPointOperation entryPoint, Stream stream)
        {
            var writer = new SimpleJsonWriter(stream);
            var serializer = new Serializer<SimpleJsonWriter>(typeof(EntryPointOperation));
            serializer.Serialize(entryPoint, writer);
            writer.Flush();
            stream.Flush();
            stream.Position = 0;
        }
    }
}
