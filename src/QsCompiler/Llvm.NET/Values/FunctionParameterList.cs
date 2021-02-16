// -----------------------------------------------------------------------
// <copyright file="FunctionParameterList.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Support class to provide read only list semantics to the parameters of a method</summary>
    internal class FunctionParameterList
        : IReadOnlyList<Argument>
    {
        public Argument this[ int index ]
        {
            get
            {
                if( index >= Count || index < 0 )
                {
                    throw new ArgumentOutOfRangeException( nameof( index ) );
                }

                LLVMValueRef valueRef = OwningFunction.ValueHandle.GetParam( ( uint )index );
                return Value.FromHandle<Argument>( valueRef )!;
            }
        }

        public int Count
        {
            get
            {
                uint count = OwningFunction.ValueHandle.ParamsCount;
                return ( int )Math.Min( count, int.MaxValue );
            }
        }

        public IEnumerator<Argument> GetEnumerator( )
        {
            for( uint i = 0; i < Count; ++i )
            {
                LLVMValueRef val = OwningFunction.ValueHandle.GetParam( i );
                yield return Value.FromHandle<Argument>( val )!;
            }
        }

        IEnumerator IEnumerable.GetEnumerator( ) => GetEnumerator( );

        internal FunctionParameterList( IrFunction owningFunction )
        {
            OwningFunction = owningFunction;
        }

        private readonly IrFunction OwningFunction;
    }
}
