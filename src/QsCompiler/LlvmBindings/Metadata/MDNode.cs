// -----------------------------------------------------------------------
// <copyright file="MDNode.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

using Ubiquity.ArgValidators;
using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Properties;

using static Ubiquity.NET.Llvm.Interop.NativeMethods;

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
                ThrowIfDeleted( );
                return GetMeatadataContext( MetadataHandle );
            }
        }

        /// <summary>Gets a value indicating whether this node was deleted</summary>
        public bool IsDeleted => MetadataHandle == default;

        /// <summary>Gets a value indicating whether this node is a temporary</summary>
        public bool IsTemporary => LibLLVMIsTemporary( MetadataHandle );

        /// <summary>Gets a value indicating whether this node is resolved</summary>
        /// <remarks>
        /// <para>If <see cref="IsTemporary"/> is <see langword="true"/>, then this always
        /// returns <see langword="false"/>; if <see cref="IsDistinct"/> is <see langword="true"/>,
        /// this always returns <see langword="true"/>.</para>
        ///
        /// <para>If <see cref="IsUniqued"/> is <see langword="true"/> then this returns <see langword="true"/>
        /// if this node has already dropped RAUW support (because all operands are resolved).</para>
        /// </remarks>
        public bool IsResolved => LibLLVMIsResolved( MetadataHandle );

        /// <summary>Gets a value indicating whether this node is uniqued</summary>
        public bool IsUniqued => LibLLVMIsUniqued( MetadataHandle );

        /// <summary>Gets a value indicating whether this node is distinct</summary>
        public bool IsDistinct => LibLLVMIsDistinct( MetadataHandle );

        /// <summary>Gets the operands for this node, if any</summary>
        public MetadataOperandCollection Operands { get; }

        /// <summary>Replace all uses of this node with a new node</summary>
        /// <param name="other">Node to replace this one with</param>
        public override void ReplaceAllUsesWith( LlvmMetadata other )
        {
            other.ValidateNotNull( nameof( other ) );

            if( !IsTemporary || IsResolved )
            {
                throw new InvalidOperationException( Resources.Cannot_replace_non_temporary_or_resolved_MDNode );
            }

            if( MetadataHandle == default )
            {
                throw new InvalidOperationException( Resources.Cannot_Replace_all_uses_of_a_null_descriptor );
            }

            // grab the context before replacement as replace deletes and invalidates the node
            var context = Context;
            LLVMMetadataReplaceAllUsesWith( MetadataHandle, other.MetadataHandle );

            // remove current node mapping from the context.
            // It won't be valid for use after clearing the handle
            context!.RemoveDeletedNode( this );
            MetadataHandle = default;
        }

        /// <summary>Gets an operand by index as a specific type</summary>
        /// <typeparam name="T">Type of the operand</typeparam>
        /// <param name="index">Index of the operand</param>
        /// <returns>Operand</returns>
        /// <exception cref="InvalidCastException">When the operand is not castable to <typeparamref name="T"/></exception>
        /// <exception cref="ArgumentOutOfRangeException">When the index is out of range for the operands of this node</exception>
        public T? GetOperand<T>( int index )
            where T : LlvmMetadata
        {
            return Operands.GetOperand<T>( index );
        }

        /// <summary>Gets a string operand by index</summary>
        /// <param name="index">Index of the operand</param>
        /// <returns>String value of the operand</returns>
        public string GetOperandString( int index )
            => GetOperand<MDString>( index )?.ToString( ) ?? string.Empty;

        /* TODO: Consider adding these additional properties/methods
        public bool IsTBAAVTableAccess { get; }

        public TempMDNode Clone() {...}

        public void ReplaceOperandWith(unsigned i, LlvmMetadata other) {...}
        public static MDNode Concat(MDNode a, MDNode b) {...}
        public static MDNode Intersect(MDNode a, MDNode b) {...}
        public static MDNode GetMostGenericTBAA(MDNode a, MDNode b) {...}
        public static MDNode GetMostGenericFPMath(MDNode a, MDNode b) {...}
        public static MDNode GetMostGenericRange(MDNode a, MDNode b) {...}
        public static MDNode GetMostGenericAliasScope(MDNode a, MDNode b) {...}
        public static MDNode GetModtGenericAlignmentOrDereferenceable(MDNode a, MDNode b) {...}

        public static T ReplaceWithPermanent<T>() where T:MDNode
        public static T ReplaceWithUniqued<T>() where T:MDNode
        public static T ReplaceWithDistinct<T>() where T:MDNode
        public static DeleteTemporary(MDNode node) {...}
        */

        internal MDNode( LLVMMetadataRef handle )
            : base( handle )
        {
            Operands = new MetadataOperandCollection( this );
        }

        [SuppressMessage( "Reliability", "CA2000:Dispose objects before losing scope", Justification = "Context created here is owned, and disposed of via the ContextCache" )]
        internal static T? FromHandle<T>( LLVMMetadataRef handle )
            where T : MDNode
        {
            return handle == default ? null : FromHandle<T>( GetMeatadataContext( handle ), handle );
        }

        private void ThrowIfDeleted( )
        {
            if( IsDeleted )
            {
                throw new InvalidOperationException( "Cannot operate on a deleted node" );
            }
        }

        private static Context GetMeatadataContext( LLVMMetadataRef metadataHandle )
        {
            var hContext = LibLLVMGetNodeContext( metadataHandle ).ThrowIfInvalid()!;
            return ContextCache.GetContextFor( hContext );
        }
    }
}
