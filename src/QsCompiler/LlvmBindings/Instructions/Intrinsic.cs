// -----------------------------------------------------------------------
// <copyright file="Intrinsic.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>base class for calls to LLVM intrinsic functions</summary>
    public class Intrinsic
        : CallInstruction
    {
        /// <summary>Looks up the LLVM intrinsic ID from it's name</summary>
        /// <param name="name">Name of the intrinsic</param>
        /// <returns>Intrinsic ID or 0 if the name does not correspond with an intrinsic function</returns>
        public unsafe static UInt32 LookupId( string name )
        {
            return LLVM.LookupIntrinsicID( name.AsMarshaledString(), (UIntPtr)name.Length );
        }

        internal Intrinsic( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
