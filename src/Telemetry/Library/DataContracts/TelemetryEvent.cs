// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// A telemetry event
    /// </summary>
    public record TelemetryEvent
    {
        public string Name { get; set; }

        public Dictionary<string, TelemetryEventProperty> Properties { get; set; } = new();

        public TelemetryEvent()
            : this(name: "TelemetryEvent")
        {
        }

        public TelemetryEvent(string name)
        {
            this.Name = name;
        }

        internal void SetProperty(string name, object value, bool isPii, TelemetryPropertyType propertyType)
        {
            this.Properties[name] = new TelemetryEventProperty()
                {
                    IsPii = isPii,
                    PropertyType = propertyType,
                    Value = value,
                };
        }

        public void SetPropertyAsJson(string name, object value, bool isPii = false) =>
            this.SetProperty(name, JsonSerializer.Serialize(value), isPii, TelemetryPropertyType.String);

        public void SetProperty(string name, string value, bool isPii = false) =>
            this.SetProperty(name, value, isPii, TelemetryPropertyType.String);

        public void SetProperty(string name, bool value, bool isPii = false) =>
            this.SetProperty(name, value, isPii, TelemetryPropertyType.Boolean);

        public void SetProperty(string name, DateTime value, bool isPii = false) =>
            this.SetProperty(name, value, isPii, TelemetryPropertyType.DateTime);

        public void SetProperty(string name, double value, bool isPii = false) =>
            this.SetProperty(name, value, isPii, TelemetryPropertyType.Double);

        public void SetProperty(string name, Guid value, bool isPii = false) =>
            this.SetProperty(name, value, isPii, TelemetryPropertyType.Guid);

        public void SetProperty(string name, long value, bool isPii = false) =>
            this.SetProperty(name, value, isPii, TelemetryPropertyType.Long);
    }
}