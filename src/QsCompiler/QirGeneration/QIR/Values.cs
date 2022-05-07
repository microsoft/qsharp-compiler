// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using LlvmBindings.Values;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QIR.Emission
{
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// Each class instance contains permits to construct QIR values for use
    /// within the compilation unit given upon instantiation.
    /// </summary>
    internal class QirValues
    {
        private readonly GenerationContext sharedState;

        internal IValue Unit { get; }

        internal QirValues(GenerationContext context, Constants constants)
        {
            this.sharedState = context;
            this.Unit = new SimpleValue(constants.UnitValue, ResolvedType.New(ResolvedTypeKind.UnitType));
        }

        /// <summary>
        /// Creates a simple value that stores the given LLVM value as well as its Q# type.
        /// </summary>
        /// <param name="value">The LLVM value to store</param>
        /// <param name="type">The Q# of the value</param>
        internal SimpleValue FromSimpleValue(Value value, ResolvedType type) =>
            new SimpleValue(value, type);

        /// <summary>
        /// Creates a tuple value that stores the given LLVM value representing a Q# value of user defined type.
        /// </summary>
        /// <param name="value">The typed tuple representing a value of user defined type</param>
        /// <param name="typeName">The Q# type name of the value</param>
        internal TupleValue FromCustomType(Value value, QsQualifiedName typeName)
        {
            if (!this.sharedState.TryGetCustomType(typeName, out var udtDecl))
            {
                throw new ArgumentException("type declaration not found");
            }

            var elementTypes = udtDecl.Type.Resolution is ResolvedTypeKind.TupleType ts ? ts.Item : ImmutableArray.Create(udtDecl.Type);
            return new TupleValue(typeName, value, elementTypes, this.sharedState);
        }

        /// <summary>
        /// Creates a new tuple value from the given tuple pointer. The casts to get the opaque and typed pointer
        /// respectively are executed lazily. When needed, instructions are emitted using the current builder.
        /// </summary>
        /// <param name="tuple">Either an opaque or a typed pointer to the tuple data structure</param>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        internal TupleValue FromTuple(Value tuple, ImmutableArray<ResolvedType> elementTypes) =>
            new TupleValue(tuple, elementTypes, this.sharedState);

        /// <summary>
        /// Creates a new array value from the given opaque array of elements of the given type.
        /// </summary>
        /// <param name="elementType">Q# type of the array elements</param>
        internal ArrayValue FromArray(Value value, ResolvedType elementType) =>
            new ArrayValue(value, elementType, this.sharedState);

        /// <summary>
        /// Creates a callable value that stores the given LLVM value representing a Q# callable.
        /// </summary>
        /// <param name="value">The LLVM value to store</param>
        /// <param name="type">The Q# of the value</param>
        internal CallableValue FromCallable(Value value, ResolvedType type) =>
            new CallableValue(value, type, this.sharedState);

        /// <summary>
        /// Creates a suitable class to pass around a built LLVM value that represents a Q# value of the given type.
        /// </summary>
        /// <param name="value">The LLVM value to store</param>
        /// <param name="type">The Q# of the value</param>
        internal IValue From(Value value, ResolvedType type) =>
            type.Resolution is ResolvedTypeKind.ArrayType it ? this.sharedState.Values.FromArray(value, it.Item) :
            type.Resolution is ResolvedTypeKind.TupleType ts ? this.sharedState.Values.FromTuple(value, ts.Item) :
            type.Resolution is ResolvedTypeKind.UserDefinedType udt ? this.sharedState.Values.FromCustomType(value, udt.Item.GetFullName()) :
            (type.Resolution.IsOperation || type.Resolution.IsFunction) ? this.sharedState.Values.FromCallable(value, type) :
            (IValue)new SimpleValue(value, type);

        /// <summary>
        /// Creates a pointer to the given value.
        /// When needed, instructions are emitted using the current builder.
        /// </summary>
        /// <param name="value">The value that the pointer points to</param>
        internal PointerValue CreatePointer(IValue value) =>
            new PointerValue(value, this.sharedState);

        /// <summary>
        /// Creates a new tuple value. The allocation of the value via invokation of the corresponding runtime function
        /// is lazy, and so are the necessary casts. When needed, instructions are emitted using the current builder.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        internal TupleValue CreateTuple(ImmutableArray<ResolvedType> elementTypes, bool registerWithScopeManager) =>
            new TupleValue(elementTypes, this.sharedState, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Builds a tuple with the items set to the given tuple elements.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="tupleElements">The tuple elements</param>
        private TupleValue CreateTuple(QsQualifiedName? typeName, ImmutableArray<TypedExpression> tupleElements, bool allocOnStack, bool registerWithScopeManager)
        {
            var elements = tupleElements.Select(this.sharedState.BuildSubitem).ToArray();
            var elementTypes = tupleElements.Select(v => v.ResolvedType).ToImmutableArray();

            TupleValue tuple = new TupleValue(typeName, elementTypes, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);
            PointerValue[] itemPointers = tuple.GetTupleElementPointers();

            for (var i = 0; i < itemPointers.Length; ++i)
            {
                itemPointers[i].StoreValue(elements[i]);
            }

            return tuple;
        }

        /// <summary>
        /// Builds a tuple with the items set to the given tuple elements.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="tupleElements">The tuple elements</param>
        internal TupleValue CreateTuple(ImmutableArray<TypedExpression> tupleElements, bool allocOnStack, bool registerWithScopeManager) =>
            this.CreateTuple(null, tupleElements, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Builds a tuple with the items set to the given tuple elements.
        /// The tuple represents a value of user defined type if a name is specified.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// Increases the reference count for the tuple elements.
        /// </summary>
        /// <param name="typeName">The name of the user defined typed that the tuple represents</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built tuple with the scope manager</param>
        /// <param name="tupleElements">The tuple elements</param>
        private TupleValue CreateTuple(QsQualifiedName? typeName, bool allocOnStack, bool registerWithScopeManager, params IValue[] tupleElements)
        {
            var elementTypes = tupleElements.Select(v => v.QSharpType).ToImmutableArray();
            TupleValue tuple = new TupleValue(typeName, elementTypes, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);
            PointerValue[] itemPointers = tuple.GetTupleElementPointers();

            for (var i = 0; i < itemPointers.Length; ++i)
            {
                itemPointers[i].StoreValue(tupleElements[i]);
                this.sharedState.ScopeMgr.IncreaseReferenceCount(tupleElements[i]);
            }

            return tuple;
        }

        /// <summary>
        /// Builds a tuple with the items set to the given tuple elements.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// Increases the reference count for the tuple elements.
        /// </summary>
        /// <param name="registerWithScopeManager">Whether or not to register the built tuple with the scope manager</param>
        /// <param name="tupleElements">The tuple elements</param>
        internal TupleValue CreateTuple(bool allocOnStack, bool registerWithScopeManager, params IValue[] tupleElements) =>
            this.CreateTuple(null, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager, tupleElements);

        /// <summary>
        /// Builds a tuple with the items set to the given tuple elements.
        /// Registers the value with the scope manager.
        /// Increases the reference count for the tuple elements.
        /// </summary>
        /// <param name="tupleElements">The tuple elements</param>
        internal TupleValue CreateTuple(bool allocOnStack, params IValue[] tupleElements) =>
            // even if the tuple is stack allocated, its items may not be and the tuple is hence registerd with the scope manager
            // FIXME: ENSURE THAT TUPLE ITEMS ARE STILL PROPERLY MANAGED WHEN TUPLE IS NOT MANAGED BY THE SCOPE MANAGER...
            // SAME FOR ARRAYS...
            this.CreateTuple(null, allocOnStack: allocOnStack, true, tupleElements);

        /// <summary>
        /// Builds a tuple representing a Q# value of user defined type with the items set to the given elements.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// Increases the reference count for the tuple elements.
        /// </summary>
        /// <param name="typeName">The name of the user defined type</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built tuple with the scope manager</param>
        /// <param name="tupleElements">The tuple elements</param>
        internal TupleValue CreateCustomType(QsQualifiedName typeName, bool allocOnStack, bool registerWithScopeManager, params IValue[] tupleElements) =>
            this.CreateTuple(typeName, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager, tupleElements);

        /// <summary>
        /// Builds a tuple representing a Q# value of user defined type with the items set to the given elements.
        /// Registers the value with the scope manager.
        /// Increases the reference count for the tuple elements.
        /// </summary>
        /// <param name="typeName">The name of the user defined type</param>
        /// <param name="tupleElements">The tuple elements</param>
        internal TupleValue CreateCustomType(QsQualifiedName typeName, bool allocOnStack, params IValue[] tupleElements) =>
            this.CreateTuple(typeName, allocOnStack: allocOnStack, registerWithScopeManager: true, tupleElements);

        /// <summary>
        /// Builds a tuple with the items set to the given tuple elements.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="tupleElements">The tuple elements</param>
        internal TupleValue CreateCustomType(QsQualifiedName typeName, ImmutableArray<TypedExpression> tupleElements, bool allocOnStack, bool registerWithScopeManager) =>
            this.CreateTuple(typeName, tupleElements, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Creates a new array value of the given length. Expects a value of type i64 for the length of the array.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="length">Value of type i64 indicating the number of elements in the array</param>
        /// <param name="elementType">Q# type of the array elements</param>
        internal ArrayValue CreateArray(Value length, ResolvedType elementType, bool allocOnStack, bool registerWithScopeManager) =>
            new ArrayValue(length, elementType, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Builds an array that contains the given array elements.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="arrayElements">The elements in the array</param>
        internal ArrayValue CreateArray(ResolvedType elementType, ImmutableArray<TypedExpression> arrayElements, bool allocOnStack, bool registerWithScopeManager)
        {
            var array = new ArrayValue((uint)arrayElements.Length, elementType, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);
            var itemPointers = array.GetArrayElementPointers();

            var elements = arrayElements.Select(this.sharedState.BuildSubitem).ToArray();
            for (var i = 0; i < itemPointers.Length; ++i)
            {
                itemPointers[i].StoreValue(elements[i]);
            }

            return array;
        }

        /// <summary>
        /// Builds an array that containsthe given array elements.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// Increases the reference count for the array elements.
        /// </summary>
        /// <param name="elementType">The Q# type of the array elements</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built tuple with the scope manager</param>
        /// <param name="arrayElements">The elements in the array</param>
        internal ArrayValue CreateArray(ResolvedType elementType, bool allocOnStack, bool registerWithScopeManager, params IValue[] arrayElements)
        {
            var array = new ArrayValue((uint)arrayElements.Length, elementType, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);
            var itemPointers = array.GetArrayElementPointers();

            for (var i = 0; i < itemPointers.Length; ++i)
            {
                itemPointers[i].StoreValue(arrayElements[i]);
                this.sharedState.ScopeMgr.IncreaseReferenceCount(arrayElements[i]);
            }

            return array;
        }

        /// <summary>
        /// Builds an array that contains the given array elements.
        /// Registers the value with the scope manager.
        /// Increases the reference count for the array elements.
        /// </summary>
        /// <param name="arrayElements">The elements in the array</param>
        internal ArrayValue CreateArray(ResolvedType elementType, bool allocOnStack, params IValue[] arrayElements) =>
            this.CreateArray(elementType, allocOnStack: allocOnStack, registerWithScopeManager: true, arrayElements);

        /// <summary>
        /// Creates an array of the given size and populates each element with the value returned by <paramref name="getElement"/>,
        /// increasing its reference count accordingly. The function <paramref name="getElement"/> is invoked with the respective index.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="elementType">The Q# type of the array elements</param>
        /// <param name="length">Value of type i64 indicating the number of elements in the array</param>
        /// <param name="getElement">Given an index into the array, returns the value to populate that element with</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built tuple with the scope manager</param>
        internal ArrayValue CreateArray(ResolvedType elementType, Value length, Func<Value, IValue> getElement, bool allocOnStack, bool registerWithScopeManager)
        {
            var array = new ArrayValue(length, elementType, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);
            if (array.Count != 0)
            {
                // We need to populate the array
                var start = this.sharedState.Context.CreateConstant(0L);
                var end = array.Count != null
                    ? this.sharedState.Context.CreateConstant((long)array.Count - 1L)
                    : this.sharedState.CurrentBuilder.Sub(array.Length, this.sharedState.Context.CreateConstant(1L));
                this.sharedState.IterateThroughRange(start, null, end, index =>
                {
                    // We need to make sure that the reference count for the item is increased by 1,
                    // and the iteration loop expects that the body handles its own reference counting.
                    this.sharedState.ScopeMgr.OpenScope();
                    var itemValue = getElement(index);
                    array.GetArrayElementPointer(index).StoreValue(itemValue);
                    this.sharedState.ScopeMgr.CloseScope(itemValue);
                });
            }

            return array;
        }

        /// <summary>
        /// Creates a callable value of the given type and registers it with the scope manager.
        /// The necessary functions to invoke the callable are defined by the callable table;
        /// i.e. the globally defined array of function pointers accessible via the given global variable.
        /// </summary>
        /// <param name="callableType">The Q# type of the callable value.</param>
        /// <param name="table">The global variable that contains the array of function pointers defining the callable.</param>
        /// <param name="captured">All captured values.</param>
        internal CallableValue CreateCallable(ResolvedType callableType, GlobalVariable table, ImmutableArray<TypedExpression>? captured = null) =>
            new CallableValue(callableType, table, this.sharedState, captured);
    }
}
