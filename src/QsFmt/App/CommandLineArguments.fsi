// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Arguments

open System
open CommandLine.Text
open Microsoft.Quantum.QsFmt.Formatter

/// An Enum for the various exit codes for the program.
type ExitCode =
    | Success = 0

    /// Syntax errors were encountered during the program.
    | SyntaxErrors = 1

    /// Invalid command-line arguments were given to the program.
    | BadArguments = 2

    /// An Input/Output error was encountered during the program.
    | IOError = 3

    /// The program attempted to access a file or resource it didn't have authority to access.
    | UnauthorizedAccess = 4

    /// The same input file was given multiple times.
    | FileAlreadyProcessed = 5

    /// A QDK version was used that is too far out of date for the program to process.
    | QdkOutOfDate = 6

    /// An Unhandled Exception was encountered during the program.
    | UnhandledException = 7

    /// We returned the version of the program
    | Version = 8

    /// We printed the program help
    | Help = 9

/// The results from running a command.
type internal RunResult =
    {
        /// The number of input source files processed.
        FilesProcessed: int

        /// The exit code.
        ExitCode: ExitCode

        /// The list of syntax errors encountered during the running of the command.
        SyntaxErrors: Errors.SyntaxError list
    }

    /// Returns a RunResult record with default field values.
    static member Default: RunResult

/// The kind of command used.
type CommandKind =

    /// Represents usage of the `update` command
    | Update

    /// Represents usage of the `format` command
    | Format

    /// Represents usage of the `update-and-format` command
    | UpdateAndFormat

/// The kind of the input.
type InputKind =

    /// Represents usage of the `input` command option
    | Files

    /// Represents usage of the `project` command option
    | Project

/// Common argument interface for all commands.
type internal IArguments =

    /// Flag to indicate if the `--backup` option was specified.
    abstract member Backup: bool

    /// Flag to indicate if the `--recurse` option was specified.
    abstract member Recurse: bool

    /// Optional argument for the `--qsharp-version` option.
    abstract member QdkVersion: string

    /// The input files specified by the `--input` argument.
    abstract member InputFiles: seq<string>

    /// The project file specified by the `--project` argument.
    abstract member ProjectFile: string

    /// The kind of command used.
    abstract member CommandKind: CommandKind

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
        ProjectFile: string
    }
    interface IArguments

    /// Provides example usage.
    static member examples: seq<Example>

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
        ProjectFile: string
    }
    interface IArguments

    /// Provides example usage.
    static member examples: seq<Example>

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
        ProjectFile: string
    }
    interface IArguments

    /// Provides example usage.
    static member examples: seq<Example>

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
        Input: string list
    }

    /// Returns a CommandWithOptions record with default field values.
    static member Default: CommandWithOptions

module internal CommandWithOptions =

    /// Creates an CommandWithOptions object from an IArguments object
    val fromIArguments: IArguments -> Result<CommandWithOptions, ExitCode>
