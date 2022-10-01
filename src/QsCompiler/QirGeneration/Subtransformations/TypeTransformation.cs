// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LlvmBindings.Types;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal class QirTypeTransformation
    {
        private readonly QirGlobalType globalType;

        public QirTypeTransformation(Types types, Func<QsQualifiedName, QsCustomType?> getTypeDecl) =>
            this.globalType = new QirGlobalType(types, getTypeDecl);

        internal ITypeRef LlvmTypeFromQsharpType(ResolvedType resolvedType) =>
            this.globalType.LlvmTypeFromQsharpType(resolvedType);

        private class QirGlobalType : TypeTransformation
        {
            private protected ITypeRef? BuiltType { get; set; }

            private protected Types QirTypes { get; }

            private protected Func<QsQualifiedName, QsCustomType?> TypeDeclaration { get; }

            public QirGlobalType(Types types, Func<QsQualifiedName, QsCustomType?> getTypeDecl)
            : base(TransformationOptions.NoRebuild)
            {
                this.QirTypes = types;
                this.TypeDeclaration = getTypeDecl;
            }

            /// <summary>
            /// Gets the QIR equivalent for a Q# type.
            /// Tuples are represented as QirTuplePointer, arrays as QirArray, and callables as QirCallable.
            /// </summary>
            /// <param name="resolvedType">The Q# type</param>
            /// <returns>The equivalent QIR type</returns>
            internal ITypeRef LlvmTypeFromQsharpType(ResolvedType resolvedType)
            {
                this.BuiltType = null;
                this.OnType(resolvedType);
                return this.BuiltType ?? throw new NotImplementedException(
                    $"Llvm type for {SyntaxTreeToQsharp.Default.ToCode(resolvedType)} could not be constructed.");
            }

            /// <summary>
            /// Creates a pointer to the concrete type of a QIR tuple value that contains items of the given types.
            /// </summary>
            private protected virtual ITypeRef CreateConcreteTupleType(IEnumerable<ResolvedType> items) =>
                this.QirTypes.TypedTuple(items.Select(this.LlvmTypeFromQsharpType)).CreatePointerType();

            /* public overrides */

            public override QsResolvedTypeKind OnArrayType(ResolvedType b)
            {
                this.BuiltType = this.QirTypes.Array;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnBigInt()
            {
                this.BuiltType = this.QirTypes.BigInt;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnBool()
            {
                this.BuiltType = this.QirTypes.Bool;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnDouble()
            {
                this.BuiltType = this.QirTypes.Double;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnFunction(ResolvedType it, ResolvedType ot)
            {
                this.BuiltType = this.QirTypes.Callable;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnInt()
            {
                this.BuiltType = this.QirTypes.Int;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnOperation(Tuple<ResolvedType, ResolvedType> _arg1, CallableInformation info)
            {
                this.BuiltType = this.QirTypes.Callable;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnPauli()
            {
                this.BuiltType = this.QirTypes.Pauli;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnQubit()
            {
                this.BuiltType = this.QirTypes.Qubit;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnRange()
            {
                this.BuiltType = this.QirTypes.Range;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnResult()
            {
                this.BuiltType = this.QirTypes.Result;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnString()
            {
                this.BuiltType = this.QirTypes.String;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnTupleType(ImmutableArray<ResolvedType> ts)
            {
                this.BuiltType = this.CreateConcreteTupleType(ts);
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnUnitType()
            {
                // Unit is represented as a null tuple pointer (an empty tuple).
                // This is necessary because "void" in LLVM is not a proper type and can't be included
                // as an element in a struct.
                this.BuiltType = this.QirTypes.Tuple;
                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnUserDefinedType(UserDefinedType udt)
            {
                // User-defined types are represented by a tuple of their items.
                var udtDefinition = this.TypeDeclaration(udt.GetFullName());
                if (udtDefinition != null)
                {
                    var resolvedType = udtDefinition.Type;
                    this.BuiltType = resolvedType.Resolution is QsResolvedTypeKind.TupleType tuple
                        ? this.CreateConcreteTupleType(tuple.Item)
                        : this.CreateConcreteTupleType(new[] { resolvedType });
                }
                else
                {
                    throw new InvalidOperationException("unknown user defined type");
                }

                return QsResolvedTypeKind.InvalidType;
            }

            public override QsResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
            {
                this.BuiltType = this.QirTypes.BytePointer;
                return QsResolvedTypeKind.InvalidType;
            }
        }
    }
}
