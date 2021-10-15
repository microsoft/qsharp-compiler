// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Quantum.Telemetry.Samples.CSharp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Initialize the TelemetryManager
                TelemetryManager.Initialize(
                    new TelemetryManagerConfig()
                    {
                        AppId = "SampleCSharpApp",
                        HostingEnvironmentVariableName = "SAMPLECSHARPAPP_HOSTING_ENV",
                        TelemetryOptOutVariableName = "QDK_TELEMETRY_OPT_OUT",
                        MaxTeardownUploadTime = TimeSpan.FromSeconds(2),
                        OutOfProcessUpload = false,
                    },
                    args);

                // Log an event using Aria EventProperties object
                // Properties that contain PII or customer data should be tagged
                // with the PiiKind != None
                Microsoft.Applications.Events.EventProperties eventProperties = new()
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

                // Log an Exception
                // Note that when we log an exception, only the name of the class will be logged.
                // No properties of the exception will be logged as they can contain customer data
                try
                {
                    throw new System.IO.FileNotFoundException(@"File path 'C:\Users\johndoe\file.txt'");
                }
                catch (Exception exception)
                {
                    TelemetryManager.LogObject(exception);
                }

                // Log a custom object
                // Custom objects will have all of their non-null public properties
                // logged with some rules applied.
                // Please check the TelemetryManager.LogObject documentation.
                ExecutionCompleted executionCompletedEvent = new()
                    {
                        SampleDateTime = DateTime.Now,
                        SampleString = "sample string",
                        SampleBool = true,
                        SampleEnum = SampleEnumType.SampleEnumValue1,
                        SamplePII = "PII data to be hashed",
                        SampleArray = new string[] { "element1", "element2" },
                        SampleTimeSpan = new TimeSpan(10, 9, 8, 7, 654),
                        SampleInt = 42,
                        SampleDictionary = new()
                        {
                            { "key1", "value1" },
                            { "key2", "value2" },
                        },
                        SampleGenericObject = new Dictionary<int, string>(),
                        SampleGuid = Guid.NewGuid(),
                    };
                TelemetryManager.LogObject(executionCompletedEvent);
            }
            catch (Exception exception)
            {
                TelemetryManager.LogObject(exception);
            }
            finally
            {
                TelemetryManager.TearDown();
            }

            stopwatch.Stop();
            Console.WriteLine($"Total time elapsed: {stopwatch.Elapsed}");
            Environment.Exit(0);
        }
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

        [PIIData]
        public string? SamplePII { get; set; }

        [SerializeJson]
        public string[]? SampleArray { get; set; }

        public TimeSpan SampleTimeSpan { get; set; }

        public int SampleInt { get; set; }

        public Guid SampleGuid { get; set; }

        [SerializeJson]
        public Dictionary<string, string>? SampleDictionary { get; set; }

        public object? SampleGenericObject { get; set; }
    }
}