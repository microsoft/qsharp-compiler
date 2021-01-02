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
        // FIXME THE STACK NEEDS TO CONTAIN SOMETHING WITH MORE TYPE INFO
        // -> HAVE TUPLEVALUE, ARRAYVALUE ETC INHERIT FROM A COMMON CLASS AND KEEP A STACK OF THAT INSTEAD...
        // WE CAN KEEP THE NECESSARY FUNCTION AS A STATIC MEMBER IN THAT CLASS AS WELL, ELMININATING THEM FROM THE ScopeManager.
        // FIXME: NAMING SCOPE SHOULD ALSO BE PART OF THIS CLASS...
        private readonly Stack<List<(Value, string)>> pendingCalls = new Stack<List<(Value, string)>>();
        private readonly GenerationContext sharedState;

        /// <summary>
        /// Is true when there are currently no stack frames tracked.
        /// Stack frames are added and removed by OpenScope and CloseScope respectively.
        /// </summary>
        public bool IsEmpty => !this.pendingCalls.Any();

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
        private void ExecutePendingCalls(IEnumerable<(Value, string)> pending, InstructionBuilder builder)
        {
            foreach ((Value value, string funcName) in pending)
            {
                this.RecursivelyModifyCounts(value, funcName, this.UnreferenceFunctionForType, builder);
            }
        }

        // public methods

        /// <summary>
        /// Opens a new ref counting scope.
        /// The new scope is a child of the current scope (it is pushed on to the scope stack).
        /// </summary>
        public void OpenScope()
        {
            this.pendingCalls.Push(new List<(Value, string)>());
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
        private void QueueDecreaseAccessCount(Value value) // FIXME: WHY IS THIS NOT NEEDED? WHY IS NAMING SCOPE A SEPARATE THING?
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
        /// Closes the current scope by emitting the calls to unreference values going out of scope and popping the scope off of the stack.
        /// If the current basic block is already terminated, presumably by a return, the calls are not generated.
        /// The calls are generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        public void CloseScope(bool isTerminated, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;
            var pending = this.pendingCalls.Pop();
            if (!isTerminated)
            {
                this.ExecutePendingCalls(pending, builder);
            }
        }

        /// <summary>
        /// Exits the current scope by emitting the calls to unreference values going out of scope for all open scopes.
        /// Increases the reference count of the returned value by 1, either by omitting to unreference it or by explicitly increasing it.
        /// The calls are generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// Exiting the current scope does *not* close the scope.
        /// </summary>
        /// <param name="returned">The value that is returned and expected to remain valid after exiting.</param>
        public void ExitScope(Value returned, InstructionBuilder? builder = null)
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
            foreach (var frame in this.pendingCalls)
            {
                if (executeAll)
                {
                    this.ExecutePendingCalls(frame, builder);
                    continue;
                }

                var unreferenceCallsForReturn = frame.Where(call => call == (returned, unreferenceFunc));
                var otherCalls = frame.Where(call => call != (returned, unreferenceFunc));
                this.ExecutePendingCalls(unreferenceCallsForReturn.Skip(1).Concat(otherCalls), builder);
                executeAll = unreferenceCallsForReturn.Any();
            }
        }
    }
}
