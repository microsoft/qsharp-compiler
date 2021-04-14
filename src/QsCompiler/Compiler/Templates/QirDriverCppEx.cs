// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint;

namespace Microsoft.Quantum.QsCompiler.Templates
{
    public partial class QirDriverCpp
    {
        private EntryPointOperationCpp entryPointOperation;

        public QirDriverCpp(EntryPointOperation entryPoint)
        {
            this.entryPointOperation = new EntryPointOperationCpp(entryPoint);
        }
    }
}
