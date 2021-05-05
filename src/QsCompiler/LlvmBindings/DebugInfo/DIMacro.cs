// -----------------------------------------------------------------------
// <copyright file="DIMacro.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Pre-Processor macro</summary>
    public class DIMacro
        : DIMacroNode
    {
        /* TODO: non-operand property
        public uint Line { get; }
        */

        /// <summary>Gets the name of the macro</summary>
        public string Name => GetOperandString( 0 );

        /// <summary>Gets the value of the property</summary>
        public string Value => GetOperandString( 1 );

        internal DIMacro( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
