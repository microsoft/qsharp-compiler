// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// Helper class to log exceptions
    /// </summary>
    public static class ExceptionLoggingHelper
    {
        public static TelemetryExceptionRecord ToExceptionLogRecord(this Exception exception, ExceptionLoggingOptions loggingOptions) =>
            new TelemetryExceptionRecord()
            {
                FullName = exception.GetType().FullName,
                TargetSite = loggingOptions.CollectTargetSite ? $"{exception.TargetSite?.DeclaringType?.FullName}: {exception.TargetSite}" : null,
                StackTrace = loggingOptions.CollectSanitizedStackTrace ? SanitizeStackTrace(exception.StackTrace) : null,
            };

        private static readonly Regex StackTraceSanitizerRegex = new Regex(@"^\s*(at)?\s+(?<method>[^)]*\)).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public static string? SanitizeStackTrace(string? originalStackTrace)
        {
            if (originalStackTrace == null)
            {
                return null;
            }

            var sanitizedStackTrace = string.Join(
                                        '\n',
                                        StackTraceSanitizerRegex
                                            .Matches(originalStackTrace)
                                            .Select((match) => match.Groups["method"].Value));
            return sanitizedStackTrace;
        }
    }
}
