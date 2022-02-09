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
        public static extern IntPtr CreateFullstateSimulatorC(long seed);

        [DllImport("Microsoft.Quantum.Qir.Runtime", ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern void InitializeQirContext(IntPtr driver, bool trackAllocatedObjects);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private delegate void SimpleFunction();

        // For testing purposes I needed to make this an executable such that we can properly detect when the invoked
        // native code throws a runtime exception; I failed to find out how to catch these exceptions as part of a unit test
        // (usually that should be possible, but it might require a different setup - rather than do this, I opted for
        // making it an out of process call for unit testing purposes).
        private static void Main(string[] args) =>
            BuildAndRun(args[0], args[1..]);

        public static void BuildAndRun(string pathToBitcode, params string[] functionNames)
        {
            // To get this line to work, I had to change the CreateFullstateSimulator API to use raw pointers instead of shared pointers,
            // and I had to update both calls to be "extern 'C'" otherwise name mangling makes then impossible to call here.
            // This should be revised more broadly as we move the runtime to a C-style API for ABI compatibility across langauges.
            InitializeQirContext(CreateFullstateSimulatorC(0), true);

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
        }
    }
}
