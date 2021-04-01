// -----------------------------------------------------------------------
// <copyright file="InstructionBuilder.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>LLVM Instruction builder allowing managed code to generate IR instructions.</summary>
    public sealed unsafe class InstructionBuilder
    {
        /// <summary>Initializes a new instance of the <see cref="InstructionBuilder"/> class for a given <see cref="Llvm.Context"/>.</summary>
        /// <param name="context">Context used for creating instructions.</param>
        public InstructionBuilder(Context context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.BuilderHandle = context.ContextHandle.CreateBuilder();
        }

        /// <summary>Initializes a new instance of the <see cref="InstructionBuilder"/> class for a <see cref="BasicBlock"/>.</summary>
        /// <param name="block">Block this builder is initially attached to.</param>
        public InstructionBuilder(BasicBlock block)
            : this(block.Context)
        {
            this.PositionAtEnd(block);
        }

        /// <summary>Gets the context this builder is creating instructions for.</summary>
        public Context Context { get; }

        /// <summary>Gets the <see cref="BasicBlock"/> this builder is building instructions for.</summary>
        public BasicBlock? InsertBlock
        {
            get
            {
                var handle = this.BuilderHandle.InsertBlock;
                return handle == default ? default : BasicBlock.FromHandle(this.BuilderHandle.InsertBlock);
            }
        }

        /// <summary>Gets the function this builder currently inserts into.</summary>
        public IrFunction? InsertFunction => this.InsertBlock?.ContainingFunction;

        internal LLVMBuilderRef BuilderHandle { get; }

        /// <summary>Creates a <see cref="Value"/> that accesses an element of a type referenced by a pointer.</summary>
        /// <param name="pointer">pointer to get an element from.</param>
        /// <param name="args">additional indices for computing the resulting pointer.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a User as LLVM may
        /// optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        /// <remarks>
        /// For details on GetElementPointer (GEP) see
        /// <see href="xref:llvm_misunderstood_gep">The Often Misunderstood GEP Instruction</see>.
        /// The basic gist is that the GEP instruction does not access memory, it only computes a pointer
        /// offset from a base. A common confusion is around the first index and what it means. For C
        /// and C++ programmers an expression like pFoo->bar seems to only have a single offset or
        /// index. However that is only syntactic sugar where the compiler implicitly hides the first
        /// index. That is, there is no difference between pFoo[0].bar and pFoo->bar except that the
        /// former makes the first index explicit. LLVM requires an explicit first index, even if it is
        /// zero, in order to properly compute the offset for a given element in an aggregate type.
        /// </remarks>
        public static Value ConstGetElementPtrInBounds(Value pointer, params Value[] args)
        {
            var llvmArgs = GetValidatedGEPArgs(pointer.NativeType, pointer, args);
            fixed (LLVMValueRef* pLlvmArgs = llvmArgs.AsSpan())
            {
                var handle = LLVM.ConstInBoundsGEP(pointer.ValueHandle, (LLVMOpaqueValue**)pLlvmArgs, (uint)llvmArgs.Length);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Positions the builder at the end of a given <see cref="BasicBlock"/>.</summary>
        /// <param name="basicBlock">Block to set the position of.</param>
        public void PositionAtEnd(BasicBlock basicBlock)
        {
            if (basicBlock == default)
            {
                throw new ArgumentNullException(nameof(basicBlock));
            }

            this.BuilderHandle.PositionAtEnd(basicBlock.BlockHandle);
        }

        /// <summary>Positions the builder before the given instruction.</summary>
        /// <param name="instr">Instruction to position the builder before.</param>
        /// <remarks>This method will position the builder to add new instructions
        /// immediately before the specified instruction.
        /// <note type="note">It is important to keep in mind that this can change the
        /// block this builder is targeting. That is, <paramref name="instr"/>
        /// is not required to come from the same block the instruction builder is
        /// currently referencing.</note>
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public void PositionBefore(Instruction instr)
        {
            if (instr == default)
            {
                throw new ArgumentNullException(nameof(instr));
            }

            this.BuilderHandle.PositionBefore(instr.ValueHandle);
        }

        /// <summary>Appends a basic block after the <see cref="InsertBlock"/> of this <see cref="InstructionBuilder"/>.</summary>
        /// <param name="block">Block to insert.</param>
        public void AppendBasicBlock(BasicBlock block)
        {
            LLVM.InsertExistingBasicBlockAfterInsertBlock(this.BuilderHandle, block.BlockHandle);
        }

        /// <summary>Creates a floating point negation operator.</summary>
        /// <param name="value">value to negate.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value FNeg(Value value) => this.BuildUnaryOp((b, v) => LLVM.BuildFNeg(b, v, string.Empty.AsMarshaledString()), value);

        /// <summary>Creates a floating point add operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value FAdd(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildFAdd(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates a floating point subtraction operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value FSub(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildFSub(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates a floating point multiple operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value FMul(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildFMul(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates a floating point division operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value FDiv(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildFDiv(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer negation operator.</summary>
        /// <param name="value">operand to negate.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value Neg(Value value) => this.BuildUnaryOp((b, v) => LLVM.BuildNeg(b, v, string.Empty.AsMarshaledString()), value);

        /// <summary>Creates an integer logical not operator.</summary>
        /// <param name="value">operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        /// <remarks>LLVM IR doesn't actually have a logical not instruction so this is implemented as value XOR {one}. </remarks>
        public Value Not(Value value) => this.BuildUnaryOp((b, v) => LLVM.BuildNot(b, v, string.Empty.AsMarshaledString()), value);

        /// <summary>Creates an integer add operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value Add(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildAdd(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer bitwise and operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value And(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildAnd(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer subtraction operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value Sub(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildSub(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer multiplication operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value Mul(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildMul(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer shift left operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value ShiftLeft(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildShl(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer arithmetic shift right operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value ArithmeticShiftRight(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildAShr(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer logical shift right operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value LogicalShiftRight(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildLShr(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer unsigned division operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value UDiv(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildUDiv(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer signed division operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value SDiv(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildUDiv(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer unsigned remainder operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value URem(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildURem(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer signed remainder operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value SRem(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildSRem(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer bitwise exclusive or operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value Xor(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildXor(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an integer bitwise or operator.</summary>
        /// <param name="lhs">left hand side operand.</param>
        /// <param name="rhs">right hand side operand.</param>
        /// <returns><see cref="Value"/> for the instruction.</returns>
        public Value Or(Value lhs, Value rhs) => this.BuildBinOp((b, v1, v2) => LLVM.BuildOr(b, v1, v2, string.Empty.AsMarshaledString()), lhs, rhs);

        /// <summary>Creates an alloca instruction.</summary>
        /// <param name="typeRef">Type of the value to allocate.</param>
        /// <returns><see cref="Instructions.Alloca"/> instruction.</returns>
        public Alloca Alloca(ITypeRef typeRef)
        {
            var handle = this.BuilderHandle.BuildAlloca(typeRef.GetTypeRef(), string.Empty);
            if (handle == default)
            {
                throw new InternalCodeGeneratorException("Failed to build an Alloca instruction");
            }

            return Value.FromHandle<Alloca>(handle)!;
        }

        /// <summary>Creates an alloca instruction.</summary>
        /// <param name="typeRef">Type of the value to allocate.</param>
        /// <param name="elements">Number of elements to allocate.</param>
        /// <returns><see cref="Instructions.Alloca"/> instruction.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public Alloca Alloca(ITypeRef typeRef, ConstantInt elements)
        {
            if (typeRef == default)
            {
                throw new ArgumentNullException(nameof(typeRef));
            }

            if (elements == default)
            {
                throw new ArgumentNullException(nameof(elements));
            }

            var instHandle = this.BuilderHandle.BuildArrayAlloca(typeRef.GetTypeRef(), elements.ValueHandle, string.Empty);
            if (instHandle == default)
            {
                throw new InternalCodeGeneratorException("Failed to build an Alloca array instruction");
            }

            return Value.FromHandle<Alloca>(instHandle)!;
        }

        /// <summary>Creates a return instruction for a function that has no return value.</summary>
        /// <returns><see cref="ReturnInstruction"/>.</returns>
        /// <exception cref="ArgumentException"> the function has a non-void return type.</exception>
        public ReturnInstruction Return()
        {
            if (this.InsertBlock == default)
            {
                throw new InvalidOperationException("No insert block is set for this builder");
            }

            if (this.InsertBlock.ContainingFunction == default)
            {
                throw new InvalidOperationException("Insert block is not associated with a function; inserting a return requires validation of the function signature");
            }

            if (!this.InsertBlock.ContainingFunction.ReturnType.IsVoid)
            {
                throw new ArgumentException("Return instruction for non-void function must have a value");
            }

            return Value.FromHandle<ReturnInstruction>(this.BuilderHandle.BuildRetVoid())!;
        }

        /// <summary>Creates a return instruction with the return value for a function.</summary>
        /// <param name="value"><see cref="Value"/> to return.</param>
        /// <returns><see cref="ReturnInstruction"/>.</returns>
        public ReturnInstruction Return(Value value)
        {
            if (this.InsertBlock == default)
            {
                throw new InvalidOperationException("No insert block is set for this builder");
            }

            if (this.InsertBlock.ContainingFunction == default)
            {
                throw new InvalidOperationException("Insert block is not associated with a function; inserting a return requires validation of the function signature");
            }

            var retType = this.InsertBlock.ContainingFunction.ReturnType;
            if (retType.IsVoid)
            {
                throw new ArgumentException();
            }

            if (retType != value.NativeType)
            {
                throw new ArgumentException();
            }

            var handle = this.BuilderHandle.BuildRet(value.ValueHandle);
            return Value.FromHandle<ReturnInstruction>(handle)!;
        }

        /// <summary>Creates a call function.</summary>
        /// <param name="func">Function to call.</param>
        /// <param name="args">Arguments to pass to the function.</param>
        /// <returns><see cref="CallInstruction"/>.</returns>
        public CallInstruction Call(Value func, params Value[] args) => this.Call(func, (IReadOnlyList<Value>)args);

        /// <summary>Creates a call function.</summary>
        /// <param name="func">Function to call.</param>
        /// <param name="args">Arguments to pass to the function.</param>
        /// <returns><see cref="CallInstruction"/>.</returns>
        public CallInstruction Call(Value func, IReadOnlyList<Value> args)
        {
            LLVMValueRef hCall = this.BuildCall(func, args);
            return Value.FromHandle<CallInstruction>(hCall)!;
        }

        /// <summary>Builds an LLVM Store instruction.</summary>
        /// <param name="value">Value to store in destination.</param>
        /// <param name="destination">value for the destination.</param>
        /// <returns><see cref="Instructions.Store"/> instruction.</returns>
        /// <remarks>
        /// Since store targets memory the type of <paramref name="destination"/>
        /// must be an <see cref="IPointerType"/>. Furthermore, the element type of
        /// the pointer must match the type of <paramref name="value"/>. Otherwise,
        /// an <see cref="ArgumentException"/> is thrown.
        /// </remarks>
        public Store Store(Value value, Value destination)
        {
            if (!(destination.NativeType is IPointerType ptrType))
            {
                throw new ArgumentException();
            }

            if (!ptrType.ElementType.Equals(value.NativeType)
             || (value.NativeType.Kind == TypeKind.Integer && value.NativeType.IntegerBitWidth != ptrType.ElementType.IntegerBitWidth))
            {
                throw new ArgumentException();
            }

            LLVMValueRef valueRef = this.BuilderHandle.BuildStore(value.ValueHandle, destination.ValueHandle);
            return Value.FromHandle<Store>(valueRef)!;
        }

        /// <summary>Creates a <see cref="Instructions.Load"/> instruction.</summary>
        /// <param name="sourcePtr">Pointer to the value to load.</param>
        /// <returns><see cref="Instructions.Load"/>.</returns>
        /// <remarks>The <paramref name="sourcePtr"/> must not be an opaque pointer type.</remarks>
        [Obsolete("Use overload accepting a type and opaque pointer instead")]
        public Load Load(Value sourcePtr)
        {
            if (!(sourcePtr.NativeType is IPointerType ptrType))
            {
                throw new ArgumentException();
            }

            return this.Load(ptrType.ElementType, sourcePtr);
        }

        /// <summary>Creates a load instruction.</summary>
        /// <param name="type">Type of the value to load.</param>
        /// <param name="sourcePtr">pointer to load the value from.</param>
        /// <returns>Load instruction.</returns>
        /// <remarks>
        /// The <paramref name="type"/> of the value must be a sized type (e.g. not Opaque with a non-zero size ).
        /// if <paramref name="sourcePtr"/> is a non-opaque pointer then its ElementType must be the same as <paramref name="type"/>.
        /// </remarks>
        public Load Load(ITypeRef type, Value sourcePtr)
        {
            if (sourcePtr.NativeType.Kind != TypeKind.Pointer)
            {
                throw new ArgumentException();
            }

            if (!type.IsSized)
            {
                throw new ArgumentException();
            }

            // TODO: validate sourceptr is opaque or sourcePtr.Type.ElementType == type
            var handle = LLVM.BuildLoad2(this.BuilderHandle, type.GetTypeRef(), sourcePtr.ValueHandle, string.Empty.AsMarshaledString());
            return Value.FromHandle<Load>(handle)!;
        }

        /// <summary>Creates a <see cref="Value"/> that accesses an element (field) of a structure.</summary>
        /// <param name="pointer">pointer to the structure to get an element from.</param>
        /// <param name="index">element index.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a <see cref="Value"/>
        /// as LLVM may optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        [Obsolete("Use the overload that takes a type and opaque pointer")]
        public Value GetStructElementPointer(Value pointer, uint index)
        {
            ValidateStructGepArgs(pointer, index);

            // TODO: verify pointer isn't an opaque pointer
            var handle = LLVM.BuildStructGEP2(this.BuilderHandle, pointer.NativeType.GetTypeRef(), pointer.ValueHandle, index, string.Empty.AsMarshaledString());
            return Value.FromHandle(handle)!;
        }

        /// <summary>Creates a <see cref="Value"/> that accesses an element (field) of a structure.</summary>
        /// <param name="type">Type of the pointer.</param>
        /// <param name="pointer">OPaque pointer to the structure to get an element from.</param>
        /// <param name="index">element index.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a <see cref="Value"/>
        /// as LLVM may optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        public Value GetStructElementPointer(ITypeRef type, Value pointer, uint index)
        {
            ValidateStructGepArgs(pointer, index);

            // TODO: verify pointer is an opaque pointer or type == pointer.NativeTYpe
            var handle = LLVM.BuildStructGEP2(this.BuilderHandle, type.GetTypeRef(), pointer.ValueHandle, index, string.Empty.AsMarshaledString());
            return Value.FromHandle(handle)!;
        }

        /// <summary>Creates a <see cref="Value"/> that accesses an element of a type referenced by a pointer.</summary>
        /// <param name="type">Type of array,vector or structure to get the element pointer from.</param>
        /// <param name="pointer">opaque pointer to get an element from.</param>
        /// <param name="args">additional indices for computing the resulting pointer.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a <see cref="Value"/>
        /// as LLVM may optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        /// <remarks>
        /// For details on GetElementPointer (GEP) see
        /// <see href="xref:llvm_misunderstood_gep">The Often Misunderstood GEP Instruction</see>.
        /// The basic gist is that the GEP instruction does not access memory, it only computes a pointer
        /// offset from a base. A common confusion is around the first index and what it means. For C
        /// and C++ programmers an expression like pFoo->bar seems to only have a single offset or
        /// index. However, that is only syntactic sugar where the compiler implicitly hides the first
        /// index. That is, there is no difference between pFoo[0].bar and pFoo->bar except that the
        /// former makes the first index explicit. LLVM requires an explicit first index, even if it is
        /// zero, in order to properly compute the offset for a given element in an aggregate type.
        /// </remarks>
        public Value GetElementPtr(ITypeRef type, Value pointer, IEnumerable<Value> args)
        {
            var llvmArgs = GetValidatedGEPArgs(type, pointer, args);
            fixed (LLVMValueRef* pLlvmArgs = llvmArgs.AsSpan())
            {
                var handle = LLVM.BuildGEP2(
                    this.BuilderHandle,
                    type.GetTypeRef(),
                    pointer.ValueHandle,
                    (LLVMOpaqueValue**)pLlvmArgs,
                    (uint)llvmArgs.Length,
                    string.Empty.AsMarshaledString());
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a <see cref="Value"/> that accesses an element of a type referenced by a pointer.</summary>
        /// <param name="pointer">pointer to get an element from.</param>
        /// <param name="args">additional indices for computing the resulting pointer.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a <see cref="Value"/>
        /// as LLVM may optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        /// <remarks>
        /// For details on GetElementPointer (GEP) see
        /// <see href="xref:llvm_misunderstood_gep">The Often Misunderstood GEP Instruction</see>.
        /// The basic gist is that the GEP instruction does not access memory, it only computes a pointer
        /// offset from a base. A common confusion is around the first index and what it means. For C
        /// and C++ programmers an expression like pFoo->bar seems to only have a single offset or
        /// index. However, that is only syntactic sugar where the compiler implicitly hides the first
        /// index. That is, there is no difference between pFoo[0].bar and pFoo->bar except that the
        /// former makes the first index explicit. LLVM requires an explicit first index, even if it is
        /// zero, in order to properly compute the offset for a given element in an aggregate type.
        /// </remarks>
        public Value GetElementPtr(Value pointer, IEnumerable<Value> args)
            => this.GetElementPtr(pointer.NativeType, pointer, args);

        /// <summary>Creates a <see cref="Value"/> that accesses an element of a type referenced by a pointer.</summary>
        /// <param name="pointer">pointer to get an element from.</param>
        /// <param name="args">additional indices for computing the resulting pointer.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a <see cref="Value"/>
        /// as LLVM may optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        /// <remarks>
        /// For details on GetElementPointer (GEP) see
        /// <see href="xref:llvm_misunderstood_gep">The Often Misunderstood GEP Instruction</see>.
        /// The basic gist is that the GEP instruction does not access memory, it only computes a pointer
        /// offset from a base. A common confusion is around the first index and what it means. For C
        /// and C++ programmers an expression like pFoo->bar seems to only have a single offset or
        /// index. However, that is only syntactic sugar where the compiler implicitly hides the first
        /// index. That is, there is no difference between pFoo[0].bar and pFoo->bar except that the
        /// former makes the first index explicit. LLVM requires an explicit first index, even if it is
        /// zero, in order to properly compute the offset for a given element in an aggregate type.
        /// </remarks>
        public Value GetElementPtr(Value pointer, params Value[] args) => this.GetElementPtr(pointer, (IEnumerable<Value>)args);

        /// <summary>Creates a <see cref="Value"/> that accesses an element of a type referenced by a pointer.</summary>
        /// <param name="pointer">pointer to get an element from.</param>
        /// <param name="args">additional indices for computing the resulting pointer.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a <see cref="Value"/>
        /// as LLVM may optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        /// <remarks>
        /// For details on GetElementPointer (GEP) see
        /// <see href="xref:llvm_misunderstood_gep">The Often Misunderstood GEP Instruction</see>.
        /// The basic gist is that the GEP instruction does not access memory, it only computes a pointer
        /// offset from a base. A common confusion is around the first index and what it means. For C
        /// and C++ programmers an expression like pFoo->bar seems to only have a single offset or
        /// index. However, that is only syntactic sugar where the compiler implicitly hides the first
        /// index. That is, there is no difference between pFoo[0].bar and pFoo->bar except that the
        /// former makes the first index explicit. LLVM requires an explicit first index, even if it is
        /// zero, in order to properly compute the offset for a given element in an aggregate type.
        /// </remarks>
        [Obsolete("Use overload that takes a pointer type and opaque pointer")]
        public Value GetElementPtrInBounds(Value pointer, IEnumerable<Value> args)
        {
            return this.GetElementPtrInBounds(pointer.NativeType, pointer, args);
        }

        /// <summary>Creates a <see cref="Value"/> that accesses an element of a type referenced by a pointer.</summary>
        /// <param name="type">Base pointer type.</param>
        /// <param name="pointer">opaque pointer to get an element from.</param>
        /// <param name="args">additional indices for computing the resulting pointer.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a <see cref="Value"/>
        /// as LLVM may optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        /// <remarks>
        /// For details on GetElementPointer (GEP) see
        /// <see href="xref:llvm_misunderstood_gep">The Often Misunderstood GEP Instruction</see>.
        /// The basic gist is that the GEP instruction does not access memory, it only computes a pointer
        /// offset from a base. A common confusion is around the first index and what it means. For C
        /// and C++ programmers an expression like pFoo->bar seems to only have a single offset or
        /// index. However, that is only syntactic sugar where the compiler implicitly hides the first
        /// index. That is, there is no difference between pFoo[0].bar and pFoo->bar except that the
        /// former makes the first index explicit. LLVM requires an explicit first index, even if it is
        /// zero, in order to properly compute the offset for a given element in an aggregate type.
        /// </remarks>
        public Value GetElementPtrInBounds(ITypeRef type, Value pointer, IEnumerable<Value> args)
        {
            var llvmArgs = GetValidatedGEPArgs(type, pointer, args);
            fixed (LLVMValueRef* pLlvmArgs = llvmArgs.AsSpan())
            {
                var hRetVal = LLVM.BuildInBoundsGEP2(
                    this.BuilderHandle,
                    type.GetTypeRef(),
                    pointer.ValueHandle,
                    (LLVMOpaqueValue**)pLlvmArgs,
                    (uint)llvmArgs.Length,
                    string.Empty.AsMarshaledString());
                return Value.FromHandle(hRetVal)!;
            }
        }

        /// <summary>Creates a <see cref="Value"/> that accesses an element of a type referenced by a pointer.</summary>
        /// <param name="pointer">pointer to get an element from.</param>
        /// <param name="args">additional indices for computing the resulting pointer.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a <see cref="Value"/>
        /// as LLVM may optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        /// <remarks>
        /// For details on GetElementPointer (GEP) see
        /// <see href="xref:llvm_misunderstood_gep">The Often Misunderstood GEP Instruction</see>.
        /// The basic gist is that the GEP instruction does not access memory, it only computes a pointer
        /// offset from a base. A common confusion is around the first index and what it means. For C
        /// and C++ programmers an expression like pFoo->bar seems to only have a single offset or
        /// index. However that is only syntactic sugar where the compiler implicitly hides the first
        /// index. That is, there is no difference between pFoo[0].bar and pFoo->bar except that the
        /// former makes the first index explicit. LLVM requires an explicit first index, even if it is
        /// zero, in order to properly compute the offset for a given element in an aggregate type.
        /// </remarks>
        [Obsolete("Use overload that accepts base pointer type and na opaque pointer")]
        public Value GetElementPtrInBounds(Value pointer, params Value[] args)
        {
            return this.GetElementPtrInBounds(pointer, (IEnumerable<Value>)args);
        }

        /// <summary>Creates a <see cref="Value"/> that accesses an element of a type referenced by a pointer.</summary>
        /// <param name="type">Base pointer type.</param>
        /// <param name="pointer">opaque pointer to get an element from.</param>
        /// <param name="args">additional indices for computing the resulting pointer.</param>
        /// <returns>
        /// <para><see cref="Value"/> for the member access. This is a <see cref="Value"/>
        /// as LLVM may optimize the expression to a <see cref="ConstantExpression"/> if it
        /// can so the actual type of the result may be <see cref="ConstantExpression"/>
        /// or <see cref="Instructions.GetElementPtr"/>.</para>
        /// <para>Note that <paramref name="pointer"/> must be a pointer to a structure
        /// or an exception is thrown.</para>
        /// </returns>
        /// <remarks>
        /// For details on GetElementPointer (GEP) see
        /// <see href="xref:llvm_misunderstood_gep">The Often Misunderstood GEP Instruction</see>.
        /// The basic gist is that the GEP instruction does not access memory, it only computes a pointer
        /// offset from a base. A common confusion is around the first index and what it means. For C
        /// and C++ programmers an expression like pFoo->bar seems to only have a single offset or
        /// index. However that is only syntactic sugar where the compiler implicitly hides the first
        /// index. That is, there is no difference between pFoo[0].bar and pFoo->bar except that the
        /// former makes the first index explicit. LLVM requires an explicit first index, even if it is
        /// zero, in order to properly compute the offset for a given element in an aggregate type.
        /// </remarks>
        public Value GetElementPtrInBounds(ITypeRef type, Value pointer, params Value[] args)
        {
            return this.GetElementPtrInBounds(type, pointer, (IEnumerable<Value>)args);
        }

        /// <summary>Builds a cast from an integer to a pointer.</summary>
        /// <param name="intValue">Integer value to cast.</param>
        /// <param name="ptrType">pointer type to return.</param>
        /// <returns>Resulting value from the cast.</returns>
        /// <remarks>
        /// The actual type of value returned depends on <paramref name="intValue"/>
        /// and is either a <see cref="ConstantExpression"/> or an <see cref="Instructions.IntToPointer"/>
        /// instruction. Conversion to a constant expression is performed whenever possible.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public Value IntToPointer(Value intValue, IPointerType ptrType)
        {
            if (intValue is Constant)
            {
                var handle = LLVM.ConstIntToPtr(intValue.ValueHandle, ptrType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildIntToPtr(intValue.ValueHandle, ptrType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Builds a cast from a pointer to an integer type.</summary>
        /// <param name="ptrValue">Pointer value to cast.</param>
        /// <param name="intType">Integer type to return.</param>
        /// <returns>Resulting value from the cast.</returns>
        /// <remarks>
        /// The actual type of value returned depends on <paramref name="ptrValue"/>
        /// and is either a <see cref="ConstantExpression"/> or a <see cref="Instructions.PointerToInt"/>
        /// instruction. Conversion to a constant expression is performed whenever possible.
        /// </remarks>
        public Value PointerToInt(Value ptrValue, ITypeRef intType)
        {
            if (ptrValue.NativeType.Kind != TypeKind.Pointer)
            {
                throw new ArgumentException();
            }

            if (intType.Kind != TypeKind.Integer)
            {
                throw new ArgumentException();
            }

            if (ptrValue is Constant)
            {
                var handle = LLVM.ConstPtrToInt(ptrValue.ValueHandle, intType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildPtrToInt(ptrValue.ValueHandle, intType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Create an unconditional branch.</summary>
        /// <param name="target">Target block for the branch.</param>
        /// <returns><see cref="Instructions.Branch"/>.</returns>
        public Branch Branch(BasicBlock target)
        {
            LLVMValueRef valueRef = this.BuilderHandle.BuildBr(target.BlockHandle);
            return Value.FromHandle<Branch>(valueRef)!;
        }

        /// <summary>Creates a conditional branch instruction.</summary>
        /// <param name="ifCondition">Condition for the branch.</param>
        /// <param name="thenTarget">Target block for the branch when <paramref name="ifCondition"/> evaluates to a non-zero value.</param>
        /// <param name="elseTarget">Target block for the branch when <paramref name="ifCondition"/> evaluates to a zero value.</param>
        /// <returns><see cref="Instructions.Branch"/>.</returns>
        public Branch Branch(Value ifCondition, BasicBlock thenTarget, BasicBlock elseTarget)
        {
            var handle = this.BuilderHandle.BuildCondBr(
                ifCondition.ValueHandle,
                thenTarget.BlockHandle,
                elseTarget.BlockHandle);

            return Value.FromHandle<Branch>(handle)!;
        }

        /// <summary>Creates an <see cref="Instructions.Unreachable"/> instruction.</summary>
        /// <returns><see cref="Instructions.Unreachable"/>. </returns>
        public Unreachable Unreachable()
            => Value.FromHandle<Unreachable>(this.BuilderHandle.BuildUnreachable())!;

        /// <summary>Builds an Integer compare instruction.</summary>
        /// <param name="predicate">Integer predicate for the comparison.</param>
        /// <param name="lhs">Left hand side of the comparison.</param>
        /// <param name="rhs">Right hand side of the comparison.</param>
        /// <returns>Comparison instruction.</returns>
        public Value Compare(IntPredicate predicate, Value lhs, Value rhs)
        {
            if (!lhs.NativeType.IsInteger && !lhs.NativeType.IsPointer)
            {
                throw new ArgumentException();
            }

            if (!rhs.NativeType.IsInteger && !lhs.NativeType.IsPointer)
            {
                throw new ArgumentException();
            }

            var handle = this.BuilderHandle.BuildICmp((LLVMIntPredicate)predicate, lhs.ValueHandle, rhs.ValueHandle, string.Empty);
            return Value.FromHandle(handle)!;
        }

        /// <summary>Builds a Floating point compare instruction.</summary>
        /// <param name="predicate">predicate for the comparison.</param>
        /// <param name="lhs">Left hand side of the comparison.</param>
        /// <param name="rhs">Right hand side of the comparison.</param>
        /// <returns>Comparison instruction.</returns>
        public Value Compare(RealPredicate predicate, Value lhs, Value rhs)
        {
            if (!lhs.NativeType.IsFloatingPoint)
            {
                throw new ArgumentException();
            }

            if (!rhs.NativeType.IsFloatingPoint)
            {
                throw new ArgumentException();
            }

            var handle = this.BuilderHandle.BuildFCmp(
                (LLVMRealPredicate)predicate,
                lhs.ValueHandle,
                rhs.ValueHandle,
                string.Empty);
            return Value.FromHandle(handle)!;
        }

        /// <summary>Builds a compare instruction.</summary>
        /// <param name="predicate">predicate for the comparison.</param>
        /// <param name="lhs">Left hand side of the comparison.</param>
        /// <param name="rhs">Right hand side of the comparison.</param>
        /// <returns>Comparison instruction.</returns>
        public Value Compare(Predicate predicate, Value lhs, Value rhs)
        {
            if (predicate <= Predicate.LastFcmpPredicate)
            {
                return this.Compare((RealPredicate)predicate, lhs, rhs);
            }

            if (predicate >= Predicate.FirstIcmpPredicate && predicate <= Predicate.LastIcmpPredicate)
            {
                return this.Compare((IntPredicate)predicate, lhs, rhs);
            }

            throw new ArgumentOutOfRangeException();
        }

        /// <summary>Creates a zero extend or bit cast instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value ZeroExtendOrBitCast(Value valueRef, ITypeRef targetType)
        {
            // short circuit cast to same type as it won't be a Constant or a BitCast
            if (valueRef.NativeType == targetType)
            {
                return valueRef;
            }

            if (valueRef is Constant)
            {
                var handle = LLVM.ConstZExtOrBitCast(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildZExtOrBitCast(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a sign extend or bit cast instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value SignExtendOrBitCast(Value valueRef, ITypeRef targetType)
        {
            // short circuit cast to same type as it won't be a Constant or a BitCast
            if (valueRef.NativeType == targetType)
            {
                return valueRef;
            }

            if (valueRef is Constant)
            {
                var handle = LLVM.ConstSExtOrBitCast(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildSExtOrBitCast(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a trunc or bit cast instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value TruncOrBitCast(Value valueRef, ITypeRef targetType)
        {
            // short circuit cast to same type as it won't be a Constant or a BitCast
            if (valueRef.NativeType == targetType)
            {
                return valueRef;
            }

            if (valueRef is Constant)
            {
                var handle = LLVM.ConstTruncOrBitCast(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildTruncOrBitCast(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a Zero Extend instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value ZeroExtend(Value valueRef, ITypeRef targetType)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstZExt(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildZExt(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a Sign Extend instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value SignExtend(Value valueRef, ITypeRef targetType)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstSExt(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildSExt(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a bitcast instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value BitCast(Value valueRef, ITypeRef targetType)
        {
            // short circuit cast to same type as it won't be a Constant or a BitCast
            if (valueRef.NativeType == targetType)
            {
                return valueRef;
            }

            if (valueRef is Constant)
            {
                var handle = LLVM.ConstBitCast(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildBitCast(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates an integer cast instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <param name="isSigned">Flag to indicate if the cast is signed or unsigned.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value IntCast(Value valueRef, ITypeRef targetType, bool isSigned)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstIntCast(valueRef.ValueHandle, targetType.GetTypeRef(), isSigned ? 1 : 0);
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildIntCast(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a trunc instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value Trunc(Value valueRef, ITypeRef targetType)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstTrunc(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildTrunc(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a signed integer to floating point cast instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value SIToFPCast(Value valueRef, ITypeRef targetType)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstSIToFP(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildSIToFP(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates an unsigned integer to floating point cast instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value UIToFPCast(Value valueRef, ITypeRef targetType)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstUIToFP(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildUIToFP(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a Floating point to unsigned integer cast instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value FPToUICast(Value valueRef, ITypeRef targetType)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstFPToUI(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildFPToUI(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a floating point to signed integer cast instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value FPToSICast(Value valueRef, ITypeRef targetType)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstFPToSI(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildFPToSI(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a floating point extend instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value FPExt(Value valueRef, ITypeRef targetType)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstFPExt(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildFPExt(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Creates a floating point truncate instruction.</summary>
        /// <param name="valueRef">Operand for the instruction.</param>
        /// <param name="targetType">Target type for the instruction.</param>
        /// <returns>Result <see cref="Value"/>.</returns>
        public Value FPTrunc(Value valueRef, ITypeRef targetType)
        {
            if (valueRef is Constant)
            {
                var handle = LLVM.ConstFPTrunc(valueRef.ValueHandle, targetType.GetTypeRef());
                return Value.FromHandle(handle)!;
            }
            else
            {
                var handle = this.BuilderHandle.BuildFPTrunc(valueRef.ValueHandle, targetType.GetTypeRef(), string.Empty);
                return Value.FromHandle(handle)!;
            }
        }

        /// <summary>Builds a <see cref="SelectInstruction"/> instruction.</summary>
        /// <param name="ifCondition">Value for the condition to select between the values.</param>
        /// <param name="thenValue">Result value if <paramref name="ifCondition"/> evaluates to 1.</param>
        /// <param name="elseValue">Result value if <paramref name="ifCondition"/> evaluates to 0.</param>
        /// <returns>Selected value.</returns>
        /// <remarks>
        /// If <paramref name="ifCondition"/> is a vector then both values must be a vector of the same
        /// size and the selection is performed element by element. The values must be the same type.
        /// </remarks>
        public Value Select(Value ifCondition, Value thenValue, Value elseValue)
        {
            var conditionVectorType = ifCondition.NativeType as IVectorType;

            if (ifCondition.NativeType.IntegerBitWidth != 1 && conditionVectorType != default && conditionVectorType.ElementType.IntegerBitWidth != 1)
            {
                throw new ArgumentException();
            }

            if (conditionVectorType != default)
            {
                if (!(thenValue.NativeType is IVectorType thenVector) || thenVector.Size != conditionVectorType.Size)
                {
                    throw new ArgumentException();
                }

                if (!(elseValue.NativeType is IVectorType elseVector) || elseVector.Size != conditionVectorType.Size)
                {
                    throw new ArgumentException();
                }
            }
            else
            {
                if (elseValue.NativeType != thenValue.NativeType)
                {
                    throw new ArgumentException();
                }
            }

            var handle = this.BuilderHandle.BuildSelect(
                ifCondition.ValueHandle,
                thenValue.ValueHandle,
                elseValue.ValueHandle,
                string.Empty);
            return Value.FromHandle(handle)!;
        }

        /// <summary>Creates a Phi instruction.</summary>
        /// <param name="resultType">Result type for the instruction.</param>
        /// <returns><see cref="Instructions.PhiNode"/>.</returns>
        public PhiNode PhiNode(ITypeRef resultType)
        {
            var handle = this.BuilderHandle.BuildPhi(resultType.GetTypeRef(), string.Empty);
            return Value.FromHandle<PhiNode>(handle)!;
        }

        /// <summary>Creates an extractvalue instruction.</summary>
        /// <param name="instance">Instance to extract a value from.</param>
        /// <param name="index">index of the element to extract.</param>
        /// <returns>Value for the instruction.</returns>
        public Value ExtractValue(Value instance, uint index)
        {
            var handle = this.BuilderHandle.BuildExtractValue(instance.ValueHandle, index, string.Empty);
            return Value.FromHandle(handle)!;
        }

        /// <summary>Creates a call to the llvm.donothing intrinsic.</summary>
        /// <returns><see cref="CallInstruction"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// <see cref="InsertBlock"/> is <see langword="default"/> or it's <see cref="BasicBlock.ContainingFunction"/> is default or has a <see langword="default"/> <see cref="GlobalValue.ParentModule"/>.
        /// </exception>
        public CallInstruction DoNothing()
        {
            BitcodeModule module = this.GetModuleOrThrow();
            var func = module.GetIntrinsicDeclaration("llvm.donothing");
            var hCall = this.BuildCall(func);
            return Value.FromHandle<CallInstruction>(hCall)!;
        }

        /// <summary>Creates a llvm.debugtrap call.</summary>
        /// <returns><see cref="CallInstruction"/>.</returns>
        public CallInstruction DebugTrap()
        {
            var module = this.GetModuleOrThrow();
            var func = module.GetIntrinsicDeclaration("llvm.debugtrap");

            return this.Call(func);
        }

        /// <summary>Creates a llvm.trap call.</summary>
        /// <returns><see cref="CallInstruction"/>.</returns>
        public CallInstruction Trap()
        {
            var module = this.GetModuleOrThrow();
            var func = module.GetIntrinsicDeclaration("llvm.trap");

            return this.Call(func);
        }

        /// <summary>Builds a memcpy intrinsic call.</summary>
        /// <param name="destination">Destination pointer of the memcpy.</param>
        /// <param name="source">Source pointer of the memcpy.</param>
        /// <param name="len">length of the data to copy.</param>
        /// <param name="isVolatile">Flag to indicate if the copy involves volatile data such as physical registers.</param>
        /// <returns><see cref="Intrinsic"/> call for the memcpy.</returns>
        /// <remarks>
        /// LLVM has many overloaded variants of the memcpy intrinsic, this implementation will deduce the types from
        /// the provided values and generate a more specific call without the need to provide overloaded forms of this
        /// method and otherwise complicating the calling code.
        /// </remarks>
        public Value MemCpy(Value destination, Value source, Value len, bool isVolatile)
        {
            var module = this.GetModuleOrThrow();

            if (destination == source)
            {
                throw new InvalidOperationException();
            }

            if (!(destination.NativeType is IPointerType dstPtrType))
            {
                throw new ArgumentException();
            }

            if (!(source.NativeType is IPointerType srcPtrType))
            {
                throw new ArgumentException();
            }

            if (!len.NativeType.IsInteger)
            {
                throw new ArgumentException();
            }

            if (this.Context != module.Context)
            {
                throw new ArgumentException();
            }

            if (!dstPtrType.ElementType.IsInteger)
            {
                dstPtrType = module.Context.Int8Type.CreatePointerType();
                destination = this.BitCast(destination, dstPtrType);
            }

            if (!srcPtrType.ElementType.IsInteger)
            {
                srcPtrType = module.Context.Int8Type.CreatePointerType();
                source = this.BitCast(source, srcPtrType);
            }

            // find the name of the appropriate overloaded form
            var func = module.GetIntrinsicDeclaration("llvm.memcpy.p.p.i", dstPtrType, srcPtrType, len.NativeType);

            var call = this.BuildCall(
                func,
                destination,
                source,
                len,
                module.Context.CreateConstant(isVolatile));
            return Value.FromHandle(call)!;
        }

        /// <summary>Builds a memmove intrinsic call.</summary>
        /// <param name="destination">Destination pointer of the memmove.</param>
        /// <param name="source">Source pointer of the memmove.</param>
        /// <param name="len">length of the data to copy.</param>
        /// <param name="isVolatile">Flag to indicate if the copy involves volatile data such as physical registers.</param>
        /// <returns><see cref="Intrinsic"/> call for the memmove.</returns>
        /// <remarks>
        /// LLVM has many overloaded variants of the memmove intrinsic, this implementation will deduce the types from
        /// the provided values and generate a more specific call without the need to provide overloaded forms of this
        /// method and otherwise complicating the calling code.
        /// </remarks>
        public Value MemMove(Value destination, Value source, Value len, bool isVolatile)
        {
            var module = this.GetModuleOrThrow();

            if (destination == source)
            {
                throw new InvalidOperationException();
            }

            if (!(destination.NativeType is IPointerType dstPtrType))
            {
                throw new ArgumentException();
            }

            if (!(source.NativeType is IPointerType srcPtrType))
            {
                throw new ArgumentException();
            }

            if (!len.NativeType.IsInteger)
            {
                throw new ArgumentException();
            }

            if (this.Context != module.Context)
            {
                throw new ArgumentException();
            }

            if (!dstPtrType.ElementType.IsInteger)
            {
                dstPtrType = module.Context.Int8Type.CreatePointerType();
                destination = this.BitCast(destination, dstPtrType);
            }

            if (!srcPtrType.ElementType.IsInteger)
            {
                srcPtrType = module.Context.Int8Type.CreatePointerType();
                source = this.BitCast(source, srcPtrType);
            }

            // find the name of the appropriate overloaded form
            var func = module.GetIntrinsicDeclaration("llvm.memmove.p.p.i", dstPtrType, srcPtrType, len.NativeType);

            var call = this.BuildCall(func, destination, source, len, module.Context.CreateConstant(isVolatile));
            return Value.FromHandle(call)!;
        }

        /// <summary>Builds a memset intrinsic call.</summary>
        /// <param name="destination">Destination pointer of the memset.</param>
        /// <param name="value">fill value for the memset.</param>
        /// <param name="len">length of the data to fill.</param>
        /// <param name="isVolatile">Flag to indicate if the fill involves volatile data such as physical registers.</param>
        /// <returns><see cref="Intrinsic"/> call for the memset.</returns>
        /// <remarks>
        /// LLVM has many overloaded variants of the memset intrinsic, this implementation will deduce the types from
        /// the provided values and generate a more specific call without the need to provide overloaded forms of this
        /// method and otherwise complicating the calling code.
        /// </remarks>
        public Value MemSet(Value destination, Value value, Value len, bool isVolatile)
        {
            var module = this.GetModuleOrThrow();

            if (!(destination.NativeType is IPointerType dstPtrType))
            {
                throw new ArgumentException();
            }

            if (dstPtrType.ElementType != value.NativeType)
            {
                throw new ArgumentException();
            }

            if (!value.NativeType.IsInteger)
            {
                throw new ArgumentException();
            }

            if (!len.NativeType.IsInteger)
            {
                throw new ArgumentException();
            }

            if (this.Context != module.Context)
            {
                throw new ArgumentException();
            }

            if (!dstPtrType.ElementType.IsInteger)
            {
                dstPtrType = module.Context.Int8Type.CreatePointerType();
                destination = this.BitCast(destination, dstPtrType);
            }

            // find the appropriate overloaded form of the function
            var func = module.GetIntrinsicDeclaration("llvm.memset.p.i", dstPtrType, value.NativeType);

            var call = this.BuildCall(
                func,
                destination,
                value,
                len,
                module.Context.CreateConstant(isVolatile));

            return Value.FromHandle(call)!;
        }

        /// <summary>Builds an <see cref="Instructions.InsertValue"/> instruction. </summary>
        /// <param name="aggValue">Aggregate value to insert <paramref name="elementValue"/> into.</param>
        /// <param name="elementValue">Value to insert into <paramref name="aggValue"/>.</param>
        /// <param name="index">Index to insert the value into.</param>
        /// <returns>Instruction as a <see cref="Value"/>.</returns>
        public Value InsertValue(Value aggValue, Value elementValue, uint index)
        {
            var handle = this.BuilderHandle.BuildInsertValue(aggValue.ValueHandle, elementValue.ValueHandle, index, string.Empty);
            return Value.FromHandle(handle)!;
        }

        /// <summary>Generates a call to the llvm.[s|u]add.with.overflow intrinsic.</summary>
        /// <param name="lhs">Left hand side of the operation.</param>
        /// <param name="rhs">Right hand side of the operation.</param>
        /// <param name="signed">Flag to indicate if the operation is signed <see langword="true"/> or unsigned <see langword="false"/>.</param>
        /// <returns>Instruction as a <see cref="Value"/>.</returns>
        public Value AddWithOverflow(Value lhs, Value rhs, bool signed)
        {
            char kind = signed ? 's' : 'u';
            string name = $"llvm.{kind}add.with.overflow.i";
            var module = this.GetModuleOrThrow();

            var function = module.GetIntrinsicDeclaration(name, lhs.NativeType);
            return this.Call(function, lhs, rhs);
        }

        /// <summary>Generates a call to the llvm.[s|u]sub.with.overflow intrinsic.</summary>
        /// <param name="lhs">Left hand side of the operation.</param>
        /// <param name="rhs">Right hand side of the operation.</param>
        /// <param name="signed">Flag to indicate if the operation is signed <see langword="true"/> or unsigned <see langword="false"/>.</param>
        /// <returns>Instruction as a <see cref="Value"/>.</returns>
        public Value SubWithOverflow(Value lhs, Value rhs, bool signed)
        {
            char kind = signed ? 's' : 'u';
            string name = $"llvm.{kind}sub.with.overflow.i";
            uint id = Intrinsic.LookupId(name);
            var module = this.GetModuleOrThrow();

            var function = module.GetIntrinsicDeclaration(id, lhs.NativeType);
            return this.Call(function, lhs, rhs);
        }

        /// <summary>Generates a call to the llvm.[s|u]mul.with.overflow intrinsic.</summary>
        /// <param name="lhs">Left hand side of the operation.</param>
        /// <param name="rhs">Right hand side of the operation.</param>
        /// <param name="signed">Flag to indicate if the operation is signed <see langword="true"/> or unsigned <see langword="false"/>.</param>
        /// <returns>Instruction as a <see cref="Value"/>.</returns>
        public Value MulWithOverflow(Value lhs, Value rhs, bool signed)
        {
            char kind = signed ? 's' : 'u';
            string name = $"llvm.{kind}mul.with.overflow.i";
            uint id = Intrinsic.LookupId(name);
            var module = this.GetModuleOrThrow();

            var function = module.GetIntrinsicDeclaration(id, lhs.NativeType);
            return this.Call(function, lhs, rhs);
        }

        internal static LLVMValueRef[] GetValidatedGEPArgs(ITypeRef type, Value pointer, IEnumerable<Value> args)
        {
            if (!(pointer.NativeType is IPointerType pointerType))
            {
                throw new ArgumentException();
            }

            if (pointerType.ElementType.GetTypeRef() != type.GetTypeRef())
            {
                throw new ArgumentException("GEP pointer and element type don't agree!");
            }

            // start with the base pointer as type for first index
            ITypeRef elementType = type.CreatePointerType();
            foreach (var index in args)
            {
                switch (elementType)
                {
                    case ISequenceType s:
                        elementType = s.ElementType;
                        break;

                    case IStructType st:
                        if (!(index is ConstantInt constIndex))
                        {
                            throw new ArgumentException("GEP index into a structure type must be constant");
                        }

                        long indexValue = constIndex.SignExtendedValue;
                        if (indexValue >= st.Members.Count || indexValue < 0)
                        {
                            throw new ArgumentException($"GEP index {indexValue} is out of range for {st.Name}");
                        }

                        elementType = st.Members[(int)constIndex.SignExtendedValue];
                        break;

                    default:
                        throw new ArgumentException($"GEP index through a non-aggregate type {elementType}");
                }
            }

            // if not an array already, pull from source enumerable into an array only once
            var argsArray = args as Value[] ?? args.ToArray();
            if (argsArray.Any(a => !a.NativeType.IsInteger))
            {
                throw new ArgumentException();
            }

            LLVMValueRef[] llvmArgs = argsArray.Select(a => a.ValueHandle).ToArray();
            if (llvmArgs.Length == 0)
            {
                throw new ArgumentException();
            }

            return llvmArgs;
        }

        private static void ValidateStructGepArgs(Value pointer, uint index)
        {
            if (!(pointer.NativeType is IPointerType ptrType))
            {
                throw new ArgumentException();
            }

            if (!(ptrType.ElementType is IStructType elementStructType))
            {
                throw new ArgumentException();
            }

            if (!elementStructType.IsSized && index > 0)
            {
                throw new ArgumentException();
            }

            if (index >= elementStructType.Members.Count)
            {
                throw new ArgumentException();
            }
        }

        private static FunctionType ValidateCallArgs(Value func, IReadOnlyList<Value> args)
        {
            if (!(func.NativeType is IPointerType funcPtrType))
            {
                throw new ArgumentException();
            }

            if (!(funcPtrType.ElementType is FunctionType signatureType))
            {
                throw new ArgumentException();
            }

            // validate arg count; too few or too many (unless the signature supports varargs) is an error
            if (args.Count < signatureType.ParameterTypes.Count
                || (args.Count > signatureType.ParameterTypes.Count && !signatureType.IsVarArg))
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < signatureType.ParameterTypes.Count; ++i)
            {
                if (args[i].NativeType != signatureType.ParameterTypes[i])
                {
                    throw new ArgumentException();
                }
            }

            return signatureType;
        }

        private BitcodeModule GetModuleOrThrow()
        {
            var module = this.InsertBlock?.ContainingFunction?.ParentModule;
            if (module == default)
            {
                throw new InvalidOperationException();
            }

            return module;
        }

        // LLVM will automatically perform constant folding, thus the result of applying
        // a unary operator instruction may actually be a constant value and not an instruction
        // this deals with that to produce a correct managed wrapper type
        private Value BuildUnaryOp(
            Func<LLVMBuilderRef, LLVMValueRef, LLVMValueRef> opFactory,
            Value operand)
        {
            var valueRef = opFactory(this.BuilderHandle, operand.ValueHandle);
            return Value.FromHandle(valueRef)!;
        }

        // LLVM will automatically perform constant folding, thus the result of applying
        // a binary operator instruction may actually be a constant value and not an instruction
        // this deals with that to produce a correct managed wrapper type
        private Value BuildBinOp(
            Func<LLVMBuilderRef, LLVMValueRef, LLVMValueRef, LLVMValueRef> opFactory,
            Value lhs,
            Value rhs)
        {
            if (lhs.NativeType != rhs.NativeType)
            {
                throw new ArgumentException();
            }

            var valueRef = opFactory(this.BuilderHandle, lhs.ValueHandle, rhs.ValueHandle);
            return Value.FromHandle(valueRef)!;
        }

        private LLVMValueRef BuildCall(Value func, params Value[] args) => this.BuildCall(func, (IReadOnlyList<Value>)args);

        private LLVMValueRef BuildCall(Value func) => this.BuildCall(func, new List<Value>());

        private LLVMValueRef BuildCall(Value func, IReadOnlyList<Value> args)
        {
            FunctionType sig = ValidateCallArgs(func, args);
            LLVMValueRef[] llvmArgs = args.Select(v => v.ValueHandle).ToArray();
            fixed (LLVMValueRef* pLlvmArgs = llvmArgs.AsSpan())
            {
                return LLVM.BuildCall2(this.BuilderHandle, sig.TypeHandle, func.ValueHandle, (LLVMOpaqueValue**)pLlvmArgs, (uint)llvmArgs.Length, string.Empty.AsMarshaledString());
            }
        }
    }
}
