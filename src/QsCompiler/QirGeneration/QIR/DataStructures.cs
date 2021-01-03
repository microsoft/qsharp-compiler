// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.QIR;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR.Emission
{
    internal class TupleValue
    {
        private readonly GenerationContext sharedState;
        private readonly InstructionBuilder? builder;

        private InstructionBuilder Builder =>
            this.builder ?? this.sharedState.CurrentBuilder;

        private Value? opaquePointer;
        private Value? typedPointer;

        public readonly IStructType StructType;

        internal Value OpaquePointer
        {
            get
            {
                if (this.opaquePointer == null && this.typedPointer == null)
                {
                    // The runtime function TupleCreate creates a new value with reference count 1 and access count 0.
                    var constructor = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate);
                    var size = this.sharedState.ComputeSizeForType(this.StructType, this.builder);
                    this.opaquePointer = this.Builder.Call(constructor, size);
                    this.sharedState.ScopeMgr.RegisterValue(this.TypedPointer);
                }

                this.opaquePointer ??= this.Builder.BitCast(this.TypedPointer, this.sharedState.Types.Tuple);
                return this.opaquePointer;
            }
        }

        internal Value TypedPointer
        {
            get
            {
                this.typedPointer ??= this.Builder.BitCast(this.OpaquePointer, this.StructType.CreatePointerType());
                return this.typedPointer;
            }
        }

        /// <summary>
        /// If no builder is specified, the builder defined in the context is used when a pointer is constructed.
        /// The construction of both pointers is lazy.
        /// </summary>
        /// <param name="tupleType">Type of the structure that contains the tuple data</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal TupleValue(IStructType tupleType, GenerationContext context, InstructionBuilder? builder = null)
        {
            this.sharedState = context;
            this.builder = builder;
            this.StructType = tupleType;
        }
    }

    internal class ArrayValue
    {
        private readonly GenerationContext sharedState;
        private readonly InstructionBuilder? builder;

        private InstructionBuilder Builder =>
            this.builder ?? this.sharedState.CurrentBuilder;

        private Value? opaquePointer;

        public readonly ITypeRef ElementType;
        public readonly uint? Count;
        public readonly Value Length;

        internal Value OpaquePointer
        {
            get
            {
                if (this.opaquePointer == null)
                {
                    // The runtime function ArrayCreate1d creates a new value with reference count 1 and access count 0.
                    var constructor = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCreate1d);
                    var elementSize = this.sharedState.ComputeSizeForType(this.ElementType, this.builder, this.sharedState.Context.Int32Type);
                    this.opaquePointer = this.Builder.Call(constructor, elementSize, this.Length);
                    this.sharedState.ScopeMgr.RegisterValue(this.opaquePointer);
                }
                return this.opaquePointer;
            }
        }

        /// <summary>
        /// Expects a value of type i64 for the length of the array.
        /// If no builder is specified, the builder defined in the context is used when the opaque pointer is constructed.
        /// The construction of the opaque pointer is lazy.
        /// </summary>
        /// <param name="count">The number of elements in the array</param>
        /// <param name="elementType">Type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal ArrayValue(uint count, ITypeRef elementType, GenerationContext context, InstructionBuilder? builder = null)
        {
            this.sharedState = context;
            this.builder = builder;
            this.ElementType = elementType;
            this.Count = count;
            this.Length = context.Context.CreateConstant((long)count);
        }

        /// <summary>
        /// Expects a value of type i64 for the length of the array.
        /// If no builder is specified, the builder defined in the context is used when the opaque pointer is constructed.
        /// The construction of the opaque pointer is lazy.
        /// </summary>
        /// <param name="length">Value of type i64 indicating the number of elements in the array</param>
        /// <param name="elementType">Type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal ArrayValue(Value length, ITypeRef elementType, GenerationContext context, InstructionBuilder? builder = null)
        {
            this.sharedState = context;
            this.builder = builder;
            this.ElementType = elementType;
            this.Length = length;
        }
    }
}
