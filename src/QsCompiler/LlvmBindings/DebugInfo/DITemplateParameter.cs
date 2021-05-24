// -----------------------------------------------------------------------
// <copyright file="DITemplateParameter.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Base class for template parameter information</summary>
    /// <seealso cref="DITemplateTypeParameter"/>
    /// <seealso cref="DITemplateValueParameter"/>
    public class DITemplateParameter
        : DINode
    {
        /// <summary>Gets the name of the template parameter</summary>
        public string Name => this.GetOperandString(0);

        /// <summary>Gets the type of the template parameter</summary>
        public DIType? Type => this.GetOperand<DIType>(1);

        internal DITemplateParameter(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
