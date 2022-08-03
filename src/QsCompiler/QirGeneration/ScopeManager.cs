// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LlvmBindings.Types;
using LlvmBindings.Values;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    /// <summary>
    /// This class is used to track the validity of variables and values, to track alias and reference counts,
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
            private readonly Action<Func<ITypeRef, string?>, (IValue, bool)[]> increaseCounts;
            private readonly Action<Func<ITypeRef, string?>, (IValue, bool)[]> decreaseCounts;

            /// <summary>
            /// Maps variable names to the corresponding value.
            /// Mutable variables are represented as PointerValues.
            /// </summary>
            private readonly Dictionary<string, IValue> variables = new();

            /// <summary>
            /// Contains all values whose reference count has been increased.
            /// The first items contains the value, and the second item indicates whether to recursively reference inner items.
            /// </summary>
            private readonly List<(IValue, bool)> pendingReferences = new();

            /// <summary>
            /// Contains all values that require unreferencing upon closing the scope.
            /// The first items contains the value, and the second item indicates whether to recursively unreference inner items.
            /// </summary>
            private readonly List<(IValue, bool)> requiredUnreferences = new();

            /// <summary>
            /// Contains the values that require invoking a release function upon closing the scope,
            /// as well as the name of the release function to invoke.
            /// </summary>
            private readonly List<Action> requiredReleases = new();

            public Scope(Action<Func<ITypeRef, string?>, (IValue, bool)[]> increaseCounts, Action<Func<ITypeRef, string?>, (IValue, bool)[]> decreaseCounts)
            {
                this.increaseCounts = increaseCounts;
                this.decreaseCounts = decreaseCounts;
            }

            private static bool ValueEquals((IValue, bool) tracked, (IValue, bool) expected) =>
                tracked.Item1.Value == expected.Item1.Value && tracked.Item2 == expected.Item2;

            private static bool TryRemoveValue(List<(IValue, bool)> values, Func<(IValue, bool), bool> condition)
            {
                var index = values.FindIndex(v => condition(v));
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    values.RemoveAt(index);
                    return true;
                }
            }

            private static IValue LoadValue(IValue value)
            {
                while (value is PointerValue ptr)
                {
                    value = ptr.LoadValue();
                }

                return value;
            }

            private static IEnumerable<(IValue, bool)> Expand(Func<ITypeRef, string?> getFunctionName, IValue value, bool recurIntoInnerItems, List<(IValue, bool)>? omitExpansion = null)
            {
                var func = getFunctionName(value.LlvmType);
                if (func != null)
                {
                    omitExpansion ??= new List<(IValue, bool)>();
                    var omitted = omitExpansion.FirstOrDefault(omitted => ValueEquals((value, recurIntoInnerItems), omitted));
                    if (omitExpansion.Remove(omitted))
                    {
                        yield break;
                    }

                    if (value is TupleValue tuple)
                    {
                        for (var i = 0; i < tuple.LlvmElementTypes.Count && recurIntoInnerItems; ++i)
                        {
                            var itemFuncName = getFunctionName(tuple.LlvmElementTypes[i]);
                            if (itemFuncName != null)
                            {
                                var item = tuple.GetTupleElement(i);
                                foreach (var inner in Expand(getFunctionName, item, true, omitExpansion))
                                {
                                    yield return inner;
                                }
                            }
                        }

                        yield return (value, false);
                    }
                    else
                    {
                        yield return (value, recurIntoInnerItems);
                    }
                }
            }

            // public and internal methods

            /// <summary>
            /// Decreases the alias count of the unassigned value immediately.
            /// Decreases the reference count immediately unless the decrease is only to be applied to the outermost
            /// container (i.e. <paramref name="shallow"/> is set to true).
            /// </summary>
            internal void UnassignFromMutable(IValue value, bool shallow = false)
            {
                var change = new[] { (value, !shallow) };
                this.decreaseCounts(AliasCountUpdateFunctionForType, change);

                if (shallow)
                {
                    // As long as the count change only applies to the outermost container,
                    // it is save to delay the decrease until the scope is closed.
                    // This is important to ensure that for update-and-reassign statements,
                    // the value can be formally unassigned prior to evaluating the copy-and-update
                    // expression followed by the subsequent reassignment.
                    this.RegisterValue(value, shallow: true);
                }
                else
                {
                    // If the change on the other hand also applies to inner items, then it needs to be
                    // applied immediately, to ensure that it is applied to the currently stored inner items,
                    // in case the container is modified in place later on.
                    if (!TryRemoveValue(this.pendingReferences, v => ValueEquals(v, (value, !shallow))))
                    {
                        this.decreaseCounts(ReferenceCountUpdateFunctionForType, change);
                    }
                }
            }

            /// <summary>
            /// Increases the alias count of the assigned value immediately.
            /// Increases the reference count immediately if the assigned value is accessed via a local identifier,
            /// and otherwise removes a pending reference count decrease at the end of the scope if possible.
            /// If <paramref name="isFromLocalId"/> is false and no reference count decrease is queued,
            /// increases the reference count immediately.
            /// </summary>
            internal void AssignToMutable(bool isFromLocalId, IValue value, bool shallow = false)
            {
                var change = new[] { (value, !shallow) };
                this.increaseCounts(AliasCountUpdateFunctionForType, change);

                // If the assigned value is accessed via a local identifier (either directly or e.g. in the form
                // of an item access expression), then we need to make sure to apply the reference count increase
                // immediately to avoid that the bound value may be deallocated if either that local identifier
                // or the mutable variable is rebound.
                if (isFromLocalId || !this.TryRemoveValue(value, !shallow))
                {
                    // The corresponding decrease is automatically done for values assigned to mutable variables
                    // when they go out of scope or are rebound.
                    this.increaseCounts(ReferenceCountUpdateFunctionForType, change);
                }
            }

            /// <summary>
            /// Registers the given variable name with the scope.
            /// Increases the alias and reference count for the value if necessary,
            /// depending on whether the value is accessed via a mutable variable or assigned to a mutable variable.
            /// Ensures that both are decreased again when the variable goes out of scope.
            /// The registered variable is mutable if the passed value is a pointer value.
            /// </summary>
            public void RegisterVariable(string varName, IValue value, IValue? fromLocalId)
            {
                this.variables.Add(varName, value);

                var change = new[] { (value, true) }; // true refers to whether the change also applies to inner items
                this.increaseCounts(AliasCountUpdateFunctionForType, change);

                // Since the value is necessarily created in the current or a parent scope,
                // it won't go out of scope before the variable does.
                // There is hence no need to increase the reference count unless either
                // a) the value is assigned to a mutable variable (destination variable) that is later rebound, or
                // b) the assigned value is accessed via a mutable variable (source variable) that is
                //    rebound before the newly created variable goes out of scope.

                // If either the source or destination variable can be rebound, however, then updating an item via a
                // copy-and-reassign statement potentially leads to the updated item(s) being unreferenced in an inner scope,
                // i.e. before the pending reference count increases of this scope are applied. We hence need to make sure
                // to increase the reference count immediately when
                // a) binding a value accessed via a local variable to a mutable variable, or
                // b) assigning a value accessed via a mutable variable.
                if (value is PointerValue)
                {
                    // Assignment to a mutable variable:
                    // We need to make sure that when a value is bound to a mutable variable, the reference count is increased immediately
                    // if the value is accessed via an existing variable. As long as we make sure to immediately apply the reference
                    // count decrease upon unassignment from mutable variables, the recursive reference count decrease of the
                    // (potentially modified in place) value and its content at the end of the scope should be correct.
                    if (fromLocalId != null || !this.TryRemoveValue(value))
                    {
                        // The corresponding decrease is automatically done for the current value of a mutable variable
                        // when it is rebound or goes out of scope.
                        this.increaseCounts(ReferenceCountUpdateFunctionForType, change);
                    }
                }
                else if (fromLocalId is PointerValue)
                {
                    // Assignment from a mutable variable:
                    // Increase the reference count of the assigned value if it is accessed via a mutable variable,
                    // and queue the dereferencing of that value.
                    this.increaseCounts(ReferenceCountUpdateFunctionForType, change);
                    this.RegisterValue(value, shallow: false);
                }
            }

            public bool TryGetVariable(string varName, out IValue value) =>
                this.variables.TryGetValue(varName, out value);

            /// <summary>
            /// Adds the given value to the list of tracked values that need to be unreferenced when closing or exiting the scope.
            /// If the given value to register is a pointer, recursively loads its content and registers the loaded value.
            /// </summary>
            public void RegisterValue(IValue value, bool shallow = false)
            {
                if (RequiresReferenceCount(value.LlvmType))
                {
                    this.requiredUnreferences.Add((LoadValue(value), !shallow));
                }
            }

            /// <summary>
            /// Adds the release function for the given value to the list of releases that need to be executed when closing or exiting the scope.
            /// If the given value to register is a pointer, recursively loads its content such that the release is applied to the loaded value.
            /// </summary>
            public void RegisterRelease(IValue value, Action<IValue> releaseFunction)
            {
                var loadedValue = LoadValue(value);
                this.requiredReleases.Add(() => releaseFunction(loadedValue));
            }

            /// <summary>
            /// Adds the given value to the list of values which have been referenced.
            /// If the given value to unreference is a pointer, recursively loads its content and queues the loaded value for unreferencing.
            /// </summary>
            internal void ReferenceValue(IValue value, bool recurIntoInnerItems)
            {
                if (RequiresReferenceCount(value.LlvmType))
                {
                    this.pendingReferences.Add((LoadValue(value), recurIntoInnerItems));
                }
            }

            /// <summary>
            /// Returns true if the scope contains calls to reference values that have not been applied yet.
            /// </summary>
            public bool HasPendingReferences =>
                this.pendingReferences.Any();

            /// <summary>
            /// Executes all pending calls to increase reference counts.
            /// </summary>
            internal void ApplyPendingReferences()
            {
                var pending = this.pendingReferences.ToArray();
                this.pendingReferences.Clear();
                this.increaseCounts(ReferenceCountUpdateFunctionForType, pending);
            }

            /// <summary>
            /// Clears and returns all pending references from the scope.
            /// </summary>
            private List<(IValue, bool)> ClearPendingReferences()
            {
                var refs = this.pendingReferences.ToList();
                this.pendingReferences.Clear();
                return refs;
            }

            /// <summary>
            /// Adds the given value to the list of tracked values that need to be unreferenced when closing or exiting the scope.
            /// If the given value to unreference is a pointer, recursively loads its content and queues the loaded value for unreferencing.
            /// </summary>
            internal void UnreferenceValue(IValue value, bool recurIntoInnerItems)
            {
                if (RequiresReferenceCount(value.LlvmType))
                {
                    this.requiredUnreferences.Add((LoadValue(value), recurIntoInnerItems));
                }
            }

            /// <summary>
            /// Removes the given value from the list of registered values such that it will no longer be
            /// unreferenced when executing pending calls in preparation for exiting or closing the scope.
            /// Any release function that has been specified when adding the value will still execute.
            /// </summary>
            private bool TryRemoveValue(IValue value, bool recurIntoInnerItems = true) =>
                TryRemoveValue(this.requiredUnreferences, tracked => ValueEquals(tracked, (value, recurIntoInnerItems)));

            /// <summary>
            /// Generates the necessary calls to unreference the tracked values, decreases the alias count for registered variables,
            /// and invokes the specified release functions for values if necessary.
            /// Applies all pending calls to increase reference counts for the current scope, as well as for any given parent scopes.
            /// The pending reference increases will be cleared for this scope only but not for the given parent scopes.
            /// </summary>
            internal void ExecutePendingCalls(params Scope[] parentScopes)
            {
                // Not the most efficient way to go about this, but it will do for now.
                var allScopes = parentScopes.Prepend(this);
                var pendingAliasCounts = allScopes.SelectMany(s => s.variables).Select(kv => (kv.Value, true)).ToArray();

                var pendingReferences = this.ClearPendingReferences()
                    .Concat(parentScopes.SelectMany(scope => scope.pendingReferences))
                    .ToList();

                var pendingUnreferences = allScopes
                    .SelectMany(s => s.requiredUnreferences)
                    .Concat(pendingAliasCounts.Where(v => v.Value is PointerValue))
                    .SelectMany(v => Expand(ReferenceCountUpdateFunctionForType, v.Item1, v.Item2, pendingReferences))
                    .ToList();

                pendingReferences = pendingReferences
                    .SelectMany(v => Expand(ReferenceCountUpdateFunctionForType, v.Item1, v.Item2))
                    .ToList();

                var lookup1 = pendingReferences.ToLookup(x => (x.Item1.Value, x.Item2));
                var lookup2 = pendingUnreferences.ToLookup(x => (x.Item1.Value, x.Item2));
                var unnecessaryRefModifications = lookup1.SelectMany(l1s => lookup2[l1s.Key].Zip(l1s, (l2, l1) => l1));
                foreach (var item in unnecessaryRefModifications)
                {
                    var removedFromRefs = TryRemoveValue(pendingReferences, v => ValueEquals(v, item));
                    var removedFromUnrefs = TryRemoveValue(pendingUnreferences, v => ValueEquals(v, item));
                    Debug.Assert(removedFromRefs && removedFromUnrefs);
                }

                this.increaseCounts(ReferenceCountUpdateFunctionForType, pendingReferences.ToArray());
                this.decreaseCounts(AliasCountUpdateFunctionForType, pendingAliasCounts);
                this.decreaseCounts(ReferenceCountUpdateFunctionForType, pendingUnreferences.ToArray());

                foreach (var release in allScopes.SelectMany(s => s.requiredReleases))
                {
                    release();
                }
            }
        }

        private readonly GenerationContext sharedState;
        private readonly Value minusOne;
        private readonly Value plusOne;

        /// <summary>
        /// New variables and values are always added to the scope on top of the stack.
        /// When looking for a name, the stack is searched top-down.
        /// </summary>
        private readonly Stack<Scope> scopes = new();

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
            this.minusOne = ctx.Context.CreateConstant(-1);
            this.plusOne = ctx.Context.CreateConstant(1);
        }

        // private helpers

        /// <summary>
        /// Gets the name of the runtime function to update the alias count for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the function to update the alias count for this type</returns>
        private static string? AliasCountUpdateFunctionForType(ITypeRef t)
        {
            if (t.IsPointer)
            {
                if (Types.IsTypedTuple(t))
                {
                    return RuntimeLibrary.TupleUpdateAliasCount;
                }
                else if (Types.IsArray(t))
                {
                    return RuntimeLibrary.ArrayUpdateAliasCount;
                }
                else if (Types.IsCallable(t))
                {
                    // We need to alias count callables to ensure that
                    // the alias counts for captured value are accurate.
                    return RuntimeLibrary.CallableUpdateAliasCount;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the name of the runtime function to update the reference count for a given LLVM type.
        /// </summary>
        /// <param name="t">The LLVM type</param>
        /// <returns>The name of the function to update the reference count for this type</returns>
        private static string? ReferenceCountUpdateFunctionForType(ITypeRef t)
        {
            if (t.IsPointer)
            {
                if (Types.IsTypedTuple(t))
                {
                    return RuntimeLibrary.TupleUpdateReferenceCount;
                }
                else if (Types.IsArray(t))
                {
                    return RuntimeLibrary.ArrayUpdateReferenceCount;
                }
                else if (Types.IsCallable(t))
                {
                    return RuntimeLibrary.CallableUpdateReferenceCount;
                }
                else if (Types.IsResult(t))
                {
                    return RuntimeLibrary.ResultUpdateReferenceCount;
                }
                else if (Types.IsString(t))
                {
                    return RuntimeLibrary.StringUpdateReferenceCount;
                }
                else if (Types.IsBigInt(t))
                {
                    return RuntimeLibrary.BigIntUpdateReferenceCount;
                }
            }

            return null;
        }

        private static bool IsStackAlloctedContainer(ITypeRef t) =>
            t.Kind == TypeKind.Array || t.Kind == TypeKind.Struct || t.Kind == TypeKind.Vector;

        /// <summary>
        /// For each value for which the given function returns a function name,
        /// applies the runtime function with that name to the value, casting the value if necessary.
        /// Recurs into contained items if the bool passed with the value is true.
        /// </summary>
        private void ModifyCounts(Func<ITypeRef, string?> getFunctionName, Value change, params (IValue, bool)[] values)
        {
            foreach (var (value, recur) in values)
            {
                this.ModifyCounts(getFunctionName, change, value, recur);
            }
        }

        /// <summary>
        /// If the given function returns a function name for the given value,
        /// applies the runtime function with that name to the given value, casting the value if necessary.
        /// Recurs into contained items if the bool passed with the value is true.
        /// </summary>
        private void ModifyCounts(Func<ITypeRef, string?> getFunctionName, Value change, IValue value, bool recurIntoInnerItems)
        {
            void ProcessValue(string? funcName, IValue value)
            {
                if (value is PointerValue pointer)
                {
                    ProcessValue(funcName, pointer.LoadValue());
                }
                else
                {
                    Func<Value> getArg;
                    if (value is TupleValue tuple)
                    {
                        for (var i = 0; recurIntoInnerItems && i < tuple.LlvmElementTypes.Count; ++i)
                        {
                            var itemFuncName = getFunctionName(tuple.LlvmElementTypes[i]);
                            if (itemFuncName != null || IsStackAlloctedContainer(tuple.LlvmElementTypes[i]))
                            {
                                var item = tuple.GetTupleElement(i);
                                ProcessValue(itemFuncName, item);
                            }
                        }

                        getArg = () => tuple.OpaquePointer;
                    }
                    else if (value is ArrayValue array)
                    {
                        var itemFuncName = getFunctionName(array.LlvmElementType);
                        if (recurIntoInnerItems && (itemFuncName != null || IsStackAlloctedContainer(array.LlvmElementType)))
                        {
                            this.sharedState.IterateThroughArray(array, arrItem => ProcessValue(itemFuncName, arrItem));
                        }

                        getArg = () => array.OpaquePointer;
                    }
                    else if (recurIntoInnerItems && value is CallableValue callable)
                    {
                        var captureCountChange =
                            funcName == RuntimeLibrary.CallableUpdateReferenceCount ? RuntimeLibrary.CaptureUpdateReferenceCount :
                            funcName == RuntimeLibrary.CallableUpdateAliasCount ? RuntimeLibrary.CaptureUpdateAliasCount :
                            throw new NotSupportedException("unknown function for capture tuple memory management");

                        var invokeMemoryManagment = this.sharedState.GetOrCreateRuntimeFunction(captureCountChange);
                        this.sharedState.CurrentBuilder.Call(invokeMemoryManagment, callable.Value, change);
                        getArg = () => callable.Value;
                    }
                    else
                    {
                        getArg = () => value.Value;
                    }

                    if (funcName is not null)
                    {
                        var func = this.sharedState.GetOrCreateRuntimeFunction(funcName);
                        this.sharedState.CurrentBuilder.Call(func, getArg(), change);
                    }
                }
            }

            var func = getFunctionName(value.LlvmType);
            if (func != null || IsStackAlloctedContainer(value.LlvmType))
            {
                ProcessValue(func, value);
            }
        }

        private void ExecutePendingCalls(bool keepCurrentScope = false)
        {
            var current = keepCurrentScope ? this.scopes.Peek() : this.scopes.Pop();
            if (current.HasPendingReferences && keepCurrentScope)
            {
                throw new InvalidOperationException("scope contains pending calls to increase reference counts");
            }

            current.ExecutePendingCalls();
        }

        // public and internal methods

        /// <summary>
        /// Opens a new scope and pushes it on top of the scope stack.
        /// IMPORTANT:
        /// This function is meant to be used only for opening Q# scopes, and *not* for blocks that have been inserted
        /// only as part of QIR generation such as e.g. for-loops to modify reference and alias counts.
        /// The reason is that for optimization purposes we omit increasing (and subsequently decreasing) counts when possible.
        /// Some of these optimizations rely on the restrictions enforced by the Q# language.
        /// </summary>
        public void OpenScope() =>
            this.scopes.Push(new Scope(
                increaseCounts: (fct, items) => this.ModifyCounts(fct, this.plusOne, items),
                decreaseCounts: (fct, items) => this.ModifyCounts(fct, this.minusOne, items)));

        /// <summary>
        /// Closes the current scope by popping it off of the stack.
        /// Emits the queued calls to unreference, release, and/or decrease the alias counts for values going out of scope.
        /// If the current basic block is already terminated, presumably by a return, the calls are not generated.
        /// IMPORTANT:
        /// This function is meant to be used only for closing Q# scopes, and *not* for blocks that have been inserted
        /// only as part of QIR generation such as e.g. for-loops to modify reference and alias counts.
        /// The reason is that for optimization purposes we omit increasing (and subsequently decreasing) counts when possible.
        /// Some of these optimizations rely on the restrictions enforced by the Q# language.
        /// </summary>
        /// <exception cref="InvalidOperationException">The scope has pending calls to increase the reference count for values</exception>
        public void CloseScope(bool isTerminated)
        {
            if (isTerminated)
            {
                // Note that it is perfectly possible that the scope has pending calls to increase reference counts;
                // This can happen when code at the end of this scope is unreachable and all execution paths terminate
                // in return or fail statements in inner scopes that have already been closed. In that case, the still
                // pending references in this scope should have been properly applied when closing the inner scope(s).
                this.scopes.Pop();
            }
            else
            {
                this.ExecutePendingCalls();
            }
        }

        /// <summary>
        /// Closes the current scope by popping it off of the stack.
        /// Emits the queued calls to unreference, release, and/or decrease the alias counts for values going out of scope.
        /// Increases the reference count of the returned value by 1, either by omitting to unreference it or by explicitly increasing it.
        /// IMPORTANT:
        /// This function is meant to be used only for closing Q# scopes, and *not* for blocks that have been inserted
        /// only as part of QIR generation such as e.g. for-loops to modify reference and alias counts.
        /// The reason is that for optimization purposes we omit increasing (and subsequently decreasing) counts when possible.
        /// Some of these optimizations rely on the restrictions enforced by the Q# language.
        /// </summary>
        public void CloseScope(IValue returned)
        {
            this.IncreaseReferenceCount(returned);
            this.ExecutePendingCalls();
        }

        /// <summary>
        /// Exits the current scope by emitting the calls to unreference, release,
        /// and/or decrease the alias counts for values going out of scope, and invoking release functions if necessary.
        /// Exiting the current scope does *not* close the scope.
        /// All pending calls to increase reference counts for values need to be applied
        /// using <see cref="ApplyPendingReferences"/> before exiting the scope.
        /// IMPORTANT:
        /// This function is meant to be used only for exiting Q# scopes, and *not* for blocks that have been inserted
        /// only as part of QIR generation such as e.g. for-loops to modify reference and alias counts.
        /// The reason is that for optimization purposes we omit increasing (and subsequently decreasing) counts when possible.
        /// Some of these optimizations rely on the restrictions enforced by the Q# language.
        /// </summary>
        /// <exception cref="InvalidOperationException">The scope has pending calls to increase the reference count for values</exception>
        public void ExitScope() =>
            this.ExecutePendingCalls(keepCurrentScope: true);

        /// <summary>
        /// Executes all pending calls to increase reference counts in the current scope.
        /// </summary>
        internal void ApplyPendingReferences() =>
            this.scopes.Peek().ApplyPendingReferences();

        /// <returns>True if reference counts are tracked for values of the given type.</returns>
        internal static bool RequiresReferenceCount(ITypeRef type) =>
            ReferenceCountUpdateFunctionForType(type) != null;

        /// <summary>
        /// Adds a call to a runtime library function to increase the reference count for the given value if necessary.
        /// The reference count is increased recursively for subitems unless shallow is set to true.
        /// </summary>
        /// <param name="value">The value which is referenced</param>
        public void IncreaseReferenceCount(IValue value, bool shallow = false) =>
            this.scopes.Peek().ReferenceValue(value, !shallow);

        /// <summary>
        /// Adds a call to a runtime library function to decrease the reference count for the given value if necessary.
        /// The reference count is decreased recursively for subitems unless shallow is set to true.
        /// </summary>
        /// <param name="value">The value which is unreferenced</param>
        public void DecreaseReferenceCount(IValue value, bool shallow = false) =>
            this.scopes.Peek().UnreferenceValue(value, !shallow);

        /// <summary>
        /// Given a callable value, increases the reference count of its capture tuple by 1.
        /// </summary>
        /// <param name="callable">The callable whose capture tuple to reference</param>
        internal void ReferenceCaptureTuple(CallableValue callable)
        {
            var updateRefCount = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CaptureUpdateReferenceCount);
            this.sharedState.CurrentBuilder.Call(updateRefCount, callable.Value, this.plusOne);
        }

        /// <summary>
        /// Adds a call to a runtime library function to change the reference count for the given value.
        /// The count is changed recursively for subitems unless shallow is set to true.
        /// </summary>
        /// <param name="change">The amount by which to change the reference count given as i64</param>
        /// <param name="value">The value for which to change the reference count</param>
        internal void UpdateReferenceCount(Value change, IValue value, bool shallow = false)
        {
            this.scopes.Peek().ApplyPendingReferences();
            this.ModifyCounts(ReferenceCountUpdateFunctionForType, change, value, !shallow);
        }

        /// <summary>
        /// Adds a call to a runtime library function to change the alias count for the given value.
        /// The alias count is changed recursively for subitems unless shallow is set to true.
        /// Modifies *only* the alias count and not the reference count.
        /// </summary>
        /// <param name="change">The amount by which to change the alias count given as i64</param>
        /// <param name="value">The value for which to change the alias count</param>
        internal void UpdateAliasCount(Value change, IValue value, bool shallow = false) =>
            this.ModifyCounts(AliasCountUpdateFunctionForType, change, value, !shallow);

        /// <summary>
        /// <inheritdoc cref="Scope.AssignToMutable(bool, IValue, bool)" />
        /// Counts changes are applied recursively for subitems unless <paramref name="shallow"/> is set to true.
        /// </summary>
        /// <param name="value">The value assigned to a mutable variable.</param>
        /// <param name="fromLocalId">
        /// Name of the local variable via which the assigned value is accessed
        /// (can be obtained by querying <see cref="QirExpressionKindTransformation.AccessViaLocalId(SyntaxTree.TypedExpression, out string)"/>.)
        /// </param>
        internal void AssignToMutable(IValue value, string? fromLocalId, bool shallow = false) =>
            this.scopes.Peek().AssignToMutable(fromLocalId != null, value, shallow: shallow);

        /// <summary>
        /// <inheritdoc cref="Scope.UnassignFromMutable(IValue, bool)" />
        /// Counts changes are applied recursively for subitems unless <paramref name="shallow"/> is set to true.
        /// </summary>
        /// <param name="value">The value unassigned from a mutable variable.</param>
        internal void UnassignFromMutable(IValue value, bool shallow = false) =>
            this.scopes.Peek().UnassignFromMutable(value, shallow: shallow);

        /// <summary>
        /// Registers a variable name as an alias for an LLVM value.
        /// Increases the alias and reference count for the value if necessary,
        /// depending on whether the value is accessed via a mutable variable or assigned to a mutable variable.
        /// Ensures that both are decreased again when the variable goes out of scope.
        /// The registered variable is mutable if the passed value is a pointer value.
        /// </summary>
        /// <param name="name">The name to register.</param>
        /// <param name="value">The LLVM value.</param>
        /// <param name="fromLocalId">
        /// Name of the local variable via which the assigned value is accessed
        /// (can be obtained by querying <see cref="QirExpressionKindTransformation.AccessViaLocalId(SyntaxTree.TypedExpression, out string)"/>.)
        /// </param>
        internal void RegisterVariable(string name, IValue value, string? fromLocalId)
        {
            IValue? localId = null;
            if (fromLocalId != null)
            {
                this.scopes.FirstOrDefault(scope => scope.TryGetVariable(fromLocalId, out localId));
            }

            value.RegisterName(this.sharedState.VariableName(name));
            this.scopes.Peek().RegisterVariable(name, value, fromLocalId: localId);
        }

        /// <summary>
        /// Registers the given value with the current scope, such that a call to a suitable runtime library function
        /// that unreferences the value is executed when the scope is closed or exited.
        /// </summary>
        /// <param name="value">Value that is created within the current scope</param>
        public void RegisterValue(IValue value, bool shallow = false) =>
            this.scopes.Peek().RegisterValue(value, shallow: shallow);

        /// <summary>
        /// Adds a value constructed as part of a qubit allocation to the current topmost scope.
        /// Makes sure that all allocated qubits are released when the scope is closed or exited
        /// and that the value and all its items are unreferenced.
        /// </summary>
        public void RegisterAllocatedQubits(IValue value)
        {
            var releaseFunctionName =
                Types.IsArray(value.LlvmType) ? RuntimeLibrary.QubitReleaseArray :
                Types.IsQubit(this.sharedState.Types.Qubit) ? RuntimeLibrary.QubitRelease :
                throw new ArgumentException("AddQubitValue expects an argument of type Qubit or Qubit[]");
            var release = this.sharedState.GetOrCreateRuntimeFunction(releaseFunctionName);
            this.scopes.Peek().RegisterRelease(value, loaded => this.sharedState.CurrentBuilder.Call(release, loaded.Value));
        }

        /// <summary>
        /// Gets the value of a named variable.
        /// The name must have been registered as an alias for the value using
        /// <see cref="RegisterVariable(string, IValue, string?)"/>.
        /// </summary>
        /// <param name="name">The registered variable name to look for</param>
        internal IValue GetVariable(string name)
        {
            IValue? value = null;
            this.scopes.FirstOrDefault(scope => scope.TryGetVariable(name, out value));
            return value ?? throw new KeyNotFoundException($"Could not find a Value for local symbol {name}");
        }

        /// <summary>
        /// Exits the current function by applying all pending calls to change reference counts for values in all open scopes,
        /// decreasing alias counts and invoking release functions if necessary.
        /// Increases the reference count of the returned value by 1, either by omitting to unreference it or by explicitly increasing it.
        /// The calls are generated using the current builder.
        /// Exiting the current function does *not* close the scopes.
        /// </summary>
        /// <param name="returned">The value that is returned and expected to remain valid after exiting.</param>
        /// <exception cref="InvalidOperationException">The current function is inlined.</exception>
        public void ExitFunction(IValue returned)
        {
            if (this.sharedState.IsInlined)
            {
                throw new InvalidOperationException("cannot exit inlined function");
            }

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
            this.IncreaseReferenceCount(returned);
            this.scopes.Peek().ExecutePendingCalls(this.scopes.Skip(1).ToArray());
        }
    }
}
