// -----------------------------------------------------------------------
// <copyright file="DebugType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

using Ubiquity.ArgValidators;
using Ubiquity.NET.Llvm.Interop;
using Ubiquity.NET.Llvm.Properties;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

#pragma warning disable SA1649 // File name must match first type ( Justification -  Interface + internal Impl + public extensions )

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Provides pairing of a <see cref="ITypeRef"/> with a <see cref="DIType"/> for function signatures</summary>
    /// <typeparam name="TNative">Native LLVM type</typeparam>
    /// <typeparam name="TDebug">Debug type description for the type</typeparam>
    /// <remarks>
    /// <para>Primitive types and function signature types are all interned in LLVM, thus there won't be a
    /// strict one to one relationship between an LLVM type and corresponding language specific debug
    /// type. (e.g. unsigned char, char, byte and signed byte might all be 8 bit integer values as far
    /// as LLVM is concerned.) Also, when using the pointer+alloca+memcpy pattern to pass by value the
    /// actual source debug info type is different than the LLVM function signature. This interface and
    /// it's implementations are used to construct native type and debug info pairing to allow applications
    /// to maintain a link from their AST or IR types into the LLVM native type and debug information.
    /// </para>
    /// <note type="note">
    /// It is important to note that the relationship from the <see cref="DIType"/> to it's <see cref="NativeType"/>
    /// properties is strictly ***one way***. That is, there is no way to take an arbitrary <see cref="ITypeRef"/>
    /// and re-associate it with the DIType or an implementation of this interface as there may be many such
    /// mappings to choose from.
    /// </note>
    /// </remarks>
    public interface IDebugType<out TNative, out TDebug>
        : ITypeRef
        where TNative : ITypeRef
        where TDebug : DIType
    {
        /// <summary>Gets the LLVM NativeType this interface is associating with debug info in <see cref="DIType"/></summary>
        TNative NativeType { get; }

        /// <summary>Gets the debug information type this interface is associating with <see cref="NativeType"/></summary>
        TDebug? DIType { get; }

        /// <summary>Creates a pointer to this type for a given module and address space</summary>
        /// <param name="bitcodeModule">Module the debug type information belongs to</param>
        /// <param name="addressSpace">Address space for the pointer</param>
        /// <returns><see cref="DebugPointerType"/></returns>
        DebugPointerType CreatePointerType( BitcodeModule bitcodeModule, uint addressSpace );

        /// <summary>Creates a type defining an array of elements of this type</summary>
        /// <param name="bitcodeModule">Module the debug information belongs to</param>
        /// <param name="lowerBound">Lower bound of the array</param>
        /// <param name="count">Count of elements in the array</param>
        /// <returns><see cref="DebugArrayType"/></returns>
        DebugArrayType CreateArrayType( BitcodeModule bitcodeModule, uint lowerBound, uint count );
    }

    /// <summary>Base class for Debug types bound with an LLVM type</summary>
    /// <typeparam name="TNative">Native LLVM type</typeparam>
    /// <typeparam name="TDebug">Debug type</typeparam>
    [SuppressMessage( "StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single class", Justification = "Interface, Generic type and static extension methods form a common API surface" )]
    public class DebugType<TNative, TDebug>
        : IDebugType<TNative, TDebug>
        , ITypeHandleOwner
        where TNative : class, ITypeRef
        where TDebug : DIType
    {
        /// <summary>Gets or sets the Debug information type for this binding</summary>
        /// <remarks>
        /// <para>Setting the debug type is only allowed when the debug type is null or <see cref="MDNode.IsTemporary"/>
        /// is <see langword="true"/>. If the debug type node is a temporary setting the type will replace all uses
        /// of the temporary type automatically, via <see cref="MDNode.ReplaceAllUsesWith(LlvmMetadata)"/></para>
        /// <para>Since setting this property will replace all uses with (RAUW) the new value then setting this property
        /// with <see langword="null"/> is not allowed. However, until set this property will be <see  langword="null"/></para>
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">The type is not <see langword="null"/> or not a temporary</exception>
        [SuppressMessage( "Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "DIType", Justification = "It is spelled correctly 8^)" )]
        public TDebug? DIType
        {
            get => RawDebugInfoType;
            set
            {
                TDebug v = value.ValidateNotNull( nameof( value ) )!;
                if( ( RawDebugInfoType != null ) && RawDebugInfoType.IsTemporary )
                {
                    if( v.IsTemporary )
                    {
                        throw new InvalidOperationException( Resources.Cannot_replace_a_temporary_with_another_temporary );
                    }

                    RawDebugInfoType.ReplaceAllUsesWith( v );
                    RawDebugInfoType = v;
                }
                else
                {
                    throw new InvalidOperationException( Resources.Cannot_replace_non_temporary_DIType_with_a_new_Type );
                }
            }
        }

        /// <summary>Gets or sets the native LLVM type for this debug type binding</summary>
        /// <remarks>
        /// Once the native type is set, it cannot be reset. Attempts to change the native
        /// type when it isn't <see langword="null"/> will result in an exception.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">The native type was already set</exception>
        public TNative NativeType
        {
            get => NativeType_.ValueOrDefault;

            protected set => NativeType_.Value = value;
        }

        /// <summary>Gets an intentionally undocumented value</summary>
        /// <remarks>internal use only</remarks>
        LLVMTypeRef ITypeHandleOwner.TypeHandle => NativeType.GetTypeRef( );

        /// <inheritdoc/>
        public bool IsSized => NativeType.IsSized;

        /// <inheritdoc/>
        public TypeKind Kind => NativeType.Kind;

        /// <inheritdoc/>
        public Context Context => NativeType.Context;

        /// <inheritdoc/>
        public uint IntegerBitWidth => NativeType.IntegerBitWidth;

        /// <inheritdoc/>
        public bool IsInteger => NativeType.IsInteger;

        /// <inheritdoc/>
        public bool IsFloat => NativeType.IsFloat;

        /// <inheritdoc/>
        public bool IsDouble => NativeType.IsDouble;

        /// <inheritdoc/>
        public bool IsVoid => NativeType.IsVoid;

        /// <inheritdoc/>
        public bool IsStruct => NativeType.IsStruct;

        /// <inheritdoc/>
        public bool IsPointer => NativeType.IsPointer;

        /// <inheritdoc/>
        public bool IsSequence => NativeType.IsSequence;

        /// <inheritdoc/>
        public bool IsFloatingPoint => NativeType.IsFloatingPoint;

        /// <inheritdoc/>
        public bool IsPointerPointer => NativeType.IsPointerPointer;

        /// <inheritdoc/>
        public Constant GetNullValue( ) => NativeType.GetNullValue( );

        /// <inheritdoc/>
        public IArrayType CreateArrayType( uint count ) => NativeType.CreateArrayType( count );

        /// <inheritdoc/>
        public IPointerType CreatePointerType( ) => NativeType.CreatePointerType( );

        /// <inheritdoc/>
        public IPointerType CreatePointerType( uint addressSpace ) => NativeType.CreatePointerType( addressSpace );

        /// <inheritdoc/>
        public DebugPointerType CreatePointerType( BitcodeModule bitcodeModule, uint addressSpace )
        {
            if( DIType == null )
            {
                throw new InvalidOperationException( Resources.Type_does_not_have_associated_Debug_type_from_which_to_construct_a_pointer_type );
            }

            var nativePointer = NativeType.CreatePointerType( addressSpace );
            return new DebugPointerType( nativePointer, bitcodeModule, DIType, string.Empty );
        }

        /// <inheritdoc/>
        public DebugArrayType CreateArrayType( BitcodeModule bitcodeModule, uint lowerBound, uint count )
        {
            if( DIType == null )
            {
                throw new InvalidOperationException( Resources.Type_does_not_have_associated_Debug_type_from_which_to_construct_an_array_type );
            }

            var llvmArray = NativeType.CreateArrayType( count );
            return new DebugArrayType( llvmArray, bitcodeModule, DIType, count, lowerBound );
        }

        /// <inheritdoc/>
        public bool TryGetExtendedPropertyValue<TProperty>( string id, [MaybeNullWhen(false)] out TProperty value )
        {
            return PropertyContainer.TryGetExtendedPropertyValue( id, out value )
                || NativeType.TryGetExtendedPropertyValue( id, out value );
        }

        /// <inheritdoc/>
        public void AddExtendedPropertyValue( string id, object? value )
        {
            PropertyContainer.AddExtendedPropertyValue( id, value );
        }

        /// <summary>Converts a <see cref="DebugType{TNative, TDebug}"/> to <typeparamref name="TDebug"/> by accessing the <see cref="DIType"/> property</summary>
        /// <param name="self">The type to convert</param>
        [SuppressMessage( "Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "DIType is available as a property, this is for convenience" )]
        public static implicit operator TDebug?( DebugType<TNative, TDebug> self ) => self.ValidateNotNull( nameof( self ) ).DIType;

        internal DebugType( TNative llvmType, TDebug? debugInfoType )
        {
            llvmType.ValidateNotNull( nameof( llvmType ) );

            NativeType_.Value = llvmType;
            RawDebugInfoType = debugInfoType;
        }

        private TDebug? RawDebugInfoType;

        // This can't be an auto property as the setter needs Enforce Set Once semantics
        [SuppressMessage( "StyleCop.CSharp.NamingRules"
                        , "SA1310:Field names must not contain underscore"
                        , Justification = "Trailing _ indicates value MUST NOT be written to directly, even internally"
                        )
        ]
        private readonly WriteOnce<TNative> NativeType_ = new WriteOnce<TNative>();

        private readonly ExtensiblePropertyContainer PropertyContainer = new ExtensiblePropertyContainer( );
    }

    /// <summary>Utility class to provide mix-in type extensions and support for Debug Types</summary>
    public static class DebugType
    {
        /// <summary>Creates a new <see cref="DebugType"/>instance inferring the generic arguments from the parameters</summary>
        /// <typeparam name="TNative">Type of the Native LLVM type for the association</typeparam>
        /// <typeparam name="TDebug">Type of the debug information type for the association</typeparam>
        /// <param name="nativeType"><typeparamref name="TNative"/> type instance for this association</param>
        /// <param name="debugType"><typeparamref name="TDebug"/> type instance for this association (use <see langword="null"/> for void)</param>
        /// <returns><see cref="IDebugType{NativeT, DebugT}"/> implementation for the specified association</returns>
        public static IDebugType<TNative, TDebug> Create<TNative, TDebug>( TNative nativeType
                                                                         , TDebug? debugType
                                                                         )
            where TNative : class, ITypeRef
            where TDebug : DIType
        {
            return new DebugType<TNative, TDebug>( nativeType, debugType );
        }

        /// <summary>Convenience extensions for determining if the <see cref="DIType"/> property is valid</summary>
        /// <param name="debugType">Debug type to test for valid Debug information</param>
        /// <remarks>In LLVM Debug information a <see langword="null"/> <see cref="Ubiquity.NET.Llvm.DebugInfo.DIType"/> is
        /// used to represent the void type. Thus, looking only at the <see cref="DIType"/> property is
        /// insufficient to distinguish between a type with no debug information and one representing the void
        /// type. This property is used to disambiguate the two possibilities.
        /// </remarks>
        /// <returns><see langword="true"/> if the type has debug information</returns>
        public static bool HasDebugInfo( this IDebugType<ITypeRef, DIType> debugType )
        {
            debugType.ValidateNotNull( nameof( debugType ) );

            return debugType.DIType != null || debugType.NativeType.IsVoid;
        }
    }
}
