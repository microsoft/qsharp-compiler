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
    /// <summary>LLVM Type.</summary>
    internal class TypeRef
        : ITypeRef,
        ITypeHandleOwner
    {
        internal TypeRef(LLVMTypeRef typeRef)
        {
            this.TypeRefHandle = typeRef;
            if (typeRef == default)
            {
                throw new ArgumentNullException(nameof(typeRef));
            }
        }

        /// <inheritdoc/>
        public LLVMTypeRef TypeHandle => this.TypeRefHandle;

        /// <inheritdoc/>
        public bool IsSized => this.Kind != TypeKind.Function && this.TypeRefHandle.IsSized;

        /// <inheritdoc/>
        public TypeKind Kind => (TypeKind)this.TypeRefHandle.Kind;

        /// <inheritdoc/>
        public bool IsInteger => this.Kind == TypeKind.Integer;

        /// <inheritdoc/>
        public bool IsFloat => this.Kind == TypeKind.Float32;

        /// <inheritdoc/>
        public bool IsDouble => this.Kind == TypeKind.Float64;

        /// <inheritdoc/>
        public bool IsVoid => this.Kind == TypeKind.Void;

        /// <inheritdoc/>
        public bool IsStruct => this.Kind == TypeKind.Struct;

        /// <inheritdoc/>
        public bool IsPointer => this.Kind == TypeKind.Pointer;

        /// <inheritdoc/>
        public bool IsSequence => this.Kind == TypeKind.Array || this.Kind == TypeKind.Vector || this.Kind == TypeKind.Pointer;

        /// <inheritdoc/>
        public bool IsFloatingPoint
        {
            get
            {
                switch (this.Kind)
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
        public bool IsPointerPointer => (this is IPointerType ptrType) && ptrType.ElementType.Kind == TypeKind.Pointer;

        /// <inheritdoc/>
        public Context Context => GetContextFor(this.TypeRefHandle);

        /// <inheritdoc/>
        public uint IntegerBitWidth => this.Kind != TypeKind.Integer ? 0 : this.TypeRefHandle.IntWidth;

        protected LLVMTypeRef TypeRefHandle { get; }

        /// <inheritdoc/>
        public Constant GetNullValue() => Constant.NullValueFor(this);

        /// <inheritdoc/>
        public unsafe IArrayType CreateArrayType(uint count) => FromHandle<IArrayType>(LLVM.ArrayType(this.TypeRefHandle, count));

        /// <inheritdoc/>
        public IPointerType CreatePointerType() => this.CreatePointerType(0);

        /// <inheritdoc/>
        public unsafe IPointerType CreatePointerType(uint addressSpace)
        {
            if (this.IsVoid)
            {
                throw new InvalidOperationException("Cannot create pointer to void in LLVM, use i8* instead");
            }

            return FromHandle<IPointerType>(LLVM.PointerType(this.TypeRefHandle, addressSpace));
        }

        public bool TryGetExtendedPropertyValue<T>(string id, [MaybeNullWhen(false)] out T value)
            => this.extensibleProperties.TryGetExtendedPropertyValue(id, out value);

        public void AddExtendedPropertyValue(string id, object? value)
            => this.extensibleProperties.AddExtendedPropertyValue(id, value);

        /// <summary>Builds a string representation for this type in LLVM assembly language form.</summary>
        /// <returns>Formatted string for this type.</returns>
        public override string ToString() => this.TypeRefHandle.PrintToString();

        internal static TypeRef FromHandle(LLVMTypeRef typeRef) => FromHandle<TypeRef>(typeRef);

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Context is owned and disposed by global ContextCache")]
        internal static T FromHandle<T>(LLVMTypeRef typeRef)
            where T : class, ITypeRef
        {
            var ctx = GetContextFor(typeRef);
            return (T)ctx.GetTypeFor(typeRef);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Context created here is owned, and disposed of via the ContextCache")]
        private static Context GetContextFor(LLVMTypeRef handle)
        {
            if (handle == default)
            {
                throw new ArgumentException("Handle is null", nameof(handle));
            }

            var hContext = handle.Context;
            return ThreadContextCache.GetOrCreateAndRegister(hContext);
        }

        internal class InterningFactory
            : HandleInterningMap<LLVMTypeRef, ITypeRef>
        {
            internal InterningFactory(Context context)
                : base(context)
            {
            }

            private protected override ITypeRef ItemFactory(LLVMTypeRef handle)
            {
                var kind = (TypeKind)handle.Kind;
                return kind switch
                {
                    TypeKind.Struct => new StructType(handle),
                    TypeKind.Array => new ArrayType(handle),
                    TypeKind.Pointer => new PointerType(handle),
                    TypeKind.Vector => new VectorType(handle),
                    TypeKind.Function => new FunctionType(handle),
                    _ => new TypeRef(handle),
                };
            }
        }

        private readonly ExtensiblePropertyContainer extensibleProperties = new ExtensiblePropertyContainer();
    }
}
