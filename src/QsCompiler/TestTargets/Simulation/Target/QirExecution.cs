// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using LlvmBindings;
using LlvmBindings.Interop;

namespace Microsoft.Quantum.QsCompiler.Testing.Qir
{
    public static class JitCompilation
    {
        [DllImport("Microsoft.Quantum.Qir.QSharp.Core", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateFullstateSimulatorC(long seed);

        [DllImport("Microsoft.Quantum.Qir.Runtime", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitializeQirContext(IntPtr driver, bool trackAllocatedObjects);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SimpleFunction();

        // For testing purposes I needed to make this an executable such that we can properly detect when the invoked
        // native code throws a runtime exception; I failed to find out how to catch these exceptions as part of a unit test
        // (usually that should be possible, but it might require a different setup - rather than do this, I opted for
        // making it an out of process call for unit testing purposes).
        private static void Main(string[] args) =>
            BuildAndRun(args[0], args[1..]);

        private static unsafe bool TryParseBitcode(string path, out LLVMSharp.Interop.LLVMModuleRef outModule, out string outMessage)
        {
            LLVMSharp.Interop.LLVMMemoryBufferRef handle;
            sbyte* msg;
            if (LLVMSharp.Interop.LLVM.CreateMemoryBufferWithContentsOfFile(path.AsMarshaledString(), (LLVMSharp.Interop.LLVMOpaqueMemoryBuffer**)&handle, &msg) != 0)
            {
                var span = new ReadOnlySpan<byte>(msg, int.MaxValue);
                var errTxt = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
                LLVMSharp.Interop.LLVM.DisposeMessage(msg);
                throw new InternalCodeGeneratorException(errTxt);
            }

            fixed (LLVMSharp.Interop.LLVMModuleRef* pOutModule = &outModule)
            {
                sbyte* pMessage = null;
                var result = LLVMSharp.Interop.LLVM.ParseBitcodeInContext(
                    LLVMSharp.Interop.LLVM.ContextCreate(),
                    handle,
                    (LLVMSharp.Interop.LLVMOpaqueModule**)pOutModule,
                    &pMessage);

                if (pMessage == null)
                {
                    outMessage = string.Empty;
                }
                else
                {
                    var span = new ReadOnlySpan<byte>(pMessage, int.MaxValue);
                    outMessage = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
                }

                return result == 0;
            }
        }

        public static unsafe void BuildAndRun(string pathToBitcode, params string[] functionNames)
        {
            NativeLibrary.Load("Microsoft.Quantum.Simulator.Runtime", typeof(JitCompilation).Assembly, null);
            NativeLibrary.Load("Microsoft.Quantum.Qir.Runtime", typeof(JitCompilation).Assembly, null);
            NativeLibrary.Load("Microsoft.Quantum.Qir.QSharp.Foundation", typeof(JitCompilation).Assembly, null);

            // To get this line to work, I had to change the CreateFullstateSimulator API to use raw pointers instead of shared pointers,
            // and I had to update both calls to be "extern 'C'" otherwise name mangling makes then impossible to call here.
            // This should be revised more broadly as we move the runtime to a C-style API for ABI compatibility across langauges.
            InitializeQirContext(CreateFullstateSimulatorC(0), true);

            if (!File.Exists(pathToBitcode))
            {
                throw new FileNotFoundException($"Could not find file {pathToBitcode}");
            }

            if (!TryParseBitcode(pathToBitcode, out LLVMSharp.Interop.LLVMModuleRef modRef, out string message))
            {
                throw new InternalCodeGeneratorException(message);
            }

            if (!modRef.TryVerify(LLVMSharp.Interop.LLVMVerifierFailureAction.LLVMReturnStatusAction, out string verifyMessage))
            {
                throw new ExternalException($"Failed to verify module: {verifyMessage}");
            }

            LLVMSharp.Interop.LLVM.InitializeNativeTarget();
            LLVMSharp.Interop.LLVM.InitializeNativeAsmParser();
            LLVMSharp.Interop.LLVM.InitializeNativeAsmPrinter();

            var engine = modRef.CreateMCJITCompiler();
            foreach (var functionName in functionNames)
            {
                var funcDef = modRef.GetNamedFunction(functionName);
                if (funcDef == default)
                {
                    throw new ExternalException($"Failed to find function '{functionName}'");
                }

                var function = engine.GetPointerToGlobal<SimpleFunction>(funcDef);
                function();
            }
        }
    }
}
