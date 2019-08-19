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
        try this.ReturnFrom(m)
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


/// The exception monad. Returns an Error if any of the lines are Errors.
type internal ResultBuilder() =

    member this.Return(x) = Ok x

    member this.ReturnFrom(m) = m

    member this.Yield(x) = Error x

    member this.YieldFrom(m) = m

    member this.Bind(m, f) = Result.bind f m

    member this.Zero() = Ok ()

    member this.Combine(m, f) = Result.bind f m

    member this.Delay(f: unit -> _) = f

    member this.Run(f) = f()

    member this.TryWith(m, h) =
        try this.ReturnFrom(m)
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
        
    member this.While(guard, f) =
        match guard() with
        | Ok false -> this.Zero()
        | x -> this.Bind(x, fun _ ->
            this.Bind(f(), fun () ->
                this.While(guard, f)))

    member this.For(sequence:seq<_>, body) =
        this.Using(sequence.GetEnumerator(),
            fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current)))

/// The exception monad. Returns an Error if any of the lines are Errors.
let internal result = ResultBuilder()
