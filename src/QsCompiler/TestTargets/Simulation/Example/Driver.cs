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
        /// Executes the Q# operation Main defined in Microsoft.Quantum.Testing.Simulation on the QuantumSimulator.
        /// </summary>
        static void Main(string[] args)
        {
            var entryPointName = args.Length == 1 ? args.Single() : "Microsoft.Quantum.Testing.Simulation.Main";
            var entryPoint = Assembly.GetExecutingAssembly()
                .GetType(entryPointName, throwOnError: false, ignoreCase: true)
                ?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
            if (entryPoint == null) throw new EntryPointNotFoundException($"{entryPointName}");

            using (var qsim = new QuantumSimulator())
            {
                Task task = entryPoint.Invoke(null, new[] { qsim }) as Task<QVoid>;
                task?.Wait();
            }
        }
    }
}