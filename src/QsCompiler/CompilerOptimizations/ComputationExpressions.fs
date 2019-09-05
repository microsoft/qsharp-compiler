﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.CompilerOptimization.ComputationExpressions

open System


/// The maybe monad. Returns None if any of the lines are None.
type internal MaybeBuilder() =

    member this.Return(x) = Some x

    member this.ReturnFrom(m: 'T option) = m

    member this.Bind(m, f) = Option.bind f m

    member this.Zero() = Some ()

    member this.Combine(m, f) = Option.bind f m

    member this.Delay(f: unit -> _) = f

    member this.Run(f) = f()

    member this.TryWith(m, h) =
        try this.ReturnFrom(m())
        with e -> h e

    member this.TryFinally(m, compensation) =
        try this.ReturnFrom(m)
        finally compensation()

    member this.Using(res:#IDisposable, body) =
        this.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())

    member this.While(guard, f) =
        if not (guard())
        then this.Zero()
        else this.Bind(f(), fun () ->
            this.While(guard, f))

    member this.For(sequence:seq<_>, body) =
        this.Using(sequence.GetEnumerator(),
            fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current)))

/// The maybe monad. Returns None if any of the lines are None.
let internal maybe = MaybeBuilder()

/// Returns Some () if x is true, and returns None otherwise.
/// Normally used after a do! in the Maybe monad, which makes this act as an assertion.
let internal check x = if x then Some () else None






type ImperativeResult<'a, 'b, 'c> =
| Normal of 'b * 'a
| Break of 'a
| Interrupt of 'c
type Imperative<'a, 'b, 'c> = ('a -> ImperativeResult<'a, 'b, 'c>)


type internal ImperativeBuilder() =

    member this.Return (x: 'b): Imperative<'a, 'b, 'c> = (fun s -> Normal (x, s))
    member this.ReturnFrom (m: Imperative<'a, 'b, 'c>): Imperative<'a, 'b, 'c> = m

    member this.Yield (x: 'c): Imperative<'a, 'b, 'c> = (fun _ -> Interrupt x)
    member this.YieldFrom (m: Imperative<'a, 'b, 'c>): Imperative<'a, 'b, 'c> = m

    member this.Bind (m: Imperative<'a, 'b, 'c>, f: 'b -> Imperative<'a, 'b2, 'c>): Imperative<'a, 'b2, 'c> =
        (fun s1 ->
            match m s1 with
            | Normal (value, s2) -> f value s2
            | Break s2 -> Break s2
            | Interrupt x -> Interrupt x)

    member this.Zero (): Imperative<'a, Unit, 'c> = this.Return ()

    member this.Combine (m1: Imperative<'a, Unit, 'c>, m2: Imperative<'a, 'b, 'c>): Imperative<'a, 'b, 'c> =
        this.Bind (m1, fun () -> m2)

    member this.Combine2 (m1: Imperative<'a, Unit, 'c>, m2: Imperative<'a, Unit, 'c>): Imperative<'a, Unit, 'c> =
        (fun s1 ->
            match m1 s1 with
            | Normal ((), s2) -> m2 s2
            | Break s2 -> Normal ((), s2)
            | Interrupt x -> Interrupt x)

    member this.Delay (f: Unit -> Imperative<'a, 'b, 'c>): Imperative<'a, 'b, 'c> = f ()

    member this.While (guard: Unit -> Imperative<'a, bool, 'c>, f: Imperative<'a, Unit, 'c>): Imperative<'a, Unit, 'c> =
        (fun s1 ->
            match guard() s1 with
            | Normal (true, s2) -> this.Combine2 (f, this.While (guard, f)) s2
            | Normal (false, s2) -> Normal ((), s2)
            | Break _ -> Exception "Cannot break in condition of while loop" |> raise
            | Interrupt x -> Interrupt x)

    member this.While (guard: Unit -> bool, f: Imperative<'a, Unit, 'c>): Imperative<'a, Unit, 'c> =
        this.While (guard >> this.Return, f)

    member this.For (sequence: list<'d>, body: 'd -> Imperative<'a, Unit, 'c>): Imperative<'a, Unit, 'c> =
        (fun s1 ->
            match sequence with
            | [] -> Normal ((), s1)
            | head :: tail -> this.Combine2 (body head, this.For (tail, body)) s1)

    member this.For (sequence: seq<'d>, body: 'd -> Imperative<'a, Unit, 'c>): Imperative<'a, Unit, 'c> =
        this.For (List.ofSeq sequence, body)



/// The exception monad. Returns an Interrupt if any of the lines are Interrupts.
let internal imperative = ImperativeBuilder()

let internal getState: Imperative<'a, 'a, 'c> = fun s -> Normal (s, s)
let internal putState s: Imperative<'a, Unit, 'c> = fun _ -> Normal ((), s)
let internal updateState f: Imperative<'a, Unit, 'c> = fun s -> Normal ((), f s)

let inline internal (>>=) m f = imperative.Bind (m, f)
