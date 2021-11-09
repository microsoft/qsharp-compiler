// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Arguments

open System
open System.IO
open System.Text.RegularExpressions
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
                 "input",
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
                 "input",
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

[<Verb("update-and-format", HelpText = "Updates depreciated syntax in the input files and formats them.", Hidden = true)>]
type UpdateAndFormatArguments =
    {
        [<Option('b', "backup", HelpText = "Option to create backup files of input files.")>]
        Backup: bool
        [<Option('r', "recurse", SetName = "INPUT_FILES", HelpText = "Option to process input folders recursively.")>]
        Recurse: bool
        [<Option("qsharp-version", SetName = "INPUT_FILES", HelpText = "Option to provide a Q# version to the tool.")>]
        QdkVersion: string
        [<Option('i',
                 "input",
                 SetName = "INPUT_FILES",
                 Required = true,
                 Min = 1,
                 HelpText = "Files or folders to update and format.")>]
        InputFiles: string seq
        [<Option('p',
                 "project",
                 SetName = "PROJ_FILE",
                 Required = true,
                 HelpText = "The project file for the project to update and format.")>]
        ProjectFile: string
    }

    [<Usage(ApplicationAlias = "qsfmt")>]
    static member examples =
        seq {
            yield
                Example(
                    "Updates and formats the source code in input files",
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
                    "Updates and formats the source code in project",
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
    | UpdateAndFormat

type Arguments =
    {
        CommandKind: CommandKind
        RecurseFlag: bool
        BackupFlag: bool
        QSharp_Version: Version option
        Input: string list
    }

module Arguments =
    let private checkArguments (arguments: UpdateArguments) =
        let mutable errors = []

        if not (isNull arguments.ProjectFile) && String.IsNullOrWhiteSpace arguments.ProjectFile then
            errors <- "Error: Bad project file given." :: errors

        if arguments.InputFiles |> Seq.exists String.IsNullOrWhiteSpace then
            errors <- "Error: Bad input(s) given." :: errors

        errors

    let fromUpdateArguments (arguments: UpdateArguments) =
        let errors = checkArguments arguments

        if List.isEmpty errors then

            let input, version =
                if isNull arguments.ProjectFile then
                    arguments.InputFiles |> Seq.toList, arguments.QdkVersion |> Option.ofObj
                else
                    getSourceFiles arguments.ProjectFile |> (fun (i, v) -> (i, v |> Some))

            let isVersionOkay, qsharp_version =
                match version with
                | Some v ->
                    let m = Regex.Match(v, "^[0-9\\.]+")

                    if m.Success then
                        match Version.TryParse m.Value with
                        | true, ver -> true, Some ver
                        | false, _ -> false, None
                    else
                        false, None
                | None -> true, None

            if isVersionOkay then
                match qsharp_version with
                | Some v when v < Version("0.16.2104.138035") ->
                    eprintfn
                        "Error: Qdk Version is out of date. Only Qdk version 0.16.2104.138035 or later is supported."

                    6 |> Result.Error
                | _ ->
                    {
                        CommandKind = Update
                        RecurseFlag = arguments.Recurse
                        BackupFlag = arguments.Backup
                        QSharp_Version = qsharp_version
                        Input = input
                    }
                    |> Result.Ok
            else
                let s =
                    match version with
                    | Some v -> sprintf ": %s" v
                    | None -> "."

                eprintfn "Error: Bad Qdk version number%s" s
                2 |> Result.Error
        else
            for e in errors do
                eprintfn "%s" e

            2 |> Result.Error
