// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// This class is used to track the validity of variables and values, to track access and reference counts,
    /// and to release and unreference values when they go out of scope.
    /// <para>
    /// There are two primary ways to leave a scope: close it and continue in the parent scope, or exit the current
    /// scope and all parent scopes and leave the callable altogether (i.e., return).
    /// While each scope has a single place it is closed, there may be many exits.
    /// </para>
    /// </summary>
    internal class ScopeManager
    {
        private class Scope
        {
            private readonly ScopeManager parent;

            /// <summary>
            /// Maps variable names to the corresponding value.
            /// Mutable variables are represented as PointerValues.
            /// </summary>
            private readonly Dictionary<string, IValue> variables = new Dictionary<string, IValue>();

            /// <summary>
            /// Contains all values that require unreferencing upon closing the scope.
            /// </summary>
            private readonly List<IValue> trackedValues = new List<IValue>();

            /// <summary>
            /// Contains the values that require invoking a release function upon closing the scope,
            /// as well as the name of the release function to invoke.
            /// </summary>
            private readonly List<(IValue, string)> requiredReleases = new List<(IValue, string)>();

            public Scope(ScopeManager parent)
            {
                this.parent = parent;
            }

            // public and internal methods

            public void AddVariable(string varName, IValue value) =>
                this.variables.Add(varName, value);

            public void AddValue(IValue value, string? releaseFunction = null)
            {
                if (releaseFunction != null)
                {
                    this.requiredReleases.Add((value, releaseFunction));
                }
                if (value.Value.NativeType.IsPointer)
                {
                    this.trackedValues.Add(value);
                }
            }

            public bool TryGetVariable(string varName, out IValue value) =>
                this.variables.TryGetValue(varName, out value);

            /// <summary>
            /// Returns true if the given value will be unreferenced by <see cref="ExecutePendingCalls" />
            /// unless it is explicitly excluded.
            /// </summary>
            internal bool WillBeUnreferenced(IValue value) =>
                this.trackedValues.Exists(tracked => tracked.Value == value.Value);

            /// <summary>
            /// Generates the necessary calls to unreference the tracked values, decrease the access count for registered variables,
            /// and invokes the specified release functions for values if necessary.
            /// Skips unreferencing the values specified in omitUnreferencing, removing them from the list.
            /// </summary>
            /// <param name="builder">The InstructionBuilder where the calls should be generated</param>
            /// <param name="omitUnreferencing">
            /// Values for which to omit the call to unreference them; for each value at most one call will be omitted and the value will be removed from the list
            /// </param>
            internal void ExecutePendingCalls(InstructionBuilder? builder = null, List<IValue>? omitUnreferencing = null)
            {
                builder ??= this.parent.sharedState.CurrentBuilder;
                omitUnreferencing ??= new List<IValue>();

                foreach (var (value, funcName) in this.requiredReleases)
                {
                    var func = this.parent.sharedState.GetOrCreateRuntimeFunction(funcName);
                    builder.Call(func, value.Value);
                }

                foreach (var (_, value) in this.variables)
                {
                    this.parent.DecreaseAccessCount(value);
                }

                foreach (var value in this.trackedValues)
                {
                    var omitted = omitUnreferencing.FirstOrDefault(omitted => omitted.Value == value.Value);
                    if (!omitUnreferencing.Remove(omitted))
                    {
                        this.parent.RecursivelyModifyCounts(this.parent.UnreferenceFunctionForType, value, builder);
                    }
                }
            }
        }

        private readonly GenerationContext sharedState;

        /// <summary>
        /// New variables and values are always added to the scope on top of the stack.
        /// When looking for a name, the stack is searched top-down.
        /// </summary>
        private readonly Stack<Scope> scopes = new Stack<Scope>();

        /// <summary>
        /// Is true when there are currently no stack frames tracked.
        /// Stack frames are added and removed by OpenScope and CloseScope respectively.
        /// </summary>
        public bool IsEmpty => !this.scopes.Any();

        /// <summary>
        /// Creates a new ref count scope manager.
        /// </summary>
        /// <param name="ctx">The generation context the new manager should be associated with</param>
        public ScopeManager(GenerationContext ctx)
        {
            this.sharedState = ctx;
        }

        // private helpers

        /// <summary>
        /// Gets the name of the runtime function to increase the access count for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? AddAccessFunctionForType(ResolvedType t)
        {
            if (t.Resolution.IsArrayType)
            {
                return RuntimeLibrary.ArrayAddAccess;
            }
            else if (t.Resolution.IsTupleType || t.Resolution.IsUserDefinedType)
            {
                return RuntimeLibrary.TupleAddAccess;
            }
            return null;
        }

        /// <summary>
        /// Gets the name of the runtime function to decrease the access count for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? RemoveAccessFunctionForType(ResolvedType t)
        {
            if (t.Resolution.IsArrayType)
            {
                return RuntimeLibrary.ArrayRemoveAccess;
            }
            else if (t.Resolution.IsTupleType || t.Resolution.IsUserDefinedType)
            {
                return RuntimeLibrary.TupleRemoveAccess;
            }
            return null;
        }

        /// <summary>
        /// Gets the name of the runtime function to increase the reference count for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? ReferenceFunctionForType(ResolvedType t)
        {
            if (t.Resolution.IsTupleType || t.Resolution.IsUserDefinedType)
            {
                return RuntimeLibrary.TupleReference;
            }
            else if (t.Resolution.IsArrayType)
            {
                return RuntimeLibrary.ArrayReference;
            }
            else if (t.Resolution.IsResult)
            {
                return RuntimeLibrary.ResultReference;
            }
            else if (t.Resolution.IsOperation || t.Resolution.IsFunction)
            {
                return RuntimeLibrary.CallableReference;
            }
            else if (t.Resolution.IsString)
            {
                return RuntimeLibrary.StringReference;
            }
            else if (t.Resolution.IsBigInt)
            {
                return RuntimeLibrary.BigIntReference;
            }
            return null;
        }

        /// <summary>
        /// Gets the name of the runtime function to decrease the access count for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? UnreferenceFunctionForType(ResolvedType t)
        {
            if (t.Resolution.IsTupleType || t.Resolution.IsUserDefinedType)
            {
                return RuntimeLibrary.TupleUnreference;
            }
            else if (t.Resolution.IsArrayType)
            {
                return RuntimeLibrary.ArrayUnreference;
            }
            else if (t.Resolution.IsResult)
            {
                return RuntimeLibrary.ResultUnreference;
            }
            else if (t.Resolution.IsOperation || t.Resolution.IsFunction)
            {
                return RuntimeLibrary.CallableUnreference;
            }
            else if (t.Resolution.IsString)
            {
                return RuntimeLibrary.StringUnreference;
            }
            else if (t.Resolution.IsBigInt)
            {
                return RuntimeLibrary.BigIntUnreference;
            }
            return null;
        }

        /// <summary>
        /// If the given function returns a function name for the given value,
        /// applies the runtime function with that name to the given value, casting the value if necessary,
        /// Recurs into contained items.
        /// </summary>
        private void RecursivelyModifyCounts(Func<ResolvedType, string?> getFunctionName, IValue value, InstructionBuilder? builder = null)
        {
            void ModifyCounts(string funcName, IValue value)
            {
                var func = this.sharedState.GetOrCreateRuntimeFunction(funcName);

                if (value is PointerValue pointer)
                {
                    ModifyCounts(funcName, pointer.LoadValue(builder));
                }
                else if (value is TupleValue tuple)
                {
                    // for tuples we also unreference all inner tuples
                    for (var i = 0; i < tuple.ElementTypes.Length; ++i)
                    {
                        var itemFuncName = getFunctionName(tuple.ElementTypes[i]);
                        if (itemFuncName != null)
                        {
                            var item = tuple.GetTupleElement(i, builder);
                            ModifyCounts(itemFuncName, item);
                        }
                    }

                    builder.Call(func, tuple.OpaquePointer);
                }
                else if (value is ArrayValue array)
                {
                    var itemFuncName = getFunctionName(array.QSharpElementType);
                    if (itemFuncName != null)
                    {
                        // FIXME: THE BUILDER FOR THIS IS ENTIRELY WRONG
                        //this.sharedState.IterateThroughArray(array, arrItem => ModifyCounts(itemFuncName, arrItem));
                    }
                    builder.Call(func, array.OpaquePointer);
                }
                else if (value is CallableValue callable)
                {
                    // TODO
                    // RECURSIVELY UNREFERENCE THE CAPTURE TUPLE
                    builder.Call(func, callable.Value);
                }
            }

            builder ??= this.sharedState.CurrentBuilder;
            var func = getFunctionName(value.QSharpType); // FIXME: MAKE NATIVE TYPE PART OF THE ITUPLE INTERFACE,
            if (func != null)
            {
                ModifyCounts(func, value);
            }
        }

        // public methods

        /// <summary>
        /// Opens a new scope and pushes it on top of the naming scope stack.
        /// Opening a new scope automatically opens a new naming scope as well.
        /// </summary>
        public void OpenScope()
        {
            this.scopes.Push(new Scope(this));
        }

        /// <summary>
        /// Closes the current scope by popping it off of the stack.
        /// Closing the scope automatically also closes the current naming scope as well.
        /// Emits the queued calls to unreference, release, and/or decrease the access counts for values going out of scope.
        /// If the current basic block is already terminated, presumably by a return, the calls are not generated.
        /// The calls are generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        public void CloseScope(bool isTerminated, InstructionBuilder? builder = null)
        {
            var scope = this.scopes.Pop();
            if (!isTerminated)
            {
                scope.ExecutePendingCalls(builder);
            }
        }

        public void ExitScope(bool isTerminated, InstructionBuilder? builder = null)
        {
            var scope = this.scopes.Peek();
            if (!isTerminated)
            {
                scope.ExecutePendingCalls(builder);
            }
        }

        /// <summary>
        /// Adds a call to a runtime library function to increase the reference count for the given value if necessary.
        /// The call is generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="value">The value which is referenced</param>
        public void IncreaseReferenceCount(IValue value, InstructionBuilder? builder = null) =>
            this.RecursivelyModifyCounts(this.ReferenceFunctionForType, value, builder);

        /// <summary>
        /// Adds a call to a runtime library function to decrease the reference count for the given value if necessary.
        /// The call is generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="value">The value which is unreferenced</param>
        public void DecreaseReferenceCount(IValue value, InstructionBuilder? builder = null) =>
            this.RecursivelyModifyCounts(this.UnreferenceFunctionForType, value, builder);

        /// <summary>
        /// Adds a call to a runtime library function to increase the access count for the given value if necessary.
        /// The call is generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="value">The value which is assigned to a handle</param>
        internal void IncreaseAccessCount(IValue value, InstructionBuilder? builder = null) =>
            this.RecursivelyModifyCounts(this.AddAccessFunctionForType, value, builder);

        /// <summary>
        /// Adds a call to a runtime library function to decrease the access count for the given value if necessary.
        /// The call is generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="value">The value which is unassigned from a handle</param>
        internal void DecreaseAccessCount(IValue value, InstructionBuilder? builder = null) =>
            this.RecursivelyModifyCounts(this.RemoveAccessFunctionForType, value, builder);

        /// <summary>
        /// Queues a call to a suitable runtime library function that unreferences the value
        /// when the scope is closed or exited.
        /// </summary>
        /// <param name="value">Value that is created within the current scope</param>
        public void RegisterValue(IValue value)
        {
            this.scopes.Peek().AddValue(value);
        }

        /// <summary>
        /// Adds a value constructed as part of a qubit allocation to the current topmost scope.
        /// Makes sure that all allocated qubits are released when the scope is closed or exited
        /// and that the value and all its items are unreferenced.
        /// </summary>
        public void RegisterAllocatedQubits(Value value)
        {
            var (releaser, type) =
                value.NativeType == this.sharedState.Types.Array ? (RuntimeLibrary.QubitReleaseArray, SyntaxGenerator.QubitArrayType) :
                value.NativeType == this.sharedState.Types.Qubit ? (RuntimeLibrary.QubitRelease, ResolvedType.New(QsResolvedTypeKind.Qubit)) :
                throw new ArgumentException("AddQubitValue expects an argument of type Qubit or Qubit[]");
            var typedValue = this.sharedState.Values.From(value, type);
            this.scopes.Peek().AddValue(typedValue, releaser);
        }

        /// <summary>
        /// Registers a variable name as an alias for an LLVM value.
        /// </summary>
        /// <param name="name">The name to register</param>
        /// <param name="value">The LLVM value</param>
        /// <param name="isMutable">true if the name binding is mutable, false if immutable; the default is false</param>
        internal void RegisterVariable(string name, IValue value)
        {
            if (string.IsNullOrEmpty(value.Value.Name))
            {
                value.Value.RegisterName(this.sharedState.InlinedName(name));
            }
            this.IncreaseAccessCount(value);
            this.scopes.Peek().AddVariable(name, value);
        }

        /// <summary>
        /// Gets the value of a named variable.
        /// The name must have been registered as an alias for the value using
        /// <see cref="RegisterVariable(string, Value, bool)"/>.
        /// </summary>
        /// <param name="name">The registered variable name to look for</param>
        internal IValue GetVariable(string name)
        {
            foreach (var scope in this.scopes)
            {
                if (scope.TryGetVariable(name, out IValue value))
                {
                    return value;
                }
            }
            throw new KeyNotFoundException($"Could not find a Value for local symbol {name}");
        }

        /// <summary>
        /// Exits the current function by emitting the calls to unreference values going out of scope for all open scopes.
        /// Increases the reference count of the returned value by 1, either by omitting to unreference it or by explicitly increasing it.
        /// The calls are generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// Exiting the current scope does *not* close the scope.
        /// </summary>
        /// <param name="returned">The value that is returned and expected to remain valid after exiting.</param>
        public void ExitFunction(IValue returned, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;

            // To avoid increasing the reference count for the returned value and all contained items
            // followed by immediately decreasing it again, we check whether we can avoid that.
            // There are a couple of pitfalls to watch out for when doing this:
            // a) It is possible that the returned value is or contains items that are not going to be
            //    unreferenced here, e.g. when the returned value has been passed as argument to the
            //    the callable we are exiting here. If that's the case, then we need to first increase
            //    the reference count for the returned value before processing all unreference calls,
            //    since otherwise an item contained in the returned value might get deallocated.
            // b) Conversely, it is also possible that the returned value is unreferenced multiple times.
            //    we hence need to make sure that we only omit one of these calls.
            // c) The stack of pending calls contains more than just those related to modifying reference
            //    counts; we need to make sure that any omitted call indeed was to unreference the
            //    returned value rather than e.g. to release qubits.
            // d) We can't modify the pending calls; they may be used by other execution paths that
            //    don't return the same value.

            var returnWillBeUnreferenced = this.scopes.Any(scope => scope.WillBeUnreferenced(returned));
            if (!returnWillBeUnreferenced)
            {
                this.IncreaseReferenceCount(returned);
            }

            // We need to extract the scopes to iterate over since for loops to release array items
            // will create new scopes and hence modify the collection.
            var currentScopes = this.scopes.ToArray();
            var omittedUnreferences = new List<IValue>() { returned };
            foreach (var scope in currentScopes)
            {
                scope.ExecutePendingCalls(builder, omittedUnreferences);
            }
        }
    }
}
