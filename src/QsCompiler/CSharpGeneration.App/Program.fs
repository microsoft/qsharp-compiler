// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CsharpGeneration.Program

open System
open System.Collections.Generic
open CommandLine
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.Diagnostics

type ExitStatus = 
    | SUCCESS = 0
    | INVALID_OPTIONS = 1
    | COMPILATION_ERRORS = 2

type Options = {

    [<Option('v', "verbose", Required = false, Default = false, 
      HelpText = "Specifies whether to compile in verbose mode.")>]
    Verbose : bool;
    
    [<Option('i', "input", Required = true, 
      HelpText = "Q# code or name of the Q# file to compile.")>]
    Input : IEnumerable<string>
            
    [<Option('r', "references", Required = false, Default = ([||] : string[]),
      HelpText = "Referenced binaries to include in the compilation.")>]
    References : IEnumerable<string>
 
    [<Option('o', "output", Required = true, 
      HelpText = "Destination folder where the output of the compilation will be generated.")>]
    OutputFolder : string

    [<Option("doc", Required = false,
      HelpText = "Destination folder where documentation will be generated.")>]
    DocFolder : string

    [<Option('q', "qst", Required = false,
      HelpText = "QST output file name. If provided, it will generate a .qst file with the binary represenation of the syntax tree.")>]
    QSTFileName : string
}

type Logger() = 
    inherit LogTracker()
    override this.Print msg =
        Formatting.MsBuildFormat msg |> Console.WriteLine
    override this.OnException ex = 
        Console.Error.WriteLine ex


let generateFiles (options : Options) = 
    let logger = new Logger()
    let outputFolder = if String.IsNullOrWhiteSpace options.QSTFileName then null else options.OutputFolder
    let codeGenDll = typeof<Emitter>.Assembly.Location
    let assemblyConstants = new Dictionary<string, string>()
    let loadOptions = 
        new CompilationLoader.Configuration(
            GenerateFunctorSupport = true,
            DocumentationOutputFolder = options.DocFolder,
            BuildOutputFolder = outputFolder,
            ProjectName = options.QSTFileName,
            RewriteSteps = [struct(codeGenDll, null)],
            AssemblyConstants = assemblyConstants
        ) 
    let loaded = new CompilationLoader(options.Input, options.References, Nullable(loadOptions), logger)
    if loaded.Success then ExitStatus.SUCCESS else ExitStatus.COMPILATION_ERRORS


let [<EntryPoint>] main args = 
    match Parser.Default.ParseArguments<Options> args with 
    | :? Parsed<Options> as options -> (int)(generateFiles options.Value)
    | _ -> (int)ExitStatus.INVALID_OPTIONS



