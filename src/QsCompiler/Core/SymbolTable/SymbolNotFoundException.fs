namespace Microsoft.Quantum.QsCompiler.SymbolManagement

open System

/// An exception that is thrown when a symbol could not be found in the symbol table.
type SymbolNotFoundException(message) =
    inherit Exception(message)
