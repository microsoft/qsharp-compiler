// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.Quantum.QsCompiler.Diagnostics;

namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    /// <summary>
    /// Provides an event tracker of the compilation process for the purpose of assessing performance.
    /// </summary>
    public static class CompilationTracker
    {
        /// <summary>
        /// Represents a task performed by the compiler (eg. source loading, reference loading, syntax tree serialization, etc.).
        /// </summary>
        private class CompilationTask
        {
            /// <summary>
            /// Represents the name of the parent compilation task.
            /// </summary>
            public readonly string? ParentName;

            /// <summary>
            /// Represents the name of the compilation task.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Gets the duration of the task in milliseconds.
            /// </summary>
            public long DurationInMs
            {
                get
                {
                    if (this.IntervalCount == 0)
                    {
                        throw new InvalidOperationException($"Attempt to get task '{this.Id}' duration when no interval has been measured");
                    }
                    else if (this.IsInProgress())
                    {
                        throw new InvalidOperationException($"Attempt to get task '{this.Id}' duration when measurement is in progress");
                    }

                    return this.watch.ElapsedMilliseconds;
                }
            }

            /// <summary>
            /// Identifier of the task.
            /// </summary>
            public string Id => GenerateKey(this.ParentName, this.Name);

            /// <summary>
            /// Number of intervals (start/stop cycles) measured.
            /// </summary>
            public int IntervalCount { get; private set; }

            /// <summary>
            /// Stopwatch used to measure the duration of the task.
            /// </summary>
            private readonly Stopwatch watch;

            /// <summary>
            /// Generates a key that uniquely identifies a task in the compilation process based on the task's name and its parent's name.
            /// </summary>
            internal static string GenerateKey(string? parentName, string name)
            {
                return string.Format("{0}.{1}", parentName ?? "ROOT", name);
            }

            /// <summary>
            /// Creates a compilation task object and starts its stopwatch.
            /// </summary>
            public CompilationTask(string? parentName, string name)
            {
                this.ParentName = parentName;
                this.Name = name;
                this.watch = new Stopwatch();
            }

            /// <summary>
            /// Returns whether a compilation class is in progress.
            /// </summary>
            public bool IsInProgress()
            {
                return this.watch.IsRunning;
            }

            /// <summary>
            /// Starts/resumes time accounting for this task.
            /// </summary>
            public void Start()
            {
                if (this.IsInProgress())
                {
                    throw new InvalidOperationException($"Attempt to start task '{this.Id}' when it is already in progress");
                }

                this.watch.Start();
            }

            /// <summary>
            /// Stops/pauses time accounting for this task.
            /// </summary>
            public void Stop()
            {
                if (!this.IsInProgress())
                {
                    throw new InvalidOperationException($"Attempt to stop task '{this.Id}' when it is not in progress");
                }

                this.watch.Stop();
                this.IntervalCount++;
            }
        }

        /// <summary>
        /// Represents a node in the tree of tasks performed by the compiler.
        /// </summary>
        private class CompilationTaskNode
        {
            public readonly CompilationTask Task;
            public readonly IDictionary<string, CompilationTaskNode> Children;

            public CompilationTaskNode(CompilationTask task)
            {
                this.Task = task;
                this.Children = new Dictionary<string, CompilationTaskNode>();
            }

            public void WriteToJson(Utf8JsonWriter jsonWriter, string? prefix)
            {
                var preparedPrefix = "";
                if (!string.IsNullOrEmpty(prefix))
                {
                    preparedPrefix = $"{prefix}.";
                }

                var propertyName = $"{preparedPrefix}{this.Task.Name}";
                jsonWriter.WriteNumber(propertyName, this.Task.DurationInMs);
                foreach (var entry in this.Children.OrderBy(e => e.Key))
                {
                    entry.Value.WriteToJson(jsonWriter, propertyName);
                }
            }
        }

        // Public members.

        /// <summary>
        /// Represents the file name where the compilation performance data will be stored.
        /// </summary>
        public const string CompilationPerfDataFileName = "CompilationPerfData.json";

        /// <summary>
        /// Defines a handler for a type of compilation task event.
        /// </summary>
        private delegate void CompilationTaskEventTypeHandler(string? parentTaskName, string taskName);

        // Private members.

        /// <summary>
        /// Provides thread-safe access to the members and methods of this class.
        /// </summary>
        private static readonly object GlobalLock = new object();

        /// <summary>
        /// Contains a handler that takes care of each type of task event.
        /// Handlers are assumed to be not null.
        /// Note that thread-safe access to this member is done through the global lock.
        /// </summary>
        private static readonly IDictionary<CompilationTaskEventType, CompilationTaskEventTypeHandler> CompilationEventTypeHandlers = new Dictionary<CompilationTaskEventType, CompilationTaskEventTypeHandler>
        {
            { CompilationTaskEventType.Start, CompilationEventStartHandler },
            { CompilationTaskEventType.End, CompilationEventEndHandler }
        };

        /// <summary>
        /// Contains the compilation tasks tracked through the handled events.
        /// Note that thread-safe access to this member is done through the global lock.
        /// </summary>
        private static readonly IDictionary<string, CompilationTask> CompilationTasks = new Dictionary<string, CompilationTask>();

        // Private methods.

        /// <summary>
        /// Creates a hierarchical structure that contains the compilation tasks.
        /// </summary>
        private static IList<CompilationTaskNode> BuildCompilationTasksHierarchy()
        {
            var compilationTasksForest = new List<CompilationTaskNode>();
            var toFindChildrenNodes = new Queue<CompilationTaskNode>();
            lock (GlobalLock)
            {
                // First add the roots (top-level tasks) of all trees to the forest.
                foreach (var entry in CompilationTasks)
                {
                    if (entry.Value.ParentName == null)
                    {
                        var node = new CompilationTaskNode(entry.Value);
                        compilationTasksForest.Add(node);
                        toFindChildrenNodes.Enqueue(node);
                    }
                }

                // Iterate through the tasks until all of them have been added to the hierarchy.
                while (toFindChildrenNodes.Count > 0)
                {
                    var parentNode = toFindChildrenNodes.Dequeue();
                    foreach (var entry in CompilationTasks)
                    {
                        if (parentNode.Task.Name.Equals(entry.Value.ParentName))
                        {
                            var childNode = new CompilationTaskNode(entry.Value);
                            parentNode.Children.Add(childNode.Task.Name, childNode);
                            toFindChildrenNodes.Enqueue(childNode);
                        }
                    }
                }
            }

            return compilationTasksForest;
        }

        /// <summary>
        /// Handles a compilation task start event.
        /// </summary>
        private static void CompilationEventStartHandler(string? parentTaskName, string taskName)
        {
            Debug.Assert(Monitor.IsEntered(GlobalLock));
            string key = CompilationTask.GenerateKey(parentTaskName, taskName);
            if (!CompilationTasks.TryGetValue(key, out var task))
            {
                task = new CompilationTask(parentTaskName, taskName);
                CompilationTasks.Add(key, task);
            }

            task.Start();
        }

        /// <summary>
        /// Handles a compilation task end event.
        /// </summary>
        private static void CompilationEventEndHandler(string? parentTaskName, string taskName)
        {
            Debug.Assert(Monitor.IsEntered(GlobalLock));
            var key = CompilationTask.GenerateKey(parentTaskName, taskName);
            if (!CompilationTasks.TryGetValue(key, out var task))
            {
                throw new InvalidOperationException($"Attempt to stop task '{key}' which does not exist");
            }

            task.Stop();
        }

        // Public methods.

        /// <summary>
        /// Clears tracked data.
        /// </summary>
        public static void ClearData()
        {
            lock (GlobalLock)
            {
                CompilationTasks.Clear();
            }
        }

        /// <summary>
        /// Handles a compilation task event.
        /// </summary>
        public static void OnCompilationTaskEvent(CompilationTaskEventType type, string? parentTaskName, string taskName)
        {
            lock (GlobalLock)
            {
                if (!CompilationEventTypeHandlers.TryGetValue(type, out var handler))
                {
                    throw new ArgumentException($"No handler for compilation task event type '{type}' exists");
                }

                handler(parentTaskName, taskName);
            }
        }

        /// <summary>
        /// Publishes the results to text files in the specified folder.
        /// </summary>
        /// <exception cref="NotSupportedException"><paramref name="outputFolder"/> is malformed.</exception>
        /// <exception cref="IOException"><paramref name="outputFolder"/> is a file path.</exception>
        public static void PublishResults(string outputFolder)
        {
            var compilationProcessesForest = BuildCompilationTasksHierarchy();
            var outputPath = Path.GetFullPath(outputFolder);
            Directory.CreateDirectory(outputPath);
            using (var file = File.CreateText(Path.Combine(outputPath, CompilationPerfDataFileName)))
            {
                var jsonWriterOptions = new JsonWriterOptions()
                {
                    Indented = true
                };

                var jsonWriter = new Utf8JsonWriter(file.BaseStream, jsonWriterOptions);
                jsonWriter.WriteStartObject();
                foreach (var tree in compilationProcessesForest.OrderBy(t => t.Task.Name))
                {
                    tree.WriteToJson(jsonWriter, null);
                }

                jsonWriter.WriteEndObject();
                jsonWriter.Flush();
            }
        }
    }
}
