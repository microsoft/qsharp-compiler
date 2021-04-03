// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Quantum.QIR.Emission;
using Microsoft.Quantum.QsCompiler.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QIR
{
    using ResolvedExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// Enum to distinguish different components that are ultimately combined
    /// to execute a program compiled into QIR.
    /// </summary>
    public enum Component
    {
        /// <summary>
        /// Contains all functions that are supported by the (classical) QIR runtime.
        /// </summary>
        RuntimeLibrary,

        /// <summary>
        /// Contains all functions that are supported by the quantum processor itself.
        /// </summary>
        QuantumInstructionSet,
    }

    internal class Functions
    {
        private static readonly ResolvedType Int = ResolvedType.New(ResolvedTypeKind.Int);
        private static readonly ResolvedType BigInt = ResolvedType.New(ResolvedTypeKind.BigInt);
        private static readonly ResolvedType Double = ResolvedType.New(ResolvedTypeKind.Double);

        private readonly GenerationContext sharedState;
        private readonly ImmutableDictionary<QsQualifiedName, Func<TypedExpression, IValue>> builtIn;

        public Functions(GenerationContext sharedState)
        {
            var dict = ImmutableDictionary.CreateBuilder<QsQualifiedName, Func<TypedExpression, IValue>>();
            dict.Add(QsCompiler.BuiltIn.Length.FullName, this.Length);
            dict.Add(QsCompiler.BuiltIn.IntAsDouble.FullName, this.IntAsDouble);
            dict.Add(QsCompiler.BuiltIn.DoubleAsInt.FullName, this.DoubleAsInt);
            dict.Add(QsCompiler.BuiltIn.IntAsBigInt.FullName, this.IntAsBigInt);
            dict.Add(QsCompiler.BuiltIn.RangeStart.FullName, this.RangeStart);
            dict.Add(QsCompiler.BuiltIn.RangeStep.FullName, this.RangeStep);
            dict.Add(QsCompiler.BuiltIn.RangeEnd.FullName, this.RangeEnd);
            dict.Add(QsCompiler.BuiltIn.RangeReverse.FullName, this.RangeReverse);
            dict.Add(QsCompiler.BuiltIn.Message.FullName, this.Message);
            dict.Add(QsCompiler.BuiltIn.Truncate.FullName, this.DoubleAsInt); // This redundancy needs to be eliminated in the Q# libraries.
            dict.Add(QsCompiler.BuiltIn.DumpMachine.FullName, this.DumpMachine);

            this.sharedState = sharedState;
            this.builtIn = dict.ToImmutable();
        }

        // static methods

        /// <summary>
        /// Generates a mangled name for a function that is expected to be provided by a component,
        /// such as QIR runtime library or the quantum instruction set, rather than defined in source code.
        /// The mangled names are a double underscore, "quantum", and another double underscore, followed by
        /// "rt" or "qis", another double underscore, and then the base name.
        /// </summary>
        /// <param name="component">The component that is expected to provide the function</param>
        /// <param name="name">The name of the function without the component prefix</param>
        /// <returns>The mangled function name</returns>
        /// <exception cref="ArgumentException">No naming convention is defined for the given component.</exception>
        public static string FunctionName(Component component, string name) => component switch
        {
            Component.RuntimeLibrary => $"__quantum__rt__{name}",
            Component.QuantumInstructionSet => $"__quantum__qis__{name}",
            _ => throw new ArgumentException("unkown software component"),
        };

        // public and internal methods

        /// <returns>
        /// True, if the callable with the given name is handled by QIR emission
        /// and does not need to be declared within QIR or implemented by the runtime.
        /// </returns>
        public bool IsBuiltIn(QsQualifiedName name) =>
            this.builtIn.ContainsKey(NameDecorator.OriginalNameFromMonomorphized(name));

        /// <returns>
        /// The result of the evaluation if the given name matches one of the recognized runtime functions,
        /// and null otherwise.
        /// </returns>
        internal bool TryEvaluate(QsQualifiedName name, TypedExpression arg, [MaybeNullWhen(false)] out IValue evaluated)
        {
            var unmangledName = NameDecorator.OriginalNameFromMonomorphized(name);
            if (this.builtIn.TryGetValue(unmangledName, out var function))
            {
                evaluated = function(arg);
                return true;
            }
            else
            {
                evaluated = null;
                return false;
            }
        }

        /// <param name="rangeEx">The range expression for which to create the access functions</param>
        /// <returns>
        /// Three functions to access the start, step, and end of a range.
        /// The function to access the step may return null if the given range does not specify the step.
        /// In that case, the step size defaults to be 1L.
        /// </returns>
        internal (Func<Value> GetStart, Func<Value?> GetStep, Func<Value> GetEnd) RangeItems(TypedExpression rangeEx)
        {
            Func<Value> startValue;
            Func<Value?> stepValue;
            Func<Value> endValue;
            if (rangeEx.Expression is ResolvedExpressionKind.RangeLiteral rlit)
            {
                if (rlit.Item1.Expression is ResolvedExpressionKind.RangeLiteral rlitInner)
                {
                    startValue = () => this.sharedState.EvaluateSubexpression(rlitInner.Item1).Value;
                    stepValue = () => this.sharedState.EvaluateSubexpression(rlitInner.Item2).Value;
                }
                else
                {
                    startValue = () => this.sharedState.EvaluateSubexpression(rlit.Item1).Value;
                    stepValue = () => null;
                }

                // Item2 is always the end. Either Item1 is the start and 1 is the step,
                // or Item1 is a range expression itself, with Item1 the start and Item2 the step.
                endValue = () => this.sharedState.EvaluateSubexpression(rlit.Item2).Value;
            }
            else
            {
                var range = this.sharedState.EvaluateSubexpression(rangeEx).Value;
                startValue = () => this.sharedState.CurrentBuilder.ExtractValue(range, 0u);
                stepValue = () => this.sharedState.CurrentBuilder.ExtractValue(range, 1u);
                endValue = () => this.sharedState.CurrentBuilder.ExtractValue(range, 2u);
            }
            return (startValue, stepValue, endValue);
        }

        // private methods

        private IValue Length(TypedExpression arg)
        {
            var arrayArg = (ArrayValue)this.sharedState.EvaluateSubexpression(arg);
            return this.sharedState.Values.FromSimpleValue(arrayArg.Length, Int);
        }

        private IValue IntAsDouble(TypedExpression arg)
        {
            var value = this.sharedState.EvaluateSubexpression(arg);
            var cast = this.sharedState.CurrentBuilder.SIToFPCast(value.Value, this.sharedState.Types.Double);
            return this.sharedState.Values.FromSimpleValue(cast, Double);
        }

        private IValue DoubleAsInt(TypedExpression arg)
        {
            var value = this.sharedState.EvaluateSubexpression(arg);
            var cast = this.sharedState.CurrentBuilder.FPToSICast(value.Value, this.sharedState.Types.Int);
            return this.sharedState.Values.FromSimpleValue(cast, Int);
        }

        private IValue IntAsBigInt(TypedExpression arg)
        {
            // The runtime function BigIntCreateI64 creates a value with reference count 1.
            var createBigInt = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.BigIntCreateI64);
            var value = this.sharedState.EvaluateSubexpression(arg);
            var res = this.sharedState.CurrentBuilder.Call(createBigInt, value.Value);
            var evaluated = this.sharedState.Values.From(res, BigInt);
            this.sharedState.ScopeMgr.RegisterValue(evaluated);
            return evaluated;
        }

        private IValue RangeStart(TypedExpression arg)
        {
            var (getStart, _, _) = this.RangeItems(arg);
            return this.sharedState.Values.FromSimpleValue(getStart(), Int);
        }

        private IValue RangeStep(TypedExpression arg)
        {
            var (_, getStep, _) = this.RangeItems(arg);
            var res = getStep() ?? this.sharedState.Context.CreateConstant(1L);
            return this.sharedState.Values.FromSimpleValue(res, Int);
        }

        private IValue RangeEnd(TypedExpression arg)
        {
            var (_, _, getEnd) = this.RangeItems(arg);
            return this.sharedState.Values.FromSimpleValue(getEnd(), Int);
        }

        private IValue RangeReverse(TypedExpression arg)
        {
            var (getStart, getStep, getEnd) = this.RangeItems(arg);
            var start = getStart();
            var step = getStep() ?? this.sharedState.Context.CreateConstant(1L);
            var end = getEnd();

            var newStart = this.sharedState.CurrentBuilder.Add(
                start,
                this.sharedState.CurrentBuilder.Mul(
                    step,
                    this.sharedState.CurrentBuilder.SDiv(
                        this.sharedState.CurrentBuilder.Sub(end, start), step)));
            return this.sharedState.CreateRange(newStart, this.sharedState.CurrentBuilder.Neg(step), start);
        }

        private IValue Message(TypedExpression arg)
        {
            var value = this.sharedState.EvaluateSubexpression(arg);
            var message = this.sharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.Message);
            this.sharedState.CurrentBuilder.Call(message, value.Value);
            return this.sharedState.Values.Unit;
        }

        private IValue DumpMachine(TypedExpression arg)
        {
            var value = this.sharedState.EvaluateSubexpression(arg).Value;
            if (!value.NativeType.IsPointer)
            {
                var pointer = this.sharedState.CurrentBuilder.Alloca(value.NativeType);
                this.sharedState.CurrentBuilder.Store(value, pointer);
                value = pointer;
            }

            var dump = this.sharedState.GetOrCreateTargetInstruction(QuantumInstructionSet.DumpMachine);
            this.sharedState.CurrentBuilder.Call(dump, value);
            return this.sharedState.Values.Unit;
        }
    }
}
