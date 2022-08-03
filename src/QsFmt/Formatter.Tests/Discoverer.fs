// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.Tests

open System
open System.Reflection
open Microsoft.Quantum.QsFmt.Formatter
open Xunit

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

/// <summary>
/// Marks a property of type <c>string</c> that should be run as a fixed point test case, with the string being the
/// before and after source code.
/// </summary>
type FixedPointAttribute() =
    inherit Attribute()

    /// A reason for skipping the test, if non-null. The test should not be skipped if null.
    member val Skip: string = null with get, set

type ExampleKind =
    | Format = 0
    | Update = 1

/// A test case containing an example of source code before and after transforming.
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

        // The kind of example it is, either format or update.
        Kind: ExampleKind
    }

    override example.ToString() = example.Name

module internal Example =
    /// Converts an example test case into a fixed point test case.
    let toFixedPoint example =
        match example.Kind with
        | ExampleKind.Format ->
            {
                Name = example.Name
                Skip = example.Skip
                Source = example.After
            }
            |> Some
        | _ -> None

/// <summary>
/// Marks a property of type <c>string * string</c> that should be run as an example test case, with the first item
/// being the source code before and the second item being the source code after.
/// </summary>
type ExampleAttribute(exampleKind: ExampleKind) =
    inherit Attribute()

    /// A reason for skipping the test, if non-null. The test should not be skipped if null.
    member val Skip: string = null with get, set

    /// The kind of example it is, either FormatExample or UpdateExample
    member _.Kind = exampleKind

/// <summary>
/// A wrapper around <see cref="Result"/> with a <see cref="Object.ToString"/> implementation that uses structured
/// formatting.
/// </summary>
type private ShowResult<'Value, 'Error> = private ShowResult of Result<'Value, 'Error>

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
    let private examples: seq<Example> =
        properties<ExampleAttribute> ()
        |> Seq.choose (fun (attribute, property) ->
            match property.GetValue null with
            | :? (string * string) as example ->
                ({
                     Name = property.Name
                     Skip = Option.ofObj attribute.Skip
                     Before = fst example
                     After = snd example
                     Kind = attribute.Kind
                 }: Example)
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
    /// Provides auto-discovered <see cref="Example"/> test cases for format examples as theory data.
    /// </summary>
    type private FormatExampleData() as data =
        inherit TheoryData<Example>()

        do examples |> Seq.filter (fun e -> e.Kind = ExampleKind.Format) |> Seq.iter data.Add

    /// <summary>
    /// Provides auto-discovered <see cref="Example"/> test cases for update examples as theory data.
    /// </summary>
    type private UpdateExampleData() as data =
        inherit TheoryData<Example>()

        do examples |> Seq.filter (fun e -> e.Kind = ExampleKind.Update) |> Seq.iter data.Add

    /// <summary>
    /// Provides auto-discovered <see cref="FixedPoint"/> test cases as theory data.
    /// </summary>
    type private FixedPointData() as data =
        inherit TheoryData<FixedPoint>()

        do examples |> Seq.choose Example.toFixedPoint |> Seq.append fixedPoints |> Seq.iter data.Add

    /// <summary>
    /// Ensures that the new line characters will conform to the standard of the environment's new line character.
    /// </summary>
    let standardizeNewLines (s: string) =
        s.Replace("\r", "").Replace("\n", Environment.NewLine)

    /// <summary>
    /// Asserts that the auto-discovered <see cref="Example"/> format test cases change from their
    /// 'Before' state to their 'After' state under formatting.
    /// </summary>
    [<SkippableTheory>]
    [<ClassData(typeof<FormatExampleData>)>]
    let ``Code is formatted correctly`` example =
        match example.Skip with
        | Some reason -> Skip.If(true, reason)
        | None ->
            let after = example.After |> standardizeNewLines |> Ok |> ShowResult
            let before = Formatter.format None example.Before |> Result.map standardizeNewLines |> ShowResult
            Assert.Equal(after, before)

    /// <summary>
    /// Asserts that the auto-discovered <see cref="Example"/> update test cases change from their
    /// 'Before' state to their 'After' state under updating.
    /// </summary>
    [<SkippableTheory>]
    [<ClassData(typeof<UpdateExampleData>)>]
    let ``Code is updated correctly`` example =
        match example.Skip with
        | Some reason -> Skip.If(true, reason)
        | None ->
            let after = example.After |> standardizeNewLines |> Ok |> ShowResult
            let before = Formatter.update "" None example.Before |> Result.map standardizeNewLines |> ShowResult
            Assert.Equal(after, before)

    /// <summary>
    /// Asserts that the auto-discovered <see cref="FixedPoint"/> test cases do not change under formatting.
    /// </summary>
    [<SkippableTheory>]
    [<ClassData(typeof<FixedPointData>)>]
    let ``Formatted code is unchanged`` (fixedPoint: FixedPoint) =
        match fixedPoint.Skip with
        | Some reason -> Skip.If(true, reason)
        | None ->
            let original = Ok fixedPoint.Source |> ShowResult
            let formatted = Formatter.format None fixedPoint.Source |> ShowResult
            Assert.Equal(original, formatted)
