// -----------------------------------------------------------------------
// <copyright file="DebugMemberInfo.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Types;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Describes a member/field of a type for creating debug information</summary>
    /// <remarks>
    /// <para>This class is used with <see cref="DebugStructType"/> to provide debug information for a type.</para>
    /// <para>In order to support explicit layout structures the members relating to layout are all nullable.
    /// When they are null then modules <see cref="BitcodeModule.Layout"/> target specific layout information is used to determine
    /// layout details. Setting the layout members of this class to non-null will override that behavior to define explicit
    /// layout details.</para>
    /// </remarks>
    public class DebugMemberInfo
    {
        /// <summary>Initializes a new instance of the <see cref="DebugMemberInfo"/> class.</summary>
        /// <param name="index">Member index</param>
        /// <param name="name">Member name</param>
        /// <param name="file">File containing the declaration of the member</param>
        /// <param name="line">Line number containing the member</param>
        /// <param name="debugType">Debug type for the member</param>
        /// <param name="debugInfoFlags">Flags for the member</param>
        /// <param name="explicitLayout">Explicit layout information for this member, if any</param>
        public DebugMemberInfo( uint index
                              , string name
                              , DIFile? file
                              , uint line
                              , IDebugType<ITypeRef, DIType> debugType
                              , DebugInfoFlags debugInfoFlags = DebugInfoFlags.None
                              , DebugMemberLayout? explicitLayout = null
                              )
        {
            Index = index;
            Name = name;
            File = file;
            Line = line;
            DebugType = debugType;
            DebugInfoFlags = debugInfoFlags;
            ExplicitLayout = explicitLayout;
        }

        /// <summary>Gets the LLVM structure element index this descriptor describes</summary>
        public uint Index { get; }

        /// <summary>Gets the name of the field</summary>
        public string Name { get; }

        /// <summary>Gets the file the field is declared in</summary>
        public DIFile? File { get; }

        /// <summary>Gets the source line the field is declared on</summary>
        public uint Line { get; }

        /// <summary>Gets the flags for the field declaration</summary>
        public DebugInfoFlags DebugInfoFlags { get; }

        /// <summary>Gets the debug type information for this field</summary>
        public IDebugType<ITypeRef, DIType> DebugType { get; }

        /// <summary>Gets the explicit layout information for this member</summary>
        /// <remarks>If this is <see langword="null"/> then <see href="xref:Ubiquity.NET.Llvm.DebugInfo.DebugStructType.SetBody*">DebugStructType.SetBody</see>
        /// will default to using <see cref="BitcodeModule.Layout"/> to determine the size using the module's target specific layout.
        /// <note type="Important">
        /// If this property is provided (e.g. is not <see langword="null"/>) for any member of a type, then
        /// it must be set for *all* members. In other words explicit layout must be defined for all members
        /// or none. Furthermore, for types using explicit layout, the LLVM type containing this member must
        /// include the "packed" modifier.
        /// </note>
        /// </remarks>
        public DebugMemberLayout? ExplicitLayout { get; }
    }
}
