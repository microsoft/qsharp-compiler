// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.TransformationTests

open System
open System.Collections.Immutable
open System.IO
open System.Linq
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit


// utils for testing syntax tree transformations and the corresponding infrastructure

type private Counter () = 
    member val callsCount = 0 with get, set
    member val opsCount = 0 with get, set
    member val funCount = 0 with get, set
    member val udtCount = 0 with get, set
    member val forCount = 0 with get, set
    member val ifsCount = 0 with get, set            


type private SyntaxCounter private(counter : Counter, ?options) = 
    inherit QsSyntaxTreeTransformation(defaultArg options TransformationOptions.Default)

    member this.Counter = counter

    new (?options) as this =
        new SyntaxCounter(new Counter()) then
            this.Namespaces <- new SyntaxCounterNamespaces(this)
            this.StatementKinds <- new SyntaxCounterStatementKinds(this)
            this.ExpressionKinds <- new SyntaxCounterExpressionKinds(this)

and private SyntaxCounterNamespaces(parent : SyntaxCounter) = 
    inherit NamespaceTransformation(parent)

    override this.beforeCallable (node:QsCallable) =
        match node.Kind with
        | Operation ->       parent.Counter.opsCount <- parent.Counter.opsCount + 1
        | Function  ->       parent.Counter.funCount <- parent.Counter.funCount + 1
        | TypeConstructor -> ()
        node

    override this.onType (udt:QsCustomType) =
        parent.Counter.udtCount <- parent.Counter.udtCount + 1
        base.onType udt

and private SyntaxCounterStatementKinds(parent : SyntaxCounter) = 
    inherit StatementKindTransformation(parent)

    override this.onConditionalStatement (node:QsConditionalStatement) =
        parent.Counter.ifsCount <- parent.Counter.ifsCount + 1
        base.onConditionalStatement node
    
    override this.onForStatement (node:QsForStatement) =
        parent.Counter.forCount <- parent.Counter.forCount + 1
        base.onForStatement node

and private SyntaxCounterExpressionKinds(parent : SyntaxCounter) = 
    inherit ExpressionKindTransformation(parent)

    override this.beforeCallLike (op,args) =
        parent.Counter.callsCount <- parent.Counter.callsCount + 1
        base.beforeCallLike (op, args)


let private buildSyntaxTree code =
    let fileId = new Uri(Path.GetFullPath "test-file.qs") 
    let compilationUnit = new CompilationUnitManager(fun ex -> failwith ex.Message) 
    let file = CompilationUnitManager.InitializeFileManager(fileId, code)
    compilationUnit.AddOrUpdateSourceFileAsync file |> ignore  // spawns a task that modifies the current compilation
    let mutable syntaxTree = compilationUnit.Build().BuiltCompilation // will wait for any current tasks to finish
    CodeGeneration.GenerateFunctorSpecializations(syntaxTree, &syntaxTree) |> ignore
    syntaxTree.Namespaces


//////////////////////////////// tests //////////////////////////////////

[<Fact>]
let ``basic walk`` () = 
    let tree = Path.Combine(Path.GetFullPath ".", "TestCases", "Transformation.qs") |> File.ReadAllText |> buildSyntaxTree
    let walker = new SyntaxCounter(TransformationOptions.NoRebuild)
    tree |> Seq.iter (walker.Namespaces.Transform >> ignore)
        
    Assert.Equal (4, walker.Counter.udtCount)
    Assert.Equal (1, walker.Counter.funCount)
    Assert.Equal (5, walker.Counter.opsCount)
    Assert.Equal (7, walker.Counter.forCount)
    Assert.Equal (6, walker.Counter.ifsCount)
    Assert.Equal (20, walker.Counter.callsCount)

[<Fact>]
let ``basic transformation`` () = 
    let tree = Path.Combine(Path.GetFullPath ".", "TestCases", "Transformation.qs") |> File.ReadAllText |> buildSyntaxTree
    let walker = new SyntaxCounter()
    tree |> Seq.iter (walker.Namespaces.Transform >> ignore)
        
    Assert.Equal (4, walker.Counter.udtCount)
    Assert.Equal (1, walker.Counter.funCount)
    Assert.Equal (5, walker.Counter.opsCount)
    Assert.Equal (7, walker.Counter.forCount)
    Assert.Equal (6, walker.Counter.ifsCount)
    Assert.Equal (20, walker.Counter.callsCount)

[<Fact>]
let ``generation of open statements`` () = 
    let tree = buildSyntaxTree @"
        namespace Microsoft.Quantum.Testing {
            operation emptyOperation () : Unit {}
        }"

    let ns = tree |> Seq.head
    let source = ns.Elements.Single() |> function
        | QsCallable callable -> callable.SourceFile
        | QsCustomType t -> t.SourceFile

    let openExplicit =
        let directive name = struct (sprintf "Microsoft.Quantum.%s" name |> NonNullable<string>.New, null)
        ["Canon"; "Intrinsic"] |> List.map directive

    let openAbbrevs =
        let directive name = struct (sprintf "Microsoft.Quantum.%s" name |> NonNullable<string>.New, name)
        ["Arithmetic"; "Math"] |> List.map directive
    
    let openDirectives = openExplicit @ openAbbrevs |> ImmutableArray.ToImmutableArray
    let imports = ImmutableDictionary.Empty.Add(ns.Name, openDirectives)

    let codeOutput = ref null
    SyntaxTreeToQs.Apply (codeOutput, tree, struct (source, imports)) |> Assert.True
    let lines = Utils.SplitLines (codeOutput.Value.Single().[ns.Name])
    Assert.Equal(13, lines.Count())

    Assert.Equal("open Microsoft.Quantum.Canon;", lines.[2].Trim())
    Assert.Equal("open Microsoft.Quantum.Intrinsic;", lines.[3].Trim())
    Assert.Equal("open Microsoft.Quantum.Arithmetic as Arithmetic;", lines.[4].Trim())
    Assert.Equal("open Microsoft.Quantum.Math as Math;", lines.[5].Trim())


