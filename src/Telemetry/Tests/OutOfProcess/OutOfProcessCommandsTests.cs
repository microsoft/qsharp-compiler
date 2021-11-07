// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Applications.Events;
using Microsoft.Quantum.Telemetry.OutOfProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Quantum.Telemetry.Tests.OutOfProcess
{
    [TestClass]
    public class OutOfProcessTests : TestCommon
    {
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

            public void Kill()
            {
                this.IsRunning = false;
            }

            public void Dispose()
            {
            }
        }

        [TestMethod]
        public void TestOutOfProcessExecutable()
        {
            var outOfProcessExecutablePath = TestCommon.GetOutOfProcessExecutablePath();

            TelemetryManagerConfig telemetryManagerConfig = new TelemetryManagerConfig()
            {
                OutOfProcessExecutablePath = outOfProcessExecutablePath,
                OutOfProcessUpload = true,
                TestMode = true,
            };

            string[] args = new string[0];
            using (TelemetryManager.Initialize(telemetryManagerConfig, args))
            {
                TelemetryManager.Configuration.EnableTelemetryExceptions = false;
                TelemetryManager.LogEvent("MyEvent1");
                TelemetryManager.LogEvent("break");
                TelemetryManager.LogEvent("MyEvent2");
            }
        }

        [TestMethod]
        public void TestOutOfProcessClientAndServer()
        {
            TelemetryManagerConfig telemetryManagerConfig = new TelemetryManagerConfig()
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
                ExternalProcessMock externalProcessMock = new ExternalProcessMock(clientInputTextWriter, serverOutputTextReader);
                OutOfProcessLogger outOfProcessLogger = new OutOfProcessLogger(telemetryManagerConfig, externalProcessMock);

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
                OutOfProcessServer outOfProcessServer = new OutOfProcessServer(telemetryManagerConfig, serverInputTextReader);
                outOfProcessServer.RunAndExit();
            }
        }
    }
}
