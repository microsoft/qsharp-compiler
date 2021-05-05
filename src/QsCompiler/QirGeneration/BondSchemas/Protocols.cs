// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace Microsoft.Quantum.Qir.Serialization
{
    /// <summary>
    /// This class provides methods for serialization/deserialization of objects in the Microsoft.Quantum.QsCompiler.BondSchemas.Execution namespace.
    /// </summary>
    public static class Protocols
    {
        /// <summary>
        /// Deserializes a QirExecutionWrapper object from its fast binary representation.
        /// </summary>
        public static QirExecutionWrapper DeserializeQirExecutionWrapperFromFastBinary(Stream stream)
        {
            var inputStream = new InputStream(stream);
            var reader = new FastBinaryReader<InputStream>(inputStream);
            var deserializer = new Deserializer<FastBinaryReader<InputStream>>(typeof(QirExecutionWrapper));
            var qirExecutionWrapper = deserializer.Deserialize<QirExecutionWrapper>(reader);
            return qirExecutionWrapper;
        }

        /// <summary>
        /// Serializes a QirExecutionWrapper object to its fast binary representation.
        /// </summary>
        public static void SerializeQirExecutionWrapperToFastBinary(QirExecutionWrapper qirExecutionWrapper, Stream stream)
        {
            var outputBuffer = new OutputBuffer();
            var writer = new FastBinaryWriter<OutputBuffer>(outputBuffer);
            var serializer = new Serializer<FastBinaryWriter<OutputBuffer>>(typeof(QirExecutionWrapper));
            serializer.Serialize(qirExecutionWrapper, writer);
            stream.Write(outputBuffer.Data);
            stream.Flush();
            stream.Position = 0;
        }
    }
}
