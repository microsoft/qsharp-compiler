using System;
using System.Runtime.InteropServices;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Values;
using LLVMSharp.Interop;

namespace JIT
{
    class Program
    {
        [DllImport("Microsoft.Quantum.Qir.QSharp.Core", ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern IntPtr CreateFullstateSimulator();

        [DllImport("Microsoft.Quantum.Qir.Runtime", ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern void InitializeQirContext(IntPtr driver);

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct InteropArray
        {
            public long Length;
            public void* Array;
        }

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        delegate long BinaryAddOperation(long op1, long op2);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        unsafe delegate long BinarySumArrayOperation(InteropArray* input);

        [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        delegate void TeleportOperation(char input);

        const string ADD_FUNCTION_NAME = "Microsoft__Quantum__Qir__Emission__Add__body";
        const string SUM_FUNCTION_NAME = "Microsoft__Quantum__Qir__Emission__Sum__Interop";
        const string TELEPORT_FUNCTION_NAME = "Microsoft__Quantum__Qir__Emission__SampleTeleport";

        const string BITCODE_PATH = "C:\\Users\\swern\\Programming\\qsharp-compiler\\examples\\QIR\\Development\\obj\\qsharp\\Development.bc";

        static void Main()
        {
            var qsharpFoundationLibrary = NativeLibrary.Load("Microsoft.Quantum.Qir.QSharp.Foundation", typeof(InteropArray).Assembly, null);
            var qsharpCoreLibrary = NativeLibrary.Load("Microsoft.Quantum.Qir.QSharp.Core", typeof(InteropArray).Assembly, null);

            // To get this line to work, I had to change the CreateFullstateSimulator API to use raw pointers instead of shared pointers,
            // and I had to update both calls to be "extern 'C'" otherwise name mangling makes then impossible to call here.
            // This should be an important consideration as we look at how to update our ABI for compatibility across langauges.
            InitializeQirContext(CreateFullstateSimulator());

            var context = new Context();
            var module = BitcodeModule.LoadFrom(BITCODE_PATH, context);
            if (!module.Verify(out string verifyMessage))
            {
                throw new ExternalException(String.Format("Failed to verify module: {0}", verifyMessage));
            }

            if (!module.TryGetFunction(ADD_FUNCTION_NAME, out IrFunction funcDef))
            {
                throw new ExternalException(String.Format("Failed to find function '{0}'", ADD_FUNCTION_NAME));
            }

            LLVM.InitializeNativeTarget();
            LLVM.InitializeNativeAsmParser();
            LLVM.InitializeNativeAsmPrinter();

            var engine = module.ModuleHandle.CreateMCJITCompiler();
            var function = engine.GetPointerToGlobal<BinaryAddOperation>(funcDef.ValueHandle);
            var result = function(2, 3);

            Console.WriteLine(result);

            if (!module.TryGetFunction(SUM_FUNCTION_NAME, out IrFunction funcDef2))
            {
                throw new ExternalException(string.Format("Failed to find function '{0}'", SUM_FUNCTION_NAME));
            }

            var function2 = engine.GetPointerToGlobal<BinarySumArrayOperation>(funcDef2.ValueHandle);
            var innerArray = new long[]{1, 2, 3, 4, 5};
            unsafe
            {
                fixed (long* rawArray = &innerArray[0])
                {
                    var array = new InteropArray {
                        Length = innerArray.Length,
                        Array = rawArray
                    };
                    result = function2(&array);
                }
            }

            Console.WriteLine(result);

            if (!module.TryGetFunction(TELEPORT_FUNCTION_NAME, out IrFunction funcDef3))
            {
                throw new ExternalException(string.Format("Failed to find function '{0}'", TELEPORT_FUNCTION_NAME));
            }

            var function3 = engine.GetPointerToGlobal<TeleportOperation>(funcDef3.ValueHandle);
            function3((char)1);
        }
    }
}
