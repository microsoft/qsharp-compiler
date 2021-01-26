// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.QIR
{
    /// <summary>
    /// Enum to distinguish different components that are ultimately combined
    /// to execute a program compiled into QIR.
    /// </summary>
    public enum Component
    {
        /// <summary>
        /// Contains all functions that are supported by the (classical) QIR runtime.
        /// </summary>
        RuntimeLibrary,

        /// <summary>
        /// Contains all functions that are supported by the quantum processor itself.
        /// </summary>
        QuantumInstructionSet
    }

    /// <summary>
    /// Static class that contains common conventions for QIR functions and callable values.
    /// </summary>
    public static class Callables
    {
        /// <summary>
        /// Generates a mangled name for a function that is expected to be provided by a component,
        /// such as QIR runtime library or the quantum instruction set, rather than defined in source code.
        /// The mangled names are a double underscore, "quantum", and another double underscore, followed by
        /// "rt" or "qis", another double underscore, and then the base name.
        /// </summary>
        /// <param name="kind">The component that is expected to provide the function</param>
        /// <param name="name">The name of the function without the component prefix</param>
        /// <returns>The mangled function name</returns>
        /// <exception cref="ArgumentException">No naming convention is defined for the given component.</exception>
        public static string FunctionName(Component component, string name) => component switch
        {
            Component.RuntimeLibrary => $"__quantum__rt__{name}",
            Component.QuantumInstructionSet => $"__quantum__qis__{name}",
            _ => throw new ArgumentException("unkown software component"),
        };
    }
}
