// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

open System
open Microsoft.Quantum.Telemetry

// By default, we set the TestMode to true unless the
// TELEMETRY compiler constant is passed during the build.
// We do that to prevent unintentional telemetry to be sent
// to Microsoft via the Sample Telemetry app.
let IsTestMode =
#if TELEMETRY
    false
#else
    true
#endif

type SampleUnionType =
    | SampleEnumValue1
    | SampleEnumValue2

type ExecutionCompleted =
    {
        SampleDateTime: DateTime
        SampleString: string
        SampleBool: bool
        SampleEnum: SampleUnionType
        [<PiiData>]
        SamplePII: string
        [<SerializeJson>]
        SampleArray: string []
        SampleTimeSpan: TimeSpan
        SampleInt: int
        SampleGuid: Guid
        [<SerializeJson>]
        SampleObjectToBeSerialized: Map<string, string>
        SampleObjectToBeIgnored: obj
        SampleException: Exception
        SampleOptionWithValue: int option
        SampleOptionWithNone: int option
    }

// The following 3 functions are just an artifact to
// simulate the raise of a exception with a call stack
let throwAnException throw : Unit =
    if throw then raise (System.IO.FileNotFoundException(@"File path 'C:\Users\johndoe\file.txt'"))

let throwANestedException throw = throwAnException throw

let createExceptionWithStackTrace throw : Exception =
    try
        throwANestedException throw
        null
    with
    | ex -> ex

[<EntryPoint>]
let main args =
    // Initialize the TelemetryManager
    // When TestMode is true, the events won't be sent to Microsoft servers
    // but will only be printed to the Debug Console
    let telemetryConfig =
        TelemetryManagerConfig(
            AppId = "SampleFSharpApp",
            HostingEnvironmentVariableName = "SAMPLEFSHARPAPP_HOSTING_ENV",
            TelemetryOptOutVariableName = "QDK_TELEMETRY_OPT_OUT",
            MaxTeardownUploadTime = TimeSpan.FromSeconds(2.0),
            OutOfProcessUpload = true,
            ExceptionLoggingOptions =
                ExceptionLoggingOptions(CollectTargetSite = true, CollectSanitizedStackTrace = true),
            SendTelemetryInitializedEvent = true,
            SendTelemetryTearDownEvent = true,
            DefaultTelemetryConsent = ConsentKind.OptedIn,
            TestMode = IsTestMode
        )

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
    use _telemetryManagerHandle = TelemetryManager.Initialize(telemetryConfig, args)

    try
        // OPTIONAL: Context Properties
        // Set context properties that are included in every event
        // If an event later set a property with the same name, the context property
        // will be completely overridden in that specific event instance.
        TelemetryManager.SetContext("CommonDateTime", DateTime.Now)
        TelemetryManager.SetContext("CommonString", "my string")
        TelemetryManager.SetContext("CommonLong", 123)
        TelemetryManager.SetContext("CommonDouble", 123.123)
        TelemetryManager.SetContext("CommonGuid", Guid.NewGuid())
        TelemetryManager.SetContext("CommonBool", true)
        TelemetryManager.SetContext("CommonPIIData", "username", isPii = true)



        // OPTIONAL: Manually construct an Aria event
        // Log an event using Aria EventProperties object
        // Properties that contain PII or customer data should be tagged
        // with the PiiKind != None
        let eventProperties = Microsoft.Applications.Events.EventProperties(Name = "SampleEvent")
        eventProperties.SetProperty("SampleDateTime", DateTime.Now) |> ignore
        eventProperties.SetProperty("SampleString", "my string") |> ignore
        eventProperties.SetProperty("SampleLong", 123) |> ignore
        eventProperties.SetProperty("SampleDouble", 123.123) |> ignore
        eventProperties.SetProperty("SampleGuid", Guid.NewGuid()) |> ignore
        eventProperties.SetProperty("SampleBool", true) |> ignore

        eventProperties.SetProperty("SamplePIIData", "username", Microsoft.Applications.Events.PiiKind.Identity)
        |> ignore

        TelemetryManager.LogEvent(eventProperties)

        // OPTIONAL: Just log an event name
        // Log just the event name, with no extra properties
        // Context properties will still be added.
        TelemetryManager.LogEvent("MyEventName")

        // OPTIONAL:
        // Log an Exception. The name of the event will be "Exception".
        // Note that when we log an exception, only the name of the class will be logged.
        // No properties of the exception will be logged as they can contain customer data
        try
            throwANestedException true
        with
        | ex -> TelemetryManager.LogObject(ex)


        // Log a custom object
        // Custom objects will have all of their non-null public properties
        // logged with some rules applied.
        // Please check the TelemetryManager.LogObject documentation.
        let executionCompletedEvent =
            {
                SampleDateTime = DateTime.Now
                SampleString = "sample string"
                SampleBool = true
                SampleEnum = SampleUnionType.SampleEnumValue1
                SamplePII = "PII data to be hashed"
                SampleArray = [| "element1"; "element2" |]
                SampleTimeSpan = TimeSpan(10, 9, 8, 7, 654)
                SampleInt = 42
                SampleObjectToBeSerialized =
                    Map [ ("key1", "value1")
                          ("key2", "value2") ]
                SampleObjectToBeIgnored = [ 1..10 ]
                SampleGuid = Guid.NewGuid()
                SampleException = createExceptionWithStackTrace true
                SampleOptionWithValue = Some 123
                SampleOptionWithNone = None
            }

        TelemetryManager.LogObject(executionCompletedEvent)

        0
    with
    | ex ->
        // RECOMMENDED: Log unhandled exceptions
        // Catch and log unhandled exceptions in main code.
        TelemetryManager.LogObject(ex)

        // Not recommended for production code.
        // This is only for debugging purposes of this sample program.
        printfn "UnhandledException:  %s" (ex.ToString())
        -1
