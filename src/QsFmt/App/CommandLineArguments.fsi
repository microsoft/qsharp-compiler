// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Arguments

open System
open CommandLine.Text
open Microsoft.Quantum.QsFmt.Formatter

type ExitCode =
    | Success = 0
    | SyntaxErrors = 1
    | BadArguments = 2
    | IOError = 3
    | UnauthorizedAccess = 4
    | FileAlreadyProcessed = 5
    | QdkOutOfDate = 6
    | UnhandledException = 7

type internal RunResult =
    { FilesProcessed: int
      ExitCode: ExitCode
      SyntaxErrors: Errors.SyntaxError list }
    static member Default : RunResult

/// The kind of command used
type CommandKind =

    /// Represents usage of the `update` command
    | Update

    /// Represents usage of the `format` command
    | Format

    /// Represents usage of the `update-and-format` command
    | UpdateAndFormat

/// The kind of the input
type InputKind =

    /// Represents usage of the `input` command option
    | Files

    /// Represents usage of the `project` command option
    | Project

/// Common argument interface for all commands
type internal IArguments =
    abstract member Backup : bool
    abstract member Recurse : bool
    abstract member QdkVersion : string
    abstract member InputFiles : seq<string>
    abstract member ProjectFile : string
    abstract member CommandKind : CommandKind

/// Object for capturing the arguments used with the `format` command.
type FormatArguments =
    {
      /// Flag to indicate if the `--backup` option was specified.
      Backup: bool

      /// Flag to indicate if the `--recurse` option was specified.
      Recurse: bool

      /// Optional argument for the `--qsharp-version` option.
      QdkVersion: string

      /// The input files specified by the `--input` argument.
      InputFiles: seq<string>

      /// The project file specified by the `--project` argument.
      ProjectFile: string }
    /// Provides example usage.
    static member examples : seq<Example>
    interface IArguments

/// Object for capturing the arguments used with the `update` command.
type UpdateArguments =
    {
      /// Flag to indicate if the `--backup` option was specified.
      Backup: bool

      /// Flag to indicate if the `--recurse` option was specified.
      Recurse: bool

      /// Optional argument for the `--qsharp-version` option.
      QdkVersion: string

      /// The input files specified by the `--input` argument.
      InputFiles: seq<string>

      /// The project file specified by the `--project` argument.
      ProjectFile: string }
    /// Provides example usage.
    static member examples : seq<Example>
    interface IArguments

/// Object for capturing the arguments used with the `update-and-format` command.
type UpdateAndFormatArguments =
    {
      /// Flag to indicate if the `--backup` option was specified.
      Backup: bool

      /// Flag to indicate if the `--recurse` option was specified.
      Recurse: bool

      /// Optional argument for the `--qsharp-version` option.
      QdkVersion: string

      /// The input files specified by the `--input` argument.
      InputFiles: seq<string>

      /// The project file specified by the `--project` argument.
      ProjectFile: string }
    /// Provides example usage.
    static member examples : seq<Example>
    interface IArguments

/// Represents the fully parsed arguments to the tool.
type internal CommandWithOptions =
    {
      /// Indicates the command specified.
      CommandKind: CommandKind

      /// The input kind
      InputKind: InputKind

      /// Flag to indicate if the `--recurse` option was specified.
      RecurseFlag: bool

      /// Flag to indicate if the `--backup` option was specified.
      BackupFlag: bool

      /// Optional Q# version specified.
      QSharpVersion: Version option

      /// The paths to the files to process.
      Input: string list }
    static member Default : CommandWithOptions

module internal CommandWithOptions =

    /// Creates an CommandWithOptions object from an IArguments object
    val fromIArguments : IArguments -> Result<CommandWithOptions, ExitCode>
