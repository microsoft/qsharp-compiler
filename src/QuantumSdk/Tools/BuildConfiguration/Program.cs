// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CommandLine;


namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    public class Options {

        [Option('i', "input", Required = true, 
        HelpText = "Q# code or name of the Q# file to compile.")]
        public IEnumerable<string> Input { get; set; }

    }


    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
