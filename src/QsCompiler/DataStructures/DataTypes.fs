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

    /// If the given nullable has a value, applies the given function to it and returns the result as Value,
    /// and returns Null otherwise.
    static member Map fct = function 
        | Null -> Null
        | Value v -> Value (fct v)

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


[<Struct>]
type NonNullable<'T> = private Item of 'T with
    static member New value = if value = null then ArgumentNullException() |> raise else Item value
    member this.Value = this |> function | Item str -> str


[<Struct>]
type QsPositionInfo = 
    {Line : int; Column: int}
    with static member Zero = {Line = 1; Column = 1}


type QsRangeInfo = 
    QsNullable<QsPositionInfo * QsPositionInfo>


[<Struct>]
type QsCompilerDiagnostic = {
    Diagnostic : DiagnosticItem
    Arguments : IEnumerable<string>
    Range : QsPositionInfo * QsPositionInfo 
}
    with 
        static member DefaultRange = QsPositionInfo.Zero, QsPositionInfo.Zero

        /// builds a new diagnostics error with the give code and range
        static member New (item, args) (startPos, endPos) = 
            {Diagnostic = item; Arguments = args; Range = (startPos, endPos)}

        /// builds a new diagnostics error with the give code and range
        static member Error (code, args) (startPos, endPos) = 
            QsCompilerDiagnostic.New (code |> Error, args) (startPos, endPos)
        
        /// builds a new diagnostics warning with the give code and range
        static member Warning (code, args) (startPos, endPos) = 
            QsCompilerDiagnostic.New (code |> Warning, args) (startPos, endPos)
        
        /// builds a new diagnostics information with the give code and range
        static member Info (code, args) (startPos, endPos) = 
            QsCompilerDiagnostic.New (code |> Information, args) (startPos, endPos)

        member this.Code = 
            match this.Diagnostic with 
            | Error code -> (int)code
            | Warning code -> (int)code
            | Information code -> (int)code

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



