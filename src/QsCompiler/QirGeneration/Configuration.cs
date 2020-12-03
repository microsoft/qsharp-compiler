// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    public class Configuration
    {
        private static readonly ImmutableDictionary<string, string> ClangInteropTypeMapping =
            ImmutableDictionary.CreateRange(new Dictionary<string, string>
            {
                ["Result"] = "class.RESULT",
                ["Array"] = "struct.quantum::Array",
                ["Callable"] = "struct.quantum::Callable",
                ["TuplePointer"] = "struct.quantum::TupleHeader",
                ["Qubit"] = "class.QUBIT"
            });

        internal readonly ImmutableDictionary<string, string> InteropTypeMapping;

        internal readonly bool GenerateInteropWrappers;

        public readonly string OutputFileName;

        public Configuration(string outputFileName, bool generateInteropWrappers = false, Dictionary<string, string>? interopTypeMapping = null)
        {
            this.GenerateInteropWrappers = generateInteropWrappers;
            this.InteropTypeMapping = interopTypeMapping != null
                ? interopTypeMapping.ToImmutableDictionary()
                : ClangInteropTypeMapping;
            this.OutputFileName = outputFileName;
        }
    }
}
