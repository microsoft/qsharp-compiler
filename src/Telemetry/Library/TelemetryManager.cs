// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Applications.Events;
using Microsoft.Quantum.Telemetry.OutOfProcess;

namespace Microsoft.Quantum.Telemetry
{
    public class TelemetryManagerHandle : IDisposable
    {
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    TelemetryManager.TearDown();
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class TelemetryTearDown
    {
        public TimeSpan? TotalRunningTime { get; set; }

        public int TotalEventsCount { get; set; }

        public TelemetryTearDown(TimeSpan? totalRunningTime, int totalEventsCount)
        {
            this.TotalEventsCount = totalEventsCount;
            this.TotalRunningTime = totalRunningTime;
        }
    }

    /// <summary>
    /// Handles general telemetry logic that is used accross Microsoft Quantum developer tools.
    /// </summary>
    public static class TelemetryManager
    {
        private const string TOKEN = "55aee962ee9445f3a86af864fc0fa766-48882422-3439-40de-8030-228042bd9089-7794";
        public const string OUTOFPROCESSUPLOADARG = "--OUT_OF_PROCESS_TELEMETRY_UPLOAD";
        public const string TESTMODE = "--TELEMETRY_TEST_MODE";

        private static ILogger? telemetryLogger;
        private static bool tearDownLogManager = false;

        public static DateTime? InitializationTime { get; private set; }

        public static int TotalEventsCount { get; private set; } = 0;

        /// <summary>
        /// Handles general telemetry logic that is used accross Microsoft Quantum developer tools.
        /// </summary>
        public static bool IsOutOfProcessInstance { get; private set; } = false;

        /// <summary>
        /// True if the TESTMODE argument was passed during initialization.
        /// This is used for some special cases of running unit tests.
        /// </summary>
        public static bool TestMode => Configuration.TestMode;

        #if DEBUG
        public static bool DebugMode => true;
        #else
        public static bool DebugMode => false;
        #endif

        /// <summary>
        /// Compiler directive ENABLE_QDK_TELEMETRY_EXCEPTIONS or DEBUG will set it to true
        /// Environment variable ENABLE_QDK_TELEMETRY_EXCEPTIONS="1" or "0" will override it.
        /// True if exceptions generated at the telemetry later should be thrown.
        /// False if they should be silenced and suppressed. They will still be logged at Trace.
        /// </summary>
        public static bool EnableTelemetryExceptions => Configuration.EnableTelemetryExceptions;

        /// <summary>
        /// The active configuration of the TelemetryManager.
        /// Should be passed during TelemetryManager.Initialize and not be modified afterwards.
        /// </summary>
        public static TelemetryManagerConfig Configuration { get; private set; }

        /// <summary>
        /// If this value is true, no data will be collected or sent.
        /// Every public method of this class check this variable first and if it's true the method
        /// immediately returns without doing anything.
        /// This property defaults to "false".
        /// This property is automatically set to true during initialization if the environment variable
        /// named in Configuration.HostingEnvironmentVariableName has a value of "1".
        /// </summary>
        public static bool TelemetryOptOut { get; private set; } = false;

        public static event EventHandler<EventProperties>? OnEventLogged;

        static TelemetryManager()
        {
            Configuration = new TelemetryManagerConfig();
            Configuration.EnableTelemetryExceptions = GetEnableTelemetryExceptions(false);
        }

        /// <summary>
        /// Initializes the TelemetryManager with the given configuration.
        /// Must be called before any other method in this class, ideally right after the program starts.
        /// </summary>
        public static IDisposable Initialize(TelemetryManagerConfig configuration, string[]? args = null)
        {
            InitializationTime = DateTime.Now;

            IsOutOfProcessInstance = args?.Contains(OUTOFPROCESSUPLOADARG) == true;

            configuration.OutOfProcessUpload = configuration.OutOfProcessUpload
                                               || IsOutOfProcessInstance;
            configuration.TestMode = (args?.Contains(TESTMODE) == true)
                                     || configuration.TestMode
                                     || "1".Equals(Environment.GetEnvironmentVariable(Configuration.EnableTelemetryTestVariableName));
            configuration.EnableTelemetryExceptions = GetEnableTelemetryExceptions(configuration.EnableTelemetryExceptions);

            Configuration = configuration;

            TelemetryOptOut = GetTelemetryOptOut(configuration);

            CheckAndRunSafe(
                () =>
                {
                    if (telemetryLogger != null)
                    {
                        throw new InvalidOperationException("The TelemetryManager was already initialized");
                    }

                    if (configuration.OutOfProcessUpload & args == null)
                    {
                        throw new ArgumentNullException(nameof(args), "The application start arguments array must be passed when using OutOfProcessUpload");
                    }

                    if (configuration.OutOfProcessUpload & !IsOutOfProcessInstance)
                    {
                        telemetryLogger = new OutOfProcessLogger(configuration);
                    }
                    else if (configuration.TestMode)
                    {
                        telemetryLogger = new DebugConsoleLogger();
                    }
                    else
                    {
                        InitializeLogManager();
                        telemetryLogger = LogManager.GetLogger(TOKEN, out _);
                    }

                    if (IsOutOfProcessInstance)
                    {
                        // After completing the next line, the current process
                        // will exit with exit code 0.
                        new OutOfProcessServer(configuration, Console.In).RunAndExit();
                    }
                    else
                    {
                        SetStartupContext();

                        if (Configuration.SendTelemetryInitializedEvent)
                        {
                            LogEvent("TelemetryInitialized");
                        }
                    }
                },
                initializingOrTearingDown: true);

            return new TelemetryManagerHandle();
        }

