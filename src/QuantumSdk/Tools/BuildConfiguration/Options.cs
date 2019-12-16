// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using CommandLine;


namespace Microsoft.Quantum.Sdk.Tools
{
    public class Options
    {

        [Option("QscReferences", Required = false,
        HelpText = ".NET Core assemblies containing rewrite steps to be passed to the Q# compiler. " +
            "Each reference need to be a string of the form \"(pathToDll, priority)\".")]
        public IEnumerable<string> QscReferences { get; set; }
    }
}
