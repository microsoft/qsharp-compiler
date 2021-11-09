// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
using Ubiquity.NET.Llvm.Types;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal class QirTypeTransformation : TypeTransformation
    {
        private ITypeRef? builtType;
        private readonly Types qirTypes;
        private readonly Func<QsQualifiedName, QsCustomType?> getTypeDeclaration;

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
            this.builtType = null;
            this.OnType(resolvedType);
            return this.builtType ?? throw new NotImplementedException(
                $"Llvm type for {SyntaxTreeToQsharp.Default.ToCode(resolvedType)} could not be constructed.");
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
            this.builtType = this.qirTypes.Array;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBigInt()
        {
            this.builtType = this.qirTypes.BigInt;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBool()
        {
            this.builtType = this.qirTypes.Bool;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnDouble()
        {
            this.builtType = this.qirTypes.Double;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnFunction(ResolvedType it, ResolvedType ot)
        {
            this.builtType = this.qirTypes.Callable;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnInt()
        {
            this.builtType = this.qirTypes.Int;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnOperation(Tuple<ResolvedType, ResolvedType> _arg1, CallableInformation info)
        {
            this.builtType = this.qirTypes.Callable;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnPauli()
        {
            this.builtType = this.qirTypes.Pauli;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnQubit()
        {
            this.builtType = this.qirTypes.Qubit;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnRange()
        {
            this.builtType = this.qirTypes.Range;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnResult()
        {
            this.builtType = this.qirTypes.Result;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnString()
        {
            this.builtType = this.qirTypes.String;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnTupleType(ImmutableArray<ResolvedType> ts)
        {
            this.builtType = this.CreateConcreteTupleType(ts).CreatePointerType();
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnUnitType()
        {
            // Unit is represented as a null tuple pointer (an empty tuple).
            // This is necessary because "void" in LLVM is not a proper type and can't be included
            // as an element in a struct.
            this.builtType = this.qirTypes.Tuple;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnUserDefinedType(UserDefinedType udt)
        {
            // User-defined types are represented by a tuple of their items.
            var udtDefinition = this.getTypeDeclaration(udt.GetFullName());
            if (udtDefinition != null)
            {
                this.builtType = this.LlvmStructTypeFromQsharpType(udtDefinition.Type).CreatePointerType();
            }
            else
            {
                throw new InvalidOperationException("unknown user defined type");
            }

            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
        {
            this.builtType = this.qirTypes.BytePointer;
            return QsResolvedTypeKind.InvalidType;
        }
    }
}
