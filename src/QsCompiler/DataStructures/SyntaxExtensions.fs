﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

[<AutoOpen>]
[<System.Runtime.CompilerServices.Extension>]
module Microsoft.Quantum.QsCompiler.SyntaxExtensions

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open System.Runtime.CompilerServices
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens 
open Microsoft.Quantum.QsCompiler.SyntaxTree


let private OnTupleItems onSingle tupleName (items : ImmutableArray<'a>) = 
    if items.Length = 0 then failwith (sprintf "empty tuple in %s instance" tupleName)
    elif items.Length = 1 then items.[0] |> onSingle
    else Some (items |> Seq.toList) 


type QsInitializer with 
    
    // utils for tuple matching

    static member private OnTupleItems = OnTupleItems (fun (single : QsInitializer) -> single.TupleItems) "QsInitializer"
    member internal this.TupleItems = 
        match this.Initializer with 
        | InvalidInitializer -> None
        | QubitTupleAllocation items -> items |> QsInitializer.OnTupleItems
        | _ -> Some [this]


type ResolvedInitializer with 

    // utils for tuple matching

    static member private OnTupleItems = OnTupleItems (fun (single : ResolvedInitializer) -> single.TupleItems) "ResolvedInitializer"
    member internal this.TupleItems = 
        match this.Resolution with
        | InvalidInitializer -> None
        | QubitTupleAllocation items -> items |> ResolvedInitializer.OnTupleItems
        | _ -> Some [this]


type QsSymbol with 

    // utils for tuple matching

    static member private OnTupleItems = OnTupleItems (fun (single : QsSymbol) -> single.TupleItems) "QsSymbol"
    member internal this.TupleItems = 
        match this.Symbol with 
        | InvalidSymbol -> None
        | MissingSymbol -> Some []
        | SymbolTuple items -> items |> QsSymbol.OnTupleItems
        | _ -> Some [this]


type SymbolTuple with 

    // utils for tuple matching

    static member private OnTupleItems = OnTupleItems (fun (single : SymbolTuple) -> single.TupleItems) "SymbolTuple"
    member internal this.TupleItems = 
        match this with
        | InvalidItem -> None
        | DiscardedItem -> Some []
        | VariableNameTuple items -> items |> SymbolTuple.OnTupleItems
        | VariableName _ -> Some [this]


type ResolvedType with 

    // utils for internal use only

    member internal this.WithoutRangeInfo = 
        match this.Resolution with 
        | QsTypeKind.ArrayType bt -> bt.WithoutRangeInfo |> QsTypeKind.ArrayType 
        | QsTypeKind.Function (it, ot) -> (it.WithoutRangeInfo, ot.WithoutRangeInfo) |> QsTypeKind.Function
        | QsTypeKind.Operation ((it, ot), fs) -> ((it.WithoutRangeInfo, ot.WithoutRangeInfo), fs) |> QsTypeKind.Operation
        | QsTypeKind.TupleType ts -> (ts |> Seq.map (fun t -> t.WithoutRangeInfo)).ToImmutableArray() |> QsTypeKind.TupleType 
        | QsTypeKind.UserDefinedType udt -> {udt with Range = Null} |> QsTypeKind.UserDefinedType
        | QsTypeKind.TypeParameter tp -> {tp with Range = Null} |> QsTypeKind.TypeParameter
        | res -> res
        |> ResolvedType.New

    // utils for tuple matching

    static member private OnTupleItems = OnTupleItems (fun (single : ResolvedType) -> single.TupleItems) "ResolvedType"
    member internal this.TupleItems = 
        match this.Resolution with
        | InvalidType -> None
        | MissingType -> Some []
        | TupleType items -> items |> ResolvedType.OnTupleItems
        | _ -> Some [this]

    // utils for walking the data structure

    /// Walks the given resolved type, 
    /// and returns true if the type contains a type satisfying the given condition.
    /// Contained types are the type itself, array base types, tuple item types, 
    /// and argument and result types of functions and operations.
    /// Returns false otherwise.
    member this.Exists condition = 
        let recur (t : ResolvedType) = t.Exists condition
        match this.Resolution with 
        | _ when condition this.Resolution -> true
        | QsTypeKind.ArrayType bt -> bt |> recur
        | QsTypeKind.Function (it, ot) 
        | QsTypeKind.Operation ((it, ot), _) -> it |> recur || ot |> recur
        | QsTypeKind.TupleType ts -> ts |> Seq.map recur |> Seq.contains true
        | _ -> false

    /// Recursively applies the given function inner to the given item and  
    /// applies the given extraction function to each contained subitem of the returned type kind.
    /// Returns an enumerable of all extracted items. 
    static member private ExtractAll (inner : _ -> QsTypeKind<_,_,_,_>, extract : _ -> IEnumerable<_>) this : IEnumerable<_> = 
        let recur = ResolvedType.ExtractAll (inner, extract)
        match inner this with 
        | QsTypeKind.ArrayType bt -> bt |> recur
        | QsTypeKind.Function (it, ot) 
        | QsTypeKind.Operation ((it, ot), _) -> (it |> recur).Concat (ot |> recur)
        | QsTypeKind.TupleType ts -> ts |> Seq.collect recur 
        | _ -> Enumerable.Empty()
        |> (extract this).Concat

    /// Walks the given resolved type, 
    /// and applies the given extraction function to each contained type, 
    /// including array base types, tuple item types, and argument and result types of functions and operations.
    /// Returns an enumerable of all extracted return values. 
    member this.ExtractAll (extract : _ -> IEnumerable<_>) : IEnumerable<_> = 
        let inner (t : ResolvedType) = t.Resolution
        ResolvedType.ExtractAll (inner, (inner >> extract)) this


type QsType with 

    // utils for tuple matching

    static member private OnTupleItems = OnTupleItems (fun (single : QsType) -> single.TupleItems) "QsType"
    member internal this.TupleItems = 
        match this.Type with
        | InvalidType -> None
        | MissingType -> Some []
        | TupleType items -> items |> QsType.OnTupleItems
        | _ -> Some [this]

    // utils for walking the data structure

    /// Walks the given QsType, 
    /// and applies the given extraction function to each contained type.
    /// Returns an enumerable of all extracted types. 
    member public this.ExtractAll (extract : _ -> IEnumerable<_>) = 
        let inner (t : QsType) = t.Type
        ResolvedType.ExtractAll (inner, extract) this


type TypedExpression with 

    // utils for tuple matching

    static member private OnTupleItems = OnTupleItems (fun (single : TypedExpression) -> single.TupleItems) "TypedExpression"
    member internal this.TupleItems = 
        match this.Expression with
        | InvalidExpr -> None
        | MissingExpr -> Some []
        | ValueTuple items -> items |> TypedExpression.OnTupleItems
        | _ -> Some [this]

    // utils for walking the data structure

    /// Returns true if the expression contains any missing expression.
    /// Returns false otherwise.
    static member public ContainsMissing (ex : TypedExpression) =
        // we only need to check for top-level missing items
        match ex.TupleItems with
        | Some items when items.Length > 1 -> items |> List.exists TypedExpression.ContainsMissing
        | Some [] -> true
        | _ -> false

    /// Returns true if the expression is a call-like expression, and the arguments contain a missing expression.
    /// Returns false otherwise.
    static member public IsPartialApplication kind =
        match kind with
        | CallLikeExpression (_, args) -> args |> TypedExpression.ContainsMissing
        | _ -> false

    /// Returns true if the expression kind does not contain any inner expressions. 
    static member private IsAtomic (kind : QsExpressionKind<'E, _, _>) = 
        match kind with
        | UnitValue
        | Identifier _ 
        | IntLiteral _
        | BigIntLiteral _ 
        | DoubleLiteral _ 
        | BoolLiteral _ 
        | ResultLiteral _ 
        | PauliLiteral _ 
        | MissingExpr
        | InvalidExpr -> true
        | _ -> false

    /// Recursively traverses an expression by first applying a function "mapper" to modify the expression,
    /// then by finding all the subexpressions of the expression, then by applying MapFold to each subexpression,
    /// then combining all the results with the "folder" function. Returns the output of the "folder" function.
    /// Is used as a general way to traverse any expression, simplifying the code for the Exists and ExtractAll functions.
    static member public MapFold (mapper: 'E -> QsExpressionKind<'E, _, _>) (folder : 'A seq -> 'E -> 'A) (expr: 'E): 'A =
        let subExprs =
            match mapper expr with
            | NEG ex                             
            | BNOT ex                            
            | NOT ex                             
            | AdjointApplication ex              
            | ControlledApplication ex           
            | UnwrapApplication ex    
            | NamedItem (ex, _)
            | NewArray (_, ex)                   -> [ex] :> seq<_>
            | ADD (lhs,rhs)                      
            | SUB (lhs,rhs)                      
            | MUL (lhs,rhs)                      
            | DIV (lhs,rhs)                      
            | LT (lhs,rhs)                       
            | LTE (lhs,rhs)                      
            | GT (lhs,rhs)                       
            | GTE (lhs,rhs)                      
            | POW (lhs,rhs)                      
            | MOD (lhs,rhs)                      
            | LSHIFT (lhs,rhs)                   
            | RSHIFT (lhs,rhs)                   
            | BOR (lhs,rhs)                      
            | BAND (lhs,rhs)                     
            | BXOR (lhs,rhs)                     
            | AND (lhs,rhs)                      
            | OR (lhs,rhs)                       
            | EQ (lhs,rhs)                       
            | NEQ (lhs,rhs)                      
            | RangeLiteral (lhs, rhs)            
            | ArrayItem (lhs, rhs)               
            | CallLikeExpression (lhs,rhs)       -> upcast [lhs; rhs]
            | CopyAndUpdate (ex1, ex2, ex3)
            | CONDITIONAL(ex1, ex2, ex3)         -> upcast [ex1; ex2; ex3]
            | StringLiteral (_,items)            
            | ValueTuple items                   
            | ValueArray items                   -> upcast items
            | kind when TypedExpression.IsAtomic kind -> Seq.empty
            | _  -> NotImplementedException "missing implementation for the given expression kind" |> raise
        folder (Seq.map (TypedExpression.MapFold mapper folder) subExprs) expr

    /// Returns true if the expression contains a sub-expression satisfying the given condition.
    /// Returns false otherwise.
    member public this.Exists (condition : TypedExpression -> bool) =
        this |> TypedExpression.MapFold (fun ex -> ex.Expression) (fun sub ex ->
            condition ex || Seq.exists id sub)

    /// Recursively applies the given function inner to the given item and  
    /// applies the given extraction function to each contained subitem of the returned expression kind.
    /// Returns an enumerable of all extracted items. 
    static member private ExtractAll (inner : 'E -> QsExpressionKind<'E, _, _>, extract : _ -> seq<_>) (this : 'E) : seq<_> =
        this |> TypedExpression.MapFold inner (fun sub ex ->
            Seq.append (extract ex) (Seq.concat sub))

    /// Walks the given expression, 
    /// and applies the given extraction function to each contained expression.
    /// Returns an enumerable of all extracted expressions. 
    member public this.ExtractAll (extract : _ -> IEnumerable<_>) = 
        let inner (ex : TypedExpression) = ex.Expression
        TypedExpression.ExtractAll (inner, extract) this


type QsExpression with 

    // utils for tuple matching

    static member private OnTupleItems = OnTupleItems (fun (single : QsExpression) -> single.TupleItems) "QsExpression"
    member internal this.TupleItems = 
        match this.Expression with
        | InvalidExpr -> None
        | MissingExpr -> Some []
        | ValueTuple items -> items |> QsExpression.OnTupleItems
        | _ -> Some [this]

    // utils for walking the data structure

    /// Walks the given QsExpression, 
    /// and applies the given extraction function to each contained expression.
    /// Returns an enumerable of all extracted expressions. 
    member public this.ExtractAll (extract : _ -> IEnumerable<_>) = 
        let inner (ex : QsExpression) = ex.Expression
        TypedExpression.ExtractAll (inner, extract) this


type QsStatement with

    /// Recursively traverses a statement by first applying a function "mapper" to modify the statement,
    /// then by finding all the substatements of the statement, then by applying MapFold to each substatement,
    /// then combining all the results with the "folder" function. Returns the output of the "folder" function.
    /// Is used as a general way to traverse any statement, simplifying the code for the BaseStatements function.
    /// Even if a statement could be executed multiple times, such as the body of a ForStatement or the outer
    /// block of a Conjugation, the Fold function will process the statement only once.
    static member public Fold folder stmt =
        let subStmts =
            match stmt.Statement with
            | QsExpressionStatement _
            | QsReturnStatement _
            | QsFailStatement _
            | QsVariableDeclaration _
            | QsValueUpdate _ -> Seq.empty
            | QsConditionalStatement s ->
                (Seq.append
                    (s.ConditionalBlocks |> Seq.collect (fun (_, b) -> b.Body.Statements))
                    (match s.Default with Null -> Seq.empty | Value v -> upcast v.Body.Statements))
            | QsForStatement s -> upcast s.Body.Statements
            | QsWhileStatement s -> upcast s.Body.Statements
            | QsConjugation s -> Seq.append s.OuterTransformation.Body.Statements s.InnerTransformation.Body.Statements
            | QsRepeatStatement s -> Seq.append s.RepeatBlock.Body.Statements s.FixupBlock.Body.Statements
            | QsQubitScope s -> upcast s.Body.Statements
        folder (Seq.map (QsStatement.Fold folder) subStmts) stmt

    /// Returns true if the statement has a sub-statement satisfying the given condition.
    /// Returns false otherwise.
    static member public Exists condition =
        QsStatement.Fold (fun sub stmt -> condition stmt || Seq.exists id sub)

    /// Walks the given statement, and applies the given extraction function to each sub-statement.
    /// Returns a sequence of all extracted values.
    static member public ExtractAll extract =
        QsStatement.Fold (fun sub stmt -> Seq.append (extract stmt) (Seq.concat sub))


type QsTuple<'I> with 

    member this.ResolveWith getType = 
        let rec resolveInner = function 
            | QsTuple items -> (items |> Seq.map resolveInner).ToImmutableArray() |> TupleType |> ResolvedType.New
            | QsTupleItem item -> getType item
        match this with 
        | QsTuple items when items.Length = 0 -> UnitType |> ResolvedType.New 
        | _ -> resolveInner this

    /// Returns an enumerable of all contained tuple items. 
    member public this.Items = 
        let rec extractAll = function
            | QsTuple items -> items |> Seq.collect extractAll
            | QsTupleItem item -> seq { yield item } 
        this |> extractAll


// active pattern for tuple matching

 // not the nicest solution, but unfortunatly type extensions cannot be used to satisfy member constraints...
 // the box >> unbox below is used to cast the value to the inferred type of 'T
let private TupleItems<'T when 'T :> ITuple> (arg: 'T): 'T list option =
    let cast a = box >> unbox |> List.map |> Option.map <| a
    match box arg with
    | :? QsExpression               as arg -> cast arg.TupleItems
    | :? TypedExpression            as arg -> cast arg.TupleItems
    | :? QsType                     as arg -> cast arg.TupleItems
    | :? ResolvedType               as arg -> cast arg.TupleItems
    | :? QsInitializer              as arg -> cast arg.TupleItems
    | :? ResolvedInitializer        as arg -> cast arg.TupleItems
    // TODO: can be made an ITuple again once empty symbol tuples are no longer valid for functor specialiations...
    //| :? QsSymbol                   as arg -> arg.TupleItems |> Option.map (List.map box)
    | :? SymbolTuple                as arg -> cast arg.TupleItems
    | _ -> InvalidOperationException("no extension provided for tuple matching of the given ITuple object") |> raise

let (| Item | _ |) arg =         
    match TupleItems arg with
    | Some [item] -> Some item
    | _ -> None

let (| Tuple | _ |) arg =         
    match TupleItems arg with
    | Some items when items.Length > 1 -> Some items
    | _ -> None

let (| Missing | _ |) arg = 
    match TupleItems arg with
    | Some [] -> Some Missing
    | _ -> None


// look-up for udt and global callables

[<Extension>]
let GlobalTypeResolutions (syntaxTree : IEnumerable<QsNamespace>) = 
    let types =
        syntaxTree |> Seq.collect (fun ns -> ns.Elements |> Seq.choose (function
        | QsCustomType t -> Some (t.FullName, t)
        | _ -> None))
    types.ToImmutableDictionary(fst, snd)

[<Extension>]
let GlobalCallableResolutions (syntaxTree : IEnumerable<QsNamespace>) = 
    let callables =
        syntaxTree |> Seq.collect (fun ns -> ns.Elements |> Seq.choose (function
        | QsCallable c -> Some (c.FullName, c)
        | _ -> None))
    callables.ToImmutableDictionary(fst, snd)
    