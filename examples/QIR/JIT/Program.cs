using System;
using System.Runtime.InteropServices;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Values;
using System.IO;
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

        // const string ADD_FUNCTION_NAME = "Microsoft__Quantum__Qir__Emission__Add__body";
        // const string SUM_FUNCTION_NAME = "Microsoft__Quantum__Qir__Emission__Sum__Interop";
        // const string TELEPORT_FUNCTION_NAME = "Microsoft__Quantum__Qir__Emission__SampleTeleport";

        const string BITCODE_PATH = "C:\\Users\\swern\\Programming\\qsharp-compiler\\examples\\QIR\\Development\\obj\\qsharp\\Development.bc";

        static int Main(string[] args)
        {
            var fullstateSim = NativeLibrary.Load("Microsoft.Quantum.Simulator.Runtime", typeof(InteropArray).Assembly, null);
            var qsharpFoundationLibrary = NativeLibrary.Load("Microsoft.Quantum.Qir.QSharp.Foundation", typeof(InteropArray).Assembly, null);
            var qsharpCoreLibrary = NativeLibrary.Load("Microsoft.Quantum.Qir.QSharp.Core", typeof(InteropArray).Assembly, null);

            // To get this line to work, I had to change the CreateFullstateSimulator API to use raw pointers instead of shared pointers,
            // and I had to update both calls to be "extern 'C'" otherwise name mangling makes then impossible to call here.
            // This should be an important consideration as we look at how to update our ABI for compatibility across langauges.
            InitializeQirContext(CreateFullstateSimulator());

            if (!File.Exists(BITCODE_PATH))
            {
                throw new FileNotFoundException($"Could not find file {BITCODE_PATH}");
            }

            var context = new Context();
            var module = BitcodeModule.LoadFrom(BITCODE_PATH, context);
            if (!module.Verify(out string verifyMessage))
            {
                throw new ExternalException(String.Format("Failed to verify module: {0}", verifyMessage));
            }

            if (args.Length == 0)
            {
                throw new ArgumentException("First argument must be QIR Entry Point Function name!");
            }

            if (!module.TryGetFunction(args[0], out IrFunction funcDef))
            {
                throw new ExternalException(String.Format("Failed to find entrypoint function '{0}'", args[0]));
            }

            var rootCommand = new RootCommand { Description = "Program for JIT Execution of QIR", TreatUnmatchedTokensAsErrors = true };
            var options = new List<Option>();
            foreach (var param in funcDef.Parameters)
            {
                var option = new Option($"--{param.Name}", ConvertLlvmType(param.NativeType.ToString()).Name, ConvertLlvmType(param.NativeType.ToString()))
                {
                    IsRequired = true,
                };
                rootCommand.AddOption(option);
                options = options.Append(option).ToList();
            }

            var ic = new InvocationContext(rootCommand.Parse(args.Skip(1).ToArray()));
            if (ic.ParseResult.Errors.Count > 0)
            {
                new ParseErrorResult(null).Apply(ic);
                return -1;
            }

            LLVM.InitializeNativeTarget();
            LLVM.InitializeNativeAsmPrinter();
            var engine = module.ModuleHandle.CreateMCJITCompiler();

            var function = engine.GetPointerToGlobal(funcDef.ValueHandle);
            var funcTypes = funcDef.Parameters.Select(p => ConvertLlvmType(p.NativeType.ToString())).ToArray();

            if (funcDef.Parameters.Count == 0)
            {
                InvokeUnsafeFunction(function);
            }
            else
            {
                typeof(Program).GetMethods()
                    .Where(m => m.Name == "InvokeUnsafeFunction" && m.IsGenericMethod && m.GetGenericMethodDefinition().GetGenericArguments().Length == funcDef.Parameters.Count)
                    .Single()
                    .MakeGenericMethod(funcTypes)
                    .Invoke(null, options.Select(o => ic.ParseResult.ValueForOption(o)).Prepend(function).ToArray());
            }

            return 0;
            // if (funcDef.Parameters.Count == 0)
            // {
            //     rootCommand.Handler = CommandHandler.Create(GetUnsafeAction(function));
            // }
            // else
            // {
            //     var lambda = typeof(Program).GetMethods()
            //         .Where(m => m.Name == "GetUnsafeAction" && m.IsGenericMethod && m.GetGenericMethodDefinition().GetGenericArguments().Length == funcDef.Parameters.Count)
            //         .Single()
            //         .MakeGenericMethod(funcTypes)
            //         .Invoke(null, new object[] { function });

            //     rootCommand.Handler = typeof(CommandHandler).GetMethods()
            //         .Where(m => m.Name == "Create" && m.IsGenericMethod && m.GetGenericMethodDefinition().GetGenericArguments().Length == funcDef.Parameters.Count && m.GetParameters().Length == 1 && m.GetParameters().Single().ParameterType.Name == $"Action`{funcDef.Parameters.Count}")
            //         .First()
            //         .MakeGenericMethod(funcTypes)
            //         .Invoke(null, new object[] { lambda }) as ICommandHandler;
            // }

            // return rootCommand.Invoke(args.Skip(1).ToArray());

            // if (!module.TryGetFunction(ADD_FUNCTION_NAME, out IrFunction funcDef))
            // {
            //     throw new ExternalException(String.Format("Failed to find function '{0}'", ADD_FUNCTION_NAME));
            // }

            // LLVM.InitializeNativeTarget();
            // LLVM.InitializeNativeAsmParser();
            // LLVM.InitializeNativeAsmPrinter();

            // var engine = module.ModuleHandle.CreateMCJITCompiler();
            // var function = engine.GetPointerToGlobal<BinaryAddOperation>(funcDef.ValueHandle);
            // var result = function(2, 3);

            // Console.WriteLine(result);

            // if (!module.TryGetFunction(SUM_FUNCTION_NAME, out IrFunction funcDef2))
            // {
            //     throw new ExternalException(string.Format("Failed to find function '{0}'", SUM_FUNCTION_NAME));
            // }

            // var function2 = engine.GetPointerToGlobal<BinarySumArrayOperation>(funcDef2.ValueHandle);
            // var innerArray = new long[]{1, 2, 3, 4, 5};
            // unsafe
            // {
            //     fixed (long* rawArray = &innerArray[0])
            //     {
            //         var array = new InteropArray {
            //             Length = innerArray.Length,
            //             Array = rawArray
            //         };
            //         result = function2(&array);
            //     }
            // }

            // Console.WriteLine(result);

            // if (!module.TryGetFunction(TELEPORT_FUNCTION_NAME, out IrFunction funcDef3))
            // {
            //     throw new ExternalException(string.Format("Failed to find function '{0}'", TELEPORT_FUNCTION_NAME));
            // }

            // var function3 = engine.GetPointerToGlobal<TeleportOperation>(funcDef3.ValueHandle);
            // function3((char)1);
        }

        internal static Type ConvertLlvmType(string llvmTypeString, bool interop = false)
        {
            return llvmTypeString switch
            {
                "i64" => typeof(long),
                "i8" => typeof(bool),
                _ => throw new NotImplementedException($"Can't handle type '{llvmTypeString}'"),
            };
        }

        public static unsafe void InvokeUnsafeFunction(IntPtr func) => ((delegate* unmanaged<void>)func)();
        public static unsafe void InvokeUnsafeFunction<T1>(IntPtr func, T1 a)
            where T1 : unmanaged
        => ((delegate*<T1, void>)func)(a);
        public static unsafe void InvokeUnsafeFunction<T1, T2>(IntPtr func, T1 a, T2 b)
            where T1 : unmanaged
            where T2 : unmanaged
        => ((delegate*<T1, T2, void>)func)(a, b);

    }
}
