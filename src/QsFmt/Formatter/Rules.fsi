// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// Syntax tree rewriters for formatting rules.
module internal Microsoft.Quantum.QsFmt.Formatter.Rules

open Microsoft.Quantum.QsFmt.Formatter.SyntaxTree

/// Collapses adjacent whitespace characters into a single space character.
val collapsedSpaces: unit Rewriter

/// Ensures that operators are spaced correctly relative to their operands.
val operatorSpacing: unit Rewriter

/// Applies correct indentation.
val indentation: int Rewriter

/// Ensures that new lines are used where needed.
val newLines: unit Rewriter

/// Replaces `using` and `borrowing` with `use` and `borrow` respectively.
/// Removes parentheses around qubit bindings.
val qubitBindingUpdate : unit Rewriter

/// Updates the `new <Type>[n]` array syntax to the new `[val, size = n]` array syntax.
val arraySyntaxUpdate : unit Rewriter

/// Provides warnings for deprecated syntax still in the syntax tree.
val updateChecker : string list Reducer
