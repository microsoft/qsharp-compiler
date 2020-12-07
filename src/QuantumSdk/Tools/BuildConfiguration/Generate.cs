// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text;


namespace Microsoft.Quantum.Sdk.Tools
{
    public static partial class BuildConfiguration
    {
        /// <summary>
        /// Generates a suitable configuration file for the Q# compiler based on the given options. 
        /// Encountered exceptions are logged to the console, and indicated by the returned status. 
        /// </summary>
        public static ReturnCode Generate(Options options)
        {
            if (options == null) return ReturnCode.MISSING_ARGUMENTS;
            var verbose =
                "detailed".Equals(options.Verbosity, StringComparison.InvariantCultureIgnoreCase) ||
                "d".Equals(options.Verbosity, StringComparison.InvariantCultureIgnoreCase) ||
                "diagnostic".Equals(options.Verbosity, StringComparison.InvariantCultureIgnoreCase) ||
                "diag".Equals(options.Verbosity, StringComparison.InvariantCultureIgnoreCase);

            static (string, int) ParseQscReference(string qscRef)
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
            catch (Exception ex)
            {
                var errMsg = $"Could not parse the given Qsc references. " +
                    $"Expecting a string of the form \"(pathToDll, priority)\" for each qsc reference.";
                Console.WriteLine(errMsg);
                if (verbose) Console.WriteLine(ex);
                return ReturnCode.INVALID_ARGUMENTS;
            }

            return BuildConfiguration.WriteConfigFile(options.OutputFile, orderedQscReferences, verbose)
                ? ReturnCode.SUCCESS
                : ReturnCode.IO_EXCEPTION;
        }

        /// <summary>
        /// Work in progress: 
        /// The signature and output of this method will change in the future. 
        /// </summary>
        private static bool WriteConfigFile(string? configFile, string[] qscReferences, bool verbose = false)
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
