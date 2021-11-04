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
    internal static class FSharpOptionHelper
    {
        internal class OptionValueGetter
        {
            private MethodInfo? isSomeMethod;
            private MethodInfo? getValueMethod;

            public OptionValueGetter(Type fSharpOptionType)
            {
                this.isSomeMethod = fSharpOptionType.GetMethod("get_IsSome");
                this.getValueMethod = fSharpOptionType.GetMethod("get_Value");
            }

            public object? GetValue(object optionValue)
            {
                if (object.Equals(true, this.isSomeMethod?.Invoke(optionValue, new object[] { optionValue })))
                {
                    return this.getValueMethod?.Invoke(optionValue, null);
                }

                return null;
            }
        }

        private static Type? fSharpOptionType = AppDomain
                                                    .CurrentDomain
                                                    .GetAssemblies()
                                                    .FirstOrDefault((a) => a.GetName().Name == "FSharp.Core")
                                                    ?.GetType("Microsoft.FSharp.Core.FSharpOption`1");

        public static bool IsFSharpOptionType(Type? type) =>
            type != null
            && fSharpOptionType != null
            && object.Equals(fSharpOptionType, ReflectionCache.GetGenericTypeDefinition(type));

        private static ConcurrentDictionary<Type, OptionValueGetter?> optionValueGetterCache = new();

        public static object? GetOptionValue(object? optionValue, Type? type)
        {
            if (optionValue == null || type == null)
            {
                return null;
            }

            return optionValueGetterCache
                        .GetOrAdd(type, (t) => IsFSharpOptionType(type) ? new(type) : null)
                        ?.GetValue(optionValue);
        }

        public static object? GetOptionValue(object? optionValue) =>
            GetOptionValue(optionValue, optionValue?.GetType());
    }
}