// -----------------------------------------------------------------------
// <copyright file="DICompositeTypeArray.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Array of <see cref="DICompositeType"/> debug information nodes for use with <see cref="DebugInfoBuilder"/> methods</summary>
    [SuppressMessage( "Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "This matches the wrapped native type" )]
    public class DICompositeTypeArray
        : TupleTypedArrayWrapper<DINode>
    {
        internal DICompositeTypeArray( MDTuple? tuple )
            : base( tuple )
        {
        }
    }
}
