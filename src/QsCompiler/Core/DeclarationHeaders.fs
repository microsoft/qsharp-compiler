// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System
open System.Collections.Immutable
open System.IO
open System.Text
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Serialization
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Newtonsoft.Json


/// to be removed in future releases
module DeclarationHeader =

    /// used for serialization purposes; to be used internally only
    type Offset =
        | Defined of Position
        | Undefined

    /// used for serialization purposes; to be used internally only
    type Range =
        | Defined of DataTypes.Range
        | Undefined

    let CreateOffset (location: QsNullable<QsLocation>) =
        location
        |> function
        | Value loc -> Offset.Defined loc.Offset
        | Null -> Offset.Undefined

    let CreateRange (location: QsNullable<QsLocation>) =
        location
        |> function
        | Value loc -> Range.Defined loc.Range
        | Null -> Range.Undefined

    let internal CreateLocation =
        function
        | Offset.Defined offset, Range.Defined range -> QsLocation.New(offset, range) |> Value
        | _ -> Null


    type private NullableOffsetConverter() =
        inherit JsonConverter<Offset>()

        override this.ReadJson(reader: JsonReader,
                               objectType: Type,
                               existingValue: Offset,
                               hasExistingValue: bool,
                               serializer: JsonSerializer) =
            if reader.ValueType <> typeof<String> || (string) reader.Value <> "Undefined" then
                let offset = serializer.Deserialize<Position>(reader)
                if Object.ReferenceEquals(offset, null) then Offset.Undefined else Offset.Defined offset
            else
                Offset.Undefined

        override this.WriteJson(writer: JsonWriter, value: Offset, serializer: JsonSerializer) =
            match value with
            | Offset.Defined offset -> serializer.Serialize(writer, offset)
            | Offset.Undefined -> serializer.Serialize(writer, "Undefined")

    type private NullableRangeConverter() =
        inherit JsonConverter<Range>()

        override this.ReadJson(reader: JsonReader,
                               objectType: Type,
                               existingValue: Range,
                               hasExistingValue: bool,
                               serializer: JsonSerializer) =
            if reader.ValueType <> typeof<String> || (string) reader.Value <> "Undefined" then
                let range =
                    serializer.Deserialize<DataTypes.Range>(reader)

                if Object.ReferenceEquals(range, null) then Range.Undefined else Range.Defined range
            else
                Range.Undefined

        override this.WriteJson(writer: JsonWriter, value: Range, serializer: JsonSerializer) =
            match value with
            | Range.Defined range -> serializer.Serialize(writer, range)
            | Range.Undefined -> serializer.Serialize(writer, "Undefined")


    let private qsNullableConverters =
        [| new NullableOffsetConverter() :> JsonConverter
           new NullableRangeConverter() :> JsonConverter |]

    let private Serializer =
        [ qsNullableConverters
          Json.Converters false ]
        |> Array.concat
        |> Json.CreateSerializer

    let private PermissiveSerializer =
        [ qsNullableConverters
          Json.Converters true ]
        |> Array.concat
        |> Json.CreateSerializer

    let internal FromJson<'T> json =
        let deserialize (serializer: JsonSerializer) =
            let reader =
                new JsonTextReader(new StringReader(json))

            serializer.Deserialize<'T>(reader)

        try
            true, Serializer |> deserialize
        with _ -> false, PermissiveSerializer |> deserialize

    let internal ToJson obj =
        let builder = new StringBuilder()
        Serializer.Serialize(new StringWriter(builder), obj)
        builder.ToString()


/// to be removed in future releases
type TypeDeclarationHeader =
    { QualifiedName: QsQualifiedName
      Attributes: ImmutableArray<QsDeclarationAttribute>
      Modifiers: Modifiers
      SourceFile: string
      Position: DeclarationHeader.Offset
      SymbolRange: DeclarationHeader.Range
      Type: ResolvedType
      TypeItems: QsTuple<QsTypeItem>
      Documentation: ImmutableArray<string> }
    [<JsonIgnore>]
    member this.Location =
        DeclarationHeader.CreateLocation(this.Position, this.SymbolRange)

    member this.FromSource source = { this with SourceFile = source }

    member this.AddAttribute att =
        { this with
              Attributes = this.Attributes.Add att }

    static member New(customType: QsCustomType) =
        { QualifiedName = customType.FullName
          Attributes = customType.Attributes
          Modifiers = customType.Modifiers
          SourceFile = customType.SourceFile
          Position = customType.Location |> DeclarationHeader.CreateOffset
          SymbolRange = customType.Location |> DeclarationHeader.CreateRange
          Type = customType.Type
          TypeItems = customType.TypeItems
          Documentation = customType.Documentation }

    static member FromJson json =
        let (success, header) =
            DeclarationHeader.FromJson<TypeDeclarationHeader> json

        let attributesAreNullOrDefault =
            Object.ReferenceEquals(header.Attributes, null) || header.Attributes.IsDefault

        let header =
            if attributesAreNullOrDefault then
                { header with
                      Attributes = ImmutableArray.Empty }
            else
                header // no reason to raise an error

        if not (Object.ReferenceEquals(header.TypeItems, null)) then
            success, header
        else
            false,
            { header with
                  TypeItems = ImmutableArray.Create(header.Type |> Anonymous |> QsTupleItem) |> QsTuple }

    member this.ToJson(): string = DeclarationHeader.ToJson this


