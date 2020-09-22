namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.Quantum.QsCompiler
open Xunit
open System.Collections.Generic

type TestRewriteStep () =
    interface IRewriteStep with
        member this.AssemblyConstants: IDictionary<string,string> = new Dictionary<string, string>() :> IDictionary<string, string>
        member this.GeneratedDiagnostics: IEnumerable<IRewriteStep.Diagnostic> = Seq.empty 
        member this.ImplementsPostconditionVerification: bool = false
        member this.ImplementsPreconditionVerification: bool = false
        member this.ImplementsTransformation: bool = false
        member this.Name: string = "Test Rewrite Step"
        member this.PostconditionVerification(compilation: SyntaxTree.QsCompilation): bool = false
        member this.PreconditionVerification(compilation: SyntaxTree.QsCompilation): bool = false
        member this.Priority: int = 0
        member this.Transformation(compilation: SyntaxTree.QsCompilation, transformed: byref<SyntaxTree.QsCompilation>): bool = true

type ExternalRewriteStepsManagerTests () =
    
    [<Fact>]
    member this.``Loading Assembly based steps`` () =
        let config = new CompilationLoader.Configuration(RewriteSteps = [(this.GetType().Assembly.Location, "")])
        let manager = new ExternalRewriteStepsManager();
        let loadedSteps = manager.Load(config)

        Assert.NotEmpty loadedSteps
        Assert.Equal(1, loadedSteps.Length)

        let loadedStep = loadedSteps.[0]
        Assert.Equal("Test Rewrite Step", loadedStep.Name)

    [<Fact>]
    member this.``Loading Type based steps`` () =
        let config = new CompilationLoader.Configuration(RewriteStepTypes = [(typedefof<TestRewriteStep>, "")])
        let manager = new ExternalRewriteStepsManager();
        let loadedSteps = manager.Load(config);

        Assert.NotEmpty loadedSteps
        Assert.Equal(1, loadedSteps.Length)

        let loadedStep = loadedSteps.[0]
        Assert.Equal("Test Rewrite Step", loadedStep.Name)

    [<Fact>]
    member this.``Loading instance based steps`` () =
        let stepInstance = new TestRewriteStep()
        let config = new CompilationLoader.Configuration(RewriteStepInstances = [(stepInstance :> IRewriteStep, "")])
        let manager = new ExternalRewriteStepsManager();
        let loadedSteps = manager.Load(config);

        Assert.NotEmpty loadedSteps
        Assert.Equal(1, loadedSteps.Length)

        let loadedStep = loadedSteps.[0]
        Assert.Equal("Test Rewrite Step", loadedStep.Name)

    [<Fact>]
    member this.``Loading assembly, type and instance based steps simultaneously`` () =
        let stepInstance = new TestRewriteStep()
        let config = new CompilationLoader.Configuration(RewriteSteps = [(this.GetType().Assembly.Location, "")], RewriteStepTypes = [(typedefof<TestRewriteStep>, "")], RewriteStepInstances = [(stepInstance :> IRewriteStep, "")])
        let manager = new ExternalRewriteStepsManager();
        let loadedSteps = manager.Load(config);

        Assert.NotEmpty loadedSteps
        Assert.Equal(3, loadedSteps.Length)
        Assert.All(loadedSteps, fun step -> step.Name = "Test Rewrite Step" |> ignore)
