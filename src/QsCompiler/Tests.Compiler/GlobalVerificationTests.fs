// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit


type GlobalVerificationTests () =
    inherit CompilerTests(CompilerTests.Compile ("TestCases", ["General.qs"; "GlobalVerification.qs"; "Types.qs"; System.IO.Path.Join("LinkingTests", "Core.qs")]))

    member private this.Expect name (diag : IEnumerable<DiagnosticItem>) = 
        let ns = "Microsoft.Quantum.Testing.GlobalVerification"
        this.VerifyDiagnostics (QsQualifiedName.New (ns, name), diag)


    [<Fact>]
    member this.``Local namespace short names`` () = 

        this.Expect "LocalNamespaceShortNames1"  []
        this.Expect "LocalNamespaceShortNames2"  []
        this.Expect "LocalNamespaceShortNames3"  [Error ErrorCode.UnknownType]
        this.Expect "LocalNamespaceShortNames4"  [Error ErrorCode.UnknownType]
        this.Expect "LocalNamespaceShortNames5"  [Error ErrorCode.UnknownIdentifier]
        this.Expect "LocalNamespaceShortNames6"  [Error ErrorCode.ArgumentTypeMismatch; Error ErrorCode.ArgumentTypeMismatch]
        this.Expect "LocalNamespaceShortNames7"  [Error ErrorCode.UnknownIdentifier]
        this.Expect "LocalNamespaceShortNames8"  []
        this.Expect "LocalNamespaceShortNames9"  []
        this.Expect "LocalNamespaceShortNames10" []

        this.Expect "LocalNamespaceShortNames11" [Error ErrorCode.UnknownType; Error ErrorCode.UnknownIdentifier]
        this.Expect "LocalNamespaceShortNames12" [Error ErrorCode.UnknownIdentifier]
        this.Expect "LocalNamespaceShortNames13" [Error ErrorCode.UnknownType]
        this.Expect "LocalNamespaceShortNames14" [Error ErrorCode.TypeMismatchInReturn] // todo: could be more descriptive...
        this.Expect "LocalNamespaceShortNames15" []
        this.Expect "LocalNamespaceShortNames16" [Error ErrorCode.UnknownType; Error ErrorCode.UnknownType]
        this.Expect "LocalNamespaceShortNames17" [Error ErrorCode.UnknownType]
        this.Expect "LocalNamespaceShortNames18" [Error ErrorCode.UnknownType]
        this.Expect "LocalNamespaceShortNames19" []
        this.Expect "LocalNamespaceShortNames20" []
        this.Expect "LocalNamespaceShortNames21" []
        this.Expect "LocalNamespaceShortNames22" []
        this.Expect "LocalNamespaceShortNames23" []
        this.Expect "LocalNamespaceShortNames24" []


    [<Fact>]
    member this.``Naming conflicts`` () =

        this.Expect "NamingConflict1" [Error ErrorCode.FullNameConflictsWithNamespace]
        this.Expect "NamingConflict2" [Error ErrorCode.FullNameConflictsWithNamespace]
        this.Expect "NamingConflict3" [Error ErrorCode.FullNameConflictsWithNamespace]
        this.Expect "NamingConflict4" [Error ErrorCode.FullNameConflictsWithNamespace]
        this.Expect "NamingConflict5" [Error ErrorCode.FullNameConflictsWithNamespace]
        this.Expect "NamingConflict6" [Error ErrorCode.FullNameConflictsWithNamespace]


    [<Fact>]
    member this.``Uniqueness of specializations`` () = 

        this.Expect "ValidSetOfSpecializations1"  []
        this.Expect "ValidSetOfSpecializations2"  []
        this.Expect "ValidSetOfSpecializations3"  []
        this.Expect "ValidSetOfSpecializations4"  []
        this.Expect "ValidSetOfSpecializations5"  []
        this.Expect "ValidSetOfSpecializations6"  []
        this.Expect "ValidSetOfSpecializations7"  []
        this.Expect "ValidSetOfSpecializations8"  []
        this.Expect "ValidSetOfSpecializations9"  []
        this.Expect "ValidSetOfSpecializations10" []
        this.Expect "ValidSetOfSpecializations11" []
        this.Expect "ValidSetOfSpecializations12" []
        this.Expect "ValidSetOfSpecializations13" []

        this.Expect "InvalidSetOfSpecializations1"  [Error ErrorCode.RedefinitionOfControlledAdjoint]
        this.Expect "InvalidSetOfSpecializations2"  [Error ErrorCode.RedefinitionOfControlledAdjoint]
        this.Expect "InvalidSetOfSpecializations3"  [Error ErrorCode.RedefinitionOfControlledAdjoint]
        this.Expect "InvalidSetOfSpecializations4"  [Error ErrorCode.RedefinitionOfControlledAdjoint]
        this.Expect "InvalidSetOfSpecializations5"  [Error ErrorCode.RedefinitionOfControlledAdjoint]
        this.Expect "InvalidSetOfSpecializations6"  [Error ErrorCode.RedefinitionOfControlledAdjoint]
        this.Expect "InvalidSetOfSpecializations7"  [Error ErrorCode.RedefinitionOfAdjoint]
        this.Expect "InvalidSetOfSpecializations8"  [Error ErrorCode.RedefinitionOfAdjoint]
        this.Expect "InvalidSetOfSpecializations9"  [Error ErrorCode.RedefinitionOfAdjoint]
        this.Expect "InvalidSetOfSpecializations10" [Error ErrorCode.RedefinitionOfControlled]
        this.Expect "InvalidSetOfSpecializations11" [Error ErrorCode.RedefinitionOfControlled]
        this.Expect "InvalidSetOfSpecializations12" [Error ErrorCode.RedefinitionOfControlled]


    [<Fact>]
    member this.``Circular dependencies in user defined types`` () = 

        this.Expect "TypeA1"       []
        this.Expect "TypeA2"       []
        this.Expect "TypeA3"       []
                                   
        this.Expect "TypeB1"       [Error ErrorCode.TypeIsPartOfCyclicDeclaration]
        this.Expect "TypeB2"       [Error ErrorCode.TypeIsPartOfCyclicDeclaration]
        this.Expect "TypeB3"       [Error ErrorCode.TypeIsPartOfCyclicDeclaration]
                                   
        this.Expect "TypeC1"       [Error ErrorCode.TypeIsPartOfCyclicDeclaration]
        this.Expect "TypeC2"       [Error ErrorCode.TypeIsPartOfCyclicDeclaration]
        this.Expect "TypeC3"       [Error ErrorCode.TypeIsPartOfCyclicDeclaration]
                                   
        this.Expect "TypeD1"       []
        this.Expect "TypeD2"       []
        this.Expect "TypeD3"       []
                                   
        this.Expect "TypeE1"       [Error ErrorCode.TypeCannotContainItself]
        this.Expect "TypeE2"       [Error ErrorCode.TypeCannotContainItself]
        this.Expect "TypeE3"       [Error ErrorCode.TypeCannotContainItself]
                                  
        this.Expect "ValidType1"   []
        this.Expect "ValidType2"   []
        this.Expect "InvalidType1" [Error ErrorCode.AmbiguousType]
        this.Expect "InvalidType2" [Error ErrorCode.TypeIsPartOfCyclicDeclaration]
        this.Expect "InvalidType3" [Error ErrorCode.TypeIsPartOfCyclicDeclaration]

        this.Expect "IntType"      []
        this.Expect "UnitType"     [Error ErrorCode.TypeCannotContainItself]
        this.Expect "Qubits"       [Error ErrorCode.TypeIsPartOfCyclicDeclaration]


    [<Fact>]
    member this.``Verify that all paths return a value`` () = 

        this.Expect "AllPathsReturnValue1"     []
        this.Expect "AllPathsReturnValue2"     []
        this.Expect "AllPathsReturnValue3"     []
        this.Expect "AllPathsReturnValue4"     []
        this.Expect "AllPathsReturnValue5"     []
        this.Expect "AllPathsReturnValue6"     []
        this.Expect "AllPathsReturnValue7"     []
        this.Expect "AllPathsReturnValue8"     []
        this.Expect "AllPathsReturnValue9"     []
        this.Expect "AllPathsReturnValue10"    []
        this.Expect "AllPathsReturnValue11"    []
        this.Expect "AllPathsReturnValue12"    []
        this.Expect "AllPathsReturnValue13"    [Error ErrorCode.ReturnStatementWithinAutoInversion]
        this.Expect "AllPathsReturnValue14"    [Error ErrorCode.ReturnFromWithinApplyBlock; Error ErrorCode.MissingReturnOrFailStatement] // these are both due to temporary restrictions

        this.Expect "AllPathsFail1"     []
        this.Expect "AllPathsFail2"     []
        this.Expect "AllPathsFail3"     []
        this.Expect "AllPathsFail4"     []
        this.Expect "AllPathsFail5"     []
        this.Expect "AllPathsFail6"     []
        this.Expect "AllPathsFail7"     []
        this.Expect "AllPathsFail8"     []
        this.Expect "AllPathsFail9"     []
        this.Expect "AllPathsFail10"    []
        this.Expect "AllPathsFail11"    []
        this.Expect "AllPathsFail12"    []
        this.Expect "AllPathsFail13"    []
        this.Expect "AllPathsFail14"    []

        this.Expect "NotAllPathsReturnValue1"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue2"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue3"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue4"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue5"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue6"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue7"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue8"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue9"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue10" [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue11" [Error ErrorCode.MissingReturnOrFailStatement; Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "NotAllPathsReturnValue12" [Error ErrorCode.MissingReturnOrFailStatement; Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "NotAllPathsReturnValue13" [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnValue14" [Error ErrorCode.MissingReturnOrFailStatement]

        this.Expect "NotAllPathsReturnOrFail1"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail2"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail3"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail4"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail5"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail6"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail7"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail8"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail9"  [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail10" [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail11" [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail12" [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail13" [Error ErrorCode.MissingReturnOrFailStatement]
        this.Expect "NotAllPathsReturnOrFail14" [Error ErrorCode.MissingReturnOrFailStatement]


    [<Fact>]
    member this.``Return from within qubit allocation scopes`` () = 

        this.Expect "ReturnFromWithinUsing1"             []
        this.Expect "ReturnFromWithinUsing2"             []
        this.Expect "ReturnFromWithinUsing3"             []
        this.Expect "ReturnFromWithinUsing4"             []
        this.Expect "ReturnFromWithinUsing5"             []
        this.Expect "ReturnFromWithinUsing7"             [Warning WarningCode.UnreachableCode]
        this.Expect "ReturnFromWithinUsing8"             []
        this.Expect "InvalidReturnFromWithinUsing1"      [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinUsing2"      [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinUsing3"      [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinUsing4"      [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinUsing5"      [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinUsing6"      [Error ErrorCode.ReturnStatementWithinAutoInversion; Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinUsing7"      [Error ErrorCode.ReturnFromWithinApplyBlock]
        this.Expect "InvalidReturnFromWithinUsing8"      [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinUsing9"      [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinUsing10"     [Error ErrorCode.InvalidReturnWithinAllocationScope; Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinUsing11"     [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinUsing12"     [Error ErrorCode.InvalidReturnWithinAllocationScope; Error ErrorCode.InvalidReturnWithinAllocationScope; Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinUsing13"     [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinUsing14"     [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinUsing15"     [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
                                                         
        this.Expect "ReturnFromWithinBorrowing1"         []
        this.Expect "ReturnFromWithinBorrowing2"         []
        this.Expect "ReturnFromWithinBorrowing3"         []
        this.Expect "ReturnFromWithinBorrowing4"         []
        this.Expect "ReturnFromWithinBorrowing5"         []
        this.Expect "ReturnFromWithinBorrowing6"         [Warning WarningCode.UnreachableCode]
        this.Expect "ReturnFromWithinBorrowing7"         [Warning WarningCode.UnreachableCode]
        this.Expect "ReturnFromWithinBorrowing8"         []
        this.Expect "InvalidReturnFromWithinBorrowing1"  [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing2"  [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing3"  [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing4"  [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing5"  [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinBorrowing6"  [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing7"  [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing8"  [Error ErrorCode.ReturnStatementWithinAutoInversion; Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinBorrowing9"  [Error ErrorCode.ReturnFromWithinApplyBlock]
        this.Expect "InvalidReturnFromWithinBorrowing10" [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing11" [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinBorrowing12" [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinBorrowing13" [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnFromWithinBorrowing14" [Error ErrorCode.InvalidReturnWithinAllocationScope; Error ErrorCode.InvalidReturnWithinAllocationScope; Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing15" [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing16" [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnFromWithinBorrowing17" [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]

        this.Expect "ValidReturnPlacement1"              [Warning WarningCode.UnreachableCode]
        this.Expect "ValidReturnPlacement2"              [Warning WarningCode.UnreachableCode]
        this.Expect "ValidReturnPlacement3"              []
        this.Expect "ValidReturnPlacement4"              []
        this.Expect "ValidReturnPlacement5"              []
        this.Expect "ValidReturnPlacement6"              []
        this.Expect "ValidReturnPlacement7"              []
        this.Expect "ValidReturnPlacement8"              []
        this.Expect "ValidReturnPlacement9"              []
        this.Expect "ValidReturnPlacement10"             []
        this.Expect "ValidReturnPlacement11"             []
        this.Expect "ValidReturnPlacement12"             []
        this.Expect "ValidReturnPlacement13"             [Warning WarningCode.UnreachableCode]
        this.Expect "ValidReturnPlacement14"             []
        this.Expect "ValidReturnPlacement15"             []
        this.Expect "InvalidReturnPlacement1"            [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnPlacement2"            [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnPlacement3"            [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnPlacement4"            [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnPlacement5"            [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnPlacement6"            [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnPlacement7"            [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnPlacement8"            [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnPlacement9"            [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnPlacement10"           [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnPlacement11"           [Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]
        this.Expect "InvalidReturnPlacement12"           [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnPlacement13"           [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnPlacement14"           [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnPlacement15"           [Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnPlacement16"           [Error ErrorCode.ReturnFromWithinApplyBlock]
        this.Expect "InvalidReturnPlacement17"           [Error ErrorCode.ReturnStatementWithinAutoInversion; Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnPlacement18"           [Error ErrorCode.ReturnFromWithinApplyBlock]
        this.Expect "InvalidReturnPlacement19"           [Error ErrorCode.ReturnStatementWithinAutoInversion; Error ErrorCode.InvalidReturnWithinAllocationScope]
        this.Expect "InvalidReturnPlacement20"           [Error ErrorCode.ReturnStatementWithinAutoInversion; Error ErrorCode.InvalidReturnWithinAllocationScope; Warning WarningCode.UnreachableCode]


    [<Fact>]
    member this.``Attribute annotations on declarations`` () = 
    
        this.Expect "ValidAttributes1"  []
        this.Expect "ValidAttributes2"  []
        this.Expect "ValidAttributes3"  []
        this.Expect "ValidAttributes4"  []
        this.Expect "ValidAttributes5"  []
        this.Expect "ValidAttributes6"  []
        this.Expect "ValidAttributes7"  []
        this.Expect "ValidAttributes8"  []
        this.Expect "ValidAttributes9"  []
        this.Expect "ValidAttributes10" []

        this.Expect "AttributeDuplication1" [Warning WarningCode.DuplicateAttribute]
        this.Expect "AttributeDuplication2" [Warning WarningCode.DuplicateAttribute]
        this.Expect "AttributeDuplication3" [Warning WarningCode.DuplicateAttribute]
        this.Expect "InvalidAttributes1"    [Error ErrorCode.InterpolatedStringInAttribute]
        this.Expect "InvalidAttributes2"    [Error ErrorCode.AmbiguousType]
        this.Expect "InvalidAttributes3"    [Error ErrorCode.UnknownType]
        this.Expect "InvalidAttributes4"    [Error ErrorCode.UnknownNamespace]
        this.Expect "InvalidAttributes5"    [Error ErrorCode.UnknownTypeInNamespace]
        this.Expect "InvalidAttributes6"    [Error ErrorCode.AttributeArgumentTypeMismatch]
        this.Expect "InvalidAttributes7"    [Error ErrorCode.InvalidAttributeArgument]
        this.Expect "InvalidAttributes8"    [Error ErrorCode.ArgumentOfUserDefinedTypeInAttribute]
        this.Expect "InvalidAttributes9"    [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidAttributes10"   [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidAttributes11"   [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidAttributes12"   [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidAttributes13"   [Error ErrorCode.AttributeInvalidOnCallable]
        this.Expect "InvalidAttributes14"   [Error ErrorCode.AttributeInvalidOnCallable]
        

