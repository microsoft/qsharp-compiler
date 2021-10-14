// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Arguments

open System
open System.IO
open CommandLine
open CommandLine.Text
open Microsoft.Quantum.QsFmt.App.DesignTimeBuild

[<Verb("format", HelpText = "Format the source code in input files.", Hidden = true)>]
type FormatArguments =
    {

        [<Option('b', "backup", HelpText = "Option to create backup files of input files.")>]
        Backup: bool
        [<Option('r', "recurse", SetName = "INPUT_FILES", HelpText = "Option to process input folders recursively.")>]
        Recurse: bool
        [<Option("qsharp-version", SetName = "INPUT_FILES", HelpText = "Option to provide a Q# version to the tool.")>]
        QdkVersion: string
        [<Option('i',
                 "inputs",
                 SetName = "INPUT_FILES",
                 Required = true,
                 Min = 1,
                 HelpText = "Files or folders to format.")>]
        InputFiles: string seq
        [<Option('p',
                 "project",
                 SetName = "PROJ_FILE",
                 Required = true,
                 HelpText = "The project file for the project to format.")>]
        ProjectFile: string
    }

    [<Usage(ApplicationAlias = "qsfmt")>]
    static member examples =
        seq {
            yield
                Example(
                    "Formats the source code in input files",
                    {
                        Backup = false
                        Recurse = false
                        InputFiles =
                            seq {
                                Path.Combine("Path", "To", "My", "File1.qs")
                                Path.Combine("Path", "To", "My", "File2.qs")
                            }
                        ProjectFile = null
                        QdkVersion = null
                    }
                )

            yield
                Example(
                    "Formats the source code in project",
                    {
                        Backup = false
                        Recurse = false
                        InputFiles = Seq.empty
                        ProjectFile = Path.Combine("Path", "To", "My", "Project.csproj")
                        QdkVersion = null
                    }
                )
        }

[<Verb("update", HelpText = "Updates depreciated syntax in the input files.")>]
type UpdateArguments =
    {

        [<Option('b', "backup", HelpText = "Option to create backup files of input files.")>]
        Backup: bool
        [<Option('r', "recurse", SetName = "INPUT_FILES", HelpText = "Option to process input folders recursively.")>]
        Recurse: bool
        [<Option("qsharp-version", SetName = "INPUT_FILES", HelpText = "Option to provide a Q# version to the tool.")>]
        QdkVersion: string
        [<Option('i',
                 "inputs",
                 SetName = "INPUT_FILES",
                 Required = true,
                 Min = 1,
                 HelpText = "Files or folders to update.")>]
        InputFiles: string seq
        [<Option('p',
                 "project",
                 SetName = "PROJ_FILE",
                 Required = true,
                 HelpText = "The project file for the project to update.")>]
        ProjectFile: string
    }

    [<Usage(ApplicationAlias = "qsfmt")>]
    static member examples =
        seq {
            yield
                Example(
                    "Updates the source code in input files",
                    {
                        Backup = false
                        Recurse = false
                        InputFiles =
                            seq {
                                Path.Combine("Path", "To", "My", "File1.qs")
                                Path.Combine("Path", "To", "My", "File2.qs")
                            }
                        ProjectFile = null
                        QdkVersion = null
                    }
                )

            yield
                Example(
                    "Updates the source code in project",
                    {
                        Backup = false
                        Recurse = false
                        InputFiles = Seq.empty
                        ProjectFile = Path.Combine("Path", "To", "My", "Project.csproj")
                        QdkVersion = null
                    }
                )
        }

type CommandKind =
    | Update
    | Format

type Arguments =
    {
        CommandKind: CommandKind
        RecurseFlag: bool
        BackupFlag: bool
        QSharp_Version: Version option
        Inputs: string list
    }

module Arguments =
    let private checkArguments (arguments: UpdateArguments) =
        let mutable errors = []

        if not (isNull arguments.ProjectFile) && String.IsNullOrWhiteSpace arguments.ProjectFile then
            errors <- "Error: Bad project file given." :: errors

        if arguments.InputFiles |> Seq.exists String.IsNullOrWhiteSpace then
            errors <- "Error: Bad input(s) given." :: errors

        if not (isNull arguments.QdkVersion) then
            match Version.TryParse arguments.QdkVersion with
            | false, _ -> errors <- "Error: Bad version number given." :: errors
            | _ -> ()

        errors

    let fromUpdateArguments (arguments: UpdateArguments) =
        let errors = checkArguments arguments

        if List.isEmpty errors then

            let inputs, version =
                if isNull arguments.ProjectFile then
                    arguments.InputFiles |> Seq.toList, arguments.QdkVersion |> Option.ofObj
                else
                    getSourceFiles arguments.ProjectFile |> (fun (i, v) -> (i, v |> Some))

            let qsharp_version = version |> Option.map Version.Parse

            match qsharp_version with
            | Some v when v < Version("0.16.2104.138035") ->
                eprintfn "Error: Qdk Version is out of date. Only Qdk version 0.16.2104.138035 or later is supported."
                6 |> Result.Error
            | _ ->
                {
                    CommandKind = Update
                    RecurseFlag = arguments.Recurse
                    BackupFlag = arguments.Backup
                    QSharp_Version = qsharp_version
                    Inputs = inputs
                }
                |> Result.Ok
        else
            for e in errors do
                eprintfn "%s" e

            2 |> Result.Error
