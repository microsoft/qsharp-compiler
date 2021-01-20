// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    using CurrentBondQsCompilation = V1.QsCompilation;

    internal static class Translators
    {
        public static SyntaxTree.QsCompilation FromBondSchemaToSyntaxTree<TBond>(TBond bondCompilation)
        {
            switch (bondCompilation)
            {
                case V1.QsCompilation bondCompilationV01:
                    return V1.CompilerObjectTranslator.CreateQsCompilation(bondCompilationV01);

                default:
                    // TODO: Use a more meaningful message.
                    throw new ArgumentException();
            }
        }

        public static CurrentBondQsCompilation FromSyntaxTreeToBondSchema(SyntaxTree.QsCompilation qsCompilation) =>
            V1.BondSchemaTranslator.CreateBondCompilation(qsCompilation);
    }
}
