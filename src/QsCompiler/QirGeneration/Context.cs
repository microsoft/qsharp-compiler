// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Interop;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using ArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// This class holds the shared state used across a QIR generation pass.
    /// It also holds a large number of shared utility routines.
    /// </summary>
#pragma warning disable SA1404 // Code analysis suppression should have justification
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Existing code already used regions.")]
    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1516:Elements should be separated by blank line")]
#pragma warning restore SA1404 // Code analysis suppression should have justification
    public sealed class GenerationContext : IDisposable
    {
        private static readonly ILibLlvm LibContext;

        static GenerationContext()
        {
            LibContext = Library.InitializeLLVM();
        }

        #region Member variables

        /// <summary>
        /// The context used for QIR generation.
        /// </summary>
        /// <inheritdoc cref="Ubiquity.NET.Llvm.Context"/>
        public Context Context { get; }

        /// <summary>
        /// The module used for QIR generation.
        /// Generated functions to facilitate interoperability are created via <see cref="Interop"/>.
        /// </summary>
        /// <inheritdoc cref="BitcodeModule"/>
        public BitcodeModule Module { get; }

        /// <summary>
        /// The used QIR types.
        /// </summary>
        public Types Types { get; }

        /// <summary>
        /// The used QIR constants.
        /// </summary>
        public Constants Constants { get; }

        /// <summary>
        /// Tools to construct and handle values throughout QIR emission.
        /// </summary>
        internal QirValues Values { get; }

        /// <summary>
        /// Tools to invoke built-in functions.
        /// </summary>
        internal Functions Functions { get; }

        /// <summary>
        /// The syntax tree transformation that constructs QIR.
        /// </summary>
        /// <exception cref="InvalidOperationException">The transformation has not been set via <see cref="SetTransformation"/>.</exception>
        public Generator Transformation =>
            this.transformation ?? throw new InvalidOperationException("no transformation defined");

        private Generator? transformation;

        private readonly ImmutableDictionary<QsQualifiedName, QsCallable> globalCallables;
        private readonly ImmutableDictionary<QsQualifiedName, QsCustomType> globalTypes;

        private readonly FunctionLibrary runtimeLibrary;
        private readonly FunctionLibrary quantumInstructionSet;

        internal IrFunction? CurrentFunction { get; private set; }
        internal BasicBlock? CurrentBlock { get; private set; }
        internal InstructionBuilder CurrentBuilder { get; private set; }

        internal ScopeManager ScopeMgr { get; }
        internal Stack<IValue> ValueStack { get; }
        internal Stack<ResolvedType> ExpressionTypeStack { get; }

        /// <summary>
        /// We support nested inlining and hence keep a stack with the information for each inline level.
        /// Each item in the stack contains the return value for that inline level.
        /// While this is currently not necessary since we currently require that any inlined callable either
        /// returns unit or has exactly one return statement, this restriction could be lifted in the future.
        /// </summary>
        private readonly Stack<IValue> inlineLevels;
        private readonly Dictionary<string, int> uniqueLocalNames = new Dictionary<string, int>();
        private readonly Dictionary<string, int> uniqueGlobalNames = new Dictionary<string, int>();
        private readonly Dictionary<string, GlobalVariable> definedStrings = new Dictionary<string, GlobalVariable>();

        private readonly List<(IrFunction, Action<IReadOnlyList<Argument>>)> liftedPartialApplications = new List<(IrFunction, Action<IReadOnlyList<Argument>>)>();
        private readonly Dictionary<string, (QsCallable, GlobalVariable)> callableTables = new Dictionary<string, (QsCallable, GlobalVariable)>();
        private readonly List<string> pendingCallableTables = new List<string>();
        private readonly Dictionary<ResolvedType, GlobalVariable> memoryManagementTables = new Dictionary<ResolvedType, GlobalVariable>();
        private readonly List<ResolvedType> pendingMemoryManagementTables = new List<ResolvedType>();

        #endregion

        #region Control flow context tracking

        // This value is used to assigne a unique id to each branch/loop in the program.
        private int uniqueControlFlowId = 0;

        // Contains the ids of all currently open branches.
        private readonly Stack<int> branchIds = new Stack<int>(new[] { 0 });
        internal int CurrentBranch => this.branchIds.Peek();
        internal bool IsOpenBranch(int id) => this.branchIds.Contains(id);

        internal void StartBranch()
        {
            this.uniqueControlFlowId += 1;
            this.branchIds.Push(this.uniqueControlFlowId);
        }

        internal void EndBranch() =>
            this.branchIds.Pop();

        // Contains the ids of all currently executing loops.
        private readonly Stack<int> loopIds = new Stack<int>();
        internal bool IsWithinLoop => this.loopIds.Any();
        internal bool IsWithinCurrentLoop(int branchId)
        {
            if (!this.loopIds.TryPeek(out var currentLoopId))
            {
                return false;
            }

            var branchesWithinCurrentLoop = this.branchIds.TakeWhile(id => id >= currentLoopId);
            return branchesWithinCurrentLoop.Contains(branchId);
        }

        internal void StartLoop()
        {
            // We need to mark the loop and also mark the branching
            // to ensure that pointers are properly loaded when needed.
            this.StartBranch();
            this.loopIds.Push(this.CurrentBranch);
        }

        internal void EndLoop()
        {
            this.loopIds.Pop();
            this.EndBranch();
        }

        internal bool IsInlined => this.inlineLevels.Any();

        internal bool IsLibrary { get; private set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationContext"/> class.
        /// Before using the constructed context, the following needs to be done:
        /// 1.) the transformation needs to be set by calling <see cref="SetTransformation"/>,
        /// 2.) the runtime library needs to be initialized by calling <see cref="InitializeRuntimeLibrary"/>, and
        /// 3.) the quantum instructions set needs to be registered by calling <see cref="RegisterQuantumInstructionSet"/>.
        /// </summary>
        /// <param name="syntaxTree">The syntax tree for which QIR is generated.</param>
        /// <param name="isLibrary">Whether the current compilation is being performed for a library.</param>
        internal GenerationContext(IEnumerable<QsNamespace> syntaxTree, bool isLibrary)
        {
            this.IsLibrary = isLibrary;
            this.globalCallables = syntaxTree.GlobalCallableResolutions();
            this.globalTypes = syntaxTree.GlobalTypeResolutions();

            this.Context = new Context();
            this.Module = this.Context.CreateBitcodeModule();

            this.Types = new Types(this.Context, name => this.globalTypes.TryGetValue(name, out var decl) ? decl : null);
            this.Constants = new Constants(this.Context, this.Module, this.Types);
            this.Values = new QirValues(this, this.Constants);
            this.Functions = new Functions(this);
            this.transformation = null; // needs to be set by the instantiating transformation

            this.CurrentBuilder = new InstructionBuilder(this.Context);
            this.ValueStack = new Stack<IValue>();
            this.ExpressionTypeStack = new Stack<ResolvedType>();
            this.inlineLevels = new Stack<IValue>();
            this.ScopeMgr = new ScopeManager(this);

            this.runtimeLibrary = new FunctionLibrary(
                this.Module,
                s => Functions.FunctionName(Component.RuntimeLibrary, s));
            this.quantumInstructionSet = new FunctionLibrary(
                this.Module,
                s => Functions.FunctionName(Component.QuantumInstructionSet, s));
        }

        /// <summary>
        /// Sets the syntax tree transformation that is used to construct QIR.
        /// </summary>
        internal void SetTransformation(
            Generator transformation,
            out FunctionLibrary runtimeLibrary,
            out FunctionLibrary quantumInstructionSet)
        {
            this.transformation = transformation;
            runtimeLibrary = this.runtimeLibrary;
            quantumInstructionSet = this.quantumInstructionSet;
        }

        /// <summary>
        /// Creates a new name by combining the given name with a unique identifier according to the given dictionary.
        /// Adds the name and the identifier to the given dictionary.
        /// </summary>
        private static string UniqueName(string name, Dictionary<string, int> names)
        {
            var index = names.TryGetValue(name, out int n) ? n + 1 : 1;
            names[name] = index;
            return $"{name}__{index}";
        }

        /// <inheritdoc cref="NameGeneration.InteropFriendlyWrapperName(QsQualifiedName)"/>
        [Obsolete("Please use NameGeneration.InteropFriendlyWrapperName instead.", true)]
        public static string InteropFriendlyWrapperName(QsQualifiedName fullName) =>
            NameGeneration.InteropFriendlyWrapperName(fullName);

        /// <inheritdoc cref="NameGeneration.EntryPointName(QsQualifiedName)"/>
        [Obsolete("Please use NameGeneration.EntryPointName instead.", true)]
        public static string EntryPointName(QsQualifiedName fullName) =>
            NameGeneration.EntryPointName(fullName);

        /// <inheritdoc cref="NameGeneration.FunctionName(QsQualifiedName, QsSpecializationKind)"/>
        [Obsolete("Please use NameGeneration.FunctionName instead.", true)]
        public static string FunctionName(QsQualifiedName fullName, QsSpecializationKind kind) =>
            NameGeneration.FunctionName(fullName, kind);

        /// <inheritdoc cref="NameGeneration.FunctionWrapperName(QsQualifiedName, QsSpecializationKind)"/>
        [Obsolete("Please use NameGeneration.FunctionWrapperName instead.", true)]
        public static string FunctionWrapperName(QsQualifiedName fullName, QsSpecializationKind kind) =>
            NameGeneration.FunctionWrapperName(fullName, kind);

        /// <summary>
        /// Order of specializations in the constant array that contains the fours IrFunctions
        /// associated with a callable.
        /// </summary>
        public static readonly ImmutableArray<QsSpecializationKind> FunctionArray = ImmutableArray.Create(
            QsSpecializationKind.QsBody,
            QsSpecializationKind.QsAdjoint,
            QsSpecializationKind.QsControlled,
            QsSpecializationKind.QsControlledAdjoint);

        #region Initialization and emission

        /// <summary>
        /// Initializes the QIR runtime library.
        /// </summary>
        public void InitializeRuntimeLibrary()
        {
            // Q# specific helpers
            this.runtimeLibrary.AddFunction(RuntimeLibrary.HeapAllocate, this.Context.Int8Type.CreatePointerType(), this.Types.Int);

            // result library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultGetZero, this.Types.Result);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultGetOne, this.Types.Result);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultUpdateReferenceCount, this.Context.VoidType, this.Types.Result, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultEqual, this.Context.BoolType, this.Types.Result, this.Types.Result);

            // string library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringCreate, this.Types.String, this.Types.DataArrayPointer);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringGetLength, this.Context.Int32Type, this.Types.String);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringGetData, this.Types.DataArrayPointer, this.Types.String);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringUpdateReferenceCount, this.Context.VoidType, this.Types.String, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringConcatenate, this.Types.String, this.Types.String, this.Types.String);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringEqual, this.Context.BoolType, this.Types.String, this.Types.String);

            // to-string conversion functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntToString, this.Types.String, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.DoubleToString, this.Types.String, this.Context.DoubleType);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.IntToString, this.Types.String, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.QubitToString, this.Types.String, this.Types.Qubit);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.RangeToString, this.Types.String, this.Types.Range);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultToString, this.Types.String, this.Types.Result);

            // bigint library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntCreateI64, this.Types.BigInt, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntCreateArray, this.Types.BigInt, this.Context.Int32Type, this.Types.DataArrayPointer);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntGetLength, this.Context.Int32Type, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntGetData, this.Types.DataArrayPointer, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntUpdateReferenceCount, this.Context.VoidType, this.Types.BigInt, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntNegate, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntAdd, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntSubtract, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntMultiply, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntDivide, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntModulus, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntPower, this.Types.BigInt, this.Types.BigInt, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntBitwiseAnd, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntBitwiseOr, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntBitwiseXor, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntBitwiseNot, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntShiftLeft, this.Types.BigInt, this.Types.BigInt, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntShiftRight, this.Types.BigInt, this.Types.BigInt, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntEqual, this.Context.BoolType, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntGreater, this.Context.BoolType, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntGreaterEq, this.Context.BoolType, this.Types.BigInt, this.Types.BigInt);

            // tuple library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleCreate, this.Types.Tuple, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleUpdateAliasCount, this.Context.VoidType, this.Types.Tuple, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleUpdateReferenceCount, this.Context.VoidType, this.Types.Tuple, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleCopy, this.Types.Tuple, this.Types.Tuple, this.Context.BoolType);

            // array library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayCreate1d, this.Types.Array, this.Context.Int32Type, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayGetElementPtr1d, this.Context.Int8Type.CreatePointerType(), this.Types.Array, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayUpdateAliasCount, this.Context.VoidType, this.Types.Array, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayUpdateReferenceCount, this.Context.VoidType, this.Types.Array, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayCopy, this.Types.Array, this.Types.Array, this.Context.BoolType);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayConcatenate, this.Types.Array, this.Types.Array, this.Types.Array);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArraySlice1d, this.Types.Array, this.Types.Array, this.Types.Range, this.Types.Bool);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayGetSize1d, this.Context.Int64Type, this.Types.Array);

            // callable library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableCreate, this.Types.Callable, this.Types.CallableTable.CreatePointerType(), this.Types.CallableMemoryManagementTable.CreatePointerType(), this.Types.Tuple);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableInvoke, this.Context.VoidType, this.Types.Callable, this.Types.Tuple, this.Types.Tuple);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableCopy, this.Types.Callable, this.Types.Callable, this.Context.BoolType);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableMakeAdjoint, this.Context.VoidType, this.Types.Callable);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableMakeControlled, this.Context.VoidType, this.Types.Callable);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableUpdateAliasCount, this.Context.VoidType, this.Types.Callable, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableUpdateReferenceCount, this.Context.VoidType, this.Types.Callable, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CaptureUpdateAliasCount, this.Context.VoidType, this.Types.Callable, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CaptureUpdateReferenceCount, this.Context.VoidType, this.Types.Callable, this.Context.Int32Type);

            // qubit library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.QubitAllocate, this.Types.Qubit);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.QubitAllocateArray, this.Types.Array, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.QubitRelease, this.Context.VoidType, this.Types.Qubit);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.QubitReleaseArray, this.Context.VoidType, this.Types.Array);

            // diagnostic library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.Fail, this.Context.VoidType, this.Types.String);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.Message, this.Context.VoidType, this.Types.String);
        }

        /// <summary>
        /// Finds and registers all of the quantum instructions in the current compilation.
        /// For this purpose, quantum instructions are any operations that have the TargetInstruction attribute.
        /// <br/><br/>
        /// Interop-compatible function declarations for all of the quantum operations can be generated
        /// upon emission if via the corresponding argument to Emit.
        /// </summary>
        public void RegisterQuantumInstructionSet()
        {
            this.quantumInstructionSet.AddFunction(QuantumInstructionSet.DumpMachine, this.Context.VoidType, this.Context.Int8Type.CreatePointerType());
            this.quantumInstructionSet.AddFunction(QuantumInstructionSet.DumpRegister, this.Context.VoidType, this.Context.Int8Type.CreatePointerType(), this.Types.Array);

            foreach (var c in this.globalCallables.Values)
            {
                if (NameGeneration.TryGetTargetInstructionName(c, out var name))
                {
                    // Special handling for Unit since by default it turns into an empty tuple
                    var returnType = c.Signature.ReturnType.Resolution.IsUnitType
                        ? this.Context.VoidType
                        : this.LlvmTypeFromQsharpType(c.Signature.ReturnType);
                    var argTypeKind = c.Signature.ArgumentType.Resolution;
                    var argTypeArray =
                        argTypeKind is ResolvedTypeKind.TupleType tuple ? tuple.Item.Select(this.LlvmTypeFromQsharpType).ToArray() :
                        argTypeKind.IsUnitType ? new ITypeRef[0] :
                        new ITypeRef[] { this.LlvmTypeFromQsharpType(c.Signature.ArgumentType) };
                    this.quantumInstructionSet.AddFunction(name, returnType, argTypeArray);
                }
            }
        }

        /// <summary>
        /// Invokes <paramref name="createBridge"/>, passing it the declaration of the callable with the givne name
        /// and the corresponding QIR function for the given specialization kind.
        /// Attaches the attributes with the given names to the returned IrFunction.
        /// </summary>
        /// <exception cref="ArgumentException">No callable with the given name exists in the compilation.</exception>
        private void CreateBridgeFunction(
            QsQualifiedName qualifiedName,
            QsSpecializationKind specKind,
            Func<QsCallable, IrFunction, IrFunction> createBridge,
            params string[] attributes)
        {
            if (this.TryGetGlobalCallable(qualifiedName, out QsCallable? callable)
                && this.TryGetFunction(qualifiedName, specKind, out IrFunction? func))
            {
                var bridge = createBridge(callable, func);
                foreach (var attName in attributes)
                {
                    var attribute = this.Context.CreateAttribute(attName);
                    bridge.AddAttributeAtIndex(FunctionAttributeIndex.Function, attribute);
                }
            }
            else
            {
                throw new ArgumentException("no function with that name exists");
            }
        }

        /// <summary>
        /// <inheritdoc cref="Interop.GenerateWrapper(GenerationContext, string, ArgumentTuple, ResolvedType, IrFunction)"/>
        /// <br/>
        /// Adds an <see cref="AttributeNames.InteropFriendly"/> attribute marking the created wrapper as interop wrapper.
        /// If no wrapper needed to be created because the signature of the callable is interop-friendly,
        /// adds the attribute to the existing function.
        /// </summary>
        /// <param name="qualifiedName">The fully qualified name of the Q# callable to create a wrapper for.</param>
        /// <exception cref="ArgumentException">No callable with the given name exists in the compilation.</exception>
        public void CreateInteropFriendlyWrapper(QsQualifiedName qualifiedName)
        {
            string wrapperName = NameGeneration.InteropFriendlyWrapperName(qualifiedName);
            IrFunction InteropWrapper(QsCallable callable, IrFunction implementation) =>
                Interop.GenerateWrapper(this, wrapperName, callable.ArgumentTuple, callable.Signature.ReturnType, implementation);
            this.CreateBridgeFunction(qualifiedName, QsSpecializationKind.QsBody, InteropWrapper, AttributeNames.InteropFriendly);
        }

        /// <summary>
        /// <inheritdoc cref="Interop.GenerateEntryPoint(GenerationContext, string, ArgumentTuple, ResolvedType, IrFunction)"/>
        /// <br/>
        /// Adds an <see cref="AttributeNames.EntryPoint"/> attribute to the created function.
        /// </summary>
        /// <param name="qualifiedName">The fully qualified name of the Q# callable to create a wrapper for.</param>
        /// <exception cref="ArgumentException">No callable with the given name exists in the compilation.</exception>
        internal void CreateEntryPoint(QsQualifiedName qualifiedName)
        {
            string entryPointName = NameGeneration.EntryPointName(qualifiedName);
            IrFunction EntryPoint(QsCallable callable, IrFunction implementation) =>
                Interop.GenerateEntryPoint(this, entryPointName, callable.ArgumentTuple, callable.Signature.ReturnType, implementation);
            this.CreateBridgeFunction(qualifiedName, QsSpecializationKind.QsBody, EntryPoint, AttributeNames.EntryPoint);
        }

        #endregion

        #region Look-up

        /// <summary>
        /// Gets the LLVM object for a runtime library function.
        /// If this is the first reference to the function, its declaration is added to the module.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <returns>The LLVM function object</returns>
        internal IrFunction GetOrCreateRuntimeFunction(string name) =>
            this.runtimeLibrary.GetOrCreateFunction(name);

        /// <summary>
        /// Gets the LLVM object for a quantum instruction set function.
        /// If this is the first reference to the function, its declaration is added to the module.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <returns>The LLVM function object</returns>
        internal IrFunction GetOrCreateTargetInstruction(string name) =>
            this.quantumInstructionSet.GetOrCreateFunction(name);

        /// <summary>
        /// Gets or creates a global constant (data array) that stores the given string.
        /// The global constant contains a data array with the zero-terminated representation of the string.
        /// </summary>
        internal GlobalVariable GetOrCreateStringConstant(string str)
        {
            if (!this.definedStrings.TryGetValue(str, out var constant))
            {
                var constantString = this.Context.CreateConstantString(str, true);
                constant = this.Module.AddGlobal(constantString.NativeType, true, Linkage.Internal, constantString);
                this.definedStrings.Add(str, constant);
            }

            return constant;
        }

        /// <summary>
        /// Tries to find a global Q# callable in the current compilation.
        /// </summary>
        /// <param name="fullName">The callable's qualified name</param>
        /// <param name="callable">The Q# callable, if found</param>
        /// <returns>true if the callable is found, false if not</returns>
        internal bool TryGetGlobalCallable(QsQualifiedName fullName, [MaybeNullWhen(false)] out QsCallable callable) =>
            this.globalCallables.TryGetValue(fullName, out callable);

        /// <summary>
        /// Tries to find a Q# user-defined type in the current compilation.
        /// </summary>
        /// <param name="fullName">The UDT's qualified name</param>
        /// <param name="udt">The Q# UDT, if found</param>
        /// <returns>true if the UDT is found, false if not</returns>
        internal bool TryGetCustomType(QsQualifiedName fullName, [MaybeNullWhen(false)] out QsCustomType udt) =>
            this.globalTypes.TryGetValue(fullName, out udt);

        #endregion

        #region Function management

        /// <summary>
        /// Preps the shared state for a new QIR function by clearing all currently listed unique names,
        /// opening a new naming scope and a new scope in the scope manager.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The current indentation level is not null or there are variables names that are still in scope.
        /// </exception>
        internal void StartFunction()
        {
            if (this.inlineLevels.Any() || !this.ScopeMgr.IsEmpty)
            {
                throw new InvalidOperationException("Processing of the current function and needs to be properly terminated before starting a new one");
            }

            this.uniqueLocalNames.Clear();
            this.ScopeMgr.OpenScope();
        }

        /// <summary>
        /// Ends a QIR function by finishing the current basic block, closing the current scope in teh scope manager
        /// and closing a naming scope.
        /// </summary>
        /// <returns>true if the function has been properly ended</returns>
        /// <exception cref="InvalidOperationException">The current function or the current block is set to null.</exception>
        internal bool EndFunction(bool generatePending = false)
        {
            if (this.CurrentFunction == null || this.CurrentBlock == null)
            {
                throw new InvalidOperationException("the current function or the current block is null");
            }

            this.ScopeMgr.CloseScope(this.CurrentBlock.Terminator != null);

            if (this.CurrentBlock.Terminator == null)
            {
                if (this.CurrentFunction.ReturnType.IsVoid)
                {
                    this.CurrentBuilder.Return();
                }
                else
                {
                    this.CurrentBuilder.Unreachable();
                }
            }

            if (generatePending)
            {
                this.GenerateRequiredFunctions();
            }

            return this.ScopeMgr.IsEmpty && !this.inlineLevels.Any();
        }

        /// <summary>
        /// If we are not currently inlining, exits the scope and generates a suitable return using the current builder,
        /// and otherwise updates the return value for the current inline level.
        /// </summary>
        /// <exception cref="InvalidOperationException">A return value for current inline level is already defined</exception>
        internal void AddReturn(IValue result, bool returnsVoid)
        {
            if (!this.inlineLevels.Any())
            {
                // The return value and its inner items either won't be unreferenced
                // when exiting the function or a reference will be added by ExitScope
                // before exiting the scope by since it will be used by the caller.
                this.ScopeMgr.ExitFunction(returned: result);

                if (returnsVoid)
                {
                    this.CurrentBuilder.Return();
                }
                else
                {
                    this.CurrentBuilder.Return(result.Value);
                }
            }
            else if (!returnsVoid)
            {
                var current = this.inlineLevels.Pop();
                if (current.Value != this.Constants.UnitValue)
                {
                    throw new InvalidOperationException("return value for current inline level already defined");
                }

                this.inlineLevels.Push(result);
            }
        }

        /// <summary>
        /// Adds the declaration for a QIR function to the current module.
        /// Usually <see cref="GenerateFunctionHeader"/> is used, which generates the start of the actual definition.
        /// This method is primarily useful for Q# specializations with external or intrinsic implementations, which get
        /// generated as declarations with no definition.
        /// </summary>
        /// <param name="spec">The Q# specialization for which to register a function</param>
        internal IrFunction RegisterFunction(QsSpecialization spec)
        {
            var name = NameGeneration.FunctionName(spec.Parent, spec.Kind);
            var returnTypeRef = spec.Signature.ReturnType.Resolution.IsUnitType
                ? this.Context.VoidType
                : this.LlvmTypeFromQsharpType(spec.Signature.ReturnType);
            var argTypeRefs =
                spec.Signature.ArgumentType.Resolution.IsUnitType ? new ITypeRef[0] :
                spec.Signature.ArgumentType.Resolution is ResolvedTypeKind.TupleType ts ? ts.Item.Select(this.LlvmTypeFromQsharpType).ToArray() :
                new ITypeRef[] { this.LlvmTypeFromQsharpType(spec.Signature.ArgumentType) };

            var signature = this.Context.GetFunctionType(returnTypeRef, argTypeRefs);
            return this.Module.CreateFunction(name, signature);
        }

        /// <summary>
        /// Generates the start of the definition for a QIR function in the current module.
        /// Specifically, an entry block for the function is created, and the function's arguments are given names.
        /// </summary>
        /// <param name="spec">The Q# specialization for which to register a function.</param>
        /// <param name="argTuple">The specialization's argument tuple.</param>
        /// <param name="deconstuctArgument">Whether or not to deconstruct the argument tuple.</param>
        /// <param name="shouldBeExtern">Whether the given specialization should be generated as extern.</param>
        internal void GenerateFunctionHeader(QsSpecialization spec, ArgumentTuple argTuple, bool deconstuctArgument = true, bool shouldBeExtern = false)
        {
            (string?, ResolvedType)[] ArgTupleToArgItems(ArgumentTuple arg, Queue<(string?, ArgumentTuple)> tupleQueue)
            {
                (string?, ResolvedType) LocalVarName(ArgumentTuple v)
                {
                    if (v is ArgumentTuple.QsTuple)
                    {
                        tupleQueue.Enqueue((null, v));
                        return (null, v.GetResolvedType());
                    }
                    else if (v is ArgumentTuple.QsTupleItem item)
                    {
                        return item.Item.VariableName is QsLocalSymbol.ValidName varName
                            ? (varName.Item, item.Item.Type)
                            : (null, item.Item.Type);
                    }
                    else
                    {
                        throw new NotImplementedException("unknown item in argument tuple");
                    }
                }

                return arg is ArgumentTuple.QsTuple tuple
                    ? tuple.Item.Select(item => LocalVarName(item)).ToArray()
                    : new[] { LocalVarName(arg) };
            }

            this.CurrentFunction = this.RegisterFunction(spec);
            this.CurrentFunction.Linkage = shouldBeExtern ? Linkage.External : Linkage.Internal;
            this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
            this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);
            if (spec.Signature.ArgumentType.Resolution.IsUnitType)
            {
                return;
            }

            var innerTuples = new Queue<(string?, ArgumentTuple)>();
            var outerArgItems = ArgTupleToArgItems(argTuple, innerTuples);
            var innerTupleValues = new Queue<TupleValue>();

            // If we have a single named tuple-valued argument, then the items of the tuple
            // are the arguments to the function and we need to reconstruct the tuple.
            // The reason for this choice of representation is that relying only on the argument type
            // rather than the argument tuple for determining the signature of a function is much cleaner.
            if (outerArgItems.Length == 1 && this.CurrentFunction.Parameters.Count > 1)
            {
                if (!(outerArgItems[0].Item2.Resolution is ResolvedTypeKind.TupleType ts)
                    || ts.Item.Length != this.CurrentFunction.Parameters.Count)
                {
                    throw new InvalidOperationException("number of function parameters does not match argument");
                }

                var tupleItems = this.CurrentFunction.Parameters.Select((v, i) => this.Values.From(v, ts.Item[i])).ToArray();
                var innerTuple = this.Values.CreateTuple(tupleItems);
                var name = outerArgItems[0].Item1;
                if (name != null)
                {
                    this.ScopeMgr.RegisterVariable(name, innerTuple, fromLocalId: null);
                }
                else
                {
                    // This case should actually never occur; the only reason we build the tuple in the first place
                    // is because there is a Q# variable (the argument) that the value is assigne to.
                    // However, there is also no reason to fail here, since the behavior implemented this way is correct.
                    innerTupleValues.Enqueue(innerTuple);
                }
            }
            else
            {
                for (var i = 0; i < outerArgItems.Length; ++i)
                {
                    var (argName, argType) = outerArgItems[i];
                    var argValue = this.Values.From(this.CurrentFunction.Parameters[i], argType);
                    if (argName != null)
                    {
                        this.CurrentFunction.Parameters[i].Name = argName;
                        this.ScopeMgr.RegisterVariable(argName, argValue, fromLocalId: null);
                    }
                    else
                    {
                        innerTupleValues.Enqueue((TupleValue)argValue);
                    }
                }
            }

            // Now break up inner argument tuples
            while (deconstuctArgument && innerTuples.TryDequeue(out (string?, ArgumentTuple) tuple))
            {
                var (tupleArgName, tupleArg) = tuple;
                var tupleValue = tupleArgName == null
                    ? innerTupleValues.Dequeue()
                    : (TupleValue)this.ScopeMgr.GetVariable(tupleArgName);

                var argTupleItems = ArgTupleToArgItems(tupleArg, innerTuples);
                for (var i = 0; i < argTupleItems.Length; ++i)
                {
                    var (argName, _) = argTupleItems[i];
                    var element = tupleValue.GetTupleElement(i);
                    if (argName != null)
                    {
                        this.ScopeMgr.RegisterVariable(argName, element, fromLocalId: null);
                    }
                    else
                    {
                        innerTupleValues.Enqueue((TupleValue)element);
                    }
                }
            }
        }

        /// <summary>
        /// Generates the default constructor for a Q# user-defined type.
        /// This routine generates all the code for the constructor, not just the header.
        /// </summary>
        internal void GenerateConstructor(QsSpecialization spec, ArgumentTuple argTuple)
        {
            this.GenerateFunctionHeader(spec, argTuple, deconstuctArgument: false);

            // create the udt (output value)
            if (spec.Signature.ArgumentType.Resolution.IsUnitType)
            {
                var udtTuple = this.Values.CreateTuple(this.Values.Unit);
                this.AddReturn(udtTuple, returnsVoid: false);
            }
            else if (this.CurrentFunction != null)
            {
                var itemTypes = spec.Signature.ArgumentType.Resolution is ResolvedTypeKind.TupleType ts
                        ? ts.Item
                        : ImmutableArray.Create(spec.Signature.ArgumentType);
                if (itemTypes.Length != this.CurrentFunction.Parameters.Count)
                {
                    throw new InvalidOperationException("number of function parameters does not match argument");
                }

                var tupleItems = this.CurrentFunction.Parameters.Select((v, i) => this.Values.From(v, itemTypes[i])).ToArray();
                var udtTuple = this.Values.CreateTuple(tupleItems);
                this.AddReturn(udtTuple, returnsVoid: false);
            }
        }

        /// <summary>
        /// Generates a suitable QIR function for the partial application with the given body.
        /// The generated function takes a capture tuple, and argument tuple, and an output tuple
        /// as arguments and returns void.
        /// </summary>
        internal IrFunction GeneratePartialApplication(string name, QsSpecializationKind kind, Action<IReadOnlyList<Argument>> body)
        {
            var funcName = NameGeneration.FunctionWrapperName(new QsQualifiedName("Lifted", name), kind);
            IrFunction func = this.Module.CreateFunction(funcName, this.Types.FunctionSignature);
            func.Linkage = Linkage.Internal;
            this.liftedPartialApplications.Add((func, body));
            return func;
        }

        /// <summary>
        /// Tries to get the QIR function for a Q# specialization by name so it can be called.
        /// If the function hasn't been generated yet, false is returned.
        /// </summary>
        /// <param name="callableName">The name of the Q# callable.</param>
        /// <param name="kind">The Q# specialization kind.</param>
        /// <param name="function">Gets filled in with the LLVM function object if it exists already.</param>
        /// <returns>true if the function has already been declared/defined, or false otherwise.</returns>
        private bool TryGetFunction(QsQualifiedName callableName, QsSpecializationKind kind, [MaybeNullWhen(false)] out IrFunction function)
        {
            var fullName = NameGeneration.FunctionName(callableName, kind);
            return this.Module.TryGetFunction(fullName, out function);
        }

        /// <summary>
        /// Gets the QIR function for a Q# specialization by name so it can be called.
        /// If the function hasn't been generated yet, its declaration is generated so that it can be called.
        /// </summary>
        /// <returns>The LLVM object for the corresponding LLVM function</returns>
        internal IrFunction GetFunctionByName(QsQualifiedName fullName, QsSpecializationKind kind)
        {
            // If the function is already defined, return it
            if (this.TryGetFunction(fullName, kind, out IrFunction? function))
            {
                return function;
            }

            // Otherwise, we need to find the function's callable to get the signature,
            // and then register the function
            if (this.TryGetGlobalCallable(fullName, out QsCallable? callable))
            {
                var spec = callable.Specializations.First(spec => spec.Kind == kind);
                return this.RegisterFunction(spec);
            }

            // If we can't find the function at all, it's a problem...
            throw new KeyNotFoundException($"Can't find callable {fullName}");
        }

        /// <summary>
        /// Creates a global variable with the given name that contains an array of function pointers
        /// that define the necessary implemementations for a callable value.
        /// The array contains the function pointers returned by the given function for each possible functor specialization,
        /// as well as a pointer to an additional function to manage the reference counts for the given capture, if any.
        /// Queues the generation of that function if necessary such that the current context is not modified
        /// beyond adding the corresponding constant and function declaration, if necessary.
        /// </summary>
        internal GlobalVariable CreateCallableTable(string name, Func<QsSpecializationKind, IrFunction?> getSpec)
        {
            var funcs = new Constant[4];
            for (var index = 0; index < 4; index++)
            {
                var func = getSpec(FunctionArray[index]);
                if (func != null)
                {
                    func.Linkage = Linkage.Internal;
                    funcs[index] = func;
                }
                else
                {
                    funcs[index] = Constant.ConstPointerToNullFor(this.Types.FunctionSignature.CreatePointerType());
                }
            }

            // Build the callable table
            var array = ConstantArray.From(this.Types.FunctionSignature.CreatePointerType(), funcs);
            return this.Module.AddGlobal(array.NativeType, true, Linkage.Internal, array, $"{name}__FunctionTable");
        }

        /// <summary>
        /// If a constant array with the IrFunctions for the given callable already exists,
        /// returns the corresponding global variable. If no such array exists, creates a constant array
        /// and instantiates the necessary wrapper functions, queues the generation of their implementations,
        /// and returns the created global constant.
        /// The generation of the implementations is queued such that the current context is not modified
        /// beyond adding the corresponding constant and functions declarations.
        /// </summary>
        internal GlobalVariable GetOrCreateCallableTable(QsCallable callable)
        {
            var tableName = $"{NameGeneration.FlattenNamespaceName(callable.FullName.Namespace)}__{callable.FullName.Name}";
            if (this.callableTables.TryGetValue(tableName, out (QsCallable, GlobalVariable) item))
            {
                return item.Item2;
            }
            else
            {
                IrFunction? BuildSpec(QsSpecializationKind kind) =>
                    callable.Specializations.Any(spec => spec.Kind == kind &&
                        (spec.Implementation.IsProvided || spec.Implementation.IsIntrinsic))
                            ? this.Module.CreateFunction(NameGeneration.FunctionWrapperName(callable.FullName, kind), this.Types.FunctionSignature)
                            : null;

                var table = this.CreateCallableTable(tableName, BuildSpec);
                this.callableTables.Add(tableName, (callable, table));
                this.pendingCallableTables.Add(tableName);
                return table;
            }
        }

        /// <summary>
        /// If a constant array with the IrFunctions for managing alias and reference counts
        /// for the given capture tuple already exists, returns the corresponding global variable.
        /// If no such array exists, creates a constant array and instantiates the necessary functions,
        /// queues the generation of their implementations, and returns the created global constant.
        /// The generation of the implementations is queued such that the current context is not modified
        /// beyond adding the corresponding constant and functions declarations.
        /// The table contains the function for updating the reference count as the first item,
        /// and a null pointer for the function to update the alias count as the second item.
        /// If the given capture is null, returns a null pointer of suitable type.
        /// </summary>
        internal Constant GetOrCreateCallableMemoryManagementTable(TupleValue? capture)
        {
            if (capture == null)
            {
                return Constant.ConstPointerToNullFor(this.Types.CallableMemoryManagementTable.CreatePointerType());
            }

            var type = StripPositionInfo.Apply(capture.QSharpType);
            if (this.memoryManagementTables.TryGetValue(type, out GlobalVariable table))
            {
                return table;
            }

            var name = this.GlobalName("MemoryManagement");
            var funcs = new Constant[2];
            var func = this.Module.CreateFunction($"{name}__RefCount", this.Types.CaptureCountFunction);
            func.Linkage = Linkage.Internal;
            funcs[0] = func;
            func = this.Module.CreateFunction($"{name}__AliasCount", this.Types.CaptureCountFunction);
            func.Linkage = Linkage.Internal;
            funcs[1] = func;

            var array = ConstantArray.From(this.Types.CaptureCountFunction.CreatePointerType(), funcs);
            table = this.Module.AddGlobal(array.NativeType, true, Linkage.Internal, array, $"{name}__FunctionTable");
            this.memoryManagementTables.Add(type, table);
            this.pendingMemoryManagementTables.Add(type);
            return table;
        }

        /// <summary>
        /// Given the value passed to a function that implements a Q# callable specialization with the given argument type,
        /// constructs a TupleValue of suitable type.
        /// </summary>
        internal TupleValue AsArgumentTuple(ResolvedType argType, Value argTuple)
        {
            if (argType.Resolution is ResolvedTypeKind.UserDefinedType udt)
            {
                return this.Values.FromCustomType(argTuple, udt.Item);
            }
            else
            {
                var itemTypes =
                    argType.Resolution.IsUnitType ? ImmutableArray.Create<ResolvedType>() :
                    argType.Resolution is ResolvedTypeKind.TupleType argItemTypes ? argItemTypes.Item :
                    ImmutableArray.Create(argType);
                return this.Values.FromTuple(argTuple, itemTypes);
            }
        }

        /// <summary>
        /// Sets the current function to the given one and sets the parameter names to the given names.
        /// Populates the body of the given function by invoking the given action with the function parameters.
        /// If the current block after the invokation is not terminated, adds a void return.
        /// Does *not* generate any required functions that have been added by <paramref name="executeBody"/>;
        /// it is up to the caller to ensure that the necessary functions are created.
        /// </summary>
        internal void GenerateFunction(IrFunction func, string?[] argNames, Action<IReadOnlyList<Argument>> executeBody)
        {
            this.StartFunction();
            this.CurrentFunction = func;
            for (var i = 0; i < argNames.Length; ++i)
            {
                var name = argNames[i];
                if (name != null)
                {
                    this.CurrentFunction.Parameters[i].Name = name;
                }
            }

            this.SetCurrentBlock(this.CurrentFunction.AppendBasicBlock("entry"));

            this.ScopeMgr.OpenScope();
            executeBody(this.CurrentFunction.Parameters);
            var isTerminated = this.CurrentBlock?.Terminator != null;
            this.ScopeMgr.CloseScope(isTerminated);
            if (!isTerminated)
            {
                this.CurrentBuilder.Return();
            }

            this.EndFunction(generatePending: false);
        }

        /// <summary>
        /// Creates a function wrapper that takes three tuples as arguments, and returns void. The wrapper
        /// takes a tuple with the captured values, one with the function arguments, and one to store the function return values.
        /// It extracts the function arguments from the corresponding tuple, calls into the given implementation with them,
        /// and populates the third tuple with the returned values.
        /// </summary>
        private void GenerateFunctionWrapper(IrFunction func, ResolvedSignature signature, Func<TupleValue, IValue> implementation)
        {
            // result value contains the return value, and output tuple is the tuple where that value should be stored
            void PopulateResultTuple(IValue resultValue, TupleValue outputTuple)
            {
                void StoreItemInOutputTuple(int itemIdx, IValue value)
                {
                    var itemOutputPointer = outputTuple.GetTupleElementPointer(itemIdx);
                    itemOutputPointer.StoreValue(value);
                    this.ScopeMgr.IncreaseReferenceCount(value);
                }

                if (resultValue is TupleValue resultTuple && resultTuple.TypeName == null)
                {
                    var outputItemPtrs = outputTuple.GetTupleElementPointers();
                    for (int j = 0; j < outputItemPtrs.Length; j++)
                    {
                        var resItem = resultTuple.GetTupleElement(j);
                        StoreItemInOutputTuple(j, resItem);
                    }
                }
                else if (!resultValue.QSharpType.Resolution.IsUnitType)
                {
                    StoreItemInOutputTuple(0, resultValue);
                }
            }

            this.GenerateFunction(func, new[] { "capture-tuple", "arg-tuple", "result-tuple" }, parameters =>
            {
                var argTuple = this.AsArgumentTuple(signature.ArgumentType, parameters[1]);
                var result = implementation(argTuple);
                var resultTupleItemTypes = signature.ReturnType.Resolution is ResolvedTypeKind.TupleType ts
                    ? ts.Item
                    : ImmutableArray.Create(signature.ReturnType);
                var outputTuple = this.Values.FromTuple(parameters[2], resultTupleItemTypes);
                PopulateResultTuple(result, outputTuple);
            });
        }

        /// <summary>
        /// Callables within QIR are bundles of four functions. When callables are assigned to variable or
        /// passed around, which one of these functions is ultimately invoked is strictly runtime information.
        /// For each declared callable, a constant array with four IrFunction values represents the bundle.
        /// All functions have the same type (accessible via Types.FunctionSignature), namely they take three
        /// opaque tuples and return void.
        /// <br/>
        /// A callable value is then created by specifying the global constant with the bundle as well as the
        /// capture tuple (which is the first of the three tuples that is passed to a function upon invokation).
        /// It can be invoked by providing the remaining two tuples; the argument tuple and the output tuple.
        /// In order to invoke the concrete implementation of a function, the argument tuple first needs to be
        /// cast to its concrete type and its item need to be assigned to the corresponding variables if need.
        /// Similarly, the output tuple needs to be populated with the computed value before exiting.
        /// We create a separate "wrapper" function to do just that; it casts and deconstructs the argument
        /// tuple, invokes the concrete implementation, and populates the output tuple.
        /// <br/>
        /// In cases where it is clear at generation time which concreate implementation needs to be invoked,
        /// we directly invoke that function. The global constant for the bundle and the wrapper functions
        /// hence only need to be created when callable values are assigned or passed around. Callables for
        /// which this is the case are hence accumulated during the sytnax tree transformation, and the
        /// corresponding wrappers are generated only upon emission by invoking this method.
        /// Additionally, this method generates all necessary partial applications and all necessary functions
        /// for managing reference counts for capture tuples.
        /// </summary>
        internal void GenerateRequiredFunctions()
        {
            IValue GenerateBaseMethodCall(QsCallable callable, QsSpecializationKind specKind, Value[] args)
            {
                Value value;
                if (NameGeneration.TryGetTargetInstructionName(callable, out var name))
                {
                    var func = this.GetOrCreateTargetInstruction(name);
                    value = specKind == QsSpecializationKind.QsBody
                        ? this.CurrentBuilder.Call(func, args)
                        : throw new ArgumentException($"non-body specialization for target instruction");
                }
                else
                {
                    var func = this.GetFunctionByName(callable.FullName, specKind);
                    value = this.CurrentBuilder.Call(func, args);
                }

                var result = this.Values.From(value, callable.Signature.ReturnType);
                this.ScopeMgr.RegisterValue(result);
                return result;
            }

            foreach (var tableName in this.pendingCallableTables)
            {
                var (callable, _) = this.callableTables[tableName];
                foreach (var spec in callable.Specializations)
                {
                    var fullName = NameGeneration.FunctionWrapperName(callable.FullName, spec.Kind);
                    if ((spec.Implementation.IsProvided || spec.Implementation.IsIntrinsic)
                        && this.Module.TryGetFunction(fullName, out IrFunction? func))
                    {
                        this.GenerateFunctionWrapper(func, spec.Signature, argTuple =>
                        {
                            if (spec.Kind == QsSpecializationKind.QsBody &&
                                this.Functions.TryGetBuiltInImplementation(callable.FullName, out var implementation))
                            {
                                var arg =
                                    spec.Signature.ArgumentType.Resolution.IsUserDefinedType ? argTuple :
                                    argTuple.ElementTypes.Length > 1 ? argTuple :
                                    argTuple.ElementTypes.Length == 1 ? argTuple.GetTupleElement(0) :
                                    this.Values.Unit;
                                return implementation(arg);
                            }
                            else
                            {
                                var args = spec.Signature.ArgumentType.Resolution.IsUserDefinedType
                                    ? new[] { argTuple.TypedPointer }
                                    : argTuple.GetTupleElements().Select(qirValue => qirValue.Value).ToArray();
                                return GenerateBaseMethodCall(callable, spec.Kind, args);
                            }
                        });
                    }
                    else
                    {
                        throw new InvalidOperationException($"failed to generate {fullName}");
                    }
                }
            }

            this.pendingCallableTables.Clear();

            foreach (var (func, body) in this.liftedPartialApplications)
            {
                this.GenerateFunction(func, new[] { "capture-tuple", "arg-tuple", "result-tuple" }, body);
            }

            this.liftedPartialApplications.Clear();

            foreach (var type in this.pendingMemoryManagementTables)
            {
                var table = this.memoryManagementTables[type];
                var name = table.Name.Substring(0, table.Name.Length - "__FunctionTable".Length);
                var functions = new List<(string, Action<Value, IValue>)>
                {
                    ($"{name}__RefCount", (change, capture) => this.ScopeMgr.UpdateReferenceCount(change, capture)),
                    ($"{name}__AliasCount", (change, capture) => this.ScopeMgr.UpdateAliasCount(change, capture)),
                };

                foreach (var (funcName, updateCounts) in functions)
                {
                    if (this.Module.TryGetFunction(funcName, out IrFunction? func))
                    {
                        this.GenerateFunction(func, new[] { "capture-tuple", "count-change" }, parameters =>
                        {
                            var capture = this.AsArgumentTuple(type, parameters[0]);
                            updateCounts(parameters[1], capture);
                        });
                    }
                    else
                    {
                        throw new InvalidOperationException($"failed to generate {funcName}");
                    }
                }
            }

            this.pendingMemoryManagementTables.Clear();
        }

        #endregion

        #region Control flow

        /// <summary>
        /// Depending on the value of the given condition, evaluates the corresponding function.
        /// If no function is specified for one of the branches, then a <paramref name="defaultValue"/> needs to be specified
        /// that the branch should evaluate to instead.
        /// Increases the reference count of the value the conditional execution evaluates to 1,
        /// unless <paramref name="increaseReferenceCount"/> is set to false.
        /// </summary>
        /// <returns>
        /// A phi node that evaluates to either the value of the expression depending on the branch that was taken,
        /// or the <paramref name="defaultValue"/> if no function has been specified for that branch.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Either <paramref name="onCondTrue"/> or <paramref name="onCondFalse"/> is null but no <paramref name="defaultValue"/> was specified.
        /// </exception>
        private Value ConditionalEvaluation(Value condition, Func<IValue>? onCondTrue = null, Func<IValue>? onCondFalse = null, IValue? defaultValue = null, bool increaseReferenceCount = true)
        {
            if (defaultValue == null && (onCondTrue == null || onCondFalse == null))
            {
                throw new InvalidOperationException("a default value is required if either onCondTrue or onCondFalse is null");
            }

            var defaultRequiresRefCount = increaseReferenceCount && defaultValue != null && ScopeManager.RequiresReferenceCount(defaultValue.LlvmType);
            var requiresTrueBlock = onCondTrue != null || defaultRequiresRefCount;
            var requiresFalseBlock = onCondFalse != null || defaultRequiresRefCount;

            var contBlock = this.AddBlockAfterCurrent("condContinue");
            var falseBlock = requiresFalseBlock
                ? this.AddBlockAfterCurrent("condFalse")
                : contBlock;
            var trueBlock = requiresTrueBlock
                ? this.AddBlockAfterCurrent("condTrue")
                : contBlock;

            // In order to ensure the correct reference counts, it is important that we create a new scope
            // for each branch of the conditional. When we close the scope, we list the computed value as
            // to be returned from that scope, meaning it either won't be dereferenced or its reference
            // count will increase by 1. The result of the expression is a phi node that we then properly
            // register with the scope manager, such that it will be unreferenced when going out of scope.
            this.CurrentBuilder.Branch(condition, trueBlock, falseBlock);
            var entryBlock = this.CurrentBlock!;

            Value ProcessConditionalBlock(BasicBlock block, Func<IValue>? evaluate)
            {
                this.SetCurrentBlock(block);
                this.StartBranch();

                IValue evaluated;
                if (increaseReferenceCount)
                {
                    this.ScopeMgr.OpenScope();
                    evaluated = evaluate?.Invoke() ?? defaultValue!;
                    this.ScopeMgr.CloseScope(evaluated); // forces that the ref count is increased within the branch
                }
                else
                {
                    evaluated = evaluate?.Invoke() ?? defaultValue!;
                }

                // We need to make sure to access the value *before* we end the branch -
                // otherwise the caching may complain that the value is no longer accessible.
                var res = evaluated.Value;
                this.EndBranch();
                this.CurrentBuilder.Branch(contBlock);
                return res;
            }

            var (evaluatedOnTrue, afterTrue) = (defaultValue?.Value, entryBlock);
            if (requiresTrueBlock)
            {
                var onTrue = ProcessConditionalBlock(trueBlock, onCondTrue);
                (evaluatedOnTrue, afterTrue) = (onTrue, this.CurrentBlock!);
            }

            var (evaluatedOnFalse, afterFalse) = (defaultValue?.Value, entryBlock);
            if (requiresFalseBlock)
            {
                var onFalse = ProcessConditionalBlock(falseBlock, onCondFalse);
                (evaluatedOnFalse, afterFalse) = (onFalse, this.CurrentBlock!);
            }

            this.SetCurrentBlock(contBlock);
            var phi = this.CurrentBuilder.PhiNode(defaultValue?.LlvmType ?? evaluatedOnTrue!.NativeType);
            phi.AddIncoming(evaluatedOnTrue!, afterTrue);
            phi.AddIncoming(evaluatedOnFalse!, afterFalse);
            return phi;
        }

        /// <summary>
        /// Depending on the value of the given condition, evaluates the corresponding function.
        /// Increases the reference count of the value the conditional execution evaluates to 1,
        /// unless <paramref name="increaseReferenceCount"/> is set to false.
        /// </summary>
        /// <returns>
        /// A phi node that evaluates to either the value of the expression depending on the branch that was taken.
        /// </returns>
        internal Value ConditionalEvaluation(Value condition, Func<IValue> onCondTrue, Func<IValue> onCondFalse, bool increaseReferenceCount = true) =>
            this.ConditionalEvaluation(condition, onCondTrue: onCondTrue, onCondFalse: onCondFalse, null, increaseReferenceCount);

        /// <returns>
        /// Returns a value that when executed either evaluates to the value defined by <paramref name="onCondTrue"/>,
        /// if the condition is true, or to the given <paramref name="defaultValueForCondFalse"/> if it is not.
        /// Increases the reference count of the evaluated value by 1,
        /// unless <paramref name="increaseReferenceCount"/> is set to false.
        /// </returns>
        internal Value ConditionalEvaluation(Value condition, Func<IValue> onCondTrue, IValue defaultValueForCondFalse, bool increaseReferenceCount = true) =>
            this.ConditionalEvaluation(condition, onCondTrue: onCondTrue, onCondFalse: null, defaultValueForCondFalse, increaseReferenceCount);

        /// <returns>A range with the given start, step and end.</returns>
        internal IValue CreateRange(Value start, Value step, Value end)
        {
            Value constant = this.CurrentBuilder.Load(this.Types.Range, this.Constants.EmptyRange);
            constant = this.CurrentBuilder.InsertValue(constant, start, 0u);
            constant = this.CurrentBuilder.InsertValue(constant, step, 1u);
            constant = this.CurrentBuilder.InsertValue(constant, end, 2u);
            return this.Values.From(constant, ResolvedType.New(ResolvedTypeKind.Range));
        }

        /// <summary>
        /// <inheritdoc cref="CreateForLoop(Value, Func{Value, Value}, Value, Action{Value})"/>
        /// The loop may optionally compute an output value. If an <paramref name="initialOutputValue"/> is given,
        /// then a suitable phi node to hold the computed output will be instantiated and updated as part of the loop.
        /// </summary>
        /// <returns>The final value of the computed output, if <paramref name="initialOutputValue"/> and the value returned by <paramref name="executeBody"/> are non-null, and null otherwise.</returns>
        /// <param name="startValue">The value to which the loop variable will be instantiated.</param>
        /// <param name="evaluateCondition">Given the current value of the loop variable, determines whether the next loop iteration should be entered.</param>
        /// <param name="increment">The value that is added to the loop variable after each iteration.</param>
        /// <param name="initialOutputValue">The initial value for the output that will be computed by the loop.</param>
        /// <param name="executeBody">Given the current value of the loop variable and the current output value, executes the body of the loop and returns the updated output value.</param>
        /// <exception cref="InvalidOperationException">The current function or the current block is set to null.</exception>
        private Value? CreateForLoop(Value startValue, Func<Value, Value> evaluateCondition, Value increment, Value? initialOutputValue, Func<Value, Value?, Value?> executeBody)
        {
            if (this.CurrentFunction == null || this.CurrentBlock == null)
            {
                throw new InvalidOperationException("current function is set to null");
            }

            // Contains the loop header that creates the phi-node, evaluates the condition,
            // and then branches into the body or exits the loop depending on whether the condition evaluates to true.
            var headerName = this.BlockName("header");
            BasicBlock headerBlock = this.CurrentFunction.AppendBasicBlock(headerName);

            // Contains the body of the loop, which has its own naming scope.
            var bodyName = this.BlockName("body");
            BasicBlock bodyBlock = this.CurrentFunction.AppendBasicBlock(bodyName);

            // Increments the loop variable and then branches into the header block
            // which determines whether to enter the next iteration.
            var exitingName = this.BlockName("exiting");
            BasicBlock exitingBlock = this.CurrentFunction.AppendBasicBlock(exitingName);

            // Empty block that will be entered when the loop exits that may get populated by subsequent computations.
            var exitName = this.BlockName("exit");
            BasicBlock exitBlock = this.CurrentFunction.AppendBasicBlock(exitName);

            (PhiNode, PhiNode?) PopulateLoopHeader(Value startValue, Func<Value, Value> evaluateCondition)
            {
                // End the current block by branching into the header of the loop
                BasicBlock precedingBlock = this.CurrentBlock ?? throw new InvalidOperationException("no preceding block");

                // We need to open a scope before starting the for-loop by creating the header block, since
                // it is possible for the condition to perform an allocation that needs to get cleaned up.
                this.ScopeMgr.OpenScope();
                this.CurrentBuilder.Branch(headerBlock);

                // Header block:
                // create a phi node for a loop output value if needed,
                // create a phi node representing the iteration variable and evaluate the condition
                this.SetCurrentBlock(headerBlock);
                var outputValue = initialOutputValue == null ? null : this.CurrentBuilder.PhiNode(initialOutputValue.NativeType);
                outputValue?.AddIncoming(initialOutputValue!, precedingBlock);

                var loopVariable = this.CurrentBuilder.PhiNode(this.Types.Int);
                loopVariable.AddIncoming(startValue, precedingBlock);

                var condition = evaluateCondition(loopVariable);
                this.ScopeMgr.CloseScope(this.CurrentBlock?.Terminator != null);

                this.CurrentBuilder.Branch(condition, bodyBlock, exitBlock);
                this.SetCurrentBlock(bodyBlock);
                return (loopVariable, outputValue);
            }

            void ContinueOrExitLoop((PhiNode LoopVariable, Value Increment) loopUpdate, (PhiNode PhiNode, Value NewValue)? outputUpdate)
            {
                // Unless there was a terminating statement in the loop body (such as return or fail),
                // continue into the exiting block, which updates the loop variable and enters the next iteration.
                if (this.CurrentBlock?.Terminator == null)
                {
                    this.CurrentBuilder.Branch(exitingBlock);
                }

                // Update the iteration value (phi node) and enter the next iteration
                this.SetCurrentBlock(exitingBlock);
                var nextValue = this.CurrentBuilder.Add(loopUpdate.LoopVariable, loopUpdate.Increment);
                loopUpdate.LoopVariable.AddIncoming(nextValue, exitingBlock);
                outputUpdate?.PhiNode.AddIncoming(outputUpdate.Value.NewValue, exitingBlock);
                this.CurrentBuilder.Branch(headerBlock);
            }

            Value? output = null;
            this.ExecuteLoop(() =>
            {
                var (loopVariable, outputValue) = PopulateLoopHeader(startValue, evaluateCondition);
                var newOutputValue = executeBody(loopVariable, outputValue);
                var outputUpdate = outputValue == null || newOutputValue == null ? ((PhiNode, Value)?)null : (outputValue!, newOutputValue!);
                ContinueOrExitLoop((loopVariable, increment), outputUpdate);
                output = outputUpdate?.Item1;
            });

            this.SetCurrentBlock(exitBlock);
            return output;
        }

        /// <summary>
        /// Creates a for-loop that breaks based on a condition.
        /// Note that <paramref name="evaluateCondition"/> - in contrast to <paramref name="executeBody"/> - is executed within its own scope,
        /// meaning anything allocated within the condition will be unreferenced at the end of the condition evaluation.
        /// The expectation for <paramref name="executeBody"/> on the other hand is that it takes care of all necessary handling itself.
        /// </summary>
        /// <param name="startValue">The value to which the loop variable will be instantiated.</param>
        /// <param name="evaluateCondition">Given the current value of the loop variable, determines whether the next loop iteration should be entered.</param>
        /// <param name="increment">The value that is added to the loop variable after each iteration.</param>
        /// <param name="executeBody">Given the current value of the loop variable, executes the body of the loop.</param>
        /// <exception cref="InvalidOperationException">The current function or the current block is set to null.</exception>
        private void CreateForLoop(Value startValue, Func<Value, Value> evaluateCondition, Value increment, Action<Value> executeBody)
        {
            Value? ExecuteBody(Value loopVar, Value? currentOutputValue)
            {
                executeBody(loopVar);
                return null;
            }

            this.CreateForLoop(startValue, evaluateCondition, increment, null, ExecuteBody);
        }

        /// <summary>
        /// Executes the loop defined by the given action.
        /// Ensures that all pointers will be properly loaded during and after the loop.
        /// </summary>
        /// <param name="loop">The loop to execute</param>
        internal void ExecuteLoop(Action loop)
        {
            this.StartLoop();
            loop();
            this.EndLoop();
        }

        /// <summary>
        /// Iterates through the range defined by start, step, and end, and executes the given action on each iteration value.
        /// Note that <paramref name="executeBody"/> is expected takes care of all necessary scope/memory management itself.
        /// </summary>
        /// <param name="start">The start of the range and first iteration value.</param>
        /// <param name="step">The optional step of the range that will be added to the iteration value in each iteration, where the default value is 1L.</param>
        /// <param name="end">The end of the range after which the iteration terminates.</param>
        /// <param name="executeBody">The action to perform on each item (needs to include the scope management).</param>
        /// <exception cref="InvalidOperationException">The current block is set to null.</exception>
        internal void IterateThroughRange(Value start, Value? step, Value end, Action<Value> executeBody)
        {
            if (this.CurrentFunction == null)
            {
                throw new InvalidOperationException("current function is set to null");
            }

            // Creates a preheader block to determine the direction of the loop.
            // The returned value evaluates to true if he given increment is positive.
            Value CreatePreheader(Value increment)
            {
                var preheaderName = this.BlockName("preheader");
                var preheaderBlock = this.CurrentFunction.AppendBasicBlock(preheaderName);

                // End the current block by branching to the preheader
                this.CurrentBuilder.Branch(preheaderBlock);

                // Preheader block: determine whether the step size is positive
                this.SetCurrentBlock(preheaderBlock);
                return this.CurrentBuilder.Compare(
                    IntPredicate.SignedGreaterThan,
                    increment,
                    this.Context.CreateConstant(0L));
            }

            Value EvaluateCondition(Value? loopVarIncreases, Value loopVariable)
            {
                var isSmallerOrEqualEnd = this.CurrentBuilder.Compare(
                    IntPredicate.SignedLessThanOrEqual, loopVariable, end);
                if (loopVarIncreases == null)
                {
                    // Step size is one by default, meaning the loop variable increases.
                    return isSmallerOrEqualEnd;
                }

                // If we increase the loop variable in each iteration (i.e. step is positive)
                // then we need to check that the current value is smaller than or equal to the end value,
                // and otherwise we check if it is larger than or equal to the end value.
                var isGreaterOrEqualEnd = this.CurrentBuilder.Compare(
                    IntPredicate.SignedGreaterThanOrEqual, loopVariable, end);
                return this.CurrentBuilder.Select(loopVarIncreases, isSmallerOrEqualEnd, isGreaterOrEqualEnd);
            }

            Value? loopVarIncreases = step == null ? null : CreatePreheader(step);
            step ??= this.Context.CreateConstant(1L);
            this.CreateForLoop(start, loopVar => EvaluateCondition(loopVarIncreases, loopVar), step, executeBody);
        }

        /// <summary>
        /// <inheritdoc cref=" IterateThroughArray(ArrayValue, Action{IValue})"/>
        /// The iteration may optionally compute an output value. If an <paramref name="initialOutputValue"/> is given,
        /// then a suitable phi node to hold the computed output will be instantiated and updated as part of the loop.
        /// </summary>
        /// <returns>The final value of the computed output, if <paramref name="initialOutputValue"/> and the value returned by <paramref name="executeBody"/> are non-null, and null otherwise.</returns>
        /// <param name="array">The array to iterate over.</param>
        /// <param name="initialOutputValue">The initial value for the output that will be computed by the loop.</param>
        /// <param name="executeBody">Given the array element and the current output value, executes the body of the loop and returns the updated output value.</param>
        /// <exception cref="InvalidOperationException">The current function or the current block is set to null.</exception>
        internal Value? IterateThroughArray(ArrayValue array, Value? initialOutputValue, Func<IValue, Value?, Value?> executeBody)
        {
            var startValue = this.Context.CreateConstant(0L);
            if (array.Length == startValue)
            {
                return initialOutputValue;
            }

            var increment = this.Context.CreateConstant(1L);
            var endValue = this.CurrentBuilder.Sub(array.Length, increment);

            Value EvaluateCondition(Value loopVariable) =>
                this.CurrentBuilder.Compare(IntPredicate.SignedLessThanOrEqual, loopVariable, endValue);

            Value? ExecuteBody(Value iterationIndex, Value? currentOutputValue) =>
                executeBody(array.GetArrayElement(iterationIndex), currentOutputValue);

            return this.CreateForLoop(startValue, EvaluateCondition, increment, initialOutputValue, ExecuteBody);
        }

        /// <summary>
        /// Iterates through the given array and executes the specifed body.
        /// Note that <paramref name="executeBody"/> is expected takes care of all necessary scope/memory management itself.
        /// </summary>
        /// <param name="array">The array to iterate over.</param>
        /// <param name="executeBody">The action to perform on each item (needs to include the scope management).</param>
        internal void IterateThroughArray(ArrayValue array, Action<IValue> executeBody)
        {
            Value? ExecuteBody(IValue arrayItem, Value? currentOutputValue)
            {
                executeBody(arrayItem);
                return null;
            }

            this.IterateThroughArray(array, null, ExecuteBody);
        }

        #endregion

        #region Type helpers

        /// <summary>
        /// Bitcasts the given value to the expected type if needed.
        /// Does nothing if the native type of the value already matches the expected type.
        /// </summary>
        internal Value CastToType(Value value, ITypeRef expectedType) =>
            value.NativeType.Equals(expectedType)
            ? value
            : this.CurrentBuilder.BitCast(value, expectedType);

        /// <returns>The kind of the Q# type on top of the expression type stack</returns>
        internal ResolvedType CurrentExpressionType() =>
            this.ExpressionTypeStack.Peek();

        /// <returns>The QIR equivalent for the Q# type that is on top of the expression type stack</returns>
        internal ITypeRef CurrentLlvmExpressionType() =>
            this.LlvmTypeFromQsharpType(this.ExpressionTypeStack.Peek());

        /// <inheritdoc cref="QirTypeTransformation.LlvmTypeFromQsharpType(ResolvedType)"/>
        internal ITypeRef LlvmTypeFromQsharpType(ResolvedType resolvedType) =>
            this.Types.Transform.LlvmTypeFromQsharpType(resolvedType);

        /// <summary>
        /// Computes the size in bytes of an LLVM type as an LLVM value.
        /// If the type isn't a simple pointer, integer, or double, we compute it using a standard LLVM idiom.
        /// </summary>
        /// <param name="type">The LLVM type to compute the size of</param>
        /// <param name="intType">The integer type to return</param>
        /// <returns>
        /// An LLVM value of the specified integer type - or i64 if none is specified - containing the size of the type in bytes
        /// </returns>
        internal Value ComputeSizeForType(ITypeRef type, ITypeRef? intType = null)
        {
            intType ??= this.Context.Int64Type;

            if (type.IsInteger)
            {
                return this.Context.CreateConstant(intType, (type.IntegerBitWidth + 7u) / 8u, false);
            }
            else if (type.IsDouble)
            {
                return this.Context.CreateConstant(intType, 8, false);
            }
            else if (type.IsPointer)
            {
                // We assume 64-bit address space
                return this.Context.CreateConstant(intType, 8, false);
            }
            else
            {
                // Everything else we let getelementptr compute for us
                var basePointer = Constant.ConstPointerToNullFor(type.CreatePointerType());

                // Note that we can't use this.GetTupleElementPtr here because we want to get a pointer to a second structure instance
                var firstPtr = this.CurrentBuilder.GetElementPtr(type, basePointer, new[] { this.Context.CreateConstant(0) });
                var first = this.CurrentBuilder.PointerToInt(firstPtr, intType);
                var secondPtr = this.CurrentBuilder.GetElementPtr(type, basePointer, new[] { this.Context.CreateConstant(1) });
                var second = this.CurrentBuilder.PointerToInt(secondPtr, intType);
                return this.CurrentBuilder.Sub(second, first);
            }
        }

        #endregion

        #region Inlining support

        /// <summary>
        /// Start inlining a callable invocation.
        /// This opens a new naming scope and increases the inlining level.
        /// </summary>
        internal void StartInlining()
        {
            this.ScopeMgr.OpenScope();
            this.inlineLevels.Push(this.Values.Unit);
        }

        /// <summary>
        /// Stop inlining a callable invocation.
        /// This pops the top naming scope and decreases the inlining level.
        /// The reference count of the return value for the inlining scope is increased before closing the scope,
        /// and the value is registered with the scope manager.
        /// </summary>
        internal IValue StopInlining()
        {
            var res = this.inlineLevels.Pop();
            this.ScopeMgr.CloseScope(res);
            this.ScopeMgr.RegisterValue(res);
            return res;
        }

        #endregion

        #region Block, scope, and value management

        /// <summary>
        /// Makes the given basic block current, creates a new builder for it, and makes that builder current.
        /// This method does not check to make sure that the block isn't already current.
        /// </summary>
        /// <param name="b">The block to make current</param>
        internal void SetCurrentBlock(BasicBlock b)
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
        internal BasicBlock AddBlockAfterCurrent(string name)
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

            var continueName = this.BlockName(name);
            return next == null
                ? this.CurrentFunction.AppendBasicBlock(continueName)
                : this.CurrentFunction.InsertBasicBlock(continueName, next);
        }

        /// <summary>
        /// Generates a unique name for a statement block.
        /// </summary>
        internal string BlockName(string name) =>
            UniqueName(name, this.uniqueLocalNames);

        /// <summary>
        /// Generates a unique name for a global constant or callable.
        /// </summary>
        internal string GlobalName(string name) =>
            UniqueName(name, this.uniqueGlobalNames);

        /// <summary>
        /// Generates a unique name for a local variable.
        /// </summary>
        internal string VariableName(string name)
        {
            var index = this.uniqueLocalNames.TryGetValue(name, out int n) ? n + 1 : 0;
            this.uniqueLocalNames[name] = index;
            return index == 0 ? name : $"{name.TrimEnd('_')}__{index}";
        }

        /// <summary>
        /// Processes an expression and returns its Value.
        /// </summary>
        /// <param name="ex">The expression to process</param>
        /// <returns>The LLVM Value that represents the result of the expression</returns>
        internal IValue EvaluateSubexpression(TypedExpression ex)
        {
            this.Transformation.Expressions.OnTypedExpression(ex);
            return this.ValueStack.Pop();
        }

        /// <summary>
        /// Evaluates the given expression and increases its reference count by 1,
        /// either by not registering a newly constructed item with the scope manager,
        /// or by explicitly increasing its reference count.
        /// Note that increasing the reference count may be delayed until needed.
        /// </summary>
        internal IValue BuildSubitem(TypedExpression ex)
        {
            this.ScopeMgr.OpenScope();
            this.Transformation.Expressions.OnTypedExpression(ex);
            var value = this.ValueStack.Pop();
            this.ScopeMgr.CloseScope(value);
            return value;
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose(bool disposing)
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

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
        }

        #endregion
    }
}
