// -----------------------------------------------------------------------
// <copyright file="DILocation.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug source location information</summary>
    public class DILocation
        : MDNode
    {
        /// <summary>Initializes a new instance of the <see cref="DILocation"/> class.</summary>
        /// <param name="context">Context that owns this location</param>
        /// <param name="line">line number for the location</param>
        /// <param name="column">Column number for the location</param>
        /// <param name="scope">Containing scope for the location</param>
        public DILocation(Context context, uint line, uint column, DILocalScope scope)
            : this(context, line, column, scope, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DILocation"/> class.</summary>
        /// <param name="context">Context that owns this location</param>
        /// <param name="line">line number for the location</param>
        /// <param name="column">Column number for the location</param>
        /// <param name="scope">Containing scope for the location</param>
        /// <param name="inlinedAt">Scope where this scope is inlined at/into</param>
        public DILocation(Context context, uint line, uint column, DILocalScope scope, DILocation? inlinedAt)
            : base(context.ContextHandle.CreateDebugLocation(
                line,
                column,
                scope.MetadataHandle,
                inlinedAt?.MetadataHandle ?? default))
        {
        }

        /// <summary>Gets the scope for this location</summary>
        public DILocalScope Scope => FromHandle<DILocalScope>(this.Context, this.MetadataHandle.DILocationGetScope())!;

        /// <summary>Gets the line for this location</summary>
        public uint Line => this.MetadataHandle.DILocationGetLine();

        /// <summary>Gets the column for this location</summary>
        public uint Column => this.MetadataHandle.DILocationGetColumn();

        /// <summary>Gets the location this location is inlined at</summary>
        public DILocation? InlinedAt => FromHandle<DILocation>(this.MetadataHandle.DILocationGetInlinedAt());

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Scope.File}({this.Line},{this.Column})";
        }

        internal DILocation(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
