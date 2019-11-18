// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Hashing

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SymbolManagement.DataStructures
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Newtonsoft.Json


/// Generates a hash for a resolved type. Does not incorporate any positional information.
let rec TypeHash (t : ResolvedType) = t.Resolution |> function
    | QsTypeKind.ArrayType b                    -> hash (0, TypeHash b)
    | QsTypeKind.TupleType ts                   -> hash (1, (ts |> Seq.map TypeHash |> Seq.toList))
    | QsTypeKind.UserDefinedType udt            -> hash (2, udt.Namespace.Value, udt.Name.Value)
    | QsTypeKind.TypeParameter tp               -> hash (3, tp.Origin.Namespace.Value, tp.Origin.Name.Value, tp.TypeName.Value)
    | QsTypeKind.Operation ((inT, outT), fList) -> hash (4, (inT |> TypeHash), (outT |> TypeHash), (fList |> JsonConvert.SerializeObject))
    | QsTypeKind.Function (inT, outT)           -> hash (5, (inT |> TypeHash), (outT |> TypeHash))
    | kind                                      -> JsonConvert.SerializeObject kind |> hash

/// Generates a hash for a typed expression. Does not incorporate any positional information.
let rec ExpressionHash (ex : TypedExpression) = ex.Expression |> function
    | StringLiteral (s, _)              -> hash (6, s)
    | ValueTuple vs                     -> hash (7, (vs |> Seq.map ExpressionHash |> Seq.toList))
    | ValueArray vs                     -> hash (8, (vs |> Seq.map ExpressionHash |> Seq.toList))
    | NewArray (bt, idx)                -> hash (9, TypeHash bt, ExpressionHash idx)
    | Identifier (GlobalCallable c, _)  -> hash (10, c.Namespace.Value, c.Name.Value)
    | kind                              -> JsonConvert.SerializeObject kind |> hash


// methods specifically for computing header hashes

let internal AttributesHash (attributes : QsDeclarationAttribute seq) = 
    let getHash arg (id : UserDefinedType) = hash (id.Namespace.Value, id.Name.Value, ExpressionHash arg)
    attributes |> QsNullable<_>.Choose (fun att -> att.TypeId |> QsNullable<_>.Map (getHash att.Argument)) |> Seq.toList

let internal CallableHeaderHash (kind, (signature,_), specs, attributes : QsDeclarationAttribute seq) =
    let signatureHash (signature : ResolvedSignature) = 
        let argStr = signature.ArgumentType |> TypeHash
        let reStr = signature.ReturnType |> TypeHash
        let nameOrInvalid = function | InvalidName -> InvalidName |> JsonConvert.SerializeObject | ValidName sym -> sym.Value
        let typeParams = signature.TypeParameters |> Seq.map nameOrInvalid |> Seq.toList
        hash (argStr, reStr, typeParams)
    let specsStr =
        let genHash (gen : ResolvedGenerator) = 
            let tArgs = gen.TypeArguments |> QsNullable<_>.Map (fun tArgs -> tArgs |> Seq.map TypeHash |> Seq.toList)
            hash (gen.Directive, hash tArgs)
        let kinds, gens = specs |> Seq.sort |> Seq.toList |> List.unzip
        hash (kinds, gens |> List.map genHash)
    hash (kind, specsStr, signatureHash signature, attributes |> AttributesHash)

let internal TypeHeaderHash (t, typeItems : QsTuple<QsTypeItem>, attributes) = 
    let getItemHash (itemName, itemType) = hash (itemName, TypeHash itemType)
    let namedItems = typeItems.Items |> Seq.choose (function | Named item -> Some item | _ -> None)
    let itemHashes = namedItems |> Seq.map (fun d -> d.VariableName, d.Type) |> Seq.map getItemHash
    hash (TypeHash t, itemHashes |> Seq.toList, attributes |> AttributesHash)

