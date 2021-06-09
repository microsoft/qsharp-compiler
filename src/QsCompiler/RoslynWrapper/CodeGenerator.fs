namespace Microsoft.Quantum.RoslynWrapper

/// <summary>
/// This is the entry point to the Roslyn Wrapper.
/// Pass a <see cref="CompilationUnitSyntax" /> to <see cref="generateCodeToString"/> to get string with the generated code.
/// </summary>
[<AutoOpen>]
module CodeGenerator =

    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let generateCodeToString (cu : CompilationUnitSyntax) =
        let fn = Formatting.Formatter.Format (cu, new AdhocWorkspace())
        let sb = new System.Text.StringBuilder()
        use sw = new System.IO.StringWriter (sb)
        fn.WriteTo(sw)
        sb.ToString()
        
