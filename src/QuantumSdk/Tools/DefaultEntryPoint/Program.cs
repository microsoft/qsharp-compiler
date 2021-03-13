// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Sdk.Tools
{
    public static class DefaultEntryPoint
    {
        private const string CSharpEntryPoint =
@"using System;

namespace Microsoft.Quantum.Sdk.Tools
{
    public static class DefaultEntryPoint
    {
        private static void Main(string[] args) =>
            Console.WriteLine(
                ""Full support for executing projects compiled to QIR is not yet integrated into the Sdk. "" +
                ""The generated QIR can be executed by manually linking the QIR runtime. "" +
                ""See https://github.com/microsoft/qsharp-runtime/tree/main/src/QirRuntime for further instructions"");
    }
}
";

        private static void Main(string[] args)
        {
            System.IO.File.WriteAllText(args[0], CSharpEntryPoint);
        }
    }
}
