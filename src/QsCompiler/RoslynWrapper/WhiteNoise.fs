namespace Microsoft.Quantum.RoslynWrapper

[<AutoOpen>]
module WhiteNoise =
    open Microsoft.CodeAnalysis.CSharp

    let ``:`` = None
    let ``,`` = None
    let ``}`` = None
    let ``{`` = None
    let ``<<`` = None
    let ``>>`` = None
    let ``(`` = None
    let ``)`` = None

    let ``of`` = None
    let ``get`` = None
    let ``set`` = None
    let ``in`` = None

    let ``private`` = SyntaxKind.PrivateKeyword
    let ``protected`` = SyntaxKind.ProtectedKeyword
    let ``internal`` = SyntaxKind.InternalKeyword
    let ``public`` = SyntaxKind.PublicKeyword
    let ``partial`` = SyntaxKind.PartialKeyword
    let ``abstract`` = SyntaxKind.AbstractKeyword
    let ``async`` = SyntaxKind.AsyncKeyword
    let ``virtual`` = SyntaxKind.VirtualKeyword
    let ``override`` = SyntaxKind.OverrideKeyword
    let ``static`` = SyntaxKind.StaticKeyword
    let ``readonly`` = SyntaxKind.ReadOnlyKeyword
    let ``const`` = SyntaxKind.ConstKeyword
