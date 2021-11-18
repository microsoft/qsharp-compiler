// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry
{
    public static class TelemetryManagerConstants
    {
        internal const string TOKEN = "55aee962ee9445f3a86af864fc0fa766-48882422-3439-40de-8030-228042bd9089-7794";
        public const string OUTOFPROCESSUPLOADARG = "--OUT_OF_PROCESS_TELEMETRY_UPLOAD";
        public const string TESTMODE = "--TELEMETRY_TEST_MODE";

        #if DEBUG
        public const bool IsDebugBuild = true;
        #else
        public const bool IsDebugBuild = false;
        #endif
    }
}
