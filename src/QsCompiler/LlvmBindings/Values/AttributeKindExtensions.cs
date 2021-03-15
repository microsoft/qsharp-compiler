// -----------------------------------------------------------------------
// <copyright file="AttributeKindExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Instructions;


namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Enumeration for the known LLVM attributes</summary>
    /// <remarks>
    /// <para>It is important to note that the integer values of this enum do NOT necessarily
    /// correlate to the LLVM attribute IDs. LLVM has moved away from using an enum Flags model
    /// as the number of attributes reached the limit of available bits. Thus, the enum was
    /// dropped. Instead, strings are used to identify attributes. However, for maximum
    /// compatibility and ease of use for this library the enum is retained and the provided
    /// attribute manipulation classes will map the enum to the associated string.</para>
    /// <note type="warning">As a result of the changes in LLVM this set of attributes is
    /// fluid and subject to change from version to version. Thus, code using any attributes
    /// that have changed or were removed will produce compile time errors. That is useful
    /// and by design so that any changes in LLVM naming will break at compile time instead
    /// of at runtime.</note>
    /// </remarks>
    /// <seealso href="xref:llvm_langref#function-attributes">LLVM Function Attributes</seealso>
    /// <seealso href="xref:llvm_langref#parameter-attributes">LLVM Parameter Attributes</seealso>
    public enum AttributeKind
    {
        /// <summary>No attributes</summary>
        None,

        /// <summary>This indicates that the pointer value may be assumed by the optimizer to
        /// have the specified alignment.</summary>
        /// <remarks>
        /// <note type="note">
        /// This attribute has additional semantics when combined with the byval attribute.
        /// </note>
        /// </remarks>
        Alignment,

        /// <summary>This attribute indicates that the annotated function will always return at
        /// least a given number of bytes (or null).</summary>
        /// <remarks>Its arguments are zero-indexed parameter numbers; if one argument is provided,
        /// then it’s assumed that at least CallSite.Args[EltSizeParam] bytes will be available at
        /// the returned pointer. If two are provided, then it’s assumed that CallSite.Args[EltSizeParam]
        /// * CallSite.Args[NumEltsParam] bytes are available. The referenced parameters must be integer
        /// types. No assumptions are made about the contents of the returned block of memory.
        /// </remarks>
        AllocSize,

        /// <summary>is attribute indicates that the inliner should attempt to inline this function
        /// into callers whenever possible, ignoring any active inlining size threshold for this caller.</summary>
        AlwaysInline,

        /// <summary>indicates that the only memory accesses inside function are loads and stores from
        /// objects pointed to by its pointer-typed arguments, with arbitrary offsets</summary>
        /// <remarks>This attribute indicates that the only memory accesses inside function are loads and
        /// stores from objects pointed to by its pointer-typed arguments, with arbitrary offsets. Or in
        /// other words, all memory operations in the function can refer to memory only using pointers
        /// based on its function arguments. Note that argmemonly can be used together with readonly
        /// attribute in order to specify that function reads only from its arguments.</remarks>
        ArgMemOnly,

        /// <summary>This indicates that the callee function at a call site should be recognized as a
        /// built-in function, even though the function’s declaration uses the nobuiltin attribute.</summary>
        /// <remarks>
        /// This is only valid at call sites for direct calls to functions that are declared with the
        /// nobuiltin attribute.</remarks>
        Builtin,

        /// <summary>This indicates that the pointer parameter should really be passed by value to the function.</summary>
        /// <remarks>
        /// <para>The attribute implies that a hidden copy of the pointee is made between the caller and
        /// the callee, so the callee is unable to modify the value in the caller. This attribute is only
        /// valid on LLVM pointer arguments. It is generally used to pass structs and arrays by value, but
        /// is also valid on pointers to scalars. The copy is considered to belong to the caller not the
        /// callee (for example, readonly functions should not write to byval parameters). This is not a
        /// valid attribute for return values.</para>
        /// <para>The byval attribute also supports specifying an alignment with the align attribute. It
        /// indicates the alignment of the stack slot to form and the known alignment of the pointer
        /// specified to the call site. If the alignment is not specified, then the code generator makes
        /// a target-specific assumption.</para>
        /// </remarks>
        ByVal,

        /// <summary>This attribute indicates that this function is rarely called.</summary>
        /// <remarks>
        /// When computing edge weights, basic blocks post-dominated by a cold function call are also considered to be cold; and, thus, given low weight.
        /// </remarks>
        Cold,

        /// <summary>This attribute marks a function as convergent</summary>
        Convergent,

        /// <summary>This indicates that the parameter or return pointer is dereferenceable</summary>
        Dereferenceable,

        /// <summary>This indicates that the parameter or return value isn’t both non-null and non-dereferenceable (up to 'n' bytes) at the same time.</summary>
        DereferenceableOrNull,

        /// <summary>The inalloca argument attribute allows the caller to take the address of outgoing stack arguments.</summary>
        /// <remarks>
        /// <para>An inalloca argument must be a pointer to stack memory produced by an <see cref="Alloca"/>
        /// instruction. The alloca, or argument allocation, must also be tagged with the inalloca keyword.
        /// Only the last argument may have the inalloca attribute, and that argument is guaranteed to be
        /// passed in memory.</para>
        /// <para>An argument allocation may be used by a call at most once because the call may deallocate
        /// it. The inalloca attribute cannot be used in conjunction with other attributes that affect argument
        /// storage, like <see cref="InReg"/>, <see cref="Nest"/>, <see cref="StructRet"/>, or <see cref="ByVal"/>.
        /// The inalloca attribute also disables LLVM’s implicit lowering of large aggregate return values,
        /// which means that frontend authors must lower them with sret pointers.</para>
        /// <para>When the call site is reached, the argument allocation must have been the most recent stack
        /// allocation that is still live, or the results are undefined. It is possible to allocate additional
        /// stack space after an argument allocation and before its call site, but it must be cleared off with
        /// llvm.stackrestore.</para>
        /// </remarks>
        InAlloca,

        /// <summary>This indicates that this parameter or return value should be treated in a special target-dependent fashion
        /// while emitting code for a function call or return (usually, by putting it in a register as opposed to memory, though
        /// some targets use it to distinguish between two different kinds of registers). Use of this attribute is target-specific.
        /// </summary>
        InReg,

        /// <summary>This attribute indicates that the function may only access memory that is not accessible by the module being compiled.</summary>
        InaccessibleMemOnly,

        /// <summary>This attribute indicates that the function may only access memory that is either not accessible by the module being compiled, or is pointed to by its pointer arguments.</summary>
        InaccessibleMemOrArgMemOnly,

        /// <summary>This attribute indicates that the source code contained a hint that inlining this function is desirable (such as the “inline” keyword in C/C++). It is just a hint; it imposes no requirements on the inliner.</summary>
        InlineHint,

        /// <summary>This attribute indicates that the function should be added to a jump-instruction table at code-generation time,</summary>
        JumpTable,

        /// <summary>This attribute suggests that optimization passes and code generator passes make choices that keep the code size of this function as small as possible</summary>
        MinSize,

        /// <summary>This attribute disables prologue / epilogue emission for the function. This can have very system-specific consequences.</summary>
        Naked,

        /// <summary>This indicates that the pointer parameter can be excised using the trampoline intrinsics.</summary>
        Nest,

        /// <summary>This indicates that objects accessed via pointer values based on the argument or return value are not also accessed, during the execution of the function, via pointer values not based on the argument or return value. </summary>
        NoAlias,

        /// <summary>This indicates that the callee function at a call site is not recognized as a built-in function.</summary>
        NoBuiltin,

        /// <summary>This indicates that the callee does not make any copies of the pointer that outlive the callee itself.</summary>
        NoCapture,

        /// <summary>This attribute indicates that calls to the function cannot be duplicated.</summary>
        NoDuplicate,

        /// <summary>This attributes disables implicit floating point instructions.</summary>
        NoImplicitFloat,

        /// <summary>This attribute indicates that the inliner should never inline this function in any situation.</summary>
        NoInline,

        /// <summary>This function attribute indicates that the function does not call itself either directly or indirectly down any possible call path</summary>
        NoRecurse,

        /// <summary>This attribute indicates that the code generator should not use a red zone, even if the target-specific ABI normally permits it</summary>
        NoRedZone,

        /// <summary>This function attribute indicates that the function never returns normally.</summary>
        NoReturn,

        /// <summary>This function attribute indicates that the function never raises an exception.</summary>
        NoUnwind,

        /// <summary>This attribute suppresses lazy symbol binding for the function.</summary>
        NonLazyBind,

        /// <summary>This indicates that the parameter or return pointer is not null.</summary>
        NonNull,

        /// <summary>Optimize for size</summary>
        OptimizeForSize,

        /// <summary>Do not optimize</summary>
        OptimizeNone,

        /// <summary>On a function, this attribute indicates that the function computes its result (or decides to unwind an exception) based strictly on its arguments, without dereferencing any pointer arguments or otherwise accessing any mutable state</summary>
        ReadNone,

        /// <summary>On a function, this attribute indicates that the function does not write through any pointer arguments (including byval arguments) or otherwise modify any state (e.g. memory, control registers, etc) visible to caller functions</summary>
        ReadOnly,

        /// <summary>This indicates that the function always returns the argument as its return value.</summary>
        Returned,

        /// <summary>This attribute indicates that this function can return twice.</summary>
        ReturnsTwice,

        /// <summary>This indicates to the code generator that the parameter or return value should be sign-extended to the extent
        /// required by the target’s ABI (which is usually 32-bits) by the caller (for a parameter) or the callee (for a return value).
        /// </summary>
        SExt,

        /// <summary>This attribute indicates that SafeStack protection is enabled for this function.</summary>
        SafeStack,

        /// <summary>This attribute indicates that AddressSanitizer checks (dynamic address safety analysis) are enabled for this function.</summary>
        SanitizeAddress,

        /// <summary>This attribute indicates that MemorySanitizer checks (dynamic detection of accesses to uninitialized memory) are enabled for this function.</summary>
        SanitizeMemory,

        /// <summary>This attribute indicates that ThreadSanitizer checks (dynamic thread safety analysis) are enabled for this function.</summary>
        SanitizeThread,

        /// <summary>This function attribute indicates that the function does not have any effects besides calculating its result and does not have undefined behavior.</summary>
        Speculatable,

        /// <summary>This attribute indicates that, when emitting the prologue and epilogue, the back-end should forcibly align the stack pointer.</summary>
        StackAlignment,

        /// <summary>This attribute indicates that the function should emit a stack smashing protector.</summary>
        StackProtect,

        /// <summary>This attribute indicates that the function should always emit a stack smashing protector.</summary>
        StackProtectReq,

        /// <summary>This attribute indicates that the function should emit a stack smashing protector.</summary>
        StackProtectStrong,

        /// <summary>This indicates that the pointer parameter specifies the address of a structure that is the return value of the function in the source program.</summary>
        StructRet,

        /// <summary>This attribute is motivated to model and optimize Swift error handling.</summary>
        SwiftError,

        /// <summary>This indicates that the parameter is the self/context parameter.</summary>
        SwiftSelf,

        /// <summary>This attribute indicates that the ABI being targeted requires that an unwind table entry be produced for this function even if we can show that no exceptions passes by it.</summary>
        UWTable,

        /// <summary>This attribute indicates the item is write only</summary>
        /// <remarks>
        /// On a function, this attribute indicates that the function may write to but does not read from memory.
        /// On an argument, this attribute indicates that the function may write to but does not read through this pointer argument (even though it may read from the memory that the pointer points to).
        /// </remarks>
        WriteOnly,

        /// <summary>This indicates to the code generator that the parameter or return value should be zero-extended to the extent
        /// required by the target’s ABI by the caller (for a parameter) or the callee (for a return value).</summary>
        ZExt
    }

    /// <summary>Enumeration flags to indicate which attribute set index an attribute may apply to</summary>
    [Flags]
    public enum FunctionIndexKinds
    {
        /// <summary>Invalid attributes don't apply to any index</summary>
        None = 0,

        /// <summary>The attribute is applicable to a function</summary>
        Function = 1,

        /// <summary>The attribute is applicable to a function's return</summary>
        Return = 2,

        /// <summary>The attribute is applicable to a function's parameter</summary>
        Parameter = 4
    }

    /// <summary>Utility class to provide extension methods for validating usage of attribute kinds</summary>
    public static class AttributeKindExtensions
    {
        /// <summary>Gets the symbolic name of the attribute</summary>
        /// <param name="kind"><see cref="AttributeKind"/> to get the name of</param>
        /// <returns>Name of the attribute</returns>
        public static string GetAttributeName( this AttributeKind kind )
        {
            return KnownAttributeNames[ ( int )kind ];
        }

        /// <summary>Gets a value indicating whether the attribute requires an integer parameter value</summary>
        /// <param name="kind"><see cref="AttributeKind"/> to check</param>
        /// <returns><see langword="true"/> if the attribute requires an integer value</returns>
        public static bool RequiresIntValue( this AttributeKind kind )
        {
            switch( kind )
            {
            case AttributeKind.Alignment:
            case AttributeKind.StackAlignment:
            case AttributeKind.Dereferenceable:
            case AttributeKind.DereferenceableOrNull:
                return true;

            default:
                return false;
            }
        }

        /// <summary>Looks up the <see cref="AttributeKind"/> for an LLVM attribute id</summary>
        /// <param name="id">LLVM attribute id</param>
        /// <returns><see cref="AttributeKind"/> that corresponds to the LLVM id</returns>
        public static AttributeKind LookupId( uint id )
        {
            return AttribIdToKindMap.Value.TryGetValue( id, out AttributeKind retValue ) ? retValue : AttributeKind.None;
        }

        internal static uint GetEnumAttributeId( this AttributeKind kind )
        {
            return KindToAttribIdMap.Value.TryGetValue( kind, out uint retVal ) ? retVal : 0;
        }

        internal static bool CheckAttributeUsage( this AttributeKind kind, FunctionAttributeIndex index, Value value )
        {
            FunctionIndexKinds allowedindices = kind.GetAllowedIndexes( );
            switch( index )
            {
            case FunctionAttributeIndex.Function:
                if( !allowedindices.HasFlag( FunctionIndexKinds.Function ) )
                {
                    return false;
                }

                break;

            case FunctionAttributeIndex.ReturnType:
                if( !allowedindices.HasFlag( FunctionIndexKinds.Return ) )
                {
                    return false;
                }

                break;

            // case FunctionAttributeIndex.Parameter0:
            default:
                {
                    if( value == default )
                    {
                        throw new ArgumentNullException( nameof( value ) );
                    }

                    if( !allowedindices.HasFlag( FunctionIndexKinds.Parameter ) )
                    {
                        return false;
                    }

                    IrFunction function;
                    switch( value )
                    {
                    case IrFunction f:
                        function = f;
                        break;

                    case CallInstruction call:
                        function = call.TargetFunction;
                        break;

                    case Argument arg:
                        function = arg.ContainingFunction;
                        break;

                    default:
                        function = default;
                        break;
                    }

                    int paramIndex = index - FunctionAttributeIndex.Parameter0;
                    if( paramIndex >= ( function?.Parameters.Count ?? 0 ) )
                    {
                        return false;
                    }
                }

                break;
            }

            return true;
        }

        internal static void VerifyAttributeUsage( this AttributeKind kind, FunctionAttributeIndex index, Value value )
        {
            VerifyAttributeUsage( kind, index );

            if( index >= FunctionAttributeIndex.Parameter0 )
            {
                IrFunction function;
                switch( value )
                {
                case IrFunction f:
                    function = f;
                    break;

                case CallInstruction call:
                    function = call.TargetFunction;
                    break;

                case Argument arg:
                    function = arg.ContainingFunction;
                    break;

                default:
                    function = default;
                    break;
                }

                int paramIndex = index - FunctionAttributeIndex.Parameter0;
                if( paramIndex > ( ( function?.Parameters.Count ?? 0 ) - 1 ) )
                {
                    throw new ArgumentException( );
                }
            }
        }

        internal static void VerifyAttributeUsage( this AttributeKind kind, FunctionAttributeIndex index )
        {
            FunctionIndexKinds allowedIndexes = kind.GetAllowedIndexes( );
            switch( index )
            {
            case FunctionAttributeIndex.Function:
                if( !allowedIndexes.HasFlag( FunctionIndexKinds.Function ) )
                {
                    throw new ArgumentException( );
                }

                break;

            case FunctionAttributeIndex.ReturnType:
                if( !allowedIndexes.HasFlag( FunctionIndexKinds.Return ) )
                {
                    throw new ArgumentException( );
                }

                break;

            // case FunctionAttributeIndex.Parameter0:
            default:
                if( !allowedIndexes.HasFlag( FunctionIndexKinds.Parameter ) )
                {
                    throw new ArgumentException( );
                }

                break;
            }
        }

        // To prevent native asserts or crashes - validates parameters before passing down to native code
        internal static void VerifyAttributeUsage( this AttributeKind kind, FunctionAttributeIndex index, ulong value )
        {
            kind.VerifyAttributeUsage( index );
            kind.RangeCheckValue( value );
        }

        internal static void RangeCheckValue( this AttributeKind kind, ulong value )
        {
            // To prevent native asserts or crashes - validate parameters before passing down to native code
            switch( kind )
            {
            case AttributeKind.Alignment:
                if( value > UInt32.MaxValue )
                {
                    throw new ArgumentOutOfRangeException( );
                }

                break;

            case AttributeKind.StackAlignment:
                if( value > UInt32.MaxValue )
                {
                    throw new ArgumentOutOfRangeException( );
                }

                if( value != 0 && !IsPowerOfTwo( value ) )
                {
                    throw new ArgumentException( );
                }

                break;

            case AttributeKind.Dereferenceable:
            case AttributeKind.DereferenceableOrNull:
                break;

            default:
                throw new ArgumentException( );
            }
        }

        [SuppressMessage( "Maintainability", "CA1502:Avoid excessive complexity", Justification = "It's just a big switch, get over it." )]
        internal static FunctionIndexKinds GetAllowedIndexes( this AttributeKind kind )
        {
            switch( kind )
            {
            default:
                return FunctionIndexKinds.None;

            case AttributeKind.ReadOnly:
            case AttributeKind.WriteOnly:
            case AttributeKind.ReadNone:
                return FunctionIndexKinds.Function | FunctionIndexKinds.Parameter;

            case AttributeKind.ByVal:
            case AttributeKind.InAlloca:
            case AttributeKind.StructRet:
            case AttributeKind.Nest:
            case AttributeKind.NoCapture:
            case AttributeKind.Returned:
            case AttributeKind.SwiftSelf:
            case AttributeKind.SwiftError:
                return FunctionIndexKinds.Parameter;

            case AttributeKind.ZExt:
            case AttributeKind.SExt:
            case AttributeKind.InReg:
            case AttributeKind.Alignment:
            case AttributeKind.NoAlias:
            case AttributeKind.NonNull:
            case AttributeKind.Dereferenceable:
            case AttributeKind.DereferenceableOrNull:
                return FunctionIndexKinds.Parameter | FunctionIndexKinds.Return;

            case AttributeKind.NoReturn:
            case AttributeKind.NoUnwind:
            case AttributeKind.NoInline:
            case AttributeKind.AlwaysInline:
            case AttributeKind.OptimizeForSize:
            case AttributeKind.StackProtect:
            case AttributeKind.StackProtectReq:
            case AttributeKind.StackProtectStrong:
            case AttributeKind.SafeStack:
            case AttributeKind.NoRedZone:
            case AttributeKind.NoImplicitFloat:
            case AttributeKind.Naked:
            case AttributeKind.InlineHint:
            case AttributeKind.StackAlignment:
            case AttributeKind.UWTable:
            case AttributeKind.NonLazyBind:
            case AttributeKind.ReturnsTwice:
            case AttributeKind.SanitizeAddress:
            case AttributeKind.SanitizeThread:
            case AttributeKind.SanitizeMemory:
            case AttributeKind.MinSize:
            case AttributeKind.NoDuplicate:
            case AttributeKind.Builtin:
            case AttributeKind.NoBuiltin:
            case AttributeKind.Cold:
            case AttributeKind.OptimizeNone:
            case AttributeKind.JumpTable:
            case AttributeKind.Convergent:
            case AttributeKind.ArgMemOnly:
            case AttributeKind.NoRecurse:
            case AttributeKind.InaccessibleMemOnly:
            case AttributeKind.InaccessibleMemOrArgMemOnly:
            case AttributeKind.AllocSize:
            case AttributeKind.Speculatable:
                return FunctionIndexKinds.Function;
            }
        }

        // use complement and compare technique for efficiency
        private static bool IsPowerOfTwo( ulong x ) => ( x != 0 ) && ( ( x & ( ~x + 1 ) ) == x );

        // Lazy initialized one time mapping of LLVM attribute Ids to AttributeKind
        private static readonly Lazy<Dictionary<uint, AttributeKind>> AttribIdToKindMap = new Lazy<Dictionary<uint, AttributeKind>>( BuildAttribIdToKindMap );

        private static unsafe Dictionary<uint, AttributeKind> BuildAttribIdToKindMap( )
        {
            return ( from kind in Enum.GetValues( typeof( AttributeKind ) ).Cast<AttributeKind>( ).Skip( 1 )
                     let name = kind.GetAttributeName( )
                     select new KeyValuePair<uint, AttributeKind>( LLVM.GetEnumAttributeKindForName( name.AsMarshaledString(), (UIntPtr)name.Length ), kind )
                   ).ToDictionary( kvp => kvp.Key, kvp => kvp.Value );
        }

        private static readonly Lazy<Dictionary<AttributeKind, uint>> KindToAttribIdMap = new Lazy<Dictionary<AttributeKind, uint>>( BuildKindToAttribIdMap );

        private static Dictionary<AttributeKind, uint> BuildKindToAttribIdMap( )
        {
            return AttribIdToKindMap.Value.ToDictionary( kvp => kvp.Value, kvp => kvp.Key );
        }

        private static readonly string[ ] KnownAttributeNames =
        {
            string.Empty,
            "align",
            "allocsize",
            "alwaysinline",
            "argmemonly",
            "builtin",
            "byval",
            "cold",
            "convergent",
            "dereferenceable",
            "dereferenceable_or_null",
            "inalloca",
            "inreg",
            "inaccessiblememonly",
            "inaccessiblemem_or_argmemonly",
            "inlinehint",
            "jumptable",
            "minsize",
            "naked",
            "nest",
            "noalias",
            "nobuiltin",
            "nocapture",
            "noduplicate",
            "noimplicitfloat",
            "noinline",
            "norecurse",
            "noredzone",
            "noreturn",
            "nounwind",
            "nonlazybind",
            "nonnull",
            "optsize",
            "optnone",
            "readnone",
            "readonly",
            "returned",
            "returns_twice",
            "signext",
            "safestack",
            "sanitize_address",
            "sanitize_memory",
            "sanitize_thread",
            "speculatable",
            "alignstack",
            "ssp",
            "sspreq",
            "sspstrong",
            "sret",
            "swifterror",
            "swiftself",
            "uwtable",
            "writeonly",
            "zeroext"
        };
    }
}
