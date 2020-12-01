// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;


namespace Microsoft.Quantum.Testing.Simulation
{
    internal static class Driver
    {
        /// <summary>
        /// Given the fully qualified name of a Q# operation 
        /// that takes Unit and returns Unit as command line argument, 
        /// executes that operation on the QuantumSimulator.
        /// If no operation has been specified, 
        /// executes the operation Main defined in Microsoft.Quantum.Testing.Simulation.
        /// </summary>
        /// <exception cref="EntryPointNotFoundException">
        /// A command line argument was given, but the corresponding operation was not found.
        /// </exception>
        static void Main(string[] args)
        {
            var entryPointName = args.FirstOrDefault() ?? "Microsoft.Quantum.Testing.Simulation.Main";
            var entryPoint = Assembly.GetExecutingAssembly()
                .GetType(entryPointName, throwOnError: false, ignoreCase: true)
                ?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
            if (entryPoint == null) throw new EntryPointNotFoundException($"{entryPointName}");

            using (var qsim = new QuantumSimulator())
            {
                var task = entryPoint.Invoke(null, new[] { qsim }) as Task<QVoid>;
                task?.Wait();
            }
        }
    }
}
