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
        /// <param name="position">1-based line/col number for the location</param>
        /// <param name="scope">Containing scope for the location</param>
        // TODO: before this goes to main, I should probably change the DebugPosition back to the line, column
        // format since this is a public API. I can move DebugPosition information to within
        // the QIR Generation only.
        public DILocation(Context context, DebugPosition position, DILocalScope scope)
            : this(context, position, scope, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DILocation"/> class.</summary>
        /// <param name="context">Context that owns this location</param>
        /// <param name="position">1-based line/col number for the location</param>
        /// <param name="scope">Containing scope for the location</param>
        /// <param name="inlinedAt">Scope where this scope is inlined at/into</param>
        public DILocation(Context context, DebugPosition position, DILocalScope scope, DILocation? inlinedAt)
            : base(context.ContextHandle.CreateDebugLocation(
                position.Line,
                position.Column,
                scope.MetadataHandle,
                inlinedAt?.MetadataHandle ?? default))
        {
        }

        /// <summary>Gets the scope for this location</summary>
        public DILocalScope Scope => FromHandle<DILocalScope>(this.Context, this.MetadataHandle.DILocationGetScope())!;

        /// <summary>Gets the 1-based line/col position for this location</summary>
        public DebugPosition Position => DebugPosition.FromOneBased(this.MetadataHandle.DILocationGetLine(), this.MetadataHandle.DILocationGetColumn());

        /// <summary>Gets the location this location is inlined at</summary>
        public DILocation? InlinedAt => FromHandle<DILocation>(this.MetadataHandle.DILocationGetInlinedAt());

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Scope.File}({this.Position.Line},{this.Position.Column})";
        }

        internal DILocation(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
