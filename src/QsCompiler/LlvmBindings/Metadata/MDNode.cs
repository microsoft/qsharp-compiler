// -----------------------------------------------------------------------
// <copyright file="MDNode.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Metadata node for LLVM IR Bitcode modules</summary>
    /// <remarks>
    /// <para>Metadata nodes may be uniqued, or distinct. Temporary nodes with
    /// support for <see cref="ReplaceAllUsesWith(LlvmMetadata)"/> may be used to
    /// defer uniqueing until the forward references are known.</para>
    /// <para>There is limited support for <see cref="ReplaceAllUsesWith(LlvmMetadata)"/>
    /// at construction. At construction, if any operand is a temporary or otherwise unresolved
    /// uniqued node, the node itself is unresolved. As soon as all operands become resolved
    /// the node will no longer support <see cref="ReplaceAllUsesWith(LlvmMetadata)"/></para>
    /// </remarks>
    public class MDNode
        : LlvmMetadata
    {
        /// <summary>Gets the <see cref="Context"/> this node belongs to</summary>
        public Context Context
        {
            get
            {
                this.ThrowIfDeleted();
                return GetMetadataContext(this.MetadataHandle);
            }
        }

        /// <summary>Gets a value indicating whether this node was deleted</summary>
        public bool IsDeleted => this.MetadataHandle == default;

        /// <summary>Gets the operands for this node, if any</summary>
        public MetadataOperandCollection Operands { get; }

        /// <summary>Replace all uses of this node with a new node</summary>
        /// <param name="other">Node to replace this one with</param>
        public override void ReplaceAllUsesWith(LlvmMetadata other)
        {
            if (this.MetadataHandle == default)
            {
                throw new InvalidOperationException();
            }

            // grab the context before replacement as replace deletes and invalidates the node
            var context = this.Context;
            this.MetadataHandle.ReplaceAllUsesWith(other.MetadataHandle);

            // remove current node mapping from the context.
            // It won't be valid for use after clearing the handle
            context!.RemoveDeletedNode(this);
            this.MetadataHandle = default;
        }

        /// <summary>Gets an operand by index as a specific type</summary>
        /// <typeparam name="T">Type of the operand</typeparam>
        /// <param name="index">Index of the operand</param>
        /// <returns>Operand</returns>
        /// <exception cref="InvalidCastException">When the operand is not castable to <typeparamref name="T"/></exception>
        /// <exception cref="ArgumentOutOfRangeException">When the index is out of range for the operands of this node</exception>
        public T? GetOperand<T>(int index)
            where T : LlvmMetadata
        {
            return this.Operands.GetOperand<T>(index);
        }

        public Value? GetOperandValue(int index)
        {
            return this.Operands.GetOperandValue(index);
        }

        /// <summary>Gets a string operand by index</summary>
        /// <param name="index">Index of the operand</param>
        /// <returns>String value of the operand</returns>
        public string GetOperandString(int index)
            => this.GetOperand<MDString>(index)?.ToString() ?? string.Empty;

        internal MDNode(LLVMMetadataRef handle)
            : base(handle)
        {
            this.Operands = new MetadataOperandCollection(this);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Context created here is owned, and disposed of via the ContextCache")]
        internal static T? FromHandle<T>(LLVMMetadataRef handle)
            where T : MDNode
        {
            return handle == default ? null : FromHandle<T>(GetMetadataContext(handle), handle);
        }

        private void ThrowIfDeleted()
        {
            if (this.IsDeleted)
            {
                throw new InvalidOperationException("Cannot operate on a deleted node");
            }
        }

        private static Context GetMetadataContext(LLVMMetadataRef metadataHandle)
        {
            return ThreadContextCache.Get();
        }
    }
}
