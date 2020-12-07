// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion

open System.Collections.Generic


/// Describes the scope of a code fragment in terms of what kind of completions are available.
type CompletionScope =
    /// The code fragment is at the top level of a file (outside a namespace).
    | TopLevel
    /// The code fragment is inside a namespace but outside any callable.
    | NamespaceTopLevel
    /// The code fragment is inside a function.
    | Function
    /// The code fragment is inside an operation at the top-level scope.
    | OperationTopLevel
    /// The code fragment is inside another scope within an operation.
    | Operation

/// Describes the kind of completion that is expected at a position in the source code.
type CompletionKind =
    /// The completion is the given keyword.
    | Keyword of string
    /// The completion is a variable.
    | Variable
    /// The completion is a mutable variable.
    | MutableVariable
    /// The completion is a callable.
    | Callable
    /// The completion is a user-defined type.
    | UserDefinedType
    /// The completion is a type parameter.
    | TypeParameter
    /// The completion is a new symbol declaration.
    | Declaration
    /// The completion is a namespace.
    | Namespace
    /// The completion is a member of the given namespace and has the given kind.
    | Member of string * CompletionKind
    /// The completion is a named item in a user-defined type.
    // TODO: Add information so completion knows the type being accessed.
    | NamedItem

/// The result of parsing a code fragment for completions.
type CompletionResult =
    /// The set of completion kinds is expected at the end of the code fragment.
    | Success of IEnumerable<CompletionKind>
    /// Parsing failed with an error message.
    | Failure of string
