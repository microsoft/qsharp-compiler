// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.Telemetry.Tests.OutOfProcess
{
    public class OutOfProcessTestException : Exception
    {
        public OutOfProcessTestException(string message)
            : base(message)
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("OutOfProcess.exe: started");
            try
            {
                TelemetryManagerConfig telemetryConfig = new TelemetryManagerConfig()
                {
                    OutOfProcessUpload = true,
                    TestMode = true,
                };

                using (TelemetryManager.Initialize(telemetryConfig, args))
                {
                    TelemetryManager.OnEventLogged += (sender, eventsProperty) =>
                    {
                        if (eventsProperty.Name.EndsWith("_break"))
                        {
                            Console.WriteLine("OutOfProcess.exe: application will throw an unhandled exception now!");
                            throw new OutOfProcessTestException("Exception thrown on purpose when received an event named 'break'.");
                        }
                    };

                    TelemetryManager.LogEvent("Event1");
                    TelemetryManager.LogEvent("break");
                    TelemetryManager.LogEvent("Event2");
                }
            }
            finally
            {
                Console.WriteLine("OutOfProcess.exe: finished");
            }
        }
    }
}
