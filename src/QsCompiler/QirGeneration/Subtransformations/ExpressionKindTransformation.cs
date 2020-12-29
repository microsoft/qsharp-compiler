// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        /// <summary>
        /// Pushes a value onto the value stack and also adds it to the current ref counting scope.
        /// </summary>
        /// <param name="value">The LLVM value to push</param>
        /// <param name="valueType">The Q# type of the value</param>
        private void PushValueInScope(Value value)
        {
            this.SharedState.ValueStack.Push(value);
            this.SharedState.ScopeMgr.AddValue(value);
        }

        /// <summary>
        /// Creates and returns a deep copy of a tuple.
        /// By default this uses the current builder, but an alternate builder may be provided.
        /// </summary>
        /// <param name="original">The original tuple as an LLVM TupleHeader pointer</param>
        /// <param name="t">The Q# type of the tuple</param>
        /// <param name="b">(optional) The instruction builder to use; the current builder is used if not provided</param>
        /// <returns>The new copy, as an LLVM value containing a TupleHeader pointer</returns>
        private Value DeepCopyTuple(Value original, ResolvedType t, InstructionBuilder? b = null)
        {
            InstructionBuilder builder = b ?? this.SharedState.CurrentBuilder;
            if (t.Resolution is QsResolvedTypeKind.TupleType tupleType)
            {
                var originalTypeRef = this.SharedState.LlvmStructTypeFromQsharpType(t);
                var originalPointerType = originalTypeRef.CreatePointerType();
                var originalSize = this.SharedState.ComputeSizeForType(originalTypeRef, builder);
                var copy = builder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate), originalSize);
                var typedOriginal = builder.BitCast(original, originalPointerType);
                var typedCopy = builder.BitCast(copy, originalPointerType);

                var elementTypes = tupleType.Item;
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    var elementType = elementTypes[i];
                    var originalElement = this.SharedState.GetTupleElement(originalTypeRef, typedOriginal, i, builder);
                    Value elementValue = elementType.Resolution switch
                    {
                        QsResolvedTypeKind.TupleType _ =>
                            this.DeepCopyTuple(originalElement, elementType, b),
                        QsResolvedTypeKind.ArrayType _ =>
                            builder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy), originalElement),
                        // FIXME: WHAT ABOUT UDTS?
                        _ => originalElement,
                    };
                    var copyElementPointer = this.SharedState.GetTupleElementPointer(originalTypeRef, typedCopy, i, builder);
                    builder.Store(elementValue, copyElementPointer);
                }

                return typedCopy;
            }
            else
            {
                return Constant.UndefinedValueFor(this.SharedState.Types.Tuple);
            }
        }

        /// <summary>
        /// Creates and returns a deep copy of a value of a user-defined type.
        /// By default this uses the current builder, but an alternate builder may be provided.
        /// </summary>
        /// <param name="original">The original value</param>
        /// <param name="t">The Q# type, which should be a user-defined type</param>
        /// <param name="b">(optional) The instruction builder to use; the current builder is used if not provided</param>
        /// <returns>The new copy</returns>
        private Value DeepCopyUDT(Value original, ResolvedType t, InstructionBuilder? b = null)
        {
            if ((t.Resolution is QsResolvedTypeKind.UserDefinedType tt) &&
                this.SharedState.TryGetCustomType(tt.Item.GetFullName(), out QsCustomType? udt))
            {
                if (udt.Type.Resolution.IsTupleType)
                {
                    return this.DeepCopyTuple(original, udt.Type, b);
                }
                // FIXME: this is not correct, also: what about udts of udts?
                else if (udt.Type.Resolution.IsArrayType)
                {
                    InstructionBuilder builder = b ?? this.SharedState.CurrentBuilder;
                    return builder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy), original);
                }
                else
                {
                    return original;
                }
            }
            else
            {
                return Constant.UndefinedValueFor(this.SharedState.Types.Tuple);
            }
        }

        /// <summary>
        /// Returns a writable Value for an expression.
        /// If necessary, this will make a copy of the item based on the rules in
        /// <see cref="ItemRequiresCopying(TypedExpression)"/>.
        /// </summary>
        /// <param name="ex">The expression to test.</param>
        /// <returns>An LLVM value that is safe to change.</returns>
        private Value GetWritableCopy(TypedExpression ex, InstructionBuilder? b = null)
        {
            static bool ItemRequiresCopying(TypedExpression ex)
            {
                if (ex.ResolvedType.Resolution.IsArrayType
                    || ex.ResolvedType.Resolution.IsUserDefinedType
                    || ex.ResolvedType.Resolution.IsTupleType)
                {
                    return ex.Expression switch
                    {
                        ResolvedExpression.Identifier _ => true,
                        ResolvedExpression.ArrayItem arr => ItemRequiresCopying(arr.Item1),
                        _ => false
                    };
                }
                else
                {
                    return false;
                }
            }

            // Evaluating the input always happens on the current builder
            var item = this.SharedState.EvaluateSubexpression(ex);

            InstructionBuilder builder = b ?? this.SharedState.CurrentBuilder;
            if (ItemRequiresCopying(ex))
            {
                Value copy = ex.ResolvedType.Resolution switch
                {
                    QsResolvedTypeKind.UserDefinedType _ => this.DeepCopyUDT(item, ex.ResolvedType, b),
                    QsResolvedTypeKind.TupleType _ => this.DeepCopyTuple(item, ex.ResolvedType, b),
                    QsResolvedTypeKind.ArrayType _ => builder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy), item),
                    _ => Constant.UndefinedValueFor(this.SharedState.LlvmTypeFromQsharpType(ex.ResolvedType)),
                };
                this.SharedState.ScopeMgr.AddValue(copy);
                return copy;
            }
            else
            {
                return item;
            }
        }

        private bool FindNamedItem(string name, QsTuple<QsTypeItem> items, List<(int, IStructType)> location)
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
                        if (this.FindNamedItem(name, list.Item[i], location))
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

        private void BuildPartialApplication(TypedExpression method, TypedExpression arg)
        {
            PartialApplicationArgument BuildPartialArgList(ResolvedType argType, TypedExpression arg, List<ResolvedType> remainingArgs, List<Value> capturedValues)
            {
                // We need argType because _'s -- missing expressions -- have MissingType, rather than the actual type.
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
                    // A value we should capture; remember that the first element in the capture tuple is the inner callable
                    var val = this.SharedState.EvaluateSubexpression(arg);
                    capturedValues.Add(val);
                    return new InnerCapture(this.SharedState, capturedValues.Count - 1);
                }
            }

            Value GetSpecializedInnerCallable(Value innerCallable, QsSpecializationKind kind, InstructionBuilder builder)
            {
                if (kind == QsSpecializationKind.QsBody)
                {
                    return innerCallable;
                }
                else
                {
                    var copier = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCopy);
                    var copy = builder.Call(copier, innerCallable);
                    this.SharedState.ScopeMgr.AddValue(copy);
                    if (kind == QsSpecializationKind.QsAdjoint)
                    {
                        var adj = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableMakeAdjoint);
                        builder.Call(adj, copy);
                    }
                    else if (kind == QsSpecializationKind.QsControlled)
                    {
                        var ctl = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableMakeControlled);
                        builder.Call(ctl, copy);
                    }
                    else if (kind == QsSpecializationKind.QsControlledAdjoint)
                    {
                        var adj = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableMakeAdjoint);
                        var ctl = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableMakeControlled);
                        builder.Call(adj, copy);
                        builder.Call(ctl, copy);
                    }
                    else
                    {
                        throw new NotImplementedException("unknown specialization");
                    }
                    return copy;
                }
            }

            IrFunction BuildLiftedSpecialization(string name, QsSpecializationKind kind, IStructType captureType, IStructType parArgsStruct, PartialApplicationArgument partialArgs)
            {
                var funcName = GenerationContext.FunctionWrapperName(new QsQualifiedName("Lifted", name), kind);
                IrFunction func = this.SharedState.Module.CreateFunction(funcName, this.SharedState.Types.FunctionSignature);

                func.Parameters[0].Name = "capture-tuple";
                func.Parameters[1].Name = "arg-tuple";
                func.Parameters[2].Name = "result-tuple";
                var entry = func.AppendBasicBlock("entry");

                InstructionBuilder builder = new InstructionBuilder(entry);
                this.SharedState.ScopeMgr.OpenScope();
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

                var innerCallable = this.SharedState.GetTupleElement(captureType, capturePointer, 0, builder);
                // Depending on the specialization, we may have to get a different specialization of the callable
                var specToCall = GetSpecializedInnerCallable(innerCallable, kind, builder);
                var invoke = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableInvoke);
                builder.Call(invoke, specToCall, innerArg, func.Parameters[2]);

                this.SharedState.ScopeMgr.CloseScope(isTerminated: false, builder);
                builder.Return();

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

            // Build the array
            var t = this.SharedState.Types.FunctionSignature.CreatePointerType();
            var array = ConstantArray.From(t, specializations);
            var table = this.SharedState.Module.AddGlobal(array.NativeType, true, Linkage.DllExport, array, liftedName);

            // Create the callable
            var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
            var callableValue = this.SharedState.CurrentBuilder.Call(func, table, capture.OpaquePointer);

            this.PushValueInScope(callableValue);
        }

        // public overrides

        public override ResolvedExpression OnAddition(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Add(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.FAdd(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntAdd);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(adder, lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsString)
            {
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringConcatenate);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(adder, lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsArrayType)
            {
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayConcatenate);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(adder, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnAdjointApplication(TypedExpression ex)
        {
            // ex will evaluate to a callable
            var baseCallable = this.SharedState.EvaluateSubexpression(ex);

            // If ex was a variable, we need to make a copy before we take the adjoint.
            Value callable;
            if (ex.Expression is ResolvedExpression.Identifier id && id.Item1.IsLocalVariable)
            {
                var copier = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCopy);
                callable = this.SharedState.CurrentBuilder.Call(copier, baseCallable);
                this.SharedState.ScopeMgr.AddValue(callable);
            }
            else
            {
                callable = baseCallable;
            }

            var adjointer = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableMakeAdjoint);
            this.SharedState.CurrentBuilder.Call(adjointer, callable);
            this.SharedState.ValueStack.Push(callable);

            return ResolvedExpression.InvalidExpr;
        }

        /// <exception cref="ArgumentException">
        /// The given arr expression is not an array or the given idx expression is not of type Int or Range.
        /// </exception>
        public override ResolvedExpression OnArrayItem(TypedExpression arr, TypedExpression idx)
        {
            var elementType = arr.ResolvedType.Resolution is QsResolvedTypeKind.ArrayType et
                ? this.SharedState.LlvmTypeFromQsharpType(et.Item)
                : throw new ArgumentException("expecting expression of type array in array item access");

            // TODO: handle multi-dimensional arrays
            var array = this.SharedState.EvaluateSubexpression(arr);
            var index = this.SharedState.EvaluateSubexpression(idx);

            if (idx.ResolvedType.Resolution.IsInt)
            {
                var element = this.SharedState.GetArrayElement(elementType, array, index);
                this.PushValueInScope(element);
                this.SharedState.ScopeMgr.AddReference(element);
            }
            else if (idx.ResolvedType.Resolution.IsRange)
            {
                var slicer = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArraySlice);
                var slice = this.SharedState.CurrentBuilder.Call(slicer, array, this.SharedState.Context.CreateConstant(0), index);
                this.PushValueInScope(slice);
            }
            else
            {
                throw new ArgumentException("expecting an expression of type Int or Range in array item access");
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBigIntLiteral(BigInteger b)
        {
            Value bigIntValue;
            if ((b <= long.MaxValue) && (b >= long.MinValue))
            {
                var val = this.SharedState.Context.CreateConstant((long)b);
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateI64);
                bigIntValue = this.SharedState.CurrentBuilder.Call(func, val);
            }
            else
            {
                var bytes = b.ToByteArray();
                var n = this.SharedState.Context.CreateConstant(bytes.Length);
                var byteArray = ConstantArray.From(
                    this.SharedState.Context.Int8Type,
                    bytes.Select(s => this.SharedState.Context.CreateConstant(s)).ToArray());
                var zeroByteArray = this.SharedState.CurrentBuilder.BitCast(
                    byteArray,
                    this.SharedState.Context.Int8Type.CreateArrayType(0));
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateArray);
                bigIntValue = this.SharedState.CurrentBuilder.Call(func, n, zeroByteArray);
            }
            this.PushValueInScope(bigIntValue);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseAnd(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.And(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitand);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseExclusiveOr(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Xor(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitxor);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseNot(TypedExpression ex)
        {
            Value exValue = this.SharedState.EvaluateSubexpression(ex);

            if (ex.ResolvedType.Resolution.IsInt)
            {
                Value minusOne = this.SharedState.Context.CreateConstant(-1L);
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Xor(exValue, minusOne));
            }
            else if (ex.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitnot);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, exValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(exValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseOr(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Or(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitor);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBoolLiteral(bool b)
        {
            Value lit = this.SharedState.Context.CreateConstant(b);
            this.SharedState.ValueStack.Push(lit);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnCallLikeExpression(TypedExpression method, TypedExpression arg)
        {
            static (TypedExpression BaseMethod, bool Adjoint, int Controlled)
                ResolveModifiers(TypedExpression m, bool a, int c) => m.Expression switch
                {
                    ResolvedExpression.AdjointApplication adj => ResolveModifiers(adj.Item, !a, c),
                    ResolvedExpression.ControlledApplication con => ResolveModifiers(con.Item, a, c + 1),
                    _ => (m, a, c),
                };

            static QsSpecializationKind GetSpecializationKind(bool isAdjoint, bool isControlled) =>
                isAdjoint && isControlled ? QsSpecializationKind.QsControlledAdjoint
                    : (isAdjoint ? QsSpecializationKind.QsAdjoint
                    : (isControlled ? QsSpecializationKind.QsControlled
                    : QsSpecializationKind.QsBody));

            TypedExpression BuildArg(TypedExpression arg, int controlledCount)
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

            // this method currently does not support handling operations or tuple-valued arguments
            bool TryBuildRuntimeFunction(QsQualifiedName name, TypedExpression arg)
            {
                if (method.ResolvedType.Resolution.IsOperation || arg.ResolvedType.Resolution.IsTupleType)
                {
                    throw new NotImplementedException("expecting a function with a single argument");
                }

                if (name.Equals(BuiltIn.Length.FullName))
                {
                    // The argument should be an array
                    var arrayArg = this.SharedState.EvaluateSubexpression(arg);
                    var lengthFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetLength);
                    var value = this.SharedState.CurrentBuilder.Call(lengthFunc, arrayArg, this.SharedState.Context.CreateConstant(0));
                    this.SharedState.ValueStack.Push(value);
                    return true;
                }
                else if (name.Equals(BuiltIn.RangeStart.FullName))
                {
                    // The argument should be an range
                    var rangeArg = this.SharedState.EvaluateSubexpression(arg);
                    var start = this.SharedState.CurrentBuilder.ExtractValue(rangeArg, 0u);
                    this.SharedState.ValueStack.Push(start);
                    return true;
                }
                else if (name.Equals(BuiltIn.RangeStep.FullName))
                {
                    // The argument should be an range
                    var rangeArg = this.SharedState.EvaluateSubexpression(arg);
                    var step = this.SharedState.CurrentBuilder.ExtractValue(rangeArg, 1u);
                    this.SharedState.ValueStack.Push(step);
                    return true;
                }
                else if (name.Equals(BuiltIn.RangeEnd))
                {
                    // The argument should be an range
                    var rangeArg = this.SharedState.EvaluateSubexpression(arg);
                    var end = this.SharedState.CurrentBuilder.ExtractValue(rangeArg, 2u);
                    this.SharedState.ValueStack.Push(end);
                    return true;
                }
                else if (name.Equals(BuiltIn.RangeReverse.FullName))
                {
                    // The argument should be an range
                    var rangeArg = this.SharedState.EvaluateSubexpression(arg);
                    var start = this.SharedState.CurrentBuilder.ExtractValue(rangeArg, 0u);
                    var step = this.SharedState.CurrentBuilder.ExtractValue(rangeArg, 1u);
                    var end = this.SharedState.CurrentBuilder.ExtractValue(rangeArg, 2u);
                    var newStart = this.SharedState.CurrentBuilder.Add(
                        start,
                        this.SharedState.CurrentBuilder.Mul(
                            step,
                            this.SharedState.CurrentBuilder.SDiv(
                                this.SharedState.CurrentBuilder.Sub(end, start), step)));
                    var newRange = this.SharedState.CurrentBuilder.Load(
                        this.SharedState.Types.Range,
                        this.SharedState.Constants.EmptyRange);
                    var reversedRange = this.SharedState.CurrentBuilder.InsertValue(newRange, newStart, 0u);
                    reversedRange = this.SharedState.CurrentBuilder.InsertValue(newRange, this.SharedState.CurrentBuilder.Neg(step), 1u);
                    reversedRange = this.SharedState.CurrentBuilder.InsertValue(newRange, start, 2u);
                    this.SharedState.ValueStack.Push(reversedRange);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            void CallCallableValue(TypedExpression method, TypedExpression arg)
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

                ResolvedType resultResolvedType = this.SharedState.ExpressionTypeStack.Peek();
                if (resultResolvedType.Resolution.IsUnitType)
                {
                    Value resultTuple = this.SharedState.Constants.UnitValue;
                    this.SharedState.CurrentBuilder.Call(func, calledValue, argValue, resultTuple);
                    this.SharedState.ValueStack.Push(this.SharedState.Constants.UnitValue);
                }
                else
                {
                    IStructType resultStructType = this.SharedState.LlvmStructTypeFromQsharpType(resultResolvedType);
                    TupleValue resultTuple = new TupleValue(resultStructType, this.SharedState);
                    this.SharedState.CurrentBuilder.Call(func, calledValue, argValue, resultTuple.OpaquePointer);

                    // Now push the result. For now we assume it's a scalar.
                    Value result = this.SharedState.GetTupleElement(resultTuple.StructType, resultTuple.TypedPointer, 0);
                    this.SharedState.ValueStack.Push(result);
                }
            }

            void CallGlobal(IrFunction func, TypedExpression arg)
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

                var result = this.SharedState.CurrentBuilder.Call(func, argList);
                this.PushValueInScope(result);
            }

            void InlineCalledRoutine(QsCallable inlinedCallable, TypedExpression arg, bool isAdjoint, bool isControlled)
            {
                var inlineKind = GetSpecializationKind(isAdjoint, isControlled);
                var inlinedSpecialization = inlinedCallable.Specializations.Where(spec => spec.Kind == inlineKind).Single();
                if (isAdjoint && inlinedSpecialization.Implementation is SpecializationImplementation.Generated gen && gen.Item.IsSelfInverse)
                {
                    inlinedSpecialization = inlinedCallable.Specializations
                        .Where(spec => spec.Kind == (isControlled ? QsSpecializationKind.QsControlled : QsSpecializationKind.QsBody))
                        .Single();
                }

                this.SharedState.StartInlining();
                if (inlinedSpecialization.Implementation is SpecializationImplementation.Provided impl)
                {
                    if (!inlinedSpecialization.Signature.ArgumentType.Resolution.IsUnitType)
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
                this.SharedState.StopInlining();

                // If the inlined routine returns Unit, we need to push an extra empty tuple on the stack
                if (inlinedCallable.Signature.ReturnType.Resolution.IsUnitType)
                {
                    this.SharedState.ValueStack.Push(this.SharedState.Constants.UnitValue);
                }
            }

            if (TypedExpression.IsPartialApplication(ResolvedExpression.NewCallLikeExpression(method, arg)))
            {
                this.BuildPartialApplication(method, arg);
            }
            else if (method.Expression is ResolvedExpression.Identifier id
                && id.Item1 is Identifier.GlobalCallable cName
                && this.SharedState.TryGetGlobalCallable(cName.Item, out QsCallable? callable)
                && SymbolResolution.TryGetTargetInstructionName(callable.Attributes) is var qisCode && qisCode.IsValue)
            {
                // Handle the special case of a call to an operation that maps directly to a quantum instruction.
                // Note that such an operation will never have an Adjoint or Controlled specialization.
                var func = this.SharedState.GetOrCreateQuantumFunction(qisCode.Item);
                CallGlobal(func, arg);
            }
            else
            {
                // Resolve Adjoint and Controlled modifiers
                var (baseMethod, isAdjoint, controlledCount) = ResolveModifiers(method, false, 0);
                arg = BuildArg(arg, controlledCount);

                // Check for, and handle, inlining
                if (baseMethod.Expression is ResolvedExpression.Identifier baseId
                    && baseId.Item1 is Identifier.GlobalCallable baseCallable)
                {
                    if (this.SharedState.TryGetGlobalCallable(baseCallable.Item, out var inlinedCallable)
                        && inlinedCallable.Attributes.Any(BuiltIn.MarksInlining))
                    {
                        InlineCalledRoutine(inlinedCallable, arg, isAdjoint, controlledCount > 0);
                    }
                    // TryBuildRuntimeFunction pushes the necessary LLVM if it is supported
                    else if (method.ResolvedType.Resolution.IsOperation
                        || arg.ResolvedType.Resolution.IsTupleType
                        || !TryBuildRuntimeFunction(baseCallable.Item, arg))
                    {
                        var kind = GetSpecializationKind(isAdjoint, controlledCount > 0);
                        var func = this.SharedState.GetFunctionByName(baseCallable.Item, kind);
                        CallGlobal(func, arg);
                    }
                }
                else
                {
                    CallCallableValue(method, arg);
                }
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnConditionalExpression(TypedExpression cond, TypedExpression ifTrue, TypedExpression ifFalse)
        {
            static bool ExpressionIsSelfEvaluating(TypedExpression ex)
            {
                return ex.Expression.IsIdentifier || ex.Expression.IsBoolLiteral || ex.Expression.IsDoubleLiteral
                    || ex.Expression.IsIntLiteral || ex.Expression.IsPauliLiteral || ex.Expression.IsRangeLiteral
                    || ex.Expression.IsResultLiteral || ex.Expression.IsUnitValue;
            }

            var condValue = this.SharedState.EvaluateSubexpression(cond);

            // Special case: if both values are self-evaluating (literals or simple identifiers), we can
            // do this with a select.
            if (ExpressionIsSelfEvaluating(ifTrue) && ExpressionIsSelfEvaluating(ifFalse))
            {
                var trueValue = this.SharedState.EvaluateSubexpression(ifTrue);
                var falseValue = this.SharedState.EvaluateSubexpression(ifFalse);
                var select = this.SharedState.CurrentBuilder.Select(condValue, trueValue, falseValue);
                this.SharedState.ValueStack.Push(select);
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
                var phi = this.SharedState.CurrentBuilder.PhiNode(trueValue.NativeType);
                phi.AddIncoming(trueValue, trueBlock);
                phi.AddIncoming(falseValue, falseBlock);

                this.SharedState.ValueStack.Push(phi);
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnControlledApplication(TypedExpression ex)
        {
            // ex will evaluate to a callable
            var baseCallable = this.SharedState.EvaluateSubexpression(ex);

            // If ex was a variable, we need to make a copy before we take the adjoint.
            Value callable;

            if (ex.Expression is ResolvedExpression.Identifier id && id.Item1.IsLocalVariable)
            {
                var copier = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCopy);
                callable = this.SharedState.CurrentBuilder.Call(copier, baseCallable);
                this.SharedState.ScopeMgr.AddValue(callable);
            }
            else
            {
                callable = baseCallable;
            }

            var adjointer = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableMakeControlled);
            this.SharedState.CurrentBuilder.Call(adjointer, callable);
            this.SharedState.ValueStack.Push(callable);

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnCopyAndUpdateExpression(TypedExpression lhs, TypedExpression accEx, TypedExpression rhs)
        {
            var copy = this.GetWritableCopy(lhs); // if a copy is made, registers the copy with the ScopeMgr

            if (lhs.ResolvedType.Resolution is QsResolvedTypeKind.ArrayType itemType)
            {
                if (accEx.ResolvedType.Resolution.IsInt)
                {
                    var index = this.SharedState.EvaluateSubexpression(accEx);
                    var value = this.SharedState.EvaluateSubexpression(rhs);
                    var elementType = this.SharedState.LlvmTypeFromQsharpType(itemType.Item);
                    var elementPtr = this.SharedState.GetArrayElementPointer(elementType, copy, index);
                    this.SharedState.CurrentBuilder.Store(value, elementPtr);
                }
                else if (accEx.ResolvedType.Resolution.IsRange)
                {
                    // TODO: handle range updates
                    throw new NotImplementedException("Array slice updates");
                }
                this.SharedState.ValueStack.Push(copy);
            }
            else if (lhs.ResolvedType.Resolution is QsResolvedTypeKind.UserDefinedType tt)
            {
                var location = new List<(int, IStructType)>();
                if (this.SharedState.TryGetCustomType(tt.Item.GetFullName(), out QsCustomType? udt)
                    && accEx.Expression is ResolvedExpression.Identifier acc
                    && acc.Item1 is Identifier.LocalVariable loc
                    && this.FindNamedItem(loc.Item, udt.TypeItems, location))
                {
                    // The location list is backwards, by design, so we have to reverse it
                    location.Reverse();
                    var current = copy;
                    for (int i = 0; i < location.Count; i++)
                    {
                        var ptr = this.SharedState.GetTupleElementPointer(
                            location[i].Item2,
                            current,
                            location[i].Item1);
                        // For the last item on the list, we store; otherwise, we load the next tuple
                        if (i == location.Count - 1)
                        {
                            var value = this.SharedState.EvaluateSubexpression(rhs);
                            this.SharedState.CurrentBuilder.Store(value, ptr);
                        }
                        else
                        {
                            current = this.SharedState.CurrentBuilder.Load(location[i + 1].Item2.CreatePointerType(), ptr);
                        }
                    }
                    this.SharedState.ValueStack.Push(copy);
                }
                else
                {
                    throw new NotImplementedException("unknown item in copy-and-update expression");
                }
            }
            else
            {
                throw new NotImplementedException("unknown expression type for copy-and-update expression");
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnDivision(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.SDiv(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.FDiv(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntDivide);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnDoubleLiteral(double d)
        {
            Value lit = this.SharedState.Context.CreateConstant(d);
            this.SharedState.ValueStack.Push(lit);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnEquality(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            // The code we generate here is highly dependent on the type of the expression
            if (lhs.ResolvedType.Resolution.IsResult)
            {
                // Generate a call to the result equality testing function
                var value = this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultEqual), lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsBool || lhs.ResolvedType.Resolution.IsInt || lhs.ResolvedType.Resolution.IsQubit
                || lhs.ResolvedType.Resolution.IsPauli)
            {
                // Works for pointers as well as integer types
                var value = this.SharedState.CurrentBuilder.Compare(IntPredicate.Equal, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                var value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndEqual, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsString)
            {
                // Generate a call to the string equality testing function
                var value = this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringEqual), lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                // Generate a call to the bigint equality testing function
                var value = this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntEqual), lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else
            {
                // TODO: Equality testing for general types
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Context.BoolType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnExponentiate(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                // RHS must be an integer that can fit into an i32
                var exponent = this.SharedState.CurrentBuilder.IntCast(rhsValue, this.SharedState.Context.Int32Type, true);
                var powFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.IntPower);
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Call(powFunc, lhsValue, exponent));
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                var powFunc = this.SharedState.Module.GetIntrinsicDeclaration("llvm.pow.f", this.SharedState.Types.Double);
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Call(powFunc, lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                // RHS must be an integer that can fit into an i32
                var exponent = this.SharedState.CurrentBuilder.IntCast(rhsValue, this.SharedState.Context.Int32Type, true);
                var powFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntPower);
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Call(powFunc, lhsValue, exponent));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Types.Int));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnGreaterThan(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                var value = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedGreaterThan, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                var value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndGreaterThan, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreater);
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Context.BoolType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnGreaterThanOrEqual(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                var value = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedGreaterThanOrEqual, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                var value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndGreaterThanOrEqual, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreaterEq);
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Context.BoolType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
        {
            if (sym is Identifier.LocalVariable local)
            {
                var value = this.SharedState.GetNamedValue(local.Item);
                this.SharedState.ValueStack.Push(value);
            }
            else if (sym is Identifier.GlobalCallable globalCallable)
            {
                if (this.SharedState.TryGetGlobalCallable(globalCallable.Item, out QsCallable? callable))
                {
                    var wrapper = this.SharedState.GetOrCreateCallableTable(callable);
                    var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
                    var callableValue = this.SharedState.CurrentBuilder.Call(func, wrapper, this.SharedState.Constants.UnitValue);
                    this.PushValueInScope(callableValue);
                }
                else
                {
                    this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Types.Callable));
                }
            }
            else
            {
                // Invalid identifier
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(
                    this.SharedState.LlvmTypeFromQsharpType(this.SharedState.ExpressionTypeStack.Peek())));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnInequality(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            // The code we generate here is highly dependent on the type of the expression
            if (lhs.ResolvedType.Resolution.IsResult)
            {
                // Generate a call to the result equality testing function
                var eq = this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultEqual), lhsValue, rhsValue);
                var ineq = this.SharedState.CurrentBuilder.Not(eq);
                this.SharedState.ValueStack.Push(ineq);
            }
            else if (lhs.ResolvedType.Resolution.IsBool || lhs.ResolvedType.Resolution.IsInt || lhs.ResolvedType.Resolution.IsQubit
                || lhs.ResolvedType.Resolution.IsPauli)
            {
                // Works for pointers as well as integer types
                var eq = this.SharedState.CurrentBuilder.Compare(IntPredicate.Equal, lhsValue, rhsValue);
                var ineq = this.SharedState.CurrentBuilder.Not(eq);
                this.SharedState.ValueStack.Push(ineq);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                var eq = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndEqual, lhsValue, rhsValue);
                var ineq = this.SharedState.CurrentBuilder.Not(eq);
                this.SharedState.ValueStack.Push(ineq);
            }
            else if (lhs.ResolvedType.Resolution.IsString)
            {
                // Generate a call to the string equality testing function
                var eq = this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringEqual), lhsValue, rhsValue);
                var ineq = this.SharedState.CurrentBuilder.Not(eq);
                this.SharedState.ValueStack.Push(ineq);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                // Generate a call to the bigint equality testing function
                var eq = this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntEqual), lhsValue, rhsValue);
                var ineq = this.SharedState.CurrentBuilder.Not(eq);
                this.SharedState.ValueStack.Push(ineq);
            }
            else
            {
                // TODO: Equality testing for general types
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Context.BoolType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnIntLiteral(long i)
        {
            Value lit = this.SharedState.Context.CreateConstant(i);
            this.SharedState.ValueStack.Push(lit);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLeftShift(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.ShiftLeft(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntShiftleft);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLessThan(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                var value = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedLessThan, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                var value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndLessThan, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreaterEq);
                var value = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
                this.SharedState.ValueStack.Push(value);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Context.BoolType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLessThanOrEqual(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                var value = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedLessThanOrEqual, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                var value = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndLessThanOrEqual, lhsValue, rhsValue);
                this.SharedState.ValueStack.Push(value);
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreater);
                var value = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
                this.SharedState.ValueStack.Push(value);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Context.BoolType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLogicalAnd(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsBool)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.And(lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Context.BoolType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLogicalNot(TypedExpression ex)
        {
            // Get the Value for the expression
            Value exValue = this.SharedState.EvaluateSubexpression(ex);

            if (ex.ResolvedType.Resolution.IsBool)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Not(exValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Context.BoolType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLogicalOr(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsBool)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Or(lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Context.BoolType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnModulo(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.SRem(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntModulus);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnMultiplication(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Mul(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.FMul(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntMultiply);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnNamedItem(TypedExpression ex, Identifier acc)
        {
            if (ex.ResolvedType.Resolution is QsResolvedTypeKind.UserDefinedType tt
                && this.SharedState.TryGetCustomType(tt.Item.GetFullName(), out QsCustomType? udt)
                && acc is Identifier.LocalVariable itemName)
            {
                var location = new List<(int, IStructType)>();
                if (this.FindNamedItem(itemName.Item, udt.TypeItems, location))
                {
                    // The location list refers to the location of the named item within the item tuple
                    // and contains inner items first, so we have to reverse it
                    location.Reverse();
                    var value = this.SharedState.EvaluateSubexpression(ex);
                    for (int i = 0; i < location.Count; i++)
                    {
                        value = this.SharedState.GetTupleElement(location[i].Item2, value, location[i].Item1);
                    }
                    this.SharedState.ScopeMgr.AddReference(value);
                    this.PushValueInScope(value);
                }
                else
                {
                    throw new InvalidOperationException("no item with that name exists");
                }
            }
            else
            {
                throw new ArgumentException("named item access requires a value of user defined type");
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnNegative(TypedExpression ex)
        {
            Value exValue = this.SharedState.EvaluateSubexpression(ex);

            if (ex.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Neg(exValue));
            }
            else if (ex.ResolvedType.Resolution.IsDouble)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.FNeg(exValue));
            }
            else if (ex.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntNegate);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, exValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(exValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnNewArray(ResolvedType elementType, TypedExpression idx)
        {
            // TODO: new multi-dimensional arrays
            var array = new ArrayValue(
                this.SharedState.EvaluateSubexpression(idx),
                this.SharedState.LlvmTypeFromQsharpType(elementType),
                this.SharedState);

            this.PushValueInScope(array.OpaquePointer);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnPauliLiteral(QsPauli p)
        {
            void LoadAndPushPauli(Value pauli) =>
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Pauli, pauli));

            if (p.IsPauliI)
            {
                LoadAndPushPauli(this.SharedState.Constants.PauliI);
            }
            else if (p.IsPauliX)
            {
                LoadAndPushPauli(this.SharedState.Constants.PauliX);
            }
            else if (p.IsPauliY)
            {
                LoadAndPushPauli(this.SharedState.Constants.PauliY);
            }
            else if (p.IsPauliZ)
            {
                LoadAndPushPauli(this.SharedState.Constants.PauliZ);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Types.Pauli));
            }

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

            var end = this.SharedState.EvaluateSubexpression(rhs);

            Value rangePtr = this.SharedState.Constants.EmptyRange;
            Value range = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Range, rangePtr);
            range = this.SharedState.CurrentBuilder.InsertValue(range, start, 0);
            range = this.SharedState.CurrentBuilder.InsertValue(range, step, 1);
            range = this.SharedState.CurrentBuilder.InsertValue(range, end, 2);
            this.SharedState.ValueStack.Push(range);

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnResultLiteral(QsResult r)
        {
            var valuePtr = r.IsOne ? this.SharedState.Constants.ResultOne : this.SharedState.Constants.ResultZero;
            var value = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Result, valuePtr);
            this.PushValueInScope(value);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnRightShift(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.ArithmeticShiftRight(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntShiftright);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnStringLiteral(string str, ImmutableArray<TypedExpression> exs)
        {
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
                var stringValue = this.SharedState.CurrentBuilder.Call(
                    this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringCreate), n, zeroLengthString);
                return stringValue;
            }

            Value SimpleToString(TypedExpression ex, string rtFuncName)
            {
                var exValue = this.SharedState.EvaluateSubexpression(ex);
                var stringValue = this.SharedState.CurrentBuilder.Call(
                    this.SharedState.GetOrCreateRuntimeFunction(rtFuncName), exValue);
                return stringValue;
            }

            Value ExpressionToString(TypedExpression ex)
            {
                var ty = ex.ResolvedType.Resolution;
                if (ty.IsString)
                {
                    // Special case -- if this is the value of an identifier, we need to increment
                    // it's reference count
                    var s = this.SharedState.EvaluateSubexpression(ex);
                    if (ex.Expression.IsIdentifier)
                    {
                        var stringValue = this.SharedState.CurrentBuilder.Call(
                            this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringReference), s);
                    }
                    return s;
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
                    return CreateConstantString("...");
                }
            }

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

            // We need to be careful here to unreference intermediate strings, but not the final value
            Value DoAppend(Value? curr, Value next)
            {
                if (curr == null)
                {
                    return next;
                }
                else
                {
                    var app = this.SharedState.CurrentBuilder.Call(
                        this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringConcatenate), curr, next);
                    // Unreference the component strings
                    this.SharedState.CurrentBuilder.Call(
                        this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringUnreference), curr);
                    this.SharedState.CurrentBuilder.Call(
                        this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringUnreference), next);
                    return app;
                }
            }

            if (exs.IsEmpty)
            {
                var stringValue = CreateConstantString(str);
                this.PushValueInScope(stringValue);
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
                current ??= CreateConstantString("");
                this.PushValueInScope(current);
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnSubtraction(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.SharedState.EvaluateSubexpression(lhs);
            Value rhsValue = this.SharedState.EvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Sub(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsDouble)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.FSub(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntSubtract);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue));
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnUnitValue()
        {
            this.SharedState.ValueStack.Push(this.SharedState.Constants.UnitValue);
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnValueArray(ImmutableArray<TypedExpression> vs)
        {
            // TODO: handle multi-dimensional arrays
            var elementType = this.SharedState.LlvmTypeFromQsharpType(vs[0].ResolvedType);
            var array = new ArrayValue((uint)vs.Length, elementType, this.SharedState).OpaquePointer;

            long idx = 0;
            foreach (var element in vs)
            {
                var index = this.SharedState.Context.CreateConstant(idx);
                var elementPointer = this.SharedState.GetArrayElementPointer(elementType, array, index);
                var elementValue = this.SharedState.EvaluateSubexpression(element);
                this.SharedState.CurrentBuilder.Store(elementValue, elementPointer);
                this.SharedState.ScopeMgr.AddReference(elementValue);
                idx++;
            }

            this.PushValueInScope(array);
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
            var udtTuplePointer = this.SharedState.EvaluateSubexpression(ex);

            // Since we simply represent user defined types as tuples, we don't need to do anything
            // except pushing the value on the value stack unless the tuples contains a single item,
            // in which case we need to remove the tuple wrapping.
            if (ex.ResolvedType.Resolution is QsResolvedTypeKind.UserDefinedType udt
                && this.SharedState.TryGetCustomType(udt.Item.GetFullName(), out var udtDecl)
                && !udtDecl.Type.Resolution.IsTupleType)
            {
                // we need to access the second item, since the first is the tuple header
                var elementType = this.SharedState.LlvmTypeFromQsharpType(udtDecl.Type);
                var element = this.SharedState.GetTupleElement(
                     this.SharedState.Types.CreateConcreteTupleType(elementType),
                     udtTuplePointer,
                     0);

                this.SharedState.ScopeMgr.AddReference(element);
                this.PushValueInScope(element);
            }
            else
            {
                this.SharedState.ScopeMgr.AddReference(udtTuplePointer);
                this.PushValueInScope(udtTuplePointer);
            }

            return ResolvedExpression.InvalidExpr;
        }
    }
}
