module Microsoft.Quantum.QsCompiler.Testing.CapabilityInferenceTests

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit

/// A mapping of all callables in the capability verification tests, after inferring capabilities.
let private callables =
    CompilerTests.Compile ("TestCases", [ "CapabilityVerification.qs" ])
    |> fun compilation -> compilation.BuiltCompilation
    |> CapabilityInference.InferCapabilities
    |> fun compilation -> compilation.Namespaces
    |> GlobalCallableResolutions

/// Returns the inferred capabilities of the callable based on its attributes.
let private attributes (callable : QsCallable) =
    let extractString = function
        | StringLiteral (value, _) -> value.Value
        | _ -> failwith "Expression is not a string."

    callable.Attributes
    |> QsNullable<_>.Choose (fun attribute ->
           attribute.TypeId |> QsNullable<_>.Map (fun name -> name, attribute.Argument))
    |> Seq.filter (fun (udt, _) -> { Namespace = udt.Namespace; Name = udt.Name } = BuiltIn.Capability.FullName)
    |> Seq.map (fun (_, value) -> extractString value.Expression)

/// Asserts that the inferred capability of the callable with the given name matches the expected capability.
let private expect capability name =
    let actual = attributes callables.[CapabilityVerificationTests.testName name]
    Assert.Equal<string> ([ capability ], actual)

[<Fact>]
let ``Infers QPRGen0`` () =
    [ "NoOp"
      "ResultTuple"
      "ResultArray" ]
    |> List.iter (expect "QPRGen0")

[<Fact>]
let ``Infers QPRGen1`` () =
    [ "SetLocal"
      "EmptyIfOp"
      "EmptyIfNeqOp"
      "Reset"
      "ResetNeq" ]
    |> List.iter (expect "QPRGen1")

[<Fact>]
let ``Infers Unknown`` () =
    [ "ResultAsBool"
      "ResultAsBoolNeq"
      "ResultAsBoolOp"
      "ResultAsBoolNeqOp"
      "ResultAsBoolOpReturnIf"
      "ResultAsBoolNeqOpReturnIf"
      "ResultAsBoolOpReturnIfNested"
      "ResultAsBoolOpSetIf"
      "ResultAsBoolNeqOpSetIf"
      "ResultAsBoolOpElseSet"
      "NestedResultIfReturn"
      "ElifSet"
      "ElifElifSet"
      "ElifElseSet"
      "SetReusedName"
      "SetTuple"
      "EmptyIf"
      "EmptyIfNeq" ]
    |> List.iter (expect "Unknown")
