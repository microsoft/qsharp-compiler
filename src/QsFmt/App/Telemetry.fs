// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Telemetry

open System
open Microsoft.Quantum.QsFmt.App.Arguments
open Microsoft.Quantum.QsFmt.Formatter
open Microsoft.Quantum.Telemetry

/// The ExecutionCompleted telemetry event record.
type ExecutionCompleted =
    {
        /// When the program execution was started.
        StartTime: DateTime

        /// Which command was specified on the command-line.
        Command: CommandKind option

        /// Which input kind was specified on the command-line.
        InputKind: InputKind option

        /// The value of the --recurse option specified on the command-line.
        RecurseFlag: bool option

        /// The value of the --backup option specified on the command-line.
        BackupFlag: bool option

        /// The value of the optional QDK version used.
        QSharpVersion: string option

        /// The unhandled exception, if one was thrown during the execution.
        UnhandledException: Exception option

        /// Any syntax errors encountered during the execution.
        [<SerializeJson>]
        SyntaxErrors: Errors.SyntaxError list option

        /// The total execution time of the program, not including sending the telemetry event.
        ExecutionTime: TimeSpan

        /// The number of Q# source files processed during execution.
        FilesProcessed: int

        /// The final exit code of the program.
        ExitCode: ExitCode
    }

#if TELEMETRY
let testMode = false
#else
let testMode = true
#endif

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
            DefaultTelemetryConsent = ConsentKind.OptedIn,
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
            QSharpVersion = qsharpVersion |> Option.map string
            UnhandledException = unhandledException
            SyntaxErrors = syntaxErrors
            ExecutionTime = executionTime
            FilesProcessed = filesProcessed
            ExitCode = exitCode
        }

    TelemetryManager.LogObject(executionCompletedEvent)
