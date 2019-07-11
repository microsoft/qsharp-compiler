// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// These attributes Mock the actual attributes used at runtime to test the Attribute Reader.
namespace Microsoft.Quantum.Simulation.Core

    open System

    [<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
    type CallableDeclarationAttribute(serialization : string) = inherit Attribute()

    [<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
    type TypeDeclarationAttribute(serialization : string) = inherit Attribute()

    [<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
    type SpecializationDeclarationAttribute(serialization : string) = inherit Attribute()


namespace Microsoft.Quantum.QsCompiler.Attributes

    open System

    [<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
    type CallableDeclarationAttribute(serialization : string) = inherit Attribute()

    [<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
    type TypeDeclarationAttribute(serialization : string) = inherit Attribute()

    [<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)>]
    type SpecializationDeclarationAttribute(serialization : string) = inherit Attribute()
