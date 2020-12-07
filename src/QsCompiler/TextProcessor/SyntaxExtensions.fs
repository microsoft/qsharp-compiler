// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module private Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxExtensions

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTokens


type QsExpression with

    static member New(value, range) =
        { Expression = value
          Range = Value range }

    static member New(value, range) = { Expression = value; Range = range }


type QsSpecializationGenerator with

    // we do not currently process/parse type specializations

    static member New(value, range) =
        { Generator = value
          TypeArguments = Null
          Range = Value range }

    static member New(value, range) =
        { Generator = value
          TypeArguments = Null
          Range = range }


type QsSymbol with

    static member New(value, range) = { Symbol = value; Range = Value range }

    static member New(value, range) = { Symbol = value; Range = range }


type Characteristics with

    static member New(kind, range) =
        { Characteristics = kind
          Range = Value range }

    static member New(kind, range) =
        { Characteristics = kind
          Range = range }


type QsType with

    static member New(value, range) = { Type = value; Range = Value range }

    static member New(value, range) = { Type = value; Range = range }


type QsInitializer with

    static member New(value, range) =
        { Initializer = value
          Range = Value range }

    static member New(value, range) = { Initializer = value; Range = range }


type QsCompilerDiagnostic with

    /// Builds a diagnostic error for the given code and range, without any associated arguments.
    static member internal NewError code range =
        { Diagnostic = Error code
          Arguments = []
          Range = range }

    /// Builds a diagnostic warning for the given code and range, without any associated arguments.
    static member internal NewWarning code range =
        { Diagnostic = Warning code
          Arguments = []
          Range = range }

    /// Builds a diagnostic information for the given code and range, without any associated arguments.
    static member internal NewInfo code range =
        { Diagnostic = Information code
          Arguments = []
          Range = range }


type CallableSignature with

    static member internal New((typeParams, (argType, returnType)), characteristics) =
        { TypeParameters = typeParams
          Argument = argType
          ReturnType = returnType
          Characteristics = characteristics }

    static member internal Invalid =
        let invalidType = (InvalidType, Null) |> QsType.New
        let invalidSymbol = (InvalidSymbol, Null) |> QsSymbol.New

        let invalidArg =
            QsTuple
                ([ QsTupleItem(invalidSymbol, invalidType) ]
                    .ToImmutableArray())

        { TypeParameters = ImmutableArray.Empty
          Argument = invalidArg
          ReturnType = invalidType
          Characteristics =
              { Characteristics = InvalidSetExpr
                Range = Null } }


type QsFragment with

    member this.WithRange range =
        { Kind = this.Kind
          Range = range
          Diagnostics = this.Diagnostics
          Text = this.Text }
