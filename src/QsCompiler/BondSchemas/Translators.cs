// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    using BondQsCompilation = V2.QsCompilation;

    internal static class Translators
    {
        public static SyntaxTree.QsCompilation FromBondSchemaToSyntaxTree<TBond>(TBond bondCompilation)
        {
            switch (bondCompilation)
            {
                case V1.QsCompilation bondCompilationV1:
                    return V1.CompilerObjectTranslator.CreateQsCompilation(bondCompilationV1);

                case BondQsCompilation bondCompilationV2:
                    return V2.CompilerObjectTranslator.CreateQsCompilation(bondCompilationV2);

                default:
                    // TODO: Use a more meaningful message.
                    throw new ArgumentException();
            }
        }

        public static BondQsCompilation FromSyntaxTreeToBondSchema(SyntaxTree.QsCompilation qsCompilation) =>
            V2.BondSchemaTranslator.CreateBondCompilation(qsCompilation);
    }
}
