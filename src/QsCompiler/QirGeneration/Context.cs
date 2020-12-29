﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Quantum.QIR;
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
    using QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

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
        internal int CurrentInlineLevel { get; private set; } = 0;
        internal ITypeRef? BuiltType { get; set; }

        internal readonly ScopeManager ScopeMgr;
        internal readonly Stack<Value> ValueStack;
        internal readonly Stack<ResolvedType> ExpressionTypeStack;

        private readonly Dictionary<string, int> uniqueNameIds = new Dictionary<string, int>();
        private readonly Stack<Dictionary<string, (Value, bool)>> namesInScope = new Stack<Dictionary<string, (Value, bool)>>();

        private readonly Dictionary<string, ITypeRef> interopType = new Dictionary<string, ITypeRef>();
        private readonly Dictionary<string, (QsCallable, GlobalVariable)> wrapperQueue = new Dictionary<string, (QsCallable, GlobalVariable)>();

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
            this.transformation = null; // needs to be set by the instantiating transformation

            this.CurrentBuilder = new InstructionBuilder(this.Context);
            this.ValueStack = new Stack<Value>();
            this.ExpressionTypeStack = new Stack<ResolvedType>();
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
        /// <param name="namespaceName">The callable's namespace name</param>
        /// <param name="name">The callable's name</param>
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
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultReference, this.Context.VoidType, this.Types.Result);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultUnreference, this.Context.VoidType, this.Types.Result);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ResultEqual, this.Context.BoolType, this.Types.Result, this.Types.Result);

            // string library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringCreate, this.Types.String, this.Context.Int32Type, this.Context.Int8Type.CreateArrayType(0));
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringReference, this.Context.VoidType, this.Types.String);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.StringUnreference, this.Context.VoidType, this.Types.String);
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
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntCreateArray, this.Types.BigInt, this.Context.Int32Type, this.Context.Int8Type.CreateArrayType(0));
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntReference, this.Context.VoidType, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntUnreference, this.Context.VoidType, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntNegate, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntAdd, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntSubtract, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntMultiply, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntDivide, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntModulus, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntPower, this.Types.BigInt, this.Types.BigInt, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntBitand, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntBitor, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntBitxor, this.Types.BigInt, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntBitnot, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntShiftleft, this.Types.BigInt, this.Types.BigInt, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntShiftright, this.Types.BigInt, this.Types.BigInt, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntEqual, this.Context.BoolType, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntGreater, this.Context.BoolType, this.Types.BigInt, this.Types.BigInt);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.BigIntGreaterEq, this.Context.BoolType, this.Types.BigInt, this.Types.BigInt);

            // tuple library functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleCreate, this.Types.Tuple, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleReference, this.Context.VoidType, this.Types.Tuple);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleUnreference, this.Context.VoidType, this.Types.Tuple);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.TupleCopy, this.Context.BoolType, this.Types.Tuple);

            // array library functions
            this.runtimeLibrary.AddVarArgsFunction(RuntimeLibrary.ArrayCreate, this.Types.Array, this.Context.Int32Type, this.Context.Int32Type);
            this.runtimeLibrary.AddVarArgsFunction(RuntimeLibrary.ArrayGetElementPtr, this.Context.Int8Type.CreatePointerType(), this.Types.Array);
            // TODO: figure out how to call a varargs function and get rid of these two functions
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayCreate1d, this.Types.Array, this.Context.Int32Type, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayGetElementPtr1d, this.Context.Int8Type.CreatePointerType(), this.Types.Array, this.Context.Int64Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayGetLength, this.Context.Int64Type, this.Types.Array, this.Context.Int32Type);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayReference, this.Context.VoidType, this.Types.Array);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayUnreference, this.Context.VoidType, this.Types.Array);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayCopy, this.Types.Array, this.Types.Array);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArrayConcatenate, this.Types.Array, this.Types.Array, this.Types.Array);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.ArraySlice, this.Types.Array, this.Types.Array, this.Context.Int32Type, this.Types.Range);

            // callable library functions
            this.runtimeLibrary.AddFunction(
                RuntimeLibrary.CallableCreate,
                this.Types.Callable,
                this.Types.FunctionSignature.CreatePointerType().CreateArrayType(4).CreatePointerType(),
                this.Types.Tuple);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableInvoke, this.Context.VoidType, this.Types.Callable, this.Types.Tuple, this.Types.Tuple);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableCopy, this.Types.Callable, this.Types.Callable);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableMakeAdjoint, this.Context.VoidType, this.Types.Callable);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableMakeControlled, this.Context.VoidType, this.Types.Callable);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableReference, this.Context.VoidType, this.Types.Callable);
            this.runtimeLibrary.AddFunction(RuntimeLibrary.CallableUnreference, this.Context.VoidType, this.Types.Callable);

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
                if (SymbolResolution.TryGetTargetInstructionName(c.Attributes) is var att && att.IsValue)
                {
                    var name = att.Item;
                    // Special handling for Unit since by default it turns into an empty tuple
                    var returnType = c.Signature.ReturnType.Resolution.IsUnitType
                        ? this.Context.VoidType
                        : this.LlvmTypeFromQsharpType(c.Signature.ReturnType);
                    var argTypeKind = c.Signature.ArgumentType.Resolution;
                    var argTypeArray =
                        argTypeKind is QsResolvedTypeKind.TupleType tuple ? tuple.Item.Select(this.LlvmTypeFromQsharpType).ToArray() :
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

            this.GenerateQueuedWrappers();

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
                    if (SymbolResolution.TryGetTargetInstructionName(c.Attributes) is var att && att.IsValue)
                    {
                        var func = this.quantumInstructionSet.GetOrCreateFunction(att.Item);
                        this.GenerateInterop(func, att.Item);
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
            bool MapType(QsArgumentTuple t, List<ITypeRef> typeList, List<string> nameList, List<ArgMapping> mappingList)
            {
                bool changed = false;

                if (t is QsArgumentTuple.QsTuple tuple)
                {
                    foreach (QsArgumentTuple inner in tuple.Item)
                    {
                        changed |= MapType(inner, typeList, nameList, mappingList);
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

            if (this.TryGetFunction(qualifiedName, QsSpecializationKind.QsBody, out IrFunction? func)
                && this.TryGetGlobalCallable(qualifiedName, out QsCallable? callable))
            {
                var epName = $"{qualifiedName.Namespace.Replace('.', '_')}_{qualifiedName.Name}";

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
                    var epFunc = this.Module.CreateFunction(epName, this.Context.GetFunctionType(mappedResultType, mappedArgList));
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
                            var array = builder.Call(this.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCreate1d), elementSize, length);
                            argValueList.Add(array);
                            arraysToReleaseList.Add(array);
                            // Fill in the array if the length is >0. Since the QIR array is new, we assume we can use memcpy.
                            var copyBlock = epFunc.AppendBasicBlock("copy");
                            var nextBlock = epFunc.AppendBasicBlock("next");
                            var cond = builder.Compare(IntPredicate.SignedGreaterThan, length, this.Context.CreateConstant(0L));
                            builder.Branch(cond, copyBlock, nextBlock);
                            builder = new InstructionBuilder(copyBlock);
                            var destBase = builder.Call(this.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d), array, this.Context.CreateConstant(0L));
                            builder.MemCpy(
                                destBase,
                                namedValues[mapping.BaseName],
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

                    Value result = builder.Call(func, argValueList);
                    foreach (var arrayToRelease in arraysToReleaseList)
                    {
                        builder.Call(this.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayUnreference), arrayToRelease);
                    }

                    if (func.ReturnType.IsVoid)
                    {
                        builder.Return();
                    }
                    else
                    {
                        if (mappedResultType != func.ReturnType)
                        {
                            result = builder.BitCast(result, mappedResultType);
                        }
                        builder.Return(result);
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
        /// <param name="m">(optional) The LLVM module in which the stub should be generated</param>
        private void GenerateInterop(IrFunction func, string baseName)
        {
            // TODO: why do we need both GenerateEntryPoint and GenerateInteropWrapper?

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
        internal IrFunction GetOrCreateQuantumFunction(string name) =>
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
            if (this.namesInScope.Any() || this.CurrentInlineLevel != 0 || !this.ScopeMgr.IsEmpty)
            {
                throw new InvalidOperationException("Processing of the current function and needs to be properly terminated before starting a new one");
            }

            this.uniqueNameIds.Clear();
            this.OpenNamingScope();
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
            this.CloseNamingScope();

            if (!HasAPredecessor(this.CurrentBlock)
                && this.CurrentFunction.BasicBlocks.Count > 1)
            {
                this.CurrentFunction.BasicBlocks.Remove(this.CurrentBlock);
            }
            else if (this.CurrentBlock.Terminator == null)
            {
                this.CurrentBuilder.Return();
            }

            return this.ScopeMgr.IsEmpty && this.CurrentInlineLevel == 0 && !this.namesInScope.Any();
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
                spec.Signature.ArgumentType.Resolution is QsResolvedTypeKind.TupleType ts ? ts.Item.Select(this.LlvmTypeFromQsharpType).ToArray() :
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
        internal void GenerateFunctionHeader(QsSpecialization spec, QsArgumentTuple argTuple, bool deconstuctArgument = true)
        {
            IEnumerable<string> ArgTupleToNames(QsArgumentTuple arg, Queue<(string, QsArgumentTuple)> tupleQueue)
            {
                string LocalVarName(QsArgumentTuple v)
                {
                    if (v is QsArgumentTuple.QsTuple)
                    {
                        var name = this.GenerateUniqueName("arg");
                        tupleQueue.Enqueue((name, v));
                        return name;
                    }
                    else
                    {
                        return v is QsArgumentTuple.QsTupleItem item && item.Item.VariableName is QsLocalSymbol.ValidName varName
                            ? varName.Item
                            : this.GenerateUniqueName("arg");
                    }
                }

                return arg is QsArgumentTuple.QsTuple tuple
                    ? tuple.Item.Select(item => LocalVarName(item))
                    : new[] { LocalVarName(arg) };
            }

            this.CurrentFunction = this.RegisterFunction(spec);
            this.CurrentBlock = this.CurrentFunction.AppendBasicBlock("entry");
            this.CurrentBuilder = new InstructionBuilder(this.CurrentBlock);
            if (spec.Signature.ArgumentType.Resolution.IsUnitType)
            {
                return;
            }

            var innerTuples = new Queue<(string, QsArgumentTuple)>();
            var outerArgNames = ArgTupleToNames(argTuple, innerTuples).ToArray();

            // If we have a single named tuple-valued argument, then the items of the tuple
            // are the arguments to the function and we need to reconstruct the tuple.
            // The reason for this choice of representation is that relying only on the argument type
            // rather than the argument tuple for determining the signature of a function is much cleaner.
            if (outerArgNames.Length == 1 && this.CurrentFunction.Parameters.Count > 1)
            {
                this.CreateAndPushTuple(this.CurrentBuilder, this.CurrentFunction.Parameters.ToArray());
                this.RegisterName(outerArgNames[0], this.ValueStack.Pop(), false);
            }
            else
            {
                var i = 0;
                foreach (var argName in outerArgNames)
                {
                    this.CurrentFunction.Parameters[i].Name = argName;
                    this.RegisterName(argName, this.CurrentFunction.Parameters[i], false);
                    i++;
                }
            }

            // Now break up inner argument tuples
            while (deconstuctArgument && innerTuples.TryDequeue(out (string, QsArgumentTuple) tuple))
            {
                var (tupleArgName, tupleArg) = tuple;
                this.PushNamedValue(tupleArgName);
                var tupleValue = this.ValueStack.Pop();
                IStructType tupleType = Types.StructFromPointer(tupleValue.NativeType);

                int idx = 0;
                foreach (var argName in ArgTupleToNames(tupleArg, innerTuples))
                {
                    var elementPointer = this.GetTupleElementPointer(tupleType, tupleValue, idx);
                    var element = this.CurrentBuilder.Load(((IPointerType)elementPointer.NativeType).ElementType, elementPointer);
                    this.RegisterName(argName, element, false);
                    idx++;
                }
            }
        }

        /// <summary>
        /// Generates the default constructor for a Q# user-defined type.
        /// This routine generates all the code for the constructor, not just the header.
        /// </summary>
        /// <param name="udt">The Q# user-defined type</param>
        internal void GenerateConstructor(QsSpecialization spec, QsArgumentTuple argTuple)
        {
            this.GenerateFunctionHeader(spec, argTuple, deconstuctArgument: false);

            // create the udt (output value)
            if (spec.Signature.ArgumentType.Resolution.IsUnitType)
            {
                QirStatementKindTransformation.AddReturn(this, this.Constants.UnitValue, returnsVoid: false);
            }
            else if (this.CurrentFunction != null)
            {
                var udtTupleType = this.LlvmStructTypeFromQsharpType(spec.Signature.ArgumentType);
                var udtTuple = this.CurrentBuilder.BitCast(this.CreateTupleForType(udtTupleType), udtTupleType.CreatePointerType());

                var nrArgs = spec.Signature.ArgumentType.Resolution is QsResolvedTypeKind.TupleType ts ? ts.Item.Length : 1;
                for (int i = 0; i < nrArgs; i++)
                {
                    var itemPtr = this.GetTupleElementPointer(udtTupleType, udtTuple, i);
                    this.CurrentBuilder.Store(this.CurrentFunction.Parameters[i], itemPtr);
                }

                QirStatementKindTransformation.AddReturn(this, udtTuple, returnsVoid: false);
            }
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
        /// Tries to get the wrapper function for a Q# specialization.
        /// If the wrapper function hasn't been generated yet, false is returned.
        /// </summary>
        /// <param name="callable">The callable</param>
        /// <param name="kind">The specialization kind</param>
        /// <param name="function">Gets filled in with the LLVM function object if it exists already</param>
        /// <returns>true if the function has already been declared/defined, or false otherwise</returns>
        private bool TryGetWrapper(QsCallable callable, QsSpecializationKind kind, [MaybeNullWhen(false)] out IrFunction function)
        {
            var fullName = FunctionWrapperName(callable.FullName, kind);
            return this.Module.TryGetFunction(fullName, out function);
        }

        /// <summary>
        /// If a constant array with the four IrFunctions for the given callable already exists,
        /// returns the corresponding global variable. If no such array exists, creates a constant array
        /// and instantiates the necessary wrapper functions, queues the generation of their implementations,
        /// and returns the created global constant.
        /// The generation of the implementations is queued such that the current context is not modified
        /// beyond adding the corresponding constant and functions declarations.
        /// </summary>
        internal GlobalVariable GetOrCreateCallableTable(QsCallable callable)
        {
            var key = $"{FlattenNamespaceName(callable.FullName.Namespace)}__{callable.FullName.Name}";
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
                    if (callable.Specializations.Any(spec => spec.Kind == kind &&
                        (spec.Implementation.IsProvided || spec.Implementation.IsIntrinsic)))
                    {
                        var f = this.Module.CreateFunction(FunctionWrapperName(callable.FullName, kind), this.Types.FunctionSignature);
                        funcs[index] = f;
                    }
                    else
                    {
                        funcs[index] = Constant.ConstPointerToNullFor(this.Types.FunctionSignature.CreatePointerType());
                    }
                }

                ITypeRef t = this.Types.FunctionSignature.CreatePointerType();
                Constant array = ConstantArray.From(t, funcs);
                var table = this.Module.AddGlobal(array.NativeType, true, Linkage.DllExport, array, key);
                this.wrapperQueue.Add(key, (callable, table));
                return table;
            }
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
        /// </summary>
        private void GenerateQueuedWrappers()
        {
            // Generate the code that decomposes the tuple back into the named arguments
            // Note that we don't want to recurse here!
            List<Value> GenerateArgTupleDecomposition(ResolvedType argType, Value value)
            {
                Value BuildLoadForArg(ResolvedType t, Value value)
                {
                    ITypeRef argTypeRef = this.LlvmTypeFromQsharpType(t); // FIXME: WHAT IF WE HAVE A TUPLE HERE; NEED TO CAST THE VALUE??
                    return this.CurrentBuilder.Load(argTypeRef, value);
                }

                List<Value> args = new List<Value>();
                if (argType.Resolution is QsResolvedTypeKind.TupleType ts)
                {
                    IStructType tupleType = Types.StructFromPointer(this.LlvmTypeFromQsharpType(argType));
                    Value[] itemPointers = this.GetTupleElementPointers(tupleType, value);
                    for (var i = 0; i < itemPointers.Length; i++)
                    {
                        args.Add(ts.Item[i].Resolution.IsUnitType
                            ? this.Constants.UnitValue
                            : BuildLoadForArg(ts.Item[i], itemPointers[i]));
                    }
                }
                else if (!argType.Resolution.IsUnitType)
                {
                    args.Add(BuildLoadForArg(argType, value));
                }

                return args;
            }

            // result value contains the return value, and output tuple is the tuple where that value should be stored
            void PopulateResultTuple(ResolvedType resultType, Value resultValue, Value outputTuple)
            {
                var resultTupleType = this.LlvmStructTypeFromQsharpType(resultType);
                if (resultType.Resolution is QsResolvedTypeKind.TupleType tupleType)
                {
                    var concreteOutputTuple = this.CurrentBuilder.BitCast(outputTuple, resultTupleType.CreatePointerType());
                    for (int j = 0; j < tupleType.Item.Length; j++)
                    {
                        var resItemPointer = this.GetTupleElementPointer(resultTupleType, resultValue, j);
                        var itemOutputPointer = this.GetTupleElementPointer(resultTupleType, concreteOutputTuple, j);

                        var itemType = this.LlvmTypeFromQsharpType(tupleType.Item[j]);
                        var resItem = this.CurrentBuilder.Load(itemType, resItemPointer);
                        this.CurrentBuilder.Store(resItem, itemOutputPointer);
                    }
                }
                else if (!resultType.Resolution.IsUnitType)
                {
                    var outputPointer = this.GetTupleElementPointer(resultTupleType, outputTuple, 0);
                    this.CurrentBuilder.Store(resultValue, outputPointer);
                }
            }

            Value GenerateBaseMethodCall(QsCallable callable, QsSpecializationKind specKind, List<Value> args)
            {
                if (SymbolResolution.TryGetTargetInstructionName(callable.Attributes) is var qisCode && qisCode.IsValue)
                {
                    var func = this.GetOrCreateQuantumFunction(qisCode.Item);
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
                    if ((spec.Implementation.IsProvided || spec.Implementation.IsIntrinsic)
                        && GenerateWrapperHeader(callable, spec) && this.CurrentFunction != null)
                    {
                        this.OpenNamingScope();
                        Value argTupleValue = this.CurrentFunction.Parameters[1];
                        var argList = GenerateArgTupleDecomposition(spec.Signature.ArgumentType, argTupleValue);
                        var result = GenerateBaseMethodCall(callable, spec.Kind, argList);
                        PopulateResultTuple(callable.Signature.ReturnType, result, this.CurrentFunction.Parameters[2]);
                        this.CurrentBuilder.Return();
                        this.CloseNamingScope();
                    }
                }
            }
        }

        #endregion

        #region Type helpers

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
        internal IStructType LlvmStructTypeFromQsharpType(ResolvedType resolvedType)
        {
            if (resolvedType.Resolution is QsResolvedTypeKind.TupleType tuple)
            {
                var elementTypes = tuple.Item.Select(this.LlvmTypeFromQsharpType);
                return this.Types.CreateConcreteTupleType(elementTypes);
            }
            else
            {
                return this.Types.CreateConcreteTupleType(this.LlvmTypeFromQsharpType(resolvedType));
            }
        }

        /// <summary>
        /// Creates a suitable array of values to access the item at a given index for a pointer to a struct.
        /// </summary>
        internal Value[] PointerIndex(int index) => new[]
        {
            this.Context.CreateConstant(0L),
            this.Context.CreateConstant(index)
        };

        /// <summary>
        /// Returns a pointer to a tuple element.
        /// If the type of the given value is not a pointer to the specified struct type, bitcasts the value.
        /// This is a thin wrapper around the LLVM GEP instruction.
        /// </summary>
        /// <param name="tupleType">The type of the tuple structure.</param>
        /// <param name="tuple">The pointer to the tuple. This will be cast to the proper type if necessary.</param>
        /// <param name="index">The element's index into the tuple. The tuple header is index 0, the first data item is index 1.</param>
        /// <param name="b">An optional InstructionBuilder to create these instructions on. The current builder is used as the default.</param>
        internal Value GetTupleElementPointer(IStructType tupleType, Value tuple, int index, InstructionBuilder? b = null)
        {
            var builder = b ?? this.CurrentBuilder;
            var typedTuple = tuple.NativeType == tupleType.CreatePointerType()
                ? tuple
                : builder.BitCast(tuple, tupleType.CreatePointerType());
            return builder.GetElementPtr(tupleType, typedTuple, this.PointerIndex(index));
        }

        /// <summary>
        /// Returns an array of pointers to each element in the given tuple.
        /// If the type of the given value is not a pointer to the specified struct type, bitcasts the value.
        /// </summary>
        /// <param name="tupleType">The type of the tuple structure.</param>
        /// <param name="tuple">The pointer to the tuple. This will be cast to the proper type if necessary.</param>
        /// <param name="b">An optional InstructionBuilder to create these instructions on. The current builder is used as the default.</param>
        internal Value[] GetTupleElementPointers(IStructType tupleType, Value tuple, InstructionBuilder? b = null)
        {
            InstructionBuilder builder = b ?? this.CurrentBuilder;
            Value typedTuple = tuple.NativeType == tupleType.CreatePointerType()
                ? tuple
                : builder.BitCast(tuple, tupleType.CreatePointerType());

            Value ItemPointer(int index) =>
                builder.GetElementPtr(tupleType, typedTuple, this.PointerIndex(index));
            return tupleType.Members.Select((_, i) => ItemPointer(i)).ToArray();
        }

        /// <summary>
        /// Computes the size in bytes of an LLVM type as an LLVM value.
        /// If the type isn't a simple pointer, integer, or double, we compute it using a standard LLVM idiom.
        /// </summary>
        /// <param name="t">The LLVM type to compute the size of</param>
        /// <param name="b">The builder to use to generate the struct size computation, if needed</param>
        /// <returns>An LLVM value containing the size of the type in bytes</returns>
        internal Value ComputeSizeForType(ITypeRef t, InstructionBuilder b)
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
                // Note that we can't use this.GetTupleElementPtr here because we want to get a pointer to a second structure instance
                var firstPtr = b.GetElementPtr(t, basePointer, new[] { this.Context.CreateConstant(0) });
                var first = b.PointerToInt(firstPtr, this.Context.Int64Type);
                var secondPtr = b.GetElementPtr(t, basePointer, new[] { this.Context.CreateConstant(1) });
                var second = b.PointerToInt(secondPtr, this.Context.Int64Type);
                return this.CurrentBuilder.Sub(second, first);
            }
        }

        #endregion

        #region Tuple and argument tuple creation

        /// <summary>
        /// Creates a new tuple for an LLVM structure type.
        /// The new tuple is created using the current builder.
        /// </summary>
        /// <param name="t">The LLVM structure type for the tuple</param>
        /// <param name="b">The builder to use to create the tuple</param>
        /// <returns>A value containing the pointer to the new tuple</returns>
        internal Value CreateTupleForType(ITypeRef t, InstructionBuilder? b = null)
        {
            var builder = b ?? this.CurrentBuilder;
            var size = this.ComputeSizeForType(t, builder);
            var tuple = builder.Call(this.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate), size);
            return tuple;
        }

        /// <summary>
        /// Builds a typed tuple with the items set to the given values and pushes it onto the value stack.
        /// The create value is added to the current scope in the scope manager.
        /// </summary>
        /// <param name="builder">The builder to use to create the tuple</param>
        /// <param name="vs">The tuple elements</param>
        internal void CreateAndPushTuple(InstructionBuilder builder, params Value[] vs)
        {
            // Build the LLVM structure type we need
            IStructType tupleType = this.Types.CreateConcreteTupleType(vs.Select(v => v.NativeType));

            // Allocate the tuple, cast it to the concrete type, and make to track if for release
            Value tuple = this.CreateTupleForType(tupleType, builder);
            Value concreteTuple = builder.BitCast(tuple, tupleType.CreatePointerType());
            this.ValueStack.Push(concreteTuple);
            this.ScopeMgr.AddValue(concreteTuple);

            // Fill it in, field by field
            Value[] itemPointers = this.GetTupleElementPointers(tupleType, concreteTuple, builder);
            for (var i = 0; i < itemPointers.Length; ++i)
            {
                builder.Store(vs[i], itemPointers[i]);
                this.ScopeMgr.AddReference(vs[i], builder);
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
            this.OpenNamingScope();
            this.CurrentInlineLevel++;
        }

        /// <summary>
        /// Stop inlining a callable invocation.
        /// This pops the top naming scope and decreases the inlining level.
        /// </summary>
        internal void StopInlining()
        {
            this.CurrentInlineLevel--;
            this.CloseNamingScope();
        }

        /// <summary>
        /// Maps a variable name to an inlining-safe name.
        /// This way, names declared in an inlined callable don't conflict with names defined in the calling routine.
        /// </summary>
        /// <param name="name">The name to map</param>
        /// <returns>The mapped name</returns>
        internal string InlinedName(string name)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < this.CurrentInlineLevel; i++)
            {
                sb.Append('.');
            }
            sb.Append(name);
            return sb.ToString();
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
        /// Opens a new naming scope and pushes it on top of the naming scope stack.
        /// <para>
        /// Naming scopes map variable names to values.
        /// New names are always added to the scope on top of the stack.
        /// When looking for a name, the stack is searched top-down.
        /// </para>
        /// </summary>
        internal void OpenNamingScope()
        {
            this.namesInScope.Push(new Dictionary<string, (Value, bool)>());
        }

        /// <summary>
        /// Closes the current naming scope by popping it off of the naming scope stack.
        /// </summary>
        internal void CloseNamingScope()
        {
            this.namesInScope.Pop();
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
        internal void RegisterName(string name, Value value, bool isMutable = false)
        {
            if (string.IsNullOrEmpty(value.Name))
            {
                value.RegisterName(this.InlinedName(name));
            }
            this.namesInScope.Peek().Add(name, (value, isMutable));
        }

        /// <summary>
        /// Gets the pointer to a mutable variable by name.
        /// The name must have been registered as an alias for the pointer value using
        /// <see cref="RegisterName(string, Value, bool)"/>.
        /// </summary>
        /// <param name="name">The registered variable name to look for</param>
        /// <returns>The pointer value for the mutable value</returns>
        internal Value GetNamedPointer(string name)
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
        /// <see cref="RegisterName(string, Value, bool)"/>.
        /// <para>
        /// If the variable is mutable, then the associated pointer value is used to load and push the actual
        /// variable value.
        /// </para>
        /// </summary>
        /// <param name="name">The registered variable name to look for</param>
        internal void PushNamedValue(string name)
        {
            foreach (var dict in this.namesInScope)
            {
                if (dict.TryGetValue(name, out (Value, bool) item))
                {
                    this.ValueStack.Push(
                        item.Item2 && item.Item1.NativeType is IPointerType ptr
                        // Mutable, so the value is a pointer; we need to load what it's pointing to
                        ? this.CurrentBuilder.Load(ptr.ElementType, item.Item1)
                        : item.Item1);
                    return;
                }
            }
            throw new KeyNotFoundException($"Could not find a Value for local symbol {name}");
        }

        /// <summary>
        /// Processes an expression and returns its Value.
        /// </summary>
        /// <param name="ex">The expression to process</param>
        /// <returns>The LLVM Value that represents the result of the expression</returns>
        internal Value EvaluateSubexpression(TypedExpression ex)
        {
            this.Transformation.Expressions.OnTypedExpression(ex);
            return this.ValueStack.Pop();
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
