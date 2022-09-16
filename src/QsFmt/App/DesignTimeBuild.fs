// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.DesignTimeBuild

open System
open System.Collections.Generic
open System.IO
open System.Runtime.Loader
open Microsoft.Build.Evaluation
open Microsoft.Build.Execution
open Microsoft.Build.Locator
open Microsoft.Build.Utilities

let globalProperties =
    let dict = new Dictionary<_, _>()
    dict.["BuildProjectReferences"] <- "false"
    dict.["EnableFrameworkPathOverride"] <- "false" // otherwise msbuild fails on .net 461 projects
    dict

/// <summary>
/// Extracts the EvaluatedInclude for all items of the given type in the given project instance,
/// and returns the combined path of the project directory and the evaluated include.
/// </summary>
let getItemsByType (project: ProjectInstance) (itemType: string) =
    project.Items
    |> Seq.where (fun item ->
        item.ItemType.Equals(itemType, StringComparison.OrdinalIgnoreCase)
        && not (isNull item.EvaluatedInclude))
    |> Seq.map (fun item -> Path.Combine(project.Directory, item.EvaluatedInclude))

let getSourceFiles (projectFile: string) =

    if not <| File.Exists(projectFile) then
        failwith (sprintf "The given project file, %s does not exist." projectFile)

    let mutable project = null

    try
        // Unloading the project unloads the project but *doesn't* clear the cache to be resilient to inconsistent states.
        // Hence we actually need to unload all projects, which does make sure the cache is cleared and changes on disk are reflected.
        // See e.g. https://github.com/Microsoft/msbuild/issues/795
        if not (isNull ProjectCollection.GlobalProjectCollection) then
            ProjectCollection.GlobalProjectCollection.UnloadAllProjects() // needed due to the caching behavior of MS build

        let properties = globalProperties // TODO: we may need to revise that
        project <- Project(projectFile, properties, ToolLocationHelper.CurrentToolsVersion)
        let instance = project.CreateProjectInstance()

        // get all the source files in the project
        let sourceFiles = getItemsByType instance "QSharpCompile" |> Seq.toList

        let version =
            match instance.Properties |> Seq.tryFind (fun x -> x.Name = "QuantumSdkVersion") with
            | Some v -> v.EvaluatedValue
            | _ ->
                failwith (
                    sprintf
                        "The given project file, %s is not a Q# project file. Please ensure your project file uses the Microsoft.Quantum.Sdk."
                        projectFile
                )

        (sourceFiles, version)

    finally
        if not (isNull project || isNull ProjectCollection.GlobalProjectCollection) then
            ProjectCollection.GlobalProjectCollection.UnloadProject project

let assemblyLoadContextSetup () =
    // We need to set the current directory to the same directory of
    // the LanguageServer executable so that it will pick the global.json file
    // and force the MSBuildLocator to use .NET Core SDK 6.0
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
    | _ -> ()
