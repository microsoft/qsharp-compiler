// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Quantum.Telemetry.Samples.CSharp
{
    public class Program
    {
        // By default, we set the TestMode to true unless the
        // TELEMETRY compiler constant is passed during the build.
        // We do that to prevent unintentional telemetry to be sent
        // to Microsoft via the Sample Telemetry app.
        private static bool IsTestMode =>
            #if TELEMETRY
                false;
            #else
                true;
            #endif

        public static void Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();

            var telemetryConfig = new TelemetryManagerConfig()
            {
                AppId = "SampleCSharpApp",
                HostingEnvironmentVariableName = "SAMPLECSHARPAPP_HOSTING_ENV",
                TelemetryOptOutVariableName = "QDK_TELEMETRY_OPT_OUT",
                MaxTeardownUploadTime = TimeSpan.FromSeconds(2),
                OutOfProcessUpload = true,
                ExceptionLoggingOptions = new ExceptionLoggingOptions()
                {
                    CollectTargetSite = true,
                    CollectSanitizedStackTrace = true,
                },
                SendTelemetryInitializedEvent = true,
                SendTelemetryTearDownEvent = true,
                DefaultTelemetryConsent = ConsentKind.OptedIn,

                // When TestMode is true, the events won't be sent to Microsoft servers
                // but will only be printed to the Debug Console
                TestMode = IsTestMode,
            };

            // REQUIRED: Initialize
            // Initialize the TelemetryManager right at the beginning of your program
            // The Initialize method returns an IDisposable handle, which when disposed
            // will call the TelemetryManager.TearDown() method for you.
            //
            // REQUIRED: TearDown
            // Before existing the program we need to "tear down" the telemetry framework
            // such that it will attempt to upload the remaining events within the
            // MaxTeardownUploadTime limit for an in-process uploader, or
            // communicate the out-of-process uploader that we are done creating
            // events.
            using (TelemetryManager.Initialize(telemetryConfig, args))
            {
                try
                {
                    // OPTIONAL: Context Properties
                    // Set context properties that are included in every event
                    // If an event later set a property with the same name, the context property
                    // will be completely overridden in that specific event instance.
                    TelemetryManager.SetContext("CommonDateTime", DateTime.Now);
                    TelemetryManager.SetContext("CommonString", "my string");
                    TelemetryManager.SetContext("CommonLong", 123);
                    TelemetryManager.SetContext("CommonDouble", 123.123);
                    TelemetryManager.SetContext("CommonGuid", Guid.NewGuid());
                    TelemetryManager.SetContext("CommonBool", true);
                    TelemetryManager.SetContext("CommonPIIData", "username", isPii: true);

                    // OPTIONAL: Manually construct an Aria event
                    // Log an event using Aria EventProperties object
                    // Properties that contain PII or customer data should be tagged
                    // with the PiiKind != None
                    Microsoft.Applications.Events.EventProperties eventProperties = new Applications.Events.EventProperties()
                        {
                            Name = "SampleEvent",
                        };
                    eventProperties.SetProperty("SampleDateTime", DateTime.Now);
                    eventProperties.SetProperty("SampleString", "my string");
                    eventProperties.SetProperty("SampleLong", 123);
                    eventProperties.SetProperty("SampleDouble", 123.123);
                    eventProperties.SetProperty("SampleGuid", Guid.NewGuid());
                    eventProperties.SetProperty("SampleBool", true);
                    eventProperties.SetProperty("SamplePIIData", "username", Applications.Events.PiiKind.Identity);
                    TelemetryManager.LogEvent(eventProperties);

                    // OPTIONAL: Just log an event name
                    // Log just the event name, with no extra properties
                    // Context properties will still be added.
                    TelemetryManager.LogEvent("MyEventName");

                    // OPTIONAL:
                    // Log an Exception. The name of the event will be "Exception".
                    // Note that when we log an exception, only the name of the class will be logged.
                    // No properties of the exception will be logged as they can contain customer data
                    try
                    {
                        ThrowANestedException();
                    }
                    catch (Exception exception)
                    {
                        TelemetryManager.LogObject(exception);
                    }

                    // OPTIONAL: Log a custom object
                    // Custom objects will have all of their non-null public properties
                    // logged with some rules applied.
                    // Please check the TelemetryManager.LogObject documentation.
                    ExecutionCompleted executionCompletedEvent = new ExecutionCompleted()
                        {
                            SampleDateTime = DateTime.Now,
                            SampleString = "sample string",
                            SampleBool = true,
                            SampleEnum = SampleEnumType.SampleEnumValue1,
                            SamplePII = "PII data to be hashed",
                            SampleArray = new string[] { "element1", "element2" },
                            SampleTimeSpan = new TimeSpan(10, 9, 8, 7, 654),
                            SampleInt = 42,
                            SampleDictionary = new Dictionary<string, string>()
                            {
                                { "key1", "value1" },
                                { "key2", "value2" },
                            },
                            SampleGenericObject = new Dictionary<int, string>(),
                            SampleGuid = Guid.NewGuid(),
                            SampleException = CreateExceptionWithStackTrace(),
                            SampleNullableWithValue = 123,
                            SampleNullableWithNull = null,
                        };
                    TelemetryManager.LogObject(executionCompletedEvent);
                }
                catch (Exception exception)
                {
                    // RECOMMENDED: Log unhandled exceptions
                    // Catch and log unhandled exceptions in main code.
                    TelemetryManager.LogObject(exception);

                    // Not recommended for production code.
                    // This is only for debugging purposes of this sample program.
                    Console.WriteLine($"UnhandledException: {exception}");
                }
                finally
                {
                    #if DEBUG
                    if (TelemetryManager.Configuration.OutOfProcessUpload)
                    {
                        // we stop the stopwatch here because in debug mode
                        // the teardown will await for the external process to finish
                        // in order to keep listening for debug information
                        stopwatch.Stop();
                    }
                    #endif
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Total time elapsed: {stopwatch.Elapsed}");
            Environment.Exit(0);
        }

        private static Exception CreateExceptionWithStackTrace()
        {
            try
            {
                ThrowANestedException();
                return null!;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static void ThrowANestedException() =>
            ThrowAnException();

        private static void ThrowAnException() =>
            throw new System.IO.FileNotFoundException(@"File path 'C:\Users\johndoe\file.txt'");
    }

    public enum SampleEnumType
    {
        SampleEnumValue1,
        SampleEnumValue2,
    }

    public class ExecutionCompleted
    {
        public DateTime SampleDateTime { get; set; }

        public string? SampleString { get; set; }

        public bool SampleBool { get; set; }

        public SampleEnumType SampleEnum { get; set; }

        [PiiData]
        public string? SamplePII { get; set; }

        [SerializeJson]
        public string[]? SampleArray { get; set; }

        public TimeSpan SampleTimeSpan { get; set; }

        public int SampleInt { get; set; }

        public Guid SampleGuid { get; set; }

        [SerializeJson]
        public Dictionary<string, string>? SampleDictionary { get; set; }

        public object? SampleGenericObject { get; set; }

        public Exception? SampleException { get; set; }

        public int? SampleNullableWithValue { get; set; }

        public int? SampleNullableWithNull { get; set; }
    }
}
