// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry
{
    public enum TelemetryManagerInstanceKind
    {
        PerThread,
        Application,
    }

    /// <summary>
    /// Handles general telemetry logic that is used accross Microsoft Quantum developer tools.
    /// </summary>
    public static class TelemetryManager
    {
        private static TelemetryManagerInstance? applicationInstance;

        [ThreadStatic]
        private static TelemetryManagerInstance? threadInstance;

        public static TelemetryManagerInstanceKind InstanceKind { get; set; } = TelemetryManagerInstanceKind.Application;

        private static TelemetryManagerInstance? OptionalInstance =>
            InstanceKind == TelemetryManagerInstanceKind.Application ? applicationInstance
                            : threadInstance;

        public static TelemetryManagerInstance Instance =>
            OptionalInstance
            ?? throw new InvalidOperationException("TelemetryManager has not been initialized. Please call TelemetryManager.Initialize before attempting to log anything.");

        public static bool IsTestHostProcess { get; private set; }

        public static DateTime? InitializationTime => OptionalInstance?.InitializationTime;

        public static int TotalEventsCount => OptionalInstance?.TotalEventsCount ?? 0;

        /// <summary>
        /// Handles general telemetry logic that is used accross Microsoft Quantum developer tools.
        /// </summary>
        public static bool IsOutOfProcessInstance => Instance.IsOutOfProcessInstance;

        /// <summary>
        /// True if the TESTMODE argument was passed during initialization.
        /// This is used for some special cases of running unit tests.
        /// </summary>
        public static bool TestMode => OptionalInstance?.Configuration.TestMode ?? false;

        /// <summary>
        /// Compiler directive ENABLE_QDK_TELEMETRY_EXCEPTIONS or DEBUG will set it to true
        /// Environment variable ENABLE_QDK_TELEMETRY_EXCEPTIONS="1" or "0" will override it.
        /// True if exceptions generated at the telemetry later should be thrown.
        /// False if they should be silenced and suppressed. They will still be logged at Trace.
        /// </summary>
        public static bool EnableTelemetryExceptions => OptionalInstance?.Configuration.EnableTelemetryExceptions ?? false;

        /// <summary>
        /// The active configuration of the TelemetryManager.
        /// Should be passed during TelemetryManager.Initialize and not be modified afterwards.
        /// </summary>
        public static TelemetryManagerConfig Configuration => Instance.Configuration;

        /// <summary>
        /// If this value is false, no data will be collected or sent.
        /// Every public method of this class check this variable first and if it's false the method
        /// immediately returns without doing anything.
        /// This property defaults to "false".
        /// This property is automatically set during initialization based on the
        /// Configuration.DefaultTelemetryConsent and based on the environment variables
        /// named in Configuration.TelemetryOptOutVariableName and
        /// Configuration.TelemetryOptInVariableName.
        /// </summary>
        public static bool TelemetryEnabled => Instance.TelemetryEnabled;

        public static event EventHandler<EventProperties>? OnEventLogged
        {
            add
            {
                lock (Instance)
                {
                    Instance.OnEventLogged += value;
                }
            }

            remove
            {
                lock (Instance)
                {
                    Instance.OnEventLogged -= value;
                }
            }
        }

        static TelemetryManager()
        {
            var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
            IsTestHostProcess = string.Equals("testhost", entryAssemblyName, StringComparison.InvariantCultureIgnoreCase);

            // Unit tests usually run in parallel threads so we default
            // the instance kind to be a per thread
            InstanceKind = IsTestHostProcess ? TelemetryManagerInstanceKind.PerThread : TelemetryManagerInstanceKind.Application;
        }

        /// <summary>
        /// Initializes the TelemetryManager with the given configuration.
        /// Must be called before any other method in this class, ideally right after the program starts.
        /// </summary>
        public static IDisposable Initialize(TelemetryManagerConfig configuration, string[]? args = null)
        {
            switch (InstanceKind)
            {
                case TelemetryManagerInstanceKind.PerThread:
                    return InternalInitialize(configuration, args, ref threadInstance);
                case TelemetryManagerInstanceKind.Application:
                    return InternalInitialize(configuration, args, ref applicationInstance);
                default:
                    throw new NotImplementedException();
            }
        }

        private static IDisposable InternalInitialize(TelemetryManagerConfig configuration, string[]? args, ref TelemetryManagerInstance? telemetryManagerInstance)
        {
            if (telemetryManagerInstance == null || telemetryManagerInstance.IsDisposed)
            {
                telemetryManagerInstance = new TelemetryManagerInstance();
            }

            return Instance.Initialize(configuration, args);
        }

        /// <summary>
        /// Kills all threads and exists as soon as possible.
        /// Will await for the time specified in Configuration.MaxTeardownUploadTime
        /// to upload the remaining events.
        /// </summary>
        public static void TearDown() =>
            OptionalInstance?.TearDown();

        /// <summary>
        /// Logs all public properties of the object into an event, respecting the following rules:
        /// - Exception objects will be partially serialized to Json respecting the Configuration.ExceptionLoggingOptions.
        ///   - The name of the event will be "Quantum_{AppId}_.Exception"
        ///   - We only serialize the exception full type name, and based on the ExceptionLoggingOptions we can serialize:
        ///     - TargetSite (the signature of the method that raised the exception)
        ///     - SanitizedStackTrace (the stack trace without the file paths)
        ///   - Other properties are not captured as they could contain customer data or PII.
        /// - Null objects won't be logged.
        /// - Value types will not get logged.
        /// - Other object types will be logged in an event named $"Quantum_{AppId}_{object type name}".
        ///   - Properties with null values will not be logged.
        ///   - Properties of Value types will be converted to values accepted by Aria.
        ///   - Properties with Nullable types will be converted their corresponding non-nullable types accepted by Aria.
        ///   - Properties of Exception types will be partially serialized to Json respecting the Configuration.ExceptionLoggingOptions.
        ///   - Properties of Enum types will be converted to strings.
        ///   - Properties with [PiiData] attribute will be marked as PII and will be hashed.
        ///   - Properties with [SerializeJson] attribute will be serialized as Json.
        ///   - All other property types won't be logged.
        /// </summary>
        public static void LogObject(string eventName, object obj) =>
            Instance.LogObject(eventName, obj);

        /// <summary>
        /// Logs all public properties of the object into an event, respecting the following rules:
        /// - Exception objects will be partially serialized to Json respecting the Configuration.ExceptionLoggingOptions.
        ///   - The name of the event will be "Quantum_{AppId}_.Exception"
        ///   - We only serialize the exception full type name, and based on the ExceptionLoggingOptions we can serialize:
        ///     - TargetSite (the signature of the method that raised the exception)
        ///     - SanitizedStackTrace (the stack trace without the file paths)
        ///   - Other properties are not captured as they could contain customer data or PII.
        /// - Null objects won't be logged.
        /// - Value types will not get logged.
        /// - Other object types will be logged in an event named $"Quantum_{AppId}_{object type name}".
        ///   - Properties with null values will not be logged.
        ///   - Properties of Value types will be converted to values accepted by Aria.
        ///   - Properties with Nullable types will be converted their corresponding non-nullable types accepted by Aria.
        ///   - Properties of Exception types will be partially serialized to Json respecting the Configuration.ExceptionLoggingOptions.
        ///   - Properties of Enum types will be converted to strings.
        ///   - Properties with [PiiData] attribute will be marked as PII and will be hashed.
        ///   - Properties with [SerializeJson] attribute will be serialized as Json.
        ///   - All other property types won't be logged.
        /// </summary>
        public static void LogObject(object obj) =>
            Instance.LogObject(obj);

        /// <summary>
        /// Logs the given Aria event.
        /// Adds an event name prefix if necessary.
        /// </summary>
        public static void LogEvent(EventProperties eventProperties) =>
            Instance.LogEvent(eventProperties);

        /// <summary>
        /// Logs an event with the given eventName, without any additional properties
        /// Adds an event name prefix if necessary.
        /// </summary>
        public static void LogEvent(string eventName) =>
            Instance.LogEvent(eventName);

        public static void SetContext(string name, object value, TelemetryPropertyType propertyType, bool isPii) =>
            Instance.SetContext(name, value, propertyType, isPii);

        public static void SetContext(string name, string? value, bool isPii = false) =>
            Instance.SetContext(name, value, isPii);

        public static void SetContext(string name, int value, bool isPii = false) =>
            Instance.SetContext(name, value, isPii);

        public static void SetContext(string name, long value, bool isPii = false) =>
            Instance.SetContext(name, value, isPii);

        public static void SetContext(string name, Guid value, bool isPii = false) =>
            Instance.SetContext(name, value, isPii);

        public static void SetContext(string name, bool value, bool isPii = false) =>
            Instance.SetContext(name, value, isPii);

        public static void SetContext(string name, double value, bool isPii = false) =>
            Instance.SetContext(name, value, isPii);

        public static void SetContext(string name, DateTime value, bool isPii = false) =>
            Instance.SetContext(name, value, isPii);

        public static void UploadNow() =>
            OptionalInstance?.UploadNow();

        internal static void LogToDebug(string message)
        {
            message = $"{DateTime.Now}: {message}";

            // If this is a OutOfProcess instance, we write it to
            // the standard console such that the main program will
            // receive it and print it to the Debug console
            if (OptionalInstance?.IsOutOfProcessInstance == true)
            {
                Console.WriteLine(message);
            }

            Debug.WriteLine(message, category: "QDK Telemetry");
        }
    }
}
