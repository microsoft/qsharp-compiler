// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CommandLine;

namespace Microsoft.Quantum.Sdk.Tools
{
    public class Options
    {
        [Option(
            'v',
            "verbosity",
            Required = false,
            Default = "Normal",
            HelpText = "Specifies the verbosity of the logged output. Valid values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].")]
        public string? Verbosity { get; set; }

        [Option(
            "QscReferences",
            Required = false,
            HelpText =
                ".NET Core assemblies containing rewrite steps to be passed to the Q# compiler. " +
                "Each reference need to be a string of the form \"(pathToDll, priority)\".")]
        public IEnumerable<string>? QscReferences { get; set; }

        [Option(
            'o',
            "output",
            Required = false,
            HelpText = "Name of the generated config file.")]
        public string? OutputFile { get; set; }
    }
}
