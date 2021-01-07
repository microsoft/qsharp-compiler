// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR.Emission
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// Interface used to pass around values during QIR emission
    /// that captures a built LLVM value along with its Q# type.
    /// </summary>
    internal interface IValue
    {
        /// <summary>
        /// The QIR representation of the value.
        /// </summary>
        public Value Value { get; }

        /// <summary>
        /// The LLVM type of the value.
        /// Accessing the type does not require constructing or loading the value.
        /// </summary>
        public ITypeRef LlvmType { get; }

        /// <summary>
        /// The Q# type of the value.
        /// </summary>
        public ResolvedType QSharpType { get; }
    }

    /// <summary>
    /// Stores the QIR representation for a Q# value of a simple type,
    /// meaning a type where the LLVM behavior matches the expected behavior
    /// and no custom handling is needed.
    /// The is the case e.g. for Int, Double, and Bool.
    /// </summary>
    internal class SimpleValue : IValue
    {
        public Value Value { get; }

        public ITypeRef LlvmType => this.Value.NativeType;

        public ResolvedType QSharpType { get; }

        internal SimpleValue(Value value, ResolvedType type)
        {
            this.Value = value;
            this.QSharpType = type;
        }
    }

    internal class PointerValue : IValue
    {
        private readonly GenerationContext sharedState;

        public readonly Value Pointer;

        public Value Value => this.Pointer;

        public ITypeRef LlvmType { get; }

        public ResolvedType QSharpType { get; }

        /// <summary>
        /// Creates a pointer that represents a mutable variable.
        /// </summary>
        /// <param name="type">The Q# type of the variable that the pointer represents</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal PointerValue(ResolvedType type, GenerationContext context)
        {
            this.sharedState = context;
            this.QSharpType = type;
            this.LlvmType = context.LlvmTypeFromQsharpType(type);
            this.Pointer = this.sharedState.CurrentBuilder.Alloca(this.LlvmType);
        }

        /// <summary>
        /// Loads and returns the current value of the mutable variable.
        /// </summary>
        public IValue LoadValue()
        {
            var loaded = this.sharedState.CurrentBuilder.Load(this.LlvmType, this.Pointer);
            return this.sharedState.Values.From(loaded, this.QSharpType);
        }
    }

    /// <summary>
    /// Stores the QIR representation of a Q# tuple or a value of user defined type.
    /// </summary>
    internal class TupleValue : IValue
    {
        private readonly GenerationContext sharedState;

        private Value? opaquePointer;
        private Value? typedPointer;
        private readonly UserDefinedType? customType;

        public Value Value => this.TypedPointer;

        public ITypeRef LlvmType => this.StructType.CreatePointerType();

        public ResolvedType QSharpType => this.customType != null
            ? ResolvedType.New(QsResolvedTypeKind.NewUserDefinedType(this.customType))
            : ResolvedType.New(QsResolvedTypeKind.NewTupleType(ImmutableArray.CreateRange(this.ElementTypes)));

        internal readonly ImmutableArray<ResolvedType> ElementTypes;
        public readonly IStructType StructType;

        private void AllocateTuple()
        {
            // The runtime function TupleCreate creates a new value with reference count 1 and access count 0.
            var constructor = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate);
            var size = this.sharedState.ComputeSizeForType(this.StructType);
            this.opaquePointer = this.sharedState.CurrentBuilder.Call(constructor, size);
            this.sharedState.ScopeMgr.RegisterValue(this);
        }

        internal Value OpaquePointer
        {
            get
            {
                if (this.opaquePointer == null && this.typedPointer == null)
                {
                    this.AllocateTuple();
                }

                this.opaquePointer ??= this.sharedState.CurrentBuilder.BitCast(this.TypedPointer, this.sharedState.Types.Tuple);
                return this.opaquePointer;
            }
        }

        internal Value TypedPointer
        {
            get
            {
                if (this.opaquePointer == null && this.typedPointer == null)
                {
                    this.AllocateTuple();
                }

                this.typedPointer ??= this.sharedState.CurrentBuilder.BitCast(this.OpaquePointer, this.StructType.CreatePointerType());
                return this.typedPointer;
            }
        }

        /// <summary>
        /// Creates a new tuple value. The allocation of the value via invokation of the corresponding runtime function
        /// is lazy, and so are the necessary casts. When needed, the instructions are emitted using the current builder.
        /// Registers the value with the scope manager once it is allocated.
        /// </summary>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal TupleValue(ImmutableArray<ResolvedType> elementTypes, GenerationContext context)
        {
            this.sharedState = context;
            this.ElementTypes = elementTypes;
            this.StructType = this.sharedState.Types.TypedTuple(elementTypes.Select(context.LlvmTypeFromQsharpType));
        }

        /// <summary>
        /// Creates a new tuple value representing a Q# value of user defined type from the given tuple pointer.
        /// The casts to get the opaque and typed pointer respectively are executed lazily. When needed, the
        /// instructions are emitted using the current builder.
        /// </summary>
        /// <param name="type">Optionally the user defined type tha that the tuple represents</param>
        /// <param name="tuple">Either an opaque or a typed pointer to the tuple data structure</param>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal TupleValue(UserDefinedType? type, Value tuple, ImmutableArray<ResolvedType> elementTypes, GenerationContext context)
        {
            var isTypedTuple = Types.IsTypedTuple(tuple.NativeType);
            var isOpaqueTuple = Types.IsTuple(tuple.NativeType);
            if (!isTypedTuple && !isOpaqueTuple)
            {
                throw new ArgumentException("expecting either an opaque or a typed tuple");
            }

            this.sharedState = context;
            this.opaquePointer = isOpaqueTuple ? tuple : null;
            this.typedPointer = isTypedTuple ? tuple : null;
            this.customType = type;
            this.ElementTypes = elementTypes;
            this.StructType = this.sharedState.Types.TypedTuple(elementTypes.Select(context.LlvmTypeFromQsharpType));
        }

        /// <summary>
        /// Creates a new tuple value from the given tuple pointer. The casts to get the opaque and typed pointer
        /// respectively are executed lazily. When needed, the instructions are emitted using the current builder.
        /// </summary>
        /// <param name="tuple">Either an opaque or a typed pointer to the tuple data structure</param>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal TupleValue(Value tuple, ImmutableArray<ResolvedType> elementTypes, GenerationContext context)
        : this(null, tuple, elementTypes, context)
        {
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
        /// Returns a pointer to the tuple element at the given index.
        /// </summary>
        /// <param name="index">The element's index into the tuple.</param>
        internal Value GetTupleElementPointer(int index) =>
            this.sharedState.CurrentBuilder.GetElementPtr(this.StructType, this.TypedPointer, this.PointerIndex(index));

        /// <summary>
        /// Returns the tuple element with the given index.
        /// </summary>
        /// <param name="index">The element's index into the tuple.</param>
        internal IValue GetTupleElement(int index)
        {
            var elementPtr = this.GetTupleElementPointer(index);
            var element = this.sharedState.CurrentBuilder.Load(this.StructType.Members[index], elementPtr);
            return this.sharedState.Values.From(element, this.ElementTypes[index]);
        }

        /// <summary>
        /// Returns an array with all pointers to the tuple elements.
        /// </summary>
        internal Value[] GetTupleElementPointers() =>
            this.StructType.Members
                .Select((_, i) => this.sharedState.CurrentBuilder.GetElementPtr(this.StructType, this.TypedPointer, this.PointerIndex(i)))
                .ToArray();

        /// <summary>
        /// Returns an array with all tuple elements.
        /// </summary>
        internal IValue[] GetTupleElements()
        {
            var elementPtrs = this.GetTupleElementPointers();
            var elements = this.StructType.Members.Select((itemType, i) => this.sharedState.CurrentBuilder.Load(itemType, elementPtrs[i]));
            return elements.Select((element, i) => this.sharedState.Values.From(element, this.ElementTypes[i])).ToArray();
        }
    }

    /// <summary>
    /// Stores the QIR representation of a Q# array.
    /// </summary>
    internal class ArrayValue : IValue
    {
        private readonly GenerationContext sharedState;

        // Imporant: the constructors must ensure that either length or opaque pointer is not null!
        private Value? opaquePointer;
        private Value? length;

        public Value Value => this.OpaquePointer;

        public ITypeRef LlvmType => this.sharedState.Types.Array;

        public ResolvedType QSharpType =>
            ResolvedType.New(QsResolvedTypeKind.NewArrayType(this.qsElementType));

        private readonly ResolvedType qsElementType;
        public readonly ITypeRef ElementType;
        public readonly uint? Count;

        public Value Length
        {
            get
            {
                if (this.length == null)
                {
                    var getLength = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetSize1d);
                    this.length = this.sharedState.CurrentBuilder.Call(getLength, this.opaquePointer ?? throw new InvalidOperationException("array has no value"));
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
                    var elementSize = this.sharedState.ComputeSizeForType(this.ElementType, this.sharedState.Context.Int32Type);
                    this.opaquePointer = this.sharedState.CurrentBuilder.Call(constructor, elementSize, this.Length);
                    this.sharedState.ScopeMgr.RegisterValue(this);
                }
                return this.opaquePointer;
            }
        }

        /// <summary>
        /// Creates a new array value. The allocation of the value via invokation of the corresponding runtime function
        /// is lazy, and so are other necessary computations. When needed, the instructions are emitted using the current builder.
        /// Registers the value with the scope manager once it is allocated.
        /// </summary>
        /// <param name="count">The number of elements in the array</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal ArrayValue(uint count, ResolvedType elementType, GenerationContext context)
        {
            this.sharedState = context;
            this.qsElementType = elementType;
            this.ElementType = context.LlvmTypeFromQsharpType(elementType);
            this.Count = count;
            this.length = context.Context.CreateConstant((long)count);
        }

        /// <summary>
        /// Creates a new array value of the given length. Expects a value of type i64 for the length of the array.
        /// The allocation of the value via invokation of the corresponding runtime function is lazy, and so are
        /// other necessary computations. When needed, the instructions are emitted using the current builder.
        /// Registers the value with the scope manager once it is allocated.
        /// </summary>
        /// <param name="length">Value of type i64 indicating the number of elements in the array</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal ArrayValue(Value length, ResolvedType elementType, GenerationContext context)
        {
            this.sharedState = context;
            this.qsElementType = elementType;
            this.ElementType = context.LlvmTypeFromQsharpType(elementType);
            this.length = length;
        }

        /// <summary>
        /// Creates a new array value from the given opaque array of elements of the given type. When needed,
        /// the instructions to compute the length of the array are emitted using the current builder.
        /// </summary>
        /// <param name="array">The opaque pointer to the array data structure</param>
        /// <param name="length">Value of type i64 indicating the number of elements in the array; will be computed on demand if the given value is null</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal ArrayValue(Value array, Value? length, ResolvedType elementType, GenerationContext context)
        {
            this.sharedState = context;
            this.qsElementType = elementType;
            this.ElementType = context.LlvmTypeFromQsharpType(elementType);
            this.opaquePointer = Types.IsArray(array.NativeType) ? array : throw new ArgumentException("expecting an opaque array");
            this.length = length;
        }

        // methods for item access

        /// <summary>
        /// Returns a pointer to the array element at the given index.
        /// </summary>
        /// <param name="index">The element's index into the array.</param>
        internal Value GetArrayElementPointer(Value index)
        {
            var getElementPointer = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d);
            var opaqueElementPointer = this.sharedState.CurrentBuilder.Call(getElementPointer, this.OpaquePointer, index);
            return this.sharedState.CurrentBuilder.BitCast(opaqueElementPointer, this.ElementType.CreatePointerType());
        }

        /// <summary>
        /// Returns the array element at the given index.
        /// </summary>
        /// <param name="index">The element's index into the array.</param>
        internal IValue GetArrayElement(Value index)
        {
            var elementPtr = this.GetArrayElementPointer(index);
            var element = this.sharedState.CurrentBuilder.Load(this.ElementType, elementPtr);
            return this.sharedState.Values.From(element, this.qsElementType);
        }

        /// <summary>
        /// Returns the pointers to an array element at the given indices.
        /// If no indices are specified, returns all element pointers if the length of the array is know,
        /// i.e. it it has been instantiated with a count, and throws an InvalidOperationException otherwise.
        /// </summary>
        internal Value[] GetArrayElementPointers(params int[] indices)
        {
            var enumerable = indices.Length != 0 ? indices :
                this.Count != null && this.Count <= int.MaxValue ? Enumerable.Range(0, (int)this.Count) :
                throw new InvalidOperationException("cannot get all element pointers for array of unknown length");

            return enumerable
                .Select(idx => this.sharedState.Context.CreateConstant((long)idx))
                .Select(idx => this.GetArrayElementPointer(idx))
                .ToArray();
        }

        /// <summary>
        /// Returns the array elements at the given indices.
        /// If no indices are specified, returns all elements if the length of the array is know,
        /// i.e. it it has been instantiated with a count, and throws an InvalidOperationException otherwise.
        /// </summary>
        internal IValue[] GetArrayElements(params int[] indices)
        {
            var elementPtrs = this.GetArrayElementPointers(indices);
            var elements = elementPtrs.Select(ptr => this.sharedState.CurrentBuilder.Load(this.ElementType, ptr));
            return elements.Select((element, i) => this.sharedState.Values.From(element, this.qsElementType)).ToArray();
        }
    }

    /// <summary>
    /// Stores the QIR representation of a Q# callable.
    /// </summary>
    internal class CallableValue : IValue
    {
        public Value Value { get; }

        /// <summary>
        /// The LLVM type by which the value is passed across function bounaries,
        /// i.e. the LLVM native type of the stored value.
        /// </summary>
        public ITypeRef LlvmType { get; }

        public ResolvedType QSharpType { get; }

        internal CallableValue(Value value, ResolvedType type, GenerationContext context)
        {
            this.Value = value;
            this.LlvmType = context.Types.Callable;
            this.QSharpType = type;
        }
    }
}
