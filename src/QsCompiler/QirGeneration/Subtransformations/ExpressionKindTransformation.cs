// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using ResolvedExpression = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;

    internal class QirExpressionKindTransformation : ExpressionKindTransformation<GenerationContext>
    {
        // inner classes

        private abstract class RebuildItem
        {
            protected readonly GenerationContext SharedState;
            public readonly ITypeRef ItemType;

            public RebuildItem(GenerationContext sharedState, ITypeRef itemType)
            {
                this.SharedState = sharedState;
                this.ItemType = itemType;
            }

            public abstract Value BuildItem(InstructionBuilder builder, ITypeRef captureType, Value capture, ITypeRef parArgsType, Value parArgs);
        }

        private class InnerCapture : RebuildItem
        {
            public readonly int CaptureIndex;

            public InnerCapture(GenerationContext sharedState, ITypeRef itemType, int captureIndex)
            : base(sharedState, itemType)
            {
                this.CaptureIndex = captureIndex;
            }

            public override Value BuildItem(InstructionBuilder builder, ITypeRef captureType, Value capture, ITypeRef parArgsType, Value parArgs)
            {
                var indices = new Value[]
                {
                    builder.Context.CreateConstant(0L),
                    builder.Context.CreateConstant(this.CaptureIndex)
                };
                var srcPtr = builder.GetElementPtr(captureType, capture, indices);
                var item = builder.Load(this.ItemType, srcPtr);
                return item;
            }
        }

        private class InnerArg : RebuildItem
        {
            public readonly int ArgIndex;

            public InnerArg(GenerationContext sharedState, ITypeRef itemType, int argIndex)
            : base(sharedState, itemType)
            {
                this.ArgIndex = argIndex;
            }

            public override Value BuildItem(InstructionBuilder builder, ITypeRef captureType, Value capture, ITypeRef parArgsType, Value parArgs)
            {
                if (this.SharedState.Types.IsTupleType(parArgs.NativeType))
                {
                    var indices = new Value[]
                    {
                        builder.Context.CreateConstant(0L),
                        builder.Context.CreateConstant(this.ArgIndex)
                    };
                    var srcPtr = builder.GetElementPtr(parArgsType, parArgs, indices);
                    var item = builder.Load(this.ItemType, srcPtr);
                    return item;
                }
                else
                {
                    return parArgs;
                }
            }
        }

        private class InnerTuple : RebuildItem
        {
            public readonly ResolvedType TupleType;
            public readonly ImmutableArray<RebuildItem> Items;

            public InnerTuple(GenerationContext sharedState, ResolvedType tupleType, ITypeRef itemType, IEnumerable<RebuildItem>? items)
            : base(sharedState, itemType)
            {
                this.TupleType = tupleType;
                this.Items = items?.ToImmutableArray() ?? ImmutableArray<RebuildItem>.Empty;
            }

            public override Value BuildItem(InstructionBuilder builder, ITypeRef captureType, Value capture, ITypeRef parArgsType, Value parArgs)
            {
                var size = this.SharedState.ComputeSizeForType(this.ItemType, builder);
                var innerTuple = builder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate), size);
                this.SharedState.ScopeMgr.AddValue(innerTuple, this.TupleType);
                var typedTuple = builder.BitCast(innerTuple, this.ItemType.CreatePointerType());
                for (int i = 0; i < this.Items.Length; i++)
                {
                    var indices = new Value[] { builder.Context.CreateConstant(0L), builder.Context.CreateConstant(i + 1) };
                    var itemDestPtr = builder.GetElementPtr(this.ItemType, typedTuple, indices);
                    var item = this.Items[i].BuildItem(builder, captureType, capture, parArgsType, parArgs);
                    if (this.Items[i] is InnerTuple)
                    {
                        // if the time is an inner tuple, then we need to cast it to a concrete tuple before storing
                        item = this.SharedState.CurrentBuilder.BitCast(item, this.Items[i].ItemType.CreatePointerType());
                    }
                    builder.Store(item, itemDestPtr);
                }
                return innerTuple;
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
        /// Processes an expression and returns its Value.
        /// </summary>
        /// <param name="ex">The expression to process</param>
        /// <returns>The LLVM Value that represents the result of the expression</returns>
        private Value ProcessAndEvaluateSubexpression(TypedExpression ex)
        {
            this.Transformation.Expressions.OnTypedExpression(ex);
            return this.SharedState.ValueStack.Pop();
        }

        /// <summary>
        /// Returns the number of bytes required for a value of the given type when stored as an element in an array.
        /// Note that non-scalar values all wind up as pointers.
        /// </summary>
        /// <param name="t">The Q# type of the array elements</param>
        /// <returns>The number of bytes required per element</returns>
        private int ComputeSizeForType(ResolvedType t)
        {
            // Sizes in bytes
            // Assumes addresses are 64 bits wide
            return
                t.Resolution.IsBool ? 1 :
                t.Resolution.IsPauli ? 1 :
                t.Resolution.IsInt ? 8 :
                t.Resolution.IsDouble ? 16 :
                t.Resolution.IsRange ? 24 :
                // Everything else is a pointer...
                8;
        }

        /// <summary>
        /// Pushes a value onto the value stack and also adds it to the current ref counting scope.
        /// </summary>
        /// <param name="value">The LLVM value to push</param>
        /// <param name="valueType">The Q# type of the value</param>
        private void PushValueInScope(Value value, ResolvedType valueType)
        {
            this.SharedState.ValueStack.Push(value);
            this.SharedState.ScopeMgr.AddValue(value, valueType);
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
                    var originalElementPointer = this.SharedState.GetTupleElementPointer(originalTypeRef, typedOriginal, i + 1, builder);
                    var originalElement = builder.Load(this.SharedState.LlvmTypeFromQsharpType(elementType), originalElementPointer);
                    Value elementValue = elementType.Resolution switch
                    {
                        QsResolvedTypeKind.TupleType _ =>
                            this.DeepCopyTuple(originalElement, elementType, b),
                        QsResolvedTypeKind.ArrayType _ =>
                            builder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy), originalElement),
                        _ => originalElement,
                    };
                    var copyElementPointer = this.SharedState.GetTupleElementPointer(originalTypeRef, typedCopy, i + 1, builder);
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
        /// Returns true if the expression is an item that should be copied for COW safety, false otherwise.
        /// <br/><br/>
        /// Specifically, an item requires copying if it is an array or a tuple, and if it is an identifier
        /// or an element or a slice of an identifier.
        /// </summary>
        /// <param name="ex">The expression to test.</param>
        /// <returns>true if the expression should be copied before use, false otherwise.</returns>
        private bool ItemRequiresCopying(TypedExpression ex)
        {
            if (ex.ResolvedType.Resolution.IsArrayType || ex.ResolvedType.Resolution.IsUserDefinedType
                || ex.ResolvedType.Resolution.IsTupleType)
            {
                return ex.Expression switch
                {
                    ResolvedExpression.Identifier _ => true,
                    ResolvedExpression.ArrayItem arr => this.ItemRequiresCopying(arr.Item1),
                    _ => false
                };
            }
            else
            {
                return false;
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
            // Evaluating the input always happens on the current builder
            var item = this.ProcessAndEvaluateSubexpression(ex);

            InstructionBuilder builder = b ?? this.SharedState.CurrentBuilder;
            if (this.ItemRequiresCopying(ex))
            {
                Value copy = ex.ResolvedType.Resolution switch
                {
                    QsResolvedTypeKind.UserDefinedType _ => this.DeepCopyUDT(item, ex.ResolvedType, b),
                    QsResolvedTypeKind.TupleType _ => this.DeepCopyTuple(item, ex.ResolvedType, b),
                    QsResolvedTypeKind.ArrayType _ => builder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy), item),
                    _ => Constant.UndefinedValueFor(this.SharedState.LlvmTypeFromQsharpType(ex.ResolvedType)),
                };
                this.SharedState.ScopeMgr.AddValue(copy, ex.ResolvedType);
                return copy;
            }
            else
            {
                return item;
            }
        }

        /// <summary>
        /// Fills in an LLVM tuple from a Q# expression.
        /// The tuple should already be allocated, but any embedded tuples will be allocated by this method.
        /// </summary>
        /// <param name="pointerToTuple">The LLVM tuple to fill in</param>
        /// <param name="expr">The Q# expression to evaluate and fill in the tuple with</param>
        private void FillTuple(Value pointerToTuple, TypedExpression expr)
        {
            void FillStructSlot(IStructType structType, Value pointerToStruct, Value fillValue, int position)
            {
                // Generate a store for the value
                Value[] indices = new Value[]
                {
                    this.SharedState.Context.CreateConstant(0L),
                    this.SharedState.Context.CreateConstant(position)
                };
                var elementPointer = this.SharedState.CurrentBuilder.GetElementPtr(structType, pointerToStruct, indices);
                var castValue = fillValue.NativeType == this.SharedState.Types.Tuple
                    ? this.SharedState.CurrentBuilder.BitCast(fillValue, structType.Members[position])
                    : fillValue;
                this.SharedState.CurrentBuilder.Store(castValue, elementPointer);
            }

            void FillItem(IStructType structType, Value pointerToStruct, TypedExpression fillExpr, int position)
            {
                if (fillExpr.ResolvedType.Resolution.IsTupleType)
                {
                    throw new ArgumentException("expecting non-tuple value");
                }
                var fillValue = this.ProcessAndEvaluateSubexpression(fillExpr);
                FillStructSlot(structType, pointerToStruct, fillValue, position);
            }

            var tupleTypeRef = this.SharedState.LlvmStructTypeFromQsharpType(expr.ResolvedType);
            var tupleToFillPointer = this.SharedState.CurrentBuilder.BitCast(pointerToTuple, tupleTypeRef.CreatePointerType());
            if (expr.Expression is ResolvedExpression.ValueTuple tuple)
            {
                var items = tuple.Item;
                for (var i = 0; i < items.Length; i++)
                {
                    switch (items[i].Expression)
                    {
                        case ResolvedExpression.ValueTuple _:
                            // Handle inner tuples: allocate space. initialize, and then recurse
                            var subTupleTypeRef = ((IPointerType)this.SharedState.LlvmTypeFromQsharpType(items[i].ResolvedType)).ElementType;
                            var subTupleAsTuplePointer = this.SharedState.CreateTupleForType(subTupleTypeRef);
                            var subTupleAsTypedPointer = this.SharedState.CurrentBuilder.BitCast(subTupleAsTuplePointer, subTupleTypeRef.CreatePointerType());
                            FillStructSlot(tupleTypeRef, tupleToFillPointer, subTupleAsTypedPointer, i + 1);
                            this.FillTuple(subTupleAsTypedPointer, items[i]);
                            break;
                        default:
                            FillItem(tupleTypeRef, tupleToFillPointer, items[i], i + 1);
                            break;
                    }
                }
            }
            else
            {
                FillItem(tupleTypeRef, tupleToFillPointer, expr, 1);
            }
        }

        /// <summary>
        /// Binds the variables in a Q# argument tuple to a Q# expression.
        /// Arbitrary tuple nesting in the argument tuple is supported.
        /// <br/><br/>
        /// This method is used when inlining to create the bindings from the
        /// argument expression to the argument variables.
        /// </summary>
        /// <exception cref="ArgumentException">The given expression is inconsistent with the given argument tuple.</exception>
        private void MapTuple(TypedExpression s, QsArgumentTuple d)
        {
            // We keep a queue of pending assignments and apply them after we've evaluated all of the expressions.
            // Effectively we need to do a LET* (parallel let) instead of a LET.
            void MapTupleInner(TypedExpression source, QsArgumentTuple destination, List<(string, Value)> assignmentQueue)
            {
                if (source.Expression.IsUnitValue)
                {
                    // Nothing to do, so bail
                    return;
                }
                else if (destination is QsArgumentTuple.QsTuple tuple)
                {
                    var items = tuple.Item;
                    if (source.Expression is ResolvedExpression.ValueTuple srcItems)
                    {
                        foreach (var (ex, ti) in srcItems.Item.Zip(items, (ex, ti) => (ex, ti)))
                        {
                            MapTupleInner(ex, ti, assignmentQueue);
                        }
                    }
                    else if (items.Length == 1)
                    {
                        MapTupleInner(source, items[0], assignmentQueue);
                    }
                    else
                    {
                        throw new ArgumentException("Argument values are inconsistent with the given expression");
                    }
                }
                else if (destination is QsArgumentTuple.QsTupleItem arg && arg.Item.VariableName is QsLocalSymbol.ValidName varName)
                {
                    var value = this.ProcessAndEvaluateSubexpression(source);
                    assignmentQueue.Add((varName.Item, value));
                }
            }

            var queue = new List<(string, Value)>();
            MapTupleInner(s, d, queue);
            foreach (var (name, value) in queue)
            {
                this.SharedState.RegisterName(name, value);
            }
        }

        private void BuildPartialApplication(TypedExpression method, TypedExpression arg)
        {
            RebuildItem BuildPartialArgList(ResolvedType argType, TypedExpression arg, List<ResolvedType> remainingArgs, List<(Value, ResolvedType)> capturedValues)
            {
                // We need argType because _'s -- missing expressions -- have MissingType, rather than the actual type.
                if (arg.Expression.IsMissingExpr)
                {
                    remainingArgs.Add(argType);
                    return new InnerArg(this.SharedState, this.SharedState.LlvmTypeFromQsharpType(argType), remainingArgs.Count);
                }
                else if (arg.Expression is ResolvedExpression.ValueTuple tuple
                    && argType.Resolution is QsResolvedTypeKind.TupleType types)
                {
                    var itemType = this.SharedState.Types.CreateConcreteTupleType(
                        types.Item.Select(i => this.SharedState.LlvmTypeFromQsharpType(i)));
                    var items = types.Item.Zip(tuple.Item, (t, v) => BuildPartialArgList(t, v, remainingArgs, capturedValues));
                    return new InnerTuple(this.SharedState, argType, itemType, items);
                }
                else
                {
                    // A value we should capture; remember that the first element in the capture tuple is the inner callable
                    var val = this.ProcessAndEvaluateSubexpression(arg);
                    capturedValues.Add((val, argType));
                    return new InnerCapture(this.SharedState, this.SharedState.LlvmTypeFromQsharpType(arg.ResolvedType), capturedValues.Count + 1);
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
                    this.SharedState.ScopeMgr.AddValue(
                        copy,
                        ResolvedType.New(QsResolvedTypeKind.NewOperation(
                            Tuple.Create(
                                ResolvedType.New(QsResolvedTypeKind.UnitType),
                                ResolvedType.New(QsResolvedTypeKind.UnitType)),
                            CallableInformation.NoInformation)));
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

            IrFunction BuildLiftedSpecialization(string name, QsSpecializationKind kind, ITypeRef captureType, ITypeRef parArgsType, RebuildItem rebuild)
            {
                var funcName = GenerationContext.FunctionWrapperName(new QsQualifiedName("Lifted", name), kind);
                var func = this.SharedState.Module.CreateFunction(funcName, this.SharedState.Types.FunctionSignature);

                func.Parameters[0].Name = "capture-tuple";
                func.Parameters[1].Name = "arg-tuple";
                func.Parameters[2].Name = "result-tuple";
                var entry = func.AppendBasicBlock("entry");
                var builder = new InstructionBuilder(entry);
                this.SharedState.ScopeMgr.OpenScope();

                var capturePointer = builder.BitCast(func.Parameters[0], captureType.CreatePointerType());
                Value innerArgTuple;
                if (kind == QsSpecializationKind.QsControlled || kind == QsSpecializationKind.QsControlledAdjoint)
                {
                    var parArgsStruct = (IStructType)parArgsType;

                    // Deal with the extra control qubit arg for controlled and controlled-adjoint
                    // Note that there's a special case if the base specialization only takes a single parameter,
                    // in which case we don't create the sub-tuple.
                    if (parArgsStruct.Members.Count > 2)
                    {
                        var ctlArgsType = this.SharedState.Types.CreateConcreteTupleType(this.SharedState.Types.Array, this.SharedState.Types.Tuple);
                        var ctlArgsPointer = builder.BitCast(func.Parameters[1], ctlArgsType.CreatePointerType());
                        var controlsPointer = builder.GetElementPtr(ctlArgsType, ctlArgsPointer, new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(1)
                        });
                        var restPointer = builder.GetElementPtr(ctlArgsType, ctlArgsPointer, new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(2)
                        });
                        var typedRestPointer = builder.BitCast(restPointer, parArgsType.CreatePointerType());
                        var restTuple = rebuild.BuildItem(builder, captureType, capturePointer, parArgsType, typedRestPointer);
                        var size = this.SharedState.ComputeSizeForType(ctlArgsType, builder);
                        innerArgTuple = builder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate), size);
                        this.SharedState.ScopeMgr.AddValue(innerArgTuple);
                        var typedNewTuple = builder.BitCast(innerArgTuple, ctlArgsType.CreatePointerType());
                        var destControlsPointer = builder.GetElementPtr(ctlArgsType, typedNewTuple, new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(1)
                        });
                        var controls = builder.Load(this.SharedState.Types.Array, controlsPointer);
                        builder.Store(controls, destControlsPointer);
                        var destArgsPointer = builder.GetElementPtr(ctlArgsType, typedNewTuple, new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(2)
                        });
                        builder.Store(restTuple, destArgsPointer);
                    }
                    else if (parArgsStruct.Members.Count == 2)
                    {
                        // First process the incoming argument. Remember, [0] is the %TupleHeader.
                        var singleArgType = parArgsStruct.Members[1];
                        var inputArgsType = this.SharedState.Types.CreateConcreteTupleType(this.SharedState.Types.Array, singleArgType);
                        var inputArgsPointer = builder.BitCast(func.Parameters[1], inputArgsType.CreatePointerType());
                        var controlsPointer = builder.GetElementPtr(inputArgsType, inputArgsPointer, new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(1)
                        });
                        var restPointer = builder.GetElementPtr(inputArgsType, inputArgsPointer, new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(2)
                        });
                        var restValue = builder.Load(singleArgType, restPointer);

                        // OK, now build the full args for the partially-applied callable, other than the controlled qubits
                        var restTuple = rebuild.BuildItem(builder, captureType, capturePointer, singleArgType, restValue);
                        // The full args for the inner callable will include the controls
                        var innerArgType = this.SharedState.Types.CreateConcreteTupleType(this.SharedState.Types.Array, restTuple.NativeType);
                        var size = this.SharedState.ComputeSizeForType(innerArgType, builder);
                        innerArgTuple = builder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate), size);
                        this.SharedState.ScopeMgr.AddValue(innerArgTuple);
                        var typedNewTuple = builder.BitCast(innerArgTuple, innerArgType.CreatePointerType());
                        var destControlsPointer = builder.GetElementPtr(innerArgType, typedNewTuple, new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(1)
                        });
                        var controls = builder.Load(this.SharedState.Types.Array, controlsPointer);
                        builder.Store(controls, destControlsPointer);
                        var destArgsPointer = builder.GetElementPtr(innerArgType, typedNewTuple, new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(2)
                        });
                        builder.Store(restTuple, destArgsPointer);
                    }
                    else
                    {
                        throw new InvalidOperationException("argument tuple is expected to have a least one member in addition to the tuple header");
                    }
                }
                else
                {
                    var parArgsPointer = builder.BitCast(func.Parameters[1], parArgsType.CreatePointerType());
                    innerArgTuple = rebuild.BuildItem(builder, captureType, capturePointer, parArgsType, parArgsPointer);
                }

                var innerCallablePtr = builder.GetElementPtr(captureType, capturePointer, new Value[]
                {
                    this.SharedState.Context.CreateConstant(0L),
                    this.SharedState.Context.CreateConstant(1)
                });
                var innerCallable = builder.Load(this.SharedState.Types.Callable, innerCallablePtr);
                // Depending on the specialization, we may have to get a different specialization of the callable
                var specToCall = GetSpecializedInnerCallable(innerCallable, kind, builder);
                var invoke = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableInvoke);
                builder.Call(invoke, specToCall, innerArgTuple, func.Parameters[2]);

                this.SharedState.ScopeMgr.ForceCloseScope(builder);

                builder.Return();

                return func;
            }

            // Figure out the inputs to the resulting callable based on the signature of the partial application expression.
            var paType = this.SharedState.ExpressionTypeStack.Peek();
            var paArgTuple = paType.Resolution switch
            {
                QsResolvedTypeKind.Function paf => paf.Item1,
                QsResolvedTypeKind.Operation pao => pao.Item1.Item1,
                _ => throw new InvalidOperationException("Partial application of a non-callable value")
            };
            var partialArgType = paArgTuple.Resolution switch
            {
                QsResolvedTypeKind.TupleType pat => this.SharedState.Types.CreateConcreteTupleType(
                    pat.Item.Select(this.SharedState.LlvmTypeFromQsharpType)),
                _ => this.SharedState.LlvmStructTypeFromQsharpType(paArgTuple)
            };

            // And the inputs to the underlying callable
            var innerTupleType = method.ResolvedType.Resolution switch
            {
                QsResolvedTypeKind.Function paf => paf.Item1,
                QsResolvedTypeKind.Operation pao => pao.Item1.Item1,
                _ => throw new InvalidOperationException("Partial application of a non-callable value")
            };

            // Figure out the args & signature of the resulting callable
            var parArgs = new List<ResolvedType>();
            var caps = new List<(Value, ResolvedType)>();
            var rebuild = BuildPartialArgList(innerTupleType, arg, parArgs, caps);

            // Create the capture tuple
            // Note that we set aside the first element of the capture tuple for the inner operation to call
            var capTypeList = caps.Select(c => c.Item1.NativeType).Prepend(this.SharedState.Types.Callable);
            var capType = this.SharedState.Types.CreateConcreteTupleType(capTypeList);
            var cap = this.SharedState.CreateTupleForType(capType);
            var capture = this.SharedState.CurrentBuilder.BitCast(cap, capType.CreatePointerType());
            var callablePointer = this.SharedState.CurrentBuilder.GetElementPtr(capType, capture, new Value[]
            {
                this.SharedState.Context.CreateConstant(0L),
                this.SharedState.Context.CreateConstant(1)
            });
            var innerCallable = this.ProcessAndEvaluateSubexpression(method);
            this.SharedState.CurrentBuilder.Store(innerCallable, callablePointer);
            this.SharedState.ScopeMgr.RemovePendingValue(innerCallable);
            for (int n = 0; n < caps.Count; n++)
            {
                var item = this.SharedState.CurrentBuilder.GetElementPtr(capType, capture, new Value[]
                {
                    this.SharedState.Context.CreateConstant(0L),
                    this.SharedState.Context.CreateConstant(n + 2)
                });
                this.SharedState.CurrentBuilder.Store(caps[n].Item1, item);
                this.SharedState.AddReference(caps[n].Item1);
            }

            // Create the lifted specialization implementation(s)
            // First, figure out which ones we need to create
            var kinds = new HashSet<QsSpecializationKind>
            {
                QsSpecializationKind.QsBody
            };
            if (method.ResolvedType.Resolution is QsResolvedTypeKind.Operation op)
            {
                if (op.Item2.Characteristics.SupportedFunctors.IsValue)
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
            }

            // Now create our specializations
            var liftedName = this.SharedState.GenerateUniqueName("PartialApplication");
            var specializations = new Constant[4];
            for (var index = 0; index < 4; index++)
            {
                var kind = GenerationContext.FunctionArray[index];
                if (kinds.Contains(kind))
                {
                    specializations[index] = BuildLiftedSpecialization(liftedName, kind, capType, partialArgType, rebuild);
                }
                else
                {
                    specializations[index] = Constant.NullValueFor(specializations[0].NativeType);
                }
            }

            // Build the array
            var t = specializations[0].NativeType;
            var array = ConstantArray.From(t, specializations);
            var table = this.SharedState.Module.AddGlobal(array.NativeType, true, Linkage.DllExport, array, liftedName);

            // Create the callable
            var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
            var callableValue = this.SharedState.CurrentBuilder.Call(func, table, cap);

            this.SharedState.ValueStack.Push(callableValue);
            // We cheat on the type because all that the scope manager cares about is that it's a callable
            this.SharedState.ScopeMgr.AddValue(callableValue, method.ResolvedType);
        }

        // public overrides

        public override ResolvedExpression OnAddition(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintAdd);
                this.PushValueInScope(
                    this.SharedState.CurrentBuilder.Call(adder, lhsValue, rhsValue),
                    lhs.ResolvedType);
            }
            else if (lhs.ResolvedType.Resolution.IsString)
            {
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringConcatenate);
                this.PushValueInScope(
                    this.SharedState.CurrentBuilder.Call(adder, lhsValue, rhsValue),
                    lhs.ResolvedType);
            }
            else if (lhs.ResolvedType.Resolution.IsArrayType)
            {
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayConcatenate);
                this.PushValueInScope(
                    this.SharedState.CurrentBuilder.Call(adder, lhsValue, rhsValue),
                    lhs.ResolvedType);
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
            var baseCallable = this.ProcessAndEvaluateSubexpression(ex);

            // If ex was a variable, we need to make a copy before we take the adjoint.
            Value callable;
            if (ex.Expression is ResolvedExpression.Identifier id && id.Item1.IsLocalVariable)
            {
                var copier = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCopy);
                callable = this.SharedState.CurrentBuilder.Call(copier, baseCallable);
                this.SharedState.ScopeMgr.AddValue(callable, ex.ResolvedType);
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
            if (!(arr.ResolvedType.Resolution is QsResolvedTypeKind.ArrayType elementType))
            {
                throw new ArgumentException("expecting expression of type array in array item access");
            }

            // TODO: handle multi-dimensional arrays
            var array = this.ProcessAndEvaluateSubexpression(arr);
            var index = this.ProcessAndEvaluateSubexpression(idx);

            if (idx.ResolvedType.Resolution.IsInt)
            {
                var pointer = this.SharedState.CurrentBuilder.Call(
                    this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d), array, index);

                // Get the element type
                var elementTypeRef = this.SharedState.LlvmTypeFromQsharpType(elementType.Item);
                var elementPointerTypeRef = elementTypeRef.CreatePointerType();

                // And now fetch the element
                var elementPointer = this.SharedState.CurrentBuilder.BitCast(pointer, elementPointerTypeRef);
                var element = this.SharedState.CurrentBuilder.Load(elementTypeRef, elementPointer);
                this.SharedState.ValueStack.Push(element);
            }
            else if (idx.ResolvedType.Resolution.IsRange)
            {
                var slicer = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArraySlice);
                var slice = this.SharedState.CurrentBuilder.Call(slicer, array, this.SharedState.Context.CreateConstant(0), index);
                this.PushValueInScope(slice, arr.ResolvedType);
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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintCreateI64);
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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintCreateArray);
                bigIntValue = this.SharedState.CurrentBuilder.Call(func, n, zeroByteArray);
            }
            this.PushValueInScope(bigIntValue, ResolvedType.New(QsResolvedTypeKind.BigInt));
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseAnd(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.And(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintBitand);
                this.PushValueInScope(
                    this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue),
                    lhs.ResolvedType);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseExclusiveOr(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Xor(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintBitxor);
                this.PushValueInScope(
                    this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue),
                    lhs.ResolvedType);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseNot(TypedExpression ex)
        {
            Value exValue = this.ProcessAndEvaluateSubexpression(ex);

            if (ex.ResolvedType.Resolution.IsInt)
            {
                Value minusOne = this.SharedState.Context.CreateConstant(-1L);
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Xor(exValue, minusOne));
            }
            else if (ex.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintBitnot);
                this.PushValueInScope(
                    this.SharedState.CurrentBuilder.Call(func, exValue),
                    ex.ResolvedType);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(exValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnBitwiseOr(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.Or(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintBitor);
                this.PushValueInScope(
                    this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue),
                    lhs.ResolvedType);
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

            void CallQuantumInstruction(string instructionName)
            {
                var func = this.SharedState.GetOrCreateQuantumFunction(instructionName);
                var argArray = (arg.Expression is ResolvedExpression.ValueTuple tuple)
                    ? tuple.Item.Select(this.ProcessAndEvaluateSubexpression).ToArray()
                    : new Value[] { this.ProcessAndEvaluateSubexpression(arg) };
                var result = this.SharedState.CurrentBuilder.Call(func, argArray);
                this.SharedState.ValueStack.Push(result);
            }

            void InlineCalledRoutine(QsCallable inlinedCallable, bool isAdjoint, bool isControlled)
            {
                var inlineKind = GetSpecializationKind(isAdjoint, isControlled);
                var inlinedSpecialization = inlinedCallable.Specializations.Where(spec => spec.Kind == inlineKind).Single();
                if (isAdjoint && inlinedSpecialization.Implementation is SpecializationImplementation.Generated gen && gen.Item.IsSelfInverse)
                {
                    inlinedSpecialization = inlinedCallable.Specializations.Where(spec => spec.Kind == QsSpecializationKind.QsBody).Single();
                }

                this.SharedState.StartInlining();
                if (inlinedSpecialization.Implementation is SpecializationImplementation.Provided impl)
                {
                    this.MapTuple(arg, impl.Item1);
                    this.Transformation.Statements.OnScope(impl.Item2);
                }
                this.SharedState.StopInlining();

                // If the inlined routine returns Unit, we need to push an extra empty tuple on the stack
                if (inlinedCallable.Signature.ReturnType.Resolution.IsUnitType)
                {
                    this.SharedState.ValueStack.Push(this.SharedState.Types.Tuple.GetNullValue());
                }
            }

            bool SupportedByRuntime(QsQualifiedName name)
            {
                if (name.Equals(BuiltIn.Length.FullName))
                {
                    // The argument should be an array
                    var arrayArg = this.ProcessAndEvaluateSubexpression(arg);
                    var lengthFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetLength);
                    var value = this.SharedState.CurrentBuilder.Call(lengthFunc, arrayArg, this.SharedState.Context.CreateConstant(0));
                    this.SharedState.ValueStack.Push(value);
                    return true;
                }
                else if (name.Equals(BuiltIn.RangeStart.FullName))
                {
                    // The argument should be an range
                    var rangeArg = this.ProcessAndEvaluateSubexpression(arg);
                    var start = this.SharedState.CurrentBuilder.ExtractValue(rangeArg, 0u);
                    this.SharedState.ValueStack.Push(start);
                    return true;
                }
                else if (name.Equals(BuiltIn.RangeStep.FullName))
                {
                    // The argument should be an range
                    var rangeArg = this.ProcessAndEvaluateSubexpression(arg);
                    var step = this.SharedState.CurrentBuilder.ExtractValue(rangeArg, 1u);
                    this.SharedState.ValueStack.Push(step);
                    return true;
                }
                else if (name.Equals(BuiltIn.RangeEnd))
                {
                    // The argument should be an range
                    var rangeArg = this.ProcessAndEvaluateSubexpression(arg);
                    var end = this.SharedState.CurrentBuilder.ExtractValue(rangeArg, 2u);
                    this.SharedState.ValueStack.Push(end);
                    return true;
                }
                else if (name.Equals(BuiltIn.RangeReverse.FullName))
                {
                    // The argument should be an range
                    var rangeArg = this.ProcessAndEvaluateSubexpression(arg);
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

            Value[] BuildControlledArgList(int controlledCount)
            {
                if (!(arg.Expression is ResolvedExpression.ValueTuple tuple) || tuple.Item.Length != 2 || controlledCount < 2)
                {
                    throw new ArgumentException("control count needs to be larger than 1");
                }

                // The arglist will be a 2-tuple with the first element an array of qubits and the second element
                // a 2-tuple containing an array of qubits and another tuple -- possibly with more nesting levels
                var controlArray = this.ProcessAndEvaluateSubexpression(tuple.Item[0]);
                var arrayType = tuple.Item[0].ResolvedType;
                var remainingArgs = tuple.Item[1];
                while (--controlledCount > 0)
                {
                    if (!(remainingArgs.Expression is ResolvedExpression.ValueTuple innerTuple) || innerTuple.Item.Length != 2)
                    {
                        throw new ArgumentException("control count is inconsistent with the shape of the argument tuple");
                    }

                    controlArray = this.SharedState.CurrentBuilder.Call(
                        this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayConcatenate),
                        controlArray,
                        this.ProcessAndEvaluateSubexpression(innerTuple.Item[0]));
                    this.SharedState.ScopeMgr.AddValue(controlArray, arrayType);
                    remainingArgs = innerTuple.Item[1];
                }
                return new[] { controlArray, this.ProcessAndEvaluateSubexpression(remainingArgs) };
            }

            void CallGlobal(Identifier.GlobalCallable callable, QsResolvedTypeKind methodType, bool isAdjoint, int controlledCount)
            {
                var kind = GetSpecializationKind(isAdjoint, controlledCount > 0);
                var func = this.SharedState.GetFunctionByName(callable.Item, kind);

                // If the operation has more than one "Controlled" functor applied, we will need to adjust the arg list
                // and build a single array of control qubits
                Value[] argList;
                if (controlledCount > 1)
                {
                    argList = BuildControlledArgList(controlledCount);
                }
                else if (arg.ResolvedType.Resolution.IsUnitType)
                {
                    argList = new Value[] { };
                }
                else if (arg.ResolvedType.Resolution.IsTupleType && arg.Expression is ResolvedExpression.ValueTuple vs)
                {
                    argList = vs.Item.Select(this.ProcessAndEvaluateSubexpression).ToArray();
                }
                else
                {
                    argList = new Value[] { this.ProcessAndEvaluateSubexpression(arg) };
                }

                var result = this.SharedState.CurrentBuilder.Call(func, argList);
                this.SharedState.ValueStack.Push(result);
                var resultType = methodType switch
                {
                    QsResolvedTypeKind.Function fct => fct.Item2,
                    QsResolvedTypeKind.Operation op => op.Item1.Item2,
                    _ => throw new InvalidOperationException("Call to a non-callable value")
                };
                this.SharedState.ScopeMgr.AddValue(result, resultType);
            }

            void CallCallableValue()
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableInvoke);

                // Build the arg tuple
                ResolvedType argType = arg.ResolvedType;
                Value argTuple;
                if (argType.Resolution.IsUnitType)
                {
                    argTuple = this.SharedState.Types.Tuple.GetNullValue();
                }
                else
                {
                    ITypeRef argStructType = this.SharedState.LlvmStructTypeFromQsharpType(argType);
                    Value argStruct = this.SharedState.CreateTupleForType(argStructType);
                    this.FillTuple(argStruct, arg);
                    argTuple = this.SharedState.CurrentBuilder.BitCast(argStruct, this.SharedState.Types.Tuple);
                }

                // Allocate the result tuple, if needed
                ResolvedType resultResolvedType = this.SharedState.ExpressionTypeStack.Peek();
                ITypeRef? resultStructType = null;
                Value? resultStruct = null;
                Value? resultTuple = null;
                if (resultResolvedType.Resolution.IsUnitType)
                {
                    resultTuple = this.SharedState.Types.Tuple.GetNullValue();
                    this.SharedState.CurrentBuilder.Call(func, this.ProcessAndEvaluateSubexpression(method), argTuple, resultTuple);

                    // Now push the result. For now we assume it's a scalar.
                    this.SharedState.ValueStack.Push(this.SharedState.Types.Tuple.GetNullValue());
                }
                else
                {
                    resultStructType = this.SharedState.LlvmStructTypeFromQsharpType(resultResolvedType);
                    resultTuple = this.SharedState.CreateTupleForType(resultStructType);
                    resultStruct = this.SharedState.CurrentBuilder.BitCast(resultTuple, resultStructType.CreatePointerType());
                    this.SharedState.CurrentBuilder.Call(func, this.ProcessAndEvaluateSubexpression(method), argTuple, resultTuple);

                    // Now push the result. For now we assume it's a scalar.
                    var indices = new Value[] { this.SharedState.Context.CreateConstant(0L), this.SharedState.Context.CreateConstant(1) };
                    Value resultPointer = this.SharedState.CurrentBuilder.GetElementPtr(resultStructType, resultStruct, indices);
                    ITypeRef resultType = this.SharedState.LlvmTypeFromQsharpType(resultResolvedType);
                    Value result = this.SharedState.CurrentBuilder.Load(resultType, resultPointer);
                    this.SharedState.ValueStack.Push(result);
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
                CallQuantumInstruction(qisCode.Item);
            }
            else
            {
                // Resolve Adjoint and Controlled modifiers
                var (baseMethod, isAdjoint, controlledCount) = ResolveModifiers(method, false, 0);

                // Check for, and handle, inlining
                if (baseMethod.Expression is ResolvedExpression.Identifier baseId
                    && baseId.Item1 is Identifier.GlobalCallable baseCallable)
                {
                    if (this.SharedState.TryGetGlobalCallable(baseCallable.Item, out var inlinedCallable)
                        && inlinedCallable.Attributes.Any(BuiltIn.MarksInlining))
                    {
                        InlineCalledRoutine(inlinedCallable, isAdjoint, controlledCount > 0);
                    }
                    // SupportedByRuntime pushes the necessary LLVM if it is supported
                    else if (!SupportedByRuntime(baseCallable.Item))
                    {
                        CallGlobal(baseCallable, baseMethod.ResolvedType.Resolution, isAdjoint, controlledCount);
                    }
                }
                else
                {
                    CallCallableValue();
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

            var condValue = this.ProcessAndEvaluateSubexpression(cond);

            // Special case: if both values are self-evaluating (literals or simple identifiers), we can
            // do this with a select.
            if (ExpressionIsSelfEvaluating(ifTrue) && ExpressionIsSelfEvaluating(ifFalse))
            {
                var trueValue = this.ProcessAndEvaluateSubexpression(ifTrue);
                var falseValue = this.ProcessAndEvaluateSubexpression(ifFalse);
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
                var trueValue = this.ProcessAndEvaluateSubexpression(ifTrue);
                this.SharedState.CurrentBuilder.Branch(contBlock);

                this.SharedState.SetCurrentBlock(falseBlock);
                var falseValue = this.ProcessAndEvaluateSubexpression(ifFalse);
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
            var baseCallable = this.ProcessAndEvaluateSubexpression(ex);

            // If ex was a variable, we need to make a copy before we take the adjoint.
            Value callable;

            if (ex.Expression is ResolvedExpression.Identifier id && id.Item1.IsLocalVariable)
            {
                var copier = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCopy);
                callable = this.SharedState.CurrentBuilder.Call(copier, baseCallable);
                this.SharedState.ScopeMgr.AddValue(callable, ex.ResolvedType);
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
            if (lhs.ResolvedType.Resolution is QsResolvedTypeKind.ArrayType itemType)
            {
                var array = this.GetWritableCopy(lhs);
                if (accEx.ResolvedType.Resolution.IsInt)
                {
                    var index = this.ProcessAndEvaluateSubexpression(accEx);
                    var value = this.ProcessAndEvaluateSubexpression(rhs);
                    var elementType = this.SharedState.LlvmTypeFromQsharpType(itemType.Item);
                    var rawElementPtr = this.SharedState.CurrentBuilder.Call(
                        this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d), array, index);
                    var elementPtr = this.SharedState.CurrentBuilder.BitCast(rawElementPtr, elementType.CreatePointerType());
                    this.SharedState.CurrentBuilder.Store(value, elementPtr);
                }
                else if (accEx.ResolvedType.Resolution.IsRange)
                {
                    // TODO: handle range updates
                    throw new NotImplementedException("Array slice updates");
                }
                this.SharedState.ValueStack.Push(array);
            }
            else if (lhs.ResolvedType.Resolution is QsResolvedTypeKind.UserDefinedType tt)
            {
                var location = new List<(int, ITypeRef)>();
                if (this.SharedState.TryGetCustomType(tt.Item.GetFullName(), out QsCustomType? udt)
                    && accEx.Expression is ResolvedExpression.Identifier acc
                    && acc.Item1 is Identifier.LocalVariable loc
                    && this.FindNamedItem(loc.Item, udt.TypeItems, location))
                {
                    // The location list is backwards, by design, so we have to reverse it
                    location.Reverse();
                    var copy = this.GetWritableCopy(lhs);
                    var current = copy;
                    for (int i = 0; i < location.Count; i++)
                    {
                        var indices = new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(location[i].Item1)
                        };
                        var ptr = this.SharedState.CurrentBuilder.GetElementPtr(((IPointerType)location[i].Item2).ElementType, current, indices);
                        // For the last item on the list, we store; otherwise, we load the next tuple
                        if (i == location.Count - 1)
                        {
                            var value = this.ProcessAndEvaluateSubexpression(rhs);
                            this.SharedState.CurrentBuilder.Store(value, ptr);
                        }
                        else
                        {
                            current = this.SharedState.CurrentBuilder.Load(location[i + 1].Item2, ptr);
                        }
                    }
                    this.SharedState.ValueStack.Push(copy);
                }
                else
                {
                    this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Types.Tuple));
                }
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Types.Int));
            }
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnDivision(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintDivide);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue), lhs.ResolvedType);
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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var value = this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintEqual), lhsValue, rhsValue);
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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var powFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintPower);
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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintGreater);
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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintGreaterEq);
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
                string name = local.Item;
                this.SharedState.PushNamedValue(name);
            }
            else if (sym is Identifier.GlobalCallable globalCallable)
            {
                if (this.SharedState.TryGetGlobalCallable(globalCallable.Item, out QsCallable? callable))
                {
                    var wrapper = this.SharedState.GetOrCreateWrapper(callable);
                    var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
                    var callableValue = this.SharedState.CurrentBuilder.Call(func, wrapper, this.SharedState.Types.Tuple.GetNullValue());

                    this.SharedState.ValueStack.Push(callableValue);
                    this.SharedState.ScopeMgr.AddValue(
                        callableValue,
                        ResolvedType.New(QsResolvedTypeKind.NewOperation(
                            new Tuple<ResolvedType, ResolvedType>(callable.Signature.ArgumentType, callable.Signature.ReturnType),
                            CallableInformation.NoInformation)));
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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var eq = this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintEqual), lhsValue, rhsValue);
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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.ShiftLeft(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintShiftleft);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue), lhs.ResolvedType);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }
            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnLessThan(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintGreaterEq);
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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintGreater);
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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
            Value exValue = this.ProcessAndEvaluateSubexpression(ex);

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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.SRem(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintModulus);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue), lhs.ResolvedType);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnMultiplication(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintMultiply);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue), lhs.ResolvedType);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnNamedItem(TypedExpression ex, Identifier acc)
        {
            var t = ex.ResolvedType;
            if (t.Resolution is QsResolvedTypeKind.UserDefinedType tt
                && this.SharedState.TryGetCustomType(tt.Item.GetFullName(), out QsCustomType? udt)
                && acc is Identifier.LocalVariable itemName)
            {
                var location = new List<(int, ITypeRef)>();
                if (this.FindNamedItem(itemName.Item, udt.TypeItems, location))
                {
                    // The location list is backwards, by design, so we have to reverse it
                    location.Reverse();
                    var value = this.ProcessAndEvaluateSubexpression(ex);
                    for (int i = 0; i < location.Count; i++)
                    {
                        var indices = new Value[]
                        {
                            this.SharedState.Context.CreateConstant(0L),
                            this.SharedState.Context.CreateConstant(location[i].Item1)
                        };
                        var ptr = this.SharedState.CurrentBuilder.GetElementPtr(((IPointerType)location[i].Item2).ElementType, value, indices);
#pragma warning disable CS0618 // Computing the correct type for ptr here is awkward, so we don't bother
                        value = this.SharedState.CurrentBuilder.Load(ptr);
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    this.SharedState.ValueStack.Push(value);
                }
                else
                {
                    this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Types.Int));
                }
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(this.SharedState.Types.Int));
            }

            return ResolvedExpression.InvalidExpr;
        }

        private bool FindNamedItem(string name, QsTuple<QsTypeItem> items, List<(int, ITypeRef)> location)
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
                        // This should never happen
                        return this.SharedState.Context.TokenType;
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
                            // +1 to skip the tuple header
                            location.Add((i + 1, GetTypeItemType(items)));
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public override ResolvedExpression OnNegative(TypedExpression ex)
        {
            Value exValue = this.ProcessAndEvaluateSubexpression(ex);

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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintNegate);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, exValue), ex.ResolvedType);
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
            var elementSize = this.ComputeSizeForType(elementType);
            var length = this.ProcessAndEvaluateSubexpression(idx);

            var createFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCreate1d);
            var array = this.SharedState.CurrentBuilder.Call(createFunc, this.SharedState.Context.CreateConstant(elementSize), length);
            this.PushValueInScope(array, ResolvedType.New(QsResolvedTypeKind.NewArrayType(elementType)));

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
                    start = this.ProcessAndEvaluateSubexpression(lit.Item1);
                    step = this.ProcessAndEvaluateSubexpression(lit.Item2);
                    break;
                default:
                    start = this.ProcessAndEvaluateSubexpression(lhs);
                    step = this.SharedState.Context.CreateConstant(1L);
                    break;
            }

            var end = this.ProcessAndEvaluateSubexpression(rhs);

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
            this.SharedState.ValueStack.Push(value);

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnRightShift(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

            if (lhs.ResolvedType.Resolution.IsInt)
            {
                this.SharedState.ValueStack.Push(this.SharedState.CurrentBuilder.ArithmeticShiftRight(lhsValue, rhsValue));
            }
            else if (lhs.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintShiftright);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue), lhs.ResolvedType);
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
                var exValue = this.ProcessAndEvaluateSubexpression(ex);
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
                    var s = this.ProcessAndEvaluateSubexpression(ex);
                    if (ex.Expression.IsIdentifier)
                    {
                        var stringValue = this.SharedState.CurrentBuilder.Call(
                            this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringReference), s);
                    }
                    return s;
                }
                else if (ty.IsBigInt)
                {
                    return SimpleToString(ex, RuntimeLibrary.BigintToString);
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
                this.SharedState.ValueStack.Push(stringValue);
                this.SharedState.ScopeMgr.AddValue(stringValue, ResolvedType.New(QsResolvedTypeKind.String));
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
                this.SharedState.ValueStack.Push(current);
                this.SharedState.ScopeMgr.AddValue(current, ResolvedType.New(QsResolvedTypeKind.String));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnSubtraction(TypedExpression lhs, TypedExpression rhs)
        {
            Value lhsValue = this.ProcessAndEvaluateSubexpression(lhs);
            Value rhsValue = this.ProcessAndEvaluateSubexpression(rhs);

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
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigintSubtract);
                this.PushValueInScope(this.SharedState.CurrentBuilder.Call(func, lhsValue, rhsValue), lhs.ResolvedType);
            }
            else
            {
                this.SharedState.ValueStack.Push(Constant.UndefinedValueFor(lhsValue.NativeType));
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnUnitValue()
        {
            this.SharedState.ValueStack.Push(this.SharedState.Types.Tuple.GetNullValue());

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnValueArray(ImmutableArray<TypedExpression> vs)
        {
            // TODO: handle multi-dimensional arrays
            long length = vs.Length;

            // Get the element type
            var elementType = vs[0].ResolvedType;
            var elementTypeRef = this.SharedState.LlvmTypeFromQsharpType(elementType);
            var elementPointerTypeRef = elementTypeRef.CreatePointerType();
            var elementSize = this.ComputeSizeForType(elementType);

            var createFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCreate1d);
            var array = this.SharedState.CurrentBuilder.Call(
                createFunc,
                this.SharedState.Context.CreateConstant(elementSize),
                this.SharedState.Context.CreateConstant(length));

            long idx = 0;
            foreach (var element in vs)
            {
                var pointer = this.SharedState.CurrentBuilder.Call(
                    this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d),
                    array,
                    this.SharedState.Context.CreateConstant(idx));

                // And now fill in the element
                var elementValue = this.ProcessAndEvaluateSubexpression(element);
                var elementPointer = this.SharedState.CurrentBuilder.BitCast(pointer, elementPointerTypeRef);
                this.SharedState.CurrentBuilder.Store(elementValue, elementPointer);
                idx++;
            }

            this.PushValueInScope(array, ResolvedType.New(QsResolvedTypeKind.NewArrayType(elementType)));

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnValueTuple(ImmutableArray<TypedExpression> vs)
        {
            // Build the LLVM structure type we need
            var itemTypes = vs.Select(v => this.SharedState.LlvmTypeFromQsharpType(v.ResolvedType));
            var tupleType = this.SharedState.Types.CreateConcreteTupleType(itemTypes);

            // Allocate the tuple and record it to get released later
            var tuple = this.SharedState.CreateTupleForType(tupleType);
            var concreteTuple = this.SharedState.CurrentBuilder.BitCast(tuple, tupleType.CreatePointerType());
            this.PushValueInScope(
                concreteTuple,
                ResolvedType.New(QsResolvedTypeKind.NewTupleType(vs.Select(v => v.ResolvedType).ToImmutableArray())));

            // Fill it in, field by field
            for (int i = 0; i < vs.Length; i++)
            {
                var itemValue = this.ProcessAndEvaluateSubexpression(vs[i]);
                var itemPointer = this.SharedState.GetTupleElementPointer(tupleType, concreteTuple, i + 1);
                this.SharedState.CurrentBuilder.Store(itemValue, itemPointer);
            }

            return ResolvedExpression.InvalidExpr;
        }

        public override ResolvedExpression OnUnwrapApplication(TypedExpression ex)
        {
            var exValue = this.ProcessAndEvaluateSubexpression(ex);

            // Since we simply represent user defined types as tuples, we don't need to do anything
            // except pushing the value on the value stack unless the tuples contains a single item,
            // in which case we need to remove the tuple wrapping.
            if (ex.ResolvedType.Resolution is QsResolvedTypeKind.UserDefinedType udt
                && this.SharedState.TryGetCustomType(udt.Item.GetFullName(), out var udtDecl)
                && !udtDecl.Type.Resolution.IsTupleType)
            {
                var tupleType = this.SharedState.LlvmStructTypeFromQsharpType(ex.ResolvedType);
                var tuplePointer = this.SharedState.CurrentBuilder.BitCast(exValue, tupleType.CreatePointerType());

                // we need to access the second item, since the first is the tuple header
                var itemType = this.SharedState.LlvmTypeFromQsharpType(udtDecl.Type);
                var itemPointer = this.SharedState.CurrentBuilder.GetElementPtr(
                     tupleType,
                     tuplePointer,
                     new[] { this.SharedState.Context.CreateConstant(0L), this.SharedState.Context.CreateConstant(1) });

                var element = this.SharedState.CurrentBuilder.Load(itemType, itemPointer);
                this.SharedState.ValueStack.Push(element);
            }
            else
            {
                this.SharedState.ValueStack.Push(exValue);
            }

            return ResolvedExpression.InvalidExpr;
        }
    }
}
