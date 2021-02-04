// Copyright (c) Microsoft and Contributors. All rights reserved. Licensed under the University of Illinois/NCSA Open Source License. See LICENSE.txt in the project root for license information.

using System;
using System.Text;

namespace LLVMSharp.Interop
{
    internal static unsafe class StringExtensions
    {
        public static MarshaledString AsMarshaledString( this string self )
        {
            return new MarshaledString( self.AsSpan( ) );
        }
    }
}
