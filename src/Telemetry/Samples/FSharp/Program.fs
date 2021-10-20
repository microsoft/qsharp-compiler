// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

open System
open System.Collections.Generic
open Microsoft.Quantum.Telemetry
open Microsoft.Applications.Events

type SampleEnumType =
    | SampleEnumValue1
    | SampleEnumValue2

[<Struct>]
type ExecutionCompleted
    (
        ?sampleDateTime: DateTime,
        ?sampleString: string,
        ?sampleBool: bool,
        ?sampleEnum: SampleEnumType,
        ?samplePII: string,
        ?sampleArray: string [],
        ?sampleTimeSpan: TimeSpan,
        ?sampleInt: int,
        ?sampleGuid: Guid,
        ?sampleDictionary: IDictionary<string, string>,
        ?sampleGenericObject: Object,
        ?sampleException: Exception
    ) =
    member this.SampleDateTime = defaultArg sampleDateTime DateTime.Now
    member this.SampleString = defaultArg sampleString null
    member this.SampleBool = defaultArg sampleBool false
    member this.SampleEnum = defaultArg sampleEnum SampleEnumType.SampleEnumValue1

    [<PiiData>]
    member this.SamplePII = defaultArg samplePII null

    [<SerializeJson>]
    member this.SampleArray = defaultArg sampleArray null

    member this.SampleTimeSpan = defaultArg sampleTimeSpan TimeSpan.Zero
    member this.SampleInt = defaultArg sampleInt 0
    member this.SampleGuid = defaultArg sampleGuid Guid.Empty

    [<SerializeJson>]
    member this.SampleDictionary = defaultArg sampleDictionary null

    member this.SampleGenericObject = defaultArg sampleGenericObject null
    member this.SampleException = defaultArg sampleException null

[<EntryPoint>]
let main args =
    try
        // Initialize the TelemetryManager
        let telemetryConfig =
            TelemetryManagerConfig(
                AppId = "SampleFSharpApp",
                HostingEnvironmentVariableName = "SAMPLEFSHARPAPP_HOSTING_ENV",
                TelemetryOptOutVariableName = "QDK_TELEMETRY_OPT_OUT",
                MaxTeardownUploadTime = TimeSpan.FromSeconds(2.0),
                OutOfProcessUpload = false
            )

        TelemetryManager.Initialize(telemetryConfig, args)

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
            //TelemetryManager.LogObject(ex);

            // Log a custom object
            // Custom objects will have all of their non-null public properties
            // logged with some rules applied.
            // Please check the TelemetryManager.LogObject documentation.
            let executionCompletedEvent =
                ExecutionCompleted(
                    sampleDateTime = DateTime.Now,
                    sampleString = "sample string",
                    sampleBool = true,
                    sampleEnum = SampleEnumType.SampleEnumValue1,
                    samplePII = "PII data to be hashed",
                    sampleArray = [| "element1"; "element2" |],
                    sampleTimeSpan = new TimeSpan(10, 9, 8, 7, 654),
                    sampleInt = 42,
                    sampleDictionary =
                        dict [ "key1", "value1"
                               "key2", "value2" ],
                    sampleGenericObject = new Dictionary<int, string>(),
                    sampleGuid = Guid.NewGuid(),
                    sampleException = unhandledException
                )

            TelemetryManager.LogObject(executionCompletedEvent)

            0
        with
        | ex ->
            TelemetryManager.LogObject(ex)
            -1
    finally
        TelemetryManager.TearDown()
