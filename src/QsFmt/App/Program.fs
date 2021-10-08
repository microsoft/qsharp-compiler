// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.Program

open System
open CommandLine
open CommandLine.Text
open System.IO
open Microsoft.Build.Locator
open System.Runtime.Loader

//open Microsoft.Quantum.QsFmt.Formatter


[<Verb("format", HelpText = "Format the source code in input files.", Hidden = true)>]
type FormatArguments = {

    [<Option('p', "project", SetName = "PROJ_FILE", Required = true, HelpText = "The project file for the project to process.")>]
    projectFile : string;

    [<Option('i', "inputs", SetName = "INPUT_FILES", Required = true, HelpText = "Files or folders to format or \"-\" to read from standard input.")>]
    inputFiles : seq<string>;

    [<Option("backup", Required = false, Default = false, HelpText = "Create backup files of input files.")>]
    createBackup : bool;

    [<Option('r', "recur", SetName = "INPUT_FILES", Required = false, Default = false, HelpText = "Process the input folder recursively.")>]
    recur : bool;
}

[<Verb("update", HelpText = "Update deprecated syntax in the input files.")>]
type UpdateArguments = {

    [<Option('p', "project", SetName = "PROJ_FILE", Required = true, HelpText = "The project file for the project to process.")>]
    projectFile : string;

    [<Option('i', "inputs", SetName = "INPUT_FILES", Required = true, HelpText = "Files or folders to format or \"-\" to read from standard input.")>]
    inputFiles : seq<string>;

    [<Option("backup", Required = false, Default = false, HelpText = "Create backup files of input files.")>]
    createBackup : bool;

    [<Option('r', "recur", SetName = "INPUT_FILES", Required = false, Default = false, HelpText = "Process the input folder recursively.")>]
    recur : bool;

    [<Option("qsharp-version", SetName = "INPUT_FILES", Required = false, Default ="", HelpText = "Provides the version to the tool.")>]
    qdkVersion : string;
}
    with

    [<Usage(ApplicationAlias = "qsfmt")>]
    static member examples
        with get() = seq {
            yield Example(
                "Formats the source code in input files",
                {createBackup = false; recur = false; inputFiles = seq {"Path\To\My\File.qs"}; projectFile = ""; qdkVersion = "" })

            yield Example(
                "Formats the source code in project",
                {createBackup = false; recur = false; inputFiles = Seq.empty; projectFile = "Path\To\My\Project.csproj"; qdkVersion = "" })
        }


let makeFullPath input =
    if input = "-" then input else Path.GetFullPath input


let run command (recur, createBackup) inputs =

    let mutable paths = Set.empty

    let rec doOne input =
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

                        if recur then
                            topLevelFiles @ (Directory.EnumerateDirectories input |> List.ofSeq)
                        else
                            topLevelFiles

                    newInputs |> doMany
                else
                    let source =
                        if input = "-" then
                            stdin.ReadToEnd()
                        else
                            if createBackup then File.Copy(input, (input + "~"), true)
                            File.ReadAllText input

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

    and doMany (inputs : string seq) =
        inputs |> Seq.fold (fun (rtrnCode: int) filePath -> max rtrnCode (filePath |> doOne)) 0

    doMany inputs


[<EntryPoint>]
let main argv =

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


    CommandLine.Parser.Default.ParseArguments<UpdateArguments, FormatArguments>(argv).MapResult(
        (fun  (parsed : Parsed<UpdateArguments>) ->
            let arguments = parsed.Value
            printfn "projectFile: %s" arguments.projectFile
            printfn "inputFiles: %s" (String.Join (", ", arguments.inputFiles))
            printfn "createBackup: %b" arguments.createBackup
            printfn "recur: %b" arguments.recur
            printfn "qdkVersion: %s" arguments.qdkVersion

            let inputs, version =
                if not <| String.IsNullOrWhiteSpace arguments.projectFile then
                    DesignTimeBuild.getSourceFiles arguments.projectFile
                else
                    arguments.inputFiles |> Seq.toList, Some arguments.qdkVersion

            let qsharp_version =
                version |> Option.map (Version.TryParse >> function
                | true, v -> Some v
                | false, _ -> None)

            if qsharp_version.IsSome then

                let command = fun input -> Formatter.update input arguments.QSharp_Version
                run command (arguments.recur, arguments.createBackup) parsed.Value.inputFiles
            else
                eprintf "Error: Bad version number."
                2
        ),

        (fun (parsed : Parsed<FormatArguments>) ->
            let arguments = parsed.Value
            let command = Formatter.format arguments.QSharp_Version
            run command (arguments.recur, arguments.createBackup) parsed.Value.inputFiles
            ),
        (fun _ ->
            // todo
            1
        )
    )

