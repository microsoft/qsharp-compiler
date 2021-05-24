// -----------------------------------------------------------------------
// <copyright file="DwarfEnumerations.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

using LLVMSharp.Interop;

// The names describe what they are, further details are available in the DWARF specs
#pragma warning disable CS1591, SA1600, SA1602 // Enumeration items must be documented

// ReSharper disable IdentifierTypo
namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>DWARF Debug information language</summary>
    public enum SourceLanguage
    {
        /// <summary>Invalid language</summary>
        Invalid = 0,

        C89 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC89,
        C = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC,
        Ada83 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageAda83,
        CPlusPlus = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC_plus_plus,
        Cobol74 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageCobol74,
        Cobol85 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageCobol85,
        Fortran77 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageFortran77,
        Fortran90 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageFortran90,
        Pascal83 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguagePascal83,
        Modula2 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageModula2,
        Java = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageJava,
        C99 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC99,
        Ada95 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageAda95,
        Fortran95 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageFortran95,
        PLI = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguagePLI,
        ObjC = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageObjC,
        ObjCPlusPlus = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageObjC_plus_plus,
        UPC = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageUPC,
        D = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageD,
        Python = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguagePython,
        OpenCL = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageOpenCL,
        Go = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageGo,
        Modula3 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageModula3,
        Haskell = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageHaskell,
        CPlusPlus03 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC_plus_plus_03,
        CPlusPlus11 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC_plus_plus_11,
        OCaml = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageOCaml,
        Rust = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageRust,
        C11 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC11,
        Swift = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageSwift,
        Julia = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageJulia,
        Dylan = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageDylan,
        CPlusPlus14 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC_plus_plus_14,
        Fortran03 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageFortran03,
        Fortran08 = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageFortran08,
        RenderScript = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageRenderScript,
        Bliss = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageBLISS,
        MipsAssembler = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageMips_Assembler,
        GoogleRenderScript = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageGOOGLE_RenderScript,
        BorlandDelphi = LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageBORLAND_Delphi,

        /// <summary>Base value for unofficial languages ids</summary>
        UserMin = 0x8000,

        /// <summary>[Ubiquity.NET.Llvm] C# Language</summary>
        CSharp = UserMin + 0x01000,

        /// <summary>[Ubiquity.NET.Llvm] .NET IL Assembly language (ILAsm)</summary>
        ILAsm = UserMin + 0x01001,

        /// <summary>Max Value for unofficial language ids</summary>
        UserMax = 0xffff,
    }

    /// <summary>Tag kind for the debug information discriminated union nodes</summary>
    [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "matches interop type from native code")]
    public enum Tag : ushort
    {
        None = 0,
        ArrayType,
        ClassType,
        EntryPoint,
        EnumerationType,
        FormalParameter,
        ImportedDeclaration,
        TagLabel,
        LexicalBlock,
        Member,
        PointerType,
        ReferenceType,
        CompileUnit,
        StringType,
        StructureType,
        SubroutineType,
        TypeDef,
        UnionType,
        UnspecifiedParameters,
        Variant,
        CommonBlock,
        CommonInclusion,
        Inheritance,
        InlinedSubroutine,
        Module,
        PointerToMemberType,
        SetType,
        SubrangeType,
        WithStatement,
        AccessDeclaration,
        BaseType,
        CatchBlock,
        ConstType,
        Constant,
        Enumerator,
        FileType,
        Friend,
        NameList,
        NameListItem,
        PackedType,
        SubProgram,
        TemplateTypeParameter,
        TemplateValueParameter,
        ThrownType,
        TryBlock,
        VariantPart,
        Variable,
        VolatileType,

        // New in DWARF v3:
        DwarfProcedure,
        RestrictType,
        InterfaceType,
        Namespace,
        ImportedModule,
        UnspecifiedType,
        PartialUnit,
        ImportedUnit,
        Condition,
        SharedType,

        // New in DWARF v4:
        TypeUnit,
        RValueReferenceType,
        TemplateAlias,

        // New in DWARF v5:
        CoArrayType,
        GenericSubrange,
        DynamicType,
        AtomicType,
        CallSite,
        CallSiteParameter,
        SkeletonUnit,
        ImmutableType,

        // Vendor extensions
        MIPSLoop = 0x4081,

        FormatLabel = 0x4101,
        FunctionTemplate,
        ClassTemplate,

        GNUTemplateParameter = 0x4106,
        GNUTemplateParameterPack,
        GNUFormalParameterPack,
        GNUCallSite,
        GNUCallSiteParameter,

        AppleProperty = 0x4200,

        BorlandProperty = 0xb000,
        BorlandDelphiString,
        BorlandDelphiDynamicString,
        BorlandDelphiSet,
        BorlandDelphiVariant,
    }

    /// <summary>Tags for qualified types</summary>
    public enum QualifiedTypeTag
    {
        None = 0,
        Const = Tag.ConstType,
        Volatile = Tag.VolatileType,
    }

    /// <summary>Primitive type supported by the debug information</summary>
    public enum DiTypeKind
    {
        Invalid = 0,

        // Encoding attribute values
        Address,
        Boolean,
        ComplexFloat,
        Float,
        Signed,
        SignedChar,
        Unsigned,
        UnsignedChar,

        // New in DWARF v3:
        ImaginaryFloat,
        PackedDecimal,
        NumericString,
        Edited,
        SignedFixed,
        UnsignedFixed,
        DecimalFloat,

        // New in DWARF v4:
        UTF,

        // New in DWARF v5:
        UCS,
        ASCII,

        // From LLVM.NET LibLLVM
        LoUser = 0x80,
        HiUser = 0xff,
    }

    /// <summary>Debug information flags</summary>
    /// <remarks>
    /// The three accessibility flags are mutually exclusive and rolled together
    /// in the first two bits.
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags", Justification = "Matches the underlying wrapped API")]
    [Flags]
    public enum DebugInfoFlags
    {
        None = LLVMDIFlags.LLVMDIFlagZero,
        Private = LLVMDIFlags.LLVMDIFlagPrivate,
        Protected = LLVMDIFlags.LLVMDIFlagProtected,
        Public = LLVMDIFlags.LLVMDIFlagPublic,
        ForwardDeclaration = LLVMDIFlags.LLVMDIFlagFwdDecl,
        AppleBlock = LLVMDIFlags.LLVMDIFlagAppleBlock,
        Virtual = LLVMDIFlags.LLVMDIFlagVirtual,
        Artificial = LLVMDIFlags.LLVMDIFlagArtificial,
        Explicit = LLVMDIFlags.LLVMDIFlagExplicit,
        Prototyped = LLVMDIFlags.LLVMDIFlagPrototyped,
        ObjcClassComplete = LLVMDIFlags.LLVMDIFlagObjcClassComplete,
        ObjectPointer = LLVMDIFlags.LLVMDIFlagObjectPointer,
        Vector = LLVMDIFlags.LLVMDIFlagVector,
        StaticMember = LLVMDIFlags.LLVMDIFlagStaticMember,
        LValueReference = LLVMDIFlags.LLVMDIFlagLValueReference,
        RValueReference = LLVMDIFlags.LLVMDIFlagRValueReference,
        Reserved = LLVMDIFlags.LLVMDIFlagReserved,
        SingleInheritance = LLVMDIFlags.LLVMDIFlagSingleInheritance,
        MultipleInheritance = LLVMDIFlags.LLVMDIFlagMultipleInheritance,
        VirtualInheritance = LLVMDIFlags.LLVMDIFlagVirtualInheritance,
        IntroducedVirtual = LLVMDIFlags.LLVMDIFlagIntroducedVirtual,
        BitField = LLVMDIFlags.LLVMDIFlagBitField,
        NoReturn = LLVMDIFlags.LLVMDIFlagNoReturn,
        TypePassByValue = LLVMDIFlags.LLVMDIFlagTypePassByValue,
        TypePassByReference = LLVMDIFlags.LLVMDIFlagTypePassByReference,
        EnumClass = LLVMDIFlags.LLVMDIFlagEnumClass,
        FixedEnum = LLVMDIFlags.LLVMDIFlagFixedEnum,
        Thunk = LLVMDIFlags.LLVMDIFlagThunk,
        NonTrivial = LLVMDIFlags.LLVMDIFlagNonTrivial,
        BigEndian = LLVMDIFlags.LLVMDIFlagBigEndian,
        LittleEndian = LLVMDIFlags.LLVMDIFlagLittleEndian,
        IndirectVirtualBase = LLVMDIFlags.LLVMDIFlagIndirectVirtualBase,
        AccessibilityMask = LLVMDIFlags.LLVMDIFlagAccessibility,
        PtrToMemberRep = LLVMDIFlags.LLVMDIFlagPtrToMemberRep,
    }

    /// <summary>Debug information expression operator</summary>
    [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "Matches underlying interop type")]
    public enum ExpressionOp : long
    {
        Invalid = 0,
        Addr = 0x03,
        Deref = 0x06,
        Const1u = 0x08,
        Const1s = 0x09,
        Const2u = 0x0a,
        Const2s = 0x0b,
        Const4u = 0x0c,
        Const4s = 0x0d,
        Const8u = 0x0e,
        Const8s = 0x0f,
        Constu = 0x10,
        Consts = 0x11,
        Dup = 0x12,
        Drop = 0x13,
        Over = 0x14,
        Pick = 0x15,
        Swap = 0x16,
        Rot = 0x17,
        Xderef = 0x18,
        Abs = 0x19,
        And = 0x1a,
        Div = 0x1b,
        Minus = 0x1c,
        Mod = 0x1d,
        Mul = 0x1e,
        Neg = 0x1f,
        Not = 0x20,
        Or = 0x21,
        Plus = 0x22,
        PlusUconst = 0x23,
        Shl = 0x24,
        Shr = 0x25,
        Shra = 0x26,
        Xor = 0x27,
        Skip = 0x2f,
        Bra = 0x28,
        Eq = 0x29,
        Ge = 0x2a,
        Gt = 0x2b,
        Le = 0x2c,
        Lt = 0x2d,
        Ne = 0x2e,
        Lit0 = 0x30,
        Lit1 = 0x31,
        Lit2 = 0x32,
        Lit3 = 0x33,
        Lit4 = 0x34,
        Lit5 = 0x35,
        Lit6 = 0x36,
        Lit7 = 0x37,
        Lit8 = 0x38,
        Lit9 = 0x39,
        Lit10 = 0x3a,
        Lit11 = 0x3b,
        Lit12 = 0x3c,
        Lit13 = 0x3d,
        Lit14 = 0x3e,
        Lit15 = 0x3f,
        Lit16 = 0x40,
        Lit17 = 0x41,
        Lit18 = 0x42,
        Lit19 = 0x43,
        Lit20 = 0x44,
        Lit21 = 0x45,
        Lit22 = 0x46,
        Lit23 = 0x47,
        Lit24 = 0x48,
        Lit25 = 0x49,
        Lit26 = 0x4a,
        Lit27 = 0x4b,
        Lit28 = 0x4c,
        Lit29 = 0x4d,
        Lit30 = 0x4e,
        Lit31 = 0x4f,
        Reg0 = 0x50,
        Reg1 = 0x51,
        Reg2 = 0x52,
        Reg3 = 0x53,
        Reg4 = 0x54,
        Reg5 = 0x55,
        Reg6 = 0x56,
        Reg7 = 0x57,
        Reg8 = 0x58,
        Reg9 = 0x59,
        Reg10 = 0x5a,
        Reg11 = 0x5b,
        Reg12 = 0x5c,
        Reg13 = 0x5d,
        Reg14 = 0x5e,
        Reg15 = 0x5f,
        Reg16 = 0x60,
        Reg17 = 0x61,
        Reg18 = 0x62,
        Reg19 = 0x63,
        Reg20 = 0x64,
        Reg21 = 0x65,
        Reg22 = 0x66,
        Reg23 = 0x67,
        Reg24 = 0x68,
        Reg25 = 0x69,
        Reg26 = 0x6a,
        Reg27 = 0x6b,
        Reg28 = 0x6c,
        Reg29 = 0x6d,
        Reg30 = 0x6e,
        Reg31 = 0x6f,
        Breg0 = 0x70,
        Breg1 = 0x71,
        Breg2 = 0x72,
        Breg3 = 0x73,
        Breg4 = 0x74,
        Breg5 = 0x75,
        Breg6 = 0x76,
        Breg7 = 0x77,
        Breg8 = 0x78,
        Breg9 = 0x79,
        Breg10 = 0x7a,
        Breg11 = 0x7b,
        Breg12 = 0x7c,
        Breg13 = 0x7d,
        Breg14 = 0x7e,
        Breg15 = 0x7f,
        Breg16 = 0x80,
        Breg17 = 0x81,
        Breg18 = 0x82,
        Breg19 = 0x83,
        Breg20 = 0x84,
        Breg21 = 0x85,
        Breg22 = 0x86,
        Breg23 = 0x87,
        Breg24 = 0x88,
        Breg25 = 0x89,
        Breg26 = 0x8a,
        Breg27 = 0x8b,
        Breg28 = 0x8c,
        Breg29 = 0x8d,
        Breg30 = 0x8e,
        Breg31 = 0x8f,
        Regx = 0x90,
        Fbreg = 0x91,
        Bregx = 0x92,
        Piece = 0x93,
        DerefSize = 0x94,
        XderefSize = 0x95,
        Nop = 0x96,
        PushObjectAddress = 0x97,
        Call2 = 0x98,
        Call4 = 0x99,
        CallRef = 0x9a,
        FormTlsAddress = 0x9b,
        CallFrameCFA = 0x9c,
        BitPiece = 0x9d,
        ImplicitValue = 0x9e,
        StackValue = 0x9f,

        // Extensions for GNU-style thread-local storage.
        GnuPushTlsAddress = 0xe0,

        // Extensions for Fission proposal.
        GnuAddrIndex = 0xfb,
        GnuConstIndex = 0xfc,
    }
}
