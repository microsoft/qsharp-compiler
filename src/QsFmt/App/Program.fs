// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Program

open System
open System.Collections.Generic
open System.IO
open System.Runtime.Loader
open CommandLine
open CommandLine.Text
open Microsoft.Build.Locator
open Microsoft.Quantum.QsFmt.App.DesignTimeBuild
open Microsoft.Quantum.QsFmt.Formatter

[<Verb("format", HelpText = "Format the source code in input files.", Hidden = true)>]
type FormatArguments = {

    [<Option('b', "backup", HelpText = "Option to create backup files of input files.")>] Backup : bool
    [<Option('r', "recurse", SetName = "INPUT_FILES", HelpText = "Option to process input folders recursively.")>] Recurse : bool
    [<Option("qsharp-version", SetName = "INPUT_FILES", HelpText = "Option to provide a Q# version to the tool.")>] QdkVersion : string
    [<Option('i', "inputs", SetName = "INPUT_FILES", Required = true, Min = 1, HelpText = "Files or folders to format or \"-\" to read from standard input.")>] InputFiles : string seq
    [<Option('p', "project", SetName = "PROJ_FILE", Required = true, HelpText = "The project file for the project to process.")>] ProjectFile : string
}
with

    [<Usage(ApplicationAlias = "qsfmt")>]
    static member examples
        with get() = seq {
            yield Example(
                "Formats the source code in input files",
                {Backup = false; Recurse = false; InputFiles = seq {"Path\To\My\File1.qs"; "Path\To\My\File2.qs"}; ProjectFile = null; QdkVersion = null; })

            yield Example(
                "Formats the source code in project",
                {Backup = false; Recurse = false; InputFiles = Seq.empty; ProjectFile = "Path\To\My\Project.csproj"; QdkVersion = null; })
        }

