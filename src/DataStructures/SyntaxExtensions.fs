// Copyright (c) Microsoft Corporation. All rights reserved.
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


type QsType with 

    // utils for tuple matching

    static member private OnTupleItems = OnTupleItems (fun (single : QsType) -> single.TupleItems) "QsType"
    member internal this.TupleItems = 
        match this.Type with
        | InvalidType -> None
        | MissingType -> Some []
        | TupleType items -> items |> QsType.OnTupleItems
        | _ -> Some [this]


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

    /// Walks the given resolved type, 
    /// and applies the given extraction function to each contained type, 
    /// including array base types, tuple item types, and argument and result types of functions and operations.
    /// Returns an enumerable of all extracted return values. 
    member this.ExtractAll (extract : _ -> IEnumerable<_>) : IEnumerable<_> = 
        let recur (t : ResolvedType) = t.ExtractAll extract
        match this.Resolution with 
        | QsTypeKind.ArrayType bt -> bt |> recur
        | QsTypeKind.Function (it, ot) 
        | QsTypeKind.Operation ((it, ot), _) -> (it |> recur).Concat (ot |> recur)
        | QsTypeKind.TupleType ts -> ts |> Seq.collect recur 
        | _ -> Enumerable.Empty()
        |> (extract this.Resolution).Concat


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

    /// Returns true if the expression is a call-like expression, and the arguments contain a missing expression.
    /// Returns false otherwise. 
    static member public IsPartialApplication kind = 
        let rec containsMissing (ex : TypedExpression) = 
            match ex.TupleItems with // we only need to check for top-level missing items
            | Some items when items.Length > 1 -> items |> List.map containsMissing |> List.contains true
            | Some [] -> true
            | _ -> false
        kind |> function 
        | CallLikeExpression (_, args) -> args |> containsMissing
        | _ -> false

    /// Returns true if the expression contains a sub-expression satisfying the given condition.
    /// Returns false otherwise.
    member public this.Exists (condition : TypedExpression -> bool) =
        condition this || this.Expression |> function
            | NEG ex                            
            | BNOT ex                           
            | NOT ex  
            | AdjointApplication ex             
            | ControlledApplication ex          
            | UnwrapApplication ex              
            | NewArray (_, ex)                   -> ex.Exists condition
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
            | CallLikeExpression (lhs,rhs)       -> lhs.Exists condition || rhs.Exists condition
            | CONDITIONAL(cond, ifTrue, ifFalse) -> cond.Exists condition || ifTrue.Exists condition || ifFalse.Exists condition
            | StringLiteral (_,items)            
            | ValueTuple items                  
            | ValueArray items                   -> items |> Seq.map (fun item -> item.Exists condition) |> Seq.contains true            
            | _                                  -> false

    /// Recursively applies the given function inner to the given item and  
    /// applies the given extraction function to each contained subitem of the returned expression kind.
    /// Returns an enumerable of all extracted items. 
    static member private ExtractAll (inner : 'E -> QsExpressionKind<'E, _, _>, extract : _ -> IEnumerable<_>) (this : 'E) : IEnumerable<_> = 
        let recur = TypedExpression.ExtractAll (inner, extract)
        match inner this with 
        | NEG ex                            
        | BNOT ex                           
        | NOT ex  
        | AdjointApplication ex             
        | ControlledApplication ex          
        | UnwrapApplication ex              
        | NewArray (_, ex)                   -> ex |> recur
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
        | CallLikeExpression (lhs,rhs)       -> (lhs |> recur).Concat (rhs |> recur)
        | CONDITIONAL(cond, ifTrue, ifFalse) -> ((cond |> recur).Concat (ifTrue |> recur)).Concat (ifFalse |> recur)
        | StringLiteral (_,items)            
        | ValueTuple items                  
        | ValueArray items                   -> items |> Seq.collect recur            
        | _                                  -> Seq.empty
        |> (extract this).Concat

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

let private TupleItems<'I> (arg : ITuple) = arg |> function  // not the nicest solution, but unfortunatly type extensions cannot be used to satisfy member constraints...
    | :? QsExpression               as arg -> arg.TupleItems |> Option.map (List.map box)
    | :? TypedExpression            as arg -> arg.TupleItems |> Option.map (List.map box)
    | :? QsType                     as arg -> arg.TupleItems |> Option.map (List.map box)
    | :? ResolvedType               as arg -> arg.TupleItems |> Option.map (List.map box)
    | :? QsInitializer              as arg -> arg.TupleItems |> Option.map (List.map box)
    | :? ResolvedInitializer        as arg -> arg.TupleItems |> Option.map (List.map box)
    // TODO: can be made an ITuple again once empty symbol tuples are no longer valid for functor specialiations...
    //| :? QsSymbol                   as arg -> arg.TupleItems |> Option.map (List.map box) 
    | :? SymbolTuple                as arg -> arg.TupleItems |> Option.map (List.map box)
    | _ -> InvalidOperationException("no extension provided for tuple matching of the given ITuple object") |> raise

let (| Item | _ |) arg =         
    match TupleItems arg with
    | Some [item] -> Some (item |> unbox)
    | _ -> None

let (| Tuple | _ |) arg =         
    match TupleItems arg with 
    | Some [] | Some [_] -> None
    | Some items when items.Length > 1 -> Some (items |> List.map unbox)
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
    