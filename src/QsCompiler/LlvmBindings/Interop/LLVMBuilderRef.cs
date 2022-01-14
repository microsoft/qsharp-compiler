// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMBuilderRef : IDisposable, IEquatable<LLVMBuilderRef>
    {
        public IntPtr Handle;

        public LLVMBuilderRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public LLVMValueRef CurrentDebugLocation
        {
            get => (this.Handle != IntPtr.Zero) ? LLVM.GetCurrentDebugLocation(this) : default;
            set => LLVM.SetCurrentDebugLocation(this, value);
        }

        public LLVMBasicBlockRef InsertBlock => (this.Handle != IntPtr.Zero) ? LLVM.GetInsertBlock(this) : default;

        public static implicit operator LLVMBuilderRef(LLVMOpaqueBuilder* builder) => new LLVMBuilderRef((IntPtr)builder);

        public static implicit operator LLVMOpaqueBuilder*(LLVMBuilderRef builder) => (LLVMOpaqueBuilder*)builder.Handle;

        public static bool operator ==(LLVMBuilderRef left, LLVMBuilderRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMBuilderRef left, LLVMBuilderRef right) => !(left == right);

        public static LLVMBuilderRef Create(LLVMContextRef c) => LLVM.CreateBuilderInContext(c);

        public LLVMValueRef BuildAdd(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildAdd(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildAdd(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildAdd(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildAddrSpaceCast(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildAddrSpaceCast(val, destTy, name.AsSpan());

        public LLVMValueRef BuildAddrSpaceCast(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildAddrSpaceCast(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildAggregateRet(LLVMValueRef[] retVals) => this.BuildAggregateRet(retVals.AsSpan());

        public LLVMValueRef BuildAggregateRet(ReadOnlySpan<LLVMValueRef> retVals)
        {
            fixed (LLVMValueRef* pRetVals = retVals)
            {
                return LLVM.BuildAggregateRet(this, (LLVMOpaqueValue**)pRetVals, (uint)retVals.Length);
            }
        }

        public LLVMValueRef BuildAlloca(LLVMTypeRef ty, string name = "") => this.BuildAlloca(ty, name.AsSpan());

        public LLVMValueRef BuildAlloca(LLVMTypeRef ty, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildAlloca(this, ty, marshaledName);
        }

        public LLVMValueRef BuildAnd(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildAnd(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildAnd(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildAnd(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildArrayAlloca(LLVMTypeRef ty, LLVMValueRef val, string name = "") => this.BuildArrayAlloca(ty, val, name.AsSpan());

        public LLVMValueRef BuildArrayAlloca(LLVMTypeRef ty, LLVMValueRef val, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildArrayAlloca(this, ty, val, marshaledName);
        }

        public LLVMValueRef BuildArrayMalloc(LLVMTypeRef ty, LLVMValueRef val, string name = "") => this.BuildArrayMalloc(ty, val, name.AsSpan());

        public LLVMValueRef BuildArrayMalloc(LLVMTypeRef ty, LLVMValueRef val, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildArrayMalloc(this, ty, val, marshaledName);
        }

        public LLVMValueRef BuildAShr(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildAShr(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildAShr(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildAShr(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildAtomicRMW(LLVMAtomicRMWBinOp op, LLVMValueRef pTR, LLVMValueRef val, LLVMAtomicOrdering ordering, bool singleThread) => LLVM.BuildAtomicRMW(this, op, pTR, val, ordering, singleThread ? 1 : 0);

        public LLVMValueRef BuildBinOp(LLVMOpcode op, LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildBinOp(op, lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildBinOp(LLVMOpcode op, LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildBinOp(this, op, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildBitCast(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildBitCast(val, destTy, name.AsSpan());

        public LLVMValueRef BuildBitCast(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildBitCast(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildBr(LLVMBasicBlockRef dest) => LLVM.BuildBr(this, dest);

        public LLVMValueRef BuildCall(LLVMValueRef fn, LLVMValueRef[] args, string name = "") => this.BuildCall(fn, args.AsSpan(), name.AsSpan());

        public LLVMValueRef BuildCall(LLVMValueRef fn, ReadOnlySpan<LLVMValueRef> args, ReadOnlySpan<char> name)
        {
            fixed (LLVMValueRef* pArgs = args)
            {
                using var marshaledName = new MarshaledString(name);
                return LLVM.BuildCall(this, fn, (LLVMOpaqueValue**)pArgs, (uint)args.Length, marshaledName);
            }
        }

        public LLVMValueRef BuildCast(LLVMOpcode op, LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildCast(op, val, destTy, name.AsSpan());

        public LLVMValueRef BuildCast(LLVMOpcode op, LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildCast(this, op, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildCondBr(LLVMValueRef ifVal, LLVMBasicBlockRef thenVal, LLVMBasicBlockRef elseVal) => LLVM.BuildCondBr(this, ifVal, thenVal, elseVal);

        public LLVMValueRef BuildExactSDiv(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildExactSDiv(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildExactSDiv(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildExactSDiv(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildExtractElement(LLVMValueRef vecVal, LLVMValueRef index, string name = "") => this.BuildExtractElement(vecVal, index, name.AsSpan());

        public LLVMValueRef BuildExtractElement(LLVMValueRef vecVal, LLVMValueRef index, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildExtractElement(this, vecVal, index, marshaledName);
        }

        public LLVMValueRef BuildExtractValue(LLVMValueRef aggVal, uint index, string name = "") => this.BuildExtractValue(aggVal, index, name.AsSpan());

        public LLVMValueRef BuildExtractValue(LLVMValueRef aggVal, uint index, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildExtractValue(this, aggVal, index, marshaledName);
        }

        public LLVMValueRef BuildFAdd(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildFAdd(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildFAdd(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFAdd(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildFCmp(LLVMRealPredicate op, LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildFCmp(op, lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildFCmp(LLVMRealPredicate op, LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFCmp(this, op, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildFDiv(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildFDiv(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildFDiv(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFDiv(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildFence(LLVMAtomicOrdering ordering, bool singleThread, string name = "") => this.BuildFence(ordering, singleThread, name.AsSpan());

        public LLVMValueRef BuildFence(LLVMAtomicOrdering ordering, bool singleThread, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFence(this, ordering, singleThread ? 1 : 0, marshaledName);
        }

        public LLVMValueRef BuildFMul(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildFMul(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildFMul(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFMul(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildFNeg(LLVMValueRef v, string name = "") => this.BuildFNeg(v, name.AsSpan());

        public LLVMValueRef BuildFNeg(LLVMValueRef v, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFNeg(this, v, marshaledName);
        }

        public LLVMValueRef BuildFPCast(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildFPCast(val, destTy, name.AsSpan());

        public LLVMValueRef BuildFPCast(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFPCast(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildFPExt(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildFPCast(val, destTy, name.AsSpan());

        public LLVMValueRef BuildFPExt(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFPExt(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildFPToSI(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildFPToSI(val, destTy, name.AsSpan());

        public LLVMValueRef BuildFPToSI(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFPToSI(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildFPToUI(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildFPToUI(val, destTy, name.AsSpan());

        public LLVMValueRef BuildFPToUI(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFPToUI(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildFPTrunc(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildFPTrunc(val, destTy, name.AsSpan());

        public LLVMValueRef BuildFPTrunc(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFPTrunc(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildFree(LLVMValueRef pointerVal) => LLVM.BuildFree(this, pointerVal);

        public LLVMValueRef BuildFreeze(LLVMValueRef val, string name = "") => this.BuildFreeze(val, name.AsSpan());

        public LLVMValueRef BuildFreeze(LLVMValueRef val, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFreeze(this, val, marshaledName);
        }

        public LLVMValueRef BuildFRem(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildFRem(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildFRem(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFRem(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildFSub(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildFSub(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildFSub(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildFSub(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildGEP(LLVMValueRef pointer, LLVMValueRef[] indices, string name = "") => this.BuildGEP(pointer, indices.AsSpan(), name.AsSpan());

        public LLVMValueRef BuildGEP(LLVMValueRef pointer, ReadOnlySpan<LLVMValueRef> indices, ReadOnlySpan<char> name)
        {
            fixed (LLVMValueRef* pindices = indices)
            {
                using var marshaledName = new MarshaledString(name);
                return LLVM.BuildGEP(this, pointer, (LLVMOpaqueValue**)pindices, (uint)indices.Length, marshaledName);
            }
        }

        public LLVMValueRef BuildGlobalString(string str, string name = "") => this.BuildGlobalString(str.AsSpan(), name.AsSpan());

        public LLVMValueRef BuildGlobalString(ReadOnlySpan<char> str, ReadOnlySpan<char> name)
        {
            using var marshaledStr = new MarshaledString(str);
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildGlobalString(this, marshaledStr, marshaledName);
        }

        public LLVMValueRef BuildGlobalStringPtr(string str, string name = "") => this.BuildGlobalStringPtr(str.AsSpan(), name.AsSpan());

        public LLVMValueRef BuildGlobalStringPtr(ReadOnlySpan<char> str, ReadOnlySpan<char> name)
        {
            using var marshaledStr = new MarshaledString(str);
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildGlobalStringPtr(this, marshaledStr, marshaledName);
        }

        public LLVMValueRef BuildICmp(LLVMIntPredicate op, LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildICmp(op, lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildICmp(LLVMIntPredicate op, LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildICmp(this, op, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildInBoundsGEP(LLVMValueRef pointer, LLVMValueRef[] indices, string name = "") => this.BuildInBoundsGEP(pointer, indices.AsSpan(), name.AsSpan());

        public LLVMValueRef BuildInBoundsGEP(LLVMValueRef pointer, ReadOnlySpan<LLVMValueRef> indices, ReadOnlySpan<char> name)
        {
            fixed (LLVMValueRef* pindices = indices)
            {
                using var marshaledName = new MarshaledString(name);
                return LLVM.BuildInBoundsGEP(this, pointer, (LLVMOpaqueValue**)pindices, (uint)indices.Length, marshaledName);
            }
        }

        public LLVMValueRef BuildIndirectBr(LLVMValueRef addr, uint numDests) => LLVM.BuildIndirectBr(this, addr, numDests);

        public LLVMValueRef BuildInsertElement(LLVMValueRef vecVal, LLVMValueRef eltVal, LLVMValueRef index, string name = "") => this.BuildInsertElement(vecVal, eltVal, index, name.AsSpan());

        public LLVMValueRef BuildInsertElement(LLVMValueRef vecVal, LLVMValueRef eltVal, LLVMValueRef index, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildInsertElement(this, vecVal, eltVal, index, marshaledName);
        }

        public LLVMValueRef BuildInsertValue(LLVMValueRef aggVal, LLVMValueRef eltVal, uint index, string name = "") => this.BuildInsertValue(aggVal, eltVal, index, name.AsSpan());

        public LLVMValueRef BuildInsertValue(LLVMValueRef aggVal, LLVMValueRef eltVal, uint index, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildInsertValue(this, aggVal, eltVal, index, marshaledName);
        }

        public LLVMValueRef BuildIntCast(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildIntCast(val, destTy, name.AsSpan());

        public LLVMValueRef BuildIntCast(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildIntCast(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildIntToPtr(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildIntToPtr(val, destTy, name.AsSpan());

        public LLVMValueRef BuildIntToPtr(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildIntToPtr(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildInvoke(LLVMValueRef fn, LLVMValueRef[] args, LLVMBasicBlockRef then, LLVMBasicBlockRef @catch, string name = "") => this.BuildInvoke(fn, args.AsSpan(), then, @catch, name.AsSpan());

        public LLVMValueRef BuildInvoke(LLVMValueRef fn, ReadOnlySpan<LLVMValueRef> args, LLVMBasicBlockRef then, LLVMBasicBlockRef @catch, ReadOnlySpan<char> name)
        {
            fixed (LLVMValueRef* pArgs = args)
            {
                using var marshaledName = new MarshaledString(name);
                return LLVM.BuildInvoke(this, fn, (LLVMOpaqueValue**)pArgs, (uint)args.Length, then, @catch, marshaledName);
            }
        }

        public LLVMValueRef BuildIsNotNull(LLVMValueRef val, string name = "") => this.BuildIsNotNull(val, name.AsSpan());

        public LLVMValueRef BuildIsNotNull(LLVMValueRef val, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildIsNotNull(this, val, marshaledName);
        }

        public LLVMValueRef BuildIsNull(LLVMValueRef val, string name = "") => this.BuildIsNull(val, name.AsSpan());

        public LLVMValueRef BuildIsNull(LLVMValueRef val, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildIsNull(this, val, marshaledName);
        }

        public LLVMValueRef BuildLandingPad(LLVMTypeRef ty, LLVMValueRef persFn, uint numClauses, string name = "") => this.BuildLandingPad(ty, persFn, numClauses, name.AsSpan());

        public LLVMValueRef BuildLandingPad(LLVMTypeRef ty, LLVMValueRef persFn, uint numClauses, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildLandingPad(this, ty, persFn, numClauses, marshaledName);
        }

        public LLVMValueRef BuildLoad(LLVMValueRef pointerVal, string name = "") => this.BuildLoad(pointerVal, name.AsSpan());

        public LLVMValueRef BuildLoad(LLVMValueRef pointerVal, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildLoad(this, pointerVal, marshaledName);
        }

        public LLVMValueRef BuildLShr(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildLShr(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildLShr(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildLShr(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildMalloc(LLVMTypeRef ty, string name = "") => this.BuildMalloc(ty, name.AsSpan());

        public LLVMValueRef BuildMalloc(LLVMTypeRef ty, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildMalloc(this, ty, marshaledName);
        }

        public LLVMValueRef BuildMul(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildMul(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildMul(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildMul(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildNeg(LLVMValueRef v, string name = "") => this.BuildNeg(v, name.AsSpan());

        public LLVMValueRef BuildNeg(LLVMValueRef v, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNeg(this, v, marshaledName);
        }

        public LLVMValueRef BuildNot(LLVMValueRef v, string name = "") => this.BuildNot(v, name.AsSpan());

        public LLVMValueRef BuildNot(LLVMValueRef v, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNot(this, v, marshaledName);
        }

        public LLVMValueRef BuildNSWAdd(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildNSWAdd(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildNSWAdd(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNSWAdd(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildNSWMul(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildNSWMul(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildNSWMul(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNSWMul(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildNSWNeg(LLVMValueRef v, string name = "") => this.BuildNSWNeg(v, name.AsSpan());

        public LLVMValueRef BuildNSWNeg(LLVMValueRef v, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNSWNeg(this, v, marshaledName);
        }

        public LLVMValueRef BuildNSWSub(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildNSWSub(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildNSWSub(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNSWSub(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildNUWAdd(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildNUWAdd(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildNUWAdd(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNUWAdd(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildNUWMul(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildNUWMul(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildNUWMul(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNUWMul(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildNUWNeg(LLVMValueRef v, string name = "") => this.BuildNUWNeg(v, name.AsSpan());

        public LLVMValueRef BuildNUWNeg(LLVMValueRef v, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNUWNeg(this, v, marshaledName);
        }

        public LLVMValueRef BuildNUWSub(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildNUWSub(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildNUWSub(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildNUWSub(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildOr(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildOr(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildOr(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildOr(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildPhi(LLVMTypeRef ty, string name = "") => this.BuildPhi(ty, name.AsSpan());

        public LLVMValueRef BuildPhi(LLVMTypeRef ty, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildPhi(this, ty, marshaledName);
        }

        public LLVMValueRef BuildPointerCast(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildPointerCast(val, destTy, name.AsSpan());

        public LLVMValueRef BuildPointerCast(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildPointerCast(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildPtrDiff(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildPtrDiff(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildPtrDiff(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildPtrDiff(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildPtrToInt(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildPtrToInt(val, destTy, name.AsSpan());

        public LLVMValueRef BuildPtrToInt(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildPtrToInt(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildResume(LLVMValueRef exn) => LLVM.BuildResume(this, exn);

        public LLVMValueRef BuildRet(LLVMValueRef v) => LLVM.BuildRet(this, v);

        public LLVMValueRef BuildRetVoid() => LLVM.BuildRetVoid(this);

        public LLVMValueRef BuildSDiv(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildSDiv(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildSDiv(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildSDiv(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildSelect(LLVMValueRef @if, LLVMValueRef then, LLVMValueRef @else, string name = "") => this.BuildSelect(@if, then, @else, name.AsSpan());

        public LLVMValueRef BuildSelect(LLVMValueRef @if, LLVMValueRef then, LLVMValueRef @else, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildSelect(this, @if, then, @else, marshaledName);
        }

        public LLVMValueRef BuildSExt(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildSExt(val, destTy, name.AsSpan());

        public LLVMValueRef BuildSExt(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildSExt(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildSExtOrBitCast(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildSExtOrBitCast(val, destTy, name.AsSpan());

        public LLVMValueRef BuildSExtOrBitCast(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildSExtOrBitCast(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildShl(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildShl(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildShl(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildShl(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildShuffleVector(LLVMValueRef v1, LLVMValueRef v2, LLVMValueRef mask, string name = "") => this.BuildShuffleVector(v1, v2, mask, name.AsSpan());

        public LLVMValueRef BuildShuffleVector(LLVMValueRef v1, LLVMValueRef v2, LLVMValueRef mask, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildShuffleVector(this, v1, v2, mask, marshaledName);
        }

        public LLVMValueRef BuildSIToFP(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildSIToFP(val, destTy, name.AsSpan());

        public LLVMValueRef BuildSIToFP(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildSIToFP(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildSRem(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildSRem(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildSRem(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildSRem(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildStore(LLVMValueRef val, LLVMValueRef ptr) => LLVM.BuildStore(this, val, ptr);

        public LLVMValueRef BuildStructGEP(LLVMValueRef pointer, uint idx, string name = "") => this.BuildStructGEP(pointer, idx, name.AsSpan());

        public LLVMValueRef BuildStructGEP(LLVMValueRef pointer, uint idx, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildStructGEP(this, pointer, idx, marshaledName);
        }

        public LLVMValueRef BuildSub(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildSub(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildSub(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildSub(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildSwitch(LLVMValueRef v, LLVMBasicBlockRef @else, uint numCases) => LLVM.BuildSwitch(this, v, @else, numCases);

        public LLVMValueRef BuildTrunc(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildTrunc(val, destTy, name.AsSpan());

        public LLVMValueRef BuildTrunc(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildTrunc(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildTruncOrBitCast(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildTruncOrBitCast(val, destTy, name.AsSpan());

        public LLVMValueRef BuildTruncOrBitCast(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildTruncOrBitCast(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildUDiv(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildUDiv(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildUDiv(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildUDiv(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildUIToFP(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildUIToFP(val, destTy, name.AsSpan());

        public LLVMValueRef BuildUIToFP(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildUIToFP(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildUnreachable() => LLVM.BuildUnreachable(this);

        public LLVMValueRef BuildURem(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildURem(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildURem(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildURem(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildVAArg(LLVMValueRef list, LLVMTypeRef ty, string name = "") => this.BuildVAArg(list, ty, name.AsSpan());

        public LLVMValueRef BuildVAArg(LLVMValueRef list, LLVMTypeRef ty, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildVAArg(this, list, ty, marshaledName);
        }

        public LLVMValueRef BuildXor(LLVMValueRef lHS, LLVMValueRef rHS, string name = "") => this.BuildXor(lHS, rHS, name.AsSpan());

        public LLVMValueRef BuildXor(LLVMValueRef lHS, LLVMValueRef rHS, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildXor(this, lHS, rHS, marshaledName);
        }

        public LLVMValueRef BuildZExt(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildZExt(val, destTy, name.AsSpan());

        public LLVMValueRef BuildZExt(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildZExt(this, val, destTy, marshaledName);
        }

        public LLVMValueRef BuildZExtOrBitCast(LLVMValueRef val, LLVMTypeRef destTy, string name = "") => this.BuildZExtOrBitCast(val, destTy, name.AsSpan());

        public LLVMValueRef BuildZExtOrBitCast(LLVMValueRef val, LLVMTypeRef destTy, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.BuildZExtOrBitCast(this, val, destTy, marshaledName);
        }

        public void ClearInsertionPosition() => LLVM.ClearInsertionPosition(this);

        public void Dispose()
        {
            if (this.Handle != IntPtr.Zero)
            {
                LLVM.DisposeBuilder(this);
                this.Handle = IntPtr.Zero;
            }
        }

        public override bool Equals(object obj) => (obj is LLVMBuilderRef other) && this.Equals(other);

        public bool Equals(LLVMBuilderRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public void Insert(LLVMValueRef instr) => LLVM.InsertIntoBuilder(this, instr);

        public void InsertWithName(LLVMValueRef instr, string name = "") => this.InsertWithName(instr, name.AsSpan());

        public void InsertWithName(LLVMValueRef instr, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            LLVM.InsertIntoBuilderWithName(this, instr, marshaledName);
        }

        public void Position(LLVMBasicBlockRef block, LLVMValueRef instr) => LLVM.PositionBuilder(this, block, instr);

        public void PositionAtEnd(LLVMBasicBlockRef block) => LLVM.PositionBuilderAtEnd(this, block);

        public void PositionBefore(LLVMValueRef instr) => LLVM.PositionBuilderBefore(this, instr);

        public void SetInstDebugLocation(LLVMValueRef inst) => LLVM.SetInstDebugLocation(this, inst);

        public override string ToString() => $"{nameof(LLVMBuilderRef)}: {this.Handle:X}";
    }
}
