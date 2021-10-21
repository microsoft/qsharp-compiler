// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Arguments

    open System
    open CommandLine
    open CommandLine.Text

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
        with
            /// Provides example usage.
            static member examples : seq<Example>

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
        with
            /// Provides example usage.
            static member examples : seq<Example>

    /// The kind of command used
    type internal CommandKind =

        /// Represents usage of the `update` command
        | Update

        /// Represents usage of the `format` command
        | Format


    /// Represents the fully parsed arguments to the tool.
    type internal Arguments =
        {
            /// Indicates the command specified.
            CommandKind: CommandKind

            /// Flag to indicate if the `--recurse` option was specified.
            RecurseFlag: bool

            /// Flag to indicate if the `--backup` option was specified.
            BackupFlag: bool

            /// Optional Q# version specified.
            QSharp_Version: Version option

            /// The paths to the files to process.
            Input: string list
        }

    module internal Arguments =

        /// Creates an Arguments object from an UpdateArguments object
        val fromUpdateArguments: UpdateArguments -> Result<Arguments,int>
