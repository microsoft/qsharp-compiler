﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
            // can't be loaded as a compiler reference. If the SDK gains support for packages containing a combination
            // of assemblies that can be loaded as rewrite steps and those that can't, this should be removed.
            //
            // See: https://github.com/microsoft/qsharp-compiler/issues/435
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

            IEnumerable<string> compatibleQscReferences;
            IEnumerable<string> incompatibleQscReferences;
            try
            {
                (compatibleQscReferences, incompatibleQscReferences) = 
                    ParseQscReferences(options.QscReferences ?? Array.Empty<string>());
            }
            catch (Exception ex)
            {
                var errMsg = $"Could not parse the given Qsc references. " +
                    $"Expecting a string of the form \"(pathToDll, priority)\" for each qsc reference.";
                Console.WriteLine(errMsg);
                if (verbose) Console.WriteLine(ex);
                return ReturnCode.INVALID_ARGUMENTS;
            }

            if (verbose)
            {
                foreach (var reference in incompatibleQscReferences)
                {
                    Console.Error.WriteLine($"Skipped incompatible QSC reference: {reference}");
                }
            }
            return WriteConfigFile(options.OutputFile, compatibleQscReferences, verbose)
                ? ReturnCode.SUCCESS
                : ReturnCode.IO_EXCEPTION;
        }

        /// <summary>
        /// Parses the QSC reference strings in the format "(path, priority)" and partitions them into compatible and
        /// incompatible references.
        /// </summary>
        /// <param name="qscReferences">The QSC reference strings to parse.</param>
        /// <returns>A tuple of compatible and incompatible QSC references.</returns>
        private static (IEnumerable<string>, IEnumerable<string>) ParseQscReferences(IEnumerable<string> qscReferences)
        {
            static (string, int) ParseQscReference(string qscRef)
            {
                var pieces = qscRef.Trim().TrimStart('(').TrimEnd(')').Split(',');
                var path = pieces.First().Trim();
                return (path, int.TryParse(pieces.Skip(1).SingleOrDefault(), out var priority) ? priority : 0);
            }
            var compatible = qscReferences
                .Select(ParseQscReference)
                .OrderByDescending(qscRef => qscRef.Item2)
                .Select(qscRef => qscRef.Item1)
                .ToLookup(qscRef => !IncompatibleQscReferences.Contains(Path.GetFileName(qscRef)));
            return (compatible[true], compatible[false]);
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
