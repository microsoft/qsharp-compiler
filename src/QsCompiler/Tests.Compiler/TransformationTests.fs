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
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit


// utils for testing syntax tree transformations and the corresponding infrastructure

type private GlobalDeclarations(parent : CheckDeclarations) =
    inherit NamespaceTransformation(parent, TransformationOptions.NoRebuild)

    override this.OnCallableDeclaration c = 
        parent.CheckCallableDeclaration c 

    override this.OnTypeDeclaration t = 
        parent.CheckTypeDeclaration t

    override this.OnSpecializationDeclaration s = 
        parent.CheckSpecializationDeclaration s

and private CheckDeclarations private (_internal_, onTypeDecl, onCallableDecl, onSpecDecl) =
    inherit SyntaxTreeTransformation()

    member internal this.CheckTypeDeclaration = onTypeDecl
    member internal this.CheckCallableDeclaration = onCallableDecl
    member internal this.CheckSpecializationDeclaration = onSpecDecl

    new (?onTypeDecl, ?onCallableDecl, ?onSpecDecl) as this =   
        let onTypeDecl = defaultArg onTypeDecl id
        let onCallableDecl = defaultArg onCallableDecl id
        let onSpecDecl = defaultArg onSpecDecl id
        CheckDeclarations("_internal_", onTypeDecl, onCallableDecl, onSpecDecl) then
            this.Types <- new TypeTransformation(this, TransformationOptions.Disabled)
            this.Expressions <- new ExpressionTransformation(this, TransformationOptions.Disabled)
            this.Statements <- new StatementTransformation(this, TransformationOptions.Disabled)
            this.Namespaces <- new GlobalDeclarations(this)


type private Counter () = 
    member val callsCount = 0 with get, set
    member val opsCount = 0 with get, set
    member val funCount = 0 with get, set
    member val udtCount = 0 with get, set
    member val forCount = 0 with get, set
    member val ifsCount = 0 with get, set            


type private SyntaxCounter private(counter : Counter, ?options) = 
    inherit SyntaxTreeTransformation(defaultArg options TransformationOptions.Default)

    member this.Counter = counter

    new (?options) as this =
        new SyntaxCounter(new Counter()) then
            this.Namespaces <- new SyntaxCounterNamespaces(this)
            this.StatementKinds <- new SyntaxCounterStatementKinds(this)
            this.ExpressionKinds <- new SyntaxCounterExpressionKinds(this)

and private SyntaxCounterNamespaces(parent : SyntaxCounter) = 
    inherit NamespaceTransformation(parent)

    override this.OnCallableDeclaration (node:QsCallable) =
        match node.Kind with
        | Operation ->       parent.Counter.opsCount <- parent.Counter.opsCount + 1
        | Function  ->       parent.Counter.funCount <- parent.Counter.funCount + 1
        | TypeConstructor -> ()
        base.OnCallableDeclaration node

    override this.OnTypeDeclaration (udt:QsCustomType) =
        parent.Counter.udtCount <- parent.Counter.udtCount + 1
        base.OnTypeDeclaration udt

and private SyntaxCounterStatementKinds(parent : SyntaxCounter) = 
    inherit StatementKindTransformation(parent)

    override this.OnConditionalStatement (node:QsConditionalStatement) =
        parent.Counter.ifsCount <- parent.Counter.ifsCount + 1
        base.OnConditionalStatement node
    
    override this.OnForStatement (node:QsForStatement) =
        parent.Counter.forCount <- parent.Counter.forCount + 1
        base.OnForStatement node

and private SyntaxCounterExpressionKinds(parent : SyntaxCounter) = 
    inherit ExpressionKindTransformation(parent)

    override this.OnCallLikeExpression (op,args) =
        parent.Counter.callsCount <- parent.Counter.callsCount + 1
        base.OnCallLikeExpression (op, args)


let private buildSyntaxTree code =
    let fileId = new Uri(Path.GetFullPath "test-file.qs") 
    let compilationUnit = new CompilationUnitManager(fun ex -> failwith ex.Message) 
    let file = CompilationUnitManager.InitializeFileManager(fileId, code)
    compilationUnit.AddOrUpdateSourceFileAsync file |> ignore  // spawns a task that modifies the current compilation
    let mutable syntaxTree = compilationUnit.Build().BuiltCompilation // will wait for any current tasks to finish
    CodeGeneration.GenerateFunctorSpecializations(syntaxTree, &syntaxTree) |> ignore
    syntaxTree


