// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Applications.Events;
using Microsoft.Quantum.Telemetry.OutOfProcess;

namespace Microsoft.Quantum.Telemetry
{
    public class TelemetryManagerInstance : IDisposable
    {
        private ILogger? telemetryLogger;
        private static int logManagerInitCount = 0;
        private static object logManagerLock = new object();

        public DateTime? InitializationTime { get; private set; }

        public int TotalEventsCount { get; private set; } = 0;

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Handles general telemetry logic that is used accross Microsoft Quantum developer tools.
        /// </summary>
        public bool IsOutOfProcessInstance { get; private set; } = false;

        /// <summary>
        /// True if the TESTMODE argument was passed during initialization.
        /// This is used for some special cases of running unit tests.
        /// </summary>
        public bool TestMode => this.Configuration.TestMode;

        /// <summary>
        /// Compiler directive ENABLE_QDK_TELEMETRY_EXCEPTIONS or DEBUG will set it to true
        /// Environment variable ENABLE_QDK_TELEMETRY_EXCEPTIONS="1" or "0" will override it.
        /// True if exceptions generated at the telemetry later should be thrown.
        /// False if they should be silenced and suppressed. They will still be logged at Trace.
        /// </summary>
        public bool EnableTelemetryExceptions => this.Configuration.EnableTelemetryExceptions;

        /// <summary>
        /// The active configuration of the TelemetryManager.
        /// Should be passed during TelemetryManager.Initialize and not be modified afterwards.
        /// </summary>
        public TelemetryManagerConfig Configuration { get; private set; }

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
        public bool TelemetryEnabled { get; private set; } = false;

        public event EventHandler<EventProperties>? OnEventLogged;

        public TelemetryManagerInstance()
        {
            this.Configuration = new TelemetryManagerConfig();
            this.Configuration.EnableTelemetryExceptions = this.GetEnableTelemetryExceptions(false);
        }

        /// <summary>
        /// Initializes the TelemetryManager with the given configuration.
        /// Must be called before any other method in this class, ideally right after the program starts.
        /// </summary>
        public IDisposable Initialize(TelemetryManagerConfig configuration, string[]? args = null)
        {
            this.InitializationTime = DateTime.Now;

            this.IsOutOfProcessInstance = args?.Contains(TelemetryManagerConstants.OUTOFPROCESSUPLOADARG) == true;

            // Do not enable OutOfProcessUpload mode if we are running inside a
            // unit test hosting process and an out of process executable path
            // is not given.
            configuration.OutOfProcessUpload = (configuration.OutOfProcessUpload
                                                || this.IsOutOfProcessInstance)
                                               &&
                                               (!TelemetryManager.IsTestHostProcess
                                                || !string.IsNullOrEmpty(configuration.OutOfProcessExecutablePath));
            configuration.TestMode = (args?.Contains(TelemetryManagerConstants.TESTMODE) == true)
                                     || configuration.TestMode
                                     || "1".Equals(Environment.GetEnvironmentVariable(this.Configuration.EnableTelemetryTestVariableName));
            configuration.EnableTelemetryExceptions = this.GetEnableTelemetryExceptions(configuration.EnableTelemetryExceptions);

            this.Configuration = configuration;

            this.TelemetryEnabled = this.GetTelemetryOptIn(configuration);

            this.CheckAndRunSafe(
                () =>
                {
                    if (this.telemetryLogger != null)
                    {
                        throw new InvalidOperationException("The TelemetryManager was already initialized");
                    }

                    if (configuration.OutOfProcessUpload & args == null)
                    {
                        throw new ArgumentNullException(nameof(args), "The application start arguments array must be passed when using OutOfProcessUpload");
                    }

                    if (configuration.OutOfProcessUpload & !this.IsOutOfProcessInstance)
                    {
                        this.telemetryLogger = new OutOfProcessLogger(configuration);
                    }
                    else if (configuration.TestMode)
                    {
                        this.telemetryLogger = new DebugConsoleLogger();
                    }
                    else
                    {
                        this.InitializeLogManager();
                        this.telemetryLogger = LogManager.GetLogger(TelemetryManagerConstants.TOKEN, out _);
                    }

                    if (this.IsOutOfProcessInstance)
                    {
                        // After completing the next line, the current process
                        // will exit with exit code 0.
                        new OutOfProcessServer(configuration, Console.In).RunAndExit();
                    }
                    else
                    {
                        this.SetStartupContext();

                        if (this.Configuration.SendTelemetryInitializedEvent)
                        {
                            this.LogEvent("TelemetryInitialized");
                        }
                    }
                },
                initializingOrTearingDown: true);

            return this;
        }

