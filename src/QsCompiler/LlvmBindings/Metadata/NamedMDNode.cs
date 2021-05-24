// -----------------------------------------------------------------------
// <copyright file="NamedMDNode.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Wraps an LLVM NamedMDNode</summary>
    /// <remarks>Despite its name a NamedMDNode is not itself an MDNode. It is owned directly by a
    /// a <see cref="BitcodeModule"/> and contains a list of <see cref="MDNode"/> operands.
    /// </remarks>
    /// <seealso cref="BitcodeModule.AddNamedMetadataOperand(string, Values.Value)"/>
    /// <seealso cref="BitcodeModule.GetNamedMetadataNumOperands(string)"/>
    /// <seealso cref="BitcodeModule.GetNamedMetadataOperands(string)"/>
    public class NamedMDNode
    {
        /// <summary>Gets the name of the node</summary>
        public string Name => this.nativeHandle.Name();

        internal NamedMDNode(LLVMNamedMDNodeRef nativeNode)
        {
            this.nativeHandle = nativeNode;
        }

        private readonly LLVMNamedMDNodeRef nativeHandle;
    }
}
