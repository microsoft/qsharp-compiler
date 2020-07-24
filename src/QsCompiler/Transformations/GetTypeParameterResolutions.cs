﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.GetTypeParameterResolutions
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    // Type Parameters are frequently referenced by the callable of the type parameter followed by the name of the specific type parameter.
    using TypeParameterName = Tuple<QsQualifiedName, NonNullable<string>>;
    using TypeParameterResolutions = ImmutableDictionary</*TypeParameterName*/ Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    /// <summary>
    /// Utility class containing methods for working with type parameters.
    /// </summary>
    internal static class TypeParamUtils
    {
        /// <summary>
        /// Reverses the dependencies of type parameters resolving to other type parameters in the given
        /// dictionary to create a lookup whose keys are type parameters and whose values are all the type
        /// parameters that can be updated by knowing the resolution of the lookup's associated key.
        /// </summary>
        private static ILookup<TypeParameterName, TypeParameterName> GetReplaceable(TypeParameterResolutions.Builder typeParamResolutions)
        {
            return typeParamResolutions
               .Select(kvp => (kvp.Key, GetTypeParameters.Apply(kvp.Value))) // Get any type parameters in the resolution type.
               .SelectMany(tup => tup.Item2.Select(value => (tup.Key, value))) // For each type parameter found, match it to the dictionary key.
               .ToLookup(// Reverse the keys and resulting type parameters to make the lookup.
                   kvp => kvp.value,
                   kvp => kvp.Key);
        }

        /// <summary>
        /// Uses the given lookup, mayBeReplaced, to determine what records in the combinedBuilder can be updated
        /// from the given type parameter, typeParam, and its resolution, paramRes. Then updates the combinedBuilder
        /// appropriately. The flag used to determine the validity of type resolutions dictionaries, success, is
        /// updated and returned.
        /// </summary>
        private static bool UpdatedReplaceableResolutions(
            bool success,
            ILookup<TypeParameterName, TypeParameterName> mayBeReplaced,
            TypeParameterResolutions.Builder combinedBuilder,
            TypeParameterName typeParam,
            ResolvedType paramRes)
        {
            // Create a dictionary with just the current resolution in it.
            var singleResolution = new[] { 0 }.ToImmutableDictionary(_ => typeParam, _ => paramRes);

            // Get all the parameters whose value is dependent on the current resolution's type parameter,
            // and update their values with this resolution's value.
            foreach (var keyInCombined in mayBeReplaced[typeParam])
            {
                // Check that we are not constricting a type parameter to another type parameter of the same callable.
                success = success && !ConstrictionCheck.Apply(keyInCombined, paramRes);
                combinedBuilder[keyInCombined] = ResolvedType.ResolveTypeParameters(singleResolution, combinedBuilder[keyInCombined]);
            }

            return success;
        }

        /// <summary>
        /// Combines independent resolutions in a disjointed dictionary, resulting in a
        /// resolution dictionary that has type parameter keys that are not referenced
        /// in its values.
        /// </summary>
        internal static bool TryCombineTypeResolutionDictionary(out TypeParameterResolutions combinedResolutions, TypeParameterResolutions independentResolutions)
        {
            if (!independentResolutions.Any())
            {
                combinedResolutions = TypeParameterResolutions.Empty;
                return true;
            }

            var combinedBuilder = ImmutableDictionary.CreateBuilder<TypeParameterName, ResolvedType>();
            var success = true;

            foreach (var (typeParam, paramRes) in independentResolutions)
            {
                // Contains a lookup of all the keys in the combined resolutions whose value needs to be updated
                // if a certain type parameter is resolved by the currently processed dictionary.
                var mayBeReplaced = GetReplaceable(combinedBuilder);

                // Check that we are not constricting a type parameter to another type parameter of the same callable
                // both before and after updating the current value with the resolutions processed so far.
                success = success && !ConstrictionCheck.Apply(typeParam, paramRes);
                var resolvedParamRes = ResolvedType.ResolveTypeParameters(combinedBuilder.ToImmutable(), paramRes);
                success = success && !ConstrictionCheck.Apply(typeParam, resolvedParamRes);

                // Do any replacements for type parameters that may be replaced with the current resolution.
                success = UpdatedReplaceableResolutions(success, mayBeReplaced, combinedBuilder, typeParam, resolvedParamRes);

                // Add the resolution to the current dictionary.
                combinedBuilder[typeParam] = resolvedParamRes;
            }

            combinedResolutions = combinedBuilder.ToImmutable();
            return success;
        }

        /// <summary>
        /// Combines subsequent type parameter resolutions dictionaries into a single dictionary containing the resolution for all
        /// the type parameters found.
        ///
        /// The given resolutions are expected to be ordered such that dictionaries containing type parameters that take a
        /// dependency on other type parameters in other dictionaries appear before those dictionaries they depend on.
        /// I.e., dictionary A depends on dictionary B, so A should come before B. When using this method to resolve
        /// the resolutions of a nested expression, this means that the innermost resolutions should come first, followed by
        /// the next innermost, and so on until the outermost expression is given last.
        ///
        /// Returns the constructed dictionary as out parameter. Returns true if the combination of the given resolutions is valid,
        /// i.e. if there are no conflicting resolutions and type parameters are uniquely resolved to either a concrete type, a
        /// type parameter belonging to a different callable, or themselves.
        /// </summary>
        internal static bool TryCombineTypeResolutions(out TypeParameterResolutions combinedResolutions, params TypeParameterResolutions[] independentResolutionDictionaries)
        {
            if (!independentResolutionDictionaries.Any())
            {
                combinedResolutions = TypeParameterResolutions.Empty;
                return true;
            }

            var combinedBuilder = ImmutableDictionary.CreateBuilder<TypeParameterName, ResolvedType>();
            var success = true;

            static bool IsSelfResolution(TypeParameterName typeParam, ResolvedType res) =>
                res.Resolution is ResolvedTypeKind.TypeParameter tp && tp.Item.Origin.Equals(typeParam.Item1) && tp.Item.TypeName.Equals(typeParam.Item2);

            foreach (var resolutionDictionary in independentResolutionDictionaries)
            {
                success = TryCombineTypeResolutionDictionary(out var resolvedDictionary, resolutionDictionary) && success;

                // Contains a lookup of all the keys in the combined resolutions whose value needs to be updated
                // if a certain type parameter is resolved by the currently processed dictionary.
                var mayBeReplaced = GetReplaceable(combinedBuilder);

                // Do any replacements for type parameters that may be replaced with values in the current dictionary.
                // This needs to be done first to cover an edge case.
                foreach (var (typeParam, paramRes) in resolvedDictionary.Where(entry => mayBeReplaced.Contains(entry.Key)))
                {
                    success = UpdatedReplaceableResolutions(success, mayBeReplaced, combinedBuilder, typeParam, paramRes);
                }

                // Validate and add each resolution to the result.
                foreach (var (typeParam, paramRes) in resolvedDictionary)
                {
                    // Check that we are not constricting a type parameter to another type parameter of the same callable.
                    success = success && !ConstrictionCheck.Apply(typeParam, paramRes);

                    // Check that there is no conflicting resolution already defined.
                    var conflictingResolutionExists = combinedBuilder.TryGetValue(typeParam, out var current)
                        && !current.Equals(paramRes) && !IsSelfResolution(typeParam, current);
                    success = success && !conflictingResolutionExists;

                    // Add the resolution to the current dictionary.
                    combinedBuilder[typeParam] = paramRes;
                }
            }

            combinedResolutions = combinedBuilder.ToImmutable();
            return success;
        }

        /// <summary>
        /// Walker that collects all of the type parameter references for a given ResolvedType
        /// and returns them as a HashSet.
        /// </summary>
        internal class GetTypeParameters : TypeTransformation<GetTypeParameters.TransformationState>
        {
            /// <summary>
            /// Walks the given ResolvedType and returns all of the type parameters referenced.
            /// </summary>
            public static HashSet<TypeParameterName> Apply(ResolvedType res)
            {
                var walker = new GetTypeParameters();
                walker.OnType(res);
                return walker.SharedState.TypeParams;
            }

            internal class TransformationState
            {
                public HashSet<TypeParameterName> TypeParams = new HashSet<TypeParameterName>();
            }

            private GetTypeParameters() : base(new TransformationState(), TransformationOptions.NoRebuild)
            {
            }

            private static TypeParameterName AsTypeResolutionKey(QsTypeParameter tp) => Tuple.Create(tp.Origin, tp.TypeName);

            public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
            {
                this.SharedState.TypeParams.Add(AsTypeResolutionKey(tp));
                return base.OnTypeParameter(tp);
            }
        }

        /// <summary>
        /// Walker that checks a given type parameter resolution to see if it constricts
        /// the type parameter to another type parameter of the same callable.
        /// </summary>
        internal class ConstrictionCheck : TypeTransformation<ConstrictionCheck.TransformationState>
        {
            private readonly TypeParameterName typeParamName;

            /// <summary>
            /// Walks the given ResolvedType, typeParamRes, and returns true if there is a reference
            /// to a different type parameter of the same callable as the given type parameter, typeParam.
            /// Otherwise returns false.
            /// </summary>
            public static bool Apply(TypeParameterName typeParam, ResolvedType typeParamRes)
            {
                var walker = new ConstrictionCheck(typeParam);
                walker.OnType(typeParamRes);
                return walker.SharedState.IsConstrictive;
            }

            internal class TransformationState
            {
                public bool IsConstrictive = false;
            }

            private ConstrictionCheck(TypeParameterName typeParamName)
                : base(new TransformationState(), TransformationOptions.NoRebuild)
            {
                this.typeParamName = typeParamName;
            }

            public new ResolvedType OnType(ResolvedType t)
            {
                // Short-circuit if we already know the type is constrictive.
                if (!this.SharedState.IsConstrictive)
                {
                    base.OnType(t);
                }

                // It doesn't matter what we return because this is a walker.
                return t;
            }

            public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
            {
                // If the type parameter is from the same callable, but is a different parameter,
                // then the type resolution is constrictive.
                if (tp.Origin.Equals(this.typeParamName.Item1) && !tp.TypeName.Equals(this.typeParamName.Item2))
                {
                    this.SharedState.IsConstrictive = true;
                }

                return base.OnTypeParameter(tp);
            }
        }
    }

    /// <summary>
    /// stuff
    /// </summary>
    public static class GetTypeParameterResolutions
    {
        public static TypeParameterResolutions Apply(TypedExpression expression)
        {
            var walker = new CombineTypeParams();
            walker.OnTypedExpression(expression);
            var valid = TypeParamUtils.TryCombineTypeResolutions(out var combined, walker.SharedState.Resolutions.ToArray());
            return combined;
        }

        private class TransformationState
        {
            public List<TypeParameterResolutions> Resolutions = new List<TypeParameterResolutions>();
            public bool InCallLike = false;
        }

        private class CombineTypeParams : ExpressionTransformation<TransformationState>
        {
            public CombineTypeParams() : base(new TransformationState(), TransformationOptions.NoRebuild)
            {
            }

            public override TypedExpression OnTypedExpression(TypedExpression ex)
            {
                if (ex.Expression is ExpressionKind.CallLikeExpression call)
                {
                    if (!this.SharedState.InCallLike || TypedExpression.IsPartialApplication(call))
                    {
                        var contextInCallLike = this.SharedState.InCallLike;
                        this.SharedState.InCallLike = true;
                        this.OnTypedExpression(call.Item1);
                        this.SharedState.Resolutions.Add(ex.TypeParameterResolutions);
                        this.SharedState.InCallLike = contextInCallLike;
                    }
                }
                else if (ex.Expression is ExpressionKind.AdjointApplication adj)
                {
                    this.OnTypedExpression(adj.Item);
                }
                else if (ex.Expression is ExpressionKind.ControlledApplication ctrl)
                {
                    this.OnTypedExpression(ctrl.Item);
                }
                else
                {
                    this.SharedState.Resolutions.Add(ex.TypeParameterResolutions);
                }

                return ex;
            }
        }
    }
}