        private void SetStartupContext()
        {
            this.SetContext("AppId", this.Configuration.AppId);
            this.SetContext("AppVersion", Assembly.GetEntryAssembly()!.GetName().Version!.ToString());
            this.SetContext("DeviceId", this.GetDeviceId(), isPii: true);
            this.SetContext("SessionId", Guid.NewGuid().ToString());
            this.SetContext("HostingEnvironment", Environment.GetEnvironmentVariable(this.Configuration.HostingEnvironmentVariableName));
            this.SetContext("OutOfProcessUpload", this.Configuration.OutOfProcessUpload);
            this.SetContext("Timezone", (TimeZoneInfo.Local.BaseUtcOffset < TimeSpan.Zero ? "-" : "")
                                        + TimeZoneInfo.Local.BaseUtcOffset.ToString(@"hh\:mm"));
        }

        private void InitializeLogManager()
        {
            lock (logManagerLock)
            {
                LogManager.Start(new LogConfiguration()
                {
                    MaxTeardownUploadTime = (int)(this.IsOutOfProcessInstance ? this.Configuration.OutOfProcessMaxTeardownUploadTime :
                                                this.Configuration.MaxTeardownUploadTime).TotalMilliseconds,
                });

                if (this.TestMode || TelemetryManagerConstants.IsDebugBuild)
                {
                    this.SubscribeTelemetryEventsForDebugging();
                }

                LogManager.SetTransmitProfile("RealTime");

                logManagerInitCount++;
            }
        }

        private void TearDownLogManager()
        {
            lock (logManagerLock)
            {
                logManagerInitCount--;
                if (logManagerInitCount == 0)
                {
                    LogManager.Teardown();
                }
            }
        }

