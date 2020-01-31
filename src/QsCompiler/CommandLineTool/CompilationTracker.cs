// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;


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
            public readonly string ParentName;
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
            private readonly Stopwatch Watch;

            /// <summary>
            /// Generates a key that uniquely identifies a task in the compilation process based on the task's name and its parent's name.
            /// </summary>
            public static string GenerateKey(string parentName, string name)
            {
                return String.Format("{0}.{1}", parentName ?? "ROOT", name);
            }

            /// <summary>
            /// Creates a compilation task object and starts its stopwatch.
            /// </summary>
            public CompilationTask(string parentName, string name)
            {
                ParentName = parentName;
                Name = name;
                UtcStart = DateTime.UtcNow;
                UtcEnd = null;
                DurationInMs = null;
                Watch = Stopwatch.StartNew();
            }

            /// <summary>
            /// Halts the stopwatch of the compilation task and stores its duration.
            /// </summary>
            public void End()
            {
                UtcEnd = DateTime.UtcNow;
                Watch.Stop();
                DurationInMs = Watch.ElapsedMilliseconds;
            }

            /// <summary>
            /// Returns whether a compilation class is in progress.
            /// </summary>
            public bool IsInProgress()
            {
                return Watch.IsRunning;
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
                Task = task;
                Children = new Dictionary<string, CompilationTaskNode>();
            }
        }

        /// <summary>
        /// Represents a warning type detected while tracking compilation events.
        /// </summary>
        private enum WarningType
        {
            ProcessAlreadyExists,
            ProcessDoesNotExist,
            ProcessAlreadyEnded
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
                UtcDateTime = DateTime.UtcNow;
                Type = type;
                Key = key;
            }
        }

        // Private members.

        /// <summary>
        /// Defines a handler for a type of compilation task event.
        /// </summary>
        private delegate void CompilationTaskEventTypeHandler(CompilationLoader.CompilationTaskEventArgs eventArgs);

        /// <summary>
        /// Contains a handler that takes care of each type of task event.
        /// </summary>
        private static readonly IDictionary<CompilationLoader.CompilationTaskEventType, CompilationTaskEventTypeHandler> CompilationEventTypeHandlers = new Dictionary<CompilationLoader.CompilationTaskEventType, CompilationTaskEventTypeHandler>
        {
            { CompilationLoader.CompilationTaskEventType.Start, CompilationEventStartHandler },
            { CompilationLoader.CompilationTaskEventType.End, CompilationEventEndHandler }
        };

        /// <summary>
        /// Contains the compilation tasks tracked through the handled events.
        /// </summary>
        private static readonly IDictionary<string, CompilationTask> CompilationTasks = new Dictionary<string, CompilationTask>();
        /// <summary>
        /// Contains the warnings generated while handling the compiler tasks events.
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

            return compilationTasksForest;
        }

        /// <summary>
        /// Handles a compilation task start event.
        /// </summary>
        private static void CompilationEventStartHandler(CompilationLoader.CompilationTaskEventArgs eventArgs)
        {
            string key = CompilationTask.GenerateKey(eventArgs.ParentTaskName, eventArgs.TaskName);
            if (CompilationTasks.ContainsKey(key))
            {
                Warnings.Add(new Warning(WarningType.ProcessAlreadyExists, key));
                return;
            }

            CompilationTasks.Add(key, new CompilationTask(eventArgs.ParentTaskName, eventArgs.TaskName));
        }

        /// <summary>
        /// Handles a compilation task end event.
        /// </summary>
        private static void CompilationEventEndHandler(CompilationLoader.CompilationTaskEventArgs eventArgs)
        {
            var key = CompilationTask.GenerateKey(eventArgs.ParentTaskName, eventArgs.TaskName);
            if (!CompilationTasks.TryGetValue(key, out var task))
            {
                Warnings.Add(new Warning(WarningType.ProcessDoesNotExist, key));
                return;
            }

            if (!task.IsInProgress())
            {
                Warnings.Add(new Warning(WarningType.ProcessAlreadyEnded, key));
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
            CompilationEventTypeHandlers[args.Type](args);
        }

        /// <summary>
        /// Publishes the results to text files in the specified folder.
        /// </summary>
        public static void PublishResults(string outputFolder)
        {
            var compilationProcessesForest = BuildCompilationTasksHierarchy();
            var outputPath = Path.GetFullPath(outputFolder);
            var outputDirectoryInfo = new DirectoryInfo(outputPath);
            if (!outputDirectoryInfo.Exists)
            {
                outputDirectoryInfo.Create();
            }

            using (var file = File.CreateText(Path.Combine(outputPath, "compilationPerf.json")))
            {
                var serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };

                serializer.Serialize(file, compilationProcessesForest);
            }

            if (Warnings.Count > 0) {
                using (var file = File.CreateText(Path.Combine(outputPath, "compilationPerfWarnings.json")))
                {
                    var serializer = new JsonSerializer
                    {
                        Formatting = Formatting.Indented
                    };

                    serializer.Serialize(file, Warnings);
                }
            }
        }
    }
}
