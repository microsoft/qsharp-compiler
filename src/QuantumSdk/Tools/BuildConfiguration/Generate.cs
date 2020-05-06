// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Microsoft.Quantum.Sdk.Tools
{
    public static partial class BuildConfiguration
    {
        /// <summary>
        /// Assembly names that are not compatible with the Q# compiler when loaded as a plugin.
        /// </summary>
        private static readonly IReadOnlyCollection<string> IncompatibleQscReferences = new[]
        {
            // TODO: This is to work around an assembly that is included with the C# generation package, but which
            // shouldn't be loaded as a compiler reference. If the Quantum SDK allows packages to explicitly specify
            // which assemblies should be loaded for rewrite steps, instead of loading all of the assemblies in the
            // package, then this can be removed.
            "Microsoft.Quantum.CsharpGeneration.EntryPointDriver.dll"
        };
        
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

            ILookup<bool, string> qscReferences;
            try
            {
                qscReferences = (options.QscReferences ?? Array.Empty<string>())
                    .Select(ParseQscReference)
                    .OrderByDescending(qscRef => qscRef.Item2)
                    .Select(qscRef => qscRef.Item1)
                    .ToLookup(qscRef => IncompatibleQscReferences.Contains(Path.GetFileName(qscRef)));
            }
            catch (Exception ex)
            {
                var errMsg = $"Could not parse the given Qsc references. " +
                    $"Expecting a string of the form \"(pathToDll, priority)\" for each qsc reference.";
                Console.WriteLine(errMsg);
                if (verbose) Console.WriteLine(ex);
                return ReturnCode.INVALID_ARGUMENTS;
            }

            var incompatible = qscReferences[true];
            foreach (var reference in incompatible)
            {
                Console.Error.WriteLine($"Ignored incompatible reference {reference}.");
            }
            
            var compatible = qscReferences[false];
            return WriteConfigFile(options.OutputFile, compatible, verbose)
                ? ReturnCode.SUCCESS
                : ReturnCode.IO_EXCEPTION;
        }

        /// <summary>
        /// Work in progress: 
        /// The signature and output of this method will change in the future. 
        /// </summary>
        private static bool WriteConfigFile(string configFile, IEnumerable<string> qscReferences, bool verbose = false)
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
