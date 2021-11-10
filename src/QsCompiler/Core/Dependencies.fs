﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTree


type BuiltInKind =
    | Attribute
    | Function of TypeParameters: ImmutableArray<string>
    | Operation of TypeParameters: ImmutableArray<string> * IsSelfAdjoint: bool


type BuiltIn =
    {
        /// contains the fully qualified name of the built-in
        FullName: QsQualifiedName
        /// contains the specific kind of built-in this is, as well as information specific to that kind
        Kind: BuiltInKind
    }

    static member CanonNamespace = "Microsoft.Quantum.Canon"
    static member ClassicallyControlledNamespace = "Microsoft.Quantum.ClassicalControl"
    static member CoreNamespace = "Microsoft.Quantum.Core"
    static member DiagnosticsNamespace = "Microsoft.Quantum.Diagnostics"
    static member LlvmNamespace = "Microsoft.Quantum.Llvm"
    static member IntrinsicNamespace = "Microsoft.Quantum.Intrinsic"
    static member StandardArrayNamespace = "Microsoft.Quantum.Arrays"
    static member TargetingNamespace = "Microsoft.Quantum.Targeting"
    static member internal ConvertNamespace = "Microsoft.Quantum.Convert"
    static member internal MathNamespace = "Microsoft.Quantum.Math"

    /// Returns the set of namespaces that is automatically opened for each compilation.
    static member NamespacesToAutoOpen = ImmutableHashSet.Create(BuiltIn.CoreNamespace)

    /// Returns the set of callables that rewrite steps take dependencies on.
    /// These should be non-Generic callables only.
    [<Obsolete "RewriteStepDependencies will be removed in favor of each rewrite step declaring its dependencies.">]
    static member RewriteStepDependencies =
        ImmutableHashSet.Create(
            BuiltIn.RangeReverse.FullName,
            BuiltIn.Length.FullName,
            BuiltIn.Inline.FullName,
            BuiltIn.TargetInstruction.FullName,
            BuiltIn.RequiresCapability.FullName,
            BuiltIn.NoOp.FullName,
            BuiltIn.ApplyConditionally.FullName,
            BuiltIn.ApplyConditionallyA.FullName,
            BuiltIn.ApplyConditionallyC.FullName,
            BuiltIn.ApplyConditionallyCA.FullName,
            BuiltIn.ApplyIfZero.FullName,
            BuiltIn.ApplyIfZeroA.FullName,
            BuiltIn.ApplyIfZeroC.FullName,
            BuiltIn.ApplyIfZeroCA.FullName,
            BuiltIn.ApplyIfOne.FullName,
            BuiltIn.ApplyIfOneA.FullName,
            BuiltIn.ApplyIfOneC.FullName,
            BuiltIn.ApplyIfOneCA.FullName,
            BuiltIn.ApplyIfElseR.FullName,
            BuiltIn.ApplyIfElseRA.FullName,
            BuiltIn.ApplyIfElseRC.FullName,
            BuiltIn.ApplyIfElseRCA.FullName
        )

    /// The set of all built in callables and attributes
    [<Obsolete "AllBuiltIns will be removed in favor of each rewrite step being able to declare its dependencies.">]
    static member AllBuiltIns =
        [|
            // in Microsoft.Quantum.Core
            BuiltIn.Length
            BuiltIn.RangeStart
            BuiltIn.RangeStep
            BuiltIn.RangeEnd
            BuiltIn.RangeReverse
            BuiltIn.Attribute
            BuiltIn.EntryPoint
            BuiltIn.Deprecated
            BuiltIn.Inline
            // in Microsoft.Quantum.Targeting
            BuiltIn.TargetInstruction
            BuiltIn.RequiresCapability
            // in Microsoft.Quantum.Diagnostics
            BuiltIn.Test
            BuiltIn.EnableTestingViaName
            BuiltIn.DumpMachine
            BuiltIn.DumpRegister
            // in Microsoft.Quantum.Llvm
            BuiltIn.ReadCycleCounter
            // in Microsoft.Quantum.Canon
            BuiltIn.NoOp
            // in Microsoft.Quantum.Convert
            BuiltIn.IntAsDouble
            BuiltIn.DoubleAsInt
            BuiltIn.IntAsBigInt
            // in Microsoft.Quantum.Math
            BuiltIn.Truncate
            BuiltIn.ApplyConditionally
            BuiltIn.ApplyConditionallyA
            BuiltIn.ApplyConditionallyC
            BuiltIn.ApplyConditionallyCA
            BuiltIn.ApplyIfZero
            BuiltIn.ApplyIfZeroA
            BuiltIn.ApplyIfZeroC
            BuiltIn.ApplyIfZeroCA
            BuiltIn.ApplyIfOne
            BuiltIn.ApplyIfOneA
            BuiltIn.ApplyIfOneC
            BuiltIn.ApplyIfOneCA
            BuiltIn.ApplyIfElseR
            BuiltIn.ApplyIfElseRA
            BuiltIn.ApplyIfElseRC
            BuiltIn.ApplyIfElseRCA
            // in other namespaces (e.g. things used for code actions)
            BuiltIn.IndexRange
        |]
        |> ImmutableHashSet.Create<BuiltIn>

    /// Returns true if the given attribute marks the corresponding declaration as entry point.
    static member MarksEntryPoint(att: QsDeclarationAttribute) =
        match att.TypeId with
        | Value tId ->
            tId.Namespace = BuiltIn.EntryPoint.FullName.Namespace && tId.Name = BuiltIn.EntryPoint.FullName.Name
        | Null -> false

    /// Returns true if the given attribute marks the corresponding declaration as deprecated.
    static member MarksDeprecation(att: QsDeclarationAttribute) =
        match att.TypeId with
        | Value tId ->
            tId.Namespace = BuiltIn.Deprecated.FullName.Namespace && tId.Name = BuiltIn.Deprecated.FullName.Name
        | Null -> false

    /// Returns true if the given attribute indicates that the corresponding callable should be inlined.
    static member MarksInlining(att: QsDeclarationAttribute) =
        match att.TypeId with
        | Value tId -> tId.Namespace = BuiltIn.Inline.FullName.Namespace && tId.Name = BuiltIn.Inline.FullName.Name
        | Null -> false

    /// Returns true if the given attribute marks the corresponding declaration as unit test.
    static member MarksTestOperation(att: QsDeclarationAttribute) =
        match att.TypeId with
        | Value tId -> tId.Namespace = BuiltIn.Test.FullName.Namespace && tId.Name = BuiltIn.Test.FullName.Name
        | Null -> false

    /// Returns true if the given attribute indicates the runtime capability required for execution of the callable.
    static member MarksRequiredCapability(att: QsDeclarationAttribute) =
        match att.TypeId with
        | Value tId ->
            tId.Namespace = BuiltIn.RequiresCapability.FullName.Namespace
            && tId.Name = BuiltIn.RequiresCapability.FullName.Name
        | Null -> false

    /// Returns true if the given attribute defines a code identifying an instruction within the quantum instruction set that matches this callable.
    static member DefinesTargetInstruction(att: QsDeclarationAttribute) =
        match att.TypeId with
        | Value tId ->
            tId.Namespace = BuiltIn.TargetInstruction.FullName.Namespace
            && tId.Name = BuiltIn.TargetInstruction.FullName.Name
        | Null -> false

    /// Returns true if the given attribute defines an alternative name that may be used when loading a type or callable for testing purposes.
    static member internal DefinesNameForTesting(att: QsDeclarationAttribute) =
        match att.TypeId with
        | Value tId ->
            tId.Namespace = BuiltIn.EnableTestingViaName.FullName.Namespace
            && tId.Name = BuiltIn.EnableTestingViaName.FullName.Name
        | Null -> false

    /// Returns true if the given attribute indicates that the type or callable has been loaded via an alternative name for testing purposes.
    static member internal DefinesLoadedViaTestNameInsteadOf(att: QsDeclarationAttribute) =
        match att.TypeId with
        | Value tId ->
            tId.Namespace = GeneratedAttributes.Namespace
            && tId.Name = GeneratedAttributes.LoadedViaTestNameInsteadOf
        | Null -> false

    // dependencies in Microsoft.Quantum.Core

    static member Length =
        {
            FullName = { Name = "Length"; Namespace = BuiltIn.CoreNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Create "T")
        }

    static member RangeStart =
        {
            FullName = { Name = "RangeStart"; Namespace = BuiltIn.CoreNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Empty)
        }

    static member RangeStep =
        {
            FullName = { Name = "RangeStep"; Namespace = BuiltIn.CoreNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Empty)
        }

    static member RangeEnd =
        {
            FullName = { Name = "RangeEnd"; Namespace = BuiltIn.CoreNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Empty)
        }

    static member RangeReverse =
        {
            FullName = { Name = "RangeReverse"; Namespace = BuiltIn.CoreNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Empty)
        }

    static member Message =
        {
            FullName = { Name = "Message"; Namespace = BuiltIn.IntrinsicNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Empty)
        }

    static member Attribute = { FullName = { Name = "Attribute"; Namespace = BuiltIn.CoreNamespace }; Kind = Attribute }

    static member EntryPoint =
        { FullName = { Name = "EntryPoint"; Namespace = BuiltIn.CoreNamespace }; Kind = Attribute }

    static member Deprecated =
        { FullName = { Name = "Deprecated"; Namespace = BuiltIn.CoreNamespace }; Kind = Attribute }

    static member Inline = { FullName = { Name = "Inline"; Namespace = BuiltIn.CoreNamespace }; Kind = Attribute }

    // dependencies in Microsoft.Quantum.Targeting

    static member TargetInstruction =
        { FullName = { Name = "TargetInstruction"; Namespace = BuiltIn.TargetingNamespace }; Kind = Attribute }

    static member RequiresCapability =
        { FullName = { Name = "RequiresCapability"; Namespace = BuiltIn.TargetingNamespace }; Kind = Attribute }

    // dependencies in Microsoft.Quantum.Diagnostics

    static member Test = { FullName = { Name = "Test"; Namespace = BuiltIn.DiagnosticsNamespace }; Kind = Attribute }

    static member EnableTestingViaName =
        { FullName = { Name = "EnableTestingViaName"; Namespace = BuiltIn.DiagnosticsNamespace }; Kind = Attribute }

    static member DumpMachine =
        {
            FullName = { Name = "DumpMachine"; Namespace = BuiltIn.DiagnosticsNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Create "T")
        }

    static member DumpRegister =
        {
            FullName = { Name = "DumpRegister"; Namespace = BuiltIn.DiagnosticsNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Create "T")
        }

    // dependencies in Microsoft.Quantum.Llvm

    static member ReadCycleCounter =
        {
            FullName = { Name = "ReadCycleCounter"; Namespace = BuiltIn.LlvmNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Empty, IsSelfAdjoint = false)
        }

    // dependencies in Microsoft.Quantum.Canon

    static member NoOp =
        {
            FullName = { Name = "NoOp"; Namespace = BuiltIn.CanonNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false)
        }

    // dependencies in Microsoft.Quantum.Convert

    static member IntAsDouble =
        {
            FullName = { Name = "IntAsDouble"; Namespace = BuiltIn.ConvertNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Empty)
        }

    static member DoubleAsInt =
        {
            FullName = { Name = "DoubleAsInt"; Namespace = BuiltIn.ConvertNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Empty)
        }

    static member IntAsBigInt =
        {
            FullName = { Name = "IntAsBigInt"; Namespace = BuiltIn.ConvertNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Empty)
        }

    // dependencies in Microsoft.Quantum.Math

    static member Truncate =
        {
            FullName = { Name = "Truncate"; Namespace = BuiltIn.MathNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Empty)
        }

    // dependencies in Microsoft.Quantum.ClassicalControl

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit), 'T) , (('U => Unit), 'U)) => Unit)
    static member ApplyConditionally =
        {
            FullName = { Name = "ApplyConditionally"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false)
        }

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit is Adj), 'T) , (('U => Unit is Adj), 'U)) => Unit is Adj)
    static member ApplyConditionallyA =
        {
            FullName = { Name = "ApplyConditionallyA"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false)
        }

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit is Ctl), 'T) , (('U => Unit is Ctl), 'U)) => Unit is Ctl)
    static member ApplyConditionallyC =
        {
            FullName = { Name = "ApplyConditionallyC"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false)
        }

    // This is expected to have type <'T, 'U>((Result[], Result[], (('T => Unit is Adj + Ctl), 'T) , (('U => Unit is Adj + Ctl), 'U)) => Unit is Adj + Ctl)
    static member ApplyConditionallyCA =
        {
            FullName = { Name = "ApplyConditionallyCA"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false)
        }

    // This is expected to have type <'T>((Result, (('T => Unit), 'T)) => Unit)
    static member ApplyIfZero =
        {
            FullName = { Name = "ApplyIfZero"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false)
        }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj), 'T)) => Unit is Adj)
    static member ApplyIfZeroA =
        {
            FullName = { Name = "ApplyIfZeroA"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false)
        }

    // This is expected to have type <'T>((Result, (('T => Unit is Ctl), 'T)) => Unit is Ctl)
    static member ApplyIfZeroC =
        {
            FullName = { Name = "ApplyIfZeroC"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false)
        }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj + Ctl), 'T)) => Unit is Adj + Ctl)
    static member ApplyIfZeroCA =
        {
            FullName = { Name = "ApplyIfZeroCA"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false)
        }

    // This is expected to have type <'T>((Result, (('T => Unit), 'T)) => Unit)
    static member ApplyIfOne =
        {
            FullName = { Name = "ApplyIfOne"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false)
        }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj), 'T)) => Unit is Adj)
    static member ApplyIfOneA =
        {
            FullName = { Name = "ApplyIfOneA"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false)
        }

    // This is expected to have type <'T>((Result, (('T => Unit is Ctl), 'T)) => Unit is Ctl)
    static member ApplyIfOneC =
        {
            FullName = { Name = "ApplyIfOneC"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false)
        }

    // This is expected to have type <'T>((Result, (('T => Unit is Adj + Ctl), 'T)) => Unit is Adj + Ctl)
    static member ApplyIfOneCA =
        {
            FullName = { Name = "ApplyIfOneCA"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create "T", IsSelfAdjoint = false)
        }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit), 'T), (('U => Unit), 'U)) => Unit)
    static member ApplyIfElseR =
        {
            FullName = { Name = "ApplyIfElseR"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false)
        }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Adj), 'T), (('U => Unit is Adj), 'U)) => Unit is Adj)
    static member ApplyIfElseRA =
        {
            FullName = { Name = "ApplyIfElseRA"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false)
        }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Ctl), 'T), (('U => Unit is Ctl), 'U)) => Unit is Ctl)
    static member ApplyIfElseRC =
        {
            FullName = { Name = "ApplyIfElseRC"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false)
        }

    // This is expected to have type <'T, 'U>((Result, (('T => Unit is Adj + Ctl), 'T), (('U => Unit is Adj + Ctl), 'U)) => Unit is Adj + Ctl)
    static member ApplyIfElseRCA =
        {
            FullName = { Name = "ApplyIfElseRCA"; Namespace = BuiltIn.ClassicallyControlledNamespace }
            Kind = Operation(TypeParameters = ImmutableArray.Create("T", "U"), IsSelfAdjoint = false)
        }

    // dependencies in other namespaces (e.g. things used for code actions)

    static member IndexRange =
        {
            FullName = { Name = "IndexRange"; Namespace = BuiltIn.StandardArrayNamespace }
            Kind = Function(TypeParameters = ImmutableArray.Create "TElement")
        }
