// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using ResolvedExpression = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedInitializerKind = QsInitializerKind<ResolvedInitializer, TypedExpression>;

    internal class QirStatementKindTransformation : StatementKindTransformation<GenerationContext>
    {
        public QirStatementKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation) : base(parentTransformation)
        {
        }

        public QirStatementKindTransformation(GenerationContext sharedState) : base(sharedState)
        {
        }

        public QirStatementKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options) : base(parentTransformation, options)
        {
        }

        public QirStatementKindTransformation(GenerationContext sharedState, TransformationOptions options) : base(sharedState, options)
        {
        }

        // static methods

        // used by the return statement transformation as well as for building constructors
        internal static void AddReturn(GenerationContext sharedState, Value result, bool returnsVoid)
        {
            // If we're not inlining, compute the result, release any pending qubits, and generate a return.
            // Otherwise, just evaluate the result and leave it on top of the stack.
            if (sharedState.CurrentInlineLevel == 0)
            {
                // The return value and its inner items won't be unreferenced when exiting the scope
                // since it will be used by the caller
                sharedState.ScopeMgr.ExitScope(returned: result);

                if (returnsVoid)
                {
                    sharedState.CurrentBuilder.Return();
                }
                else
                {
                    sharedState.CurrentBuilder.Return(result);
                }
            }
            else
            {
                sharedState.ValueStack.Push(result);
            }
        }

        // private helpers

        /// <summary>
        /// Binds a SymbolTuple, which may be a single Symbol or a tuple of Symbols and embedded tuples,
        /// to a Value.
        /// If the SymbolTuple to bind is structured, the Value will point to an LLVM structure with
        /// matching structure.
        /// </summary>
        /// <param name="symbolTuple">The SymbolTuple to bind</param>
        /// <param name="symbolValue">The Value to bind to</param>
        /// <param name="symbolType">The Q# type of the SymbolTuple</param>
        /// <param name="isImmutable">true if the binding is immutable, false if mutable</param>
        private void BindSymbolTuple(SymbolTuple symbolTuple, Value symbolValue, ResolvedType symbolType, bool mutable = false)
        {
            // Bind a Value to a simple variable
            void BindVariable(string variable, Value value, ResolvedType type)
            {
                if (mutable)
                {
                    var ptr = this.SharedState.CurrentBuilder.Alloca(this.SharedState.LlvmTypeFromQsharpType(type));
                    this.SharedState.RegisterName(variable, ptr, true);
                    this.SharedState.CurrentBuilder.Store(value, ptr);
                }
                else
                {
                    this.SharedState.RegisterName(variable, value, false);
                }
            }

            // Bind a structured Value to a tuple of variables (which might contain embedded tuples)
            void BindTuple(ImmutableArray<SymbolTuple> items, ImmutableArray<ResolvedType> types, Value val)
            {
                Contract.Assert(items.Length == types.Length, "Tuple to deconstruct doesn't match symbols");
                var itemTypes = types.Select(this.SharedState.LlvmTypeFromQsharpType).ToArray();
                var tupleType = this.SharedState.Types.CreateConcreteTupleType(itemTypes);
                var itemPointers = this.SharedState.GetTupleElementPointers(tupleType, val);

                for (int i = 0; i < items.Length; i++)
                {
                    if (!items[i].IsDiscardedItem && !items[i].IsInvalidItem)
                    {
                        var itemValue = this.SharedState.CurrentBuilder.Load(itemTypes[i], itemPointers[i]);
                        BindItem(items[i], itemValue, types[i]);
                    }
                }
            }

            // Bind a Value to an item, which might be a single variable or a tuple
            void BindItem(SymbolTuple item, Value itemValue, ResolvedType itemType)
            {
                if (item is SymbolTuple.VariableName v)
                {
                    BindVariable(v.Item, itemValue, itemType);
                }
                else if (item is SymbolTuple.VariableNameTuple t)
                {
                    BindTuple(t.Item, ((QsResolvedTypeKind.TupleType)itemType.Resolution).Item, itemValue);
                }
            }

            BindItem(symbolTuple, symbolValue, symbolType);
        }

        /// <summary>
        /// Generate the allocations and bindings for the qubit bindings in a using statement.
        /// </summary>
        /// <param name="binding">The Q# binding to process</param>
        private void ProcessQubitBinding(QsBinding<ResolvedInitializer> binding)
        {
            ResolvedType qubitType = ResolvedType.New(QsResolvedTypeKind.Qubit);
            IrFunction allocateOne = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.QubitAllocate);
            IrFunction allocateArray = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.QubitAllocateArray);

            Value Allocate(ResolvedInitializer init)
            {
                if (init.Resolution.IsSingleQubitAllocation)
                {
                    Value allocation = this.SharedState.CurrentBuilder.Call(allocateOne);
                    this.SharedState.ScopeMgr.AddQubitAllocation(allocation);
                    return allocation;
                }
                else if (init.Resolution is ResolvedInitializerKind.QubitRegisterAllocation reg)
                {
                    Value countValue = this.SharedState.EvaluateSubexpression(reg.Item);
                    Value allocation = this.SharedState.CurrentBuilder.Call(allocateArray, countValue);
                    this.SharedState.ScopeMgr.AddQubitAllocation(allocation);
                    return allocation;
                }
                else if (init.Resolution is ResolvedInitializerKind.QubitTupleAllocation inits)
                {
                    var items = inits.Item.Select(Allocate).ToArray();
                    return this.SharedState.CreateTuple(this.SharedState.CurrentBuilder, items).TypedPointer;
                }
                else
                {
                    throw new NotImplementedException("unknown initializer in qubit allocation");
                }
            }

            // Generate the allocations for an item, which might be a single variable or might be a tuple
            void AllocateAndAssign(SymbolTuple item, ResolvedInitializer itemInit)
            {
                switch (item)
                {
                    case SymbolTuple.VariableName v:
                        this.SharedState.RegisterName(v.Item, Allocate(itemInit));
                        break;
                    case SymbolTuple.VariableNameTuple syms:
                        if (itemInit.Resolution is ResolvedInitializerKind.QubitTupleAllocation inits
                            && inits.Item.Length == syms.Item.Length)
                        {
                            for (int i = 0; i < syms.Item.Length; i++)
                            {
                                AllocateAndAssign(syms.Item[i], inits.Item[i]);
                            }
                            break;
                        }
                        else
                        {
                            throw new ArgumentException("shape of symbol tuple does not match initializers");
                        }
                }
            }

            AllocateAndAssign(binding.Lhs, binding.Rhs);
        }

        /// <summary>
        /// Generate QIR for a Q# scope, with a specified continuation block.
        /// The QIR starts in the provided basic block, but may be generated into many blocks depending
        /// on the Q# code.
        /// The QIR generation is wrapped in a ref counting scope; temporary references created in the scope
        /// will be unreferenced before branch to the continuation block.
        /// </summary>
        /// <param name="block">The LLVM basic block to start in</param>
        /// <param name="scope">The Q# scope to generate QIR for</param>
        /// <param name="continuation">The block where execution should continue after this scope,
        /// assuming that the scope doesn't end with a return statement</param>
        private void ProcessBlock(BasicBlock block, QsScope scope, BasicBlock continuation)
        {
            this.SharedState.SetCurrentBlock(block);
            this.SharedState.ScopeMgr.OpenScope();
            this.Transformation.Statements.OnScope(scope);
            var isTerminated = this.SharedState.CurrentBlock?.Terminator != null;
            this.SharedState.ScopeMgr.CloseScope(isTerminated);
            if (!isTerminated)
            {
                this.SharedState.CurrentBuilder.Branch(continuation);
            }
        }

        /// <summary>
        /// Generate QIR for a Q# scope, with a specified continuation block.
        /// The QIR starts in the current basic block, but may be generated into many blocks depending
        /// on the Q# code.
        /// This method does not open or close a reference-counting scope.
        /// </summary>
        /// <param name="block">The LLVM basic block to start in</param>
        /// <param name="scope">The Q# scope to generate QIR for</param>
        /// <param name="continuation">The block where execution should continue after this scope,
        /// assuming that the scope doesn't end with a return statement</param>
        private void ProcessUnscopedBlock(BasicBlock block, QsScope scope, BasicBlock continuation)
        {
            this.SharedState.SetCurrentBlock(block);
            this.Transformation.Statements.OnScope(scope);
            if (this.SharedState.CurrentBlock?.Terminator == null)
            {
                this.SharedState.CurrentBuilder.Branch(continuation);
            }
        }

        // public overrides

        public override QsStatementKind OnQubitScope(QsQubitScope stm)
        {
            this.SharedState.ScopeMgr.OpenScope();
            this.ProcessQubitBinding(stm.Binding); // Apply the bindings and add them to the scope
            this.Transformation.Statements.OnScope(stm.Body); // Process the body
            this.SharedState.ScopeMgr.CloseScope(this.SharedState.CurrentBlock?.Terminator != null);
            return QsStatementKind.EmptyStatement;
        }

        /// <exception cref="InvalidOperationException">The current function or the current block is set to null.</exception>
        public override QsStatementKind OnConditionalStatement(QsConditionalStatement stm)
        {
            if (this.SharedState.CurrentFunction == null || this.SharedState.CurrentBlock == null)
            {
                throw new InvalidOperationException("the current function or the current block is set to null");
            }

            var clauses = stm.ConditionalBlocks;
            // Create the "continuation" block, used for all conditionals
            var contBlock = this.SharedState.AddBlockAfterCurrent("continue");

            // if/then/elif...else
            // The first test goes in the current block. If it succeeds, we go to the "then0" block.
            // If it fails, we go to the "test1" block, where the second test is made.
            // If the second test succeeds, we go to the "then1" block, otherwise to the "test2" block, etc.
            // If all tests fail, we go to the "else" block if there's a default block, or to the
            // continuation block if not.
            for (int n = 0; n < clauses.Length; n++)
            {
                // Evaluate the test, which should be a Boolean at this point
                var test = clauses[n].Item1;
                var testValue = this.SharedState.EvaluateSubexpression(test);

                // The success block is always then{n}
                var successBlock = this.SharedState.CurrentFunction.InsertBasicBlock(
                            this.SharedState.GenerateUniqueName($"then{n}"), contBlock);

                // If this is an intermediate clause, then the next block if the test fails
                // is the next clause's test block.
                // If this is the last clause, then the next block is the default clause's block
                // if there is one, or the continue block if not.
                var failureBlock = n < clauses.Length - 1
                    ? this.SharedState.CurrentFunction.InsertBasicBlock(this.SharedState.GenerateUniqueName($"test{n + 1}"), contBlock)
                    : (stm.Default.IsNull
                        ? contBlock
                        : this.SharedState.CurrentFunction.InsertBasicBlock(
                                this.SharedState.GenerateUniqueName($"else"), contBlock));

                // Create the branch
                this.SharedState.CurrentBuilder.Branch(testValue, successBlock, failureBlock);

                // Get a builder for the then block, make it current, and then process the block
                var thenScope = clauses[n].Item2.Body;
                this.SharedState.OpenNamingScope();
                this.ProcessBlock(successBlock, thenScope, contBlock);
                this.SharedState.CloseNamingScope();

                // Make the failure current for the next clause
                this.SharedState.SetCurrentBlock(failureBlock);
            }

            // Deal with the default, if there is any
            if (stm.Default.IsValue)
            {
                this.SharedState.OpenNamingScope();
                this.ProcessBlock(this.SharedState.CurrentBlock, stm.Default.Item.Body, contBlock);
                this.SharedState.CloseNamingScope();
            }

            // Finally, set the continuation block as current
            this.SharedState.SetCurrentBlock(contBlock);

            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnExpressionStatement(TypedExpression ex)
        {
            this.SharedState.EvaluateSubexpression(ex);
            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnFailStatement(TypedExpression ex)
        {
            var message = this.SharedState.EvaluateSubexpression(ex);

            // Release any resources (qubits or memory) before we fail.
            this.SharedState.ScopeMgr.ExitScope(message);
            this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.Fail), message);
            this.SharedState.CurrentBuilder.Unreachable();

            // Even though this terminates the block execution, we'll still wind up terminating
            // the containing Q# statement block, and thus the LLVM basic block, so we don't need
            // to tell LLVM that this is actually a terminator.

            return QsStatementKind.EmptyStatement;
        }

        /// <exception cref="InvalidOperationException">
        /// The current function or the current block is set to null, or if the iteration is not over an array or range.
        /// </exception>
        public override QsStatementKind OnForStatement(QsForStatement stm)
        {
            if (this.SharedState.CurrentFunction == null || this.SharedState.CurrentBlock == null)
            {
                throw new InvalidOperationException("current function is set to null");
            }

            (Value Start, Value Step, Value End) RangeItems(TypedExpression range)
            {
                (Value? startValue, Value? stepValue, Value? endValue) = (null, null, null);
                if (range.Expression is ResolvedExpression.RangeLiteral rlit)
                {
                    if (rlit.Item1.Expression is ResolvedExpression.RangeLiteral rlitInner)
                    {
                        startValue = this.SharedState.EvaluateSubexpression(rlitInner.Item1);
                        stepValue = this.SharedState.EvaluateSubexpression(rlitInner.Item2);
                    }
                    else
                    {
                        // 1 is the step
                        startValue = this.SharedState.EvaluateSubexpression(rlit.Item1);
                        stepValue = this.SharedState.Context.CreateConstant(1L);
                    }

                    // Item2 is always the end. Either Item1 is the start and 1 is the step,
                    // or Item1 is a range expression itself, with Item1 the start and Item2 the step.
                    endValue = this.SharedState.EvaluateSubexpression(rlit.Item2);
                }
                else
                {
                    var rangeValue = this.SharedState.EvaluateSubexpression(range);
                    startValue = this.SharedState.CurrentBuilder.ExtractValue(rangeValue, 0);
                    stepValue = this.SharedState.CurrentBuilder.ExtractValue(rangeValue, 1);
                    endValue = this.SharedState.CurrentBuilder.ExtractValue(rangeValue, 2);
                }
                return (startValue, stepValue, endValue);
            }

            void IterateThroughRange(Value startValue, Value increment, Value endValue, Action<Value> executeBody)
            {
                // Creates a preheader block to determine the direction of the loop.
                Value CreatePreheader()
                {
                    var preheaderName = this.SharedState.GenerateUniqueName("preheader");
                    var preheaderBlock = this.SharedState.CurrentFunction.AppendBasicBlock(preheaderName);

                    // End the current block by branching to the preheader
                    this.SharedState.CurrentBuilder.Branch(preheaderBlock);

                    // Preheader block: determine whether the step size is positive
                    this.SharedState.SetCurrentBlock(preheaderBlock);
                    return this.SharedState.CurrentBuilder.Compare(
                        IntPredicate.SignedGreaterThan,
                        increment,
                        this.SharedState.Context.CreateConstant(0L));
                }

                Value EvaluateCondition(Value loopVarIncreases, Value loopVariable)
                {
                    var isGreaterOrEqualEnd = this.SharedState.CurrentBuilder.Compare(
                        IntPredicate.SignedGreaterThanOrEqual, loopVariable, endValue);
                    var isSmallerOrEqualEnd = this.SharedState.CurrentBuilder.Compare(
                        IntPredicate.SignedLessThanOrEqual, loopVariable, endValue);
                    // If we increase the loop variable in each iteration (i.e. step is positive)
                    // then we need to check that the current value is smaller than or equal to the end value,
                    // and otherwise we check if it is larger than or equal to the end value.
                    return this.SharedState.CurrentBuilder.Select(loopVarIncreases, isSmallerOrEqualEnd, isGreaterOrEqualEnd);
                }

                Value loopVarIncreases = CreatePreheader();
                this.SharedState.CreateForLoop(startValue, loopVar => EvaluateCondition(loopVarIncreases, loopVar), increment, executeBody);
            }

            if (stm.IterationValues.ResolvedType.Resolution.IsRange)
            {
                void ExecuteBody(Value loopVariable)
                {
                    // If we iterate through a range, we don't inject an additional binding for the loop variable
                    // at the beginning of the body and instead directly register the iteration value under that name.
                    var loopVarName = stm.LoopItem.Item1 is SymbolTuple.VariableName name
                        ? name.Item
                        : throw new ArgumentException("invalid loop variable name");
                    this.SharedState.RegisterName(loopVarName, loopVariable);
                    this.Transformation.Statements.OnScope(stm.Body);
                }

                var (startValue, stepValue, endValue) = RangeItems(stm.IterationValues);
                IterateThroughRange(startValue, stepValue, endValue, ExecuteBody);
            }
            else if (stm.IterationValues.ResolvedType.Resolution.IsArrayType)
            {
                void ExecuteBody(Value arrayItem)
                {
                    // If we iterate through an array, we inject a binding at the beginning of the body.
                    this.BindSymbolTuple(stm.LoopItem.Item1, arrayItem, stm.LoopItem.Item2);
                    this.Transformation.Statements.OnScope(stm.Body);
                }

                var itemType = this.SharedState.LlvmTypeFromQsharpType(stm.LoopItem.Item2);
                var array = this.SharedState.EvaluateSubexpression(stm.IterationValues);
                this.SharedState.IterateThroughArray(itemType, array, ExecuteBody);
            }
            else
            {
                throw new InvalidOperationException("For loop through invalid value");
            }

            return QsStatementKind.EmptyStatement;
        }

        /// <exception cref="InvalidOperationException">The current function is set to null.</exception>
        public override QsStatementKind OnRepeatStatement(QsRepeatStatement stm)
        {
            if (this.SharedState.CurrentFunction == null)
            {
                throw new InvalidOperationException("current function is set to null");
            }

            // The basic approach here is to put the repeat into one basic block.
            // A second basic block holds the evaluation of the test expression and the test itself.
            // The fixup is in a third basic block, and then there is a final basic block as the continuation.
            // We could merge the repeat and the test into one block, but it seems that it might be easier to
            // analyze the loop later if we do it this way.
            // We need to be a bit careful about scopes here, though.

            this.SharedState.OpenNamingScope();

            var repeatBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("repeat"));
            var testBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("until"));
            var fixupBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("fixup"));
            var contBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("rend"));

            this.SharedState.CurrentBuilder.Branch(repeatBlock);

            this.SharedState.ScopeMgr.OpenScope();
            this.ProcessUnscopedBlock(repeatBlock, stm.RepeatBlock.Body, testBlock);

            this.SharedState.SetCurrentBlock(testBlock);
            var test = this.SharedState.EvaluateSubexpression(stm.SuccessCondition);
            this.SharedState.CurrentBuilder.Branch(test, contBlock, fixupBlock);

            this.ProcessUnscopedBlock(fixupBlock, stm.FixupBlock.Body, repeatBlock);

            this.SharedState.SetCurrentBlock(contBlock);
            this.SharedState.ScopeMgr.CloseScope(this.SharedState.CurrentBlock?.Terminator != null);

            this.SharedState.CloseNamingScope();

            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnReturnStatement(TypedExpression ex)
        {
            Value result = this.SharedState.EvaluateSubexpression(ex);
            AddReturn(this.SharedState, result, ex.ResolvedType.Resolution.IsUnitType);
            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnValueUpdate(QsValueUpdate stm)
        {
            // Given a symbol with an existing binding, calls RemoveReference on the old value,
            // update the binding to a new value and calls AddReference on the new value.
            void UpdateBinding(string symbol, Value newValue)
            {
                Value ptr = this.SharedState.GetNamedPointer(symbol);
                this.SharedState.ScopeMgr.RemoveReference(ptr);
                this.SharedState.CurrentBuilder.Store(newValue, ptr);
                this.SharedState.ScopeMgr.AddReference(newValue);
            }

            // Update a tuple of items from a tuple value.
            void UpdateTuple(ImmutableArray<TypedExpression> items, Value val)
            {
                var itemTypes = items.Select(i => this.SharedState.LlvmTypeFromQsharpType(i.ResolvedType)).ToArray();
                var tupleType = this.SharedState.Types.CreateConcreteTupleType(itemTypes);
                var tupleItems = this.SharedState.GetTupleElements(tupleType, val);
                for (int i = 0; i < tupleItems.Length; i++)
                {
                    UpdateItem(items[i], tupleItems[i]);
                }
            }

            // Update an item, which might be a single symbol or a tuple, to a new Value
            void UpdateItem(TypedExpression item, Value itemValue)
            {
                if (item.Expression is ResolvedExpression.Identifier id && id.Item1 is Identifier.LocalVariable varName)
                {
                    UpdateBinding(varName.Item, itemValue);
                }
                else if (item.Expression is ResolvedExpression.ValueTuple tuple)
                {
                    UpdateTuple(tuple.Item, itemValue);
                }
                else
                {
                    // This should never happen
                    throw new InvalidOperationException("set statement with invalid left-hand side");
                }
            }

            var value = this.SharedState.EvaluateSubexpression(stm.Rhs);
            UpdateItem(stm.Lhs, value);
            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnVariableDeclaration(QsBinding<TypedExpression> stm)
        {
            var val = this.SharedState.EvaluateSubexpression(stm.Rhs);
            this.BindSymbolTuple(stm.Lhs, val, stm.Rhs.ResolvedType, stm.Kind.IsMutableBinding);
            return QsStatementKind.EmptyStatement;
        }

        /// <exception cref="InvalidOperationException">The current function is set to null.</exception>
        public override QsStatementKind OnWhileStatement(QsWhileStatement stm)
        {
            if (this.SharedState.CurrentFunction == null)
            {
                throw new InvalidOperationException("current function is set to null");
            }

            // The basic approach here is to put the evaluation of the test expression into one basic block,
            // the body of the loop in a second basic block, and then have a third basic block as the continuation.

            var testBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("while"));
            var bodyBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("do"));
            var contBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("wend"));

            this.SharedState.CurrentBuilder.Branch(testBlock);
            this.SharedState.SetCurrentBlock(testBlock);
            // The OpenScope is almost certainly unnecessary, but it is technically possible for the condition
            // expression to perform an allocation that needs to get cleaned up, so...
            this.SharedState.ScopeMgr.OpenScope();
            var test = this.SharedState.EvaluateSubexpression(stm.Condition);
            this.SharedState.ScopeMgr.CloseScope(this.SharedState.CurrentBlock?.Terminator != null);
            this.SharedState.CurrentBuilder.Branch(test, bodyBlock, contBlock);

            this.SharedState.OpenNamingScope();
            this.ProcessBlock(bodyBlock, stm.Body, testBlock);
            this.SharedState.CloseNamingScope();
            this.SharedState.SetCurrentBlock(contBlock);
            return QsStatementKind.EmptyStatement;
        }
    }
}