[<Verb("update", HelpText = "Updates depreciated syntax in the input files.")>]
type UpdateArguments = {

    [<Option('b', "backup", HelpText = "Option to create backup files of input files.")>] Backup : bool
    [<Option('r', "recurse", SetName = "INPUT_FILES", HelpText = "Option to process input folders recursively.")>] Recurse : bool
    [<Option("qsharp-version", SetName = "INPUT_FILES", HelpText = "Option to provide a Q# version to the tool.")>] QdkVersion : string
    [<Option('i', "inputs", SetName = "INPUT_FILES", Required = true, Min = 1, HelpText = "Files or folders to format or \"-\" to read from standard input.")>] InputFiles : string seq
    [<Option('p', "project", SetName = "PROJ_FILE", Required = true, HelpText = "The project file for the project to process.")>] ProjectFile : string
}
with

    [<Usage(ApplicationAlias = "qsfmt")>]
    static member examples
        with get() = seq {
            yield Example(
                "Updates the source code in input files",
                {Backup = false; Recurse = false; InputFiles = seq {"Path\To\My\File1.qs"; "Path\To\My\File2.qs"}; ProjectFile = null; QdkVersion = null; })

            yield Example(
                "Updates the source code in project",
                {Backup = false; Recurse = false; InputFiles = Seq.empty; ProjectFile = "Path\To\My\Project.csproj"; QdkVersion = null; })
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
    let private checkArguments (arguments : UpdateArguments) =
        let mutable errors = []
    
        if not (isNull arguments.ProjectFile) && String.IsNullOrWhiteSpace arguments.ProjectFile then
            errors <- "Error: Bad project file given." :: errors
    
        if arguments.InputFiles |> Seq.exists String.IsNullOrWhiteSpace then
            errors <- "Error: Bad input(s) given." :: errors
    
        if not (isNull arguments.QdkVersion) then
            match Version.TryParse arguments.QdkVersion with
            | false, _  -> errors <- "Error: Bad version number given." :: errors
            | _ -> ()
    
        errors

    let fromUpdateArguments (arguments : UpdateArguments) =
        let errors = checkArguments arguments
        if List.isEmpty errors then
    
            let inputs, version =
                if isNull arguments.ProjectFile then
                    arguments.InputFiles |> Seq.toList,
                    arguments.QdkVersion |> Option.ofObj
                else
                    getSourceFiles arguments.ProjectFile
    
            let qsharp_version =
                match version with
                | Some s -> Version.Parse s |> Some
                | None -> None
    
            {
                CommandKind = Format
                RecurseFlag = arguments.Recurse
                BackupFlag = arguments.Backup
                QSharp_Version = qsharp_version
                Inputs = inputs
            } |> Result.Ok
    
            //args.Inputs |> run args
        else
            for e in errors do
                eprintfn "%s" e
            2 |> Result.Error

let makeFullPath input =
    if input = "-" then input else Path.GetFullPath input

let run arguments inputs =

    let mutable paths = Set.empty

    let rec doOne arguments input =
        // Make sure inputs are not processed more than once.
        if input |> makeFullPath |> paths.Contains then
            // Change the "-" input to say "<Standard Input>" in the error
            let input = if input = "-" then "<Standard Input>" else input
            eprintfn "This input has already been processed: %s" input
            5
        else
            paths <- input |> makeFullPath |> paths.Add

            try
                if input <> "-" && (File.GetAttributes input).HasFlag(FileAttributes.Directory) then
                    let newInputs =
                        let topLevelFiles = Directory.EnumerateFiles(input, "*.qs") |> List.ofSeq

                        if arguments.RecurseFlag then
                            topLevelFiles @ (Directory.EnumerateDirectories input |> List.ofSeq)
                        else
                            topLevelFiles

                    newInputs |> doMany arguments
                else
                    let source =
                        if input = "-" then
                            stdin.ReadToEnd()
                        else
                            if arguments.BackupFlag then File.Copy(input, (input + "~"), true)
                            File.ReadAllText input

                    let command =
                        match arguments.CommandKind with
                        | Update -> Formatter.update input arguments.QSharp_Version
                        | Format -> Formatter.format arguments.QSharp_Version

                    match command source with
                    | Ok result ->
                        if input = "-" then printf "%s" result else File.WriteAllText(input, result)
                        0
                    | Error errors ->
                        // Change the "-" input to say "<Standard Input>" in the error
                        let input = if input = "-" then "<Standard Input>" else input
                        errors |> List.iter (eprintfn "%s, %O" input)
                        1
            with
            | :? IOException as ex ->
                eprintfn "%s" ex.Message
                3
            | :? UnauthorizedAccessException as ex ->
                eprintfn "%s" ex.Message
                4

    and doMany arguments inputs =
        inputs |> Seq.fold (fun (rtrnCode: int) filePath -> max rtrnCode (filePath |> doOne arguments)) 0

    doMany arguments inputs

let runUpdate (arguments : UpdateArguments) =
    match Arguments.fromUpdateArguments arguments with
    | Ok args -> args.Inputs |> run args
    | Error errorCode -> errorCode

let runFormat (arguments : FormatArguments) =
    let asUpdateArguments =
        {
            Backup = arguments.Backup
            Recurse = arguments.Recurse
            QdkVersion = arguments.QdkVersion
            InputFiles = arguments.InputFiles
            ProjectFile = arguments.ProjectFile
        }

    match Arguments.fromUpdateArguments asUpdateArguments with
    | Ok args -> args.Inputs |> run { args with CommandKind = Format }
    | Error errorCode -> errorCode

[<CompiledName "Main">]
[<EntryPoint>]
let main args =

    // We need to set the current directory to the same directory of
    // the LanguageServer executable so that it will pick the global.json file
    // and force the MSBuildLocator to use .NET Core SDK 3.1
    let cwd = Directory.GetCurrentDirectory()
    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory)
    // In the case where we actually instantiate a server, we need to "configure" the design time build.
    // This needs to be done before any MsBuild packages are loaded.
    try
        try
            let vsi = MSBuildLocator.RegisterDefaults()

            // We're using the installed version of the binaries to avoid a dependency between
            // the .NET Core SDK version and NuGet. This is a workaround due to the issue below:
            // https://github.com/microsoft/MSBuildLocator/issues/86
            AssemblyLoadContext.Default.add_Resolving (
                new Func<_, _, _>(fun assemblyLoadContext assemblyName ->
                    let path = Path.Combine(vsi.MSBuildPath, sprintf "%s.dll" assemblyName.Name)
                    if File.Exists(path) then assemblyLoadContext.LoadFromAssemblyPath path else null)
            )
        finally
            Directory.SetCurrentDirectory(cwd)
    with
    | _ ->
        // TODO: give some meaningful warning?
        ()


    let result = CommandLine.Parser.Default.ParseArguments<FormatArguments, UpdateArguments> args
    result.MapResult(
        (fun (options: FormatArguments) -> options |> runFormat),
        (fun (options: UpdateArguments) -> options |> runUpdate),
        (fun (_ : IEnumerable<Error>)-> 2)
    )
