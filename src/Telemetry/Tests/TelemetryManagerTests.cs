// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Quantum.Telemetry.Tests
{
    [TestClass]
    public class TelemetryManagerTests
    {
        private TelemetryManagerConfig GetConfig() =>
            new TelemetryManagerConfig()
            {
                AppId = "QDKTelemetryTestApp",
                HostingEnvironmentVariableName = "QDKTELTESTAPP_HOSTING_ENV",
                TelemetryOptOutVariableName = "QDKTELTEST_TELEMETRY_OPT_OUT",
                MaxTeardownUploadTime = TimeSpan.Zero,
                OutOfProcessUpload = false,
                ExceptionLoggingOptions = new()
                {
                    CollectTargetSite = true,
                    CollectSanitizedStackTrace = true,
                },
                SendTelemetryInitializedEvent = false,
                SendTelemetryTearDownEvent = false,
            };

        private Exception CreateExceptionWithStackTrace()
        {
            try
            {
                this.ThrowANestedException();
                return null!;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private void ThrowANestedException() =>
            this.ThrowAnException();

        private void ThrowAnException() =>
            throw new System.IO.FileNotFoundException(@"File path 'C:\Users\johndoe\file.txt'");

        private TestEvent CreateTestEventObject() =>
            new()
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
                SampleException = this.CreateExceptionWithStackTrace(),
                SampleNullableWithValue = 123,
                SampleNullableWithNull = null,
            };

        private Microsoft.Applications.Events.EventProperties CreateEventPropertiesObject()
        {
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
            eventProperties.SetProperty("CommonPIIData", true);
            eventProperties.SetProperty("CommonGuid", true);
            return eventProperties;
        }

        private string[] invalidValues = new string[]
        {
            "a",
            "Invalid name",
            "InvalidName#",
            "InvalidName@",
            "Invalid_Name",
            "Invalid.Name",
            new string('a', 31),
        };

        [TestMethod]
        public void TestTelemetryManagerConfig()
        {
            // Test AppId validations
            foreach (var value in this.invalidValues)
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                    new TelemetryManagerConfig()
                    {
                        AppId = value,
                    });
            }

            // Test EventNamePrefix validations
            foreach (var value in this.invalidValues)
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                    new TelemetryManagerConfig()
                    {
                        EventNamePrefix = value,
                    });
            }

            // Test default values
            var telemetryManagerConfig = new TelemetryManagerConfig();
            Assert.AreEqual("DefaultQDKApp", telemetryManagerConfig.AppId);
            Assert.AreEqual("Quantum", telemetryManagerConfig.EventNamePrefix);
            Assert.AreEqual("QDK_TELEMETRY_OPT_OUT", telemetryManagerConfig.TelemetryOptOutVariableName);
            Assert.AreEqual("QDK_HOSTING_ENV", telemetryManagerConfig.HostingEnvironmentVariableName);
            Assert.AreEqual(TimeSpan.FromSeconds(2), telemetryManagerConfig.MaxTeardownUploadTime);
            Assert.AreEqual(TimeSpan.FromSeconds(30), telemetryManagerConfig.OutOfProcessMaxTeardownUploadTime);
            Assert.AreEqual(TimeSpan.FromSeconds(30), telemetryManagerConfig.OutOProcessMaxIdleTime);
            Assert.AreEqual(TimeSpan.FromSeconds(1), telemetryManagerConfig.OutOfProcessPollWaitTime);
            Assert.AreEqual(false, telemetryManagerConfig.OutOfProcessUpload);
            Assert.AreEqual(true, telemetryManagerConfig.SendTelemetryInitializedEvent);
            Assert.AreEqual(true, telemetryManagerConfig.SendTelemetryTearDownEvent);
            Assert.IsNotNull(telemetryManagerConfig.ExceptionLoggingOptions);
            Assert.AreEqual(true, telemetryManagerConfig.ExceptionLoggingOptions.CollectTargetSite);
            Assert.AreEqual(true, telemetryManagerConfig.ExceptionLoggingOptions.CollectSanitizedStackTrace);
        }

        [TestMethod]
        public void TestInitializationAndTearDown()
        {
            var telemetryManagerConfig = this.GetConfig();

            // Check if the correct configuration values are applied
            try
            {
                TelemetryManager.Initialize(telemetryManagerConfig);
                Assert.AreSame(TelemetryManager.Configuration, telemetryManagerConfig);
                Assert.AreEqual(TelemetryManager.Configuration.AppId, telemetryManagerConfig.AppId);
                Assert.AreEqual(TelemetryManager.Configuration.EventNamePrefix, telemetryManagerConfig.EventNamePrefix);
                Assert.AreEqual(TelemetryManager.Configuration.TelemetryOptOutVariableName, telemetryManagerConfig.TelemetryOptOutVariableName);
                Assert.AreEqual(TelemetryManager.Configuration.HostingEnvironmentVariableName, telemetryManagerConfig.HostingEnvironmentVariableName);
                Assert.AreEqual(TelemetryManager.Configuration.MaxTeardownUploadTime, telemetryManagerConfig.MaxTeardownUploadTime);
                Assert.AreEqual(TelemetryManager.Configuration.OutOfProcessMaxTeardownUploadTime, telemetryManagerConfig.OutOfProcessMaxTeardownUploadTime);
                Assert.AreEqual(TelemetryManager.Configuration.OutOProcessMaxIdleTime, telemetryManagerConfig.OutOProcessMaxIdleTime);
                Assert.AreEqual(TelemetryManager.Configuration.OutOfProcessPollWaitTime, telemetryManagerConfig.OutOfProcessPollWaitTime);
                Assert.AreEqual(TelemetryManager.Configuration.OutOfProcessUpload, telemetryManagerConfig.OutOfProcessUpload);
                Assert.AreEqual(TelemetryManager.Configuration.SendTelemetryInitializedEvent, telemetryManagerConfig.SendTelemetryInitializedEvent);
                Assert.AreEqual(TelemetryManager.Configuration.SendTelemetryTearDownEvent, telemetryManagerConfig.SendTelemetryTearDownEvent);
                Assert.IsNotNull(telemetryManagerConfig.ExceptionLoggingOptions);
                Assert.AreEqual(TelemetryManager.Configuration.ExceptionLoggingOptions.CollectTargetSite, telemetryManagerConfig.ExceptionLoggingOptions.CollectTargetSite);
                Assert.AreEqual(TelemetryManager.Configuration.ExceptionLoggingOptions.CollectSanitizedStackTrace, telemetryManagerConfig.ExceptionLoggingOptions.CollectSanitizedStackTrace);
            }
            finally
            {
                TelemetryManager.TearDown();
            }

            // Send initialization and teardown events
            try
            {
                Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
                TelemetryManager.Initialize(telemetryManagerConfig with
                {
                    SendTelemetryInitializedEvent = true,
                    SendTelemetryTearDownEvent = true,
                });
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);
            }
            finally
            {
                TelemetryManager.TearDown();
                Assert.AreEqual(0, TelemetryManager.TotalEventsCount);

                // The second time we do a teardown, it should do nothing and not raise an exception
                TelemetryManager.TearDown();
            }

            // Try initialize it more than once
            try
            {
                TelemetryManager.Initialize(telemetryManagerConfig);
                Assert.ThrowsException<InvalidOperationException>(() =>
                    TelemetryManager.Initialize(telemetryManagerConfig));
            }
            finally
            {
                TelemetryManager.TearDown();
            }

            // Try passing args and check that they are not changed
            try
            {
                var originalArgs = new string[] { "arg1", Guid.NewGuid().ToString() };
                var args = originalArgs.ToArray();
                TelemetryManager.Initialize(telemetryManagerConfig, args);
                CollectionAssert.AreEqual(originalArgs, args);
            }
            finally
            {
                TelemetryManager.TearDown();
            }

            var outOfProcessConfig = telemetryManagerConfig with
            {
                OutOfProcessUpload = true,
                OutOProcessMaxIdleTime = TimeSpan.Zero,
                OutOfProcessMaxTeardownUploadTime = TimeSpan.Zero,
            };

            // Try with OutOfProcessUpload=true, args=null
            try
            {
                Assert.ThrowsException<ArgumentNullException>(
                    () => TelemetryManager.Initialize(outOfProcessConfig),
                    "When using out-of-process without passing the args array, we should throw an exception");
            }
            finally
            {
                TelemetryManager.TearDown();
            }

            // Try with OutOfProcessUpload=true, args!=null
            try
            {
                var args = new string[] { };
                TelemetryManager.Initialize(outOfProcessConfig, args);
            }
            finally
            {
                TelemetryManager.TearDown();
            }

            // Try with OutOfProcessUpload=true, args=OUTOFPROCESSUPLOADARG
            try
            {
                var args = new string[] { TelemetryManager.OUTOFPROCESSUPLOADARG, TelemetryManager.TESTMODE };
                TelemetryManager.Initialize(outOfProcessConfig, args);
            }
            finally
            {
                TelemetryManager.TearDown();
            }
        }

        [TestMethod]
        public void TestLogObjectNull()
        {
            try
            {
                var telemetryManagerConfig = this.GetConfig();
                TelemetryManager.Initialize(telemetryManagerConfig);

                TelemetryManager.LogObject(null!);
                Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
            }
            finally
            {
                TelemetryManager.TearDown();
            }
        }

        [TestMethod]
        public void TestLogWithoutInitialization()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
                TelemetryManager.LogObject(null!));
        }

        [TestMethod]
        public void TestLogObjectException()
        {
            try
            {
                var telemetryManagerConfig = this.GetConfig();
                TelemetryManager.Initialize(telemetryManagerConfig);

                TelemetryManager.LogObject(this.CreateExceptionWithStackTrace());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);
            }
            finally
            {
                TelemetryManager.TearDown();
            }
        }

        [TestMethod]
        public void TestLogObject()
        {
            try
            {
                var telemetryManagerConfig = this.GetConfig();
                TelemetryManager.Initialize(telemetryManagerConfig);

                TelemetryManager.LogObject(this.CreateTestEventObject());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);
            }
            finally
            {
                TelemetryManager.TearDown();
            }
        }

        [TestMethod]
        public void TestLogEvent()
        {
            try
            {
                var telemetryManagerConfig = this.GetConfig();
                TelemetryManager.Initialize(telemetryManagerConfig);

                TelemetryManager.LogEvent(this.CreateEventPropertiesObject());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);

                TelemetryManager.LogObject(this.CreateEventPropertiesObject());
                Assert.AreEqual(2, TelemetryManager.TotalEventsCount);
            }
            finally
            {
                TelemetryManager.TearDown();
            }
        }

        [TestMethod]
        public void TestLogEventName()
        {
            try
            {
                var telemetryManagerConfig = this.GetConfig();
                TelemetryManager.Initialize(telemetryManagerConfig);

                TelemetryManager.LogEvent("MyEventName");
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);

                foreach (var value in this.invalidValues)
                {
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                        TelemetryManager.LogEvent(value));
                }
            }
            finally
            {
                TelemetryManager.TearDown();
            }
        }

        [TestMethod]
        public void TestSetContext()
        {
            try
            {
                var telemetryManagerConfig = this.GetConfig();
                TelemetryManager.Initialize(telemetryManagerConfig);

                TelemetryManager.SetContext("CommonDateTime", DateTime.Now);
                TelemetryManager.SetContext("CommonString", "my string");
                TelemetryManager.SetContext("CommonLong", 123);
                TelemetryManager.SetContext("CommonDouble", 123.123);
                TelemetryManager.SetContext("CommonGuid", Guid.NewGuid());
                TelemetryManager.SetContext("CommonBool", true);
                TelemetryManager.SetContext("CommonPIIData", "username", isPii: true);
                TelemetryManager.SetContext("CommonPIIData2", "username", TelemetryPropertyType.String, isPii: true);
            }
            finally
            {
                TelemetryManager.TearDown();
            }
        }

        [TestMethod]
        public void TestLogObjectOutOfProcess()
        {
            try
            {
                var telemetryManagerConfig = this.GetConfig() with
                {
                    OutOfProcessMaxTeardownUploadTime = TimeSpan.Zero,
                    OutOProcessMaxIdleTime = TimeSpan.Zero,
                    OutOfProcessUpload = true,
                };
                var args = new string[] { };
                TelemetryManager.Initialize(telemetryManagerConfig, args);

                TelemetryManager.SetContext("CommonDateTime", DateTime.Now);
                TelemetryManager.SetContext("CommonString", "my string");
                TelemetryManager.SetContext("CommonLong", 123);
                TelemetryManager.SetContext("CommonDouble", 123.123);
                TelemetryManager.SetContext("CommonGuid", Guid.NewGuid());
                TelemetryManager.SetContext("CommonBool", true);
                TelemetryManager.SetContext("CommonPIIData", "username", isPii: true);

                var telemetryLogger = typeof(TelemetryManager)
                                      .GetField("telemetryLogger", BindingFlags.Static | BindingFlags.NonPublic)!
                                      .GetValue(null) as Applications.Events.ILogger;
                telemetryLogger!.SetContext("CommonLong", (sbyte)123);
                telemetryLogger!.SetContext("CommonLong", (byte)123);
                telemetryLogger!.SetContext("CommonLong", (ushort)123);
                telemetryLogger!.SetContext("CommonLong", (short)123);
                telemetryLogger!.SetContext("CommonLong", 123u);
                telemetryLogger!.SetContext("CommonLong", (int)123);

                TelemetryManager.LogObject(this.CreateTestEventObject());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);
            }
            finally
            {
                TelemetryManager.TearDown();
            }
        }

        [TestMethod]
        public void TestTelemetryOptOut()
        {
            var telemetryManagerConfig = this.GetConfig();
            try
            {
                Environment.SetEnvironmentVariable(telemetryManagerConfig.TelemetryOptOutVariableName, "1");

                TelemetryManager.Initialize(telemetryManagerConfig);

                TelemetryManager.LogEvent("MyEventName");
                TelemetryManager.LogEvent(this.CreateEventPropertiesObject());
                TelemetryManager.LogObject(this.CreateTestEventObject());
                Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
            }
            finally
            {
                TelemetryManager.TearDown();
                Environment.SetEnvironmentVariable(telemetryManagerConfig.TelemetryOptOutVariableName, "");
            }
        }

        [TestMethod]
        public void TestEnableTelemetryExceptions()
        {
            var telemetryManagerConfig = this.GetConfig();

            try
            {
                Environment.SetEnvironmentVariable(telemetryManagerConfig.EnableTelemetryExceptionsVariableName, "1");

                TelemetryManager.Initialize(telemetryManagerConfig);

                Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                    TelemetryManager.LogEvent("####"));
                Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
            }
            finally
            {
                TelemetryManager.TearDown();
                Environment.SetEnvironmentVariable(telemetryManagerConfig.EnableTelemetryExceptionsVariableName, "");
            }

            try
            {
                Environment.SetEnvironmentVariable(telemetryManagerConfig.EnableTelemetryExceptionsVariableName, "0");

                TelemetryManager.Initialize(telemetryManagerConfig);

                TelemetryManager.LogEvent("####");
                Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
            }
            finally
            {
                TelemetryManager.TearDown();
                Environment.SetEnvironmentVariable(telemetryManagerConfig.EnableTelemetryExceptionsVariableName, "");
            }
        }
    }

    public enum SampleEnumType
    {
        SampleEnumValue1,
        SampleEnumValue2,
    }

    public record TestEvent
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
