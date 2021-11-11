// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Telemetry

open System
open Microsoft.Quantum.QsFmt.App.Arguments

module Telemetry =
    val internal initializeTelemetry : string [] -> IDisposable

    val internal logExecutionCompleted :
        Result<Result<CommandWithOptions, ExitCode>, Exception> ->
        Result<RunResult, Exception> ->
        DateTime ->
        TimeSpan ->
        unit
