// -----------------------------------------------------------------------
// <copyright file="DebugUnionType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Ubiquity.ArgValidators;
using Ubiquity.NET.Llvm.Properties;
using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug representation of a union type</summary>
    /// <remarks>The underlying native LLVM type is a structure with a single member</remarks>
    /// <seealso href="xref:llvm_langref#dicompositetype">LLVM DICompositeType</seealso>
    public class DebugUnionType
        : DebugType<INamedStructuralType, DICompositeType>
        , INamedStructuralType
    {
        /// <summary>Initializes a new instance of the <see cref="DebugUnionType"/> class.</summary>
        /// <param name="llvmType">Underlying native type this debug type describes</param>
        /// <param name="module">Module to contain the debug metadata for this type</param>
        /// <param name="scope">Scope containing this type</param>
        /// <param name="name">Debug/source name of the type</param>
        /// <param name="file">Source file containing this type</param>
        /// <param name="line">Line number for this type</param>
        /// <param name="debugFlags">Debug flags for this type</param>
        /// <param name="elements">Descriptors for the members of the type</param>
        public DebugUnionType( IStructType llvmType
                             , BitcodeModule module
                             , DIScope? scope
                             , string name
                             , DIFile? file
                             , uint line
                             , DebugInfoFlags debugFlags
                             , IEnumerable<DebugMemberInfo> elements
                             )
            : base( llvmType.ValidateNotNull( nameof( llvmType ) ),
                    module.ValidateNotNull( nameof( module ) )
                          .DIBuilder
                          .CreateReplaceableCompositeType( Tag.UnionType
                                                         , name
                                                         , scope
                                                         , file
                                                         , line
                                                         )
                  )
        {
            if( !llvmType.IsOpaque )
            {
                throw new ArgumentException( Resources.Struct_type_used_as_basis_for_a_union_must_not_have_a_body, nameof( llvmType ) );
            }

            SetBody( module, scope, file, line, debugFlags, elements );
        }

        /// <summary>Initializes a new instance of the <see cref="DebugUnionType"/> class.</summary>
        /// <param name="module">Module to contain the debug metadata for this type</param>
        /// <param name="nativeName">Native LLVM type name</param>
        /// <param name="scope">Scope containing this type</param>
        /// <param name="name">Debug/source name of the type</param>
        /// <param name="file">Source file containing this type</param>
        /// <param name="line">Line number for this type</param>
        public DebugUnionType( BitcodeModule module
                             , string nativeName
                             , DIScope? scope
                             , string name
                             , DIFile? file
                             , uint line = 0
                             )
            : base( module.ValidateNotNull( nameof( module ) ).Context.CreateStructType( nativeName ),
                    module.DIBuilder
                          .CreateReplaceableCompositeType( Tag.UnionType
                                                         , name
                                                         , scope
                                                         , file
                                                         , line
                                                         )
                  )
        {
        }

        /// <inheritdoc/>
        public bool IsOpaque => NativeType.IsOpaque;

        /// <inheritdoc/>
        public IReadOnlyList<ITypeRef> Members => NativeType.Members;

        /// <inheritdoc/>
        public string Name => NativeType.Name;

        /// <summary>Gets the description of each member of the type</summary>
        public IReadOnlyList<DebugMemberInfo> DebugMembers { get; private set; } = new List<DebugMemberInfo>( ).AsReadOnly( );

        /// <summary>Sets the body of the union type</summary>
        /// <param name="module">Module to contain the debug metadata</param>
        /// <param name="scope">Scope containing this type</param>
        /// <param name="file">File for the type</param>
        /// <param name="line">line number for the type</param>
        /// <param name="debugFlags">Flags for the type</param>
        /// <param name="debugElements">Descriptors for each element in the type</param>
        public void SetBody( BitcodeModule module
                           , DIScope? scope
                           , DIFile? file
                           , uint line
                           , DebugInfoFlags debugFlags
                           , IEnumerable<DebugMemberInfo> debugElements
                           )
        {
            module.ValidateNotNull( nameof( module ) );
            debugElements.ValidateNotNull( nameof( debugElements ) );

            if( module.Layout == null )
            {
                throw new ArgumentException( Resources.Module_needs_Layout_to_build_basic_types, nameof( module ) );
            }

            // Native body is a single element of a type with the largest size
            ulong maxSize = 0UL;
            ITypeRef[ ] nativeMembers = { null! };
            foreach( var elem in debugElements )
            {
                ulong? bitSize = elem.ExplicitLayout?.BitSize ?? module.Layout?.BitSizeOf( elem.DebugType );
                if( !bitSize.HasValue )
                {
                    throw new ArgumentException( Resources.Cannot_determine_layout_for_element__The_element_must_have_an_explicit_layout_or_the_module_has_a_layout_to_use, nameof( debugElements ) );
                }

                if( maxSize >= bitSize.Value )
                {
                    continue;
                }

                maxSize = bitSize.Value;
                nativeMembers[ 0 ] = elem.DebugType;
            }

            var nativeType = ( IStructType )NativeType;
            nativeType.SetBody( false, nativeMembers );

            // Debug info contains details of each member of the union
            DebugMembers = new ReadOnlyCollection<DebugMemberInfo>( debugElements as IList<DebugMemberInfo> ?? debugElements.ToList( ) );
            var memberTypes = ( from memberInfo in DebugMembers
                                select CreateMemberType( module, memberInfo )
                              ).ToList<DIDerivedType>();

            var (unionBitSize, unionAlign)
                = memberTypes.Aggregate( (MaxSize: 0ul, MaxAlign: 0ul)
                                       , ( a, d ) => (Math.Max( a.MaxSize, d.BitSize ), Math.Max( a.MaxAlign, d.BitAlignment ))
                                       );
            var concreteType = module.DIBuilder.CreateUnionType( scope: scope
                                                               , name: DIType!.Name // not null via construction
                                                               , file: file
                                                               , line: line
                                                               , bitSize: checked((uint)unionBitSize)
                                                               , bitAlign: checked((uint)unionAlign)
                                                               , debugFlags: debugFlags
                                                               , elements: memberTypes
                                                               );
            DIType = concreteType;
        }

        private DIDerivedType CreateMemberType( BitcodeModule module, DebugMemberInfo memberInfo )
        {
            ulong bitSize;
            if( !( memberInfo.ExplicitLayout is null ) )
            {
                bitSize = memberInfo.ExplicitLayout.BitSize;
            }
            else if( !( module.Layout is null ) )
            {
                bitSize = module.Layout.BitSizeOf( memberInfo.DebugType );
            }
            else
            {
                throw new ArgumentException( "Cannot determine size of member", nameof( memberInfo ) );
            }

            return module.DIBuilder.CreateMemberType( scope: DIType
                                                    , name: memberInfo.Name
                                                    , file: memberInfo.File
                                                    , line: memberInfo.Line
                                                    , bitSize: bitSize
                                                    , bitAlign: memberInfo.ExplicitLayout?.BitAlignment ?? 0
                                                    , bitOffset: 0
                                                    , debugFlags: memberInfo.DebugInfoFlags
                                                    , type: memberInfo.DebugType.DIType
                                                    );
        }
    }
}
