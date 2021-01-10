// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
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

            public abstract IValue BuildItem(TupleValue capture, IValue parArgs);
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
            public override IValue BuildItem(TupleValue capture, IValue parArgs) =>
                capture.GetTupleElement(this.CaptureIndex);
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
            public override IValue BuildItem(TupleValue capture, IValue paArgs) =>
                // parArgs.NativeType == this.ItemType may occur if we have an item of user defined type (represented as a tuple)
                (paArgs is TupleValue paArgsTuple) && paArgsTuple.StructType.CreatePointerType() != this.ItemType
                    ? paArgsTuple.GetTupleElement(this.ArgIndex)
                    : paArgs;
        }

        private class InnerTuple : PartialApplicationArgument
        {
            public readonly ResolvedType TupleType;
            public readonly ImmutableArray<PartialApplicationArgument> Items;

            public InnerTuple(GenerationContext sharedState, ResolvedType tupleType, IEnumerable<PartialApplicationArgument> items)
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
            public override IValue BuildItem(TupleValue capture, IValue parArgs)
            {
                var items = this.Items.Select(item => item.BuildItem(capture, parArgs)).ToArray();
                var tuple = this.SharedState.Values.CreateTuple(items);
                return tuple;
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
        /// <param name="rangeEx">The range expression for which to create the access functions</param>
        /// <returns>
        /// Three functions to access the start, step, and end of a range.
        /// The function to access the step may return null if the given range does not specify the step.
        /// In that case, the step size defaults to be 1L.
        /// </returns>
        internal static (Func<Value> GetStart, Func<Value?> GetStep, Func<Value> GetEnd) RangeItems(GenerationContext sharedState, TypedExpression rangeEx)
        {
            Func<Value> startValue;
            Func<Value?> stepValue;
            Func<Value> endValue;
            if (rangeEx.Expression is ResolvedExpression.RangeLiteral rlit)
            {
                if (rlit.Item1.Expression is ResolvedExpression.RangeLiteral rlitInner)
                {
                    startValue = () => sharedState.EvaluateSubexpression(rlitInner.Item1).Value;
                    stepValue = () => sharedState.EvaluateSubexpression(rlitInner.Item2).Value;
                }
                else
                {
                    startValue = () => sharedState.EvaluateSubexpression(rlit.Item1).Value;
                    stepValue = () => null;
                }

                // Item2 is always the end. Either Item1 is the start and 1 is the step,
                // or Item1 is a range expression itself, with Item1 the start and Item2 the step.
                endValue = () => sharedState.EvaluateSubexpression(rlit.Item2).Value;
            }
            else
            {
                var range = sharedState.EvaluateSubexpression(rangeEx).Value;
                startValue = () => sharedState.CurrentBuilder.ExtractValue(range, 0);
                stepValue = () => sharedState.CurrentBuilder.ExtractValue(range, 1);
                endValue = () => sharedState.CurrentBuilder.ExtractValue(range, 2);
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
        private bool FindNamedItem(string name, QsTuple<QsTypeItem> typeItems, out List<int> itemLocation)
        {
            bool FindNamedItem(QsTuple<QsTypeItem> items, List<int> location)
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
                                location.Add(i);
                                return true;
                            }
                        }
                        break;
                }
                return false;
            }

            itemLocation = new List<int>();
            var found = FindNamedItem(typeItems, itemLocation);
            itemLocation.Reverse();
            return found;
        }

        /// <returns>
        /// The result of the evaluation if the given name matches one of the recognized runtime functions,
        /// and null otherwise.
        /// </returns>
        private bool TryEvaluateRuntimeFunction(QsQualifiedName name, TypedExpression arg, [MaybeNullWhen(false)] out IValue evaluated)
        {
            var intType = ResolvedType.New(QsResolvedTypeKind.Int);
            var rangeType = ResolvedType.New(QsResolvedTypeKind.Range);

            if (name.Equals(BuiltIn.Length.FullName))
            {
                var arrayArg = (ArrayValue)this.SharedState.EvaluateSubexpression(arg);
                evaluated = this.SharedState.Values.FromSimpleValue(arrayArg.Length, intType);
                return true;
            }
            else if (name.Equals(BuiltIn.RangeStart.FullName))
            {
                var (getStart, _, _) = RangeItems(this.SharedState, arg);
                evaluated = this.SharedState.Values.FromSimpleValue(getStart(), intType);
                return true;
            }
            else if (name.Equals(BuiltIn.RangeStep.FullName))
            {
                var (_, getStep, _) = RangeItems(this.SharedState, arg);
                var res = getStep() ?? this.SharedState.Context.CreateConstant(1L);
                evaluated = this.SharedState.Values.FromSimpleValue(res, intType);
                return true;
            }
            else if (name.Equals(BuiltIn.RangeEnd))
            {
                var (_, _, getEnd) = RangeItems(this.SharedState, arg);
                evaluated = this.SharedState.Values.FromSimpleValue(getEnd(), intType);
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
                Value reversed = this.SharedState.CurrentBuilder.Load(
                    this.SharedState.Types.Range,
                    this.SharedState.Constants.EmptyRange);
                reversed = this.SharedState.CurrentBuilder.InsertValue(reversed, newStart, 0u);
                reversed = this.SharedState.CurrentBuilder.InsertValue(reversed, this.SharedState.CurrentBuilder.Neg(step), 1u);
                reversed = this.SharedState.CurrentBuilder.InsertValue(reversed, start, 2u);
                evaluated = this.SharedState.Values.From(reversed, rangeType);
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
        private IValue InvokeGlobalCallable(QsQualifiedName callableName, QsSpecializationKind kind, TypedExpression arg)
        {
            IValue CallGlobal(IrFunction func, TypedExpression arg, ResolvedType returnType)
            {
                IEnumerable<IValue> args;
                if (arg.ResolvedType.Resolution.IsUnitType)
                {
                    args = Enumerable.Empty<IValue>();
                }
                else if (arg.ResolvedType.Resolution.IsTupleType && arg.Expression is ResolvedExpression.ValueTuple vs)
                {
                    args = vs.Item.Select(this.SharedState.EvaluateSubexpression);
                }
                else if (arg.ResolvedType.Resolution.IsTupleType && arg.ResolvedType.Resolution is QsResolvedTypeKind.TupleType ts)
                {
                    var evaluatedArg = (TupleValue)this.SharedState.EvaluateSubexpression(arg);
                    args = evaluatedArg.GetTupleElements();
                }
                else
                {
                    args = new[] { this.SharedState.EvaluateSubexpression(arg) };
                }

                var argList = args.Select(a => a.Value).ToArray();
                var res = this.SharedState.CurrentBuilder.Call(func, argList);
                if (func.Signature.ReturnType.IsVoid)
                {
                    return this.SharedState.Values.Unit;
                }

                var value = this.SharedState.Values.From(res, returnType);
                this.SharedState.ScopeMgr.RegisterValue(value);
                return value;
            }

            IValue InlineSpecialization(QsSpecialization spec, TypedExpression arg)
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
                return CallGlobal(targetInstruction, arg, callable.Signature.ReturnType);
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
                return CallGlobal(func, arg, callable.Signature.ReturnType);
            }
        }

        /// <summary>
        /// Handles calls to callables that are (only) locally defined, i.e. calls to callable values.
        /// </summary>
        private IValue InvokeLocalCallable(TypedExpression method, TypedExpression arg)
        {
            var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableInvoke);
            var calledValue = this.SharedState.EvaluateSubexpression(method);
            var argValue = this.SharedState.EvaluateSubexpression(arg);
            if (!arg.ResolvedType.Resolution.IsTupleType &&
                !arg.ResolvedType.Resolution.IsUserDefinedType &&
                !arg.ResolvedType.Resolution.IsUnitType)
            {
                // If the argument is not already of a type that results in the creation of a tuple,
                // then we need to create a tuple to store the (single) argument to be able to pass
                // it to the callable value.
                argValue = this.SharedState.Values.CreateTuple(argValue);
            }

            var callableArg = argValue.QSharpType.Resolution.IsUnitType
                ? this.SharedState.Values.Unit.Value
                : ((TupleValue)argValue).OpaquePointer;
            var returnType = method.ResolvedType.TryGetReturnType().Item;

            if (returnType.Resolution.IsUnitType)
            {
                Value resultTuple = this.SharedState.Constants.UnitValue;
                this.SharedState.CurrentBuilder.Call(func, calledValue.Value, callableArg, resultTuple);
                return this.SharedState.Values.Unit;
            }
            else
            {
                var resElementTypes = returnType.Resolution is QsResolvedTypeKind.TupleType elementTypes
                    ? elementTypes.Item
                    : ImmutableArray.Create(returnType);
                TupleValue resultTuple = this.SharedState.Values.CreateTuple(resElementTypes);
                this.SharedState.CurrentBuilder.Call(func, calledValue.Value, callableArg, resultTuple.OpaquePointer);
                return returnType.Resolution.IsTupleType
                    ? resultTuple
                    : resultTuple.GetTupleElements().Single();
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
            var value = this.ApplyFunctor(runtimeFunctionName, callable, safeToModify);

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
        /// <returns>The callable value to which the functor has been applied</returns>
        private IValue ApplyFunctor(string runtimeFunctionName, IValue callable, bool modifyInPlace = false)
        {
            // This method is used when applying functors when building a functor application expression
            // as well as when creating the specializations for a partial application.

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
                var modified = this.SharedState.CurrentBuilder.Call(makeCopy, callable.Value, forceCopy);
                callable = this.SharedState.Values.FromCallable(modified, callable.QSharpType);
                this.SharedState.ScopeMgr.RegisterValue(callable);
            }

            // CallableMakeAdjoint and CallableMakeControlled do *not* create a new value
            // but instead modify the given callable in place.
            var applyFunctor = this.SharedState.GetOrCreateRuntimeFunction(runtimeFunctionName);
            this.SharedState.CurrentBuilder.Call(applyFunctor, callable.Value);
            return callable;
        }

        // public overrides

        public override ResolvedExpression OnAddition(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Add(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FAdd(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntAdd creates a new value with reference count 1.
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntAdd);
                var res = this.SharedState.CurrentBuilder.Call(adder, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else if (exType.Resolution.IsString)
            {
                // The runtime function StringConcatenate creates a new value with reference count 1.
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringConcatenate);
                var res = this.SharedState.CurrentBuilder.Call(adder, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else if (exType.Resolution is QsResolvedTypeKind.ArrayType elementType)
            {
                // The runtime function ArrayConcatenate creates a new value with reference count 1 and access count 0.
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayConcatenate);
                var res = this.SharedState.CurrentBuilder.Call(adder, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromArray(res, elementType.Item);
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
            var array = (ArrayValue)this.SharedState.EvaluateSubexpression(arr);
            var index = this.SharedState.EvaluateSubexpression(idx);
            var elementType = arr.ResolvedType.Resolution is QsResolvedTypeKind.ArrayType arrElementType
                ? arrElementType.Item
                : throw new InvalidOperationException("expecting an array in array item access");

            IValue value;
            if (idx.ResolvedType.Resolution.IsInt)
            {
                value = array.GetArrayElement(index.Value);
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
                var slice = this.SharedState.CurrentBuilder.Call(sliceArray, array.Value, index.Value, forceCopy);
                value = this.SharedState.Values.FromArray(slice, elementType);
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
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (b <= long.MaxValue && b >= long.MinValue)
            {
                // The runtime function BigIntCreateI64 creates a value with reference count 1.
                var createBigInt = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateI64);
                var val = this.SharedState.Context.CreateConstant((long)b);
                var res = this.SharedState.CurrentBuilder.Call(createBigInt, val);
                value = this.SharedState.Values.From(res, exType);
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
                var byteArrayPointer = this.SharedState.CurrentBuilder.GetElementPtr(
                    this.SharedState.Context.Int8Type.CreateArrayType((uint)bytes.Length),
                    byteArray,
                    new[] { this.SharedState.Context.CreateConstant(0) });
                var zeroByteArray = this.SharedState.CurrentBuilder.BitCast(
                    byteArrayPointer,
                    this.SharedState.Types.DataArrayPointer);
                var res = this.SharedState.CurrentBuilder.Call(createBigInt, n, zeroByteArray);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseAnd(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.And(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntBitwiseAnd creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseAnd);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise AND");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseExclusiveOr(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Xor(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntBitwiseXor creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseXor);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
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
            var exValue = this.SharedState.EvaluateSubexpression(ex);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                Value minusOne = this.SharedState.Context.CreateConstant(-1L);
                var res = this.SharedState.CurrentBuilder.Xor(exValue.Value, minusOne);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntBitwiseNot creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseNot);
                var res = this.SharedState.CurrentBuilder.Call(func, exValue.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise NOT");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseOr(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Or(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntBitwiseOr creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseOr);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
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
            var constant = this.SharedState.Context.CreateConstant(b);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.FromSimpleValue(constant, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnConditionalExpression(TypedExpression condEx, TypedExpression ifTrueEx, TypedExpression ifFalseEx)
        {
            static bool ExpressionIsSelfEvaluating(TypedExpression ex) =>
                ex.Expression.IsIdentifier || ex.Expression.IsBoolLiteral || ex.Expression.IsDoubleLiteral
                    || ex.Expression.IsIntLiteral || ex.Expression.IsPauliLiteral || ex.Expression.IsRangeLiteral
                    || ex.Expression.IsResultLiteral || ex.Expression.IsUnitValue;

            var cond = this.SharedState.EvaluateSubexpression(condEx);
            var exType = this.SharedState.CurrentExpressionType();
            IValue value;

            // Special case: if both values are self-evaluating (literals or simple identifiers), we can
            // do this with a select.
            if (ExpressionIsSelfEvaluating(ifTrueEx) && ExpressionIsSelfEvaluating(ifFalseEx))
            {
                var ifTrue = this.SharedState.EvaluateSubexpression(ifTrueEx);
                var ifFalse = this.SharedState.EvaluateSubexpression(ifFalseEx);
                var res = this.SharedState.CurrentBuilder.Select(cond.Value, ifTrue.Value, ifFalse.Value);
                value = this.SharedState.Values.From(res, exType);
            }
            else
            {
                var contBlock = this.SharedState.AddBlockAfterCurrent("condContinue");
                var falseBlock = this.SharedState.AddBlockAfterCurrent("condFalse");
                var trueBlock = this.SharedState.AddBlockAfterCurrent("condTrue");

                // In order to ensure the correct reference counts, it is important that we create a new scope
                // for each branch of the conditional. When we close the scope, we list the computed value as
                // to be returned from that scope, meaning it either won't be dereferenced or its reference
                // count will increase by 1. The result of the expression is a phi node that we then properly
                // register with the scope manager, such that it will be unreferenced when going out of scope.

                this.SharedState.CurrentBuilder.Branch(cond.Value, trueBlock, falseBlock);

                this.SharedState.SetCurrentBlock(trueBlock);
                var ifTrue = this.SharedState.BuildSubitem(ifTrueEx);
                this.SharedState.CurrentBuilder.Branch(contBlock);

                this.SharedState.SetCurrentBlock(falseBlock);
                var ifFalse = this.SharedState.BuildSubitem(ifFalseEx);
                this.SharedState.CurrentBuilder.Branch(contBlock);

                this.SharedState.SetCurrentBlock(contBlock);
                var phi = this.SharedState.CurrentBuilder.PhiNode(this.SharedState.CurrentLlvmExpressionType());
                phi.AddIncoming(ifTrue.Value, trueBlock);
                phi.AddIncoming(ifFalse.Value, falseBlock);
                value = this.SharedState.Values.From(phi, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnControlledApplication(TypedExpression ex) =>
            this.ApplyFunctor(RuntimeLibrary.CallableMakeControlled, ex);

        public override ResolvedExpression OnCopyAndUpdateExpression(TypedExpression lhs, TypedExpression accEx, TypedExpression rhs)
        {
            IValue CopyAndUpdateArray(ResolvedType elementType)
            {
                // Since we keep track of access counts for arrays we always ask the runtime to create a shallow copy
                // if needed. The runtime function ArrayCopy creates a new value with reference count 1 if the current
                // access count is larger than 0, and otherwise merely increases the reference count of the array by 1.
                var createShallowCopy = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy);
                var originalArray = this.SharedState.EvaluateSubexpression(lhs);
                var forceCopy = this.SharedState.Context.CreateConstant(false);
                var copy = this.SharedState.CurrentBuilder.Call(createShallowCopy, originalArray.Value, forceCopy);
                var array = this.SharedState.Values.FromArray(copy, elementType);
                this.SharedState.ScopeMgr.RegisterValue(array);

                // In order to accurately reflect which items are still in use and thus need to remain allocated,
                // reference counts always need to be modified recursively. However, while the reference count for
                // the value returned by ArrayCopy is set to 1 or increased by 1, it is not possible for the runtime
                // to increase the reference count of the contained items due to lacking type information.
                // In the same way that we increase the reference count when we populate an array, we hence need to
                // manually (recursively) increase the reference counts for all items.
                if (this.SharedState.ScopeMgr.RequiresReferenceCount(array.ElementType))
                {
                    this.SharedState.IterateThroughArray(array, item => this.SharedState.ScopeMgr.IncreaseReferenceCount(item));
                }

                // The getNewItemForIndex function is expected to increase the reference count of the time by 1.
                void UpdateElement(Func<Value, IValue> getNewItemForIndex, Value index)
                {
                    var elementPtr = array.GetArrayElementPointer(index);
                    var originalElement = elementPtr.LoadValue();
                    var newElement = getNewItemForIndex(index);

                    // Remark: Avoiding to increase and then decrease the reference count for the original item
                    // would require generating a pointer comparison that is evaluated at runtime, and I am not sure
                    // whether that would be much better.
                    this.SharedState.ScopeMgr.DecreaseReferenceCount(originalElement);
                    elementPtr.StoreValue(newElement);
                }

                if (accEx.ResolvedType.Resolution.IsInt)
                {
                    IValue newItemValue = this.SharedState.BuildSubitem(rhs);
                    var index = this.SharedState.EvaluateSubexpression(accEx);
                    UpdateElement(_ => newItemValue, index.Value);
                }
                else if (accEx.ResolvedType.Resolution.IsRange)
                {
                    var newItemValue = (ArrayValue)this.SharedState.BuildSubitem(rhs);
                    var (getStart, getStep, getEnd) = RangeItems(this.SharedState, accEx);
                    this.SharedState.IterateThroughRange(getStart(), getStep(), getEnd(), index => UpdateElement(newItemValue.GetArrayElement, index));
                }
                else
                {
                    throw new InvalidOperationException("invalid item name in named item access");
                }

                return array;
            }

            IValue CopyAndUpdateUdt(QsQualifiedName udtName)
            {
                // Returns the shallow copy as tuple.
                TupleValue GetTupleCopy(TupleValue original)
                {
                    // Since we keep track of access counts for tuples we always ask the runtime to create a shallow copy
                    // if needed. The runtime function TupleCopy creates a new value with reference count 1 if the current
                    // access count is larger than 0, and otherwise merely increases the reference count of the tuple by 1.
                    var createShallowCopy = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCopy);
                    var forceCopy = this.SharedState.Context.CreateConstant(false);
                    var copy = this.SharedState.CurrentBuilder.Call(createShallowCopy, original.OpaquePointer, forceCopy);
                    var tuple = this.SharedState.Values.FromTuple(copy, original.ElementTypes);
                    return tuple;
                }

                var originalValue = (TupleValue)this.SharedState.EvaluateSubexpression(lhs);
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
                    TupleValue innerTuple = value;
                    for (int depth = 0; depth < location.Count; depth++)
                    {
                        // In order to accurately reflect which items are still in use and thus need to remain allocated,
                        // reference counts always need to be modified recursively. However, while the reference count for
                        // the value returned by TupleCopy is set to 1 or increased by 1, it is not possible for the runtime
                        // to increase the reference count of the contained items due to lacking type information.
                        // In the same way that we increase the reference count when we populate a tuple, we hence need to
                        // manually (recursively) increase the reference counts for all items.
                        var itemPointers = innerTuple.GetTupleElementPointers();
                        var itemIndex = location[depth];
                        for (var i = 0; i < itemPointers.Length; ++i)
                        {
                            if (i != itemIndex && this.SharedState.ScopeMgr.RequiresReferenceCount(innerTuple.StructType.Members[i]))
                            {
                                var item = itemPointers[i].LoadValue();
                                this.SharedState.ScopeMgr.IncreaseReferenceCount(item);
                            }
                        }

                        if (depth == location.Count - 1)
                        {
                            var newItemValue = this.SharedState.BuildSubitem(rhs);
                            itemPointers[itemIndex].StoreValue(newItemValue);
                        }
                        else
                        {
                            // We load the original item at that location (which is an inner tuple),
                            // and replace it with a copy of it (if a copy is needed),
                            // such that we can then proceed to modify that copy (the next inner tuple).
                            var originalItem = (TupleValue)itemPointers[itemIndex].LoadValue();
                            innerTuple = GetTupleCopy(originalItem);
                            itemPointers[itemIndex].StoreValue(innerTuple);
                        }
                    }

                    return value;
                }
                else
                {
                    throw new InvalidOperationException("invalid item name in named item access");
                }
            }

            IValue value;
            if (lhs.ResolvedType.Resolution is QsResolvedTypeKind.ArrayType elementType)
            {
                value = CopyAndUpdateArray(elementType.Item);
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

        public override ResolvedExpression OnDivision(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.SDiv(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FDiv(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntDivide creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntDivide);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
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
            var res = this.SharedState.Context.CreateConstant(d);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.FromSimpleValue(res, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnEquality(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsResult)
            {
                // Generate a call to the result equality testing function
                var equals = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultEqual);
                var res = this.SharedState.CurrentBuilder.Call(equals, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBool || lhsEx.ResolvedType.Resolution.IsInt || lhsEx.ResolvedType.Resolution.IsQubit
                || lhsEx.ResolvedType.Resolution.IsPauli)
            {
                // Works for pointers as well as integer types
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.Equal, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsString)
            {
                // Generate a call to the string equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringEqual);
                var res = this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                // Generate a call to the bigint equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntEqual);
                var res = this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                // TODO: Equality testing for general types
                throw new NotSupportedException("invalid type for equality comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnExponentiate(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                // The exponent must be an integer that can fit into an i32.
                var powFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.IntPower);
                var exponent = this.SharedState.CurrentBuilder.IntCast(rhs.Value, this.SharedState.Context.Int32Type, true);
                var res = this.SharedState.CurrentBuilder.Call(powFunc, lhs.Value, exponent);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var powFunc = this.SharedState.Module.GetIntrinsicDeclaration("llvm.pow.f", this.SharedState.Types.Double);
                var res = this.SharedState.CurrentBuilder.Call(powFunc, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntPower creates a new value with reference count 1.
                // The exponent must be an integer that can fit into an i32.
                var powFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntPower);
                var exponent = this.SharedState.CurrentBuilder.IntCast(rhs.Value, this.SharedState.Context.Int32Type, true);
                var res = this.SharedState.CurrentBuilder.Call(powFunc, lhs.Value, exponent);
                value = this.SharedState.Values.From(res, exType);
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
            IValue value;
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

        public override ResolvedExpression OnGreaterThan(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedGreaterThan, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndGreaterThan, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreater);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnGreaterThanOrEqual(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedGreaterThanOrEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndGreaterThanOrEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreaterEq);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
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
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (sym is Identifier.LocalVariable local)
            {
                var variable = this.SharedState.ScopeMgr.GetVariable(local.Item);
                value = variable is PointerValue pointer
                    ? pointer.LoadValue()
                    : variable;
            }
            else if (!(sym is Identifier.GlobalCallable globalCallable))
            {
                throw new NotSupportedException("unknown identifier");
            }
            else if (this.SharedState.TryGetGlobalCallable(globalCallable.Item, out QsCallable? callable))
            {
                // The runtime function CallableCreate creates a new value with reference count 1.
                var createCallable = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
                var table = this.SharedState.GetOrCreateCallableTable(callable);
                var capture = this.SharedState.Constants.UnitValue; // nothing to capture
                var res = this.SharedState.CurrentBuilder.Call(createCallable, table, capture);
                value = this.SharedState.Values.FromCallable(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new InvalidOperationException("Q# declaration for global callable not found");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnInequality(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsResult)
            {
                // Generate a call to the result equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultEqual);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBool || lhsEx.ResolvedType.Resolution.IsInt || lhsEx.ResolvedType.Resolution.IsQubit
                || lhsEx.ResolvedType.Resolution.IsPauli)
            {
                // Works for pointers as well as integer types
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.NotEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndNotEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsString)
            {
                // Generate a call to the string equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringEqual);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                // Generate a call to the bigint equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntEqual);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
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
            var constant = this.SharedState.Context.CreateConstant(i);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.FromSimpleValue(constant, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLeftShift(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.ShiftLeft(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntLeftShift creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntShiftLeft);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for left shift");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLessThan(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedLessThan, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndLessThan, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreaterEq);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLessThanOrEqual(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedLessThanOrEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndLessThanOrEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreater);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLogicalAnd(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();
            var res = this.SharedState.CurrentBuilder.And(lhs.Value, rhs.Value);
            var value = this.SharedState.Values.FromSimpleValue(res, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLogicalNot(TypedExpression ex)
        {
            // Get the Value for the expression
            var arg = this.SharedState.EvaluateSubexpression(ex);
            var res = this.SharedState.CurrentBuilder.Not(arg.Value);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.FromSimpleValue(res, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLogicalOr(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();
            var res = this.SharedState.CurrentBuilder.Or(lhs.Value, rhs.Value);
            var value = this.SharedState.Values.FromSimpleValue(res, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnModulo(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.SRem(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntModulus creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntModulus);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for modulo");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnMultiplication(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Mul(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FMul(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntMultiply creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntMultiply);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
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
            IValue value;
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
                    value = ((TupleValue)value).GetTupleElement(location[i]);
                }
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
            var exValue = this.SharedState.EvaluateSubexpression(ex);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Neg(exValue.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FNeg(exValue.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntNegative creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntNegate);
                var res = this.SharedState.CurrentBuilder.Call(func, exValue.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for negative");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnNewArray(ResolvedType elementType, TypedExpression lengthEx)
        {
            // TODO: new multi-dimensional arrays
            var length = this.SharedState.EvaluateSubexpression(lengthEx);
            var array = this.SharedState.Values.CreateArray(length.Value, elementType);
            this.SharedState.ValueStack.Push(array);
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

            IValue value;
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
            PartialApplicationArgument BuildPartialArgList(ResolvedType argType, TypedExpression arg, IList<ResolvedType> remainingArgs, IList<TypedExpression> capturedValues)
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
                    capturedValues.Add(arg);
                    return new InnerCapture(this.SharedState, capturedValues.Count - 1);
                }
            }

            IrFunction BuildLiftedSpecialization(string name, QsSpecializationKind kind, ImmutableArray<ResolvedType> captureType, ImmutableArray<ResolvedType> paArgsTypes, PartialApplicationArgument partialArgs)
            {
                IValue ApplyFunctors(IValue innerCallable)
                {
                    if (kind == QsSpecializationKind.QsBody)
                    {
                        return innerCallable;
                    }
                    else if (kind == QsSpecializationKind.QsAdjoint)
                    {
                        return this.ApplyFunctor(RuntimeLibrary.CallableMakeAdjoint, innerCallable, modifyInPlace: false);
                    }
                    else if (kind == QsSpecializationKind.QsControlled)
                    {
                        return this.ApplyFunctor(RuntimeLibrary.CallableMakeControlled, innerCallable, modifyInPlace: false);
                    }
                    else if (kind == QsSpecializationKind.QsControlledAdjoint)
                    {
                        innerCallable = this.ApplyFunctor(RuntimeLibrary.CallableMakeAdjoint, innerCallable, modifyInPlace: false);
                        return this.ApplyFunctor(RuntimeLibrary.CallableMakeControlled, innerCallable, modifyInPlace: true);
                    }
                    else
                    {
                        throw new NotImplementedException("unknown specialization");
                    }
                }

                void BuildPartialApplicationBody(IReadOnlyList<Argument> parameters)
                {
                    var captureTuple = this.SharedState.Values.FromTuple(parameters[0], captureType);
                    TupleValue BuildControlledInnerArgument(ResolvedType paArgType)
                    {
                        // The argument tuple given to the controlled version of the partial application consists of the array of control qubits
                        // as well as a tuple with the remaining arguments for the partial application.
                        // We need to cast the corresponding function parameter to the appropriate type and load both of these items.
                        var ctlPaArgsTypes = ImmutableArray.Create(SyntaxGenerator.QubitArrayType, paArgType);
                        var ctlPaArgsTuple = this.SharedState.Values.FromTuple(parameters[1], ctlPaArgsTypes);
                        var ctlPaArgItems = ctlPaArgsTuple.GetTupleElements();

                        // We then create and populate the complete argument tuple for the controlled specialization of the inner callable.
                        // The tuple consists of the control qubits and the combined tuple of captured values and the arguments given to the partial application.
                        var innerArgs = partialArgs.BuildItem(captureTuple, ctlPaArgItems[1]);
                        return this.SharedState.Values.CreateTuple(ctlPaArgItems[0], innerArgs);
                    }

                    TupleValue innerArg;
                    if (kind == QsSpecializationKind.QsControlled || kind == QsSpecializationKind.QsControlledAdjoint)
                    {
                        // Deal with the extra control qubit arg for controlled and controlled-adjoint
                        // We special case if the base specialization only takes a single parameter and don't create the sub-tuple in this case.
                        innerArg = BuildControlledInnerArgument(
                            paArgsTypes.Length == 1
                            ? paArgsTypes[0]
                            : ResolvedType.New(QsResolvedTypeKind.NewTupleType(paArgsTypes)));
                    }
                    else
                    {
                        var parArgsTuple = this.SharedState.Values.FromTuple(parameters[1], paArgsTypes);
                        var typedInnerArg = partialArgs.BuildItem(captureTuple, parArgsTuple);
                        innerArg = typedInnerArg is TupleValue innerArgTuple
                            ? innerArgTuple
                            : this.SharedState.Values.CreateTuple(typedInnerArg);
                    }

                    var invokeCallable = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableInvoke);
                    var innerCallable = captureTuple.GetTupleElement(0);
                    this.SharedState.CurrentBuilder.Call(invokeCallable, ApplyFunctors(innerCallable).Value, innerArg.OpaquePointer, parameters[2]);
                }

                return this.SharedState.GeneratePartialApplication(name, kind, BuildPartialApplicationBody);
            }

            var liftedName = this.SharedState.GenerateUniqueName("PartialApplication");
            ResolvedType CallableArgumentType(ResolvedType t) => t.Resolution switch
            {
                QsResolvedTypeKind.Function paf => paf.Item1,
                QsResolvedTypeKind.Operation pao => pao.Item1.Item1,
                _ => throw new InvalidOperationException("expecting an operation or function type")
            };

            // Figure out the inputs to the resulting callable based on the signature of the partial application expression
            var exType = this.SharedState.CurrentExpressionType();
            var callableArgType = CallableArgumentType(exType);
            var paArgsTypes = callableArgType.Resolution is QsResolvedTypeKind.TupleType itemTypes
                ? itemTypes.Item
                : ImmutableArray.Create(callableArgType);

            // Argument type of the callable that is partially applied
            var innerArgType = CallableArgumentType(method.ResolvedType);

            // Create the capture tuple, which contains the inner callable as the first item and
            // construct the mapping to combine captured arguments with the arguments for the partial application.
            // Since the capture tuple won't be accessible and will only be used by the partial application,
            // we don't register the tuple with the scope manager and instead don't increase the ref count
            // when building the partial application. Since the partial application is registered with the scope
            // manager, the capture tuple will be released when the partial application is, as it should be.
            var captured = ImmutableArray.CreateBuilder<TypedExpression>();
            captured.Add(method);
            var rebuild = BuildPartialArgList(innerArgType, arg, new List<ResolvedType>(), captured);
            var capture = this.SharedState.Values.CreateTuple(captured.ToImmutable(), registerWithScopeManager: false);

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
                specializations[index] = kinds.Contains(kind)
                    ? BuildLiftedSpecialization(liftedName, kind, capture.ElementTypes, paArgsTypes, rebuild)
                    : Constant.ConstPointerToNullFor(this.SharedState.Types.FunctionSignature.CreatePointerType());
            }

            // Build the callable table
            var array = ConstantArray.From(this.SharedState.Types.FunctionSignature.CreatePointerType(), specializations);
            var table = this.SharedState.Module.AddGlobal(array.NativeType, true, Linkage.DllExport, array, liftedName);

            // Create the callable and make sure we inject/queue the functions to manage the reference counts
            var createCallable = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
            var callable = this.SharedState.CurrentBuilder.Call(createCallable, table, capture.OpaquePointer);
            var value = this.SharedState.Values.FromCallable(callable, exType);
            this.SharedState.ScopeMgr.RegisterValue(value);

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnPauliLiteral(QsPauli p)
        {
            IValue LoadPauli(Value pauli)
            {
                var constant = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Pauli, pauli);
                var exType = this.SharedState.CurrentExpressionType();
                return this.SharedState.Values.From(constant, exType);
            }

            IValue value;
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
                    start = this.SharedState.EvaluateSubexpression(lit.Item1).Value;
                    step = this.SharedState.EvaluateSubexpression(lit.Item2).Value;
                    break;
                default:
                    start = this.SharedState.EvaluateSubexpression(lhs).Value;
                    step = this.SharedState.Context.CreateConstant(1L);
                    break;
            }
            Value end = this.SharedState.EvaluateSubexpression(rhs).Value;

            Value rangePtr = this.SharedState.Constants.EmptyRange;
            Value constant = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Range, rangePtr);
            constant = this.SharedState.CurrentBuilder.InsertValue(constant, start, 0);
            constant = this.SharedState.CurrentBuilder.InsertValue(constant, step, 1);
            constant = this.SharedState.CurrentBuilder.InsertValue(constant, end, 2);

            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.From(constant, exType);

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnResultLiteral(QsResult r)
        {
            var valuePtr = r.IsOne ? this.SharedState.Constants.ResultOne : this.SharedState.Constants.ResultZero;
            var constant = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Result, valuePtr);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.From(constant, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnRightShift(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.ArithmeticShiftRight(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntRightShift creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntShiftRight);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
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

                Value? constantArray = null;
                if (cleanStr.Length > 0)
                {
                    var constantString = this.SharedState.Context.CreateConstantString(cleanStr, true);
                    var globalConstant = this.SharedState.Module.AddGlobal(
                        constantString.NativeType, true, Linkage.Internal, constantString);
                    constantArray = this.SharedState.CurrentBuilder.GetElementPtr(
                        this.SharedState.Context.Int8Type.CreateArrayType((uint)cleanStr.Length + 1), // +1 because zero terminated
                        globalConstant,
                        new[] { this.SharedState.Context.CreateConstant(0) });
                }

                var zeroLengthString = constantArray == null
                    ? this.SharedState.Types.DataArrayPointer.GetNullValue()
                    : this.SharedState.CurrentBuilder.BitCast(
                        constantArray,
                        this.SharedState.Types.DataArrayPointer);

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
                    var exValue = this.SharedState.EvaluateSubexpression(ex).Value;
                    var createString = this.SharedState.GetOrCreateRuntimeFunction(rtFuncName);
                    return this.SharedState.CurrentBuilder.Call(createString, exValue);
                }

                var ty = ex.ResolvedType.Resolution;
                if (ty.IsString)
                {
                    var addReference = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringReference);
                    var value = this.SharedState.EvaluateSubexpression(ex).Value;
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

            Value? current = null;
            if (!exs.IsEmpty)
            {
                // Compiled interpolated strings look like <text>{<int>}<text>...
                // Our basic pattern is to scan for the next '{', append the intervening text if any
                // as a constant string, scan for the closing '}', parse out the integer in between,
                // evaluate the corresponding expression, append it, and keep going.
                // We do have to be a little careful because we can't just look for '{', we have to
                // make sure we skip escaped braces -- "\{".
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
            }

            current ??= CreateConstantString(str);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.From(current, exType);
            this.SharedState.ScopeMgr.RegisterValue(value);

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnSubtraction(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Sub(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FSub(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntSubtract creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntSubtract);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
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
            var value = this.SharedState.Values.Unit;
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnValueArray(ImmutableArray<TypedExpression> vs)
        {
            // TODO: handle multi-dimensional arrays
            var elementType = this.SharedState.CurrentExpressionType().Resolution is QsResolvedTypeKind.ArrayType arrItemType
                ? arrItemType.Item
                : throw new InvalidOperationException("current expression is not of type array");

            var value = this.SharedState.Values.CreateArray(elementType, vs);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnValueTuple(ImmutableArray<TypedExpression> vs)
        {
            var value = this.SharedState.Values.CreateTuple(vs);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnUnwrapApplication(TypedExpression ex)
        {
            // Since we simply represent user defined types as tuples, we don't need to do anything
            // except pushing the value on the value stack unless the tuples contains a single item,
            // in which case we need to remove the tuple wrapping.
            var value = this.SharedState.EvaluateSubexpression(ex);
            if (!(ex.ResolvedType.Resolution is QsResolvedTypeKind.UserDefinedType udt))
            {
                throw new NotSupportedException("invalid type for unwrap operator");
            }
            else if (!this.SharedState.TryGetCustomType(udt.Item.GetFullName(), out var udtDecl))
            {
                throw new InvalidOperationException("Q# declaration for type not found");
            }
            else if (!udtDecl.Type.Resolution.IsTupleType)
            {
                value = ((TupleValue)value).GetTupleElement(0);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpression.InvalidExpr;
        }
    }
}
