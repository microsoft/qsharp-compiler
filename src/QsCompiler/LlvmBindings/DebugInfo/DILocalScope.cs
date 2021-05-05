// -----------------------------------------------------------------------
// <copyright file="DILocalScope.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Legal scope for lexical blocks, local variables, and debug info locations</summary>
    public class DILocalScope
        : DIScope
    {
        /// <summary>Gets the parent scope as a <see cref="DILocalScope"/></summary>
        public DILocalScope? LocalScope => Scope as DILocalScope;

        /// <summary>Gets the DISubprogram for this scope</summary>
        /// <remarks>If this scope is a <see cref="DISubProgram"/> then it is returned, otherwise
        /// the scope is walked up to find the subprogram that ultimately owns this scope</remarks>
        public DISubProgram? SubProgram => this is DILexicalBlockBase block ? block.LocalScope?.SubProgram : this as DISubProgram;

        /// <summary>Gets the first non-<see cref="DILexicalBlockFile"/> scope in the chain of parent scopes</summary>
        public DILocalScope FirstNonLexicalBlockFileScope => this is DILexicalBlockFile file ? file.FirstNonLexicalBlockFileScope : ( this );

        internal DILocalScope( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
