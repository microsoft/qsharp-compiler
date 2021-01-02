// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QIR;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    /// <summary>
    /// This class is used to track reference counts that must be decremented before leaving the current scope.
    /// <para>
    /// Scopes are nested, corresponding to nested scopes in Q#.
    /// In general any Q# construct that opens a naming scope also opens a ref counting scope.
    /// </para>
    /// <para>
    /// There are two primary ways to leave a scope: close it and continue in the parent scope, or exit the current
    /// scope and all parent scopes and leave the callable altogether (i.e., return).
    /// While each scope has a single place it is closed, there may be many exits.
    /// When a scope is closed, the unreferences for that scope are emitted.
    /// When the callable is exited, all pending unreferences for the entire stack of scopes need to be emitted.
    /// </para>
    /// </summary>
    internal class ScopeManager
    {
        private readonly GenerationContext sharedState;

        /// <summary>
        /// Naming scopes map variable names to values.
        /// New names are always added to the scope on top of the stack.
        /// When looking for a name, the stack is searched top-down.
        /// </summary>
        private readonly Stack<Dictionary<string, (Value, bool)>> namesInScope = new Stack<Dictionary<string, (Value, bool)>>();

        // FIXME THE STACK NEEDS TO CONTAIN SOMETHING WITH MORE TYPE INFO
        // -> HAVE TUPLEVALUE, ARRAYVALUE ETC INHERIT FROM A COMMON CLASS AND KEEP A STACK OF THAT INSTEAD...
        // WE CAN KEEP THE NECESSARY FUNCTION AS A STATIC MEMBER IN THAT CLASS AS WELL, ELMININATING THEM FROM THE ScopeManager.
        // FIXME: NAMING SCOPE SHOULD ALSO BE PART OF THIS CLASS...
        private readonly Stack<List<(Value, string)>> pendingCalls = new Stack<List<(Value, string)>>();

        /// <summary>
        /// Is true when there are currently no stack frames tracked.
        /// Stack frames are added and removed by OpenScope and CloseScope respectively.
        /// </summary>
        public bool IsEmpty => !this.pendingCalls.Any(); // FIXME: NAMES IN SCOPE AND PENDING CALLS SHOULD ALWAYS MATCH - MAKE SURE OF THAT BY ADDING A SUITABLE DATA STRUCTURE

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
                else if (this.sharedState.Types.IsTupleType(t))
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
                else if (this.sharedState.Types.IsTupleType(t))
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
                else if (this.sharedState.Types.IsTupleType(t))
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
                else if (this.sharedState.Types.IsTupleType(t))
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
        /// Applies the given function to the given value, casting the value if necessary,
        /// and then recurs into contained items and if getItemFunc returns not null, applies the returned function to them.
        /// </summary>
        private void RecursivelyModifyCounts(Value value, string funcName, Func<ITypeRef, string?> getItemFunc, InstructionBuilder builder)
        {
            if (this.sharedState.Types.IsTupleType(value.NativeType))
            {
                // for tuples we also unreference all inner tuples
                var tupleStruct = Types.StructFromPointer(value.NativeType);
                for (var i = 0; i < tupleStruct.Members.Count; ++i)
                {
                    var itemFuncName = getItemFunc(tupleStruct.Members[i]);
                    if (itemFuncName != null)
                    {
                        var item = this.sharedState.GetTupleElement(tupleStruct, value, i, builder);
                        this.RecursivelyModifyCounts(item, itemFuncName, getItemFunc, builder);
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

        /// <summary>
        /// Generates the calls to unreference the the registered values for a single scope in the stack.
        /// </summary>
        /// <param name="pending">The list of pending calls to unreference the values for a scope</param>
        /// <param name="builder">The InstructionBuilder where the calls should be generated</param>
        private void ExecutePendingCalls(IDictionary<string, (Value, bool)> variables, IEnumerable<(Value, string)> calls, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;

            foreach (var (_, (item, mutable)) in variables)
            {
                var value = mutable // mutable values are represented as pointers and require loading
                    ? builder.Load(((IPointerType)item.NativeType).ElementType, item)
                    : item;
                this.DecreaseAccessCount(value);
            }

            foreach ((Value value, string funcName) in calls)
            {
                this.RecursivelyModifyCounts(value, funcName, this.UnreferenceFunctionForType, builder);
            }
        }

        // public methods

        /// <summary>
        /// Opens a new scope and pushes it on top of the naming scope stack.
        /// Opening a new scope automatically opens a new naming scope as well.
        /// </summary>
        public void OpenScope()
        {
            this.pendingCalls.Push(new List<(Value, string)>());
            this.namesInScope.Push(new Dictionary<string, (Value, bool)>());
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
            var variables = this.namesInScope.Pop();
            var pending = this.pendingCalls.Pop();
            if (!isTerminated)
            {
                this.ExecutePendingCalls(variables, pending, builder);
            }
        }

        public void ExitScope(bool isTerminated, InstructionBuilder? builder = null)
        {
            var variables = this.namesInScope.Peek();
            var pending = this.pendingCalls.Peek();
            if (!isTerminated)
            {
                this.ExecutePendingCalls(variables, pending, builder);
            }
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
                value.RegisterName(this.sharedState.InlinedName(name));
            }
            this.IncreaseAccessCount(value);
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
        /// Gets the value of a named variable on the value stack, loading the value if necessary.
        /// The name must have been registered as an alias for the value using
        /// <see cref="RegisterName(string, Value, bool)"/>.
        /// <para>
        /// If the variable is mutable, then the associated pointer value is used to load and push the actual
        /// variable value.
        /// </para>
        /// </summary>
        /// <param name="name">The registered variable name to look for</param>
        internal Value GetNamedValue(string name)
        {
            foreach (var dict in this.namesInScope)
            {
                if (dict.TryGetValue(name, out (Value, bool) item))
                {
                    return item.Item2
                        // Mutable, so the value is a pointer; we need to load what it's pointing to
                        ? this.sharedState.CurrentBuilder.Load(((IPointerType)item.Item1.NativeType).ElementType, item.Item1)
                        : item.Item1;
                }
            }
            throw new KeyNotFoundException($"Could not find a Value for local symbol {name}");
        }

        /// <summary>
        /// Adds a call to a runtime library function to increase the access count for the given value if necessary.
        /// The call is generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="value">The value which is assigned to a handle</param>
        internal void IncreaseAccessCount(Value value, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;
            var referenceFunc = this.AddAccessFunctionForType(value.NativeType);
            if (referenceFunc != null)
            {
                this.RecursivelyModifyCounts(value, referenceFunc, this.AddAccessFunctionForType, builder);
            }
        }

        /// <summary>
        /// Adds a call to a runtime library function to decrease the access count for the given value if necessary.
        /// The call is generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="value">The value which is unassigned from a handle</param>
        internal void DecreaseAccessCount(Value value, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;
            var unreferenceFunc = this.RemoveAccessFunctionForType(value.NativeType);
            if (unreferenceFunc != null)
            {
                this.RecursivelyModifyCounts(value, unreferenceFunc, this.RemoveAccessFunctionForType, builder);
            }
        }

        /// <summary>
        /// Queues a call to a suitable runtime library function that decreases the access count for the value
        /// when the scope is closed or exited.
        /// </summary>
        /// <param name="value">The value which is unassigned from a handle</param>
        private void QueueDecreaseAccessCount(Value value)
        // FIXME: WHY IS THIS NOT NEEDED? WHY IS NAMING SCOPE A SEPARATE THING?
        // QUEUE DECREASE ACCESS COUNT IS NOT NEEDED BECAUSE ACCESS COUNTS REFLECT BINDINGS TO VARIABLES,
        // AND SINCE WE TRACK VARIABLES, WE CAN AUTOMATICALLY DECREASET THE ACCESS COUNT UPON POPING THE STACK
        // QueueDecreaseReferenceCount IS JUST THE REGISTRATION FOR UNNAMED VARIABLES
        {
            var func = this.RemoveAccessFunctionForType(value.NativeType);
            if (func != null)
            {
                this.pendingCalls.Peek().Add((value, func));
            }
        }

        /// <summary>
        /// Adds a call to a runtime library function to increase the reference count for the given value if necessary.
        /// The call is generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="value">The value which is referenced</param>
        public void IncreaseReferenceCount(Value value, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;
            var referenceFunc = this.ReferenceFunctionForType(value.NativeType);
            if (referenceFunc != null)
            {
                this.RecursivelyModifyCounts(value, referenceFunc, this.ReferenceFunctionForType, builder);
            }
        }

        /// <summary>
        /// Adds a call to a runtime library function to decrease the reference count for the given value if necessary.
        /// The call is generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="value">The value which is unreferenced</param>
        public void DecreaseReferenceCount(Value value, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;
            var unreferenceFunc = this.UnreferenceFunctionForType(value.NativeType);
            if (unreferenceFunc != null)
            {
                this.RecursivelyModifyCounts(value, unreferenceFunc, this.UnreferenceFunctionForType, builder);
            }
        }

        /// <summary>
        /// Queues a call to a suitable runtime library function that unreferences the value
        /// when the scope is closed or exited.
        /// </summary>
        /// <param name="value">Value that is created within the current scope</param>
        public void QueueDecreaseReferenceCount(Value value)
        {
            var func = this.UnreferenceFunctionForType(value.NativeType);
            if (func != null)
            {
                this.pendingCalls.Peek().Add((value, func));
            }
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
            this.pendingCalls.Peek().Add((value, releaser)); // this takes care purely of the deallocation
            this.QueueDecreaseReferenceCount(value); // this takes care of properly unreferencing the created array if necessary
        }

        /// <summary>
        /// Exits the current function by emitting the calls to unreference values going out of scope for all open scopes.
        /// Increases the reference count of the returned value by 1, either by omitting to unreference it or by explicitly increasing it.
        /// The calls are generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// Exiting the current scope does *not* close the scope.
        /// </summary>
        /// <param name="returned">The value that is returned and expected to remain valid after exiting.</param>
        public void ExitFunction(Value returned, InstructionBuilder? builder = null)
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

            string? unreferenceFunc = this.UnreferenceFunctionForType(returned.NativeType);
            var returnWillBeUnreferenced =
                unreferenceFunc != null
                && this.pendingCalls.Any(pending => pending.Contains((returned, unreferenceFunc)));

            if (!returnWillBeUnreferenced)
            {
                this.IncreaseReferenceCount(returned);
            }

            var executeAll = !returnWillBeUnreferenced;
            foreach (var (variables, frame) in this.namesInScope.Zip(this.pendingCalls, (n, f) => (n, f)))
            {
                if (executeAll)
                {
                    this.ExecutePendingCalls(variables, frame, builder);
                    continue;
                }

                var unreferenceCallsForReturn = frame.Where(call => call == (returned, unreferenceFunc));
                var otherCalls = frame.Where(call => call != (returned, unreferenceFunc));
                this.ExecutePendingCalls(variables, unreferenceCallsForReturn.Skip(1).Concat(otherCalls), builder);
                executeAll = unreferenceCallsForReturn.Any();
            }
        }
    }
}
