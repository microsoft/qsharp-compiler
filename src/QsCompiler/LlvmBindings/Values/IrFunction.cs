// -----------------------------------------------------------------------
// <copyright file="IrFunction.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.Values
{
    /* values for CallingConvention enum come directly from LLVM's CallingConv.h
    // rather then the mapped C API version as the C version is not
    // a complete set.
    */

    /// <summary>Calling Convention for functions.</summary>
    /// <seealso href="xref:llvm_langref#calling-conventions">LLVM calling conventions</seealso>
    public enum CallingConvention
    {
        /// <summary> The default llvm calling convention, compatible with C.</summary>
        /// <remarks>
        /// This convention is the only calling convention that supports varargs calls.
        /// As with typical C calling conventions, the callee/caller have to
        /// tolerate certain amounts of prototype mismatch.
        /// </remarks>
        C = 0,

        // [gap]

        /// <summary>Fast calling convention.</summary>
        FastCall = 8,

        /// <summary>Cold calling.</summary>
        ColdCall = 9,

        /// <summary>Glasgow Haskell Compiler.</summary>
        GlasgowHaskellCompiler = 10,

        /// <summary>The High=Performance Erlang convention.</summary>
        HiPE = 11,

        /// <summary>Webkit JavaScript calling convention.</summary>
        WebKitJS = 12,

        /// <summary>Calling convention for dynamic register based calls (e.g.stackmap and patchpoint intrinsics).</summary>
        AnyReg = 13,

        /// <summary>Preserve most calling convention for runtime calls that preserves most registers.</summary>
        PreserveMost = 14,

        /// <summary>Preserve all calling convention for runtime calls that preserves (almost) all registers.</summary>
        PreserveAll = 15,

        /// <summary>Swift calling convention.</summary>
        Swift = 16,

        /// <summary>Calling convention for access functions.</summary>
        CxxFastTls = 17,

        // [Gap]

        /// <summary>Marker enum that identifies the start of the target specific conventions all values greater than or equal to this value are target specific.</summary>
        FirstTargetSpecific = 64, // [marker]

        /// <summary>X86 stdcall convention.</summary>
        /// <remarks>
        /// This calling convention is mostly used by the Win32 API. It is basically the same as the C
        /// convention with the difference in that the callee is responsible for popping the arguments
        /// from the stack.
        /// </remarks>
        X86StdCall = 64,

        /// <summary>X86 fast call convention.</summary>
        /// <remarks>
        /// 'fast' analog of <see cref="X86StdCall"/>. Passes first two arguments
        /// in ECX:EDX registers, others - via stack. Callee is responsible for
        /// stack cleaning.
        /// </remarks>
        X86FastCall = 65,

        /// <summary>ARM APCS (officially obsolete but some old targets use it).</summary>
        ArmAPCS = 66,

        /// <summary>ARM Architecture Procedure Calling Standard calling convention (aka EABI). Soft float variant.</summary>
        ArmAAPCS = 67,

        /// <summary>Same as <see cref="ArmAAPCS"/> but uses hard floating point ABI.</summary>
        ArmAAPCSVfp = 68,

        /// <summary>Calling convention used for MSP430 interrupt routines.</summary>
        MSP430Interrupt = 69,

        /// <summary>Similar to <see cref="X86StdCall"/>, passes first 'this' argument in ECX all others via stack.</summary>
        /// <remarks>
        /// Callee is responsible for stack cleaning. MSVC uses this by default for C++ instance methods in its ABI.
        /// </remarks>
        X86ThisCall = 70,

        /// <summary>Call to a PTX kernel.</summary>
        /// <remarks>Passes all arguments in parameter space.</remarks>
        PtxKernel = 71,

        /// <summary>Call to a PTX device function.</summary>
        /// <remarks>
        /// Passes all arguments in register or parameter space.
        /// </remarks>
        PtxDevice = 72,

        /// <summary>Calling convention for SPIR non-kernel device functions.</summary>
        SpirFunction = 75,

        /// <summary>Calling convention for SPIR kernel functions.</summary>
        SpirKernel = 76,

        /// <summary>Calling conventions for Intel OpenCL built-ins.</summary>
        IntelOpenCLBuiltIn = 77,

        /// <summary>The C convention as specified in the x86-64 supplement to the System V ABI, used on most non-Windows systems.</summary>
        X86x64SysV = 78,

        /// <summary>The C convention as implemented on Windows/x86-64 and AArch64.</summary>
        /// <remarks>
        /// <para>This convention differs from the more common <see cref="X86x64SysV"/> convention in a number of ways, most notably in
        /// that XMM registers used to pass arguments are shadowed by GPRs, and vice versa.</para>
        /// <para>On AArch64, this is identical to the normal C (AAPCS) calling convention for normal functions,
        /// but floats are passed in integer registers to variadic functions.</para>
        /// </remarks>
        X86x64Win64 = 79,

        /// <summary>MSVC calling convention that passes vectors and vector aggregates in SSE registers.</summary>
        X86VectorCall = 80,

        /// <summary>Calling convention used by HipHop Virtual Machine (HHVM).</summary>
        HHVM = 81,

        /// <summary>HHVM calling convention for invoking C/C++ helpers.</summary>
        HHVMCCall = 82,

        /// <summary>x86 hardware interrupt context.</summary>
        /// <remarks>
        /// Callee may take one or two parameters, where the 1st represents a pointer to hardware context frame
        /// and the 2nd represents hardware error code, the presence of the later depends on the interrupt vector
        /// taken. Valid for both 32- and 64-bit subtargets.
        /// </remarks>
        X86Interrupt = 83,

        /// <summary>Used for AVR interrupt routines.</summary>
        AVRInterrupt = 84,

        /// <summary>Calling convention used for AVR signal routines.</summary>
        AVRSignal = 85,

        /// <summary>Calling convention used for special AVR rtlib functions which have an "optimized" convention to preserve registers.</summary>
        AVRBuiltIn = 86,

        /// <summary>Calling convention used for Mesa vertex shaders.</summary>
        AMDGpuVertexShader = 87,

        /// <summary>Calling convention used for Mesa geometry shaders.</summary>
        AMDGpuGeometryShader = 88,

        /// <summary>Calling convention used for Mesa pixel shaders.</summary>
        AMDGpuPixelShader = 89,

        /// <summary>Calling convention used for Mesa compute shaders.</summary>
        AMDGpuComputeShader = 90,

        /// <summary>Calling convention for AMDGPU code object kernels.</summary>
        AMDGpuKernel = 91,

        /// <summary>Register calling convention used for parameters transfer optimization.</summary>
        X86RegCall = 92,

        /// <summary>Calling convention used for Mesa hull shaders. (= tessellation control shaders).</summary>
        AMDGpuHullShader = 93,

        /// <summary>Calling convention used for special MSP430 rtlib functions which have an "optimized" convention using additional registers.</summary>
        MSP430BuiltIn = 94,

        /// <summary>Calling convention used for AMDPAL vertex shader if tessellation is in use.</summary>
        AMDGpuLS = 95,

        /// <summary>Calling convention used for AMDPAL shader stage before geometry shader if geometry is in use.</summary>
        /// <remarks>
        /// Either the domain (= tessellation evaluation) shader if tessellation is in use, or otherwise the vertex shader.
        /// </remarks>
        AMDGpuEs = 96,

        /// <summary>The highest possible calling convention ID. Must be some 2^k - 1.</summary>
        MaxCallingConvention = 1023,
    }

    /// <summary>LLVM Function definition.</summary>
    public class IrFunction
        : GlobalObject,
        IAttributeAccessor
    {
        internal IrFunction(LLVMValueRef valueRef)
            : base(valueRef)
        {
            this.Attributes = new ValueAttributeDictionary(this, () => this);
            this.BasicBlocks = new BasicBlockCollection(this);
        }

        /// <summary>Gets the signature type of the function.</summary>
        public IFunctionType Signature
        {
            get
            {
                LLVMTypeRef ty = this.ValueHandle.TypeOf;
                LLVMTypeRef typeRef = ty.ElementType;
                return TypeRef.FromHandle<IFunctionType>(typeRef);
            }
        }

        /// <summary>Gets the Entry block for this function.</summary>
        public BasicBlock? EntryBlock
            => this.ValueHandle.BasicBlocksCount == 0 ? default : BasicBlock.FromHandle(this.ValueHandle.EntryBasicBlock);

        /// <summary>Gets the basic blocks for the function.</summary>
        public ICollection<BasicBlock> BasicBlocks { get; }

        /// <summary>Gets the parameters for the function including any method definition specific attributes (i.e. ByVal).</summary>
        public IReadOnlyList<Argument> Parameters => new FunctionParameterList(this);

        /// <summary>Gets or sets the Calling convention for the method.</summary>
        public CallingConvention CallingConvention
        {
            get => (CallingConvention)this.ValueHandle.FunctionCallConv;
            set
            {
                var val = this.ValueHandle;
                val.FunctionCallConv = (uint)value;
            }
        }

        /// <summary>Gets the LLVM instrinsicID for the method.</summary>
        public uint IntrinsicId => this.ValueHandle.IntrinsicID;

        /// <summary>Gets a value indicating whether the method signature accepts variable arguments.</summary>
        public bool IsVarArg => this.Signature.IsVarArg;

        /// <summary>Gets the return type of the function.</summary>
        public ITypeRef ReturnType => this.Signature.ReturnType;

        /// <summary>Gets or sets the personality function for exception handling in this function.</summary>
        public unsafe IrFunction? PersonalityFunction
        {
            get => LLVM.HasPersonalityFn(this.ValueHandle) == 0 ? default : FromHandle<IrFunction>(this.ValueHandle.PersonalityFn)!;

            set
            {
                var val = this.ValueHandle;
                val.PersonalityFn = value?.ValueHandle ?? default;
            }
        }

        /// <summary>Gets or sets the Garbage collection engine name that this function is generated to work with.</summary>
        /// <seealso href="xref:llvm_docs_garbagecollection">Garbage Collection with LLVM</seealso>
        public string GcName
        {
            get => this.ValueHandle.GC;
            set
            {
                var val = this.ValueHandle;
                val.GC = value;
            }
        }

        /// <summary>Gets the attributes for this function.</summary>
        public IAttributeDictionary Attributes { get; }

        /// <summary>Verifies the function is valid and all blocks properly terminated.</summary>
        public bool Verify() => this.ValueHandle.VerifyFunction(LLVMVerifierFailureAction.LLVMReturnStatusAction);

        /// <summary>Add a new basic block to the beginning of a function.</summary>
        /// <param name="name">Name (label) for the block.</param>
        /// <returns><see cref="BasicBlock"/> created and inserted at the beginning of the function.</returns>
        public BasicBlock PrependBasicBlock(string name)
        {
            LLVMBasicBlockRef firstBlock = this.ValueHandle.FirstBasicBlock;
            BasicBlock retVal;
            if (firstBlock == default)
            {
                retVal = this.AppendBasicBlock(name);
            }
            else
            {
                var blockRef = this.NativeType.Context.ContextHandle.InsertBasicBlock(firstBlock, name);
                retVal = BasicBlock.FromHandle(blockRef)!;
            }

            return retVal;
        }

        /// <summary>Appends a new basic block to a function.</summary>
        /// <param name="block">Existing block to append to the function's list of blocks.</param>
        public unsafe void AppendBasicBlock(BasicBlock block)
        {
            LLVM.AppendExistingBasicBlock(this.ValueHandle, block.BlockHandle);
        }

        /// <summary>Creates an appends a new basic block to a function.</summary>
        /// <param name="name">Name (label) of the block.</param>
        /// <returns><see cref="BasicBlock"/> created and inserted onto the end of the function.</returns>
        public BasicBlock AppendBasicBlock(string name)
        {
            LLVMBasicBlockRef blockRef = this.NativeType.Context.ContextHandle.AppendBasicBlock(this.ValueHandle, name);
            return BasicBlock.FromHandle(blockRef)!;
        }

        /// <summary>Inserts a basic block before another block in the function.</summary>
        /// <param name="name">Name of the block.</param>
        /// <param name="insertBefore">Block to insert the new block before.</param>
        /// <returns>New <see cref="BasicBlock"/> inserted.</returns>
        /// <exception cref="ArgumentException"><paramref name="insertBefore"/> belongs to a different function.</exception>
        public BasicBlock InsertBasicBlock(string name, BasicBlock insertBefore)
        {
            if (insertBefore.ContainingFunction != default && insertBefore.ContainingFunction != this)
            {
                throw new ArgumentException("Basic block belongs to another function", nameof(insertBefore));
            }

            LLVMBasicBlockRef basicBlockRef = this.NativeType.Context.ContextHandle.InsertBasicBlock(insertBefore.BlockHandle, name);
            return BasicBlock.FromHandle(basicBlockRef)!;
        }

        /// <inheritdoc/>
        public unsafe void AddAttributeAtIndex(FunctionAttributeIndex index, AttributeValue attrib)
        {
            attrib.VerifyValidOn(index, this);

            LLVM.AddAttributeAtIndex(this.ValueHandle, (uint)index, attrib.NativeAttribute);
        }

        /// <inheritdoc/>
        public uint GetAttributeCountAtIndex(FunctionAttributeIndex index)
        {
            return this.ValueHandle.GetAttributeCountAtIndex((LLVMAttributeIndex)index);
        }

        /// <inheritdoc/>
        public IEnumerable<AttributeValue> GetAttributesAtIndex(FunctionAttributeIndex index)
        {
            uint count = this.GetAttributeCountAtIndex(index);
            if (count == 0)
            {
                return Enumerable.Empty<AttributeValue>();
            }

            var buffer = this.ValueHandle.GetAttributesAtIndex((LLVMAttributeIndex)index);
            return from attribRef in buffer
                   select AttributeValue.FromHandle(this.Context, attribRef);
        }

        /// <inheritdoc/>
        public unsafe AttributeValue GetAttributeAtIndex(FunctionAttributeIndex index, AttributeKind kind)
        {
            var handle = LLVM.GetEnumAttributeAtIndex(this.ValueHandle, (uint)index, kind.GetEnumAttributeId());
            return AttributeValue.FromHandle(this.Context, handle);
        }

        /// <inheritdoc/>
        public unsafe AttributeValue GetAttributeAtIndex(FunctionAttributeIndex index, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException();
            }

            var handle = LLVM.GetStringAttributeAtIndex(this.ValueHandle, (uint)index, name.AsMarshaledString(), (uint)name.Length);
            return AttributeValue.FromHandle(this.Context, handle);
        }

        /// <inheritdoc/>
        public unsafe void RemoveAttributeAtIndex(FunctionAttributeIndex index, AttributeKind kind)
        {
            LLVM.RemoveEnumAttributeAtIndex(this.ValueHandle, (uint)index, kind.GetEnumAttributeId());
        }

        /// <inheritdoc/>
        public unsafe void RemoveAttributeAtIndex(FunctionAttributeIndex index, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException();
            }

            LLVM.RemoveStringAttributeAtIndex(this.ValueHandle, (uint)index, name.AsMarshaledString(), (uint)name.Length);
        }

        /// <summary>Removes this function from the parent module.</summary>
        public void EraseFromParent()
        {
            this.ValueHandle.DeleteFunction();
        }
    }
}
