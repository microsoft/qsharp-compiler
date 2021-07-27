// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsFmt.Formatter.Tests

open Microsoft.Quantum.QsFmt.Formatter
open System
open System.Reflection
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

/// A test case containing an example of source code before and after formatting.
type FormatExample =
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

module internal FormatExample =
    /// Converts an example test case into a fixed point test case.
    let toFixedPoint (example: FormatExample) =
        {
            Name = example.Name
            Skip = example.Skip
            Source = example.After
        }

/// <summary>
/// Marks a property of type <c>string * string</c> that should be run as an example test case, with the first item
/// being the source code before and the second item being the source code after.
/// </summary>
type FormatExampleAttribute() =
    inherit Attribute()

    /// A reason for skipping the test, if non-null. The test should not be skipped if null.
    member val Skip: string = null with get, set

/// A test case containing an example of source code before and after updating.
type UpdateExample =
    {
        /// The name of the test case.
        Name: string

        /// A reason for skipping the test, if Some. The test should not be skipped if None.
        Skip: string option

        /// The source code before updating.
        Before: string

        /// The expected source code after updating.
        After: string
    }

    override example.ToString() = example.Name

module internal UpdateExample =
    /// Converts an example test case into a fixed point test case.
    let toFixedPoint (example: UpdateExample) =
        {
            Name = example.Name
            Skip = example.Skip
            Source = example.After
        }

/// <summary>
/// Marks a property of type <c>string * string</c> that should be run as an example test case, with the first item
/// being the source code before and the second item being the source code after.
/// </summary>
type UpdateExampleAttribute() =
    inherit Attribute()

    /// A reason for skipping the test, if non-null. The test should not be skipped if null.
    member val Skip: string = null with get, set

/// <summary>
/// A wrapper around <see cref="Result"/> with a <see cref="Object.ToString"/> implementation that uses structured
/// formatting.
/// </summary>
type private ShowResult<'value, 'error> = private ShowResult of Result<'value, 'error>

/// <summary>
/// Test cases that are auto-discovered based on <see cref="FormatExampleAttribute"/> and <see cref="FixedPointAttribute"/>.
/// </summary>
module Discoverer =
    /// <summary>
    /// Returns all properties in the assembly that have an attribute of type <typeparamref name="a"/>.
    /// </summary>
    let private properties<'a when 'a :> Attribute> () =
        Assembly.GetCallingAssembly().GetTypes()
        |> Seq.collect (fun typ -> typ.GetProperties())
        |> Seq.choose
            (fun property ->
                property.GetCustomAttributes typeof<'a>
                |> Seq.tryHead
                |> Option.map (fun attribute' -> attribute' :?> 'a, property))

    /// <summary>
    /// The auto-discovered <see cref="FormatExample"/> test cases.
    /// </summary>
    let private formatExamples : seq<FormatExample> =
        properties<FormatExampleAttribute> ()
        |> Seq.choose
            (fun (attribute, property) ->
                match property.GetValue null with
                | :? (string * string) as example ->
                    ({
                        Name = property.Name
                        Skip = Option.ofObj attribute.Skip
                        Before = fst example
                        After = snd example
                    } : FormatExample)
                    |> Some
                | _ -> None
            )

    /// <summary>
    /// The auto-discovered <see cref="UpdateExample"/> test cases.
    /// </summary>
    let private updateExamples : seq<UpdateExample> =
        properties<UpdateExampleAttribute> ()
        |> Seq.choose
            (fun (attribute, property) ->
                match property.GetValue null with
                | :? (string * string) as example ->
                    ({
                        Name = property.Name
                        Skip = Option.ofObj attribute.Skip
                        Before = fst example
                        After = snd example
                    } : UpdateExample)
                    |> Some
                | _ -> None
            )

    /// <summary>
    /// The auto-discovered <see cref="FixedPoint"/> test cases.
    /// </summary>
    let private fixedPoints =
        properties<FixedPointAttribute> ()
        |> Seq.choose
            (fun (attribute, property) ->
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
    /// Provides auto-discovered <see cref="FormatExample"/> test cases as theory data.
    /// </summary>
    type private FormatExampleData() as data =
        inherit TheoryData<FormatExample>()

        do formatExamples |> Seq.iter data.Add

    /// <summary>
    /// Provides auto-discovered <see cref="UpdateExample"/> test cases as theory data.
    /// </summary>
    type private UpdateExampleData() as data =
        inherit TheoryData<UpdateExample>()

        do updateExamples |> Seq.iter data.Add

    /// <summary>
    /// Provides auto-discovered <see cref="FixedPoint"/> test cases as theory data.
    /// </summary>
    type private FixedPointData() as data =
        inherit TheoryData<FixedPoint>()

        do
            let formatFixPoints = formatExamples |> Seq.map FormatExample.toFixedPoint
            let updateFixPoints = updateExamples |> Seq.map UpdateExample.toFixedPoint
            formatFixPoints
            |> Seq.append updateFixPoints
            |> Seq.append fixedPoints
            |> Seq.iter data.Add

    /// <summary>
    /// Asserts that the auto-discovered <see cref="FormatExample"/> test cases change from their
    /// 'Before' state to their 'After' state under formatting.
    /// </summary>
    [<SkippableTheory>]
    [<ClassData(typeof<FormatExampleData>)>]
    let ``Code is formatted correctly`` (example: FormatExample) =
        match example.Skip with
        | Some reason -> Skip.If(true, reason)
        | None -> Assert.Equal(Ok example.After |> ShowResult, Formatter.format example.Before |> ShowResult)

    /// <summary>
    /// Asserts that the auto-discovered <see cref="UpdateExample"/> test cases change from their
    /// 'Before' state to their 'After' state under updating.
    /// </summary>
    [<SkippableTheory>]
    [<ClassData(typeof<UpdateExampleData>)>]
    let ``Code is updated correctly`` (example: UpdateExample) =
        match example.Skip with
        | Some reason -> Skip.If(true, reason)
        | None -> Assert.Equal(Ok example.After |> ShowResult, Formatter.update example.Before |> ShowResult)

    /// <summary>
    /// Asserts that the auto-discovered <see cref="FixedPoint"/> test cases do not change under formatting.
    /// </summary>
    [<SkippableTheory>]
    [<ClassData(typeof<FixedPointData>)>]
    let ``Formatted code is unchanged`` (fixedPoint : FixedPoint) =
        match fixedPoint.Skip with
        | Some reason -> Skip.If(true, reason)
        | None -> Assert.Equal(Ok fixedPoint.Source |> ShowResult, Formatter.format fixedPoint.Source |> ShowResult)
