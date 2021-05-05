// -----------------------------------------------------------------------
// <copyright file="DITypeArray.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Array of <see cref="DIType"/> nodes for use with see <see cref="DebugInfoBuilder"/> methods</summary>
    [SuppressMessage( "Naming", "CA1710:Identifiers should have correct suffix", Justification = "Name matches underlying LLVM and is descriptive of what it is" )]
    public class DITypeArray
        : TupleTypedArrayWrapper<DIType>
    {
        internal DITypeArray( MDTuple? tuple )
            : base( tuple )
        {
        }
    }
}
