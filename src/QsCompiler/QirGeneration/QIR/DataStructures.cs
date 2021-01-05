// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR.Emission
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal interface IValue
    {
        public Value Value { get; }

        public ResolvedType QSharpType { get; }
    }

    internal class SimpleValue : IValue
    {
        public Value Value { get; }

        public ResolvedType QSharpType { get; }

        internal SimpleValue(Value value, ResolvedType type, InstructionBuilder? builder = null)
        {
            this.Value = value;
            this.QSharpType = type;
        }
    }

    internal class TupleValue : IValue
    {
        private readonly GenerationContext sharedState;
        private readonly InstructionBuilder? builder;

        private InstructionBuilder Builder =>
            this.builder ?? this.sharedState.CurrentBuilder;

        private Value? opaquePointer;
        private Value? typedPointer;

        public Value Value => this.TypedPointer;

        public ResolvedType QSharpType =>
            ResolvedType.New(QsResolvedTypeKind.NewTupleType(ImmutableArray.CreateRange(this.ElementTypes)));

        internal readonly ImmutableArray<ResolvedType> ElementTypes;
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
                    this.sharedState.ScopeMgr.RegisterValue(this);
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
        /// Creates a new tuple value. The allocation of the value via invokation of the corresponding runtime function
        /// is lazy, and so are the necessary casts. When needed, the instructions are emitted using the given builder.
        /// If no builder is specified, the builder defined in the context is used when a pointer is constructed.
        /// </summary>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal TupleValue(ImmutableArray<ResolvedType> elementTypes, GenerationContext context, InstructionBuilder? builder = null)
        {
            this.sharedState = context;
            this.builder = builder;
            this.ElementTypes = elementTypes;
            this.StructType = this.sharedState.Types.TypedTuple(elementTypes.Select(context.LlvmTypeFromQsharpType));
        }

        /// <summary>
        /// Creates a new tuple value from the given tuple pointer. The casts to get the opaque and typed pointer
        /// respectively are executed lazily. When needed, the instructions are emitted using the given builder.
        /// If no builder is specified, the builder defined in the context is used when a pointer is constructed.
        /// </summary>
        /// <param name="tuple">Either an opaque or a typed pointer to the tuple data structure</param>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal TupleValue(Value tuple, ImmutableArray<ResolvedType> elementTypes, GenerationContext context, InstructionBuilder? builder = null)
        {
            var isTypedTuple = Types.IsTypedTuple(tuple.NativeType);
            var isOpaqueTuple = Types.IsTuple(tuple.NativeType);
            if (!isTypedTuple && !isOpaqueTuple)
            {
                throw new ArgumentException("expecting either an opaque or a typed tuple");
            }

            this.sharedState = context;
            this.builder = builder;
            this.opaquePointer = isOpaqueTuple ? tuple : null;
            this.typedPointer = isTypedTuple ? tuple : null;
            this.ElementTypes = elementTypes;
            this.StructType = this.sharedState.Types.TypedTuple(elementTypes.Select(context.LlvmTypeFromQsharpType));
        }

        // methods for item access

        /// <summary>
        /// Creates a suitable array of values to access the item at a given index for a pointer to a struct.
        /// </summary>
        private Value[] PointerIndex(int index) => new[]
        {
            this.sharedState.Context.CreateConstant(0L),
            this.sharedState.Context.CreateConstant(index)
        };

        /// <summary>
        /// Returns a pointer to a tuple element.
        /// If no builder is specified, the current builder is used.
        /// </summary>
        /// <param name="index">The element's index into the tuple.</param>
        /// <param name="builder">Optional argument specifying the builder to use to create the instructions</param>
        internal Value GetTupleElementPointer(int index) =>
            this.Builder.GetElementPtr(this.StructType, this.TypedPointer, this.PointerIndex(index));

        /// <summary>
        /// Returns a tuple element.
        /// If no builder is specified, the current builder is used.
        /// </summary>
        /// <param name="index">The element's index into the tuple.</param>
        /// <param name="builder">Optional argument specifying the builder to use to create the instructions</param>
        internal IValue GetTupleElement(int index)
        {
            var elementPtr = this.GetTupleElementPointer(index);
            var element = this.Builder.Load(this.StructType.Members[index], elementPtr);
            return this.sharedState.Values.From(element, this.ElementTypes[index], this.builder);
        }

        /// <summary>
        /// Returns an array with all pointers to the tuple elements.
        /// If no builder is specified, the current builder is used.
        /// </summary>
        /// <param name="builder">Optional argument specifying the builder to use to create the instructions</param>
        internal Value[] GetTupleElementPointers() =>
            this.StructType.Members
                .Select((_, i) => this.Builder.GetElementPtr(this.TypedPointer, this.PointerIndex(i)))
                .ToArray();

        /// <summary>
        /// Returns an array with all tuple elements.
        /// If no builder is specified, the current builder is used.
        /// </summary>
        /// <param name="builder">Optional argument specifying the builder to use to create the instructions</param>
        internal IValue[] GetTupleElements()
        {
            var elementPtrs = this.GetTupleElementPointers();
            var elements = this.StructType.Members.Select((itemType, i) => this.Builder.Load(itemType, elementPtrs[i]));
            return elements.Select((element, i) => this.sharedState.Values.From(element, this.ElementTypes[i], this.builder)).ToArray();
        }
    }

    internal class ArrayValue : IValue
    {
        private readonly GenerationContext sharedState;
        private readonly InstructionBuilder? builder;

        private InstructionBuilder Builder =>
            this.builder ?? this.sharedState.CurrentBuilder;

        // Imporant: the constructors must ensure that either length or opaque pointer is not null!
        private Value? opaquePointer;
        private Value? length;

        public Value Value => this.OpaquePointer;

        public ResolvedType QSharpType =>
            ResolvedType.New(QsResolvedTypeKind.NewArrayType(this.elementType));

        private readonly ResolvedType elementType;
        public readonly ITypeRef ElementType;
        public readonly uint? Count;

        public Value Length
        {
            get
            {
                if (this.length == null)
                {
                    var getLength = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetSize1d);
                    this.length = this.Builder.Call(getLength, this.opaquePointer ?? throw new InvalidOperationException("array has no value"));
                }
                return this.length;
            }
        }

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
                    this.sharedState.ScopeMgr.RegisterValue(this);
                }
                return this.opaquePointer;
            }
        }

        /// <summary>
        /// Creates a new array value. The allocation of the value via invokation of the corresponding runtime function
        /// is lazy, and so are other necessary computations. When needed, the instructions are emitted using the given builder.
        /// If no builder is specified, the builder defined in the context is used when a pointer is constructed.
        /// </summary>
        /// <param name="count">The number of elements in the array</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal ArrayValue(uint count, ResolvedType elementType, GenerationContext context, InstructionBuilder? builder = null)
        {
            this.sharedState = context;
            this.builder = builder;
            this.elementType = elementType;
            this.ElementType = context.LlvmTypeFromQsharpType(elementType);
            this.Count = count;
            this.length = context.Context.CreateConstant((long)count);
        }

        /// <summary>
        /// Creates a new array value of the given length. Expects a value of type i64 for the length of the array.
        /// The allocation of the value via invokation of the corresponding runtime function is lazy, and so are
        /// other necessary computations. When needed, the instructions are emitted using the given builder.
        /// If no builder is specified, the builder defined in the context is used when a pointer is constructed.
        /// </summary>
        /// <param name="length">Value of type i64 indicating the number of elements in the array</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal ArrayValue(Value length, ResolvedType elementType, GenerationContext context, InstructionBuilder? builder = null)
        {
            this.sharedState = context;
            this.builder = builder;
            this.elementType = elementType;
            this.ElementType = context.LlvmTypeFromQsharpType(elementType);
            this.length = length;
        }

        /// <summary>
        /// Creates a new array value from the given opaque array of elements of the given type. When needed,
        /// the instructions to compute the length of the array are emitted using the given builder.
        /// If no builder is specified, the builder defined in the context is used when a pointer is constructed.
        /// </summary>
        /// <param name="array">The opaque pointer to the array data structure</param>
        /// <param name="length">Value of type i64 indicating the number of elements in the array; will be computed on demand if the given value is null</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal ArrayValue(Value array, Value? length, ResolvedType elementType, GenerationContext context, InstructionBuilder? builder = null)
        {
            this.sharedState = context;
            this.builder = builder;
            this.elementType = elementType;
            this.ElementType = context.LlvmTypeFromQsharpType(elementType);
            this.opaquePointer = Types.IsArray(array.NativeType) ? array : throw new ArgumentException("expecting an opaque array");
            this.length = length;
        }

        // methods for item access

        /// <summary>
        /// Returns a pointer to an array element.
        /// If no builder is specified, the current builder is used.
        /// </summary>
        /// <param name="index">The element's index into the array.</param>
        /// <param name="builder">Optional argument specifying the builder to use to create the instructions</param>
        internal Value GetArrayElementPointer(Value index)
        {
            var getElementPointer = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d);
            var opaqueElementPointer = this.Builder.Call(getElementPointer, this.OpaquePointer, index);
            return this.Builder.BitCast(opaqueElementPointer, this.ElementType.CreatePointerType());
        }

        /// <summary>
        /// Returns an array element.
        /// If no builder is specified, the current builder is used.
        /// </summary>
        /// <param name="index">The element's index into the array.</param>
        /// <param name="builder">Optional argument specifying the builder to use to create the instructions</param>
        internal IValue GetArrayElement(Value index)
        {
            var elementPtr = this.GetArrayElementPointer(index);
            var element = this.Builder.Load(this.ElementType, elementPtr);
            return this.sharedState.Values.From(element, this.elementType, this.builder);
        }

        /// <summary>
        /// Returns a pointer to an array element.
        /// If no builder is specified, the current builder is used.
        /// </summary>
        /// <param name="builder">Optional argument specifying the builder to use to create the instructions</param>
        internal Value[] GetArrayElementPointers(params int[] indices)
        {
            var enumerable = indices.Length != 0 ? indices :
                this.Count != null && this.Count <= int.MaxValue ? Enumerable.Range(0, (int)this.Count) :
                throw new InvalidOperationException("cannot get all element pointers for array of unknown length");

            return enumerable
                .Select(idx => this.sharedState.Context.CreateConstant((long)idx))
                .Select(this.GetArrayElementPointer)
                .ToArray();
        }

        /// <summary>
        /// Returns an array element.
        /// If no builder is specified, the current builder is used.
        /// </summary>
        /// <param name="builder">Optional argument specifying the builder to use to create the instructions</param>
        internal IValue[] GetArrayElements(params int[] indices)
        {
            var elementPtrs = this.GetArrayElementPointers(indices);
            var elements = elementPtrs.Select(ptr => this.Builder.Load(this.ElementType, ptr));
            return elements.Select((element, i) => this.sharedState.Values.From(element, this.elementType, this.builder)).ToArray();
        }
    }

    internal class CallableValue : SimpleValue // FIXME
    {
        internal CallableValue(Value callabe, ResolvedType type, InstructionBuilder? builder = null)
        : base(callabe, type, builder)
        {
        }
    }
}
