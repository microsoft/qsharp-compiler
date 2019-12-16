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
        UNEXPECTED_ERROR = 100
    }


    public static class BuildConfiguration
    {
        public static ReturnCode Generate(Options options)
        {
            if (options == null) return ReturnCode.MISSING_ARGUMENTS;
            (string, int) ParseQscReference(string qscRef)
            {
                var pieces = qscRef.Trim().TrimStart('(').TrimEnd(')').Split(',');
                var path = pieces.First().Trim();
                return (path, Int32.TryParse(pieces.Skip(1).SingleOrDefault(), out var priority) ? priority : 0);
            }

            var qscReferences = options.QscReferences?.ToArray() ?? new string[0];
            var orderedQscReferences = new string[0];
            try
            {
                orderedQscReferences = qscReferences
                    .Select(ParseQscReference)
                    .OrderByDescending(qscRef => qscRef.Item2)
                    .Select(qscRef => qscRef.Item1).ToArray();
            }
            catch
            {
                var errMsg = $"Could not parse the given Qsc references. " +
                    $"Expecting a string of the form \"(pathToDll, priority)\" for each qsc reference.";
                Console.WriteLine(errMsg);
                return ReturnCode.INVALID_ARGUMENTS;
            }

            //if (qscReferences.Any())
            //{ Console.WriteLine($"The given qsc references will be loaded in the following order:"); }
            //foreach (var qscRef in orderedQscReferences)
            //{
            //    Console.WriteLine(qscRef);
            //}

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
