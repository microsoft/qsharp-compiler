namespace QsFmt.Formatter.Tests

open QsFmt.Formatter
open System
open System.Reflection
open Xunit

type Example =
    {
        Name: string
        Skip: string option
        Before: string
        After: string
    }

    override example.ToString() = example.Name

type FixedPoint =
    {
        Name: string
        Skip: string option
        Source: string
    }

    override fixedPoint.ToString() = fixedPoint.Name

module internal Example =
    let toFixedPoint (example: Example) =
        {
            Name = example.Name
            Skip = example.Skip
            Source = example.After
        }

type ExampleAttribute() =
    inherit Attribute()

    member val Skip: string = null with get, set

type FixedPointAttribute() =
    inherit Attribute()

    member val Skip: string = null with get, set

module Discoverer =
    let private properties<'a when 'a :> Attribute> () =
        Assembly.GetCallingAssembly().GetTypes()
        |> Seq.collect (fun typ -> typ.GetProperties())
        |> Seq.choose (fun property ->
            property.GetCustomAttributes typeof<'a>
            |> Seq.tryHead
            |> Option.map (fun attribute' -> attribute' :?> 'a, property))

    let private examples =
        properties<ExampleAttribute> ()
        |> Seq.choose (fun (attribute, property) ->
            match property.GetValue null with
            | :? (string * string) as example ->
                {
                    Name = property.Name
                    Skip = Option.ofObj attribute.Skip
                    Before = fst example
                    After = snd example
                }
                |> Some
            | _ -> None)

    let private fixedPoints =
        properties<FixedPointAttribute> ()
        |> Seq.choose (fun (attribute, property) ->
            match property.GetValue null with
            | :? string as source ->
                {
                    Name = property.Name
                    Skip = Option.ofObj attribute.Skip
                    Source = source
                }
                |> Some
            | _ -> None)

    type private ExampleData() as data =
        inherit TheoryData<Example>()

        do examples |> Seq.iter data.Add

    type private FixedPointData() as data =
        inherit TheoryData<FixedPoint>()

        do examples |> Seq.map Example.toFixedPoint |> Seq.append fixedPoints |> Seq.iter data.Add

    [<SkippableTheory>]
    [<ClassData(typeof<ExampleData>)>]
    let ``Code is formatted correctly`` (example: Example) =
        match example.Skip with
        | Some reason -> Skip.If(true, reason)
        | None -> Assert.Equal(example.After, Formatter.format example.Before)

    [<SkippableTheory>]
    [<ClassData(typeof<FixedPointData>)>]
    let ``Formatted code is unchanged`` fixedPoint =
        match fixedPoint.Skip with
        | Some reason -> Skip.If(true, reason)
        | None -> Assert.Equal(fixedPoint.Source, Formatter.format fixedPoint.Source)
