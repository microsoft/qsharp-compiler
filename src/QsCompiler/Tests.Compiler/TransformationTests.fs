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
open Microsoft.Quantum.QsCompiler.Transformations
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

type private StatementKindCounter(stm, counter : Counter) = 
    inherit StatementKindTransformation<StatementCounter>(stm)

    override this.onConditionalStatement (node:QsConditionalStatement) =
        counter.ifsCount <- counter.ifsCount + 1
        base.onConditionalStatement node
        
    override this.onForStatement (node:QsForStatement) =
        counter.forCount <- counter.forCount + 1
        base.onForStatement node

and private StatementCounter(counter) = 
    inherit ScopeTransformation<StatementKindCounter, ExpressionCounter>
            (Func<_,_>(fun s -> new StatementKindCounter(s :?> StatementCounter, counter)), new ExpressionCounter(counter))
                
and private ExpressionKindCounter(ex, counter : Counter) = 
    inherit ExpressionKindTransformation<ExpressionCounter>(ex)

    override this.beforeCallLike (op,args) =
        counter.callsCount <- counter.callsCount + 1
        (op,args)

and private ExpressionCounter(counter) = 
    inherit ExpressionTransformation<ExpressionKindCounter>
            (new Func<_,_>(fun e -> new ExpressionKindCounter(e :?> ExpressionCounter, counter)))


type private SyntaxCounter(counter) = 
    inherit SyntaxTreeTransformation<StatementCounter>(new StatementCounter(counter))

    override this.beforeCallable (node:QsCallable) =
        match node.Kind with
        | Operation ->       counter.opsCount <- counter.opsCount + 1
        | Function  ->       counter.funCount <- counter.funCount + 1
        | TypeConstructor -> ()
        node
    
    override this.onType (udt:QsCustomType) =
        counter.udtCount <- counter.udtCount + 1
        base.onType udt


let private buildSyntaxTree code =
    let fileId = new Uri(Path.GetFullPath "test-file.qs") 
    let compilationUnit = new CompilationUnitManager(fun ex -> failwith ex.Message) 
    let file = CompilationUnitManager.InitializeFileManager(fileId, code)
    compilationUnit.AddOrUpdateSourceFileAsync file |> ignore  // spawns a task that modifies the current compilation
    let mutable syntaxTree = compilationUnit.GetSyntaxTree()   // will wait for any current tasks to finish
    FunctorGeneration.GenerateFunctorSpecializations(syntaxTree, &syntaxTree) |> ignore
    syntaxTree


//////////////////////////////// tests //////////////////////////////////

[<Fact>]
let ``basic walk`` () = 
    let tree = Path.Combine(Path.GetFullPath ".", "TestCases", "Transformation.qs") |> File.ReadAllText |> buildSyntaxTree
    let counter = new Counter()
    tree |> Seq.map (SyntaxCounter(counter)).Transform |> Seq.toList |> ignore
        
    Assert.Equal (4, counter.udtCount)
    Assert.Equal (1, counter.funCount)
    Assert.Equal (5, counter.opsCount)
    Assert.Equal (7, counter.forCount)
    Assert.Equal (6, counter.ifsCount)
    Assert.Equal (20, counter.callsCount)


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


