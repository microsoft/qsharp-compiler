module Microsoft.Quantum.QsCompiler.CompilerOptimization.ComputationExpressions

open System


type Monad (returnMethod, bindMethod) =

    member this.Bind(m, f) = (bindMethod |> box |> unbox) f m

    member this.Return(x) = (returnMethod) x

    member this.ReturnFrom(x) = x

    member this.Yield(x) = this.Return x

    member this.YieldFrom(x) = x

    member this.Zero() = this.Return ()

    member this.Combine(m, f) = this.Bind(m, f)

    member this.Delay(f) = f

    member this.Run(f) = f()

    member this.While(guard, body) =
        if not (guard()) 
        then this.Zero() 
        else this.Bind(body(), fun () ->
            this.While(guard, body))

    member this.TryWith(body, handler) =
        try this.ReturnFrom(body())
        with e -> handler e

    member this.TryFinally(body, compensation) =
        try this.ReturnFrom(body())
        finally compensation() 

    member this.Using(disposable: #System.IDisposable, body) =
        let body' = fun () -> body disposable
        this.TryFinally(body', fun () ->
            match disposable with
            | null -> ()
            | disp -> disp.Dispose())

    member this.For(sequence: seq<_>, body) =
        this.Using(sequence.GetEnumerator(), fun enum ->
            this.While(enum.MoveNext,
                this.Delay(fun () -> body enum.Current)))


/// The maybe monad. Returns None if any of the lines are None.
type MaybeBuilder() =

    member this.Return(x) = Some x

    member this.ReturnFrom(m: 'T option) = m

    member this.Bind(m, f) = Option.bind f m

    member this.Zero() = None

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
        if not (guard()) then Some () else
        do f() |> ignore
        this.While(guard, f)

    member this.For(sequence:seq<_>, body) =
        this.Using(sequence.GetEnumerator(),
            fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current)))

let maybe = MaybeBuilder()


type ResultBuilder() =

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

let result = ResultBuilder()

