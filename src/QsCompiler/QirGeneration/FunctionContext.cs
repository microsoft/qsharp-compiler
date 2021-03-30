using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    internal class FunctionContext
    {
        private readonly IrFunction function;
        private readonly Func<string, string> uniqueBlockName;
        private InstructionBuilder currentBuilder;
        private InstructionBuilder? activelyEmittingTo = null;

        internal IrFunction Function => this.function;

        internal BasicBlock CurrentBlock => this.currentBuilder.InsertBlock;

        internal bool IsCurrentBlockTerminated => this.CurrentBlock.Terminator != null;

        internal bool IsCurrentBlockEmpty => !this.CurrentBlock.Instructions.Any();

        internal FunctionContext(IrFunction function, Func<string, string> uniqueBlockName)
        {
            this.function = function;
            this.uniqueBlockName = uniqueBlockName;
            this.currentBuilder = new InstructionBuilder(this.function.AppendBasicBlock("entry"));
        }

        internal void Emit(Action<InstructionBuilder> action)
        {
            this.Emit(builder =>
            {
                action(builder);
                return true;
            });
        }

        internal T Emit<T>(Func<InstructionBuilder, T> func)
        {
            if (this.activelyEmittingTo != null)
            {
                return func(this.activelyEmittingTo);
            }

            try
            {
                T result;
                if (this.IsCurrentBlockTerminated)
                {
                    // Current block has already been terminated.
                    // Create a new block to hold any unreachable instructions emitted.
                    var unreachableBlock = this.AddBlockAfterCurrent($"{this.CurrentBlock.Name}__unreachable");
                    var unreachableBuilder = new InstructionBuilder(unreachableBlock);

                    this.activelyEmittingTo = unreachableBuilder;
                    result = func(unreachableBuilder);

                    if (!unreachableBlock.Instructions.Any())
                    {
                        // No instructions were emitted by the action. Prune empty unreachable block.
                        this.RemoveBlock(unreachableBlock);
                    }
                    else
                    {
                        this.SetCurrentBlock(unreachableBlock);
                    }
                }
                else
                {
                    this.activelyEmittingTo = this.currentBuilder;
                    result = func(this.currentBuilder);
                }

                return result;
            }
            finally
            {
                this.activelyEmittingTo = null;
            }
        }

        /// <summary>
        /// Creates a new basic block and adds it to the current function immediately after the current block.
        /// </summary>
        /// <param name="name">The base name for the new block; a counter will be appended to ensure uniqueness</param>
        /// <returns>The new block.</returns>
        /// <exception cref="InvalidOperationException">The current function is set to null.</exception>
        internal BasicBlock AddBlockAfterCurrent(string name)
        {
            var flag = false;
            BasicBlock? next = null;
            foreach (var block in this.function.BasicBlocks)
            {
                if (flag)
                {
                    next = block;
                    break;
                }

                if (block == this.CurrentBlock)
                {
                    flag = true;
                }
            }

            var continueName = this.uniqueBlockName(name);
            return next == null
                ? this.function.AppendBasicBlock(continueName)
                : this.function.InsertBasicBlock(continueName, next);
        }

        /// <summary>
        /// Makes the given basic block current, creates a new builder for it, and makes that builder current.
        /// This method does not check to make sure that the block isn't already current.
        /// </summary>
        /// <remarks>
        /// Note that if the current block has no instructions, it is pruned from the current function.
        /// </remarks>
        /// <param name="b">The block to make current</param>
        internal void SetCurrentBlock(BasicBlock b)
        {
            this.AssertNotEmitting(nameof(this.SetCurrentBlock));

            // TODO: validate that b is inside this function.
            this.currentBuilder = new InstructionBuilder(b);
        }

        internal bool RemoveBlock(BasicBlock b)
        {
            this.AssertNotEmitting(nameof(this.RemoveBlock));
            return this.function.BasicBlocks.Remove(b);
        }

        private void AssertNotEmitting(string operation)
        {
            if (this.activelyEmittingTo != null)
            {
                throw new InvalidOperationException(
                    $"Call to {nameof(FunctionContext)}.{operation} not valid from inside {nameof(this.Emit)}");
            }
        }
    }
}
