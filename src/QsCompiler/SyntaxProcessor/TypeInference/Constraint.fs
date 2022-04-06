// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput

type ClassConstraint =
    | Adjointable of operation: ResolvedType
    | Callable of callable: ResolvedType * input: ResolvedType * output: ResolvedType
    | Controllable of operation: ResolvedType * controlled: ResolvedType
    | Eq of ResolvedType
    | HasFunctorsIfOperation of callable: ResolvedType * functors: QsFunctor Set
    | HasPartialApplication of callable: ResolvedType * missing: ResolvedType * callable': ResolvedType
    | Index of container: ResolvedType * index: ResolvedType * item: ResolvedType
    | Integral of ResolvedType
    | Iterable of container: ResolvedType * item: ResolvedType
    | Num of ResolvedType
    | Semigroup of ResolvedType
    | Unwrap of container: ResolvedType * item: ResolvedType

    override cls.ToString() =
        let p: ResolvedType -> _ = SyntaxTreeToQsharp.Default.ToCode

        match cls with
        | Adjointable operation -> sprintf "Adjointable<%s>" (p operation)
        | Callable (callable, input, output) -> sprintf "Callable<%s, %s, %s>" (p callable) (p input) (p output)
        | Controllable (operation, controlled) -> sprintf "Controllable<%s, %s>" (p operation) (p controlled)
        | Eq ty -> sprintf "Eq<%s>" (p ty)
        | HasFunctorsIfOperation (callable, functors) ->
            let functors = Seq.map string functors |> String.concat ", "
            sprintf "HasFunctorsIfOperation<%s, {%s}>" (p callable) functors
        | HasPartialApplication (callable, missing, callable') ->
            sprintf "HasPartialApplication<%s, %s, %s>" (p callable) (p missing) (p callable')
        | Index (container, index, item) -> sprintf "Index<%s, %s, %s>" (p container) (p index) (p item)
        | Integral ty -> sprintf "Integral<%s>" (p ty)
        | Iterable (container, item) -> sprintf "Iterable<%s, %s>" (p container) (p item)
        | Num ty -> sprintf "Num<%s>" (p ty)
        | Semigroup ty -> sprintf "Semigroup<%s>" (p ty)
        | Unwrap (container, item) -> sprintf "Unwrap<%s, %s>" (p container) (p item)

module ClassConstraint =
    let types =
        function
        | Adjointable operation -> [ operation ]
        | Callable (callable, input, output) -> [ callable; input; output ]
        | Controllable (operation, controlled) -> [ operation; controlled ]
        | Eq ty -> [ ty ]
        | HasFunctorsIfOperation (callable, _) -> [ callable ]
        | HasPartialApplication (callable, missing, callable') -> [ callable; missing; callable' ]
        | Index (container, index, item) -> [ container; index; item ]
        | Integral ty -> [ ty ]
        | Iterable (container, item) -> [ container; item ]
        | Num ty -> [ ty ]
        | Semigroup ty -> [ ty ]
        | Unwrap (container, item) -> [ container; item ]

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

type Constraint =
    | Class of ClassConstraint
    | Relation of ResolvedType * Ordering * ResolvedType

module Constraint =
    let types =
        function
        | Class cls -> ClassConstraint.types cls
        | Relation (lhs, _, rhs) -> [ lhs; rhs ]

module RelationOps =
    let (<.) lhs rhs = Relation(lhs, Subtype, rhs)

    let (.=) lhs rhs = Relation(lhs, Equal, rhs)

    let (.>) lhs rhs = Relation(lhs, Supertype, rhs)
