// -----------------------------------------------------------------------
// <copyright file="Value.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>LLVM Value.</summary>
    /// <remarks>
    /// Value is the root of a hierarchy of types representing values in LLVM. Values (and derived classes)
    /// are never constructed directly with the new operator. Instead, they are produced by other classes
    /// in this library internally. This is because they are just wrappers around the LLVM-C API handles
    /// and must maintain the "uniqueing" semantics. (e.g. allowing reference equality for values that are
    /// fundamentally the same value). This is generally hidden in the internals of the Ubiquity.NET.Llvm library so
    /// that callers need not be concerned with the details but can rely on the expected behavior that two
    /// Value instances referring to the same actual value (i.e. a function) are actually the same .NET object
    /// as well within the same <see cref="Llvm.Context"/>.
    /// </remarks>
    public class Value
    {
        internal Value(LLVMValueRef valueRef)
        {
            if (valueRef == default)
            {
                throw new ArgumentNullException(nameof(valueRef));
            }

            this.ValueHandle = valueRef;
        }

        /// <summary>Gets or sets name of the value (if any).</summary>
        /// <remarks>
        /// <note type="note">
        /// LLVM will add a numeric suffix to the name set if a
        /// value with the name already exists. Thus, the name
        /// read from this property may not match what is set.
        /// </note>
        /// </remarks>
        public string Name
        {
            get => this.Context.IsDisposed ? string.Empty : this.ValueHandle.Name;

            set
            {
                var val = this.ValueHandle;
                val.Name = value;
            }
        }

        /// <summary>Gets a value indicating whether this value is Undefined.</summary>
        public bool IsUndefined => this.ValueHandle.IsUndef;

        /// <summary>Gets a value indicating whether the Value represents the NULL value for the values type.</summary>
        public bool IsNull => this.ValueHandle.IsNull;

        /// <summary>Gets the type of the value.</summary>
        public ITypeRef NativeType => TypeRef.FromHandle(this.ValueHandle.TypeOf)!;

        /// <summary>Gets the context for this value.</summary>
        public Context Context => this.NativeType.Context;

        /// <summary>Gets a value indicating whether the Value is an instruction.</summary>
        public bool IsInstruction => this.ValueHandle.Kind == LLVMValueKind.LLVMInstructionValueKind;

        /// <summary>Gets a value indicating whether the Value is a function.</summary>
        public bool IsFunction => this.ValueHandle.Kind == LLVMValueKind.LLVMFunctionValueKind;

        internal LLVMValueRef ValueHandle { get; }

        /// <summary>Generates a string representing the LLVM syntax of the value.</summary>
        /// <returns>string version of the value formatted by LLVM.</returns>
        public override string ToString() => this.ValueHandle == default ? string.Empty : this.ValueHandle.PrintToString();

        /// <summary>Replace all uses of a <see cref="Value"/> with another one.</summary>
        /// <param name="other">New value.</param>
        public void ReplaceAllUsesWith(Value other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }

            this.ValueHandle.ReplaceAllUsesWith(other.ValueHandle);
        }

        /// <summary>Gets an Ubiquity.NET.Llvm managed wrapper for a LibLLVM value handle.</summary>
        /// <param name="valueRef">Value handle to wrap.</param>
        /// <returns>Ubiquity.NET.Llvm managed instance for the handle.</returns>
        /// <remarks>
        /// This method uses a cached mapping to ensure that two calls given the same
        /// input handle returns the same managed instance so that reference equality
        /// works as expected.
        /// </remarks>
        internal static Value FromHandle(LLVMValueRef valueRef) => FromHandle<Value>(valueRef);

        /// <summary>Gets an Ubiquity.NET.Llvm managed wrapper for a LibLLVM value handle.</summary>
        /// <typeparam name="T">Required type for the handle.</typeparam>
        /// <param name="valueRef">Value handle to wrap.</param>
        /// <returns>Ubiquity.NET.Llvm managed instance for the handle.</returns>
        /// <remarks>
        /// This method uses a cached mapping to ensure that two calls given the same
        /// input handle returns the same managed instance so that reference equality
        /// works as expected.
        /// </remarks>
        /// <exception cref="InvalidCastException">When the handle is for a different type of handle than specified by <typeparamref name="T"/>.</exception>
        internal static T FromHandle<T>(LLVMValueRef valueRef)
            where T : Value
        {
            var context = valueRef.GetContext();

            return (T)context.GetValueFor(valueRef);
        }

        internal class InterningFactory
            : HandleInterningMap<LLVMValueRef, Value>
        {
            internal InterningFactory(Context context)
                : base(context)
            {
            }

            private protected override Value ItemFactory(LLVMValueRef handle)
            {
                var handleContext = handle.GetContext();
                if (handleContext != this.Context)
                {
                    throw new ArgumentException();
                }

                var kind = handle.Kind;
                switch (kind)
                {
                    case LLVMValueKind.LLVMArgumentValueKind:
                        return new Argument(handle);

                    case LLVMValueKind.LLVMBasicBlockValueKind:
                        return new BasicBlock(handle);

                    case LLVMValueKind.LLVMFunctionValueKind:
                        return new IrFunction(handle);

                    case LLVMValueKind.LLVMGlobalAliasValueKind:
                        return new GlobalAlias(handle);

                    case LLVMValueKind.LLVMGlobalVariableValueKind:
                        return new GlobalVariable(handle);

                    case LLVMValueKind.LLVMConstantDataArrayValueKind:
                        return new ConstantDataArray(handle);

                    case LLVMValueKind.LLVMConstantIntValueKind:
                        return new ConstantInt(handle);

                    case LLVMValueKind.LLVMConstantFPValueKind:
                        return new ConstantFP(handle);

                    case LLVMValueKind.LLVMConstantArrayValueKind:
                        return new ConstantArray(handle);

                    case LLVMValueKind.LLVMInstructionValueKind:
                        // Need to determine what kind of instruction it is.
                        if (handle.IsAAllocaInst is LLVMValueRef allocaInst &&
                            allocaInst != default)
                        {
                            return new Alloca(allocaInst);
                        }
                        else if (handle.IsABranchInst is LLVMValueRef branchInst &&
                            branchInst != default)
                        {
                            return new Branch(branchInst);
                        }
                        else if (handle.IsACallInst is LLVMValueRef callInst &&
                            callInst != default)
                        {
                            return new CallInstruction(callInst);
                        }
                        else if (handle.IsALoadInst is LLVMValueRef loadInst &&
                            loadInst != default)
                        {
                            return new Load(loadInst);
                        }
                        else if (handle.IsAPHINode is LLVMValueRef phiNode &&
                            phiNode != default)
                        {
                            return new PhiNode(phiNode);
                        }
                        else if (handle.IsAReturnInst is LLVMValueRef returnInst &&
                            returnInst != default)
                        {
                            return new ReturnInstruction(returnInst);
                        }
                        else if (handle.IsAStoreInst is LLVMValueRef storeInst &&
                            storeInst != default)
                        {
                            return new Store(storeInst);
                        }
                        else if (handle.IsAUnreachableInst is LLVMValueRef unreachableInst &&
                            unreachableInst != default)
                        {
                            return new Unreachable(unreachableInst);
                        }

                        return new Instruction(handle);

                    // Default to generic base Value
                    default:
                        if (handle.IsAConstant is LLVMValueRef constantVal &&
                            constantVal != default)
                        {
                            return new Constant(constantVal);
                        }

                        return new Value(handle);
                }
            }
        }
    }
}
