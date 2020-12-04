using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Interop;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    using QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using ResolvedExpression = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;

    public class Configuration
    {
        private static readonly ImmutableDictionary<string, string> clangInteropTypeMapping =
            ImmutableDictionary.CreateRange(new Dictionary<string, string>
            {
                ["Result"] = "class.RESULT",
                ["Array"] = "struct.quantum::Array",
                ["Callable"] = "struct.quantum::Callable",
                ["TuplePointer"] = "struct.quantum::TupleHeader",
                ["Qubit"] = "class.QUBIT"
            });

        internal readonly ImmutableDictionary<string, string> InteropTypeMapping;

        internal readonly bool GenerateInteropWrappers;

        public readonly string OutputFileName;

        public Configuration(string outputFileName, bool generateInteropWrappers = false, Dictionary<string, string>? interopTypeMapping = null)
        {
            this.GenerateInteropWrappers = generateInteropWrappers;
            this.InteropTypeMapping = interopTypeMapping != null
                ? interopTypeMapping.ToImmutableDictionary()
                : clangInteropTypeMapping;
            this.OutputFileName = outputFileName;
        }
    }

    /// <summary>
    /// This class holds the shared state used across a QIR generation pass.
    /// It also holds a large number of shared utility routines.
    /// </summary>
    public class GenerationContext : IDisposable
    {
        private static readonly ILibLlvm libContext = Library.InitializeLLVM();

        #region Member variables

        public readonly Configuration Config;
        public readonly QsCompilation Compilation;
        internal QirTransformation? _Transformation { private get; set; }
        public QirTransformation Transformation =>
            this._Transformation ?? throw new InvalidOperationException("no transformation defined");

        // Current state
        public readonly Context Context;
        public readonly BitcodeModule Module;
        private readonly BitcodeModule BridgeModule;
        protected internal IrFunction? CurrentFunction { get; set; }
        protected internal BasicBlock? CurrentBlock { get; set; }
        protected internal InstructionBuilder CurrentBuilder { get; set; }
        protected internal readonly Stack<Value> ValueStack;
        protected internal readonly Stack<ResolvedType> ExpressionTypeStack;
        internal readonly ScopeManager ScopeMgr;

        // LLVM type for passing up layers
        internal ITypeRef? BuiltType { get; set;}

        // Current inlining level
        private int inlineLevel = 0;
        public int CurrentInlineLevel => this.inlineLevel;

        // QIR types
        public readonly ITypeRef QirInt;
        public readonly ITypeRef QirDouble;
        public readonly ITypeRef QirBool;
        public readonly ITypeRef QirResult;
        public readonly ITypeRef QirPauli;
        public readonly ITypeRef QirQubit;
        public readonly ITypeRef QirRange;
        public readonly ITypeRef QirString;
        public readonly ITypeRef QirBigInt;
        public readonly ITypeRef QirArray;
        public readonly ITypeRef QirCallable;
        private readonly ITypeRef qirResultStruct;

        // QIR constants
        public readonly Value QirResultZero;
        public readonly Value QirResultOne;
        public readonly Value QirPauliI;
        public readonly Value QirPauliX;
        public readonly Value QirPauliY;
        public readonly Value QirPauliZ;
        public readonly Value QirEmptyRange;

        // Internal types
        public readonly ITypeRef QirTupleHeader;
        public readonly ITypeRef QirTuplePointer;
        public readonly IFunctionType StandardWrapperSignature;

        // Various internal bits of data
        private readonly Stack<Dictionary<string, (Value, bool)>> namesInScope = new Stack<Dictionary<string, (Value, bool)>>();
        private readonly Dictionary<string, int> uniqueNameIds = new Dictionary<string, int>();
        private readonly Dictionary<string, (QsCallable, GlobalVariable)> wrapperQueue = new Dictionary<string, (QsCallable, GlobalVariable)>();
        private readonly FunctionLibrary runtimeLibrary;
        private readonly FunctionLibrary quantumLibrary;
        private readonly Dictionary<string, ITypeRef> interopType = new Dictionary<string, ITypeRef>();

        /// <summary>
        /// Constructs a new generation context.
        /// </summary>
        /// <param name="comp">The current compilation</param>
        /// <param name="outputFile">The base path of the QIR file to write, with no extension</param>
        internal GenerationContext(QsCompilation comp, Configuration config, QirTransformation? transformation = null)
        {
            libContext.RegisterTarget(CodeGenTarget.Native);

            this.Compilation = comp;
            this.Config = config;
            this._Transformation = transformation;

            this.Context = new Context();
            this.CurrentBuilder = new InstructionBuilder(this.Context);
            this.Module = this.Context.CreateBitcodeModule();
            this.BridgeModule = this.Context.CreateBitcodeModule("bridge");
            this.ValueStack = new Stack<Value>();
            this.ExpressionTypeStack = new Stack<ResolvedType>();
            this.ScopeMgr = new ScopeManager(this);

            #region Standard types

            this.QirInt = this.Context.Int64Type;
            this.QirDouble = this.Context.DoubleType;
            this.QirBool = this.Context.BoolType;
            this.qirResultStruct = this.Context.CreateStructType("Result");
            this.QirResult = this.qirResultStruct.CreatePointerType();
            //this.QirPauli = this.CurrentContext.CreateStructType("Pauli", false, this.CurrentContext.Int8Type);
            this.QirPauli = this.Context.GetIntType(2);
            var qirQubitStruct = this.Context.CreateStructType("Qubit");
            this.QirQubit = qirQubitStruct.CreatePointerType();
            this.QirRange = this.Context.CreateStructType("Range", false, this.Context.Int64Type,
                this.Context.Int64Type, this.Context.Int64Type);
            var qirStringStruct = this.Context.CreateStructType("String");
            this.QirString = qirStringStruct.CreatePointerType();
            var qirBigIntStruct = this.Context.CreateStructType("BigInt");
            this.QirBigInt = qirBigIntStruct.CreatePointerType();
            // It would be nice if TupleHeader were opaque, but it can't be because it appears directly
            // (that is, not as a pointer) in tuple structures, but would have unknown length if it were opaque.
            this.QirTupleHeader = this.Context.CreateStructType("TupleHeader", false,
                this.Context.Int32Type);
            this.QirTuplePointer = this.QirTupleHeader.CreatePointerType();
            var qirArrayStruct = this.Context.CreateStructType("Array");
            this.QirArray = qirArrayStruct.CreatePointerType();
            var qirCallableStruct = this.Context.CreateStructType("Callable");
            this.QirCallable = qirCallableStruct.CreatePointerType();

            #endregion

            this.StandardWrapperSignature = this.Context.GetFunctionType(this.Context.VoidType,
                    new[] { this.QirTuplePointer, this.QirTuplePointer, this.QirTuplePointer });
            this.runtimeLibrary = new FunctionLibrary(this.Module,
                s => SpecialFunctionName(SpecialFunctionKind.Runtime, s));
            this.quantumLibrary = new FunctionLibrary(this.Module,
                s => SpecialFunctionName(SpecialFunctionKind.QuantumInstruction, s));

            #region Constants

            this.QirResultZero = this.Module.AddGlobal(this.QirResult, "ResultZero");
            this.QirResultOne = this.Module.AddGlobal(this.QirResult, "ResultOne");
            this.QirPauliI = this.Module.AddGlobal(this.QirPauli, true, Linkage.External,
                this.Context.CreateConstant(this.QirPauli, 0, false), "PauliI");
            this.QirPauliX = this.Module.AddGlobal(this.QirPauli, true, Linkage.External,
                this.Context.CreateConstant(this.QirPauli, 1, false), "PauliX");
            this.QirPauliY = this.Module.AddGlobal(this.QirPauli, true, Linkage.External,
                this.Context.CreateConstant(this.QirPauli, 3, false), "PauliY");
            this.QirPauliZ = this.Module.AddGlobal(this.QirPauli, true, Linkage.External,
                this.Context.CreateConstant(this.QirPauli, 2, false), "PauliZ");
            this.QirEmptyRange = this.Module.AddGlobal(this.QirRange, true, Linkage.Internal,
                this.Context.CreateNamedConstantStruct((IStructType)this.QirRange,
                    this.Context.CreateConstant(0L),
                    this.Context.CreateConstant(1L),
                    this.Context.CreateConstant(-1L)),
                "EmptyRange");

            #endregion
        }

        #endregion

        #region Static properties and methods

        /// <summary>
        /// Order of specializations in the wrapper array
        /// </summary>
        public static readonly ImmutableArray<QsSpecializationKind> FunctionArray = ImmutableArray.Create(
            QsSpecializationKind.QsBody,
            QsSpecializationKind.QsAdjoint,
            QsSpecializationKind.QsControlled,
            QsSpecializationKind.QsControlledAdjoint
        );

        /// <summary>
        /// Cleans a namespace name by replacing periods with double underscores.
        /// </summary>
        /// <param name="namespaceName">The namespace name to clean</param>
        /// <returns>The cleaned name</returns>
        static public string CleanNamespaceName(string namespaceName) => namespaceName.Replace(".", "__");

        /// <summary>
        /// Generates a mangled name for a callable specialization.
        /// QIR mangled names are the namespace name, with periods replaced by double underscores, followed
        /// by a double underscore and the callable name, then another double underscore and the name of the
        /// callable kind ("body", "adj", "ctl", or "ctladj").
        /// </summary>
        /// <param name="namespaceName">The callable's namespace name</param>
        /// <param name="name">The callable's name</param>
        /// <param name="kind">The specialization kind</param>
        /// <returns>The mangled name for the specialization</returns>
        static public string CallableName(string namespaceName, string name, QsSpecializationKind kind)
        {
            var suffix = kind.IsQsBody ? "body" :
                (kind.IsQsAdjoint ? "adj" :
                    (kind.IsQsControlled ? "ctrl" :
                        (kind.IsQsControlledAdjoint ? "ctrladj" : "**ERROR**")));
            return $"{CleanNamespaceName(namespaceName)}__{name}__{suffix}";
        }

        /// <summary>
        /// Generates a mangled name for a callable specialization.
        /// QIR mangled names are the namespace name, with periods replaced by double underscores, followed
        /// by a double underscore and the callable name, then another double underscore and the name of the
        /// callable kind ("body", "adj", "ctl", or "ctladj").
        /// </summary>
        /// <param name="callable">The Q# callable</param>
        /// <param name="kind">The specialization kind</param>
        /// <returns>The mangled name for the specialization</returns>
        static public string CallableName(QsCallable callable, QsSpecializationKind kind) => 
            CallableName(callable.FullName.Namespace, callable.FullName.Name, kind);

        /// <summary>
        /// Generates a mangled name for a callable specialization wrapper.
        /// Wrapper names are the mangled specialization name followed by double underscore and "wrapper".
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="name"></param>
        /// <param name="kind"></param>
        /// <returns>The mangled name for the wrapper</returns>
        static public string CallableWrapperName(string namespaceName, string name, QsSpecializationKind kind) =>
            $"{CallableName(namespaceName, name, kind)}__wrapper";

        /// <summary>
        /// Generates a mangled name for a callable specialization wrapper.
        /// Wrapper names are the mangled specialization name followed by double underscore and "wrapper".
        /// </summary>
        /// <param name="callable">The Q# callable</param>
        /// <param name="kind">The specialization kind</param>
        /// <returns>The mangled name for the wrapper</returns>
        static public string CallableWrapperName(QsCallable callable, QsSpecializationKind kind) =>
            CallableWrapperName(callable.FullName.Namespace, callable.FullName.Name, kind);

        private enum SpecialFunctionKind
        {
            Runtime,
            QuantumInstruction
        }

        /// <summary>
        /// Generates a mangled name for a special function.
        /// Special functions are either part of the QIR runtime library or part of the target-specified
        /// quantum instruction set.
        /// The mangled names are a double underscore, "quantum", and another double underscore, followed by
        /// "rt" or "qis", another double underscore, and then the base name.
        /// </summary>
        /// <param name="kind">The kind of special function</param>
        /// <param name="name">The name of the special function</param>
        /// <returns>The mangled function name</returns>
        static private string SpecialFunctionName(SpecialFunctionKind kind, string name)
        {
            return kind switch
            {
                SpecialFunctionKind.Runtime => $"__quantum__rt__{name}",
                SpecialFunctionKind.QuantumInstruction => $"__quantum__qis__{name}",
                _ => "**ERROR**",
            };
        }
        #endregion

        #region Module initialization and emission

        /// <summary>
        /// Initializes the QIR runtime library.
        /// See <see cref="FunctionLibrary"/>.
        /// </summary>
        public void InitializeRuntimeLibrary()
        {
            #region int library functions
            this.runtimeLibrary.AddFunction("int_power", this.QirInt, this.QirInt, this.QirInt);
            #endregion
            #region Standard result library functions
            //this.runtimeLibrary.AddFunction("result_create", this.QirResult);
            //this.runtimeLibrary.AddFunction("result_copy", this.QirResult, this.QirResult);
            this.runtimeLibrary.AddFunction("result_reference", this.Context.VoidType, this.QirResult);
            this.runtimeLibrary.AddFunction("result_unreference", this.Context.VoidType, this.QirResult);
            this.runtimeLibrary.AddFunction("result_equal", this.Context.BoolType, this.QirResult, this.QirResult);
            #endregion
            #region Standard string library functions
            this.runtimeLibrary.AddFunction("string_create", this.QirString, this.Context.Int32Type,
                this.Context.Int8Type.CreateArrayType(0));
            this.runtimeLibrary.AddFunction("string_reference", this.Context.VoidType, this.QirString);
            this.runtimeLibrary.AddFunction("string_unreference", this.Context.VoidType, this.QirString);
            this.runtimeLibrary.AddFunction("string_concatenate", this.QirString, this.QirString, this.QirString);
            this.runtimeLibrary.AddFunction("string_equal", this.Context.BoolType, this.QirString, this.QirString);
            #endregion
            #region To-string library functions
            this.runtimeLibrary.AddFunction("bigint_to_string", this.QirString, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bool_to_string", this.QirString, this.Context.BoolType);
            this.runtimeLibrary.AddFunction("double_to_string", this.QirString, this.Context.DoubleType);
            this.runtimeLibrary.AddFunction("int_to_string", this.QirString, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction("pauli_to_string", this.QirString, this.QirPauli);
            this.runtimeLibrary.AddFunction("qubit_to_string", this.QirString, this.QirQubit);
            this.runtimeLibrary.AddFunction("range_to_string", this.QirString, this.QirRange);
            this.runtimeLibrary.AddFunction("result_to_string", this.QirString, this.QirResult);
            #endregion
            #region Standard bigint library functions
            this.runtimeLibrary.AddFunction("bigint_create_i64", this.QirBigInt, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction("bigint_create_array", this.QirBigInt, this.Context.Int32Type,
                this.Context.Int8Type.CreateArrayType(0));
            this.runtimeLibrary.AddFunction("bigint_reference", this.Context.VoidType, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_unreference", this.Context.VoidType, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_negate", this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_add", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_subtract", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_multiply", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_divide", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_modulus", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_power", this.QirBigInt, this.QirBigInt,
                this.Context.Int32Type);
            this.runtimeLibrary.AddFunction("bigint_bitand", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_bitor", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_bitxor", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_bitnot", this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_shiftleft", this.QirBigInt, this.QirBigInt,
                this.Context.Int64Type);
            this.runtimeLibrary.AddFunction("bigint_shiftright", this.QirBigInt, this.QirBigInt,
                this.Context.Int64Type);
            this.runtimeLibrary.AddFunction("bigint_equal", this.Context.BoolType, this.QirBigInt,
                this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_greater", this.Context.BoolType, this.QirBigInt,
                this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_greater_eq", this.Context.BoolType, this.QirBigInt,
                this.QirBigInt);
            #endregion
            #region Standard tuple library functions
            this.runtimeLibrary.AddFunction("tuple_init_stack", this.Context.VoidType, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("tuple_init_heap", this.Context.VoidType, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("tuple_create", this.QirTuplePointer, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction("tuple_reference", this.Context.VoidType, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("tuple_unreference", this.Context.VoidType, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("tuple_is_writable", this.Context.BoolType, this.QirTuplePointer);
            #endregion
            #region Standard array library functions
            this.runtimeLibrary.AddVarargsFunction("array_create", this.QirArray, this.Context.Int32Type, 
                this.Context.Int32Type);
            this.runtimeLibrary.AddVarargsFunction("array_get_element_ptr", 
                this.Context.Int8Type.CreatePointerType(), this.QirArray);
            // TODO: figure out how to call a varargs function and get rid of these two functions
            this.runtimeLibrary.AddFunction("array_create_1d", this.QirArray, this.Context.Int32Type, 
                this.Context.Int64Type);
            this.runtimeLibrary.AddFunction("array_get_element_ptr_1d", this.Context.Int8Type.CreatePointerType(),
                this.QirArray, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction("array_get_length", this.Context.Int64Type, this.QirArray,
                this.Context.Int32Type);
            this.runtimeLibrary.AddFunction("array_reference", this.Context.VoidType, this.QirArray);
            this.runtimeLibrary.AddFunction("array_unreference", this.Context.VoidType, this.QirArray);
            this.runtimeLibrary.AddFunction("array_copy", this.QirArray, this.QirArray);
            this.runtimeLibrary.AddFunction("array_concatenate", this.QirArray, this.QirArray, this.QirArray);
            this.runtimeLibrary.AddFunction("array_slice", this.QirArray, this.QirArray, 
                this.Context.Int32Type, this.QirRange);
            #endregion
            #region Callable library functions
            this.runtimeLibrary.AddFunction("callable_create", this.QirCallable, 
                this.StandardWrapperSignature.CreatePointerType().CreateArrayType(4).CreatePointerType(),
                this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("callable_invoke", this.Context.VoidType, this.QirCallable,
                this.QirTuplePointer, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("callable_copy", this.QirCallable, this.QirCallable);
            this.runtimeLibrary.AddFunction("callable_make_adjoint", this.Context.VoidType, this.QirCallable);
            this.runtimeLibrary.AddFunction("callable_make_controlled", this.Context.VoidType, this.QirCallable);
            this.runtimeLibrary.AddFunction("callable_reference", this.Context.VoidType, this.QirCallable);
            this.runtimeLibrary.AddFunction("callable_unreference", this.Context.VoidType, this.QirCallable);
            #endregion
            #region Standard qubit library functions
            this.runtimeLibrary.AddFunction("qubit_allocate", this.QirQubit);
            this.runtimeLibrary.AddFunction("qubit_allocate_array", this.QirArray, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction("qubit_release", this.Context.VoidType, this.QirQubit);
            this.runtimeLibrary.AddFunction("qubit_release_array", this.Context.VoidType, this.QirArray);
            #endregion
            #region Other library functions
            this.runtimeLibrary.AddFunction("fail", this.Context.VoidType, this.QirString);
            this.runtimeLibrary.AddFunction("message", this.Context.VoidType, this.QirString);
            #endregion
        }

        /// <summary>
        /// Finds and registers all of the quantum instructions in the current compilation.
        /// For this purpose, quantum instructions are any operations that have the "Intrinsic" attribute.
        /// <br/><br/>
        /// In addition, interop-compatible wrappers are generated for all of the quantum operations.
        /// </summary>
        public void RegisterQuantumInstructions()
        {
            foreach (var ns in this.Compilation.Namespaces)
            {
                foreach (var element in ns.Elements)
                {
                    if (element is QsNamespaceElement.QsCallable c
                        && SymbolResolution.TryGetQISCode(c.Item.Attributes) is var att && att.IsValue)
                    {
                        var name = att.Item;
                        // Special handling for Unit since by default it turns into an empty tuple
                        var returnType = c.Item.Signature.ReturnType.Resolution.IsUnitType
                            ? this.Context.VoidType
                            : this.LlvmTypeFromQsharpType(c.Item.Signature.ReturnType);
                        var argTypeArray = (c.Item.Signature.ArgumentType.Resolution is QsResolvedTypeKind.TupleType tuple)
                            ? tuple.Item.Select(this.LlvmTypeFromQsharpType).ToArray()
                            : new ITypeRef[] { this.LlvmTypeFromQsharpType(c.Item.Signature.ArgumentType) };
                        this.quantumLibrary.AddFunction(name, returnType, argTypeArray);
                        if (this.Config.GenerateInteropWrappers)
                        {
                            var func = this.quantumLibrary.GetFunction(name);
                            this.GenerateInteropWrapper(func, name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the LLVM object for a runtime library function.
        /// If this is the first reference to the function, its declaration is added to the module.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <returns>The LLVM function object</returns>
        public IrFunction GetRuntimeFunction(string name) => this.runtimeLibrary.GetFunction(name);

        /// <summary>
        /// Gets the LLVM object for a quantum instruction set function.
        /// If this is the first reference to the function, its declaration is added to the module.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <returns>The LLVM function object</returns>
        public IrFunction GetQuantumFunction(string name) => this.quantumLibrary.GetFunction(name);

        /// <summary>
        /// Writes the current content to the output file.
        /// </summary>
        public void Emit()
        {
            this.GenerateQueuedWrappers();

            //var outFileName = Path.ChangeExtension(this.CurrentModule.SourceFileName, "ll");
            if (this.Module.Verify(out string validationErrors))
            {
                File.WriteAllText($"{this.Config.OutputFileName}.log", "No errors\n");
            }
            else
            {
                File.WriteAllText($"{this.Config.OutputFileName}.log", $"LLVM errors:\n{validationErrors}");
            }

            if (!this.Module.WriteToTextFile($"{this.Config.OutputFileName}.ll", out string errorMessage))
            {
                throw new IOException(errorMessage);
            }

            // Generate the wrappers for the runtime library that were used, if requested
            if (this.Config.GenerateInteropWrappers)
            {
                foreach (var kvp in this.runtimeLibrary)
                {
                    this.GenerateInteropWrapper(kvp.Value, kvp.Key);
                }

                var bridgeFileName = Path.Combine(Path.GetDirectoryName(this.Config.OutputFileName), "bridge.ll");

                if (!this.BridgeModule.Verify(out string bridgeValidationErrors))
                {
                    File.WriteAllText(bridgeFileName, $"LLVM errors:\n{bridgeValidationErrors}");
                }
                else if (!this.BridgeModule.WriteToTextFile(bridgeFileName, out string bridgeError))
                {
                    throw new IOException(bridgeError);
                }
            }
        }

        #endregion

        #region Specialization management

        /// <summary>
        /// Preps the shared state for a new specialization.
        /// </summary>
        public void StartSpecialization()
        {
            //this.QubitReleaseStack.Clear();
            this.ScopeMgr.Reset();
            this.namesInScope.Clear();
            this.inlineLevel = 0;
            this.uniqueNameIds.Clear();
        }

        /// <summary>
        /// Generates the declaration for a specialization to the current module.
        /// Usually <see cref="GenerateFunctionHeader"/> is used, which generates the start of the actual definition.
        /// This method is primarily useful for specializations with external or intrinsic implementations, which get
        /// generated as declarations with no definition.
        /// </summary>
        /// <param name="spec">The specialization</param>
        /// <param name="argTuple">The specialization's argument tuple</param>
        /// <returns></returns>
        public IrFunction RegisterFunction(QsSpecialization spec, QsArgumentTuple argTuple)
        {
            // TODO: this won't work for parameter lists with embedded tuples (as opposed to arguments
            // of tuple type, which should be fine).

            IEnumerable<ITypeRef> ArgTupleToTypes(QsArgumentTuple arg)
            {
                if (arg is QsArgumentTuple.QsTuple tuple)
                {
                    return tuple.Item.Select(this.BuildArgItemTupleType).ToArray();
                }
                else
                {
                    var typeRef = this.BuildArgItemTupleType(arg);
                    return new ITypeRef[] { typeRef };
                }
            }

            var name = CallableName(spec.Parent.Namespace, spec.Parent.Name, spec.Kind);
            var returnTypeRef = spec.Signature.ReturnType.Resolution.IsUnitType
                ? this.Context.VoidType
                : this.LlvmTypeFromQsharpType(spec.Signature.ReturnType);
            var argTypeRefs = ArgTupleToTypes(argTuple);
            var signature = this.Context.GetFunctionType(returnTypeRef, argTypeRefs);
            return this.Module.CreateFunction(name, signature);
        }

        /// <summary>
        /// Generates the start of the definition for a specialization in the current module.
        /// Specifically, an entry block for the function is created, and the function's arguments are given names.
        /// </summary>
        /// <param name="spec">The specialization</param>
        /// <param name="argTuple">The specialization's argument tuple</param>
        public void GenerateFunctionHeader(QsSpecialization spec, QsArgumentTuple argTuple)
        {
            IEnumerable<string> ArgTupleToNames(QsArgumentTuple arg)
            {
                string LocalVarName(QsArgumentTuple v) => 
                    v is QsArgumentTuple.QsTupleItem item && item.Item.VariableName is QsLocalSymbol.ValidName varName
                    ? varName.Item
                    : this.GenerateUniqueName("arg");

                return arg is QsArgumentTuple.QsTuple tuple
                    ? tuple.Item.Select(item => LocalVarName(item))
                    : new [] { LocalVarName(arg) };
            }

            this.CurrentFunction = this.RegisterFunction(spec, argTuple);
            this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
            this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);

            this.namesInScope.Push(new Dictionary<string, (Value, bool)>());
            var i = 0;
            foreach (var argName in ArgTupleToNames(argTuple))
            {
                this.CurrentFunction.Parameters[i].Name = argName;
                this.namesInScope.Peek().Add(argName, (this.CurrentFunction.Parameters[i], false));
                i++;
            }
        }

        /// <summary>
        /// Generates the default constructor for a Q# user-defined type.
        /// This routine generates all the code for the constructor, not just the header.
        /// </summary>
        /// <param name="udt">The Q# user-defined type</param>
        public void GenerateConstructor(QsCustomType udt)
        {
            var name = CallableName(udt.FullName.Namespace, udt.FullName.Name, QsSpecializationKind.QsBody);

            var args = udt.Type.Resolution switch
            {
                QsResolvedTypeKind.TupleType tup => tup.Item.Select(this.LlvmTypeFromQsharpType).Prepend(this.QirTupleHeader).ToArray(),
                _ when udt.Type.Resolution.IsUnitType => new ITypeRef[] { this.QirTupleHeader },
                _ => new ITypeRef[] { this.QirTupleHeader, this.LlvmTypeFromQsharpType(udt.Type) }
            };
            var udtTupleType = this.Context.CreateStructType(false, args);
            var udtPointerType = args.Length > 1 ? udtTupleType.CreatePointerType() : this.QirTuplePointer;
            var signature = this.Context.GetFunctionType(udtPointerType, args[1..]);

            this.StartSpecialization();            
            this.CurrentFunction = this.Module.CreateFunction(name, signature);
            this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
            this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);

            // An easy case -- (), a marker UDT
            if (args.Length == 1)
            {
                this.CurrentBuilder.Return(udtPointerType.GetNullValue());
            }
            else
            {
                var tuple = this.CreateTupleForType(udtTupleType);
                var udtTuple = this.CurrentBuilder.BitCast(tuple, udtPointerType);

                for (int i = 0; i < args.Length-1; i++)
                {
                    this.CurrentFunction.Parameters[i].Name = $"arg{i}";
                    var itemPtr = this.CurrentBuilder.GetStructElementPointer(udtTupleType,
                        udtTuple, (uint)i + 1);
                    this.CurrentBuilder.Store(this.CurrentFunction.Parameters[i], itemPtr);
                    // Add a reference to the value, if necessary
                    this.AddRef(this.CurrentFunction.Parameters[i]);
                }

                this.CurrentBuilder.Return(udtTuple);
            }
        }

        /// <summary>
        /// Ends a specialization by finishing the current basic block.
        /// </summary>
        /// <exception cref="InvalidOperationException">The current function or the current block is set to null.</exception>
        public void EndSpecialization()
        {
            if (this.CurrentFunction == null || this.CurrentBlock == null)
            {
                throw new InvalidOperationException("the current function or the current block is null");
            }

            bool HasAPredecessor(BasicBlock block)
            {
                foreach (var b in this.CurrentFunction.BasicBlocks)
                {
                    if ((b != block) && (b.Terminator != null))
                    {
                        var term = b.Terminator;
                        if (term is Branch br)
                        {
                            if (br.Successors.Contains(block))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            this.ScopeMgr.CloseScope(this.CurrentBlock.Terminator != null);
            if (this.CurrentBlock.Instructions.Count() == 0 && !HasAPredecessor(this.CurrentBlock))
            {
                this.CurrentFunction.BasicBlocks.Remove(this.CurrentBlock);
            }
            else if (this.CurrentBlock.Terminator == null)
            {
                this.CurrentBuilder.Return();
            }
        }

        /// <summary>
        /// Gets the function for a specialization by name so it can be called.
        /// If the function hasn't been generated yet, its declaration is generated so that it can be called.
        /// </summary>
        /// <param name="namespaceName">The callable's namespace</param>
        /// <param name="name">The callable's name</param>
        /// <param name="kind">The specialization kind</param>
        /// <returns>The LLVM object for the corresponding LLVM function</returns>
        public IrFunction GetFunctionByName(string namespaceName, string name, QsSpecializationKind kind)
        {
            // If the function is already defined, return it
            if (this.TryGetFunction(namespaceName, name, kind, out IrFunction? function))
            {
                return function;
            }
            // Otherwise, we need to find the function's callable to get the signature,
            // and then register the function
            if (this.TryFindGlobalCallable(namespaceName, name, out QsCallable? callable))
            {
                var spec = callable.Specializations.First(spec => spec.Kind == kind);
                return this.RegisterFunction(spec, callable.ArgumentTuple);
            }
            // If we can't find the function at all, it's a problem...
            throw new KeyNotFoundException($"Can't find callable {namespaceName}.{name}");
        }

        /// <summary>
        /// Tries to get the function for a specialization by name so it can be called.
        /// If the function hasn't been generated yet, false is returned.
        /// </summary>
        /// <param name="namespaceName">The callable's namespace</param>
        /// <param name="name">The callable's name</param>
        /// <param name="kind">The specialization kind</param>
        /// <param name="function">Gets filled in with the LLVM function object if it exists already</param>
        /// <returns>true if the function has already been declared/defined, or false otherwise</returns>
        public bool TryGetFunction(string namespaceName, string name, QsSpecializationKind kind, [MaybeNullWhen(false)] out IrFunction function)
        {
            var fullName = CallableName(namespaceName, name, kind);
            foreach (var func in this.Module.Functions)
            {
                if (func.Name == fullName)
                {
                    function = func;
                    return true;
                }
            }

            function = null;
            return false;
        }

        /// <summary>
        /// Tries to get the wrapper function for a specialization.
        /// If the wrapper function hasn't been generated yet, false is returned.
        /// </summary>
        /// <param name="callable">The callable</param>
        /// <param name="kind">The specialization kind</param>
        /// <param name="function">Gets filled in with the LLVM function object if it exists already</param>
        /// <returns>true if the function has already been declared/defined, or false otherwise</returns>
        public bool TryGetWrapper(QsCallable callable, QsSpecializationKind kind, [MaybeNullWhen(false)] out IrFunction function)
        {
            var fullName = CallableWrapperName(callable, kind);
            foreach (var func in this.Module.Functions)
            {
                if (func.Name == fullName)
                {
                    function = func;
                    return true;
                }
            }

            function = null;
            return false;
        }

        /// <summary>
        /// Makes the given basic block current, creates a new builder for it, and makes that builder current.
        /// This method does not check to make sure that the block isn't already current.
        /// </summary>
        /// <param name="b">The block to make current</param>
        protected internal void SetCurrentBlock(BasicBlock b)
        {
            this.CurrentBlock = b;
            this.CurrentBuilder = new InstructionBuilder(b);
        }

        /// <summary>
        /// Creates a new basic block and adds it to the current function immediately after the current block.
        /// </summary>
        /// <param name="name">The base name for the new block; a counter will be appended to ensure uniqueness</param>
        /// <returns>The new block</returns>
        /// <exception cref="InvalidOperationException">The current function is set to null.</exception>
        protected internal BasicBlock AddBlockAfterCurrent(string name)
        {
            if (this.CurrentFunction == null)
            {
                throw new InvalidOperationException("no current function specified");
            }

            var flag = false;
            BasicBlock? next = null;
            foreach (var block in this.CurrentFunction.BasicBlocks)
            {
                if (flag)
                {
                    next = block;
                    break;
                }
                if (block == this.CurrentBlock)
                {
                    flag = true;
                }
            }
            var continueName = this.GenerateUniqueName(name);
            return next == null
                ? this.CurrentFunction.AppendBasicBlock(continueName)
                : this.CurrentFunction.InsertBasicBlock(continueName, next);
        }

        #endregion

        #region Type conversion and helpers

        /// <summary>
        /// Gets the QIR equivalent for a Q# type.
        /// Tuples are represented as QirTuplePointer, arrays as QirArray, and callables as QirCallable.
        /// </summary>
        /// <param name="resolvedType">The Q# type</param>
        /// <returns>The equivalent QIR type</returns>
        public ITypeRef LlvmTypeFromQsharpType(ResolvedType resolvedType)
        {
            this.BuiltType = null;
            this.Transformation.Types.OnType(resolvedType);
            return this.BuiltType ?? throw new NotImplementedException("Llvm type could not be constructed");
        }

        /// <summary>
        /// Gets the QIR equivalent for a Q# type, as a structure.
        /// Tuples are represented as an anonymous LLVM structure type with a TupleHeader as the first element.
        /// Other types are represented as anonymous LLVM structure types with a TupleHeader in the first element
        /// and the "normal" converted type as the second element.
        /// </summary>
        /// <param name="resolvedType">The Q# type</param>
        /// <returns>The equivalent QIR structure type</returns>
        public ITypeRef LlvmStructTypeFromQsharpType(ResolvedType resolvedType)
        {
            if (resolvedType.Resolution is QsResolvedTypeKind.TupleType tuple)
            {
                var elementTypes = tuple.Item.Select(this.LlvmTypeFromQsharpType).Prepend(this.QirTupleHeader).ToArray();
                return this.Context.CreateStructType(false, elementTypes);
            }
            else
            {
                return this.Context.CreateStructType(false, this.QirTupleHeader, this.LlvmTypeFromQsharpType(resolvedType));
            }
        }

        /// <summary>
        /// Computes the size in bytes of an LLVM type as an LLVM value.
        /// If the type isn't a simple pointer, integer, or double, we compute it using a standard LLVM idiom.
        /// </summary>
        /// <param name="t">The LLVM type to compute the size of</param>
        /// <param name="b">The builder to use to generate the struct size computation, if needed</param>
        /// <returns>An LLVM value containing the size of the type in bytes</returns>
        protected internal Value ComputeSizeForType(ITypeRef t, InstructionBuilder b)
        {
            if (t.IsInteger)
            {
                return this.Context.CreateConstant((long)((t.IntegerBitWidth + 7) / 8));
            }
            else if (t.IsDouble)
            {
                return this.Context.CreateConstant(8L);
            }
            else if (t.IsPointer)
            {
                // We assume 64-bit address space
                return this.Context.CreateConstant(8L);
            }
            else
            {
                // Everything else we let getelementptr compute for us
                var basePointer = Constant.ConstPointerToNullFor(t.CreatePointerType());
                var firstPtr = b.GetElementPtr(t, basePointer,
                    new[] { this.Context.CreateConstant(0) }); ;
                var first = b.PointerToInt(firstPtr, this.Context.Int64Type);
                var secondPtr = b.GetElementPtr(t, basePointer,
                    new[] { this.Context.CreateConstant(1) });
                var second = b.PointerToInt(secondPtr, this.Context.Int64Type);
                return this.CurrentBuilder.Sub(second, first);
            }
        }

        /// <summary>
        /// Determines whether an LLVM type is a pointer to a tuple.
        /// Specifically, is the type a pointer to a structure whose first element is a TupleHeader?
        /// </summary>
        /// <param name="t">The type to check</param>
        /// <returns>true if t is a pointer to a tuple, false otherwise</returns>
        public bool IsTupleType(ITypeRef t) => ((t is IPointerType pt) && (pt.ElementType is IStructType st) &&
                        (st.Members.Count > 0) && (st.Members[0] == this.QirTupleHeader));

        #endregion

        #region Value stack and named value management

        /// <summary>
        /// Pushes a value onto the value stack and also adds it to the current ref counting scope.
        /// </summary>
        /// <param name="value">The LLVM value to push</param>
        /// <param name="valueType">The Q# type of the value</param>
        public void PushValueInScope(Value value, ResolvedType valueType)
        {
            this.ValueStack.Push(value);
            this.ScopeMgr.AddValue(value, valueType);
        }

        /// <summary>
        /// Generates a unique string with a given prefix.
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <returns>A string that is unique across calls to this method</returns>
        internal string GenerateUniqueName(string prefix)
        {
            var index = this.uniqueNameIds.TryGetValue(prefix, out int n) ? n + 1 : 1;
            this.uniqueNameIds[prefix] = index;
            return $"{prefix}__{index}";
        }

        /// <summary>
        /// Registers a variable name as an alias for an LLVM value.
        /// </summary>
        /// <param name="name">The name to register</param>
        /// <param name="value">The LLVM value</param>
        /// <param name="isMutable">true if the name binding is mutable, false if immutable; the default is false</param>
        public void RegisterName(string name, Value value, bool isMutable = false)
        {
            if (String.IsNullOrEmpty(value.Name))
            {
                value.RegisterName(this.InlinedName(name));
            }
            this.namesInScope.Peek().Add(name, (value, isMutable));
        }

        /// <summary>
        /// Pops a value off of the value stack and registers a variable name as an alias for it.
        /// </summary>
        /// <param name="name">The name (alias) to register</param>
        /// <param name="isMutable">true if the name binding is mutable, false if immutable; the default is false</param>
        /// <returns>The value that was popped off the stack</returns>
        public Value PopAndRegister(string name, bool isMutable = false)
        {
            Value value = this.ValueStack.Pop();
            this.RegisterName(name, value, isMutable);
            return value;
        }

        /// <summary>
        /// Gets the pointer to a mutable variable by name.
        /// The name must have been registered as an alias for the pointer value using 
        /// <see cref="RegisterName(string, Value, bool)"/> or <see cref="PopAndRegister(string, bool)"/>.
        /// </summary>
        /// <param name="name">The registered variable name to look for</param>
        /// <returns>The pointer value for the mutable value</returns>
        public Value GetNamedPointer(string name)
        {
            foreach (var dict in this.namesInScope)
            {
                if (dict.TryGetValue(name, out (Value, bool) item))
                {
                    if (item.Item2)
                    {
                        return item.Item1;
                    }
                }
            }
            throw new KeyNotFoundException($"Could not find a Value for mutable symbol {name}");
        }

        /// <summary>
        /// Pushes the value of a named variable on the value stack.
        /// The name must have been registered as an alias for the value using 
        /// <see cref="RegisterName(string, Value, bool)"/> or <see cref="PopAndRegister(string, bool)"/>.
        /// <para>
        /// If the variable is mutable, then the associated pointer value is used to load and push the actual
        /// variable value.
        /// </para>
        /// </summary>
        /// <param name="name">The registered variable name to look for</param>
        public void PushNamedValue(string name)
        {
            foreach (var dict in this.namesInScope)
            {
                if (dict.TryGetValue(name, out (Value, bool) item))
                {
                    this.ValueStack.Push(
                        item.Item2 && item.Item1.NativeType is IPointerType ptr
                        // Mutable, so the value is a pointer; we need to load what it's pointing to
                        ? this.CurrentBuilder.Load(ptr.ElementType, item.Item1)
                        : item.Item1
                    );
                    return;
                }
            }
            throw new KeyNotFoundException($"Could not find a Value for local symbol {name}");
        }

        /// <summary>
        /// Opens a new naming scope and pushes it on top of the naming scope stack.
        /// <para>
        /// Naming scopes map variable names to values.
        /// New names are always added to the scope on top of the stack.
        /// When looking for a name, the stack is searched top-down.
        /// </para>
        /// </summary>
        public void OpenNamingScope()
        {
            this.namesInScope.Push(new Dictionary<string, (Value, bool)>());
        }

        /// <summary>
        /// Closes the current naming scope by popping it off of the naming scope stack.
        /// </summary>
        public void CloseNamingScope()
        {
            this.namesInScope.Pop();
        }

        #endregion

        #region Inlining support
        // Embedded inlining -- inlining while in the middle of inlining -- should work,
        // but is not tested.

        /// <summary>
        /// Start inlining a callable invocation.
        /// This opens a new naming scope and increases the inlining level.
        /// </summary>
        public void StartInlining()
        {
            this.OpenNamingScope();
            this.inlineLevel++;
        }

        /// <summary>
        /// Stop inlining a callable invocation.
        /// This pops the top naming scope and decreases the inlining level.
        /// </summary>
        public void StopInlining()
        {
            this.inlineLevel--;
            this.CloseNamingScope();
        }

        /// <summary>
        /// Maps a variable name to an inlining-safe name.
        /// This way, names declared in an inlined callable don't conflict with names defined in the calling routine.
        /// </summary>
        /// <param name="name">The name to map</param>
        /// <returns>The mapped name</returns>
        public string InlinedName(string name)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < this.inlineLevel; i++)
            {
                sb.Append('.');
            }
            sb.Append(name);
            //sb.Append('.');
            return sb.ToString();
        }

        #endregion

        #region Utilities for tuples

        /// <summary>
        /// Builds the LLVM type that represents a Q# argument tuple as a passed value.
        /// <br/><br/>
        /// See also <seealso cref="LlvmTypeFromQsharpType(ResolvedType)"/>.
        /// </summary>
        /// <param name="argItem">The Q# argument tuple</param>
        /// <returns>The LLVM type</returns>
        internal ITypeRef BuildArgItemTupleType(QsArgumentTuple argItem)
        {
            switch (argItem)
            {
                case QsArgumentTuple.QsTuple tuple:
                {
                    var elems = tuple.Item.Select(this.BuildArgItemTupleType).Prepend(this.QirTupleHeader).ToArray();
                    return this.Context.CreateStructType(false, elems).CreatePointerType();
                }

                case QsArgumentTuple.QsTupleItem item:
                {
                    // Single items get translated to the appropriate LLVM type
                    return this.LlvmTypeFromQsharpType(item.Item.Type);
                }
                default:
                {
                    throw new NotImplementedException("Unknown item in argument tuple.");
                }
            }
        }

        /// <summary>
        /// Builds the LLVM type that represents a Q# argument tuple as a structure.
        /// Note that tupled arguments generate an LLVM structure type.
        /// <br/><br/>
        /// See also <seealso cref="LlvmStructTypeFromQsharpType(ResolvedType)"/>.
        /// </summary>
        /// <param name="arg">The Q# argument tuple</param>
        /// <returns>The LLVM type</returns>
        internal ITypeRef BuildArgTupleType(QsArgumentTuple arg)
        {
            if (arg is QsArgumentTuple.QsTuple tuple)
            {
                return tuple.Item.Length == 0
                    ? this.Context.VoidType
                    : this.Context.CreateStructType(
                        false,
                        tuple.Item.Select(this.BuildArgItemTupleType).Prepend(this.QirTupleHeader).ToArray());
            }
            else if (arg is QsArgumentTuple.QsTupleItem item)
            {
                var itemTypeRef = this.LlvmTypeFromQsharpType(item.Item.Type);
                return this.Context.CreateStructType(false, this.QirTupleHeader, itemTypeRef);
            }
            else
            {
                throw new NotImplementedException("Unknown item in argument tuple.");
            }
        }

        /// <summary>
        /// Creates a new tuple for an LLVM structure type.
        /// The new tuple is created using the current builder.
        /// </summary>
        /// <param name="t">The LLVM structure type for the tuple</param>
        /// <returns>A value containing the pointer to the new tuple</returns>
        internal Value CreateTupleForType(ITypeRef t)
        {
            var size = this.ComputeSizeForType(t, this.CurrentBuilder);
            var tuple = this.CurrentBuilder.Call(this.GetRuntimeFunction("tuple_create"), size);
            return tuple;
        }

        /// <summary>
        /// Creates and returns a deep copy of a tuple.
        /// By default this uses the current builder, but an alternate builder may be provided.
        /// </summary>
        /// <param name="original">The original tuple as an LLVM TupleHeader pointer</param>
        /// <param name="t">The Q# type of the tuple</param>
        /// <param name="b">(optional) The instruction builder to use; the current builder is used if not provided</param>
        /// <returns>The new copy, as an LLVM value containing a TupleHeader pointer</returns>
        internal Value DeepCopyTuple(Value original, ResolvedType t, InstructionBuilder? b = null)
        {
            InstructionBuilder builder = b ?? this.CurrentBuilder;
            if (t.Resolution is QsResolvedTypeKind.TupleType tupleType)
            {
                var originalTypeRef = this.LlvmStructTypeFromQsharpType(t);
                var originalPointerType = originalTypeRef.CreatePointerType();
                var originalSize = this.ComputeSizeForType(originalTypeRef, builder);
                var copy = builder.Call(this.GetRuntimeFunction("tuple_create"), originalSize);
                var typedOriginal = builder.BitCast(original, originalPointerType);
                var typedCopy = builder.BitCast(copy, originalPointerType);

                var elementTypes = tupleType.Item;
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    var elementType = elementTypes[i];
                    var originalElementPointer = builder.GetStructElementPointer(originalTypeRef,
                        typedOriginal, (uint)i + 1);
                    var originalElement = builder.Load(this.LlvmTypeFromQsharpType(elementType), 
                        originalElementPointer);
                    Value elementValue = elementType.Resolution switch
                    {
                        QsResolvedTypeKind.TupleType _ => 
                            this.DeepCopyTuple(originalElement, elementType, b),
                        QsResolvedTypeKind.ArrayType _ => 
                            builder.Call(this.GetRuntimeFunction("array_copy"), originalElement),
                        _ => originalElement,
                    };
                    var copyElementPointer = builder.GetStructElementPointer(originalTypeRef,
                        typedCopy, (uint)i + 1);
                    builder.Store(elementValue, copyElementPointer);
                }

                return typedCopy;
            }
            else
            {
                return Constant.UndefinedValueFor(this.QirTuplePointer);
            }
        }

        /// <summary>
        /// Creates and returns a deep copy of a value of a user-defined type.
        /// By default this uses the current builder, but an alternate builder may be provided.
        /// </summary>
        /// <param name="original">The original value</param>
        /// <param name="t">The Q# type, which should be a user-defined type</param>
        /// <param name="b">(optional) The instruction builder to use; the current builder is used if not provided</param>
        /// <returns>The new copy</returns>
        internal Value DeepCopyUDT(Value original, ResolvedType t, InstructionBuilder? b = null)
        {
            if ((t.Resolution is QsResolvedTypeKind.UserDefinedType tt) &&
                this.TryFindUDT(tt.Item.Namespace, tt.Item.Name, out QsCustomType? udt))
            {
                if (udt.Type.Resolution.IsTupleType)
                {
                    return this.DeepCopyTuple(original, udt.Type, b);
                }
                else if (udt.Type.Resolution.IsArrayType)
                {
                    InstructionBuilder builder = b ?? this.CurrentBuilder;
                    return builder.Call(this.GetRuntimeFunction("array_copy"), original);
                }
                else
                {
                    return original;
                }
            }
            else
            {
                return Constant.UndefinedValueFor(this.QirTuplePointer);
            }
        }

        /// <summary>
        /// Returns true if the expression is an item that should be copied for COW safety, false otherwise.
        /// <br/><br/>
        /// Specifically, an item requires copying if it is an array or a tuple, and if it is an identifier
        /// or an element or a slice of an identifier.
        /// </summary>
        /// <param name="ex">The expression to test.</param>
        /// <returns>true if the expression should be copied before use, false otherwise.</returns>
        internal bool ItemRequiresCopying(TypedExpression ex)
        {
            if (ex.ResolvedType.Resolution.IsArrayType || ex.ResolvedType.Resolution.IsUserDefinedType
                || ex.ResolvedType.Resolution.IsTupleType)
            {
                return ex.Expression switch
                {
                    ResolvedExpression.Identifier _ => true,
                    ResolvedExpression.ArrayItem arr => this.ItemRequiresCopying(arr.Item1),
                    _ => false
                };
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a writable Value for an expression.
        /// If necessary, this will make a copy of the item based on the rules in
        /// <see cref="ItemRequiresCopying(TypedExpression)"/>.
        /// </summary>
        /// <param name="ex">The expression to test.</param>
        /// <returns>An LLVM value that is safe to change.</returns>
        internal Value GetWritableCopy(TypedExpression ex, InstructionBuilder? b = null)
        {
            // Evaluating the input always happens on the current builder
            this.Transformation.Expressions.OnTypedExpression(ex);
            var item = this.ValueStack.Pop();
            InstructionBuilder builder = b ?? this.CurrentBuilder;
            if (this.ItemRequiresCopying(ex))
            {
                Value copy = ex.ResolvedType.Resolution switch
                {
                    QsResolvedTypeKind.UserDefinedType _ => this.DeepCopyUDT(item, ex.ResolvedType, b),
                    QsResolvedTypeKind.TupleType _ => this.DeepCopyTuple(item, ex.ResolvedType, b),
                    QsResolvedTypeKind.ArrayType _ => builder.Call(this.GetRuntimeFunction("array_copy"), item),
                    _ => Constant.UndefinedValueFor(this.LlvmTypeFromQsharpType(ex.ResolvedType)),
                };
                this.ScopeMgr.AddValue(copy, ex.ResolvedType);
                return copy;
            }
            else
            {
                return item;
            }
        }

        /// <summary>
        /// Fills in an LLVM tuple from a Q# expression.
        /// The tuple should already be allocated, but any embedded tuples will be allocated by this method.
        /// </summary>
        /// <param name="pointerToTuple">The LLVM tuple to fill in</param>
        /// <param name="expr">The Q# expression to evaluate and fill in the tuple with</param>
        internal void FillTuple(Value pointerToTuple, TypedExpression expr)
        {
            void FillStructSlot(ITypeRef structType, Value pointerToStruct, Value fillValue, int position)
            {
                // Generate a store for the value
                Value[] indices = new Value[] {
                        this.Context.CreateConstant(0L),
                        this.Context.CreateConstant(position)
                    };
                var elementPointer = this.CurrentBuilder.GetElementPtr(structType, pointerToStruct, indices);
                this.CurrentBuilder.Store(fillValue, elementPointer);
            }

            void FillItem(ITypeRef structType, Value pointerToStruct, TypedExpression fillExpr, int position)
            {
                Contract.Assert(!fillExpr.ResolvedType.Resolution.IsTupleType, "FillItem is for non-tuple items only");
                this.Transformation.Expressions.OnTypedExpression(fillExpr);
                var fillValue = this.ValueStack.Pop();
                FillStructSlot(structType, pointerToStruct, fillValue, position);
            }

            var tupleTypeRef = this.LlvmStructTypeFromQsharpType(expr.ResolvedType);
            var tupleToFillPointer = this.CurrentBuilder.BitCast(pointerToTuple, tupleTypeRef.CreatePointerType());
            if (expr.Expression is ResolvedExpression.ValueTuple tuple)
            {
                var items = tuple.Item;
                for (var i = 0; i < items.Length; i++)
                {
                    switch (items[i].Expression)
                    {
                        case ResolvedExpression.ValueTuple _:
                            // Handle inner tuples: allocate space. initialize, and then recurse
                            var subTupleTypeRef = this.LlvmTypeFromQsharpType(items[i].ResolvedType);
                            var subTupleAsTuplePointer = this.CreateTupleForType(subTupleTypeRef);
                            FillStructSlot(tupleTypeRef, tupleToFillPointer, subTupleAsTuplePointer, i + 1);
                            this.FillTuple(subTupleAsTuplePointer, items[i]);
                            break;
                        default:
                            FillItem(tupleTypeRef, tupleToFillPointer, items[i], i + 1);
                            break;
                    }
                }
            }
            else
            {
                FillItem(tupleTypeRef, tupleToFillPointer, expr, 1);
            }
        }

        /// <summary>
        /// Binds the variables in a Q# argument tuple to a Q# expression.
        /// Arbitrary tuple nesting in the argument tuple is supported.
        /// <br/><br/>
        /// This method is used when inlining to create the bindings from the 
        /// argument expression to the argument variables.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="d"></param>
        internal void MapTuple(TypedExpression s, QsArgumentTuple d)
        {
            // We keep a queue of pending assignments and apply them after we've evaluated all of the expressions.
            // Effectively we need to do a LET* (parallel let) instead of a LET.
            void MapTupleInner(TypedExpression source, QsArgumentTuple destination, 
                List<(string, Value)> assignmentQueue)
            {
                if (destination is QsArgumentTuple.QsTuple tuple)
                {
                    var items = tuple.Item;
                    if (source.Expression is ResolvedExpression.ValueTuple srcItems)
                    {
                        foreach (var (ex, ti) in srcItems.Item.Zip(items, (ex, ti) => (ex, ti)))
                        {
                            MapTupleInner(ex, ti, assignmentQueue);
                        }
                    }
                    else if (items.Length == 1)
                    {
                        MapTupleInner(source, items[0], assignmentQueue);
                    }
                    else
                    {
                        Contract.Assert(source.Expression.IsValueTuple, "Argument values are inconsistent with actual arguments while inlining");
                    }
                }
                else if (destination is QsArgumentTuple.QsTupleItem arg 
                    && arg.Item.VariableName is QsLocalSymbol.ValidName varName)
                {
                    this.Transformation.Expressions.OnTypedExpression(source);
                    var value = this.ValueStack.Pop();
                    assignmentQueue.Add((varName.Item, value));
                }
            }

            var queue = new List<(string, Value)>();
            MapTupleInner(s, d, queue);
            foreach (var (name, value) in queue)
            {
                this.RegisterName(name, value);
            }
        }

        #endregion

        #region Wrapper management

        public GlobalVariable EnsureWrapperFor(QsCallable callable)
        {
            var key = $"{CleanNamespaceName(callable.FullName.Namespace)}__{callable.FullName.Name}";
            if (this.wrapperQueue.TryGetValue(key, out (QsCallable, GlobalVariable) item))
            {
                return item.Item2;
            }
            else
            {
                // Generate the callable's function array
                Constant[] funcs = new Constant[4];
                for (var index = 0; index < 4; index++)
                {
                    QsSpecializationKind kind = FunctionArray[index];
                    if (callable.Specializations.Any(spec => spec.Kind == kind))
                    {
                        var f = this.Module.CreateFunction(CallableWrapperName(callable, kind),
                            this.StandardWrapperSignature);
                        funcs[index] = f;
                    }
                    else
                    {
                        funcs[index] = Constant.NullValueFor(funcs[0].NativeType);
                    }
                }

                ITypeRef t = funcs[0].NativeType;
                Constant array = ConstantArray.From(t, funcs);
                var table = this.Module.AddGlobal(array.NativeType, true, Linkage.DllExport, array, key);
                this.wrapperQueue.Add(key, (callable, table));
                return table;
            }
        }

        private void GenerateQueuedWrappers()
        {
            // Generate the code that decomposes the tuple back into the named arguments
            // Note that we don't want to recurse here!!.
            List<Value> GenerateArgTupleDecomposition(QsArgumentTuple arg, Value value)
            {
                Value BuildLoadForArg(QsArgumentTuple arg, Value value)
                {
                    ITypeRef argTypeRef = arg is QsArgumentTuple.QsTupleItem item
                        ? this.BuildArgItemTupleType(item)
                        : this.QirTuplePointer;
                    // value is a pointer to the argument
                    Value actualArg = this.CurrentBuilder.Load(argTypeRef, value);
                    return actualArg;
                }

                List<Value> args = new List<Value>();
                if (arg is QsArgumentTuple.QsTuple tuple)
                {
                    var items = tuple.Item;
                    var n = items.Length;
                    if (n > 0)
                    {
                        ITypeRef tupleTypeRef = this.BuildArgTupleType(arg);
                        // Convert value from TuplePointer to the proper type
                        Value asStructPointer = this.CurrentBuilder.BitCast(value,
                            tupleTypeRef.CreatePointerType());
                        var indices = new Value[] { this.Context.CreateConstant(0L),
                                                    this.Context.CreateConstant(1) };
                        for (var i = 0; i < n; i++)
                        {
                            indices[1] = this.Context.CreateConstant(i + 1);
                            Value ptr = this.CurrentBuilder.GetElementPtr(tupleTypeRef, asStructPointer, indices);
                            args.Add(BuildLoadForArg(items[i], ptr));
                        }
                    }
                }
                else
                {
                    args.Add(BuildLoadForArg(arg, value));
                }

                return args;
            }

            void PopulateResultTuple(ResolvedType resultType, Value resultValue, Value resultTuplePointer, int item)
            {
                var resultTupleTypeRef = this.LlvmStructTypeFromQsharpType(resultType);
                if (resultType.Resolution is QsResolvedTypeKind.TupleType tupleType)
                {
                    // Here we'll step through and recurse
                    var itemCount = tupleType.Item.Length;
                    // Start with 1 because the 0 element of the LLVM structures is the tuple header
                    for (int i = 1; i <= itemCount; i++)
                    {

                    }
                }
                else if (!resultType.Resolution.IsUnitType)
                {
                    Value structPointer = this.CurrentBuilder.BitCast(resultTuplePointer,
                        resultTupleTypeRef.CreatePointerType());
                    Value resultPointer = this.CurrentBuilder.GetElementPtr(resultTupleTypeRef, structPointer,
                        new Value[] { this.Context.CreateConstant(0L), this.Context.CreateConstant(item) });
                    this.CurrentBuilder.Store(resultValue, resultPointer);
                }
            }

            Value GenerateBaseMethodCall(QsCallable callable, QsSpecialization spec, List<Value> args)
            {
                if (this.TryGetFunction(callable.FullName.Namespace, callable.FullName.Name, spec.Kind,
                    out IrFunction? func))
                {
                    return this.CurrentBuilder.Call(func, args.ToArray());
                }
                else
                {
                    return Constant.UndefinedValueFor(this.QirTuplePointer);
                }
            }

            bool GenerateWrapperHeader(QsCallable callable, QsSpecialization spec)
            {
                if (this.TryGetWrapper(callable, spec.Kind, out IrFunction? func))
                {
                    this.CurrentFunction = func;
                    this.CurrentFunction.Parameters[0].Name = "capture-tuple";
                    this.CurrentFunction.Parameters[1].Name = "arg-tuple";
                    this.CurrentFunction.Parameters[2].Name = "result-tuple";
                    this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
                    this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);
                    this.namesInScope.Push(new Dictionary<string, (Value, bool)>());
                    return true;
                }
                else
                {
                    return false;
                }
            }

            foreach (var kvp in this.wrapperQueue)
            {
                var callable = kvp.Value.Item1;
                foreach (var spec in callable.Specializations)
                {
                    if (GenerateWrapperHeader(callable, spec) && this.CurrentFunction != null)
                    {
                        Value argTupleValue = this.CurrentFunction.Parameters[1];
                        var argList = GenerateArgTupleDecomposition(callable.ArgumentTuple, argTupleValue);
                        var result = GenerateBaseMethodCall(callable, spec, argList);
                        PopulateResultTuple(callable.Signature.ReturnType, result, this.CurrentFunction.Parameters[2], 1);
                        this.CurrentBuilder.Return();
                    }
                }
            }
        }

        #endregion

        #region Utils for global declaration

        /// <summary>
        /// Tries to find a global Q# callable in the current compilation.
        /// </summary>
        /// <param name="nsName">The callable's namespace</param>
        /// <param name="name">The callable's name</param>
        /// <param name="callable">The Q# callable, if found</param>
        /// <returns>true if the callable is found, false if not</returns>
        public bool TryFindGlobalCallable(string nsName, string name, [MaybeNullWhen(false)] out QsCallable callable)
        {
            callable = null;

            foreach (var ns in this.Compilation.Namespaces)
            {
                if (ns.Name == nsName)
                {
                    foreach (var element in ns.Elements)
                    {
                        if (element is QsNamespaceElement.QsCallable c)
                        {
                            if (c.GetFullName().Name == name)
                            {
                                callable = c.Item;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to find a Q# user-defined type in the current compilation.
        /// </summary>
        /// <param name="nsName">The UDT's namespace</param>
        /// <param name="name">The UDT's name</param>
        /// <param name="udt">The Q# UDT< if found</param>
        /// <returns>true if the UDT is found, false if not</returns>
        public bool TryFindUDT(string nsName, string name, [MaybeNullWhen(false)] out QsCustomType udt)
        {
            udt = null;

            foreach (var ns in this.Compilation.Namespaces)
            {
                if (ns.Name == nsName)
                {
                    foreach (var element in ns.Elements)
                    {
                        if (element is QsNamespaceElement.QsCustomType t)
                        {
                            if (t.GetFullName().Name == name)
                            {
                                udt = t.Item;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tells whether or not a callable invocation should be inlined.
        /// For this purpose, any invocation of a top-level callable with the "Inline" attribute
        /// should be inlined.
        /// </summary>
        /// <param name="method">The Q# callable expression to invoke</param>
        /// <param name="callable">The top-level callable to inline, if appropriate</param>
        /// <returns>true if the callable should be inlined, false if not</returns>
        public bool IsInlined(TypedExpression method, [MaybeNullWhen(false)] out QsCallable callable)
        {
            callable = null;
            if ((method.Expression is ResolvedExpression.Identifier id) && id.Item1 is Identifier.GlobalCallable c)
            {
                var nsName = c.Item.Namespace;
                var name = c.Item.Name;
                if (this.TryFindGlobalCallable(nsName, name, out callable))
                {
                    return callable.Attributes.Any(att =>
                        att.TypeId.IsValue &&
                        att.TypeId.Item.Namespace == BuiltIn.Inline.FullName.Namespace &&
                        att.TypeId.Item.Name == BuiltIn.Inline.FullName.Name);
                }
            }
            return false;
        }

        /// <summary>
        /// Tells whether or not a Q# callable expression is a quantum instruction.
        /// For this purpose, quantum instructions are top-level operations that have the "Intrinsic" attribute.
        /// </summary>
        /// <param name="method">The Q# callable expression to invoke</param>
        /// <param name="instructionName">The quantum instruction name, if the callable is a quantum instruction</param>
        /// <returns>true if the callable is a quantum instruction, false if not</returns>
        public bool IsQuantumInstructionCall(TypedExpression method, out string instructionName)
        {
            instructionName = "";
            if ((method.Expression is ResolvedExpression.Identifier id) && id.Item1 is Identifier.GlobalCallable c)
            {
                var nsName = c.Item.Namespace;
                var name = c.Item.Name;

                if (this.TryFindGlobalCallable(nsName, name, out QsCallable? callable)
                    && SymbolResolution.TryGetQISCode(callable.Attributes) is var att && att.IsValue)
                {
                    instructionName = att.Item;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Interop utils

        /// <summary>
        /// This type is used by <see cref="GenerateEntryPoint(QsQualifiedName)"/> to map Q# types to
        /// interop-friendly types.
        /// </summary>
        private class ArgMapping
        {
            internal readonly string BaseName;
            /// <summary>
            /// The first item contains the array type, and the second item contains the array count name.
            /// </summary>
            private readonly (ITypeRef, string)? ArrayInfo;
            /// <summary>
            /// Contains the struct type.
            /// </summary>
            private readonly ITypeRef? StructInfo;

            internal bool IsArray => this.ArrayInfo != null;
            internal ITypeRef ArrayType => this.ArrayInfo?.Item1 ?? throw new InvalidOperationException("not an array");
            internal string ArrayCountName => this.ArrayInfo?.Item2 ?? throw new InvalidOperationException("not an array");

            internal bool IsStruct => this.StructInfo != null;
            internal ITypeRef StructType => this.StructInfo ?? throw new InvalidOperationException("not a struct");

            private ArgMapping(string baseName, (ITypeRef, string)? arrayInfo = null, ITypeRef? structInfo = null)
            {
                this.BaseName = baseName;
                this.ArrayInfo = arrayInfo;
                this.StructInfo = structInfo;
            }

            static internal ArgMapping Create(string baseName) =>
                new ArgMapping(baseName);

            internal ArgMapping WithArrayInfo(ITypeRef arrayType, string arrayCountName) =>
                new ArgMapping(this.BaseName, arrayInfo: (arrayType, arrayCountName));

            internal ArgMapping WithStructInfo(ITypeRef arrayType, string arrayCountName) =>
                new ArgMapping(this.BaseName, arrayInfo: (arrayType, arrayCountName));
        }

        // TODO: why do we need both this routine and GenerateInteropWrapper?
        /// <summary>
        /// Generates an interop-friendly wrapper around the Q# entry point using the configured type mapping.
        /// </summary>
        /// <param name="qualifiedName">The namespace-qualified name of the Q# entry point</param>
        public void GenerateEntryPoint(QsQualifiedName qualifiedName)
        {
            // Unfortunately this is different enough from all of the other type mapping we do to require
            // an actual different routine. Blech...
            bool MapToCType(QsArgumentTuple t, List<ITypeRef> typeList, List<string> nameList, List<ArgMapping> mappingList)
            {
                bool changed = false;

                if (t is QsArgumentTuple.QsTuple tuple)
                {
                    foreach (QsArgumentTuple inner in tuple.Item)
                    {
                        changed |= MapToCType(inner, typeList, nameList, mappingList);
                    }
                }
                else if (t is QsArgumentTuple.QsTupleItem item && item.Item.VariableName is QsLocalSymbol.ValidName varName)
                {
                    var baseName = varName.Item;
                    var map = ArgMapping.Create(baseName);
                    switch (item.Item.Type.Resolution)
                    {
                        case QsResolvedTypeKind.ArrayType array:
                            // TODO: Handle multidimensional arrays
                            // TODO: Handle arrays of structs
                            typeList.Add(this.Context.Int64Type);
                            var elementTypeRef = this.LlvmTypeFromQsharpType(array.Item);
                            typeList.Add(elementTypeRef.CreatePointerType());
                            var arrayCountName = $"{baseName}__count";
                            nameList.Add(arrayCountName);
                            nameList.Add(baseName);
                            map = map.WithArrayInfo(elementTypeRef, arrayCountName);
                            changed = true;
                            mappingList.Add(map);
                            break;
                        case QsResolvedTypeKind.TupleType _:
                            // TODO: Handle structs
                            break;
                        default:
                            typeList.Add(this.LlvmTypeFromQsharpType(item.Item.Type));
                            nameList.Add(baseName);
                            mappingList.Add(map);
                            break;
                    }
                }
                return changed;
            }

            if (this.TryGetFunction(qualifiedName.Namespace, qualifiedName.Name, QsSpecializationKind.QsBody, out IrFunction? func)
                && this.TryFindGlobalCallable(qualifiedName.Namespace, qualifiedName.Name, out QsCallable? callable))
            {
                var epName = $"{qualifiedName.Namespace.Replace('.', '_')}_{qualifiedName.Name}";

                // Check to see if the arg list needs mapping to more C-friendly types
                // TODO: handle complicated return types
                var mappedArgList = new List<ITypeRef>();
                var mappedNameList = new List<string>();
                var mappingList = new List<ArgMapping>();
                var arraysToReleaseList = new List<Value>();
                var mappedResultType = this.MapToInteropType(func.ReturnType);
                if (MapToCType(callable.ArgumentTuple, mappedArgList, mappedNameList, mappingList) ||
                    (mappedResultType != func.ReturnType))
                {
                    var epFunc = this.Module.CreateFunction(epName, 
                        this.Context.GetFunctionType(mappedResultType, mappedArgList));
                    var namedValues = new Dictionary<string, Value>();
                    for (var i = 0; i < mappedNameList.Count; i++)
                    {
                        epFunc.Parameters[i].Name = mappedNameList[i];
                        namedValues[epFunc.Parameters[i].Name] = epFunc.Parameters[i];
                    }
                    var entryBlock = epFunc.AppendBasicBlock("entry");
                    var builder = new InstructionBuilder(entryBlock);
                    // Build the argument list for the inner function
                    var argValueList = new List<Value>();
                    foreach (var mapping in mappingList)
                    {
                        if (mapping.IsArray)
                        {
                            var elementSize64 = this.ComputeSizeForType(mapping.ArrayType, builder);
                            var elementSize = builder.IntCast(elementSize64, this.Context.Int32Type, false);
                            var length = namedValues[mapping.ArrayCountName];
                            var array = builder.Call(this.GetRuntimeFunction("array_create_1d"), elementSize, 
                                length);
                            argValueList.Add(array);
                            arraysToReleaseList.Add(array);
                            // Fill in the array if the length is >0. Since the QIR array is new, we assume we can use memcpy.
                            var copyBlock = epFunc.AppendBasicBlock("copy");
                            var nextBlock = epFunc.AppendBasicBlock("next");
                            var cond = builder.Compare(IntPredicate.SignedGreaterThan, length, this.Context.CreateConstant(0L));
                            builder.Branch(cond, copyBlock, nextBlock);
                            builder = new InstructionBuilder(copyBlock);
                            var destBase = builder.Call(this.GetRuntimeFunction("array_get_element_ptr_1d"), array,
                                this.Context.CreateConstant(0L));
                            builder.MemCpy(destBase, namedValues[mapping.BaseName], 
                                builder.Mul(length, builder.IntCast(elementSize, this.Context.Int64Type, true)), 
                                false);
                            builder.Branch(nextBlock);
                            builder = new InstructionBuilder(nextBlock);
                        }
                        else if (mapping.IsStruct)
                        {
                            // TODO: map structures
                        }
                        else
                        {
                            argValueList.Add(namedValues[mapping.BaseName]);
                        }
                    }
                    if (func.ReturnType.IsVoid)
                    {
                        // A void entry point would be odd, but it isn't illegal
                        builder.Call(func, argValueList);
                        foreach (var arrayToRelease in arraysToReleaseList)
                        {
                            builder.Call(this.GetRuntimeFunction("array_unreference"), arrayToRelease);
                        }
                        builder.Return();
                    }
                    else
                    {
                        Value result = builder.Call(func, argValueList);
                        foreach (var arrayToRelease in arraysToReleaseList)
                        {
                            builder.Call(this.GetRuntimeFunction("array_unreference"), arrayToRelease);
                        }

                        if (mappedResultType != func.ReturnType)
                        {
                            result = builder.BitCast(result, mappedResultType);
                        }
                        builder.Return(result);
                    }
                    // Mark the function as an entry point
                    epFunc.AddAttributeAtIndex(FunctionAttributeIndex.Function, 
                        this.Context.CreateAttribute("EntryPoint"));
                }
                else
                {
                    this.Module.AddAlias(func, epName).Linkage = Linkage.External;
                    // Mark the function as an entry point
                    func.AddAttributeAtIndex(FunctionAttributeIndex.Function, 
                        this.Context.CreateAttribute("EntryPoint"));
                }
            }
        }

        /// <summary>
        /// Generates a stub implementation for a runtime function or quantum instruction using the specified type mappings for interoperability.
        /// Note that wrappers go into a separate module from the other QIR code.
        /// </summary>
        /// <param name="func">The function to generate a stub for</param>
        /// <param name="baseName">The function that the stub should call</param>
        /// <param name="m">(optional) The LLVM module in which the stub should be generated</param>
        private void GenerateInteropWrapper(IrFunction func, string baseName)
        {
            func = this.BridgeModule.CreateFunction(func.Name, func.Signature);

            var mappedResultType = this.MapToInteropType(func.ReturnType);
            var argTypes = func.Parameters.Select(p => p.NativeType).ToArray();
            var mappedArgTypes = argTypes.Select(this.MapToInteropType).ToArray();

            var interopFunction = this.BridgeModule.CreateFunction(baseName,
                this.Context.GetFunctionType(mappedResultType, mappedArgTypes));

            for (var i = 0; i < func.Parameters.Count; i++)
            {
                func.Parameters[i].Name = $"arg{i + 1}";
            }

            var builder = new InstructionBuilder(func.AppendBasicBlock("entry"));
            var implArgs = Enumerable.Range(0, argTypes.Length)
                .Select(index => argTypes[index] == mappedArgTypes[index]
                    ? func.Parameters[index]
                    : builder.BitCast(func.Parameters[index], mappedArgTypes[index]))
                .ToArray();
            var interopReturnValue = builder.Call(interopFunction, implArgs);
            if (func.ReturnType == this.Context.VoidType)
            {
                builder.Return();
            }
            else if (func.ReturnType == mappedResultType)
            {
                builder.Return(interopReturnValue);
            }
            else
            {
                var returnValue = builder.BitCast(interopReturnValue, func.ReturnType);
                builder.Return(returnValue);
            }
        }

        /// <summary>
        /// Maps a QIR type to a more interop-friendly type using the specified type mapping for interoperability.
        /// </summary>
        /// <param name="t">The type to map</param>
        /// <returns>The mapped type</returns>
        private ITypeRef MapToInteropType(ITypeRef t)
        {
            string typeName = "";
            if (t == this.QirResult)
            {
                typeName = "Result";
            }
            else if (t == this.QirArray)
            {
                typeName = "Array";
            }
            else if (t == this.QirPauli)
            {
                typeName = "Pauli";
            }
            else if (t == this.QirBigInt)
            {
                typeName = "BigInt";
            }
            else if (t == this.QirString)
            {
                typeName = "String";
            }
            else if (t == this.QirQubit)
            {
                typeName = "Qubit";
            }
            else if (t == this.QirCallable)
            {
                typeName = "Callable";
            }
            else if (t == this.QirTuplePointer)
            {
                typeName = "TuplePointer";
            }

            if ((typeName != "") && this.Config.InteropTypeMapping.TryGetValue(typeName, out string replacementName))
            {
                if (this.interopType.TryGetValue(typeName, out ITypeRef interopType))
                {
                    return interopType;
                }
                else
                {
                    var newType = this.Context.CreateStructType(replacementName).CreatePointerType();
                    this.interopType[typeName] = newType;
                    return newType;
                }
            }
            else
            {
                return t;
            }
        }

        #endregion

        #region Miscellaneous

        internal void AddRef(Value v)
        {
            string? s = null;
            var t = v.NativeType;
            Value valToAddref = v;
            if (t.IsPointer)
            {
                if (t == this.QirArray)
                {
                    s = "array_reference";
                }
                else if (t == this.QirResult)
                {
                    s = "result_reference";
                }
                else if (t == this.QirString)
                {
                    s = "string_reference";
                }
                else if (t == this.QirBigInt)
                {
                    s = "bigint_reference";
                }
                else if (this.IsTupleType(t))
                {
                    s = "tuple_reference";
                    valToAddref = this.CurrentBuilder.BitCast(v, this.QirTuplePointer);
                }
                else if (t == this.QirCallable)
                {
                    s = "callable_reference";
                }
            }
            if (s != null)
            {
                var func = this.GetRuntimeFunction(s);
                this.CurrentBuilder.Call(func, valToAddref);
            }
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.Context.Dispose();
                }

                this.disposedValue = true;
            }
        }


        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
        }

        #endregion
    }
}
