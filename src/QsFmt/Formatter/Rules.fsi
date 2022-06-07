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
val qubitBindingUpdate: unit Rewriter

/// Replaces `()` with `Unit` when referencing the Unit type.
/// Will not replace `()` when referencing the Unit value literal.
val unitUpdate: unit Rewriter

/// Updates for-loops to remove deprecated parentheses.
val forParensUpdate: unit Rewriter

/// Updates deprecated specialization declarations to add a `...` parameter.
val specializationUpdate: unit Rewriter

/// Updates the `new <Type>[n]` array syntax to the new `[val, size = n]` array syntax.
val arraySyntaxUpdate: unit Rewriter

/// Provides warnings for deprecated array syntax still in the syntax tree.
val checkArraySyntax: string -> Document -> string list

/// Replaces deprecated use of boolean operators `&&`, `||`, and `!` with their keyword
/// equivalence `and`, `or`, and `not` respectively.
val booleanOperatorUpdate: unit Rewriter
