// <copyright file="StringExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

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
