// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    using SimpleBinaryDeserializer = Deserializer<SimpleBinaryReader<InputBuffer>>;
    using SimpleBinarySerializer = Serializer<SimpleBinaryWriter<OutputBuffer>>;

    /// <summary>
    /// This class provides methods for serialization/deserialization of Q# compilation objects.
    /// </summary>
    public static class Protocols
    {
        /// <summary>
        /// Provides thread-safe access to the members and methods of this class.
        /// </summary>
        private static readonly object BondSharedDataStructuresLock = new object();
        private static Task<SimpleBinaryDeserializer>? simpleBinaryDeserializerInitialization = null;
        private static Task<SimpleBinarySerializer>? simpleBinarySerializerInitialization = null;

        /// <summary>
        /// Deserializes a Q# compilation object from its Bond simple binary representation.
        /// </summary>
        /// <param name="byteArray">Bond simple binary representation of a Q# compilation object.</param>
        /// <remarks>This method waits for <see cref="Task"/>s to complete and may deadlock if invoked through a <see cref="Task"/>.</remarks>
        public static SyntaxTree.QsCompilation? DeserializeQsCompilationFromSimpleBinary(
            byte[] byteArray)
        {
            QsCompilation? bondCompilation = null;
            lock (BondSharedDataStructuresLock)
            {
                var inputBuffer = new InputBuffer(byteArray);
                var deserializer = GetSimpleBinaryDeserializer();
                var reader = new SimpleBinaryReader<InputBuffer>(inputBuffer);
                bondCompilation = deserializer.Deserialize<QsCompilation>(reader);
            }

            return CompilerObjectTranslator.CreateQsCompilation(bondCompilation);
        }

        /// <summary>
        /// Starts the creation of Bond serializers and deserializers.
        /// </summary>
        /// <remarks>This method waits for <see cref="Task"/>s to complete and may deadlock if invoked through a <see cref="Task"/>.</remarks>
        public static void Initialize()
        {
            lock (BondSharedDataStructuresLock)
            {
                if (simpleBinaryDeserializerInitialization == null)
                {
                    simpleBinaryDeserializerInitialization = QueueSimpleBinaryDeserializerInitialization();
                }

                if (simpleBinarySerializerInitialization == null)
                {
                    simpleBinarySerializerInitialization = QueueSimpleBinarySerializerInitialization();
                }
            }
        }

        /// <summary>
        /// Starts the creation of a Bond deserializer.
        /// </summary>
        /// <remarks>This method waits for <see cref="Task"/>s to complete and may deadlock if invoked through a <see cref="Task"/>.</remarks>
        public static void InitializeDeserializer()
        {
            lock (BondSharedDataStructuresLock)
            {
                if (simpleBinaryDeserializerInitialization == null)
                {
                    simpleBinaryDeserializerInitialization = QueueSimpleBinaryDeserializerInitialization();
                }
            }
        }

        /// <summary>
        /// Starts the creation of a Bond serializer.
        /// </summary>
        /// <remarks>This method waits for <see cref="Task"/>s to complete and may deadlock if invoked through a <see cref="Task"/>.</remarks>
        public static void InitializeSerializer()
        {
            lock (BondSharedDataStructuresLock)
            {
                if (simpleBinarySerializerInitialization == null)
                {
                    simpleBinarySerializerInitialization = QueueSimpleBinarySerializerInitialization();
                }
            }
        }

        /// <summary>
        /// Serializes a Q# compilation object to its Bond simple binary representation.
        /// </summary>
        /// <param name="qsCompilation">Q# compilation object to serialize.</param>
        /// <param name="stream">Stream to write the serialization to.</param>
        /// <remarks>This method waits for <see cref="Task"/>s to complete and may deadlock if invoked through a <see cref="Task"/>.</remarks>
        public static void SerializeQsCompilationToSimpleBinary(
            SyntaxTree.QsCompilation qsCompilation,
            Stream stream)
        {
            lock (BondSharedDataStructuresLock)
            {
                var outputBuffer = new OutputBuffer();
                var serializer = GetSimpleBinarySerializer();
                var writer = new SimpleBinaryWriter<OutputBuffer>(outputBuffer);
                var bondCompilation = BondSchemaTranslator.CreateBondCompilation(qsCompilation);
                serializer.Serialize(bondCompilation, writer);
                stream.Write(outputBuffer.Data);
            }

            stream.Flush();
            stream.Position = 0;
        }

        private static SimpleBinaryDeserializer GetSimpleBinaryDeserializer()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            if (simpleBinaryDeserializerInitialization == null)
            {
                simpleBinaryDeserializerInitialization = QueueSimpleBinaryDeserializerInitialization();
            }

            simpleBinaryDeserializerInitialization.Wait();
            return simpleBinaryDeserializerInitialization.Result;
        }

        private static SimpleBinarySerializer GetSimpleBinarySerializer()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            if (simpleBinarySerializerInitialization == null)
            {
                simpleBinarySerializerInitialization = QueueSimpleBinarySerializerInitialization();
            }

            simpleBinarySerializerInitialization.Wait();
            return simpleBinarySerializerInitialization.Result;
        }

        private static Task<SimpleBinaryDeserializer> QueueSimpleBinaryDeserializerInitialization()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            return Task.Run(() => new SimpleBinaryDeserializer(typeof(QsCompilation)));
        }

        private static Task<SimpleBinarySerializer> QueueSimpleBinarySerializerInitialization()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            return Task.Run(() => new SimpleBinarySerializer(typeof(QsCompilation)));
        }

        private static void VerifyLockAcquired(object lockObject)
        {
#if DEBUG
            if (!Monitor.IsEntered(lockObject))
            {
                throw new InvalidOperationException("Lock is expected to be acquired");
            }
#endif
        }
    }
}
