// -----------------------------------------------------------------------
// <copyright file="Switch.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Switch instruction</summary>
    public class Switch
        : Terminator
    {
        /// <summary>Gets the default <see cref="BasicBlock"/> for the switch</summary>
        public BasicBlock Default => BasicBlock.FromHandle( ValueHandle.SwitchDefaultDest )!;

        /// <summary>Adds a new case to the <see cref="Switch"/> instruction</summary>
        /// <param name="onVal">Value for the case to match</param>
        /// <param name="destination">Destination <see cref="BasicBlock"/> if the case matches</param>
        public void AddCase( Value onVal, BasicBlock destination )
        {
            if( onVal == null )
            {
                throw new ArgumentNullException( nameof( onVal ) );
            }

            if( destination == null )
            {
                throw new ArgumentNullException( nameof( destination ) );
            }

            ValueHandle.AddCase( onVal.ValueHandle, destination.BlockHandle );
        }

        internal Switch( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
