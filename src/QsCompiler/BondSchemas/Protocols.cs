// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    /// <summary>
    /// This class provides methods for serialization/deserialization of Q# compilation objects.
    /// </summary>
    public static class Protocols
    {
        private static Deserializer<FastBinaryReader<InputBuffer>>? fastBinaryDeserializer = null;
        private static Serializer<FastBinaryWriter<OutputBuffer>>? fastBinarySerializer = null;

        /// <summary>
        /// Deserializes a Q# compilation object from its Bond fast binary representation.
        /// </summary>
        /// <param name="byteArray">Bond fast binary representation of a Q# compilation object.</param>
        public static SyntaxTree.QsCompilation? DeserializeQsCompilationFromFastBinary(
            byte[] byteArray)
        {
            var inputBuffer = new InputBuffer(byteArray);
            var deserializer = GetFastBinaryDeserializer();
            var reader = new FastBinaryReader<InputBuffer>(inputBuffer);
            var bondCompilation = deserializer.Deserialize<QsCompilation>(reader);
            return CompilerObjectTranslator.CreateQsCompilation(bondCompilation);
        }

        /// <summary>
        /// Serializes a Q# compilation object to its Bond fast binary representation.
        /// </summary>
        /// <param name="qsCompilation">Q# compilation object to serialize.</param>
        /// <param name="stream">Stream to write the serialization to.</param>
        public static void SerializeQsCompilationToFastBinary(
            SyntaxTree.QsCompilation qsCompilation,
            Stream stream)
        {
            var outputBuffer = new OutputBuffer();
            var serializer = GetFastBinarySerializer();
            var fastBinaryWriter = new FastBinaryWriter<OutputBuffer>(outputBuffer);
            var bondCompilation = BondSchemaTranslator.CreateBondCompilation(qsCompilation);
            serializer.Serialize(bondCompilation, fastBinaryWriter);
            stream.Write(outputBuffer.Data);
            stream.Flush();
            stream.Position = 0;
        }

        private static Deserializer<FastBinaryReader<InputBuffer>> GetFastBinaryDeserializer()
        {
            if (fastBinaryDeserializer == null)
            {
                fastBinaryDeserializer = new Deserializer<FastBinaryReader<InputBuffer>>(typeof(QsCompilation));
            }

            return fastBinaryDeserializer;
        }

        private static Serializer<FastBinaryWriter<OutputBuffer>> GetFastBinarySerializer()
        {
            if (fastBinarySerializer == null)
            {
                fastBinarySerializer = new Serializer<FastBinaryWriter<OutputBuffer>>(typeof(QsCompilation));
            }

            return fastBinarySerializer;
        }
    }
}
