// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    public static class Factory
    {
        private static Serializer<FastBinaryWriter<OutputBuffer>>? fastBinarySerializer = null;

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
