// -----------------------------------------------------------------------
// <copyright file="DIEnumerator.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug Information for a name value par of an enumerated type</summary>
    /// <seealso href="xref:llvm_langref#dienumerator">LLVM DIEnumerator</seealso>
    public class DIEnumerator
        : DINode
    {
        /*
        public Int64 Value {get;}
        */

        /// <summary>Gets the Name of the enumerated value</summary>
        public string Name => GetOperandString( 0 );

        /// <summary>Initializes a new instance of the <see cref="DIEnumerator"/> class from an LLVM handle</summary>
        /// <param name="handle">Native LLVM reference for an enumerator</param>
        internal DIEnumerator( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
