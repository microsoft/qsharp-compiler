// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Serialization

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.SyntaxTokens 
open Newtonsoft.Json
open Newtonsoft.Json.Serialization


type NonNullableConverter<'T when 'T: equality and 'T: null>()  =
    inherit JsonConverter<NonNullable<'T>>()
    
    override this.ReadJson(reader : JsonReader, objectType : Type, existingValue : NonNullable<'T>, hasExistingValue : bool, serializer : JsonSerializer) =
        let value = reader.Value :?> 'T
        NonNullable<'T>.New(value)
        
    override this.WriteJson(writer : JsonWriter, value : NonNullable<'T>, serializer : JsonSerializer) =
        serializer.Serialize(writer, value.Value)


type ResolvedTypeConverter(?ignoreSerializationException) =
    inherit JsonConverter<ResolvedType>()
    let ignoreSerializationException = defaultArg ignoreSerializationException false

    /// Returns an invalid type if the deserialization fails and ignoreSerializationException was set to true upon initialization
    override this.ReadJson(reader : JsonReader, objectType : Type, existingValue : ResolvedType, hasExistingValue : bool, serializer : JsonSerializer) = 
        try let resolvedType = (true, serializer.Deserialize<QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>>(reader)) |> ResolvedType.New
            match resolvedType.Resolution with 
            | Operation ((i, o), c) when Object.ReferenceEquals(c, null) || Object.ReferenceEquals(c.Characteristics, null) -> 
                new JsonSerializationException("failed to deserialize operation characteristics") |> raise
            | _ -> resolvedType
        with | :? JsonSerializationException as ex -> 
            if ignoreSerializationException then ResolvedType.New (true, InvalidType)
            else raise ex

    override this.WriteJson(writer : JsonWriter, value : ResolvedType, serializer : JsonSerializer) =
        serializer.Serialize(writer, value.Resolution)


type ResolvedCharacteristicsConverter(?ignoreSerializationException) =
    inherit JsonConverter<ResolvedCharacteristics>()
    let ignoreSerializationException = defaultArg ignoreSerializationException false

    /// Returns an invalid expression if the deserialization fails and ignoreSerializationException was set to true upon initialization
    override this.ReadJson(reader : JsonReader, objectType : Type, existingValue : ResolvedCharacteristics, hasExistingValue : bool, serializer : JsonSerializer) = 
        try serializer.Deserialize<CharacteristicsKind<ResolvedCharacteristics>>(reader) |> ResolvedCharacteristics.New
        with | :? JsonSerializationException as ex -> 
            if ignoreSerializationException then ResolvedCharacteristics.New InvalidSetExpr
            else raise ex

    override this.WriteJson(writer : JsonWriter, value : ResolvedCharacteristics, serializer : JsonSerializer) =
        serializer.Serialize(writer, value.Expression)


type ResolvedInitializerConverter() =
    inherit JsonConverter<ResolvedInitializer>()

    override this.ReadJson(reader : JsonReader, objectType : Type, existingValue : ResolvedInitializer, hasExistingValue : bool, serializer : JsonSerializer) = 
        serializer.Deserialize<QsInitializerKind<ResolvedInitializer, TypedExpression>>(reader) |> ResolvedInitializer.New

    override this.WriteJson(writer : JsonWriter, value : ResolvedInitializer, serializer : JsonSerializer) =
        serializer.Serialize(writer, (value.Resolution))


type TypedExpressionConverter() =
    inherit JsonConverter<TypedExpression>()

    override this.ReadJson(reader : JsonReader, objectType : Type, existingValue : TypedExpression, hasExistingValue : bool, serializer : JsonSerializer) = 
        let (ex, paramRes, t, info, range) = serializer.Deserialize<QsExpressionKind<TypedExpression, Identifier, ResolvedType> * IEnumerable<KeyValuePair<QsTypeParameter,ResolvedType>> * ResolvedType * InferredExpressionInformation * QsRangeInfo>(reader) 
        {Expression = ex; TypeParameterResolutions = paramRes.ToImmutableDictionary(); ResolvedType = t; InferredInformation = info; Range = range}

    override this.WriteJson(writer : JsonWriter, value : TypedExpression, serializer : JsonSerializer) =
        serializer.Serialize(writer, (value.Expression, value.TypeParameterResolutions, value.ResolvedType, value.InferredInformation, value.Range))


type QsNamespaceConverter() =
    inherit JsonConverter<QsNamespace>()

    override this.ReadJson(reader : JsonReader, objectType : Type, existingValue : QsNamespace, hasExistingValue : bool, serializer : JsonSerializer) = 
        let (nsName, elements) = serializer.Deserialize<NonNullable<string> * IEnumerable<QsNamespaceElement>>(reader) 
        {Name = nsName; Elements = elements.ToImmutableArray(); Documentation = [].ToLookup(fst, snd)}

    override this.WriteJson(writer : JsonWriter, value : QsNamespace, serializer : JsonSerializer) =
        serializer.Serialize(writer, (value.Name, value.Elements))


type DictionaryAsArrayResolver () =
    inherit DefaultContractResolver()

    override this.CreateContract (objectType : Type) = 
        let isDictionary (t : Type) = 
            t = typedefof<IDictionary<_,_>> || 
            (t.IsGenericType && t.GetGenericTypeDefinition() = typeof<IDictionary<_,_>>.GetGenericTypeDefinition())
        if objectType.GetInterfaces().Any(new Func<_,_>(isDictionary)) then base.CreateArrayContract(objectType) :> JsonContract;
        else base.CreateContract(objectType);


module Json =
    
    let Converters ignoreSerializationException = 
        [|
            new NonNullableConverter<string>()                                  :> JsonConverter
            new ResolvedTypeConverter(ignoreSerializationException)             :> JsonConverter
            new ResolvedCharacteristicsConverter(ignoreSerializationException)  :> JsonConverter
            new TypedExpressionConverter()                                      :> JsonConverter
            new ResolvedInitializerConverter()                                  :> JsonConverter
            new QsNamespaceConverter()                                          :> JsonConverter
        |]

    let Serializer = 
        let settings = new JsonSerializerSettings() 
        settings.Converters <- Converters false
        settings.ContractResolver <- new DictionaryAsArrayResolver()
        JsonSerializer.CreateDefault(settings)

    let PermissiveSerializer = 
        let settings = new JsonSerializerSettings() 
        settings.Converters <- Converters true
        settings.ContractResolver <- new DictionaryAsArrayResolver()
        JsonSerializer.CreateDefault(settings)

