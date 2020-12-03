// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    /// <summary>
    /// Class that contains all configurable settings for the QIR emission.
    /// </summary>
    public class Configuration
    {
        private static readonly ImmutableDictionary<string, string> ClangInteropTypeMapping =
            ImmutableDictionary.CreateRange(new Dictionary<string, string>
            {
                ["Result"] = "class.RESULT",
                ["Array"] = "struct.quantum::Array",
                ["Callable"] = "struct.quantum::Callable",
                ["Tuple"] = "struct.quantum::TupleHeader",
                ["Qubit"] = "class.QUBIT"
            });

        internal readonly ImmutableDictionary<string, string> InteropTypeMapping;

        /// <summary>
        /// Constructs a class instance storing the configurable settings for QIR emission.
        /// </summary>
        /// <param name="interopTypeMapping"></param>
        public Configuration(Dictionary<string, string>? interopTypeMapping = null)
        {
            this.InteropTypeMapping = interopTypeMapping != null
                ? interopTypeMapping.ToImmutableDictionary()
                : ClangInteropTypeMapping;
        }
    }
}
