// -----------------------------------------------------------------------
// <copyright file="CallInstruction.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Call instruction.</summary>
    /// <seealso href="xref:llvm_langref#call-instruction"/>
    public class CallInstruction
        : Instruction,
        IAttributeAccessor
    {
        internal CallInstruction(LLVMValueRef valueRef)
            : base(valueRef)
        {
            this.Attributes = new ValueAttributeDictionary(this, () => this.TargetFunction);
        }

        /// <summary>Gets the target function of the call.</summary>
        public unsafe IrFunction TargetFunction
            => FromHandle<IrFunction>(LLVM.GetCalledValue(this.ValueHandle))!;

        /// <summary>Gets or sets a value indicating whether the call is a tail call.</summary>
        public unsafe bool IsTailCall
        {
            get => this.ValueHandle.IsTailCall;
            set => LLVM.SetTailCall(this.ValueHandle, value ? 1 : 0);
        }

        /// <summary>Gets the attributes for this call site.</summary>
        public IAttributeDictionary Attributes { get; }

        /// <inheritdoc/>
        public unsafe void AddAttributeAtIndex(FunctionAttributeIndex index, AttributeValue attrib)
        {
            attrib.VerifyValidOn(index, this);
            LLVM.AddCallSiteAttribute(this.ValueHandle, (uint)index, attrib.NativeAttribute);
        }

        /// <inheritdoc/>
        public uint GetAttributeCountAtIndex(FunctionAttributeIndex index)
        {
            return this.ValueHandle.GetCallSiteAttributeCount((LLVMAttributeIndex)index);
        }

        /// <inheritdoc/>
        public IEnumerable<AttributeValue> GetAttributesAtIndex(FunctionAttributeIndex index)
        {
            uint count = this.GetAttributeCountAtIndex(index);
            if (count == 0)
            {
                return Enumerable.Empty<AttributeValue>();
            }

            var buffer = this.ValueHandle.GetCallSiteAttributes((LLVMAttributeIndex)index);
            return from attribRef in buffer
                   select AttributeValue.FromHandle(this.Context, attribRef);
        }

        /// <inheritdoc/>
        public unsafe AttributeValue GetAttributeAtIndex(FunctionAttributeIndex index, AttributeKind kind)
        {
            var handle = LLVM.GetCallSiteEnumAttribute(this.ValueHandle, (uint)index, kind.GetEnumAttributeId());
            return AttributeValue.FromHandle(this.Context, handle);
        }

        /// <inheritdoc/>
        public unsafe AttributeValue GetAttributeAtIndex(FunctionAttributeIndex index, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException();
            }

            var handle = LLVM.GetCallSiteStringAttribute(this.ValueHandle, (uint)index, name.AsMarshaledString(), (uint)name.Length);
            return AttributeValue.FromHandle(this.Context, handle);
        }

        /// <inheritdoc/>
        public unsafe void RemoveAttributeAtIndex(FunctionAttributeIndex index, AttributeKind kind)
        {
            LLVM.RemoveCallSiteEnumAttribute(this.ValueHandle, (uint)index, kind.GetEnumAttributeId());
        }

        /// <inheritdoc/>
        public unsafe void RemoveAttributeAtIndex(FunctionAttributeIndex index, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            LLVM.RemoveCallSiteStringAttribute(this.ValueHandle, (uint)index, name.AsMarshaledString(), (uint)name.Length);
        }
    }
}
