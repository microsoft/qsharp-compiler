// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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

        internal QirValues(GenerationContext context)
        {
            this.sharedState = context;
            this.Unit = new SimpleValue(context.Types.Tuple.GetNullValue(), ResolvedType.New(ResolvedTypeKind.UnitType));
        }

        /// <returns>
        /// An unsigned integer if the given value is a constant that is in the range [0, int.MaxValue], and null otherwise.
        /// </returns>
        public static uint? AsConstantUInt32(Value? value) =>

            // Todo: it would be nice if we could also check that the value is larger than 0
            // rather than blindly treat it as unsigned and zero extend.
            value is ConstantInt count && count.ZeroExtendedValue <= int.MaxValue
            ? (uint?)count.ZeroExtendedValue
            : null;

        /// <returns>
        /// A signed integer if the given value is a constant that is in the range [int.MinVaue, int.MaxValue], and null otherwise.
        /// </returns>
        public static int? AsConstantInt32(Value? value) =>
            value is ConstantInt count && count.SignExtendedValue >= int.MinValue && count.SignExtendedValue <= int.MaxValue
            ? (int?)count.SignExtendedValue
            : null;

        /// <returns>A default value to represent an uninitialized Q# value of the given type.</returns>
        internal IValue DefaultValue(ResolvedType type)
        {
            if (type.Resolution.IsInt)
            {
                var value = this.sharedState.Context.CreateConstant(0L);
                return this.sharedState.Values.FromSimpleValue(value, type);
            }
            else if (type.Resolution.IsDouble)
            {
                var value = this.sharedState.Context.CreateConstant(0.0);
                return this.sharedState.Values.FromSimpleValue(value, type);
            }
            else if (type.Resolution.IsBool)
            {
                var value = this.sharedState.Context.CreateConstant(false);
                return this.sharedState.Values.FromSimpleValue(value, type);
            }
            else if (type.Resolution.IsPauli)
            {
                return this.CreatePauli(QsPauli.PauliI);
            }
            else if (type.Resolution.IsResult)
            {
                var getZero = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultGetZero);
                var constant = this.sharedState.CurrentBuilder.Call(getZero);
                return this.sharedState.Values.From(constant, type);
            }
            else if (type.Resolution.IsQubit)
            {
                var value = Constant.ConstPointerToNullFor(this.sharedState.Types.Qubit);
                return this.sharedState.Values.From(value, type);
            }
            else if (type.Resolution.IsRange)
            {
                return this.sharedState.CreateRange(
                    this.sharedState.Context.CreateConstant(0L),
                    this.sharedState.Context.CreateConstant(1L),
                    this.sharedState.Context.CreateConstant(-1L));
            }
            else if (type.Resolution is ResolvedTypeKind.TupleType ts)
            {
                var values = ts.Item.Select(this.DefaultValue).ToArray();
                return this.sharedState.Values.CreateTuple(values, allocOnStack: this.sharedState.TargetQirProfile);
            }
            else if (type.Resolution is ResolvedTypeKind.UserDefinedType udt)
            {
                var elementTypes = this.sharedState.GetItemTypes(udt.Item.GetFullName());
                var values = elementTypes.Select(this.DefaultValue).ToArray();
                return this.sharedState.Values.CreateCustomType(udt.Item.GetFullName(), values, allocOnStack: this.sharedState.TargetQirProfile);
            }

            if (type.Resolution is ResolvedTypeKind.ArrayType itemType)
            {
                return this.sharedState.Values.CreateArray(itemType.Item, Array.Empty<IValue>(), allocOnStack: this.sharedState.TargetQirProfile);
            }
            else if (type.Resolution.IsFunction || type.Resolution.IsOperation)
            {
                // We can't simply set this to null, unless the reference and alias counting functions
                // in the runtime accept null values as arguments.
                var nullTableName = $"DefaultCallable__NullFunctionTable";
                var nullTable = this.sharedState.Module.GetNamedGlobal(nullTableName);
                if (nullTable == null)
                {
                    var fctType = this.sharedState.Types.FunctionSignature.CreatePointerType();
                    var funcs = Enumerable.Repeat(Constant.ConstPointerToNullFor(fctType), 4);
                    var array = ConstantArray.From(fctType, funcs.ToArray());
                    nullTable = this.sharedState.Module.AddGlobal(array.NativeType, true, Linkage.Internal, array, nullTableName);
                }

                var createCallable = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
                var memoryManagementTable = this.sharedState.GetOrCreateCallableMemoryManagementTable(null);
                var value = this.sharedState.CurrentBuilder.Call(createCallable, nullTable, memoryManagementTable, this.Unit.Value);
                var built = this.sharedState.Values.FromCallable(value, type);
                this.sharedState.ScopeMgr.RegisterValue(built);
                return built;
            }
            else if (type.Resolution.IsString)
            {
                return QirExpressionKindTransformation.CreateStringLiteral(this.sharedState, "");
            }
            else if (type.Resolution.IsBigInt)
            {
                var value = this.sharedState.CurrentBuilder.Call(
                    this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateI64),
                    this.sharedState.Context.CreateConstant(0L));
                var built = this.sharedState.Values.From(value, type);
                this.sharedState.ScopeMgr.RegisterValue(built);
                return built;
            }
            else if (type.Resolution.IsUnitType)
            {
                return this.sharedState.Values.Unit;
            }
            else
            {
                throw new NotSupportedException("no known default value for the given type");
            }
        }

        /// <summary>
        /// Creates a simple value that contains the given LLVM value as well as
        /// additional infos used for optimization during QIR generation.
        /// </summary>
        /// <param name="value">The LLVM value representing a value of the given Q# type.</param>
        /// <param name="type">The Q# type of the value.</param>
        internal SimpleValue FromSimpleValue(Value value, ResolvedType type) =>
            new(value, type);

        /// <summary>
        /// Creates a tuple value that represents a Q# value of user defined type and
        /// contains the given LLVM value as well as additional infos used for optimization
        /// during QIR generation.
        /// </summary>
        /// <param name="value">The LLVM value representing a Q# value of user defined type.</param>
        /// <param name="typeName">The Q# type name of the value.</param>
        internal TupleValue FromCustomType(Value value, QsQualifiedName typeName) =>
            new(typeName, value, this.sharedState.GetItemTypes(typeName), this.sharedState);

        /// <summary>
        /// Creates a tuple value that represents a Q# value of tuple type and contains
        /// the given LLVM value as well as additional infos used for optimization
        /// during QIR generation.
        /// </summary>
        /// <param name="value">The LLVM value representing a Q# tuple with elements of the given types.</param>
        /// <param name="elementTypes">The Q# types of the tuple elements.</param>
        internal TupleValue FromTuple(Value value, ImmutableArray<ResolvedType> elementTypes) =>
            new(value, elementTypes, this.sharedState);

        /// <inheritdoc cref="TupleValue(TupleValue, bool)"/>
        /// <param name="value">The tuple value to copy.</param>
        /// <param name="alwaysCopy">Whether to force the runtime to make a copy of the contained LLVM value even if the alias count is zero.</param>
        internal TupleValue FromTuple(TupleValue value, bool alwaysCopy) =>
            new(value, alwaysCopy);

        /// <summary>
        /// Creates an array value that represents a Q# value of array type and contains
        /// the given LLVM value as well as additional infos used for optimization
        /// during QIR generation.
        /// </summary>
        /// <param name="value">The LLVM value representing a Q# array with elements of the given type.</param>
        /// <param name="elementType">Q# type of the array elements.</param>
        /// <param name="count">The number of elements in the array, or null if unknown.</param>
        internal ArrayValue FromArray(Value value, ResolvedType elementType, uint? count) =>
            new(value, elementType, count, this.sharedState);

        /// <inheritdoc cref="ArrayValue(ArrayValue, bool)"/>
        /// <param name="value">The array value to copy.</param>
        /// <param name="alwaysCopy">Whether to force the runtime to make a copy of the contained LLVM value even if the alias count is zero.</param>
        internal ArrayValue FromArray(ArrayValue value, bool alwaysCopy) =>
            new(value, alwaysCopy);

        /// <summary>
        /// Creates a callable value that represents a Q# value of opertion or function type
        /// and contains the given LLVM value as well as additional infos used for optimization
        /// during QIR generation.
        /// </summary>
        /// <param name="value">The LLVM value representing a Q# callable of the given type.</param>
        /// <param name="type">The Q# type of the value.</param>
        internal CallableValue FromCallable(Value value, ResolvedType type) =>
            new(value, type, this.sharedState);

        /// <summary>
        /// Creates a suitable class instance that represents a Q# value of the given type
        /// and contains the given LLVM value as well as additional infos used for optimization
        /// during QIR generation.
        /// </summary>
        /// <param name="value">The LLVM value representing a Q# value of the given type.</param>
        /// <param name="type">The Q# type of the value.</param>
        internal IValue From(Value value, ResolvedType type) =>
            type.Resolution is ResolvedTypeKind.ArrayType it ? this.sharedState.Values.FromArray(value, it.Item, null) :
            type.Resolution is ResolvedTypeKind.TupleType ts ? this.sharedState.Values.FromTuple(value, ts.Item) :
            type.Resolution is ResolvedTypeKind.UserDefinedType udt ? this.sharedState.Values.FromCustomType(value, udt.Item.GetFullName()) :
            (type.Resolution.IsOperation || type.Resolution.IsFunction) ? this.sharedState.Values.FromCallable(value, type) :
            (IValue)new SimpleValue(value, type);

        internal SimpleValue CreatePauli(QsPauli pauli) =>
            new(this.sharedState.Context.CreateConstant(
                    this.sharedState.Types.Pauli,
                    pauli.IsPauliI ? 0ul :
                    pauli.IsPauliX ? 1ul :
                    pauli.IsPauliZ ? 2ul :
                    pauli.IsPauliY ? 3ul :
                    throw new NotImplementedException("unknown Pauli"),
                    false),
                ResolvedType.New(ResolvedTypeKind.Pauli));

        /// <summary>
        /// Creates a pointer to the given value.
        /// </summary>
        /// <param name="value">The value that the pointer points to</param>
        internal PointerValue CreatePointer(IValue value) =>
            new(value, this.sharedState);

        /// <summary>
        /// Builds a new tuple value that represents a Q# value of tuple type
        /// with the items set to the given tuple elements.
        /// The allocation of the value via invokation of the corresponding runtime function is lazy,
        /// and so are the necessary casts. Registers the value with the scope manager,
        /// unless <paramref name="registerWithScopeManager"/> is set to false.
        /// </summary>
        /// <param name="elementTypes">The Q# types of the tuple items.</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built value with the scope manager.</param>
        internal TupleValue CreateTuple(ImmutableArray<ResolvedType> elementTypes, bool registerWithScopeManager = true) =>
            new(elementTypes, this.sharedState, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Builds a new tuple value that represents a Q# value of tuple type
        /// with the items set to the given tuple elements.
        /// The tuple itself is allocated via invokation of the corresponding runtime function
        /// if <paramref name="allocOnStack"/> is set to false, and is allocated on the stack
        /// as an LLVM value of IStructType using the built-in LLVM support otherwise.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/>
        /// is set to false.
        /// </summary>
        /// <param name="tupleElements">The tuple elements.</param>
        /// <param name="allocOnStack">Whether to represent the tuple as a stack-allocated LLVM value of IStructType.</param>
        /// <param name="registerWithScopeManager">Whether to register the built value with the scope manager.</param>
        internal TupleValue CreateTuple(ImmutableArray<TypedExpression> tupleElements, bool allocOnStack, bool registerWithScopeManager = true) =>
            new(null, tupleElements, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Builds a tuple value that represents a Q# value of tuple type
        /// with the items set to the given tuple elements.
        /// The tuple itself is allocated via invokation of the corresponding runtime function
        /// if <paramref name="allocOnStack"/> is set to false, and is allocated on the stack
        /// as an LLVM value of IStructType usinf the built-in LLVM support otherwise.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/>
        /// is set to false. Increases the reference count for the tuple elements.
        /// </summary>
        /// <param name="tupleElements">The tuple elements.</param>
        /// <param name="allocOnStack">Whether to represent the tuple as a stack-allocated LLVM value of IStructType.</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built value with the scope manager.</param>
        internal TupleValue CreateTuple(IReadOnlyList<IValue> tupleElements, bool allocOnStack, bool registerWithScopeManager = true) =>
            new(null, tupleElements, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Builds a tuple value that represents a Q# value of user defined type
        /// with the items set to the given elements.
        /// The tuple itself is allocated via invokation of the corresponding runtime function
        /// if <paramref name="allocOnStack"/> is set to false, and is allocated on the stack
        /// as an LLVM value of IStructType usinf the built-in LLVM support otherwise.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/>
        /// is set to false.
        /// </summary>
        /// <param name="typeName">The name of the user defined type.</param>
        /// <param name="elements">The named or anonymous items that constitute the value of custom type.</param>
        /// <param name="allocOnStack">Whether to represent the value as a stack-allocated LLVM value of IStructType.</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built value with the scope manager.</param>
        internal TupleValue CreateCustomType(QsQualifiedName typeName, ImmutableArray<TypedExpression> elements, bool allocOnStack, bool registerWithScopeManager = true) =>
            new(typeName, elements, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Builds a tuple value that represents a Q# value of user defined type
        /// with the items set to the given elements.
        /// The tuple itself is allocated via invokation of the corresponding runtime function
        /// if <paramref name="allocOnStack"/> is set to false, and is allocated on the stack
        /// as an LLVM value of IStructType using the built-in LLVM support otherwise.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/>
        /// is set to false. Increases the reference count for the tuple elements.
        /// </summary>
        /// <param name="typeName">The name of the user defined type.</param>
        /// <param name="elements">The named or anonymous items that constitute the value of custom type.</param>
        /// <param name="allocOnStack">Whether to represent the value as a stack-allocated LLVM value of IStructType.</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built value with the scope manager.</param>
        internal TupleValue CreateCustomType(QsQualifiedName typeName, IReadOnlyList<IValue> elements, bool allocOnStack, bool registerWithScopeManager = true) =>
            new(typeName, elements, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Creates an array value that represents a Q# value of array type with the given length.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/>
        /// is set to false.
        /// </summary>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="length">Value of type i64 indicating the number of elements in the array</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built value with the scope manager.</param>
        internal ArrayValue CreateArray(ResolvedType elementType, Value length, bool registerWithScopeManager = true) =>
            new(elementType, length, this.sharedState, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Builds an array value that represents a Q# value of array type
        /// with the items set to the given array elements.
        /// The array itself is allocated via invokation of the corresponding runtime function
        /// if <paramref name="allocOnStack"/> is set to false, and is allocated on the stack
        /// as an LLVM value of IArrayType using the built-in LLVM support for constant arrays otherwise.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/>
        /// is set to false.
        /// </summary>
        /// <param name="elementType">The Q# type of the array elements.</param>
        /// <param name="arrayElements">The elements in the array.</param>
        /// <param name="allocOnStack">Whether to represent the array as a stack-allocated LLVM value of IArrayType.</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built value with the scope manager.</param>
        internal ArrayValue CreateArray(ResolvedType elementType, ImmutableArray<TypedExpression> arrayElements, bool allocOnStack, bool registerWithScopeManager = true) =>
            new(elementType, arrayElements, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Builds an array value that represents a Q# value of array type
        /// with the items set to the given array elements.
        /// The array itself is allocated via invokation of the corresponding runtime function
        /// if <paramref name="allocOnStack"/> is set to false, and is allocated on the stack
        /// as an LLVM value of IArrayType using the built-in LLVM support for constant arrays otherwise.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/>
        /// is set to false. Increases the reference count for the array elements.
        /// </summary>
        /// <param name="elementType">The Q# type of the array elements.</param>
        /// <param name="arrayElements">The elements in the array.</param>
        /// <param name="allocOnStack">Whether to represent the array as a stack-allocated LLVM value of IArrayType.</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built value with the scope manager.</param>
        internal ArrayValue CreateArray(ResolvedType elementType, IReadOnlyList<IValue> arrayElements, bool allocOnStack, bool registerWithScopeManager = true) =>
            new(elementType, arrayElements, this.sharedState, allocOnStack: allocOnStack, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Creates an array of the given length and populates each element with the value
        /// returned by <paramref name="getElement"/>, increasing its reference count accordingly.
        /// The function <paramref name="getElement"/> is invoked with the respective index.
        /// Registers the value with the scope manager, unless <paramref name="registerWithScopeManager"/>
        /// is set to false.
        /// </summary>
        /// <param name="elementType">The Q# type of the array elements.</param>
        /// <param name="length">Value of type i64 indicating the number of elements in the array.</param>
        /// <param name="getElement">Given an index into the array, returns the value to populate that element with.</param>
        /// <param name="registerWithScopeManager">Whether or not to register the built value with the scope manager.</param>
        internal ArrayValue CreateArray(ResolvedType elementType, Value length, Func<Value, IValue> getElement, bool registerWithScopeManager = true) =>
            new(elementType, length, getElement, this.sharedState, registerWithScopeManager: registerWithScopeManager);

        /// <summary>
        /// Creates a callable value of the given type and registers it with the scope manager.
        /// The necessary functions to invoke the callable are defined by the callable table;
        /// i.e. the globally defined array of function pointers accessible via the given global variable.
        /// </summary>
        /// <param name="globalName">The Q# name of the callable, if the callable is globally defined.</param>
        /// <param name="callableType">The Q# type of the callable value.</param>
        /// <param name="table">The global variable that contains the array of function pointers defining the callable.</param>
        internal CallableValue CreateCallable(QsQualifiedName globalName, ResolvedType callableType, GlobalVariable table) =>
            new(globalName, callableType, table, this.sharedState);

        /// <summary>
        /// Creates a callable value of the given type and registers it with the scope manager.
        /// The necessary functions to invoke the callable are defined by the callable table;
        /// i.e. the globally defined array of function pointers accessible via the given global variable.
        /// </summary>
        /// <param name="callableType">The Q# type of the callable value.</param>
        /// <param name="table">The global variable that contains the array of function pointers defining the callable.</param>
        /// <param name="captured">All captured values.</param>
        internal CallableValue CreateCallable(ResolvedType callableType, GlobalVariable table, ImmutableArray<TypedExpression>? captured = null) =>
            new(callableType, table, this.sharedState, captured ?? ImmutableArray<TypedExpression>.Empty);
    }
}
