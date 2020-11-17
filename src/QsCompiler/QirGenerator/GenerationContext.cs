using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Llvm.NET;
using Llvm.NET.Instructions;
using Llvm.NET.Interop;
using Llvm.NET.Types;
using Llvm.NET.Values;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    using QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using ResolvedExpression = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;

    /// <summary>
    /// This class holds the shared state used across a QIR generation pass.
    /// It also holds a large number of shared utility routines.
    /// </summary>
    public class GenerationContext : IDisposable
    {
        #region Member variables

        // The current transformation
        public QirTransformation Transformation { get; internal set; }

        // The current compilation
        public QsCompilation Compilation { get; private set; }

        // Should we generate clang-compatible wrappers?
        public bool GenerateClangWrappers { get; set; }

        // clang type name mappings, used only if we're generating wrappers
        public Dictionary<string, string> ClangTypeMappings { get; set; }

        // Current state
        public Context CurrentContext { get; }
        public BitcodeModule CurrentModule { get; private set; }
        public IrFunction CurrentFunction { get; private set; }
        public BasicBlock CurrentBlock { get; private set; }
        public InstructionBuilder CurrentBuilder { get; private set; }
        public Stack<Value> ValueStack { get; private set; }
        public Stack<ResolvedType> ExpressionTypeStack { get; private set; }
        public Stack<List<Value>> QubitReleaseStack { get; private set; }
        internal ScopeManager ScopeMgr { get; private set; }
        private BitcodeModule BridgeModule { get; set; }

        // LLVM type for passing up layers
        public ITypeRef BuiltType { get; set;}

        // Current inlining level
        private int inlineLevel = 0;
        public int CurrentInlineLevel => inlineLevel;

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
        public Value QirResultZero { get; private set; }
        public Value QirResultOne { get; private set; }
        public Value QirPauliI { get; private set; }
        public Value QirPauliX { get; private set; }
        public Value QirPauliY { get; private set; }
        public Value QirPauliZ { get; private set; }
        public Value QirEmptyRange { get; private set; }

        // Internal types
        public readonly ITypeRef QirTupleHeader;
        public readonly ITypeRef QirTuplePointer;
        public readonly IFunctionType StandardWrapperSignature;

        // Various internal bits of data
        private readonly IDisposable libContext;
        private readonly Stack<Dictionary<string, (Value, bool)>> namesInScope
            = new Stack<Dictionary<string, (Value, bool)>>();
        private readonly string outputFileName;
        private readonly Dictionary<string, int> uniqueNameIds = new Dictionary<string, int>();
        private readonly Dictionary<string, (QsCallable, GlobalVariable)> wrapperQueue
            = new Dictionary<string, (QsCallable, GlobalVariable)>();
        private FunctionLibrary runtimeLibrary;
        private FunctionLibrary quantumLibrary;
        private readonly Dictionary<string, ITypeRef> clangTranslatedType = new Dictionary<string, ITypeRef>();

        // Order of specializations in the wrapper array
        internal readonly QsSpecializationKind[] FunctionArray = new[] {
            QsSpecializationKind.QsBody,
            QsSpecializationKind.QsAdjoint,
            QsSpecializationKind.QsControlled,
            QsSpecializationKind.QsControlledAdjoint,
        };
        #endregion

        #region Static methods: compute function names
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
            CallableName(callable.FullName.Namespace.Value, callable.FullName.Name.Value, kind);

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
            CallableWrapperName(callable.FullName.Namespace.Value, callable.FullName.Name.Value, kind);

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

        #region Constructor and related initialization
        /// <summary>
        /// Constructs a new generation context.
        /// </summary>
        /// <param name="comp">The current compilation</param>
        /// <param name="outputFile">The base path of the QIR file to write, with no extension</param>
        public GenerationContext(QsCompilation comp, string outputFile)
        {
            this.Compilation = comp;
            this.outputFileName = outputFile;

            this.libContext = Llvm.NET.Interop.Library.InitializeLLVM();
            Llvm.NET.Interop.Library.RegisterNative();
            this.CurrentContext = new Context();
            this.CurrentBuilder = new InstructionBuilder(this.CurrentContext);
            this.BridgeModule = this.CurrentContext.CreateBitcodeModule("bridge");
            this.ValueStack = new Stack<Value>();
            this.ExpressionTypeStack = new Stack<ResolvedType>();
            //this.QubitReleaseStack = new Stack<List<Value>>();
            this.ScopeMgr = new ScopeManager(this);

            #region Standard types
            this.QirInt = this.CurrentContext.Int64Type;
            this.QirDouble = this.CurrentContext.DoubleType;
            this.QirBool = this.CurrentContext.BoolType;
            this.qirResultStruct = this.CurrentContext.CreateStructType("Result");
            this.QirResult = this.qirResultStruct.CreatePointerType();
            //this.QirPauli = this.CurrentContext.CreateStructType("Pauli", false, this.CurrentContext.Int8Type);
            this.QirPauli = this.CurrentContext.GetIntType(2);
            var qirQubitStruct = this.CurrentContext.CreateStructType("Qubit");
            this.QirQubit = qirQubitStruct.CreatePointerType();
            this.QirRange = this.CurrentContext.CreateStructType("Range", false, this.CurrentContext.Int64Type, 
                this.CurrentContext.Int64Type,  this.CurrentContext.Int64Type);
            var qirStringStruct = this.CurrentContext.CreateStructType("String");
            this.QirString = qirStringStruct.CreatePointerType();
            var qirBigIntStruct = this.CurrentContext.CreateStructType("BigInt");
            this.QirBigInt = qirBigIntStruct.CreatePointerType();
            // It would be nice if TupleHeader were opaque, but it can't be because it appears directly
            // (that is, not as a pointer) in tuple structures, but would have unknown length if it were opaque.
            this.QirTupleHeader = this.CurrentContext.CreateStructType("TupleHeader", false, 
                this.CurrentContext.Int32Type, this.CurrentContext.Int32Type);
            this.QirTuplePointer = this.QirTupleHeader.CreatePointerType();
            var qirArrayStruct = this.CurrentContext.CreateStructType("Array");
            this.QirArray = qirArrayStruct.CreatePointerType();
            var qirCallableStruct = this.CurrentContext.CreateStructType("Callable");
            this.QirCallable = qirCallableStruct.CreatePointerType();
            #endregion

            // Initialization
            this.StandardWrapperSignature = this.CurrentContext.GetFunctionType(this.CurrentContext.VoidType, 
                    new[] { this.QirTuplePointer, this.QirTuplePointer, this.QirTuplePointer });
        }
        #endregion

        #region Module management
        /// <summary>
        /// Starts a new LLVM module.
        /// By default we generate all of the QIR for a complete Q# program into a single module.
        /// </summary>
        public void StartNewModule()
        {
            this.CurrentModule = this.CurrentContext.CreateBitcodeModule();

            this.QirResultZero = this.CurrentModule.AddGlobal(this.QirResult, "ResultZero");
            this.QirResultOne = this.CurrentModule.AddGlobal(this.QirResult, "ResultOne");
            this.QirPauliI = this.CurrentModule.AddGlobal(this.QirPauli, true, Linkage.External,
                this.CurrentContext.CreateConstant(this.QirPauli, 0, false), "PauliI");
            this.QirPauliX = this.CurrentModule.AddGlobal(this.QirPauli, true, Linkage.External,
                this.CurrentContext.CreateConstant(this.QirPauli, 1, false), "PauliX");
            this.QirPauliY = this.CurrentModule.AddGlobal(this.QirPauli, true, Linkage.External,
                this.CurrentContext.CreateConstant(this.QirPauli, 3, false), "PauliY");
            this.QirPauliZ = this.CurrentModule.AddGlobal(this.QirPauli, true, Linkage.External,
                this.CurrentContext.CreateConstant(this.QirPauli, 2, false), "PauliZ");
            this.QirEmptyRange = this.CurrentModule.AddGlobal(this.QirRange, true, Linkage.Internal,
                this.CurrentContext.CreateNamedConstantStruct(this.QirRange as IStructType, 
                    this.CurrentContext.CreateConstant(0L),
                    this.CurrentContext.CreateConstant(1L), 
                    this.CurrentContext.CreateConstant(-1L)),
                "EmptyRange");

            this.InitializeRuntimeLibrary();
            this.RegisterQuantumInstructions();
        }

        /// <summary>
        /// Initializes the QIR runtime library.
        /// See <see cref="FunctionLibrary"/>.
        /// </summary>
        private void InitializeRuntimeLibrary()
        {
            this.runtimeLibrary = new FunctionLibrary(this.CurrentModule, 
                s => SpecialFunctionName(SpecialFunctionKind.Runtime, s));

            #region int library functions
            this.runtimeLibrary.AddFunction("int_power", this.QirInt, this.QirInt, this.QirInt);
            #endregion
            #region Standard result library functions
            //this.runtimeLibrary.AddFunction("result_create", this.QirResult);
            //this.runtimeLibrary.AddFunction("result_copy", this.QirResult, this.QirResult);
            this.runtimeLibrary.AddFunction("result_reference", this.CurrentContext.VoidType, this.QirResult);
            this.runtimeLibrary.AddFunction("result_unreference", this.CurrentContext.VoidType, this.QirResult);
            this.runtimeLibrary.AddFunction("result_equal", this.CurrentContext.BoolType, this.QirResult, this.QirResult);
            #endregion
            #region Standard string library functions
            this.runtimeLibrary.AddFunction("string_create", this.QirString, this.CurrentContext.Int32Type,
                this.CurrentContext.Int8Type.CreateArrayType(0));
            this.runtimeLibrary.AddFunction("string_reference", this.CurrentContext.VoidType, this.QirString);
            this.runtimeLibrary.AddFunction("string_unreference", this.CurrentContext.VoidType, this.QirString);
            this.runtimeLibrary.AddFunction("string_concatenate", this.QirString, this.QirString, this.QirString);
            this.runtimeLibrary.AddFunction("string_equal", this.CurrentContext.BoolType, this.QirString, this.QirString);
            #endregion
            #region To-string library functions
            this.runtimeLibrary.AddFunction("bigint_to_string", this.QirString, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bool_to_string", this.QirString, this.CurrentContext.BoolType);
            this.runtimeLibrary.AddFunction("double_to_string", this.QirString, this.CurrentContext.DoubleType);
            this.runtimeLibrary.AddFunction("int_to_string", this.QirString, this.CurrentContext.Int64Type);
            this.runtimeLibrary.AddFunction("pauli_to_string", this.QirString, this.QirPauli);
            this.runtimeLibrary.AddFunction("qubit_to_string", this.QirString, this.QirQubit);
            this.runtimeLibrary.AddFunction("range_to_string", this.QirString, this.QirRange);
            this.runtimeLibrary.AddFunction("result_to_string", this.QirString, this.QirResult);
            #endregion
            #region Standard bigint library functions
            this.runtimeLibrary.AddFunction("bigint_create_i64", this.QirBigInt, this.CurrentContext.Int64Type);
            this.runtimeLibrary.AddFunction("bigint_create_array", this.QirBigInt, this.CurrentContext.Int32Type,
                this.CurrentContext.Int8Type.CreateArrayType(0));
            this.runtimeLibrary.AddFunction("bigint_reference", this.CurrentContext.VoidType, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_unreference", this.CurrentContext.VoidType, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_negate", this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_add", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_subtract", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_multiply", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_divide", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_modulus", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_power", this.QirBigInt, this.QirBigInt,
                this.CurrentContext.Int32Type);
            this.runtimeLibrary.AddFunction("bigint_bitand", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_bitor", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_bitxor", this.QirBigInt, this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_bitnot", this.QirBigInt, this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_shiftleft", this.QirBigInt, this.QirBigInt,
                this.CurrentContext.Int64Type);
            this.runtimeLibrary.AddFunction("bigint_shiftright", this.QirBigInt, this.QirBigInt,
                this.CurrentContext.Int64Type);
            this.runtimeLibrary.AddFunction("bigint_equal", this.CurrentContext.BoolType, this.QirBigInt,
                this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_greater", this.CurrentContext.BoolType, this.QirBigInt,
                this.QirBigInt);
            this.runtimeLibrary.AddFunction("bigint_greater_eq", this.CurrentContext.BoolType, this.QirBigInt,
                this.QirBigInt);
            #endregion
            #region Standard tuple library functions
            this.runtimeLibrary.AddFunction("tuple_init_stack", this.CurrentContext.VoidType, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("tuple_init_heap", this.CurrentContext.VoidType, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("tuple_create", this.QirTuplePointer, this.CurrentContext.Int64Type);
            this.runtimeLibrary.AddFunction("tuple_reference", this.CurrentContext.VoidType, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("tuple_unreference", this.CurrentContext.VoidType, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("tuple_is_writable", this.CurrentContext.BoolType, this.QirTuplePointer);
            #endregion
            #region Standard array library functions
            this.runtimeLibrary.AddVarargsFunction("array_create", this.QirArray, this.CurrentContext.Int32Type, 
                this.CurrentContext.Int32Type);
            this.runtimeLibrary.AddVarargsFunction("array_get_element_ptr", 
                this.CurrentContext.Int8Type.CreatePointerType(), this.QirArray);
            // TODO: figure out how to call a varargs function and get rid of these two functions
            this.runtimeLibrary.AddFunction("array_create_1d", this.QirArray, this.CurrentContext.Int32Type, 
                this.CurrentContext.Int64Type);
            this.runtimeLibrary.AddFunction("array_get_element_ptr_1d", this.CurrentContext.Int8Type.CreatePointerType(),
                this.QirArray, this.CurrentContext.Int64Type);
            this.runtimeLibrary.AddFunction("array_get_length", this.CurrentContext.Int64Type, this.QirArray,
                this.CurrentContext.Int32Type);
            this.runtimeLibrary.AddFunction("array_reference", this.CurrentContext.VoidType, this.QirArray);
            this.runtimeLibrary.AddFunction("array_unreference", this.CurrentContext.VoidType, this.QirArray);
            this.runtimeLibrary.AddFunction("array_copy", this.QirArray, this.QirArray);
            this.runtimeLibrary.AddFunction("array_concatenate", this.QirArray, this.QirArray, this.QirArray);
            this.runtimeLibrary.AddFunction("array_slice", this.QirArray, this.QirArray, 
                this.CurrentContext.Int32Type, this.QirRange);
            #endregion
            #region Callable library functions
            this.runtimeLibrary.AddFunction("callable_create", this.QirCallable, 
                this.StandardWrapperSignature.CreatePointerType().CreateArrayType(4).CreatePointerType(),
                this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("callable_invoke", this.CurrentContext.VoidType, this.QirCallable,
                this.QirTuplePointer, this.QirTuplePointer);
            this.runtimeLibrary.AddFunction("callable_copy", this.QirCallable, this.QirCallable);
            this.runtimeLibrary.AddFunction("callable_make_adjoint", this.CurrentContext.VoidType, this.QirCallable);
            this.runtimeLibrary.AddFunction("callable_make_controlled", this.CurrentContext.VoidType, this.QirCallable);
            this.runtimeLibrary.AddFunction("callable_reference", this.CurrentContext.VoidType, this.QirCallable);
            this.runtimeLibrary.AddFunction("callable_unreference", this.CurrentContext.VoidType, this.QirCallable);
            #endregion
            #region Standard qubit library functions
            this.runtimeLibrary.AddFunction("qubit_allocate", this.QirQubit);
            this.runtimeLibrary.AddFunction("qubit_allocate_array", this.QirArray, this.CurrentContext.Int64Type);
            this.runtimeLibrary.AddFunction("qubit_release", this.CurrentContext.VoidType, this.QirQubit);
            this.runtimeLibrary.AddFunction("qubit_release_array", this.CurrentContext.VoidType, this.QirArray);
            #endregion
            #region Other library functions
            this.runtimeLibrary.AddFunction("fail", this.CurrentContext.VoidType, this.QirString);
            this.runtimeLibrary.AddFunction("message", this.CurrentContext.VoidType, this.QirString);
            #endregion
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
        /// Completes the module and writes the QIR to the output file.
        /// </summary>
        public void EndModule()
        {
            this.GenerateQueuedWrappers();

            //var outFileName = Path.ChangeExtension(this.CurrentModule.SourceFileName, "ll");
            if (this.CurrentModule.Verify(out string validationErrors))
            {
                File.WriteAllText($"{outputFileName}.log", "No errors\n");
            }
            else
            {
                File.WriteAllText($"{outputFileName}.log", $"LLVM errors:\n{validationErrors}");
            }

            if (!this.CurrentModule.WriteToTextFile($"{outputFileName}.ll", out string errorMessage))
            {
                throw new IOException(errorMessage);
            }

            // Generate the clang wrappers for the runtime library that were used, if requested
            if (this.GenerateClangWrappers)
            {
                foreach (var kvp in this.runtimeLibrary)
                {
                    this.GenerateClangWrapper(kvp.Value, kvp.Key);
                }

                var bridgeFileName = Path.Combine(Path.GetDirectoryName(outputFileName), "bridge.ll");

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
        public void StartNewSpecialization()
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
                    var item = arg as QsArgumentTuple.QsTupleItem;
                    var typeRef = this.BuildArgItemTupleType(item);
                    return new ITypeRef[] { typeRef };
                }
            }

            var name = CallableName(spec.Parent.Namespace.Value, spec.Parent.Name.Value, spec.Kind);
            var returnTypeRef = spec.Signature.ReturnType.Resolution.IsUnitType
                ? this.CurrentContext.VoidType
                : this.LlvmTypeFromQsharpType(spec.Signature.ReturnType);
            var argTypeRefs = ArgTupleToTypes(argTuple);
            var signature = this.CurrentContext.GetFunctionType(returnTypeRef, argTypeRefs);
            return this.CurrentModule.AddFunction(name, signature);
        }

        /// <summary>
        /// Generates the start of the definition for a specialization in the current module.
        /// Specifically, an entry block for the function is created, and the function's arguments are given names.
        /// </summary>
        /// <param name="spec">The specialization</param>
        /// <param name="argTuple">The specialization's argument tuple</param>
        public void GenerateFunctionHeader(QsSpecialization spec, QsArgumentTuple argTuple)
        {
            //            void ProcessSubArg(QsArgumentTuple arg, Value val)
            //            {
            //                switch (arg)
            //                {
            //                    case QsArgumentTuple.QsTuple tuple:
            //                        var items = tuple.Item;
            //                        var n = items.Length;
            //                        if (n > 0)
            //                        {
            //                            ITypeRef tupleTypeRef = this.BuildArgTupleType(arg);
            //                            // Convert value from TuplePointer to the proper type
            //                            Value asStructPointer = this.CurrentBuilder.BitCast(val,
            //                                tupleTypeRef.CreatePointerType());
            //                            var indices = new Value[] { this.CurrentContext.CreateConstant(0L),
            //                                                    this.CurrentContext.CreateConstant(1) };
            //                            for (var i = 0; i < n; i++)
            //                            {
            //                                indices[1] = this.CurrentContext.CreateConstant(i + 1);
            //                                Value ptr = this.CurrentBuilder.GetElementPtr(tupleTypeRef, asStructPointer, indices);
            //#pragma warning disable CS0618 // Type or member is obsolete -- computing the type that ptr points to is tricky, so we just rely on it's .NativeType
            //                                var subVal = this.CurrentBuilder.Load(ptr);
            //#pragma warning restore CS0618 // Type or member is obsolete
            //                                ProcessSubArg(tuple.Item[i], subVal);
            //                            }
            //                        }
            //                        break;
            //                    case QsArgumentTuple.QsTupleItem item:
            //                        var argName = (item.Item.VariableName as QsLocalSymbol.ValidName).Item.Value;
            //                        namesInScope.Peek().Add(argName, (val, false));
            //                        break;
            //                }
            //            }

            //            void ProcessTopLevelArg(QsArgumentTuple arg, int index)
            //            {
            //                // Register the parameter name
            //                var argName = arg switch
            //                {
            //                    QsArgumentTuple.QsTupleItem item => (item.Item.VariableName as QsLocalSymbol.ValidName).Item.Value,
            //                    _ => this.GenerateUniqueName("arg")
            //                };
            //                this.CurrentFunction.Parameters[index].Name = argName;
            //                namesInScope.Peek().Add(argName, (this.CurrentFunction.Parameters[index], false));

            //                // If the arg is a sub-tuple, process it
            //                if (arg.IsQsTuple)
            //                {
            //                    ProcessSubArg(arg, this.CurrentFunction.Parameters[index]);
            //                }
            //            }

            //            this.CurrentFunction = this.RegisterFunction(spec, argTuple);
            //            this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
            //            this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);

            //            this.namesInScope.Push(new Dictionary<string, (Value, bool)>());
            //            var topLevelArgs = argTuple switch
            //            {
            //                QsArgumentTuple.QsTuple tuple => tuple.Item,
            //                _ => new QsArgumentTuple[] { argTuple }.ToImmutableArray()
            //            };
            //            var i = 0;
            //            foreach (var arg in topLevelArgs)
            //            {
            //                ProcessTopLevelArg(arg, i);
            //                i++;
            //            }
            IEnumerable<string> ArgTupleToNames(QsArgumentTuple arg, Queue<(string, QsArgumentTuple)> tupleQueue)
            {
                string LocalVarName(QsArgumentTuple v)
                {
                    if (v is QsArgumentTuple.QsTupleItem item)
                    {
                        return (item.Item.VariableName as QsLocalSymbol.ValidName).Item.Value;
                    }
                    else
                    {
                        var name = this.GenerateUniqueName("arg");
                        tupleQueue.Enqueue((name, v));
                        return name;
                    }
                }

                if (arg is QsArgumentTuple.QsTuple tuple)
                {
                    return tuple.Item.Select(item => LocalVarName(item));
                }
                else
                {
                    return new string[] { LocalVarName(arg) };
                }
            }

            this.CurrentFunction = this.RegisterFunction(spec, argTuple);
            this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
            this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);

            namesInScope.Push(new Dictionary<string, (Value, bool)>());
            var pendingTuples = new Queue<(string, QsArgumentTuple)>();
            var i = 0;
            foreach (var argName in ArgTupleToNames(argTuple, pendingTuples))
            {
                this.CurrentFunction.Parameters[i].Name = argName;
                namesInScope.Peek().Add(argName, (this.CurrentFunction.Parameters[i], false));
                i++;
            }

            // Now break up input tuples
            while (pendingTuples.TryDequeue(out (string, QsArgumentTuple) tuple))
            {
                var (tupleArgName, tupleArg) = tuple;
                this.PushNamedValue(tupleArgName);
                var tupleValue = this.ValueStack.Pop();
                int idx = 1;
                foreach (var argName in ArgTupleToNames(tupleArg, pendingTuples))
                {
                    var elementPointer = this.GetTupleElementPointer((tupleValue.NativeType as IPointerType).ElementType, tupleValue, idx);
                    var element = this.CurrentBuilder.Load((elementPointer.NativeType as IPointerType).ElementType, elementPointer);
                    namesInScope.Peek().Add(argName, (element, false));
                    idx++;
                }
            }
        }

        /// <summary>
        /// Generates the default constructor for a Q# user-defined type.
        /// This routine generates all the code for the constructor, not just the header.
        /// </summary>
        /// <param name="udt">The Q# user-defined type</param>
        public void GenerateConstructor(QsCustomType udt)
        {
            var name = CallableName(udt.FullName.Namespace.Value, udt.FullName.Name.Value, QsSpecializationKind.QsBody);

            var args = udt.Type.Resolution switch
            {
                QsResolvedTypeKind.TupleType tup => tup.Item.Select(LlvmTypeFromQsharpType).ToArray(),
                _ when udt.Type.Resolution.IsUnitType => new ITypeRef[0],
                _ => new ITypeRef[] { LlvmTypeFromQsharpType(udt.Type) }
            };
            var udtTupleType = this.CurrentContext.CreateStructType(false, this.QirTupleHeader, args);
            var udtPointerType = args.Length > 0 ? udtTupleType.CreatePointerType() : this.QirTuplePointer;
            var signature = this.CurrentContext.GetFunctionType(udtPointerType, args);

            this.StartNewSpecialization();
            this.CurrentFunction = this.CurrentModule.AddFunction(name, signature);
            this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
            this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);

            // An easy case -- (), a marker UDT
            if (args.Length == 0)
            {
                this.CurrentBuilder.Return(udtPointerType.GetNullValue());
            }
            else
            {
                var tuple = this.CreateTupleForType(udtTupleType);
                var udtTuple = this.CurrentBuilder.BitCast(tuple, udtPointerType);

                for (int i = 0; i < args.Length; i++)
                {
                    this.CurrentFunction.Parameters[i].Name = $"arg{i}";
                    var itemPtr = this.GetTupleElementPointer(udtTupleType, udtTuple, i + 1);
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
        public void EndSpecialization()
        {
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

            this.ScopeMgr.CloseScope();
            if ((this.CurrentBlock.Instructions.Count() == 0) && !HasAPredecessor(this.CurrentBlock))
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
            if (this.TryGetFunction(namespaceName, name, kind, out IrFunction function))
            {
                return function;
            }
            // Otherwise, we need to find the function's callable to get the signature,
            // and then register the function
            if (this.TryFindGlobalCallable(namespaceName, name, out QsCallable callable))
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
        public bool TryGetFunction(string namespaceName, string name, QsSpecializationKind kind, out IrFunction function)
        {
            var fullName = CallableName(namespaceName, name, kind);
            foreach (var func in this.CurrentModule.Functions)
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
        public bool TryGetWrapper(QsCallable callable, QsSpecializationKind kind, out IrFunction function)
        {
            var fullName = CallableWrapperName(callable, kind);
            foreach (var func in this.CurrentModule.Functions)
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
        public void SetCurrentBlock(BasicBlock b)
        {
            this.CurrentBlock = b;
            this.CurrentBuilder = new InstructionBuilder(b);
        }

        /// <summary>
        /// Creates a new basic block and adds it to the current function immediately after the current block.
        /// </summary>
        /// <param name="name">The base name for the new block; a counter will be appended to ensure uniqueness</param>
        /// <returns>The new block</returns>
        public BasicBlock AddBlockAfterCurrent(string name)
        {
            var flag = false;
            BasicBlock next = null;
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
            this.Transformation.Types.OnType(resolvedType);
            return this.BuiltType;
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
                var elementTypes = tuple.Item.Select(this.LlvmTypeFromQsharpType).ToArray();
                return this.CurrentContext.CreateStructType(false, this.QirTupleHeader, elementTypes);
            }
            else
            {
                this.Transformation.Types.OnType(resolvedType);
                return this.CurrentContext.CreateStructType(false, this.QirTupleHeader, this.BuiltType);
            }
        }

        /// <summary>
        /// Computes the size in bytes of an LLVM type as an LLVM value.
        /// If the type isn't a simple pointer, integer, or double, we compute it using a standard LLVM idiom.
        /// </summary>
        /// <param name="t">The LLVM type to compute the size of</param>
        /// <param name="b">The builder to use to generate the struct size computation, if needed</param>
        /// <returns>An LLVM value containing the size of the type in bytes</returns>
        public Value ComputeSizeForType(ITypeRef t, InstructionBuilder b)
        {
            if (t.IsInteger)
            {
                return this.CurrentContext.CreateConstant((long)((t.IntegerBitWidth + 7) / 8));
            }
            else if (t.IsDouble)
            {
                return this.CurrentContext.CreateConstant(8L);
            }
            else if (t.IsPointer)
            {
                // We assume 64-bit address space
                return this.CurrentContext.CreateConstant(8L);
            }
            else
            {
                // Everything else we let getelementptr compute for us
                var basePointer = Constant.ConstPointerToNullFor(t.CreatePointerType());
                var firstPtr = b.GetElementPtr(t, basePointer,
                    new[] { this.CurrentContext.CreateConstant(0) }); ;
                var first = b.PointerToInt(firstPtr, this.CurrentContext.Int64Type);
                var secondPtr = b.GetElementPtr(t, basePointer,
                    new[] { this.CurrentContext.CreateConstant(1) });
                var second = b.PointerToInt(secondPtr, this.CurrentContext.Int64Type);
                return CurrentBuilder.Sub(second, first);
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
        public string GenerateUniqueName(string prefix)
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
            namesInScope.Peek().Add(name, (value, isMutable));
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
            foreach (var dict in namesInScope)
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
            foreach (var dict in namesInScope)
            {
                if (dict.TryGetValue(name, out (Value, bool) item))
                {
                    if (item.Item2)
                    {
                        // Mutable, so the value is a pointer; we need to load what it's pointing to
                        var typeRef = (item.Item1.NativeType as IPointerType).ElementType;
                        this.ValueStack.Push(this.CurrentBuilder.Load(typeRef, item.Item1));
                    }
                    else
                    {
                        this.ValueStack.Push(item.Item1);
                    }
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
            namesInScope.Push(new Dictionary<string, (Value, bool)>());
        }

        /// <summary>
        /// Closes the current naming scope by popping it off of the naming scope stack.
        /// </summary>
        public void CloseNamingScope()
        {
            namesInScope.Pop();
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
                case QsArgumentTuple.QsTuple tup:
                    {
                        var items = tup.Item;
                        var elems = items.Select(BuildArgItemTupleType).ToArray();
                        return CurrentContext.CreateStructType(false, QirTupleHeader, elems).CreatePointerType();
                    }

                default:
                    {
                        // Single items get translated to the appropriate LLVM type
                        var item = argItem as QsArgumentTuple.QsTupleItem;
                        return LlvmTypeFromQsharpType(item.Item.Type);
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
                var itemTypeRefs = tuple.Item.Select(BuildArgItemTupleType).ToArray();
                var n = itemTypeRefs.Length;
                if (n == 0)
                {
                    return this.CurrentContext.VoidType;
                }
                else
                {
                    return this.CurrentContext.CreateStructType(false, this.QirTupleHeader, itemTypeRefs);
                }
            }
            else
            {
                var item = arg as QsArgumentTuple.QsTupleItem;
                var itemTypeRef = this.LlvmTypeFromQsharpType(item.Item.Type);
                return this.CurrentContext.CreateStructType(false, this.QirTupleHeader, itemTypeRef);
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
        /// Returns a pointer to a tuple element.
        /// This is a thin wrapper around the LLVM GEP instruction.
        /// </summary>
        /// <param name="t">The type of the tuple structure (not the type of the pointer!).</param>
        /// <param name="tuple">The pointer to the tuple. This will be cast to the proper type if necessary.</param>
        /// <param name="index">The element's index into the tuple. The tuple header is index 0, the first data item is index 1.</param>
        /// <param name="b">An optional InstructionBuilder to create these instructions on. The current builder is used as the default.</param>
        /// <returns></returns>
        internal Value GetTupleElementPointer(ITypeRef t, Value tuple, int index, InstructionBuilder b = null)
        {
            Value[] indices = new Value[] {
                        this.CurrentContext.CreateConstant(0L),
                        this.CurrentContext.CreateConstant(index)
                    };
            var builder = b ?? this.CurrentBuilder;
            var typedTuple = tuple.NativeType == t.CreatePointerType()
                ? tuple
                : builder.BitCast(tuple, t.CreatePointerType());
            var elementPointer = builder.GetElementPtr(t, typedTuple, indices);
            return elementPointer;
        }

        /// <summary>
        /// Creates and returns a deep copy of a tuple.
        /// By default this uses the current builder, but an alternate builder may be provided.
        /// </summary>
        /// <param name="original">The original tuple as an LLVM TupleHeader pointer</param>
        /// <param name="t">The Q# type of the tuple</param>
        /// <param name="b">(optional) The instruction builder to use; the current builder is used if not provided</param>
        /// <returns>The new copy, as an LLVM value containing a TupleHeader pointer</returns>
        internal Value DeepCopyTuple(Value original, ResolvedType t, InstructionBuilder b = null)
        {
            var builder = b ?? this.CurrentBuilder;
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
                    var originalElementPointer = this.GetTupleElementPointer(originalTypeRef, typedOriginal, i + 1, builder);
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
                    var copyElementPointer = this.GetTupleElementPointer(originalTypeRef, typedCopy, i + 1, builder);
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
        internal Value DeepCopyUDT(Value original, ResolvedType t, InstructionBuilder b = null)
        {
            if ((t.Resolution is QsResolvedTypeKind.UserDefinedType tt) &&
                this.TryFindUDT(tt.Item.Namespace.Value, tt.Item.Name.Value, out QsCustomType udt))
            {
                if (udt.Type.Resolution.IsTupleType)
                {
                    return this.DeepCopyTuple(original, udt.Type, b);
                }
                else if (udt.Type.Resolution.IsArrayType)
                {
                    var builder = b ?? this.CurrentBuilder;
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
                    ResolvedExpression.ArrayItem arr => ItemRequiresCopying(arr.Item1),
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
        internal Value GetWritableCopy(TypedExpression ex, InstructionBuilder b = null)
        {
            // Evaluating the input always happens on the current builder
            this.Transformation.Expressions.OnTypedExpression(ex);
            var item = this.ValueStack.Pop();
            var builder = b ?? this.CurrentBuilder;
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
                        this.CurrentContext.CreateConstant(0L),
                        this.CurrentContext.CreateConstant(position)
                    };
                var elementPointer = this.CurrentBuilder.GetElementPtr(structType, pointerToStruct, indices);
                var castValue = fillValue.NativeType == this.QirTuplePointer
                    ? this.CurrentBuilder.BitCast(fillValue, (structType as IStructType).Members[position])
                    : fillValue;
                this.CurrentBuilder.Store(castValue, elementPointer);
            }

            void FillItem(ITypeRef structType, Value pointerToStruct, TypedExpression fillExpr, int position)
            {
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
                            var subTupleTypeRef = (this.LlvmTypeFromQsharpType(items[i].ResolvedType) as IPointerType).ElementType;
                            var subTupleAsTuplePointer = this.CreateTupleForType(subTupleTypeRef);
                            var subTupleAsTypedPointer = this.CurrentBuilder.BitCast(subTupleAsTuplePointer, subTupleTypeRef.CreatePointerType());
                            FillStructSlot(tupleTypeRef, tupleToFillPointer, subTupleAsTypedPointer, i + 1);
                            this.FillTuple(subTupleAsTypedPointer, items[i]);
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
                if (source.Expression.IsUnitValue)
                {
                    // Nothing to do, so bail
                    return;
                }
                if (destination is QsArgumentTuple.QsTuple tuple)
                {
                    var items = tuple.Item;
                    if ((items.Length == 1) && !source.Expression.IsValueTuple)
                    {
                        MapTupleInner(source, items[0], assignmentQueue);
                        return;
                    }
                    var srcItems = (source.Expression as ResolvedExpression.ValueTuple).Item;
                    foreach (var (ex, ti) in srcItems.Zip(items, (ex, ti) => (ex, ti)))
                    {
                        MapTupleInner(ex, ti, assignmentQueue);
                    }
                }
                else
                {
                    this.Transformation.Expressions.OnTypedExpression(source);
                    var arg = destination as QsArgumentTuple.QsTupleItem;
                    var baseName = (arg.Item.VariableName as QsLocalSymbol.ValidName).Item.Value;
                    var value = this.ValueStack.Pop();
                    assignmentQueue.Add((baseName, value));
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
            var key = $"{CleanNamespaceName(callable.FullName.Namespace.Value)}__{callable.FullName.Name.Value}";
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
                    if (callable.Specializations.Any(spec => spec.Kind == kind && spec.Implementation.IsProvided))
                    {
                        var f = this.CurrentModule.AddFunction(CallableWrapperName(callable, kind),
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
                var table = this.CurrentModule.AddGlobal(array.NativeType, true, Linkage.DllExport, array, key);
                this.wrapperQueue.Add(key, (callable, table));
                return table;
            }
        }

        private void GenerateQueuedWrappers()
        {
            bool GenerateWrapperHeader(QsCallable callable, QsSpecialization spec)
            {
                if (this.TryGetWrapper(callable, spec.Kind, out IrFunction func))
                {
                    this.CurrentFunction = func;
                    this.CurrentFunction.Parameters[0].Name = "capture-tuple";
                    this.CurrentFunction.Parameters[1].Name = "arg-tuple";
                    this.CurrentFunction.Parameters[2].Name = "result-tuple";
                    this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
                    this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);
                    namesInScope.Push(new Dictionary<string, (Value, bool)>());
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // Generate the code that decomposes the tuple back into the named arguments
            // Note that we don't want to recurse here!!.
            List<Value> GenerateArgTupleDecomposition(QsArgumentTuple arg, Value value, QsSpecializationKind kind)
            {
                Value BuildLoadForArg(QsArgumentTuple arg, Value value)
                {
                    ITypeRef argTypeRef = arg is QsArgumentTuple.QsTupleItem item
                        ? this.BuildArgItemTupleType(item)
                        : this.BuildArgTupleType(arg).CreatePointerType();
                    // value is a pointer to the argument
                    Value actualArg = this.CurrentBuilder.Load(argTypeRef, value);
                    return actualArg;
                }

                // Controlled specializations have different signatures, so adjust what we have
                if (kind.IsQsControlled || kind.IsQsControlledAdjoint)
                {
                    var ctlArg = new LocalVariableDeclaration<QsLocalSymbol>(
                        QsLocalSymbol.NewValidName(NonNullable<string>.New(this.GenerateUniqueName("ctls"))),
                        ResolvedType.New(QsResolvedTypeKind.NewArrayType(ResolvedType.New(QsResolvedTypeKind.Qubit))),
                        new InferredExpressionInformation(false, false),
                        QsNullable<Position>.Null,
                        DataTypes.Range.Zero);
                    var ctlArgs = new QsArgumentTuple[] { QsArgumentTuple.NewQsTupleItem(ctlArg), arg };
                    arg = QsArgumentTuple.NewQsTuple(ctlArgs.ToImmutableArray());
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
                        var indices = new Value[] { this.CurrentContext.CreateConstant(0L),
                                                    this.CurrentContext.CreateConstant(1) };
                        for (var i = 0; i < n; i++)
                        {
                            indices[1] = this.CurrentContext.CreateConstant(i + 1);
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
                        new Value[] { this.CurrentContext.CreateConstant(0L), this.CurrentContext.CreateConstant(item) });
                    this.CurrentBuilder.Store(resultValue, resultPointer);
                }
            }

            Value GenerateBaseMethodCall(QsCallable callable, QsSpecialization spec, List<Value> args)
            {
                if (this.TryGetFunction(callable.FullName.Namespace.Value, callable.FullName.Name.Value, spec.Kind,
                    out IrFunction func))
                {
                    return this.CurrentBuilder.Call(func, args.ToArray());
                }
                else
                {
                    return Constant.UndefinedValueFor(this.QirTuplePointer);
                }
            }

            foreach (var kvp in this.wrapperQueue)
            {
                var callable = kvp.Value.Item1;
                foreach (var spec in callable.Specializations)
                {
                    if (spec.Implementation.IsProvided && GenerateWrapperHeader(callable, spec))
                    {
                        Value argTupleValue = this.CurrentFunction.Parameters[1];
                        //var argTuple = (spec.Implementation as SpecializationImplementation.Provided).Item1;
                        var argList = GenerateArgTupleDecomposition(callable.ArgumentTuple, argTupleValue, spec.Kind);
                        var result = GenerateBaseMethodCall(callable, spec, argList);
                        PopulateResultTuple(callable.Signature.ReturnType, result, this.CurrentFunction.Parameters[2], 1);
                        this.CurrentBuilder.Return();
                    }
                }
            }
        }
        #endregion

        #region Global callable and UDT utilities
        /// <summary>
        /// Tries to find a global Q# callable in the current compilation.
        /// </summary>
        /// <param name="nsName">The callable's namespace</param>
        /// <param name="name">The callable's name</param>
        /// <param name="callable">The Q# callable, if found</param>
        /// <returns>true if the callable is found, false if not</returns>
        public bool TryFindGlobalCallable(string nsName, string name, out QsCallable callable)
        {
            callable = null;

            foreach (var ns in this.Compilation.Namespaces)
            {
                if (ns.Name.Value == nsName)
                {
                    foreach (var element in ns.Elements)
                    {
                        if (element is QsNamespaceElement.QsCallable c)
                        {
                            if (c.GetFullName().Name.Value == name)
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
        public bool TryFindUDT(string nsName, string name, out QsCustomType udt)
        {
            udt = null;

            foreach (var ns in this.Compilation.Namespaces)
            {
                if (ns.Name.Value == nsName)
                {
                    foreach (var element in ns.Elements)
                    {
                        if (element is QsNamespaceElement.QsCustomType t)
                        {
                            if (t.GetFullName().Name.Value == name)
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
        /// Tells whether or not a Q# callable has s specific built-in attribute.
        /// </summary>
        /// <param name="callable">The Q# callable</param>
        /// <param name="coreAttr">The attribute to look for</param>
        /// <param name="attr">The attribute, if found</param>
        /// <returns>true if the attribute is found, false if not</returns>
        private static bool CallableHasCoreAttribute(QsCallable callable, BuiltIn coreAttr, out QsDeclarationAttribute attr)
        {
            attr = null;
            foreach (var attribute in callable.Attributes)
            {
                if (attribute.TypeId.IsValue &&
                    (attribute.TypeId.Item.Namespace.Value == BuiltIn.CoreNamespace.Value) &&
                    (attribute.TypeId.Item.Name.Value == coreAttr.FullName.Name.Value))
                {
                    attr = attribute;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tells whether or not a global callable is a quantum instruction.
        /// For this purpose, quantum instructions are any operations that have the "Intrinsic" attribute.
        /// </summary>
        /// <param name="nsName">The callable's namespace</param>
        /// <param name="name">The callable's name</param>
        /// <param name="attr">The Intrinsic attribute, if found</param>
        /// <returns>true if the callable is a quantum instruction, false if not</returns>
        public bool IsQuantumInstruction(string nsName, string name, out QsDeclarationAttribute attr)
        {
            attr = null;
            if (this.TryFindGlobalCallable(nsName, name, out QsCallable callable))
            {
                return CallableHasCoreAttribute(callable, BuiltIn.Intrinsic, out attr);
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
        public bool IsInlined(TypedExpression method, out QsCallable callable)
        {
            callable = null;
            if ((method.Expression is ResolvedExpression.Identifier id) && id.Item1.IsGlobalCallable)
            {
                Identifier.GlobalCallable c = id.Item1 as Identifier.GlobalCallable;
                var nsName = c.Item.Namespace.Value;
                var name = c.Item.Name.Value;
                if (this.TryFindGlobalCallable(nsName, name, out callable))
                {
                    return CallableHasCoreAttribute(callable, BuiltIn.Inline, out _);
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
            if ((method.Expression is ResolvedExpression.Identifier id) && id.Item1.IsGlobalCallable)
            {
                Identifier.GlobalCallable callable = id.Item1 as Identifier.GlobalCallable;
                var nsName = callable.Item.Namespace.Value;
                var name = callable.Item.Name.Value;
                if (this.IsQuantumInstruction(nsName, name, out QsDeclarationAttribute attr))
                {
                    // The attribute argument is an un-interpolated string
                    var arg = attr.Argument.Expression;
                    instructionName = (arg is ResolvedExpression.StringLiteral lit) 
                        ? lit.Item1.Value
                        : ((arg as ResolvedExpression.ValueTuple).Item[0].Expression as 
                                ResolvedExpression.StringLiteral).Item1.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Finds and registers all of the quantum instructions in the current compilation.
        /// For this purpose, quantum instructions are any operations that have the "Intrinsic" attribute.
        /// <br/><br/>
        /// In addition, clang-compatible wrappers are generated for all of the quantum operations.
        /// </summary>
        public void RegisterQuantumInstructions()
        {
            this.quantumLibrary = new FunctionLibrary(this.CurrentModule,
                s => SpecialFunctionName(SpecialFunctionKind.QuantumInstruction, s));

            foreach (var ns in this.Compilation.Namespaces)
            {
                foreach (var element in ns.Elements)
                {
                    if ((element is QsNamespaceElement.QsCallable c) &&
                        CallableHasCoreAttribute(c.Item, BuiltIn.Intrinsic, out QsDeclarationAttribute attr))
                    {
                        var name = attr.Argument.Expression switch
                        {
                            ResolvedExpression.ValueTuple vt => (vt.Item[0].Expression as ResolvedExpression.StringLiteral).Item1.Value,
                            ResolvedExpression.StringLiteral sl => sl.Item1.Value,
                            _ => "ERROR"
                        };
                    // Special handling for Unit since by default it turns into an empty tuple
                    var returnType = c.Item.Signature.ReturnType.Resolution.IsUnitType
                            ? this.CurrentContext.VoidType
                            : LlvmTypeFromQsharpType(c.Item.Signature.ReturnType);
                        var argTypeArray = (c.Item.Signature.ArgumentType.Resolution is QsResolvedTypeKind.TupleType tuple)
                            ? tuple.Item.Select(LlvmTypeFromQsharpType).ToArray()
                            : new ITypeRef[] { LlvmTypeFromQsharpType(c.Item.Signature.ArgumentType) };
                        this.quantumLibrary.AddFunction(name, returnType, argTypeArray);
                        if (this.GenerateClangWrappers)
                        {
                            var func = this.quantumLibrary.GetFunction(name);
                            this.GenerateClangWrapper(func, name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates a clang-compatible stub implementation for a runtime function or quantum instruction.
        /// Note that clang wrappers go into a separate module from the other QIR code.
        /// </summary>
        /// <param name="func">The function to generate a stub for</param>
        /// <param name="baseName">The function that the stub should call</param>
        /// <param name="m">(optional) The LLVM module in which the stub should be generated</param>
        private void GenerateClangWrapper(IrFunction func, string baseName)
        {
            func = this.BridgeModule.AddFunction(func.Name, func.Signature);

            var mappedResultType = this.MapClangType(func.ReturnType);
            var argTypes = func.Parameters.Select(p => p.NativeType).ToArray();
            var mappedArgTypes = argTypes.Select(this.MapClangType).ToArray();

            var clangFunction = this.BridgeModule.AddFunction(baseName,
                this.CurrentContext.GetFunctionType(mappedResultType, mappedArgTypes));

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
            var clangReturnValue = builder.Call(clangFunction, implArgs);
            if (func.ReturnType == this.CurrentContext.VoidType)
            {
                builder.Return();
            }
            else if (func.ReturnType == mappedResultType)
            {
                builder.Return(clangReturnValue);
            }
            else
            {
                var returnValue = builder.BitCast(clangReturnValue, func.ReturnType);
                builder.Return(returnValue);
            }
        }

        /// <summary>
        /// This type is used by <see cref="GenerateEntryPoint(QsQualifiedName)"/> to map Q# types to more
        /// clang-friendly types.
        /// </summary>
        private class ArgMapping
        {
            public string BaseName { get; set; }
            public ITypeRef ArrayType { get; set; }
            public string ArrayCountName { get; set; }
            public ITypeRef StructType { get; set; }
        }

        // TODO: why do we need both this routine and GenerateClangWrapper?
        /// <summary>
        /// Generates a clang-friendly wrapper around the Q# entry point.
        /// </summary>
        /// <param name="qualifiedName">The namespace-qualified name of the Q# entry point</param>
        public void GenerateEntryPoint(QsQualifiedName qualifiedName)
        {
            // Unfortunately this is different enough from all of the other type mapping we do to require
            // an actual different routine. Blech...
            bool MapToCType(QsArgumentTuple t, List<ITypeRef> typeList, List<string> nameList, List<ArgMapping> mappingList)
            {
                bool changed = false;

                switch (t)
                {
                    case QsArgumentTuple.QsTuple tuple:
                        foreach (QsArgumentTuple inner in tuple.Item)
                        {
                            changed |= MapToCType(inner, typeList, nameList, mappingList);
                        }
                        break;
                    case QsArgumentTuple.QsTupleItem item:
                        var baseName = (item.Item.VariableName as QsLocalSymbol.ValidName).Item.Value;
                        var map = new ArgMapping { BaseName = baseName };
                        switch (item.Item.Type.Resolution)
                        {
                            case QsResolvedTypeKind.ArrayType array:
                                // TODO: Handle multidimensional arrays
                                // TODO: Handle arrays of structs
                                typeList.Add(this.CurrentContext.Int64Type);
                                var elementTypeRef = this.LlvmTypeFromQsharpType(array.Item);
                                typeList.Add(elementTypeRef.CreatePointerType());
                                var arrayCountName = $"{baseName}__count";
                                nameList.Add(arrayCountName);
                                nameList.Add(baseName);
                                map.ArrayCountName = arrayCountName;
                                map.ArrayType = elementTypeRef;
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
                        break;
                    default:
                        break;
                }
                return changed;
            }

            if (this.TryGetFunction(qualifiedName.Namespace.Value, qualifiedName.Name.Value, QsSpecializationKind.QsBody,
                out IrFunction func))
            {
                var epName = $"{qualifiedName.Namespace.Value.Replace('.', '_')}_{qualifiedName.Name.Value}";

                // Check to see if the arg list needs mapping to more C-friendly types
                // TODO: handle complicated return types
                this.TryFindGlobalCallable(qualifiedName.Namespace.Value, qualifiedName.Name.Value, out QsCallable callable);
                var mappedArgList = new List<ITypeRef>();
                var mappedNameList = new List<string>();
                var mappingList = new List<ArgMapping>();
                var arraysToReleaseList = new List<Value>();
                var mappedResultType = MapClangType(func.ReturnType);
                if (MapToCType(callable.ArgumentTuple, mappedArgList, mappedNameList, mappingList) ||
                    (mappedResultType != func.ReturnType))
                {
                    var epFunc = this.CurrentModule.AddFunction(epName, 
                        this.CurrentContext.GetFunctionType(mappedResultType, mappedArgList));
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
                        if (mapping.ArrayType != null)
                        {
                            var elementSize64 = this.ComputeSizeForType(mapping.ArrayType, builder);
                            var elementSize = builder.IntCast(elementSize64, this.CurrentContext.Int32Type, false);
                            var length = namedValues[mapping.ArrayCountName];
                            var array = builder.Call(this.GetRuntimeFunction("array_create_1d"), elementSize, 
                                length);
                            argValueList.Add(array);
                            arraysToReleaseList.Add(array);
                            // Fill in the array if the length is >0. Since the QIR array is new, we assume we can use memcpy.
                            var copyBlock = epFunc.AppendBasicBlock("copy");
                            var nextBlock = epFunc.AppendBasicBlock("next");
                            var cond = builder.Compare(IntPredicate.SignedGreater, length, this.CurrentContext.CreateConstant(0L));
                            builder.Branch(cond, copyBlock, nextBlock);
                            builder = new InstructionBuilder(copyBlock);
                            var destBase = builder.Call(this.GetRuntimeFunction("array_get_element_ptr_1d"), array,
                                this.CurrentContext.CreateConstant(0L));
                            builder.MemCpy(destBase, namedValues[mapping.BaseName], 
                                builder.Mul(length, builder.IntCast(elementSize, this.CurrentContext.Int64Type, true)), 
                                false);
                            builder.Branch(nextBlock);
                            builder = new InstructionBuilder(nextBlock);
                        }
                        else if (mapping.StructType != null)
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
                        this.CurrentContext.CreateAttribute("EntryPoint"));
                }
                else
                {
                    this.CurrentModule.AddAlias(func, epName).Linkage = Linkage.External;
                    // Mark the function as an entry point
                    func.AddAttributeAtIndex(FunctionAttributeIndex.Function, 
                        this.CurrentContext.CreateAttribute("EntryPoint"));
                }
            }
        }

        /// <summary>
        /// Maps a QIR type to a more clang-friendly type.
        /// </summary>
        /// <param name="t">The type to map</param>
        /// <returns>The mapped type</returns>
        private ITypeRef MapClangType(ITypeRef t)
        {
            string typeName = "";
            if (t == QirResult)
            {
                typeName = "Result";
            }
            else if (t == QirArray)
            {
                typeName = "Array";
            }
            else if (t == QirPauli)
            {
                typeName = "Pauli";
            }
            else if (t == QirBigInt)
            {
                typeName = "BigInt";
            }
            else if (t == QirString)
            {
                typeName = "String";
            }
            else if (t == QirQubit)
            {
                typeName = "Qubit";
            }
            else if (t == QirCallable)
            {
                typeName = "Callable";
            }
            else if (t == QirTuplePointer)
            {
                typeName = "TuplePointer";
            }

            if ((typeName != "") && this.ClangTypeMappings.TryGetValue(typeName, out string replacementName))
            {
                if (clangTranslatedType.TryGetValue(typeName, out ITypeRef clangType))
                {
                    return clangType;
                }
                else
                {
                    var newType = this.CurrentContext.CreateStructType(replacementName).CreatePointerType();
                    clangTranslatedType[typeName] = newType;
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
        public void AddRef(Value v)
        {
            string s = null;
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.CurrentContext.Dispose();
                    this.libContext.Dispose();
                }

                disposedValue = true;
            }
        }


        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
