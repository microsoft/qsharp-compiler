// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.Json
open System.Threading
open Microsoft.Quantum.QsCompiler.CommandLineCompiler
open Microsoft.Quantum.QsCompiler.Diagnostics
open Xunit
open Xunit.Abstractions

type CompilationTrackerTests (output:ITestOutputHelper) =

    let getResultsFolder (folderName: string) =
        Path.Combine("Results", folderName)

    let getResultsFile (folderName: string) =
        let resultsFolder = getResultsFolder folderName
        Path.Combine(resultsFolder, CompilationTracker.CompilationPerfDataFileName)

    let getResultsDictionary (folderName: string) =
        let resultsFile = getResultsFile folderName
        let json = File.ReadAllText resultsFile
        JsonSerializer.Deserialize<IDictionary<string, int>>(json)

    [<Theory>]
    [<InlineData(100)>]
    member this.``Measure Task`` (durationInMs: int) =
        CompilationTracker.ClearData ()
        let taskName = "TestTask"

        // Measure time spent in a task.
        CompilationTracker.OnCompilationTaskEvent (CompilationTaskEventType.Start, null, taskName)
        Thread.Sleep durationInMs
        CompilationTracker.OnCompilationTaskEvent (CompilationTaskEventType.End, null, taskName)

        // Publish measurement results.
        let resultsFolderName = MethodBase.GetCurrentMethod().Name
        resultsFolderName |> getResultsFolder |> CompilationTracker.PublishResults
        let resultsDictionary = resultsFolderName |> getResultsDictionary
        resultsDictionary |> Assert.NotEmpty
