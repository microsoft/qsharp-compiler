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
