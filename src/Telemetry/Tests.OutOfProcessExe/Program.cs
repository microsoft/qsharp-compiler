// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.Telemetry.Tests.OutOfProcess
{
    [System.Serializable]
    public class OutOfProcessTestException : Exception
    {
        public OutOfProcessTestException() { }
        public OutOfProcessTestException(string message) : base(message) { }
        public OutOfProcessTestException(string message, System.Exception inner) : base(message, inner) { }
        protected OutOfProcessTestException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("OutOfProcess.exe: started");
            try
            {
                var telemetryConfig = new TelemetryManagerConfig();
                TelemetryManager.OnEventLogged += (sender, eventsProperty) =>
                {
                    if (eventsProperty.Name.EndsWith("_break"))
                    {
                        Console.WriteLine("OutOfProcess.exe: application will throw an unhandled exception now!");
                        throw new OutOfProcessTestException("Exception thrown on purpose when received an event named 'break'.");
                    }
                };
                TelemetryManager.Initialize(telemetryConfig, args);
            }
            finally
            {
                Console.WriteLine("OutOfProcess.exe: finished");
            }
        }
    }
}
