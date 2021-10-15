// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// Apply the PIIData attribute to properties or fields that should be
    /// tagged as PII in the telemetry. The values of these fields will
    /// be hashed with a rotating salt.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PiiDataAttribute : Attribute
    {
        public PiiDataAttribute()
        {
        }
    }

    /// <summary>
    /// Serializes the property as Json
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SerializeJsonAttribute : Attribute
    {
        public SerializeJsonAttribute()
        {
        }
    }

    /// <summary>
    /// Allowed property types for the telemetry events
    /// </summary>
    public enum TelemetryPropertyType
    {
        String,
        DateTime,
        Long,
        Double,
        Guid,
        Boolean,
    }

    /// <summary>
    /// A property that belongs to a telemetry event
    /// </summary>
    public class TelemetryEventProperty
    {
        public object? Value { get; set; }

        public bool IsPii { get; set; }

        public TelemetryPropertyType PropertyType { get; set; }
    }

    /// <summary>
    /// A telemetry event
    /// </summary>
    public class TelemetryEvent
    {
        public string Name { get; set; }

        public Dictionary<string, TelemetryEventProperty> Properties { get; private set; } = new();

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