// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    public static class Factory
    {
        private static Deserializer<FastBinaryReader<InputBuffer>>? fastBinaryDeserializer = null;
        private static Serializer<FastBinaryWriter<OutputBuffer>>? fastBinarySerializer = null;

        /// <summary>
        /// Creates a Bond fast binary deserializer for the QsCompilation schema if one does not exist, and returns it.
        /// </summary>
        public static Deserializer<FastBinaryReader<InputBuffer>> GetFastBinaryDeserializer()
        {
            if (fastBinaryDeserializer == null)
            {
                fastBinaryDeserializer = new Deserializer<FastBinaryReader<InputBuffer>>(typeof(QsCompilation));
            }

            return fastBinaryDeserializer;
        }

        /// <summary>
        /// Creates a Bond fast binary serializer for the QsCompilation schema if one does not exist, and returns it.
        /// </summary>
        public static Serializer<FastBinaryWriter<OutputBuffer>> GetFastBinarySerializer()
        {
            if (fastBinarySerializer == null)
            {
                fastBinarySerializer = new Serializer<FastBinaryWriter<OutputBuffer>>(typeof(QsCompilation));
            }

            return fastBinarySerializer;
        }
    }
}
