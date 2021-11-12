// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.Telemetry
{
    /// <summary>
    /// Apply the PIIData attribute to properties or fields that should be
    /// tagged as PII in the telemetry. The values of these fields will
    /// be hashed with a rotating salt.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PiiDataAttribute : Attribute
    {
        public PiiDataAttribute()
        {
        }
    }
}