        private static void SetStartupContext()
        {
            SetContext("AppId", Configuration.AppId);
            SetContext("AppVersion", Assembly.GetEntryAssembly()!.GetName().Version!.ToString());
            SetContext("DeviceId", GetDeviceId(), isPii: true);
            SetContext("SessionId", Guid.NewGuid().ToString());
            SetContext("HostingEnvironment", Environment.GetEnvironmentVariable(Configuration.HostingEnvironmentVariableName));
            SetContext("OutOfProcessUpload", Configuration.OutOfProcessUpload);
            SetContext("Timezone", TimeZoneInfo.Local.BaseUtcOffset.ToString(@"hh\:mm"));
        }

        private static void InitializeLogManager()
        {
            LogManager.Start(new LogConfiguration()
            {
                MaxTeardownUploadTime = (int)(IsOutOfProcessInstance ? Configuration.OutOfProcessMaxTeardownUploadTime :
                                              Configuration.MaxTeardownUploadTime).TotalMilliseconds,
            });

            if (TestMode || DebugMode)
            {
                SubscribeTelemetryEventsForDebugging();
            }

            LogManager.SetTransmitProfile("RealTime");
            tearDownLogManager = true;
        }

        /// <summary>
        /// Kills all threads and exists as soon as possible.
        /// Will await for the time specified in Configuration.MaxTeardownUploadTime
        /// to upload the remaining events.
        /// </summary>
        public static void TearDown() =>
            CheckAndRunSafe(
                () =>
                {
                    if (telemetryLogger != null
                        && Configuration.SendTelemetryTearDownEvent
                        && !IsOutOfProcessInstance)
                    {
                        LogObject(new TelemetryTearDown(DateTime.Now - InitializationTime, TotalEventsCount + 1));
                    }

                    if (telemetryLogger is OutOfProcessLogger outOfProcessLogger)
                    {
                        outOfProcessLogger.Quit();
                        if (DebugMode)
                        {
                            outOfProcessLogger.AwaitForExternalProcessExit();
                        }
                    }

                    telemetryLogger = null;
                    TotalEventsCount = 0;
                    InitializationTime = null;

                    if (tearDownLogManager)
                    {
                        LogManager.Teardown();
                    }
                },
                initializingOrTearingDown: true);

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
            InternalLogObject(eventName, obj);

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
            InternalLogObject(null, obj);

        private static void InternalLogObject(string? eventName, object obj) =>
            CheckAndRunSafe(() =>
            {
                if (obj == null || obj is ValueType)
                {
                    return;
                }

                if (obj is EventProperties eventProp)
                {
                    LogEvent(eventProp);
                    return;
                }

                EventProperties eventProperties = new EventProperties();

                // We don't log exception fields as they can potentially contain customer data
                if (eventName == null && obj is Exception exception)
                {
                    obj = exception.ToExceptionLogRecord(Configuration.ExceptionLoggingOptions);
                    eventName = $"{Configuration.EventNamePrefix}_{Configuration.AppId}_Exception";
                }

                var type = obj.GetType();
                eventProperties.Name = eventName ?? $"{Configuration.EventNamePrefix}_{Configuration.AppId}_{type.Name}";
                var properties = ReflectionCache.GetProperties(type);
                foreach (var property in properties)
                {
                    var isPii = property.GetCustomAttribute<PiiDataAttribute>() != null;
                    var serializeJson = property.GetCustomAttribute<SerializeJsonAttribute>() != null;
                    var value = property.GetValue(obj);
                    eventProperties.SetProperty(property.Name, value, isPii, serializeJson);
                }

                LogEvent(eventProperties);
            });

        /// <summary>
        /// Logs the given Aria event.
        /// Adds an event name prefix if necessary.
        /// </summary>
        public static void LogEvent(EventProperties eventProperties) =>
            CheckAndRunSafe(() =>
            {
                TotalEventsCount++;

                var eventNamePrefix = $"{Configuration.EventNamePrefix}_{Configuration.AppId}_";
                if (!eventProperties.Name.StartsWith(eventNamePrefix))
                {
                    eventProperties.Name = eventNamePrefix + eventProperties.Name;
                }

                if (!eventProperties.Properties.ContainsKey("TimestampUTC"))
                {
                    eventProperties.SetProperty("TimestampUTC", DateTime.UtcNow);
                }

                telemetryLogger!.LogEvent(eventProperties);

                OnEventLogged?.Invoke(telemetryLogger, eventProperties);

                if (DebugMode || TestMode)
                {
                    LogToDebug($"{telemetryLogger!.GetType().Name} logged event {eventProperties.Name}");
                }
            });

