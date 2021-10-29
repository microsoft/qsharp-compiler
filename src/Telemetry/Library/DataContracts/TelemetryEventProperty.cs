// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// A property that belongs to a telemetry event
    /// </summary>
    public record TelemetryEventProperty
    {
        public object? Value { get; set; }

        public bool IsPii { get; set; }

        public TelemetryPropertyType PropertyType { get; set; }
    }
}