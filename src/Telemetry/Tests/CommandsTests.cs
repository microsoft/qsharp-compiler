// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Quantum.Telemetry.Tests
{
    [TestClass]
    public class CommandsTests : TestCommon
    {
        [TestMethod]
        public void TestLogEventCommand()
        {
            var command1 = CreateLogEventCommand(0);
            var commandEqualTo1 = CreateLogEventCommand(0);
            var commandDifferentThan1a = CreateLogEventCommand(1);
            var commandDifferentThan1b = CreateLogEventCommand(0);
            commandDifferentThan1b.Args.SetProperty("stringProp", "differentValue");
            var commandDifferentThan1c = CreateQuitCommand();

            // Test object.Equals
            Assert.AreEqual(command1, commandEqualTo1);
            Assert.AreNotEqual(command1, commandDifferentThan1a);
            Assert.AreNotEqual(command1, commandDifferentThan1b);
            Assert.AreNotEqual(command1, commandDifferentThan1c);

            // Test object.GetHashCode
            Assert.AreEqual(command1.GetHashCode(), commandEqualTo1.GetHashCode());
            Assert.AreNotEqual(command1.GetHashCode(), commandDifferentThan1a.GetHashCode());
            Assert.AreNotEqual(command1.GetHashCode(), commandDifferentThan1c.GetHashCode());

            // The hashcodes in this case are equal because
            // it just considers the Type and event name
            Assert.AreEqual(command1.GetHashCode(), commandDifferentThan1b.GetHashCode());

            // Test Visitor Pattern on .Process method
            var outOfProcessServer = CreateNullOutOfProcessServer();
            command1.Process(outOfProcessServer);
            Assert.AreEqual(command1, outOfProcessServer.LastProcessedCommand);
            Assert.AreEqual(command1.GetType(), outOfProcessServer.LastProcessedType);
        }

        [TestMethod]
        public void TestQuitCommand()
        {
            var command1 = CreateQuitCommand();
            var commandEqualTo1 = CreateQuitCommand();
            var commandDifferentThan1 = CreateLogEventCommand(0);

            // Test object.Equals
            Assert.AreEqual(command1, commandEqualTo1);
            Assert.AreNotEqual(command1, commandDifferentThan1);

            // Test object.GetHashCode
            Assert.AreEqual(command1.GetHashCode(), commandEqualTo1.GetHashCode());
            Assert.AreNotEqual(command1.GetHashCode(), commandDifferentThan1.GetHashCode());

            // Test Visitor Pattern on .Process method
            var outOfProcessServer = CreateNullOutOfProcessServer();
            command1.Process(outOfProcessServer);
            Assert.AreEqual(command1, outOfProcessServer.LastProcessedCommand);
            Assert.AreEqual(command1.GetType(), outOfProcessServer.LastProcessedType);
        }

        [TestMethod]
        public void TestSetContextCommand()
        {
            var commandList1 = CreateSetContextCommands().ToList();
            var commandListEqualTo1 = CreateSetContextCommands().ToList();

            for (int i = 0; i < commandList1.Count; i++)
            {
                var nextIndex = (i + 1) % commandList1.Count;
                var command1 = commandList1[i];
                var commandEqualTo1 = commandListEqualTo1[i];
                var commandDifferentThan1 = commandList1[nextIndex];

               // Test object.Equals
                Assert.AreEqual(command1, commandEqualTo1);
                Assert.AreNotEqual(command1, commandDifferentThan1);

                // Test object.GetHashCode
                Assert.AreEqual(command1.GetHashCode(), commandEqualTo1.GetHashCode());
                Assert.AreNotEqual(command1.GetHashCode(), commandDifferentThan1.GetHashCode());

                // Test Visitor Pattern on .Process method
                var outOfProcessServer = CreateNullOutOfProcessServer();
                command1.Process(outOfProcessServer);
                Assert.AreEqual(command1, outOfProcessServer.LastProcessedCommand);
                Assert.AreEqual(command1.GetType(), outOfProcessServer.LastProcessedType);
            }
        }
    }
}
