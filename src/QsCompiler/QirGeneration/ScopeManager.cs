// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
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
            /// Maps variable names to a tuple with a value to access them and whether or not accessing them requires loading the value first.
            /// </summary>
            private readonly Dictionary<string, (IValue, bool)> variables = new Dictionary<string, (IValue, bool)>();

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

            public void AddVariable(string varName, IValue accessHandle, bool requiresLoading) =>
                this.variables.Add(varName, (accessHandle, requiresLoading));

            public void AddValue(IValue value, string? releaseFunction = null)
            {
                if (releaseFunction != null)
                {
                    this.requiredReleases.Add((value, releaseFunction));
                }
                if (value.NativeType.IsPointer)
                {
                    this.trackedValues.Add(value);
                }
            }

            public bool TryGetVariable(string varName, out (IValue, bool) accessHandle) =>
                this.variables.TryGetValue(varName, out accessHandle);

            /// <summary>
            /// Returns true if the given value will be unreferenced by <see cref="ExecutePendingCalls" />
            /// unless it is explicitly excluded.
            /// </summary>
            internal bool WillBeUnreferenced(IValue value) =>
                this.trackedValues.Contains(value);

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
                    builder.Call(func, value);
                }

                foreach (var (_, (item, mutable)) in this.variables)
                {
                    var value = mutable // mutable values are represented as pointers and require loading
                        ? builder.Load(Types.PointerElementType(item), item)
                        : item;
                    this.parent.DecreaseAccessCount(value);
                }

                foreach (var value in this.trackedValues)
                {
                    if (!omitUnreferencing.Remove(value)) // FIXME: FOR COMPARISON WE NEED TO GO BY VALUE, NOT IVALUE!
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
        private string? AddAccessFunctionForType(ITypeRef t)
        {
            if (t.IsPointer)
            {
                if (t == this.sharedState.Types.Array)
                {
                    return RuntimeLibrary.ArrayAddAccess;
                }
                else if (Types.IsTypedTuple(t))
                {
                    return RuntimeLibrary.TupleAddAccess;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the name of the runtime function to decrease the access count for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? RemoveAccessFunctionForType(ITypeRef t)
        {
            if (t.IsPointer)
            {
                if (t == this.sharedState.Types.Array)
                {
                    return RuntimeLibrary.ArrayRemoveAccess;
                }
                else if (Types.IsTypedTuple(t))
                {
                    return RuntimeLibrary.TupleRemoveAccess;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the name of the runtime function to increase the reference count for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? ReferenceFunctionForType(ITypeRef t)
        {
            if (t.IsPointer)
            {
                if (t == this.sharedState.Types.Array)
                {
                    return RuntimeLibrary.ArrayReference;
                }
                else if (t == this.sharedState.Types.Result)
                {
                    return RuntimeLibrary.ResultReference;
                }
                else if (t == this.sharedState.Types.String)
                {
                    return RuntimeLibrary.StringReference;
                }
                else if (t == this.sharedState.Types.BigInt)
                {
                    return RuntimeLibrary.BigIntReference;
                }
                else if (Types.IsTypedTuple(t))
                {
                    return RuntimeLibrary.TupleReference;
                }
                else if (t == this.sharedState.Types.Callable)
                {
                    return RuntimeLibrary.CallableReference;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the name of the runtime function to decrease the access count for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? UnreferenceFunctionForType(ITypeRef t)
        {
            if (t.IsPointer)
            {
                if (t == this.sharedState.Types.Array)
                {
                    return RuntimeLibrary.ArrayUnreference;
                }
                else if (t == this.sharedState.Types.Result)
                {
                    return RuntimeLibrary.ResultUnreference;
                }
                else if (t == this.sharedState.Types.String)
                {
                    return RuntimeLibrary.StringUnreference;
                }
                else if (t == this.sharedState.Types.BigInt)
                {
                    return RuntimeLibrary.BigIntUnreference;
                }
                else if (Types.IsTypedTuple(t))
                {
                    return RuntimeLibrary.TupleUnreference;
                }
                else if (t == this.sharedState.Types.Callable)
                {
                    return RuntimeLibrary.CallableUnreference;
                }
            }
            return null;
        }

        /// <summary>
        /// If the given function returns a function name for the given value,
        /// applies the runtime function with that name to the given value, casting the value if necessary,
        /// Recurs into contained items.
        /// </summary>
        private void RecursivelyModifyCounts(Func<ITypeRef, string?> getFunctionName, IValue value, InstructionBuilder? builder = null)
        {
            void ModifyCounts(Func<ITypeRef, string?> getItemFunc, string funcName, IValue value, InstructionBuilder builder)
            {
                if (this.sharedState.Types.IsTypedTuple(value.NativeType))
                {
                    // for tuples we also unreference all inner tuples
                    var tupleStruct = Types.StructFromPointer(value.NativeType);
                    for (var i = 0; i < tupleStruct.Members.Count; ++i)
                    {
                        var itemFuncName = getItemFunc(tupleStruct.Members[i]);
                        if (itemFuncName != null)
                        {
                            var item = this.sharedState.GetTupleElement(tupleStruct, value, i, builder);
                            ModifyCounts(getItemFunc, itemFuncName, item, builder);
                        }
                    }

                    var untypedTuple = builder.BitCast(value, this.sharedState.Types.Tuple);
                    var func = this.sharedState.GetOrCreateRuntimeFunction(funcName);
                    builder.Call(func, untypedTuple);
                }
                else
                {
                    if (value.NativeType == this.sharedState.Types.Array)
                    {
                        // TODO
                        // RECURSIVELY UNREFERENCE INNER ITEMS
                    }
                    else if (value.NativeType == this.sharedState.Types.Callable)
                    {
                        // TODO
                        // RECURSIVELY UNREFERENCE THE CAPTURE TUPLE
                    }

                    var func = this.sharedState.GetOrCreateRuntimeFunction(funcName);
                    builder.Call(func, value);
                }
            }

            builder ??= this.sharedState.CurrentBuilder;
            var func = getFunctionName(value.NativeType);
            if (func != null)
            {
                ModifyCounts(getFunctionName, func, value, builder);
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
            var releaser =
                value.NativeType == this.sharedState.Types.Array ? RuntimeLibrary.QubitReleaseArray :
                value.NativeType == this.sharedState.Types.Qubit ? RuntimeLibrary.QubitRelease :
                throw new ArgumentException("AddQubitValue expects an argument of type Qubit or Qubit[]");
            this.scopes.Peek().AddValue(value, releaser);
        }

        /// <summary>
        /// Registers a variable name as an alias for an LLVM value.
        /// </summary>
        /// <param name="name">The name to register</param>
        /// <param name="value">The LLVM value</param>
        /// <param name="isMutable">true if the name binding is mutable, false if immutable; the default is false</param>
        internal void RegisterVariable(string name, IValue value, bool isMutable = false)
        {
            if (string.IsNullOrEmpty(value.Name))
            {
                value.RegisterName(this.sharedState.InlinedName(name));
            }
            this.IncreaseAccessCount(value);
            this.scopes.Peek().AddVariable(name, value, isMutable);
        }

        /// <summary>
        /// Gets the pointer to a mutable variable by name.
        /// The name must have been registered as an alias for the pointer value using
        /// <see cref="RegisterVariable(string, Value, bool)"/>.
        /// </summary>
        /// <param name="name">The registered variable name to look for</param>
        /// <returns>The pointer value for the mutable value</returns>
        internal Value GetNamedPointer(string name)
        {
            foreach (var scope in this.scopes)
            {
                if (scope.TryGetVariable(name, out (IValue, bool) item))
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
        /// Gets the value of a named variable on the value stack, loading the value if necessary.
        /// The name must have been registered as an alias for the value using
        /// <see cref="RegisterVariable(string, Value, bool)"/>.
        /// <para>
        /// If the variable is mutable, then the associated pointer value is used to load and push the actual
        /// variable value.
        /// </para>
        /// </summary>
        /// <param name="name">The registered variable name to look for</param>
        internal IValue GetNamedValue(string name)
        {
            foreach (var scope in this.scopes)
            {
                if (scope.TryGetVariable(name, out (QirValues, bool) item))
                {
                    return item.Item2
                        // Mutable, so the value is a pointer; we need to load what it's pointing to
                        ? this.sharedState.CurrentBuilder.Load(Types.PointerElementType(item.Item1), item.Item1)
                        : item.Item1;
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

            var omittedUnreferences = new List<Value>() { returned };
            foreach (var scope in this.scopes)
            {
                scope.ExecutePendingCalls(builder, omittedUnreferences);
            }
        }
    }
}
