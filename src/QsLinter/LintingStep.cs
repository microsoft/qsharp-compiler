// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using System.Diagnostics;

namespace Microsoft.Quantum.Compiler.Linter
{
    public partial class LintingStep : IRewriteStep
    {
        private List<Warning> Warnings = new List<Warning>();
        private Stack<string> NamespaceStack = new Stack<string>();
        private string? OutputPath;

        public string? CurrentNamespace => NamespaceStack.Count == 0 ? null : NamespaceStack.Peek();
        public string Name => "QSLint";

        public int Priority => 0;

        public IDictionary<string, string> AssemblyConstants => new Dictionary<string, string>();

        public bool ImplementsTransformation => true;

        public bool ImplementsPreconditionVerification => false;

        public bool ImplementsPostconditionVerification => false;

        public bool PostconditionVerification(QsCompilation compilation) => throw new NotImplementedException();

        public bool PreconditionVerification(QsCompilation compilation) => throw new NotImplementedException();

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => null;

        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            System.Console.WriteLine("I: Beginning qslint scan.");

            // Write assembly constants out for debugging.
            foreach (var item in AssemblyConstants)
            {
                System.Console.WriteLine($"D: Using constant {item.Key} = {item.Value}.");
            }
            // TODO: populate OutputPath here.

            // Always perform the identity transformation, as this step is only
            // used to check the syntax tree, not do anything.
            transformed = compilation;

            foreach (var ns in compilation.Namespaces)
            {
                try
                {
                    CheckNamespace(ns);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Exception writing checking namespace {ns.Name.GetValue()}:");
                    System.Console.WriteLine($"\t{ex.GetType()} {ex.Message}");
                    System.Console.WriteLine(ex.StackTrace);
                }
            }

            // After we're done, report out.
            System.Console.WriteLine($"I: qslint complete, found {Warnings.Count} possible issues.");
            try
            {
                WriteReport().Wait();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Exception writing out linting report:");
                System.Console.WriteLine($"\t{ex.GetType()} {ex.Message}");
                System.Console.WriteLine(ex.StackTrace);
            }

            return true;
        }

        private async Task WriteReport()
        {
            var outputPath = OutputPath == null
                ? Path.Join(
                    Directory.GetCurrentDirectory(),
                    "obj",
                    "qslint-report.json"
                )
                : OutputPath;
            var serializerOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            serializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            );
            var groupedWarnings = Warnings
                .GroupBy(
                    warning => warning.Namespace ?? ""
                )
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .GroupBy(warning => warning.Category)
                        .ToDictionary(
                            group => group.Key.ToString(),
                            group => group.ToArray()
                        )
                );

            var serializedReport = JsonSerializer.Serialize(
                groupedWarnings, serializerOptions
            );
            await File.WriteAllTextAsync(outputPath, serializedReport);
        }

        private void Warn(Warning warning)
        {
            Warnings.Add(warning);
            System.Console.WriteLine(warning.Format());
        }

        private void Warn(WarningCategory category, string message, SourceLocation? location = null) =>
            Warn(new Warning
            {
                Category = category,
                Message = message,
                Namespace = CurrentNamespace,
                Location = location
            });

        private ContextManager EnterNamespace(string @namespace)
        {
            NamespaceStack.Push(@namespace);
            return new ContextManager(() => NamespaceStack.Pop());
        }

        private bool IdentifierExists(string identifier)
        {
            return true; // FIXME: walk the AST to find the given identifier.
        }

        private void CheckNamespace(QsNamespace ns)
        {
            using (EnterNamespace(ns.Name.Value))
            {
                // Check each part of the fully qualified namespace name.
                foreach (var nsPart in ns.Name.Value.Split("."))
                {
                    CheckIdentifier(nsPart);
                }

                // Make sure that the documentation on the namespace itself is
                // checked.
                CheckNamespaceDocumentation(ns);

                // After checking the namespace itself, check the contents of
                // the namespace.
                foreach (var element in ns.Elements.Where(item => !item.IsPrivate()))
                {
                    if (element.TryGetCallable(out var callable))
                    {
                        Debug.Assert(callable != null);
                        if (callable.IsDeprecated())
                        {
                            continue;
                        }
                        CheckCallableDeclaration(((QsNamespaceElement.QsCallable)element).Item);
                    }
                    else if (element.TryGetUdt(out var udt))
                    {
                        Debug.Assert(udt != null);
                        if (udt.IsDeprecated())
                        {
                            continue;
                        }
                        CheckUdtDeclaration(((QsNamespaceElement.QsCustomType)element).Item);
                    }
                }
            }
        }

        private void CheckUdtDeclaration(QsCustomType udt)
        {
            CheckUdtDocumentation(udt);

            // Check the name of the UDT and each of its constructors inputs.
            CheckIdentifier(udt.FullName.Name.Value, IdentifierKind.Global);

            // Check the items as well.
            // TODO: check type items.
        }
        private void CheckCallableDeclaration(QsCallable callable)
        {
            // Don't check type constructors here, we'll check the items of those
            // directly.
            if (callable.Kind == QsCallableKind.TypeConstructor)
            {
                return;
            }

            CheckCallableDocumentation(callable);

            // Check the name of the callable and each of its inputs.
            CheckIdentifier(
                callable.FullName.Name.Value,
                kind: IdentifierKind.Global,
                location: callable.GetLocation()
            );
            CheckInputs(callable.ArgumentTuple);
        }

        private void CheckInputs(QsTuple<LocalVariableDeclaration<QsLocalSymbol>> argumentTuple)
        {
            foreach (var input in argumentTuple.GetItems())
            {
                if (input.VariableName.TryGetName(out var name))
                {
                    // TryGetName guarantees that name is non-null when its return
                    // value is true.
                    Debug.Assert(name != null);

                    // If the callable is a type constructor, we are allowed to ignore
                    // reserved identifiers, such as __Item1__.
                    if (!name.IsReserved())
                    {
                        // TODO: preserve location information here.
                        CheckIdentifier(name, IdentifierKind.Local);
                    }
                }
            }
        }
    }
}
