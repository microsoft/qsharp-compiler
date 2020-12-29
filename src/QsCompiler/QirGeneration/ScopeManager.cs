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
        private readonly Stack<List<(Value, string)>> releaseStack = new Stack<List<(Value, string)>>();
        private readonly GenerationContext sharedState;

        /// <summary>
        /// Is true when there are currently no stack frames tracked.
        /// Stack frames are added and removed by OpenScope and CloseScope respectively.
        /// </summary>
        public bool IsEmpty => !this.releaseStack.Any();

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
        /// Gets the name of the reference runtime function for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? GetReferenceFunctionForType(ITypeRef t)
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
        /// Gets the name of the unreference runtime function for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? GetReleaseFunctionForType(ITypeRef t)
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
        private void RecursivelyModifyReferences(Value value, Value func, Func<ITypeRef, string?> getItemFunc, InstructionBuilder builder)
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
                        var ptr = this.sharedState.GetTupleElementPointer(tupleStruct, value, i, builder);
                        var item = builder.Load(tupleStruct.Members[i], ptr);
                        this.RecursivelyModifyReferences(item, this.sharedState.GetOrCreateRuntimeFunction(itemFuncName), getItemFunc, builder);
                    }
                }

                var untypedTuple = builder.BitCast(value, this.sharedState.Types.Tuple);
                builder.Call(func, untypedTuple);
            }
            else
            {
                if (value.NativeType == this.sharedState.Types.Array)
                {
                    // TODO:
                    // We need to generate and pass in a release function that is to be applied to each item
                }
                else if (value.NativeType == this.sharedState.Types.Callable)
                {
                    // TODO
                    // Releasing any captured callable (first item in the capture tuple for a partial application)
                    // could in principle be done by the runtime, so not sure if we should do something here
                }

                builder.Call(func, value);
            }
        }

        /// <summary>
        /// Generates the releases (unreference calls) for a single scope in the stack.
        /// </summary>
        /// <param name="pendingReleases">The list of pending releases that defines a scope</param>
        /// <param name="builder">The InstructionBuilder where the release calls should be generated</param>
        private void GenerateReleasesForLevel(IEnumerable<(Value, string)> pendingReleases, InstructionBuilder builder)
        {
            foreach ((Value valueToRelease, string releaseFunc) in pendingReleases)
            {
                IrFunction func = this.sharedState.GetOrCreateRuntimeFunction(releaseFunc);
                this.RecursivelyModifyReferences(valueToRelease, func, this.GetReleaseFunctionForType, builder);
            }
        }

        // public methods

        /// <summary>
        /// Opens a new ref counting scope.
        /// The new scope is a child of the current scope (it is pushed on to the scope stack).
        /// </summary>
        public void OpenScope()
        {
            this.releaseStack.Push(new List<(Value, string)>());
        }

        /// <summary>
        /// Adds a value to the current topmost scope,
        /// and makes sure that the value and all its items are unreferenced when the scope is closed or exited.
        /// </summary>
        /// <param name="value">Value that is created within the current scope</param>
        public void AddValue(Value value)
        {
            var releaser = this.GetReleaseFunctionForType(value.NativeType);
            if (releaser != null)
            {
                this.releaseStack.Peek().Add((value, releaser));
            }
        }

        /// <summary>
        /// Adds a value constructed as part of a qubit allocation to the current topmost scope.
        /// Makes sure that all allocated qubits are released when the scope is closed or exited
        /// and that the value and all its items are unreferenced.
        /// </summary>
        /// <param name="value">The value to be released</param>
        /// or an array of Qubits.</param>
        public void AddQubitAllocation(Value value)
        {
            var releaser =
                value.NativeType == this.sharedState.Types.Array ? RuntimeLibrary.QubitReleaseArray :
                value.NativeType == this.sharedState.Types.Qubit ? RuntimeLibrary.QubitRelease :
                throw new ArgumentException("AddQubitValue expects an argument of type Qubit or Qubit[]");
            this.releaseStack.Peek().Add((value, releaser)); // this takes care purely of the deallocation
            this.AddValue(value); // this takes care of properly unreferencing the created array if necessary
        }

        /// <summary>
        /// Adds a call to a runtime library function to increase the reference count
        /// for the given value if necessary.
        /// The call is generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="value">The value which is referenced</param>
        public void AddReference(Value value, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;
            var referenceFunc = this.GetReferenceFunctionForType(value.NativeType);
            if (referenceFunc != null)
            {
                IrFunction func = this.sharedState.GetOrCreateRuntimeFunction(referenceFunc);
                this.RecursivelyModifyReferences(value, func, this.GetReferenceFunctionForType, builder);
            }
        }

        /// <summary>
        /// Given a pointer loads the current value at that location and queues a suitable function
        /// to unreference that value upon closing or exciting the scope.
        /// The loads and call are generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        /// <param name="pointer">Pointer to the value that will no longer be accessible via that pointer</param>
        public void RemoveReference(Value pointer, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;
            var type = ((IPointerType)pointer.NativeType).ElementType;
            var releaser = this.GetReleaseFunctionForType(type);
            if (releaser != null)
            {
                var value = builder.Load(type, pointer);
                this.releaseStack.Peek().Add((value, releaser));
            }
        }

        /// <summary>
        /// Closes the current scope by emitting any pending releases and popping the scope off of the stack.
        /// If the current basic block is already terminated, presumably by a return, the pending releases are not generated.
        /// The releases are generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// </summary>
        public void CloseScope(bool isTerminated, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;
            var releases = this.releaseStack.Pop();
            // If the current block is already terminated, presumably be a return, don't generate releases
            if (!isTerminated)
            {
                this.GenerateReleasesForLevel(releases, builder);
            }
        }

        /// <summary>
        /// Exits the current scope stack by generating all of the pending releases for all open scopes.
        /// Skips any release function for the returned value.
        /// The releases are generated in the current block if no builder is specified, and otherwise the given builder is used.
        /// Exiting the current scope does *not* close the scope.
        /// </summary>
        /// <param name="returned">The value that is returned and expected to remain valid after exiting.</param>
        public void ExitScope(Value returned, InstructionBuilder? builder = null)
        {
            builder ??= this.sharedState.CurrentBuilder;
            foreach (var releases in this.releaseStack)
            {
                this.GenerateReleasesForLevel(releases.Where(value => value.Item1 != returned), builder);
            }
        }
    }
}
