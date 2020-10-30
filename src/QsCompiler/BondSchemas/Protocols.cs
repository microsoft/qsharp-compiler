// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;
using System.IO;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    public static class Protocols
    {
        private static Deserializer<FastBinaryReader<InputBuffer>>? fastBinaryDeserializer = null;
        private static Serializer<FastBinaryWriter<OutputBuffer>>? fastBinarySerializer = null;

        public static SyntaxTree.QsCompilation? DeserializeQsCompilationFromFastBinary(
            byte[] byteArray)
        {
            var inputBuffer = new InputBuffer(byteArray);
            var deserializer = GetFastBinaryDeserializer();
            var reader = new FastBinaryReader<InputBuffer>(inputBuffer);
            var bondCompilation = deserializer.Deserialize<QsCompilation>(reader);
            return CompilerObjectTranslator.CreateQsCompilation(bondCompilation);
        }

        public static void SerializeQsCompilationToFastBinary(
            SyntaxTree.QsCompilation qsCompilation,
            MemoryStream memoryStream)
        {
            var outputBuffer = new OutputBuffer();
            var serializer = GetFastBinarySerializer();
            var fastBinaryWriter = new FastBinaryWriter<OutputBuffer>(outputBuffer);
            var bondCompilation = BondSchemaTranslator.CreateBondCompilation(qsCompilation);
            serializer.Serialize(bondCompilation, fastBinaryWriter);
            memoryStream.Write(outputBuffer.Data);
            memoryStream.Flush();
            memoryStream.Position = 0;
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
