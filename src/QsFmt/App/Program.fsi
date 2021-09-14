// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// The command-line interface for the Q# formatter.
module Microsoft.Quantum.QsFmt.App.Program

/// The Update Verb
type UpdateOptions =
  { Backup: bool
    Recurse: bool
    Input: seq<string> }
  with
    static member examples : seq<CommandLine.Text.Example>
  end

/// The Format Verb
type FormatOptions =
  { Backup: bool
    Recurse: bool
    Input: seq<string> }
  with
    static member examples : seq<CommandLine.Text.Example>
  end

/// Runs the Q# formatter.
[<CompiledName "Main">]
val main : args:string [] -> int
