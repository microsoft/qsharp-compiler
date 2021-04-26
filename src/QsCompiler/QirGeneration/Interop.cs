﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using ArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// This class contains utils for facilitating interoperability and entry point handling.
    /// </summary>
    internal sealed class Interop
    {
        private readonly GenerationContext sharedState;

        private Interop(GenerationContext sharedState) =>
            this.sharedState = sharedState;

        // public static methods calling into private methods

        /// <inheritdoc cref="GenerateWrapper(string, ArgumentTuple, ResolvedType, IrFunction)"/>
        public static IrFunction GenerateWrapper(GenerationContext sharedState, string wrapperName, ArgumentTuple argumentTuple, ResolvedType returnType, IrFunction implementation) =>
            new Interop(sharedState).GenerateWrapper(wrapperName, argumentTuple, returnType, implementation);

        /// <inheritdoc cref="GenerateEntryPoint(string, ArgumentTuple, ResolvedType, IrFunction)"/>
        internal static IrFunction GenerateEntryPoint(GenerationContext sharedState, string entryPointName, ArgumentTuple argumentTuple, ResolvedType returnType, IrFunction implementation) =>
            new Interop(sharedState).GenerateEntryPoint(entryPointName, argumentTuple, returnType, implementation);

        // private methods

        /// <summary>
        /// Creates a suitable array of values to access the item at a given index for a pointer to a struct.
        /// </summary>
        private Value[] PointerIndex(int index) => new[]
        {
            this.sharedState.Context.CreateConstant(0L),
            this.sharedState.Context.CreateConstant(index)
        };

        /// <summary>
        /// Applies the map function to the given items, filters all items that are null,
        /// and returns the mapped non-null values as array.
        /// </summary>
        /// <typeparam name="TIn">The type of the items in the given sequence.</typeparam>
        /// <typeparam name="TOut">The type of the items in the returned array.</typeparam>
        /// <param name="map">The function to apply to each item in the sequence.</param>
        /// <param name="items">The sequence of items to map.</param>
        private static TOut[] WithoutNullValues<TIn, TOut>(Func<TIn, TOut?> map, IEnumerable<TIn> items)
            where TOut : class =>
            items.Select(map).Where(i => i != null).Select(i => i!).ToArray();

        /// <inheritdoc cref="MapToInteropType(ITypeRef)"/>
        private ITypeRef? MapToInteropType(ResolvedType type) =>
            this.MapToInteropType(this.sharedState.LlvmTypeFromQsharpType(type));

        /// <summary>
        /// Maps the given Q#/QIR type to a more interop-friendly type.
        /// Returns null only if the given type is Unit. Strips items of type Unit inside tuples.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The given type is a pointer to a non-struct type,
        /// or the given type is not specified in the QIR format.
        /// </exception>
        private ITypeRef? MapToInteropType(ITypeRef t)
        {
            // Range, Tuple (typed and untyped), Array, Result, String, BigInt, Callable, and Qubit
            // are all structs or struct pointers.
            t = t.IsPointer ? Types.StructFromPointer(t) : t;
            var typeName = (t as IStructType)?.Name;

            var bytePtrType = this.sharedState.Context.Int8Type.CreatePointerType();

            if (typeName == TypeNames.Array || typeName == TypeNames.BigInt)
            {
                return this.sharedState.Context.CreateStructType(
                    packed: false,
                    this.sharedState.Context.Int64Type,
                    this.sharedState.Types.DataArrayPointer)
                   .CreatePointerType();
            }
            else if (typeName == TypeNames.String)
            {
                return this.sharedState.Types.DataArrayPointer;
            }
            else if (typeName == TypeNames.Callable || typeName == TypeNames.Qubit)
            {
                return bytePtrType;
            }
            else if (typeName == TypeNames.Result)
            {
                return this.sharedState.Context.Int8Type;
            }
            else if (t is IStructType st)
            {
                var itemTypes = WithoutNullValues(this.MapToInteropType, st.Members);
                return itemTypes.Length > 0
                    ? this.sharedState.Context.CreateStructType(packed: false, itemTypes).CreatePointerType()
                    : null;
            }
            if (t.IsInteger)
            {
                // covers Int, Bool, Pauli
                var nrBytes = 1 + ((t.IntegerBitWidth - 1) / 8);
                return this.sharedState.Context.GetIntType(8 * nrBytes);
            }
            else if (t.IsFloatingPoint)
            {
                return this.sharedState.Context.DoubleType;
            }
            else
            {
                throw new ArgumentException("Unrecognized type");
            }
        }

        /// <summary>
        /// Assuming the given parameters are defined by flattening the given argument tuple and stripping
        /// all values of type Unit, constructs the argument(s) to the QIR function that matches the argument tuple.
        /// The arguments of the current function are assumed to be given as interop friendly types
        /// defined by <see cref="MapToInteropType(ITypeRef)"/>.
        /// This method generates suitable calls to the QIR runtime functions and other necessary
        /// conversions and casts to construct the arguments for the QIR function;
        /// i.e. this method implements the mapping "interop-friendly function arguments -> QIR function arguments".
        /// </summary>
        /// <returns>The array of arguments with which to invoke the QIR function with the given argument tuple.</returns>
        /// <exception cref="InvalidOperationException">The current function is null.</exception>
        private Value[] ProcessArguments(ArgumentTuple arg, IReadOnlyList<Argument> parameters)
        {
            if (this.sharedState.CurrentFunction == null)
            {
                throw new InvalidOperationException("the current function is null");
            }

            (Value Length, Value DataArray) LoadSizedArray(Value value)
            {
                var lengthPtr = this.sharedState.CurrentBuilder.GetElementPtr(Types.PointerElementType(value), value, this.PointerIndex(0));
                var dataArrPtr = this.sharedState.CurrentBuilder.GetElementPtr(Types.PointerElementType(value), value, this.PointerIndex(1));
                var length = this.sharedState.CurrentBuilder.Load(this.sharedState.Types.Int, lengthPtr);
                var dataArr = this.sharedState.CurrentBuilder.Load(this.sharedState.Types.DataArrayPointer, dataArrPtr);
                return (length, dataArr);
            }

            IValue[] GetStructItems(Value value, IEnumerable<ResolvedType> itemTypes, bool registerWithScopeManager)
            {
                var itemIndex = 0;
                Value NextTupleItem()
                {
                    var itemPtr = this.sharedState.CurrentBuilder.GetElementPtr(Types.PointerElementType(value), value, this.PointerIndex(itemIndex++));
                    return this.sharedState.CurrentBuilder.Load(Types.PointerElementType(itemPtr), itemPtr);
                }
                return itemTypes.Select(arg => ProcessGivenValue(arg, registerWithScopeManager, NextTupleItem)).ToArray();
            }

            IValue ProcessGivenValue(ResolvedType type, bool registerWithScopeManager, Func<Value> next)
            {
                if (type.Resolution.IsUnitType)
                {
                    return this.sharedState.Values.Unit;
                }

                Value givenValue = next();
                if (type.Resolution is ResolvedTypeKind.ArrayType arrItemType)
                {
                    var (length, dataArr) = LoadSizedArray(givenValue);
                    ArrayValue array = this.sharedState.Values.CreateArray(length, arrItemType.Item, registerWithScopeManager: false);
                    if (registerWithScopeManager)
                    {
                        this.sharedState.ScopeMgr.RegisterValue(array);
                    }

                    var dataArrStart = this.sharedState.CurrentBuilder.PointerToInt(dataArr, this.sharedState.Context.Int64Type);
                    var givenArrElementType = this.MapToInteropType(array.LlvmElementType) ?? this.sharedState.Values.Unit.LlvmType;
                    var givenArrElementSize = this.sharedState.ComputeSizeForType(givenArrElementType);

                    void PopulateItem(Value index)
                    {
                        var element = ProcessGivenValue(array.QSharpElementType, false, () =>
                        {
                            var offset = this.sharedState.CurrentBuilder.Mul(index, givenArrElementSize);
                            var elementPointer = this.sharedState.CurrentBuilder.IntToPointer(
                                this.sharedState.CurrentBuilder.Add(dataArrStart, offset),
                                givenArrElementType.CreatePointerType());
                            return this.sharedState.CurrentBuilder.Load(givenArrElementType, elementPointer);
                        });
                        array.GetArrayElementPointer(index).StoreValue(element);
                    }

                    var start = this.sharedState.Context.CreateConstant(0L);
                    var end = this.sharedState.CurrentBuilder.Sub(array.Length, this.sharedState.Context.CreateConstant(1L));
                    this.sharedState.IterateThroughRange(start, null, end, PopulateItem);
                    return array;
                }
                else if (type.Resolution is ResolvedTypeKind.TupleType items)
                {
                    var tupleItems = GetStructItems(givenValue, items.Item, registerWithScopeManager);
                    return this.sharedState.Values.CreateTuple(registerWithScopeManager, tupleItems);
                }
                else if (type.Resolution.IsBigInt)
                {
                    var createBigInt = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateArray);
                    var (length, dataArr) = LoadSizedArray(givenValue);
                    var argValue = this.sharedState.CurrentBuilder.Call(createBigInt, length, dataArr);
                    var value = this.sharedState.Values.From(argValue, type);
                    if (registerWithScopeManager)
                    {
                        this.sharedState.ScopeMgr.RegisterValue(value);
                    }
                    return value;
                }
                else if (type.Resolution.IsString)
                {
                    var createString = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringCreate);
                    var argValue = this.sharedState.CurrentBuilder.Call(createString, givenValue);
                    var value = this.sharedState.Values.From(argValue, type);
                    if (registerWithScopeManager)
                    {
                        this.sharedState.ScopeMgr.RegisterValue(value);
                    }
                    return value;
                }
                else if (type.Resolution.IsResult)
                {
                    var getZero = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultGetZero);
                    var getOne = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultGetOne);
                    var cond = this.sharedState.CurrentBuilder.Compare(
                        IntPredicate.Equal,
                        givenValue,
                        this.sharedState.Context.CreateConstant(givenValue.NativeType, 0u, false));
                    var argValue = this.sharedState.CurrentBuilder.Select(
                        cond,
                        this.sharedState.CurrentBuilder.Call(getZero),
                        this.sharedState.CurrentBuilder.Call(getOne));
                    return this.sharedState.Values.From(argValue, type);
                }
                else if (type.Resolution.IsRange)
                {
                    var itemTypes = Enumerable.Repeat(ResolvedType.New(ResolvedTypeKind.Int), 3);
                    var rangeItems = GetStructItems(givenValue, itemTypes, registerWithScopeManager);
                    return this.sharedState.CreateRange(rangeItems[0].Value, rangeItems[1].Value, rangeItems[2].Value);
                }
                else if (givenValue.NativeType.IsInteger)
                {
                    var expectedType = this.sharedState.LlvmTypeFromQsharpType(type);
                    var argValue = this.sharedState.CurrentBuilder.IntCast(givenValue, expectedType, type.Resolution.IsInt);
                    return this.sharedState.Values.FromSimpleValue(argValue, type);
                }
                else if (givenValue.NativeType.IsFloatingPoint)
                {
                    return this.sharedState.Values.FromSimpleValue(givenValue, type);
                }
                else
                {
                    // bitcast to the correct type and return
                    var expectedArgType = this.sharedState.LlvmTypeFromQsharpType(type);
                    var argValue = this.sharedState.CastToType(givenValue, expectedArgType);
                    return this.sharedState.Values.From(argValue, type);
                }
            }

            IValue ProcessArgumentTupleItem(ArgumentTuple item, Func<Value> nextArgument)
            {
                if (item is ArgumentTuple.QsTuple innerTuple)
                {
                    var tupleItems = innerTuple.Item.Select(arg => ProcessArgumentTupleItem(arg, nextArgument)).ToArray();
                    return this.sharedState.Values.CreateTuple(tupleItems);
                }
                else
                {
                    return item is ArgumentTuple.QsTupleItem innerItem
                        ? ProcessGivenValue(innerItem.Item.Type, true, nextArgument)
                        : throw new NotSupportedException("unknown item in argument tuple");
                }
            }

            var currentFunctionArgIndex = 0;
            Value NextArgument() => parameters[currentFunctionArgIndex++];
            var args = arg is ArgumentTuple.QsTuple argTuple ? argTuple.Item : ImmutableArray.Create(arg);
            return args.Select(item => ProcessArgumentTupleItem(item, NextArgument).Value).ToArray();
        }

        /// <summary>
        /// This method generates suitable calls, conversions and casts to map the given return value
        /// of a QIR function to an interop-friendly value; i.e. this method implements the mapping
        /// "QIR value -> interop friendly value". It strips all inner tuple items of type Unit,
        /// and returns null only if the given value represents a value of type Unit.
        /// <br/><br/>
        /// The memory for the returned value is allocated on the heap using the corresponding runtime
        /// function <see cref="RuntimeLibrary.HeapAllocate"/> and will not be freed by the QIR runtime.
        /// It is the responsibility of the code calling into the QIR entry point wrapper to free that memory.
        /// </summary>
        /// <returns>The interop-friendly value for the given value obtained by invoking a QIR function.</returns>
        /// <exception cref="InvalidOperationException">The current function is null.</exception>
        private Value? ProcessReturnValue(IValue res)
        {
            if (this.sharedState.CurrentFunction == null)
            {
                throw new InvalidOperationException("the current function is null");
            }

            Value CopyToMemory(IPointerType targetType, Value size, Value? sourcePtr = null)
            {
                var malloc = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.HeapAllocate);
                var allocated = this.sharedState.CurrentBuilder.Call(malloc, size);
                if (sourcePtr != null)
                {
                    this.sharedState.CurrentBuilder.MemCpy(allocated, sourcePtr, size, false);
                }
                return this.sharedState.CurrentBuilder.BitCast(allocated, targetType);
            }

            Value PopulateStruct(IPointerType mappedType, Value[] tupleItems)
            {
                var mappedStructType = Types.StructFromPointer(mappedType);
                var size = this.sharedState.ComputeSizeForType(mappedStructType);
                var mappedTuple = CopyToMemory(mappedType, size);

                for (var itemIdx = 0; itemIdx < mappedStructType.Members.Count; ++itemIdx)
                {
                    var itemPtr = this.sharedState.CurrentBuilder.GetElementPtr(mappedStructType, mappedTuple, this.PointerIndex(itemIdx));
                    var tupleItem = this.sharedState.CastToType(tupleItems[itemIdx], mappedStructType.Members[itemIdx]);
                    this.sharedState.CurrentBuilder.Store(tupleItem, itemPtr);
                }
                return mappedTuple;
            }

            if (res.QSharpType.Resolution.IsUnitType)
            {
                return null;
            }

            if (res is ArrayValue array)
            {
                var malloc = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.HeapAllocate);
                var dataArrElementType = this.MapToInteropType(array.QSharpElementType) ?? this.sharedState.Values.Unit.LlvmType;
                var sizePerElement = this.sharedState.ComputeSizeForType(dataArrElementType);
                var dataArr = this.sharedState.CurrentBuilder.Call(malloc, this.sharedState.CurrentBuilder.Mul(array.Length, sizePerElement));

                var dataArrStart = this.sharedState.CurrentBuilder.PointerToInt(dataArr, this.sharedState.Context.Int64Type);
                void PopulateItem(Value index)
                {
                    var offset = this.sharedState.CurrentBuilder.Mul(index, sizePerElement);
                    var elementPointer = this.sharedState.CurrentBuilder.IntToPointer(
                        this.sharedState.CurrentBuilder.Add(dataArrStart, offset),
                        dataArrElementType.CreatePointerType());
                    var element = this.ProcessReturnValue(array.GetArrayElement(index)) ?? this.sharedState.Values.Unit.Value;
                    this.sharedState.CurrentBuilder.Store(element, elementPointer);
                }

                var start = this.sharedState.Context.CreateConstant(0L);
                var end = this.sharedState.CurrentBuilder.Sub(array.Length, this.sharedState.Context.CreateConstant(1L));
                this.sharedState.IterateThroughRange(start, null, end, PopulateItem);

                var tupleItems = new[] { array.Length, dataArr };
                var mappedType = this.MapToInteropType(array.QSharpType)!;
                return PopulateStruct((IPointerType)mappedType, tupleItems);
            }
            else if (res is TupleValue tuple)
            {
                var mappedType = this.MapToInteropType(tuple.QSharpType);
                if (mappedType == null)
                {
                    return null;
                }
                else if (tuple.LlvmType.Equals(mappedType))
                {
                    var size = this.sharedState.ComputeSizeForType(tuple.StructType);
                    return CopyToMemory((IPointerType)mappedType, size, tuple.TypedPointer);
                }

                var tupleItems = WithoutNullValues(this.ProcessReturnValue, tuple.GetTupleElements());
                return PopulateStruct((IPointerType)mappedType, tupleItems);
            }
            else if (res.QSharpType.Resolution.IsBigInt)
            {
                // TODO: We can't know the length of the big int without runtime support.
                // We may also need functions to access the data array for both string and big int.
                throw new NotImplementedException("returning values of type BigInt is not yet supported");
            }
            else if (res.QSharpType.Resolution.IsString)
            {
                var getLength = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringGetLength);
                var strLength = this.sharedState.CurrentBuilder.Call(getLength, res.Value);
                var size = this.sharedState.CurrentBuilder.IntCast(strLength, this.sharedState.Context.Int64Type, true);

                var getData = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringGetData);
                var strData = this.sharedState.CurrentBuilder.Call(getData, res.Value);

                var expectedType = this.MapToInteropType(res.LlvmType)!;
                return CopyToMemory((IPointerType)expectedType, size, strData); // not sure if it is better to avoid the copy and increase the ref count
            }
            else if (res.QSharpType.Resolution.IsResult)
            {
                var getZero = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultGetZero);
                var resType = this.MapToInteropType(this.sharedState.Types.Result)!;
                var zeroValue = this.sharedState.Context.CreateConstant(resType, 0u, false);
                var oneValue = this.sharedState.Context.CreateConstant(resType, ~0u, false);

                var equals = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultEqual);
                var cond = this.sharedState.CurrentBuilder.Call(equals, res.Value, this.sharedState.CurrentBuilder.Call(getZero));
                return this.sharedState.CurrentBuilder.Select(cond, zeroValue, oneValue);
            }
            else if (res.QSharpType.Resolution.IsRange)
            {
                var start = this.sharedState.CurrentBuilder.ExtractValue(res.Value, 0u);
                var step = this.sharedState.CurrentBuilder.ExtractValue(res.Value, 1u);
                var end = this.sharedState.CurrentBuilder.ExtractValue(res.Value, 2u);

                var expectedType = this.MapToInteropType(res.LlvmType)!;
                return PopulateStruct((IPointerType)expectedType, new[] { start, step, end });
            }
            else if (res.LlvmType.IsInteger)
            {
                var expectedType = this.MapToInteropType(res.LlvmType)!;
                return this.sharedState.CurrentBuilder.IntCast(res.Value, expectedType, res.QSharpType.Resolution.IsInt);
            }
            else if (res.LlvmType.IsFloatingPoint)
            {
                return res.Value;
            }
            else
            {
                // callables and qubits
                var expectedType = this.MapToInteropType(res.LlvmType)!;
                this.sharedState.ScopeMgr.IncreaseReferenceCount(res);
                return this.sharedState.CastToType(res.Value, expectedType);
            }
        }

        /// <summary>
        /// Generates an interop-friendly wrapper around a QIR function that can be invoked from within
        /// native code without relying on the QIR runtime or adhering to the QIR specification.
        /// See <seealso cref="ProcessArguments"/> and <seealso cref="ProcessReturnValue"/>
        /// for more detail.
        /// <br/>
        /// Creates an alias with the given name instead of a wrapper function
        /// if the wrapper signature and signature of the QIR implementation match.
        /// </summary>
        /// <param name="wrapperName">The function name to give the wrapper.</param>
        /// <param name="argumentTuple">The argument tuple of the callable that the wrapper should invoke.</param>
        /// <param name="returnType">The return type of the callable that the wrapper should invoke.</param>
        /// <param name="implementation">The QIR function that implements the body of the function that should be invoked.</param>
        /// <returns>The created wrapper function or the implementation if no wrapper function has been created.</returns>
        private IrFunction GenerateWrapper(string wrapperName, ArgumentTuple argumentTuple, ResolvedType returnType, IrFunction implementation)
        {
            var argItems = SyntaxGenerator.ExtractItems(argumentTuple)
                .Where(sym => !sym.Type.Resolution.IsUnitType)
                .ToArray();

            var wrapperReturnType = this.MapToInteropType(returnType);
            var wrapperSignature = this.sharedState.Context.GetFunctionType(
                wrapperReturnType ?? this.sharedState.Context.VoidType,
                argItems.Select(sym => this.MapToInteropType(sym.Type)!));

            if (wrapperSignature.Equals(implementation.Signature))
            {
                this.sharedState.Module.AddAlias(implementation, wrapperName).Linkage = Linkage.External;
                return implementation;
            }
            else
            {
                var wrapperFunc = this.sharedState.Module.CreateFunction(wrapperName, wrapperSignature);
                var argNames = argItems.Select(arg => arg.VariableName is QsLocalSymbol.ValidName name ? name.Item : null).ToArray();

                this.sharedState.GenerateFunction(wrapperFunc, argNames, parameters =>
                {
                    var argValueList = this.ProcessArguments(argumentTuple, parameters);
                    var evaluatedValue = this.sharedState.CurrentBuilder.Call(implementation, argValueList);
                    var result = this.sharedState.Values.From(evaluatedValue, returnType);
                    this.sharedState.ScopeMgr.RegisterValue(result);

                    if (wrapperSignature.ReturnType.IsVoid)
                    {
                        this.sharedState.AddReturn(this.sharedState.Values.Unit, true);
                    }
                    else if (wrapperSignature.ReturnType.Equals(result.LlvmType))
                    {
                        this.sharedState.AddReturn(result, false);
                    }
                    else
                    {
                        // ProcessReturnValue makes sure the memory for the returned value isn't freed
                        var returnValue = this.ProcessReturnValue(result)!;
                        this.sharedState.ScopeMgr.ExitFunction(this.sharedState.Values.Unit);
                        this.sharedState.CurrentBuilder.Return(returnValue);
                    }
                });

                return wrapperFunc;
            }
        }

        /// <summary>
        /// Generates an entry point function that calls into the corresponding QIR function,
        /// invokes the runtime function <see cref="RuntimeLibrary.Message"/> with the result, and returns void.
        /// The arguments for the entry point are the C-compatible representation for the given argument tuple,
        /// see <seealso cref="ProcessArguments"/>.
        /// </summary>
        /// <param name="entryPointName">The name to give the created entry point function.</param>
        /// <param name="argumentTuple">The argument tuple of the callable that the entry point should invoke.</param>
        /// <param name="returnType">The return type of the callable that the entry point should invoke.</param>
        /// <param name="implementation">The QIR function that implements the body of the function that should be invoked.</param>
        /// <returns>The created entry point function.</returns>
        private IrFunction GenerateEntryPoint(string entryPointName, ArgumentTuple argumentTuple, ResolvedType returnType, IrFunction implementation)
        {
            var argItems = SyntaxGenerator.ExtractItems(argumentTuple)
                .Where(sym => !sym.Type.Resolution.IsUnitType)
                .ToArray();

            var entryPointSignature = this.sharedState.Context.GetFunctionType(
                this.sharedState.Context.VoidType,
                argItems.Select(sym => this.MapToInteropType(sym.Type)!));

            var entryPointFunction = this.sharedState.Module.CreateFunction(entryPointName, entryPointSignature);
            var argNames = argItems.Select(arg => arg.VariableName is QsLocalSymbol.ValidName name ? name.Item : null).ToArray();

            this.sharedState.GenerateFunction(entryPointFunction, argNames, parameters =>
            {
                var argValueList = this.ProcessArguments(argumentTuple, parameters);
                var evaluatedValue = this.sharedState.CurrentBuilder.Call(implementation, argValueList);
                var result = this.sharedState.Values.From(evaluatedValue, returnType);
                this.sharedState.ScopeMgr.RegisterValue(result);

                // print the return value
                if (!returnType.Resolution.IsUnitType)
                {
                    var message = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.Message);
                    var outputStr = QirExpressionKindTransformation.CreateStringLiteral(this.sharedState, "{0}", result);
                    this.sharedState.CurrentBuilder.Call(message, outputStr.Value);
                }

                this.sharedState.AddReturn(this.sharedState.Values.Unit, true);
            });

            return entryPointFunction;
        }
    }
}
