// -----------------------------------------------------------------------
// <copyright file="ModuleFlag.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Module Flags Tuple for a module</summary>
    public class ModuleFlag
    {
        /// <summary>Initializes a new instance of the <see cref="ModuleFlag"/> class.</summary>
        /// <param name="behavior">Behavior for the flag</param>
        /// <param name="name">Name of the flag</param>
        /// <param name="metadata">Metadata for the flag</param>
        public ModuleFlag(ModuleFlagBehavior behavior, string name, LlvmMetadata metadata)
        {
            this.Behavior = behavior;
            this.Name = name;
            this.Metadata = metadata;
        }

        /// <summary>Gets the <see cref="ModuleFlagBehavior"/> options for this module flag</summary>
        public ModuleFlagBehavior Behavior { get; }

        /// <summary>Gets the name of flag</summary>
        public string Name { get; }

        /// <summary>Gets the Metadata for this flag</summary>
        public LlvmMetadata Metadata { get; }
    }
}
