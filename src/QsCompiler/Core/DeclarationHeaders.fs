// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System
open System.Collections.Immutable
open System.IO
open System.Text
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Serialization
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Newtonsoft.Json


/// to be removed in future releases
type private DeclarationHeader = 

    static member internal FromJson<'T> json = 
        let deserialize converters =             
            let reader = new JsonTextReader(new StringReader(json));
            (converters |> Json.Serializer).Deserialize<'T>(reader)
        try true, Json.Converters false |> deserialize
        with _ -> false, Json.Converters true |> deserialize

    static member internal ToJson obj = 
        let builder = new StringBuilder()
        (Json.Converters false |> Json.Serializer).Serialize(new StringWriter(builder), obj)
        builder.ToString()


/// to be removed in future releases
type TypeDeclarationHeader = {
    QualifiedName   : QsQualifiedName
    Attributes      : ImmutableArray<QsDeclarationAttribute>
    SourceFile      : NonNullable<string>
    Position        : int * int
    SymbolRange     : QsPositionInfo * QsPositionInfo
    Type            : ResolvedType
    TypeItems       : QsTuple<QsTypeItem>
    Documentation   : ImmutableArray<string>
}
    with 
    member this.FromSource source = {this with SourceFile = source}

    static member New (customType : QsCustomType) = {
        QualifiedName   = customType.FullName
        Attributes      = customType.Attributes
        SourceFile      = customType.SourceFile
        Position        = customType.Location.Offset
        SymbolRange     = customType.Location.Range
        Type            = customType.Type
        TypeItems       = customType.TypeItems
        Documentation   = customType.Documentation
    }

    static member FromJson json =
        let (success, header) = DeclarationHeader.FromJson<TypeDeclarationHeader> json
        let attributesAreNullOrDefault = Object.ReferenceEquals(header.Attributes, null) || header.Attributes.IsDefault
        let header = if attributesAreNullOrDefault then {header with Attributes = ImmutableArray.Empty} else header // no reason to raise an error
        if not (Object.ReferenceEquals(header.TypeItems, null)) then success, header
        else false, {header with TypeItems = ImmutableArray.Create (header.Type |> Anonymous |> QsTupleItem) |> QsTuple}

    member this.ToJson() : string  =
        DeclarationHeader.ToJson this


/// to be removed in future releases
type CallableDeclarationHeader = { 
    Kind            : QsCallableKind
    QualifiedName   : QsQualifiedName
    Attributes      : ImmutableArray<QsDeclarationAttribute>
    SourceFile      : NonNullable<string>
    Position        : int * int
    SymbolRange     : QsPositionInfo * QsPositionInfo
    ArgumentTuple   : QsTuple<LocalVariableDeclaration<QsLocalSymbol>>
    Signature       : ResolvedSignature
    Documentation   : ImmutableArray<string>
}
    with 
    member this.FromSource source = {this with SourceFile = source}

    static member New (callable : QsCallable) = {
        Kind            = callable.Kind
        QualifiedName   = callable.FullName
        Attributes      = callable.Attributes
        SourceFile      = callable.SourceFile
        Position        = callable.Location.Offset
        SymbolRange     = callable.Location.Range
        ArgumentTuple   = callable.ArgumentTuple
        Signature       = callable.Signature
        Documentation   = callable.Documentation
    }

    static member FromJson json =
        let info = {IsMutable = false; HasLocalQuantumDependency = false} 
        let rec setInferredInfo = function // no need to raise an error if anything needs to be set - the info above is always correct
            | QsTuple ts -> (ts |> Seq.map setInferredInfo).ToImmutableArray() |> QsTuple
            | QsTupleItem (decl : LocalVariableDeclaration<_>) -> QsTupleItem {decl with InferredInformation = info}
        // we need to make sure that all fields that could possibly be null after deserializing 
        // due to changes of fields over time are initialized to a proper value
        let (success, header) = DeclarationHeader.FromJson<CallableDeclarationHeader> json
        let attributesAreNullOrDefault = Object.ReferenceEquals(header.Attributes, null) || header.Attributes.IsDefault
        let header = if attributesAreNullOrDefault then {header with Attributes = ImmutableArray.Empty} else header // no reason to raise an error
        let header = {header with ArgumentTuple = header.ArgumentTuple |> setInferredInfo}
        if Object.ReferenceEquals(header.Signature.Information, null) || Object.ReferenceEquals(header.Signature.Information.Characteristics, null) then 
            false, {header with Signature = {header.Signature with Information = CallableInformation.Invalid}}
        else success, header

    member this.ToJson() : string  =
        DeclarationHeader.ToJson this


/// to be removed in future releases
type SpecializationDeclarationHeader = {
    Kind            : QsSpecializationKind
    TypeArguments   : QsNullable<ImmutableArray<ResolvedType>>
    Information     : CallableInformation
    Parent          : QsQualifiedName    
    Attributes      : ImmutableArray<QsDeclarationAttribute>
    SourceFile      : NonNullable<string>
    Position        : int * int
    HeaderRange     : QsPositionInfo*QsPositionInfo
    Documentation   : ImmutableArray<string>
}
    with 
    member this.FromSource source = {this with SourceFile = source}

    static member New (specialization : QsSpecialization) = {
        Kind            = specialization.Kind
        TypeArguments   = specialization.TypeArguments
        Information     = specialization.Signature.Information
        Parent          = specialization.Parent 
        Attributes      = specialization.Attributes
        SourceFile      = specialization.SourceFile
        Position        = specialization.Location.Offset
        HeaderRange     = specialization.Location.Range
        Documentation   = specialization.Documentation
    }

    static member FromJson json =
        // we need to make sure that all fields that could possibly be null after deserializing 
        // due to changes of fields over time are initialized to a proper value
        let (success, header) = DeclarationHeader.FromJson<SpecializationDeclarationHeader> json
        let infoIsNull = Object.ReferenceEquals(header.Information, null)
        let typeArgsAreNull = Object.ReferenceEquals(header.TypeArguments, null)
        let attributesAreNullOrDefault = Object.ReferenceEquals(header.Attributes, null) || header.Attributes.IsDefault
        let header = if attributesAreNullOrDefault then {header with Attributes = ImmutableArray.Empty} else header // no reason to raise an error
        if not (infoIsNull || typeArgsAreNull) then success, header
        else
            let information = if not infoIsNull then header.Information else CallableInformation.Invalid
            let typeArguments = if typeArgsAreNull then Null else header.TypeArguments
            false, {header with Information = information; TypeArguments = typeArguments }

    member this.ToJson() : string  =
        DeclarationHeader.ToJson this
        


