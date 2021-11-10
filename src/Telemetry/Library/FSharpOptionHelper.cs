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
            private MethodInfo? getValueMethod;

            public OptionValueGetter(Type fSharpOptionType)
            {
                this.getValueMethod = fSharpOptionType.GetMethod("get_Value");
            }

            public object? GetValue(object optionValue) =>
                this.getValueMethod?.Invoke(optionValue, null);
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

        private static ConcurrentDictionary<Type, OptionValueGetter?> optionValueGetterCache = new ConcurrentDictionary<Type, OptionValueGetter?>();

        public static object? GetOptionValue(object? optionValue, Type? type)
        {
            if (optionValue == null || type == null)
            {
                return null;
            }

            return optionValueGetterCache
                        .GetOrAdd(type, (t) => IsFSharpOptionType(type) ? new OptionValueGetter(type) : null)
                        ?.GetValue(optionValue);
        }

        public static object? GetOptionValue(object? optionValue) =>
            GetOptionValue(optionValue, optionValue?.GetType());
    }
}
