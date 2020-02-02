using System;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.Demo
{
    class Driver
    {
        static void Main(string[] args)
        {
            using var qsim = new QuantumSimulator();
            SampleProgram.Run(qsim).Wait();
            Console.WriteLine("SampleProgram executed successfully!");
        }
    }
}