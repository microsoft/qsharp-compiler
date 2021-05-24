// -----------------------------------------------------------------------
// <copyright file="DISubProgram.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a SubProgram</summary>
    /// <seealso href="xref:llvm_langref#disubprogram"/>
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "It is already correct 8^)")]
    public class DISubProgram
        : DILocalScope
    {
        /// <summary>Gets the source line associated with this <see cref="DISubProgram"/></summary>
        public uint Line => this.MetadataHandle.DISubprogramGetLine();

        /// <summary>Gets the name of this <see cref="DISubProgram"/></summary>
        public override string Name => this.GetOperandString(2);

        /// <summary>Gets the linkage name of this <see cref="DISubProgram"/></summary>
        public string LinkageName => this.GetOperandString(3);

        /// <summary>Gets the signature of this <see cref="DISubProgram"/></summary>
        public DISubroutineType Signature => this.GetOperand<DISubroutineType>(4)!;

        /// <summary>Gets the <see cref="DICompileUnit"/> that contains this <see cref="DISubProgram"/></summary>
        public DICompileUnit CompileUnit => this.GetOperand<DICompileUnit>(5)!;

        /// <summary>Gets the <see cref="DISubProgram"/> that declares this <see cref="DISubProgram"/></summary>
        public DISubProgram Declaration => this.GetOperand<DISubProgram>(6)!;

        /// <summary>Gets the variables of this <see cref="DISubProgram"/></summary>
        public DILocalVariableArray Variables => new DILocalVariableArray(this.GetOperand<MDTuple>(7)!);

        /// <summary>Gets the type that contains this <see cref="DISubProgram"/>, if any</summary>
        public DIType? ContainingType => this.Operands.Count < 9 ? null : this.GetOperand<DIType>(8);

        /// <summary>Gets the template parameters of this <see cref="DISubProgram"/>, if any</summary>
        public DITemplateParameterArray? TemplateParams => this.Operands.Count < 10 ? null : new DITemplateParameterArray(this.GetOperand<MDTuple>(9)!);

        /// <summary>Gets the exception types this <see cref="DISubProgram"/> can throw</summary>
        // Does the list include exceptions thrown by the complete call graph? or only those explicitly thrown by this function?
        public DITypeArray? ThrownTypes => this.Operands.Count < 11 ? null : new DITypeArray(this.GetOperand<MDTuple>(10));

        internal DISubProgram(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
