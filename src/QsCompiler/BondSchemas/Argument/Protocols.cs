// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Bond;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.Argument
{
    /// <summary>
    /// This class provides methods for serialization/deserialization of objects in the Microsoft.Quantum.QsCompiler.BondSchemas.Argument namespace.
    /// </summary>
    public static class Protocols
    {
        /// <summary>
        /// Deserializes an Argument object from its JSON representation.
        /// </summary>
        internal static Argument DeserializeFromJson(Stream stream)
        {
            var reader = new SimpleJsonReader(stream);
            var deserializer = new Deserializer<SimpleJsonReader>(typeof(Argument));
            return deserializer.Deserialize<Argument>(reader);
        }

        /// <summary>
        /// Serializes an Argument object to its JSON representation.
        /// </summary>
        internal static void SerializeToJson(Argument entryPoint, Stream stream)
        {
            var writer = new SimpleJsonWriter(stream);
            var serializer = new Serializer<SimpleJsonWriter>(typeof(Argument));
            serializer.Serialize(entryPoint, writer);
            writer.Flush();
            stream.Flush();
            stream.Position = 0;
        }
    }
}
