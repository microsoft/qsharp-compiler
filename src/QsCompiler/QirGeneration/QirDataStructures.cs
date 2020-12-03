// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR
{
    public class Types
    {
        public readonly ITypeRef Int;
        public readonly ITypeRef Double;
        public readonly ITypeRef Bool;
        public readonly ITypeRef Pauli;

        public readonly IPointerType Result;
        public readonly IPointerType Qubit;
        public readonly IPointerType String;
        public readonly IPointerType BigInt;
        public readonly IPointerType Tuple;
        public readonly IPointerType Array;
        public readonly IPointerType Callable;

        public readonly IStructType Range;
        public readonly IStructType TupleHeader;

        public readonly IFunctionType CallableSignature;

        internal Types(Context context)
        {
            this.Int = context.Int64Type;
            this.Double = context.DoubleType;
            this.Bool = context.BoolType;
            this.Pauli = context.GetIntType(2);

            this.Range = context.CreateStructType("Range", false, context.Int64Type, context.Int64Type, context.Int64Type);
            // It would be nice if TupleHeader were opaque, but it can't be because it appears directly
            // (that is, not as a pointer) in tuple structures, but would have unknown length if it were opaque.
            this.TupleHeader = context.CreateStructType("TupleHeader", false, context.Int32Type);

            this.Result = context.CreateStructType("Result").CreatePointerType();
            this.Qubit = context.CreateStructType("Qubit").CreatePointerType();
            this.String = context.CreateStructType("String").CreatePointerType();
            this.BigInt = context.CreateStructType("BigInt").CreatePointerType();
            this.Tuple = this.TupleHeader.CreatePointerType();
            this.Array = context.CreateStructType("Array").CreatePointerType();
            this.Callable = context.CreateStructType("Callable").CreatePointerType();

            this.CallableSignature = context.GetFunctionType(
                context.VoidType,
                new[] { this.Tuple, this.Tuple, this.Tuple });
        }
    }

    public class Constants
    {
        public readonly Value ResultZero;
        public readonly Value ResultOne;
        public readonly Value PauliI;
        public readonly Value PauliX;
        public readonly Value PauliY;
        public readonly Value PauliZ;
        public readonly Value EmptyRange;

        internal Constants(Context context, BitcodeModule module, Types types)
        {
            Value CreatePauli(string name, ulong idx) =>
                module.AddGlobal(types.Pauli, true, Linkage.External, context.CreateConstant(types.Pauli, idx, false), name);

            this.ResultZero = module.AddGlobal(types.Result, "ResultZero");
            this.ResultOne = module.AddGlobal(types.Result, "ResultOne");
            this.PauliI = CreatePauli("PauliI", 0);
            this.PauliX = CreatePauli("PauliX", 1);
            this.PauliY = CreatePauli("PauliY", 3);
            this.PauliZ = CreatePauli("PauliZ", 2);
            this.EmptyRange = module.AddGlobal(
                types.Range,
                true,
                Linkage.Internal,
                context.CreateNamedConstantStruct(
                    types.Range,
                    context.CreateConstant(0L),
                    context.CreateConstant(1L),
                    context.CreateConstant(-1L)),
                "EmptyRange");
        }
    }
}
