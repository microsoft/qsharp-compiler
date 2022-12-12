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
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SimpleFunction();

        // For testing purposes I needed to make this an executable such that we can properly detect when the invoked
        // native code throws a runtime exception; I failed to find out how to catch these exceptions as part of a unit test
        // (usually that should be possible, but it might require a different setup - rather than do this, I opted for
        // making it an out of process call for unit testing purposes).
        private static void Main(string[] args) =>
            BuildAndRun(args[0], args[1..]);

        private static unsafe bool TryParseBitcode(string path, out LLVMModuleRef outModule, out string outMessage)
        {
            LLVMMemoryBufferRef handle;
            sbyte* msg;
            if (LLVM.CreateMemoryBufferWithContentsOfFile(path.AsMarshaledString(), (LLVMOpaqueMemoryBuffer**)&handle, &msg) != 0)
            {
                var span = new ReadOnlySpan<byte>(msg, int.MaxValue);
                var errTxt = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
                LLVM.DisposeMessage(msg);
                throw new InternalCodeGeneratorException(errTxt);
            }

            fixed (LLVMModuleRef* pOutModule = &outModule)
            {
                sbyte* pMessage = null;
                var result = LLVM.ParseBitcodeInContext(
                    LLVM.ContextCreate(),
                    handle,
                    (LLVMOpaqueModule**)pOutModule,
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

        private static unsafe void ExplicitLibraryLoad(string libraryName)
        {
            if (LLVM.LoadLibraryPermanently(libraryName.AsMarshaledString()) != 0)
            {
                throw new ExternalException($"Failed explicit load of library '{libraryName}'");
            }
        }

        public static unsafe void BuildAndRun(string pathToBitcode, params string[] functionNames)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On non-Windows platforms, explicitly load OpenMP library to ensure the bundled
                // version from the simulator package is found first.
                NativeLibrary.Load("omp", typeof(JitCompilation).Assembly, null);
            }

            // Explicitly load dependent libraries so that they are already present in memory.
            NativeLibrary.Load("Microsoft.Quantum.Simulator.Runtime", typeof(JitCompilation).Assembly, null);

            if (!File.Exists(pathToBitcode))
            {
                throw new FileNotFoundException($"Could not find file {pathToBitcode}");
            }

            if (!TryParseBitcode(pathToBitcode, out LLVMModuleRef modRef, out string message))
            {
                throw new InternalCodeGeneratorException(message);
            }

            if (!modRef.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out string verifyMessage))
            {
                throw new ExternalException($"Failed to verify module: {verifyMessage}");
            }

            LLVM.InitializeNativeTarget();
            LLVM.InitializeNativeAsmParser();
            LLVM.InitializeNativeAsmPrinter();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux requires an additional explicit load of the libraries into MCJIT.
                // Full paths are not needed since .NET already loaded these into program memory above,
                // but without this explict load the JIT logic won't find them.
                ExplicitLibraryLoad("libMicrosoft.Quantum.Simulator.Runtime.so");
            }

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
