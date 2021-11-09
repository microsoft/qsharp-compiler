// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Telemetry

open System
open System.Collections.Generic
open Microsoft.Quantum.QsFmt.App.Arguments
open Microsoft.Quantum.QsFmt.Formatter
open Microsoft.Quantum.Telemetry
open CommandLine

type internal ExecutionCompleted =
    {
        StartTime: DateTime
        Command: CommandKind option
        InputKind: InputKind option
        RecurseFlag: bool
        BackupFlag: bool
        QSharpVersion: string
        UnhandledException: Exception option
        [<SerializeJson>]
        SyntaxErrors: Errors.SyntaxError list option
        ExecutionTime: TimeSpan
        FilesProcessed: int
        ExitCode: ExitCode
    }

let internal initializeTelemetry args =
    let telemetryConfig =
        TelemetryManagerConfig(
            AppId = "QsFmt",
            HostingEnvironmentVariableName = "QSFMT_HOSTING_ENV",
            TelemetryOptOutVariableName = "QSFMT_TELEMETRY_OPT_OUT",
            MaxTeardownUploadTime = TimeSpan.FromSeconds(2.0),
            OutOfProcessUpload = true,
            ExceptionLoggingOptions =
                ExceptionLoggingOptions(CollectTargetSite = true, CollectSanitizedStackTrace = true),
            SendTelemetryInitializedEvent = false,
            SendTelemetryTearDownEvent = false,
            #if TELEMETRY
            TestMode = false
            #else
            TestMode = true
            #endif
        )

    TelemetryManager.Initialize(telemetryConfig, args)

let internal logExecutionCompleted
    (parseArgsResult: Result<ParserResult<obj>, Exception>)
    (runResult: Result<RunResult, Exception>)
    (startTime: DateTime)
    (executionTime: TimeSpan)
    =

    let (syntaxErrors, filesProcessed, exitCode, unhandledException) =
        match runResult with
        | Ok runResult -> (Some runResult.SyntaxErrors, runResult.FilesProcessed, runResult.ExitCode, None)
        | Error ex -> (None, 0, ExitCode.UnhandledException, Some ex)

    let argumentOptions =
        match parseArgsResult with
        | Ok parsedArgs ->
            parsedArgs.MapResult(
                (fun (options: IArguments) ->
                    {
                        CommandKind = Some Format
                        InputKind = Some (if String.IsNullOrEmpty(options.ProjectFile) then Project else Files)
                        RecurseFlag = options.Recurse
                        BackupFlag = options.Backup
                        QSharpVersion = Some(Version(options.QdkVersion))
                        Input = List.empty<string>
                    }),
                (fun (options: UpdateArguments) ->
                    {
                        CommandKind = Some Update
                        InputKind = Some (if String.IsNullOrEmpty(options.ProjectFile) then Project else Files)
                        RecurseFlag = options.Recurse
                        BackupFlag = options.Backup
                        QSharpVersion = Some(Version(options.QdkVersion))
                        Input = List.empty<string>
                    }),
                (fun (options: UpdateAndFormatArguments) ->
                    {
                        CommandKind = Some UpdateAndFormat
                        InputKind = Some (if String.IsNullOrEmpty(options.ProjectFile) then Project else Files)
                        RecurseFlag = options.Recurse
                        BackupFlag = options.Backup
                        QSharpVersion = Some(Version(options.QdkVersion))
                        Input = List.empty<string>
                    }),
                (fun (_: IEnumerable<Error>) ->
                    {
                        CommandKind = None
                        InputKind = None
                        RecurseFlag = false
                        BackupFlag = false
                        QSharpVersion = None
                        Input = List.empty<string>
                    })
            )
        | Result.Error ex ->
            {
                CommandKind = None
                InputKind = None
                RecurseFlag = false
                BackupFlag = false
                QSharpVersion = None
                Input = List.empty<string>
            }

    let executionCompletedEvent =
        {
            StartTime = startTime
            Command = argumentOptions.CommandKind
            RecurseFlag = argumentOptions.RecurseFlag
            BackupFlag = argumentOptions.BackupFlag
            InputKind = argumentOptions.InputKind
            QSharpVersion = argumentOptions.QSharpVersion |> string
            UnhandledException = unhandledException
            SyntaxErrors = syntaxErrors
            ExecutionTime = executionTime
            FilesProcessed = filesProcessed
            ExitCode = exitCode
        }

    TelemetryManager.LogObject(executionCompletedEvent)
