// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.DataTypes

open System
open System.Collections.Generic
open Microsoft.Quantum.QsCompiler.Diagnostics


[<Struct>]
type QsNullable<'T> = // to avoid having to include the F# core in the C# part of the compiler...
| Null
| Value of 'T

    /// If the given nullable has a value, applies the given function to it and returns the result, which must be
    /// another nullable. Returns Null otherwise.
    static member Bind fct = function
        | Null -> Null
        | Value v -> fct v

    /// If the given nullable has a value, applies the given function to it and returns the result as Value,
    /// and returns Null otherwise.
    static member Map fct =
        QsNullable<_>.Bind (fct >> Value)

    /// If the given nullable has a value, applies the given function to it.
    static member Iter fct = function 
        | Null -> ()
        | Value v -> fct v

    /// If the given nullable has a value, applies (fct fallback) to it and returns the result, 
    /// and returns fallback otherwise.
    static member Fold fct fallback = function
        | Null -> fallback
        | Value v -> fct fallback v

    /// If the given Nullable has a value returns it, calls fallback otherwise, and returns its result.
    member this.ValueOrApply fallback = 
        match this with | Null -> fallback() | Value v -> v

    /// If the given nullable has a value returns it, and returns the given fallback otherwise.
    member this.ValueOr fallback = 
        this.ValueOrApply (fun _ -> fallback)

    /// Applies the given function to each element of the sequence. 
    /// Returns the sequence comprised of the results x for each element where the function returns Value x
    static member Choose fct (seq : _ seq) = 
        seq |> Seq.choose (fct >> function | Null -> None | Value v -> Some v) 

    /// Converts the given F# Option to a QsNullable.
    static member FromOption opt = opt |> function 
        | Some v -> Value v
        | None -> Null

/// Operations for QsNullable<'T>.
module QsNullable =
    /// Applies 'f' to both nullable arguments if both have a Value. Returns Null otherwise.
    let Map2 f a b =
        match a, b with
        | Value a, Value b -> f a b |> Value
        | _ -> Null

[<Struct>]
type NonNullable<'T> = private Item of 'T with
    static member New value = if value = null then ArgumentNullException() |> raise else Item value
    member this.Value = this |> function | Item str -> str


// Zero-based position.
// TODO: Add documentation.
[<CustomEquality>]
[<CustomComparison>]
type Position =
    private
    | Position of int * int

    member this.Line = match this with Position (line, _) -> line

    member this.Column = match this with Position (_, column) -> column

    member a.CompareTo (b : Position) =
        if a.Line < b.Line || a.Line = b.Line && a.Column < b.Column
        then -1
        elif a.Line = b.Line && a.Column = b.Column
        then 0
        else 1

    override a.Equals b =
        match b with
        | :? Position as b' -> a.CompareTo b' = 0
        | _ -> false

    override this.GetHashCode () = hash this

    interface Position IComparable with
        override a.CompareTo b = a.CompareTo b

    interface IComparable with
        override a.CompareTo b =
            match b with
            | :? Position as b' -> a.CompareTo b'
            | _ -> ArgumentException "The object is not a Position." |> raise

    static member (+) (a : Position, b : Position) =
        Position (a.Line + b.Line,
                  if b.Line > 0 then b.Column else a.Column + b.Column)

    static member op_Equality (a : Position, b : Position) = a.Equals b

    static member op_GreaterThan (a : Position, b) = (a :> Position IComparable).CompareTo b > 0

    static member Create line column =
        if line < 0 || column < 0
        then ArgumentOutOfRangeException "Line and column cannot be negative." |> raise
        Position (line, column)

    static member Zero = Position (0, 0)

// TODO: Add documentation.
type Range =
    private
    | Range of Position * Position

    member this.Start = match this with Range (start, _) -> start

    member this.End = match this with Range (_, end') -> end'

    static member (+) (position : Position, range : Range) =
        Range (range.Start + position, range.End + position)

    static member (+) (range : Range, position : Position) = position + range

    static member op_Equality (a : Range, b : Range) = a = b

    static member Create start ``end`` =
        if start > ``end``
        then ArgumentException "Range start cannot occur after range end." |> raise
        Range (start, ``end``)

    static member Zero = Range (Position.Zero, Position.Zero)

    static member Combine (a : Range) (b : Range) =
        Range (min a.Start b.Start, max a.End b.End)

[<Struct>]
type QsCompilerDiagnostic =
    { Diagnostic : DiagnosticItem
      Arguments : IEnumerable<string>
      Range : Range }

    /// builds a new diagnostics error with the give code and range
    static member New (item, args) range = { Diagnostic = item; Arguments = args; Range = range }

    /// builds a new diagnostics error with the give code and range
    static member Error (code, args) range = QsCompilerDiagnostic.New (Error code, args) range

    /// builds a new diagnostics warning with the give code and range
    static member Warning (code, args) range = QsCompilerDiagnostic.New (Warning code, args) range

    /// builds a new diagnostics information with the give code and range
    static member Info (code, args) range = QsCompilerDiagnostic.New (Information code, args) range

    member this.Code =
        match this.Diagnostic with
        | Error code -> int code
        | Warning code -> int code
        | Information code -> int code

    member this.Message =
        match this.Diagnostic with
        | Error code -> DiagnosticItem.Message (code, this.Arguments)
        | Warning code -> DiagnosticItem.Message (code, this.Arguments)
        | Information code -> DiagnosticItem.Message (code, this.Arguments)


/// interface used to pass anything lock-like to the symbol table (could not find an existing one??) 
type IReaderWriterLock = 
    abstract member EnterReadLock : unit -> unit
    abstract member ExitReadLock : unit -> unit
    abstract member EnterWriteLock : unit -> unit
    abstract member ExitWriteLock : unit -> unit
