// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bond;
using Bond.IO.Unsafe;
using Bond.Protocols;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    using BondQsCompilation = V2.QsCompilation;
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
        private static readonly IDictionary<Type, Task<SimpleBinaryDeserializer>?> DeserializerInitializations =
            new Dictionary<Type, Task<SimpleBinaryDeserializer>?>()
            {
#pragma warning disable IDE0001 // Simplify Names
                { typeof(V1.QsCompilation), null },
                { typeof(V2.QsCompilation), null }
#pragma warning restore IDE0001 // Simplify Names
            };

        private static Task<SimpleBinarySerializer>? serializerInitialization = null;

        /// <summary>
        /// Deserializes a Q# compilation object from its Bond simple binary representation.
        /// </summary>
        /// <param name="byteArray">Bond simple binary representation of a Q# compilation object.</param>
        /// <remarks>This method waits for <see cref="Task"/>s to complete and may deadlock if invoked through a <see cref="Task"/>.</remarks>
        // N.B. Consider adding an options argument to this method to allow for selection of payload to deserialize.
        public static SyntaxTree.QsCompilation? DeserializeQsCompilationFromSimpleBinary(
            byte[] byteArray,
            Type bondSchemaType)
        {
            object? bondCompilation = null;
            var inputBuffer = new InputBuffer(byteArray);
            var reader = new SimpleBinaryReader<InputBuffer>(inputBuffer);
            lock (BondSharedDataStructuresLock)
            {
                bondCompilation = DeserializeBondSchemaFromSimpleBinary(reader, bondSchemaType);
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
                TryInitializeDeserializers();
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
                TryInitializeDeserializers();
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

        private static object DeserializeBondSchemaFromSimpleBinary(
            SimpleBinaryReader<InputBuffer> reader,
            Type bondSchemaType)
        {
            var deserializer = GetSimpleBinaryDeserializer(bondSchemaType);
#pragma warning disable IDE0001 // Simplify Names
            if (bondSchemaType == typeof(V1.QsCompilation))
            {
                return deserializer.Deserialize<V1.QsCompilation>(reader);
            }
            else if (bondSchemaType == typeof(V2.QsCompilation))
            {
                return deserializer.Deserialize<V2.QsCompilation>(reader);
            }
#pragma warning restore IDE0001 // Simplify Names

            throw new ArgumentException($"Unknown Bond schema type '{bondSchemaType}'");
        }

        private static SimpleBinaryDeserializer GetSimpleBinaryDeserializer(Type bondSchemaType)
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            var deserializerInitialization = TryInitializeDeserializer(bondSchemaType);
            deserializerInitialization.Wait();
            return deserializerInitialization.Result;
        }

        private static SimpleBinarySerializer GetSimpleBinarySerializer()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            if (serializerInitialization == null)
            {
                serializerInitialization = QueueSimpleBinarySerializerInitialization();
            }

            serializerInitialization.Wait();
            return serializerInitialization.Result;
        }

        private static Task<SimpleBinaryDeserializer> QueueSimpleBinaryDeserializerInitialization(Type deserializerType)
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);

            // inlineNested is false in order to decrease the time needed to initialize the deserializer.
            // While this setting may also increase deserialization time, we did not notice any performance drawbacks with our Bond schemas.
            return Task.Run(() => new SimpleBinaryDeserializer(
                type: deserializerType,
                factory: (Factory?)null,
                inlineNested: false));
        }

        private static Task<SimpleBinarySerializer> QueueSimpleBinarySerializerInitialization()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            return Task.Run(() => new SimpleBinarySerializer(typeof(BondQsCompilation)));
        }

        private static Task<SimpleBinaryDeserializer> TryInitializeDeserializer(Type bondSchemaType)
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            if (!DeserializerInitializations.TryGetValue(bondSchemaType, out var deserializerInitialization))
            {
                throw new ArgumentException($"Unknown Bond schema type '{bondSchemaType}'");
            }

            if (deserializerInitialization == null)
            {
                deserializerInitialization = QueueSimpleBinaryDeserializerInitialization(bondSchemaType);
                DeserializerInitializations[bondSchemaType] = deserializerInitialization;
            }

            return deserializerInitialization;
        }

        private static void TryInitializeDeserializers()
        {
            foreach (var bondSchemaType in DeserializerInitializations.Keys.ToList())
            {
                _ = TryInitializeDeserializer(bondSchemaType);
            }
        }

        private static Task<SimpleBinarySerializer> TryInitializeSerializer()
        {
            VerifyLockAcquired(BondSharedDataStructuresLock);
            if (serializerInitialization == null)
            {
                serializerInitialization = QueueSimpleBinarySerializerInitialization();
            }

            return serializerInitialization;
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
