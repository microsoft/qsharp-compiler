module Microsoft.Quantum.QsCompiler.Utils

/// Converts a C# Try-style return value into an F# option.
[<CompiledName "TryOption">]
let tryOption = function
    | true, value -> Some value
    | false, _ -> None
