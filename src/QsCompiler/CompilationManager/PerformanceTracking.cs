﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Quantum.QsCompiler.Diagnostics
{
    /// <summary>
    /// Defines the handler for compilation task events.
    /// </summary>
    public delegate void CompilationTaskEventHandler(CompilationTaskEventType type, string? parentTaskName, string taskName);

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
        End,
    }

    /// <summary>
    /// Provides a way to track performance accross the compiler.
    /// </summary>
    public static class PerformanceTracking
    {
        /// <summary>
        /// Describes the hierarchichal relationship between tasks.
        /// The key represents the task and the associated value represents the parent of that task.
        /// </summary>
        private static readonly IDictionary<Task, Task?> TasksHierarchy = new Dictionary<Task, Task?>()
        {
            { Task.OverallCompilation, null },
            { Task.Build, Task.OverallCompilation },
            { Task.OutputGeneration, Task.OverallCompilation },
            { Task.ReferenceLoading, Task.OverallCompilation },
            { Task.RewriteSteps, Task.OverallCompilation },
            { Task.SingleRewriteStep, Task.RewriteSteps },
            { Task.SourcesLoading, Task.OverallCompilation },
            { Task.ReplaceTargetSpecificImplementations, Task.Build },
            { Task.BinaryGeneration, Task.OutputGeneration },
            { Task.DllGeneration, Task.OutputGeneration },
            { Task.QirGeneration, Task.OutputGeneration },
            { Task.DocumentationGeneration, Task.OutputGeneration },
            { Task.SyntaxTreeSerialization, Task.OutputGeneration },
            { Task.LoadDataFromReferenceToStream, Task.ReferenceLoading },
            { Task.DeserializerInit, Task.ReferenceLoading },
            { Task.SyntaxTreeDeserialization, Task.ReferenceLoading },
            { Task.HeaderAttributesLoading, Task.ReferenceLoading },
            { Task.ReferenceHeadersCreation, Task.ReferenceLoading },
        };

        /// <summary>
        /// Used to raise a compilation task event.
        /// </summary>
        public static event CompilationTaskEventHandler? CompilationTaskEvent;

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
            /// Task that generates QIR as part of the 'OutputGeneration' task.
            /// </summary>
            QirGeneration,

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
            SyntaxTreeDeserialization,

            /// <summary>
            /// Task for a specific rewrite step.
            /// </summary>
            /// <remarks>
            /// These tasks should be accompanied with details of which rewrite step it is specific to.
            /// </remarks>
            SingleRewriteStep,

            /// <summary>
            /// Task that loads data directly from a .NET DLL.
            /// </summary>
            HeaderAttributesLoading,

            /// <summary>
            /// Task that creates headers from references.
            /// </summary>
            ReferenceHeadersCreation,
        }

        /// <summary>
        /// Gets a value indicating whether a failure ocurred while tracking performance.
        /// </summary>
        public static bool FailureOccurred => FailureException != null;

        /// <summary>
        /// Gets the exception that caused the failure to occur.
        /// </summary>
        public static Exception? FailureException { get; private set; }

        /// <summary>
        /// Raises a task start event.
        /// </summary>
        /// <param name="task">Indicates the task to start.</param>
        /// <param name="leafSuffix">
        /// Supplies a string to label the task more precisely.
        /// N.B. Can only be non-null on tasks that are not a parent of another task.
        /// </param>
        public static void TaskStart(Task task, string? leafSuffix = null)
        {
            InvokeTaskEvent(CompilationTaskEventType.Start, task, leafSuffix);
        }

        /// <summary>
        /// Raises a task end event.
        /// </summary>
        /// <param name="task">Indicates the task to end.</param>
        /// <param name="leafSuffix">
        /// Supplies a string to label the task more precisely.
        /// N.B. Can only be non-null on tasks that are not a parent of another task.
        /// </param>
        public static void TaskEnd(Task task, string? leafSuffix = null)
        {
            InvokeTaskEvent(CompilationTaskEventType.End, task, leafSuffix);
        }

        /// <summary>
        /// Gets the parent of <paramref name="task"/>.
        /// </summary>
        /// <exception cref="ArgumentException">When the parent is not defined.</exception>
        private static Task? GetTaskParent(Task task)
        {
            if (!TasksHierarchy.TryGetValue(task, out var parent))
            {
                throw new ArgumentException($"Task '{task}' does not have a defined parent");
            }

            return parent;
        }

        private static bool IsLeaf(this Task task) => !TasksHierarchy.Values.Contains(task);

        /// <summary>
        /// Invokes a compilation task event.
        /// </summary>
        /// <remarks>
        /// If an exception occurs when calling this method, the error message is cached and subsequent calls do nothing.
        /// </remarks>
        private static void InvokeTaskEvent(CompilationTaskEventType eventType, Task task, string? leafSuffix = null)
        {
            if (FailureOccurred)
            {
                return;
            }

            if (!(leafSuffix is null) && !task.IsLeaf())
            {
                throw new ArgumentException($"Non-leaf Task '{task}' cannot use a suffix");
            }

            try
            {
                var parent = GetTaskParent(task);
                var taskId = task.ToString() + (leafSuffix is null ? string.Empty : $"-{Regex.Replace(leafSuffix, @"\s+", string.Empty)}");
                CompilationTaskEvent?.Invoke(eventType, parent?.ToString(), taskId.ToString());
            }
            catch (Exception ex)
            {
                FailureException = ex;
            }
        }
    }
}
