namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.Quantum.QsCompiler
open Xunit
open System.Collections.Generic
open System.Collections.Immutable

type TestRewriteStep(priority: int) =
    interface IRewriteStep with
        member this.AssemblyConstants: IDictionary<string, string> =
            new Dictionary<string, string>() :> IDictionary<string, string>

        member this.GeneratedDiagnostics: IEnumerable<IRewriteStep.Diagnostic> = Seq.empty
        member this.ImplementsPostconditionVerification: bool = false
        member this.ImplementsPreconditionVerification: bool = false
        member this.ImplementsTransformation: bool = false
        member this.Name: string = "Test Rewrite Step"
        member this.PostconditionVerification(compilation: SyntaxTree.QsCompilation): bool = false
        member this.PreconditionVerification(compilation: SyntaxTree.QsCompilation): bool = false
        member this.Priority: int = priority

        member this.Transformation(compilation: SyntaxTree.QsCompilation, transformed: byref<SyntaxTree.QsCompilation>)
                                   : bool =
            true

    new() = TestRewriteStep(0)

type ExternalRewriteStepsManagerTests() =

    let GetSteps config =
        ExternalRewriteStepsManager.Load(config, null, null)

    let AssertLength (expectedLength, loadedSteps: ImmutableArray<LoadedStep>) =
        Assert.NotEmpty loadedSteps
        Assert.Equal(expectedLength, loadedSteps.Length)

    let VerifyStep (loadedSteps: ImmutableArray<LoadedStep>) =
        AssertLength(1, loadedSteps)

        let loadedStep = loadedSteps.[0]
        Assert.Equal("Test Rewrite Step", loadedStep.Name)

    [<Fact>]
    member this.``No steps``() =
        let config = new CompilationLoader.Configuration()
        let loadedSteps = GetSteps config
        Assert.Empty loadedSteps

    [<Fact>]
    member this.``Loading Assembly based steps (legacy)``() =
        let config =
            new CompilationLoader.Configuration(RewriteSteps = [ (this.GetType().Assembly.Location, "") ])

        let loadedSteps = GetSteps config
        VerifyStep loadedSteps

    [<Fact>]
    member this.``Loading Assembly based steps``() =
        let config =
            new CompilationLoader.Configuration(RewriteStepAssemblies = [ (this.GetType().Assembly.Location, "") ])

        let loadedSteps = GetSteps config
        VerifyStep loadedSteps

    [<Fact>]
    member this.``Loading Type based steps``() =
        let config =
            new CompilationLoader.Configuration(RewriteStepTypes = [ (typedefof<TestRewriteStep>, "") ])

        let loadedSteps = GetSteps config
        VerifyStep loadedSteps

    [<Fact>]
    member this.``Loading instance based steps``() =
        let stepInstance = new TestRewriteStep()

        let config =
            new CompilationLoader.Configuration(RewriteStepInstances = [ (stepInstance :> IRewriteStep, "") ])

        let loadedSteps = GetSteps config
        VerifyStep loadedSteps

    [<Fact>]
    member this.``Loading assembly, type and instance based steps simultaneously``() =
        let stepInstance = new TestRewriteStep()

        let config =
            new CompilationLoader.Configuration(RewriteStepAssemblies = [ (this.GetType().Assembly.Location, "") ],
                                                RewriteStepTypes = [ (typedefof<TestRewriteStep>, "") ],
                                                RewriteStepInstances = [ (stepInstance :> IRewriteStep, "") ])

        let loadedSteps = GetSteps config

        AssertLength(3, loadedSteps)
        Assert.All(loadedSteps, (fun step -> step.Name = "Test Rewrite Step" |> ignore))

    [<Fact>]
    member this.``Steps are ordered``() =
        let stepInstance1 = new TestRewriteStep -10
        let stepInstance2 = new TestRewriteStep 20

        let config =
            new CompilationLoader.Configuration(RewriteStepTypes = [ (typedefof<TestRewriteStep>, "") ],
                                                RewriteStepInstances =
                                                    [
                                                        (stepInstance1 :> IRewriteStep, "")
                                                        (stepInstance2 :> IRewriteStep, "")
                                                    ])

        let loadedSteps = GetSteps config

        AssertLength(3, loadedSteps)
        Assert.Equal(20, loadedSteps.[0].Priority)
        Assert.Equal(0, loadedSteps.[1].Priority)
        Assert.Equal(-10, loadedSteps.[2].Priority)
