// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
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
        private void BindSymbolTuple(SymbolTuple symbols, IValue ex, ResolvedType type, bool mutable = false)
        {
            if (symbols is SymbolTuple.VariableName varName)
            {
                if (mutable)
                {
                    var ptr = this.SharedState.Values.CreatePointer(type);
                    this.SharedState.CurrentBuilder.Store(ex.Value, ptr.Pointer);
                    this.SharedState.ScopeMgr.RegisterVariable(varName.Item, ptr);
                }
                else
                {
                    this.SharedState.ScopeMgr.RegisterVariable(varName.Item, ex);
                }
            }
            else if (symbols is SymbolTuple.VariableNameTuple syms)
            {
                if (!(type.Resolution is QsResolvedTypeKind.TupleType types) || syms.Item.Length != types.Item.Length)
                {
                    throw new InvalidOperationException("shape mismatch in symbol binding");
                }

                var tuple = (TupleValue)ex;
                for (int i = 0; i < syms.Item.Length; i++)
                {
                    if (!syms.Item[i].IsDiscardedItem && !syms.Item[i].IsInvalidItem)
                    {
                        var itemValue = tuple.GetTupleElement(i);
                        this.BindSymbolTuple(syms.Item[i], itemValue, types.Item[i], mutable);
                    }
                }
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

        // public overrides

        public override QsStatementKind OnQubitScope(QsQubitScope stm)
        {
            void ProcessQubitBinding(QsBinding<ResolvedInitializer> binding)
            {
                ResolvedType qubitType = ResolvedType.New(QsResolvedTypeKind.Qubit);
                IrFunction allocateOne = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.QubitAllocate);
                IrFunction allocateArray = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.QubitAllocateArray);

                IValue Allocate(ResolvedInitializer init)
                {
                    if (init.Resolution.IsSingleQubitAllocation)
                    {
                        Value allocation = this.SharedState.CurrentBuilder.Call(allocateOne);
                        var value = this.SharedState.Values.From(allocation, init.Type);
                        this.SharedState.ScopeMgr.RegisterAllocatedQubits(value);
                        return value;
                    }
                    else if (init.Resolution is ResolvedInitializerKind.QubitRegisterAllocation reg)
                    {
                        Value countValue = this.SharedState.EvaluateSubexpression(reg.Item).Value;
                        Value allocation = this.SharedState.CurrentBuilder.Call(allocateArray, countValue);
                        var value = this.SharedState.Values.From(allocation, init.Type);
                        this.SharedState.ScopeMgr.RegisterAllocatedQubits(value);
                        return value;
                    }
                    else if (init.Resolution is ResolvedInitializerKind.QubitTupleAllocation inits)
                    {
                        var items = inits.Item.Select(Allocate).ToArray();
                        return this.SharedState.Values.CreateTuple(items);
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
                            this.SharedState.ScopeMgr.RegisterVariable(v.Item, Allocate(itemInit));
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
                var testValue = this.SharedState.EvaluateSubexpression(test).Value;
                var conditionalBlock = this.SharedState.CurrentFunction.InsertBasicBlock(
                            this.SharedState.GenerateUniqueName($"then{n}"), contBlock);

                // If this is an intermediate clause, then the next block if the test fails
                // is the next clause's test block.
                // If this is the last clause, then the next block is the default clause's block
                // if there is one, or the continue block if not.
                var nextConditional = n < clauses.Length - 1
                    ? this.SharedState.CurrentFunction.InsertBasicBlock(this.SharedState.GenerateUniqueName($"test{n + 1}"), contBlock)
                    : (stm.Default.IsNull
                        ? contBlock
                        : this.SharedState.CurrentFunction.InsertBasicBlock(
                                this.SharedState.GenerateUniqueName($"else"), contBlock));

                // Create the branch
                this.SharedState.CurrentBuilder.Branch(testValue, conditionalBlock, nextConditional);

                // Get a builder for the then block, make it current, and then process the block
                this.ProcessBlock(conditionalBlock, clauses[n].Item2.Body, contBlock);

                this.SharedState.SetCurrentBlock(nextConditional);
            }

            // Deal with the default, if there is any
            if (stm.Default.IsValue)
            {
                this.ProcessBlock(this.SharedState.CurrentBlock, stm.Default.Item.Body, contBlock);
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
            this.SharedState.ScopeMgr.ExitFunction(message);
            var fail = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.Fail);
            this.SharedState.CurrentBuilder.Call(fail, message.Value);
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

            if (stm.IterationValues.ResolvedType.Resolution.IsRange)
            {
                void ExecuteBody(Value loopVariable)
                {
                    // If we iterate through a range, we don't inject an additional binding for the loop variable
                    // at the beginning of the body and instead directly register the iteration value under that name.
                    var loopVarName = stm.LoopItem.Item1 is SymbolTuple.VariableName name
                        ? name.Item
                        : throw new ArgumentException("invalid loop variable name");
                    var variableValue = this.SharedState.Values.From(loopVariable, ResolvedType.New(QsResolvedTypeKind.Int));
                    this.SharedState.ScopeMgr.RegisterVariable(loopVarName, variableValue);
                    this.Transformation.Statements.OnScope(stm.Body);
                }

                var (getStart, getStep, getEnd) = QirExpressionKindTransformation.RangeItems(this.SharedState, stm.IterationValues);
                this.SharedState.IterateThroughRange(getStart(), getStep(), getEnd(), ExecuteBody);
            }
            else if (stm.IterationValues.ResolvedType.Resolution.IsArrayType)
            {
                void ExecuteBody(IValue arrayItem)
                {
                    // If we iterate through an array, we inject a binding at the beginning of the body.
                    this.BindSymbolTuple(stm.LoopItem.Item1, arrayItem, stm.LoopItem.Item2);
                    this.Transformation.Statements.OnScope(stm.Body);
                }

                var array = (ArrayValue)this.SharedState.EvaluateSubexpression(stm.IterationValues);
                this.SharedState.IterateThroughArray(array, ExecuteBody);
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

            var repeatBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("repeat"));
            var testBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("until"));
            var fixupBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("fixup"));
            var contBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("rend"));

            this.SharedState.CurrentBuilder.Branch(repeatBlock);

            this.SharedState.SetCurrentBlock(repeatBlock);
            this.SharedState.ScopeMgr.OpenScope();
            this.Transformation.Statements.OnScope(stm.RepeatBlock.Body);
            if (this.SharedState.CurrentBlock?.Terminator == null)
            {
                this.SharedState.CurrentBuilder.Branch(testBlock);

                // We have a do-while pattern here, and the repeat block will be executed one more time than the fixup.
                // We need to make sure to properly invoke all calls to unreference, release, and remove access counts
                // for variables and values in the repeat-block after the statement ends.
                this.SharedState.SetCurrentBlock(contBlock);
                this.SharedState.ScopeMgr.ExitScope(false);
            }

            this.SharedState.SetCurrentBlock(testBlock);
            var test = this.SharedState.EvaluateSubexpression(stm.SuccessCondition).Value;
            this.SharedState.CurrentBuilder.Branch(test, contBlock, fixupBlock);

            this.SharedState.SetCurrentBlock(fixupBlock);
            this.Transformation.Statements.OnScope(stm.FixupBlock.Body);
            var isTerminated = this.SharedState.CurrentBlock?.Terminator != null;
            this.SharedState.ScopeMgr.CloseScope(isTerminated);
            if (!isTerminated)
            {
                this.SharedState.CurrentBuilder.Branch(repeatBlock);
            }

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
            // Update an item, which might be a single symbol or a tuple, to a new Value
            void UpdateItem(TypedExpression symbols, IValue rhs)
            {
                if (symbols.Expression is ResolvedExpression.Identifier id && id.Item1 is Identifier.LocalVariable varName)
                {
                    var originalValue = (PointerValue)this.SharedState.ScopeMgr.GetVariable(varName.Item);
                    this.SharedState.ScopeMgr.DecreaseAccessCount(originalValue);
                    this.SharedState.CurrentBuilder.Store(rhs.Value, originalValue.Pointer);
                    this.SharedState.ScopeMgr.IncreaseAccessCount(rhs);
                }
                else if (symbols.Expression is ResolvedExpression.ValueTuple ids)
                {
                    var tuple = (TupleValue)rhs;
                    var tupleItems = tuple.GetTupleElements();
                    if (tupleItems.Length != ids.Item.Length)
                    {
                        throw new InvalidOperationException("shape mismatch in value update");
                    }

                    for (int i = 0; i < tupleItems.Length; i++)
                    {
                        UpdateItem(ids.Item[i], tupleItems[i]);
                    }
                }
                else
                {
                    throw new NotSupportedException("unknown expression in left-hand side of set statement");
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
            var test = this.SharedState.EvaluateSubexpression(stm.Condition).Value;
            this.SharedState.ScopeMgr.CloseScope(this.SharedState.CurrentBlock?.Terminator != null);
            this.SharedState.CurrentBuilder.Branch(test, bodyBlock, contBlock);

            this.ProcessBlock(bodyBlock, stm.Body, testBlock);
            this.SharedState.SetCurrentBlock(contBlock);
            return QsStatementKind.EmptyStatement;
        }
    }
}
