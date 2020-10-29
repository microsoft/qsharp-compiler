// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Quantum.QsCompiler.CommandLineCompiler;
using Xunit;

namespace Tests.PerformanceTracking
{
    public class CompilationTrackerTests
    {
        private static readonly string ResultsFolderRootName = "Results";
        private static readonly int TaskErrorMarginInMs = 25;

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(25)]
        [InlineData(100)]
        [InlineData(350)]
        [InlineData(635)]
        [InlineData(1000)]
        [InlineData(3333)]
        [InlineData(5050)]
        [InlineData(10000)]
        public void MeasureTask(int durationInMs)
        {
            CompilationTracker.ClearData();
            const string taskName = "TestTask";

            // Measure time spent in a task.
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                null,
                taskName);

            Thread.Sleep(durationInMs);
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                null,
                taskName);

            // Publish measurement results.
            var resultsFolder = Path.Combine(ResultsFolderRootName, GetCurrentMethodName());
            CompilationTracker.PublishResults(resultsFolder);
            var resultsFile = Path.Combine(resultsFolder, CompilationTracker.CompilationPerfDataFileName);
            var resultsDictionary = ParseJsonToDictionary(resultsFile);

            // Verify measured results are the expected ones.
            Assert.True(resultsDictionary.TryGetValue(taskName, out var measuredDurationInMs));
            var (lowerLimit, upperLimit) = CalculateMeasurementLimits(durationInMs, TaskErrorMarginInMs);
            Assert.InRange(measuredDurationInMs, lowerLimit, upperLimit);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(325)]
        [InlineData(700)]
        [InlineData(1050)]
        public void MeasureTaskMultipleTimes(int taskDurationInMs)
        {
            CompilationTracker.ClearData();
            const int delayBetweenMeasurementsInMs = 100;
            const int measurementCount = 5;
            const string taskName = "TestTask";

            // Measure time spent in a task.
            for (int index = 0; index < measurementCount; index++)
            {
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                    null,
                    taskName);

                Thread.Sleep(taskDurationInMs);
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                    null,
                    taskName);

                Thread.Sleep(delayBetweenMeasurementsInMs);
            }

            // Publish measurement results.
            var resultsFolder = Path.Combine(ResultsFolderRootName, GetCurrentMethodName());
            CompilationTracker.PublishResults(resultsFolder);
            var resultsFile = Path.Combine(resultsFolder, CompilationTracker.CompilationPerfDataFileName);
            var resultsDictionary = ParseJsonToDictionary(resultsFile);

            // Verify measured results are the expected ones.
            Assert.True(resultsDictionary.TryGetValue(taskName, out var measuredDurationInMs));
            var acumulatedErrorMargin = TaskErrorMarginInMs * measurementCount;
            var expectedDurationInMs = taskDurationInMs * measurementCount;
            var (lowerLimit, upperLimit) = CalculateMeasurementLimits(expectedDurationInMs, acumulatedErrorMargin);
            Assert.InRange(measuredDurationInMs, lowerLimit, upperLimit);
        }

        [Theory]
        [InlineData(new int[] { })]
        [InlineData(new int[] { 1000 })]
        [InlineData(new int[] { 150, 225 })]
        [InlineData(new int[] { 1000, 250, 825, 1555 })]
        [InlineData(new int[] { 250, 300, 400, 200, 150, 750, 425, 1035, 900 })]
        public void MeasureTasks1LevelNested(int[] nestedTasksDurationInMs)
        {
            CompilationTracker.ClearData();
            const string parentTaskName = "ParentTask";
            const string nestedTaskPrefix = "NestedTask";

            // Measure time spent in a tasks.
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                null,
                parentTaskName);

            var expectedParentTaskDurationInMs = 0;
            for (int taskIndex = 0; taskIndex < nestedTasksDurationInMs.Length; taskIndex++)
            {
                var taskName = $"{nestedTaskPrefix}-{taskIndex}";
                var taskDurationInMs = nestedTasksDurationInMs[taskIndex];
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                    parentTaskName,
                    taskName);

                Thread.Sleep(taskDurationInMs);
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                    parentTaskName,
                    taskName);

                expectedParentTaskDurationInMs += taskDurationInMs;
            }

            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                null,
                parentTaskName);

            // Publish measurement results.
            var resultsFolder = Path.Combine(ResultsFolderRootName, GetCurrentMethodName());
            CompilationTracker.PublishResults(resultsFolder);
            var resultsFile = Path.Combine(resultsFolder, CompilationTracker.CompilationPerfDataFileName);
            var resultsDictionary = ParseJsonToDictionary(resultsFile);

            // Verify measured results are the expected ones.
            Assert.True(resultsDictionary.TryGetValue(parentTaskName, out var measuredParentTaskDurationInMs));
            var acumulatedErrorMargin = TaskErrorMarginInMs * nestedTasksDurationInMs.Length;
            int lowerLimit, upperLimit;
            (lowerLimit, upperLimit) = CalculateMeasurementLimits(expectedParentTaskDurationInMs, acumulatedErrorMargin);
            Assert.InRange(measuredParentTaskDurationInMs, lowerLimit, upperLimit);
            for (int taskIndex = 0; taskIndex < nestedTasksDurationInMs.Length; taskIndex++)
            {
                var taskName = $"{nestedTaskPrefix}-{taskIndex}";
                var taskId = $"{parentTaskName}.{taskName}";
                var expectedTaskDurationInMs = nestedTasksDurationInMs[taskIndex];
                Assert.True(resultsDictionary.TryGetValue(taskId, out var measuredTaskDurationInMs));
                (lowerLimit, upperLimit) = CalculateMeasurementLimits(expectedTaskDurationInMs, TaskErrorMarginInMs);
                Assert.InRange(measuredTaskDurationInMs, lowerLimit, upperLimit);
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(250, 0)]
        [InlineData(0, 250)]
        [InlineData(250, 250)]
        [InlineData(1000, 1000)]
        public void MeasureTasks1LevelNestedPadded(int startPaddingInMs, int endPaddingInMs)
        {
            CompilationTracker.ClearData();
            const int nestedTaskCount = 5;
            const int nestedTaskDurationInMs = 500;
            const string parentTaskName = "ParentTask";
            const string nestedTaskPrefix = "NestedTask";

            // Measure time spent in a tasks.
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                null,
                parentTaskName);

            Thread.Sleep(startPaddingInMs);
            for (int index = 0; index < nestedTaskCount; index++)
            {
                var taskName = $"{nestedTaskPrefix}-{index}";
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                    parentTaskName,
                    taskName);

                Thread.Sleep(nestedTaskDurationInMs);
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                    parentTaskName,
                    taskName);
            }

            Thread.Sleep(endPaddingInMs);
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                null,
                parentTaskName);

            // Publish measurement results.
            var resultsFolder = Path.Combine(ResultsFolderRootName, GetCurrentMethodName());
            CompilationTracker.PublishResults(resultsFolder);
            var resultsFile = Path.Combine(resultsFolder, CompilationTracker.CompilationPerfDataFileName);
            var resultsDictionary = ParseJsonToDictionary(resultsFile);

            // Verify measured results are the expected ones.
            Assert.True(resultsDictionary.TryGetValue(parentTaskName, out var measuredParentTaskDurationInMs));
            var expectedParentTaskDuration =
                startPaddingInMs +
                (nestedTaskCount * nestedTaskDurationInMs) +
                endPaddingInMs;

            var acumulatedErrorMargin = TaskErrorMarginInMs * nestedTaskCount;
            var (lowerLimit, upperLimit) = CalculateMeasurementLimits(expectedParentTaskDuration, acumulatedErrorMargin);
            Assert.InRange(measuredParentTaskDurationInMs, lowerLimit, upperLimit);
        }

        [Theory]
        [InlineData(new int[] { },
                    new int[] { })]
        [InlineData(new int[] { },
                    new int[] { 500 })]
        [InlineData(new int[] { 500 },
                    new int[] { })]
        [InlineData(new int[] { 0 },
                    new int[] { 500 })]
        [InlineData(new int[] { 500 },
                    new int[] { 0 })]
        [InlineData(new int[] { 500 },
                    new int[] { 500 })]
        [InlineData(new int[] { 150, 325 },
                    new int[] { 500, 250, 765 })]
        [InlineData(new int[] { 500, 250, 765 },
                    new int[] { 150, 325 })]
        [InlineData(new int[] { 100 },
                    new int[] { 1035, 555, 775, 225, 2500 })]
        [InlineData(new int[] { 350, 155, 670, 2250, 25, 1110 },
                    new int[] { 0 })]
        public void MeasureTasks2LevelNested(
            int[] firstNestedTasksDurationInMs,
            int[] secondNestedTasksDurationInMs)
        {
            CompilationTracker.ClearData();
            const string parentTaskName = "ParentTask";
            const string firstNestedTaskName = "FirstNestedTask";
            const string firstLeafTaskPrefix = "FirstLeafTask";
            const string secondNestedTaskName = "SecondNestedTask";
            const string secondLeafTaskPrefix = "SecondLeafTask";

            // Measure time spent in a tasks.
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                null,
                parentTaskName);

            var expectedFirstNestedTaskDurationInMs = 0;
            CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                    parentTaskName,
                    firstNestedTaskName);

            for (int taskIndex = 0; taskIndex < firstNestedTasksDurationInMs.Length; taskIndex++)
            {
                var taskName = $"{firstLeafTaskPrefix}-{taskIndex}";
                var taskDurationInMs = firstNestedTasksDurationInMs[taskIndex];
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                    firstNestedTaskName,
                    taskName);

                Thread.Sleep(taskDurationInMs);
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                    firstNestedTaskName,
                    taskName);

                expectedFirstNestedTaskDurationInMs += taskDurationInMs;
            }

            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                parentTaskName,
                firstNestedTaskName);

            var expectedSecondNestedTaskDurationInMs = 0;
            CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                    parentTaskName,
                    secondNestedTaskName);

            for (int taskIndex = 0; taskIndex < secondNestedTasksDurationInMs.Length; taskIndex++)
            {
                var taskName = $"{secondLeafTaskPrefix}-{taskIndex}";
                var taskDurationInMs = secondNestedTasksDurationInMs[taskIndex];
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                    secondNestedTaskName,
                    taskName);

                Thread.Sleep(taskDurationInMs);
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                    secondNestedTaskName,
                    taskName);

                expectedSecondNestedTaskDurationInMs += taskDurationInMs;
            }

            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                parentTaskName,
                secondNestedTaskName);

            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                null,
                parentTaskName);

            // Publish measurement results.
            var resultsFolder = Path.Combine(ResultsFolderRootName, GetCurrentMethodName());
            CompilationTracker.PublishResults(resultsFolder);
            var resultsFile = Path.Combine(resultsFolder, CompilationTracker.CompilationPerfDataFileName);
            var resultsDictionary = ParseJsonToDictionary(resultsFile);

            // Verify measured results are the expected ones.
            Assert.True(resultsDictionary.TryGetValue(parentTaskName, out var measuredParentTaskDurationInMs));
            var totalErrorMargin = TaskErrorMarginInMs * (firstNestedTasksDurationInMs.Length + secondNestedTasksDurationInMs.Length);
            var expectedParentTaskDurationInMs = expectedFirstNestedTaskDurationInMs + expectedSecondNestedTaskDurationInMs;
            int lowerLimit, upperLimit;
            (lowerLimit, upperLimit) = CalculateMeasurementLimits(expectedParentTaskDurationInMs, totalErrorMargin);
            Assert.InRange(measuredParentTaskDurationInMs, lowerLimit, upperLimit);
            var firstNestedTaskId = $"{parentTaskName}.{firstNestedTaskName}";
            Assert.True(resultsDictionary.TryGetValue(firstNestedTaskId, out var measuredFirstNestedTaskDurationInMs));
            var firstNestedTaskErrorMargin = TaskErrorMarginInMs * firstNestedTasksDurationInMs.Length;
            (lowerLimit, upperLimit) = CalculateMeasurementLimits(expectedFirstNestedTaskDurationInMs, firstNestedTaskErrorMargin);
            Assert.InRange(measuredFirstNestedTaskDurationInMs, lowerLimit, upperLimit);
            for (int taskIndex = 0; taskIndex < firstNestedTasksDurationInMs.Length; taskIndex++)
            {
                var taskName = $"{firstLeafTaskPrefix}-{taskIndex}";
                var taskId = $"{firstNestedTaskId}.{taskName}";
                var expectedTaskDurationInMs = firstNestedTasksDurationInMs[taskIndex];
                Assert.True(resultsDictionary.TryGetValue(taskId, out var measuredTaskDurationInMs));
                (lowerLimit, upperLimit) = CalculateMeasurementLimits(expectedTaskDurationInMs, TaskErrorMarginInMs);
                Assert.InRange(measuredTaskDurationInMs, lowerLimit, upperLimit);
            }

            var secondNestedTaskId = $"{parentTaskName}.{secondNestedTaskName}";
            Assert.True(resultsDictionary.TryGetValue(secondNestedTaskId, out var measuredSecondNestedTaskDurationInMs));
            var secondNestedTaskErrorMargin = TaskErrorMarginInMs * secondNestedTasksDurationInMs.Length;
            (lowerLimit, upperLimit) = CalculateMeasurementLimits(expectedSecondNestedTaskDurationInMs, secondNestedTaskErrorMargin);
            Assert.InRange(measuredSecondNestedTaskDurationInMs, lowerLimit, upperLimit);
            for (int taskIndex = 0; taskIndex < secondNestedTasksDurationInMs.Length; taskIndex++)
            {
                var taskName = $"{secondLeafTaskPrefix}-{taskIndex}";
                var taskId = $"{secondNestedTaskId}.{taskName}";
                var expectedTaskDurationInMs = secondNestedTasksDurationInMs[taskIndex];
                Assert.True(resultsDictionary.TryGetValue(taskId, out var measuredTaskDurationInMs));
                (lowerLimit, upperLimit) = CalculateMeasurementLimits(expectedTaskDurationInMs, TaskErrorMarginInMs);
                Assert.InRange(measuredTaskDurationInMs, lowerLimit, upperLimit);
            }
        }

        [Fact]
        public void PublishEmptyResults()
        {
            CompilationTracker.ClearData();
            var resultsFolder = Path.Combine(ResultsFolderRootName, GetCurrentMethodName());
            CompilationTracker.PublishResults(resultsFolder);
            var resultsFile = Path.Combine(resultsFolder, CompilationTracker.CompilationPerfDataFileName);
            var resultsDictionary = ParseJsonToDictionary(resultsFile);
            Assert.Empty(resultsDictionary);
        }

        [Fact]
        public void PublishWhenStillInProgress()
        {
            CompilationTracker.ClearData();
            const string taskName = "TestTask";

            // Start measuring a task but attempt to publish when it is still in progress.
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                null,
                taskName);

            var resultsFolder = Path.Combine(ResultsFolderRootName, GetCurrentMethodName());
            Exception caughtException = null;
            try
            {
                CompilationTracker.PublishResults(resultsFolder);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.NotNull(caughtException);
            Assert.IsType<InvalidOperationException>(caughtException);
        }

        [Fact]
        public void StartWhenAlreadyInProgress()
        {
            CompilationTracker.ClearData();
            const string taskName = "TestTask";

            // Start measuring a task when it is already in progress.
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                null,
                taskName);

            Exception caughtException = null;
            try
            {
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                    null,
                    taskName);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.NotNull(caughtException);
            Assert.IsType<InvalidOperationException>(caughtException);
        }

        [Fact]
        public void StopWhenNeverStarted()
        {
            CompilationTracker.ClearData();
            const string taskName = "TestTask";

            // Stop measuring a task when it was never started.
            Exception caughtException = null;
            try
            {
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                    null,
                    taskName);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.NotNull(caughtException);
            Assert.IsType<InvalidOperationException>(caughtException);
        }

        [Fact]
        public void StopWhenNotInProgress()
        {
            CompilationTracker.ClearData();
            const string taskName = "TestTask";

            // Stop measuring a task when it was not in progress.
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                null,
                taskName);

            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                null,
                taskName);

            Exception caughtException = null;
            try
            {
                CompilationTracker.OnCompilationTaskEvent(
                    Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                    null,
                    taskName);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.NotNull(caughtException);
            Assert.IsType<InvalidOperationException>(caughtException);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentMethodName()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(1);
            return frame.GetMethod().Name;
        }

        private static (int, int) CalculateMeasurementLimits(int expected, int errorMargin)
        {
            return (Math.Max(expected - errorMargin, 0), expected + errorMargin);
        }

        private static IDictionary<string, int> ParseJsonToDictionary(string path)
        {
            var jsonString = File.ReadAllText(path);
            var dictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(jsonString);
            return dictionary;
        }
    }
}
