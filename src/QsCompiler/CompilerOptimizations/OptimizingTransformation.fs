module Microsoft.Quantum.QsCompiler.CompilerOptimization.OptimizingTransformation

open Microsoft.Quantum.QsCompiler.Transformations.Core

open Printer


/// Represents a transformation meant to optimize a syntax tree
type OptimizingTransformation() =
    inherit SyntaxTreeTransformation()

    let mutable changed = false

    /// Returns whether the syntax tree has been modified since this function was last called
    member this.checkChanged() =
        let x = changed
        changed <- false
        x

    /// Checks whether the syntax tree changed at all
    override this.Transform x =
        let newX = base.Transform x
        if not (x.Equals newX) then
            let s1 = printNamespace x
            let s2 = printNamespace newX
            if s1 <> s2 then
                printfn "Made change! Size went from %d to %d" s1.Length s2.Length
                changed <- true
            else
                // this shouldn't happen, but sometimes does anyways
                ()
        newX

