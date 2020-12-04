// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Quantum.QIR;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    /// <summary>
    /// Class that contains all configurable settings for the QIR emission.
    /// </summary>
    public class Configuration
    {
        private static readonly ImmutableDictionary<string, string> ClangInteropTypeMapping =
            ImmutableDictionary.CreateRange(new Dictionary<string, string>
            {
                [TypeNames.Result] = "class.RESULT",
                [TypeNames.Array] = "struct.quantum::Array",
                [TypeNames.Callable] = "struct.quantum::Callable",
                [TypeNames.Tuple] = "struct.quantum::TupleHeader",
                [TypeNames.Qubit] = "class.QUBIT"
            });

        internal readonly ImmutableDictionary<string, string> InteropTypeMapping;

        /// <summary>
        /// Constructs a class instance storing the configurable settings for QIR emission.
        /// </summary>
        /// <param name="interopTypeMapping">
        /// Optional parameter that maps the name of a QIR type to the name of the corresponding interop type.
        /// The mapping specifies with which type names the QIR types are replaced with
        /// when generating the interop wrappers and entry point(s).
        /// </param>
        public Configuration(Dictionary<string, string>? interopTypeMapping = null)
        {
            this.InteropTypeMapping = interopTypeMapping != null
                ? interopTypeMapping.ToImmutableDictionary()
                : ClangInteropTypeMapping;
        }
    }
}
