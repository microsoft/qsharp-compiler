// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file contains data structures that are used internally 
// for the purpose of symbol management and resolution. 
// They are not part of the resolved data structures, and are not intended to be used outside the Q# compiler. 

namespace Microsoft.Quantum.QsCompiler.SymbolManagement.DataStructures

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


/// This data structure is not intended to be used outside the Q# compiler. 
/// It is used to represent an unresolved attribute attached to a declaration.
type AttributeAnnotation = {
    Id : QsSymbol
    Argument : QsExpression
    Position : int * int
    Comments : QsComments
}
    with
    static member internal NonInterpolatedStringArgument inner = function 
        | Item arg -> inner arg |> function 
            | StringLiteral (str, interpol) when interpol.Length = 0 -> str.Value
            | _ -> null
        | _ -> null


/// This data structure is not intended to be used outside the Q# compiler. 
/// It is used internally for symbol resolution.
type internal Resolution<'T,'R> = internal {
    Position : int * int
    Range : QsPositionInfo * QsPositionInfo
    Defined : 'T
    Resolved : QsNullable<'R>
    DefinedAttributes : ImmutableArray<AttributeAnnotation>
    ResolvedAttributes : ImmutableArray<QsDeclarationAttribute>
    Documentation : ImmutableArray<string>
}


/// This data structure is not intended to be used outside the Q# compiler. 
/// It is used internally for symbol resolution.
type internal ResolvedGenerator = internal {
    TypeArguments   : QsNullable<ImmutableArray<ResolvedType>>
    Information     : CallableInformation
    Directive       : QsNullable<QsGeneratorDirective>
}


/// This data structure is not intended to be used outside the Q# compiler. 
/// It is used to group the relevant sets of specializations (i.e. group them according to type- and set-arguments).
type SpecializationBundleProperties = internal {
    BundleInfo : CallableInformation
    DefinedGenerators : ImmutableDictionary<QsSpecializationKind, QsSpecializationGenerator> 
}   with 

    /// Given the type- and set-arguments associated with a certain specialization, 
    /// determines the corresponding unique identifier for all specializations with the same type- and set-arguments.
    static member public BundleId (typeArgs : QsNullable<ImmutableArray<ResolvedType>>) = 
        typeArgs |> QsNullable<_>.Map (fun args -> (args |> Seq.map (fun t -> t.WithoutRangeInfo)).ToImmutableArray())

    /// Returns an identifier for the bundle to which the given specialization declaration belongs to. 
    /// Throws an InvalidOperationException if no (partial) resolution is defined for the given specialization. 
    static member internal BundleId (spec : Resolution<_,_>) = 
        match spec.Resolved with
        | Null -> InvalidOperationException "cannot determine id for unresolved specialization" |> raise
        | Value (gen : ResolvedGenerator) -> SpecializationBundleProperties.BundleId gen.TypeArguments 

    /// Given a function that associates an item of the given array with a particular set of type- and set-arguments, 
    /// as well as a function that associates it with a certain specialization kind, 
    /// returns a dictionary that contains a dictionary mapping the specialization kind to the corresponding item for each set of type arguments. 
    /// The keys for the returned dictionary are the BundleIds for particular sets of type- and characteristics-arguments. 
    static member public Bundle (getTypeArgs : Func<_,_>, getKind : Func<_,QsSpecializationKind>) (specs : IEnumerable<'T>) = 
        specs.ToLookup(new Func<_,_>(getTypeArgs.Invoke >> SpecializationBundleProperties.BundleId)).ToDictionary(
            (fun group -> group.Key), 
            (fun group -> group.ToImmutableDictionary(getKind)))



