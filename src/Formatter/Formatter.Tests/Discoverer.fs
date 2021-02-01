// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace QsFmt.Formatter.Tests

open QsFmt.Formatter
open System
open System.Reflection
open Xunit

/// A test case containing an example of source code before and after formatting.
type Example =
    {
        /// The name of the test case.
        Name: string

        /// A reason for skipping the test, if Some. The test should not be skipped if None.
        Skip: string option

        /// The source code before formatting.
        Before: string

        /// The expected source code after formatting.
        After: string
    }

    override example.ToString() = example.Name

/// A test case containing source code that should not change after formatting.
type FixedPoint =
    {
        /// The name of the test case.
        Name: string

        /// A reason for skipping the test, if Some. The test should not be skipped if None.
        Skip: string option

        /// The source code before and after formatting.
        Source: string
    }

    override fixedPoint.ToString() = fixedPoint.Name

module internal Example =
    /// Converts an example test case into a fixed point test case.
    let toFixedPoint (example: Example) =
        {
            Name = example.Name
            Skip = example.Skip
            Source = example.After
        }

/// <summary>
/// Marks a property of type <c>string * string</c> that should be run as an example test case, with the first item
/// being the source code before and the second item being the source code after.
/// </summary>
type ExampleAttribute() =
    inherit Attribute()

    /// A reason for skipping the test, if non-null. The test should not be skipped if null.
    member val Skip: string = null with get, set

/// <summary>
/// Marks a property of type <c>string</c> that should be run as a fixed point test case, with the string being the
/// before and after source code.
/// </summary>
type FixedPointAttribute() =
    inherit Attribute()

    /// A reason for skipping the test, if non-null. The test should not be skipped if null.
    member val Skip: string = null with get, set

/// <summary>
/// Test cases that are auto-discovered based on <see cref="ExampleAttribute"/> and <see cref="FixedPointAttribute"/>.
/// </summary>
module Discoverer =
    /// <summary>
    /// Returns all properties in the assembly that have an attribute of type <typeparamref name="a"/>.
    /// </summary>
    let private properties<'a when 'a :> Attribute> () =
        Assembly.GetCallingAssembly().GetTypes()
        |> Seq.collect (fun typ -> typ.GetProperties())
        |> Seq.choose (fun property ->
            property.GetCustomAttributes typeof<'a>
            |> Seq.tryHead
            |> Option.map (fun attribute' -> attribute' :?> 'a, property))

    /// <summary>
    /// The auto-discovered <see cref="Example"/> test cases.
    /// </summary>
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

    /// <summary>
    /// The auto-discovered <see cref="FixedPoint"/> test cases.
    /// </summary>
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

    /// <summary>
    /// Provides auto-discovered <see cref="Example"/> test cases as theory data.
    /// </summary>
    type private ExampleData() as data =
        inherit TheoryData<Example>()

        do examples |> Seq.iter data.Add

    /// <summary>
    /// Provides auto-discovered <see cref="FixedPoint"/> test cases as theory data.
    /// </summary>
    type private FixedPointData() as data =
        inherit TheoryData<FixedPoint>()

        do examples |> Seq.map Example.toFixedPoint |> Seq.append fixedPoints |> Seq.iter data.Add

    /// <summary>
    /// Asserts that the auto-discovered <see cref="Example"/> test cases are correct.
    /// </summary>
    [<SkippableTheory>]
    [<ClassData(typeof<ExampleData>)>]
    let ``Code is formatted correctly`` (example: Example) =
        match example.Skip with
        | Some reason -> Skip.If(true, reason)
        | None -> Assert.Equal(example.After, Formatter.format example.Before)

    /// <summary>
    /// Asserts that the auto-discovered <see cref="FixedPoint"/> test cases are correct.
    /// </summary>
    [<SkippableTheory>]
    [<ClassData(typeof<FixedPointData>)>]
    let ``Formatted code is unchanged`` fixedPoint =
        match fixedPoint.Skip with
        | Some reason -> Skip.If(true, reason)
        | None -> Assert.Equal(fixedPoint.Source, Formatter.format fixedPoint.Source)
