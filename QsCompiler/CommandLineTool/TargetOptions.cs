// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using CommandLine;


namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    /// command line options for Q# build targets
    public class TargetOptions
    {
        [Option('v', "verbose", Required = false, Default = false,
        HelpText = "Specifies whether to execute in verbose mode.")]
        public bool Verbose { get; set; }

        [Option('i', "input", Required = true,
        HelpText = "Path to the Q# binary file(s) to process.")]
        public IEnumerable<string> Input { get; set; }

        [Option('o', "output", Required = false,
        HelpText = "Destination folder where the process output will be generated.")]
        public string OutputFolder { get; set; }

        [Option('l', "log", Required = false,
        HelpText = "Destination folder where the process log will be generated.")]
        public string LogFolder { get; set; }

        [Option('n', "noWarn", Required = false, Default = new int[0],
        HelpText = "Warnings with the given code(s) will be ignored.")]
        public IEnumerable<int> NoWarn { get; set; }
    }
}
