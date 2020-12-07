// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
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
        // We keep the name of the release function, rather than the actual IrFunction, so that we don't
        // generate references to functions that we wind up not actually calling (because the value gets removed).
        private readonly Stack<List<(Value, string)>> releaseStack = new Stack<List<(Value, string)>>();
        private readonly GenerationContext sharedState;

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
        /// Gets the name of the unreference runtime function for a given Q# type.
        /// </summary>
        /// <param name="t">The Q# type</param>
        /// <param name="isQubit">true if the unreference function should deallocate qubits as well as
        /// decrement the reference count</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? GetReleaseFunctionForType(ResolvedType t, bool isQubit)
        {
            if (t.Resolution.IsArrayType)
            {
                if (isQubit)
                {
                    return RuntimeLibrary.QubitReleaseArray;
                }
                else
                {
                    return RuntimeLibrary.ArrayUnreference;
                }
            }
            else if (t.Resolution.IsQubit)
            {
                return RuntimeLibrary.QubitRelease;
            }
            else if (t.Resolution.IsResult)
            {
                return RuntimeLibrary.ResultUnreference;
            }
            else if (t.Resolution.IsString)
            {
                return RuntimeLibrary.StringUnreference;
            }
            else if (t.Resolution.IsBigInt)
            {
                return RuntimeLibrary.BigintUnreference;
            }
            else if (t.Resolution.IsTupleType)
            {
                return RuntimeLibrary.TupleUnreference;
            }
            else if (t.Resolution.IsOperation || t.Resolution.IsFunction)
            {
                return RuntimeLibrary.CallableUnreference;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the name of the unreference runtime function for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <param name="isQubit">true if the unreference function should deallocate qubits as well as
        /// decrement the reference count</param>
        /// <returns>The name of the unreference function for this type</returns>
        private string? GetReleaseFunctionForType(ITypeRef t, bool isQubit)
        {
            if (t == this.sharedState.Types.Array)
            {
                if (isQubit)
                {
                    return RuntimeLibrary.QubitReleaseArray;
                }
                else
                {
                    return RuntimeLibrary.ArrayUnreference;
                }
            }
            else if (t == this.sharedState.Types.Qubit)
            {
                return RuntimeLibrary.QubitRelease;
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
                return RuntimeLibrary.BigintUnreference;
            }
            else if (t == this.sharedState.Types.Tuple || this.sharedState.Types.IsTupleType(t))
            {
                return RuntimeLibrary.TupleUnreference;
            }
            else if (t == this.sharedState.Types.Callable)
            {
                return RuntimeLibrary.CallableUnreference;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Generates the releases (unreference calls) for a single scope in the stack.
        /// </summary>
        /// <param name="pendingReleases">The list of pending releases that defines a scope</param>
        /// <param name="builder">The InstructionBuilder where the release calls should be generated</param>
        private void GenerateReleasesForLevel(List<(Value, string)> pendingReleases, InstructionBuilder builder)
        {
            foreach ((Value valueToRelease, string releaseFunc) in pendingReleases)
            {
                IrFunction func = this.sharedState.GetOrCreateRuntimeFunction(releaseFunc);
                // special case for tuples
                if (releaseFunc == RuntimeLibrary.TupleUnreference)
                {
                    var untypedTuple = builder.BitCast(valueToRelease, this.sharedState.Types.Tuple);
                    builder.Call(func, untypedTuple);
                }
                else
                {
                    builder.Call(func, valueToRelease);
                }
            }
        }

        // public methods

        /// <summary>
        /// Resets the manager by emptying the scope stack.
        /// </summary>
        public void Reset()
        {
            this.releaseStack.Clear();
            this.releaseStack.Push(new List<(Value, string)>());
        }

        /// <summary>
        /// Opens a new ref counting scope.
        /// The new scope is a child of the current scope (it is pushed on to the scope stack).
        /// </summary>
        public void OpenScope()
        {
            this.releaseStack.Push(new List<(Value, string)>());
        }

        /// <summary>
        /// Adds a value to the current topmost scope.
        /// </summary>
        /// <param name="valueToRelease">The Value to be released</param>
        /// <param name="valueType">
        /// Optional parameter with the Q# type of the value;
        /// If no parameter is given, then the release function will be determined
        /// based on the NativeType of the value to release.
        /// </param>
        public void AddValue(Value valueToRelease, ResolvedType? valueType = null)
        {
            var releaser = valueType == null
                ? this.GetReleaseFunctionForType(valueToRelease.NativeType, false)
                : this.GetReleaseFunctionForType(valueType, false);
            if (releaser != null)
            {
                this.releaseStack.Peek().Add((valueToRelease, releaser));
            }
        }

        /// <summary>
        /// Adds a qubit value to the current topmost scope.
        /// </summary>
        /// <param name="valueToRelease">The Value to be released</param>
        /// <param name="valueType">The Q# type of the value, which should be either a Qubit
        /// or an array of Qubits.</param>
        public void AddQubitValue(Value valueToRelease, ResolvedType valueType)
        {
            var releaser = this.GetReleaseFunctionForType(valueType, true);
            if (releaser != null)
            {
                this.releaseStack.Peek().Add((valueToRelease, releaser));
            }
        }

        /// <summary>
        /// Removes a pending Value from those to be unreferenced.
        /// This is necessary, for example, for values that are returned to the caller.
        /// </summary>
        /// <param name="valueToRemove">The Value to remove</param>
        public void RemovePendingValue(Value valueToRemove)
        {
            foreach (var level in this.releaseStack)
            {
                level.RemoveAll(item => item.Item1 == valueToRemove);
            }
        }

        /// <summary>
        /// Closes the current scope by emitting any pending releases and popping the scope off of the stack.
        /// Note that if the current basic block is already terminated, presumably by a return,
        /// the pending releases are not generated.
        /// The releases are generated in the current block.
        /// </summary>
        public void CloseScope(bool isTerminated)
        {
            var releases = this.releaseStack.Pop();
            // If the current block is already terminated, presumably be a return, don't generate releases
            if (!isTerminated)
            {
                this.GenerateReleasesForLevel(releases, this.sharedState.CurrentBuilder);
            }
        }

        /// <summary>
        /// Closes the current scope by emitting any pending releases and popping the scope off of the stack.
        /// The releases are generated unconditionally.
        /// </summary>
        /// <param name="builder">The InstructionBuilder where the release calls should be generated</param>
        public void ForceCloseScope(InstructionBuilder builder)
        {
            var releases = this.releaseStack.Pop();
            this.GenerateReleasesForLevel(releases, builder);
        }

        /// <summary>
        /// Exits the current scope stack by generating all of the pending releases for all open scopes.
        /// The releases are generated in the current block.
        /// </summary>
        public void ExitScope()
        {
            foreach (var level in this.releaseStack)
            {
                this.GenerateReleasesForLevel(level, this.sharedState.CurrentBuilder);
            }
        }
    }
}
