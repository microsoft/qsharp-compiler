// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.SandboxInput
{
    /// <summary>
    /// This class provides methods for serialization/deserialization of objects in the Microsoft.Quantum.QsCompiler.BondSchemas.SandboxInput namespace.
    /// </summary>
    internal static class Protocols
    {
        /// <summary>
        /// Deserializes a sandbox input object from its fast binary representation.
        /// </summary>
        public static Input DeserializeFromFastBinary(Stream stream)
        {
            var inputStream = new InputStream(stream);
            var reader = new FastBinaryReader<InputStream>(inputStream);
            var deserializer = new Deserializer<FastBinaryReader<InputStream>>(typeof(Input));
            var sandboxInput = deserializer.Deserialize<Input>(reader);
            return sandboxInput;
        }

        /// <summary>
        /// Serializes a sandbox input object to its fast binary representation.
        /// </summary>
        public static void SerializeToJson(Input sandboxInput, Stream stream)
        {
            var outputStream = new OutputStream(stream);
            var writer = new FastBinaryWriter<OutputStream>(outputStream);
            var serializer = new Serializer<FastBinaryWriter<OutputStream>>(typeof(Input));
            serializer.Serialize(sandboxInput, writer);
            stream.Flush();
            stream.Position = 0;
        }
    }
}
