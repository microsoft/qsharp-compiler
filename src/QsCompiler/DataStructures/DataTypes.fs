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

    /// <summary>
    /// Returns the original nullable if it is <see cref="Value"/>, otherwise returns <paramref name="ifNull"/>.
    /// </summary>
    let internal orElse ifNull = function
        | Null -> ifNull
        | Value value -> Value value

    /// <summary>
    /// Returns true if the nullable is <see cref="Value"/> and is equal to <paramref name="value"/>.
    /// </summary>
    [<CompiledName "Contains">]
    let contains value = function
        | Null -> false
        | Value value' -> value = value'

    /// <summary>
    /// Returns true if the nullable is <see cref="Null"/>.
    /// </summary>
    [<CompiledName "IsNull">]
    let isNull = function
        | Null -> true
        | Value _ -> false

    /// <summary>
    /// Returns true if the nullable is <see cref="Value"/>.
    /// </summary>
    [<CompiledName "IsValue">]
    let isValue nullable = isNull nullable |> not

/// A position in a text document.
type Position = private Position of int * int with

    /// The line number, where line zero is the first line in the document.
    member this.Line = match this with Position (line, _) -> line

    /// The column number, where column zero is the first column in the line.
    member this.Column = match this with Position (_, column) -> column

    /// <summary>
    /// Translates the first position by the amount of the second position. If the resulting position is on the same
    /// line, the column numbers are added; otherwise, the second position's column number is used.
    /// </summary>
    /// <remarks>
    /// Addition is an associative but not commutative operation.
    /// </remarks>
    static member (+) (offset : Position, relative : Position) =
        let line = offset.Line + relative.Line
        let column =
            if relative.Line = 0
            then offset.Column + relative.Column
            else relative.Column
        Position (line, column)

    /// <summary>
    /// Translates the first position backwards by the amount of the second position. If the resulting position is on
    /// the same line, the column numbers are subtracted; otherwise, the first position's column number is used.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the resulting position has a negative line or column number.
    /// </exception>
    static member (-) (position : Position, offset : Position) =
        let line = position.Line - offset.Line
        let column =
            if position.Line = offset.Line
            then position.Column - offset.Column
            else position.Column
        Position (line, column)

    /// Returns true if the positions have the same line and column numbers.
    static member op_Equality (a : Position, b : Position) = a = b

    /// Returns true if the second position occurs before the first position.
    static member op_LessThan (a : Position, b) = (a :> Position IComparable).CompareTo b < 0

    /// Returns true if the second position occurs on or before the first position.
    static member op_LessThanOrEqual (a : Position, b) = (a :> Position IComparable).CompareTo b <= 0

    /// Returns true if the second position occurs after the first position. 
    static member op_GreaterThan (a : Position, b) = (a :> Position IComparable).CompareTo b > 0

    /// Returns true if the second position occurs on or after the first position.
    static member op_GreaterThanOrEqual (a : Position, b) = (a :> Position IComparable).CompareTo b >= 0

    /// <summary>
    /// Creates a position.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the line or column is negative.</exception>
    static member Create line column =
        if line < 0 || column < 0
        then ArgumentOutOfRangeException "Line and column cannot be negative." |> raise
        Position (line, column)

    /// The position at line zero and column zero.
    static member Zero = Position (0, 0)

/// A range between two positions in a text document.
type Range = private Range of Position * Position with

    /// The start of the range.
    member this.Start = match this with Range (start, _) -> start

    /// The end of the range.
    member this.End = match this with Range (_, end') -> end'

    /// Returns true if the range contains the given position, excluding the end position.
    member this.Contains position = this.Start <= position && position < this.End

    /// Returns true if the range contains the given position, including the end position.
    member this.ContainsEnd position = this.Start <= position && position <= this.End

    /// Returns a copy of this range with the given offset added to the line numbers.
    member this.WithLineNumOffset offset =
        Range (Position (this.Start.Line + offset, this.Start.Column),
               Position (this.End.Line + offset, this.End.Column))

    /// Adds the range's start and end positions to the given position.
    static member (+) (position : Position, range : Range) =
        Range (position + range.Start, position + range.End)

    /// Returns true if the ranges have the same start and end positions.
    static member op_Equality (a : Range, b : Range) = a = b

    /// <summary>
    /// Creates a range.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="start"/> occurs after <paramref name="end"/>.
    /// </exception>
    static member Create start ``end`` =
        if start > ``end``
        then ArgumentException "Range start cannot occur after range end." |> raise
        Range (start, ``end``)

    /// The empty range starting and ending at position zero.
    static member Zero = Range (Position.Zero, Position.Zero)

    /// Creates a new range that spans the smallest starting position of either range to the largest ending position of
    /// either range.
    static member Span (a : Range) (b : Range) =
        Range (min a.Start b.Start, max a.End b.End)

    /// Returns true if the ranges overlap.
    static member Overlaps (a : Range) (b : Range) = min a.Start b.Start < max a.End b.End

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
