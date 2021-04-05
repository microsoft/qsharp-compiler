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
            stream.Flush();
            stream.Position = 0;
            var inputStream = new InputStream(stream);
            var reader = new FastBinaryReader<InputStream>(inputStream);
            var deserializer = new Deserializer<FastBinaryReader<InputStream>>(typeof(Input));
            var sandboxInput = deserializer.Deserialize<Input>(reader);
            return sandboxInput;
        }

        /// <summary>
        /// Serializes a sandbox input object to its fast binary representation.
        /// </summary>
        public static void SerializeToFastBinary(Input sandboxInput, Stream stream)
        {
            var outputBuffer = new OutputBuffer();
            var writer = new FastBinaryWriter<OutputBuffer>(outputBuffer);
            var serializer = new Serializer<FastBinaryWriter<OutputBuffer>>(typeof(Input));
            serializer.Serialize(sandboxInput, writer);
            stream.Write(outputBuffer.Data);
            stream.Flush();
            stream.Position = 0;
        }
    }
}
