// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using LLVMSharp.Interop;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.Testing.Qir
{
    public static class JitCompilation
    {
        [DllImport("Microsoft.Quantum.Qir.QSharp.Core", ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern IntPtr CreateFullstateSimulator(long seed);

        [DllImport("Microsoft.Quantum.Qir.Runtime", ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern void InitializeQirContext(IntPtr driver);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private delegate long SimpleFunction();

        public static void BuildAndRun(string pathToBitcode, params string[] functionNames)
        {
            var output = new StreamWriter(Path.GetFullPath("output.txt"));
            Console.SetOut(output);

            // To get this line to work, I had to change the CreateFullstateSimulator API to use raw pointers instead of shared pointers,
            // and I had to update both calls to be "extern 'C'" otherwise name mangling makes then impossible to call here.
            // This should be an important consideration as we look at how to update our ABI for compatibility across langauges.
            InitializeQirContext(CreateFullstateSimulator(0));

            if (!File.Exists(pathToBitcode))
            {
                throw new FileNotFoundException($"Could not find file {pathToBitcode}");
            }

            var context = new Context();
            var module = BitcodeModule.LoadFrom(pathToBitcode, context);
            if (!module.Verify(out string verifyMessage))
            {
                throw new ExternalException($"Failed to verify module: {verifyMessage}");
            }

            LLVM.InitializeNativeTarget();
            LLVM.InitializeNativeAsmParser();
            LLVM.InitializeNativeAsmPrinter();

            var engine = module.ModuleHandle.CreateMCJITCompiler();
            foreach (var functionName in functionNames)
            {
                if (!module.TryGetFunction(functionName, out IrFunction? funcDef))
                {
                    throw new ExternalException($"Failed to find function '{functionName}'");
                }

                var function = engine.GetPointerToGlobal<SimpleFunction>(funcDef.ValueHandle);
                function();
            }

            Console.WriteLine("done");
        }
    }
}