/// to be removed in future releases
type CallableDeclarationHeader =
    { Kind: QsCallableKind
      QualifiedName: QsQualifiedName
      Attributes: ImmutableArray<QsDeclarationAttribute>
      Modifiers: Modifiers
      SourceFile: string
      Position: DeclarationHeader.Offset
      SymbolRange: DeclarationHeader.Range
      ArgumentTuple: QsTuple<LocalVariableDeclaration<QsLocalSymbol>>
      Signature: ResolvedSignature
      Documentation: ImmutableArray<string> }
    [<JsonIgnore>]
    member this.Location =
        DeclarationHeader.CreateLocation(this.Position, this.SymbolRange)

    member this.FromSource source = { this with SourceFile = source }

    member this.AddAttribute att =
        { this with
              Attributes = this.Attributes.Add att }

    static member New(callable: QsCallable) =
        { Kind = callable.Kind
          QualifiedName = callable.FullName
          Attributes = callable.Attributes
          Modifiers = callable.Modifiers
          SourceFile = callable.SourceFile
          Position = callable.Location |> DeclarationHeader.CreateOffset
          SymbolRange = callable.Location |> DeclarationHeader.CreateRange
          ArgumentTuple = callable.ArgumentTuple
          Signature = callable.Signature
          Documentation = callable.Documentation }

    static member FromJson json =
        let info =
            { IsMutable = false
              HasLocalQuantumDependency = false }

        let rec setInferredInfo =
            function // no need to raise an error if anything needs to be set - the info above is always correct
            | QsTuple ts -> ts |> Seq.map setInferredInfo |> ImmutableArray.CreateRange |> QsTuple
            | QsTupleItem (decl: LocalVariableDeclaration<_>) -> QsTupleItem { decl with InferredInformation = info }
        // we need to make sure that all fields that could possibly be null after deserializing
        // due to changes of fields over time are initialized to a proper value
        let (success, header) =
            DeclarationHeader.FromJson<CallableDeclarationHeader> json

        let attributesAreNullOrDefault =
            Object.ReferenceEquals(header.Attributes, null) || header.Attributes.IsDefault

        let header =
            if attributesAreNullOrDefault then
                { header with
                      Attributes = ImmutableArray.Empty }
            else
                header // no reason to raise an error

        let header =
            { header with
                  ArgumentTuple = header.ArgumentTuple |> setInferredInfo }

        if Object.ReferenceEquals(header.Signature.Information, null)
           || Object.ReferenceEquals(header.Signature.Information.Characteristics, null) then
            false,
            { header with
                  Signature =
                      { header.Signature with
                            Information = CallableInformation.Invalid } }
        else
            success, header

    member this.ToJson(): string = DeclarationHeader.ToJson this


/// to be removed in future releases
type SpecializationDeclarationHeader =
    { Kind: QsSpecializationKind
      TypeArguments: QsNullable<ImmutableArray<ResolvedType>>
      Information: CallableInformation
      Parent: QsQualifiedName
      Attributes: ImmutableArray<QsDeclarationAttribute>
      SourceFile: string
      Position: DeclarationHeader.Offset
      HeaderRange: DeclarationHeader.Range
      Documentation: ImmutableArray<string> }
    [<JsonIgnore>]
    member this.Location =
        DeclarationHeader.CreateLocation(this.Position, this.HeaderRange)

    member this.FromSource source = { this with SourceFile = source }

    static member New(specialization: QsSpecialization) =
        { Kind = specialization.Kind
          TypeArguments = specialization.TypeArguments
          Information = specialization.Signature.Information
          Parent = specialization.Parent
          Attributes = specialization.Attributes
          SourceFile = specialization.SourceFile
          Position = specialization.Location |> DeclarationHeader.CreateOffset
          HeaderRange = specialization.Location |> DeclarationHeader.CreateRange
          Documentation = specialization.Documentation }

    static member FromJson json =
        // we need to make sure that all fields that could possibly be null after deserializing
        // due to changes of fields over time are initialized to a proper value
        let (success, header) =
            DeclarationHeader.FromJson<SpecializationDeclarationHeader> json

        let infoIsNull =
            Object.ReferenceEquals(header.Information, null)

        let typeArgsAreNull =
            Object.ReferenceEquals(header.TypeArguments, null)

        let attributesAreNullOrDefault =
            Object.ReferenceEquals(header.Attributes, null) || header.Attributes.IsDefault

        let header =
            if attributesAreNullOrDefault then
                { header with
                      Attributes = ImmutableArray.Empty }
            else
                header // no reason to raise an error

        if not (infoIsNull || typeArgsAreNull) then
            success, header
        else
            let information =
                if not infoIsNull then header.Information else CallableInformation.Invalid

            let typeArguments =
                if typeArgsAreNull then Null else header.TypeArguments

            false,
            { header with
                  Information = information
                  TypeArguments = typeArguments }

    member this.ToJson(): string = DeclarationHeader.ToJson this
