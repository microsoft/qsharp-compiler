// -----------------------------------------------------------------------
// <copyright file="Invoke.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to invoke (call) a function with exception handling</summary>
    /// <seealso href="xref:llvm_langref#i-invoke">LLVM invoke Instruction</seealso>
    public unsafe class Invoke
        : Terminator
        , IAttributeAccessor
    {
        /// <summary>Gets the target function of the invocation</summary>
        public IrFunction TargetFunction => FromHandle<IrFunction>( LLVM.GetCalledValue( ValueHandle ) )!;

        /// <summary>Gets or sets the normal destination for the invoke</summary>
        public BasicBlock NormalDestination
        {
            get => BasicBlock.FromHandle( LLVM.GetNormalDest( ValueHandle ) )!;
            set => LLVM.SetNormalDest( ValueHandle, value.BlockHandle );
        }

        /// <summary>Gets the attributes for this call site</summary>
        public IAttributeDictionary Attributes { get; }

        /// <inheritdoc/>
        public void AddAttributeAtIndex( FunctionAttributeIndex index, AttributeValue attrib )
        {
            attrib.VerifyValidOn( index, this );

            LLVM.AddCallSiteAttribute( ValueHandle, ( uint )index, attrib.NativeAttribute );
        }

        /// <inheritdoc/>
        public uint GetAttributeCountAtIndex( FunctionAttributeIndex index )
        {
            return LLVM.GetCallSiteAttributeCount( ValueHandle, ( uint )index );
        }

        /// <inheritdoc/>
        public IEnumerable<AttributeValue> GetAttributesAtIndex( FunctionAttributeIndex index )
        {
            uint count = GetAttributeCountAtIndex( index );
            if( count == 0 )
            {
                return Enumerable.Empty<AttributeValue>( );
            }

            var buffer = ValueHandle.GetCallSiteAttributes( ( LLVMAttributeIndex )index );
            return from attribRef in buffer
                   select AttributeValue.FromHandle( Context, attribRef );
        }

        /// <inheritdoc/>
        public AttributeValue GetAttributeAtIndex( FunctionAttributeIndex index, AttributeKind kind )
        {
            var handle = LLVM.GetCallSiteEnumAttribute( ValueHandle, ( uint )index, kind.GetEnumAttributeId( ) );
            return AttributeValue.FromHandle( Context, handle );
        }

        /// <inheritdoc/>
        public AttributeValue GetAttributeAtIndex( FunctionAttributeIndex index, string name )
        {
            var handle = LLVM.GetCallSiteStringAttribute( ValueHandle, ( uint )index, name.AsMarshaledString(), ( uint )name.Length );
            return AttributeValue.FromHandle( Context, handle );
        }

        /// <inheritdoc/>
        public void RemoveAttributeAtIndex( FunctionAttributeIndex index, AttributeKind kind )
        {
            LLVM.RemoveCallSiteEnumAttribute( ValueHandle, ( uint )index, kind.GetEnumAttributeId( ) );
        }

        /// <inheritdoc/>
        public void RemoveAttributeAtIndex( FunctionAttributeIndex index, string name )
        {
            LLVM.RemoveCallSiteStringAttribute( ValueHandle, ( uint )index, name.AsMarshaledString(), ( uint )name.Length );
        }

        internal Invoke( LLVMValueRef valueRef )
            : base( valueRef )
        {
            Attributes = new ValueAttributeDictionary( this, ( ) => TargetFunction );
        }
    }
}
