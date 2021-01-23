// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    using BondQsCompilation = V1.QsCompilation;

    internal static class Translators
    {
        public static SyntaxTree.QsCompilation FromBondSchemaToSyntaxTree<TBond>(
            TBond bondCompilation,
            Protocols.Option[]? options = null)
        {
            switch (bondCompilation)
            {
                case V1.QsCompilation bondCompilationV1:
                    return V1.CompilerObjectTranslator.CreateQsCompilation(bondCompilationV1, options);

                case V2.QsCompilation bondCompilationV2:
                    return V2.CompilerObjectTranslator.CreateQsCompilation(bondCompilationV2, options);

                default:
                    // TODO: Use a more meaningful message.
                    throw new ArgumentException();
            }
        }

        public static BondQsCompilation FromSyntaxTreeToBondSchema(SyntaxTree.QsCompilation qsCompilation) =>
            V1.BondSchemaTranslator.CreateBondCompilation(qsCompilation);
    }
}
