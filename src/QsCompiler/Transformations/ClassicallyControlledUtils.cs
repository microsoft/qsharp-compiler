// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlled
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>;

    /// <summary>
    /// These tools are specific to the classically-controlled transformation and are not intended for wider use in their current state. 
    /// They rely on the specific context in which they are invoked during that transformation and are not general purpose tools. 
    /// </summary>
    internal static class Utils 
    {
        internal static TypedExpression CreateIdentifierExpression(Identifier id,
            TypeArgsResolution typeArgsMapping, ResolvedType resolvedType) =>
            new TypedExpression
            (
                ExpressionKind.NewIdentifier(
                    id,
                    typeArgsMapping.Any()
                    ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(typeArgsMapping
                        .Select(argMapping => argMapping.Item3) // This should preserve the order of the type args
                        .ToImmutableArray())
                    : QsNullable<ImmutableArray<ResolvedType>>.Null),
                typeArgsMapping,
                resolvedType,
                new InferredExpressionInformation(false, false),
                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
            );

        internal static TypedExpression CreateValueTupleExpression(params TypedExpression[] expressions) =>
            new TypedExpression
            (
                ExpressionKind.NewValueTuple(expressions.ToImmutableArray()),
                TypeArgsResolution.Empty,
                ResolvedType.New(ResolvedTypeKind.NewTupleType(expressions.Select(expr => expr.ResolvedType).ToImmutableArray())),
                new InferredExpressionInformation(false, expressions.Any(exp => exp.InferredInformation.HasLocalQuantumDependency)),
                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
            );

        internal static TypedExpression CreateCallLikeExpression(TypedExpression id, TypedExpression args, TypeArgsResolution typeRes) =>
            new TypedExpression
            (
                ExpressionKind.NewCallLikeExpression(id, args),
                typeRes,
                ResolvedType.New(ResolvedTypeKind.UnitType),
                new InferredExpressionInformation(false, true),
                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
            );

        /// <summary>
        /// Creates an array literal with the given items, setting the range information to Null. 
        /// If no items are given, creates an empty array of type Unit[]. 
        /// The resolved types for all of the given expressions must match, 
        /// none of the given expressions should have a local quantum dependency, 
        /// and all range information should be stripped from each given expression. 
        /// </summary>
        internal static TypedExpression CreateValueArray(ResolvedType baseType, params TypedExpression[] expressions)
        {
            return new TypedExpression
            (
                ExpressionKind.NewValueArray(expressions.ToImmutableArray()),
                TypeArgsResolution.Empty,
                ResolvedType.New(ResolvedTypeKind.NewArrayType(baseType)),
                new InferredExpressionInformation(false, false),
                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
            );
        }

        internal static ResolvedType GetOperationType(IEnumerable<OpProperty> props, ResolvedType argumentType)
        {
            var characteristics = new CallableInformation(
                ResolvedCharacteristics.FromProperties(props),
                InferredCallableInformation.NoInformation);

            return ResolvedType.New(ResolvedTypeKind.NewOperation(
                Tuple.Create(argumentType, ResolvedType.New(ResolvedTypeKind.UnitType)),
                characteristics));
        }

        internal static TypeArgsResolution GetCombinedTypeResolution(TypeArgsResolution outer, TypeArgsResolution inner)
        {
            var outerDict = outer.ToDictionary(x => (x.Item1, x.Item2), x => x.Item3);
            return inner.Select(innerRes =>
            {
                if (innerRes.Item3.Resolution is ResolvedTypeKind.TypeParameter typeParam &&
                    outerDict.TryGetValue((typeParam.Item.Origin, typeParam.Item.TypeName), out var outerRes))
                {
                    outerDict.Remove((typeParam.Item.Origin, typeParam.Item.TypeName));
                    return Tuple.Create(innerRes.Item1, innerRes.Item2, outerRes);
                }
                else
                {
                    return innerRes;
                }
            })
            .Concat(outerDict.Select(x => Tuple.Create(x.Key.Item1, x.Key.Item2, x.Value))).ToImmutableArray();
        }

        internal static (TypedExpression, TypedExpression) GetNoOp()
        {
            var identifier = Utils.CreateIdentifierExpression(
                Identifier.NewGlobalCallable(new QsQualifiedName(BuiltIn.NoOp.Namespace, BuiltIn.NoOp.Name)),
                BuiltIn.NoOp.TypeParameters
                    .Select(param => Tuple.Create(new QsQualifiedName(BuiltIn.NoOp.Namespace, BuiltIn.NoOp.Name), param, ResolvedType.New(ResolvedTypeKind.UnitType)))
                    .ToImmutableArray(),
                Utils.GetOperationType(new[] { OpProperty.Adjointable, OpProperty.Controllable }, ResolvedType.New(ResolvedTypeKind.UnitType)));

            var args = new TypedExpression
            (
                ExpressionKind.UnitValue,
                TypeArgsResolution.Empty,
                ResolvedType.New(ResolvedTypeKind.UnitType),
                new InferredExpressionInformation(false, false),
                QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null
            );

            return (identifier, args);
        }
    }
}
