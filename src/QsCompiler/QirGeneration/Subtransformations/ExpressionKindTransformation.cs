// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using ResolvedExpression = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;

    internal class QirExpressionKindTransformation : ExpressionKindTransformation<GenerationContext>
    {
        // inner classes

        private abstract class PartialApplicationArgument
        {
            protected readonly GenerationContext SharedState;

            public PartialApplicationArgument(GenerationContext sharedState)
            {
                this.SharedState = sharedState;
            }

            public abstract Value BuildItem(InstructionBuilder builder, Value capture, Value parArgs);
        }

        private class InnerCapture : PartialApplicationArgument
        {
            public readonly int CaptureIndex;

            public InnerCapture(GenerationContext sharedState, int captureIndex)
            : base(sharedState)
            {
                this.CaptureIndex = captureIndex;
            }

            /// <summary>
            /// The given capture is expected to be fully typed.
            /// The parArgs parameter is unused.
            /// </summary>
            public override Value BuildItem(InstructionBuilder builder, Value capture, Value parArgs)
            {
                var captureType = Quantum.QIR.Types.StructFromPointer(capture.NativeType);
                return this.SharedState.GetTupleElement(captureType, capture, this.CaptureIndex, builder);
            }
        }

        private class InnerArg : PartialApplicationArgument
        {
            public readonly ITypeRef ItemType;
            public readonly int ArgIndex;

            public InnerArg(GenerationContext sharedState, ITypeRef itemType, int argIndex)
            : base(sharedState)
            {
                this.ItemType = itemType;
                this.ArgIndex = argIndex;
            }

            /// <summary>
            /// The given parameter parArgs is expected to contain an argument to a partial application, and is expected to be fully typed.
            /// The given capture is unused.
            /// </summary>
            public override Value BuildItem(InstructionBuilder builder, Value capture, Value parArgs)
            {
                // parArgs.NativeType == this.ItemType may occur if we have an item of user defined type (represented as a tuple)
                if (this.SharedState.Types.IsTupleType(parArgs.NativeType) && parArgs.NativeType != this.ItemType)
                {
                    var parArgsStruct = Quantum.QIR.Types.StructFromPointer(parArgs.NativeType);
                    return this.SharedState.GetTupleElement(parArgsStruct, parArgs, this.ArgIndex, builder);
                }
                else
                {
                    return parArgs;
                }
            }
        }

        private class InnerTuple : PartialApplicationArgument
        {
            public readonly ResolvedType TupleType;
            public readonly ImmutableArray<PartialApplicationArgument> Items;

            public InnerTuple(GenerationContext sharedState, ResolvedType tupleType, IEnumerable<PartialApplicationArgument>? items)
            : base(sharedState)
            {
                this.TupleType = tupleType;
                this.Items = items?.ToImmutableArray() ?? ImmutableArray<PartialApplicationArgument>.Empty;
            }

            /// <summary>
            /// The given capture is expected to be fully typed.
            /// The given parameter parArgs is expected to contain an argument to a partial application, and is expected to be fully typed.
            /// </summary>
            /// <returns>A fully typed tuple that combines the captured values as well as the arguments to the partial application</returns>
            public override Value BuildItem(InstructionBuilder builder, Value capture, Value parArgs)
            {
                var items = this.Items.Select(item => item.BuildItem(builder, capture, parArgs)).ToArray();
                var tuple = this.SharedState.CreateTuple(builder, items);
                return tuple.TypedPointer;
            }
        }

        // constructors

        public QirExpressionKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation) : base(parentTransformation)
        {
        }

        public QirExpressionKindTransformation(GenerationContext sharedState) : base(sharedState)
        {
        }

        public QirExpressionKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options) : base(parentTransformation, options)
        {
        }

        public QirExpressionKindTransformation(GenerationContext sharedState, TransformationOptions options) : base(sharedState, options)
        {
        }

        // private helpers

        /// <param name="sharedState">The generation context in which to emit the instructions</param>
        /// <param name="range">The range expression for which to create the access functions</param>
        /// <returns>
        /// Three functions to access the start, step, and end of a range.
        /// The function to access the step may return null if the given range does not specify the step.
        /// In that case, the step size defaults to be 1L.
        /// </returns>
        internal static (Func<Value> GetStart, Func<Value?> GetStep, Func<Value> GetEnd) RangeItems(GenerationContext sharedState, TypedExpression range)
        {
            Func<Value> startValue;
            Func<Value?> stepValue;
            Func<Value> endValue;
            if (range.Expression is ResolvedExpression.RangeLiteral rlit)
            {
                if (rlit.Item1.Expression is ResolvedExpression.RangeLiteral rlitInner)
                {
                    startValue = () => sharedState.EvaluateSubexpression(rlitInner.Item1);
                    stepValue = () => sharedState.EvaluateSubexpression(rlitInner.Item2);
                }
                else
                {
                    startValue = () => sharedState.EvaluateSubexpression(rlit.Item1);
                    stepValue = () => null;
                }

                // Item2 is always the end. Either Item1 is the start and 1 is the step,
                // or Item1 is a range expression itself, with Item1 the start and Item2 the step.
                endValue = () => sharedState.EvaluateSubexpression(rlit.Item2);
            }
            else
            {
                var rangeValue = sharedState.EvaluateSubexpression(range);
                startValue = () => sharedState.CurrentBuilder.ExtractValue(rangeValue, 0);
                stepValue = () => sharedState.CurrentBuilder.ExtractValue(rangeValue, 1);
                endValue = () => sharedState.CurrentBuilder.ExtractValue(rangeValue, 2);
            }
            return (startValue, stepValue, endValue);
        }

        /// <summary>
        /// Determines the location of the item with the given name within the tuple of type items.
        /// The returned list contains the index of the item starting from the outermost tuple
        /// as well as the type of the subtuple or item at that location.
        /// </summary>
        /// <param name="name">The name if the item to find the location for in the tuple of type items</param>
        /// <param name="typeItems">The tuple defining the items of a custom type</param>
        /// <param name="itemLocation">The location of the item with the given name within the item tuple</param>
        /// <returns>Returns true if the item was found and false otherwise</returns>
        private bool FindNamedItem(string name, QsTuple<QsTypeItem> typeItems, out List<(int, IStructType)> itemLocation)
        {
            ITypeRef GetTypeItemType(QsTuple<QsTypeItem> item)
            {
                switch (item)
                {
                    case QsTuple<QsTypeItem>.QsTupleItem leaf:
                        var leafType = leaf.Item switch
                        {
                            QsTypeItem.Anonymous anon => anon.Item,
                            QsTypeItem.Named named => named.Item.Type,
                            _ => ResolvedType.New(QsResolvedTypeKind.InvalidType)
                        };
                        return this.SharedState.LlvmTypeFromQsharpType(leafType);

                    case QsTuple<QsTypeItem>.QsTuple list:
                        var types = list.Item.Select(i => i switch
                        {
                            QsTuple<QsTypeItem>.QsTuple l => GetTypeItemType(l),
                            QsTuple<QsTypeItem>.QsTupleItem l => GetTypeItemType(l),
                            _ => this.SharedState.Context.TokenType
                        });
                        return this.SharedState.Types.CreateConcreteTupleType(types).CreatePointerType();

                    default:
                        throw new NotImplementedException("unknown item in argument tuple");
                }
            }

            bool FindNamedItem(QsTuple<QsTypeItem> items, List<(int, IStructType)> location)
            {
                switch (items)
                {
                    case QsTuple<QsTypeItem>.QsTupleItem leaf:
                        if ((leaf.Item is QsTypeItem.Named n) && (n.Item.VariableName == name))
                        {
                            return true;
                        }
                        break;
                    case QsTuple<QsTypeItem>.QsTuple list:
                        for (int i = 0; i < list.Item.Length; i++)
                        {
                            if (FindNamedItem(list.Item[i], location))
                            {
                                var tupleStruct = this.SharedState.Types.CreateConcreteTupleType(list.Item.Select(GetTypeItemType));
                                location.Add((i, tupleStruct));
                                return true;
                            }
                        }
                        break;
                }
                return false;
            }

            itemLocation = new List<(int, IStructType)>();
            var found = FindNamedItem(typeItems, itemLocation);
            itemLocation.Reverse();
            return found;
        }

        /// <returns>
        /// The result of the evaluation if the given name matches one of the recognized runtime functions,
        /// and null otherwise.
        /// </returns>
        private bool TryEvaluateRuntimeFunction(QsQualifiedName name, TypedExpression arg, [MaybeNullWhen(false)] out Value evaluated)
        {
            if (name.Equals(BuiltIn.Length.FullName))
            {
                var arrayArg = this.SharedState.EvaluateSubexpression(arg);
                var lengthFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetSize1d);
                evaluated = this.SharedState.CurrentBuilder.Call(lengthFunc, arrayArg);
                return true;
            }
            else if (name.Equals(BuiltIn.RangeStart.FullName))
            {
                var (getStart, _, _) = RangeItems(this.SharedState, arg);
                evaluated = getStart();
                return true;
            }
            else if (name.Equals(BuiltIn.RangeStep.FullName))
            {
                var (_, getStep, _) = RangeItems(this.SharedState, arg);
                evaluated = getStep() ?? this.SharedState.Context.CreateConstant(1L);
                return true;
            }
            else if (name.Equals(BuiltIn.RangeEnd))
            {
                var (_, _, getEnd) = RangeItems(this.SharedState, arg);
                evaluated = getEnd();
                return true;
            }
            else if (name.Equals(BuiltIn.RangeReverse.FullName))
            {
                var (getStart, getStep, getEnd) = RangeItems(this.SharedState, arg);
                var start = getStart();
                var step = getStep() ?? this.SharedState.Context.CreateConstant(1L);
                var end = getEnd();

                var newStart = this.SharedState.CurrentBuilder.Add(
                    start,
                    this.SharedState.CurrentBuilder.Mul(
                        step,
                        this.SharedState.CurrentBuilder.SDiv(
                            this.SharedState.CurrentBuilder.Sub(end, start), step)));
                evaluated = this.SharedState.CurrentBuilder.Load(
                    this.SharedState.Types.Range,
                    this.SharedState.Constants.EmptyRange);
                evaluated = this.SharedState.CurrentBuilder.InsertValue(evaluated, newStart, 0u);
                evaluated = this.SharedState.CurrentBuilder.InsertValue(evaluated, this.SharedState.CurrentBuilder.Neg(step), 1u);
                evaluated = this.SharedState.CurrentBuilder.InsertValue(evaluated, start, 2u);
                return true;
            }
            else
            {
                evaluated = null;
                return false;
            }
        }

        /// <summary>
        /// Handles calls to specific functor specializations of global callables.
        /// Directly invokes the corresponding target instruction If a target instruction name is associated the callable.
        /// Inlines the corresponding function if the callable is marked as to be inlined.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no callable with the given name exists, or the corresponding specialization cannot be found.
        /// </exception>
        private Value InvokeGlobalCallable(QsQualifiedName callableName, QsSpecializationKind kind, TypedExpression arg)
        {
            Value CallGlobal(IrFunction func, TypedExpression arg)
            {
                Value[] argList;
                if (arg.ResolvedType.Resolution.IsUnitType)
                {
                    argList = new Value[] { };
                }
                else if (arg.ResolvedType.Resolution.IsTupleType && arg.Expression is ResolvedExpression.ValueTuple vs)
                {
                    argList = vs.Item.Select(this.SharedState.EvaluateSubexpression).ToArray();
                }
                else if (arg.ResolvedType.Resolution.IsTupleType && arg.ResolvedType.Resolution is QsResolvedTypeKind.TupleType ts)
                {
                    Value evaluatedArg = this.SharedState.EvaluateSubexpression(arg);
                    IStructType tupleType = this.SharedState.CreateConcreteTupleType(ts.Item);
                    argList = this.SharedState.GetTupleElements(tupleType, evaluatedArg);
                }
                else
                {
                    argList = new Value[] { this.SharedState.EvaluateSubexpression(arg) };
                }

                var value = this.SharedState.CurrentBuilder.Call(func, argList);
                return func.Signature.ReturnType.IsVoid
                    ? this.SharedState.Constants.UnitValue
                    : value;
            }

            Value InlineSpecialization(QsSpecialization spec, TypedExpression arg)
            {
                this.SharedState.StartInlining();
                if (spec.Implementation is SpecializationImplementation.Provided impl)
                {
                    if (!spec.Signature.ArgumentType.Resolution.IsUnitType)
                    {
                        var symbolTuple = SyntaxGenerator.ArgumentTupleAsSymbolTuple(impl.Item1);
                        var binding = new QsBinding<TypedExpression>(QsBindingKind.ImmutableBinding, symbolTuple, arg);
                        this.Transformation.StatementKinds.OnVariableDeclaration(binding);
                    }
                    this.Transformation.Statements.OnScope(impl.Item2);
                }
                else
                {
                    throw new InvalidOperationException("missing specialization implementation for inlining");
                }
                return this.SharedState.StopInlining();
            }

            if (!this.SharedState.TryGetGlobalCallable(callableName, out var callable))
            {
                throw new InvalidOperationException("Q# declaration for global callable not found");
            }
            else if (GenerationContext.TryGetTargetInstructionName(callable, out var instructionName))
            {
                // deal with functions that are part of the target specific instruction set
                var targetInstruction = this.SharedState.GetOrCreateTargetInstruction(instructionName);
                return CallGlobal(targetInstruction, arg);
            }
            else if (callable.Attributes.Any(BuiltIn.MarksInlining))
            {
                // deal with global callables that need to be inlined
                var inlinedSpec = callable.Specializations.Where(spec => spec.Kind == kind).Single();
                return InlineSpecialization(inlinedSpec, arg);
            }
            else
            {
                // deal with all other global callables
                var func = this.SharedState.GetFunctionByName(callableName, kind);
                return CallGlobal(func, arg);
            }
        }

        /// <summary>
        /// Handles calls to callables that are (only) locally defined, i.e. calls to callable values.
        /// </summary>
        private Value InvokeLocalCallable(TypedExpression method, TypedExpression arg)
        {
            var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableInvoke);
            Value calledValue = this.SharedState.EvaluateSubexpression(method);
            Value argValue = this.SharedState.EvaluateSubexpression(arg);
            if (!arg.ResolvedType.Resolution.IsTupleType &&
                !arg.ResolvedType.Resolution.IsUserDefinedType &&
                !arg.ResolvedType.Resolution.IsUnitType)
            {
                // If the argument is not already of a type that results in the creation of a tuple,
                // then we need to create a tuple to store the (single) argument to be able to pass
                // it to the callable value.
                argValue = this.SharedState.CreateTuple(this.SharedState.CurrentBuilder, argValue).OpaquePointer;
            }
            else
            {
                argValue = this.SharedState.CurrentBuilder.BitCast(argValue, this.SharedState.Types.Tuple);
            }

            var returnType = method.ResolvedType.TryGetReturnType().Item;
            if (returnType.Resolution.IsUnitType)
            {
                Value resultTuple = this.SharedState.Constants.UnitValue;
                this.SharedState.CurrentBuilder.Call(func, calledValue, argValue, resultTuple);
                return this.SharedState.Constants.UnitValue;
            }
            else
            {
                IStructType resultStructType = this.SharedState.LlvmStructTypeFromQsharpType(returnType);
                TupleValue resultTuple = new TupleValue(resultStructType, this.SharedState);
                this.SharedState.CurrentBuilder.Call(func, calledValue, argValue, resultTuple.OpaquePointer);
                return returnType.Resolution.IsTupleType
                    ? resultTuple.TypedPointer
                    : this.SharedState.GetTupleElements(resultTuple.StructType, resultTuple.TypedPointer).Single();
            }
        }

        /// <summary>
        /// Evaluates the give expression and uses the runtime function with the given name to apply the corresponding functor.
        /// Does not validate the given arguments.
        /// </summary>
        /// <returns>An invalid expression</returns>
        private ResolvedExpression ApplyFunctor(string runtimeFunctionName, TypedExpression ex)
        {
            var callable = this.SharedState.EvaluateSubexpression(ex);

            // We don't keep track of access counts for callables and hence instead
            // take care here to not make unnecessary copies. We have to be pessimistic, however,
            // and make a copy for anything that would require further evaluation of the expression,
            // such as e.g. if ex is a conditional expression.

            // If ex is an identifier to a global callable then it is safe to apply the functor directly,
            // since in that case baseCallable is a freshly created callable value.
            // The same holds if ex is a partial application or another functor application.
            // Call-expression on the other hand may take a callable as argument and return the same value;
            // it is thus not save to apply the functor directly to the returned value (pointer) in that case.

            var isGlobalCallable = ex.TryAsGlobalCallable().IsValue;
            var isPartialApplication = TypedExpression.IsPartialApplication(ex.Expression);
            var isFunctorApplication = ex.Expression.IsAdjointApplication || ex.Expression.IsControlledApplication;
            var safeToModify = isGlobalCallable || isPartialApplication || isFunctorApplication;
            Value value = this.ApplyFunctor(runtimeFunctionName, callable, safeToModify);

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        /// <summary>
        /// Invokes the runtime function with the given name to apply a functor to a callable value.
        /// Unless modifyInPlace is set to true, a copy of the callable is made prior to applying the functor.
        /// </summary>
        /// <param name="runtimeFunctionName">The runtime method to invoke in order to apply the funtor</param>
        /// <param name="callable">The callable to copy (unless modifyInPlace is true) before applying the functor to it</param>
        /// <param name="modifyInPlace">If set to true, modifies and returns the given callable</param>
        /// <param name="builder">Builder to use to generate the instructions</param>
        /// <returns>The callable value to which the functor has been applied</returns>
        private Value ApplyFunctor(string runtimeFunctionName, Value callable, bool modifyInPlace = false, InstructionBuilder? builder = null)
        {
            // This method is used when applying functors when building a functor application expression
            // as well as when creating the specializations for a partial application.

            builder ??= this.SharedState.CurrentBuilder;
            if (!modifyInPlace)
            {
                // FIXME:
                // WE NEED TO INCREASE THE REFERENCE COUNT FOR THE CAPTURE TUPLE.
                // For that we need a callable_get_capture runtime function,
                // and we need to keep track of additional type information during QIR emission.
                // Dereferencing needs to recur into the capture tuple as well.

                // Since we don't track access counts for callables we need to force the copy.
                var makeCopy = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCopy);
                var forceCopy = this.SharedState.Context.CreateConstant(true);
                callable = builder.Call(makeCopy, callable, forceCopy);
                this.SharedState.ScopeMgr.RegisterValue(callable);
            }

            // CallableMakeAdjoint and CallableMakeControlled do *not* create a new value
            // but instead modify the given callable in place.
            var applyFunctor = this.SharedState.GetOrCreateRuntimeFunction(runtimeFunctionName);
            builder.Call(applyFunctor, callable);
            return callable;
        }

        // public overrides

        public override ResolvedExpression OnAddition(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Add(lhsValue, rhsValue);
            }
            else if (exType.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.FAdd(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntAdd creates a new value with reference count 1.
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntAdd);
                value = this.SharedState.CurrentBuilder.Call(adder, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else if (exType.IsString)
            {
                // The runtime function StringConcatenate creates a new value with reference count 1.
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringConcatenate);
                value = this.SharedState.CurrentBuilder.Call(adder, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else if (exType.IsArrayType)
            {
                // The runtime function ArrayConcatenate creates a new value with reference count 1 and access count 0.
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayConcatenate);
                value = this.SharedState.CurrentBuilder.Call(adder, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for addition");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnAdjointApplication(TypedExpression ex) =>
            this.ApplyFunctor(RuntimeLibrary.CallableMakeAdjoint, ex);

        public override ResolvedExpression OnArrayItem(TypedExpression arr, TypedExpression idx)
        {
            // TODO: handle multi-dimensional arrays
            var array = this.SharedState.EvaluateSubexpression(arr);
            var index = this.SharedState.EvaluateSubexpression(idx);

            Value value;
            if (idx.ResolvedType.Resolution.IsInt)
            {
                var elementType = this.SharedState.CurrentLlvmExpressionType();
                value = this.SharedState.GetArrayElement(elementType, array, index);
            }
            else if (idx.ResolvedType.Resolution.IsRange)
            {
                // Array slice creates a new array if the current access count is larger than zero.
                // The created array is instantiated with reference count 1 and access count 0.
                // If the current access count is zero, then the array may be modified in place.
                // In this case, its reference count is increased by 1.
                // Since we keep track of access counts for arrays, there is no need to force the copy.
                var forceCopy = this.SharedState.Context.CreateConstant(false);
                var sliceArray = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArraySlice1d);
                value = this.SharedState.CurrentBuilder.Call(sliceArray, array, index, forceCopy);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid index type for array item access");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBigIntLiteral(BigInteger b)
        {
            Value value;
            if (b <= long.MaxValue && b >= long.MinValue)
            {
                // The runtime function BigIntCreateI64 creates a value with reference count 1.
                var createBigInt = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateI64);
                var val = this.SharedState.Context.CreateConstant((long)b);
                value = this.SharedState.CurrentBuilder.Call(createBigInt, val);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                // The runtime function BigIntCreateArray creates a value with reference count 1.
                var createBigInt = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateArray);
                var bytes = b.ToByteArray();
                var n = this.SharedState.Context.CreateConstant(bytes.Length);
                var byteArray = ConstantArray.From(
                    this.SharedState.Context.Int8Type,
                    bytes.Select(s => this.SharedState.Context.CreateConstant(s)).ToArray());
                var zeroByteArray = this.SharedState.CurrentBuilder.BitCast(
                    byteArray,
                    this.SharedState.Context.Int8Type.CreateArrayType(0));
                value = this.SharedState.CurrentBuilder.Call(createBigInt, n, zeroByteArray);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseAnd(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.And(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntBitwiseAnd creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseAnd);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise AND");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseExclusiveOr(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Xor(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntBitwiseXor creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseXor);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise XOR");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseNot(TypedExpression ex)
        {
            Value exValue = this.SharedState.EvaluateSubexpression(ex);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                Value minusOne = this.SharedState.Context.CreateConstant(-1L);
                value = this.SharedState.CurrentBuilder.Xor(exValue, minusOne);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntBitwiseNot creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseNot);
                value = this.SharedState.CurrentBuilder.Call(func, exValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise NOT");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseOr(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Or(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntBitwiseOr creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseOr);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise OR");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBoolLiteral(bool b)
        {
            Value value = this.SharedState.Context.CreateConstant(b);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnConditionalExpression(TypedExpression cond, TypedExpression ifTrue, TypedExpression ifFalse)
        {
            static bool ExpressionIsSelfEvaluating(TypedExpression ex) =>
                ex.Expression.IsIdentifier || ex.Expression.IsBoolLiteral || ex.Expression.IsDoubleLiteral
                    || ex.Expression.IsIntLiteral || ex.Expression.IsPauliLiteral || ex.Expression.IsRangeLiteral
                    || ex.Expression.IsResultLiteral || ex.Expression.IsUnitValue;

            var condValue = this.SharedState.EvaluateSubexpression(cond);
            Value value;

            // Special case: if both values are self-evaluating (literals or simple identifiers), we can
            // do this with a select.
            if (ExpressionIsSelfEvaluating(ifTrue) && ExpressionIsSelfEvaluating(ifFalse))
            {
                var trueValue = this.SharedState.EvaluateSubexpression(ifTrue);
                var falseValue = this.SharedState.EvaluateSubexpression(ifFalse);
                value = this.SharedState.CurrentBuilder.Select(condValue, trueValue, falseValue);
            }
            else
            {
                // This is similar to conditional statements, but actually a bit simpler because there's always an else,
                // and we don't need to open a new scope. On the other hand, we do need to build a phi node in the
                // continuation block.
                var contBlock = this.SharedState.AddBlockAfterCurrent("condContinue");
                var falseBlock = this.SharedState.AddBlockAfterCurrent("condFalse");
                var trueBlock = this.SharedState.AddBlockAfterCurrent("condTrue");

                this.SharedState.CurrentBuilder.Branch(condValue, trueBlock, falseBlock);

                this.SharedState.SetCurrentBlock(trueBlock);
                var trueValue = this.SharedState.EvaluateSubexpression(ifTrue);
                this.SharedState.CurrentBuilder.Branch(contBlock);

                this.SharedState.SetCurrentBlock(falseBlock);
                var falseValue = this.SharedState.EvaluateSubexpression(ifFalse);
                this.SharedState.CurrentBuilder.Branch(contBlock);

                this.SharedState.SetCurrentBlock(contBlock);
                var phi = this.SharedState.CurrentBuilder.PhiNode(this.SharedState.CurrentLlvmExpressionType());
                phi.AddIncoming(trueValue, trueBlock);
                phi.AddIncoming(falseValue, falseBlock);
                value = phi;
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnControlledApplication(TypedExpression ex) =>
            this.ApplyFunctor(RuntimeLibrary.CallableMakeControlled, ex);

        public override ResolvedExpression OnCopyAndUpdateExpression(TypedExpression lhs, TypedExpression accEx, TypedExpression rhs)
        {
            Value CopyAndUpdateArray(ITypeRef elementType)
            {
                var originalArray = this.SharedState.EvaluateSubexpression(lhs);
                var newItemValue = this.SharedState.EvaluateSubexpression(rhs);

                // Since we keep track of access counts for arrays we always ask the runtime to create a shallow copy
                // if needed. The runtime function ArrayCopy creates a new value with reference count 1 if the current
                // access count is larger than 0, and otherwise merely increases the reference count of the array by 1.
                var createShallowCopy = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy);
                var forceCopy = this.SharedState.Context.CreateConstant(false);
                var value = this.SharedState.CurrentBuilder.Call(createShallowCopy, originalArray, forceCopy);

                // In order to accurately reflect which items are still in use and thus need to remain allocated,
                // reference counts always need to be modified recursively. However, while the reference count for
                // the value returned by ArrayCopy is set to 1 or increased by 1, it is not possible for the runtime
                // to increase the reference count of the contained items due to lacking type information.
                // In the same way that we increase the reference count when we populate an array, we hence need to
                // manually (recursively) increase the reference counts for all items.
                this.SharedState.IterateThroughArray(elementType, value, item => this.SharedState.ScopeMgr.IncreaseReferenceCount(item));
                this.SharedState.ScopeMgr.RegisterValue(value);

                void UpdateElement(Func<Value, Value> getNewItemForIndex, Value index)
                {
                    var elementPtr = this.SharedState.GetArrayElementPointer(elementType, value, index);
                    var originalElement = this.SharedState.CurrentBuilder.Load(elementType, elementPtr);
                    var newElement = getNewItemForIndex(index);

                    // Remark: Avoiding to increase and then decrease the reference count for the original item
                    // would require generating a pointer comparison that is evaluated at runtime, and I am not sure
                    // whether that would be much better.
                    this.SharedState.ScopeMgr.DecreaseReferenceCount(originalElement);
                    this.SharedState.CurrentBuilder.Store(newElement, elementPtr);
                    this.SharedState.ScopeMgr.IncreaseReferenceCount(newElement);
                }

                if (accEx.ResolvedType.Resolution.IsInt)
                {
                    var index = this.SharedState.EvaluateSubexpression(accEx);
                    UpdateElement(_ => newItemValue, index);
                }
                else if (accEx.ResolvedType.Resolution.IsRange)
                {
                    Value GetNewItemForIndex(Value index) =>
                        this.SharedState.GetArrayElement(elementType, newItemValue, index);

                    var (getStart, getStep, getEnd) = RangeItems(this.SharedState, accEx);
                    this.SharedState.IterateThroughRange(getStart(), getStep(), getEnd(), index => UpdateElement(GetNewItemForIndex, index));
                }
                else
                {
                    throw new InvalidOperationException("invalid item name in named item access");
                }

                return value;
            }

            Value CopyAndUpdateUdt(QsQualifiedName udtName)
            {
                Value GetTupleCopy(Value original)
                {
                    // Since we keep track of access counts for tuples we always ask the runtime to create a shallow copy
                    // if needed. The runtime function TupleCopy creates a new value with reference count 1 if the current
                    // access count is larger than 0, and otherwise merely increases the reference count of the tuple by 1.
                    var createShallowCopy = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCopy);
                    var forceCopy = this.SharedState.Context.CreateConstant(false);

                    // We need to cast the value to the opaque tuple type before passing it to the runtime.
                    var tuple = this.SharedState.CurrentBuilder.BitCast(original, this.SharedState.Types.Tuple);
                    return this.SharedState.CurrentBuilder.Call(createShallowCopy, tuple, forceCopy);
                }

                var originalValue = this.SharedState.EvaluateSubexpression(lhs);
                var newItemValue = this.SharedState.EvaluateSubexpression(rhs);
                var value = GetTupleCopy(originalValue);
                this.SharedState.ScopeMgr.RegisterValue(value);

                if (!this.SharedState.TryGetCustomType(udtName, out QsCustomType? udtDecl))
                {
                    throw new InvalidOperationException("Q# declaration for type not found");
                }
                else if (accEx.Expression is ResolvedExpression.Identifier id
                    && id.Item1 is Identifier.LocalVariable name
                    && this.FindNamedItem(name.Item, udtDecl.TypeItems, out var location))
                {
                    Value innerTuple = value;
                    for (int depth = 0; depth < location.Count; depth++)
                    {
                        // In order to accurately reflect which items are still in use and thus need to remain allocated,
                        // reference counts always need to be modified recursively. However, while the reference count for
                        // the value returned by TupleCopy is set to 1 or increased by 1, it is not possible for the runtime
                        // to increase the reference count of the contained items due to lacking type information.
                        // In the same way that we increase the reference count when we populate a tuple, we hence need to
                        // manually (recursively) increase the reference counts for all items.
                        var itemPointers = this.SharedState.GetTupleElementPointers(location[depth].Item2, innerTuple);
                        var itemIndex = location[depth].Item1;
                        for (var i = 0; i < itemPointers.Length; ++i)
                        {
                            if (i != itemIndex)
                            {
                                var item = this.SharedState.CurrentBuilder.Load(itemPointers[i].NativeType, itemPointers[i]);
                                this.SharedState.ScopeMgr.IncreaseReferenceCount(item);
                            }
                        }

                        if (depth == location.Count - 1)
                        {
                            this.SharedState.CurrentBuilder.Store(newItemValue, itemPointers[itemIndex]);
                            this.SharedState.ScopeMgr.IncreaseReferenceCount(newItemValue);
                        }
                        else
                        {
                            // We load the original item at that location (which is an inner tuple),
                            // and replace it with a copy of it (if a copy is needed),
                            // such that we can then proceed to modify that copy (the next inner tuple).
                            var originalItem = this.SharedState.CurrentBuilder.Load(
                                itemPointers[itemIndex].NativeType,
                                itemPointers[itemIndex]);
                            innerTuple = GetTupleCopy(originalItem);
                            this.SharedState.CurrentBuilder.Store(innerTuple, itemPointers[itemIndex]);
                        }
                    }

                    return value;
                }
                else
                {
                    throw new InvalidOperationException("invalid item name in named item access");
                }
            }

            Value value;
            if (lhs.ResolvedType.Resolution is QsResolvedTypeKind.ArrayType it)
            {
                var elementType = this.SharedState.LlvmTypeFromQsharpType(it.Item);
                value = CopyAndUpdateArray(elementType);
            }
            else if (lhs.ResolvedType.Resolution is QsResolvedTypeKind.UserDefinedType udt)
            {
                value = CopyAndUpdateUdt(udt.Item.GetFullName());
            }
            else
            {
                throw new NotSupportedException("invalid type for copy-and-update expression");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnDivision(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.SDiv(lhsValue, rhsValue);
            }
            else if (exType.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.FDiv(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntDivide creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntDivide);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for division");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnDoubleLiteral(double d)
        {
            Value value = this.SharedState.Context.CreateConstant(d);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnEquality(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            Value value;
            if (lhs.ResolvedType.Resolution.IsResult)
            {
                // Generate a call to the result equality testing function
                value = this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultEqual), lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsBool || lhs.ResolvedType.Resolution.IsInt || lhs.ResolvedType.Resolution.IsQubit
                || lhs.ResolvedType.Resolution.IsPauli)
            {
                // Works for pointers as well as integer types
                value = this.SharedState.CurrentBuilder.Compare(IntPredicate.Equal, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndEqual, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsString)
            {
                // Generate a call to the string equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringEqual);
                value = this.SharedState.CurrentBuilder.Call(compareEquality, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                // Generate a call to the bigint equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntEqual);
                value = this.SharedState.CurrentBuilder.Call(compareEquality, lhsValue, rhsValue);
            }
            else
            {
                // TODO: Equality testing for general types
                throw new NotSupportedException("invalid type for equality comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnExponentiate(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                // The exponent must be an integer that can fit into an i32.
                var powFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.IntPower);
                var exponent = this.SharedState.CurrentBuilder.IntCast(rhsValue, this.SharedState.Context.Int32Type, true);
                value = this.SharedState.CurrentBuilder.Call(powFunc, lhsValue, exponent);
            }
            else if (exType.IsDouble)
            {
                var powFunc = this.SharedState.Module.GetIntrinsicDeclaration("llvm.pow.f", this.SharedState.Types.Double);
                value = this.SharedState.CurrentBuilder.Call(powFunc, lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntPower creates a new value with reference count 1.
                // The exponent must be an integer that can fit into an i32.
                var powFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntPower);
                var exponent = this.SharedState.CurrentBuilder.IntCast(rhsValue, this.SharedState.Context.Int32Type, true);
                value = this.SharedState.CurrentBuilder.Call(powFunc, lhsValue, exponent);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for exponentiation");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnFunctionCall(TypedExpression method, TypedExpression arg)
        {
            Value value;
            var callableName = method.TryAsGlobalCallable().ValueOr(null);
            if (callableName == null)
            {
                // deal with local values; i.e. callables e.g. from partial applications or stored in local variables
                value = this.InvokeLocalCallable(method, arg);
            }
            else if (this.TryEvaluateRuntimeFunction(callableName, arg, out var evaluated))
            {
                // deal with recognized runtime functions
                value = evaluated;
            }
            else
            {
                // deal with other global callables
                value = this.InvokeGlobalCallable(callableName, QsSpecializationKind.QsBody, arg);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnGreaterThan(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            Value value;
            if (lhs.ResolvedType.Resolution.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedGreaterThan, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndGreaterThan, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreater);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnGreaterThanOrEqual(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            Value value;
            if (lhs.ResolvedType.Resolution.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedGreaterThanOrEqual, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndGreaterThanOrEqual, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreaterEq);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
        {
            Value value;
            if (sym is Identifier.LocalVariable local)
            {
                value = this.SharedState.ScopeMgr.GetNamedValue(local.Item);
            }
            else if (!(sym is Identifier.GlobalCallable globalCallable))
            {
                throw new NotSupportedException("unknown identifier");
            }
            else if (this.SharedState.TryGetGlobalCallable(globalCallable.Item, out QsCallable? callable))
            {
                // The runtime function CallableCreate creates a new value with reference count 1.
                var createCallable = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
                var wrapper = this.SharedState.GetOrCreateCallableTable(callable);
                var capture = this.SharedState.Constants.UnitValue; // nothing to capture
                value = this.SharedState.CurrentBuilder.Call(createCallable, wrapper, capture);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new InvalidOperationException("Q# declaration for global callable not found");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnInequality(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            Value value;
            if (lhs.ResolvedType.Resolution.IsResult)
            {
                // Generate a call to the result equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultEqual);
                value = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(compareEquality, lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBool || lhs.ResolvedType.Resolution.IsInt || lhs.ResolvedType.Resolution.IsQubit
                || lhs.ResolvedType.Resolution.IsPauli)
            {
                // Works for pointers as well as integer types
                value = this.SharedState.CurrentBuilder.Compare(IntPredicate.NotEqual, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndNotEqual, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsString)
            {
                // Generate a call to the string equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringEqual);
                value = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(compareEquality, lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                // Generate a call to the bigint equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntEqual);
                value = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(compareEquality, lhsValue, rhsValue));
            }
            else
            {
                // TODO: Equality testing for general types
                throw new NotSupportedException("invalid type for inequality comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnIntLiteral(long i)
        {
            Value value = this.SharedState.Context.CreateConstant(i);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLeftShift(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.ShiftLeft(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntLeftShift creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntShiftLeft);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for left shift");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLessThan(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            Value value;
            if (lhs.ResolvedType.Resolution.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedLessThan, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndLessThan, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreaterEq);
                value = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLessThanOrEqual(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            Value value;
            if (lhs.ResolvedType.Resolution.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedLessThanOrEqual, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndLessThanOrEqual, lhsValue, rhsValue);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreater);
                value = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLogicalAnd(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            Value value = this.SharedState.CurrentBuilder.And(lhsValue, rhsValue);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLogicalNot(TypedExpression ex)
        {
            // Get the Value for the expression
            Value exValue = this.SharedState.EvaluateSubexpression(ex);
            Value value = this.SharedState.CurrentBuilder.Not(exValue);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLogicalOr(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            Value value = this.SharedState.CurrentBuilder.Or(lhsValue, rhsValue);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnModulo(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.SRem(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntModulus creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntModulus);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for modulo");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnMultiplication(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Mul(lhsValue, rhsValue);
            }
            else if (exType.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.FMul(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntMultiply creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntMultiply);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for multiplication");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnNamedItem(TypedExpression ex, Identifier acc)
        {
            Value value;
            if (!(ex.ResolvedType.Resolution is QsResolvedTypeKind.UserDefinedType udt))
            {
                throw new NotSupportedException("invalid type for named item access");
            }
            else if (!this.SharedState.TryGetCustomType(udt.Item.GetFullName(), out var udtDecl))
            {
                throw new InvalidOperationException("Q# declaration for type not found");
            }
            else if (acc is Identifier.LocalVariable itemName && this.FindNamedItem(itemName.Item, udtDecl.TypeItems, out var location))
            {
                value = this.SharedState.EvaluateSubexpression(ex);
                for (int i = 0; i < location.Count; i++)
                {
                    value = this.SharedState.GetTupleElement(location[i].Item2, value, location[i].Item1);
                }

                this.SharedState.ScopeMgr.IncreaseReferenceCount(value);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new InvalidOperationException("invalid item name in named item access");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnNegative(TypedExpression ex)
        {
            Value exValue = this.SharedState.EvaluateSubexpression(ex);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Neg(exValue);
            }
            else if (exType.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.FNeg(exValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntNegative creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntNegate);
                value = this.SharedState.CurrentBuilder.Call(func, exValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for negative");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnNewArray(ResolvedType elementType, TypedExpression idx)
        {
            // TODO: new multi-dimensional arrays
            var array = new ArrayValue(
                this.SharedState.EvaluateSubexpression(idx),
                this.SharedState.LlvmTypeFromQsharpType(elementType),
                this.SharedState);

            this.SharedState.ValueStack.Push(array.OpaquePointer);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnOperationCall(TypedExpression method, TypedExpression arg)
        {
            (TypedExpression, bool, int) StripModifiers(TypedExpression m, bool a, int c) =>
                m.Expression switch
                {
                    ResolvedExpression.AdjointApplication adj => StripModifiers(adj.Item, !a, c),
                    ResolvedExpression.ControlledApplication con => StripModifiers(con.Item, a, c + 1),
                    _ => (m, a, c),
                };

            TypedExpression BuildInnerArg(TypedExpression arg, int controlledCount)
            {
                // throws an InvalidOperationException if the remainingArg is not a tuple with two items
                (TypedExpression, TypedExpression) TupleItems(TypedExpression remainingArg) =>
                    (remainingArg.Expression is ResolvedExpression.ValueTuple tuple && tuple.Item.Length == 2)
                    ? (tuple.Item[0], tuple.Item[1])
                    : throw new InvalidOperationException("control count is inconsistent with the shape of the argument tuple");

                if (controlledCount < 2)
                {
                    // no need to concatenate the controlled arguments
                    return arg;
                }

                // The arglist will be a 2-tuple with the first element an array of qubits and the second element
                // a 2-tuple containing an array of qubits and another tuple -- possibly with more nesting levels
                var (controls, remainingArg) = TupleItems(arg);
                while (--controlledCount > 0)
                {
                    var (innerControls, innerArg) = TupleItems(remainingArg);
                    controls = SyntaxGenerator.AddExpressions(controls.ResolvedType.Resolution, controls, innerControls);
                    remainingArg = innerArg;
                }

                return SyntaxGenerator.TupleLiteral(new[] { controls, remainingArg });
            }

            static QsSpecializationKind GetSpecializationKind(bool isAdjoint, bool isControlled) =>
                isAdjoint && isControlled ? QsSpecializationKind.QsControlledAdjoint :
                isControlled ? QsSpecializationKind.QsControlled :
                isAdjoint ? QsSpecializationKind.QsAdjoint :
                QsSpecializationKind.QsBody;

            // We avoid constructing a callable value when functors are applied to global callables.
            var (innerCallable, isAdjoint, controlledCount) = StripModifiers(method, false, 0);

            Value value;
            var callableName = innerCallable.TryAsGlobalCallable().ValueOr(null);
            if (callableName == null)
            {
                // deal with local values; i.e. callables e.g. from partial applications or stored in local variables
                value = this.InvokeLocalCallable(method, arg);
            }
            else
            {
                // deal with global callables
                var innerArg = BuildInnerArg(arg, controlledCount);
                var kind = GetSpecializationKind(isAdjoint, controlledCount > 0);
                value = this.InvokeGlobalCallable(callableName, kind, innerArg);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnPartialApplication(TypedExpression method, TypedExpression arg)
        {
            PartialApplicationArgument BuildPartialArgList(ResolvedType argType, TypedExpression arg, List<ResolvedType> remainingArgs, List<Value> capturedValues)
            {
                // We need argType because missing argument items have MissingType, rather than the actual type.
                if (arg.Expression.IsMissingExpr)
                {
                    remainingArgs.Add(argType);
                    var itemType = this.SharedState.LlvmTypeFromQsharpType(argType);
                    return new InnerArg(this.SharedState, itemType, remainingArgs.Count - 1);
                }
                else if (arg.Expression is ResolvedExpression.ValueTuple tuple
                    && argType.Resolution is QsResolvedTypeKind.TupleType types)
                {
                    var items = types.Item.Zip(tuple.Item, (t, v) => BuildPartialArgList(t, v, remainingArgs, capturedValues));
                    return new InnerTuple(this.SharedState, argType, items);
                }
                else
                {
                    capturedValues.Add(this.SharedState.EvaluateSubexpression(arg));
                    return new InnerCapture(this.SharedState, capturedValues.Count - 1);
                }
            }

            IrFunction BuildLiftedSpecialization(string name, QsSpecializationKind kind, IStructType captureType, IStructType parArgsStruct, PartialApplicationArgument partialArgs)
            {
                Value ApplyFunctors(Value innerCallable, InstructionBuilder builder)
                {
                    if (kind == QsSpecializationKind.QsBody)
                    {
                        return innerCallable;
                    }
                    else if (kind == QsSpecializationKind.QsAdjoint)
                    {
                        return this.ApplyFunctor(RuntimeLibrary.CallableMakeAdjoint, innerCallable, modifyInPlace: false, builder);
                    }
                    else if (kind == QsSpecializationKind.QsControlled)
                    {
                        return this.ApplyFunctor(RuntimeLibrary.CallableMakeControlled, innerCallable, modifyInPlace: false, builder);
                    }
                    else if (kind == QsSpecializationKind.QsControlledAdjoint)
                    {
                        innerCallable = this.ApplyFunctor(RuntimeLibrary.CallableMakeAdjoint, innerCallable, modifyInPlace: false, builder);
                        return this.ApplyFunctor(RuntimeLibrary.CallableMakeControlled, innerCallable, modifyInPlace: true, builder);
                    }
                    else
                    {
                        throw new NotImplementedException("unknown specialization");
                    }
                }

                var funcName = GenerationContext.FunctionWrapperName(new QsQualifiedName("Lifted", name), kind);
                IrFunction func = this.SharedState.Module.CreateFunction(funcName, this.SharedState.Types.FunctionSignature);

                func.Parameters[0].Name = "capture-tuple";
                func.Parameters[1].Name = "arg-tuple";
                func.Parameters[2].Name = "result-tuple";
                var entry = func.AppendBasicBlock("entry");

                this.SharedState.ScopeMgr.OpenScope();
                InstructionBuilder builder = new InstructionBuilder(entry);

                Value capturePointer = builder.BitCast(func.Parameters[0], captureType.CreatePointerType());
                TupleValue BuildControlledInnerArgument(ITypeRef paArgType)
                {
                    // The argument tuple given to the controlled version of the partial application consists of the array of control qubits
                    // as well as a tuple with the remaining arguments for the partial application.
                    // We need to cast the corresponding function parameter to the appropriate type and load both of these items.
                    var ctlPaArgsStruct = this.SharedState.Types.CreateConcreteTupleType(this.SharedState.Types.Array, paArgType);
                    var ctlPaArgs = builder.BitCast(func.Parameters[1], ctlPaArgsStruct.CreatePointerType());
                    var ctlPaArgItems = this.SharedState.GetTupleElements(ctlPaArgsStruct, ctlPaArgs, builder);

                    // We then create and populate the complete argument tuple for the controlled specialization of the inner callable.
                    // The tuple consists of the control qubits and the combined tuple of captured values and the arguments given to the partial application.
                    var innerArgs = partialArgs.BuildItem(builder, capturePointer, ctlPaArgItems[1]);
                    return this.SharedState.CreateTuple(builder, ctlPaArgItems[0], innerArgs);
                }

                Value innerArg;
                if (kind == QsSpecializationKind.QsControlled || kind == QsSpecializationKind.QsControlledAdjoint)
                {
                    // Deal with the extra control qubit arg for controlled and controlled-adjoint
                    // We special case if the base specialization only takes a single parameter and don't create the sub-tuple in this case.
                    innerArg = BuildControlledInnerArgument(
                        parArgsStruct.Members.Count == 1
                        ? parArgsStruct.Members[0]
                        : parArgsStruct.CreatePointerType())
                    .OpaquePointer;
                }
                else
                {
                    var parArgsPointer = builder.BitCast(func.Parameters[1], parArgsStruct.CreatePointerType());
                    var typedInnerArg = partialArgs.BuildItem(builder, capturePointer, parArgsPointer);
                    innerArg = this.SharedState.Types.IsTupleType(typedInnerArg.NativeType)
                        ? builder.BitCast(typedInnerArg, this.SharedState.Types.Tuple)
                        : this.SharedState.CreateTuple(builder, typedInnerArg).OpaquePointer;
                }

                var invokeCallable = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableInvoke);
                var innerCallable = this.SharedState.GetTupleElement(captureType, capturePointer, 0, builder);
                builder.Call(invokeCallable, ApplyFunctors(innerCallable, builder), innerArg, func.Parameters[2]);

                builder.Return();
                this.SharedState.ScopeMgr.CloseScope(isTerminated: false, builder);

                return func;
            }

            var liftedName = this.SharedState.GenerateUniqueName("PartialApplication");
            ResolvedType CallableArgumentType(ResolvedType t) => t.Resolution switch
            {
                QsResolvedTypeKind.Function paf => paf.Item1,
                QsResolvedTypeKind.Operation pao => pao.Item1.Item1,
                _ => throw new InvalidOperationException("expecting an operation or function type")
            };

            // Figure out the inputs to the resulting callable based on the signature of the partial application expression
            var partialArgType = this.SharedState.LlvmStructTypeFromQsharpType(
                CallableArgumentType(this.SharedState.ExpressionTypeStack.Peek()));

            // Argument type of the callable that is partially applied
            var innerArgType = CallableArgumentType(method.ResolvedType);

            // Create the capture tuple, which contains the inner callable as the first item and
            // construct the mapping to compine captured arguments with the arguments for the partial application
            var captured = new List<Value>();
            captured.Add(this.SharedState.EvaluateSubexpression(method));
            var rebuild = BuildPartialArgList(innerArgType, arg, new List<ResolvedType>(), captured);
            var capture = this.SharedState.CreateTuple(this.SharedState.CurrentBuilder, captured.ToArray());

            // Create the lifted specialization implementation(s)
            // First, figure out which ones we need to create
            var kinds = new HashSet<QsSpecializationKind>
            {
                QsSpecializationKind.QsBody
            };
            if (method.ResolvedType.Resolution is QsResolvedTypeKind.Operation op
                && op.Item2.Characteristics.SupportedFunctors.IsValue)
            {
                var functors = op.Item2.Characteristics.SupportedFunctors.Item;
                if (functors.Contains(QsFunctor.Adjoint))
                {
                    kinds.Add(QsSpecializationKind.QsAdjoint);
                }
                if (functors.Contains(QsFunctor.Controlled))
                {
                    kinds.Add(QsSpecializationKind.QsControlled);
                    if (functors.Contains(QsFunctor.Adjoint))
                    {
                        kinds.Add(QsSpecializationKind.QsControlledAdjoint);
                    }
                }
            }

            // Now create our specializations
            var specializations = new Constant[4];
            for (var index = 0; index < 4; index++)
            {
                var kind = GenerationContext.FunctionArray[index];
                if (kinds.Contains(kind))
                {
                    specializations[index] = BuildLiftedSpecialization(liftedName, kind, capture.StructType, partialArgType, rebuild);
                }
                else
                {
                    specializations[index] = Constant.ConstPointerToNullFor(this.SharedState.Types.FunctionSignature.CreatePointerType());
                }
            }

            // Build the callable table
            var array = ConstantArray.From(this.SharedState.Types.FunctionSignature.CreatePointerType(), specializations);
            var table = this.SharedState.Module.AddGlobal(array.NativeType, true, Linkage.DllExport, array, liftedName);

            // Create the callable and make sure we inject/queue the functions to manage the reference counts
            var createCallable = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
            var value = this.SharedState.CurrentBuilder.Call(createCallable, table, capture.OpaquePointer);
            this.SharedState.ScopeMgr.IncreaseReferenceCount(capture.OpaquePointer);
            this.SharedState.ScopeMgr.RegisterValue(value);

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnPauliLiteral(QsPauli p)
        {
            Value LoadPauli(Value pauli) =>
                this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Pauli, pauli);

            Value value;
            if (p.IsPauliI)
            {
                value = LoadPauli(this.SharedState.Constants.PauliI);
            }
            else if (p.IsPauliX)
            {
                value = LoadPauli(this.SharedState.Constants.PauliX);
            }
            else if (p.IsPauliY)
            {
                value = LoadPauli(this.SharedState.Constants.PauliY);
            }
            else if (p.IsPauliZ)
            {
                value = LoadPauli(this.SharedState.Constants.PauliZ);
            }
            else
            {
                throw new NotSupportedException("unknown value for Pauli");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnRangeLiteral(TypedExpression lhs, TypedExpression rhs)
        {
            Value start;
            Value step;
            switch (lhs.Expression)
            {
                case ResolvedExpression.RangeLiteral lit:
                    start = this.SharedState.EvaluateSubexpression(lit.Item1);
                    step = this.SharedState.EvaluateSubexpression(lit.Item2);
                    break;
                default:
                    start = this.SharedState.EvaluateSubexpression(lhs);
                    step = this.SharedState.Context.CreateConstant(1L);
                    break;
            }
            Value end = this.SharedState.EvaluateSubexpression(rhs);

            Value rangePtr = this.SharedState.Constants.EmptyRange;
            Value value = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Range, rangePtr);
            value = this.SharedState.CurrentBuilder.InsertValue(value, start, 0);
            value = this.SharedState.CurrentBuilder.InsertValue(value, step, 1);
            value = this.SharedState.CurrentBuilder.InsertValue(value, end, 2);

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnResultLiteral(QsResult r)
        {
            var valuePtr = r.IsOne ? this.SharedState.Constants.ResultOne : this.SharedState.Constants.ResultZero;
            var value = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Result, valuePtr);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnRightShift(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.ArithmeticShiftRight(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntRightShift creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntShiftRight);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for right shift");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnStringLiteral(string str, ImmutableArray<TypedExpression> exs)
        {
            static (int, int, int) FindNextExpression(string s, int start)
            {
                while (true)
                {
                    var i = s.IndexOf('{', start);
                    if (i < 0)
                    {
                        return (-1, s.Length, -1);
                    }
                    else if ((i == start) || (s[i - 1] != '\\'))
                    {
                        var j = s.IndexOf('}', i + 1);
                        if (j < 0)
                        {
                            throw new FormatException("Missing } in interpolated string");
                        }
                        var n = int.Parse(s[(i + 1)..j]);
                        return (i, j + 1, n);
                    }
                    start = i + 1;
                }
            }

            // Creates a string value that needs to be queued for unreferencing.
            Value CreateConstantString(string s)
            {
                // Deal with escape sequences: \{, \\, \n, \r, \t, \". This is not an efficient
                // way to do this, but it's simple and clear, and strings are uncommon in Q#.
                var cleanStr = s.Replace("\\{", "{").Replace("\\\\", "\\").Replace("\\n", "\n")
                    .Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"");
                var constantString = cleanStr.Length > 0
                    ? this.SharedState.Context.CreateConstantString(cleanStr)
                    : this.SharedState.Types.String.GetNullValue();
                var zeroLengthString = this.SharedState.CurrentBuilder.BitCast(
                    constantString,
                    this.SharedState.Context.Int8Type.CreateArrayType(0));
                var n = this.SharedState.Context.CreateConstant(cleanStr.Length);
                var createString = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringCreate);
                return this.SharedState.CurrentBuilder.Call(createString, n, zeroLengthString);
            }

            // Creates a string value that needs to be queued for unreferencing.
            Value ExpressionToString(TypedExpression ex)
            {
                // Creates a string value that needs to be queued for unreferencing.
                Value SimpleToString(TypedExpression ex, string rtFuncName)
                {
                    var exValue = this.SharedState.EvaluateSubexpression(ex);
                    var createString = this.SharedState.GetOrCreateRuntimeFunction(rtFuncName);
                    return this.SharedState.CurrentBuilder.Call(createString, exValue);
                }

                var ty = ex.ResolvedType.Resolution;
                if (ty.IsString)
                {
                    var addReference = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringReference);
                    var value = this.SharedState.EvaluateSubexpression(ex);
                    this.SharedState.CurrentBuilder.Call(addReference, value);
                    return value;
                }
                else if (ty.IsBigInt)
                {
                    return SimpleToString(ex, RuntimeLibrary.BigIntToString);
                }
                else if (ty.IsBool)
                {
                    return SimpleToString(ex, RuntimeLibrary.BoolToString);
                }
                else if (ty.IsInt)
                {
                    return SimpleToString(ex, RuntimeLibrary.IntToString);
                }
                else if (ty.IsResult)
                {
                    return SimpleToString(ex, RuntimeLibrary.ResultToString);
                }
                else if (ty.IsPauli)
                {
                    return SimpleToString(ex, RuntimeLibrary.PauliToString);
                }
                else if (ty.IsQubit)
                {
                    return SimpleToString(ex, RuntimeLibrary.QubitToString);
                }
                else if (ty.IsRange)
                {
                    return SimpleToString(ex, RuntimeLibrary.RangeToString);
                }
                else if (ty.IsDouble)
                {
                    return SimpleToString(ex, RuntimeLibrary.DoubleToString);
                }
                else if (ty.IsFunction)
                {
                    return CreateConstantString("<function>");
                }
                else if (ty.IsOperation)
                {
                    return CreateConstantString("<operation>");
                }
                else if (ty.IsUnitType)
                {
                    return CreateConstantString("()");
                }
                else if (ty.IsArrayType)
                {
                    // TODO: Do something better for array-to-string
                    return CreateConstantString("[...]");
                }
                else if (ty.IsTupleType)
                {
                    // TODO: Do something better for tuple-to-string
                    return CreateConstantString("(...)");
                }
                else if (ty is QsResolvedTypeKind.UserDefinedType udt)
                {
                    // TODO: Do something better for UDT-to-string
                    var udtName = udt.Item.Name;
                    return CreateConstantString(udtName + "(...)");
                }
                else
                {
                    throw new NotSupportedException("unkown type for expression in conversion to string");
                }
            }

            // Creates a new string with reference count 1 that needs to be queued for unreferencing
            // and contains the concatenation of both values. Both both arguments are unreferenced.
            Value DoAppend(Value? curr, Value next)
            {
                if (curr == null)
                {
                    return next;
                }

                // The runtime function StringConcatenate creates a new value with reference count 1.
                var concatenate = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringConcatenate);
                var unreference = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringUnreference);
                var app = this.SharedState.CurrentBuilder.Call(concatenate, curr, next);
                this.SharedState.CurrentBuilder.Call(unreference, curr);
                this.SharedState.CurrentBuilder.Call(unreference, next);
                return app;
            }

            Value value;
            if (exs.IsEmpty)
            {
                value = CreateConstantString(str);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                // Compiled interpolated strings look like <text>{<int>}<text>...
                // Our basic pattern is to scan for the next '{', append the intervening text if any
                // as a constant string, scan for the closing '}', parse out the integer in between,
                // evaluate the corresponding expression, append it, and keep going.
                // We do have to be a little careful because we can't just look for '{', we have to
                // make sure we skip escaped braces -- "\{".
                Value? current = null;
                var offset = 0;
                while (offset < str.Length)
                {
                    var (end, next, index) = FindNextExpression(str, offset);
                    if (end < 0)
                    {
                        var last = CreateConstantString(str[offset..]);
                        current = DoAppend(current, last);
                        break;
                    }
                    else
                    {
                        if (end > offset)
                        {
                            var last = CreateConstantString(str[offset..end]);
                            current = DoAppend(current, last);
                        }
                        if (index >= 0)
                        {
                            var exString = ExpressionToString(exs[index]);
                            current = DoAppend(current, exString);
                        }

                        offset = next;
                    }
                }

                value = current ?? CreateConstantString("");
                this.SharedState.ScopeMgr.RegisterValue(value);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnSubtraction(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);
            var exType = this.SharedState.CurrentExpressionType();

            Value value;
            if (exType.IsInt)
            {
                value = this.SharedState.CurrentBuilder.Sub(lhsValue, rhsValue);
            }
            else if (exType.IsDouble)
            {
                value = this.SharedState.CurrentBuilder.FSub(lhsValue, rhsValue);
            }
            else if (exType.IsBigInt)
            {
                // The runtime function BigIntSubtract creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntSubtract);
                value = this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for subtraction");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnUnitValue()
        {
            Value value = this.SharedState.Constants.UnitValue;
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnValueArray(ImmutableArray<TypedExpression> vs)
        {
            // TODO: handle multi-dimensional arrays
            var elementType = this.SharedState.LlvmTypeFromQsharpType(vs[0].ResolvedType);
            var value = new ArrayValue((uint)vs.Length, elementType, this.SharedState).OpaquePointer;

            long idx = 0;
            foreach (var element in vs)
            {
                var index = this.SharedState.Context.CreateConstant(idx);
                var elementPointer = this.SharedState.GetArrayElementPointer(elementType, value, index);
                var elementValue = this.SharedState.EvaluateSubexpression(element);
                this.SharedState.CurrentBuilder.Store(elementValue, elementPointer);
                this.SharedState.ScopeMgr.IncreaseReferenceCount(elementValue);
                idx++;
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnValueTuple(ImmutableArray<TypedExpression> vs)
        {
            var items = vs.Select(v => this.SharedState.EvaluateSubexpression(v)).ToArray();
            var tuple = this.SharedState.CreateTuple(this.SharedState.CurrentBuilder, items);
            this.SharedState.ValueStack.Push(tuple.TypedPointer);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnUnwrapApplication(TypedExpression ex)
        {
            // Since we simply represent user defined types as tuples, we don't need to do anything
            // except pushing the value on the value stack unless the tuples contains a single item,
            // in which case we need to remove the tuple wrapping.
            Value value = this.SharedState.EvaluateSubexpression(ex);
            var udt = ex.ResolvedType.Resolution as QsResolvedTypeKind.UserDefinedType;
            if (udt == null)
            {
                throw new NotSupportedException("invalid type for unwrap operator");
            }
            else if (!this.SharedState.TryGetCustomType(udt.Item.GetFullName(), out var udtDecl))
            {
                throw new InvalidOperationException("Q# declaration for type not found");
            }
            else if (!udtDecl.Type.Resolution.IsTupleType)
            {
                var elementType = this.SharedState.LlvmTypeFromQsharpType(udtDecl.Type);
                value = this.SharedState.GetTupleElement(
                     this.SharedState.Types.CreateConcreteTupleType(elementType),
                     value,
                     0);
            }

            this.SharedState.ScopeMgr.IncreaseReferenceCount(value);
            this.SharedState.ScopeMgr.RegisterValue(value);

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }
    }
}
