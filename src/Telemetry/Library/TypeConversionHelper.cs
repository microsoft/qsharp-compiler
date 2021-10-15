// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// Helper class to convert values into types that are accepted by Aria
    /// </summary>
    internal static class TypeConversionHelper
    {
        internal static PiiKind ToPiiKind(this bool isPii) =>
            isPii ? PiiKind.GenericData : PiiKind.None;

        private static Dictionary<TelemetryPropertyType, Action<ILogger, string, object, PiiKind>> setContextActions = new()
        {
            { TelemetryPropertyType.Boolean, (logger, name, value, piiKind) => logger.SetContext(name, (bool)value, piiKind) },
            { TelemetryPropertyType.DateTime, (logger, name, value, piiKind) => logger.SetContext(name, (DateTime)value, piiKind) },
            { TelemetryPropertyType.Double, (logger, name, value, piiKind) => logger.SetContext(name, (double)value, piiKind) },
            { TelemetryPropertyType.Guid, (logger, name, value, piiKind) => logger.SetContext(name, (Guid)value, piiKind) },
            { TelemetryPropertyType.Long, (logger, name, value, piiKind) => logger.SetContext(name, (long)value, piiKind) },
            { TelemetryPropertyType.String, (logger, name, value, piiKind) => logger.SetContext(name, (string)value, piiKind) },
        };

        internal static void SetContext(this ILogger logger, string name, object value, TelemetryPropertyType propertyType, bool isPii) =>
            setContextActions[propertyType](logger, name, value, isPii.ToPiiKind());

        private static Dictionary<TelemetryPropertyType, Func<JsonElement, object>> fromJsonElementConversion = new()
        {
            { TelemetryPropertyType.Boolean, (jsonElement) => jsonElement.GetBoolean() },
            { TelemetryPropertyType.DateTime, (jsonElement) => jsonElement.GetDateTime() },
            { TelemetryPropertyType.Double, (jsonElement) => jsonElement.GetDouble() },
            { TelemetryPropertyType.Guid, (jsonElement) => jsonElement.GetGuid() },
            { TelemetryPropertyType.Long, (jsonElement) => jsonElement.GetInt64() },
            { TelemetryPropertyType.String, (jsonElement) => jsonElement.GetString() ?? "" },
        };

        internal static object FromJsonElement(JsonElement jsonElement, TelemetryPropertyType propertyType) =>
            fromJsonElementConversion[propertyType](jsonElement);

        private static Dictionary<TelemetryPropertyType, Action<EventProperties, string, object, PiiKind>> setPropertyMethods = new()
        {
            { TelemetryPropertyType.Boolean, (eventProperties, name, value, piiKind) => eventProperties.SetProperty(name, (bool)value, piiKind) },
            { TelemetryPropertyType.DateTime, (eventProperties, name, value, piiKind) => eventProperties.SetProperty(name, (DateTime)value, piiKind) },
            { TelemetryPropertyType.Double, (eventProperties, name, value, piiKind) => eventProperties.SetProperty(name, (double)value, piiKind) },
            { TelemetryPropertyType.Guid, (eventProperties, name, value, piiKind) => eventProperties.SetProperty(name, (Guid)value, piiKind) },
            { TelemetryPropertyType.Long, (eventProperties, name, value, piiKind) => eventProperties.SetProperty(name, (long)value, piiKind) },
            { TelemetryPropertyType.String, (eventProperties, name, value, piiKind) => eventProperties.SetProperty(name, (string)value, piiKind) },
        };

        internal static void SetProperty(this EventProperties eventProperties, string name, object value, TelemetryPropertyType propertyType, PiiKind piiKind) =>
            setPropertyMethods[propertyType](eventProperties, name, value, piiKind);

        internal static void SetProperty(this EventProperties eventProperties, string name, object? value, bool isPii, bool serializeJson)
        {
            if (value != null)
            {
                var convertedValue = TypeConversionHelper.ConvertValue(value.GetType(), value, serializeJson);
                if (convertedValue != null)
                {
                    eventProperties.SetProperty(name, convertedValue.Item2, convertedValue.Item1, isPii.ToPiiKind());
                }
            }
        }

        internal static EventProperties ToEventProperties(this TelemetryEvent telemetryEvent)
        {
            EventProperties eventProperties = new();
            eventProperties.Name = telemetryEvent.Name;
            foreach (var property in telemetryEvent.Properties)
            {
                if (property.Value.Value != null)
                {
                    eventProperties.SetProperty(property.Key, property.Value.Value, property.Value.PropertyType, property.Value.IsPii.ToPiiKind());
                }
            }

            return eventProperties;
        }

        internal static TelemetryEvent ToTelemetryEvent(this EventProperties eventProperties)
        {
            TelemetryEvent telemetryEvent = new();
            telemetryEvent.Name = eventProperties.Name;
            foreach (var property in eventProperties.Properties)
            {
                var isPii = eventProperties.PiiProperties.ContainsKey(property.Key);
                if (!TypeConversionHelper.TypeMap.TryGetValue(property.Value.GetType(), out var propertyType))
                {
                    propertyType = TelemetryPropertyType.String;
                }

                telemetryEvent.SetProperty(property.Key, property.Value, isPii, propertyType);
            }

            return telemetryEvent;
        }

        internal static Tuple<TelemetryPropertyType, object>? ConvertValue(Type fromType, object value, bool serializeJson)
        {
            if (serializeJson)
            {
                return AnyToJson(value);
            }

            if (convertValueFunctions.TryGetValue(fromType, out var conversionFunction))
            {
                return conversionFunction(value);
            }

            if (value is Enum)
            {
                return AnyToString(value);
            }

            return null;
        }

        private static Tuple<TelemetryPropertyType, object> AnyToJson(object value) =>
            new Tuple<TelemetryPropertyType, object>(TelemetryPropertyType.String, JsonSerializer.Serialize(value));

        private static Tuple<TelemetryPropertyType, object> AnyToString(object value) =>
            new Tuple<TelemetryPropertyType, object>(TelemetryPropertyType.String, $"{value}");

        private static Tuple<TelemetryPropertyType, object> AnyToLong(object value) =>
            new Tuple<TelemetryPropertyType, object>(TelemetryPropertyType.Long, Convert.ChangeType(value, typeof(long)));

        private static Tuple<TelemetryPropertyType, object> AnyToDouble(object value) =>
            new Tuple<TelemetryPropertyType, object>(TelemetryPropertyType.Double, Convert.ChangeType(value, typeof(double)));

        private static Tuple<TelemetryPropertyType, object> KeepOriginalType<TOriginalType>(object value) =>
            new Tuple<TelemetryPropertyType, object>(TypeMap[typeof(TOriginalType)], value);

        // The "G" format also output fractional seconds, which is useful for TimeSpans < 1s
        private static Tuple<TelemetryPropertyType, object> TimeSpanToString(object value) =>
            new Tuple<TelemetryPropertyType, object>(TelemetryPropertyType.String, $"{value:G}");

        internal static Dictionary<Type, TelemetryPropertyType> TypeMap { get; private set; } = new()
        {
            { typeof(string), TelemetryPropertyType.String },
            { typeof(bool), TelemetryPropertyType.Boolean },
            { typeof(double), TelemetryPropertyType.Double },
            { typeof(long), TelemetryPropertyType.Long },
            { typeof(DateTime), TelemetryPropertyType.DateTime },
            { typeof(Guid), TelemetryPropertyType.Guid },
        };

        private static Dictionary<Type, Func<object, Tuple<TelemetryPropertyType, object>>> convertValueFunctions = new()
        {
            // Values accepted by Aria
            { typeof(string), KeepOriginalType<string> },
            { typeof(long), KeepOriginalType<long> },
            { typeof(Guid), KeepOriginalType<Guid> },
            { typeof(double), KeepOriginalType<double> },
            { typeof(DateTime), KeepOriginalType<DateTime> },
            { typeof(bool), KeepOriginalType<bool> },

            // Integer values to be converted to long
            { typeof(byte), AnyToLong },
            { typeof(sbyte), AnyToLong },
            { typeof(short), AnyToLong },
            { typeof(ushort), AnyToLong },
            { typeof(int), AnyToLong },
            { typeof(uint), AnyToLong },
            { typeof(ulong), AnyToLong },

            // Real values to be converted to double
            { typeof(float), AnyToDouble },
            { typeof(decimal), AnyToDouble },

            // TimeSpan values to be converted to string
            { typeof(TimeSpan), TimeSpanToString },
        };
    }

    /// <summary>
    /// Helper class to cache expensive reflection operations
    /// </summary>
    internal static class ReflectionCache
    {
        private static ConcurrentDictionary<Type, PropertyInfo[]> propertyInfoCache = new();

        public static PropertyInfo[] GetProperties(Type type) =>
            propertyInfoCache.GetOrAdd(type, (t) => t.GetProperties().Where((p) => p.CanRead).ToArray());
    }
}