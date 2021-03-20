// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.TypeInference

open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

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
