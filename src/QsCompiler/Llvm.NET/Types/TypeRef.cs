// -----------------------------------------------------------------------
// <copyright file="TypeRef.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Types
{
    /// <summary>LLVM Type</summary>
    internal class TypeRef
        : ITypeRef
        , ITypeHandleOwner
    {
        /// <inheritdoc/>
        public LLVMTypeRef TypeHandle => TypeRefHandle;

        /// <inheritdoc/>
        public bool IsSized => Kind != TypeKind.Function && TypeRefHandle.IsSized;

        /// <inheritdoc/>
        public TypeKind Kind => ( TypeKind )TypeRefHandle.Kind;

        /// <inheritdoc/>
        public bool IsInteger => Kind == TypeKind.Integer;

        /// <inheritdoc/>
        public bool IsFloat => Kind == TypeKind.Float32;

        /// <inheritdoc/>
        public bool IsDouble => Kind == TypeKind.Float64;

        /// <inheritdoc/>
        public bool IsVoid => Kind == TypeKind.Void;

        /// <inheritdoc/>
        public bool IsStruct => Kind == TypeKind.Struct;

        /// <inheritdoc/>
        public bool IsPointer => Kind == TypeKind.Pointer;

        /// <inheritdoc/>
        public bool IsSequence => Kind == TypeKind.Array || Kind == TypeKind.Vector || Kind == TypeKind.Pointer;

        /// <inheritdoc/>
        public bool IsFloatingPoint
        {
            get
            {
                switch( Kind )
                {
                case TypeKind.Float16:
                case TypeKind.Float32:
                case TypeKind.Float64:
                case TypeKind.X86Float80:
                case TypeKind.Float128m112:
                case TypeKind.Float128:
                    return true;

                default:
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsPointerPointer => ( this is IPointerType ptrType ) && ptrType.ElementType.Kind == TypeKind.Pointer;

        /// <inheritdoc/>
        public Context Context => GetContextFor( TypeRefHandle );

        /// <inheritdoc/>
        public uint IntegerBitWidth => Kind != TypeKind.Integer ? 0 : TypeRefHandle.IntWidth;

        /// <inheritdoc/>
        public Constant GetNullValue( ) => Constant.NullValueFor( this );

        /// <inheritdoc/>
        public unsafe IArrayType CreateArrayType( uint count ) => FromHandle<IArrayType>( LLVM.ArrayType( TypeRefHandle, count ) );

        /// <inheritdoc/>
        public IPointerType CreatePointerType( ) => CreatePointerType( 0 );

        /// <inheritdoc/>
        public unsafe IPointerType CreatePointerType( uint addressSpace )
        {
            if( IsVoid )
            {
                throw new InvalidOperationException( "Cannot create pointer to void in LLVM, use i8* instead" );
            }

            return FromHandle<IPointerType>( LLVM.PointerType( TypeRefHandle, addressSpace ) );
        }

        /// <summary>Builds a string representation for this type in LLVM assembly language form</summary>
        /// <returns>Formatted string for this type</returns>
        public override string ToString( ) => TypeRefHandle.PrintToString( );

        internal TypeRef( LLVMTypeRef typeRef )
        {
            TypeRefHandle = typeRef;
            if( typeRef == default )
            {
                throw new ArgumentNullException( nameof( typeRef ) );
            }
        }

        internal static TypeRef FromHandle( LLVMTypeRef typeRef ) => FromHandle<TypeRef>( typeRef );

        [SuppressMessage( "Reliability", "CA2000:Dispose objects before losing scope", Justification = "Context is owned and disposed by global ContextCache" )]
        internal static T FromHandle<T>( LLVMTypeRef typeRef )
            where T : class, ITypeRef
        {
            if( typeRef == default )
            {
                return default;
            }

            var ctx = GetContextFor( typeRef );
            return ctx.GetTypeFor( typeRef ) as T;
        }

        internal class InterningFactory
            : HandleInterningMap<LLVMTypeRef, ITypeRef>
        {
            internal InterningFactory( Context context )
                : base( context )
            {
            }

            private protected override ITypeRef ItemFactory( LLVMTypeRef handle )
            {
                var kind = ( TypeKind )handle.Kind;
                return kind switch
                {
                    TypeKind.Struct => new StructType( handle ),
                    TypeKind.Array => new ArrayType( handle ),
                    TypeKind.Pointer => new PointerType( handle ),
                    TypeKind.Vector => new VectorType( handle ),
                    TypeKind.Function => new FunctionType( handle ),
                    _ => new TypeRef( handle ),
                };
            }
        }

        protected LLVMTypeRef TypeRefHandle { get; }

        [SuppressMessage( "Reliability", "CA2000:Dispose objects before losing scope", Justification = "Context created here is owned, and disposed of via the ContextCache" )]
        private static Context GetContextFor( LLVMTypeRef handle )
        {
            if( handle == default )
            {
                throw new ArgumentException( "Handle is null", nameof( handle ) );
            }

            var hContext = handle.Context;
            return ContextCache.GetContextFor( hContext );
        }
    }
}
