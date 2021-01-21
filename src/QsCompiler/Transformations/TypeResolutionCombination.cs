﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    // Type Parameters are frequently referenced by the callable of the type parameter followed by the name of the specific type parameter.
    using TypeParameterName = Tuple<QsQualifiedName, string>;
    using TypeParameterResolutions = ImmutableDictionary</*TypeParameterName*/ Tuple<QsQualifiedName, string>, ResolvedType>;

    /// <summary>
    /// Combines a series of type parameter resolution dictionaries, IndependentResolutionDictionaries,
    /// into one resolution dictionary, CombinedResolutionDictionary, containing the ultimate type
    /// resolutions for all the type parameters found in the dictionaries. Validation is done on the
    /// resolutions, which can be checked through the IsValid flag.
    /// </summary>
    public class TypeResolutionCombination
    {
        // Static Members

        /// <summary>
        /// Checks if the given type parameter directly resolves to itself.
        /// </summary>
        private static bool IsSelfResolution(TypeParameterName typeParam, ResolvedType res)
        {
            return res.Resolution is ResolvedTypeKind.TypeParameter tp
                && tp.Item.Origin.Equals(typeParam.Item1)
                && tp.Item.TypeName.Equals(typeParam.Item2);
        }

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

        // Fields and Properties

        /// <summary>
        /// Array of all the type parameter resolution dictionaries that are combined in this combination.
        /// The items are ordered such that dictionaries containing type parameters resolutions that
        /// reference type parameters in other dictionaries appear before those dictionaries containing
        /// the referenced type parameters. I.e., dictionary A depends on dictionary B, so A should come before B.
        /// </summary>
        public readonly ImmutableArray<TypeParameterResolutions> IndependentResolutionDictionaries;

        /// <summary>
        /// The resulting resolution dictionary from combining all the input resolutions in
        /// IndependentResolutionDictionaries. Represents a combination of all the type parameters
        /// found and their ultimate type resolutions.
        /// </summary>
        public TypeParameterResolutions CombinedResolutionDictionary { get; private set; } = TypeParameterResolutions.Empty;

        /// <summary>
        /// Flag for if there were any invalid scenarios encountered while creating the combination.
        /// Invalid scenarios include a type parameter being assigned to multiple conflicting types
        /// and a type parameter being assigned to a type referencing a different type parameter of
        /// the same callable. Has value true if no invalid scenarios were encountered.
        /// </summary>
        public bool IsValid => !this.combinesOverConflictingResolution && !this.combinesOverParameterConstriction;

        /// <summary>
        /// Flag for if, at any time in the creation of the combination, there was a type parameter that
        /// was assigned conflicting type resolutions. Has value true if a conflict was encountered.
        /// </summary>
        private bool combinesOverConflictingResolution = false;

        /// <summary>
        /// Flag for if, at any time in the creation of the combination, there was a type parameter that
        /// was assigned a type resolution referencing a different type parameter of the same callable.
        /// </summary>
        private bool combinesOverParameterConstriction = false;

        // Constructors

        /// <summary>
        /// Creates a type parameter resolution combination from the independent type parameter resolutions
        /// found in the given typed expression and its sub expressions. Only sub-expressions whose
        /// type parameter resolutions are relevant to the given expression's type parameter resolutions
        /// are considered.
        /// </summary>
        public TypeResolutionCombination(TypedExpression expression)
            : this(GetTypeParameterResolutions.Apply(expression))
        {
        }

        /// <summary>
        /// Creates a type parameter resolution combination from independent type parameter resolution dictionaries.
        /// The given resolutions are expected to be ordered such that dictionaries containing type parameters resolutions that
        /// reference type parameters in other dictionaries appear before those dictionaries containing the referenced type parameters.
        /// I.e., dictionary A depends on dictionary B, so A should come before B. When using this method to resolve
        /// the resolutions of a nested expression, this means that the innermost resolutions should come first, followed by
        /// the next innermost, and so on until the outermost expression is given last. Empty and null dictionaries are ignored.
        /// </summary>
        internal TypeResolutionCombination(IEnumerable<TypeParameterResolutions> independentResolutionDictionaries)
        {
            // Filter out empty dictionaries
            this.IndependentResolutionDictionaries = independentResolutionDictionaries.Where(res => !(res is null || res.IsEmpty)).ToImmutableArray();

            if (this.IndependentResolutionDictionaries.Any())
            {
                this.CombineTypeResolutions();
            }
        }

        // Methods

        /// <summary>
        /// Updates the combinesOverParameterConstriction flag. If the flag is already set to true,
        /// nothing will be done. If not, the given type parameter will be checked against the given
        /// resolution for type parameter constriction, which is when one type parameter is dependent
        /// on another type parameter of the same callable.
        /// </summary>
        private void UpdateConstrictionFlag(TypeParameterName typeParamName, ResolvedType typeParamResolution)
        {
            this.combinesOverParameterConstriction = this.combinesOverParameterConstriction
                || CheckForConstriction.IsConstrictiveResolution(typeParamName, typeParamResolution);
        }

        /// <summary>
        /// Uses the given lookup, mayBeReplaced, to determine what records in the combinedBuilder can be updated
        /// from the given type parameter, typeParam, and its resolution, paramRes. Then updates the combinedBuilder
        /// appropriately.
        /// </summary>
        private void UpdatedReplaceableResolutions(
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
                this.UpdateConstrictionFlag(keyInCombined, paramRes);
                combinedBuilder[keyInCombined] = ResolvedType.ResolveTypeParameters(singleResolution, combinedBuilder[keyInCombined]);
            }
        }

        /// <summary>
        /// Combines independent resolutions in a disjointed dictionary, resulting in a
        /// resolution dictionary that has type parameter keys that are not referenced
        /// in its values. Null mappings are removed in the resulting dictionary.
        /// Returns the resulting dictionary.
        /// </summary>
        private TypeParameterResolutions CombineTypeResolutionDictionary(TypeParameterResolutions independentResolutions)
        {
            var combinedBuilder = ImmutableDictionary.CreateBuilder<TypeParameterName, ResolvedType>();

            foreach (var (typeParam, paramRes) in independentResolutions)
            {
                // Skip any null mappings
                if (paramRes is null)
                {
                    continue;
                }

                // Contains a lookup of all the keys in the combined resolutions whose value needs to be updated
                // if a certain type parameter is resolved by the currently processed dictionary.
                var mayBeReplaced = GetReplaceable(combinedBuilder);

                // Check that we are not constricting a type parameter to another type parameter of the same callable
                // both before and after updating the current value with the resolutions processed so far.
                this.UpdateConstrictionFlag(typeParam, paramRes);
                var resolvedParamRes = ResolvedType.ResolveTypeParameters(combinedBuilder.ToImmutable(), paramRes);
                this.UpdateConstrictionFlag(typeParam, resolvedParamRes);

                // Do any replacements for type parameters that may be replaced with the current resolution.
                this.UpdatedReplaceableResolutions(mayBeReplaced, combinedBuilder, typeParam, resolvedParamRes);

                // Add the resolution to the current dictionary.
                combinedBuilder[typeParam] = resolvedParamRes;
            }

            return combinedBuilder.ToImmutable();
        }

        /// <summary>
        /// Combines the resolution dictionaries in the combination into one resolution dictionary containing
        /// the resolutions for all the type parameters found.
        /// Updates the combination with the constructed dictionary. Updates the validation flags accordingly.
        /// </summary>
        private void CombineTypeResolutions()
        {
            var combinedBuilder = ImmutableDictionary.CreateBuilder<TypeParameterName, ResolvedType>();

            foreach (var resolutionDictionary in this.IndependentResolutionDictionaries)
            {
                var resolvedDictionary = this.CombineTypeResolutionDictionary(resolutionDictionary);

                // Contains a lookup of all the keys in the combined resolutions whose value needs to be updated
                // if a certain type parameter is resolved by the currently processed dictionary.
                var mayBeReplaced = GetReplaceable(combinedBuilder);

                // Do any replacements for type parameters that may be replaced with values in the current dictionary.
                // This needs to be done first to cover an edge case.
                foreach (var (typeParam, paramRes) in resolvedDictionary.Where(entry => mayBeReplaced.Contains(entry.Key)))
                {
                    this.UpdatedReplaceableResolutions(mayBeReplaced, combinedBuilder, typeParam, paramRes);
                }

                // Validate and add each resolution to the result.
                foreach (var (typeParam, paramRes) in resolvedDictionary)
                {
                    // Check that we are not constricting a type parameter to another type parameter of the same callable.
                    this.UpdateConstrictionFlag(typeParam, paramRes);

                    // Check that there is no conflicting resolution already defined.
                    if (!this.combinesOverConflictingResolution)
                    {
                        this.combinesOverConflictingResolution = combinedBuilder.TryGetValue(typeParam, out var current)
                            && !current.Equals(paramRes) && !IsSelfResolution(typeParam, current);
                    }

                    // Add the resolution to the current dictionary.
                    combinedBuilder[typeParam] = paramRes;
                }
            }

            this.CombinedResolutionDictionary = this.CombineTypeResolutionDictionary(combinedBuilder.ToImmutable());
        }

        // Nested Classes

        /// <summary>
        /// Walker that collects all of the type parameter references for a given ResolvedType
        /// and returns them as a HashSet.
        /// </summary>
        private class GetTypeParameters : TypeTransformation<GetTypeParameters.TransformationState>
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

            private GetTypeParameters()
                : base(new TransformationState(), TransformationOptions.NoRebuild)
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
        /// the type parameter to another type parameter of the same callable or to a
        /// nested reference of the same type parameter.
        /// </summary>
        private class CheckForConstriction : TypeTransformation<CheckForConstriction.TransformationState>
        {
            /// <summary>
            /// Walks the given ResolvedType, typeParamRes, and returns true if there is a reference
            /// to a different type parameter of the same callable as the given type parameter, typeParam,
            /// or the same type parameter but in a nested type.
            /// Otherwise returns false.
            /// </summary>
            public static bool IsConstrictiveResolution(TypeParameterName typeParam, ResolvedType typeParamRes)
            {
                if (typeParamRes.Resolution is ResolvedTypeKind.TypeParameter tp
                    && tp.Item.Origin.Equals(typeParam.Item1))
                {
                    // If given a type parameter whose origin matches the callable,
                    // the only valid resolution is a direct self-resolution
                    return !tp.Item.TypeName.Equals(typeParam.Item2);
                }

                var walker = new CheckForConstriction(typeParam.Item1);
                walker.OnType(typeParamRes);
                return walker.SharedState.IsConstrictive;
            }

            internal class TransformationState
            {
                public readonly QsQualifiedName Origin;
                public bool IsConstrictive = false;

                public TransformationState(QsQualifiedName origin)
                {
                    this.Origin = origin;
                }
            }

            private CheckForConstriction(QsQualifiedName origin)
                : base(new TransformationState(origin), TransformationOptions.NoRebuild)
            {
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
                // If the type parameter is from the same callable,
                // then the type resolution is constrictive.
                if (tp.Origin.Equals(this.SharedState.Origin))
                {
                    this.SharedState.IsConstrictive = true;
                }

                return base.OnTypeParameter(tp);
            }
        }

        /// <summary>
        /// Walker that returns the relevant type parameter resolution dictionaries from a given
        /// TypedExpression and its sub-expressions.
        /// </summary>
        private class GetTypeParameterResolutions : ExpressionTransformation<GetTypeParameterResolutions.TransformationState>
        {
            /// <summary>
            /// Walk the given TypedExpression, collecting type parameter resolution dictionaries relevant to
            /// the type parameter resolutions of the topmost expression. Returns the resolution dictionaries
            /// ordered from the innermost expression's resolutions to the outermost expression's resolutions.
            /// </summary>
            public static IEnumerable<TypeParameterResolutions> Apply(TypedExpression expression)
            {
                var walker = new GetTypeParameterResolutions();
                walker.OnTypedExpression(expression);
                return walker.SharedState.Resolutions;
            }

            internal class TransformationState
            {
                public List<TypeParameterResolutions> Resolutions = new List<TypeParameterResolutions>();
                public bool InCallLike = false;
            }

            private GetTypeParameterResolutions()
                : base(new TransformationState(), TransformationOptions.NoRebuild)
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
