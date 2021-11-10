// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// Serializes the property as Json
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SerializeJsonAttribute : Attribute
    {
        public SerializeJsonAttribute()
        {
        }
    }
}