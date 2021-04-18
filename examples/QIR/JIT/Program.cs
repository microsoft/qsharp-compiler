using System;
using System.Runtime.InteropServices;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Values;
using LLVMSharp.Interop;

namespace JIT
{
    class Program
    {
        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        delegate Int64 BinaryInt64Operation(Int64 op1, Int64 op2);

        const string FUNCTION_NAME = "Microsoft__Quantum__Qir__Emission__Add__body";
        const string BITCODE_PATH = "../Development/obj/qsharp/Development.bc";

        static void Main(string[] args)
        {
            var context = new Context();
            var module = BitcodeModule.LoadFrom(BITCODE_PATH, context);
            string verifyMessage;
            if (!module.Verify(out verifyMessage))
            {
                throw new ExternalException(String.Format("Failed to verify module: {0}", verifyMessage));
            }

            IrFunction funcDef;
            if (!module.TryGetFunction(FUNCTION_NAME, out funcDef))
            {
                throw new ExternalException(String.Format("Failed to find function '{0}'", FUNCTION_NAME));
            }

            LLVM.InitializeNativeTarget();
            LLVM.InitializeNativeAsmParser();
            LLVM.InitializeNativeAsmPrinter();

            var engine = module.ModuleHandle.CreateMCJITCompiler();
            var function = engine.GetPointerToGlobal<BinaryInt64Operation>(funcDef.ValueHandle);
            var result = function(2, 3);

            System.Console.WriteLine(result);
        }
    }
}
