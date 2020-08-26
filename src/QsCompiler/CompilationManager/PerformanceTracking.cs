// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Quantum.QsCompiler.Diagnostics
{
    /// <summary>
    /// Represents the type of a task event.
    /// </summary>
    public enum CompilationTaskEventType
    {
        /// <summary>
        /// TODO
        /// </summary>
        Start,

        /// <summary>
        /// TODO
        /// </summary>
        End
    }

    /// <summary>
    /// Defines the handler for compilation task events.
    /// </summary>
    public delegate void CompilationTaskEventHandler(object sender, CompilationTaskEventType type, string parentTaskName, string taskName);

    /// <summary>
    /// TODO
    /// </summary>
    public static class PerformanceTracking
    {

        /// <summary>
        /// TODO
        /// </summary>
        public enum Task
        {
            /// <summary>
            /// TODO
            /// </summary>
            OverallCompilation,

            /// <summary>
            /// TODO
            /// </summary>
            Build,

            /// <summary>
            /// TODO
            /// </summary>
            OutputGeneration,

            /// <summary>
            /// TODO
            /// </summary>
            ReferenceLoading,

            /// <summary>
            /// TODO
            /// </summary>
            RewriteSteps,

            /// <summary>
            /// TODO
            /// </summary>
            SourcesLoading,

            /// <summary>
            /// TODO
            /// </summary>
            ReplaceTargetSpecificImplementations,

            /// <summary>
            /// TODO
            /// </summary>
            BinaryGeneration,

            /// <summary>
            /// TODO
            /// </summary>
            DllGeneration,

            /// <summary>
            /// TODO
            /// </summary>
            DocumentationGeneration,

            /// <summary>
            /// TODO
            /// </summary>
            SyntaxTreeSerialization,

            /// <summary>
            /// TODO
            /// </summary>
            LoadDataToStream,

            /// <summary>
            /// TODO
            /// </summary>
            DeserializerInit,

            /// <summary>
            /// TODO
            /// </summary>
            SyntaxTreeDeserialization
        }

        /// <summary>
        /// Used to raise a compilation task event.
        /// </summary>
        public static event CompilationTaskEventHandler CompilationTaskEvent;

        /// <summary>
        /// TODO
        /// </summary>
        private static IDictionary<Task, Task?> tasksHierarchy = new Dictionary<Task, Task?>()
        {
            { Task.OverallCompilation, null },
            { Task.Build, Task.OverallCompilation },
            { Task.OutputGeneration, Task.OverallCompilation },
            { Task.ReferenceLoading, Task.OverallCompilation },
            { Task.RewriteSteps, Task.OverallCompilation },
            { Task.SourcesLoading, Task.OverallCompilation },
            { Task.ReplaceTargetSpecificImplementations, Task.Build },
            { Task.BinaryGeneration, Task.OutputGeneration },
            { Task.DllGeneration, Task.OutputGeneration },
            { Task.DocumentationGeneration, Task.OutputGeneration },
            { Task.SyntaxTreeSerialization, Task.OutputGeneration },
            { Task.LoadDataToStream, Task.ReferenceLoading },
            { Task.DeserializerInit, Task.ReferenceLoading },
            { Task.SyntaxTreeDeserialization, Task.ReferenceLoading }
        };

        /// <summary>
        /// Raises a task start event.
        /// </summary>
        public static void TaskStart(Task task, object sender = null)
        {
            var parent = tasksHierarchy[task];
            CompilationTaskEvent?.Invoke(sender, CompilationTaskEventType.Start, parent?.ToString(), task.ToString());
        }

        /// <summary>
        /// Raises a task end event.
        /// </summary>
        public static void TaskEnd(Task task, object sender = null)
        {
            var parent = tasksHierarchy[task];
            CompilationTaskEvent?.Invoke(sender, CompilationTaskEventType.End, parent?.ToString(), task.ToString());
        }
    }
}
