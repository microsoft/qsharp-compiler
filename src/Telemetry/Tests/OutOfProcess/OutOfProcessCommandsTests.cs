// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Applications.Events;
using Microsoft.Quantum.Telemetry.OutOfProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Quantum.Telemetry.Tests.OutOfProcess
{
    [TestClass]
    public class OutOfProcessCommandsTests : OutOfProcessCommandsTestCommon
    {
        [TestMethod]
        public void TestOutOfProcessLogEventCommand()
        {
            var command1 = CreateOutOfProcessLogEventCommand(0);
            var commandEqualTo1 = CreateOutOfProcessLogEventCommand(0);
            var commandDifferentThan1a = CreateOutOfProcessLogEventCommand(1);
            var commandDifferentThan1b = CreateOutOfProcessLogEventCommand(0);
            commandDifferentThan1b.Args.SetProperty("stringProp", "differentValue");
            var commandDifferentThan1c = CreateOutOfProcessQuitCommand();

            // Test object.Equals
            Assert.AreEqual(command1, commandEqualTo1);
            Assert.AreNotEqual(command1, commandDifferentThan1a);
            Assert.AreNotEqual(command1, commandDifferentThan1b);
            Assert.AreNotEqual(command1, commandDifferentThan1c);

            // Test object.GetHashCode
            Assert.AreEqual(command1.GetHashCode(), commandEqualTo1.GetHashCode());
            Assert.AreNotEqual(command1.GetHashCode(), commandDifferentThan1a.GetHashCode());
            Assert.AreNotEqual(command1.GetHashCode(), commandDifferentThan1c.GetHashCode());

            // The hashcodes in this case are equal because
            // it just considers the Type and event name
            Assert.AreEqual(command1.GetHashCode(), commandDifferentThan1b.GetHashCode());

            // Test Visitor Pattern on .Process method
            var outOfProcessServer = CreateNullOutOfProcessServer();
            command1.Process(outOfProcessServer);
            Assert.AreEqual(command1, outOfProcessServer.LastProcessedCommand);
            Assert.AreEqual(command1.GetType(), outOfProcessServer.LastProcessedType);
        }

        [TestMethod]
        public void TestOutOfProcessQuitCommand()
        {
            var command1 = CreateOutOfProcessQuitCommand();
            var commandEqualTo1 = CreateOutOfProcessQuitCommand();
            var commandDifferentThan1 = CreateOutOfProcessLogEventCommand(0);

            // Test object.Equals
            Assert.AreEqual(command1, commandEqualTo1);
            Assert.AreNotEqual(command1, commandDifferentThan1);

            // Test object.GetHashCode
            Assert.AreEqual(command1.GetHashCode(), commandEqualTo1.GetHashCode());
            Assert.AreNotEqual(command1.GetHashCode(), commandDifferentThan1.GetHashCode());

            // Test Visitor Pattern on .Process method
            var outOfProcessServer = CreateNullOutOfProcessServer();
            command1.Process(outOfProcessServer);
            Assert.AreEqual(command1, outOfProcessServer.LastProcessedCommand);
            Assert.AreEqual(command1.GetType(), outOfProcessServer.LastProcessedType);
        }

        [TestMethod]
        public void TestOutOfProcessSetContextCommand()
        {
            var commandList1 = CreateOutOfProcessSetContextCommands().ToList();
            var commandListEqualTo1 = CreateOutOfProcessSetContextCommands().ToList();

            for (int i = 0; i < commandList1.Count; i++)
            {
                var nextIndex = (i + 1) % commandList1.Count;
                var command1 = commandList1[i];
                var commandEqualTo1 = commandListEqualTo1[i];
                var commandDifferentThan1 = commandList1[nextIndex];

               // Test object.Equals
                Assert.AreEqual(command1, commandEqualTo1);
                Assert.AreNotEqual(command1, commandDifferentThan1);

                // Test object.GetHashCode
                Assert.AreEqual(command1.GetHashCode(), commandEqualTo1.GetHashCode());
                Assert.AreNotEqual(command1.GetHashCode(), commandDifferentThan1.GetHashCode());

                // Test Visitor Pattern on .Process method
                var outOfProcessServer = CreateNullOutOfProcessServer();
                command1.Process(outOfProcessServer);
                Assert.AreEqual(command1, outOfProcessServer.LastProcessedCommand);
                Assert.AreEqual(command1.GetType(), outOfProcessServer.LastProcessedType);
            }
        }

        internal class ExternalProcessMock : IExternalProcessConnector
        {
            public TextWriter InputTextWriter { get; private set; }

            public TextReader OutputTextReader { get; private set; }

            public bool IsRunning { get; private set; }

            public ExternalProcessMock(TextWriter inputTextWriter, TextReader outputTextReader)
            {
                this.InputTextWriter = inputTextWriter;
                this.OutputTextReader = outputTextReader;
            }

            public void Start()
            {
                this.IsRunning = true;
            }

            public void WaitForExit()
            {
                this.IsRunning = false;
            }
        }

        [TestMethod]
        public async Task TestOutOfProcessClientAndServer()
        {
            TelemetryManagerConfig telemetryManagerConfig = new TelemetryManagerConfig() with
            {
                OutOfProcessMaxTeardownUploadTime = TimeSpan.Zero,
                OutOfProcessMaxIdleTime = TimeSpan.FromSeconds(10),
                TestMode = true,
            };

            using var clientToServerStream = new MemoryStream();
            using var serverToClientStream = new MemoryStream();
            using var clientInputTextWriter = new StreamWriter(clientToServerStream)
                                                    {
                                                        AutoFlush = true,
                                                    };
            using var serverOutputTextReader = new StreamReader(serverToClientStream);
            using var serverInputTextReader = new StreamReader(clientToServerStream);

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                ExternalProcessMock externalProcessMock = new(clientInputTextWriter, serverOutputTextReader);
                OutOfProcessLogger outOfProcessLogger = new(telemetryManagerConfig, externalProcessMock);

                outOfProcessLogger.LogEvent(CreateEventProperties(0));

                outOfProcessLogger.SetContext("CommonDateTime", DateTime.Now);
                outOfProcessLogger.SetContext("CommonString", "my string");
                outOfProcessLogger.SetContext("CommonLong", 123);
                outOfProcessLogger.SetContext("CommonDouble", 123.123);
                outOfProcessLogger.SetContext("CommonGuid", Guid.NewGuid());
                outOfProcessLogger.SetContext("CommonBool", true);
                outOfProcessLogger.SetContext("CommonPIIData", "username", PiiKind.GenericData);
                outOfProcessLogger.SetContext("CommonSByte", (sbyte)123);
                outOfProcessLogger.SetContext("CommonByte", (byte)123);
                outOfProcessLogger.SetContext("CommonUShort", (ushort)123);
                outOfProcessLogger.SetContext("CommonShort", (short)123);
                outOfProcessLogger.SetContext("CommonUInt", 123u);
                outOfProcessLogger.SetContext("CommonInt", (int)123);

                outOfProcessLogger.Quit();

                externalProcessMock.WaitForExit();
            }

            clientToServerStream.Position = 0;
            serverToClientStream.Position = 0;

            using (TelemetryManager.Initialize(telemetryManagerConfig))
            {
                OutOfProcessServer outOfProcessServer = new(telemetryManagerConfig, serverInputTextReader);
                await outOfProcessServer.RunAndExitAsync();
            }
        }
    }
}