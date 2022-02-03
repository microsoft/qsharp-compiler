// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMValueRef : IEquatable<LLVMValueRef>
    {
        public IntPtr Handle;

        public LLVMValueRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public uint Alignment
        {
            get => ((this.IsAGlobalValue != null) || (this.IsAAllocaInst != null) || (this.IsALoadInst != null) || (this.IsAStoreInst != null)) ? LLVM.GetAlignment(this) : default;
            set => LLVM.SetAlignment(this, value);
        }

        public LLVMAtomicRMWBinOp AtomicRMWBinOp
        {
            get => (this.IsAAtomicRMWInst != null) ? LLVM.GetAtomicRMWBinOp(this) : default;
            set => LLVM.SetAtomicRMWBinOp(this, value);
        }

        public LLVMBasicBlockRef[] BasicBlocks
        {
            get
            {
                if (this.IsAFunction == null)
                {
                    return Array.Empty<LLVMBasicBlockRef>();
                }

                var basicBlocks = new LLVMBasicBlockRef[this.BasicBlocksCount];

                fixed (LLVMBasicBlockRef* pBasicBlocks = basicBlocks)
                {
                    LLVM.GetBasicBlocks(this, (LLVMOpaqueBasicBlock**)pBasicBlocks);
                }

                return basicBlocks;
            }
        }

        public uint BasicBlocksCount => (this.IsAFunction != null) ? LLVM.CountBasicBlocks(this) : default;

        public LLVMValueRef Condition
        {
            get => (this.IsABranchInst != null) ? LLVM.GetCondition(this) : default;
            set => LLVM.SetCondition(this, value);
        }

        public ulong ConstIntZExt => (this.IsAConstantInt != null) ? LLVM.ConstIntGetZExtValue(this) : default;

        public long ConstIntSExt => (this.IsAConstantInt != null) ? LLVM.ConstIntGetSExtValue(this) : default;

        public LLVMOpcode ConstOpcode => (this.IsAConstantExpr != null) ? LLVM.GetConstOpcode(this) : default;

        public LLVMDLLStorageClass DLLStorageClass
        {
            get => (this.IsAGlobalValue != null) ? LLVM.GetDLLStorageClass(this) : default;
            set => LLVM.SetDLLStorageClass(this, value);
        }

        public LLVMBasicBlockRef EntryBasicBlock => (this.IsAFunction != null) ? LLVM.GetEntryBasicBlock(this) : default;

        public LLVMRealPredicate FCmpPredicate => (this.Handle != IntPtr.Zero) ? LLVM.GetFCmpPredicate(this) : default;

        public LLVMBasicBlockRef FirstBasicBlock => (this.IsAFunction != null) ? LLVM.GetFirstBasicBlock(this) : default;

        public LLVMValueRef FirstParam => (this.IsAFunction != null) ? LLVM.GetFirstParam(this) : default;

        public LLVMUseRef FirstUse => (this.Handle != IntPtr.Zero) ? LLVM.GetFirstUse(this) : default;

        public uint FunctionCallConv
        {
            get => (this.IsAFunction != null) ? LLVM.GetFunctionCallConv(this) : default;
            set => LLVM.SetFunctionCallConv(this, value);
        }

        public string GC
        {
            get
            {
                if (this.IsAFunction == null)
                {
                    return string.Empty;
                }

                var pName = LLVM.GetGC(this);

                if (pName == null)
                {
                    return string.Empty;
                }

                var span = new ReadOnlySpan<byte>(pName, int.MaxValue);
                return span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            }

            set
            {
                using var marshaledName = new MarshaledString(value.AsSpan());
                LLVM.SetGC(this, marshaledName);
            }
        }

        public LLVMModuleRef GlobalParent => (this.IsAGlobalValue != null) ? LLVM.GetGlobalParent(this) : default;

        public bool HasMetadata => (this.IsAInstruction != null) && LLVM.HasMetadata(this) != 0;

        public bool HasUnnamedAddr
        {
            get => (this.IsAGlobalValue != null) && LLVM.HasUnnamedAddr(this) != 0;
            set => LLVM.SetUnnamedAddr(this, value ? 1 : 0);
        }

        public LLVMIntPredicate ICmpPredicate => (this.Handle != IntPtr.Zero) ? LLVM.GetICmpPredicate(this) : default;

        public uint IncomingCount => (this.IsAPHINode != null) ? LLVM.CountIncoming(this) : default;

        public LLVMValueRef Initializer
        {
            get => (this.IsAGlobalVariable != null) ? LLVM.GetInitializer(this) : default;
            set => LLVM.SetInitializer(this, value);
        }

        public uint InstructionCallConv
        {
            get => ((this.IsACallBrInst != null) || (this.IsACallInst != null) || (this.IsAInvokeInst != null)) ? LLVM.GetInstructionCallConv(this) : default;
            set => LLVM.SetInstructionCallConv(this, value);
        }

        public LLVMValueRef InstructionClone => (this.Handle != IntPtr.Zero) ? LLVM.InstructionClone(this) : default;

        public LLVMOpcode InstructionOpcode => (this.Handle != IntPtr.Zero) ? LLVM.GetInstructionOpcode(this) : default;

        public LLVMBasicBlockRef InstructionParent => (this.IsAInstruction != null) ? LLVM.GetInstructionParent(this) : default;

        public uint IntrinsicID => (this.Handle != IntPtr.Zero) ? LLVM.GetIntrinsicID(this) : default;

        public LLVMValueRef IsAAddrSpaceCastInst => LLVM.IsAAddrSpaceCastInst(this);

        public LLVMValueRef IsAAllocaInst => LLVM.IsAAllocaInst(this);

        public LLVMValueRef IsAArgument => LLVM.IsAArgument(this);

        public LLVMValueRef IsAAtomicCmpXchgInst => LLVM.IsAAtomicCmpXchgInst(this);

        public LLVMValueRef IsAAtomicRMWInst => LLVM.IsAAtomicRMWInst(this);

        public LLVMValueRef IsABasicBlock => LLVM.IsABasicBlock(this);

        public LLVMValueRef IsABinaryOperator => LLVM.IsABinaryOperator(this);

        public LLVMValueRef IsABitCastInst => LLVM.IsABitCastInst(this);

        public LLVMValueRef IsABlockAddress => LLVM.IsABlockAddress(this);

        public LLVMValueRef IsABranchInst => LLVM.IsABranchInst(this);

        public LLVMValueRef IsACallBrInst => LLVM.IsACallBrInst(this);

        public LLVMValueRef IsACallInst => LLVM.IsACallInst(this);

        public LLVMValueRef IsACastInst => LLVM.IsACastInst(this);

        public LLVMValueRef IsACatchPadInst => LLVM.IsACatchPadInst(this);

        public LLVMValueRef IsACatchReturnInst => LLVM.IsACatchReturnInst(this);

        public LLVMValueRef IsACatchSwitchInst => LLVM.IsACatchSwitchInst(this);

        public LLVMValueRef IsACleanupPadInst => LLVM.IsACleanupPadInst(this);

        public LLVMValueRef IsACleanupReturnInst => LLVM.IsACleanupReturnInst(this);

        public LLVMValueRef IsACmpInst => LLVM.IsACmpInst(this);

        public LLVMValueRef IsAConstant => LLVM.IsAConstant(this);

        public LLVMValueRef IsAConstantAggregateZero => LLVM.IsAConstantAggregateZero(this);

        public LLVMValueRef IsAConstantArray => LLVM.IsAConstantArray(this);

        public LLVMValueRef IsAConstantDataArray => LLVM.IsAConstantDataArray(this);

        public LLVMValueRef IsAConstantDataSequential => LLVM.IsAConstantDataSequential(this);

        public LLVMValueRef IsAConstantDataVector => LLVM.IsAConstantDataVector(this);

        public LLVMValueRef IsAConstantExpr => LLVM.IsAConstantExpr(this);

        public LLVMValueRef IsAConstantFP => LLVM.IsAConstantFP(this);

        public LLVMValueRef IsAConstantInt => LLVM.IsAConstantInt(this);

        public LLVMValueRef IsAConstantPointerNull => LLVM.IsAConstantPointerNull(this);

        public LLVMValueRef IsAConstantStruct => LLVM.IsAConstantStruct(this);

        public LLVMValueRef IsAConstantTokenNone => LLVM.IsAConstantTokenNone(this);

        public LLVMValueRef IsAConstantVector => LLVM.IsAConstantVector(this);

        public LLVMValueRef IsADbgDeclareInst => LLVM.IsADbgDeclareInst(this);

        public LLVMValueRef IsADbgInfoIntrinsic => LLVM.IsADbgInfoIntrinsic(this);

        public LLVMValueRef IsADbgLabelInst => LLVM.IsADbgLabelInst(this);

        public LLVMValueRef IsADbgVariableIntrinsic => LLVM.IsADbgVariableIntrinsic(this);

        public LLVMValueRef IsAExtractElementInst => LLVM.IsAExtractElementInst(this);

        public LLVMValueRef IsAExtractValueInst => LLVM.IsAExtractValueInst(this);

        public LLVMValueRef IsAFCmpInst => LLVM.IsAFCmpInst(this);

        public LLVMValueRef IsAFenceInst => LLVM.IsAFenceInst(this);

        public LLVMValueRef IsAFPExtInst => LLVM.IsAFPExtInst(this);

        public LLVMValueRef IsAFPToSIInst => LLVM.IsAFPToSIInst(this);

        public LLVMValueRef IsAFPToUIInst => LLVM.IsAFPToUIInst(this);

        public LLVMValueRef IsAFPTruncInst => LLVM.IsAFPTruncInst(this);

        public LLVMValueRef IsAFreezeInst => LLVM.IsAFreezeInst(this);

        public LLVMValueRef IsAFuncletPadInst => LLVM.IsAFuncletPadInst(this);

        public LLVMValueRef IsAFunction => LLVM.IsAFunction(this);

        public LLVMValueRef IsAGetElementPtrInst => LLVM.IsAGetElementPtrInst(this);

        public LLVMValueRef IsAGlobalAlias => LLVM.IsAGlobalAlias(this);

        public LLVMValueRef IsAGlobalIFunc => LLVM.IsAGlobalIFunc(this);

        public LLVMValueRef IsAGlobalObject => LLVM.IsAGlobalObject(this);

        public LLVMValueRef IsAGlobalValue => LLVM.IsAGlobalValue(this);

        public LLVMValueRef IsAGlobalVariable => LLVM.IsAGlobalVariable(this);

        public LLVMValueRef IsAICmpInst => LLVM.IsAICmpInst(this);

        public LLVMValueRef IsAIndirectBrInst => LLVM.IsAIndirectBrInst(this);

        public LLVMValueRef IsAInlineAsm => LLVM.IsAInlineAsm(this);

        public LLVMValueRef IsAInsertElementInst => LLVM.IsAInsertElementInst(this);

        public LLVMValueRef IsAInsertValueInst => LLVM.IsAInsertValueInst(this);

        public LLVMValueRef IsAInstruction => LLVM.IsAInstruction(this);

        public LLVMValueRef IsAIntrinsicInst => LLVM.IsAIntrinsicInst(this);

        public LLVMValueRef IsAIntToPtrInst => LLVM.IsAIntToPtrInst(this);

        public LLVMValueRef IsAInvokeInst => LLVM.IsAInvokeInst(this);

        public LLVMValueRef IsALandingPadInst => LLVM.IsALandingPadInst(this);

        public LLVMValueRef IsALoadInst => LLVM.IsALoadInst(this);

        public LLVMValueRef IsAMDNode => LLVM.IsAMDNode(this);

        public LLVMValueRef IsAMDString => LLVM.IsAMDString(this);

        public LLVMValueRef IsAMemCpyInst => LLVM.IsAMemCpyInst(this);

        public LLVMValueRef IsAMemIntrinsic => LLVM.IsAMemIntrinsic(this);

        public LLVMValueRef IsAMemMoveInst => LLVM.IsAMemMoveInst(this);

        public LLVMValueRef IsAMemSetInst => LLVM.IsAMemSetInst(this);

        public LLVMValueRef IsAPHINode => LLVM.IsAPHINode(this);

        public LLVMValueRef IsAPoisonValue => LLVM.IsAPoisonValue(this);

        public LLVMValueRef IsAPtrToIntInst => LLVM.IsAPtrToIntInst(this);

        public LLVMValueRef IsAResumeInst => LLVM.IsAResumeInst(this);

        public LLVMValueRef IsAReturnInst => LLVM.IsAReturnInst(this);

        public LLVMValueRef IsASelectInst => LLVM.IsASelectInst(this);

        public LLVMValueRef IsASExtInst => LLVM.IsASExtInst(this);

        public LLVMValueRef IsAShuffleVectorInst => LLVM.IsAShuffleVectorInst(this);

        public LLVMValueRef IsASIToFPInst => LLVM.IsASIToFPInst(this);

        public LLVMValueRef IsAStoreInst => LLVM.IsAStoreInst(this);

        public LLVMValueRef IsASwitchInst => LLVM.IsASwitchInst(this);

        public LLVMValueRef IsATerminatorInst => LLVM.IsATerminatorInst(this);

        public LLVMValueRef IsATruncInst => LLVM.IsATruncInst(this);

        public LLVMValueRef IsAUIToFPInst => LLVM.IsAUIToFPInst(this);

        public LLVMValueRef IsAUnaryInstruction => LLVM.IsAUnaryInstruction(this);

        public LLVMValueRef IsAUnaryOperator => LLVM.IsAUnaryOperator(this);

        public LLVMValueRef IsAUndefValue => LLVM.IsAUndefValue(this);

        public LLVMValueRef IsAUnreachableInst => LLVM.IsAUnreachableInst(this);

        public LLVMValueRef IsAUser => LLVM.IsAUser(this);

        public LLVMValueRef IsAVAArgInst => LLVM.IsAVAArgInst(this);

        public LLVMValueRef IsAZExtInst => LLVM.IsAZExtInst(this);

        public bool IsBasicBlock => (this.Handle != IntPtr.Zero) && LLVM.ValueIsBasicBlock(this) != 0;

        public bool IsCleanup
        {
            get => (this.IsALandingPadInst != null) && LLVM.IsCleanup(this) != 0;
            set => LLVM.SetCleanup(this, value ? 1 : 0);
        }

        public bool IsConditional => (this.IsABranchInst != null) && LLVM.IsConditional(this) != 0;

        public bool IsConstant => (this.Handle != IntPtr.Zero) && LLVM.IsConstant(this) != 0;

        public bool IsConstantString => (this.IsAConstantDataSequential != null) && LLVM.IsConstantString(this) != 0;

        public bool IsDeclaration => (this.IsAGlobalValue != null) && LLVM.IsDeclaration(this) != 0;

        public bool IsExternallyInitialized
        {
            get => (this.IsAGlobalVariable != null) && LLVM.IsExternallyInitialized(this) != 0;
            set => LLVM.SetExternallyInitialized(this, value ? 1 : 0);
        }

        public bool IsGlobalConstant
        {
            get => (this.IsAGlobalVariable != null) && LLVM.IsGlobalConstant(this) != 0;
            set => LLVM.SetGlobalConstant(this, value ? 1 : 0);
        }

        public bool IsNull => (this.Handle != IntPtr.Zero) && LLVM.IsNull(this) != 0;

        public bool IsPoison => (this.Handle != IntPtr.Zero) && LLVM.IsPoison(this) != 0;

        public bool IsTailCall
        {
            get => (this.IsACallInst != null) && LLVM.IsTailCall(this) != 0;
            set => LLVM.SetTailCall(this, this.IsTailCall ? 1 : 0);
        }

        public bool IsThreadLocal
        {
            get => (this.IsAGlobalVariable != null) && LLVM.IsThreadLocal(this) != 0;
            set => LLVM.SetThreadLocal(this, value ? 1 : 0);
        }

        public bool IsUndef => (this.Handle != IntPtr.Zero) && LLVM.IsUndef(this) != 0;

        public LLVMValueKind Kind => (this.Handle != IntPtr.Zero) ? LLVM.GetValueKind(this) : default;

        public LLVMBasicBlockRef LastBasicBlock => (this.IsAFunction != null) ? LLVM.GetLastBasicBlock(this) : default;

        public LLVMValueRef LastParam => (this.IsAFunction != null) ? LLVM.GetLastParam(this) : default;

        public LLVMLinkage Linkage
        {
            get => (this.IsAGlobalValue != null) ? LLVM.GetLinkage(this) : default;
            set => LLVM.SetLinkage(this, value);
        }

        public LLVMValueRef[] MDNodeOperands
        {
            get
            {
                if (this.Kind != LLVMValueKind.LLVMMetadataAsValueValueKind)
                {
                    return Array.Empty<LLVMValueRef>();
                }

                var dest = new LLVMValueRef[this.MDNodeOperandsCount];

                fixed (LLVMValueRef* pDest = dest)
                {
                    LLVM.GetMDNodeOperands(this, (LLVMOpaqueValue**)pDest);
                }

                return dest;
            }
        }

        public uint MDNodeOperandsCount => (this.Kind != LLVMValueKind.LLVMMetadataAsValueValueKind) ? LLVM.GetMDNodeNumOperands(this) : default;

        public string Name
        {
            get
            {
                if (this.Handle == IntPtr.Zero)
                {
                    return string.Empty;
                }

                var pStr = LLVM.GetValueName(this);

                if (pStr == null)
                {
                    return string.Empty;
                }

                var span = new ReadOnlySpan<byte>(pStr, int.MaxValue);
                return span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            }

            set
            {
                using var marshaledName = new MarshaledString(value.AsSpan());
                LLVM.SetValueName(this, marshaledName);
            }
        }

        public LLVMValueRef NextFunction => (this.IsAFunction != null) ? LLVM.GetNextFunction(this) : default;

        public LLVMValueRef NextGlobal => (this.IsAGlobalVariable != null) ? LLVM.GetNextGlobal(this) : default;

        public LLVMValueRef NextInstruction => (this.IsAInstruction != null) ? LLVM.GetNextInstruction(this) : default;

        public LLVMValueRef NextParam => (this.IsAArgument != null) ? LLVM.GetNextParam(this) : default;

        public int OperandCount => ((this.Kind == LLVMValueKind.LLVMMetadataAsValueValueKind) || (this.IsAUser != null)) ? LLVM.GetNumOperands(this) : default;

        public LLVMValueRef[] Params
        {
            get
            {
                if (this.IsAFunction == null)
                {
                    return Array.Empty<LLVMValueRef>();
                }

                var @params = new LLVMValueRef[this.ParamsCount];

                fixed (LLVMValueRef* pParams = @params)
                {
                    LLVM.GetParams(this, (LLVMOpaqueValue**)pParams);
                }

                return @params;
            }
        }

        public uint ParamsCount => (this.IsAFunction != null) ? LLVM.CountParams(this) : default;

        public LLVMValueRef ParamParent => (this.IsAArgument != null) ? LLVM.GetParamParent(this) : default;

        public LLVMValueRef PersonalityFn
        {
            get => (this.IsAFunction != null) ? LLVM.GetPersonalityFn(this) : default;
            set => LLVM.SetPersonalityFn(this, value);
        }

        public LLVMValueRef PreviousGlobal => (this.IsAGlobalVariable != null) ? LLVM.GetPreviousGlobal(this) : default;

        public LLVMValueRef PreviousInstruction => (this.IsAInstruction != null) ? LLVM.GetPreviousInstruction(this) : default;

        public LLVMValueRef PreviousParam => (this.IsAArgument != null) ? LLVM.GetPreviousParam(this) : default;

        public LLVMValueRef PreviousFunction => (this.IsAFunction != null) ? LLVM.GetPreviousFunction(this) : default;

        public string Section
        {
            get
            {
                if (this.IsAGlobalValue == null)
                {
                    return string.Empty;
                }

                var pSection = LLVM.GetSection(this);

                if (pSection == null)
                {
                    return string.Empty;
                }

                var span = new ReadOnlySpan<byte>(pSection, int.MaxValue);
                return span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            }

            set
            {
                using var marshaledSection = new MarshaledString(value.AsSpan());
                LLVM.SetSection(this, marshaledSection);
            }
        }

        public uint SuccessorsCount => (this.IsAInstruction != null) ? LLVM.GetNumSuccessors(this) : default;

        public LLVMBasicBlockRef SwitchDefaultDest => (this.IsASwitchInst != null) ? LLVM.GetSwitchDefaultDest(this) : default;

        public LLVMThreadLocalMode ThreadLocalMode
        {
            get => (this.IsAGlobalVariable != null) ? LLVM.GetThreadLocalMode(this) : default;
            set => LLVM.SetThreadLocalMode(this, value);
        }

        public LLVMTypeRef TypeOf => (this.Handle != IntPtr.Zero) ? LLVM.TypeOf(this) : default;

        public LLVMVisibility Visibility
        {
            get => (this.IsAGlobalValue != null) ? LLVM.GetVisibility(this) : default;
            set => LLVM.SetVisibility(this, value);
        }

        public bool Volatile
        {
            get => ((this.IsALoadInst != null) || (this.IsAStoreInst != null) || (this.IsAAtomicRMWInst != null) || (this.IsAAtomicCmpXchgInst != null)) && LLVM.GetVolatile(this) != 0;
            set => LLVM.SetVolatile(this, value ? 1 : 0);
        }

        public bool Weak
        {
            get => (this.IsAAtomicCmpXchgInst != null) && LLVM.GetWeak(this) != 0;
            set => LLVM.SetWeak(this, value ? 1 : 0);
        }

        public static implicit operator LLVMValueRef(LLVMOpaqueValue* value) => new LLVMValueRef((IntPtr)value);

        public static implicit operator LLVMOpaqueValue*(LLVMValueRef value) => (LLVMOpaqueValue*)value.Handle;

        public static bool operator ==(LLVMValueRef left, LLVMValueRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMValueRef left, LLVMValueRef right) => !(left == right);

        public static LLVMValueRef CreateConstAdd(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstAdd(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstAddrSpaceCast(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstAddrSpaceCast(constantVal, toType);

        public static LLVMValueRef CreateConstAllOnes(LLVMTypeRef ty) => LLVM.ConstAllOnes(ty);

        public static LLVMValueRef CreateConstAnd(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstAnd(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstArray(LLVMTypeRef elementTy, LLVMValueRef[] constantVals) => CreateConstArray(elementTy, constantVals.AsSpan());

        public static LLVMValueRef CreateConstArray(LLVMTypeRef elementTy, ReadOnlySpan<LLVMValueRef> constantVals)
        {
            fixed (LLVMValueRef* pconstantVals = constantVals)
            {
                return LLVM.ConstArray(elementTy, (LLVMOpaqueValue**)pconstantVals, (uint)constantVals.Length);
            }
        }

        public static LLVMValueRef CreateConstAShr(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstAShr(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstBitCast(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstBitCast(constantVal, toType);

        public static LLVMValueRef CreateConstExactSDiv(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstExactSDiv(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstExactUDiv(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstExactUDiv(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstExtractElement(LLVMValueRef vectorConstant, LLVMValueRef indexConstant) => LLVM.ConstExtractElement(vectorConstant, indexConstant);

        public static LLVMValueRef CreateConstExtractValue(LLVMValueRef aggConstant, uint[] idxList) => CreateConstExtractValue(aggConstant, idxList.AsSpan());

        public static LLVMValueRef CreateConstExtractValue(LLVMValueRef aggConstant, ReadOnlySpan<uint> idxList)
        {
            fixed (uint* pIdxList = idxList)
            {
                return LLVM.ConstExtractValue(aggConstant, pIdxList, (uint)idxList.Length);
            }
        }

        public static LLVMValueRef CreateConstFAdd(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstFAdd(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstFDiv(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstFDiv(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstFMul(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstFMul(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstFNeg(LLVMValueRef constantVal) => LLVM.ConstFNeg(constantVal);

        public static LLVMValueRef CreateConstFPCast(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstFPCast(constantVal, toType);

        public static LLVMValueRef CreateConstFPExt(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstFPExt(constantVal, toType);

        public static LLVMValueRef CreateConstFPToSI(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstFPToSI(constantVal, toType);

        public static LLVMValueRef CreateConstFPToUI(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstFPToUI(constantVal, toType);

        public static LLVMValueRef CreateConstFPTrunc(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstFPTrunc(constantVal, toType);

        public static LLVMValueRef CreateConstFRem(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstFRem(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstFSub(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstFSub(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstGEP(LLVMValueRef constantVal, LLVMValueRef[] constantIndices) => CreateConstGEP(constantVal, constantIndices.AsSpan());

        public static LLVMValueRef CreateConstGEP(LLVMValueRef constantVal, ReadOnlySpan<LLVMValueRef> constantIndices)
        {
            fixed (LLVMValueRef* pconstantIndices = constantIndices)
            {
                return LLVM.ConstGEP(constantVal, (LLVMOpaqueValue**)pconstantIndices, (uint)constantIndices.Length);
            }
        }

        public static LLVMValueRef CreateConstInBoundsGEP(LLVMValueRef constantVal, LLVMValueRef[] constantIndices) => CreateConstInBoundsGEP(constantVal, constantIndices.AsSpan());

        public static LLVMValueRef CreateConstInBoundsGEP(LLVMValueRef constantVal, ReadOnlySpan<LLVMValueRef> constantIndices)
        {
            fixed (LLVMValueRef* pconstantIndices = constantIndices)
            {
                return LLVM.ConstInBoundsGEP(constantVal, (LLVMOpaqueValue**)pconstantIndices, (uint)constantIndices.Length);
            }
        }

        public static LLVMValueRef CreateConstInlineAsm(LLVMTypeRef ty, string asmString, string constraints, bool hasSideEffects, bool isAlignStack) => CreateConstInlineAsm(ty, asmString.AsSpan(), constraints.AsSpan(), hasSideEffects, isAlignStack);

        public static LLVMValueRef CreateConstInlineAsm(LLVMTypeRef ty, ReadOnlySpan<char> asmString, ReadOnlySpan<char> constraints, bool hasSideEffects, bool isAlignStack)
        {
            using var marshaledAsmString = new MarshaledString(asmString);
            using var marshaledConstraints = new MarshaledString(constraints);
            return LLVM.ConstInlineAsm(ty, marshaledAsmString, marshaledConstraints, hasSideEffects ? 1 : 0, isAlignStack ? 1 : 0);
        }

        public static LLVMValueRef CreateConstInsertElement(LLVMValueRef vectorConstant, LLVMValueRef elementValueConstant, LLVMValueRef indexConstant) => LLVM.ConstInsertElement(vectorConstant, elementValueConstant, indexConstant);

        public static LLVMValueRef CreateConstInsertValue(LLVMValueRef aggConstant, LLVMValueRef elementValueConstant, uint[] idxList) => CreateConstInsertValue(aggConstant, elementValueConstant, idxList.AsSpan());

        public static LLVMValueRef CreateConstInsertValue(LLVMValueRef aggConstant, LLVMValueRef elementValueConstant, ReadOnlySpan<uint> idxList)
        {
            fixed (uint* pIdxList = idxList)
            {
                return LLVM.ConstInsertValue(aggConstant, elementValueConstant, pIdxList, (uint)idxList.Length);
            }
        }

        public static LLVMValueRef CreateConstInt(LLVMTypeRef intTy, ulong n, bool signExtend = false) => LLVM.ConstInt(intTy, n, signExtend ? 1 : 0);

        public static LLVMValueRef CreateConstIntCast(LLVMValueRef constantVal, LLVMTypeRef toType, bool isSigned) => LLVM.ConstIntCast(constantVal, toType, isSigned ? 1 : 0);

        public static LLVMValueRef CreateConstIntOfArbitraryPrecision(LLVMTypeRef intTy, ulong[] words) => CreateConstIntOfArbitraryPrecision(intTy, words.AsSpan());

        public static LLVMValueRef CreateConstIntOfArbitraryPrecision(LLVMTypeRef intTy, ReadOnlySpan<ulong> words)
        {
            fixed (ulong* pWords = words)
            {
                return LLVM.ConstIntOfArbitraryPrecision(intTy, (uint)words.Length, pWords);
            }
        }

        public static LLVMValueRef CreateConstIntOfString(LLVMTypeRef intTy, string text, byte radix) => CreateConstIntOfString(intTy, text.AsSpan(), radix);

        public static LLVMValueRef CreateConstIntOfString(LLVMTypeRef intTy, ReadOnlySpan<char> text, byte radix)
        {
            using var marshaledText = new MarshaledString(text);
            return LLVM.ConstIntOfString(intTy, marshaledText, radix);
        }

        public static LLVMValueRef CreateConstIntOfStringAndSize(LLVMTypeRef intTy, string text, uint sLen, byte radix) => CreateConstIntOfStringAndSize(intTy, text.AsSpan(0, (int)sLen), radix);

        public static LLVMValueRef CreateConstIntOfStringAndSize(LLVMTypeRef intTy, ReadOnlySpan<char> text, byte radix)
        {
            using var marshaledText = new MarshaledString(text);
            return LLVM.ConstIntOfStringAndSize(intTy, marshaledText, (uint)marshaledText.Length, radix);
        }

        public static LLVMValueRef CreateConstIntToPtr(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstIntToPtr(constantVal, toType);

        public static LLVMValueRef CreateConstLShr(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstLShr(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstMul(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstMul(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstNamedStruct(LLVMTypeRef structTy, LLVMValueRef[] constantVals) => CreateConstNamedStruct(structTy, constantVals.AsSpan());

        public static LLVMValueRef CreateConstNamedStruct(LLVMTypeRef structTy, ReadOnlySpan<LLVMValueRef> constantVals)
        {
            fixed (LLVMValueRef* pconstantVals = constantVals)
            {
                return LLVM.ConstNamedStruct(structTy, (LLVMOpaqueValue**)pconstantVals, (uint)constantVals.Length);
            }
        }

        public static LLVMValueRef CreateConstNeg(LLVMValueRef constantVal) => LLVM.ConstNeg(constantVal);

        public static LLVMValueRef CreateConstNot(LLVMValueRef constantVal) => LLVM.ConstNot(constantVal);

        public static LLVMValueRef CreateConstNSWAdd(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstNSWAdd(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstNSWMul(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstNSWMul(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstNSWNeg(LLVMValueRef constantVal) => LLVM.ConstNSWNeg(constantVal);

        public static LLVMValueRef CreateConstNSWSub(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstNSWSub(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstNull(LLVMTypeRef ty) => LLVM.ConstNull(ty);

        public static LLVMValueRef CreateConstNUWAdd(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstNUWAdd(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstNUWMul(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstNUWMul(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstNUWNeg(LLVMValueRef constantVal) => LLVM.ConstNUWNeg(constantVal);

        public static LLVMValueRef CreateConstNUWSub(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstNUWSub(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstOr(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstOr(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstPointerCast(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstPointerCast(constantVal, toType);

        public static LLVMValueRef CreateConstPointerNull(LLVMTypeRef ty) => LLVM.ConstPointerNull(ty);

        public static LLVMValueRef CreateConstPtrToInt(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstPtrToInt(constantVal, toType);

        public static LLVMValueRef CreateConstReal(LLVMTypeRef realTy, double n) => LLVM.ConstReal(realTy, n);

        public static LLVMValueRef CreateConstRealOfString(LLVMTypeRef realTy, string text) => CreateConstRealOfString(realTy, text.AsSpan());

        public static LLVMValueRef CreateConstRealOfString(LLVMTypeRef realTy, ReadOnlySpan<char> text)
        {
            using var marshaledText = new MarshaledString(text);
            return LLVM.ConstRealOfString(realTy, marshaledText);
        }

        public static LLVMValueRef CreateConstRealOfStringAndSize(LLVMTypeRef realTy, string text, uint sLen) => CreateConstRealOfStringAndSize(realTy, text.AsSpan(0, (int)sLen));

        public static LLVMValueRef CreateConstRealOfStringAndSize(LLVMTypeRef realTy, ReadOnlySpan<char> text)
        {
            using var marshaledText = new MarshaledString(text);
            return LLVM.ConstRealOfStringAndSize(realTy, marshaledText, (uint)marshaledText.Length);
        }

        public static LLVMValueRef CreateConstSDiv(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstSDiv(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstSelect(LLVMValueRef constantCondition, LLVMValueRef constantIfTrue, LLVMValueRef constantIfFalse) => LLVM.ConstSelect(constantCondition, constantIfTrue, constantIfFalse);

        public static LLVMValueRef CreateConstSExt(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstSExt(constantVal, toType);

        public static LLVMValueRef CreateConstSExtOrBitCast(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstSExtOrBitCast(constantVal, toType);

        public static LLVMValueRef CreateConstShl(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstShl(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstShuffleVector(LLVMValueRef vectorAConstant, LLVMValueRef vectorBConstant, LLVMValueRef maskConstant) => LLVM.ConstShuffleVector(vectorAConstant, vectorBConstant, maskConstant);

        public static LLVMValueRef CreateConstSIToFP(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstSIToFP(constantVal, toType);

        public static LLVMValueRef CreateConstSRem(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstSRem(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstStruct(LLVMValueRef[] constantVals, bool packed) => CreateConstStruct(constantVals.AsSpan(), packed);

        public static LLVMValueRef CreateConstStruct(ReadOnlySpan<LLVMValueRef> constantVals, bool packed)
        {
            fixed (LLVMValueRef* pconstantVals = constantVals)
            {
                return LLVM.ConstStruct((LLVMOpaqueValue**)pconstantVals, (uint)constantVals.Length, packed ? 1 : 0);
            }
        }

        public static LLVMValueRef CreateConstSub(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstSub(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstTrunc(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstTrunc(constantVal, toType);

        public static LLVMValueRef CreateConstTruncOrBitCast(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstTruncOrBitCast(constantVal, toType);

        public static LLVMValueRef CreateConstUDiv(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstUDiv(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstUIToFP(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstUIToFP(constantVal, toType);

        public static LLVMValueRef CreateConstURem(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstURem(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstVector(LLVMValueRef[] scalarConstantVars) => CreateConstVector(scalarConstantVars.AsSpan());

        public static LLVMValueRef CreateConstVector(ReadOnlySpan<LLVMValueRef> scalarConstantVars)
        {
            fixed (LLVMValueRef* pScalarConstantVars = scalarConstantVars)
            {
                return LLVM.ConstVector((LLVMOpaqueValue**)pScalarConstantVars, (uint)scalarConstantVars.Length);
            }
        }

        public static LLVMValueRef CreateConstXor(LLVMValueRef lHSConstant, LLVMValueRef rHSConstant) => LLVM.ConstXor(lHSConstant, rHSConstant);

        public static LLVMValueRef CreateConstZExt(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstZExt(constantVal, toType);

        public static LLVMValueRef CreateConstZExtOrBitCast(LLVMValueRef constantVal, LLVMTypeRef toType) => LLVM.ConstZExtOrBitCast(constantVal, toType);

        public static LLVMValueRef CreateMDNode(LLVMValueRef[] vals) => CreateMDNode(vals.AsSpan());

        public static LLVMValueRef CreateMDNode(ReadOnlySpan<LLVMValueRef> vals)
        {
            fixed (LLVMValueRef* pVals = vals)
            {
                return LLVM.MDNode((LLVMOpaqueValue**)pVals, (uint)vals.Length);
            }
        }

        public void AddCase(LLVMValueRef onVal, LLVMBasicBlockRef dest) => LLVM.AddCase(this, onVal, dest);

        public void AddClause(LLVMValueRef clauseVal) => LLVM.AddClause(this, clauseVal);

        public void AddDestination(LLVMBasicBlockRef dest) => LLVM.AddDestination(this, dest);

        public void AddIncoming(LLVMValueRef[] incomingValues, LLVMBasicBlockRef[] incomingBlocks, uint count) => this.AddIncoming(incomingValues.AsSpan(), incomingBlocks.AsSpan(), count);

        public void AddIncoming(ReadOnlySpan<LLVMValueRef> incomingValues, ReadOnlySpan<LLVMBasicBlockRef> incomingBlocks, uint count)
        {
            fixed (LLVMValueRef* pIncomingValues = incomingValues)
            {
                fixed (LLVMBasicBlockRef* pIncomingBlocks = incomingBlocks)
                {
                    LLVM.AddIncoming(this, (LLVMOpaqueValue**)pIncomingValues, (LLVMOpaqueBasicBlock**)pIncomingBlocks, count);
                }
            }
        }

        public void AddTargetDependentFunctionAttr(string a, string v) => this.AddTargetDependentFunctionAttr(a.AsSpan(), v.AsSpan());

        public void AddTargetDependentFunctionAttr(ReadOnlySpan<char> a, ReadOnlySpan<char> v)
        {
            using var marshaledA = new MarshaledString(a);
            using var marshaledV = new MarshaledString(v);
            LLVM.AddTargetDependentFunctionAttr(this, marshaledA, marshaledV);
        }

        public LLVMBasicBlockRef AppendBasicBlock(string name) => this.AppendBasicBlock(name.AsSpan());

        public LLVMBasicBlockRef AppendBasicBlock(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.AppendBasicBlock(this, marshaledName);
        }

        public LLVMBasicBlockRef AsBasicBlock() => LLVM.ValueAsBasicBlock(this);

        public void DeleteFunction() => LLVM.DeleteFunction(this);

        public void DeleteGlobal() => LLVM.DeleteGlobal(this);

        public void Dump() => LLVM.DumpValue(this);

        public override bool Equals(object obj) => (obj is LLVMValueRef other) && this.Equals(other);

        public bool Equals(LLVMValueRef other) => this == other;

        public string GetAsString(out UIntPtr length)
        {
            fixed (UIntPtr* pLength = &length)
            {
                var pStr = LLVM.GetAsString(this, pLength);

                if (pStr == null)
                {
                    return string.Empty;
                }

                var span = new ReadOnlySpan<byte>(pStr, (int)length);
                return span.AsString();
            }
        }

        public LLVMAttributeRef[] GetAttributesAtIndex(LLVMAttributeIndex idx)
        {
            var attrs = new LLVMAttributeRef[this.GetAttributeCountAtIndex(idx)];

            fixed (LLVMAttributeRef* pAttrs = attrs)
            {
                LLVM.GetAttributesAtIndex(this, (uint)idx, (LLVMOpaqueAttributeRef**)pAttrs);
            }

            return attrs;
        }

        public uint GetAttributeCountAtIndex(LLVMAttributeIndex idx) => LLVM.GetAttributeCountAtIndex(this, (uint)idx);

        public LLVMValueRef GetBlockAddress(LLVMBasicBlockRef bB) => LLVM.BlockAddress(this, bB);

        public uint GetCallSiteAttributeCount(LLVMAttributeIndex idx) => LLVM.GetCallSiteAttributeCount(this, (uint)idx);

        public LLVMAttributeRef[] GetCallSiteAttributes(LLVMAttributeIndex idx)
        {
            var attrs = new LLVMAttributeRef[this.GetCallSiteAttributeCount(idx)];

            fixed (LLVMAttributeRef* pAttrs = attrs)
            {
                LLVM.GetCallSiteAttributes(this, (uint)idx, (LLVMOpaqueAttributeRef**)pAttrs);
            }

            return attrs;
        }

        public double GetConstRealDouble(out bool losesInfo)
        {
            int losesInfoOut;
            var result = LLVM.ConstRealGetDouble(this, &losesInfoOut);

            losesInfo = losesInfoOut != 0;
            return result;
        }

        public LLVMValueRef GetElementAsConstant(uint idx) => LLVM.GetElementAsConstant(this, idx);

        public override int GetHashCode() => this.Handle.GetHashCode();

        public LLVMBasicBlockRef GetIncomingBlock(uint index) => LLVM.GetIncomingBlock(this, index);

        public LLVMValueRef GetIncomingValue(uint index) => LLVM.GetIncomingValue(this, index);

        public string GetMDString(out uint len)
        {
            fixed (uint* pLen = &len)
            {
                var pMDStr = LLVM.GetMDString(this, pLen);

                if (pMDStr == null)
                {
                    return string.Empty;
                }

                var span = new ReadOnlySpan<byte>(pMDStr, (int)len);
                return span.AsString();
            }
        }

        public LLVMValueRef GetMetadata(uint kindID) => LLVM.GetMetadata(this, kindID);

        public LLVMValueRef GetOperand(uint index) => LLVM.GetOperand(this, index);

        public LLVMUseRef GetOperandUse(uint index) => LLVM.GetOperandUse(this, index);

        public LLVMValueRef GetParam(uint index) => LLVM.GetParam(this, index);

        public LLVMBasicBlockRef GetSuccessor(uint i) => LLVM.GetSuccessor(this, i);

        public void InstructionEraseFromParent() => LLVM.InstructionEraseFromParent(this);

        public string PrintToString()
        {
            var pStr = LLVM.PrintValueToString(this);

            if (pStr == null)
            {
                return string.Empty;
            }

            var span = new ReadOnlySpan<byte>(pStr, int.MaxValue);

            var result = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            LLVM.DisposeMessage(pStr);
            return result;
        }

        public void ReplaceAllUsesWith(LLVMValueRef newVal) => LLVM.ReplaceAllUsesWith(this, newVal);

        public void SetAlignment(uint bytes)
        {
            this.Alignment = bytes;
        }

        public void SetInstrParamAlignment(uint index, uint align) => LLVM.SetInstrParamAlignment(this, index, align);

        public void SetMetadata(uint kindID, LLVMValueRef node) => LLVM.SetMetadata(this, kindID, node);

        public void SetOperand(uint index, LLVMValueRef val) => LLVM.SetOperand(this, index, val);

        public void SetParamAlignment(uint align) => LLVM.SetParamAlignment(this, align);

        public void SetSuccessor(uint i, LLVMBasicBlockRef block) => LLVM.SetSuccessor(this, i, block);

        public override string ToString() => (this.Handle != IntPtr.Zero) ? this.PrintToString() : string.Empty;

        public bool VerifyFunction(LLVMVerifierFailureAction action) => LLVM.VerifyFunction(this, action) == 0;

        public void ViewFunctionCFG() => LLVM.ViewFunctionCFG(this);

        public void ViewFunctionCFGOnly() => LLVM.ViewFunctionCFGOnly(this);
    }
}
