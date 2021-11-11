// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// Contains non-sensitive/identifiable information from an Exception
    /// </summary>
    public class TelemetryExceptionRecord
    {
        public string? FullName { get; set; }

        public string? TargetSite { get; set; }

        public string? StackTrace { get; set; }
    }
}
