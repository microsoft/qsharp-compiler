// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Quantum.Telemetry.OutOfProcess;
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
            throw this.CreateException();

        private Exception CreateException() =>
            new System.IO.FileNotFoundException(@"File path 'C:\Users\johndoe\file.txt'");

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
            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
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

            // Send initialization and teardown events
            Assert.AreEqual(0, TelemetryManager.TotalEventsCount);
            using (TelemetryManager.Initialize(telemetryManagerConfig with
                                                {
                                                    SendTelemetryInitializedEvent = true,
                                                    SendTelemetryTearDownEvent = true,
                                                }))
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
            args = new string[] { };
            using (TelemetryManager.Initialize(outOfProcessConfig, args))
            {
            }

            // Try with OutOfProcessUpload=true, args=OUTOFPROCESSUPLOADARG
            args = new string[] { TelemetryManager.OUTOFPROCESSUPLOADARG, TelemetryManager.TESTMODE };

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
            var telemetryManagerConfig = this.GetConfig() with
            {
                ExceptionLoggingOptions = new()
                {
                    CollectSanitizedStackTrace = true,
                    CollectTargetSite = true,
                },
            };

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                TelemetryManager.LogObject(this.CreateExceptionWithStackTrace());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);

                TelemetryManager.LogObject(this.CreateException());
                Assert.AreEqual(2, TelemetryManager.TotalEventsCount);
            }

            telemetryManagerConfig = this.GetConfig() with
            {
                ExceptionLoggingOptions = new()
                {
                    CollectSanitizedStackTrace = false,
                    CollectTargetSite = false,
                },
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

        // [TestMethod]
        public void TestLogObjectOutOfProcess()
        {
            var telemetryManagerConfig = this.GetConfig() with
            {
                OutOfProcessMaxTeardownUploadTime = TimeSpan.Zero,
                OutOProcessMaxIdleTime = TimeSpan.Zero,
                OutOfProcessUpload = true,
            };
            var args = new string[] { };

            using (TelemetryManager.Initialize(telemetryManagerConfig, args))
            {
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
                telemetryLogger!.SetContext("CommonSByte", (sbyte)123);
                telemetryLogger!.SetContext("CommonByte", (byte)123);
                telemetryLogger!.SetContext("CommonUShort", (ushort)123);
                telemetryLogger!.SetContext("CommonShort", (short)123);
                telemetryLogger!.SetContext("CommonUInt", 123u);
                telemetryLogger!.SetContext("CommonInt", (int)123);

                TelemetryManager.LogObject(this.CreateTestEventObject());
                Assert.AreEqual(1, TelemetryManager.TotalEventsCount);
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

        private static async IAsyncEnumerable<T> EnumerableToAsyncEnumerable<T>(IEnumerable<T> items, int delayInMilliseconds = 1)
        {
            foreach (var item in items)
            {
                await Task.Delay(delayInMilliseconds);
                yield return item;
            }
        }

        private static async Task<IEnumerable<T>> AsyncEnumerableToEnumerable<T>(IAsyncEnumerable<T> items)
        {
            List<T> result = new();
            await foreach (var item in items)
            {
                result.Add(item);
            }

            return result;
        }

        private static OutOfProcessLogEventCommand CreateOutOfProcessLogEventCommand(int seed)
        {
            OutOfProcessLogEventCommand outOfProcessLogEventCommand = new(new Applications.Events.EventProperties());
            outOfProcessLogEventCommand.Args.Name = $"eventName{seed}";
            outOfProcessLogEventCommand.Args.SetProperty("stringProp", $"stringPropValue{seed}");
            outOfProcessLogEventCommand.Args.SetProperty("stringMultilineProp", $"line1_{seed}\r\nline2\r\nline3");
            outOfProcessLogEventCommand.Args.SetProperty("longProp", 123L + seed);
            outOfProcessLogEventCommand.Args.SetProperty("doubleProp", (double)123.123 + seed);
            outOfProcessLogEventCommand.Args.SetProperty("dateTimeProp", new DateTime(2021, 08, 12, 08, 09, 10) + TimeSpan.FromHours(seed));
            outOfProcessLogEventCommand.Args.SetProperty("boolProp", true);
            outOfProcessLogEventCommand.Args.SetProperty("guidProp", Guid.NewGuid());
            outOfProcessLogEventCommand.Args.SetProperty("stringPropPii", "stringPropValue{seed}", Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("longPropPii", 123L + seed, Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("doublePropPii", (double)123.123 + seed, Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("dateTimePropPii", new DateTime(2021, 08, 12, 08, 09, 10) + TimeSpan.FromHours(seed), Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("boolPropPii", true, Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("guidPropPii", Guid.NewGuid(), Applications.Events.PiiKind.GenericData);
            return outOfProcessLogEventCommand;
        }

        private static IEnumerable<OutOfProcessLogEventCommand> CreateOutOfProcessLogEventCommands()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return CreateOutOfProcessLogEventCommand(seed: i);
            }
        }

        private static IEnumerable<OutOfProcessSetContextCommand> CreateOutOfProcessSetContextCommands()
        {
            var setContextArgs = new SetContextArgs[]
            {
                new SetContextArgs("commonString", "commonStringValue", TelemetryPropertyType.String, false),
                new SetContextArgs("commonBool", true, TelemetryPropertyType.Boolean, false),
                new SetContextArgs("commonDateTime", new DateTime(2021, 08, 12, 08, 09, 10), TelemetryPropertyType.DateTime, false),
                new SetContextArgs("commonDouble", (double)123.123, TelemetryPropertyType.Double, false),
                new SetContextArgs("commonGuid", Guid.NewGuid(), TelemetryPropertyType.Guid, false),
                new SetContextArgs("commonLong", 123L, TelemetryPropertyType.Long, false),
                new SetContextArgs("commonStringPii", "commonStringValue", TelemetryPropertyType.String, true),
                new SetContextArgs("commonBoolPii", true, TelemetryPropertyType.Boolean, true),
                new SetContextArgs("commonDateTimePii", new DateTime(2021, 08, 12, 08, 09, 10), TelemetryPropertyType.DateTime, true),
                new SetContextArgs("commonDoublePii", (double)123.123, TelemetryPropertyType.Double, true),
                new SetContextArgs("commonGuidPii", Guid.NewGuid(), TelemetryPropertyType.Guid, true),
                new SetContextArgs("commonLongPii", 123L, TelemetryPropertyType.Long, true),
            };
            return setContextArgs.Select((args) => new OutOfProcessSetContextCommand(args));
        }

        [TestMethod]
        public async Task TestSimpleYamlSerializer()
        {
            var yamlSerializer = new SimpleYamlSerializer();

            List<OutOfProcessCommand> commands = new();
            commands.AddRange(CreateOutOfProcessSetContextCommands());
            commands.AddRange(CreateOutOfProcessLogEventCommands());
            commands.Add(new OutOfProcessQuitCommand());

            var yamlResults = yamlSerializer.Write(commands).ToList();
            var asyncCommandResults = yamlSerializer.Read(EnumerableToAsyncEnumerable(yamlResults));
            var commandResults = (await AsyncEnumerableToEnumerable(asyncCommandResults)).ToList();

            Assert.AreEqual(commands.Count, commandResults.Count);
            for (int i = 0; i < commands.Count; i++)
            {
                Assert.AreEqual(commands[i], commandResults[i]);
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
        public Dictionary<string, string>? SampleDictionary { get; set; } = new()
            {
                { "key1", "value1" },
                { "key2", "value2" },
            };

        public object? SampleGenericObject { get; set; } = TimeZoneInfo.Local;

        public Exception? SampleException { get; set; }

        public int? SampleNullableWithValue { get; set; } = 123;

        public int? SampleNullableWithNull { get; set; } = null;

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
