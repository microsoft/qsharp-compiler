open System.IO
open QsFmt.Formatter

/// Runs the Q# formatter.
[<EntryPoint>]
let private main args =
    if Array.isEmpty args then
        stdin.ReadToEnd()
        |> Formatter.format
        |> printfn "%s"
    else
        args
        |> Array.map (File.ReadAllText >> Formatter.format)
        |> Array.iter (printfn "%s")

    0
