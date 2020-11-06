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
        private static Deserializer<SimpleBinaryReader<InputBuffer>>? simpleBinaryDeserializer = null;
        private static Serializer<SimpleBinaryWriter<OutputBuffer>>? simpleBinarySerializer = null;

        /// <summary>
        /// Deserializes a Q# compilation object from its Bond simple binary representation.
        /// </summary>
        /// <param name="byteArray">Bond simple binary representation of a Q# compilation object.</param>
        public static SyntaxTree.QsCompilation? DeserializeQsCompilationFromSimpleBinary(
            byte[] byteArray)
        {
            var inputBuffer = new InputBuffer(byteArray);
            var deserializer = GetSimpleBinaryDeserializer();
            var reader = new SimpleBinaryReader<InputBuffer>(inputBuffer);
            var bondCompilation = deserializer.Deserialize<QsCompilation>(reader);
            return CompilerObjectTranslator.CreateQsCompilation(bondCompilation);
        }

        /// <summary>
        /// Serializes a Q# compilation object to its Bond simple binary representation.
        /// </summary>
        /// <param name="qsCompilation">Q# compilation object to serialize.</param>
        /// <param name="stream">Stream to write the serialization to.</param>
        public static void SerializeQsCompilationToSimpleBinary(
            SyntaxTree.QsCompilation qsCompilation,
            Stream stream)
        {
            var outputBuffer = new OutputBuffer();
            var serializer = GetSimpleBinarySerializer();
            var writer = new SimpleBinaryWriter<OutputBuffer>(outputBuffer);
            var bondCompilation = BondSchemaTranslator.CreateBondCompilation(qsCompilation);
            serializer.Serialize(bondCompilation, writer);
            stream.Write(outputBuffer.Data);
            stream.Flush();
            stream.Position = 0;
        }

        private static Deserializer<SimpleBinaryReader<InputBuffer>> GetSimpleBinaryDeserializer()
        {
            if (simpleBinaryDeserializer == null)
            {
                simpleBinaryDeserializer = new Deserializer<SimpleBinaryReader<InputBuffer>>(typeof(QsCompilation));
            }

            return simpleBinaryDeserializer;
        }

        private static Serializer<SimpleBinaryWriter<OutputBuffer>> GetSimpleBinarySerializer()
        {
            if (simpleBinarySerializer == null)
            {
                simpleBinarySerializer = new Serializer<SimpleBinaryWriter<OutputBuffer>>(typeof(QsCompilation));
            }

            return simpleBinarySerializer;
        }
    }
}
