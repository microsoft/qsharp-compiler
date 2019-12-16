// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using CommandLine;


namespace Microsoft.Quantum.Sdk.Tools
{
    public static class BuildConfiguration
    {
        public enum ReturnCode
        {
            SUCCESS = 0,
            MISSING_ARGUMENTS = 1,
            INVALID_ARGUMENTS = 2,
            IO_EXCEPTION = 3,
            UNEXPECTED_ERROR = 100
        }

        static int Main(string[] args) =>
            Parser.Default
                .ParseArguments<Options>(args)
                .MapResult(
                    (Options opts) => (int)BuildConfiguration.Generate(opts),
                    (errs => (int)ReturnCode.INVALID_ARGUMENTS)
                );


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

            return BuildConfiguration.WriteConfigFile(options.OutputFile, orderedQscReferences, options.Verbose)
                ? ReturnCode.SUCCESS
                : ReturnCode.IO_EXCEPTION;
        }

        private static bool WriteConfigFile(string configFile, string[] qscReferences, bool verbose = false)
        {
            try 
            { 
                File.WriteAllLines(configFile ?? "qsc.config", qscReferences);
                return true;
            }
            catch (Exception ex)
            {
                var turnOnVerboseMsg = verbose ? "" : "Increase verbosity for detailed error logging.";
                Console.WriteLine($"Failed to generate config file. {turnOnVerboseMsg}");
                if (verbose) Console.WriteLine(ex);
                return false;
            }
        }
    }
}
