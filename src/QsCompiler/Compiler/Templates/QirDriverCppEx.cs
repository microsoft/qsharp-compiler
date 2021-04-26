// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.BondSchemas.Execution;

namespace Microsoft.Quantum.QsCompiler.Templates
{
    public partial class QirDriverCpp
    {
        private EntryPointOperation entryPointOperation;

        public QirDriverCpp(EntryPointOperation entryPoint)
        {
            this.entryPointOperation = entryPoint;
        }
    }
}
