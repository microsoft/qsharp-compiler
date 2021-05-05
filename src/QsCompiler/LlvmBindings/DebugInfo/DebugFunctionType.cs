// -----------------------------------------------------------------------
// <copyright file="DebugFunctionType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Ubiquity.ArgValidators;
using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>This class provides debug information binding for an <see cref="IFunctionType"/>
    /// and a <see cref="DISubroutineType"/>
    /// </summary>
    /// <remarks>
    /// <para>Function signatures are unnamed interned types in LLVM. While there is usually a one
    /// to one mapping between an LLVM function signature type and the source language debug
    /// signature type - that isn't always true. In particular, when passing data by value. In
    /// cases where the address of a by value structure is needed, a common pattern is to use
    /// a pointer to the structure in the signature, then perform an Alloca + memcpy. The
    /// actual approach taken depends on the calling conventions of the target. In these cases
    /// you get an LLVM signature that doesn't match the source and could actually match another
    /// source function where a pointer to the structure is actually used in the source.</para>
    /// <para>For example, the following two C language functions might use the same LLVM signature:
    /// <code>void foo(struct bar)</code>
    /// <code>void foo2(struct bar*)</code>
    /// Implementing both of those might be done in LLVM with a single signature:
    /// <code>void (%struct.bar*)</code></para>
    /// <para>This class is designed to provide mapping between the debug signature type
    /// and the underlying LLVM type</para>
    /// <note type="note">It is important to keep in mind that signatures are only concerned
    /// with types. That is, they do not include names of parameters. Parameter information is
    /// provided by <see cref="DebugInfoBuilder.CreateArgument(DIScope, string, DIFile, uint, DIType, bool, DebugInfoFlags, ushort)"/>
    /// and [DebugInfoBuilder.InsertDeclare](xref:Ubiquity.NET.Llvm.DebugInfo.DebugInfoBuilder.InsertDeclare*)</note>
    /// </remarks>
    /// <seealso href="xref:llvm_langref#disubroutinetype">LLVM DISubroutineType</seealso>
    public class DebugFunctionType
        : DebugType<IFunctionType, DISubroutineType>
        , IFunctionType
    {
        /// <summary>Initializes a new instance of the <see cref="DebugFunctionType"/> class.</summary>
        /// <param name="llvmType">Native LLVM function signature</param>
        /// <param name="module"><see cref="BitcodeModule"/> to use when construction debug information</param>
        /// <param name="debugFlags"><see cref="DebugInfoFlags"/> for this signature</param>
        /// <param name="retType">Return type for the function</param>
        /// <param name="argTypes">Potentially empty set of argument types for the signature</param>
        public DebugFunctionType( IFunctionType llvmType
                                , BitcodeModule module
                                , DebugInfoFlags debugFlags
                                , IDebugType<ITypeRef, DIType> retType
                                , params IDebugType<ITypeRef, DIType>[ ] argTypes
                                )
            : base( llvmType.ValidateNotNull( nameof( llvmType ) )
                  , module.ValidateNotNull( nameof( module ) )
                          .DIBuilder.CreateSubroutineType( debugFlags
                                                         , retType.ValidateNotNull( nameof( retType ) ).DIType
                                                         , argTypes.Select( t => t.DIType )
                                                         )
                  )
        {
        }

        /// <inheritdoc/>
        public bool IsVarArg => NativeType.IsVarArg;

        /// <inheritdoc/>
        public ITypeRef ReturnType => NativeType.ReturnType;

        /// <inheritdoc/>
        public IReadOnlyList<ITypeRef> ParameterTypes => NativeType.ParameterTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugFunctionType"/> class from an LLVM type and <see cref="DISubroutineType"/></summary>
        /// <param name="rawType">Raw native type</param>
        /// <param name="sub">Debug information to bind with the native type</param>
        internal DebugFunctionType( IFunctionType rawType, DISubroutineType sub )
            : base( rawType, sub )
        {
        }
    }
}
