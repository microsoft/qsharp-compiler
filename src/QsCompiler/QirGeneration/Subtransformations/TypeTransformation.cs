// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal class QirTypeTransformation : TypeTransformation<GenerationContext>
    {
        public QirTypeTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options)
            : base(parentTransformation, options)
        {
        }

        // public overrides

        public override QsResolvedTypeKind OnArrayType(ResolvedType b)
        {
            this.SharedState.BuiltType = this.SharedState.Types.Array;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBigInt()
        {
            this.SharedState.BuiltType = this.SharedState.Types.BigInt;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBool()
        {
            this.SharedState.BuiltType = this.SharedState.Types.Bool;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnDouble()
        {
            this.SharedState.BuiltType = this.SharedState.Types.Double;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnFunction(ResolvedType it, ResolvedType ot)
        {
            this.SharedState.BuiltType = this.SharedState.Types.Callable;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnInt()
        {
            this.SharedState.BuiltType = this.SharedState.Types.Int;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnOperation(Tuple<ResolvedType, ResolvedType> _arg1, CallableInformation info)
        {
            this.SharedState.BuiltType = this.SharedState.Types.Callable;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnPauli()
        {
            this.SharedState.BuiltType = this.SharedState.Types.Pauli;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnQubit()
        {
            this.SharedState.BuiltType = this.SharedState.Types.Qubit;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnRange()
        {
            this.SharedState.BuiltType = this.SharedState.Types.Range;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnResult()
        {
            this.SharedState.BuiltType = this.SharedState.Types.Result;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnString()
        {
            this.SharedState.BuiltType = this.SharedState.Types.String;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnTupleType(ImmutableArray<ResolvedType> ts)
        {
            var elementTypes = ts.Select(this.SharedState.LlvmTypeFromQsharpType);
            this.SharedState.BuiltType = this.SharedState.Types.CreateConcreteTupleType(elementTypes).CreatePointerType();
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnUnitType()
        {
            // Unit is represented as a null tuple pointer (an empty tuple).
            // This is necessary because "void" in LLVM is not a proper type and can't be included
            // as an element in a struct.
            this.SharedState.BuiltType = this.SharedState.Types.Tuple;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnUserDefinedType(UserDefinedType udt)
        {
            // User-defined types are represented by a tuple of their items.
            if (this.SharedState.TryGetCustomType(udt.GetFullName(), out QsCustomType? udtDefinition))
            {
                this.SharedState.BuiltType = udtDefinition.Type.Resolution.IsUnitType
                    ? this.SharedState.Types.Tuple
                    : this.SharedState.LlvmStructTypeFromQsharpType(udtDefinition.Type).CreatePointerType();
            }
            else
            {
                throw new InvalidOperationException("unknown user defined type");
            }
            return QsResolvedTypeKind.InvalidType;
        }
    }
}
