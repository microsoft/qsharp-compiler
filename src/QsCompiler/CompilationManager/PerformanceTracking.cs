// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Quantum.QsCompiler.Diagnostics
{
    /// <summary>
    /// Represents the type of a compilation task event.
    /// </summary>
    public enum CompilationTaskEventType
    {
        /// <summary>
        /// Represents a compilation task start.
        /// </summary>
        Start,

        /// <summary>
        /// Represents a compilation task end.
        /// </summary>
        End
    }

    /// <summary>
    /// Defines the handler for compilation task events.
    /// </summary>
    public delegate void CompilationTaskEventHandler(object sender, CompilationTaskEventType type, string parentTaskName, string taskName);

    /// <summary>
    /// Provides a way to track performance accross the compiler.
    /// </summary>
    public static class PerformanceTracking
    {
        /// <summary>
        /// Represents a compiler task whose performance is tracked.
        /// </summary>
        public enum Task
        {
            /// <summary>
            /// Overall compilation process.
            /// </summary>
            OverallCompilation,

            /// <summary>
            /// Task that builds the compilation object.
            /// </summary>
            Build,

            /// <summary>
            /// Task that generates the compiled binary, DLL and docs.
            /// </summary>
            OutputGeneration,

            /// <summary>
            /// Task that loads references.
            /// </summary>
            ReferenceLoading,

            /// <summary>
            /// Task that performs rewrite steps.
            /// </summary>
            RewriteSteps,

            /// <summary>
            /// Task that loads sources.
            /// </summary>
            SourcesLoading,

            /// <summary>
            /// Task that replaces specific implementations as part of the 'Build' task.
            /// </summary>
            ReplaceTargetSpecificImplementations,

            /// <summary>
            /// Task that generates a binary as part of the 'OutputGeneration' task.
            /// </summary>
            BinaryGeneration,

            /// <summary>
            /// Task that generates a DLL as part of the 'OutputGeneration' task.
            /// </summary>
            DllGeneration,

            /// <summary>
            /// Task that generates documentation as part of the 'OutputGeneration' task.
            /// </summary>
            DocumentationGeneration,

            /// <summary>
            /// Task that serializes the syntax tree to be appended to the DLL as part of the 'OutputGeneration' task.
            /// </summary>
            SyntaxTreeSerialization,

            /// <summary>
            /// Task that loads data from references to a stream as part of the 'ReferenceLoading' task.
            /// </summary>
            LoadDataFromReferenceToStream,

            /// <summary>
            /// Task that initializes the deserializer object as part of the 'ReferenceLoading' task.
            /// </summary>
            DeserializerInit,

            /// <summary>
            /// Task that deserializes as part of the 'ReferenceLoading' task.
            /// </summary>
            SyntaxTreeDeserialization
        }

        /// <summary>
        /// Used to raise a compilation task event.
        /// </summary>
        public static event CompilationTaskEventHandler CompilationTaskEvent;

        /// <summary>
        /// Describes the hierarchichal relationship between tasks.
        /// The key represents the task and the associated value represents the parent of that task.
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
            { Task.LoadDataFromReferenceToStream, Task.ReferenceLoading },
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
