// -----------------------------------------------------------------------
// <copyright file="DebugDeclare.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Intrinsic LLVM IR instruction to declare Debug information for a <see cref="Value"/></summary>
    /// <seealso href="xref:llvm_sourcelevel_debugging#llvm-dbg-declare">llvm.dbg.declare</seealso>
    /// <seealso href="xref:llvm_sourcelevel_debugging">LLVM Source Level Debugging</seealso>
    public class DebugDeclare
        : DebugInfoIntrinsic
    {
        internal DebugDeclare( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