        /// <summary>
        /// Logs an event with the given eventName, without any additional properties
        /// Adds an event name prefix if necessary.
        /// </summary>
        public static void LogEvent(string eventName) =>
            CheckAndRunSafe(() => LogEvent(new EventProperties() { Name = ValidateEventName(eventName) }));

        public static void SetContext(string name, object value, TelemetryPropertyType propertyType, bool isPii) =>
            CheckAndRunSafe(() => telemetryLogger!.SetContext(name, value, propertyType, isPii));

        public static void SetContext(string name, string? value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, int value, bool isPii = false) =>
            SetContext(name, (long)value, isPii);

        public static void SetContext(string name, long value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, Guid value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, bool value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, double value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, DateTime value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public static void UploadNow() =>
            CheckAndRunSafe(() => LogManager.UploadNow());

        private static void CheckAndRunSafe(Action action, bool initializingOrTearingDown = false)
        {
            if (TelemetryOptOut)
            {
                return;
            }

            try
            {
                if (telemetryLogger == null && !initializingOrTearingDown)
                {
                    throw new InvalidOperationException("TelemetryManager has not been initialized. Please call TelemetryManager.Initialize before attempting to log anything.");
                }

                action();
            }
            catch (Exception exception)
            {
                var message = $"QDK Telemetry error. Exception: {exception.ToString()}";

                Trace.TraceError(message);
                Console.Error.WriteLine(message);

                if (DebugMode || TestMode)
                {
                    LogToDebug(message);
                }

                if (EnableTelemetryExceptions)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Return an Id for this device, namely, the first non-empty MAC address it can find across all network interfaces (if any).
        /// </summary>
        private static string? GetDeviceId() =>
            NetworkInterface.GetAllNetworkInterfaces()?
                .Select(n => n?.GetPhysicalAddress()?.ToString())
                .Where(address => address != null && !string.IsNullOrWhiteSpace(address) && !address.StartsWith("000000"))
                .FirstOrDefault();

        private static bool GetTelemetryOptOut(TelemetryManagerConfig configuration) =>
            Environment.GetEnvironmentVariable(configuration.TelemetryOptOutVariableName) == "1";

        private static bool GetEnableTelemetryExceptions(bool enableTelemetryExceptions)
        {
            #if ENABLE_QDK_TELEMETRY_EXCEPTIONS || DEBUG
            enableTelemetryExceptions = true;
            #endif

            var envVarValue = Environment.GetEnvironmentVariable(Configuration.EnableTelemetryExceptionsVariableName);
            switch (envVarValue)
            {
                case "0":
                    enableTelemetryExceptions = false;
                    break;
                case "1":
                    enableTelemetryExceptions = true;
                    break;
            }

            return enableTelemetryExceptions;
        }

        private static readonly Regex EventNameValidationRegex = new Regex("^[a-zA-Z0-9]{3,30}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string ValidateEventName(string value)
        {
            if (!EventNameValidationRegex.IsMatch(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value should only contains letters or numbers and have 3-30 characters");
            }

            return value;
        }

        internal static void LogToDebug(string message)
        {
            message = $"{DateTime.Now}: {message}";

            // If this is a OutOfProcess instance, we write it to
            // the standard console such that the main program will
            // receive it and print it to the Debug console
            if (IsOutOfProcessInstance)
            {
                Console.WriteLine(message);
            }

            Debug.WriteLine(message, category: "QDK Telemetry");
        }

        private static void SubscribeTelemetryEventsForDebugging()
        {
            var telemetryEvents = LogManager.GetTelemetryEvents(out _);
            if (telemetryEvents != null)
            {
                telemetryEvents.EventsDropped += (sender, e) => LogToDebug($"{e.GetType().Name} {e.EventsCount} {e.DroppedReason} {e.DroppedDetails}");
                telemetryEvents.EventsRejected += (sender, e) => LogToDebug($"{e.GetType().Name} {e.EventsCount} {e.RejectedReason} {e.RejectedDetails}");
                telemetryEvents.EventsRetrying += (sender, e) => LogToDebug($"{e.GetType().Name} {e.EventsCount} {e.RetryReason} {e.RetryDetails}");
                telemetryEvents.EventsSent += (sender, e) => LogToDebug($"{e.GetType().Name} {e.EventsCount}");
                telemetryEvents.QueueOverThreshold += (sender, e) => LogToDebug($"{e.GetType().Name}");
                telemetryEvents.QueueUnderThreshold += (sender, e) => LogToDebug($"{e.GetType().Name}");
                telemetryEvents.TokenRejected += (sender, e) => LogToDebug($"{e.GetType().Name} {e.EventsCount} {e.RejectedReason} {e.TicketType}");
            }
        }
    }
}
