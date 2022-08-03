// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LlvmBindings.Values;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using ResolvedExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedInitializerKind = QsInitializerKind<ResolvedInitializer, TypedExpression>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    internal class QirStatementKindTransformation : StatementKindTransformation<GenerationContext>
    {
        public QirStatementKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation)
            : base(parentTransformation)
        {
        }

        public QirStatementKindTransformation(GenerationContext sharedState)
            : base(sharedState)
        {
        }

        public QirStatementKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options)
            : base(parentTransformation, options)
        {
        }

        public QirStatementKindTransformation(GenerationContext sharedState, TransformationOptions options)
            : base(sharedState, options)
        {
        }

        // private helpers

        /// <summary>
        /// Binds a symbol tuple by invoking the given action for each sub-binding with a suitable value.
        /// to a Value.
        /// </summary>
        /// <param name="symbols">The symbols to bind</param>
        /// <param name="ex">
        /// <param name="bindVariables">The action to invoke to bind each symbol or symbol tuple to a value</param>
        /// The Q# expression that defines the value to bind the symbols to; it will be deconstructed if necessary
        /// </param>
        private void BindSymbolTuple(SymbolTuple symbols, TypedExpression ex, Action<SymbolTuple, TypedExpression> bindVariables)
        {
            if (symbols is SymbolTuple.VariableNameTuple syms
                && ex.Expression is ResolvedExpressionKind.ValueTuple vs)
            {
                if (vs.Item.Length != syms.Item.Length)
                {
                    throw new InvalidOperationException("shape mismatch in symbol binding");
                }

                foreach (var (sym, value) in syms.Item.Zip(vs.Item, (s, v) => (s, v)))
                {
                    this.BindSymbolTuple(sym, value, bindVariables);
                }
            }
            else
            {
                bindVariables(symbols, ex);
            }
        }

        /// <summary>
        /// Binds a symbol tuple by invoking the given action for each symbol name with a suitable value.
        /// to a Value.
        /// </summary>
        /// <param name="symbols">The symbols to bind</param>
        /// <param name="value">The value to bind the symbols to; it will be deconstructed if necessary</param>
        /// <param name="bindVariable">The action to invoke to bind each symbol</param>
        private void BindSymbolTuple(SymbolTuple symbols, IValue value, Action<string, IValue> bindVariable)
        {
            void DestructTuple(ImmutableArray<SymbolTuple> symbols, IValue value)
            {
                if (value is not TupleValue tuple || symbols.Length != tuple.ElementTypes.Length)
                {
                    throw new InvalidOperationException("shape mismatch in symbol binding");
                }

                for (int i = 0; i < symbols.Length; i++)
                {
                    if (!symbols[i].IsDiscardedItem)
                    {
                        var itemValue = tuple.GetTupleElement(i);
                        this.BindSymbolTuple(symbols[i], itemValue, bindVariable);
                    }
                }
            }

            if (symbols is SymbolTuple.VariableName varName)
            {
                bindVariable(varName.Item, value);
            }
            else if (symbols is SymbolTuple.VariableNameTuple syms)
            {
                DestructTuple(syms.Item, value);
            }
            else if (!symbols.IsDiscardedItem)
            {
                throw new NotImplementedException("unknown item in symbol tuple");
            }
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
        /// <return>True if the <paramref name="continuation"/> block is used, False otherwise.</return>
        private bool ProcessBlock(BasicBlock block, QsScope scope, BasicBlock continuation)
        {
            this.SharedState.ScopeMgr.OpenScope();
            this.SharedState.SetCurrentBlock(block);
            this.Transformation.Statements.OnScope(scope);
            var isTerminated = this.SharedState.CurrentBlock?.Terminator != null;
            this.SharedState.ScopeMgr.CloseScope(isTerminated);
            if (!isTerminated)
            {
                this.SharedState.CurrentBuilder.Branch(continuation);
                return true;
            }

            return false;
        }

        /* public overrides */

        public override QsStatementKind OnQubitScope(QsQubitScope stm)
        {
            void ProcessQubitBinding(QsBinding<ResolvedInitializer> binding)
            {
                ResolvedType qubitType = ResolvedType.New(ResolvedTypeKind.Qubit);
                IrFunction allocateQubit = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.QubitAllocate);
                IrFunction allocateQubitArray = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.QubitAllocateArray);

                IValue AllocateQubit(Action<IValue> afterAlloc)
                {
                    var qubit = this.SharedState.Values.From(
                        this.SharedState.CurrentBuilder.Call(allocateQubit),
                        qubitType);
                    afterAlloc(qubit);
                    return qubit;
                }

                IValue Allocate(ResolvedInitializer init)
                {
                    if (init.Resolution.IsSingleQubitAllocation)
                    {
                        return AllocateQubit(this.SharedState.ScopeMgr.RegisterAllocatedQubits);
                    }
                    else if (init.Resolution is ResolvedInitializerKind.QubitRegisterAllocation reg)
                    {
                        // There may be benefits to allocating a register as a whole,
                        // so we only allocate them one by one when this is preferable (the case for certain QIR profiles).
                        Value size = this.SharedState.EvaluateSubexpression(reg.Item).Value;
                        if (this.SharedState.TargetQirProfile && QirValues.AsConstantUInt32(size) is uint count)
                        {
                            var qubits = Enumerable.Repeat(
                                this.SharedState.ScopeMgr.RegisterAllocatedQubits,
                                (int)count).Select(AllocateQubit).ToArray();
                            return this.SharedState.Values.CreateArray(qubitType, qubits, allocOnStack: true);
                        }
                        else
                        {
                            Value allocation = this.SharedState.CurrentBuilder.Call(allocateQubitArray, size);
                            var array = this.SharedState.Values.From(allocation, init.Type);
                            this.SharedState.ScopeMgr.RegisterAllocatedQubits(array);
                            return array;
                        }
                    }
                    else if (init.Resolution is ResolvedInitializerKind.QubitTupleAllocation inits)
                    {
                        var items = inits.Item.Select(Allocate).ToArray();

                        // Todo: In principle it would be reasonable to always stack allocate the tuple
                        // (given that it is immutable and the qubits it contains must not escape the scope).
                        // However, this will currently cause issues when this tuple is then passed as an
                        // argument to a callable. We hence chose to stack allocate based on target for now,
                        // but this would be good to revise.
                        return this.SharedState.Values.CreateTuple(items, allocOnStack: this.SharedState.TargetQirProfile);
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
                            this.SharedState.ScopeMgr.RegisterVariable(v.Item, Allocate(itemInit), fromLocalId: null);
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

            this.SharedState.ScopeMgr.OpenScope();
            ProcessQubitBinding(stm.Binding); // Apply the bindings and add them to the scope
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
            var contBlock = this.SharedState.AddBlockAfterCurrent("continue");
            var (contBlockUsed, skipRest) = (false, false);

            // if/then/elif...else
            // The first test goes in the current block. If it succeeds, we go to the "then0" block.
            // If it fails, we go to the "test1" block, where the second test is made.
            // If the second test succeeds, we go to the "then1" block, otherwise to the "test2" block, etc.
            // If all tests fail, we go to the "else" block if there's a default block, or to the
            // continuation block if not.
            for (int n = 0; n < clauses.Length && !skipRest; n++)
            {
                // Evaluate the test, which should be a Boolean at this point, and create the branch.
                // Note that we need to start the branching and create a scope for the condition
                // to ensure that anything created as part of the condition is released as part of the condition,
                // and the caching properly detects that things inside the condition are no longer accessible.
                this.SharedState.StartBranch();
                this.SharedState.ScopeMgr.OpenScope();
                var testValue = this.SharedState.EvaluateSubexpression(clauses[n].Item1).Value;
                this.SharedState.ScopeMgr.CloseScope(false);

                if (!(QirValues.AsConstantUInt32(testValue) == 0))
                {
                    // If this is an intermediate clause, then the next block if the test fails
                    // is the next clause's test block.
                    // If this is the last clause, then the next block is the default clause's block
                    // if there is one, or the continue block if not.
                    var conditionalBlock =
                        this.SharedState.CurrentFunction.InsertBasicBlock(
                            this.SharedState.BlockName($"then{n}"), contBlock);
                    skipRest = (QirValues.AsConstantUInt32(testValue) & 1) == 1;
                    var nextConditional = n < clauses.Length - 1 && !skipRest
                        ? this.SharedState.CurrentFunction.InsertBasicBlock(this.SharedState.BlockName($"test{n + 1}"), contBlock)
                        : (stm.Default.IsNull || skipRest
                            ? contBlock
                            : this.SharedState.CurrentFunction.InsertBasicBlock(this.SharedState.BlockName($"else"), contBlock));
                    contBlockUsed = contBlockUsed || nextConditional == contBlock;

                    this.SharedState.CurrentBuilder.Branch(testValue, conditionalBlock, nextConditional);
                    contBlockUsed = this.ProcessBlock(conditionalBlock, clauses[n].Item2.Body, contBlock) || contBlockUsed;
                    this.SharedState.SetCurrentBlock(nextConditional);
                }

                this.SharedState.EndBranch();
            }

            // Deal with the default, if there is any
            if (stm.Default.IsValue && !skipRest)
            {
                this.SharedState.StartBranch();
                contBlockUsed = this.ProcessBlock(this.SharedState.CurrentBlock, stm.Default.Item.Body, contBlock) || contBlockUsed;
                this.SharedState.EndBranch();
            }
            else if (this.SharedState.CurrentBlock.Terminator is null && this.SharedState.CurrentBlock != contBlock)
            {
                // If the evaluation of the first condition is inconclusive,
                // all subsequent conditions are known to be false, and there is no else defined,
                // then we can end up with a test block that is unpopulated.
                this.SharedState.CurrentBuilder.Branch(contBlock);
                this.SharedState.SetCurrentBlock(contBlock);
                contBlockUsed = true;
            }

            var currentBlock = this.SharedState.CurrentBlock;
            this.SharedState.SetCurrentBlock(contBlock);
            if (!contBlockUsed)
            {
                this.SharedState.CurrentBuilder.Unreachable();
                this.SharedState.SetCurrentBlock(currentBlock);
            }

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

            // FIXME: this is a not so clean solution;
            // update the scope manager to allow exiting an inlined function,
            // and update the data structures to allow managed items in stack allocated ones.
            if (!this.SharedState.TargetQirProfile)
            {
                // Release any resources (qubits or memory) before we fail.
                this.SharedState.ScopeMgr.ExitFunction(message);
                var fail = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.Fail);
                this.SharedState.CurrentBuilder.Call(fail, message.Value);
            }

            this.SharedState.CurrentBuilder.Unreachable();
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

            Action<T> LoopBody<T>(Action<T> execute) => loopVariable =>
            {
                this.SharedState.ScopeMgr.OpenScope();
                execute(loopVariable);
                var isTerminated = this.SharedState.CurrentBlock?.Terminator != null;
                this.SharedState.ScopeMgr.CloseScope(isTerminated);
            };

            QirExpressionKindTransformation.AccessViaLocalId(stm.IterationValues, out var accessedViaLocal);
            if (stm.IterationValues.ResolvedType.Resolution.IsRange)
            {
                void ExecuteBody(Value loopVariable)
                {
                    // If we iterate through a range, we don't inject an additional binding for the loop variable
                    // at the beginning of the body and instead directly register the iteration value under that name.
                    // We don't need to register a name if e.g. the loop variable is discarded.
                    string? loopVarName = stm.LoopItem.Item1 is SymbolTuple.VariableName name ? name.Item : null;
                    var variableValue = this.SharedState.Values.FromSimpleValue(loopVariable, ResolvedType.New(ResolvedTypeKind.Int));
                    if (loopVarName != null)
                    {
                        this.SharedState.ScopeMgr.RegisterVariable(loopVarName, variableValue, fromLocalId: accessedViaLocal);
                    }

                    this.Transformation.Statements.OnScope(stm.Body);
                }

                var (start, step, end) = this.SharedState.Functions.RangeItems(stm.IterationValues);
                this.SharedState.IterateThroughRange(start, step, end, LoopBody<Value>(ExecuteBody));
            }
            else if (stm.IterationValues.ResolvedType.Resolution.IsArrayType)
            {
                void ExecuteBody(IValue arrayItem)
                {
                    // If we iterate through an array, we inject a binding at the beginning of the body.
                    this.BindSymbolTuple(stm.LoopItem.Item1, arrayItem, (varName, value) =>
                        this.SharedState.ScopeMgr.RegisterVariable(varName, value, fromLocalId: accessedViaLocal));
                    this.Transformation.Statements.OnScope(stm.Body);
                }

                var array = (ArrayValue)this.SharedState.EvaluateSubexpression(stm.IterationValues);
                this.SharedState.IterateThroughArray(array, LoopBody<IValue>(ExecuteBody));
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
            var repeatBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.BlockName("repeat"));
            var testBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.BlockName("until"));
            var fixupBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.BlockName("fixup"));
            var contBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.BlockName("rend"));

            this.SharedState.ExecuteLoop(() =>
            {
                this.SharedState.ScopeMgr.OpenScope();
                this.SharedState.CurrentBuilder.Branch(repeatBlock);
                this.SharedState.SetCurrentBlock(repeatBlock);
                this.Transformation.Statements.OnScope(stm.RepeatBlock.Body);
                if (this.SharedState.CurrentBlock?.Terminator == null)
                {
                    this.SharedState.CurrentBuilder.Branch(testBlock);
                }

                this.SharedState.SetCurrentBlock(testBlock);
                var test = this.SharedState.EvaluateSubexpression(stm.SuccessCondition).Value;
                this.SharedState.ScopeMgr.ApplyPendingReferences();
                this.SharedState.CurrentBuilder.Branch(test, contBlock, fixupBlock);

                // We have a do-while pattern here, and the repeat block will be executed one more time than the fixup.
                // We need to make sure to properly invoke all calls to unreference, release, and remove alias counts
                // for variables and values in the repeat-block after the statement ends.
                this.SharedState.SetCurrentBlock(contBlock);
                this.SharedState.ScopeMgr.ExitScope();
                contBlock = this.SharedState.CurrentBlock;

                this.SharedState.SetCurrentBlock(fixupBlock);
                this.Transformation.Statements.OnScope(stm.FixupBlock.Body);
                var isTerminated = this.SharedState.CurrentBlock?.Terminator != null;
                this.SharedState.ScopeMgr.CloseScope(isTerminated);
                if (!isTerminated)
                {
                    this.SharedState.CurrentBuilder.Branch(repeatBlock);
                }
            });

            this.SharedState.SetCurrentBlock(contBlock);
            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnReturnStatement(TypedExpression ex)
        {
            var result = this.SharedState.EvaluateSubexpression(ex);
            this.SharedState.AddReturn(result, ex.ResolvedType.Resolution.IsUnitType);
            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnValueUpdate(QsValueUpdate stm)
        {
            var symbols = SyntaxGenerator.ExpressionAsSymbolTuple(stm.Lhs);
            if (stm.Rhs.Expression is ResolvedExpressionKind.CopyAndUpdate ex
                && ex.Item1.Expression is ResolvedExpressionKind.Identifier id
                && id.Item1 is Identifier.LocalVariable varName
                && symbols is SymbolTuple.VariableName symName
                && symName.Item == varName.Item)
            {
                // For copy-and-reassign statements we want to make sure that the alias count is reduced
                // before evaluating the copy-and-update expression, such that in the case where the variable
                // that is reassigned is the only handle that has access to the original value, the copy is
                // omitted. We can omit that alias count manipulation for inner items, since besides the
                // items that are updated, all counts will remain the same and while also doing the same for
                // inner items could avoid copies in rare edge cases it is not worth the increased cost for
                // the majority of cases. For the items that are updated, we need to make sure that the alias
                // count of the old item is decreased and the one of the new item is increased. CopyAndUpdate
                // takes care of that when updateItemAliasCount is set to true.
                var pointer = (PointerValue)this.SharedState.ScopeMgr.GetVariable(varName.Item);
                this.SharedState.ScopeMgr.UnassignFromMutable(pointer, shallow: true);

                QirExpressionKindTransformation.CopyAndUpdate(
                    this.SharedState,
                    ((varName.Item, pointer.LoadValue()), ex.Item2, ex.Item3),
                    updateItemAliasCount: true);
                var value = this.SharedState.ValueStack.Pop();

                this.SharedState.ScopeMgr.AssignToMutable(value, fromLocalId: null, shallow: true);
                pointer.StoreValue(value);
            }
            else
            {
                Action<string, IValue> RebindVariable(TypedExpression boundEx) =>
                    (string varName, IValue value) =>
                    {
                        var pointer = (PointerValue)this.SharedState.ScopeMgr.GetVariable(varName);
                        QirExpressionKindTransformation.AccessViaLocalId(boundEx, out var fromLocalId);
                        this.SharedState.ScopeMgr.AssignToMutable(value, fromLocalId: fromLocalId);
                        this.SharedState.ScopeMgr.UnassignFromMutable(pointer);
                        pointer.StoreValue(value);
                    };

                this.BindSymbolTuple(symbols, stm.Rhs, (syms, boundEx) =>
                    this.BindSymbolTuple(syms, this.SharedState.EvaluateSubexpression(boundEx), RebindVariable(boundEx)));
            }

            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnVariableDeclaration(QsBinding<TypedExpression> stm)
        {
            Action<string, IValue> BindVariable(TypedExpression ex) =>
                (string varName, IValue value) =>
                {
                    if (stm.Kind.IsMutableBinding)
                    {
                        value = this.SharedState.Values.CreatePointer(value);
                    }

                    QirExpressionKindTransformation.AccessViaLocalId(ex, out var localId);
                    this.SharedState.ScopeMgr.RegisterVariable(varName, value, fromLocalId: localId);
                };

            this.BindSymbolTuple(stm.Lhs, stm.Rhs, (syms, boundEx) =>
                this.BindSymbolTuple(syms, this.SharedState.EvaluateSubexpression(boundEx), BindVariable(boundEx)));
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
            var testBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.BlockName("while"));
            var bodyBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.BlockName("do"));
            var contBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.BlockName("wend"));

            this.SharedState.ExecuteLoop(() =>
            {
                this.SharedState.ScopeMgr.OpenScope();
                this.SharedState.CurrentBuilder.Branch(testBlock);
                this.SharedState.SetCurrentBlock(testBlock);

                var test = this.SharedState.EvaluateSubexpression(stm.Condition).Value;
                this.SharedState.ScopeMgr.CloseScope(this.SharedState.CurrentBlock?.Terminator != null);
                this.SharedState.CurrentBuilder.Branch(test, bodyBlock, contBlock);
                this.ProcessBlock(bodyBlock, stm.Body, testBlock);
            });

            this.SharedState.SetCurrentBlock(contBlock);
            return QsStatementKind.EmptyStatement;
        }
    }
}
