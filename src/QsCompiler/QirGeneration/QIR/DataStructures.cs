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
        /// <param name="pointer">Optional parameter to provide an existing pointer to use.</param>
        /// <param name="type">The Q# type of the value that the pointer points to.</param>
        /// <param name="llvmType">The LLVM type of the value that the pointer points to.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
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
        /// <param name="type">The Q# type of the value that the pointer points to.</param>
        /// <param name="llvmType">The LLVM type of the value that the pointer points to.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
        /// <param name="load">Function used to access the stored value.</param>
        /// <param name="store">Function used to update the stored value.</param>
        internal PointerValue(ResolvedType type, ITypeRef llvmType, GenerationContext context, Func<IValue> load, Action<IValue> store)
        {
            this.QSharpType = type;
            this.LlvmType = llvmType;
            this.cachedValue = new IValue.Cached<IValue>(context, load, store);
        }

        /// <summary>
        /// Creates a abstraction for storing and retrieving a value, including a caching mechanism for accessing that value
        /// to avoid unnecessary loads. The cache is populated with the given <paramref name="initialContent"/>.
        /// The given load and store functions are used to access and modify the stored value if the cache is outdated.
        /// </summary>
        /// <param name="initialContent">The current content of the pointer to populate the cache with.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
        /// <param name="load">Function used to access the stored value.</param>
        /// <param name="store">Function used to update the stored value.</param>
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

        /// <returns>The currently catched value if the cache is still valid and null otherwise.</returns>
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

        private bool IsRuntimeManaged => this.LlvmNativeValue is null;

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
            this.IsRuntimeManaged
            ? this.StructType.CreatePointerType()
            : this.StructType;

        internal ImmutableArray<ResolvedType> ElementTypes { get; }

        public IReadOnlyList<ITypeRef> LlvmElementTypes => this.StructType.Members;

        public ResolvedType QSharpType => this.TypeName != null
            ? ResolvedType.New(QsResolvedTypeKind.NewUserDefinedType(
                new UserDefinedType(this.TypeName.Namespace, this.TypeName.Name, QsNullable<QsCompiler.DataTypes.Range>.Null)))
            : ResolvedType.New(QsResolvedTypeKind.NewTupleType(ImmutableArray.CreateRange(this.ElementTypes)));

        public QsQualifiedName? TypeName { get; }

        /// <summary>
        /// Creates a new tuple value that represents either a Q# value of tuple type or one of user defined type,
        /// and contains additional infos used for optimization during QIR generation as well as the LLVM representation of the value.
        /// The casts to get the opaque and typed pointer respectively are executed lazily.
        /// Accessing the opaque or typed pointer will throw an <see cref="InvalidOperationException"/> if the value is stack allocated.
        /// </summary>
        /// <remarks>
        /// IMPORTANT:
        /// Does *not* increase the reference count of the given tupleElements.
        /// This constructor should remain private.
        /// </remarks>
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
            this.tupleElementPointers = this.CreateTupleElementPointersCaches();

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

        /// <summary>
        /// Creates a new tuple value that represents either a Q# value of tuple type or one of user defined type,
        /// and contains additional infos used for optimization during QIR generation as well as the LLVM representation of the value.
        /// The casts to get the opaque and typed pointer respectively are executed lazily.
        /// Accessing the opaque or typed pointer will throw an <see cref="InvalidOperationException"/> if the value is stack allocated.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/> is set to false.
        /// </summary>
        internal TupleValue(QsQualifiedName? type, IReadOnlyList<IValue> tupleElements, GenerationContext context, bool allocOnStack, bool registerWithScopeManager)
            : this(type, self => self.AllocateTuple(allocOnStack: allocOnStack), null, tupleElements, context)
        {
            if (registerWithScopeManager)
            {
                this.sharedState.ScopeMgr.RegisterValue(this);
            }

            foreach (var element in tupleElements)
            {
                this.sharedState.ScopeMgr.IncreaseReferenceCount(element);
            }
        }

        /// <inheritdoc cref="TupleValue(QsQualifiedName?, IReadOnlyList{IValue}, GenerationContext, bool, bool)"/>
        internal TupleValue(QsQualifiedName? type, ImmutableArray<TypedExpression> tupleElements, GenerationContext context, bool allocOnStack, bool registerWithScopeManager)
            : this(type, self => self.AllocateTuple(allocOnStack: allocOnStack), null, tupleElements.Select(context.BuildSubitem).ToArray(), context)
        {
            if (registerWithScopeManager)
            {
                this.sharedState.ScopeMgr.RegisterValue(this);
            }
        }

        /// <inheritdoc cref="TupleValue(QsQualifiedName?, IReadOnlyList{IValue}, GenerationContext, bool, bool)"/>
        internal TupleValue(ImmutableArray<ResolvedType> elementTypes, GenerationContext context, bool registerWithScopeManager)
            : this(null, self => self.AllocateTuple(allocOnStack: false), elementTypes, null, context)
        {
            if (registerWithScopeManager)
            {
                this.sharedState.ScopeMgr.RegisterValue(this);
            }
        }

        /// <summary>
        /// Creates a tuple value that represents either a Q# value of tuple type or one of user defined type and
        /// contains the given LLVM value as well as additional infos used for optimization during QIR generation.
        /// Accessing the opaque or typed pointer will throw an <see cref="InvalidOperationException"/> if the value is stack allocated.
        /// </summary>
        internal TupleValue(QsQualifiedName? type, Value tuple, ImmutableArray<ResolvedType> elementTypes, GenerationContext context)
            : this(type, _ => tuple, elementTypes, null, context)
        {
        }

        /// <inheritdoc cref="TupleValue(QsQualifiedName?, Value, ImmutableArray{ResolvedType}, GenerationContext)"/>
        internal TupleValue(Value tuple, ImmutableArray<ResolvedType> elementTypes, GenerationContext context)
        : this(null, tuple, elementTypes, context)
        {
        }

        /// <summary>
        /// Creates an new tuple value that is a copy of the given value.
        /// The new tuple will be allocated on the stack if the value to copy is.
        /// Does *not* increase the reference count of the tuple elements, nor does it register the new value with the scope manager.
        /// </summary>
        internal TupleValue(TupleValue tuple, bool alwaysCopy = false)
        {
            this.sharedState = tuple.sharedState;
            this.TypeName = tuple.TypeName;
            this.ElementTypes = tuple.ElementTypes;
            this.StructType = tuple.StructType;

            var value = tuple.IsRuntimeManaged
                ? tuple.sharedState.CurrentBuilder.Call(
                    tuple.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCopy),
                    tuple.OpaquePointer,
                    tuple.sharedState.Context.CreateConstant(alwaysCopy))
                : tuple.Value;

            this.LlvmNativeValue = tuple.IsRuntimeManaged ? null : value;
            this.opaquePointer = this.CreateOpaquePointerCache(Types.IsTupleOrUnit(value.NativeType) && !value.IsNull ? value : null);
            this.typedPointer = this.CreateTypedPointerCache(Types.IsTypedTuple(value.NativeType) ? value : null);

            this.tupleElementPointers = tuple.tupleElementPointers
                .Select((cache, idx) => this.CreateTupleElementPointerCache(idx, cache.IsCached ? cache.Load() : null)).ToArray();
        }

        /* private helpers */

        private IValue.Cached<Value> CreateOpaquePointerCache(Value? pointer = null) =>
            new(pointer, this.sharedState, () =>
                this.typedPointer.IsCached
                ? this.sharedState.CurrentBuilder.BitCast(this.TypedPointer, this.sharedState.Types.Tuple)
                : throw new InvalidOperationException("tuple pointer is undefined"));

        private IValue.Cached<Value> CreateTypedPointerCache(Value? pointer = null) =>
            new(pointer, this.sharedState, () =>
                this.opaquePointer.IsCached
                ? this.sharedState.CurrentBuilder.BitCast(this.OpaquePointer, this.StructType.CreatePointerType())
                : throw new InvalidOperationException("tuple pointer is undefined"));

        private PointerValue CreateTupleElementPointer(int index, IValue? element = null)
        {
            if (this.IsRuntimeManaged)
            {
                var elementPtr = this.sharedState.CurrentBuilder.GetStructElementPointer(this.StructType, this.TypedPointer, (uint)index);
                return new PointerValue(elementPtr, this.ElementTypes[index], this.LlvmElementTypes[index], this.sharedState);
            }
            else
            {
                void Store(IValue v) =>
                    this.LlvmNativeValue = this.sharedState.CurrentBuilder.InsertValue(this.Value, v.Value, (uint)index);

                IValue Reload() =>
                    this.sharedState.Values.From(this.sharedState.CurrentBuilder.ExtractValue(this.Value, (uint)index), this.ElementTypes[index]);

                return element is null
                    ? new PointerValue(this.ElementTypes[index], this.LlvmElementTypes[index], this.sharedState, Reload, Store)
                    : new PointerValue(element, this.sharedState, Reload, Store);
            }
        }

        private IValue.Cached<PointerValue> CreateTupleElementPointerCache(int index, PointerValue? pointer = null) =>
            new(pointer is null || this.IsRuntimeManaged
                    ? null
                    : this.CreateTupleElementPointer(index, pointer.CurrentCache()),
                this.sharedState,
                () => this.CreateTupleElementPointer(index));

        private IValue.Cached<PointerValue>[] CreateTupleElementPointersCaches() =>
            Enumerable.ToArray(Enumerable.Range(0, this.ElementTypes.Length).Select(index => this.CreateTupleElementPointerCache(index)));

        private Value AllocateTuple(bool allocOnStack) =>
            allocOnStack
            ? this.StructType.GetNullValue()

            // The runtime function TupleCreate creates a new value with reference count 1 and alias count 0.
            : this.sharedState.CurrentBuilder.Call(
                this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate),
                this.sharedState.ComputeSizeForType(this.StructType));

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
        private readonly GenerationContext sharedState;

        private bool IsRuntimeManaged => Types.IsArray(this.LlvmType);

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
            this.IsRuntimeManaged
            ? this.Value
            : throw new InvalidOperationException("cannot get opaque pointer for a constant array allocated on the stack");

        public Value Length => this.length.Load();

        /// <summary>
        /// Creates an array value that represents a Q# value of array type and contains additional infos used
        /// for optimization during QIR generation as well as the LLVM representation of the value.
        /// Accessing the opaque pointer will throw an <see cref="InvalidOperationException"/> if the value is stack allocated.
        /// </summary>
        /// <remarks>
        /// IMPORTANT:
        /// Does *not* increase the reference count of the given <paramref name="arrayElements"/>.
        /// This constructor should remain private.
        /// </remarks>
        private ArrayValue(ResolvedType elementType, uint count, IReadOnlyList<IValue> arrayElements, GenerationContext context, bool allocOnStack)
        {
            this.sharedState = context;
            this.Count = count < arrayElements.Count ? count : (uint)arrayElements.Count;
            this.length = this.CreateLengthCache();
            this.QSharpElementType = elementType;

            if (allocOnStack)
            {
                // If we stack allocate the array we need to rebuild the array elements
                // such that they all have the same (sized) type.
                arrayElements = this.NormalizeArrayElements(elementType, arrayElements);
                var elementTypes = arrayElements.Select(e => e.LlvmType).Distinct();
                this.LlvmElementType = elementTypes.SingleOrDefault() ?? context.Values.DefaultValue(elementType).LlvmType;
                this.Value = CreateNativeValue(this.LlvmElementType, (uint)arrayElements.Count(), this.Count.Value, context);
                this.LlvmType = this.Value.NativeType;
            }
            else
            {
                this.LlvmElementType = context.LlvmTypeFromQsharpType(elementType);
                this.LlvmType = this.sharedState.Types.Array;
                this.Value = this.AllocateArray();
            }

            this.arrayElementPointers = this.CreateArrayElementPointerCaches();
            var itemPointers = this.GetArrayElementPointers();
            for (var i = 0; i < itemPointers.Length; ++i)
            {
                itemPointers[i].StoreValue(arrayElements[i]);
            }
        }

        /// <summary>
        /// Creates an array value that represents a Q# value of array type and contains additional infos used
        /// for optimization during QIR generation as well as the LLVM representation of the value.
        /// Accessing the opaque pointer will throw an <see cref="InvalidOperationException"/> if the value is stack allocated.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/> is set to false.
        /// </summary>
        internal ArrayValue(ResolvedType elementType, ImmutableArray<TypedExpression> arrayElements, GenerationContext context, bool allocOnStack, bool registerWithScopeManager)
            : this(elementType, (uint)arrayElements.Length, arrayElements.Select(context.BuildSubitem).ToArray(), context, allocOnStack: allocOnStack)
        {
            if (registerWithScopeManager)
            {
                this.sharedState.ScopeMgr.RegisterValue(this);
            }
        }

        /// <inheritdoc cref="ArrayValue(ResolvedType, ImmutableArray{TypedExpression}, GenerationContext, bool, bool)"/>
        internal ArrayValue(ResolvedType elementType, IReadOnlyList<IValue> arrayElements, GenerationContext context, bool allocOnStack, bool registerWithScopeManager)
            : this(elementType, (uint)arrayElements.Count, arrayElements, context, allocOnStack: allocOnStack)
        {
            if (registerWithScopeManager)
            {
                this.sharedState.ScopeMgr.RegisterValue(this);
            }

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

        /// <summary>
        /// Creates an new array value that is a copy of the given value.
        /// The new array will be allocated on the stack if the value to copy is.
        /// Does *not* increase the reference count of the array elements, nor does it register the new value with the scope manager.
        /// </summary>
        internal ArrayValue(ArrayValue array, bool alwaysCopy = false)
        {
            this.sharedState = array.sharedState;
            this.Count = array.Count;
            this.length = this.CreateLengthCache(array.length.IsCached ? array.length.Load() : null);
            this.QSharpElementType = array.QSharpElementType;
            this.LlvmType = array.LlvmType;
            this.LlvmElementType = array.LlvmElementType;

            this.Value = array.IsRuntimeManaged
                ? this.sharedState.CurrentBuilder.Call(
                    this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy),
                    array.OpaquePointer,
                    this.sharedState.Context.CreateConstant(alwaysCopy))
                : array.Value;

            this.arrayElementPointers = array.arrayElementPointers?.Select((cache, idx) =>
                this.CreateArrayElementPointerCache(idx, cache.IsCached ? cache.Load() : null)).ToArray();
        }

        /// <summary>
        /// Creates an array value that represents a Q# value of array type and
        /// contains the given LLVM value as well as additional infos used for optimization during QIR generation.
        /// Accessing the opaque pointer will throw an <see cref="InvalidOperationException"/> if the value is stack allocated.
        /// </summary>
        internal ArrayValue(Value value, ResolvedType elementType, uint? count, GenerationContext context)
        {
            this.sharedState = context;
            this.length = this.CreateLengthCache();
            this.QSharpElementType = elementType;
            this.LlvmType = value.NativeType;
            this.Count = count;

            if (this.AsNativeValue(out var constArr, out var length, value: value))
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

            this.arrayElementPointers = this.Count is null ? null : this.CreateArrayElementPointerCaches();
        }

        /// <summary>
        /// Creates an array value of the given length that represents a Q# value of array type and contains
        /// additional infos used for optimization during QIR generation as well as the LLVM representation of the value.
        /// Expects a value of type i64 for the length of the array.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/> is set to false.
        /// </summary>
        internal ArrayValue(ResolvedType elementType, Value length, GenerationContext context, bool registerWithScopeManager)
        {
            this.sharedState = context;
            this.Count = QirValues.AsConstantUInt32(length);
            this.length = this.CreateLengthCache(length);
            this.QSharpElementType = elementType;
            this.LlvmElementType = context.LlvmTypeFromQsharpType(elementType);
            this.LlvmType = this.sharedState.Types.Array;
            this.Value = this.AllocateArray();
            this.arrayElementPointers = this.Count is null ? null : this.CreateArrayElementPointerCaches();

            if (registerWithScopeManager)
            {
                this.sharedState.ScopeMgr.RegisterValue(this);
            }
        }

        /// <inheritdoc cref="ArrayValue(ResolvedType, Value, GenerationContext, bool)"/>
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
            this.AsNativeValue(out var constArr, out var length)
                ? CreateNativeValue(
                    transformConstArr?.Invoke(constArr) ?? constArr,
                    this.Count != null ? this.sharedState.Context.CreateConstant((long)this.Count) : length,
                    this.sharedState)
                : throw new InvalidOperationException("no native llvm represenation available");

        private bool IsNativeValue(Value? value = null) =>
            (value ?? this.Value).NativeType is IStructType st
                && st.Members.Count == 2
                && st.Members[0] is IArrayType
                && st.Members[1] == this.sharedState.Types.Int;

        private bool AsNativeValue([MaybeNullWhen(false)] out Value constArr, [MaybeNullWhen(false)] out Value length, Value? value = null)
        {
            value ??= this.Value;
            if (this.IsNativeValue(value))
            {
                constArr = this.sharedState.CurrentBuilder.ExtractValue(value, 0u);
                length = this.sharedState.CurrentBuilder.ExtractValue(value, 1u);
                return true;
            }

            (constArr, length) = (null, null);
            return false;
        }

        private IValue.Cached<Value> CreateLengthCache(Value? length = null) =>
            new(length, this.sharedState, () =>
                this.Count is uint count ? this.sharedState.Context.CreateConstant((long)count)
                : this.AsNativeValue(out var _, out var length) ? length
                : this.sharedState.CurrentBuilder.Call(
                    this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetSize1d),
                    this.OpaquePointer));

        private IValue.Cached<PointerValue> CreateArrayElementPointerCache(int index, PointerValue? pointer = null) =>
            new(pointer is null || Types.IsArray(this.LlvmType)
                    ? null
                    : this.CreateArrayElementPointer(index, pointer.CurrentCache()),
                this.sharedState,
                () => this.CreateArrayElementPointer(index));

        private IValue.Cached<PointerValue>[] CreateArrayElementPointerCaches() =>
            Enumerable.ToArray(Enumerable.Range(0, (int)this.Count!).Select(idx => this.CreateArrayElementPointerCache(idx)));

        private Value AllocateArray() =>

            // The runtime function ArrayCreate1d creates a new value with reference count 1 and alias count 0.
            this.sharedState.CurrentBuilder.Call(
                this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCreate1d),
                this.sharedState.ComputeSizeForType(this.LlvmElementType, this.sharedState.Context.Int32Type),
                this.Length);

        private IReadOnlyList<IValue> NormalizeArrayElements(ResolvedType eltype, IReadOnlyList<IValue> elements)
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
                    var rebuiltElements = this.NormalizeArrayElements(elementType(innerItemIdx), innerElements);
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
                    a.AsNativeValue(out var constArr, out var _) && constArr.NativeType is IArrayType iat ? iat.Length : 0u);
                var innerArrSize = Enumerable.Prepend(sizes, 0u).Max();

                IValue GetInnerElements(int idx, ArrayValue array) =>
                    idx < array.Count
                    ? array.GetArrayElement(idx)
                    : this.sharedState.Values.From(array.LlvmElementType.GetNullValue(), array.QSharpElementType);

                return NormalizeItems(innerArrSize, arrs, GetInnerElements, _ => it.Item, (idx, newElements) =>
                    arrs[idx].Count is uint count && !arrs[idx].IsRuntimeManaged

                    // elements are processed as part of the scope management of the containing array -
                    // no need to register them separately
                    ? new ArrayValue(it.Item, count, newElements, this.sharedState, allocOnStack: true)
                    : throw new InvalidOperationException("cannot resize array of unknown length to match the size of a new item"));
            }
            else if (eltype.Resolution is QsResolvedTypeKind.TupleType ts)
            {
                var tuples = elements.Select(e => (TupleValue)e).ToArray();
                return NormalizeItems(
                    (uint)ts.Item.Length, tuples, (idx, tuple) => tuple.GetTupleElement(idx), idx => ts.Item[idx], (idx, newElements) =>

                    // elements are processed as part of the scope management of the containing array -
                    // no need to register them separately
                    new TupleValue(null, newElements, this.sharedState, allocOnStack: true, registerWithScopeManager: false));
            }
            else if (eltype.Resolution is QsResolvedTypeKind.UserDefinedType udt)
            {
                var uts = this.sharedState.GetItemTypes(udt.Item.GetFullName());
                var tuples = elements.Select(e => (TupleValue)e).ToArray();
                return NormalizeItems(
                    (uint)uts.Length, tuples, (idx, tuple) => tuple.GetTupleElement(idx), idx => uts[idx], (idx, newElements) =>

                    // elements are processed as part of the scope management of the containing array -
                    // no need to register them separately
                    new TupleValue(udt.Item.GetFullName(), newElements, this.sharedState, allocOnStack: true, registerWithScopeManager: false));
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
                var newElements = this.NormalizeArrayElements(this.QSharpElementType, currentElements);
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
            if (this.IsRuntimeManaged)
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
                    this.AsNativeValue(out var constArr, out var _) && constIndex < ((IArrayType)constArr.NativeType).Length
                    ? this.sharedState.Values.From(
                        this.sharedState.CurrentBuilder.ExtractValue(constArr, constIndex),
                        this.QSharpElementType)
                    : this.sharedState.Values.From(Constant.PoisonValueFor(this.LlvmElementType), this.QSharpElementType);

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
                    this.AsNativeValue(out var constArr, out var _);
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
            ? (idx < this.arrayElementPointers.Length ? this.arrayElementPointers[idx].Load() : this.CreatePointerForPoison())
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
        /// <exception cref="InvalidOperationException">
        /// The array contains an unknown number of elements and no indices have been specified.
        /// </exception>
        internal PointerValue[] GetArrayElementPointers(IEnumerable<int>? indices = null)
        {
            var enumerable = indices
                ?? (this.Count != null && this.Count <= int.MaxValue ? Enumerable.Range(0, (int)this.Count)
                : throw new InvalidOperationException("cannot get all element pointers for array of unknown length"));

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
        private class CreationItems
        {
            internal GlobalVariable FunctionTable { get; }

            internal Constant MemoryManagementTable { get; }

            internal Value Capture { get; }

            internal bool RequireClosure { get; }

            internal CreationItems(GlobalVariable functionTable, GenerationContext context, TupleValue? capture = null)
            {
                this.FunctionTable = functionTable;
                this.MemoryManagementTable = context.GetOrCreateCallableMemoryManagementTable(capture);
                this.Capture = capture?.OpaquePointer ?? context.Values.Unit.Value;
                this.RequireClosure = capture is not null;
            }
        }

        internal class CallableState
        {
            public QsQualifiedName GlobalName { get; }

            public QsSpecializationKind RelevantSpecialization { get; }

            private CallableState(QsQualifiedName name, QsSpecializationKind? relevantSpec = null)
            {
                this.GlobalName = name;
                this.RelevantSpecialization = relevantSpec ?? QsSpecializationKind.QsBody;
            }

            internal static CallableState Create(QsQualifiedName name) => new(name);

            private CallableState WithRelevantSpec(QsSpecializationKind relevantSpec) => new(this.GlobalName, relevantSpec);

            private static QsSpecializationKind ApplyAdjoint(QsSpecializationKind spec) =>
                spec.IsQsBody ? QsSpecializationKind.QsAdjoint :
                spec.IsQsAdjoint ? QsSpecializationKind.QsBody :
                spec.IsQsControlled ? QsSpecializationKind.QsControlledAdjoint :
                spec.IsQsControlledAdjoint ? QsSpecializationKind.QsControlled :
                throw new NotImplementedException("Unknown specialization");

            private static QsSpecializationKind ApplyControlled(QsSpecializationKind spec) =>
                spec.IsQsBody || spec.IsQsControlled ? QsSpecializationKind.QsControlled :
                spec.IsQsAdjoint || spec.IsQsControlledAdjoint ? QsSpecializationKind.QsControlledAdjoint :
                throw new NotImplementedException("Unknown specialization");

            internal CallableState ApplyFunctor(QsFunctor functor) =>
                this.WithRelevantSpec(
                    functor.IsAdjoint ? ApplyAdjoint(this.RelevantSpecialization) :
                    functor.IsControlled ? ApplyControlled(this.RelevantSpecialization) :
                    throw new NotImplementedException("unkown functor"));
        }

        private readonly GenerationContext sharedState;

        // We use a cache to enable lazy creation of callable values when the capture is null.
        private readonly IValue.Cached<Value> createdCallable;
        private readonly CreationItems? creationItems;

        // If the callable is globally defined then we can apply the functors during QIR generation, i.e. we
        // track applied functors by updating the CurrentState property, and apply the appropriate functors
        // only when needed; when accessing the Value property, functors are automatically applied by invoking
        // the corresponding runtime function(s). This permits to omit calls to runtime functions entirely,
        // if the callable value is fully trackable during QIR generation; before emiting a call instruction,
        // we check if the name of the callable is known, and if it is we merely select the correct specialization
        // and never even access the Value property.
        internal CallableState? CurrentState { get; private set; } // FIXME: GET RID OF SETTER HERE

        public Value Value
        {
            get
            {
                var callable = this.createdCallable.Load();
                var specKind = this.CurrentState?.RelevantSpecialization ?? QsSpecializationKind.QsBody;

                if (specKind.IsQsAdjoint || specKind.IsQsControlledAdjoint)
                {
                    // does *not* create a new value but instead modifies the given callable in place.
                    var applyAdjoint = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableMakeAdjoint);
                    this.sharedState.CurrentBuilder.Call(applyAdjoint, callable);
                }

                if (specKind.IsQsControlled || specKind.IsQsControlledAdjoint)
                {
                    // does *not* create a new value but instead modifies the given callable in place.
                    var applyControlled = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableMakeControlled);
                    this.sharedState.CurrentBuilder.Call(applyControlled, callable);
                }

                // If we updated (i.e. did an in-place modification of) the value of the orginally created callable,
                // the stored callable name and relevant specialization no longer apply.
                this.CurrentState = specKind.IsQsBody ? this.CurrentState : null;
                return callable;
            }
        }

        /// <inheritdoc cref="IValue.LlvmType" />
        public ITypeRef LlvmType { get; }

        public ResolvedType QSharpType { get; }

        /// <summary>
        /// Instantiates a new callable value of the given type.
        /// </summary>
        /// <param name="value">The pointer to a QIR callable value, if the value has already been created.</param>
        /// <param name="callableType">Q# type of the callable.</param>
        /// <param name="creationItems">The items required for creating the callable value; if the callable captures values, the capture needs to be in scope.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
        private CallableValue(Value? value, ResolvedType callableType, CreationItems? creationItems, GenerationContext context)
        {
            this.sharedState = context;
            this.QSharpType = callableType;
            this.LlvmType = context.Types.Callable;
            this.creationItems = creationItems;
            this.createdCallable = this.CreateCallableCache(value);
        }

        /// <summary>
        /// Creates a callable value of the given type and registers it with the scope manager.
        /// The necessary functions to invoke the callable are defined by the callable table;
        /// i.e. the globally defined array of function pointers accessible via the given global variable.
        /// </summary>
        /// <param name="globalName">The Q# name of the callable, if the callable is globally defined.</param>
        /// <param name="callableType">The Q# type of the callable value.</param>
        /// <param name="functionTable">The global variable that contains the array of function pointers defining the callable.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
        /// <param name="captured">All captured values.</param>
        private CallableValue(QsQualifiedName? globalName, ResolvedType callableType, GlobalVariable functionTable, GenerationContext context, ImmutableArray<TypedExpression>? captured)
        : this(
            null,
            callableType,
            new CreationItems(functionTable, context, captured is not null && captured.Value.Length > 0
                ? context.Values.CreateTuple(captured.Value, allocOnStack: false, registerWithScopeManager: false)
                : null),
            context)
        {
            this.CurrentState = globalName is null ? null : CallableState.Create(globalName);
        }

        /// <summary>
        /// Creates a callable value of the given type and registers it with the scope manager.
        /// The necessary functions to invoke the callable are defined by the callable table;
        /// i.e. the globally defined array of function pointers accessible via the given global variable.
        /// </summary>
        /// <param name="globalName">The Q# name of the callable, if the callable is globally defined.</param>
        /// <param name="callableType">The Q# type of the callable value.</param>
        /// <param name="functionTable">The global variable that contains the array of function pointers defining the callable.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
        internal CallableValue(QsQualifiedName globalName, ResolvedType callableType, GlobalVariable functionTable, GenerationContext context)
        : this(globalName, callableType, functionTable, context, null)
        {
        }

        /// <summary>
        /// Creates a callable value of the given type and registers it with the scope manager.
        /// The necessary functions to invoke the callable are defined by the callable table;
        /// i.e. the globally defined array of function pointers accessible via the given global variable.
        /// </summary>
        /// <param name="callableType">The Q# type of the callable value.</param>
        /// <param name="functionTable">The global variable that contains the array of function pointers defining the callable.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
        /// <param name="captured">All captured values.</param>
        internal CallableValue(ResolvedType callableType, GlobalVariable functionTable, GenerationContext context, ImmutableArray<TypedExpression> captured)
        : this(null, callableType, functionTable, context, captured)
        {
        }

        /// <summary>
        /// Creates a new callable value of the given type.
        /// </summary>
        /// <param name="value">The pointer to a QIR callable value.</param>
        /// <param name="callableType">Q# type of the callable.</param>
        /// <param name="context">Generation context where constants are defined and generated if needed.</param>
        internal CallableValue(Value value, ResolvedType callableType, GenerationContext context)
        : this(value, callableType, null, context)
        {
        }

        /* private helpers */

        private IValue.Cached<Value> CreateCallableCache(Value? value = null)
        {
            Value CreateCallable(CreationItems items)
            {
                // The runtime function CallableCreate creates a new value with reference count 1.
                var createCallable = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
                var callable = this.sharedState.CurrentBuilder.Call(createCallable, items.FunctionTable, items.MemoryManagementTable, items.Capture);
                this.sharedState.ScopeMgr.RegisterValue(this);
                return callable;
            }

            // If the callable requires creating a closure, and we invoke the runtime method CallableCreate to build that closure.
            // If there are no values to capture (i.e. there is no need to build a closure), we can create the callable lazily instead.
            value ??= this.creationItems is not null && this.creationItems.RequireClosure ? CreateCallable(this.creationItems) : null;
            return value is not null || this.creationItems is not null
                ? new IValue.Cached<Value>(value, this.sharedState, () => value is not null ? value : CreateCallable(this.creationItems!))
                : throw new InvalidOperationException("a cache value needs to be defined when no creation items have been defined");
        }

        // public helpers

        /// <summary>
        /// Invokes the runtime function with the given name to apply a functor to a callable value.
        /// Unless modifyInPlace is set to true, a copy of the callable is made prior to applying the functor.
        /// Reference counts for the callable and the capture are updated as needed, and if a new value is created,
        /// it is registered with the scope manager.
        /// </summary>
        /// <param name="functor">The functor to apply.</param>
        /// <param name="modifyInPlace">If set to true, modifies and returns the given callable</param>
        /// <returns>The callable value to which the functor has been applied</returns>
        public CallableValue ApplyFunctor(QsFunctor functor, bool modifyInPlace)
        {
            // This method is used when applying functors when building a functor application expression
            // as well as when creating the specializations for a partial application.
            var callable = this;
            if (!modifyInPlace)
            {
                // Even though functors are applied lazily when possible, accessing the callable value will trigger
                // functor application by applying the corresponding runtime function. Since the runtime function
                // will modify the callable in place, we need to make a copy unless an in-place modification has
                // been requested. Since we track alias counts for callables there is no need to force the copy.
                var makeCopy = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCopy);
                var forceCopy = this.sharedState.Context.CreateConstant(false);
                var value = this.sharedState.CurrentBuilder.Call(makeCopy, this.Value, forceCopy);
                callable = new CallableValue(value, this.QSharpType, this.creationItems, this.sharedState);

                // While making a copy ensures that either a new callable is created with reference count 1,
                // or the reference count of the existing callable is increased by 1,
                // we also need to increase the reference counts for all contained items, i.e. for the capture tuple,
                // and register the new value with the scope manager.
                this.sharedState.ScopeMgr.ReferenceCaptureTuple(callable);
                this.sharedState.ScopeMgr.RegisterValue(callable);
            }

            // We apply functors lazily; they only need to be applied when we are actually accessing the callable value.
            // Rather than invoking the runtime functions when a functor is applied, we hence merely keep track of
            // what functors applications are "pending". Accessing the Value triggers applications of the functors.
            callable.CurrentState = this.CurrentState?.ApplyFunctor(functor);
            if (callable.CurrentState is null)
            {
                // Given that we are not tracking the state of the callable, e.g. because the callable
                // requires a closure, we invoke the corresponding runtime function to apply the functor.
                var runtimeFunctionName =
                    functor.IsAdjoint ? RuntimeLibrary.CallableMakeAdjoint :
                    functor.IsControlled ? RuntimeLibrary.CallableMakeControlled :
                    throw new ArgumentException("unknown functor");
                var applyFunctor = this.sharedState.GetOrCreateRuntimeFunction(runtimeFunctionName);
                this.sharedState.CurrentBuilder.Call(applyFunctor, callable.Value);
            }

            return callable;
        }
    }
}
