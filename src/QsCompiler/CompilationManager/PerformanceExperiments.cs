using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler
{
    public static class PerformanceExperiments
    {
        public static (Deserializer<FastBinaryReader<InputBuffer>>, FastBinaryReader<InputBuffer>) CreateFastBinaryBufferDeserializationTuple(
            OutputBuffer dataBuffer)
        {
            var inputBuffer = new InputBuffer(dataBuffer.Data);
            var deserializer = new Deserializer<FastBinaryReader<InputBuffer>>(typeof(BondSchemas.QsCompilation));
            var reader = new FastBinaryReader<InputBuffer>(inputBuffer);
            return (deserializer, reader);
        }

        public static (Serializer<FastBinaryWriter<OutputBuffer>>, FastBinaryWriter<OutputBuffer>, OutputBuffer) CreateFastBinaryBufferSerializationTuple()
        {
            var outputBuffer = new OutputBuffer();
            var serializer = new Serializer<FastBinaryWriter<OutputBuffer>>(typeof(BondSchemas.QsCompilation));
            var binaryWriter = new FastBinaryWriter<OutputBuffer>(outputBuffer);
            return (serializer, binaryWriter, outputBuffer);
        }

        public static (Deserializer<SimpleBinaryReader<InputBuffer>>, SimpleBinaryReader<InputBuffer>) CreateSimpleBinaryBufferDeserializationTuple(
            OutputBuffer dataBuffer)
        {
            var inputBuffer = new InputBuffer(dataBuffer.Data);
            var deserializer = new Deserializer<SimpleBinaryReader<InputBuffer>>(typeof(BondSchemas.QsCompilation));
            var reader = new SimpleBinaryReader<InputBuffer>(inputBuffer);
            return (deserializer, reader);
        }

        public static (Serializer<SimpleBinaryWriter<OutputBuffer>>, SimpleBinaryWriter<OutputBuffer>, OutputBuffer) CreateSimpleBinaryBufferSerializationTuple()
        {
            var outputBuffer = new OutputBuffer();
            var serializer = new Serializer<SimpleBinaryWriter<OutputBuffer>>(typeof(BondSchemas.QsCompilation));
            var binaryWriter = new SimpleBinaryWriter<OutputBuffer>(outputBuffer);
            return (serializer, binaryWriter, outputBuffer);
        }
    }
}
