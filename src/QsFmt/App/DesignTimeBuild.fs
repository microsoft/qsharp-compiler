// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.App.DesignTimeBuild

open Microsoft.Build.Utilities
open Microsoft.Build.Evaluation
open System.Collections.Generic
open Microsoft.Build.Execution
open System.IO
open System

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
    |> Seq.where
        (fun item ->
            item.ItemType.Equals(itemType, StringComparison.OrdinalIgnoreCase) && not (isNull item.EvaluatedInclude))
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
        project <- new Project(projectFile, properties, ToolLocationHelper.CurrentToolsVersion)
        let instance = project.CreateProjectInstance()

        // do we want to check if the project is a Q# project and if not fail?
        if not <| instance.Targets.ContainsKey "QSharpCompile" then
            failwith (sprintf "The given project file, %s is not a Q# project file." projectFile)

        // get all the source files in the project
        let sourceFiles = getItemsByType instance "QSharpCompile" |> Seq.toList

        let version =
            instance.Properties
            |> Seq.tryFind (fun x -> x.Name = "QuantumSdkVersion")
            |> Option.map (fun x -> x.EvaluatedValue)

        (sourceFiles, version)

    finally
        if not (isNull project || isNull ProjectCollection.GlobalProjectCollection) then
            ProjectCollection.GlobalProjectCollection.UnloadProject project
