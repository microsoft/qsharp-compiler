using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Quantum.QsCompiler;


namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    public static class CompilationTracker
    {
        public static void OnCompilationEvent(object sender, CompilationLoader.CompilationProcessEventArgs args)
        {
            Console.WriteLine(">> " + args.Name + " " + args.Type);
        }
    }
}
