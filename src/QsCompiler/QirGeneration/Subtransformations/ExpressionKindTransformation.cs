// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using ResolvedExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal class QirExpressionKindTransformation : ExpressionKindTransformation<GenerationContext>
    {
        /* inner classes */

        private abstract class PartialApplicationArgument
        {
            protected GenerationContext SharedState { get; }

            public PartialApplicationArgument(GenerationContext sharedState)
            {
                this.SharedState = sharedState;
            }

            public abstract IValue BuildItem(TupleValue capture, IValue parArgs);
        }

        private class InnerCapture : PartialApplicationArgument
        {
            public int CaptureIndex { get; }

            public InnerCapture(GenerationContext sharedState, int captureIndex)
            : base(sharedState)
            {
                this.CaptureIndex = captureIndex;
            }

            /// <summary>
            /// The given capture is expected to be fully typed.
            /// The parArgs parameter is unused.
            /// </summary>
            public override IValue BuildItem(TupleValue capture, IValue parArgs) =>
                capture.GetTupleElement(this.CaptureIndex);
        }

        private class InnerArg : PartialApplicationArgument
        {
            public ITypeRef ItemType { get; }

            public int ArgIndex { get; }

            public InnerArg(GenerationContext sharedState, ITypeRef itemType, int argIndex)
            : base(sharedState)
            {
                this.ItemType = itemType;
                this.ArgIndex = argIndex;
            }

            /// <summary>
            /// The given parameter parArgs is expected to contain an argument to a partial application, and is expected to be fully typed.
            /// The given capture is unused.
            /// </summary>
            public override IValue BuildItem(TupleValue capture, IValue paArgs) =>

                // parArgs.NativeType == this.ItemType may occur if we have an item of user defined type (represented as a tuple)
                (paArgs is TupleValue paArgsTuple) && paArgsTuple.StructType.CreatePointerType() != this.ItemType
                    ? paArgsTuple.GetTupleElement(this.ArgIndex)
                    : paArgs;
        }

        private class InnerTuple : PartialApplicationArgument
        {
            public ResolvedType TupleType { get; }

            public ImmutableArray<PartialApplicationArgument> Items { get; }

            public InnerTuple(GenerationContext sharedState, ResolvedType tupleType, IEnumerable<PartialApplicationArgument> items)
            : base(sharedState)
            {
                this.TupleType = tupleType;
                this.Items = items?.ToImmutableArray() ?? ImmutableArray<PartialApplicationArgument>.Empty;
            }

            /// <summary>
            /// The given capture is expected to be fully typed.
            /// The given parameter parArgs is expected to contain an argument to a partial application, and is expected to be fully typed.
            /// </summary>
            /// <returns>A fully typed tuple that combines the captured values as well as the arguments to the partial application</returns>
            public override IValue BuildItem(TupleValue capture, IValue parArgs)
            {
                var items = this.Items.Select(item => item.BuildItem(capture, parArgs)).ToArray();
                var tuple = this.SharedState.Values.CreateTuple(items);
                return tuple;
            }
        }

        /* constructors */

        public QirExpressionKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation)
            : base(parentTransformation)
        {
        }

        public QirExpressionKindTransformation(GenerationContext sharedState)
            : base(sharedState)
        {
        }

        public QirExpressionKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options)
            : base(parentTransformation, options)
        {
        }

        public QirExpressionKindTransformation(GenerationContext sharedState, TransformationOptions options)
            : base(sharedState, options)
        {
        }

        /* static methods */

        /// <returns>
        /// True if the expression is self-evaluating and
        /// doesn't require encapsulating into its own block if it should only be evaluated conditionally.
        /// </returns>
        private bool IsSelfEvaluating(TypedExpression ex) =>
        /*  Do *not* return true for anything that needs to be reference counted,
            since otherwise reference counts may not be tracked accurately. */
            (ex.Expression.IsIdentifier
                || ex.Expression.IsBoolLiteral
                || ex.Expression.IsDoubleLiteral || ex.Expression.IsIntLiteral
                || ex.Expression.IsPauliLiteral || ex.Expression.IsRangeLiteral
                || ex.Expression.IsUnitValue)
            && !ScopeManager.RequiresReferenceCount(this.SharedState.LlvmTypeFromQsharpType(ex.ResolvedType));

        /// <returns>
        /// True if the value of the given expression is accessed via a local variable
        /// (whether directly via the identifier or e.g. as part of an item access expression),
        /// as well as the name of that variable as out parameter.
        /// <br/>
        /// The reference count of values accessed via local variables needs to be increased
        /// upon assignment to or from a mutable variable.
        /// </returns>
        internal static bool AccessViaLocalId(TypedExpression ex, [MaybeNullWhen(false)] out string identifierName)
        {
            if (ex.Expression is ResolvedExpressionKind.Identifier id)
            {
                identifierName = id.Item1 is Identifier.LocalVariable var ? var.Item : null;
                return identifierName != null;
            }
            else if (ex.Expression is ResolvedExpressionKind.ArrayItem arrayItem)
            {
                return AccessViaLocalId(arrayItem.Item1, out identifierName);
            }
            else if (ex.Expression is ResolvedExpressionKind.NamedItem namedItem)
            {
                return AccessViaLocalId(namedItem.Item1, out identifierName);
            }
            else if (ex.Expression is ResolvedExpressionKind.UnwrapApplication unwrap)
            {
                return AccessViaLocalId(unwrap.Item, out identifierName);
            }
            else
            {
                // Note that the reference count for conditional expressions is already increased by 1
                // unless both branches are self-evaluating (i.e. no ref count change is needed).
                identifierName = null;
                return false;
            }
        }

        /// <summary>
        /// Determines the location of the item with the given name within the tuple of type items.
        /// The returned list contains the index of the item starting from the outermost tuple
        /// as well as the type of the subtuple or item at that location.
        /// </summary>
        /// <param name="name">The name if the item to find the location for in the tuple of type items</param>
        /// <param name="typeItems">The tuple defining the items of a custom type</param>
        /// <param name="itemLocation">The location of the item with the given name within the item tuple</param>
        /// <returns>Returns true if the item was found and false otherwise</returns>
        private static bool FindNamedItem(string name, QsTuple<QsTypeItem> typeItems, out List<int> itemLocation)
        {
            bool FindNamedItem(QsTuple<QsTypeItem> items, List<int> location)
            {
                switch (items)
                {
                    case QsTuple<QsTypeItem>.QsTupleItem leaf:
                        if ((leaf.Item is QsTypeItem.Named n) && (n.Item.VariableName == name))
                        {
                            return true;
                        }

                        break;
                    case QsTuple<QsTypeItem>.QsTuple list:
                        for (int i = 0; i < list.Item.Length; i++)
                        {
                            if (FindNamedItem(list.Item[i], location))
                            {
                                location.Add(i);
                                return true;
                            }
                        }

                        break;
                }

                return false;
            }

            itemLocation = new List<int>();
            var found = FindNamedItem(typeItems, itemLocation);
            itemLocation.Reverse();
            return found;
        }

        /// <summary>
        /// Evaluates the copy-and-update expression defined by the given left-hand side, the item access and the right-hand side
        /// within the given context.
        /// If updateItemAliasCount is set to true, decreases the alias count of the items that are updated by 1 and
        /// increases the alias count for the new items.
        /// If unreferenceOriginal original is set to true, then the original value is effectively unreferenced by omitting to
        /// to increase the reference counts for items that are not updated, and decreasing the reference counts for items that
        /// are replaced.
        /// </summary>
        /// <param name="sharedState">The context within which to evaluate the expression</param>
        /// <param name="copyAndUpdate">
        /// Tuple containing the original value which should be copied and updated,
        /// an alias expression indicating the item(s) to update,
        /// and the new value(s) for the item(s) to update.
        /// </param>
        internal static ResolvedExpressionKind CopyAndUpdate(
            GenerationContext sharedState,
            ((string?, IValue), TypedExpression, TypedExpression) copyAndUpdate,
            bool updateItemAliasCount = false,
            bool unreferenceOriginal = false)
        {
            if (updateItemAliasCount && unreferenceOriginal)
            {
                throw new InvalidOperationException("cannot unreference original upon assignment of a copy-and-update expression to a variable");
            }

            var (originalValue, accEx, updated) = copyAndUpdate;
            AccessViaLocalId(updated, out var fromLocalId);

            void StoreElement(PointerValue pointer, IValue value, string? fromLocalId, bool shallow = false)
            {
                if (updateItemAliasCount)
                {
                    sharedState.ScopeMgr.AssignToMutable(value, fromLocalId: fromLocalId, shallow: shallow);
                    sharedState.ScopeMgr.UnassignFromMutable(pointer, shallow: shallow);
                }

                pointer.StoreValue(value);
            }

            IValue CopyAndUpdateArray(ArrayValue originalArray)
            {
                ArrayValue GetArrayCopy(bool needsToBeCopied)
                {
                    // Since we keep track of alias counts for arrays we always ask the runtime to create a shallow copy
                    // if needed. The runtime function ArrayCopy creates a new value with reference count 1 if the current
                    // alias count is larger than 0, and otherwise merely increases the reference count of the array by 1.
                    var createShallowCopy = sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayCopy);
                    var forceCopy = sharedState.Context.CreateConstant(needsToBeCopied);
                    var copy = sharedState.CurrentBuilder.Call(createShallowCopy, originalArray.OpaquePointer, forceCopy);
                    return sharedState.Values.FromArray(copy, originalArray.QSharpElementType);
                }

                void UpdateElement(Func<IValue> getNewItemForIndex, PointerValue itemToUpdate)
                {
                    var newElement = getNewItemForIndex();

                    if (unreferenceOriginal)
                    {
                        sharedState.ScopeMgr.IncreaseReferenceCount(newElement);
                        sharedState.ScopeMgr.DecreaseReferenceCount(itemToUpdate);
                    }

                    StoreElement(itemToUpdate, newElement, fromLocalId);
                }

                ArrayValue array;
                if (accEx.ResolvedType.Resolution.IsInt)
                {
                    // Even if the rhs contains an access to the same array as the lhs that is updated,
                    // and the update is done in place, we don't need to force the copy since we will never
                    // load more than a single item, i.e. the update happens only after the rhs value is evaluated.
                    array = GetArrayCopy(false);

                    // do not increase the ref count here - we will increase the ref count of all new items at the end
                    IValue newItemValue = sharedState.EvaluateSubexpression(updated);
                    var index = sharedState.EvaluateSubexpression(accEx);
                    UpdateElement(() => newItemValue, array.GetArrayElementPointer(index.Value));
                }
                else if (accEx.ResolvedType.Resolution.IsRange)
                {
                    // In the case where we update a range of values in the original array, we need to be more careful
                    // when the values on the rhs are subitems of the array that is updated. In this case, we need to
                    // ensure that we can still load the original values even after updates have already been performed.
                    // We hence for the (shallow) copy of the array in that case.
                    var updateFromSelf = originalValue.Item1 != null && originalValue.Item1 == fromLocalId;
                    array = GetArrayCopy(updateFromSelf);

                    // do not increase the ref count here - we will increase the ref count of all new items at the end
                    var newItemValues = (ArrayValue)sharedState.EvaluateSubexpression(updated);
                    var (getStart, getStep, getEnd) = sharedState.Functions.RangeItems(accEx);
                    sharedState.IterateThroughArray(newItemValues, getStart(), (newItem, targetIdx) =>
                    {
                        sharedState.ScopeMgr.OpenScope();
                        var elementPtr = array.GetArrayElementPointer(targetIdx!);
                        if (updateFromSelf)
                        {
                            // We need to make sure that the old value is not unreferenced before all values
                            // have been updated, since a subsequent iteration may still need to access it.
                            // The old values are instead unreferenced in a separate loop at the end.
                            sharedState.ScopeMgr.IncreaseReferenceCount(elementPtr);
                        }

                        UpdateElement(() => newItem, elementPtr);
                        var step = getStep() ?? sharedState.Context.CreateConstant(1L);
                        var nextIdx = sharedState.CurrentBuilder.Add(targetIdx!, step);
                        var isTerminated = sharedState.CurrentBlock?.Terminator != null;
                        sharedState.ScopeMgr.CloseScope(isTerminated);
                        return nextIdx;
                    });

                    if (updateFromSelf)
                    {
                        // separate loop after we have performed all updates to unreferenced the old values
                        sharedState.IterateThroughRange(getStart(), getStep(), getEnd(), targetIdx =>
                        {
                            sharedState.ScopeMgr.OpenScope();
                            sharedState.ScopeMgr.DecreaseReferenceCount(originalArray.GetArrayElement(targetIdx));
                            var isTerminated = sharedState.CurrentBlock?.Terminator != null;
                            sharedState.ScopeMgr.CloseScope(isTerminated);
                        });
                    }
                }
                else
                {
                    throw new InvalidOperationException("invalid item name in named item access");
                }

                if (!unreferenceOriginal)
                {
                    // In order to accurately reflect which items are still in use and thus need to remain allocated,
                    // reference counts always need to be modified recursively. However, while the reference count for
                    // the value returned by ArrayCopy is set to 1 or increased by 1, it is not possible for the runtime
                    // to increase the reference count of the contained items due to lacking type information.
                    // In the same way that we increase the reference count when we populate an array, we hence need to
                    // manually (recursively) increase the reference counts for all items.
                    sharedState.ScopeMgr.IncreaseReferenceCount(array);
                    sharedState.ScopeMgr.DecreaseReferenceCount(array, shallow: true);
                }
                else
                {
                    // We effectively decrease the reference count for the unmodified array items by not increasing it
                    // to reflect their use in the copy, and we have manually decreased the reference count for the updated item(s).
                    // What's left to do is to unreference the original array itself.
                    sharedState.ScopeMgr.DecreaseReferenceCount(originalArray, shallow: true);
                }

                sharedState.ScopeMgr.RegisterValue(array);
                return array;
            }

            IValue CopyAndUpdateUdt(TupleValue originalTuple)
            {
                TupleValue GetTupleCopy(TupleValue original)
                {
                    // Since we keep track of alias counts for tuples we always ask the runtime to create a shallow copy
                    // if needed. The runtime function TupleCopy creates a new value with reference count 1 if the current
                    // alias count is larger than 0, and otherwise merely increases the reference count of the tuple by 1.
                    var createShallowCopy = sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCopy);
                    var forceCopy = sharedState.Context.CreateConstant(false);
                    var copy = sharedState.CurrentBuilder.Call(createShallowCopy, original.OpaquePointer, forceCopy);
                    return original.TypeName == null
                        ? sharedState.Values.FromTuple(copy, original.ElementTypes)
                        : sharedState.Values.FromCustomType(copy, new UserDefinedType(original.TypeName.Namespace, original.TypeName.Name, QsNullable<DataTypes.Range>.Null));
                }

                var udtName = originalTuple.TypeName;
                if (udtName == null || !sharedState.TryGetCustomType(udtName, out QsCustomType? udtDecl))
                {
                    throw new InvalidOperationException("Q# declaration for type not found");
                }
                else if (accEx.Expression is ResolvedExpressionKind.Identifier id
                    && id.Item1 is Identifier.LocalVariable name
                    && FindNamedItem(name.Item, udtDecl.TypeItems, out var location))
                {
                    var originalTuples = new Stack<TupleValue>();
                    originalTuples.Push(originalTuple);

                    var copies = new Stack<TupleValue>();
                    var value = GetTupleCopy(originalTuple);
                    copies.Push(value);

                    for (int depth = 0; depth < location.Count; depth++)
                    {
                        var itemPointer = copies.Peek().GetTupleElementPointer(location[depth]);
                        if (depth == location.Count - 1)
                        {
                            // needs to be before the ref count decrease in case it accesses the old value
                            var newItemValue = sharedState.EvaluateSubexpression(updated);

                            if (unreferenceOriginal)
                            {
                                sharedState.ScopeMgr.IncreaseReferenceCount(newItemValue);
                                sharedState.ScopeMgr.DecreaseReferenceCount(itemPointer);
                            }

                            StoreElement(itemPointer, newItemValue, fromLocalId);
                        }
                        else
                        {
                            // We load the original item at that location (which is an inner tuple),
                            // and replace it with a copy of it (if a copy is needed),
                            // such that we can then proceed to modify that copy (the next inner tuple).
                            var originalItem = (TupleValue)itemPointer.LoadValue();
                            originalTuples.Push(originalItem);
                            var copy = GetTupleCopy(originalItem);
                            copies.Push(copy);
                            StoreElement(itemPointer, copy, null, shallow: true);
                        }
                    }

                    if (!unreferenceOriginal)
                    {
                        // In order to accurately reflect which items are still in use and thus need to remain allocated,
                        // reference counts always need to be modified recursively. However, while the reference count for
                        // the value returned by TupleCopy is set to 1 or increased by 1, it is not possible for the runtime
                        // to increase the reference count of the contained items due to lacking type information.
                        // In the same way that we increase the reference count when we populate a tuple, we hence need to
                        // manually (recursively) increase the reference counts for all items.
                        sharedState.ScopeMgr.IncreaseReferenceCount(value);
                        while (copies.TryPop(out var copy))
                        {
                            sharedState.ScopeMgr.DecreaseReferenceCount(copy, shallow: true);
                        }
                    }
                    else
                    {
                        // We effectively decrease the reference count for the unmodified tuple items by not increasing it
                        // to reflect their use in the copy, and we have manually decreased the reference count for the updated item(s).
                        // What's left to do is to unreference the original tuple(s) that has/have been replaced.
                        while (originalTuples.TryPop(out var original))
                        {
                            sharedState.ScopeMgr.DecreaseReferenceCount(original, shallow: true);
                        }
                    }

                    sharedState.ScopeMgr.RegisterValue(value);
                    return value;
                }
                else
                {
                    throw new InvalidOperationException("invalid item name in named item access");
                }
            }

            IValue value;
            if (originalValue.Item2 is ArrayValue originalArray)
            {
                value = CopyAndUpdateArray(originalArray);
            }
            else if (originalValue.Item2 is TupleValue originalTuple && originalTuple.TypeName != null)
            {
                value = CopyAndUpdateUdt(originalTuple);
            }
            else
            {
                throw new NotSupportedException("invalid type for copy-and-update expression");
            }

            sharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        /// <summary>
        /// Creates a string literal that contains the given values as interpolation arguments.
        /// The given string is expected to consist of text interspaced with integer constants between curly brackets,
        /// e.g. "Hello, {0}!". The content between non-escaped curly brackets is parsed as integer,
        /// and replaced with the string representation of the interpolation argument at that index.
        /// Registers the built string with the scope manager.
        /// </summary>
        internal static IValue CreateStringLiteral(GenerationContext sharedState, string str, params IValue[] interpolArgs)
        {
            static (int, int, int) FindNextExpression(string s, int start)
            {
                while (true)
                {
                    var i = s.IndexOf('{', start);
                    if (i == -1)
                    {
                        return (-1, s.Length, -1);
                    }

                    // if the number of backslashes before the '{' is even, then the '{' is not escapted
                    else if (((i - start) - s.Substring(start, i - start).TrimEnd('\\').Length) % 2 == 0)
                    {
                        var j = s.IndexOf('}', i + 1);
                        if (j == -1)
                        {
                            throw new FormatException("Missing } in interpolated string");
                        }

                        var n = int.Parse(s[(i + 1)..j]);
                        return (i, j + 1, n);
                    }

                    start = i + 1;
                }
            }

            IValue CreateStringValue(Value str) =>
                sharedState.Values.From(str, ResolvedType.New(ResolvedTypeKind.String));

            // Creates a string value that needs to be queued for unreferencing.
            Value CreateConstantString(string s)
            {
                // Deal with escape sequences: \{, \\, \n, \r, \t, \". This is not an efficient
                // way to do this, but it's simple and clear, and strings are uncommon in Q#.
                var cleanStr = s.Replace("\\{", "{").Replace("\\\\", "\\").Replace("\\n", "\n")
                    .Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"");

                var sizedDataArrayPtr = sharedState.CurrentBuilder.GetElementPtr(
                    sharedState.Context.Int8Type.CreateArrayType((uint)cleanStr.Length + 1), // +1 because zero terminated
                    sharedState.GetOrCreateStringConstant(cleanStr),
                    new[] { sharedState.Context.CreateConstant(0) });
                var dataArrayPtr = sharedState.CurrentBuilder.BitCast(
                        sizedDataArrayPtr,
                        sharedState.Types.DataArrayPointer);

                var createString = sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringCreate);
                return sharedState.CurrentBuilder.Call(createString, dataArrayPtr);
            }

            // Creates a new string with reference count 1 that needs to be queued for unreferencing
            // and contains the concatenation of both values. Both arguments are unreferenced,
            // unless unreferenceNext and/or unreferenceCurrent are set to false.
            Value DoAppend(Value? current, Value next, bool unreferenceCurrent = true, bool unreferenceNext = true)
            {
                var refCountUpdate = sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringUpdateReferenceCount);
                var plusOne = sharedState.Context.CreateConstant(1);
                var minusOne = sharedState.Context.CreateConstant(-1);

                if (current == null)
                {
                    if (!unreferenceNext)
                    {
                        // Since we return next instead of a new string,
                        // we need to increase the reference count of next unless unreferenceNext is true.
                        sharedState.CurrentBuilder.Call(refCountUpdate, next, plusOne);
                    }

                    return next;
                }

                // The runtime function StringConcatenate creates a new value with reference count 1.
                var concatenate = sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringConcatenate);
                var app = sharedState.CurrentBuilder.Call(concatenate, current, next);

                if (unreferenceCurrent)
                {
                    sharedState.CurrentBuilder.Call(refCountUpdate, current, minusOne);
                }

                if (unreferenceNext)
                {
                    sharedState.CurrentBuilder.Call(refCountUpdate, next, minusOne);
                }

                return app;
            }

            // Creates a string value that needs to be queued for unreferencing.
            Value ExpressionToString(IValue evaluated)
            {
                void UpdateStringRefCount(Value str, int change)
                {
                    var addReference = sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringUpdateReferenceCount);
                    var countChange = sharedState.Context.CreateConstant(change);
                    sharedState.CurrentBuilder.Call(addReference, str, countChange);
                }

                // Creates a string value that needs to be queued for unreferencing.
                Value SimpleToString(string rtFuncName)
                {
                    var createString = sharedState.GetOrCreateRuntimeFunction(rtFuncName);
                    return sharedState.CurrentBuilder.Call(createString, evaluated.Value);
                }

                // Creates a string value that needs to be queued for unreferencing.
                Value TupleToString(TupleValue tuple)
                {
                    var str = CreateConstantString($"{tuple.TypeName}(");
                    var tupleElements = tuple.GetTupleElements();
                    Value? comma = null;

                    for (var idx = 0; idx < tupleElements.Length; ++idx)
                    {
                        if (idx > 0)
                        {
                            comma ??= CreateConstantString(", ");
                            str = DoAppend(str, comma!, unreferenceNext: false);
                        }

                        str = DoAppend(str, ExpressionToString(tupleElements[idx]));
                    }

                    str = DoAppend(str, CreateConstantString(")"));
                    if (comma != null)
                    {
                        UpdateStringRefCount(comma, -1);
                    }

                    return str;
                }

                // Creates a string value that needs to be queued for unreferencing.
                Value ArrayToString(ArrayValue array)
                {
                    var comma = CreateConstantString(", ");
                    var openParens = CreateConstantString("[");
                    UpdateStringRefCount(openParens, 1); // added to avoid dangling pointer in comparison inside loop
                    var outputStr = sharedState.IterateThroughArray(array, openParens, (item, str) =>
                    {
                        var cond = sharedState.CurrentBuilder.Compare(IntPredicate.NotEqual, str!, openParens);
                        var updatedStr = sharedState.ConditionalEvaluation(
                            cond,
                            onCondTrue: () => CreateStringValue(DoAppend(str!, comma, unreferenceNext: false)),
                            defaultValueForCondFalse: CreateStringValue(str!),
                            increaseReferenceCount: false);
                        return DoAppend(updatedStr, ExpressionToString(item));
                    });

                    outputStr = DoAppend(outputStr, CreateConstantString("]"));
                    UpdateStringRefCount(comma, -1);
                    UpdateStringRefCount(openParens, -1);
                    return outputStr;
                }

                var ty = evaluated.QSharpType.Resolution;
                if (ty.IsString)
                {
                    var quote = CreateConstantString("\"");
                    var stringValue = DoAppend(quote, evaluated.Value, unreferenceCurrent: false, unreferenceNext: false);
                    return DoAppend(stringValue, quote);
                }
                else if (ty.IsBigInt)
                {
                    return SimpleToString(RuntimeLibrary.BigIntToString);
                }
                else if (ty.IsBool)
                {
                    return sharedState.ConditionalEvaluation(
                        evaluated.Value,
                        onCondTrue: () => CreateStringValue(CreateConstantString("true")),
                        onCondFalse: () => CreateStringValue(CreateConstantString("false")),
                        increaseReferenceCount: false); // CreateConstantString already increases the reference count
                }
                else if (ty.IsInt)
                {
                    return SimpleToString(RuntimeLibrary.IntToString);
                }
                else if (ty.IsResult)
                {
                    return SimpleToString(RuntimeLibrary.ResultToString);
                }
                else if (ty.IsPauli)
                {
                    Value LoadPauli(QsPauli pauli)
                    {
                        sharedState.ExpressionTypeStack.Push(ResolvedType.New(ResolvedTypeKind.Pauli));
                        sharedState.Transformation.ExpressionKinds.OnPauliLiteral(pauli);
                        sharedState.ExpressionTypeStack.Pop();
                        return sharedState.ValueStack.Pop().Value;
                    }

                    Value CompareValueEquals(QsPauli pauli) =>
                        sharedState.CurrentBuilder.Compare(IntPredicate.Equal, LoadPauli(pauli), evaluated.Value);

                    IValue EvaluateEqualityComparison(QsPauli pauli, string pauliStr, Func<IValue> continuation) =>
                        CreateStringValue(
                            sharedState.ConditionalEvaluation(
                                CompareValueEquals(pauli),
                                onCondTrue: () => CreateStringValue(CreateConstantString(pauliStr)),
                                onCondFalse: continuation,
                                increaseReferenceCount: false)); // CreateConstantString already increases the reference count

                    return EvaluateEqualityComparison(QsPauli.PauliX, "PauliX", () =>
                           EvaluateEqualityComparison(QsPauli.PauliY, "PauliY", () =>
                           EvaluateEqualityComparison(QsPauli.PauliZ, "PauliZ", () =>
                           CreateStringValue(CreateConstantString("PauliI"))))).Value;
                }
                else if (ty.IsQubit)
                {
                    return SimpleToString(RuntimeLibrary.QubitToString);
                }
                else if (ty.IsRange)
                {
                    return SimpleToString(RuntimeLibrary.RangeToString);
                }
                else if (ty.IsDouble)
                {
                    return SimpleToString(RuntimeLibrary.DoubleToString);
                }
                else if (ty.IsFunction)
                {
                    return CreateConstantString("<function>");
                }
                else if (ty.IsOperation)
                {
                    return CreateConstantString("<operation>");
                }
                else if (ty.IsUnitType)
                {
                    return CreateConstantString("()");
                }
                else if (ty.IsArrayType)
                {
                    var array = (ArrayValue)evaluated;
                    return ArrayToString(array);
                }
                else if (ty.IsTupleType || ty.IsUserDefinedType)
                {
                    var tuple = (TupleValue)evaluated;
                    return TupleToString(tuple);
                }
                else
                {
                    throw new NotSupportedException("unkown type for expression in conversion to string");
                }
            }

            Value? current = null;
            if (interpolArgs.Length > 0)
            {
                // Compiled interpolated strings look like <text>{<int>}<text>...
                // Our basic pattern is to scan for the next '{', append the intervening text if any
                // as a constant string, scan for the closing '}', parse out the integer in between,
                // evaluate the corresponding expression, append it, and keep going.
                // We do have to be a little careful because we can't just look for '{', we have to
                // make sure we skip escaped braces -- "\{".
                var offset = 0;
                while (offset < str.Length)
                {
                    var (end, next, index) = FindNextExpression(str, offset);
                    if (end < 0)
                    {
                        var last = CreateConstantString(str[offset..]);
                        current = DoAppend(current, last);
                        break;
                    }
                    else
                    {
                        if (end > offset)
                        {
                            var last = CreateConstantString(str[offset..end]);
                            current = DoAppend(current, last);
                        }

                        if (index >= 0)
                        {
                            var exString = ExpressionToString(interpolArgs[index]);
                            current = DoAppend(current, exString);
                        }

                        offset = next;
                    }
                }
            }

            current ??= CreateConstantString(str);
            var value = CreateStringValue(current);
            sharedState.ScopeMgr.RegisterValue(value);
            return value;
        }

        // private helpers

        /// <summary>
        /// Handles calls to specific functor specializations of global callables.
        /// Directly invokes the corresponding target instruction If a target instruction name is associated the callable.
        /// Inlines the corresponding function if the callable is marked as to be inlined.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no callable with the given name exists, or the corresponding specialization cannot be found.
        /// </exception>
        private IValue InvokeGlobalCallable(QsQualifiedName callableName, QsSpecializationKind kind, TypedExpression arg)
        {
            IValue CallGlobal(IrFunction func, TypedExpression arg, ResolvedType returnType)
            {
                IEnumerable<IValue> args;
                if (arg.ResolvedType.Resolution.IsUnitType)
                {
                    args = Enumerable.Empty<IValue>();
                }
                else if (arg.ResolvedType.Resolution.IsTupleType && arg.Expression is ResolvedExpressionKind.ValueTuple vs)
                {
                    args = vs.Item.Select(this.SharedState.EvaluateSubexpression);
                }
                else if (arg.ResolvedType.Resolution is ResolvedTypeKind.TupleType ts)
                {
                    var evaluatedArg = (TupleValue)this.SharedState.EvaluateSubexpression(arg);
                    args = evaluatedArg.GetTupleElements();
                }
                else
                {
                    args = new[] { this.SharedState.EvaluateSubexpression(arg) };
                }

                var argList = args.Select(a => a.Value).ToArray();
                var res = this.SharedState.CurrentBuilder.Call(func, argList);
                if (func.Signature.ReturnType.IsVoid)
                {
                    return this.SharedState.Values.Unit;
                }

                var value = this.SharedState.Values.From(res, returnType);
                this.SharedState.ScopeMgr.RegisterValue(value);
                return value;
            }

            IValue InlineSpecialization(QsSpecialization spec, TypedExpression arg)
            {
                this.SharedState.StartInlining();
                if (spec.Implementation is SpecializationImplementation.Provided impl)
                {
                    if (!spec.Signature.ArgumentType.Resolution.IsUnitType)
                    {
                        var symbolTuple = SyntaxGenerator.ArgumentTupleAsSymbolTuple(impl.Item1);
                        var binding = new QsBinding<TypedExpression>(QsBindingKind.ImmutableBinding, symbolTuple, arg);
                        this.Transformation.StatementKinds.OnVariableDeclaration(binding);
                    }

                    this.Transformation.Statements.OnScope(impl.Item2);
                }
                else
                {
                    throw new InvalidOperationException("missing specialization implementation for inlining");
                }

                return this.SharedState.StopInlining();
            }

            if (!this.SharedState.TryGetGlobalCallable(callableName, out var callable))
            {
                throw new InvalidOperationException("Q# declaration for global callable not found");
            }
            else if (kind == QsSpecializationKind.QsBody // adjointable and controllable operations are not evaluated by the runtime
                && this.SharedState.Functions.TryEvaluate(callableName, arg, out var evaluated))
            {
                // deal with recognized callables provided by the runtime
                return evaluated;
            }
            else if (NameGeneration.TryGetTargetInstructionName(callable, out var instructionName))
            {
                // deal with functions that are part of the target specific instruction set
                var targetInstruction = this.SharedState.GetOrCreateTargetInstruction(instructionName);
                return CallGlobal(targetInstruction, arg, callable.Signature.ReturnType);
            }
            else if (callable.Attributes.Any(BuiltIn.MarksInlining))
            {
                // deal with global callables that need to be inlined
                var inlinedSpec = callable.Specializations.Where(spec => spec.Kind == kind).Single();
                return InlineSpecialization(inlinedSpec, arg);
            }
            else
            {
                // deal with all other global callables
                var func = this.SharedState.GetFunctionByName(callableName, kind);
                return CallGlobal(func, arg, callable.Signature.ReturnType);
            }
        }

        /// <summary>
        /// Handles calls to callables that are (only) locally defined, i.e. calls to callable values.
        /// </summary>
        private IValue InvokeLocalCallable(TypedExpression method, TypedExpression arg)
        {
            var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableInvoke);
            var calledValue = this.SharedState.EvaluateSubexpression(method);
            var argValue = this.SharedState.EvaluateSubexpression(arg);
            if (!arg.ResolvedType.Resolution.IsTupleType &&
                !arg.ResolvedType.Resolution.IsUserDefinedType &&
                !arg.ResolvedType.Resolution.IsUnitType)
            {
                // If the argument is not already of a type that results in the creation of a tuple,
                // then we need to create a tuple to store the (single) argument to be able to pass
                // it to the callable value.
                argValue = this.SharedState.Values.CreateTuple(argValue);
            }

            var callableArg = argValue.QSharpType.Resolution.IsUnitType
                ? this.SharedState.Values.Unit.Value
                : ((TupleValue)argValue).OpaquePointer;
            var returnType = method.ResolvedType.TryGetReturnType().Item;

            if (returnType.Resolution.IsUnitType)
            {
                Value resultTuple = this.SharedState.Constants.UnitValue;
                this.SharedState.CurrentBuilder.Call(func, calledValue.Value, callableArg, resultTuple);
                return this.SharedState.Values.Unit;
            }
            else
            {
                var resElementTypes = returnType.Resolution is ResolvedTypeKind.TupleType elementTypes
                    ? elementTypes.Item
                    : ImmutableArray.Create(returnType);
                TupleValue resultTuple = this.SharedState.Values.CreateTuple(resElementTypes);
                this.SharedState.CurrentBuilder.Call(func, calledValue.Value, callableArg, resultTuple.OpaquePointer);
                return returnType.Resolution.IsTupleType
                    ? resultTuple
                    : resultTuple.GetTupleElements().Single();
            }
        }

        /// <summary>
        /// Evaluates the give expression and uses the runtime function with the given name to apply the corresponding functor.
        /// Does not validate the given arguments.
        /// </summary>
        /// <returns>An invalid expression</returns>
        private ResolvedExpressionKind ApplyFunctor(string runtimeFunctionName, TypedExpression ex)
        {
            var callable = (CallableValue)this.SharedState.EvaluateSubexpression(ex);

            // We don't keep track of alias counts for callables and hence instead
            // take care here to not make unnecessary copies. We have to be pessimistic, however,
            // and make a copy for anything that would require further evaluation of the expression,
            // such as e.g. if ex is a conditional expression.

            // If ex is an identifier to a global callable then it is safe to apply the functor directly,
            // since in that case baseCallable is a freshly created callable value.
            // The same holds if ex is a partial application or another functor application.
            // Call-expression on the other hand may take a callable as argument and return the same value;
            // it is thus not save to apply the functor directly to the returned value (pointer) in that case.
            var isGlobalCallable = ex.TryAsGlobalCallable().IsValue;
            var isPartialApplication = TypedExpression.IsPartialApplication(ex.Expression);
            var isFunctorApplication = ex.Expression.IsAdjointApplication || ex.Expression.IsControlledApplication;
            var safeToModify = isGlobalCallable || isPartialApplication || isFunctorApplication;
            var value = this.ApplyFunctor(runtimeFunctionName, callable, safeToModify);

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        /// <summary>
        /// Invokes the runtime function with the given name to apply a functor to a callable value.
        /// Unless modifyInPlace is set to true, a copy of the callable is made prior to applying the functor.
        /// </summary>
        /// <param name="runtimeFunctionName">The runtime method to invoke in order to apply the funtor</param>
        /// <param name="callable">The callable to copy (unless modifyInPlace is true) before applying the functor to it</param>
        /// <param name="modifyInPlace">If set to true, modifies and returns the given callable</param>
        /// <returns>The callable value to which the functor has been applied</returns>
        private CallableValue ApplyFunctor(string runtimeFunctionName, CallableValue callable, bool modifyInPlace = false)
        {
            // This method is used when applying functors when building a functor application expression
            // as well as when creating the specializations for a partial application.
            if (!modifyInPlace)
            {
                // Since we track alias counts for callables there is no need to force the copy.
                // While making a copy ensures that either a new callable is created with reference count 1,
                // or the reference count of the existing callable is increased by 1,
                // we also need to increase the reference counts for all contained items; i.e. for the capture tuple in this case.
                var makeCopy = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCopy);
                var forceCopy = this.SharedState.Context.CreateConstant(false);
                var modified = this.SharedState.CurrentBuilder.Call(makeCopy, callable.Value, forceCopy);
                callable = this.SharedState.Values.FromCallable(modified, callable.QSharpType);
                this.SharedState.ScopeMgr.ReferenceCaptureTuple(callable);
                this.SharedState.ScopeMgr.RegisterValue(callable);
            }

            // CallableMakeAdjoint and CallableMakeControlled do *not* create a new value
            // but instead modify the given callable in place.
            var applyFunctor = this.SharedState.GetOrCreateRuntimeFunction(runtimeFunctionName);
            this.SharedState.CurrentBuilder.Call(applyFunctor, callable.Value);
            return callable;
        }

        /// <summary>
        /// Creates an array of the given size and populates each element with the given <paramref name="itemValue"/>,
        /// increasing its reference count accordingly. The type of the created array is the current expression type.
        /// Registers the contructed array with the scope manager.
        /// </summary>
        /// <exception cref="InvalidOperationException">The type of the current expression is not an array type.</exception>
        private ResolvedExpressionKind CreateAndPopulateArray(TypedExpression sizeEx, IValue itemValue)
        {
            var elementType = this.SharedState.CurrentExpressionType().Resolution is ResolvedTypeKind.ArrayType et
                ? et.Item
                : throw new InvalidOperationException("current expression is expected to be an array");

            var size = this.SharedState.EvaluateSubexpression(sizeEx);
            var array = this.SharedState.Values.CreateArray(size.Value, elementType);
            this.SharedState.ValueStack.Push(array);
            if (array.Length == this.SharedState.Context.CreateConstant(0L))
            {
                return ResolvedExpressionKind.InvalidExpr;
            }

            // We need to populate the array
            var start = this.SharedState.Context.CreateConstant(0L);
            var end = this.SharedState.CurrentBuilder.Sub(array.Length, this.SharedState.Context.CreateConstant(1L));
            void PopulateItem(Value index)
            {
                // We need to make sure that the reference count for the built item is increased by 1.
                this.SharedState.ScopeMgr.OpenScope();
                array.GetArrayElementPointer(index).StoreValue(itemValue);
                this.SharedState.ScopeMgr.CloseScope(itemValue);
            }

            this.SharedState.IterateThroughRange(start, null, end, PopulateItem);
            return ResolvedExpressionKind.InvalidExpr;
        }

        /* public overrides */

        public override ResolvedExpressionKind OnAddition(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Add(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FAdd(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntAdd creates a new value with reference count 1.
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntAdd);
                var res = this.SharedState.CurrentBuilder.Call(adder, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else if (exType.Resolution.IsString)
            {
                // The runtime function StringConcatenate creates a new value with reference count 1.
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringConcatenate);
                var res = this.SharedState.CurrentBuilder.Call(adder, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else if (exType.Resolution is ResolvedTypeKind.ArrayType elementType)
            {
                // The runtime function ArrayConcatenate creates a new value with reference count 1 and alias count 0.
                var adder = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayConcatenate);
                var res = this.SharedState.CurrentBuilder.Call(adder, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromArray(res, elementType.Item);

                // The explicit ref count increase for all items is necessary for the sake of
                // consistency such that the reference count adjustment for copy-and-update is correct.
                this.SharedState.ScopeMgr.IncreaseReferenceCount(value);
                this.SharedState.ScopeMgr.RegisterValue(value, shallow: true);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException($"invalid type {exType.Resolution} for addition");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnAdjointApplication(TypedExpression ex) =>
            this.ApplyFunctor(RuntimeLibrary.CallableMakeAdjoint, ex);

        public override ResolvedExpressionKind OnArrayItem(TypedExpression arr, TypedExpression idx)
        {
            // TODO: handle multi-dimensional arrays
            var array = (ArrayValue)this.SharedState.EvaluateSubexpression(arr);
            var index = this.SharedState.EvaluateSubexpression(idx);
            var elementType = arr.ResolvedType.Resolution is ResolvedTypeKind.ArrayType arrElementType
                ? arrElementType.Item
                : throw new InvalidOperationException("expecting an array in array item access");

            IValue value;
            if (idx.ResolvedType.Resolution.IsInt)
            {
                value = array.GetArrayElement(index.Value);
            }
            else if (idx.ResolvedType.Resolution.IsRange)
            {
                // Unless we force that memory is copied when a new slice is created,
                // array sliceing creates a new array only if the current alias count is larger than zero.
                // The created array is instantiated with reference count 1 and alias count 0.
                // otherwise, if the current alias count is zero, then the array may be modified in place.
                // In this case, its reference count is increased by 1.
                // Even though we keep track of alias counts for arrays, we force a copy here to simplify
                // the logic we do to avoid alias count increases when possible, while also ensuring that
                // the additional reference count compensation for copy-and-update as explained in the
                // the comment there is exactly one.
                var forceCopy = this.SharedState.Context.CreateConstant(true);
                var sliceArray = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArraySlice1d);
                var slice = this.SharedState.CurrentBuilder.Call(sliceArray, array.Value, index.Value, forceCopy);
                value = this.SharedState.Values.FromArray(slice, elementType);

                // The explicit ref count increase for all items is necessary for the sake of
                // consistency such that the reference count adjustment for copy-and-update is correct.
                this.SharedState.ScopeMgr.IncreaseReferenceCount(value);
                this.SharedState.ScopeMgr.RegisterValue(value, shallow: true);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid index type for array item access");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnBigIntLiteral(BigInteger b)
        {
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (b <= long.MaxValue && b >= long.MinValue)
            {
                // The runtime function BigIntCreateI64 creates a value with reference count 1.
                var createBigInt = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateI64);
                var val = this.SharedState.Context.CreateConstant((long)b);
                var res = this.SharedState.CurrentBuilder.Call(createBigInt, val);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                // The runtime function BigIntCreateArray creates a value with reference count 1.
                var createBigInt = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateArray);
                var bytes = b.ToByteArray();
                var n = this.SharedState.Context.CreateConstant(bytes.Length);
                var byteArray = ConstantArray.From(
                    this.SharedState.Context.Int8Type,
                    bytes.Select(s => this.SharedState.Context.CreateConstant(s)).ToArray());
                var byteArrayPointer = this.SharedState.CurrentBuilder.GetElementPtr(
                    this.SharedState.Context.Int8Type.CreateArrayType((uint)bytes.Length),
                    byteArray,
                    new[] { this.SharedState.Context.CreateConstant(0) });
                var zeroByteArray = this.SharedState.CurrentBuilder.BitCast(
                    byteArrayPointer,
                    this.SharedState.Types.DataArrayPointer);
                var res = this.SharedState.CurrentBuilder.Call(createBigInt, n, zeroByteArray);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnBitwiseAnd(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.And(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntBitwiseAnd creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseAnd);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise AND");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnBitwiseExclusiveOr(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Xor(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntBitwiseXor creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseXor);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise XOR");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnBitwiseNot(TypedExpression ex)
        {
            var exValue = this.SharedState.EvaluateSubexpression(ex);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                Value minusOne = this.SharedState.Context.CreateConstant(-1L);
                var res = this.SharedState.CurrentBuilder.Xor(exValue.Value, minusOne);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntBitwiseNot creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseNot);
                var res = this.SharedState.CurrentBuilder.Call(func, exValue.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise NOT");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnBitwiseOr(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Or(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntBitwiseOr creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntBitwiseOr);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for bitwise OR");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnBoolLiteral(bool b)
        {
            var constant = this.SharedState.Context.CreateConstant(b);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.FromSimpleValue(constant, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnConditionalExpression(TypedExpression condEx, TypedExpression ifTrueEx, TypedExpression ifFalseEx)
        {
            var cond = this.SharedState.EvaluateSubexpression(condEx);
            var exType = this.SharedState.CurrentExpressionType();
            IValue value;

            // Special case: if both values are self-evaluating, we can do this with a select.
            if (this.IsSelfEvaluating(ifTrueEx) && this.IsSelfEvaluating(ifFalseEx))
            {
                var ifTrue = this.SharedState.EvaluateSubexpression(ifTrueEx);
                var ifFalse = this.SharedState.EvaluateSubexpression(ifFalseEx);
                var res = this.SharedState.CurrentBuilder.Select(cond.Value, ifTrue.Value, ifFalse.Value);
                value = this.SharedState.Values.From(res, exType);
            }
            else
            {
                var evaluated = this.SharedState.ConditionalEvaluation(
                    cond.Value,
                    onCondTrue: () => this.SharedState.EvaluateSubexpression(ifTrueEx),
                    onCondFalse: () => this.SharedState.EvaluateSubexpression(ifFalseEx));
                value = this.SharedState.Values.From(evaluated, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnControlledApplication(TypedExpression ex) =>
            this.ApplyFunctor(RuntimeLibrary.CallableMakeControlled, ex);

        public override ResolvedExpressionKind OnCopyAndUpdateExpression(TypedExpression lhs, TypedExpression accEx, TypedExpression rhs)
        {
            // We need to be careful here when the lhs is a freshly create value; in that case, the copy will not be executed
            // during construction of the copy-and-update expression, and we hence must ensure that the created value is not queued
            // for a ref count decrease at the end of the scope (otherwise that ref count decrease will access the updated items
            // instead of the original ones due to the in-place modification).
            // We hence first check whether the lhs is accessed via a local identifier (i.e. can be accessed after the copy-and-update),
            // and if it is, we can (and must) delay the reference count decrease until the end of the scope, since the copy will be
            // executed in this case. If it is not, then we need to make sure that any newly created value is not registered with
            // the scope manager. Instead, we ensure that the necessary ref count decrease happens by unreferencing the original value
            // (lhs of the expression) as part of building the copy-and-update expression.
            var isFromIdentifier = AccessViaLocalId(lhs, out var fromId);
            var originalValue = isFromIdentifier
                ? this.SharedState.EvaluateSubexpression(lhs)
                : this.SharedState.BuildSubitem(lhs); // ensures that newly created values are not registered with the scope manager
            return CopyAndUpdate(this.SharedState, ((fromId, originalValue), accEx, rhs), unreferenceOriginal: !isFromIdentifier);
        }

        public override ResolvedExpressionKind OnDivision(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.SDiv(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FDiv(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntDivide creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntDivide);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for division");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnDoubleLiteral(double d)
        {
            var res = this.SharedState.Context.CreateConstant(d);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.FromSimpleValue(res, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnEquality(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsResult)
            {
                // Generate a call to the result equality testing function
                var equals = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultEqual);
                var res = this.SharedState.CurrentBuilder.Call(equals, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBool || lhsEx.ResolvedType.Resolution.IsInt || lhsEx.ResolvedType.Resolution.IsQubit
                || lhsEx.ResolvedType.Resolution.IsPauli)
            {
                // Works for pointers as well as integer types
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.Equal, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsString)
            {
                // Generate a call to the string equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringEqual);
                var res = this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                // Generate a call to the bigint equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntEqual);
                var res = this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                // TODO: Equality testing for general types
                throw new NotSupportedException("invalid type for equality comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnExponentiate(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var baseValue = this.SharedState.CurrentBuilder.SIToFPCast(lhs.Value, this.SharedState.Context.DoubleType);
                var powFunc = this.SharedState.Module.GetIntrinsicDeclaration("llvm.powi.f", this.SharedState.Context.DoubleType);
                var exponent = this.SharedState.CurrentBuilder.IntCast(rhs.Value, this.SharedState.Context.Int32Type, true);
                var resAsDouble = this.SharedState.CurrentBuilder.Call(powFunc, baseValue, exponent);
                var res = this.SharedState.CurrentBuilder.FPToSICast(resAsDouble, this.SharedState.Types.Int);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var powFunc = this.SharedState.Module.GetIntrinsicDeclaration("llvm.pow.f", this.SharedState.Types.Double);
                var res = this.SharedState.CurrentBuilder.Call(powFunc, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntPower creates a new value with reference count 1.
                // The exponent must be an integer that can fit into an i32.
                var powFunc = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntPower);
                var exponent = this.SharedState.CurrentBuilder.IntCast(rhs.Value, this.SharedState.Context.Int32Type, true);
                var res = this.SharedState.CurrentBuilder.Call(powFunc, lhs.Value, exponent);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for exponentiation");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnFunctionCall(TypedExpression method, TypedExpression arg)
        {
            IValue value;
            var callableName = method.TryAsGlobalCallable().ValueOr(null);
            if (callableName == null)
            {
                // deal with local values; i.e. callables e.g. from partial applications or stored in local variables
                value = this.InvokeLocalCallable(method, arg);
            }
            else
            {
                // deal with global callables
                value = this.InvokeGlobalCallable(callableName, QsSpecializationKind.QsBody, arg);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnGreaterThan(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedGreaterThan, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndGreaterThan, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreater);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnGreaterThanOrEqual(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedGreaterThanOrEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndGreaterThanOrEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreaterEq);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
        {
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (sym is Identifier.LocalVariable local)
            {
                var variable = this.SharedState.ScopeMgr.GetVariable(local.Item);
                value = variable is PointerValue pointer
                    ? pointer.LoadValue()
                    : variable;
            }
            else if (!(sym is Identifier.GlobalCallable globalCallable))
            {
                throw new NotSupportedException("unknown identifier");
            }
            else if (this.SharedState.TryGetGlobalCallable(globalCallable.Item, out QsCallable? callable))
            {
                var table = this.SharedState.GetOrCreateCallableTable(callable);
                value = this.SharedState.Values.CreateCallable(exType, table);
            }
            else
            {
                throw new InvalidOperationException("Q# declaration for global callable not found");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnInequality(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsResult)
            {
                // Generate a call to the result equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultEqual);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBool || lhsEx.ResolvedType.Resolution.IsInt || lhsEx.ResolvedType.Resolution.IsQubit
                || lhsEx.ResolvedType.Resolution.IsPauli)
            {
                // Works for pointers as well as integer types
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.NotEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndNotEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsString)
            {
                // Generate a call to the string equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.StringEqual);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                // Generate a call to the bigint equality testing function
                var compareEquality = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntEqual);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(compareEquality, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                // TODO: Equality testing for general types
                throw new NotSupportedException("invalid type for inequality comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnIntLiteral(long i)
        {
            var constant = this.SharedState.Context.CreateConstant(i);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.FromSimpleValue(constant, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnLeftShift(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.ShiftLeft(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntLeftShift creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntShiftLeft);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for left shift");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnLessThan(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedLessThan, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndLessThan, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreaterEq);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnLessThanOrEqual(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (lhsEx.ResolvedType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Compare(IntPredicate.SignedLessThanOrEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.Compare(RealPredicate.OrderedAndLessThanOrEqual, lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (lhsEx.ResolvedType.Resolution.IsBigInt)
            {
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntGreater);
                var res = this.SharedState.CurrentBuilder.Not(this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value));
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else
            {
                throw new NotSupportedException("invalid type for comparison");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnLogicalAnd(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            Value evaluated;
            var exType = this.SharedState.CurrentExpressionType();

            // Special case: if the right hand side is self-evaluating,
            // we can safely evaluate both expression without introducing a branching.
            if (this.IsSelfEvaluating(rhsEx))
            {
                var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
                var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
                evaluated = this.SharedState.CurrentBuilder.And(lhs.Value, rhs.Value);
            }
            else
            {
                var evaluatedLhs = this.SharedState.EvaluateSubexpression(lhsEx);
                evaluated = this.SharedState.ConditionalEvaluation(
                    evaluatedLhs.Value,
                    onCondTrue: () => this.SharedState.EvaluateSubexpression(rhsEx),
                    defaultValueForCondFalse: evaluatedLhs);
            }

            var value = this.SharedState.Values.FromSimpleValue(evaluated, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnLogicalNot(TypedExpression ex)
        {
            // Get the Value for the expression
            var arg = this.SharedState.EvaluateSubexpression(ex);
            var res = this.SharedState.CurrentBuilder.Not(arg.Value);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.FromSimpleValue(res, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnLogicalOr(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            Value evaluated;
            var exType = this.SharedState.CurrentExpressionType();

            // Special case: if the right hand side is self-evaluating,
            // we can safely evaluate both expression without introducing a branching.
            if (this.IsSelfEvaluating(rhsEx))
            {
                var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
                var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
                evaluated = this.SharedState.CurrentBuilder.Or(lhs.Value, rhs.Value);
            }
            else
            {
                var evaluatedLhs = this.SharedState.EvaluateSubexpression(lhsEx);
                evaluated = this.SharedState.ConditionalEvaluation(
                    this.SharedState.CurrentBuilder.Not(evaluatedLhs.Value),
                    onCondTrue: () => this.SharedState.EvaluateSubexpression(rhsEx),
                    defaultValueForCondFalse: evaluatedLhs);
            }

            var value = this.SharedState.Values.FromSimpleValue(evaluated, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnModulo(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.SRem(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntModulus creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntModulus);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for modulo");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnMultiplication(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Mul(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FMul(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntMultiply creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntMultiply);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for multiplication");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnNamedItem(TypedExpression ex, Identifier acc)
        {
            IValue value;
            if (!(ex.ResolvedType.Resolution is ResolvedTypeKind.UserDefinedType udt))
            {
                throw new NotSupportedException("invalid type for named item access");
            }
            else if (!this.SharedState.TryGetCustomType(udt.Item.GetFullName(), out var udtDecl))
            {
                throw new InvalidOperationException("Q# declaration for type not found");
            }
            else if (acc is Identifier.LocalVariable itemName && FindNamedItem(itemName.Item, udtDecl.TypeItems, out var location))
            {
                value = this.SharedState.EvaluateSubexpression(ex);
                for (int i = 0; i < location.Count; i++)
                {
                    value = ((TupleValue)value).GetTupleElement(location[i]);
                }
            }
            else
            {
                throw new InvalidOperationException("invalid item name in named item access");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnNegative(TypedExpression ex)
        {
            var exValue = this.SharedState.EvaluateSubexpression(ex);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Neg(exValue.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FNeg(exValue.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntNegative creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntNegate);
                var res = this.SharedState.CurrentBuilder.Call(func, exValue.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for negative");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnSizedArray(TypedExpression ex, TypedExpression size)
        {
            var itemValue = this.SharedState.EvaluateSubexpression(ex);
            return this.CreateAndPopulateArray(size, itemValue);
        }

        public override ResolvedExpressionKind OnNewArray(ResolvedType elementType, TypedExpression size)
        {
            IValue DefaultValue(ResolvedType type)
            {
                if (type.Resolution.IsInt)
                {
                    var value = this.SharedState.Context.CreateConstant(0L);
                    return this.SharedState.Values.FromSimpleValue(value, type);
                }
                else if (type.Resolution.IsDouble)
                {
                    var value = this.SharedState.Context.CreateConstant(0.0);
                    return this.SharedState.Values.FromSimpleValue(value, type);
                }
                else if (type.Resolution.IsBool)
                {
                    var value = this.SharedState.Context.CreateConstant(false);
                    return this.SharedState.Values.FromSimpleValue(value, type);
                }
                else if (type.Resolution.IsPauli)
                {
                    var pointer = this.SharedState.Constants.PauliI;
                    var constant = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Pauli, pointer);
                    return this.SharedState.Values.From(constant, type);
                }
                else if (type.Resolution.IsResult)
                {
                    var getZero = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ResultGetZero);
                    var constant = this.SharedState.CurrentBuilder.Call(getZero);
                    return this.SharedState.Values.From(constant, type);
                }
                else if (type.Resolution.IsQubit)
                {
                    var value = Constant.ConstPointerToNullFor(this.SharedState.Types.Qubit);
                    return this.SharedState.Values.From(value, type);
                }
                else if (type.Resolution.IsRange)
                {
                    var pointer = this.SharedState.Constants.EmptyRange;
                    var constant = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Range, pointer);
                    return this.SharedState.Values.From(constant, type);
                }
                else if (type.Resolution is ResolvedTypeKind.TupleType ts)
                {
                    var values = ts.Item.Select(DefaultValue).ToArray();
                    return this.SharedState.Values.CreateTuple(values);
                }
                else if (type.Resolution is ResolvedTypeKind.UserDefinedType udt)
                {
                    if (!this.SharedState.TryGetCustomType(udt.Item.GetFullName(), out var udtDecl))
                    {
                        throw new ArgumentException("type declaration not found");
                    }

                    var elementTypes = udtDecl.Type.Resolution is ResolvedTypeKind.TupleType items ? items.Item : ImmutableArray.Create(udtDecl.Type);
                    var values = elementTypes.Select(DefaultValue).ToArray();
                    return this.SharedState.Values.CreateCustomType(udt.Item, values);
                }

                if (type.Resolution is ResolvedTypeKind.ArrayType itemType)
                {
                    return this.SharedState.Values.CreateArray(itemType.Item);
                }
                else if (type.Resolution.IsFunction || type.Resolution.IsOperation)
                {
                    // We can't simply set this to null, unless the reference and alias counting functions
                    // in the runtime accept null values as arguments.
                    var nullTableName = $"DefaultCallable__NullFunctionTable";
                    var nullTable = this.SharedState.Module.GetNamedGlobal(nullTableName);
                    if (nullTable == null)
                    {
                        var fctType = this.SharedState.Types.FunctionSignature.CreatePointerType();
                        var funcs = Enumerable.Repeat(Constant.ConstPointerToNullFor(fctType), 4);
                        var array = ConstantArray.From(fctType, funcs.ToArray());
                        nullTable = this.SharedState.Module.AddGlobal(array.NativeType, true, Linkage.Internal, array, nullTableName);
                    }

                    var createCallable = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableCreate);
                    var memoryManagementTable = this.SharedState.GetOrCreateCallableMemoryManagementTable(null);
                    var value = this.SharedState.CurrentBuilder.Call(createCallable, nullTable, memoryManagementTable, this.SharedState.Constants.UnitValue);
                    var built = this.SharedState.Values.FromCallable(value, type);
                    this.SharedState.ScopeMgr.RegisterValue(built);
                    return built;
                }
                else if (type.Resolution.IsString)
                {
                    return CreateStringLiteral(this.SharedState, "");
                }
                else if (type.Resolution.IsBigInt)
                {
                    var value = this.SharedState.CurrentBuilder.Call(
                        this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateI64),
                        this.SharedState.Context.CreateConstant(0L));
                    var built = this.SharedState.Values.From(value, type);
                    this.SharedState.ScopeMgr.RegisterValue(built);
                    return built;
                }
                else if (type.Resolution.IsUnitType)
                {
                    return this.SharedState.Values.Unit;
                }
                else
                {
                    throw new NotSupportedException("no known default value for the given type");
                }
            }

            var defaultValue = DefaultValue(elementType);
            return this.CreateAndPopulateArray(size, defaultValue);
        }

        public override ResolvedExpressionKind OnOperationCall(TypedExpression method, TypedExpression arg)
        {
            (TypedExpression, bool, int) StripModifiers(TypedExpression m, bool a, int c) =>
                m.Expression switch
                {
                    ResolvedExpressionKind.AdjointApplication adj => StripModifiers(adj.Item, !a, c),
                    ResolvedExpressionKind.ControlledApplication con => StripModifiers(con.Item, a, c + 1),
                    _ => (m, a, c),
                };

            TypedExpression BuildInnerArg(TypedExpression arg, int controlledCount)
            {
                // throws an InvalidOperationException if the remainingArg is not a tuple with two items
                (TypedExpression, TypedExpression) TupleItems(TypedExpression remainingArg) =>
                    (remainingArg.Expression is ResolvedExpressionKind.ValueTuple tuple && tuple.Item.Length == 2)
                    ? (tuple.Item[0], tuple.Item[1])
                    : throw new InvalidOperationException("control count is inconsistent with the shape of the argument tuple");

                if (controlledCount < 2)
                {
                    // no need to concatenate the controlled arguments
                    return arg;
                }

                // The arglist will be a 2-tuple with the first element an array of qubits and the second element
                // a 2-tuple containing an array of qubits and another tuple -- possibly with more nesting levels
                var (controls, remainingArg) = TupleItems(arg);
                while (--controlledCount > 0)
                {
                    var (innerControls, innerArg) = TupleItems(remainingArg);
                    controls = SyntaxGenerator.AddExpressions(controls.ResolvedType.Resolution, controls, innerControls);
                    remainingArg = innerArg;
                }

                return SyntaxGenerator.TupleLiteral(new[] { controls, remainingArg });
            }

            static QsSpecializationKind GetSpecializationKind(bool isAdjoint, bool isControlled) =>
                isAdjoint && isControlled ? QsSpecializationKind.QsControlledAdjoint :
                isControlled ? QsSpecializationKind.QsControlled :
                isAdjoint ? QsSpecializationKind.QsAdjoint :
                QsSpecializationKind.QsBody;

            // We avoid constructing a callable value when functors are applied to global callables.
            var (innerCallable, isAdjoint, controlledCount) = StripModifiers(method, false, 0);

            IValue value;
            var callableName = innerCallable.TryAsGlobalCallable().ValueOr(null);
            if (callableName == null)
            {
                // deal with local values; i.e. callables e.g. from partial applications or stored in local variables
                value = this.InvokeLocalCallable(method, arg);
            }
            else
            {
                // deal with global callables
                var innerArg = BuildInnerArg(arg, controlledCount);
                var kind = GetSpecializationKind(isAdjoint, controlledCount > 0);
                value = this.InvokeGlobalCallable(callableName, kind, innerArg);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnPartialApplication(TypedExpression method, TypedExpression arg)
        {
            PartialApplicationArgument BuildPartialArgList(ResolvedType argType, TypedExpression arg, IList<ResolvedType> remainingArgs, IList<TypedExpression> capturedValues)
            {
                // We need argType because missing argument items have MissingType, rather than the actual type.
                if (arg.Expression.IsMissingExpr)
                {
                    remainingArgs.Add(argType);
                    var itemType = this.SharedState.LlvmTypeFromQsharpType(argType);
                    return new InnerArg(this.SharedState, itemType, remainingArgs.Count - 1);
                }
                else if (arg.Expression is ResolvedExpressionKind.ValueTuple tuple
                    && argType.Resolution is ResolvedTypeKind.TupleType types)
                {
                    var items = types.Item.Zip(tuple.Item, (t, v) => BuildPartialArgList(t, v, remainingArgs, capturedValues));
                    return new InnerTuple(this.SharedState, argType, items);
                }
                else
                {
                    capturedValues.Add(arg);
                    return new InnerCapture(this.SharedState, capturedValues.Count - 1);
                }
            }

            IrFunction BuildLiftedSpecialization(string name, QsSpecializationKind kind, ResolvedType captureType, ResolvedType paArgsType, PartialApplicationArgument partialArgs)
            {
                IValue ApplyFunctors(CallableValue innerCallable)
                {
                    if (kind == QsSpecializationKind.QsBody)
                    {
                        return innerCallable;
                    }
                    else if (kind == QsSpecializationKind.QsAdjoint)
                    {
                        return this.ApplyFunctor(RuntimeLibrary.CallableMakeAdjoint, innerCallable, modifyInPlace: false);
                    }
                    else if (kind == QsSpecializationKind.QsControlled)
                    {
                        return this.ApplyFunctor(RuntimeLibrary.CallableMakeControlled, innerCallable, modifyInPlace: false);
                    }
                    else if (kind == QsSpecializationKind.QsControlledAdjoint)
                    {
                        innerCallable = this.ApplyFunctor(RuntimeLibrary.CallableMakeAdjoint, innerCallable, modifyInPlace: false);
                        return this.ApplyFunctor(RuntimeLibrary.CallableMakeControlled, innerCallable, modifyInPlace: true);
                    }
                    else
                    {
                        throw new NotImplementedException("unknown specialization");
                    }
                }

                void BuildPartialApplicationBody(IReadOnlyList<Argument> parameters)
                {
                    var captureTuple = this.SharedState.AsArgumentTuple(captureType, parameters[0]);
                    TupleValue BuildControlledInnerArgument()
                    {
                        // The argument tuple given to the controlled version of the partial application consists of the array of control qubits
                        // as well as a tuple with the remaining arguments for the partial application.
                        // We need to cast the corresponding function parameter to the appropriate type and load both of these items.
                        var ctlPaArgsTypes = ImmutableArray.Create(SyntaxGenerator.QubitArrayType, paArgsType);
                        var ctlPaArgsTuple = this.SharedState.Values.FromTuple(parameters[1], ctlPaArgsTypes);
                        var ctlPaArgItems = ctlPaArgsTuple.GetTupleElements();

                        // We then create and populate the complete argument tuple for the controlled specialization of the inner callable.
                        // The tuple consists of the control qubits and the combined tuple of captured values and the arguments given to the partial application.
                        var innerArgs = partialArgs.BuildItem(captureTuple, ctlPaArgItems[1]);
                        return this.SharedState.Values.CreateTuple(ctlPaArgItems[0], innerArgs);
                    }

                    TupleValue innerArg;
                    if (kind == QsSpecializationKind.QsControlled || kind == QsSpecializationKind.QsControlledAdjoint)
                    {
                        // Deal with the extra control qubit arg for controlled and controlled-adjoint
                        // We special case if the base specialization only takes a single parameter and don't create the sub-tuple in this case.
                        innerArg = BuildControlledInnerArgument();
                    }
                    else
                    {
                        var parArgsTuple = paArgsType.Resolution.IsUnitType
                            ? this.SharedState.Values.Unit
                            : this.SharedState.AsArgumentTuple(paArgsType, parameters[1]);
                        var typedInnerArg = partialArgs.BuildItem(captureTuple, parArgsTuple);
                        innerArg = typedInnerArg is TupleValue innerArgTuple
                            ? innerArgTuple
                            : this.SharedState.Values.CreateTuple(typedInnerArg);
                    }

                    var invokeCallable = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.CallableInvoke);
                    var innerCallable = (CallableValue)captureTuple.GetTupleElement(0);
                    this.SharedState.CurrentBuilder.Call(invokeCallable, ApplyFunctors(innerCallable).Value, innerArg.OpaquePointer, parameters[2]);
                }

                return this.SharedState.GeneratePartialApplication(name, kind, BuildPartialApplicationBody);
            }

            var isTrivial = !arg.Exists(ex => !(ex.Expression.IsMissingExpr || ex.Expression.IsValueTuple));
            if (isTrivial)
            {
                // a partial application where all arguments are partially applied
                // is the same as just the method.
                return this.Expressions.OnTypedExpression(method).Expression;
            }

            var liftedName = this.SharedState.GlobalName("PartialApplication");
            ResolvedType CallableArgumentType(ResolvedType t) => t.Resolution switch
            {
                ResolvedTypeKind.Function paf => paf.Item1,
                ResolvedTypeKind.Operation pao => pao.Item1.Item1,
                _ => throw new InvalidOperationException("expecting an operation or function type"),
            };

            // Figure out the inputs to the resulting callable based on the signature of the partial application expression
            var exType = this.SharedState.CurrentExpressionType();
            var callableArgType = CallableArgumentType(exType);

            // Argument type of the callable that is partially applied
            var innerArgType = CallableArgumentType(method.ResolvedType);

            // Create the capture tuple, which contains the inner callable as the first item and
            // construct the mapping to combine captured arguments with the arguments for the partial application.
            // Since the capture tuple won't be accessible and will only be used by the partial application,
            // we don't register the tuple with the scope manager and instead don't increase the ref count
            // when building the partial application. Since the partial application is registered with the scope
            // manager, the capture tuple will be released when the partial application is, as it should be.
            var captured = ImmutableArray.CreateBuilder<TypedExpression>();
            captured.Add(method);
            var rebuild = BuildPartialArgList(innerArgType, arg, new List<ResolvedType>(), captured);
            var captureType = ResolvedType.New(ResolvedTypeKind.NewTupleType(captured.Select(element => element.ResolvedType).ToImmutableArray()));

            // Create the lifted specialization implementation(s)
            // First, figure out which ones we need to create
            var callableInfo = method.ResolvedType.TryGetCallableInformation();
            var supportedFunctors = callableInfo.IsValue
                ? callableInfo.Item.Characteristics.SupportedFunctors
                : QsNullable<ImmutableHashSet<QsFunctor>>.Null;

            bool SupportsFunctors(params QsFunctor[] functors) =>
                supportedFunctors.IsValue && functors.All(supportedFunctors.Item.Contains);

            bool SupportsNecessaryFunctors(QsSpecializationKind kind) =>
                kind == QsSpecializationKind.QsAdjoint ? SupportsFunctors(QsFunctor.Adjoint) :
                kind == QsSpecializationKind.QsControlled ? SupportsFunctors(QsFunctor.Controlled) :
                kind == QsSpecializationKind.QsControlledAdjoint ? SupportsFunctors(QsFunctor.Adjoint, QsFunctor.Controlled) :
                true;

            IrFunction? BuildSpec(QsSpecializationKind kind) =>
                SupportsNecessaryFunctors(kind)
                    ? BuildLiftedSpecialization(liftedName, kind, captureType, callableArgType, rebuild)
                    : null;
            var table = this.SharedState.CreateCallableTable(liftedName, BuildSpec);
            var value = this.SharedState.Values.CreateCallable(exType, table, captured.ToImmutable());

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnPauliLiteral(QsPauli p)
        {
            IValue LoadPauli(Value pauli)
            {
                var constant = this.SharedState.CurrentBuilder.Load(this.SharedState.Types.Pauli, pauli);
                var exType = this.SharedState.CurrentExpressionType();
                return this.SharedState.Values.From(constant, exType);
            }

            IValue value;
            if (p.IsPauliI)
            {
                value = LoadPauli(this.SharedState.Constants.PauliI);
            }
            else if (p.IsPauliX)
            {
                value = LoadPauli(this.SharedState.Constants.PauliX);
            }
            else if (p.IsPauliY)
            {
                value = LoadPauli(this.SharedState.Constants.PauliY);
            }
            else if (p.IsPauliZ)
            {
                value = LoadPauli(this.SharedState.Constants.PauliZ);
            }
            else
            {
                throw new NotSupportedException("unknown value for Pauli");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnRangeLiteral(TypedExpression lhs, TypedExpression rhs)
        {
            Value start;
            Value step;
            switch (lhs.Expression)
            {
                case ResolvedExpressionKind.RangeLiteral lit:
                    start = this.SharedState.EvaluateSubexpression(lit.Item1).Value;
                    step = this.SharedState.EvaluateSubexpression(lit.Item2).Value;
                    break;
                default:
                    start = this.SharedState.EvaluateSubexpression(lhs).Value;
                    step = this.SharedState.Context.CreateConstant(1L);
                    break;
            }

            Value end = this.SharedState.EvaluateSubexpression(rhs).Value;
            var value = this.SharedState.CreateRange(start, step, end);

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnResultLiteral(QsResult r)
        {
            var getResultLiteral = this.SharedState.GetOrCreateRuntimeFunction(
                r.IsZero ? RuntimeLibrary.ResultGetZero : RuntimeLibrary.ResultGetOne);
            var constant = this.SharedState.CurrentBuilder.Call(getResultLiteral);
            var exType = this.SharedState.CurrentExpressionType();
            var value = this.SharedState.Values.From(constant, exType);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnRightShift(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.ArithmeticShiftRight(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntRightShift creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntShiftRight);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for right shift");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnStringLiteral(string str, ImmutableArray<TypedExpression> exs)
        {
            var subexpressions = exs.Select(this.SharedState.EvaluateSubexpression).ToArray();
            var value = CreateStringLiteral(this.SharedState, str, subexpressions);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnSubtraction(TypedExpression lhsEx, TypedExpression rhsEx)
        {
            var lhs = this.SharedState.EvaluateSubexpression(lhsEx);
            var rhs = this.SharedState.EvaluateSubexpression(rhsEx);
            var exType = this.SharedState.CurrentExpressionType();

            IValue value;
            if (exType.Resolution.IsInt)
            {
                var res = this.SharedState.CurrentBuilder.Sub(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsDouble)
            {
                var res = this.SharedState.CurrentBuilder.FSub(lhs.Value, rhs.Value);
                value = this.SharedState.Values.FromSimpleValue(res, exType);
            }
            else if (exType.Resolution.IsBigInt)
            {
                // The runtime function BigIntSubtract creates a new value with reference count 1.
                var func = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntSubtract);
                var res = this.SharedState.CurrentBuilder.Call(func, lhs.Value, rhs.Value);
                value = this.SharedState.Values.From(res, exType);
                this.SharedState.ScopeMgr.RegisterValue(value);
            }
            else
            {
                throw new NotSupportedException("invalid type for subtraction");
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnUnitValue()
        {
            var value = this.SharedState.Values.Unit;
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnValueArray(ImmutableArray<TypedExpression> vs)
        {
            // TODO: handle multi-dimensional arrays
            var elementType = this.SharedState.CurrentExpressionType().Resolution is ResolvedTypeKind.ArrayType arrItemType
                ? arrItemType.Item
                : throw new InvalidOperationException("current expression is not of type array");

            var value = this.SharedState.Values.CreateArray(elementType, vs);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnValueTuple(ImmutableArray<TypedExpression> vs)
        {
            IValue value =
                vs.Length == 0 ? this.SharedState.Values.Unit :
                vs.Length == 1 ? this.SharedState.EvaluateSubexpression(vs.Single()) :
                this.SharedState.Values.CreateTuple(vs);
            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }

        public override ResolvedExpressionKind OnUnwrapApplication(TypedExpression ex)
        {
            // Since we simply represent user defined types as tuples, we don't need to do anything
            // except pushing the value on the value stack unless the tuples contains a single item,
            // in which case we need to remove the tuple wrapping.
            var value = this.SharedState.EvaluateSubexpression(ex);
            if (!(ex.ResolvedType.Resolution is ResolvedTypeKind.UserDefinedType udt))
            {
                throw new NotSupportedException("invalid type for unwrap operator");
            }
            else if (!this.SharedState.TryGetCustomType(udt.Item.GetFullName(), out var udtDecl))
            {
                throw new InvalidOperationException("Q# declaration for type not found");
            }
            else if (!udtDecl.Type.Resolution.IsTupleType)
            {
                value = ((TupleValue)value).GetTupleElement(0);
            }

            this.SharedState.ValueStack.Push(value);
            return ResolvedExpressionKind.InvalidExpr;
        }
    }
}
