// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
using Ubiquity.NET.Llvm.DebugInfo;
using Ubiquity.NET.Llvm.Types;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal class QirTypeTransformation : TypeTransformation
    {
        private ITypeRef? builtLLVMType;
        private IDebugType<ITypeRef, DIType>? builtDebugType;
        private readonly Types qirTypes;
        private readonly Func<QsQualifiedName, QsCustomType?> getTypeDeclaration;
        private DebugInfoBuilder? currDIBuilder;
        internal static string DebugTypeNotSupportedMessage = "This debug type is not yet supported";

        private void ResetConversionVariables()
        {
            this.builtLLVMType = null;
            this.builtDebugType = null;
            this.currDIBuilder = null;
        }

        private IDebugType<ITypeRef, DIType>? GetNotYetSupportedDebugType(ITypeRef llvmType)
        {
            if (this.currDIBuilder != null)
            {
                var dIType = this.currDIBuilder.CreateBasicType(
                    DebugTypeNotSupportedMessage,
                    0,
                    DiTypeKind.Invalid);

                return DebugType.Create(llvmType, dIType);
            }
            else
            {
                return null;
            }
        }

        public QirTypeTransformation(Types types, Func<QsQualifiedName, QsCustomType?> getTypeDecl)
        : base(TransformationOptions.NoRebuild)
        {
            this.qirTypes = types;
            this.getTypeDeclaration = getTypeDecl;
        }

        /// <summary>
        /// Gets the QIR equivalent for a Q# type.
        /// Tuples are represented as QirTuplePointer, arrays as QirArray, and callables as QirCallable.
        /// </summary>
        /// <param name="resolvedType">The Q# type</param>
        /// <returns>The equivalent QIR type</returns>
        internal ITypeRef LlvmTypeFromQsharpType(ResolvedType resolvedType)
        {
            this.ResetConversionVariables();
            this.OnType(resolvedType);
            return this.builtLLVMType ?? throw new NotImplementedException(
                $"Llvm type for {SyntaxTreeToQsharp.Default.ToCode(resolvedType)} could not be constructed.");
        }

        /// <summary>
        /// Gets the DebugInfoType equivalent for a Q# type.
        /// </summary>
        /// <param name="resolvedType">The Q# type</param>
        /// <param name="dIBuilder">The <see cref="DebugInfoBuilder"/> to use to build the <see cref="DebugType"/>.</param>
        internal IDebugType<ITypeRef, DIType>? DebugTypeFromQsharpType(ResolvedType resolvedType, DebugInfoBuilder dIBuilder)
        {
            this.ResetConversionVariables();
            this.currDIBuilder = dIBuilder;
            this.OnType(resolvedType);
            return this.builtDebugType;
        }

        /// <summary>
        /// Gets the QIR equivalent for a Q# type, as a structure.
        /// Tuples are represented as an anonymous LLVM structure type with a TupleHeader as the first element.
        /// Other types are represented as anonymous LLVM structure types with a TupleHeader in the first element
        /// and the "normal" converted type as the second element.
        /// </summary>
        /// <param name="resolvedType">The Q# type</param>
        /// <returns>The equivalent QIR structure type</returns>
        private IStructType LlvmStructTypeFromQsharpType(ResolvedType resolvedType) =>
            resolvedType.Resolution is QsResolvedTypeKind.TupleType tuple
                ? this.CreateConcreteTupleType(tuple.Item)
                : this.CreateConcreteTupleType(new[] { resolvedType });

        /// <summary>
        /// Creates the concrete type of a QIR tuple value that contains items of the given types.
        /// </summary>
        private IStructType CreateConcreteTupleType(IEnumerable<ResolvedType> items) =>
            this.qirTypes.TypedTuple(items.Select(this.LlvmTypeFromQsharpType));

        /* public overrides */

        public override QsResolvedTypeKind OnArrayType(ResolvedType b)
        {
            this.builtLLVMType = this.qirTypes.Array;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBigInt()
        {
            this.builtLLVMType = this.qirTypes.BigInt;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBool()
        {
            this.builtLLVMType = this.qirTypes.Bool;
            if (this.currDIBuilder != null)
            {
                var dIType = this.currDIBuilder.CreateBasicType(
                    TypeNames.Bool,
                    8,
                    DiTypeKind.Boolean);
                this.builtDebugType = DebugType.Create(this.qirTypes.Bool, dIType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnDouble()
        {
            this.builtLLVMType = this.qirTypes.Double;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = new DebugBasicType(
                    this.qirTypes.Double,
                    this.currDIBuilder,
                    TypeNames.Double,
                    DiTypeKind.Float);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnFunction(ResolvedType it, ResolvedType ot)
        {
            this.builtLLVMType = this.qirTypes.Callable;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnInt()
        {
            this.builtLLVMType = this.qirTypes.Int;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = new DebugBasicType(
                    this.qirTypes.Int,
                    this.currDIBuilder,
                    TypeNames.Int,
                    DiTypeKind.Signed);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnOperation(Tuple<ResolvedType, ResolvedType> _arg1, CallableInformation info)
        {
            this.builtLLVMType = this.qirTypes.Callable;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnPauli()
        {
            this.builtLLVMType = this.qirTypes.Pauli;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnQubit()
        {
            this.builtLLVMType = this.qirTypes.Qubit;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnRange()
        {
            this.builtLLVMType = this.qirTypes.Range;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnResult()
        {
            this.builtLLVMType = this.qirTypes.Result;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnString()
        {
            this.builtLLVMType = this.qirTypes.String;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnTupleType(ImmutableArray<ResolvedType> ts)
        {
            this.builtLLVMType = this.CreateConcreteTupleType(ts).CreatePointerType();
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnUnitType()
        {
            // Unit is represented as a null tuple pointer (an empty tuple).
            // This is necessary because "void" in LLVM is not a proper type and can't be included
            // as an element in a struct.
            this.builtLLVMType = this.qirTypes.Tuple;
            if (this.currDIBuilder != null)
            {
                // this type would only be displayed to the user in the context of the type of a function.
                // since the function types are not supported yet, we haven't verified the correct debug type
                // for the unit type.
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnUserDefinedType(UserDefinedType udt)
        {
            // User-defined types are represented by a tuple of their items.
            var udtDefinition = this.getTypeDeclaration(udt.GetFullName());
            if (udtDefinition != null)
            {
                this.builtLLVMType = this.LlvmStructTypeFromQsharpType(udtDefinition.Type).CreatePointerType();
                if (this.currDIBuilder != null)
                {
                    this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
                }
            }
            else
            {
                throw new InvalidOperationException("unknown user defined type");
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
        {
            this.builtLLVMType = this.qirTypes.BytePointer;
            if (this.currDIBuilder != null)
            {
                this.builtDebugType = this.GetNotYetSupportedDebugType(this.builtLLVMType);
            }

            return QsResolvedTypeKind.InvalidType;
        }
    }
}
