// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// Helper class to convert values into types that are accepted by Aria
    /// </summary>
    public static class TypeConversionHelper
    {
        internal static PiiKind ToPiiKind(this bool isPii) =>
            isPii ? PiiKind.GenericData : PiiKind.None;

        private static readonly Dictionary<TelemetryPropertyType, Action<ILogger, string, object, PiiKind>> SetContextActions = new()
        {
            { TelemetryPropertyType.Boolean, (logger, name, value, piiKind) => logger.SetContext(name, (bool)value, piiKind) },
            { TelemetryPropertyType.DateTime, (logger, name, value, piiKind) => logger.SetContext(name, (DateTime)value, piiKind) },
            { TelemetryPropertyType.Double, (logger, name, value, piiKind) => logger.SetContext(name, (double)value, piiKind) },
            { TelemetryPropertyType.Guid, (logger, name, value, piiKind) => logger.SetContext(name, (Guid)value, piiKind) },
            { TelemetryPropertyType.Long, (logger, name, value, piiKind) => logger.SetContext(name, (long)value, piiKind) },
            { TelemetryPropertyType.String, (logger, name, value, piiKind) => logger.SetContext(name, (string)value, piiKind) },
        };

        internal static void SetContext(this ILogger logger, string name, object value, TelemetryPropertyType propertyType, bool isPii) =>
            SetContextActions[propertyType](logger, name, value, isPii.ToPiiKind());

        private static Dictionary<TelemetryPropertyType, Action<EventProperties, string, object, PiiKind>> setPropertyMethods =
        new Dictionary<TelemetryPropertyType, Action<EventProperties, string, object, PiiKind>>()
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

        public static void SetProperty(this EventProperties eventProperties, string name, int value, PiiKind piiKind = PiiKind.None) =>
            eventProperties.SetProperty(name, (long)value, piiKind);

        internal static void SetProperty(this EventProperties eventProperties, string name, object? value, bool isPii, bool serializeJson)
        {
            if (value != null)
            {
                var convertedValue = ConvertValue(value, serializeJson);
                if (convertedValue != null)
                {
                    eventProperties.SetProperty(name, convertedValue.Item2, convertedValue.Item1, isPii.ToPiiKind());
                }
            }
        }

        private static Tuple<TelemetryPropertyType, object> AnyToJson(object? value) =>
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

        internal static Dictionary<Type, TelemetryPropertyType> TypeMap { get; private set; } =
        new Dictionary<Type, TelemetryPropertyType>()
        {
            // Values accepted by Aria
            { typeof(string), TelemetryPropertyType.String },
            { typeof(bool), TelemetryPropertyType.Boolean },
            { typeof(double), TelemetryPropertyType.Double },
            { typeof(long), TelemetryPropertyType.Long },
            { typeof(DateTime), TelemetryPropertyType.DateTime },
            { typeof(Guid), TelemetryPropertyType.Guid },

            // Integer values to be converted to long
            { typeof(byte), TelemetryPropertyType.Long },
            { typeof(sbyte), TelemetryPropertyType.Long },
            { typeof(short), TelemetryPropertyType.Long },
            { typeof(ushort), TelemetryPropertyType.Long },
            { typeof(int), TelemetryPropertyType.Long },
            { typeof(uint), TelemetryPropertyType.Long },
            { typeof(ulong), TelemetryPropertyType.Long },

            // Real values to be converted to double
            { typeof(float), TelemetryPropertyType.Double },
            { typeof(decimal), TelemetryPropertyType.Double },

            // TimeSpan values to be converted to string
            { typeof(TimeSpan), TelemetryPropertyType.String },
        };

        private static Dictionary<Type, Func<object, Tuple<TelemetryPropertyType, object>>> convertValueFunctions =
        new Dictionary<Type, Func<object, Tuple<TelemetryPropertyType, object>>>()
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

        internal static Tuple<TelemetryPropertyType, object>? ConvertValue(object? value, bool serializeJson)
        {
            if (value == null)
            {
                return null;
            }

            if (serializeJson)
            {
                return AnyToJson(value);
            }

            // Note that GetType() will return the underlying type of a Nullable type
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types#how-to-identify-a-nullable-value-type
            Type fromType = value.GetType();

            // Handle F# Option type the same way we would handle a Nullable type in C#
            if (FSharpOptionHelper.IsFSharpOptionType(fromType))
            {
                value = FSharpOptionHelper.GetOptionValue(value, fromType);
                fromType = fromType.GenericTypeArguments[0];
            }

            if (value == null)
            {
                return null;
            }

            if (value is Exception exception)
            {
                return AnyToJson(exception.ToExceptionLogRecord(TelemetryManager.Configuration.ExceptionLoggingOptions));
            }

            if (convertValueFunctions.TryGetValue(fromType, out var conversionFunction))
            {
                return conversionFunction(value);
            }

            if (value is Enum
                || FSharpUnionHelper.IsFSharpUnionType(fromType))
            {
                return AnyToString(value);
            }

            return null;
        }
    }

    /// <summary>
    /// Helper class to cache expensive reflection operations
    /// </summary>
    internal static class ReflectionCache
    {
        private static ConcurrentDictionary<Type, PropertyInfo[]> propertyInfoCache = new ConcurrentDictionary<Type, PropertyInfo[]>();

        public static PropertyInfo[] GetProperties(Type type) =>
            propertyInfoCache.GetOrAdd(type, (t) => t.GetProperties().Where((p) => p.CanRead).ToArray());

        private static ConcurrentDictionary<Type, Type?> genericTypeDefinitionCache = new ConcurrentDictionary<Type, Type?>();

        public static Type? GetGenericTypeDefinition(Type type) =>
            genericTypeDefinitionCache.GetOrAdd(type, (t) => t.IsGenericType ? t.GetGenericTypeDefinition() : null);
    }
}
