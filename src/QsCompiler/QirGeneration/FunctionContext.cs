// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    /// <summary>
    /// Encapsulates basic block code generation for a function
    /// currently being built by the QIR generator.
    /// </summary>
    internal class FunctionContext
    {
        private readonly IrFunction function;
        private readonly Func<string, string> uniqueBlockName;
        private InstructionBuilder currentBuilder;
        private InstructionBuilder? activelyEmittingTo = null;

        /// <summary>
        /// Gets the underlying function.
        /// </summary>
        internal IrFunction Function => this.function;

        /// <summary>
        /// Gets the current block to which instructions are being emitted.
        /// </summary>
        internal BasicBlock CurrentBlock => this.currentBuilder.InsertBlock;

        /// <summary>
        /// Indicates whether the current block is terminated.
        /// </summary>
        internal bool IsCurrentBlockTerminated => this.CurrentBlock.Terminator != null;

        /// <summary>
        /// Indicates whether the current block contains instuctions.
        /// </summary>
        internal bool IsCurrentBlockEmpty => !this.CurrentBlock.Instructions.Any();

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionContext"/> class.
        /// </summary>
        /// <param name="function">The current function that the QIR generator is processing.</param>
        /// <param name="uniqueLocalName">A function used to generate unique local names within the function.</param>
        internal FunctionContext(IrFunction function, Func<string, string> uniqueLocalName)
        {
            this.function = function;
            this.uniqueBlockName = uniqueLocalName;
            this.currentBuilder = new InstructionBuilder(this.function.AppendBasicBlock("entry"));
        }

        /// <inheritdoc cref="Emit{T}(Func{InstructionBuilder, T})"/>
        internal void Emit(Action<InstructionBuilder> action)
        {
            this.Emit(builder =>
            {
                action(builder);
                return true;
            });
        }

        /// <summary>
        /// Provides brokered access to the current block's <see cref="InstructionBuilder"/>,
        /// ensuring that emitted code does not result in an invalid block.
        /// </summary>
        /// <remarks>
        /// If the current block ends with a <see cref="Terminator"/>, a new (unreachable) block is
        /// prepared, and <paramref name="func"/> is invoked with its builder. If instructions
        /// are emitted, the unreachable block becomes the current block. Otherwise, the empty
        /// unreachable block is removed.
        /// <para/>
        /// Note that nested calls to <see cref="Emit"/> are valid. In this case, validation is only
        /// performed when the root call to <paramref name="func"/> returns.
        /// I.e. Inner calls to <see cref="Emit"/> invoke their <paramref name="func"/> with
        /// the same builder being used by that of the most outter.
        /// </remarks>
        /// <typeparam name="T">The return type of <paramref name="func"/>.</typeparam>
        /// <param name="func">The function.</param>
        /// <returns>The return value of <paramref name="func"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// The call to <paramref name="func"/> emitted an instruction sequence containing
        /// a <see cref="Terminator"/> that is not the last instruction.
        /// </exception>
        internal T Emit<T>(Func<InstructionBuilder, T> func)
        {
            if (this.activelyEmittingTo != null)
            {
                return func(this.activelyEmittingTo);
            }

            try
            {
                T result;
                var currentInstCount = 0;

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
                        this.currentBuilder = unreachableBuilder;
                    }
                }
                else
                {
                    currentInstCount = this.CurrentBlock.Instructions.Count();
                    this.activelyEmittingTo = this.currentBuilder;
                    result = func(this.currentBuilder);
                }

                // Validate that the new instruction sequence does not contain
                // terminators before the last instruction.
                var newInstructions = this.CurrentBlock.Instructions.Skip(currentInstCount);
                foreach (var inst in newInstructions.SkipLast(1))
                {
                    if (inst is Terminator)
                    {
                        throw new InvalidOperationException($"Call to {nameof(this.Emit)} results in an invalid block.");
                    }
                }

                return result;
            }
            finally
            {
                this.activelyEmittingTo = null;
            }
        }

        /// <summary>
        /// Creates a new basic block and adds it to the function immediately after the current block.
        /// </summary>
        /// <param name="name">The base name for the new block; a counter will be appended to ensure uniqueness</param>
        /// <returns>The new block.</returns>
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
        /// Creates a new basic block and adds it to the end of the function.
        /// </summary>
        /// <param name="name">The base name for the new block; a counter will be appended to ensure uniqueness</param>
        /// <returns>The new block.</returns>
        internal BasicBlock AppendBlock(string name)
        {
            return this.Function.AppendBasicBlock(this.uniqueBlockName(name));
        }

        /// <summary>
        /// Makes the given basic block current, creates a new builder for it, and makes that builder current.
        /// This method does not check to make sure that the block isn't already current.
        /// </summary>
        /// <param name="block">The block to make current</param>
        /// <exception cref="InvalidOperationException">Function must contain block.</exception>
        /// <exception cref="InvalidOperationException">Call not valid from inside <see cref="Emit"/>.</exception>
        internal void SetCurrentBlock(BasicBlock block)
        {
            this.AssertNotEmitting(nameof(this.SetCurrentBlock));

            if (!this.Function.BasicBlocks.Contains(block))
            {
                throw new InvalidOperationException($"Function must contain block.");
            }

            this.currentBuilder = new InstructionBuilder(block);
        }

        /// <summary>
        /// Removes <paramref name="block"/> from the function.
        /// </summary>
        /// <param name="block">The block to remove.</param>
        /// <returns>True if the block existed and was removed, false otherwise.</returns>
        internal bool RemoveBlock(BasicBlock block)
        {
            return this.function.BasicBlocks.Remove(block);
        }

        /// <summary>
        /// Raises an exception if called inside a call to <see cref="Emit"/>.
        /// </summary>
        /// <param name="invalidOp">The name of the invalid function called.</param>
        /// <exception cref="InvalidOperationException">Call not valid from inside <see cref="Emit"/>.</exception>
        private void AssertNotEmitting(string invalidOp)
        {
            if (this.activelyEmittingTo != null)
            {
                throw new InvalidOperationException(
                    $"Call to {nameof(FunctionContext)}.{invalidOp} not valid from inside {nameof(this.Emit)}");
            }
        }
    }
}
