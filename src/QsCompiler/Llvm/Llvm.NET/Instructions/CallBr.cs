// -----------------------------------------------------------------------
// <copyright file="CallBr.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Interop;
using Ubiquity.NET.Llvm.Properties;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>CallBr instruction</summary>
    /// <seealso href="xref:llvm_langref#i-callbr"/>
    public class CallBr
        : Instruction
        , IAttributeAccessor
    {
        /// <summary>Gets the target function of the call</summary>
        public unsafe IrFunction TargetFunction
            => FromHandle<IrFunction>( LLVM.GetCalledValue( ValueHandle ) )!;

        /// <summary>Gets the attributes for this call site</summary>
        public IAttributeDictionary Attributes { get; }

        /// <inheritdoc/>
        public unsafe void AddAttributeAtIndex( FunctionAttributeIndex index, AttributeValue attrib )
        {
            attrib.VerifyValidOn( index, this );
            LLVM.AddCallSiteAttribute( ValueHandle, ( uint )index, attrib.NativeAttribute );
        }

        /// <inheritdoc/>
        public uint GetAttributeCountAtIndex( FunctionAttributeIndex index )
        {
            return ValueHandle.GetCallSiteAttributeCount( ( LLVMAttributeIndex )index );
        }

        /// <inheritdoc/>
        public IEnumerable<AttributeValue> GetAttributesAtIndex( FunctionAttributeIndex index )
        {
            var buffer = ValueHandle.GetCallSiteAttributes( ( LLVMAttributeIndex )index );
            return from attribRef in buffer
                   select AttributeValue.FromHandle( Context, attribRef );
        }

        /// <inheritdoc/>
        public unsafe AttributeValue GetAttributeAtIndex( FunctionAttributeIndex index, AttributeKind kind )
        {
            var handle = LLVM.GetCallSiteEnumAttribute( ValueHandle, ( uint )index, kind.GetEnumAttributeId( ) );
            return AttributeValue.FromHandle( Context, handle );
        }

        /// <inheritdoc/>
        public unsafe AttributeValue GetAttributeAtIndex( FunctionAttributeIndex index, string name )
        {
            if( string.IsNullOrWhiteSpace( name ) )
            {
                throw new ArgumentException( Resources.Name_cannot_be_null_or_empty, nameof( name ) );
            }
            var handle = LLVM.GetCallSiteStringAttribute( ValueHandle, ( uint )index, name.AsMarshaledString(), ( uint )name.Length );
            return AttributeValue.FromHandle( Context, handle );
        }

        /// <inheritdoc/>
        public unsafe void RemoveAttributeAtIndex( FunctionAttributeIndex index, AttributeKind kind )
        {
            LLVM.RemoveCallSiteEnumAttribute( ValueHandle, ( uint )index, kind.GetEnumAttributeId( ) );
        }

        /// <inheritdoc/>
        public unsafe void RemoveAttributeAtIndex( FunctionAttributeIndex index, string name )
        {
            if ( String.IsNullOrWhiteSpace( name ) ) 
            {
                throw new ArgumentNullException( nameof( name ) );
            }
            LLVM.RemoveCallSiteStringAttribute( ValueHandle, ( uint )index, name.AsMarshaledString(), ( uint )name.Length );
        }

        internal CallBr( LLVMValueRef valueRef )
            : base( valueRef )
        {
            Attributes = new ValueAttributeDictionary( this, ( ) => TargetFunction );
        }
    }
}
