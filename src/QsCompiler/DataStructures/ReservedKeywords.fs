// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.ReservedKeywords

open System.Collections.Immutable


/// contains keywords for Q# types
module Types = 

    // keyword for a Q# transformation characteristics annotation
    let Characteristics = "is"
    // keyword for a predefined set of Q# transformation characteristics 
    let AdjSet          = "Adj"
    // keyword for a predefined set of Q# transformation characteristics 
    let CtlSet          = "Ctl"

    /// keyword for a predefined Q# type
    let Unit   = "Unit"
    /// keyword for a predefined Q# type
    let Int    = "Int"
    /// keyword for a predefined Q# type
    let BigInt = "BigInt"
    /// keyword for a predefined Q# type
    let Double = "Double"
    /// keyword for a predefined Q# type
    let Bool   = "Bool"
    /// keyword for a predefined Q# type
    let Qubit  = "Qubit"     
    /// keyword for a predefined Q# type
    let Result = "Result" 
    /// keyword for a predefined Q# type
    let Pauli  = "Pauli"
    /// keyword for a predefined Q# type
    let Range  = "Range" 
    /// keyword for a predefined Q# type
    let String = "String" 


/// contains keywords for Q# literals
module Literals =

    /// keyword for a Q# literal
    let PauliX = "PauliX" 
    /// keyword for a Q# literal
    let PauliY = "PauliY" 
    /// keyword for a Q# literal
    let PauliZ = "PauliZ" 
    /// keyword for a Q# literal
    let PauliI = "PauliI" 

    /// keyword for a Q# literal
    let Zero   = "Zero"
    /// keyword for a Q# literal
    let One    = "One"

    /// keyword for a Q# literal
    let True   = "true"
    /// keyword for a Q# literal
    let False  = "false"


/// contains keywords for Q# functors
module Functors = 

    /// keyword for a Q# functor application
    let Adjoint    = "Adjoint"
    /// keyword for a Q# functor application
    let Controlled = "Controlled"


/// contains keywords for Q# statements
module Statements = 

    // simple statements 

    /// keyword for a Q# statement header
    let Return  = "return"
    /// keyword for a Q# statement header
    let Fail    = "fail"
    /// keyword for a Q# statement header
    let Let     = "let"
    /// keyword for a Q# statement header
    let Mutable = "mutable"
    /// keyword for a Q# statement header
    let Set     = "set"

    // control flow statements

    /// keyword for a Q# control flow statement
    let If         = "if"
    /// keyword for a Q# control flow statement
    let Elif       = "elif"
    /// keyword for a Q# control flow statement
    let Else       = "else"

    /// keyword for a Q# control flow statement
    let For        = "for"
    /// keyword for a Q# control flow statement
    let In         = "in"

    /// keyword for a Q# control flow statement
    let While      = "while"

    /// keyword for a Q# control flow statement
    let Repeat     = "repeat"
    /// keyword for a Q# control flow statement
    let Until      = "until"
    /// keyword for a Q# control flow statement
    let Fixup      = "fixup"

    // block statements

    /// keyword for a Q# transformation pattern
    let Within     = "within"
    /// keyword for a Q# transformation pattern
    let Apply      = "apply"

    /// keyword for a Q# allocation statement
    let Using      = "using"
    /// keyword for a Q# allocation statement
    let Borrowing  = "borrowing"


/// contains keywords for Q# expressions 
module Expressions = 

    /// keyword used within a Q# new-array-expression
    let New    = "new" 
    /// keyword used as operator for Q# expressions
    let Not    = "not"
    /// keyword used as operator for Q# expressions
    let And    = "and"
    /// keyword used as operator for Q# expressions
    let Or     = "or"


/// contains keywords for Q# declarations
module Declarations = 

    /// keyword for a Q# declaration
    let Body        = "body"
    /// keyword for a Q# declaration
    let Adjoint     = "adjoint"
    /// keyword for a Q# declaration
    let Controlled  = "controlled"

    /// keyword for a Q# declaration
    let Operation   = "operation"
    /// keyword for a Q# declaration
    let Function    = "function"
    /// keyword for a Q# declaration
    let Type        = "newtype"

    /// keyword for a Q# declaration
    let Namespace   = "namespace"
    
    /// keyword for a Q# declaration modifier
    let Internal    = "internal"


