﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Telemetry

open System
open Microsoft.Quantum.QsFmt.App.Arguments
open Microsoft.Quantum.QsFmt.Formatter
open Microsoft.Quantum.Telemetry

type ExecutionCompleted =
    {
        StartTime: DateTime
        Command: CommandKind option
        InputKind: InputKind option
        RecurseFlag: bool option
        BackupFlag: bool option
        QSharpVersion: string
        UnhandledException: Exception option
        [<SerializeJson>]
        SyntaxErrors: Errors.SyntaxError list option
        ExecutionTime: TimeSpan
        FilesProcessed: int
        ExitCode: ExitCode
    }

#if TELEMETRY
let testMode = false
#else
let testMode = true
#endif

module Telemetry =
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
                TestMode = testMode
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

        let (commandKind, inputKind, recurseFlag, backupFlag, qsharpVersion) =
            match commandWithOptions with
            | Ok commandWithOptions ->
                match commandWithOptions with
                | Ok commandWithOptions ->
                    (Some commandWithOptions.CommandKind,
                     Some commandWithOptions.InputKind,
                     Some commandWithOptions.RecurseFlag,
                     Some commandWithOptions.BackupFlag,
                     commandWithOptions.QSharpVersion)
                | Error _ -> (None, None, None, None, None)
            | Error _ -> (None, None, None, None, None)

        let executionCompletedEvent =
            {
                StartTime = startTime
                Command = commandKind
                RecurseFlag = recurseFlag
                BackupFlag = backupFlag
                InputKind = inputKind
                QSharpVersion = qsharpVersion |> string
                UnhandledException = unhandledException
                SyntaxErrors = syntaxErrors
                ExecutionTime = executionTime
                FilesProcessed = filesProcessed
                ExitCode = exitCode
            }

        TelemetryManager.LogObject(executionCompletedEvent)
