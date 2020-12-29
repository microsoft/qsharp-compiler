// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.QIR;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR.Emission
{
    internal class TupleValue
    {
        private readonly GenerationContext context;
        private readonly InstructionBuilder builder;

        private Value? opaquePointer;
        private Value? typedPointer;

        public readonly IStructType StructType;

        internal Value OpaquePointer
        {
            get
            {
                this.opaquePointer ??= this.builder.BitCast(this.TypedPointer, this.context.Types.Tuple);
                return this.opaquePointer;
            }
        }

        internal Value TypedPointer
        {
            get
            {
                if (this.opaquePointer == null && this.typedPointer == null)
                {
                    var size = this.context.ComputeSizeForType(this.StructType, this.builder);
                    this.opaquePointer = this.builder.Call(this.context.GetOrCreateRuntimeFunction(RuntimeLibrary.TupleCreate), size);
                }
                this.typedPointer ??= this.builder.BitCast(this.OpaquePointer, this.StructType.CreatePointerType());
                return this.typedPointer;
            }
        }

        internal TupleValue(IStructType tupleType, GenerationContext context, InstructionBuilder? builder)
        {
            this.context = context;
            this.builder = builder ?? this.context.CurrentBuilder;
            this.StructType = tupleType;
        }
    }
}
