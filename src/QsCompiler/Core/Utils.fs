module internal Microsoft.Quantum.QsCompiler.Utils

/// Converts a C# Try-style return value into an F# option.
let tryToOption =
    function
    | true, value -> Some value
    | false, _ -> None
