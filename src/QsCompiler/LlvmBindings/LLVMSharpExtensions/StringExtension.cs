// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;

namespace LLVMSharp.Interop
{
    internal static unsafe class StringExtensions
    {
        public static MarshaledString AsMarshaledString(this string self)
        {
            return new MarshaledString(self.AsSpan());
        }
    }
}
