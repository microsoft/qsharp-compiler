using System;
using System.IO;
using Microsoft.Quantum.QsCompiler.QIR;

namespace EntryPointHandling
{
    class Program
    {
        static void Main(string[] args)
        {
            var bitcode = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, "Development", "obj", "qsharp", "Development.bc");
            if (!File.Exists(bitcode))
            {
                Console.WriteLine($"file {bitcode} not found");
            }
            EntryPointOperationLoader.LoadEntryPointOperations(bitcode);
        }
    }
}
