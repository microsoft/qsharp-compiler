// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// Helper class to interact with FSharp Option types
    /// </summary>
    public static class FSharpOptionHelper
    {
        internal class OptionValueGetter
        {
            private readonly MethodInfo? getValueMethod;

            public OptionValueGetter(Type fSharpOptionType)
            {
                this.getValueMethod = fSharpOptionType.GetMethod("get_Value");
            }

            public object? GetValue(object optionValue) =>
                this.getValueMethod?.Invoke(optionValue, null);
        }

        private static readonly Type? FSharpOptionType = AppDomain
                                                         .CurrentDomain
                                                         .GetAssemblies()
                                                         .FirstOrDefault((a) => a.GetName().Name == "FSharp.Core")
                                                         ?.GetType("Microsoft.FSharp.Core.FSharpOption`1");

        public static bool IsFSharpOptionType(Type? type) =>
            type != null
            && FSharpOptionType != null
            && Equals(FSharpOptionType, ReflectionCache.GetGenericTypeDefinition(type));

        private static readonly ConcurrentDictionary<Type, OptionValueGetter?> OptionValueGetterCache = new ConcurrentDictionary<Type, OptionValueGetter?>();

        public static object? GetOptionValue(object? optionValue, Type? type)
        {
            if (optionValue == null || type == null)
            {
                return null;
            }

            return OptionValueGetterCache
                        .GetOrAdd(type, (t) => IsFSharpOptionType(type) ? new OptionValueGetter(type) : null)
                        ?.GetValue(optionValue);
        }

        public static object? GetOptionValue(object? optionValue) =>
            GetOptionValue(optionValue, optionValue?.GetType());
    }

    /// <summary>
    /// Helper class to interact with FSharp Union types
    /// </summary>
    public static class FSharpUnionHelper
    {
        private static readonly MethodInfo? FSharpIsUnionTypeMethod = AppDomain
                                                                      .CurrentDomain
                                                                      .GetAssemblies()
                                                                      .FirstOrDefault((a) => a.GetName().Name == "FSharp.Core")
                                                                      ?.GetType("Microsoft.FSharp.Reflection.FSharpType")
                                                                      ?.GetMethod("IsUnion", BindingFlags.Static | BindingFlags.Public);

        public static bool IsFSharpUnionType(Type type)
        {
            if (FSharpIsUnionTypeMethod == null)
            {
                return false;
            }

            return (bool)FSharpIsUnionTypeMethod.Invoke(null, new object?[]
            {
                type,
                null,
            })!;
        }
    }
}