//////////////////////////////// tests //////////////////////////////////

[<Fact>]
let ``basic walk`` () = 
    let compilation = Path.Combine(Path.GetFullPath ".", "TestCases", "Transformation.qs") |> File.ReadAllText |> buildSyntaxTree
    let walker = new SyntaxCounter(TransformationOptions.NoRebuild)
    compilation.Namespaces |> Seq.iter (walker.Namespaces.OnNamespace >> ignore)
        
    Assert.Equal (4, walker.Counter.udtCount)
    Assert.Equal (1, walker.Counter.funCount)
    Assert.Equal (5, walker.Counter.opsCount)
    Assert.Equal (7, walker.Counter.forCount)
    Assert.Equal (6, walker.Counter.ifsCount)
    Assert.Equal (20, walker.Counter.callsCount)


[<Fact>]
let ``basic transformation`` () = 
    let compilation = Path.Combine(Path.GetFullPath ".", "TestCases", "Transformation.qs") |> File.ReadAllText |> buildSyntaxTree
    let walker = new SyntaxCounter()
    compilation.Namespaces |> Seq.iter (walker.Namespaces.OnNamespace >> ignore)
        
    Assert.Equal (4, walker.Counter.udtCount)
    Assert.Equal (1, walker.Counter.funCount)
    Assert.Equal (5, walker.Counter.opsCount)
    Assert.Equal (7, walker.Counter.forCount)
    Assert.Equal (6, walker.Counter.ifsCount)
    Assert.Equal (20, walker.Counter.callsCount)


[<Fact>]
let ``attaching attributes to callables`` () = 
    let WithinNamespace nsName (c : QsNamespaceElement) = c.GetFullName().Namespace.Value = nsName
    let attGenNs = "Microsoft.Quantum.Testing.AttributeGeneration"
    let sources = [
        Path.Combine(Path.GetFullPath ".", "TestCases", "LinkingTests", "Core.qs")
        Path.Combine(Path.GetFullPath ".", "TestCases", "AttributeGeneration.qs")
    ]
    let compilation = sources |> Seq.map File.ReadAllText |> String.Concat |> buildSyntaxTree
    let testAttribute = Attributes.BuildAttribute(BuiltIn.Test.FullName, Attributes.StringArgument "QuantumSimulator")

    let checkSpec (spec : QsSpecialization) = Assert.Empty spec.Attributes; spec
    let checkType (customType : QsCustomType) = 
        if customType |> QsCustomType |> WithinNamespace attGenNs then Assert.Empty customType.Attributes;
        customType
    let checkCallable limitedToNs (callable : QsCallable) = 
        if limitedToNs = null || callable |> QsCallable |> WithinNamespace limitedToNs then 
            Assert.Equal(1, callable.Attributes.Length)
            Assert.Equal(testAttribute, callable.Attributes.Single())
        else Assert.Empty callable.Attributes
        callable

    let transformed = Attributes.AddToCallables(compilation, testAttribute, QsCallable >> WithinNamespace attGenNs)
    let checker = new CheckDeclarations(checkType, checkCallable attGenNs, checkSpec)
    checker.Apply transformed |> ignore

    let transformed = Attributes.AddToCallables(compilation, testAttribute, null)
    let checker = new CheckDeclarations(checkType, checkCallable null, checkSpec)
    checker.Apply transformed |> ignore

    let transformed = Attributes.AddToCallables(compilation, testAttribute)
    let checker = new CheckDeclarations(checkType, checkCallable null, checkSpec)
    checker.Apply transformed |> ignore
    

[<Fact>]
let ``generation of open statements`` () = 
    let compilation = buildSyntaxTree @"
        namespace Microsoft.Quantum.Testing {
            operation emptyOperation () : Unit {}
        }"

    let ns = compilation.Namespaces |> Seq.head
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
    SyntaxTreeToQsharp.Apply (codeOutput, compilation.Namespaces, struct (source, imports)) |> Assert.True
    let lines = Utils.SplitLines (codeOutput.Value.Single().[ns.Name])
    Assert.Equal(13, lines.Count())

    Assert.Equal("open Microsoft.Quantum.Canon;", lines.[2].Trim())
    Assert.Equal("open Microsoft.Quantum.Intrinsic;", lines.[3].Trim())
    Assert.Equal("open Microsoft.Quantum.Arithmetic as Arithmetic;", lines.[4].Trim())
    Assert.Equal("open Microsoft.Quantum.Math as Math;", lines.[5].Trim())


