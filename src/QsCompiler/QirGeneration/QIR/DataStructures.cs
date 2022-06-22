// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LlvmBindings.Types;
using LlvmBindings.Values;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

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
                this.sharedState.IsOpenBranch(this.cache.Item1) &&
                (this.store == null || !this.sharedState.IsWithinLoop || this.sharedState.IsWithinCurrentLoop(this.cache.Item1));

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

                return this.cache.Item2!; // safe since IsCached checks for null and load returns a non-null value
            }

            /// <summary>
            /// If a store function has been defined upon constuction, stores and caches the given value.
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
        private readonly Value? accessHandle; // this handle is a pointer for loading the current value or null if a custom store and load is defined

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
        internal PointerValue(Value? pointer, ResolvedType type, ITypeRef llvmType, GenerationContext context)
        {
            void Store(IValue v) =>
                context.CurrentBuilder.Store(v.Value, this.accessHandle);

            IValue Reload() =>
                context.Values.From(
                    context.CurrentBuilder.Load(this.LlvmType, this.accessHandle),
                    this.QSharpType);

            this.QSharpType = type;
            this.LlvmType = llvmType;
            this.accessHandle = pointer ?? context.Allocate(this.LlvmType);
            this.cachedValue = new IValue.Cached<IValue>(context, Reload, Store);
        }

        /// <summary>
        /// Creates a abstraction for storing and retrieving a value, including a caching mechanism for accessing that value
        /// to avoid unnecessary loads. The given load and store functions are used to access and modify the stored value if necessary.
        /// </summary>
        /// <param name="type">The Q# type of the value that the pointer points to</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="load">Function used to access the stored value</param>
        /// <param name="store">Function used to update the stored value</param>
        internal PointerValue(ResolvedType type, ITypeRef llvmType, GenerationContext context, Func<IValue> load, Action<IValue> store)
        {
            this.QSharpType = type;
            this.LlvmType = llvmType;
            this.cachedValue = new IValue.Cached<IValue>(context, load, store);
        }

        internal PointerValue(IValue initialContent, GenerationContext context, Func<IValue> load, Action<IValue> store)
        {
            this.QSharpType = initialContent.QSharpType;
            this.LlvmType = initialContent.LlvmType;
            this.cachedValue = new IValue.Cached<IValue>(initialContent, context, load, store);
        }

        /// <summary>
        /// Creates a pointer that represents a mutable variable.
        /// </summary>
        /// <param name="value">The value to store</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal PointerValue(IValue value, GenerationContext context)
        : this(null, value.QSharpType, value.LlvmType, context) =>
            this.cachedValue.Store(value);

        /// <summary>
        /// Loads and returns the current value of the mutable variable.
        /// </summary>
        public IValue LoadValue() =>
            this.cachedValue.Load();

        internal IValue? CurrentCache() =>
            this.cachedValue.IsCached ? this.cachedValue.Load() : null;

        /// <summary>
        /// Sets the mutable variable to the given value.
        /// </summary>
        public void StoreValue(IValue value) =>
            this.cachedValue.Store(value);

        void IValue.RegisterName(string name)
        {
            if (this.accessHandle != null && string.IsNullOrEmpty(this.accessHandle.Name))
            {
                this.accessHandle.RegisterName(name);
            }
        }
    }

    /// <summary>
    /// Stores the QIR representation of a Q# tuple or a value of user defined type.
    /// </summary>
    internal class TupleValue : IValue
    {
        private readonly GenerationContext sharedState;

        // IMPORTANT:
        // The constructors need to ensure that either the typed pointer
        // or the opaque pointer or the llvm native value is set to a value!
        private readonly IValue.Cached<Value> opaquePointer;
        private readonly IValue.Cached<Value> typedPointer;
        private readonly IValue.Cached<PointerValue>[] tupleElementPointers;

        private Value? LlvmNativeValue { get; set; }

        internal Value OpaquePointer => this.opaquePointer.Load();

        internal Value TypedPointer => this.typedPointer.Load();

        public Value Value => this.LlvmNativeValue ?? this.TypedPointer;

        public IStructType StructType { get; }

        public ITypeRef LlvmType =>
            this.LlvmNativeValue is null
            ? this.StructType.CreatePointerType()
            : (ITypeRef)this.StructType;

        internal ImmutableArray<ResolvedType> ElementTypes { get; }

        public ResolvedType QSharpType => this.TypeName != null
                ? ResolvedType.New(QsResolvedTypeKind.NewUserDefinedType(
                    new UserDefinedType(this.TypeName.Namespace, this.TypeName.Name, QsNullable<QsCompiler.DataTypes.Range>.Null)))
                : ResolvedType.New(QsResolvedTypeKind.NewTupleType(ImmutableArray.CreateRange(this.ElementTypes)));

        public QsQualifiedName? TypeName { get; }

        /// <summary>
        /// Creates a new tuple value representing a Q# value of user defined type.
        /// The casts to get the opaque and typed pointer respectively are executed lazily. When needed,
        /// the instructions are emitted using the current builder.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// IMPORTANT:
        /// Does *not* increase the reference count of the given tupleElements.
        /// This constructor should remain private.
        /// </summary>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        private TupleValue(QsQualifiedName? type, Func<TupleValue, Value> createValue, ImmutableArray<ResolvedType>? qsElementTypes, IReadOnlyList<IValue>? tupleElements, GenerationContext context)
        {
            var elementTypes = tupleElements is null
                ? qsElementTypes!.Value.Select(t => (t, context.LlvmTypeFromQsharpType(t))).ToArray()
                : tupleElements.Select(element => (element.QSharpType, element.LlvmType)).ToArray();

            this.sharedState = context;
            this.TypeName = type;
            this.ElementTypes = elementTypes.Select(t => t.Item1).ToImmutableArray();
            this.StructType = this.sharedState.Types.TypedTuple(elementTypes.Select(t => t.Item2));

            var value = createValue(this);
            this.LlvmNativeValue = value.NativeType is IStructType ? value : null;
            this.opaquePointer = this.CreateOpaquePointerCache(Types.IsTupleOrUnit(value.NativeType) && !value.IsNull ? value : null);
            this.typedPointer = this.CreateTypedPointerCache(Types.IsTypedTuple(value.NativeType) ? value : null);
            this.tupleElementPointers = this.CreateTupleElementPointersCaches(elementTypes);

            if (tupleElements != null)
            {
                var itemPointers = this.GetTupleElementPointers();
                for (var i = 0; i < itemPointers.Length; ++i)
                {
                    // Keep this constructor private, since we don't increase the ref counts!
                    itemPointers[i].StoreValue(tupleElements[i]);
                }
            }
        }

        internal TupleValue(QsQualifiedName? type, IReadOnlyList<IValue> tupleElements, GenerationContext context, bool allocOnStack, bool registerWithScopeManager)
            : this(type, self => allocOnStack ? self.StructType.GetNullValue() : self.AllocateTuple(registerWithScopeManager), null, tupleElements, context)
        {
            foreach (var element in tupleElements)
            {
                this.sharedState.ScopeMgr.IncreaseReferenceCount(element);
            }
        }

        internal TupleValue(QsQualifiedName? type, ImmutableArray<TypedExpression> tupleElements, GenerationContext context, bool allocOnStack, bool registerWithScopeManager)
            : this(type, self => allocOnStack ? self.StructType.GetNullValue() : self.AllocateTuple(registerWithScopeManager), null, tupleElements.Select(context.BuildSubitem).ToArray(), context)
        {
        }

        /// <summary>
        /// Creates a new tuple value. The casts to get the opaque and typed pointer
        /// respectively are executed lazily. When needed, the instructions are emitted using the current builder.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal TupleValue(IReadOnlyList<IValue> tupleElements, GenerationContext context, bool registerWithScopeManager)
            : this(null, tupleElements, context, allocOnStack: false, registerWithScopeManager: registerWithScopeManager)
        {
        }

        internal TupleValue(ImmutableArray<ResolvedType> elementTypes, GenerationContext context, bool registerWithScopeManager)
            : this(null, self => self.AllocateTuple(registerWithScopeManager), elementTypes, null, context)
        {
        }

        /// <summary>
        /// Creates a new tuple value representing a Q# value of user defined type from the given tuple pointer.
        /// The casts to get the opaque and typed pointer respectively are executed lazily. When needed,
        /// instructions are emitted using the current builder.
        /// </summary>
        /// <param name="type">Optionally the user defined type tha that the tuple represents</param>
        /// <param name="tuple">Either an opaque or a typed pointer to the tuple data structure</param>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal TupleValue(QsQualifiedName? type, Value tuple, ImmutableArray<ResolvedType> elementTypes, GenerationContext context)
            : this(type, _ => tuple, elementTypes, null, context)
        {
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

        /* private helpers */

        private IValue.Cached<Value> CreateOpaquePointerCache(Value? pointer = null) =>
            new IValue.Cached<Value>(pointer, this.sharedState, () =>
                this.typedPointer.IsCached
                ? this.sharedState.CurrentBuilder.BitCast(this.TypedPointer, this.sharedState.Types.Tuple)
                : throw new InvalidOperationException("tuple pointer is undefined"));

        private IValue.Cached<Value> CreateTypedPointerCache(Value? pointer = null) =>
            new IValue.Cached<Value>(pointer, this.sharedState, () =>
                this.opaquePointer.IsCached
                ? this.sharedState.CurrentBuilder.BitCast(this.OpaquePointer, this.StructType.CreatePointerType())
                : throw new InvalidOperationException("tuple pointer is undefined"));

        private IValue.Cached<PointerValue>[] CreateTupleElementPointersCaches(IReadOnlyList<(ResolvedType, ITypeRef)>? elementTypes = null) =>
            Enumerable.ToArray(elementTypes.Select((type, index) => new IValue.Cached<PointerValue>(this.sharedState, () =>
            {
                if (this.LlvmNativeValue is null)
                {
                    var elementPtr = this.sharedState.CurrentBuilder.GetStructElementPointer(this.StructType, this.TypedPointer, (uint)index);
                    return new PointerValue(elementPtr, type.Item1, type.Item2, this.sharedState);
                }
                else
                {
                    void Store(IValue v) =>
                        this.LlvmNativeValue = this.sharedState.CurrentBuilder.InsertValue(this.Value, v.Value, (uint)index);

                    IValue Reload() =>
                        this.sharedState.Values.From(this.sharedState.CurrentBuilder.ExtractValue(this.Value, (uint)index), type.Item1);

                    return new PointerValue(type.Item1, type.Item2, this.sharedState, Reload, Store);
                }
            })));

        private Value AllocateTuple(bool registerWithScopeManager)
        {
            // The runtime function TupleCreate creates a new value with reference count 1 and alias count 0.
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
        // FIXME: ENFORCE THAT STACKALLOC IS ONLY TRUE WHEN ALL ELEMENTS ARE STACK ALLOC?
        // TODO: make Paulis i2s rather than loading them.
        private readonly GenerationContext sharedState;
        private readonly IValue.Cached<Value> length;
        private readonly IValue.Cached<PointerValue>[]? arrayElementPointers;

        internal ResolvedType QSharpElementType { get; }

        public ResolvedType QSharpType =>
            ResolvedType.New(QsResolvedTypeKind.NewArrayType(this.QSharpElementType));

        public ITypeRef LlvmElementType { get; private set; }

        public ITypeRef LlvmType { get; private set; }

        public uint? Count { get; }

        public Value Value { get; private set; }

        public Value OpaquePointer =>
            Types.IsArray(this.LlvmType)
            ? this.Value
            : throw new InvalidOperationException("cannot get opaque pointer for a constant array allocated on the stack");

        public Value Length => this.length.Load();

        /// <summary>
        /// Creates a new array value.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// IMPORTANT:
        /// Does *not* increase the reference count of the given arrayElements.
        /// This constructor should remain private.
        /// </summary>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        private ArrayValue(ResolvedType elementType, uint count, IReadOnlyList<IValue> arrayElements, GenerationContext context, bool allocOnStack, bool registerWithScopeManager)
        {
            this.sharedState = context;
            this.Count = count < arrayElements.Count ? count : (uint)arrayElements.Count;
            this.length = this.CreateLengthCache();
            this.QSharpElementType = elementType;

            if (allocOnStack)
            {
                // If we stack allocate the array we need to rebuild the array elements
                // such that they all have the same (sized) type.
                arrayElements = this.NormalizeArrayElements(elementType, arrayElements, registerWithScopeManager);
                var elementTypes = arrayElements.Select(e => e.LlvmType).Distinct();
                this.LlvmElementType = elementTypes.SingleOrDefault() ?? context.Values.DefaultValue(elementType).LlvmType;
                this.Value = CreateNativeValue(this.LlvmElementType, (uint)arrayElements.Count(), this.Count.Value, context);
                this.LlvmType = this.Value.NativeType;
            }
            else
            {
                this.LlvmElementType = context.LlvmTypeFromQsharpType(elementType);
                this.LlvmType = this.sharedState.Types.Array;
                this.Value = this.AllocateArray(registerWithScopeManager);
            }

            this.arrayElementPointers = this.CreateArrayElementPointersCaches();
            var itemPointers = this.GetArrayElementPointers();
            for (var i = 0; i < itemPointers.Length; ++i)
            {
                itemPointers[i].StoreValue(arrayElements[i]);
            }
        }

        internal ArrayValue(ResolvedType elementType, ImmutableArray<TypedExpression> arrayElements, GenerationContext context, bool allocOnStack, bool registerWithScopeManager)
            : this(elementType, (uint)arrayElements.Length, arrayElements.Select(context.BuildSubitem).ToArray(), context, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager)
        {
        }

        internal ArrayValue(ResolvedType elementType, IReadOnlyList<IValue> arrayElements, GenerationContext context, bool allocOnStack, bool registerWithScopeManager)
            : this(elementType, (uint)arrayElements.Count, arrayElements, context, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager)
        {
            foreach (var element in arrayElements)
            {
                // We need to make sure the reference count increase is applied here;
                // In contrast to tuples and custom types, it is possible for the same item
                // to be assigned to multiple items in an array without binding the item to
                // a variable first. If we applied the reference count increase lazily here,
                // then it would be possible that copy-and-update expressions of the array
                // would lead to a reference count decrease of that item before the pending
                // increases have been applied, resulting in a memory error.
                this.sharedState.ScopeMgr.OpenScope();
                this.sharedState.ScopeMgr.CloseScope(element);
            }
        }

        internal ArrayValue(ArrayValue value, bool alwaysCopy = false)
        {
            this.sharedState = value.sharedState;
            this.Count = value.Count;
            this.length = this.CreateLengthCache(value.length.IsCached ? value.length.Load() : null);
            this.QSharpElementType = value.QSharpElementType;
            this.LlvmType = value.LlvmType;
            this.LlvmElementType = value.LlvmElementType;
            this.Value = value.Value;

            if (Types.IsArray(value.LlvmType))
            {
                var createShallowCopy = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy);
                var forceCopy = this.sharedState.Context.CreateConstant(alwaysCopy);
                this.Value = this.sharedState.CurrentBuilder.Call(createShallowCopy, value.OpaquePointer, forceCopy);
            }

            this.arrayElementPointers = value.arrayElementPointers?.Select((cache, idx) =>
                this.CreateArrayElementPointersCache(idx, cache.IsCached ? cache.Load() : null)).ToArray();
        }

        /// <summary>
        /// Creates a new array value from the given opaque array of elements of the given type.
        /// </summary>
        /// <param name="value">The opaque pointer to the array data structure</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal ArrayValue(Value value, ResolvedType elementType, uint? count, GenerationContext context)
        {
            this.sharedState = context;
            this.length = this.CreateLengthCache();
            this.QSharpElementType = elementType;
            this.LlvmType = value.NativeType;
            this.Count = count;

            if (this.IsNativeValue(out var constArr, out var length, value: value))
            {
                this.Count ??= QirValues.AsConstantUInt32(length);
                this.LlvmElementType = ((IArrayType)constArr.NativeType).ElementType;
                this.Value = value;
            }
            else
            {
                this.LlvmElementType = context.LlvmTypeFromQsharpType(elementType);
                this.Value = Types.IsArray(value.NativeType) ? value : throw new ArgumentException("expecting an opaque array");
            }

            this.arrayElementPointers = this.Count is null ? null : this.CreateArrayElementPointersCaches();
        }

        /// <summary>
        /// Creates a new array value of the given length. Expects a value of type i64 for the length of the array.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="length">Value of type i64 indicating the number of elements in the array</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        internal ArrayValue(ResolvedType elementType, Value length, GenerationContext context, bool registerWithScopeManager)
        {
            this.sharedState = context;
            this.Count = QirValues.AsConstantUInt32(length);
            this.length = this.CreateLengthCache(length);
            this.QSharpElementType = elementType;
            this.LlvmElementType = context.LlvmTypeFromQsharpType(elementType);
            this.LlvmType = this.sharedState.Types.Array;
            this.Value = this.AllocateArray(registerWithScopeManager);
            this.arrayElementPointers = this.Count is null ? null : this.CreateArrayElementPointersCaches();
        }

        internal ArrayValue(ResolvedType elementType, Value length, Func<Value, IValue> getElement, GenerationContext context, bool registerWithScopeManager)
            : this(elementType, length, context, registerWithScopeManager: registerWithScopeManager)
        {
            if (this.Count != 0)
            {
                // We need to populate the array
                var start = this.sharedState.Context.CreateConstant(0L);
                var end = this.Count != null
                    ? this.sharedState.Context.CreateConstant((long)this.Count - 1L)
                    : this.sharedState.CurrentBuilder.Sub(this.Length, this.sharedState.Context.CreateConstant(1L));
                this.sharedState.IterateThroughRange(start, null, end, index =>
                {
                    // We need to make sure that the reference count for the item is increased by 1,
                    // and the iteration loop expects that the body handles its own reference counting.
                    this.sharedState.ScopeMgr.OpenScope();
                    var itemValue = getElement(index);
                    this.GetArrayElementPointer(index).StoreValue(itemValue);
                    this.sharedState.ScopeMgr.CloseScope(itemValue);
                });
            }
        }

        /* private helpers */

        private static IStructType NativeType(ITypeRef elementType, uint nrElements, GenerationContext context) =>
            context.Context.CreateStructType(
                packed: false,
                elementType.CreateArrayType(nrElements),
                context.Types.Int); // to store the actual count

        private static Value CreateNativeValue(Value constArray, Value length, GenerationContext context)
        {
            var constArrType = (IArrayType)constArray.NativeType;
            var nativeType = NativeType(constArrType.ElementType, constArrType.Length, context);

            Value nativeValue = nativeType.GetNullValue();
            nativeValue = context.CurrentBuilder.InsertValue(nativeValue, constArray, 0u);
            nativeValue = context.CurrentBuilder.InsertValue(nativeValue, length, 1u);
            return nativeValue;
        }

        private static Value CreateNativeValue(Value constArray, uint count, GenerationContext context) =>
            CreateNativeValue(constArray, context.Context.CreateConstant((long)count), context);

        private static Value CreateNativeValue(ITypeRef elementType, uint nrElements, uint count, GenerationContext context) =>
            CreateNativeValue(elementType.CreateArrayType(nrElements).GetNullValue(), count, context);

        private Value UpdateNativeValue(Func<Value, Value>? transformConstArr = null) =>
            this.IsNativeValue(out var constArr, out var length)
                ? CreateNativeValue(
                    transformConstArr?.Invoke(constArr) ?? constArr,
                    this.Count != null ? this.sharedState.Context.CreateConstant((long)this.Count) : length,
                    this.sharedState)
                : throw new InvalidOperationException("no native llvm represenation available");

        private bool IsNativeValue([MaybeNullWhen(false)] out Value constArr, [MaybeNullWhen(false)] out Value length, Value? value = null)
        {
            value ??= this.Value;
            if (value.NativeType is IStructType st
                && st.Members.Count == 2
                && st.Members[0] is IArrayType
                && st.Members[1] == this.sharedState.Types.Int)
            {
                constArr = this.sharedState.CurrentBuilder.ExtractValue(value, 0u);
                length = this.sharedState.CurrentBuilder.ExtractValue(value, 1u);
                return true;
            }

            (constArr, length) = (null, null);
            return false;
        }

        private IValue.Cached<Value> CreateLengthCache(Value? length = null) =>
            new IValue.Cached<Value>(length, this.sharedState, () =>
                this.Count is uint count ? this.sharedState.Context.CreateConstant((long)count)
                : this.IsNativeValue(out var _, out var length) ? length
                : this.sharedState.CurrentBuilder.Call(
                    this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetSize1d),
                    this.OpaquePointer));

        private IValue.Cached<PointerValue> CreateArrayElementPointersCache(int index, PointerValue? pointer = null) =>
            new IValue.Cached<PointerValue>(
                pointer is null || Types.IsArray(this.LlvmType) ? null : this.CreateArrayElementPointer(index, pointer.CurrentCache()),
                this.sharedState,
                () => this.CreateArrayElementPointer(index));

        private IValue.Cached<PointerValue>[] CreateArrayElementPointersCaches() =>
            Enumerable.ToArray(Enumerable.Range(0, (int)this.Count!).Select(idx => this.CreateArrayElementPointersCache(idx)));

        private Value AllocateArray(bool registerWithScopeManager)
        {
            // The runtime function ArrayCreate1d creates a new value with reference count 1 and alias count 0.
            var constructor = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCreate1d);
            var elementSize = this.sharedState.ComputeSizeForType(this.LlvmElementType, this.sharedState.Context.Int32Type);
            var pointer = this.sharedState.CurrentBuilder.Call(constructor, elementSize, this.Length);
            if (registerWithScopeManager)
            {
                this.sharedState.ScopeMgr.RegisterValue(this);
            }

            return pointer;
        }

        private IReadOnlyList<IValue> NormalizeArrayElements(ResolvedType eltype, IReadOnlyList<IValue> elements, bool registerWithScopeManager)
        {
            IReadOnlyList<T> NormalizeItems<T>(
                uint normalizedCount, IReadOnlyList<T> elements, Func<int, T, IValue> getInnerElements, Func<int, ResolvedType> elementType, Func<int, IReadOnlyList<IValue>, T> buildElement)
            where T : IValue
            {
                if (elements.Select(e => e.LlvmType).Distinct().Count() <= 1)
                {
                    return elements;
                }

                var newElements = new List<IReadOnlyList<IValue>>();
                for (var innerItemIdx = 0; innerItemIdx < normalizedCount; ++innerItemIdx)
                {
                    var innerElements = Enumerable.ToArray(elements.Select(e => getInnerElements(innerItemIdx, e)));
                    var rebuiltElements = this.NormalizeArrayElements(elementType(innerItemIdx), innerElements, registerWithScopeManager);
                    newElements.Add(rebuiltElements.Select((built, idx) =>
                        built.LlvmType == innerElements[idx].LlvmType ? innerElements[idx] : built).ToArray());
                }

                return Enumerable.ToArray(Enumerable.Range(0, elements.Count)
                    .Select(idx => buildElement(idx, newElements.Select(e => e[idx]).ToArray())));
            }

            if (eltype.Resolution is QsResolvedTypeKind.ArrayType it)
            {
                var arrs = elements.Select(e => (ArrayValue)e).ToArray();
                var sizes = arrs.Select(a =>
                    a.IsNativeValue(out var constArr, out var _) && constArr.NativeType is IArrayType iat
                    ? iat.Length
                    : throw new InvalidOperationException("expecting a constant array as inner element"));
                var innerArrSize = Enumerable.Prepend(sizes, 0u).Max();

                IValue GetInnerElements(int idx, ArrayValue array) =>
                    idx < array.Count
                    ? array.GetArrayElement(idx)
                    : this.sharedState.Values.From(array.LlvmElementType.GetNullValue(), array.QSharpElementType);

                return NormalizeItems(innerArrSize, arrs, GetInnerElements, _ => it.Item, (idx, newElements) =>
                    arrs[idx].Count is uint count
                    ? new ArrayValue(it.Item, count, newElements, this.sharedState, allocOnStack: true, registerWithScopeManager: registerWithScopeManager)
                    : throw new InvalidOperationException("cannot resize array of unknown length to match the size of a new item"));
            }
            else if (eltype.Resolution is QsResolvedTypeKind.TupleType ts)
            {
                var tuples = elements.Select(e => (TupleValue)e).ToArray();
                return NormalizeItems(
                    (uint)ts.Item.Length, tuples, (idx, tuple) => tuple.GetTupleElement(idx), idx => ts.Item[idx], (idx, newElements) =>
                    new TupleValue(null, newElements, this.sharedState, allocOnStack: true, registerWithScopeManager: registerWithScopeManager));
            }
            else if (eltype.Resolution is QsResolvedTypeKind.UserDefinedType udt)
            {
                var uts = this.sharedState.GetItemTypes(udt.Item.GetFullName());
                var tuples = elements.Select(e => (TupleValue)e).ToArray();
                return NormalizeItems(
                    (uint)uts.Length, tuples, (idx, tuple) => tuple.GetTupleElement(idx), idx => uts[idx], (idx, newElements) =>
                    new TupleValue(udt.Item.GetFullName(), newElements, this.sharedState, allocOnStack: true, registerWithScopeManager: registerWithScopeManager));
            }
            else
            {
                return elements;
            }
        }

        private (Value, IValue) NormalizeIfNecessary(Value constArr, IValue newElement)
        {
            if (newElement.LlvmType == this.LlvmElementType)
            {
                return (constArr, newElement);
            }
            else
            {
                var currentElements = this.GetArrayElements().Prepend(newElement).ToArray();
                var newElements = this.NormalizeArrayElements(this.QSharpElementType, currentElements, false);
                this.LlvmElementType = newElements.Select(e => e.LlvmType).Distinct().Single();
                this.LlvmType = this.LlvmElementType.CreateArrayType(((IArrayType)constArr.NativeType).Length);

                var rebuiltArr = newElements.Skip(1).Select((element, idx) => (element, idx)).Aggregate(
                    (Value)this.LlvmType.GetNullValue(),
                    (current, next) => this.sharedState.CurrentBuilder.InsertValue(current, next.element.Value, (uint)next.idx));

                return (rebuiltArr, newElements[0]);
            }
        }

        // methods for item access

        /// <summary>
        /// Returns a pointer to the array element at the given index.
        /// </summary>
        /// <param name="index">The element's index into the array.</param>
        private PointerValue CreateArrayElementPointer(Value index, IValue? element = null)
        {
            if (Types.IsArray(this.LlvmType))
            {
                var getElementPointer = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d);
                var opaqueElementPointer = this.sharedState.CurrentBuilder.Call(getElementPointer, this.OpaquePointer, index);
                var typedElementPointer = this.sharedState.CurrentBuilder.BitCast(opaqueElementPointer, this.LlvmElementType.CreatePointerType());
                return new PointerValue(typedElementPointer, this.QSharpElementType, this.LlvmElementType, this.sharedState);
            }
            else if (QirValues.AsConstantUInt32(index) is uint constIndex)
            {
                void Store(IValue v)
                {
                    this.Value = this.UpdateNativeValue(constArr =>
                    {
                        (constArr, v) = this.NormalizeIfNecessary(constArr, v);
                        return this.sharedState.CurrentBuilder.InsertValue(constArr, v.Value, constIndex);
                    });
                }

                IValue Reload() =>
                    this.IsNativeValue(out var constArr, out var _)
                    ? this.sharedState.Values.From(
                        this.sharedState.CurrentBuilder.ExtractValue(constArr, constIndex),
                        this.QSharpElementType)
                    : throw new InvalidOperationException("invalid pointer access in array");

                // TODO: emit poison value if access is out of bounds
                // -> requires update to at least llvm 13 across all consumers of this IR (preferably even newer).
                return element is null
                    ? new PointerValue(this.QSharpElementType, this.LlvmElementType, this.sharedState, Reload, Store)
                    : new PointerValue(element, this.sharedState, Reload, Store);
            }
            else
            {
                (Value ConstArrPtr, Value ElementPtr) GetElementPointer(Value constArr, Value index)
                {
                    var constArrPtr = this.sharedState.Allocate(constArr.NativeType);
                    this.sharedState.CurrentBuilder.Store(constArr, constArrPtr);
                    var elementPtr = this.sharedState.CurrentBuilder.GetElementPtr(
                        constArr.NativeType, constArrPtr, new[] { this.sharedState.Context.CreateConstant(0), index });
                    return (constArrPtr, elementPtr);
                }

                void Store(IValue v) =>
                    this.Value = this.UpdateNativeValue(constArr =>
                    {
                        (constArr, v) = this.NormalizeIfNecessary(constArr, v);
                        var (constArrPtr, elementPtr) = GetElementPointer(constArr, index);
                        this.sharedState.CurrentBuilder.Store(v.Value, elementPtr);
                        return this.sharedState.CurrentBuilder.Load(constArr.NativeType, constArrPtr);
                    });

                IValue Reload()
                {
                    // Since the native value may be replaced as part of "updating" other items,
                    // it is important that we reload the constant array every time!
                    this.IsNativeValue(out var constArr, out var _);
                    var (constArrPtr, elementPtr) = GetElementPointer(constArr!, index);
                    var element = this.sharedState.CurrentBuilder.Load(this.LlvmElementType, elementPtr);
                    return this.sharedState.Values.From(element, this.QSharpElementType);
                }

                return element is null
                    ? new PointerValue(this.QSharpElementType, this.LlvmElementType, this.sharedState, Reload, Store)
                    : new PointerValue(element, this.sharedState, Reload, Store);
            }
        }

        private PointerValue CreatePointerForPoison() => new(
            this.QSharpElementType,
            this.LlvmElementType,
            this.sharedState,
            () => this.sharedState.Values.From(Constant.PoisonValueFor(this.LlvmElementType), this.QSharpElementType),
            _ => { });

        private PointerValue CreateArrayElementPointer(int index, IValue? element = null) =>
            this.CreateArrayElementPointer(this.sharedState.Context.CreateConstant((long)index), element);

        internal PointerValue GetArrayElementPointer(Value index) =>
            this.arrayElementPointers != null && QirValues.AsConstantUInt32(index) is uint idx
            ? (idx < this.arrayElementPointers.Length
                ? this.arrayElementPointers[idx].Load() : this.CreatePointerForPoison())
            : this.CreateArrayElementPointer(index);

        private IValue GetArrayElement(int index) =>
            this.GetArrayElementPointer(this.sharedState.Context.CreateConstant((long)index)).LoadValue();

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
        internal PointerValue[] GetArrayElementPointers(IEnumerable<int>? indices = null)
        {
            var enumerable = indices != null ? indices :
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
        internal IValue[] GetArrayElements(IEnumerable<int>? indices = null) =>
            this.GetArrayElementPointers(indices).Select(ptr => ptr.LoadValue()).ToArray();
    }

    /// <summary>
    /// Stores the QIR representation of a Q# callable.
    /// </summary>
    internal class CallableValue : IValue
    {
        public Value Value { get; }

        /// <inheritdoc cref="IValue.LlvmType" />
        public ITypeRef LlvmType { get; }

        public ResolvedType QSharpType { get; }

        /// <summary>
        /// Creates a callable value of the given type and registers it with the scope manager.
        /// The necessary functions to invoke the callable are defined by the callable table;
        /// i.e. the globally defined array of function pointers accessible via the given global variable.
        /// </summary>
        /// <param name="callableType">The Q# type of the callable value.</param>
        /// <param name="table">The global variable that contains the array of function pointers defining the callable.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
        /// <param name="captured">All captured values.</param>
        internal CallableValue(ResolvedType callableType, GlobalVariable table, GenerationContext context, ImmutableArray<TypedExpression>? captured = null)
        {
            this.QSharpType = callableType;
            this.LlvmType = context.Types.Callable;

            // The runtime function CallableCreate creates a new value with reference count 1.
            var createCallable = context.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
            var capture = captured == null || captured.Value.Length == 0 ? null : context.Values.CreateTuple(captured.Value, allocOnStack: false, registerWithScopeManager: false);
            var memoryManagementTable = context.GetOrCreateCallableMemoryManagementTable(capture);
            this.Value = context.CurrentBuilder.Call(createCallable, table, memoryManagementTable, capture?.OpaquePointer ?? context.Values.Unit.Value);
            context.ScopeMgr.RegisterValue(this);
        }

        /// <summary>
        /// Creates a new callable value of the given type.
        /// </summary>
        /// <param name="value">The pointer to a QIR callable value.</param>
        /// <param name="type">Q# type of the callable.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
        internal CallableValue(Value value, ResolvedType type, GenerationContext context)
        {
            this.QSharpType = type;
            this.LlvmType = context.Types.Callable;
            this.Value = value;
        }
    }
}
