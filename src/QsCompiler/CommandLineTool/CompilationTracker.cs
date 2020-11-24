// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    /// <summary>
    /// Provides an event tracker of the compilation process for the purpose of assessing performance.
    /// </summary>
    public static class CompilationTracker
    {
        // Private classes and types.

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
            /// Contains the UTC datetime when the task started.
            /// </summary>
            public readonly DateTime UtcStart;

            /// <summary>
            /// Contains the UTC datetime when the task ended.
            /// </summary>
            public DateTime? UtcEnd;

            /// <summary>
            /// Contains the duration of the task in milliseconds.
            /// </summary>
            public long? DurationInMs;

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
                this.UtcStart = DateTime.UtcNow;
                this.UtcEnd = null;
                this.DurationInMs = null;
                this.watch = Stopwatch.StartNew();
            }

            /// <summary>
            /// Halts the stopwatch of the compilation task and stores its duration.
            /// </summary>
            public void End()
            {
                this.UtcEnd = DateTime.UtcNow;
                this.watch.Stop();
                this.DurationInMs = this.watch.ElapsedMilliseconds;
            }

            /// <summary>
            /// Returns whether a compilation class is in progress.
            /// </summary>
            public bool IsInProgress()
            {
                return this.watch.IsRunning;
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
                jsonWriter.WriteNumber(propertyName, this.Task.DurationInMs ?? -1);
                foreach (var entry in this.Children.OrderBy(e => e.Key))
                {
                    entry.Value.WriteToJson(jsonWriter, propertyName);
                }
            }
        }

        /// <summary>
        /// Represents a warning type detected while tracking compilation events.
        /// </summary>
        private enum WarningType
        {
            TaskAlreadyExists,
            TaskDoesNotExist,
            TaskAlreadyEnded,
            UknownTaskEventType
        }

        /// <summary>
        /// Represents a warning detected while tracking compilation events.
        /// </summary>
        private class Warning
        {
            public readonly WarningType Type;
            public readonly DateTime UtcDateTime;
            public readonly string Key;

            public Warning(WarningType type, string key)
            {
                this.UtcDateTime = DateTime.UtcNow;
                this.Type = type;
                this.Key = key;
            }
        }

        /// <summary>
        /// Defines a handler for a type of compilation task event.
        /// </summary>
        private delegate void CompilationTaskEventTypeHandler(CompilationLoader.CompilationTaskEventArgs eventArgs);

        // Private members.

        /// <summary>
        /// Represents the file name where the compilation performance data will be stored.
        /// </summary>
        private const string CompilationPerfDataFileName = "CompilationPerfData.json";

        /// <summary>
        /// Represents the file name where the compilation performance warnings will be stored.
        /// </summary>
        private const string CompilationPerfWarningsFileName = "CompilationPerfWarnings.json";

        /// <summary>
        /// Provides thread-safe access to the members and methods of this class.
        /// </summary>
        private static readonly object GlobalLock = new object();

        /// <summary>
        /// Contains a handler that takes care of each type of task event.
        /// Handlers are assumed to be not null.
        /// Note that thread-safe access to this member is done through the global lock.
        /// </summary>
        private static readonly IDictionary<CompilationLoader.CompilationTaskEventType, CompilationTaskEventTypeHandler> CompilationEventTypeHandlers = new Dictionary<CompilationLoader.CompilationTaskEventType, CompilationTaskEventTypeHandler>
        {
            { CompilationLoader.CompilationTaskEventType.Start, CompilationEventStartHandler },
            { CompilationLoader.CompilationTaskEventType.End, CompilationEventEndHandler }
        };

        /// <summary>
        /// Contains the compilation tasks tracked through the handled events.
        /// Note that thread-safe access to this member is done through the global lock.
        /// </summary>
        private static readonly IDictionary<string, CompilationTask> CompilationTasks = new Dictionary<string, CompilationTask>();

        /// <summary>
        /// Contains the warnings generated while handling the compiler tasks events.
        /// Note that thread-safe access to this member is done through the global lock.
        /// </summary>
        private static readonly IList<Warning> Warnings = new List<Warning>();

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
        private static void CompilationEventStartHandler(CompilationLoader.CompilationTaskEventArgs eventArgs)
        {
            Debug.Assert(Monitor.IsEntered(GlobalLock));
            string key = CompilationTask.GenerateKey(eventArgs.ParentTaskName, eventArgs.TaskName);
            if (CompilationTasks.ContainsKey(key))
            {
                Warnings.Add(new Warning(WarningType.TaskAlreadyExists, key));
                return;
            }

            CompilationTasks.Add(key, new CompilationTask(eventArgs.ParentTaskName, eventArgs.TaskName));
        }

        /// <summary>
        /// Handles a compilation task end event.
        /// </summary>
        private static void CompilationEventEndHandler(CompilationLoader.CompilationTaskEventArgs eventArgs)
        {
            Debug.Assert(Monitor.IsEntered(GlobalLock));
            var key = CompilationTask.GenerateKey(eventArgs.ParentTaskName, eventArgs.TaskName);
            if (!CompilationTasks.TryGetValue(key, out var task))
            {
                Warnings.Add(new Warning(WarningType.TaskDoesNotExist, key));
                return;
            }

            if (!task.IsInProgress())
            {
                Warnings.Add(new Warning(WarningType.TaskAlreadyEnded, key));
                return;
            }

            task.End();
        }

        // Public methods.

        /// <summary>
        /// Handles a compilation task event.
        /// </summary>
        public static void OnCompilationTaskEvent(object sender, CompilationLoader.CompilationTaskEventArgs args)
        {
            lock (GlobalLock)
            {
                if (CompilationEventTypeHandlers.TryGetValue(args.Type, out var hanlder))
                {
                    hanlder(args);
                }
                else
                {
                    Warnings.Add(new Warning(WarningType.UknownTaskEventType, args.Type.ToString()));
                }
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

            if (Warnings.Count > 0)
            {
                using (var file = File.CreateText(Path.Combine(outputPath, CompilationPerfWarningsFileName)))
                {
                    JsonSerializer.SerializeAsync(file.BaseStream, Warnings).Wait();
                }
            }
        }
    }
}
