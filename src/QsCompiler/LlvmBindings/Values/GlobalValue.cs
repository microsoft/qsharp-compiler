// -----------------------------------------------------------------------
// <copyright file="GlobalValue.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Linkage specification for functions and globals.</summary>
    /// <seealso href="xref:llvm_langref#linkage-types">LLVM Linkage Types</seealso>
    public enum Linkage
    {
        /// <summary>Externally visible Global.</summary>
        External = LLVMLinkage.LLVMExternalLinkage,    /*< Externally visible function */

        /// <summary>Available Externally.</summary>
        /// <remarks>Globals with “available_externally” linkage are never emitted into the object file corresponding to the LLVM module.
        /// From the linker’s perspective, an available_externally global is equivalent to an external declaration. They exist to allow
        /// in-lining and other optimizations to take place given knowledge of the definition of the global, which is known to be somewhere
        /// outside the module. Globals with available_externally linkage are allowed to be discarded at will, and allow in-lining and other
        /// optimizations. This linkage type is only allowed on definitions, not declarations.
        /// </remarks>
        AvailableExternally = LLVMLinkage.LLVMAvailableExternallyLinkage,

        /// <summary>Keep a single copy when linking.</summary>
        LinkOnceAny = LLVMLinkage.LLVMLinkOnceAnyLinkage,

        /// <summary>Like <see cref="LinkOnceAny"/> but can only be replaced by equivalent (One Definition Rule).</summary>
        LinkOnceODR = LLVMLinkage.LLVMLinkOnceODRLinkage,

        // LLVMLinkage.LLVMLinkOnceODRAutoHideLinkage, /**< Obsolete */

        /// <summary>Keep one copy when linking (weak).</summary>
        Weak = LLVMLinkage.LLVMWeakAnyLinkage,

        /// <summary>Like <seealso cref="Weak"/> but only replaced by something equivalent (e.g. One Definition Rule).</summary>
        WeakODR = LLVMLinkage.LLVMWeakODRLinkage,

        /// <summary>Special purpose, applies only to global arrays.</summary>
        /// <seealso href="xref:llvm_langref#linkage-types"/>
        Append = LLVMLinkage.LLVMAppendingLinkage,

        /// <summary>Rename collision when linking (i.e static function).</summary>
        Internal = LLVMLinkage.LLVMInternalLinkage,

        /// <summary>Link as <see cref="Internal"/> but omit from the generated symbol table.</summary>
        Private = LLVMLinkage.LLVMPrivateLinkage,

        /// <summary>Global to be imported from a DLL.</summary>
        DllImport = LLVMLinkage.LLVMDLLImportLinkage,

        /// <summary>Global to be Exported from a DLL.</summary>
        DllExport = LLVMLinkage.LLVMDLLExportLinkage,

        /// <summary>External weak linkage.</summary>
        /// <remarks>
        /// The semantics of this linkage follow the ELF object file model: the symbol is weak until linked,
        /// if not linked, the symbol becomes null instead of being an undefined reference.
        /// </remarks>
        ExternalWeak = LLVMLinkage.LLVMExternalWeakLinkage, /*< ExternalWeak linkage description */

        // LLVMLinkage.LLVMGhostLinkage,       /*< Obsolete */

        /// <summary>Tentative definitions.</summary>
        Common = LLVMLinkage.LLVMCommonLinkage,

        /// <summary>Like <see cref="Private"/> but the linker remove this symbol.</summary>
        LinkerPrivate = LLVMLinkage.LLVMLinkerPrivateLinkage,

        /// <summary>Weak form of <see cref="LinkerPrivate"/>.</summary>
        LinkerPrivateWeak = LLVMLinkage.LLVMLinkerPrivateWeakLinkage, /*< Like LinkerPrivate, but is weak. */
    }

    // TODO: auto enforce default visibility in Linkage setter(s)
    // TODO: verify default visibility in global value factory methods

    /// <summary>Enumeration for the visibility of a global value.</summary>
    /// <remarks>
    /// A symbol with <see cref="Linkage.Internal"/> or <see cref="Linkage.Private"/>
    /// must have <see cref="Default"/> visibility.
    /// </remarks>
    /// <seealso href="xref:llvm_langref#visibility-styles">LLVM Visibility Styles</seealso>
    public enum Visibility
    {
        /// <summary>Default visibility for a <see cref="GlobalValue"/>.</summary>
        Default = LLVMVisibility.LLVMDefaultVisibility,

        /// <summary>Two declarations of an object with hidden visibility refer to the same object if they are in the same shared object.</summary>
        Hidden = LLVMVisibility.LLVMHiddenVisibility,

        /// <summary>Symbol cannot be overridden by another module.</summary>
        Protected = LLVMVisibility.LLVMProtectedVisibility,
    }

    /// <summary>Unnamed address state of a global value.</summary>
    public enum UnnamedAddressKind
    {
        /// <summary>Address of the global is significant.</summary>
        None = LLVMUnnamedAddr.LLVMNoUnnamedAddr,

        /// <summary>Address of the global is locally significant.</summary>
        Local = LLVMUnnamedAddr.LLVMLocalUnnamedAddr,

        /// <summary>Address of the global is globally significant.</summary>
        Global = LLVMUnnamedAddr.LLVMGlobalUnnamedAddr,
    }

    /// <summary>LLVM Global value. </summary>
    public unsafe class GlobalValue
        : Constant
    {
        internal GlobalValue(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }

        /// <summary>Gets or sets the visibility of this global value.</summary>
        public Visibility Visibility
        {
            get => (Visibility)this.ValueHandle.Visibility;
            set => LLVM.SetVisibility(this.ValueHandle, (LLVMVisibility)value);
        }

        /// <summary>Gets or sets the linkage specification for this symbol.</summary>
        public Linkage Linkage
        {
            get => (Linkage)this.ValueHandle.Linkage;
            set => LLVM.SetLinkage(this.ValueHandle, (LLVMLinkage)value);
        }

        /// <summary>Gets or sets a value indicating whether this is an Unnamed address.</summary>
        public UnnamedAddressKind UnnamedAddress
        {
            get => (UnnamedAddressKind)LLVM.GetUnnamedAddress(this.ValueHandle);
            set => LLVM.SetUnnamedAddress(this.ValueHandle, (LLVMUnnamedAddr)value);
        }

        /// <summary>Gets a value indicating whether this is a declaration.</summary>
        public bool IsDeclaration => this.ValueHandle.IsDeclaration;

        /// <summary>Gets the Module containing this global value.</summary>
        public BitcodeModule ParentModule => this.NativeType.Context.GetModuleFor(this.ValueHandle.GlobalParent);
    }
}
