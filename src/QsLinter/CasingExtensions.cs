// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace Microsoft.Quantum.Compiler.Linter
{
    internal static class CasingExtensions
    {
        internal static bool IsCamelCase(this string id) =>
            Regex.Match(
                id,
                "^[a-z]([a-zA-Z0-9])*$"
            )
            .Success;

        internal static bool IsPascalCase(this string id) =>
            Regex.Match(
                id,
                "^[A-Z]([a-zA-Z0-9])*$"
            )
            .Success;

        /* TODO
        internal static bool IsSnakeCase(this string id)
        {
            
        }

        internal static bool IsAngryCase(this string id)
        {
            
        }

        internal static bool IsKebabCase(this string id)
        {
            
        }
        */
    }
}