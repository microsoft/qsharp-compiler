// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using Microsoft.Quantum.Telemetry.Commands;

namespace Microsoft.Quantum.Telemetry
{
    public class ExceptionLoggingOptions
    {
        public bool CollectTargetSite { get; set; } = true;

        public bool CollectSanitizedStackTrace { get; set; } = true;
    }

    /// <summary>
    /// The kind of consent to collect telemetry.
    /// </summary>
    public enum ConsentKind
    {
        /// <summary>
        /// User has opted in, and telemetry may be collected.
        /// </summary>
        OptedIn,

        /// <summary>
        /// User has opted out and telemetry will not be collected.
        /// </summary>
        OptedOut,
    }

    public class TelemetryManagerConfig
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
        /// The default User Consent to collect or not collect telemetry
        /// (defaults to ConsentKind.OptedOut if not set).
        /// If the default value is ConsentKind.OptedOut, then the user can opt-in via
        /// the environment variable defined in TelemetryOptInVariableName.
        /// If the default value is ConsentKind.OptedIn, then the user can opt-out via
        /// the environment variable defined in TelemetryOptOutVariableName.
        /// </summary>
        public ConsentKind DefaultTelemetryConsent { get; set; } = ConsentKind.OptedOut;

        /// <summary>
        /// The name of the environment variable to be checked at initialization to see if
        /// the user wants to opt-out of sending telemetry data.
        /// If the value of the environment variable is "1", no data will be collected or sent.
        /// </summary>
        public string TelemetryOptOutVariableName { get; set; } = "QDK_TELEMETRY_OPT_OUT";

        /// <summary>
        /// The name of the environment variable to be checked at initialization to see if
        /// the user wants to opt-in of sending telemetry data.
        /// If the value of the environment variable is "1", data will be collected and sent.
        /// The "TelemetryOptOut" will always take precedence.
        /// </summary>
        public string TelemetryOptInVariableName { get; set; } = "QDK_TELEMETRY_OPT_IN";

        /// <summary>
        /// The name of the environment variable to be logged as HostingEnvironment in the telemetry.
        /// Useful to identify hosting environments like build agents in the telemetry.
        /// </summary>
        public string HostingEnvironmentVariableName { get; set; } = "QDK_HOSTING_ENV";

        /// <summary>
        /// The name of the environment variable to control whether the telemetry library throws exceptions.
        /// The value of this env var will override the compiler directive ENABLE_QDK_TELEMETRY_EXCEPTIONS.
        /// Modes:
        ///  - Disabled (default): The telemetry library will quietly catch all exceptions
        ///    Recommended for customer release, where we don't want the telemetry to impact user experience
        ///  - Enabled: The telemetry library will freely throw exceptions to the caller program
        ///    Recommended for development, debug, and automated tests
        /// Conditions:
        ///  - Enabled if ENABLE_QDK_TELEMETRY_EXCEPTIONS or DEBUG are present in the compiler directives,
        ///    Disabled otherwise
        /// Override:
        ///  - The value of the env var given by this property can force enable or disable the exceptions by
        ///    setting "1" or "0" respectivilly.
        /// </summary>
        public string EnableTelemetryExceptionsVariableName { get; set; } = "ENABLE_QDK_TELEMETRY_EXCEPTIONS";

        public bool EnableTelemetryExceptions { get; set; }

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
        public TimeSpan OutOfProcessMaxTeardownUploadTime { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How long the external process will await without receiving any messages
        /// before quitting.
        /// This is useful in case the main process crashes and we don't want to keep
        /// the external process running forever.
        /// Defaults to 30 seconds.
        /// </summary>
        public TimeSpan OutOfProcessMaxIdleTime { get; set; } = TimeSpan.FromSeconds(30);

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

        public string? OutOfProcessExecutablePath { get; set; }

        public Type OutOfProcessSerializerType { get; set; } = typeof(SimpleYamlSerializer);

        public ExceptionLoggingOptions ExceptionLoggingOptions { get; set; } = new ExceptionLoggingOptions();

        public bool SendTelemetryInitializedEvent { get; set; } = true;

        public bool SendTelemetryTearDownEvent { get; set; } = true;

        /// <summary>
        /// The name of the environment variable to control whether the telemetry library is in Test mode.
        /// The value of this env var will be used if the TestMode option is false.
        /// If the value is "1" the Test mode will be enabled.
        /// </summary>
        public string EnableTelemetryTestVariableName { get; set; } = "ENABLE_QDK_TELEMETRY_TEST";

        public bool TestMode { get; set; }

        private static readonly Regex NameValidationRegex = new Regex("^[a-zA-Z0-9]{3,20}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string ValidateName(string value)
        {
            if (!NameValidationRegex.IsMatch(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value should only contains letters or numbers and have 3-20 characters");
            }

            return value;
        }
    }
}
