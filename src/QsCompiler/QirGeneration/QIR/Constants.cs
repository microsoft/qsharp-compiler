// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR
{
    /// <summary>
    /// Each class instance contains the QIR constants defined and used
    /// within the compilation unit given upon instantiation.
    /// </summary>
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
