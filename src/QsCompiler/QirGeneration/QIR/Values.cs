// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR.Emission
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// Each class instance contains permits to construct QIR values for use
    /// within the compilation unit given upon instantiation.
    /// </summary>
    internal class QirValues
    {
        private readonly GenerationContext sharedState;

        internal readonly IValue Unit;

        internal QirValues(GenerationContext context, Constants constants)
        {
            this.sharedState = context;
            this.Unit = new SimpleValue(constants.UnitValue, ResolvedType.New(QsResolvedTypeKind.UnitType));
        }

        internal SimpleValue FromSimpleValue(Value value, ResolvedType type, InstructionBuilder? builder = null) =>
            new SimpleValue(value, type, builder);

        /// <summary>
        /// Creates a new tuple value from the given tuple pointer. The casts to get the opaque and typed pointer
        /// respectively are executed lazily. When needed, the instructions are emitted using the given builder.
        /// If no builder is specified, the builder defined in the context is used when a pointer is constructed.
        /// </summary>
        /// <param name="tuple">Either an opaque or a typed pointer to the tuple data structure</param>
        /// <param name="elementTypes">The Q# types of the tuple items</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal TupleValue FromTuple(Value tuple, ImmutableArray<ResolvedType> elementTypes, InstructionBuilder? builder = null) =>
            new TupleValue(tuple, elementTypes, this.sharedState, builder);

        /// <summary>
        /// Creates a new array value from the given opaque array of elements of the given type. When needed,
        /// the instructions to compute the length of the array are emitted using the given builder.
        /// If no builder is specified, the builder defined in the context is used when a pointer is constructed.
        /// </summary>
        /// <param name="array">The opaque pointer to the array data structure</param>
        /// <param name="length">Value of type i64 indicating the number of elements in the array; will be computed on demand if the given value is null</param>
        /// <param name="elementType">Q# type of the array elements</param>
        /// <param name="context">Generation context where constants are defined and generated if needed</param>
        /// <param name="builder">Builder used to construct the opaque pointer the first time it is requested</param>
        internal ArrayValue FromArray(Value value, ResolvedType elementType, InstructionBuilder? builder = null) =>
            new ArrayValue(value, null, elementType, this.sharedState, builder);

        internal CallableValue FromCallable(Value value, ResolvedType type, InstructionBuilder? builder = null) =>
            new CallableValue(value, type, builder);

        internal IValue From(Value value, ResolvedType type, InstructionBuilder? builder = null) =>
            type.Resolution is QsResolvedTypeKind.ArrayType it ? this.sharedState.Values.FromArray(value, it.Item, builder) :
            type.Resolution is QsResolvedTypeKind.TupleType ts ? this.sharedState.Values.FromTuple(value, ts.Item, builder) :
            (type.Resolution.IsOperation || type.Resolution.IsFunction) ? this.sharedState.Values.FromCallable(value, type, builder) :
            (IValue)new SimpleValue(value, type, builder);
    }
}
