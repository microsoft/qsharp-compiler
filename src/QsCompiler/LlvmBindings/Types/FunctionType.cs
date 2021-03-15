// -----------------------------------------------------------------------
// <copyright file="FunctionType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

// Interface+internal type matches file name
#pragma warning disable SA1649

namespace Ubiquity.NET.Llvm.Types
{
    /// <summary>Interface to represent the LLVM type of a function (e.g. a signature)</summary>
    public interface IFunctionType
        : ITypeRef
    {
        /// <summary>Gets a value indicating whether this signature is for a variadic function</summary>
        bool IsVarArg { get; }

        /// <summary>Gets the return type of the function</summary>
        ITypeRef ReturnType { get; }

        /// <summary>Gets the types of the parameters for the function</summary>
        IReadOnlyList<ITypeRef> ParameterTypes { get; }
    }

    /// <summary>Class to represent the LLVM type of a function (e.g. a signature)</summary>
    internal class FunctionType
        : TypeRef
        , IFunctionType
    {
        /// <inheritdoc/>
        public bool IsVarArg => TypeRefHandle.IsFunctionVarArg;

        /// <inheritdoc/>
        public ITypeRef ReturnType => FromHandle<ITypeRef>( TypeRefHandle.ReturnType );

        /// <inheritdoc/>
        public IReadOnlyList<ITypeRef> ParameterTypes
        {
            get
            {
                uint paramCount = TypeRefHandle.ParamTypesCount;
                if( paramCount == 0 )
                {
                    return new List<TypeRef>( ).AsReadOnly( );
                }

                var paramTypes = TypeRefHandle.ParamTypes;
                return ( from p in paramTypes
                         select FromHandle<TypeRef>( p )
                       ).ToList( )
                        .AsReadOnly( );
            }
        }

        internal FunctionType( LLVMTypeRef typeRef )
            : base( typeRef )
        {
        }
    }
}
