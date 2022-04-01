// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput

type Constraint =
    | Adjointable of operation: ResolvedType
    | Callable of callable: ResolvedType * input: ResolvedType * output: ResolvedType
    | Controllable of operation: ResolvedType * controlled: ResolvedType
    | Equatable of ty: ResolvedType
    | GenerateFunctors of callable: ResolvedType * functors: QsFunctor Set
    | Index of container: ResolvedType * index: ResolvedType * item: ResolvedType
    | Integral of ty: ResolvedType
    | Iterable of container: ResolvedType * item: ResolvedType
    | Numeric of ty: ResolvedType
    | PartialApplication of callable: ResolvedType * missing: ResolvedType * result: ResolvedType
    | Semigroup of ty: ResolvedType
    | Unwrap of container: ResolvedType * item: ResolvedType

module Constraint =
    let dependencies =
        function
        | Adjointable operation -> [ operation ]
        | Callable (callable, _, _) -> [ callable ]
        | Controllable (operation, _) -> [ operation ]
        | Equatable ty -> [ ty ]
        | GenerateFunctors (callable, _) -> [ callable ]
        | Index (container, index, _) -> [ container; index ]
        | Integral ty -> [ ty ]
        | Iterable (container, _) -> [ container ]
        | Numeric ty -> [ ty ]
        | PartialApplication (callable, _, _) -> [ callable ] // TODO: missing
        | Semigroup ty -> [ ty ]
        | Unwrap (container, _) -> [ container ]

    let types =
        function
        | Adjointable operation -> [ operation ]
        | Callable (callable, input, output) -> [ callable; input; output ]
        | Controllable (operation, controlled) -> [ operation; controlled ]
        | Equatable ty -> [ ty ]
        | GenerateFunctors (callable, _) -> [ callable ]
        | Index (container, index, item) -> [ container; index; item ]
        | Integral ty -> [ ty ]
        | Iterable (container, item) -> [ container; item ]
        | Numeric ty -> [ ty ]
        | PartialApplication (callable, missing, result) -> [ callable; missing; result ]
        | Semigroup ty -> [ ty ]
        | Unwrap (container, item) -> [ container; item ]

    let pretty ty =
        let p: ResolvedType -> _ = SyntaxTreeToQsharp.Default.ToCode

        match ty with
        | Adjointable operation -> sprintf "Adjointable<%s>" (p operation)
        | Callable (callable, input, output) -> sprintf "Callable<%s, %s, %s>" (p callable) (p input) (p output)
        | Controllable (operation, controlled) -> sprintf "Controllable<%s, %s>" (p operation) (p controlled)
        | Equatable ty -> sprintf "Equatable<%s>" (p ty)
        | GenerateFunctors (callable, functors) ->
            let functors = Seq.map string functors |> String.concat ", "
            sprintf "GenerateFunctors<%s, {%s}>" (p callable) functors
        | Index (container, index, item) -> sprintf "Indexed<%s, %s, %s>" (p container) (p index) (p item)
        | Integral ty -> sprintf "Integral<%s>" (p ty)
        | Iterable (container, item) -> sprintf "Iterable<%s, %s>" (p container) (p item)
        | Numeric ty -> sprintf "Numeric<%s>" (p ty)
        | PartialApplication (callable, missing, result) ->
            sprintf "PartialApplication<%s, %s, %s>" (p callable) (p missing) (p result)
        | Semigroup ty -> sprintf "Semigroup<%s>" (p ty)
        | Unwrap (container, item) -> sprintf "Unwrap<%s, %s>" (p container) (p item)

type Ordering =
    | Subtype
    | Equal
    | Supertype

module Ordering =
    let reverse =
        function
        | Subtype -> Supertype
        | Equal -> Equal
        | Supertype -> Subtype

type 'a Relation = Relation of lhs: 'a * ordering: Ordering * rhs: 'a

module RelationOps =
    let (<.) lhs rhs = Relation(lhs, Subtype, rhs)

    let (.=) lhs rhs = Relation(lhs, Equal, rhs)

    let (.>) lhs rhs = Relation(lhs, Supertype, rhs)
