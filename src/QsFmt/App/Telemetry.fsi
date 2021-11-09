// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Telemetry

open System
open Microsoft.Quantum.QsFmt.App.Arguments
open Microsoft.Quantum.QsFmt.Formatter
open Microsoft.Quantum.Telemetry
open CommandLine

type internal ExecutionCompleted =
    { StartTime: DateTime
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
      ExitCode: ExitCode }

val internal initializeTelemetry : string [] -> IDisposable

val internal logExecutionCompleted : Result<ParserResult<obj>, Exception> -> Result<RunResult, Exception> -> DateTime -> TimeSpan -> unit
