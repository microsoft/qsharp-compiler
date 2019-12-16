// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using CommandLine;


namespace Microsoft.Quantum.Sdk.Tools
{
    public enum ReturnCode
    {
        SUCCESS = 0,
        MISSING_ARGUMENTS = 1,
        INVALID_ARGUMENTS = 2,
        ARGUMENT_MISMATCH = 3,
        UNEXPECTED_ERROR = 100
    }


    public static class BuildConfiguration
    {
        public static ReturnCode Generate(Options options)
        {
            if (options == null) return ReturnCode.MISSING_ARGUMENTS;

            var qscReferences = options.QscReferences?.ToArray() ?? new string[0];
            var qscRefPriorities = options.QscReferencePriorities?.ToArray() ?? new int[qscReferences.Length];
            if (qscRefPriorities.Length != qscReferences.Length)
            {
                var errMsg = $"Argument mismatch: " +
                    $"The number of the given Qsc references does not match the number of given priorities. {Environment.NewLine}" +
                    $"Given QscReferences: {String.Join(", ", qscReferences)} {Environment.NewLine}" +
                    $"Given Priorities: {String.Join(", ", qscRefPriorities)}";
                Console.WriteLine(errMsg);
                return ReturnCode.ARGUMENT_MISMATCH;
            }

            var orderedQscReferences = qscReferences.Zip(qscRefPriorities)
                .OrderByDescending(qscRef => qscRef.Second)
                .Select(qscRef => qscRef.First);

            Console.WriteLine($"Qsc references ordered according to their priority:");
            foreach (var qscRef in orderedQscReferences)
            {
                Console.WriteLine(qscRef);
            }

            return ReturnCode.SUCCESS;
        }

        static int Main(string[] args) => 
            Parser.Default
                .ParseArguments<Options>(args)
                .MapResult(
                    (Options opts) => (int)BuildConfiguration.Generate(opts),
                    (errs => (int)ReturnCode.INVALID_ARGUMENTS)
                );
    }
}
