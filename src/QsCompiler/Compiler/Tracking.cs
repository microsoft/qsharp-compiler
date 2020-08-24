// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
}
