using System;

namespace Microsoft.Quantum.Sdk.Tools
{
    public static class DefaultEntryPoint
    {
        private static void Main(string[] args) =>
            Console.WriteLine(
                "Full support for executing projects compiled to QIR is not yet integrated into the Sdk. " +
                "For more information about the feature, see https://github.com/microsoft/qsharp-compiler/tree/main/src/QsCompiler/QirGeneration. " +
                "The generated QIR can be executed by manually linking the QIR runtime. " +
                "See https://github.com/microsoft/qsharp-runtime/tree/main/src/QirRuntime for further instructions.");
    }
}
