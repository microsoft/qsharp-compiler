// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Core;
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
                ExceptionLoggingOptions = new ExceptionLoggingOptions()
                {
                    CollectTargetSite = true,
                    CollectSanitizedStackTrace = true,
                },
                SendTelemetryInitializedEvent = false,
                SendTelemetryTearDownEvent = false,
                TestMode = true,
                DefaultTelemetryConsent = ConsentKind.OptedIn,
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
            throw this.CreateException();

        private Exception CreateException() =>
            new System.IO.FileNotFoundException(@"File path 'C:\Users\johndoe\file.txt'");

        private TestEvent CreateTestEventObject() =>
            new TestEvent()
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
                SampleException = this.CreateExceptionWithStackTrace(),
                SampleNullableWithValue = 123,
                SampleNullableWithNull = null,
            };

        private Microsoft.Applications.Events.EventProperties CreateEventPropertiesObject()
        {
            Microsoft.Applications.Events.EventProperties eventProperties = new Applications.Events.EventProperties()
            {
                Name = "SampleEvent",
            };
            eventProperties.SetProperty("SampleDateTime", DateTime.Now);
            eventProperties.SetProperty("SampleString", "my string");
            eventProperties.SetProperty("SampleLong", 123L);
            TypeConversionHelper.SetProperty(eventProperties, "SampleInt", 456);
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

        [TestCleanup]
        public void Cleanup()
        {
            TelemetryManager.TearDown();
        }

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
            Assert.AreEqual(TimeSpan.FromSeconds(30), telemetryManagerConfig.OutOfProcessMaxIdleTime);
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
            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                Assert.AreSame(TelemetryManager.Configuration, telemetryManagerConfig);
                Assert.AreEqual(TelemetryManager.Configuration.AppId, telemetryManagerConfig.AppId);
                Assert.AreEqual(TelemetryManager.Configuration.EventNamePrefix, telemetryManagerConfig.EventNamePrefix);
                Assert.AreEqual(TelemetryManager.Configuration.TelemetryOptOutVariableName, telemetryManagerConfig.TelemetryOptOutVariableName);
                Assert.AreEqual(TelemetryManager.Configuration.HostingEnvironmentVariableName, telemetryManagerConfig.HostingEnvironmentVariableName);
                Assert.AreEqual(TelemetryManager.Configuration.MaxTeardownUploadTime, telemetryManagerConfig.MaxTeardownUploadTime);
                Assert.AreEqual(TelemetryManager.Configuration.OutOfProcessMaxTeardownUploadTime, telemetryManagerConfig.OutOfProcessMaxTeardownUploadTime);
                Assert.AreEqual(TelemetryManager.Configuration.OutOfProcessMaxIdleTime, telemetryManagerConfig.OutOfProcessMaxIdleTime);
                Assert.AreEqual(TelemetryManager.Configuration.OutOfProcessPollWaitTime, telemetryManagerConfig.OutOfProcessPollWaitTime);
                Assert.AreEqual(TelemetryManager.Configuration.OutOfProcessUpload, telemetryManagerConfig.OutOfProcessUpload);
                Assert.AreEqual(TelemetryManager.Configuration.SendTelemetryInitializedEvent, telemetryManagerConfig.SendTelemetryInitializedEvent);
                Assert.AreEqual(TelemetryManager.Configuration.SendTelemetryTearDownEvent, telemetryManagerConfig.SendTelemetryTearDownEvent);
                Assert.IsNotNull(telemetryManagerConfig.ExceptionLoggingOptions);
                Assert.AreEqual(TelemetryManager.Configuration.ExceptionLoggingOptions.CollectTargetSite, telemetryManagerConfig.ExceptionLoggingOptions.CollectTargetSite);
                Assert.AreEqual(TelemetryManager.Configuration.ExceptionLoggingOptions.CollectSanitizedStackTrace, telemetryManagerConfig.ExceptionLoggingOptions.CollectSanitizedStackTrace);
            }

            // Send initialization and teardown events
            Assert.AreEqual(0, TelemetryManager.TotalEventsCount);

            var telemetryManagerConfigWithEvents = this.GetConfig();
            telemetryManagerConfigWithEvents.SendTelemetryInitializedEvent = true;
            telemetryManagerConfigWithEvents.SendTelemetryTearDownEvent = true;
            using (TelemetryManager.Initialize(telemetryManagerConfigWithEvents))
            {
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);
            }

            Assert.AreEqual(0, TelemetryManager.TotalEventsCount);

            // The second time we do a teardown, it should do nothing and not raise an exception
            TelemetryManager.TearDown();

            // Try initialize it more than once
            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                Assert.ThrowsException<InvalidOperationException>(() =>
                    TelemetryManager.Initialize(telemetryManagerConfig));
            }

            // Try passing args and check that they are not changed
            var originalArgs = new string[] { "arg1", Guid.NewGuid().ToString() };
            var args = originalArgs.ToArray();
            using (TelemetryManager.Initialize(telemetryManagerConfig, args))
            {
                CollectionAssert.AreEqual(originalArgs, args);
            }

            var outOfProcessConfig = this.GetConfig();
            outOfProcessConfig.OutOfProcessUpload = true;
            outOfProcessConfig.OutOfProcessMaxIdleTime = TimeSpan.Zero;
            outOfProcessConfig.OutOfProcessMaxTeardownUploadTime = TimeSpan.Zero;
            outOfProcessConfig.OutOfProcessExecutablePath = TestCommon.GetOutOfProcessExecutablePath();
            outOfProcessConfig.SendTelemetryInitializedEvent = false;
            outOfProcessConfig.SendTelemetryTearDownEvent = false;

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
            args = new string[] { };
            using (TelemetryManager.Initialize(outOfProcessConfig, args))
            {
            }

            // Try with OutOfProcessUpload=true, args=OUTOFPROCESSUPLOADARG
            args = new string[]
            {
                TelemetryManagerConstants.OUTOFPROCESSUPLOADARG,
                TelemetryManagerConstants.TESTMODE,
            };
            using (TelemetryManager.Initialize(outOfProcessConfig, args))
            {
            }
        }

        [TestMethod]
        public void TestLogObjectNull()
        {
            var telemetryManagerConfig = this.GetConfig();

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                TelemetryManager.LogObject(null!);
                Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
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
            var telemetryManagerConfig = this.GetConfig();
            telemetryManagerConfig.ExceptionLoggingOptions = new ExceptionLoggingOptions()
                {
                    CollectSanitizedStackTrace = true,
                    CollectTargetSite = true,
                };

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                TelemetryManager.LogObject(this.CreateExceptionWithStackTrace());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);

                TelemetryManager.LogObject(this.CreateException());
                Assert.AreEqual(2, TelemetryManager.TotalEventsCount);
            }

            telemetryManagerConfig = this.GetConfig();
            telemetryManagerConfig.ExceptionLoggingOptions = new ExceptionLoggingOptions()
                {
                    CollectSanitizedStackTrace = false,
                    CollectTargetSite = false,
                };

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                TelemetryManager.LogObject(this.CreateExceptionWithStackTrace());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);
            }
        }

        [TestMethod]
        public void TestLogObject()
        {
            var telemetryManagerConfig = this.GetConfig();

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                TelemetryManager.LogObject(this.CreateTestEventObject());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);

                TelemetryManager.LogObject("MyCustomEventName", this.CreateTestEventObject());
                Assert.AreEqual(2, TelemetryManager.TotalEventsCount);
            }
        }

        [TestMethod]
        public void TestLogEvent()
        {
            var telemetryManagerConfig = this.GetConfig();

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                TelemetryManager.LogEvent(this.CreateEventPropertiesObject());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);

                TelemetryManager.LogObject(this.CreateEventPropertiesObject());
                Assert.AreEqual(2, TelemetryManager.TotalEventsCount);
            }
        }

        [TestMethod]
        public void TestLogEventName()
        {
            var telemetryManagerConfig = this.GetConfig();

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                TelemetryManager.LogEvent("MyEventName");
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);

                foreach (var value in this.invalidValues)
                {
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                        TelemetryManager.LogEvent(value));
                }
            }
        }

        [TestMethod]
        public void TestSetContext()
        {
            var telemetryManagerConfig = this.GetConfig();

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                TelemetryManager.SetContext("CommonDateTime", DateTime.Now);
                TelemetryManager.SetContext("CommonString", "my string");
                TelemetryManager.SetContext("CommonLong", 123);
                TelemetryManager.SetContext("CommonDouble", 123.123);
                TelemetryManager.SetContext("CommonGuid", Guid.NewGuid());
                TelemetryManager.SetContext("CommonBool", true);
                TelemetryManager.SetContext("CommonPIIData", "username", isPii: true);
                TelemetryManager.SetContext("CommonPIIData2", "username", TelemetryPropertyType.String, isPii: true);
            }
        }

        [TestMethod]
        public void TestTelemetryOptOut()
        {
            var telemetryManagerConfig = this.GetConfig();
            try
            {
                Environment.SetEnvironmentVariable(telemetryManagerConfig.TelemetryOptOutVariableName, "1");

                using (TelemetryManager.Initialize(telemetryManagerConfig))
                {
                    TelemetryManager.LogEvent("MyEventName");
                    TelemetryManager.LogEvent(this.CreateEventPropertiesObject());
                    TelemetryManager.LogObject(this.CreateTestEventObject());
                    Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(telemetryManagerConfig.TelemetryOptOutVariableName, "");
            }
        }

        [TestMethod]
        public void TestTelemetryOptIn()
        {
            var telemetryManagerConfig = this.GetConfig();
            telemetryManagerConfig.DefaultTelemetryConsent = ConsentKind.OptedOut;
            try
            {
                using (TelemetryManager.Initialize(telemetryManagerConfig))
                {
                    TelemetryManager.LogEvent("MyEventName");
                    TelemetryManager.LogEvent(this.CreateEventPropertiesObject());
                    TelemetryManager.LogObject(this.CreateTestEventObject());
                    Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
                }

                Environment.SetEnvironmentVariable(telemetryManagerConfig.TelemetryOptInVariableName, "1");
                Environment.SetEnvironmentVariable(telemetryManagerConfig.TelemetryOptOutVariableName, "1");

                using (TelemetryManager.Initialize(telemetryManagerConfig))
                {
                    TelemetryManager.LogEvent("MyEventName");
                    TelemetryManager.LogEvent(this.CreateEventPropertiesObject());
                    TelemetryManager.LogObject(this.CreateTestEventObject());
                    Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
                }

                Environment.SetEnvironmentVariable(telemetryManagerConfig.TelemetryOptOutVariableName, "");

                using (TelemetryManager.Initialize(telemetryManagerConfig))
                {
                    TelemetryManager.LogEvent("MyEventName");
                    TelemetryManager.LogEvent(this.CreateEventPropertiesObject());
                    TelemetryManager.LogObject(this.CreateTestEventObject());
                    Assert.AreEqual(3, TelemetryManager.TotalEventsCount);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(telemetryManagerConfig.TelemetryOptInVariableName, "");
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

                using (TelemetryManager.Initialize(telemetryManagerConfig))
                {
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                        TelemetryManager.LogEvent("####"));
                    Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(telemetryManagerConfig.EnableTelemetryExceptionsVariableName, "");
            }

            try
            {
                Environment.SetEnvironmentVariable(telemetryManagerConfig.EnableTelemetryExceptionsVariableName, "0");

                using (TelemetryManager.Initialize(telemetryManagerConfig))
                {
                    TelemetryManager.LogEvent("####");
                    Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(telemetryManagerConfig.EnableTelemetryExceptionsVariableName, "");
            }
        }
    }

    public enum SampleEnumType
    {
        SampleEnumValue1,
        SampleEnumValue2,
    }

    public class TestEvent
    {
        public DateTime SampleDateTime { get; set; } = DateTime.Now;

        public string? SampleString { get; set; } = "sample string";

        public bool SampleBool { get; set; } = true;

        public SampleEnumType SampleEnum { get; set; } = SampleEnumType.SampleEnumValue1;

        [PiiData]
        public string? SamplePII { get; set; } = "myusername";

        [SerializeJson]
        public string[]? SampleArray { get; set; } = new string[] { "element1", "element2" };

        public TimeSpan SampleTimeSpan { get; set; } = new TimeSpan(1, 2, 3, 4, 5);

        public Guid SampleGuid { get; set; } = Guid.NewGuid();

        [SerializeJson]
        public Dictionary<string, string> SampleDictionary { get; set; } = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "value2" },
            };

        public object? SampleGenericObject { get; set; } = TimeZoneInfo.Local;

        public Exception? SampleException { get; set; }

        public int? SampleNullableWithValue { get; set; } = 123;

        public int? SampleNullableWithNull { get; set; } = null;

        public FSharpOption<int> SampleFSharpOptionWithValue { get; set; } = 123;

        public FSharpOption<int> SampleFSharpOptionWithNone { get; set; } = FSharpOption<int>.None;

        public sbyte SampleSByte { get; set; } = 1;

        public byte SampleByte { get; set; } = 2;

        public ushort SampleUShort { get; set; } = 3;

        public short SampleShort { get; set; } = 4;

        public uint SampleUInt { get; set; } = 5;

        public int SampleInt { get; set; } = 6;

        public ulong SampleULong { get; set; } = 7;

        public long SampleLong { get; set; } = 8;

        public float SampleFloat { get; set; } = 9.1F;

        public double SampleDouble { get; set; } = 9.2;

        public decimal SampleDecimal { get; set; } = 9.3M;
    }
}
