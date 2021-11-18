// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Quantum.Telemetry.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Quantum.Telemetry.Tests.OutOfProcess
{
    [TestClass]
    public class SimpleYamlSerializerTests : TestCommon
    {
        private static IEnumerable<string> StringToEnumerable(string text, int delayInMilliseconds = 1) =>
            text.Split(new string[] { "\r\n", "\r", "\r", "\n" }, int.MaxValue, StringSplitOptions.None);

        [TestMethod]
        public void TestSimpleYamlSerializerSingleCommand()
        {
            var yamlSerializer = new SimpleYamlSerializer();

            var command = CreateLogEventCommand(seed: 0);

            var serializedResults = yamlSerializer.Write(command).ToList();
            var serializedText = string.Join(System.Environment.NewLine, serializedResults);
            var commandResults = yamlSerializer.Read(serializedResults).ToList();

            // Test that the deserialized command is equal to the serialized one
            Assert.AreEqual(1, commandResults.Count());
            Assert.AreEqual(command, commandResults[0]);

            // Test that the serialized text is as expected
            var expectedSerializedResults =
@"- command: !LogEvent
    __name__: !String eventName0
    stringProp: !String stringPropValue0
    stringMultilineProp: !String line1_0__\r\n__line2__\r\n__line3
    longProp: !Long 123
    doubleProp: !Double 123.123
    dateTimeProp: !DateTime 2021-08-12T08:09:10.0000000
    boolProp: !Boolean True
    guidProp: !Guid 00000000-007b-01c8-0102-030405060708
    stringPropPii: !String+Pii stringPropValue0
    longPropPii: !Long+Pii 123
    doublePropPii: !Double+Pii 123.123
    dateTimePropPii: !DateTime+Pii 2021-08-12T08:09:10.0000000
    boolPropPii: !Boolean+Pii True
    guidPropPii: !Guid+Pii 00000000-037a-01c8-0102-030405060708
";
            Assert.AreEqual(expectedSerializedResults, serializedText);

            var unexpectedCommandType =
@"- command: !MyNewCommand
    longProp: !Long 123
    boolPropPii: !Boolean+Pii True
";
            commandResults = yamlSerializer.Read(StringToEnumerable(unexpectedCommandType)).ToList();
            Assert.AreEqual(0, commandResults.Count);

            var unexpectedLines =
@"--- # line to ignore

another line to ignore

- command: !LogEvent
    __name__: !String eventName0
    stringProp: !String stringPropValue0
    stringMultilineProp: !String line1_0__\r\n__line2__\r\n__line3

    - one more line to ignore
    longProp: !Long 123
    doubleProp: !Double 123.123
    dateTimeProp: !DateTime 8/12/2021 8:09:10 AM

!@#$#@$$@#!#!@#!@#!@#!@#
%$#@$ Trash Data
%ˆ#%@#!@#!3

- command: !LogEvent
    __name__: !String eventName1
  yet another line to ignore
    stringProp: string value with no type
    emptyStringProp1: !String
    emptyStringProp2:

- command: !LogEvent
    __name__: !String consecutiveEvent1
- command: !LogEvent
    __name__: !String consecutiveEvent2

";
            var logEventCommandResults = yamlSerializer.Read(StringToEnumerable(unexpectedLines))
                                                       .OfType<LogEventCommand>()
                                                       .ToList();
            Assert.AreEqual(4, logEventCommandResults.Count);

            Assert.AreEqual("eventName0", logEventCommandResults[0].Args.Name);
            Assert.AreEqual("stringPropValue0", logEventCommandResults[0].Args.Properties["stringProp"]);
            Assert.AreEqual(2, logEventCommandResults[0].Args.Properties.Count);

            Assert.AreEqual("eventName1", logEventCommandResults[1].Args.Name);
            Assert.AreEqual("string value with no type", logEventCommandResults[1].Args.Properties["stringProp"]);
            Assert.AreEqual("", logEventCommandResults[1].Args.Properties["emptyStringProp1"]);
            Assert.AreEqual("", logEventCommandResults[1].Args.Properties["emptyStringProp2"]);

            Assert.AreEqual("consecutiveEvent1", logEventCommandResults[2].Args.Name);

            Assert.AreEqual("consecutiveEvent2", logEventCommandResults[3].Args.Name);
        }

        [TestMethod]
        public void TestSimpleYamlSerializerMultipleCommands()
        {
            var yamlSerializer = new SimpleYamlSerializer();

            List<CommandBase> commands = new List<CommandBase>();
            commands.AddRange(CreateSetContextCommands());
            commands.AddRange(CreateLogEventCommands());
            commands.Add(new QuitCommand());

            var serializedResults = yamlSerializer.Write(commands).ToList();
            var commandResults = yamlSerializer.Read(serializedResults).ToList();

            CollectionAssert.AreEqual(commands, commandResults);
        }
    }
}
