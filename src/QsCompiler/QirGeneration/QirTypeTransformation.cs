// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
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
            this.SharedState.BuiltType = this.SharedState.Types.QirArray;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBigInt()
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirBigInt;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBool()
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirBool;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnDouble()
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirDouble;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnFunction(ResolvedType it, ResolvedType ot)
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirCallable;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnInt()
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirInt;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnOperation(Tuple<ResolvedType, ResolvedType> _arg1, CallableInformation info)
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirCallable;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnPauli()
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirPauli;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnQubit()
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirQubit;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnRange()
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirRange;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnResult()
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirResult;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnString()
        {
            this.SharedState.BuiltType = this.SharedState.Types.QirString;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnTupleType(ImmutableArray<ResolvedType> ts)
        {
            var elementTypes = ts
                .Select(this.SharedState.LlvmTypeFromQsharpType)
                .Prepend(this.SharedState.Types.QirTupleHeader).ToArray();
            this.SharedState.BuiltType = this.SharedState.Context.CreateStructType(false, elementTypes).CreatePointerType();
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnUnitType()
        {
            // Unit is represented as a null tuple pointer (an empty tuple).
            // This is necessary because "void" in LLVM is not a proper type and can't be included
            // as an element in a struct.
            this.SharedState.BuiltType = this.SharedState.Types.QirTuple;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnUserDefinedType(UserDefinedType udt)
        {
            // User-defined types are represented by their underlying types.
            if (this.SharedState.TryFindUDT(udt.Namespace, udt.Name, out QsCustomType? udtDefinition))
            {
                this.OnType(udtDefinition.Type);
            }
            else
            {
                // This should never happen.
                this.SharedState.BuiltType = this.SharedState.Context.TokenType;
            }
            return QsResolvedTypeKind.InvalidType;
        }
    }
}
