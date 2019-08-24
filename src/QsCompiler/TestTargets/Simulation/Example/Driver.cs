// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
            using (var qsim = new QuantumSimulator())
            { Simulation.Main.Run(qsim).Wait(); }
        }
    }
}