// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR.Emission
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// Each class instance contains permits to construct QIR values for use
    /// within the compilation unit given upon instantiation.
    /// </summary>
    internal class QirValues
    {
        private readonly GenerationContext sharedState;

        internal readonly IValue Unit;

        internal QirValues(GenerationContext context, Constants constants)
        {
            this.sharedState = context;
            this.Unit = new SimpleValue(constants.UnitValue, ResolvedType.New(QsResolvedTypeKind.UnitType));
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
        /// <param name="udt">The Q# type of the value</param>
        internal TupleValue FromCustomType(Value value, UserDefinedType udt)
        {
            if (!this.sharedState.TryGetCustomType(udt.GetFullName(), out var udtDecl))
            {
                throw new ArgumentException("type declaration not found");
            }

            var elementTypes = udtDecl.Type.Resolution is QsResolvedTypeKind.TupleType ts ? ts.Item : ImmutableArray.Create(udtDecl.Type);
            return new TupleValue(udt, value, elementTypes, this.sharedState);
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
            new ArrayValue(value, null, elementType, this.sharedState);

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
            type.Resolution is QsResolvedTypeKind.ArrayType it ? this.sharedState.Values.FromArray(value, it.Item) :
            type.Resolution is QsResolvedTypeKind.TupleType ts ? this.sharedState.Values.FromTuple(value, ts.Item) :
            type.Resolution is QsResolvedTypeKind.UserDefinedType udt ? this.sharedState.Values.FromCustomType(value, udt.Item) :
            (type.Resolution.IsOperation || type.Resolution.IsFunction) ? this.sharedState.Values.FromCallable(value, type) :
            (IValue)new SimpleValue(value, type);

        /// <summary>
        /// Creates a pointer to the given value.
        /// When needed, instructions are emitted using the current builder.
        /// </summary>
        /// <param name="type">The Q# type of the value that the pointer points to</param>
        internal PointerValue CreatePointer(IValue value) =>
            new PointerValue(value, this.sharedState);

        /// <summary>
        /// Creates a new tuple value. The allocation of the value via invokation of the corresponding runtime function
        /// is lazy, and so are the necessary casts. When needed, instructions are emitted using the current builder.
        /// Registers the value with the scope manager.
        /// </summary>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        internal TupleValue CreateTuple(ImmutableArray<ResolvedType> elementTypes) =>
            new TupleValue(elementTypes, this.sharedState);

        /// <summary>
        /// Builds a tuple with the items set to the given tuple elements.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="tupleElements">The tuple elements</param>
        internal TupleValue CreateTuple(ImmutableArray<TypedExpression> tupleElements, bool registerWithScopeManager = true)
        {
            var elementTypes = tupleElements.Select(v => v.ResolvedType).ToImmutableArray();
            TupleValue tuple = new TupleValue(elementTypes, this.sharedState, registerWithScopeManager);
            PointerValue[] itemPointers = tuple.GetTupleElementPointers();

            var elements = tupleElements.Select(this.sharedState.BuildSubitem).ToArray();
            for (var i = 0; i < itemPointers.Length; ++i)
            {
                itemPointers[i].StoreValue(elements[i]);
            }

            return tuple;
        }

        /// <summary>
        /// Builds a tuple with the items set to the given tuple elements.
        /// Registers the value with the scope manager.
        /// Increases the reference count for the tuple elements.
        /// </summary>
        /// <param name="tupleElements">The tuple elements</param>
        internal TupleValue CreateTuple(params IValue[] tupleElements)
        {
            var elementTypes = tupleElements.Select(v => v.QSharpType).ToImmutableArray();
            TupleValue tuple = new TupleValue(elementTypes, this.sharedState);
            PointerValue[] itemPointers = tuple.GetTupleElementPointers();

            for (var i = 0; i < itemPointers.Length; ++i)
            {
                itemPointers[i].StoreValue(tupleElements[i]);
                this.sharedState.ScopeMgr.IncreaseReferenceCount(tupleElements[i]);
            }

            return tuple;
        }

        /// <summary>
        /// Creates a new array value of the given length. Expects a value of type i64 for the length of the array.
        /// Registers the value with the scope manager.
        /// </summary>
        /// <param name="length">Value of type i64 indicating the number of elements in the array</param>
        /// <param name="elementType">Q# type of the array elements</param>
        internal ArrayValue CreateArray(Value length, ResolvedType elementType) =>
            new ArrayValue(length, elementType, this.sharedState);

        /// <summary>
        /// Builds an array that contains the given array elements.
        /// Registers the value with the scope manager, unless registerWithScopeManager is set to false.
        /// </summary>
        /// <param name="arrayElements">The elements in the array</param>
        internal ArrayValue CreateArray(ResolvedType elementType, ImmutableArray<TypedExpression> arrayElements, bool registerWithScopeManager = true)
        {
            var array = new ArrayValue((uint)arrayElements.Length, elementType, this.sharedState, registerWithScopeManager);
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
        /// Registers the value with the scope manager.
        /// Increases the reference count for the array elements.
        /// </summary>
        /// <param name="arrayElements">The elements in the array</param>
        internal ArrayValue CreateArray(ResolvedType elementType, params IValue[] arrayElements)
        {
            var array = new ArrayValue((uint)arrayElements.Length, elementType, this.sharedState);
            var itemPointers = array.GetArrayElementPointers();

            for (var i = 0; i < itemPointers.Length; ++i)
            {
                itemPointers[i].StoreValue(arrayElements[i]);
                this.sharedState.ScopeMgr.IncreaseReferenceCount(arrayElements[i]);
            }

            return array;
        }
    }
}