/// contains keywords for Q# directives
module Directives = 

    /// keyword for a Q# directive
    let Open        = "open"
    /// keyword for a Q# directive
    let OpenedAs    = "as"

    /// keyword for a Q# directive
    let Auto        = "auto"
    /// keyword for a Q# directive
    let Intrinsic   = "intrinsic"
    /// keyword for a Q# directive
    let Self        = "self"
    /// keyword for a Q# directive
    let Invert      = "invert"
    /// keyword for a Q# directive
    let Distribute  = "distribute"


/// contains keywords reserved for internal use
module InternalUse = 
    // IMPORTANT: with the exception of the CsKeyworks the keywords here are expected to follow the pattern __*__ 
    // otherwise they need to be manually added to the list of reserved keywords in the TextProcessor!

    /// to be used as name for all namespaces that do not have a valid name
    let UnknownNamespace = "__UnknownNamespaceName__"

    /// name for the control qubits used for compiler-generated controlled specializations (argument to the controlled functor)
    let ControlQubitsName = "__controlQubits__"

    /// auto-generated name for the unit argument upon constructing a controlled specialization for callables that take only a single unit argument
    let UnitArgument = "__unitArg__"

    /// contains all keywords reserved due to clashes with auto-generated Q# code, or generated C# code
    let CsKeywords = ImmutableHashSet.CreateRange [

            "Allocate"
            "Release" 
            "Borrow" 
            "Return"

            "Data"
            "Item"

            "QVoid"
            "Int64" 
            "BigInteger"
            "Boolean"
            "QArray" 
            "QTuple" 
            "UDTBase"

            "IUnitary" 
            "IAdjointable"
            "IControllable" 
            "ICallable"
            "IOperationFactory"
            "IApplyData"
        ]
    
    // TODO: ReservedForFutureUse = ...


/// contains the names used for compiler-generated Q# attributes 
/// that do not and should not have a definition in source code
module GeneratedAttributes =
    let Namespace = "Microsoft.Quantum.QsCompiler.Metadata.Attributes"
    let LoadedViaTestNameInsteadOf = "__LoadedViaTestNameInsteadOf__"

/// contains project specific settings specified during Q# compilation
module AssemblyConstants = 
    let OutputPath = "OutputPath"
    let AssemblyName = "AssemblyName"
    let QsharpOutputType = "ResolvedQsharpOutputType"
    let QsharpExe = "QsharpExe"
    let QsharpLibrary = "QsharpLibrary"
    let ExecutionTarget = "ResolvedExecutionTarget"
    let HoneywellProcessor = "HoneywellProcessor"
    let IonQProcessor = "IonQProcessor"
    let QCIProcessor = "QCIProcessor"
    let DefaultSimulator = "DefaultSimulator"
    let QuantumSimulator = "QuantumSimulator"
    let ToffoliSimulator = "ToffoliSimulator"
    let ResourcesEstimator = "ResourcesEstimator"

    // Note: The names of the capabilities here need to match the ones defined by the Sdk.
    type RuntimeCapabilities = 
    | Unknown = 0 
    | QPRGen0 = 1
    | QPRGen1 = 2


/// contains reserved names for command line arguments of Q# projects
module CommandLineArguments = 
    let SimulatorOption = ("simulator", "s")
    let ReservedArguments = ImmutableArray.Create (fst SimulatorOption)
    let ReservedArgumentAbbreviations = ImmutableArray.Create (snd SimulatorOption) 

    let BuiltInSimulators =
        [AssemblyConstants.QuantumSimulator; AssemblyConstants.ToffoliSimulator; AssemblyConstants.ResourcesEstimator]
        |> ImmutableHashSet.CreateRange


/// contains specific names used within Q# dlls
module DotnetCoreDll = 
    let ResourceName = "__qsharp_data__.bson"
    let MetadataNamespace = "__qsharp__"
    let ReferenceAlias = "__qsharp_reference__"
    let MetadataType = "Metadata"
    let Dependencies = "Dependencies"

