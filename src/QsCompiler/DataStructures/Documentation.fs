// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Documentation.BuiltIn

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


type QsType with

    /// Allows to partially resolve a transformation characteristics expression for documentation purposes.
    static member TryResolve(ex: Characteristics) =
        let extract (a: Characteristics) = a.Characteristics

        match ResolvedCharacteristics.ExtractProperties extract ex with
        | None -> InvalidSetExpr |> ResolvedCharacteristics.New
        | Some props -> ResolvedCharacteristics.FromProperties props

    member public this.Documentation: ImmutableArray<string> =
        let doc =
            match this.Type with
            | QsTypeKind.UnitType -> [ "# Summary"; "Represents a singleton type whose only value is \"()\"." ]
            | QsTypeKind.Int ->
                [
                    "# Summary"
                    "Represents a 64-bit signed integer."
                    "Values range from -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807."
                ]
            | QsTypeKind.BigInt -> [ "# Summary"; "Represents a signed integer of any size." ]
            | QsTypeKind.Double ->
                [
                    "# Summary"
                    "Represents a double-precision 64-bit floating-point number."
                    "Values range from -1.79769313486232e308 to 1.79769313486232e308 as well as NaN (not a number)."
                ]
            | QsTypeKind.Bool -> [ "# Summary"; "Represents a Boolean (true or false) value." ]
            | QsTypeKind.String -> [ "# Summary"; "Represents text as a sequence of UTF-16 code units." ]
            | QsTypeKind.Range ->
                [
                    "# Summary"
                    "Represents an ordered sequence of equally spaced 64-bit signed integers."
                    "The sequence may be in ascending or descending order, or empty."
                ]
            | QsTypeKind.Pauli -> [ "# Summary"; "Represents a single-qubit Pauli matrix." ]
            | QsTypeKind.Result ->
                [
                    "# Summary"
                    "Represents the result of a projective measurement onto the eigenspaces of a quantum operator with eigenvalues ±1."
                    "\"Zero\" indicates a projection onto the +1 eigenspace, \"One\" indicates a projection onto the -1 eigenspace."
                ]
            | QsTypeKind.Qubit ->
                [
                    "# Summary"
                    "Represents an opaque identifier by which logical qubits can be addressed."
                ]
            | QsTypeKind.ArrayType _ ->
                [
                    "# Summary"
                    "Represents a data structure containing a sequence of objects of the same type."
                ]
            | QsTypeKind.TupleType _ ->
                [
                    "# Summary"
                    "Represents a data structure containing a fixed number of elements of different types."
                    "Tuples containing a single element are equivalent to the element they contain."
                ]
            | QsTypeKind.Function _ ->
                [
                    "# Summary"
                    "Represents a deterministic callable"
                    "that takes exactly one input argument of the type specified to the left of the arrow."
                    "and returns one output value of the type specified to the right of the arrow."
                    "The side effects and output value of a function are always fully defined by its input argument."
                ]
            | QsTypeKind.Operation (_, characteristics) ->
                let supportedFunctors =
                    (characteristics |> QsType.TryResolve).SupportedFunctors.ValueOr ImmutableHashSet.Empty

                let adj, ctl = supportedFunctors |> Seq.contains Adjoint, supportedFunctors |> Seq.contains Controlled

                let functors =
                    match adj, ctl with
                    | true, true -> " supporting both the Adjoint and Controlled functor,"
                    | true, false -> " supporting the Adjoint functor,"
                    | false, true -> " supporting the Controlled functor,"
                    | false, false -> ","

                [
                    "# Summary"
                    "Represents a non-deterministic callable" + functors
                    "that takes exactly one input argument of the type specified to the left of the arrow"
                    "and returns one output value of the type specified to the right of the arrow."
                    "Side effects and output value may vary from operation call to operation call."
                ]
            | QsTypeKind.UserDefinedType _ ->
                [
                    "# Summary"
                    "Represents a user defined type. User defined types consist of named and anonymous items of different types."
                ]
            | QsTypeKind.TypeParameter _ ->
                [
                    "# Summary"
                    "Represents a type given as argument to the containing template."
                ]
            | QsTypeKind.InvalidType ->
                [
                    "# Summary"
                    "Represents an object whose type is unknown due to compilation errors. "
                ]
            | QsTypeKind.MissingType ->
                [
                    "# Summary"
                    "Represents the type of a missing object, e.g. in partial applications."
                ]

        doc.ToImmutableArray()


