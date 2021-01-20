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

type MeasureNestedTasksTestCases() as this =
    inherit TheoryData<int []>()

    do
        this.Add([||])
        this.Add([| 1000 |])
        this.Add([| 150; 225 |])
        this.Add([| 1000; 250; 825; 1555 |])
        this.Add([| 250; 300; 400; 200; 150; 750; 425; 103; 900 |])

type MeasureDoubleNestedTasksTestCases() as this =
    inherit TheoryData<int [], int []>()

    do
        this.Add([||], [||])
        this.Add([| 0 |], [| 500 |])
        this.Add([| 500 |], [| 0 |])
        this.Add([| 500 |], [| 500 |])
        this.Add([| 150; 325 |], [| 500; 250; 765 |])
        this.Add([| 500; 250; 765 |], [| 150; 325 |])
        this.Add([| 500 |], [| 1035; 555; 775; 225; 2500; 15; 95 |])
        this.Add([| 350; 155; 670; 2250; 25; 1110 |], [| 100 |])
        this.Add([| 0 |], [| 35; 625; 1275; 335; 1500; 155; 5 |])
        this.Add([| 5; 5; 10; 5; 5 |], [| 35; 10; 5; 30; 15; 5 |])

type CompilationTrackerTests(output: ITestOutputHelper) =

    let getResultsFolder (folderName: string) = Path.Combine("Results", folderName)

    let getResultsFile (folderName: string) =
        let resultsFolder = getResultsFolder folderName
        Path.Combine(resultsFolder, CompilationTracker.CompilationPerfDataFileName)

    let getResultsDictionary (folderName: string) =
        folderName |> getResultsFolder |> CompilationTracker.PublishResults

        getResultsFile folderName
        |> File.ReadAllText
        |> JsonSerializer.Deserialize<IDictionary<string, int>>

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
    member this.``Measure Task``(durationInMs: int) =
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
    member this.``Measure Task Intervals`` (intervalCount: int) (intervalDurationInMs: int) =
        CompilationTracker.ClearData()
        let taskName = "TestTask"

        // Measure time spent in a task.
        for i in 1 .. intervalCount do
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, taskName)
            Thread.Sleep intervalDurationInMs
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, taskName)

        // Verify measured results are the expected ones.
        let resultsDictionary = MethodBase.GetCurrentMethod().Name |> getResultsDictionary
        let expectedDurationInMs = intervalDurationInMs * intervalCount
        let mutable measuredDurationInMs = 0
        Assert.True(resultsDictionary.TryGetValue(taskName, &measuredDurationInMs))
        Assert.InRange(measuredDurationInMs, expectedDurationInMs, Int32.MaxValue)

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
    member this.``Measure Task Intervals With Pause`` (firstIntervalDurationInMs: int)
                                                      (pauseDurationInMs: int)
                                                      (secondIntervalDurationInMs: int)
                                                      =
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

    [<Theory; ClassData(typeof<MeasureNestedTasksTestCases>)>]
    member this.``Measure Nested Tasks``(nestedTasksDurationInMs: int []) =
        CompilationTracker.ClearData()
        let parentTaskName = "ParentTask"
        let nestedTaskPrefix = "NestedTask"

        // Measure time spent in a tasks.
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, parentTaskName)

        for i in 0 .. nestedTasksDurationInMs.Length - 1 do
            let durationInMs = nestedTasksDurationInMs.[i]
            let taskName = sprintf "%s-%i" nestedTaskPrefix i
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, parentTaskName, taskName)
            Thread.Sleep durationInMs
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, parentTaskName, taskName)

        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, parentTaskName)

        // Verify measured results are the expected ones.
        let resultsDictionary = MethodBase.GetCurrentMethod().Name |> getResultsDictionary
        let expectedParentTaskDurationInMs = Array.sum nestedTasksDurationInMs
        let mutable measuredParentTaskDurationInMs = 0
        Assert.True(resultsDictionary.TryGetValue(parentTaskName, &measuredParentTaskDurationInMs))
        Assert.InRange(measuredParentTaskDurationInMs, expectedParentTaskDurationInMs, Int32.MaxValue)

        for i in 0 .. nestedTasksDurationInMs.Length - 1 do
            let expectedDurationInMs = nestedTasksDurationInMs.[i]
            let taskId = sprintf "%s.%s-%i" parentTaskName nestedTaskPrefix i
            let mutable measuredDurationInMs = 0
            Assert.True(resultsDictionary.TryGetValue(taskId, &measuredDurationInMs))
            Assert.InRange(measuredDurationInMs, expectedDurationInMs, Int32.MaxValue)

    [<Theory>]
    [<InlineData(0, 0)>]
    [<InlineData(250, 0)>]
    [<InlineData(0, 250)>]
    [<InlineData(250, 250)>]
    [<InlineData(1000, 1000)>]
    member this.``Measure Nested Tasks With Padding`` (initialPaddingInMs: int) (endingPaddingInMs: int) =
        CompilationTracker.ClearData()
        let parentTaskName = "ParentTask"
        let nestedTaskCount = 5
        let nestedTaskDurationInMs = 500
        let nestedTaskPrefix = "NestedTask"

        // Measure time spent in a tasks.
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, parentTaskName)
        Thread.Sleep initialPaddingInMs

        for i in 0 .. nestedTaskCount do
            let taskName = sprintf "%s-%i" nestedTaskPrefix i
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, parentTaskName, taskName)
            Thread.Sleep nestedTaskDurationInMs
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, parentTaskName, taskName)

        Thread.Sleep endingPaddingInMs
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, parentTaskName)

        // Verify measured results are the expected ones.
        let resultsDictionary = MethodBase.GetCurrentMethod().Name |> getResultsDictionary

        let expectedParentTaskDurationInMs =
            initialPaddingInMs + (nestedTaskDurationInMs * nestedTaskCount) + endingPaddingInMs

        let mutable measuredParentTaskDurationInMs = 0
        Assert.True(resultsDictionary.TryGetValue(parentTaskName, &measuredParentTaskDurationInMs))
        Assert.InRange(measuredParentTaskDurationInMs, expectedParentTaskDurationInMs, Int32.MaxValue)

        for i in 0 .. nestedTaskCount do
            let taskId = sprintf "%s.%s-%i" parentTaskName nestedTaskPrefix i
            let mutable measuredTaskDurationInMs = 0
            Assert.True(resultsDictionary.TryGetValue(taskId, &measuredTaskDurationInMs))
            Assert.InRange(measuredTaskDurationInMs, nestedTaskDurationInMs, Int32.MaxValue)

    [<Theory; ClassData(typeof<MeasureDoubleNestedTasksTestCases>)>]
    member this.``Measure Double Nested Tasks`` (firstNestedTasksDurationInMs: int [])
                                                (secondNestedTasksDurationInMs: int [])
                                                =
        CompilationTracker.ClearData()
        let parentTaskName = "ParentTask"
        let firstNestedTaskName = "FirstNestedTask"
        let firstLeafTaskPrefix = "FirstLeafTask"
        let secondNestedTaskName = "SecondNestedTask"
        let secondLeafTaskPrefix = "SecondLeafTask"

        // Measure time spent in a tasks.
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, parentTaskName)
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, parentTaskName, firstNestedTaskName)

        for i in 0 .. firstNestedTasksDurationInMs.Length - 1 do
            let taskDurationInMs = firstNestedTasksDurationInMs.[i]
            let taskName = sprintf "%s-%i" firstLeafTaskPrefix i
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, firstNestedTaskName, taskName)
            Thread.Sleep taskDurationInMs
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, firstNestedTaskName, taskName)

        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, parentTaskName, firstNestedTaskName)
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, parentTaskName, secondNestedTaskName)

        for i in 0 .. secondNestedTasksDurationInMs.Length - 1 do
            let taskDurationInMs = secondNestedTasksDurationInMs.[i]
            let taskName = sprintf "%s-%i" secondLeafTaskPrefix i
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, secondNestedTaskName, taskName)
            Thread.Sleep taskDurationInMs
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, secondNestedTaskName, taskName)

        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, parentTaskName, secondNestedTaskName)
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, parentTaskName)

        // Verify measured results are the expected ones.
        let resultsDictionary = MethodBase.GetCurrentMethod().Name |> getResultsDictionary
        let expectedFirstNestedTaskDurationInMs = Array.sum firstNestedTasksDurationInMs
        let expectedSecondNestedTaskDurationInMs = Array.sum secondNestedTasksDurationInMs
        let expectedParentTaskDurationInMs = expectedFirstNestedTaskDurationInMs + expectedSecondNestedTaskDurationInMs
        let mutable measuredParentTaskDurationInMs = 0
        Assert.True(resultsDictionary.TryGetValue(parentTaskName, &measuredParentTaskDurationInMs))
        Assert.InRange(measuredParentTaskDurationInMs, expectedParentTaskDurationInMs, Int32.MaxValue)
        let firstNestedTaskId = sprintf "%s.%s" parentTaskName firstNestedTaskName
        let mutable mesauredFirstNestedTaskDurationInMs = 0
        Assert.True(resultsDictionary.TryGetValue(firstNestedTaskId, &mesauredFirstNestedTaskDurationInMs))
        Assert.InRange(mesauredFirstNestedTaskDurationInMs, expectedFirstNestedTaskDurationInMs, Int32.MaxValue)

        for i in 0 .. firstNestedTasksDurationInMs.Length - 1 do
            let expectedTaskDurationInMs = firstNestedTasksDurationInMs.[i]
            let taskId = sprintf "%s.%s-%i" firstNestedTaskId firstLeafTaskPrefix i
            let mutable measuredTaskDurationInMs = 0
            Assert.True(resultsDictionary.TryGetValue(taskId, &measuredTaskDurationInMs))
            Assert.InRange(measuredTaskDurationInMs, expectedTaskDurationInMs, Int32.MaxValue)

        let secondNestedTaskId = sprintf "%s.%s" parentTaskName secondNestedTaskName
        let mutable measuredSecondNestedTaskDurationInMs = 0
        Assert.True(resultsDictionary.TryGetValue(secondNestedTaskId, &measuredSecondNestedTaskDurationInMs))
        Assert.InRange(measuredSecondNestedTaskDurationInMs, expectedSecondNestedTaskDurationInMs, Int32.MaxValue)

        for i in 0 .. secondNestedTasksDurationInMs.Length - 1 do
            let expectedTaskDurationInMs = secondNestedTasksDurationInMs.[i]
            let taskId = sprintf "%s.%s-%i" secondNestedTaskId secondLeafTaskPrefix i
            let mutable measuredTaskDurationInMs = 0
            Assert.True(resultsDictionary.TryGetValue(taskId, &measuredTaskDurationInMs))
            Assert.InRange(measuredTaskDurationInMs, expectedTaskDurationInMs, Int32.MaxValue)

    [<Fact>]
    member this.``Publish Empty Results``() =
        CompilationTracker.ClearData()
        let resultsDictionary = MethodBase.GetCurrentMethod().Name |> getResultsDictionary
        Assert.Empty(resultsDictionary)

    [<Fact>]
    member this.``Publish When Still In Progess``() =
        CompilationTracker.ClearData()
        let taskName = "TestTask"

        // Start measuring a task but attempt to publish when it is still in progress.
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, taskName)

        Assert.Throws<InvalidOperationException>(fun () ->
            MethodBase.GetCurrentMethod().Name |> getResultsDictionary |> ignore)
        |> ignore


    [<Fact>]
    member this.``Start When Already In Progess``() =
        CompilationTracker.ClearData()
        let taskName = "TestTask"

        // Start measuring a task when it is already in progress.
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, taskName)

        Assert.Throws<InvalidOperationException>(fun () ->
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, taskName))
        |> ignore

    [<Fact>]
    member this.``Stop When Never Started``() =
        CompilationTracker.ClearData()
        let taskName = "TestTask"

        // Stop measuring a task when it was never started.
        Assert.Throws<InvalidOperationException>(fun () ->
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, taskName))
        |> ignore

    [<Fact>]
    member this.``Stop When Not In Progress``() =
        CompilationTracker.ClearData()
        let taskName = "TestTask"

        // Stop measuring a task when it was not in progress.
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.Start, null, taskName)
        CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, taskName)

        Assert.Throws<InvalidOperationException>(fun () ->
            CompilationTracker.OnCompilationTaskEvent(CompilationTaskEventType.End, null, taskName))
        |> ignore
