// -----------------------------------------------------------------------
// <copyright file="DIObjCProperty.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Objective-C Property</summary>
    public class DIObjCProperty
        : DINode
    {
        /*
        public uint Line {get;}
        public uint Attributes {get;}
        */

        /// <summary>Gets the Debug information for the file containing this property</summary>
        public DIFile File => GetOperand<DIFile>( 1 )!;

        /// <summary>Gets the name of the property</summary>
        public string Name => GetOperandString( 0 );

        /// <summary>Gets the name of the getter method for the property</summary>
        public string GetterName => GetOperandString( 2 );

        /// <summary>Gets the name of the setter method for the property</summary>
        public string SetterName => GetOperandString( 3 );

        /// <summary>Gets the type of the property</summary>
        public DIType Type => GetOperand<DIType>( 4 )!;

        internal DIObjCProperty( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
