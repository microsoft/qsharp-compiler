// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.FSharp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Quantum.Telemetry.Tests
{
    [TestClass]
    public class HelpersTests
    {
        [TestMethod]
        public void TestFSharpOptionsHelper()
        {
            Type fSharpOptionType = typeof(FSharpOption<int>);
            FSharpOption<int> fsharpOptionWithValue = 123;
            FSharpOption<int> fsharpOptionWithNone = FSharpOption<int>.None;

            Assert.IsTrue(FSharpOptionHelper.IsFSharpOptionType(fSharpOptionType));
            Assert.IsFalse(FSharpOptionHelper.IsFSharpOptionType(typeof(string)));

            Assert.IsNull(FSharpOptionHelper.GetOptionValue(fsharpOptionWithNone));
            Assert.IsNull(FSharpOptionHelper.GetOptionValue(fsharpOptionWithNone, fSharpOptionType));

            Assert.AreEqual(fsharpOptionWithValue.Value, FSharpOptionHelper.GetOptionValue(fsharpOptionWithValue));
            Assert.AreEqual(fsharpOptionWithValue.Value, FSharpOptionHelper.GetOptionValue(fsharpOptionWithValue, fSharpOptionType));
        }
    }
}
