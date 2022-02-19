// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxTree

open Microsoft.Quantum.QsCompiler.DataTypes

/// used to represent the names of declared type parameters or the name of the declared argument items of a callable
type QsLocalSymbol =
    | ValidName of string
    | InvalidName

/// used to represent information on typed expressions generated and/or tracked during compilation
type InferredExpressionInformation =
    {
        /// whether or not the value of this expression can be modified (true if it can)
        IsMutable: bool
        /// indicates whether the annotated expression directly or indirectly depends on an operation call within the surrounding implementation block
        /// -> it will be set to false for variables declared within the argument tuple
        /// -> using and borrowing are *not* considered to implicitly invoke a call to an operation, and are thus *not* considered to have a quantum dependency.
        HasLocalQuantumDependency: bool
    }

    /// Returns the inferred expression information used for declared parameters to callables.
    /// Note that this information is used within the declaration but not for the argument(s) in any given call-like expressions.
    static member ParameterDeclaration = { IsMutable = false; HasLocalQuantumDependency = false }

type LocalVariableDeclaration<'Name, 'Type> =
    {
        /// the name of the declared variable
        VariableName: 'Name
        /// the fully resolved type of the declared variable
        Type: 'Type
        /// contains information generated and/or tracked by the compiler
        /// -> in particular, contains the information about whether or not the symbol may be re-bound
        InferredInformation: InferredExpressionInformation
        /// Denotes the position where the variable is declared
        /// relative to the position of the specialization declaration within which the variable is declared.
        /// If the Position is Null, then the variable is not declared within a specialization (but belongs to a callable or type declaration).
        Position: QsNullable<Position>
        /// Denotes the range of the variable name relative to the position of the variable declaration.
        Range: Range
    }

    member this.WithName name =
        {
            VariableName = name
            Type = this.Type
            InferredInformation = this.InferredInformation
            Position = this.Position
            Range = this.Range
        }

    member this.WithType t =
        {
            VariableName = this.VariableName
            Type = t
            InferredInformation = this.InferredInformation
            Position = this.Position
            Range = this.Range
        }

    member this.WithPosition position = { this with Position = position }

    member this.WithRange range = { this with Range = range }

    member this.WithInferredInformation info =
        { this with InferredInformation = info }

module LocalVariableDeclaration =
    let New isMutable ((pos, range), vName: 'Name, t, hasLocalQuantumDependency) =
        {
            VariableName = vName
            Type = t
            InferredInformation = { IsMutable = isMutable; HasLocalQuantumDependency = hasLocalQuantumDependency }
            Position = pos
            Range = range
        }