        /// <summary>
        /// Kills all threads and exists as soon as possible.
        /// Will await for the time specified in Configuration.MaxTeardownUploadTime
        /// to upload the remaining events.
        /// </summary>
        public void TearDown() =>
            this.CheckAndRunSafe(
                () =>
                {
                    if (this.telemetryLogger != null
                        && this.Configuration.SendTelemetryTearDownEvent
                        && !this.IsOutOfProcessInstance)
                    {
                        this.LogObject(new TelemetryTearDown(DateTime.Now - this.InitializationTime, this.TotalEventsCount + 1));
                    }

                    if (this.telemetryLogger is OutOfProcessLogger outOfProcessLogger)
                    {
                        outOfProcessLogger.Quit();
                        if (TelemetryManagerConstants.IsDebugBuild
                            && !TelemetryManager.IsTestHostProcess)
                        {
                            outOfProcessLogger.AwaitForExternalProcessExit();
                        }
                    }

                    this.telemetryLogger = null;
                    this.TotalEventsCount = 0;
                    this.InitializationTime = null;
                    this.TearDownLogManager();
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
        public void LogObject(string eventName, object obj) =>
            this.InternalLogObject(eventName, obj);

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
        public void LogObject(object obj) =>
            this.InternalLogObject(null, obj);

        private void InternalLogObject(string? eventName, object obj) =>
            this.CheckAndRunSafe(() =>
            {
                if (obj == null || obj is ValueType)
                {
                    return;
                }

                if (obj is EventProperties eventProp)
                {
                    this.LogEvent(eventProp);
                    return;
                }

                EventProperties eventProperties = new EventProperties();

                // We don't log exception fields as they can potentially contain customer data
                if (eventName == null && obj is Exception exception)
                {
                    obj = exception.ToExceptionLogRecord(this.Configuration.ExceptionLoggingOptions);
                    eventName = $"{this.Configuration.EventNamePrefix}_{this.Configuration.AppId}_Exception";
                }

                var type = obj.GetType();
                eventProperties.Name = eventName ?? $"{this.Configuration.EventNamePrefix}_{this.Configuration.AppId}_{type.Name}";
                var properties = ReflectionCache.GetProperties(type);
                foreach (var property in properties)
                {
                    var isPii = property.GetCustomAttribute<PiiDataAttribute>() != null;
                    var serializeJson = property.GetCustomAttribute<SerializeJsonAttribute>() != null;
                    var value = property.GetValue(obj);
                    eventProperties.SetProperty(property.Name, value, isPii, serializeJson);
                }

                this.LogEvent(eventProperties);
            });

        /// <summary>
        /// Logs the given Aria event.
        /// Adds an event name prefix if necessary.
        /// </summary>
        public void LogEvent(EventProperties eventProperties) =>
            this.CheckAndRunSafe(() =>
            {
                this.TotalEventsCount++;

                var eventNamePrefix = $"{this.Configuration.EventNamePrefix}_{this.Configuration.AppId}_";
                if (!eventProperties.Name.StartsWith(eventNamePrefix))
                {
                    eventProperties.Name = eventNamePrefix + eventProperties.Name;
                }

                if (!eventProperties.Properties.ContainsKey("TimestampUTC"))
                {
                    eventProperties.SetProperty("TimestampUTC", DateTime.UtcNow);
                }

                this.telemetryLogger!.LogEvent(eventProperties);

                this.OnEventLogged?.Invoke(this.telemetryLogger, eventProperties);

                if (TelemetryManagerConstants.IsDebugBuild || this.TestMode)
                {
                    TelemetryManager.LogToDebug($"{this.telemetryLogger!.GetType().Name} logged event {eventProperties.Name}");
                }
            });

        /// <summary>
        /// Logs an event with the given eventName, without any additional properties
        /// Adds an event name prefix if necessary.
        /// </summary>
        public void LogEvent(string eventName) =>
            this.CheckAndRunSafe(() => this.LogEvent(new EventProperties() { Name = ValidateEventName(eventName) }));

        public void SetContext(string name, object value, TelemetryPropertyType propertyType, bool isPii) =>
            this.CheckAndRunSafe(() => this.telemetryLogger!.SetContext(name, value, propertyType, isPii));

        public void SetContext(string name, string? value, bool isPii = false) =>
            this.CheckAndRunSafe(() => this.telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public void SetContext(string name, int value, bool isPii = false) =>
            this.SetContext(name, (long)value, isPii);

        public void SetContext(string name, long value, bool isPii = false) =>
            this.CheckAndRunSafe(() => this.telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public void SetContext(string name, Guid value, bool isPii = false) =>
            this.CheckAndRunSafe(() => this.telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public void SetContext(string name, bool value, bool isPii = false) =>
            this.CheckAndRunSafe(() => this.telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public void SetContext(string name, double value, bool isPii = false) =>
            this.CheckAndRunSafe(() => this.telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public void SetContext(string name, DateTime value, bool isPii = false) =>
            this.CheckAndRunSafe(() => this.telemetryLogger!.SetContext(name, value, isPii.ToPiiKind()));

        public void UploadNow() =>
            this.CheckAndRunSafe(() =>
            {
                if (logManagerInitCount > 0)
                {
                    LogManager.UploadNow();
                }
            });

        private void CheckAndRunSafe(Action action, bool initializingOrTearingDown = false)
        {
            if (!this.TelemetryEnabled)
            {
                return;
            }

            try
            {
                if (this.telemetryLogger == null && !initializingOrTearingDown)
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

                if (TelemetryManagerConstants.IsDebugBuild || this.TestMode)
                {
                    TelemetryManager.LogToDebug(message);
                }

                if (this.EnableTelemetryExceptions)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Return an Id for this device, namely, the first non-empty MAC address it can find across all network interfaces (if any).
        /// </summary>
        private string? GetDeviceId() =>
            NetworkInterface.GetAllNetworkInterfaces()?
                .Select(n => n?.GetPhysicalAddress()?.ToString())
                .Where(address => address != null && !string.IsNullOrWhiteSpace(address) && !address.StartsWith("000000"))
                .FirstOrDefault();

        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:ClosingParenthesisMustBeSpacedCorrectly", Justification = "Easier to read the logic.")]
        private bool GetTelemetryOptIn(TelemetryManagerConfig configuration) =>
            (
                (configuration.DefaultTelemetryConsent == ConsentKind.OptedIn)
                ||
                (Environment.GetEnvironmentVariable(configuration.TelemetryOptInVariableName) == "1")
            )
            && !(Environment.GetEnvironmentVariable(configuration.TelemetryOptOutVariableName) == "1");

        private bool GetEnableTelemetryExceptions(bool enableTelemetryExceptions)
        {
            #if ENABLE_QDK_TELEMETRY_EXCEPTIONS || DEBUG
            enableTelemetryExceptions = true;
            #endif

            var envVarValue = Environment.GetEnvironmentVariable(this.Configuration.EnableTelemetryExceptionsVariableName);
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

        private void SubscribeTelemetryEventsForDebugging()
        {
            var telemetryEvents = LogManager.GetTelemetryEvents(out _);
            if (telemetryEvents != null)
            {
                telemetryEvents.EventsDropped += (sender, e) => TelemetryManager.LogToDebug($"{e.GetType().Name} {e.EventsCount} {e.DroppedReason} {e.DroppedDetails}");
                telemetryEvents.EventsRejected += (sender, e) => TelemetryManager.LogToDebug($"{e.GetType().Name} {e.EventsCount} {e.RejectedReason} {e.RejectedDetails}");
                telemetryEvents.EventsRetrying += (sender, e) => TelemetryManager.LogToDebug($"{e.GetType().Name} {e.EventsCount} {e.RetryReason} {e.RetryDetails}");
                telemetryEvents.EventsSent += (sender, e) => TelemetryManager.LogToDebug($"{e.GetType().Name} {e.EventsCount}");
                telemetryEvents.QueueOverThreshold += (sender, e) => TelemetryManager.LogToDebug($"{e.GetType().Name}");
                telemetryEvents.QueueUnderThreshold += (sender, e) => TelemetryManager.LogToDebug($"{e.GetType().Name}");
                telemetryEvents.TokenRejected += (sender, e) => TelemetryManager.LogToDebug($"{e.GetType().Name} {e.EventsCount} {e.RejectedReason} {e.TicketType}");
            }
        }

        public void Dispose()
        {
            this.TearDown();
            this.IsDisposed = true;

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
}
