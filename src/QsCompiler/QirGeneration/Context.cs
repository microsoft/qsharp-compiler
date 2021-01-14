// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Targeting;
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
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions")]
    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1516:Elements should be separated by blank line")]
    public sealed class GenerationContext : IDisposable
    {
        private static readonly ILibLlvm LibContext;

        static GenerationContext()
        {
            LibContext = Library.InitializeLLVM();
            LibContext.RegisterTarget(CodeGenTarget.Native);
        }

        /// <summary>
        /// This type is used to map Q# types to interop-friendly types.
        /// </summary>
        private class ArgMapping
        {
            internal readonly string BaseName;

            /// <summary>
            /// The first item contains the array type, and the second item contains the array count name.
            /// If <see cref="arrayInfo"/> is not null, then <see cref="structInfo"/> is null.
            /// </summary>
            private readonly (ITypeRef, string)? arrayInfo;

            /// <summary>
            /// Contains the struct type.
            /// If <see cref="structInfo"/> is not null, then <see cref="arrayInfo"/> is null.
            /// </summary>
            private readonly ITypeRef? structInfo;

            internal bool IsArray => this.arrayInfo != null;
            internal ITypeRef ArrayType => this.arrayInfo?.Item1 ?? throw new InvalidOperationException("not an array");
            internal string ArrayCountName => this.arrayInfo?.Item2 ?? throw new InvalidOperationException("not an array");

            internal bool IsStruct => this.structInfo != null;
            internal ITypeRef StructType => this.structInfo ?? throw new InvalidOperationException("not a struct");

            private ArgMapping(string baseName, (ITypeRef, string)? arrayInfo = null, ITypeRef? structInfo = null)
            {
                this.BaseName = baseName;
                this.arrayInfo = arrayInfo;
                this.structInfo = structInfo;
            }

            internal static ArgMapping Create(string baseName) =>
                new ArgMapping(baseName);

            internal ArgMapping WithArrayInfo(ITypeRef arrayType, string arrayCountName) =>
                new ArgMapping(this.BaseName, arrayInfo: (arrayType, arrayCountName));

            internal ArgMapping WithStructInfo(ITypeRef arrayType, string arrayCountName) =>
                new ArgMapping(this.BaseName, arrayInfo: (arrayType, arrayCountName));
        }

        #region Member variables

        /// <summary>
        /// The configuration for QIR generation.
        /// </summary>
        public readonly Configuration Config;

        /// <summary>
        /// The context used for QIR generation.
        /// </summary>
        /// <inheritdoc cref="Ubiquity.NET.Llvm.Context"/>
        public readonly Context Context;

        /// <summary>
        /// The module used for QIR generation.
        /// Generated functions to facilitate interoperability are created in a separate <see cref="InteropModule"/>.
        /// </summary>
        /// <inheritdoc cref="BitcodeModule"/>
        public readonly BitcodeModule Module;

        /// <summary>
        /// The module used for constructing functions to facilitate interoperability.
        /// </summary>
        /// <inheritdoc cref="BitcodeModule"/>
        public readonly BitcodeModule InteropModule;

        /// <summary>
        /// The used QIR types.
        /// </summary>
        public readonly Types Types;

        /// <summary>
        /// The used QIR constants.
        /// </summary>
        public readonly Constants Constants;

        /// <summary>
        /// Tools to construct and handle values throughout QIR emission.
        /// </summary>
        internal readonly QirValues Values;

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
        internal ITypeRef? BuiltType { get; set; }

        internal readonly ScopeManager ScopeMgr;
        internal readonly Stack<IValue> ValueStack;
        internal readonly Stack<ResolvedType> ExpressionTypeStack;

        /// <summary>
        /// We support nested inlining and hence keep a stack with the information for each inline level.
        /// Each item in the stack contains a an identifier for the inlining that is unique within the
        /// current callable and can be used to construct suitable variable names for inlined variables.
        /// It also contains the return value for that inline level.
        /// While this is currently not necessary since we currently require that any inlined callable either
        /// returns unit or has exactly one return statement, this restriction could be lifted in the future.
        /// </summary>
        private readonly Stack<(string, IValue)> inlineLevels;
        private readonly Dictionary<string, int> uniqueNameIds = new Dictionary<string, int>();

        private readonly Dictionary<string, ITypeRef> interopType = new Dictionary<string, ITypeRef>();
        private readonly Dictionary<string, (QsCallable, GlobalVariable)> functionWrappers = new Dictionary<string, (QsCallable, GlobalVariable)>();
        private readonly List<(IrFunction, Action<IReadOnlyList<Argument>>)> liftedPartialApplications = new List<(IrFunction, Action<IReadOnlyList<Argument>>)>();
        private readonly Dictionary<ResolvedType, IrFunction> refCountFunctions = new Dictionary<ResolvedType, IrFunction>();

        #endregion

        #region Control flow context tracking

        internal bool IsWithinLoop = false;

        private (int, Stack<int>) branchIds = (0, new Stack<int>(new[] { 0 }));
        internal int CurrentBranch => this.branchIds.Item2.Peek();
        internal bool IsOpenBranch(int id) => this.branchIds.Item2.Contains(id);

        internal void StartBranch()
        {
            var (lastUsedId, stack) = this.branchIds;
            stack.Push(lastUsedId + 1);
            this.branchIds = (stack.Peek(), stack);
        }

        internal void EndBranch() =>
            this.branchIds.Item2.Pop();

        #endregion

        /// <summary>
        /// Constructs a new generation context.
        /// Before using the constructed context, the following needs to be done:
        /// 1.) the transformation needs to be set by calling <see cref="SetTransformation"/>,
        /// 2.) the runtime library needs to be initialized by calling <see cref="InitializeRuntimeLibrary"/>, and
        /// 3.) the quantum instructions set needs to be registered by calling <see cref="RegisterQuantumInstructionSet"/>.
        /// </summary>
        /// <param name="compilation">The compilation unit for which QIR is generated.</param>
        /// <param name="config">The configuration for QIR generation.</param>
        internal GenerationContext(IEnumerable<QsNamespace> syntaxTree, Configuration config)
        {
            this.Config = config;
            this.Context = new Context();
            this.Module = this.Context.CreateBitcodeModule();
            this.InteropModule = this.Context.CreateBitcodeModule("bridge");

            this.Types = new Types(this.Context);
            this.Constants = new Constants(this.Context, this.Module, this.Types);
            this.Values = new QirValues(this, this.Constants);
            this.transformation = null; // needs to be set by the instantiating transformation

            this.CurrentBuilder = new InstructionBuilder(this.Context);
            this.ValueStack = new Stack<IValue>();
            this.ExpressionTypeStack = new Stack<ResolvedType>();
            this.inlineLevels = new Stack<(string, IValue)>();
            this.ScopeMgr = new ScopeManager(this);

            this.globalCallables = syntaxTree.GlobalCallableResolutions();
            this.globalTypes = syntaxTree.GlobalTypeResolutions();

            this.runtimeLibrary = new FunctionLibrary(
                this.Module,
                s => Callables.FunctionName(Component.RuntimeLibrary, s));
            this.quantumInstructionSet = new FunctionLibrary(
                this.Module,
                s => Callables.FunctionName(Component.QuantumInstructionSet, s));
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

        #region Static members

        /// <summary>
        /// Cleans a namespace name by replacing periods with double underscores.
        /// </summary>
        /// <param name="namespaceName">The namespace name to clean</param>
        /// <returns>The cleaned name</returns>
        internal static string FlattenNamespaceName(string namespaceName) =>
            namespaceName.Replace(".", "__");

        /// <summary>
        /// Generates a mangled name for a callable specialization.
        /// QIR mangled names are the namespace name, with periods replaced by double underscores, followed
        /// by a double underscore and the callable name, then another double underscore and the name of the
        /// callable kind ("body", "adj", "ctl", or "ctladj").
        /// </summary>
        /// <param name="fullName">The callable's qualified name</param>
        /// <param name="kind">The specialization kind</param>
        /// <returns>The mangled name for the specialization</returns>
        public static string FunctionName(QsQualifiedName fullName, QsSpecializationKind kind)
        {
            var suffix = InferTargetInstructions.SpecializationSuffix(kind).ToLowerInvariant();
            return $"{FlattenNamespaceName(fullName.Namespace)}__{fullName.Name}{suffix}";
        }

        /// <summary>
        /// Generates a mangled name for a callable specialization wrapper.
        /// Wrapper names are the mangled specialization name followed by double underscore and "wrapper".
        /// </summary>
        /// <param name="namespaceName">The namespace of the Q# callable.</param>
        /// <param name="name">The unqualified name of the Q# callable.</param>
        /// <param name="kind">The specialization kind</param>
        /// <returns>The mangled name for the wrapper</returns>
        public static string FunctionWrapperName(QsQualifiedName fullName, QsSpecializationKind kind) =>
            $"{FunctionName(fullName, kind)}__wrapper";

        /// <returns>
        /// Returns true and the target instruction name for the callable as out parameter
        /// if a target instruction exists for the callable.
        /// Returns false otherwise.
        /// </returns>
        internal static bool TryGetTargetInstructionName(QsCallable callable, [MaybeNullWhen(false)] out string instructionName)
        {
            if (SymbolResolution.TryGetTargetInstructionName(callable.Attributes) is var att && att.IsValue)
            {
                instructionName = att.Item;
                return true;
            }
            else
            {
                instructionName = null;
                return false;
            }
        }

        /// <summary>
        /// Order of specializations in the constant array that contains the fours IrFunctions
        /// associated with a callable.
        /// </summary>
        public static readonly ImmutableArray<QsSpecializationKind> FunctionArray = ImmutableArray.Create(
            QsSpecializationKind.QsBody,
            QsSpecializationKind.QsAdjoint,
            QsSpecializationKind.QsControlled,
            QsSpecializationKind.QsControlledAdjoint);

        #endregion

        #region Initialization and emission

        /// <summary>
        /// Initializes the QIR runtime library.
        /// </summary>
        public void InitializeRuntimeLibrary()
        {
            // int library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.IntPower, this.Types.Int, this.Types.Int, this.Context.Int32Type);

            // result library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultUpdateReferenceCount, this.Context.VoidType, this.Types.Result, this.Types.Int);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultEqual, this.Context.BoolType, this.Types.Result, this.Types.Result);

            // string library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringCreate, this.Types.String, this.Context.Int32Type, this.Types.DataArrayPointer);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringUpdateReferenceCount, this.Context.VoidType, this.Types.String, this.Types.Int);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringConcatenate, this.Types.String, this.Types.String, this.Types.String);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringEqual, this.Context.BoolType, this.Types.String, this.Types.String);

            // to-string conversion functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntToString, this.Types.String, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BoolToString, this.Types.String, this.Context.BoolType);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.DoubleToString, this.Types.String, this.Context.DoubleType);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.IntToString, this.Types.String, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.PauliToString, this.Types.String, this.Types.Pauli);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.QubitToString, this.Types.String, this.Types.Qubit);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.RangeToString, this.Types.String, this.Types.Range);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultToString, this.Types.String, this.Types.Result);

            // bigint library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntCreateI64, this.Types.BigInt, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntCreateArray, this.Types.BigInt, this.Context.Int32Type, this.Types.DataArrayPointer);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntUpdateReferenceCount, this.Context.VoidType, this.Types.BigInt, this.Types.Int);
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
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleUpdateAccessCount, this.Context.VoidType, this.Types.Tuple, this.Types.Int);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleUpdateReferenceCount, this.Context.VoidType, this.Types.Tuple, this.Types.Int);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleCopy, this.Types.Tuple, this.Types.Tuple, this.Context.BoolType);

            // array library functions
            this.runtimeLibrary.AddVarArgsFunction(RuntimeLibrary.ArrayCreate, this.Types.Array, this.Context.Int32Type, this.Context.Int32Type);
            this.runtimeLibrary.AddVarArgsFunction(RuntimeLibrary.ArrayGetElementPtr, this.Context.Int8Type.CreatePointerType(), this.Types.Array);
            // TODO: figure out how to call a varargs function and get rid of these two functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayCreate1d, this.Types.Array, this.Context.Int32Type, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayGetElementPtr1d, this.Context.Int8Type.CreatePointerType(), this.Types.Array, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayUpdateAccessCount, this.Context.VoidType, this.Types.Array, this.Types.Int);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayUpdateReferenceCount, this.Context.VoidType, this.Types.Array, this.Types.Int);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayCopy, this.Types.Array, this.Types.Array, this.Context.BoolType);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayConcatenate, this.Types.Array, this.Types.Array, this.Types.Array);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArraySlice, this.Types.Array, this.Context.Int32Type, this.Types.Array, this.Types.Range);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArraySlice1d, this.Types.Array, this.Types.Array, this.Types.Range, this.Context.BoolType);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayGetSize1d, this.Context.Int64Type, this.Types.Array);

            // callable library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableCreate, this.Types.Callable, this.Types.CallableTable.CreatePointerType(), this.Types.CallableMemoryManagementTable.CreatePointerType(), this.Types.Tuple);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableInvoke, this.Context.VoidType, this.Types.Callable, this.Types.Tuple, this.Types.Tuple);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableCopy, this.Types.Callable, this.Types.Callable, this.Context.BoolType);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableMakeAdjoint, this.Context.VoidType, this.Types.Callable);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableMakeControlled, this.Context.VoidType, this.Types.Callable);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableUpdateReferenceCount, this.Context.VoidType, this.Types.Callable, this.Types.Int);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableMemoryManagement, this.Context.VoidType, this.Context.Int32Type, this.Types.Callable, this.Types.Int);

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
            foreach (var c in this.globalCallables.Values)
            {
                if (TryGetTargetInstructionName(c, out var name))
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
        /// Writes the current content to the output file.
        /// </summary>
        public void Emit(string fileName, bool overwrite = true, bool generateInteropWrappers = false)
        {
            var bridgeFile = Path.Combine(
                Path.GetDirectoryName(fileName),
                Path.GetFileNameWithoutExtension(fileName) + "_bridge.ll");
            var existing = new[]
            {
                File.Exists(fileName) ? fileName : null,
                generateInteropWrappers && File.Exists(bridgeFile) ? bridgeFile : null
            };

            if (!overwrite && existing.Any(s => s != null))
            {
                var argStr = string.Join(", ", existing.Where(s => s == null));
                throw new ArgumentException($"The following file(s) already exist(s): {argStr}");
            }

            this.GenerateRequiredFunctions();

            if (!this.Module.Verify(out string validationErrors))
            {
                File.WriteAllText(fileName, $"LLVM errors:{Environment.NewLine}{validationErrors}");
            }

            if (!this.Module.WriteToTextFile(fileName, out string errorMessage))
            {
                throw new IOException(errorMessage);
            }

            // Generate the wrappers for the runtime library that were used, if requested
            if (generateInteropWrappers)
            {
                foreach (var kvp in this.runtimeLibrary)
                {
                    this.GenerateInterop(kvp.Value, kvp.Key);
                }

                foreach (var c in this.globalCallables.Values)
                {
                    if (TryGetTargetInstructionName(c, out var name))
                    {
                        var func = this.quantumInstructionSet.GetOrCreateFunction(name);
                        this.GenerateInterop(func, name);
                    }
                }

                if (!this.InteropModule.Verify(out string bridgeValidationErrors))
                {
                    File.WriteAllText(bridgeFile, $"LLVM errors:{Environment.NewLine}{bridgeValidationErrors}");
                }
                else if (!this.InteropModule.WriteToTextFile(bridgeFile, out string bridgeError))
                {
                    throw new IOException(bridgeError);
                }
            }
        }

        #endregion

        #region Interop utils

        /// <summary>
        /// Generates an interop-friendly wrapper around the Q# entry point using the configured type mapping.
        /// </summary>
        /// <param name="qualifiedName">The namespace-qualified name of the Q# entry point</param>
        public void GenerateEntryPoint(QsQualifiedName qualifiedName)
        {
            // Unfortunately this is different enough from all of the other type mapping we do to require
            // an actual different routine. Blech...
            bool MapType(ArgumentTuple t, List<ITypeRef> typeList, List<string> nameList, List<ArgMapping> mappingList)
            {
                bool changed = false;

                if (t is ArgumentTuple.QsTuple tuple)
                {
                    foreach (ArgumentTuple inner in tuple.Item)
                    {
                        changed |= MapType(inner, typeList, nameList, mappingList);
                    }
                }
                else if (t is ArgumentTuple.QsTupleItem item && item.Item.VariableName is QsLocalSymbol.ValidName varName)
                {
                    var baseName = varName.Item;
                    var map = ArgMapping.Create(baseName);
                    switch (item.Item.Type.Resolution)
                    {
                        case ResolvedTypeKind.ArrayType array:
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
                        case ResolvedTypeKind.TupleType _:
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

            if (this.TryGetFunction(qualifiedName, QsSpecializationKind.QsBody, out IrFunction? func)
                && this.TryGetGlobalCallable(qualifiedName, out QsCallable? callable))
            {
                var epName = $"{qualifiedName.Namespace.Replace(".", "__")}__{qualifiedName.Name}";

                // Check to see if the arg list needs mapping to more C-friendly types
                // TODO: handle complicated return types
                var mappedArgList = new List<ITypeRef>();
                var mappedNameList = new List<string>();
                var mappingList = new List<ArgMapping>();
                var arraysToReleaseList = new List<Value>();
                var mappedResultType = this.MapToInteropType(func.ReturnType);
                if (MapType(callable.ArgumentTuple, mappedArgList, mappedNameList, mappingList) ||
                    (mappedResultType != func.ReturnType))
                {
                    this.StartFunction();
                    var epFunc = this.Module.CreateFunction(epName, this.Context.GetFunctionType(mappedResultType, mappedArgList));
                    var namedValues = new Dictionary<string, Value>();
                    for (var i = 0; i < mappedNameList.Count; i++)
                    {
                        epFunc.Parameters[i].Name = mappedNameList[i];
                        namedValues[epFunc.Parameters[i].Name] = epFunc.Parameters[i];
                    }
                    var entryBlock = epFunc.AppendBasicBlock("entry");
                    this.SetCurrentBlock(entryBlock);

                    // Build the argument list for the inner function
                    var argValueList = new List<Value>();
                    foreach (var mapping in mappingList)
                    {
                        if (mapping.IsArray)
                        {
                            var elementSize64 = this.ComputeSizeForType(mapping.ArrayType);
                            var elementSize = this.CurrentBuilder.IntCast(elementSize64, this.Context.Int32Type, false);
                            var length = namedValues[mapping.ArrayCountName];
                            var array = this.CurrentBuilder.Call(this.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCreate1d), elementSize, length);
                            argValueList.Add(array);
                            arraysToReleaseList.Add(array);
                            // Fill in the array if the length is >0. Since the QIR array is new, we assume we can use memcpy.
                            var copyBlock = epFunc.AppendBasicBlock("copy");
                            var nextBlock = epFunc.AppendBasicBlock("next");
                            var cond = this.CurrentBuilder.Compare(IntPredicate.SignedGreaterThan, length, this.Context.CreateConstant(0L));
                            this.CurrentBuilder.Branch(cond, copyBlock, nextBlock);

                            this.CurrentBuilder = new InstructionBuilder(copyBlock);
                            var destBase = this.CurrentBuilder.Call(this.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d), array, this.Context.CreateConstant(0L));
                            this.CurrentBuilder.MemCpy(
                                destBase,
                                namedValues[mapping.BaseName],
                                this.CurrentBuilder.Mul(length, this.CurrentBuilder.IntCast(elementSize, this.Context.Int64Type, true)),
                                false);
                            this.CurrentBuilder.Branch(nextBlock);
                            this.SetCurrentBlock(nextBlock);
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

                    Value result = this.CurrentBuilder.Call(func, argValueList);
                    // TODO: release array

                    if (func.ReturnType.IsVoid)
                    {
                        this.CurrentBuilder.Return();
                    }
                    else
                    {
                        if (mappedResultType != func.ReturnType)
                        {
                            result = this.CurrentBuilder.BitCast(result, mappedResultType);
                        }
                        this.CurrentBuilder.Return(result);
                    }

                    // Mark the function as an entry point
                    epFunc.AddAttributeAtIndex(
                        FunctionAttributeIndex.Function,
                        this.Context.CreateAttribute("EntryPoint"));
                }
                else
                {
                    this.Module.AddAlias(func, epName).Linkage = Linkage.External;
                    // Mark the function as an entry point
                    func.AddAttributeAtIndex(
                        FunctionAttributeIndex.Function,
                        this.Context.CreateAttribute("EntryPoint"));
                }
            }
        }

        /// <summary>
        /// Generates a stub implementation for a runtime function or quantum instruction using the specified type
        /// mappings for interoperability. Note that the create functions go into a separate module from the other QIR code.
        /// </summary>
        /// <param name="func">The function to generate a stub for</param>
        /// <param name="baseName">The function that the stub should call</param>
        private void GenerateInterop(IrFunction func, string baseName)
        {
            // TODO: why do we need both GenerateEntryPoint and GenerateInterop?

            func = this.InteropModule.CreateFunction(func.Name, func.Signature);

            var mappedResultType = this.MapToInteropType(func.ReturnType);
            var argTypes = func.Parameters.Select(p => p.NativeType).ToArray();
            var mappedArgTypes = argTypes.Select(this.MapToInteropType).ToArray();

            var interopFunction = this.InteropModule.CreateFunction(
                baseName,
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
            if (t == this.Types.Result)
            {
                typeName = TypeNames.Result;
            }
            else if (t == this.Types.Array)
            {
                typeName = TypeNames.Array;
            }
            else if (t == this.Types.Pauli)
            {
                typeName = TypeNames.Pauli;
            }
            // we use 32 bit ints in some cases, e.g. for exponents
            else if (t == this.Types.Int || t == this.Context.Int32Type)
            {
                typeName = TypeNames.Int;
            }
            else if (t == this.Types.Double)
            {
                typeName = TypeNames.Double;
            }
            else if (t == this.Types.Bool)
            {
                typeName = TypeNames.Bool;
            }
            else if (t == this.Types.BigInt)
            {
                typeName = TypeNames.BigInt;
            }
            else if (t == this.Types.String)
            {
                typeName = TypeNames.String;
            }
            else if (t == this.Types.Qubit)
            {
                typeName = TypeNames.Qubit;
            }
            else if (t == this.Types.Callable)
            {
                typeName = TypeNames.Callable;
            }
            else if (t == this.Types.Range)
            {
                typeName = TypeNames.Range;
            }
            else if (t == this.Types.Tuple)
            {
                typeName = TypeNames.Tuple;
            }
            // todo: Currently, e.g. void (this.Context.VoidType) is not covered,
            // and for some reason we end up with i8* here as well. It would be good to cover everything and throw if something is not covered.

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
        /// Tries to find a global Q# callable in the current compilation.
        /// </summary>
        /// <param name="nsName">The callable's namespace</param>
        /// <param name="name">The callable's name</param>
        /// <param name="callable">The Q# callable, if found</param>
        /// <returns>true if the callable is found, false if not</returns>
        internal bool TryGetGlobalCallable(QsQualifiedName fullName, [MaybeNullWhen(false)] out QsCallable callable) =>
            this.globalCallables.TryGetValue(fullName, out callable);

        /// <summary>
        /// Tries to find a Q# user-defined type in the current compilation.
        /// </summary>
        /// <param name="nsName">The UDT's namespace</param>
        /// <param name="name">The UDT's name</param>
        /// <param name="udt">The Q# UDT< if found</param>
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

            this.uniqueNameIds.Clear();
            this.ScopeMgr.OpenScope();
        }

        /// <summary>
        /// Ends a QIR function by finishing the current basic block, closing the current scope in teh scope manager
        /// and closing a naming scope.
        /// </summary>
        /// <returns>true if the function has been properly ended</returns>
        /// <exception cref="InvalidOperationException">The current function or the current block is set to null.</exception>
        internal bool EndFunction()
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

            if (!HasAPredecessor(this.CurrentBlock)
                && this.CurrentFunction.BasicBlocks.Count > 1)
            {
                this.CurrentFunction.BasicBlocks.Remove(this.CurrentBlock);
            }
            else if (this.CurrentBlock.Terminator == null)
            {
                this.CurrentBuilder.Return();
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
                // when exiting the scope or a reference will be added by ExitScope
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
                var (inlineId, current) = this.inlineLevels.Pop();
                if (current.Value != this.Constants.UnitValue)
                {
                    throw new InvalidOperationException("return value for current inline level already defined");
                }

                this.inlineLevels.Push((inlineId, result));
            }
        }

        /// <summary>
        /// Adds the declaration for a QIR function to the current module.
        /// Usually <see cref="GenerateFunctionHeader"/> is used, which generates the start of the actual definition.
        /// This method is primarily useful for Q# specializations with external or intrinsic implementations, which get
        /// generated as declarations with no definition.
        /// </summary>
        /// <param name="spec">The Q# specialization for which to register a function</param>
        /// <param name="argTuple">The specialization's argument tuple</param>
        internal IrFunction RegisterFunction(QsSpecialization spec)
        {
            var name = FunctionName(spec.Parent, spec.Kind);
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
        internal void GenerateFunctionHeader(QsSpecialization spec, ArgumentTuple argTuple, bool deconstuctArgument = true)
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
                    this.ScopeMgr.RegisterVariable(name, innerTuple);
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
                        this.ScopeMgr.RegisterVariable(argName, argValue);
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
                        this.ScopeMgr.RegisterVariable(argName, element);
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
        /// <param name="udt">The Q# user-defined type</param>
        internal void GenerateConstructor(QsSpecialization spec, ArgumentTuple argTuple)
        {
            this.GenerateFunctionHeader(spec, argTuple, deconstuctArgument: false);

            // create the udt (output value)
            if (spec.Signature.ArgumentType.Resolution.IsUnitType)
            {
                this.AddReturn(this.Values.Unit, returnsVoid: false);
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
            var funcName = FunctionWrapperName(new QsQualifiedName("Lifted", name), kind);
            IrFunction func = this.Module.CreateFunction(funcName, this.Types.FunctionSignature);
            this.liftedPartialApplications.Add((func, body));
            return func;
        }

        /// <summary>
        /// Tries to get the QIR function for a Q# specialization by name so it can be called.
        /// If the function hasn't been generated yet, false is returned.
        /// </summary>
        /// <param name="namespaceName">The callable's namespace</param>
        /// <param name="name">The Q# callable's name</param>
        /// <param name="kind">The Q# specialization kind</param>
        /// <param name="function">Gets filled in with the LLVM function object if it exists already</param>
        /// <returns>true if the function has already been declared/defined, or false otherwise</returns>
        private bool TryGetFunction(QsQualifiedName callableName, QsSpecializationKind kind, [MaybeNullWhen(false)] out IrFunction function)
        {
            var fullName = FunctionName(callableName, kind);
            return this.Module.TryGetFunction(fullName, out function);
        }

        /// <summary>
        /// Gets the QIR function for a Q# specialization by name so it can be called.
        /// If the function hasn't been generated yet, its declaration is generated so that it can be called.
        /// </summary>
        /// <param name="namespaceName">The callable's namespace</param>
        /// <param name="name">The Q# callable's name</param>
        /// <param name="kind">The Q# specialization kind</param>
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
        private GlobalVariable CreateCallableTable(string name, Func<QsSpecializationKind, IrFunction?> getSpec)
        {
            var funcs = new Constant[4];
            for (var index = 0; index < 4; index++)
            {
                funcs[index] = getSpec(FunctionArray[index])
                    ?? Constant.ConstPointerToNullFor(this.Types.FunctionSignature.CreatePointerType());
            }

            // Build the callable table
            var array = ConstantArray.From(this.Types.FunctionSignature.CreatePointerType(), funcs);
            return this.Module.AddGlobal(array.NativeType, true, Linkage.DllExport, array, name);
        }

        /// <inheritdoc cref="CreateCallableTable"/>
        internal GlobalVariable GetOrCreateCallableTable(string name, Func<QsSpecializationKind, IrFunction?> getSpec) =>
            this.CreateCallableTable(name, getSpec);

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
            var key = $"{FlattenNamespaceName(callable.FullName.Namespace)}__{callable.FullName.Name}";
            if (this.functionWrappers.TryGetValue(key, out (QsCallable, GlobalVariable) item))
            {
                return item.Item2;
            }
            else
            {
                IrFunction? BuildSpec(QsSpecializationKind kind) =>
                    callable.Specializations.Any(spec => spec.Kind == kind &&
                    (spec.Implementation.IsProvided || spec.Implementation.IsIntrinsic))
                        ? this.Module.CreateFunction(FunctionWrapperName(callable.FullName, kind), this.Types.FunctionSignature)
                        : null;
                var table = this.CreateCallableTable(key, BuildSpec);
                this.functionWrappers.Add(key, (callable, table));
                return table;
            }
        }

        /// <summary>
        /// If a constant array with the IrFunctions for managing access and reference counts
        /// for the given capture tuple already exists, returns the corresponding global variable.
        /// If no such array exists, creates a constant array and instantiates the necessary functions,
        /// queues the generation of their implementations, and returns the created global constant.
        /// The generation of the implementations is queued such that the current context is not modified
        /// beyond adding the corresponding constant and functions declarations.
        /// The table contains the function for updating the reference count as the first item,
        /// and a null pointer for the function to update the access count as the second item.
        /// If the given capture is null, returns a null pointer of suitable type.
        /// </summary>
        internal Constant GetOrCreateCallableMemoryManagementTable(TupleValue? capture)
        {
            if (capture == null)
            {
                return Constant.ConstPointerToNullFor(this.Types.CallableMemoryManagementTable.CreatePointerType());
            }

            var nullPointer = Constant.ConstPointerToNullFor(this.Types.CaptureCountFunction.CreatePointerType());
            var funcs = new Constant[2] { nullPointer, nullPointer };

            var type = StripPositionInfo.Apply(capture.QSharpType);
            if (this.refCountFunctions.TryGetValue(type, out IrFunction func))
            {
                funcs[0] = func;
            }
            else
            {
                var funcName = this.GenerateUniqueName("ReferencesManagement");
                func = this.Module.CreateFunction(funcName, this.Types.CaptureCountFunction);
                this.refCountFunctions.Add(type, func);
                funcs[0] = func;
            }

            var name = this.GenerateUniqueName("MemoryManagement");
            var array = ConstantArray.From(this.Types.CaptureCountFunction.CreatePointerType(), funcs);
            return this.Module.AddGlobal(array.NativeType, true, Linkage.DllExport, array, name);
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
        private void GenerateRequiredFunctions()
        {
            TupleValue GetArgumentTuple(ResolvedType type, Value argTuple)
            {
                var itemTypes =
                    type.Resolution.IsUnitType ? ImmutableArray.Create<ResolvedType>() :
                    type.Resolution is ResolvedTypeKind.TupleType argItemTypes ? argItemTypes.Item :
                    ImmutableArray.Create(type);
                return this.Values.FromTuple(argTuple, itemTypes);
            }

            // result value contains the return value, and output tuple is the tuple where that value should be stored
            void PopulateResultTuple(ResolvedType resultType, Value resultValue, Value outputTuple)
            {
                var resultTupleItemTypes = resultType.Resolution is ResolvedTypeKind.TupleType ts
                    ? ts.Item
                    : ImmutableArray.Create(resultType);
                var qirOutputTuple = this.Values.FromTuple(outputTuple, resultTupleItemTypes);

                if (resultType.Resolution is ResolvedTypeKind.TupleType tupleType)
                {
                    var resultTuple = this.Values.FromTuple(resultValue, resultTupleItemTypes);
                    for (int j = 0; j < tupleType.Item.Length; j++)
                    {
                        var itemOutputPointer = qirOutputTuple.GetTupleElementPointer(j);
                        var resItem = resultTuple.GetTupleElement(j);
                        itemOutputPointer.StoreValue(resItem);
                    }
                }
                else if (!resultType.Resolution.IsUnitType)
                {
                    var result = this.Values.From(resultValue, resultType);
                    var outputPointer = qirOutputTuple.GetTupleElementPointer(0);
                    outputPointer.StoreValue(result);
                }
            }

            Value GenerateBaseMethodCall(QsCallable callable, QsSpecializationKind specKind, List<Value> args)
            {
                if (TryGetTargetInstructionName(callable, out var name))
                {
                    var func = this.GetOrCreateTargetInstruction(name);
                    return specKind == QsSpecializationKind.QsBody
                        ? this.CurrentBuilder.Call(func, args.ToArray())
                        : throw new ArgumentException($"non-body specialization for target instruction");
                }
                else
                {
                    return this.TryGetFunction(callable.FullName, specKind, out IrFunction? func)
                        ? this.CurrentBuilder.Call(func, args.ToArray())
                        : throw new InvalidOperationException($"No function defined for {callable.FullName} {specKind}");
                }
            }

            void GenerateFunction(IrFunction func, string[] argNames, Action<IReadOnlyList<Argument>> executeBody)
            {
                this.CurrentFunction = func;
                for (var i = 0; i < argNames.Length; ++i)
                {
                    this.CurrentFunction.Parameters[i].Name = argNames[i];
                }
                this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
                this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);

                this.ScopeMgr.OpenScope();
                executeBody(this.CurrentFunction.Parameters);
                this.ScopeMgr.CloseScope(isTerminated: false);
                this.CurrentBuilder.Return();
            }

            foreach (var (callable, _) in this.functionWrappers.Values)
            {
                foreach (var spec in callable.Specializations)
                {
                    var fullName = FunctionWrapperName(callable.FullName, spec.Kind);
                    if ((spec.Implementation.IsProvided || spec.Implementation.IsIntrinsic)
                        && this.Module.TryGetFunction(fullName, out IrFunction? func))
                    {
                        GenerateFunction(func, new[] { "capture-tuple", "arg-tuple", "result-tuple" }, parameters =>
                        {
                            var argTuple = GetArgumentTuple(spec.Signature.ArgumentType, parameters[1]);
                            var argList = new List<Value>(argTuple.GetTupleElements().Select(qirValue => qirValue.Value));
                            var result = GenerateBaseMethodCall(callable, spec.Kind, argList);
                            PopulateResultTuple(callable.Signature.ReturnType, result, parameters[2]);
                        });
                    }
                    else
                    {
                        throw new InvalidOperationException($"failed to generate {fullName}");
                    }
                }
            }

            foreach (var (func, body) in this.liftedPartialApplications)
            {
                GenerateFunction(func, new[] { "capture-tuple", "arg-tuple", "result-tuple" }, body);
            }

            foreach (var (type, func) in this.refCountFunctions)
            {
                GenerateFunction(func, new[] { "capture-tuple", "count-change" }, parameters =>
                {
                    var capture = GetArgumentTuple(type, parameters[0]);
                    var argument = this.Values.FromSimpleValue(parameters[1], ResolvedType.New(ResolvedTypeKind.Int));
                    this.ScopeMgr.UpdateReferenceCount(argument, capture);
                });
            }
        }

        #endregion

        #region Iteration

        /// <summary>
        /// Creates a for-loop that breaks based on a condition.
        /// </summary>
        /// <param name="startValue">The value to which the loop variable will be instantiated</param>
        /// <param name="evaluateCondition">Given the current value of the loop variable, determines whether the next loop iteration should be entered</param>
        /// <param name="increment">The value that is added to the loop variable after each iteration </param>
        /// <param name="executeBody">Given the current value of the loop variable, executes the body of the loop</param>
        /// <exception cref="InvalidOperationException">The current function or the current block is set to null.</exception>
        internal void CreateForLoop(Value startValue, Func<Value, Value> evaluateCondition, Value increment, Action<Value> executeBody)
        {
            if (this.CurrentFunction == null || this.CurrentBlock == null)
            {
                throw new InvalidOperationException("current function is set to null");
            }

            // Contains the loop header that creates the phi-node, evaluates the condition,
            // and then branches into the body or exits the loop depending on whether the condition evaluates to true.
            var headerName = this.GenerateUniqueName("header");
            var headerBlock = this.CurrentFunction.AppendBasicBlock(headerName);

            // Contains the body of the loop, which has its own naming scope.
            var bodyName = this.GenerateUniqueName("body");
            var bodyBlock = this.CurrentFunction.AppendBasicBlock(bodyName);

            // Increments the loop variable and then branches into the header block
            // which determines whether to enter the next iteration.
            var exitingName = this.GenerateUniqueName("exiting");
            var exitingBlock = this.CurrentFunction.AppendBasicBlock(exitingName);

            // Empty block that will be entered when the loop exits that may get populated by subsequent computations.
            var exitName = this.GenerateUniqueName("exit");
            var exitBlock = this.CurrentFunction.AppendBasicBlock(exitName);

            PhiNode PopulateLoopHeader(Value startValue, Func<Value, Value> evaluateCondition)
            {
                // End the current block by branching into the header of the loop
                BasicBlock precedingBlock = this.CurrentBlock ?? throw new InvalidOperationException("no preceding block");
                this.CurrentBuilder.Branch(headerBlock);

                // Header block: create/update phi node representing the iteration variable and evaluate the condition
                this.SetCurrentBlock(headerBlock);
                var loopVariable = this.CurrentBuilder.PhiNode(this.Types.Int);
                loopVariable.AddIncoming(startValue, precedingBlock);

                // The OpenScope is almost certainly unnecessary, but it is technically possible for the condition
                // expression to perform an allocation that needs to get cleaned up, so...
                this.ScopeMgr.OpenScope();
                var condition = evaluateCondition(loopVariable);
                this.ScopeMgr.CloseScope(this.CurrentBlock?.Terminator != null);

                this.CurrentBuilder.Branch(condition, bodyBlock, exitBlock);
                return loopVariable;
            }

            bool PopulateLoopBody(Action executeBody)
            {
                this.ScopeMgr.OpenScope();
                this.SetCurrentBlock(bodyBlock);
                executeBody();
                var isTerminated = this.CurrentBlock?.Terminator != null;
                this.ScopeMgr.CloseScope(isTerminated);
                return isTerminated;
            }

            void ContinueOrExitLoop(PhiNode loopVariable, Value increment, bool bodyWasTerminated = false)
            {
                // Unless there was a terminating statement in the loop body (such as return or fail),
                // continue into the exiting block, which updates the loop variable and enters the next iteration.
                if (!bodyWasTerminated)
                {
                    this.CurrentBuilder.Branch(exitingBlock);
                }

                // Update the iteration value (phi node) and enter the next iteration
                this.SetCurrentBlock(exitingBlock);
                var nextValue = this.CurrentBuilder.Add(loopVariable, increment);
                loopVariable.AddIncoming(nextValue, exitingBlock);
                this.CurrentBuilder.Branch(headerBlock);
            }

            this.ExecuteLoop(exitBlock, () =>
            {
                var loopVariable = PopulateLoopHeader(startValue, evaluateCondition);
                var bodyWasTerminated = PopulateLoopBody(() => executeBody(loopVariable));
                ContinueOrExitLoop(loopVariable, increment, bodyWasTerminated);
            });
        }

        /// <summary>
        /// Executes the loop defined by the given action.
        /// Ensures that all pointers will be properly loaded during and after the loop.
        /// </summary>
        /// <param name="continuation">The block to set as the current block after executing the loop</param>
        /// <param name="loop">The loop to execute</param>
        internal void ExecuteLoop(BasicBlock continuation, Action loop)
        {
            // We need to mark the loop and also mark the branching
            // to ensure that pointers are properly loaded when needed.
            bool withinOuterLoop = this.IsWithinLoop;
            this.IsWithinLoop = true;
            this.StartBranch();
            loop();
            this.EndBranch();
            this.IsWithinLoop = withinOuterLoop;
            this.SetCurrentBlock(continuation);
        }

        /// <summary>
        /// Iterates through the range defined by start, step, and end, and executes the given action on each iteration value.
        /// The action is executed within its own scope.
        /// </summary>
        /// <param name="start">The start of the range and first iteration value</param>
        /// <param name="step">The optional step of the range that will be added to the iteration value in each iteration, where the default value is 1L</param>
        /// <param name="end">The end of the range after which the iteration terminates</param>
        /// <param name="executeBody">The action to perform on each item</param>
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
                var preheaderName = this.GenerateUniqueName("preheader");
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

            Value EvaluateCondition(Value loopVarIncreases, Value loopVariable)
            {
                var isGreaterOrEqualEnd = this.CurrentBuilder.Compare(
                    IntPredicate.SignedGreaterThanOrEqual, loopVariable, end);
                var isSmallerOrEqualEnd = this.CurrentBuilder.Compare(
                    IntPredicate.SignedLessThanOrEqual, loopVariable, end);
                // If we increase the loop variable in each iteration (i.e. step is positive)
                // then we need to check that the current value is smaller than or equal to the end value,
                // and otherwise we check if it is larger than or equal to the end value.
                return this.CurrentBuilder.Select(loopVarIncreases, isSmallerOrEqualEnd, isGreaterOrEqualEnd);
            }

            Value loopVarIncreases = step == null
                ? this.Context.CreateConstant(true) // the step is one by default
                : CreatePreheader(step);
            step ??= this.Context.CreateConstant(1L);
            this.CreateForLoop(start, loopVar => EvaluateCondition(loopVarIncreases, loopVar), step, executeBody);
        }

        /// <summary>
        /// Iterates through the given array and executes the given action on each element.
        /// The action is executed within its own scope.
        /// </summary>
        /// <param name="elementType">The type of an array item</param>
        /// <param name="array">The array to iterate over</param>
        /// <param name="executeBody">The action to perform on each item</param>
        internal void IterateThroughArray(ArrayValue array, Action<IValue> executeBody)
        {
            var startValue = this.Context.CreateConstant(0L);
            var increment = this.Context.CreateConstant(1L);
            var endValue = this.CurrentBuilder.Sub(array.Length, increment);

            Value EvaluateCondition(Value loopVariable) =>
                this.CurrentBuilder.Compare(IntPredicate.SignedLessThanOrEqual, loopVariable, endValue);

            void ExecuteBody(Value loopVariable) =>
                executeBody(array.GetArrayElement(loopVariable));

            this.CreateForLoop(startValue, EvaluateCondition, increment, ExecuteBody);
        }

        #endregion

        #region Type helpers

        /// <returns>The kind of the Q# type on top of the expression type stack</returns>
        internal ResolvedType CurrentExpressionType() =>
            this.ExpressionTypeStack.Peek();

        /// <returns>The QIR equivalent for the Q# type that is on top of the expression type stack</returns>
        internal ITypeRef CurrentLlvmExpressionType() =>
            this.LlvmTypeFromQsharpType(this.ExpressionTypeStack.Peek());

        /// <summary>
        /// Gets the QIR equivalent for a Q# type.
        /// Tuples are represented as QirTuplePointer, arrays as QirArray, and callables as QirCallable.
        /// </summary>
        /// <param name="resolvedType">The Q# type</param>
        /// <returns>The equivalent QIR type</returns>
        internal ITypeRef LlvmTypeFromQsharpType(ResolvedType resolvedType)
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
        internal IStructType LlvmStructTypeFromQsharpType(ResolvedType resolvedType) =>
            resolvedType.Resolution is ResolvedTypeKind.TupleType tuple
                ? this.CreateConcreteTupleType(tuple.Item)
                : this.CreateConcreteTupleType(new[] { resolvedType });

        /// <summary>
        /// Creates the concrete type of a QIR tuple value that contains items of the given types.
        /// </summary>
        internal IStructType CreateConcreteTupleType(IEnumerable<ResolvedType> items) =>
            this.Types.TypedTuple(items.Select(this.LlvmTypeFromQsharpType));

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
            var inlineId = this.GenerateUniqueName("__inline");
            this.inlineLevels.Push((inlineId, this.Values.Unit));
        }

        /// <summary>
        /// Stop inlining a callable invocation.
        /// This pops the top naming scope and decreases the inlining level.
        /// The reference count of the return value for the inlining scope is increased before closing the scope,
        /// and the value is registered with the scope manager.
        /// </summary>
        internal IValue StopInlining()
        {
            var res = this.inlineLevels.Pop().Item2;
            this.ScopeMgr.CloseScope(res);
            this.ScopeMgr.RegisterValue(res);
            return res;
        }

        /// <summary>
        /// Maps a variable name to an inlining-safe name.
        /// This way, names declared in an inlined callable don't conflict with names defined in the calling routine.
        /// </summary>
        /// <param name="name">The name to map</param>
        /// <returns>The mapped name</returns>
        internal string InlinedName(string name)
        {
            var postfix = this.inlineLevels.TryPeek(out var level) ? level.Item1 : "";
            return $"{name}{postfix}";
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
            var continueName = this.GenerateUniqueName(name);
            return next == null
                ? this.CurrentFunction.AppendBasicBlock(continueName)
                : this.CurrentFunction.InsertBasicBlock(continueName, next);
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
