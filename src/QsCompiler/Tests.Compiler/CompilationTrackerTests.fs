// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.Json
open System.Threading
open Microsoft.FSharp.Core
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
        folderName |> getResultsFolder |> CompilationTracker.PublishResults
        getResultsFile folderName |> File.ReadAllText |> JsonSerializer.Deserialize<IDictionary<string, int>>

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(10)>]
    [<InlineData(25)>]
    [<InlineData(100)>]
    [<InlineData(350)>]
    [<InlineData(635)>]
    [<InlineData(1000)>]
    [<InlineData(3333)>]
    [<InlineData(5050)>]
    [<InlineData(10000)>]
    member this.``Measure Task`` (durationInMs: int) =
        CompilationTracker.ClearData()
        let taskName = "TestTask"

        // Measure time spent in a task.
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, taskName)
        Thread.Sleep durationInMs
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, taskName)

        // Verify measured results are the expected ones.
        let resultsDictionary = MethodBase.GetCurrentMethod().Name |> getResultsDictionary
        let mutable measuredDurationInMs = 0
        Assert.True(resultsDictionary.TryGetValue(taskName, &measuredDurationInMs))
        Assert.InRange(measuredDurationInMs, durationInMs, Int32.MaxValue)

    [<Theory>]
    [<InlineData(0, 0, 0)>]
    [<InlineData(100, 0, 100)>]
    [<InlineData(0, 100, 0)>]
    [<InlineData(0, 100, 100)>]
    [<InlineData(100, 100, 0)>]
    [<InlineData(100, 100, 100)>]
    [<InlineData(750, 500, 650)>]
    [<InlineData(355, 10, 555)>]
    [<InlineData(835, 110, 55)>]
    [<InlineData(155, 2905, 225)>]
    member this.``Measure Task Intervals With Pause`` (firstIntervalDurationInMs: int) (pauseDurationInMs: int) (secondIntervalDurationInMs: int) =
        CompilationTracker.ClearData()
        let taskName = "TestTask"

        // Measure time spent in a task.
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, taskName)
        Thread.Sleep firstIntervalDurationInMs
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, taskName)
        Thread.Sleep pauseDurationInMs
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, taskName)
        Thread.Sleep secondIntervalDurationInMs
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, taskName)

        // Verify measured results are the expected ones.
        let resultsDictionary = MethodBase.GetCurrentMethod().Name |> getResultsDictionary
        let expectedDurationInMs = firstIntervalDurationInMs + secondIntervalDurationInMs
        let mutable measuredDurationInMs = 0
        Assert.True(resultsDictionary.TryGetValue(taskName, &measuredDurationInMs))
        Assert.InRange(measuredDurationInMs, expectedDurationInMs, Int32.MaxValue)

    [<Theory>]
    [<InlineData(1, 0)>]
    [<InlineData(10, 0)>]
    [<InlineData(10, 100)>]
    [<InlineData(25, 100)>]
    [<InlineData(3, 325)>]
    [<InlineData(5, 325)>]
    [<InlineData(2, 700)>]
    [<InlineData(5, 700)>]
    [<InlineData(1, 1050)>]
    [<InlineData(5, 1050)>]
    member this.``Measure Task Multiple Intervals`` (intervalCount: int) (intervalDurationInMs: int) =
        CompilationTracker.ClearData()
        let taskName = "TestTask"

        // Measure time spent in a task.
        for i in 1..intervalCount do
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, taskName)
            Thread.Sleep intervalDurationInMs
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, taskName)

        // Verify measured results are the expected ones.
        let resultsDictionary = MethodBase.GetCurrentMethod().Name |> getResultsDictionary
        let expectedDurationInMs = intervalDurationInMs * intervalCount
        let mutable measuredDurationInMs = 0
        Assert.True(resultsDictionary.TryGetValue(taskName, &measuredDurationInMs))
        Assert.InRange(measuredDurationInMs, expectedDurationInMs, Int32.MaxValue)
