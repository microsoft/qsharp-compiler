// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Serialization

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open System.Runtime.Serialization
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Serialization

type PositionConverter() =
    inherit JsonConverter<Position>()

    override this.ReadJson(reader, _, _, _, serializer) =
        serializer.Deserialize<int * int> reader ||> Position.Create

    override this.WriteJson(writer: JsonWriter, position: Position, serializer: JsonSerializer) =
        serializer.Serialize(writer, (position.Line, position.Column))


[<CLIMutable>]
[<DataContract>]
type private RangePosition =
    {
        [<DataMember>]
        Line: int
        [<DataMember>]
        Column: int
    }

type RangeConverter() =
    inherit JsonConverter<Range>()

    override this.ReadJson(reader, _, _, _, serializer) =
        let start, end' = serializer.Deserialize<RangePosition * RangePosition> reader
        // For backwards compatibility, convert the serialized one-based positions to zero-based positions.
        Range.Create
            (Position.Create (start.Line - 1) (start.Column - 1))
            (Position.Create (end'.Line - 1) (end'.Column - 1))

    override this.WriteJson(writer: JsonWriter, range: Range, serializer: JsonSerializer) =
        // For backwards compatibility, convert the zero-based positions to one-based serialized positions.
        let start = { Line = range.Start.Line + 1; Column = range.Start.Column + 1 }
        let end' = { Line = range.End.Line + 1; Column = range.End.Column + 1 }
        serializer.Serialize(writer, (start, end'))


type QsNullableLocationConverter(?ignoreSerializationException) =
    inherit JsonConverter<QsNullable<QsLocation>>()
    let ignoreSerializationException = defaultArg ignoreSerializationException false

    override this.ReadJson(reader: JsonReader,
                           objectType: Type,
                           existingValue: QsNullable<QsLocation>,
                           hasExistingValue: bool,
                           serializer: JsonSerializer) =
        try
            if reader.ValueType <> typeof<String> || (string) reader.Value <> "Null" then
                let token = JObject.Load(reader)
                let loc = serializer.Deserialize<QsLocation>(token.CreateReader())

                if Object.ReferenceEquals(loc.Offset, null) || Object.ReferenceEquals(loc.Range, null) then
                    match serializer.Deserialize<QsNullable<JToken>>(token.CreateReader()) with
                    | Value loc -> loc.ToObject<QsLocation>() |> Value
                    | Null -> Null
                else
                    loc |> Value
            else
                Null
        with :? JsonSerializationException as ex -> if ignoreSerializationException then Null else raise ex

    override this.WriteJson(writer: JsonWriter, value: QsNullable<QsLocation>, serializer: JsonSerializer) =
        match value with
        | Value loc -> serializer.Serialize(writer, loc)
        | Null -> serializer.Serialize(writer, "Null")


type ResolvedTypeConverter(?ignoreSerializationException) =
    inherit JsonConverter<ResolvedType>()
    let ignoreSerializationException = defaultArg ignoreSerializationException false

    /// Returns an invalid type if the deserialization fails and ignoreSerializationException was set to true upon initialization
    override this.ReadJson(reader: JsonReader,
                           objectType: Type,
                           existingValue: ResolvedType,
                           hasExistingValue: bool,
                           serializer: JsonSerializer) =
        try
            let resolvedType =
                (true,
                 serializer.Deserialize<QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>>
                     (reader))
                |> ResolvedType.New

            match resolvedType.Resolution with
            | Operation ((i, o), c) when Object.ReferenceEquals(c, null)
                                         || Object.ReferenceEquals(c.Characteristics, null) ->
                new JsonSerializationException("failed to deserialize operation characteristics") |> raise
            | _ -> resolvedType
        with :? JsonSerializationException as ex ->
            if ignoreSerializationException then ResolvedType.New(true, InvalidType) else raise ex

    override this.WriteJson(writer: JsonWriter, value: ResolvedType, serializer: JsonSerializer) =
        serializer.Serialize(writer, value.Resolution)


type ResolvedCharacteristicsConverter(?ignoreSerializationException) =
    inherit JsonConverter<ResolvedCharacteristics>()
    let ignoreSerializationException = defaultArg ignoreSerializationException false

    /// Returns an invalid expression if the deserialization fails and ignoreSerializationException was set to true upon initialization
    override this.ReadJson(reader: JsonReader,
                           objectType: Type,
                           existingValue: ResolvedCharacteristics,
                           hasExistingValue: bool,
                           serializer: JsonSerializer) =
        try
            serializer.Deserialize<CharacteristicsKind<ResolvedCharacteristics>>(reader)
            |> ResolvedCharacteristics.New
        with :? JsonSerializationException as ex ->
            if ignoreSerializationException
            then ResolvedCharacteristics.New InvalidSetExpr
            else raise ex

    override this.WriteJson(writer: JsonWriter, value: ResolvedCharacteristics, serializer: JsonSerializer) =
        serializer.Serialize(writer, value.Expression)


type ResolvedInitializerConverter() =
    inherit JsonConverter<ResolvedInitializer>()

    override this.ReadJson(reader: JsonReader,
                           objectType: Type,
                           existingValue: ResolvedInitializer,
                           hasExistingValue: bool,
                           serializer: JsonSerializer) =
        serializer.Deserialize<QsInitializerKind<ResolvedInitializer, TypedExpression>>(reader)
        |> ResolvedInitializer.New

    override this.WriteJson(writer: JsonWriter, value: ResolvedInitializer, serializer: JsonSerializer) =
        serializer.Serialize(writer, (value.Resolution))


type TypedExpressionConverter() =
    inherit JsonConverter<TypedExpression>()

    override this.ReadJson(reader: JsonReader,
                           objectType: Type,
                           existingValue: TypedExpression,
                           hasExistingValue: bool,
                           serializer: JsonSerializer) =
        let (ex, paramRes, t, info, range) =
            serializer.Deserialize<QsExpressionKind<TypedExpression, Identifier, ResolvedType> * IEnumerable<QsQualifiedName * string * ResolvedType> * ResolvedType * InferredExpressionInformation * QsNullable<Range>>
                reader

        {
            Expression = ex
            TypeArguments = paramRes.ToImmutableArray()
            ResolvedType = t
            InferredInformation = info
            Range = range
        }

    override this.WriteJson(writer: JsonWriter, value: TypedExpression, serializer: JsonSerializer) =
        serializer.Serialize
            (writer, (value.Expression, value.TypeArguments, value.ResolvedType, value.InferredInformation, value.Range))


type private QsSpecializationConverter() =
    inherit JsonConverter<QsSpecialization>()

    override _.ReadJson(reader, _, _, _, serializer) =
        let schema = serializer.Deserialize<QsSpecializationSchema> reader

        {
            Kind = schema.Kind
            Parent = schema.Parent
            Attributes = schema.Attributes
            Source = { CodePath = schema.SourceFile; AssemblyPath = Null }
            Location = schema.Location
            TypeArguments = schema.TypeArguments
            Signature = schema.Signature
            Implementation = schema.Implementation
            Documentation = schema.Documentation
            Comments = schema.Comments
        }

    override _.WriteJson(writer: JsonWriter, value: QsSpecialization, serializer: JsonSerializer) =
        let schema =
            {
                Kind = value.Kind
                Parent = value.Parent
                Attributes = value.Attributes
                SourceFile = value.Source.CodePath
                Location = value.Location
                TypeArguments = value.TypeArguments
                Signature = value.Signature
                Implementation = value.Implementation
                Documentation = value.Documentation
                Comments = value.Comments
            }

        serializer.Serialize(writer, schema)


type private QsCallableConverter() =
    inherit JsonConverter<QsCallable>()

    override _.ReadJson(reader, _, _, _, serializer) =
        let schema = serializer.Deserialize<QsCallableSchema> reader

        {
            Kind = schema.Kind
            FullName = schema.FullName
            Attributes = schema.Attributes
            Modifiers = schema.Modifiers
            Source = { CodePath = schema.SourceFile; AssemblyPath = Null }
            Location = schema.Location
            Signature = schema.Signature
            ArgumentTuple = schema.ArgumentTuple
            Specializations = schema.Specializations
            Documentation = schema.Documentation
            Comments = schema.Comments
        }

    override _.WriteJson(writer: JsonWriter, value: QsCallable, serializer: JsonSerializer) =
        let schema =
            {
                Kind = value.Kind
                FullName = value.FullName
                Attributes = value.Attributes
                Modifiers = value.Modifiers
                SourceFile = value.Source.CodePath
                Location = value.Location
                Signature = value.Signature
                ArgumentTuple = value.ArgumentTuple
                Specializations = value.Specializations
                Documentation = value.Documentation
                Comments = value.Comments
            }

        serializer.Serialize(writer, schema)


type private QsCustomTypeConverter() =
    inherit JsonConverter<QsCustomType>()

    override _.ReadJson(reader, _, _, _, serializer) =
        let schema = serializer.Deserialize<QsCustomTypeSchema> reader

        {
            FullName = schema.FullName
            Attributes = schema.Attributes
            Modifiers = schema.Modifiers
            Source = { CodePath = schema.SourceFile; AssemblyPath = Null }
            Location = schema.Location
            Type = schema.Type
            TypeItems = schema.TypeItems
            Documentation = schema.Documentation
            Comments = schema.Comments
        }

    override _.WriteJson(writer: JsonWriter, value: QsCustomType, serializer: JsonSerializer) =
        let schema =
            {
                FullName = value.FullName
                Attributes = value.Attributes
                Modifiers = value.Modifiers
                SourceFile = value.Source.CodePath
                Location = value.Location
                Type = value.Type
                TypeItems = value.TypeItems
                Documentation = value.Documentation
                Comments = value.Comments
            }

        serializer.Serialize(writer, schema)


type QsNamespaceConverter() =
    inherit JsonConverter<QsNamespace>()

    override this.ReadJson(reader: JsonReader,
                           objectType: Type,
                           existingValue: QsNamespace,
                           hasExistingValue: bool,
                           serializer: JsonSerializer) =
        let (nsName, elements) = serializer.Deserialize<string * IEnumerable<QsNamespaceElement>>(reader)

        {
            Name = nsName
            Elements = elements.ToImmutableArray()
            Documentation = [].ToLookup(fst, snd)
        }

    override this.WriteJson(writer: JsonWriter, value: QsNamespace, serializer: JsonSerializer) =
        serializer.Serialize(writer, (value.Name, value.Elements))


type DictionaryAsArrayResolver() =
    inherit DefaultContractResolver()

    override this.CreateContract(objectType: Type) =
        let isDictionary (t: Type) =
            t = typedefof<IDictionary<_, _>>
            || (t.IsGenericType
                && t.GetGenericTypeDefinition() = typeof<IDictionary<_, _>>.GetGenericTypeDefinition())

        if objectType.GetInterfaces().Any(new Func<_, _>(isDictionary))
        then base.CreateArrayContract(objectType) :> JsonContract
        else base.CreateContract(objectType)


module Json =

    let Converters ignoreSerializationException =
        [|
            new PositionConverter() :> JsonConverter
            new RangeConverter() :> JsonConverter
            new QsNullableLocationConverter(ignoreSerializationException) :> JsonConverter
            new ResolvedTypeConverter(ignoreSerializationException) :> JsonConverter
            new ResolvedCharacteristicsConverter(ignoreSerializationException) :> JsonConverter
            new TypedExpressionConverter() :> JsonConverter
            new ResolvedInitializerConverter() :> JsonConverter
            new QsSpecializationConverter() :> JsonConverter
            new QsCallableConverter() :> JsonConverter
            new QsCustomTypeConverter() :> JsonConverter
            new QsNamespaceConverter() :> JsonConverter
        |]

    /// Creates a serializer using the given converters.
    /// Be aware that this is expensive and repeated creation of a serializer should be avoided.
    let CreateSerializer converters =
        let settings = new JsonSerializerSettings()
        settings.Converters <- converters
        settings.ContractResolver <- new DictionaryAsArrayResolver()
        settings.NullValueHandling <- NullValueHandling.Include
        settings.MissingMemberHandling <- MissingMemberHandling.Ignore
        settings.CheckAdditionalContent <- false
        JsonSerializer.CreateDefault settings

    let Serializer = Converters false |> CreateSerializer
    let PermissiveSerializer = Converters true |> CreateSerializer
