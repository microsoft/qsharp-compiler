// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.Simulation.Simulators;


namespace Microsoft.Quantum.Testing.Simulation
{
    internal static class Driver
    {
        static void Main(string[] args)
        {
            using (var qsim = new QuantumSimulator())
            { Simulation.Main.Run(qsim).Wait(); }
        }
    }
}