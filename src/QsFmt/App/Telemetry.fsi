// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Telemetry

open System
open Microsoft.Quantum.QsFmt.App.Arguments
open Microsoft.Quantum.QsFmt.Formatter
open Microsoft.Quantum.Telemetry

/// The ExecutionCompleted telemetry event record.
/// ATTENTION: The Telemetry Library only logs public properties.
/// We need to have this record as public such that
/// all of its properties will be made public too.
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

/// Initializes the telemetry manager. This is required before logging telemetry.
val internal initializeTelemetry: string [] -> IDisposable

/// Logs the ExecutionCompleted telemetry event.
val internal logExecutionCompleted:
    Result<Result<CommandWithOptions, ExitCode>, Exception> ->
    Result<RunResult, Exception> ->
    DateTime ->
    TimeSpan ->
        unit
