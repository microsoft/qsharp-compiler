// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Telemetry

open System
open System.Collections.Generic
open Microsoft.Quantum.QsFmt.App.Arguments
open Microsoft.Quantum.QsFmt.Formatter
open Microsoft.Quantum.Telemetry
open CommandLine

type ExecutionCompleted =
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
            OutOfProcessUpload = false,
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
    (commandWithOptions: Result<Result<CommandWithOptions, ExitCode>, Exception>)
    (runResult: Result<RunResult, Exception>)
    (startTime: DateTime)
    (executionTime: TimeSpan)
    =

    let (syntaxErrors, filesProcessed, exitCode, unhandledException) =
        match runResult with
        | Ok runResult -> (Some runResult.SyntaxErrors, runResult.FilesProcessed, runResult.ExitCode, None)
        | Error ex -> (None, 0, ExitCode.UnhandledException, Some ex)

    let commandWithOptions =
        match commandWithOptions with
        | Ok commandWithOptions ->
            match commandWithOptions with
                | Ok commandWithOptions -> commandWithOptions
                | Error exitCode -> CommandWithOptions.Default
        | Result.Error ex -> CommandWithOptions.Default

    let executionCompletedEvent =
        {
            StartTime = startTime
            Command = commandWithOptions.CommandKind
            RecurseFlag = commandWithOptions.RecurseFlag
            BackupFlag = commandWithOptions.BackupFlag
            InputKind = commandWithOptions.InputKind
            QSharpVersion = commandWithOptions.QSharpVersion |> string
            UnhandledException = unhandledException
            SyntaxErrors = syntaxErrors
            ExecutionTime = executionTime
            FilesProcessed = filesProcessed
            ExitCode = exitCode
        }

    TelemetryManager.LogObject(executionCompletedEvent)
