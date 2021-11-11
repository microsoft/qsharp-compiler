// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Telemetry

open System
open Microsoft.Quantum.QsFmt.App.Arguments

/// Initializes the telemetry manager. This is required before logging telemetry.
val internal initializeTelemetry : string [] -> IDisposable

/// Logs the ExecutionCompleted telemetry event.
val internal logExecutionCompleted :
    Result<Result<CommandWithOptions, ExitCode>, Exception> ->
    Result<RunResult, Exception> ->
    DateTime ->
    TimeSpan ->
    unit
