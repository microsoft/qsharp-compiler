// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

open System
open System.Collections.Generic
open Microsoft.Quantum.Telemetry

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
    }

[<EntryPoint>]
let main args =
    // Initialize the TelemetryManager
    let telemetryConfig =
        TelemetryManagerConfig(
            AppId = "SampleFSharpApp",
            HostingEnvironmentVariableName = "SAMPLEFSHARPAPP_HOSTING_ENV",
            TelemetryOptOutVariableName = "QDK_TELEMETRY_OPT_OUT",
            MaxTeardownUploadTime = TimeSpan.FromSeconds(2.0),
            OutOfProcessUpload = false
        )

    use _telemetryManagerHandle = TelemetryManager.Initialize(telemetryConfig, args)

    try
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

        // Log just the event name
        TelemetryManager.LogEvent("MyEventName")

        // Log an Exception
        // Note that when we log an exception, only the name of the class will be logged.
        // No properties of the exception will be logged as they can contain customer data
        let unhandledException =
            try
                raise (System.IO.FileNotFoundException(@"File path 'C:\Users\johndoe\file.txt'"))
            with
            | ex -> ex

        TelemetryManager.LogObject(unhandledException)

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
                SampleObjectToBeIgnored = [ 1 .. 10 ]
                SampleGuid = Guid.NewGuid()
                SampleException = unhandledException
            }

        TelemetryManager.LogObject(executionCompletedEvent)

        0
    with
    | ex ->
        TelemetryManager.LogObject(ex)
        -1
