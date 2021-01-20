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
    using BondQsCompilation = V1.QsCompilation;
    using SimpleBinaryDeserializer = Deserializer<SimpleBinaryReader<InputBuffer>>;
    using SimpleBinarySerializer = Serializer<SimpleBinaryWriter<OutputBuffer>>;

    /// <summary>
    /// This class provides methods for serialization/deserialization of Q# compilation objects.
    /// </summary>
    public static class Protocols
    {

        // TODO: Document.
        public enum Option
        {
            ExcludeNamespaceDocumentation
        }

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
        // TODO: Extend to receive options.
        public static SyntaxTree.QsCompilation? DeserializeQsCompilationFromSimpleBinary(
            byte[] byteArray,
            Type bondType,
            Option[]? options = null)
        {
            BondQsCompilation? bondCompilation = null;
            var inputBuffer = new InputBuffer(byteArray);
            var reader = new SimpleBinaryReader<InputBuffer>(inputBuffer);
            lock (BondSharedDataStructuresLock)
            {
                var deserializer = GetSimpleBinaryDeserializer();
                bondCompilation = deserializer.Deserialize<BondQsCompilation>(reader);
            }

            return Translators.FromBondSchemaToSyntaxTree(bondCompilation);
        }

        /// <summary>
        /// Starts the creation of Bond serializers and deserializers.
        /// </summary>
        /// <remarks>This method waits for <see cref="Task"/>s to complete and may deadlock if invoked through a <see cref="Task"/>.</remarks>
        public static void Initialize()
        {
            lock (BondSharedDataStructuresLock)
            {
                _ = TryInitializeDeserializer();
                _ = TryInitializeSerializer();
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
                _ = TryInitializeDeserializer();
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
                _ = TryInitializeSerializer();
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
            var outputBuffer = new OutputBuffer();
            var writer = new SimpleBinaryWriter<OutputBuffer>(outputBuffer);
            var bondCompilation = Translators.FromSyntaxTreeToBondSchema(qsCompilation);
            lock (BondSharedDataStructuresLock)
            {
                var serializer = GetSimpleBinarySerializer();
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

        private static SimpleBinaryDeserializer GetSimpleBinaryDeserializer(Type type)
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            Task<SimpleBinaryDeserializer> deserializerInitialization;
            if (type == typeof(BondQsCompilation))
            {
                deserializerInitialization = TryInitializeDeserializer();
            }
            else
            {
                // TODO: Add a meaningful message.
                throw new ArgumentException();
            }

            deserializerInitialization.Wait();
            return deserializerInitialization.Result;
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
            // TODO: Maybe use this method to initialize everything by iterating through a dictionary.

            // inlineNested is false in order to decrease the time needed to initialize the deserializer.
            // While this setting may also increase deserialization time, we did not notice any performance drawbacks with our Bond schemas.
            return Task.Run(() => new SimpleBinaryDeserializer(
                type: typeof(BondQsCompilation),
                factory: (Factory?)null,
                inlineNested: false));
        }

        private static Task<SimpleBinarySerializer> QueueSimpleBinarySerializerInitialization()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            return Task.Run(() => new SimpleBinarySerializer(typeof(BondQsCompilation)));
        }

        private static Task<SimpleBinaryDeserializer> TryInitializeDeserializer()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            if (simpleBinaryDeserializerInitialization == null)
            {
                simpleBinaryDeserializerInitialization = QueueSimpleBinaryDeserializerInitialization();
            }

            return simpleBinaryDeserializerInitialization;
        }

        private static Task<SimpleBinarySerializer> TryInitializeSerializer()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            if (simpleBinarySerializerInitialization == null)
            {
                simpleBinarySerializerInitialization = QueueSimpleBinarySerializerInitialization();
            }

            return simpleBinarySerializerInitialization;
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
