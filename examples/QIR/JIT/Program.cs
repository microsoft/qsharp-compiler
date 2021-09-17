using System;
using System.Runtime.InteropServices;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
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

            var rootCommand = GenerateCommands(module);
            var ic = new InvocationContext(rootCommand.Parse(args));
            if (ic.ParseResult.Errors.Count > 0)
            {
                new ParseErrorResult(null).Apply(ic);
                return -1;
            }

            if (!module.TryGetFunction(ic.ParseResult.CommandResult.Command.Name, out IrFunction funcDef))
            {
                throw new ExternalException(String.Format("Failed to find entrypoint function '{0}'", ic.ParseResult.CommandResult.Command.Name));
            }

            LLVM.InitializeNativeTarget();
            LLVM.InitializeNativeAsmPrinter();
            var engine = module.ModuleHandle.CreateMCJITCompiler();

            var function = engine.GetPointerToGlobal(funcDef.ValueHandle);
            var funcTypes = funcDef.Parameters.Select(p => ConvertLlvmType(p.NativeType.ToString())).ToArray();
            var interopTypes = funcDef.Parameters.Select(p => ConvertLlvmType(p.NativeType.ToString(), interop: true)).ToArray();

            if (funcDef.Parameters.Count == 0)
            {
                InvokeUnsafeFunction(function);
            }
            else
            {
                typeof(Program).GetMethods()
                    .Where(m => m.Name == "InvokeUnsafeFunction" && m.IsGenericMethod && m.GetGenericMethodDefinition().GetGenericArguments().Length == funcDef.Parameters.Count)
                    .Single()
                    .MakeGenericMethod(interopTypes)
                    .Invoke(null, ic.ParseResult.CommandResult.Command.Arguments.Select((a, i) => Convert.ChangeType(ic.ParseResult.ValueForArgument(a.Name), interopTypes[i])).Prepend(function).ToArray());
            }

            return 0;
        }

        internal static RootCommand GenerateCommands(BitcodeModule module)
        {
            var rootCommand = new RootCommand { Description = "Program for JIT Execution of QIR", TreatUnmatchedTokensAsErrors = true };

            foreach (var funcDef in module.Functions)
            {
                if (funcDef.Attributes.Any(attrList => attrList.Value.Any(attr => attr.Name == "EntryPoint")))
                {
                    var entryCommand = new Command(funcDef.Name);
                    foreach (var param in funcDef.Parameters)
                    {
                        var argType = ConvertLlvmType(param.NativeType.ToString());
                        var interopType = ConvertLlvmType(param.NativeType.ToString(), interop: true);
                        var argument = new System.CommandLine.Argument($"{param.Name}")
                        {
                            ArgumentType = argType,
                            Description = $"Type: {interopType} ({param.NativeType.ToString()})"
                        };
                        entryCommand.AddArgument(argument);
                    }
                    rootCommand.AddCommand(entryCommand);
                }
            }

            return rootCommand;
        }

        internal static Type ConvertLlvmType(string llvmTypeString, bool interop = false)
        {
            return llvmTypeString switch
            {
                "i64" => typeof(long),
                "i8" => interop ? typeof(bool) : typeof(String),
                "i8*" => interop ? typeof(Int64) : typeof(InteropString),
                _ => typeof(object),//throw new NotImplementedException($"Can't handle type '{llvmTypeString}'"),
            };
        }

        internal unsafe struct InteropString : IConvertible, IDisposable
        {
            public InteropString(string source)
            {
                if (string.IsNullOrEmpty(source))
                {
                    this.Value = Marshal.AllocHGlobal(1);
                    Marshal.WriteByte(this.Value, 0, 0);
                }
                else
                {
                    var valueBytes = Encoding.UTF8.GetBytes(source);
                    var length = valueBytes.Length;
                    this.Value = Marshal.AllocHGlobal(length + 1);
                    Marshal.Copy(valueBytes, 0, this.Value, length);
                    Marshal.WriteByte(this.Value, length, 0);
                }
            }

            public IntPtr Value { get; private set;}

            public void Dispose()
            {
                if (this.Value != default)
                {
                    Marshal.FreeHGlobal((IntPtr)this.Value);
                    this.Value = default;
                }
            }

            public TypeCode GetTypeCode() => TypeCode.Int64;

            public Int64 ToInt64(IFormatProvider f) => (Int64)this.Value;

            public bool ToBoolean(IFormatProvider f) => throw new InvalidCastException();
            public byte ToByte(IFormatProvider f) => throw new InvalidCastException();
            public char ToChar(IFormatProvider f) => throw new InvalidCastException();
            public DateTime ToDateTime(IFormatProvider f) => throw new InvalidCastException();
            public Decimal ToDecimal(IFormatProvider f) => throw new InvalidCastException();
            public double ToDouble(IFormatProvider f) => throw new InvalidCastException();
            public Int16 ToInt16(IFormatProvider f) => throw new InvalidCastException();
            public int ToInt32(IFormatProvider f) => throw new InvalidCastException();
            public sbyte ToSByte(IFormatProvider f) => throw new InvalidCastException();
            public float ToSingle(IFormatProvider f) => throw new InvalidCastException();
            public string ToString(IFormatProvider f) => throw new InvalidCastException();
            public object ToType(Type conversionType, IFormatProvider provider) => throw new NotImplementedException();
            public ushort ToUInt16(IFormatProvider f) => throw new InvalidCastException();
            public uint ToUInt32(IFormatProvider f) => throw new InvalidCastException();
            public ulong ToUInt64(IFormatProvider f) => throw new InvalidCastException();
        }

        public static unsafe void InvokeUnsafeFunction(IntPtr func) => ((delegate* unmanaged<void>)func)();
        public static unsafe void InvokeUnsafeFunction<T1>(IntPtr func, T1 a)
            where T1 : unmanaged
        => ((delegate*<T1, void>)func)(a);
        public static unsafe void InvokeUnsafeFunction<T1, T2>(IntPtr func, T1 a, T2 b)
            where T1 : unmanaged
            where T2 : unmanaged
        => ((delegate*<T1, T2, void>)func)(a, b);
        public static unsafe void InvokeUnsafeFunction<T1, T2, T3>(IntPtr func, T1 a, T2 b, T3 c)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        => ((delegate*<T1, T2, T3, void>)func)(a, b, c);

    }
}