type QsExpression with
    member public this.Documentation =
        let isRange (ex: QsExpression) =
            match ex.Expression with
            | RangeLiteral _ -> true
            | _ -> false

        let doc =
            match this.Expression with
            | QsExpressionKind.UnitValue -> [ "# Summary"; "Unit value." ]
            | QsExpressionKind.IntLiteral _ -> [ "# Summary"; "64-bit integer." ]
            | QsExpressionKind.BigIntLiteral _ -> [ "# Summary"; "Arbitrary-sized integer." ]
            | QsExpressionKind.DoubleLiteral _ -> [ "# Summary"; "Double-precision 64-bit floating-point number." ]
            | QsExpressionKind.BoolLiteral _ -> [ "# Summary"; "Boolean value." ]
            | QsExpressionKind.StringLiteral _ -> [ "# Summary"; "Text stored as a sequence of UTF-16 code units." ]
            | QsExpressionKind.RangeLiteral (ex, _) ->
                if ex |> isRange then
                    [
                        "# Summary"
                        "Ordered sequence of equally spaced 64-bit signed integers."
                        "The leftmost value indicates the first element in the sequence."
                        "Each subsequent value in the sequence adds the middle value to the preceding element,"
                        "until the next element would be larger than the rightmost value."
                    ]
                else
                    [
                        "# Summary"
                        "Ordered sequence of equally spaced 64-bit signed integers."
                        "The left value indicates the first element in the sequence."
                        "Each subsequent value in the sequence increments the preceding element by 1,"
                        "until the next element would be larger than the right value."
                    ]
            | QsExpressionKind.PauliLiteral ex ->
                match ex with
                | PauliI -> // TODO: link
                    [ "# Summary"; "Identity matrix." ]
                | PauliX -> // TODO: link
                    [ "# Summary"; "Pauli matrix σ\u2081." ]
                | PauliY -> // TODO: link
                    [ "# Summary"; "Pauli matrix σ\u2082." ]
                | PauliZ -> // TODO: link
                    [ "# Summary"; "Pauli matrix σ\u2083." ]
            | QsExpressionKind.ResultLiteral ex ->
                let eigval =
                    match ex with
                    | Zero -> "+1"
                    | One -> "-1"

                [
                    "# Summary"
                    "The result of a measurement that projected the state onto the "
                    + eigval
                    + " eigenspace of the measured quantum operator."
                ]
            | QsExpressionKind.ValueTuple _ -> [ "# Summary"; "Value tuple consisting of the specified items." ]
            | QsExpressionKind.NewArray _ ->
                [
                    "# Summary"
                    "Creates an array of the specified length initialized to the default values for the specified type."
                ]
            | QsExpressionKind.ValueArray _ -> [ "# Summary"; "Value array containing the specified elements." ]
            | QsExpressionKind.ArrayItem _ ->
                [ "# Summary"; "Accesses the element at the specified index of the array." ]
            | QsExpressionKind.AdjointApplication _ ->
                [ "# Summary"; "Applies the Adjoint functor to the right hand side." ]
            | QsExpressionKind.ControlledApplication _ ->
                [ "# Summary"; "Applies the Controlled functor to the right hand side." ]
            | QsExpressionKind.UnwrapApplication _ ->
                [
                    "# Summary"
                    "Postfix operator accessing the value stored within an object of user defined type."
                ]
            | QsExpressionKind.InvalidExpr -> [ "# Summary"; "Invalid expression due to compilation errors." ]
            | QsExpressionKind.MissingExpr ->
                [ "# Summary"; "Indicates a missing value, e.g. in partial applications." ]
            | QsExpressionKind.CallLikeExpression _ -> []
            | QsExpressionKind.Identifier _ -> []
            // TODO: documentation for all operators
            | _ -> []

        doc.ToImmutableArray()
