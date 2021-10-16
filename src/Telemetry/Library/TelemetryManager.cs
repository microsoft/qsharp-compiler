// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry
{
    public record ExceptionLoggingOptions
    {
        public bool CollectTargetSite { get; set; } = true;

        public bool CollectSanitizedStackTrace { get; set; } = true;
    }

    public record TelemetryManagerConfig
    {
        private string appId = "DefaultQDKApp";

        /// <summary>
        /// The name of the application to be logged in all events.
        /// </summary>
        public string AppId
        {
            get { return this.appId; }
            set { this.appId = ValidateName(value); }
        }

        private string eventNamePrefix = "Quantum";

        /// <summary>
        /// The prefix to be added to event names if they are not already present.
        /// Defaults to "Quantum".
        /// </summary>
        public string EventNamePrefix
        {
            get { return this.eventNamePrefix; }
            set { this.eventNamePrefix = ValidateName(value); }
        }

        /// <summary>
        /// The name of the environment variable to be checked at initialization to see if
        /// the user wants to opt-out of sending telemetry data.
        /// If the value of the environment variable is "1", no data will be collected or sent.
        /// </summary>
        public string TelemetryOptOutVariableName { get; set; } = "QDK_TELEMETRY_OPT_OUT";

        /// <summary>
        /// The name of the environment variable to be logged as HostingEnvironment in the telemetry.
        /// Useful to identify hosting environments like build agents in the telemetry.
        /// </summary>
        public string HostingEnvironmentVariableName { get; set; } = "QDK_HOSTING_ENV";

        /// <summary>
        /// Await up to X milliseconds for the telemetry
        /// to get uploaded before tearing down.
        /// Defaults to 2 seconds.
        /// </summary>
        public TimeSpan MaxTeardownUploadTime { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Await up to X milliseconds for the telemetry
        /// to get uploaded before tearing down.
        /// Defaults to 30 seconds.
        /// </summary>
        public TimeSpan OutOProcessMaxTeardownUploadTime { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How long the external process will await without receiving any messages
        /// before quitting.
        /// This is useful in case the main process crashes and we don't want to keep
        /// the external process running forever.
        /// Defaults to 30 seconds.
        /// </summary>
        public TimeSpan OutOProcessMaxIdleTime { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How long the external process will await when receiving no messages in the message loop.
        /// Defaults to 1 seconds.
        /// </summary>
        public TimeSpan OutOfProcessPollWaitTime { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// When true, all telemetry will be routed to a different process so that it can be uploaded
        /// asynchronously even if the main application finishes first.
        /// This is particularly useful for command line applications that are super fast and that
        /// we don't want to add additional runtime just for the sake of uploading telemetry data.
        /// </summary>
        public bool OutOfProcessUpload { get; set; } = false;

        public ExceptionLoggingOptions ExceptionLoggingOptions { get; set; } = new();

        public bool SendTelemetryInitializedEvent { get; set; } = true;

        public bool SendTelemetryTearDownEvent { get; set; } = true;

        private static readonly Regex NameValidationRegex = new("^[a-zA-Z0-9]{3,20}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string ValidateName(string value)
        {
            if (!NameValidationRegex.IsMatch(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value should only contains letters or numbers and have 3-20 characters");
            }

            return value;
        }
    }

    internal record TelemetryTearDown(TimeSpan? TotalRunningTime, int TotalEventsCount);

    /// <summary>
    /// Handles general telemetry logic that is used accross Microsoft Quantum developer tools.
    /// </summary>
    public static class TelemetryManager
    {
        private const string TOKEN = "55aee962ee9445f3a86af864fc0fa766-48882422-3439-40de-8030-228042bd9089-7794";
        internal const string OUTOFPROCESSUPLOADARG = "--OUT_OF_PROCESS_UPLOAD_ARG";

        private static ILogger? telemetryLogger;
        private static bool tearDownLogManager = false;
        private static DateTime? initializationTime;
        private static int totalEventsCount = 0;

        /// <summary>
        /// Handles general telemetry logic that is used accross Microsoft Quantum developer tools.
        /// </summary>
        public static bool IsOutOfProcessInstance { get; private set; } = false;

        /// <summary>
        /// Compiler directive ENABLE_QDK_TELEMETRY_EXCEPTIONS or DEBUG will set it to true
        /// Environment variable ENABLE_QDK_TELEMETRY_EXCEPTIONS="1" or "0" will override it.
        /// True if exceptions generated at the telemetry later should be thrown.
        /// False if they should be silenced and suppressed. They will still be logged at Trace.
        /// </summary>
        public static bool EnableTelemetryExceptions { get; private set; } = false;

        /// <summary>
        /// The active configuration of the TelemetryManager.
        /// Should be passed during TelemetryManager.Initialize and not be modified afterwards.
        /// </summary>
        public static TelemetryManagerConfig Configuration { get; private set; } = new();

        /// <summary>
        /// If this value is true, no data will be collected or sent.
        /// Every public method of this class check this variable first and if it's true the method
        /// immediately returns without doing anything.
        /// This property defaults to "false".
        /// This property is automatically set to true during initialization if the environment variable
        /// named in Configuration.HostingEnvironmentVariableName has a value of "1".
        /// </summary>
        public static bool TelemetryOptOut { get; private set; } = false;

        /// <summary>
        /// Initializes the TelemetryManager with the given configuration.
        /// Must be called before any other method in this class, ideally right after the program starts.
        /// </summary>
        public static void Initialize(TelemetryManagerConfig configuration, string[]? args = null)
        {
            initializationTime = DateTime.Now;
            Configuration = configuration;
            EnableTelemetryExceptions = GetEnableTelemetryExceptions();
            TelemetryOptOut = GetTelemetryOptOut(configuration);
            IsOutOfProcessInstance = args != null && args.Contains(OUTOFPROCESSUPLOADARG);

            CheckAndRunSafe(
                () =>
                {
                    if (telemetryLogger != null)
                    {
                        throw new InvalidOperationException("The TelemetryManager was already initialized");
                    }

                    if (configuration.OutOfProcessUpload & !IsOutOfProcessInstance)
                    {
                        telemetryLogger = new OutOfProcessLogger();
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
                        OutOfProcessServer.RunAndExit(configuration);
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
        }

        private static void SetStartupContext()
        {
            SetContext("AppId", Configuration.AppId);
            SetContext("AppVersion", Assembly.GetEntryAssembly()?.GetName().Version?.ToString());
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
                MaxTeardownUploadTime = (int)(IsOutOfProcessInstance ? Configuration.OutOProcessMaxTeardownUploadTime :
                                              Configuration.MaxTeardownUploadTime).TotalMilliseconds,
            });

            #if DEBUG
            SubscribeTelemetryEventsForDebugging();
            #endif

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
                    if (Configuration.SendTelemetryTearDownEvent && !IsOutOfProcessInstance)
                    {
                        LogObject(new TelemetryTearDown(DateTime.Now - initializationTime, totalEventsCount));
                    }

                    if (telemetryLogger is OutOfProcessLogger outOfProcessLogger)
                    {
                        outOfProcessLogger.Quit();
                        #if DEBUG
                        outOfProcessLogger.AwaitForExternalProcessExit();
                        #endif
                    }

                    telemetryLogger = null;

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
        public static void LogObject(object obj) =>
            CheckAndRunSafe(() =>
            {
                if (obj == null || obj is ValueType)
                {
                    return;
                }

                EventProperties eventProperties = new();
                string? eventName = null;

                // We don't log exception fields as they can potentially contain customer data
                if (obj is Exception exception)
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
                totalEventsCount++;

                var eventNamePrefix = $"{Configuration.EventNamePrefix}_{Configuration.AppId}_";
                if (!eventProperties.Name.StartsWith(eventNamePrefix))
                {
                    eventProperties.Name = eventNamePrefix + eventProperties.Name;
                }

                if (!eventProperties.Properties.ContainsKey("TimestampUTC"))
                {
                    eventProperties.SetProperty("TimestampUTC", DateTime.UtcNow);
                }

                telemetryLogger?.LogEvent(eventProperties);

                #if DEBUG
                LogToDebug($"{telemetryLogger?.GetType().Name} logged event {eventProperties.Name}");
                #endif
            });

        /// <summary>
        /// Logs an event with the given eventName, without any additional properties
        /// Adds an event name prefix if necessary.
        /// </summary>
        public static void LogEvent(string eventName) =>
            CheckAndRunSafe(() => LogEvent(new EventProperties() { Name = ValidateEventName(eventName) }));

        public static void SetContext(string name, object value, TelemetryPropertyType propertyType, bool isPii) =>
            CheckAndRunSafe(() => telemetryLogger?.SetContext(name, value, propertyType, isPii));

        public static void SetContext(string name, string? value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger?.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, long value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger?.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, Guid value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger?.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, bool value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger?.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, double value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger?.SetContext(name, value, isPii.ToPiiKind()));

        public static void SetContext(string name, DateTime value, bool isPii = false) =>
            CheckAndRunSafe(() => telemetryLogger?.SetContext(name, value, isPii.ToPiiKind()));

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
                    throw new ArgumentException("TelemetryManager has not been initialized. Please call TelemetryManager.Initialize before attempting to log anything.");
                }

                action();
            }
            catch (Exception exception)
            {
                Trace.TraceError($"QDK Telemetry error. Exception: {exception.ToString()}");

                #if DEBUG
                LogToDebug($"QDK Telemetry error. Exception: {exception.ToString()}");
                #endif

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

        private static bool GetEnableTelemetryExceptions()
        {
            var enableTelemetryExceptions = false;

            #if ENABLE_QDK_TELEMETRY_EXCEPTIONS || DEBUG
            enableTelemetryExceptions = true;
            #endif

            var envVarValue = Environment.GetEnvironmentVariable("ENABLE_QDK_TELEMETRY_EXCEPTIONS");
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

        private static readonly Regex EventNameValidationRegex = new("^[a-zA-Z0-9]{3,30}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string ValidateEventName(string value)
        {
            if (!EventNameValidationRegex.IsMatch(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value should only contains letters or numbers and have 3-30 characters");
            }

            return value;
        }

        #if DEBUG
        internal static void LogToDebug(string message)
        {
            message = $"{DateTime.Now}: {message}";
            if (IsOutOfProcessInstance)
            {
                Console.WriteLine(message);
            }

            Debug.WriteLine(message, "QDK Telemetry");
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
        #endif
    }
}