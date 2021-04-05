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
        private readonly Func<string, string> blockNameAllocate;
        private readonly Dictionary<string, string> unreachableToOriginalName;

        private InstructionBuilder currentBuilder;
        private InstructionBuilder? activelyEmittingTo = null;

        /// <summary>
        /// Gets the underlying function.
        /// </summary>
        internal IrFunction Function => this.function;

        /// <summary>
        /// Gets the current block to which instructions are being emitted.
        /// </summary>
        internal BasicBlock CurrentBlock => this.currentBuilder.InsertBlock!;

        /// <summary>
        /// Indicates whether the current block is terminated.
        /// </summary>
        internal bool IsCurrentBlockTerminated => this.CurrentBlock.Terminator != null;

        /// <summary>
        /// Indicates whether the current block contains instuctions.
        /// </summary>
        internal bool IsCurrentBlockEmpty => !this.CurrentBlock.Instructions.Any();

        /// <summary>
        /// Construsts a function context, automatically creating a new basic block
        /// called "entry".
        /// </summary>
        /// <param name="function">The current function that the QIR generator is processing.</param>
        /// <param name="blockNameAllocate">A function used to allocate unique local names within the function.</param>
        internal FunctionContext(IrFunction function, Func<string, string> blockNameAllocate)
        {
            this.function = function;
            this.blockNameAllocate = blockNameAllocate;
            this.currentBuilder = new InstructionBuilder(this.function.AppendBasicBlock("entry"));
            this.unreachableToOriginalName = new Dictionary<string, string>();
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

            // Reserve a new unique name for an unreachable block based on
            // the name of the first preceeding reachable block.
            string ReserveUnreachableBlockName()
            {
                string originalBlockName;
                if (!this.unreachableToOriginalName.TryGetValue(this.CurrentBlock.Name, out originalBlockName))
                {
                    originalBlockName = this.CurrentBlock.Name;
                }

                var blockNameBase = $"{originalBlockName}__unreachable";
                var uniqueBlockName = this.blockNameAllocate(blockNameBase);

                this.unreachableToOriginalName.Add(uniqueBlockName, originalBlockName);
                return uniqueBlockName;
            }

            try
            {
                T result;
                var currentInstCount = 0;

                if (this.IsCurrentBlockTerminated)
                {
                    // Current block has already been terminated.
                    // Create a new block to hold any unreachable instructions emitted.
                    // If no instructions are emitted by func(), we will delete this block.
                    // For this reason, we do not reserve a unique block name until we know
                    // we'll keep the block.
                    var unreachableBlock = this.AddBlockAfterCurrent("temp__unreachable", blockNameAllocate: null);
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
                        // Keep the unreachable block, and allocate a unique name for it.
                        unreachableBlock.Name = ReserveUnreachableBlockName();
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
        /// <param name="name">The base name for the new block; a counter will be appended to ensure uniqueness.</param>
        /// <returns>The new block.</returns>
        internal BasicBlock AddBlockAfterCurrent(string name)
            => this.AddBlockAfterCurrent(name, blockNameAllocate: this.blockNameAllocate);

        /// <summary>
        /// Creates a new basic block and adds it to the end of the function.
        /// </summary>
        /// <param name="name">The base name for the new block; a counter will be appended to ensure uniqueness</param>
        /// <returns>The new block.</returns>
        internal BasicBlock AppendBlock(string name)
        {
            return this.Function.AppendBasicBlock(this.blockNameAllocate(name));
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
        /// Creates a new basic block and adds it to the function immediately after the current block.
        /// </summary>
        /// <param name="name">The base name for the new block.</param>
        /// <param name="blockNameAllocate">
        /// If provided, used to allocate a new unique name based on <paramref name="name"/>.
        /// </param>
        /// <returns>The new block.</returns>
        private BasicBlock AddBlockAfterCurrent(string name, Func<string, string>? blockNameAllocate)
        {
            BasicBlock? next = this.function.BasicBlocks.SkipWhile(b => b != this.CurrentBlock).Skip(1).FirstOrDefault();

            var continueName = blockNameAllocate != null ? blockNameAllocate(name) : name;
            return next == null
                ? this.function.AppendBasicBlock(continueName)
                : this.function.InsertBasicBlock(continueName, next);
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
