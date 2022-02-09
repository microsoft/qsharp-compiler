// -----------------------------------------------------------------------
// <copyright file="LlvmMetadata.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.DebugInfo;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Enumeration to define metadata type kind</summary>
    [SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "It's not a flags enum, get over it...")]
    public enum MetadataKind
    {
        /// <summary>Metadata string</summary>
        MDString = LLVMMetadataKind.LLVMMDStringMetadataKind,

        /// <summary>Constant Value as metadata</summary>
        ConstantAsMetadata = LLVMMetadataKind.LLVMConstantAsMetadataMetadataKind,

        /// <summary>Local value as metadata</summary>
        LocalAsMetadata = LLVMMetadataKind.LLVMLocalAsMetadataMetadataKind,

        /// <summary>Distinct metadata place holder</summary>
        DistinctMDOperandPlaceholder = LLVMMetadataKind.LLVMDistinctMDOperandPlaceholderMetadataKind,

        /// <summary>Metadata tuple</summary>
        MDTuple = LLVMMetadataKind.LLVMMDTupleMetadataKind,

        /// <summary>Debug info location</summary>
        DILocation = LLVMMetadataKind.LLVMDILocationMetadataKind,

        /// <summary>Debug info expression</summary>
        DIExpression = LLVMMetadataKind.LLVMDIExpressionMetadataKind,

        /// <summary>Debug info global variable expression</summary>
        DIGlobalVariableExpression = LLVMMetadataKind.LLVMDIGlobalVariableExpressionMetadataKind,

        /// <summary>Generic Debug info node</summary>
        GenericDINode = LLVMMetadataKind.LLVMGenericDINodeMetadataKind,

        /// <summary>Debug info sub range</summary>
        DISubrange = LLVMMetadataKind.LLVMDISubrangeMetadataKind,

        /// <summary>Debug info enumerator</summary>
        DIEnumerator = LLVMMetadataKind.LLVMDIEnumeratorMetadataKind,

        /// <summary>Debug info basic type</summary>
        DIBasicType = LLVMMetadataKind.LLVMDIBasicTypeMetadataKind,

        /// <summary>Debug info derived type</summary>
        DIDerivedType = LLVMMetadataKind.LLVMDIDerivedTypeMetadataKind,

        /// <summary>Debug info composite type</summary>
        DICompositeType = LLVMMetadataKind.LLVMDICompositeTypeMetadataKind,

        /// <summary>Debug info subroutine type</summary>
        DISubroutineType = LLVMMetadataKind.LLVMDISubroutineTypeMetadataKind,

        /// <summary>Debug info file reference</summary>
        DIFile = LLVMMetadataKind.LLVMDIFileMetadataKind,

        /// <summary>Debug info Compilation Unit</summary>
        DICompileUnit = LLVMMetadataKind.LLVMDICompileUnitMetadataKind,

        /// <summary>Debug info sub program</summary>
        DISubprogram = LLVMMetadataKind.LLVMDISubprogramMetadataKind,

        /// <summary>Debug info lexical block</summary>
        DILexicalBlock = LLVMMetadataKind.LLVMDILexicalBlockMetadataKind,

        /// <summary>Debug info lexical block file</summary>
        DILexicalBlockFile = LLVMMetadataKind.LLVMDILexicalBlockFileMetadataKind,

        /// <summary>Debug info namespace</summary>
        DINamespace = LLVMMetadataKind.LLVMDINamespaceMetadataKind,

        /// <summary>Debug info fro a module</summary>
        DIModule = LLVMMetadataKind.LLVMDIModuleMetadataKind,

        /// <summary>Debug info for a template type parameter</summary>
        DITemplateTypeParameter = LLVMMetadataKind.LLVMDITemplateTypeParameterMetadataKind,

        /// <summary>Debug info for a template value parameter</summary>
        DITemplateValueParameter = LLVMMetadataKind.LLVMDITemplateValueParameterMetadataKind,

        /// <summary>Debug info for a global variable</summary>
        DIGlobalVariable = LLVMMetadataKind.LLVMDIGlobalVariableMetadataKind,

        /// <summary>Debug info for a local variable</summary>
        DILocalVariable = LLVMMetadataKind.LLVMDILocalVariableMetadataKind,

        /// <summary>Debug info for an Objective C style property</summary>
        DIObjCProperty = LLVMMetadataKind.LLVMDIObjCPropertyMetadataKind,

        /// <summary>Debug info for an imported entity</summary>
        DIImportedEntity = LLVMMetadataKind.LLVMDIImportedEntityMetadataKind,

        /// <summary>Debug info for a macro</summary>
        DIMacro = LLVMMetadataKind.LLVMDIMacroMetadataKind,

        /// <summary>Debug info for a macro file</summary>
        DIMacroFile = LLVMMetadataKind.LLVMDIMacroFileMetadataKind,
    }

    /// <summary>Root of the LLVM Metadata hierarchy</summary>
    /// <remarks>In LLVM this is just "Metadata" however that name has the potential
    /// to conflict with the .NET runtime namespace of the same name, so the name
    /// is changed in the .NET bindings to avoid the conflict.</remarks>
    public abstract class LlvmMetadata
    {
        /// <summary>Replace all uses of this descriptor with another</summary>
        /// <param name="other">New descriptor to replace this one with</param>
        public virtual void ReplaceAllUsesWith(LlvmMetadata other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (this.MetadataHandle == default)
            {
                throw new InvalidOperationException();
            }

            this.MetadataHandle.ReplaceAllUsesWith(other.MetadataHandle);
            this.MetadataHandle = default;
        }

        /// <summary>Formats the metadata as a string</summary>
        /// <returns>Metadata as a string</returns>
        public override string ToString()
        {
            if (this.MetadataHandle == default)
            {
                return string.Empty;
            }

            var context = ThreadContextCache.Get();
            var asValue = context.ContextHandle.MetadataAsValue(this.MetadataHandle);
            return asValue.PrintToString();
        }

        /// <summary>Gets a value indicating this metadata's kind</summary>
        public MetadataKind Kind => (MetadataKind)this.MetadataHandle.GetMetadataKind();

        internal LLVMMetadataRef MetadataHandle { get; /*protected*/ set; }

        internal static T? FromHandle<T>(Context context, LLVMMetadataRef handle)
            where T : LlvmMetadata
        {
            return handle == default ? null : (T)context.GetNodeFor(handle);
        }

        internal class InterningFactory
            : HandleInterningMap<LLVMMetadataRef, LlvmMetadata>
        {
            internal InterningFactory(Context context)
                : base(context)
            {
            }

            [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "This is an internal factory method for mapping to a managed type")]
            private protected override LlvmMetadata ItemFactory(LLVMMetadataRef handle)
            {
                // use the native kind value to determine the managed type
                // that should wrap this particular handle
                var kind = (MetadataKind)handle.GetMetadataKind();
                switch (kind)
                {
                case MetadataKind.MDString:
                    return new MDString(handle);

                case MetadataKind.ConstantAsMetadata:
                    return new ConstantAsMetadata(handle);

                case MetadataKind.LocalAsMetadata:
                    return new LocalAsMetadata(handle);

                case MetadataKind.DistinctMDOperandPlaceholder:
                    throw new NotSupportedException(); // return new DistinctMDOperandPlaceHodler( handle );

                case MetadataKind.MDTuple:
                    return new MDTuple(handle);

                case MetadataKind.DILocation:
                    return new DILocation(handle);

                case MetadataKind.DIExpression:
                    return new DIExpression(handle);

                case MetadataKind.DIGlobalVariableExpression:
                    return new DIGlobalVariableExpression(handle);

                case MetadataKind.GenericDINode:
                    return new GenericDINode(handle);

                case MetadataKind.DISubrange:
                    return new DISubRange(handle);

                case MetadataKind.DIEnumerator:
                    return new DIEnumerator(handle);

                case MetadataKind.DIBasicType:
                    return new DIBasicType(handle);

                case MetadataKind.DIDerivedType:
                    return new DIDerivedType(handle);

                case MetadataKind.DICompositeType:
                    return new DICompositeType(handle);

                case MetadataKind.DISubroutineType:
                    return new DISubroutineType(handle);

                case MetadataKind.DIFile:
                    return new DIFile(handle);

                case MetadataKind.DICompileUnit:
                    return new DICompileUnit(handle);

                case MetadataKind.DISubprogram:
                    return new DISubProgram(handle);

                case MetadataKind.DILexicalBlock:
                    return new DILexicalBlock(handle);

                case MetadataKind.DILexicalBlockFile:
                    return new DILexicalBlockFile(handle);

                case MetadataKind.DINamespace:
                    return new DINamespace(handle);

                case MetadataKind.DIModule:
                    return new DIModule(handle);

                case MetadataKind.DITemplateTypeParameter:
                    return new DITemplateTypeParameter(handle);

                case MetadataKind.DITemplateValueParameter:
                    return new DITemplateValueParameter(handle);

                case MetadataKind.DIGlobalVariable:
                    return new DIGlobalVariable(handle);

                case MetadataKind.DILocalVariable:
                    return new DILocalVariable(handle);

                case MetadataKind.DIObjCProperty:
                    return new DIObjCProperty(handle);

                case MetadataKind.DIImportedEntity:
                    return new DIImportedEntity(handle);

                case MetadataKind.DIMacro:
                    return new DIMacro(handle);

                case MetadataKind.DIMacroFile:
                    return new DIMacroFile(handle);

                default:
#pragma warning disable RECS0083 // Intentional trigger to catch changes in underlying LLVM libs
                    throw new NotImplementedException();
#pragma warning restore RECS0083
                }
            }
        }

        private protected LlvmMetadata(LLVMMetadataRef handle)
        {
            this.MetadataHandle = handle;
        }
    }
}
