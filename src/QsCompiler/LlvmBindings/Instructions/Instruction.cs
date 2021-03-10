// -----------------------------------------------------------------------
// <copyright file="Instruction.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LLVMSharp.Interop;


using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>LLVM Instruction opcodes</summary>
    /// <remarks>
    /// These are based on the "C" API and therefore more stable as changes in the underlying instruction ids are remapped in the C API layer
    /// </remarks>
    /// <seealso href="xref:llvm_langref#instruction-reference">LLVM instruction Reference</seealso>
    [SuppressMessage( "Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "Not actually flags" )]
    public enum OpCode
    {
        /// <summary>Invalid or unknown instruction</summary>
        Invalid = 0,

        /* Terminator Instructions */

        /// <summary>Return instruction</summary>
        /// <seealso cref="ReturnInstruction"/>
        /// <seealso href="xref:llvm_langref#ret-instruction">LLVM ret Instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        Return = LLVMOpcode.LLVMRet,

        /// <summary>Branch instruction</summary>
        /// <seealso cref="Instructions.Branch"/>
        /// <seealso href="xref:llvm_langref#br-instruction">LLVM br Instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        Branch = LLVMOpcode.LLVMBr,

        /// <summary>Switch instruction</summary>
        /// <seealso cref="Instructions.Switch"/>
        /// <seealso href="xref:llvm_langref#switch-instruction">LLVM switch instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        Switch = LLVMOpcode.LLVMSwitch,

        /// <summary>Indirect branch instruction</summary>
        /// <seealso cref="Instructions.IndirectBranch"/>
        /// <seealso href="xref:llvm_langref#indirectbr-instruction">LLVM indirectbr instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        IndirectBranch = LLVMOpcode.LLVMIndirectBr,

        /// <summary>Invoke instruction</summary>
        /// <seealso cref="Instructions.Invoke"/>
        /// <seealso href="xref:llvm_langref#invoke-instruction">LLVM invoke instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        Invoke = LLVMOpcode.LLVMInvoke,

        /// <summary>Unreachable instruction</summary>
        /// <seealso cref="Instructions.Unreachable"/>
        /// <seealso href="xref:llvm_langref#unreachable-instruction">LLVM unreachable instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        Unreachable = LLVMOpcode.LLVMUnreachable,

        /* Standard Binary Operators */

        /// <summary>Add instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#add-instruction">LLVM add instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        Add = LLVMOpcode.LLVMAdd,

        /// <summary>FAdd instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#fadd-instruction">LLVM FAdd instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        FAdd = LLVMOpcode.LLVMFAdd,

        /// <summary>Sub instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        /// <seealso href="xref:llvm_langref#sub-instruction">LLVM sub instruction</seealso>
        Sub = LLVMOpcode.LLVMSub,

        /// <summary>FSub instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#fsub-instruction">LLVM fsub instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        FSub = LLVMOpcode.LLVMFSub,

        /// <summary>Mul instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#mul-instruction">LLVM mul instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        Mul = LLVMOpcode.LLVMMul,

        /// <summary>FMul instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#fmul-instruction">LLVM fmul instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        FMul = LLVMOpcode.LLVMFMul,

        /// <summary>UDiv instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#udiv-instruction">LLVM udiv instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        UDiv = LLVMOpcode.LLVMUDiv,

        /// <summary>SDiv instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#sdiv-instruction">LLVM sdiv instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        SDiv = LLVMOpcode.LLVMSDiv,

        /// <summary>FDiv instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#fdiv-instruction">LLVM fdiv instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        FDiv = LLVMOpcode.LLVMFDiv,

        /// <summary>URem instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#urem-instruction">LLVM urem instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        URem = LLVMOpcode.LLVMURem,

        /// <summary>SRem instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#srem-instruction">LLVM srem instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        SRem = LLVMOpcode.LLVMSRem,

        /// <summary>FRem instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#frem-instruction">LLVM frem instruction</seealso>
        /// <seealso href="xref:llvm_langref#binary-operations">LLVM Binary Operations</seealso>
        FRem = LLVMOpcode.LLVMFRem,

        /* Logical Operators */

        /// <summary>Shift Left instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#shl-instruction">LLVM shl instruction</seealso>
        /// <seealso href="xref:llvm_langref#bitwise-binary-operations">LLVM Bitwise Binary Operations</seealso>
        Shl = LLVMOpcode.LLVMShl,

        /// <summary>Logical Shift Right instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#lshr-instruction">LLVM lshr instruction</seealso>
        /// <seealso href="xref:llvm_langref#bitwise-binary-operations">LLVM Bitwise Binary Operations</seealso>
        LShr = LLVMOpcode.LLVMLShr,

        /// <summary>Arithmetic Shift Right instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#ashr-instruction">LLVM ashr instruction</seealso>
        /// <seealso href="xref:llvm_langref#bitwise-binary-operations">LLVM Bitwise Binary Operations</seealso>
        AShr = LLVMOpcode.LLVMAShr,

        /// <summary>Bitwise And instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#an-instruction">LLVM and instruction</seealso>
        /// <seealso href="xref:llvm_langref#bitwise-binary-operations">LLVM Bitwise Binary Operations</seealso>
        And = LLVMOpcode.LLVMAnd,

        /// <summary>Bitwise Or instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#or-instruction">LLVM or instruction</seealso>
        /// <seealso href="xref:llvm_langref#bitwise-binary-operations">LLVM Bitwise Binary Operations</seealso>
        Or = LLVMOpcode.LLVMOr,

        /// <summary>Bitwise Xor instruction</summary>
        /// <seealso cref="BinaryOperator"/>
        /// <seealso href="xref:llvm_langref#xor-instruction">LLVM xor instruction</seealso>
        /// <seealso href="xref:llvm_langref#bitwise-binary-operations">LLVM Bitwise Binary Operations</seealso>
        Xor = LLVMOpcode.LLVMXor,

        /* Memory Operators */

        /// <summary>xxx instruction</summary>
        /// <seealso cref="Instructions.Alloca"/>
        /// <seealso href="xref:llvm_langref#alloca-instruction">LLVM alloca instruction</seealso>
        /// <seealso href="xref:llvm_langref#memory-access-and-addressing-operations">LLVM Memory Access and Addressing Operations</seealso>
        Alloca = LLVMOpcode.LLVMAlloca,

        /// <summary>Load instruction</summary>
        /// <seealso cref="Instructions.Load"/>
        /// <seealso href="xref:llvm_langref#load-instruction">LLVM load instruction</seealso>
        /// <seealso href="xref:llvm_langref#memory-access-and-addressing-operations">LLVM Memory Access and Addressing Operations</seealso>
        Load = LLVMOpcode.LLVMLoad,

        /// <summary>Store instruction</summary>
        /// <seealso cref="Instructions.Store"/>
        /// <seealso href="xref:llvm_langref#store-instruction">LLVM store instruction</seealso>
        /// <seealso href="xref:llvm_langref#memory-access-and-addressing-operations">LLVM Memory Access and Addressing Operations</seealso>
        Store = LLVMOpcode.LLVMStore,

        /// <summary>Fence instruction</summary>
        /// <seealso cref="Instructions.Fence"/>
        /// <seealso href="xref:llvm_langref#fence-instruction">LLVM fence instruction</seealso>
        /// <seealso href="xref:llvm_langref#memory-access-and-addressing-operations">LLVM Memory Access and Addressing Operations</seealso>
        Fence = LLVMOpcode.LLVMFence,

        /// <summary>CmpXchg instruction</summary>
        /// <seealso cref="Instructions.AtomicCmpXchg"/>
        /// <seealso href="xref:llvm_langref#cmpxchg-instruction">LLVM cmpxchg instruction</seealso>
        /// <seealso href="xref:llvm_langref#memory-access-and-addressing-operations">LLVM Memory Access and Addressing Operations</seealso>
        AtomicCmpXchg = LLVMOpcode.LLVMAtomicCmpXchg,

        /// <summary>atomicrmw instruction</summary>
        /// <seealso cref="Instructions.AtomicRMW"/>
        /// <seealso href="xref:llvm_langref#atomicrmw-instruction">LLVM atomicrmw instruction</seealso>
        /// <seealso href="xref:llvm_langref#memory-access-and-addressing-operations">LLVM Memory Access and Addressing Operations</seealso>
        AtomicRMW = LLVMOpcode.LLVMAtomicRMW,

        /// <summary>getelementptr instruction</summary>
        /// <seealso cref="Instructions.GetElementPtr"/>
        /// <seealso href="xref:llvm_langref#getelementptr-instruction">LLVM getelementptr instruction</seealso>
        /// <seealso href="xref:llvm_langref#memory-access-and-addressing-operations">LLVM Memory Access and Addressing Operations</seealso>
        GetElementPtr = LLVMOpcode.LLVMGetElementPtr,

        /* Cast Operators */

        /// <summary>trunc .. to instruction</summary>
        /// <seealso cref="Instructions.Trunc"/>
        /// <seealso href="xref:llvm_langref#trunc-to-instruction">LLVM trunc .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        Trunc = LLVMOpcode.LLVMTrunc,

        /// <summary>zext .. to instruction</summary>
        /// <seealso cref="Instructions.ZeroExtend"/>
        /// <seealso href="xref:llvm_langref#zext-to-instruction">LLVM zext .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        ZeroExtend = LLVMOpcode.LLVMZExt,

        /// <summary>sext .. to instruction</summary>
        /// <seealso cref="Instructions.SignExtend"/>
        /// <seealso href="xref:llvm_langref#sext-to-instruction">LLVM sext .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        SignExtend = LLVMOpcode.LLVMSExt,

        /// <summary>fptoui .. to instruction</summary>
        /// <seealso cref="Instructions.FPToUI"/>
        /// <seealso href="xref:llvm_langref#fptoui-to-instruction">LLVM fptoui .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        FPToUI = LLVMOpcode.LLVMFPToUI,

        /// <summary>fptosi .. to instruction</summary>
        /// <seealso cref="Instructions.FPToSI"/>
        /// <seealso href="xref:llvm_langref#fptosi-to-instruction">LLVM fptosi .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        FPToSI = LLVMOpcode.LLVMFPToSI,

        /// <summary>uitofp .. to instruction</summary>
        /// <seealso cref="Instructions.UIToFP"/>
        /// <seealso href="xref:llvm_langref#uitofp-to-instruction">LLVM uitofp .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        UIToFP = LLVMOpcode.LLVMUIToFP,

        /// <summary>sitofp .. to instruction</summary>
        /// <seealso cref="Instructions.SIToFP"/>
        /// <seealso href="xref:llvm_langref#sitofp-to-instruction">LLVM sitofp .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        SIToFP = LLVMOpcode.LLVMSIToFP,

        /// <summary>fptrunc .. to instruction</summary>
        /// <seealso cref="Instructions.FPTrunc"/>
        /// <seealso href="xref:llvm_langref#fptrunct-to-instruction">LLVM fptrunc .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        FPTrunc = LLVMOpcode.LLVMFPTrunc,

        /// <summary>fpext .. to instruction</summary>
        /// <seealso cref="Instructions.FPExt"/>
        /// <seealso href="xref:llvm_langref#fpext-to-instruction">LLVM fpext .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        FPExt = LLVMOpcode.LLVMFPExt,

        /// <summary>ptrtoint .. to instruction</summary>
        /// <seealso cref="PointerToInt"/>
        /// <seealso href="xref:llvm_langref#ptrtoint-to-instruction">LLVM ptrtoint .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        PtrToInt = LLVMOpcode.LLVMPtrToInt,

        /// <summary>inttoptr .. to instruction</summary>
        /// <seealso cref="IntToPointer"/>
        /// <seealso href="xref:llvm_langref#inttoptr-to-instruction">LLVM inttoptr .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        IntToPtr = LLVMOpcode.LLVMIntToPtr,

        /// <summary>bitcast .. to instruction</summary>
        /// <seealso cref="Instructions.BitCast"/>
        /// <seealso href="xref:llvm_langref#bitcast-to-instruction">LLVM bitcast .. to instruction</seealso>
        /// <seealso href="xref:llvm_langref#conversion-operations">LLVM Conversion Operations</seealso>
        BitCast = LLVMOpcode.LLVMBitCast,

        /// <summary>addressspacecast .. to instruction</summary>
        /// <seealso cref="AddressSpaceCast"/>
        /// <seealso href="xref:llvm_langref#addressspacecast-to-instruction">LLVM addressspacecast .. to instruction</seealso>
        AddrSpaceCast = LLVMOpcode.LLVMAddrSpaceCast,

        /* Other Operators */

        /// <summary>icmp instruction</summary>
        /// <seealso cref="IntCmp"/>
        /// <seealso href="xref:llvm_langref#icmp-instruction">LLVM icmp instruction</seealso>
        /// <seealso href="xref:llvm_langref#other-operations">LLVM Other Operations</seealso>
        ICmp = LLVMOpcode.LLVMICmp,

        /// <summary>fcmp instruction</summary>
        /// <seealso cref="Instructions.FCmp"/>
        /// <seealso href="xref:llvm_langref#fcmp-instruction">LLVM fcmp instruction</seealso>
        /// <seealso href="xref:llvm_langref#other-operations">LLVM Other Operations</seealso>
        FCmp = LLVMOpcode.LLVMFCmp,

        /// <summary>phi instruction</summary>
        /// <seealso cref="PhiNode"/>
        /// <seealso href="xref:llvm_langref#phi-instruction">LLVM phi instruction</seealso>
        /// <seealso href="xref:llvm_langref#other-operations">LLVM Other Operations</seealso>
        Phi = LLVMOpcode.LLVMPHI,

        /// <summary>call instruction</summary>
        /// <seealso cref="CallInstruction"/>
        /// <seealso href="xref:llvm_langref#call-instruction">LLVM call instruction</seealso>
        /// <seealso href="xref:llvm_langref#other-operations">LLVM Other Operations</seealso>
        Call = LLVMOpcode.LLVMCall,

        /// <summary>select instruction</summary>
        /// <seealso cref="SelectInstruction"/>
        /// <seealso href="xref:llvm_langref#select-instruction">LLVM select instruction</seealso>
        /// <seealso href="xref:llvm_langref#other-operations">LLVM Other Operations</seealso>
        Select = LLVMOpcode.LLVMSelect,

        /// <summary>Custom user operator1 instruction</summary>
        /// <seealso cref="Instructions.UserOp1"/>
        UserOp1 = LLVMOpcode.LLVMUserOp1,

        /// <summary>Custom user operator2 instruction</summary>
        /// <seealso cref="Instructions.UserOp2"/>
        UserOp2 = LLVMOpcode.LLVMUserOp2,

        /// <summary>va_arg instruction</summary>
        /// <seealso cref="Instructions.VaArg"/>
        /// <seealso href="xref:llvm_langref#va-arg-instruction">LLVM va_arg instruction</seealso>
        /// <seealso href="xref:llvm_langref#other-operations">LLVM Other Operations</seealso>
        VaArg = LLVMOpcode.LLVMVAArg,

        /// <summary>extractelement instruction</summary>
        /// <seealso cref="Instructions.ExtractElement"/>
        /// <seealso href="xref:llvm_langref#extractelement-instruction">LLVM extractelement instruction</seealso>
        /// <seealso href="xref:llvm_langref#vector-operations">LLVM Vector Operations</seealso>
        ExtractElement = LLVMOpcode.LLVMExtractElement,

        /// <summary>insert instruction</summary>
        /// <seealso cref="Instructions.InsertElement"/>
        /// <seealso href="xref:llvm_langref#insert-instruction">LLVM insert instruction</seealso>
        /// <seealso href="xref:llvm_langref#vector-operations">LLVM Vector Operations</seealso>
        InsertElement = LLVMOpcode.LLVMInsertElement,

        /// <summary>shufflevector instruction</summary>
        /// <seealso cref="Instructions.ShuffleVector"/>
        /// <seealso href="xref:llvm_langref#shufflevector-instruction">LLVM shufflevector instruction</seealso>
        /// <seealso href="xref:llvm_langref#vector-operations">LLVM Vector Operations</seealso>
        ShuffleVector = LLVMOpcode.LLVMShuffleVector,

        /// <summary>extractvalue instruction</summary>
        /// <seealso cref="Instructions.ExtractValue"/>
        /// <seealso href="xref:llvm_langref#extractvalue-instruction">LLVM extractvalue instruction</seealso>
        /// <seealso href="xref:llvm_langref#aggregate-operations">LLVM Vector Operations</seealso>
        ExtractValue = LLVMOpcode.LLVMExtractValue,

        /// <summary>insertvalue instruction</summary>
        /// <seealso cref="Instructions.InsertValue"/>
        /// <seealso href="xref:llvm_langref#xxx-instruction">LLVM insertvalue instruction</seealso>
        /// <seealso href="xref:llvm_langref#aggregate-operations">LLVM Aggregate Operations</seealso>
        InsertValue = LLVMOpcode.LLVMInsertValue,

        /* Exception Handling Operators */

        /// <summary>resume instruction</summary>
        /// <seealso cref="Instructions.ResumeInstruction"/>
        /// <seealso href="xref:llvm_langref#xxx-instruction">LLVM resume instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        Resume = LLVMOpcode.LLVMResume,

        /// <summary>landingpad instruction</summary>
        /// <seealso cref="Instructions.LandingPad"/>
        /// <seealso href="xref:llvm_langref#landingpad-instruction">LLVM landingpad instruction</seealso>
        /// <seealso href="xref:llvm_langref#other-operations">LLVM Other Operations</seealso>
        LandingPad = LLVMOpcode.LLVMLandingPad,

        /// <summary>cleanupret instruction</summary>
        /// <seealso cref="Instructions.CleanupReturn"/>
        /// <seealso href="xref:llvm_langref#cleanupret-instruction">LLVM cleanupret instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        CleanupRet = LLVMOpcode.LLVMCleanupRet,

        /// <summary>catchret instruction</summary>
        /// <seealso cref="Instructions.CatchReturn"/>
        /// <seealso href="xref:llvm_langref#catchret-instruction">LLVM catchret instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        CatchRet = LLVMOpcode.LLVMCatchRet,

        /// <summary>catchpad instruction</summary>
        /// <seealso cref="Instructions.CatchPad"/>
        /// <seealso href="xref:llvm_langref#catchpad-instruction">LLVM catchpad instruction</seealso>
        /// <seealso href="xref:llvm_langref#other-operations">LLVM Other Operations</seealso>
        CatchPad = LLVMOpcode.LLVMCatchPad,

        /// <summary>cleanuppad instruction</summary>
        /// <seealso cref="Instructions.CleanupPad"/>
        /// <seealso href="xref:llvm_langref#cleanuppad-instruction">LLVM cleanuppad instruction</seealso>
        /// <seealso href="xref:llvm_langref#other-operations">LLVM Other Operations</seealso>
        CleanupPad = LLVMOpcode.LLVMCleanupPad,

        /// <summary>catchswitch instruction</summary>
        /// <seealso cref="Instructions.CatchSwitch"/>
        /// <seealso href="xref:llvm_langref#catchswitch-instruction">LLVM catchswitch instruction</seealso>
        /// <seealso href="xref:llvm_langref#terminator-instructions">LLVM Terminator Instructions</seealso>
        CatchSwitch = LLVMOpcode.LLVMCatchSwitch,

        /// <summary>callbr instruction</summary>
        /// <seealso href="xref::llvm_langref#i-callbr"/>
        CallBr = LLVMOpcode.LLVMCallBr,

        /// <summary>Freeze instruction</summary>
        /// <seealso href="xref:llvm_langref#i-freeze"/>
        Freeze = LLVMOpcode.LLVMFreeze,
    }

    /// <summary>Exposes an LLVM Instruction</summary>
    public class Instruction
        : User
    {
        /// <summary>Gets the <see cref="BasicBlock"/> that contains this instruction</summary>
        public BasicBlock ContainingBlock
            => BasicBlock.FromHandle( ValueHandle.InstructionParent )!;

        /// <summary>Gets the LLVM opcode for the instruction</summary>
        public OpCode Opcode => ( OpCode )ValueHandle.InstructionOpcode;

        /// <summary>Gets a value indicating whether the opcode is for a memory access (<see cref="Alloca"/>, <see cref="Load"/>, <see cref="Store"/>)</summary>
        public bool IsMemoryAccess
        {
            get
            {
                var opCode = Opcode;
                return opCode == OpCode.Alloca
                    || opCode == OpCode.Load
                    || opCode == OpCode.Store;
            }
        }

        /// <summary>Gets a value indicating whether this instruction has metadata</summary>
        public bool HasMetadata => ValueHandle.HasMetadata;

        /// <summary>Gets or sets the alignment for the instruction</summary>
        /// <remarks>
        /// The alignment is always 0 for instructions other than <see cref="Alloca"/>,
        /// <see cref="Load"/>, <see cref="Store"/> that deal with memory accesses.
        /// Setting the alignment for other instructions results in an
        /// <see cref="InvalidOperationException"/>
        /// </remarks>
        public unsafe uint Alignment
        {
            get => IsMemoryAccess ? LLVM.GetAlignment( ValueHandle ) : 0;

            set
            {
                if( !IsMemoryAccess )
                {
                    throw new InvalidOperationException( );
                }

                LLVM.SetAlignment( ValueHandle, value );
            }
        }

        /// <summary>Gets a, potentially empty, collection of successor blocks for this instruction</summary>
        public IOperandCollection<BasicBlock> Successors { get; }

        internal Instruction( LLVMValueRef valueRef )
            : base( valueRef )
        {
            Successors = new SuccessorBlockCollection( this );
        }
    }
}
