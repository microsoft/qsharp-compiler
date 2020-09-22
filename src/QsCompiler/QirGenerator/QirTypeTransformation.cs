using Llvm.NET.Types;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal class QirTypeTransformation : TypeTransformation<GenerationContext>
    {
        public QirTypeTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options) 
            : base(parentTransformation, options)
        {
        }

        public override QsResolvedTypeKind OnArrayType(ResolvedType b)
        {
            this.SharedState.BuiltType = this.SharedState.QirArray;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBigInt()
        {
            this.SharedState.BuiltType = this.SharedState.QirBigInt;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnBool()
        {
            this.SharedState.BuiltType = this.SharedState.QirBool;
            return QsResolvedTypeKind.InvalidType;
        }

        //public override CallableInformation OnCallableInformation(CallableInformation opInfo)
        //{
        //    return base.OnCallableInformation(opInfo);
        //}

        //public override ResolvedCharacteristics OnCharacteristicsExpression(ResolvedCharacteristics fs)
        //{
        //    return base.OnCharacteristicsExpression(fs);
        //}

        public override QsResolvedTypeKind OnDouble()
        {
            this.SharedState.BuiltType = this.SharedState.QirDouble;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnFunction(ResolvedType it, ResolvedType ot)
        {
            this.SharedState.BuiltType = this.SharedState.QirCallable;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnInt()
        {
            this.SharedState.BuiltType = this.SharedState.QirInt;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnInvalidType()
        {
            return base.OnInvalidType();
        }

        public override QsResolvedTypeKind OnMissingType()
        {
            return base.OnMissingType();
        }

        public override QsResolvedTypeKind OnOperation(Tuple<ResolvedType, ResolvedType> _arg1, CallableInformation info)
        {
            this.SharedState.BuiltType = this.SharedState.QirCallable;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnPauli()
        {
            this.SharedState.BuiltType = this.SharedState.QirPauli;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnQubit()
        {
            this.SharedState.BuiltType = this.SharedState.QirQubit;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnRange()
        {
            this.SharedState.BuiltType = this.SharedState.QirRange;
            return QsResolvedTypeKind.InvalidType;
        }

        //public override QsNullable<Tuple<QsPositionInfo, QsPositionInfo>> OnRangeInformation(QsNullable<Tuple<QsPositionInfo, QsPositionInfo>> r)
        //{
        //    return base.OnRangeInformation(r);
        //}

        public override QsResolvedTypeKind OnResult()
        {
            this.SharedState.BuiltType = this.SharedState.QirResult;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnString()
        {
            this.SharedState.BuiltType = this.SharedState.QirString;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnTupleType(ImmutableArray<ResolvedType> ts)
        {
            var elementTypes = ts.Select(this.GetITypeForType).ToArray();
            this.SharedState.BuiltType = this.SharedState.CurrentContext.CreateStructType(
                false, this.SharedState.QirTupleHeader, elementTypes).CreatePointerType();
            return QsResolvedTypeKind.InvalidType;
        }

        //public override QsResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
        //{
        //    return base.OnTypeParameter(tp);
        //}

        public override QsResolvedTypeKind OnUnitType()
        {
            // Unit is represented as a null tuple pointer (an empty tuple).
            // This is necessary because "void" in LLVM is not a proper type and can't be included
            // as an element in a struct.
            this.SharedState.BuiltType = this.SharedState.QirTuplePointer;
            return QsResolvedTypeKind.InvalidType;
        }

        public override QsResolvedTypeKind OnUserDefinedType(UserDefinedType udt)
        {
            // User-defined types are represented by their underlying types.
            if (this.SharedState.TryFindUDT(udt.Namespace.Value, udt.Name.Value, out QsCustomType udtDefinition))
            {
                this.OnType(udtDefinition.Type);
            }
            else
            {
                // This should never happen.
                this.SharedState.BuiltType = this.SharedState.CurrentContext.TokenType;
            }
            return QsResolvedTypeKind.InvalidType;
        }

        private ITypeRef GetITypeForType(ResolvedType t)
        {
            this.OnType(t);
            return this.SharedState.BuiltType;
        }
    }
}
