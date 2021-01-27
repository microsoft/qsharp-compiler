// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommandLine;

namespace Microsoft.Quantum.Sdk.Tools
{
    public static partial class BuildConfiguration
    {
        public enum ReturnCode
        {
            SUCCESS = 0,
            MISSING_ARGUMENTS = 1,
            INVALID_ARGUMENTS = 2,
            IO_EXCEPTION = 3,
            UNEXPECTED_ERROR = 100
        }

        private static int Main(string[] args) =>
            Parser.Default
                .ParseArguments<Options>(args)
                .MapResult(
                    (Options opts) => (int)BuildConfiguration.Generate(opts),
                    errs => (int)ReturnCode.INVALID_ARGUMENTS);
    }
}
