// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Quantum.Telemetry.Commands;
using Microsoft.Quantum.Telemetry.OutOfProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Quantum.Telemetry.Tests.OutOfProcess
{
    [TestClass]
    public class SimpleYamlSerializerTests : CommandsTestCommon
    {
        private static async IAsyncEnumerable<T> EnumerableToAsyncEnumerable<T>(IEnumerable<T> items, int delayInMilliseconds = 1)
        {
            foreach (var item in items)
            {
                await Task.Delay(delayInMilliseconds);
                yield return item;
            }
        }

        private static IAsyncEnumerable<string> EnumerableToAsyncEnumerable(string text, int delayInMilliseconds = 1) =>
            EnumerableToAsyncEnumerable(text.Split(new string[] { "\r\n", "\r", "\r", "\n" }, int.MaxValue, StringSplitOptions.None));

        private static async Task<IEnumerable<T>> AsyncEnumerableToEnumerable<T>(IAsyncEnumerable<T> items)
        {
            List<T> result = new List<T>();
            await foreach (var item in items)
            {
                result.Add(item);
            }

            return result;
        }

        [TestMethod]
        public async Task TestSimpleYamlSerializerSingleCommand()
        {
            var yamlSerializer = new SimpleYamlSerializer();

            var command = CreateLogEventCommand(seed: 0);

            var serializedResults = yamlSerializer.Write(command).ToList();
            var serializedText = string.Join(System.Environment.NewLine, serializedResults);
            var deserializedResults = yamlSerializer.Read(EnumerableToAsyncEnumerable(serializedResults));
            var commandResults = (await AsyncEnumerableToEnumerable(deserializedResults)).ToList();

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
    dateTimeProp: !DateTime 8/12/2021 8:09:10 AM
    boolProp: !Boolean True
    guidProp: !Guid 00000000-007b-01c8-0102-030405060708
    stringPropPii: !String+Pii stringPropValue{seed}
    longPropPii: !Long+Pii 123
    doublePropPii: !Double+Pii 123.123
    dateTimePropPii: !DateTime+Pii 8/12/2021 8:09:10 AM
    boolPropPii: !Boolean+Pii True
    guidPropPii: !Guid+Pii 00000000-037a-01c8-0102-030405060708
";
            Assert.AreEqual(expectedSerializedResults, serializedText);

            var unexpectedCommandType =
@"- command: !MyNewCommand
    longProp: !Long 123
    boolPropPii: !Boolean+Pii True
";
            deserializedResults = yamlSerializer.Read(EnumerableToAsyncEnumerable(unexpectedCommandType));
            commandResults = (await AsyncEnumerableToEnumerable(deserializedResults)).ToList();
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

- command: !LogEvent
    __name__: !String eventName1
  yet another line to ignore
    stringProp: stringPropValue1

";
            deserializedResults = yamlSerializer.Read(EnumerableToAsyncEnumerable(unexpectedLines));
            var logEventCommandResults = (await AsyncEnumerableToEnumerable(deserializedResults))
                .OfType<LogEventCommand>().ToList();
            Assert.AreEqual(2, logEventCommandResults.Count);
            Assert.AreEqual("eventName0", logEventCommandResults[0].Args.Name);
            Assert.AreEqual("stringPropValue0", logEventCommandResults[0].Args.Properties["stringProp"]);
            Assert.AreEqual("eventName1", logEventCommandResults[1].Args.Name);
            Assert.AreEqual("stringPropValue1", logEventCommandResults[1].Args.Properties["stringProp"]);
        }

        [TestMethod]
        public async Task TestSimpleYamlSerializerMultipleCommands()
        {
            var yamlSerializer = new SimpleYamlSerializer();

            List<CommandBase> commands = new List<CommandBase>();
            commands.AddRange(CreateSetContextCommands());
            commands.AddRange(CreateLogEventCommands());
            commands.Add(new QuitCommand());

            var serializedResults = yamlSerializer.Write(commands).ToList();
            var deserializedResults = yamlSerializer.Read(EnumerableToAsyncEnumerable(serializedResults));
            var commandResults = (await AsyncEnumerableToEnumerable(deserializedResults)).ToList();

            Assert.AreEqual(commands.Count, commandResults.Count);
            for (int i = 0; i < commands.Count; i++)
            {
                Assert.AreEqual(commands[i], commandResults[i]);
            }
        }
    }
}
