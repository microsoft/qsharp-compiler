// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas.QirExecutionWrapper
{
    /// <summary>
    /// This class provides methods for serialization/deserialization of objects in the Microsoft.Quantum.QsCompiler.BondSchemas.QirExecutionWrapper namespace.
    /// </summary>
    public static class Protocols
    {
        /// <summary>
        /// Deserializes a QirExecutionWrapper object from its fast binary representation.
        /// </summary>
        public static QirExecutionWrapper DeserializeFromFastBinary(Stream stream)
        {
            var inputStream = new InputStream(stream);
            var reader = new FastBinaryReader<InputStream>(inputStream);
            var deserializer = new Deserializer<FastBinaryReader<InputStream>>(typeof(QirExecutionWrapper));
            var sandboxInput = deserializer.Deserialize<QirExecutionWrapper>(reader);
            return sandboxInput;
        }

        /// <summary>
        /// Serializes a QirExecutionWrapper object to its fast binary representation.
        /// </summary>
        public static void SerializeToFastBinary(QirExecutionWrapper sandboxInput, Stream stream)
        {
            var outputBuffer = new OutputBuffer();
            var writer = new FastBinaryWriter<OutputBuffer>(outputBuffer);
            var serializer = new Serializer<FastBinaryWriter<OutputBuffer>>(typeof(QirExecutionWrapper));
            serializer.Serialize(sandboxInput, writer);
            stream.Write(outputBuffer.Data);
            stream.Flush();
            stream.Position = 0;
        }
    }
}
