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
        /// Used to handle value properties that are computed on demand, such as e.g. the length of an array
        /// or loading the content of a pointer. Using this class avoids unnecessary recomputes by storing
        /// the output of the computation and only recomputing it when needed.
        /// </summary>
        protected class Cached<T>
        where T : class
        {
            private readonly GenerationContext sharedState;
            private readonly Func<T> load;
            private readonly Action<T>? store;

            // We need to store the branch id with the value since the value needs to be reloaded
            // (only) if it was set within a branch that is not a parent branch of the current branch.
            private (int, T?) cache;

            internal Cached(T? value, GenerationContext context, Func<T> load, Action<T>? store = null)
            {
                this.sharedState = context;
                this.load = load;
                this.store = store;
                this.cache = (context.CurrentBranch, value);
            }

            internal Cached(GenerationContext context, Func<T> load, Action<T>? store = null)
            : this(null, context, load, store)
            {
            }

            public bool IsCached =>
                this.cache.Item2 != null &&
                (this.store == null || !this.sharedState.IsWithinLoop) &&
                this.sharedState.IsOpenBranch(this.cache.Item1);

            /// <summary>
            /// Returns the cached value stored or loads it if necessary.
            /// </summary>
            public T Load()
            {
                // We need to force that mutable variables that are set within the loop are reloaded
                // when they are used instead of accessing the cached version.
                // We could be smarter and only reload them if they are indeed updated as part of the loop.
                if (!this.IsCached)
                {
                    var loaded = this.load();
                    this.cache = (this.sharedState.CurrentBranch, loaded);
                }

                return this.cache.Item2;
            }

            /// <summary>
            /// If a store function has been defind upon constuction, stores and caches the given value.
            /// Throws and InvalidOperationException if no store function has been defined.
            /// </summary>
            public void Store(T value)
            {
                if (this.store == null)
                {
                    throw new InvalidOperationException("no storage function defined");
                }

                this.store(value);
                this.cache = (this.sharedState.CurrentBranch, value);
            }
        }

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

        /// <summary>
        /// Registers the given name as the name of the LLVM value using <see cref="ValueExtensions.RegisterName" />.
        /// Does nothing if a name is already defined for the value.
        /// </summary>
        internal void RegisterName(string name)
        {
            if (string.IsNullOrEmpty(this.Value.Name))
            {
                this.Value.RegisterName(name);
            }
        }
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
        private readonly IValue.Cached<IValue> cachedValue;
        private readonly Value pointer;

        public Value Value => this.LoadValue().Value;

        public ITypeRef LlvmType { get; }

        public ResolvedType QSharpType { get; }

        /// <summary>
        /// Creates a pointer that can store a value and provides a caching mechanism for accessing that value
        /// to avoid unnecessary loads. The pointer is instantiated with the given pointer.
        /// If the given pointer is null, a new pointer is created via an alloca instruction.
        /// </summary>
        /// <param name="pointer">Optional parameter to provide an existing pointer to use</param>
        /// <param name="type">The Q# type of the value that the pointer points to</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal PointerValue(Value? pointer, ResolvedType type, GenerationContext context)
        {
            void Store(IValue v) =>
                context.CurrentBuilder.Store(v.Value, this.pointer);

            IValue Reload() =>
                context.Values.From(
                    context.CurrentBuilder.Load(this.LlvmType, this.pointer),
                    this.QSharpType);

            this.QSharpType = type;
            this.LlvmType = context.LlvmTypeFromQsharpType(this.QSharpType);
            this.pointer = pointer ?? context.CurrentBuilder.Alloca(this.LlvmType);
            this.cachedValue = new IValue.Cached<IValue>(context, Reload, Store);
        }

        /// <summary>
        /// Creates a pointer that represents a mutable variable.
        /// </summary>
        /// <param name="value">The value to store</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal PointerValue(IValue value, GenerationContext context)
        : this(null, value.QSharpType, context)
        {
            this.cachedValue.Store(value);
        }

        /// <summary>
        /// Loads and returns the current value of the mutable variable.
        /// </summary>
        public IValue LoadValue() =>
            this.cachedValue.Load();

        /// <summary>
        /// Sets the mutable variable to the given value.
        /// </summary>
        public void StoreValue(IValue value) =>
            this.cachedValue.Store(value);

        void IValue.RegisterName(string name)
        {
            if (string.IsNullOrEmpty(this.pointer.Name))
            {
                this.pointer.RegisterName(name);
            }
        }
    }

    /// <summary>
    /// Stores the QIR representation of a Q# tuple or a value of user defined type.
    /// </summary>
    internal class TupleValue : IValue
    {
        private readonly GenerationContext sharedState;
        private readonly UserDefinedType? customType;

        // IMPORTANT:
        // The constructors need to ensure that either the typed pointer
        // or the opaque pointer is instantiated with a value!
        private readonly IValue.Cached<Value> opaquePointer;
        private readonly IValue.Cached<Value> typedPointer;
        private readonly IValue.Cached<PointerValue>[] tupleElementPointers;

        public Value Value => this.TypedPointer;

        public ITypeRef LlvmType => this.StructType.CreatePointerType();

        public ResolvedType QSharpType => this.customType != null
            ? ResolvedType.New(QsResolvedTypeKind.NewUserDefinedType(this.customType))
            : ResolvedType.New(QsResolvedTypeKind.NewTupleType(ImmutableArray.CreateRange(this.ElementTypes)));

        internal readonly ImmutableArray<ResolvedType> ElementTypes;
        public readonly IStructType StructType;

        internal Value OpaquePointer =>
            this.opaquePointer.Load();

        internal Value TypedPointer =>
            this.typedPointer.Load();

        /// <summary>
        /// Creates a new tuple value. The allocation of the value via invokation of the corresponding runtime function
        /// is lazy, and so are the necessary casts. When needed, the instructions are emitted using the current builder.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal TupleValue(ImmutableArray<ResolvedType> elementTypes, GenerationContext context, bool registerWithScopeManager = true)
        {
            this.sharedState = context;
            this.ElementTypes = elementTypes;
            this.StructType = this.sharedState.Types.TypedTuple(elementTypes.Select(context.LlvmTypeFromQsharpType));
            this.opaquePointer = this.CreateOpaquePointerCache(this.AllocateTuple(registerWithScopeManager));
            this.typedPointer = this.CreateTypedPointerCache();
            this.tupleElementPointers = this.CreateTupleElementPointersCaches();
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
            this.customType = type;
            this.ElementTypes = elementTypes;
            this.StructType = this.sharedState.Types.TypedTuple(elementTypes.Select(context.LlvmTypeFromQsharpType));
            this.opaquePointer = this.CreateOpaquePointerCache(isOpaqueTuple ? tuple : null);
            this.typedPointer = this.CreateTypedPointerCache(isTypedTuple ? tuple : null);
            this.tupleElementPointers = this.CreateTupleElementPointersCaches();
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

        // private helpers

        private Value GetOpaquePointer() =>
            this.typedPointer.IsCached
            ? this.sharedState.CurrentBuilder.BitCast(this.TypedPointer, this.sharedState.Types.Tuple)
            : throw new InvalidOperationException("tuple pointer is undefined");

        private Value GetTypedPointer() =>
            this.opaquePointer.IsCached
            ? this.sharedState.CurrentBuilder.BitCast(this.OpaquePointer, this.StructType.CreatePointerType())
            : throw new InvalidOperationException("tuple pointer is undefined");

        private IValue.Cached<Value> CreateOpaquePointerCache(Value? pointer = null) =>
            new IValue.Cached<Value>(pointer, this.sharedState, this.GetOpaquePointer);

        private IValue.Cached<Value> CreateTypedPointerCache(Value? pointer = null) =>
            new IValue.Cached<Value>(pointer, this.sharedState, this.GetTypedPointer);

        private Value GetElementPointer(int index) =>
            this.sharedState.CurrentBuilder.GetElementPtr(this.StructType, this.TypedPointer, this.PointerIndex(index));

        private IValue.Cached<PointerValue> CreateCachedPointer(ResolvedType type, int index) =>
            new IValue.Cached<PointerValue>(
                this.sharedState,
                () => new PointerValue(this.GetElementPointer(index), type, this.sharedState));

        private IValue.Cached<PointerValue>[] CreateTupleElementPointersCaches() =>
            this.ElementTypes.Select(this.CreateCachedPointer).ToArray();

        private Value AllocateTuple(bool registerWithScopeManager)
        {
            // The runtime function TupleCreate creates a new value with reference count 1 and access count 0.
            var constructor = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate);
            var size = this.sharedState.ComputeSizeForType(this.StructType);
            var tuple = this.sharedState.CurrentBuilder.Call(constructor, size);
            if (registerWithScopeManager)
            {
                this.sharedState.ScopeMgr.RegisterValue(this);
            }
            return tuple;
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
        internal PointerValue GetTupleElementPointer(int index) =>
            this.tupleElementPointers[index].Load();

        /// <summary>
        /// Returns the tuple element with the given index.
        /// </summary>
        /// <param name="index">The element's index into the tuple.</param>
        internal IValue GetTupleElement(int index) =>
            this.GetTupleElementPointer(index).LoadValue();

        /// <summary>
        /// Returns an array with all pointers to the tuple elements.
        /// </summary>
        internal PointerValue[] GetTupleElementPointers() =>
            this.tupleElementPointers.Select(ptr => ptr.Load()).ToArray();

        /// <summary>
        /// Returns an array with all tuple elements.
        /// </summary>
        internal IValue[] GetTupleElements() =>
            this.GetTupleElementPointers().Select(ptr => ptr.LoadValue()).ToArray();
    }

    /// <summary>
    /// Stores the QIR representation of a Q# array.
    /// </summary>
    internal class ArrayValue : IValue
    {
        private readonly GenerationContext sharedState;
        private readonly ResolvedType qsElementType;
        private readonly IValue.Cached<Value> length;

        public readonly ITypeRef ElementType;
        public readonly uint? Count;
        public readonly Value OpaquePointer;

        public Value Value => this.OpaquePointer;

        public ITypeRef LlvmType => this.sharedState.Types.Array;

        public ResolvedType QSharpType =>
            ResolvedType.New(QsResolvedTypeKind.NewArrayType(this.qsElementType));

        public Value Length => this.length.Load();

        /// <summary>
        /// Creates a new array value.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="count">The number of elements in the array</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal ArrayValue(uint count, ResolvedType elementType, GenerationContext context, bool registerWithScopeManager = true)
        {
            this.sharedState = context;
            this.qsElementType = elementType;
            this.ElementType = context.LlvmTypeFromQsharpType(elementType);
            this.Count = count;
            this.length = this.CreateLengthCache(context.Context.CreateConstant((long)count));
            this.OpaquePointer = this.AllocateArray(registerWithScopeManager);
        }

        /// <summary>
        /// Creates a new array value of the given length. Expects a value of type i64 for the length of the array.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="length">Value of type i64 indicating the number of elements in the array</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal ArrayValue(Value length, ResolvedType elementType, GenerationContext context, bool registerWithScopeManager = true)
        {
            this.sharedState = context;
            this.qsElementType = elementType;
            this.ElementType = context.LlvmTypeFromQsharpType(elementType);
            this.length = this.CreateLengthCache(length);
            this.OpaquePointer = this.AllocateArray(registerWithScopeManager);
        }

        /// <summary>
        /// Creates a new array value from the given opaque array of elements of the given type.
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
            this.OpaquePointer = Types.IsArray(array.NativeType) ? array : throw new ArgumentException("expecting an opaque array");
            this.length = length == null
                ? new IValue.Cached<Value>(context, this.GetLength)
                : this.CreateLengthCache(length);
        }

        // private helpers

        private Value GetLength() => this.sharedState.CurrentBuilder.Call(
            this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetSize1d),
            this.OpaquePointer);

        private IValue.Cached<Value> CreateLengthCache(Value length) =>
            new IValue.Cached<Value>(length, this.sharedState, this.GetLength);

        private Value AllocateArray(bool registerWithScopeManager)
        {
            // The runtime function ArrayCreate1d creates a new value with reference count 1 and access count 0.
            var constructor = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCreate1d);
            var elementSize = this.sharedState.ComputeSizeForType(this.ElementType, this.sharedState.Context.Int32Type);
            var pointer = this.sharedState.CurrentBuilder.Call(constructor, elementSize, this.Length);
            if (registerWithScopeManager)
            {
                this.sharedState.ScopeMgr.RegisterValue(this);
            }
            return pointer;
        }

        // methods for item access

        /// <summary>
        /// Returns a pointer to the array element at the given index.
        /// </summary>
        /// <param name="index">The element's index into the array.</param>
        internal PointerValue GetArrayElementPointer(Value index)
        {
            var getElementPointer = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d);
            var opaqueElementPointer = this.sharedState.CurrentBuilder.Call(getElementPointer, this.OpaquePointer, index);
            var typedElementPointer = this.sharedState.CurrentBuilder.BitCast(opaqueElementPointer, this.ElementType.CreatePointerType());
            return new PointerValue(typedElementPointer, this.qsElementType, this.sharedState);
        }

        /// <summary>
        /// Returns the array element at the given index.
        /// </summary>
        /// <param name="index">The element's index into the array.</param>
        internal IValue GetArrayElement(Value index) =>
            this.GetArrayElementPointer(index).LoadValue();

        /// <summary>
        /// Returns the pointers to the array elements at the given indices.
        /// If no indices are specified, returns all element pointers if the length of the array is known,
        /// i.e. it it has been instantiated with a count, and throws an InvalidOperationException otherwise.
        /// </summary>
        internal PointerValue[] GetArrayElementPointers(params int[] indices)
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
        /// If no indices are specified, returns all elements if the length of the array is known,
        /// i.e. it it has been instantiated with a count, and throws an InvalidOperationException otherwise.
        /// </summary>
        internal IValue[] GetArrayElements(params int[] indices) =>
            this.GetArrayElementPointers(indices).Select(ptr => ptr.LoadValue()).ToArray();
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
