// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput

type internal Constraint =
    | Adjointable
    | Callable of input: ResolvedType * output: ResolvedType
    | CanGenerateFunctors of functors: QsFunctor Set
    | Controllable of controlled: ResolvedType
    | Equatable
    | HasPartialApplication of missing: ResolvedType * result: ResolvedType
    | Indexed of index: ResolvedType * item: ResolvedType
    | Integral
    | Iterable of item: ResolvedType
    | Numeric
    | Semigroup
    | Wrapped of item: ResolvedType

module internal Constraint =
    /// Pretty prints a type.
    let private prettyType : ResolvedType -> _ = SyntaxTreeToQsharp.Default.ToCode

    let types =
        function
        | Adjointable -> []
        | Callable (input, output) -> [ input; output ]
        | CanGenerateFunctors _ -> []
        | Controllable controlled -> [ controlled ]
        | Equatable -> []
        | HasPartialApplication (missing, result) -> [ missing; result ]
        | Indexed (index, item) -> [ index; item ]
        | Integral -> []
        | Iterable item -> [ item ]
        | Numeric -> []
        | Semigroup -> []
        | Wrapped item -> [ item ]

    let pretty =
        function
        | Adjointable -> "Adjointable"
        | Callable (input, output) -> sprintf "Callable(%s, %s)" (prettyType input) (prettyType output)
        | CanGenerateFunctors functors ->
            sprintf "CanGenerateFunctors(%s)" (functors |> Seq.map string |> String.concat ", ")
        | Controllable controlled -> sprintf "Controllable(%s)" (prettyType controlled)
        | Equatable -> "Equatable"
        | HasPartialApplication (missing, result) ->
            sprintf "HasPartialApplication(%s, %s)" (prettyType missing) (prettyType result)
        | Indexed (index, item) -> sprintf "Indexed(%s, %s)" (prettyType index) (prettyType item)
        | Integral -> "Integral"
        | Iterable item -> sprintf "Iterable(%s)" (prettyType item)
        | Numeric -> "Numeric"
        | Semigroup -> "Semigroup"
        | Wrapped item -> sprintf "Wrapped(%s)" (prettyType item)
