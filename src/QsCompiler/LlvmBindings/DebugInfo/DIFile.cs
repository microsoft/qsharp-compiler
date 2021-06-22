// -----------------------------------------------------------------------
// <copyright file="DIFile.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a source file</summary>
    /// <seealso href="xref:llvm_langref#difile">LLVM DIFile</seealso>
    public class DIFile
        : DIScope
    {
        /// <summary>Gets the file name for this file</summary>
        public string FileName => this.MetadataHandle == default ? string.Empty : this.MetadataHandle.DIFileGetFilename() ?? string.Empty;

        /// <summary>Gets the Directory for this file</summary>
        public string Directory => this.MetadataHandle == default ? string.Empty : this.MetadataHandle.DIFileGetDirectory() ?? string.Empty;

        /// <summary>Gets the source of the file or an empty string if not available</summary>
        public string Source => this.MetadataHandle == default ? string.Empty : this.MetadataHandle.DIFileGetSource() ?? string.Empty;

        /// <summary>Gets the Checksum for this file</summary>
        public string CheckSum => this.MetadataHandle == default ? string.Empty : this.GetOperandString(2);

        /// <summary>Gets the full path for this file</summary>
        public string Path => this.MetadataHandle == default ? string.Empty : System.IO.Path.Combine(this.Directory, this.FileName);

        internal DIFile(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
