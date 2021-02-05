// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.Documentation
{
    /// <summary>
    ///     Writes API documentation files as Markdown to a given output
    ///     directory, using parsed API documentation comments.
    /// </summary>
    public class DocumentationWriter
    {
        /// <summary>
        ///     An event that is raised on diagnostics about documentation
        ///     writing (e.g., if an I/O problem prevents writing to disk).
        /// </summary>
        public event Action<IRewriteStep.Diagnostic>? OnDiagnostic;

        /// <summary>
        ///      Path to which output documentation files should be written.
        /// </summary>
        public string OutputPath { get; }

        private readonly string? packageName;

        /// <summary>
        ///     The name of the NuGet package whose contents are being
        ///     documented, or <c>null</c> if the documentation being written
        ///     does not concern a particular package.
        /// </summary>
        public string? PackageName => this.packageName;

        private readonly string packageLink;

        /// <summary>
        ///     Markdown mode used to mark Q# syntax blocks.
        /// </summary>
        public virtual string LanguageMode => "qsharp";

        private static string AsSeeAlsoLink(string target, string? currentNamespace = null)
        {
            var actualTarget = currentNamespace == null || target.Contains(".")
                ? target
                : $"{currentNamespace}.{target}";
            return $"- [{actualTarget}](xref:{actualTarget})";
        }

        private async Task TryWithExceptionsAsDiagnostics(string description, Func<Task> action, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                this.OnDiagnostic?.Invoke(new IRewriteStep.Diagnostic
                {
                    Severity = severity,
                    Message = $"Exception raised when {description}:\n{ex.Message}",
                    Range = null,
                    Source = null,
                    Stage = IRewriteStep.Stage.Transformation,
                });
            }
        }

        private async Task WriteAllTextAsync(string filename, string contents)
        {
            await this.TryWithExceptionsAsDiagnostics(
                $"writing output to {filename}",
                async () => await File.WriteAllTextAsync(
                    Path.Join(this.OutputPath, filename.ToLowerInvariant()),
                    contents));
        }

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="DocumentationWriter"/> class.
        /// </summary>
        /// <param name="outputPath">
        ///     The path to which documentation files should be written.
        /// </param>
        /// <param name="packageName">
        ///     The name of the NuGet package being documented, or <c>null</c>
        ///     if the documentation to be written by this object does not
        ///     relate to a particular package.
        /// </param>
        public DocumentationWriter(string outputPath, string? packageName)
        {
            this.OutputPath = outputPath;
            this.packageName =
                string.IsNullOrWhiteSpace(packageName)
                ? null : packageName;

            // If the output path is not null, make sure the directory exists.
            if (outputPath != null)
            {
                this.OnDiagnostic?.Invoke(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Info,
                    Message = $"Writing documentation output to: {outputPath}...",
                    Range = null,
                    Source = null,
                    Stage = IRewriteStep.Stage.Transformation,
                });
                if (!Directory.Exists(outputPath))
                {
                    this
                        .TryWithExceptionsAsDiagnostics(
                            "creating directory",
                            () => Task.FromResult(Directory.CreateDirectory(outputPath)))
                        .Wait();
                }
            }

            this.packageLink = this.PackageName == null
                ? string.Empty
                : $"\nPackage: [{this.PackageName}](https://nuget.org/packages/{this.PackageName})\n";
        }

        /// <summary>
        ///     Given a documentation comment describing a Q# namespace,
        ///     writes a Markdown file documenting that namespace to
        ///     <see cref="OutputPath" />.
        /// </summary>
        /// <param name="ns">The Q# namespace being documented.</param>
        /// <param name="docComment">
        ///     The API documentation comment describing <paramref name="ns" />.
        /// </param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task WriteOutput(QsNamespace ns, DocComment docComment)
        {
            var name = ns.Name;
            var uid = name;
            var title = $"{name} namespace";
            var header = new Dictionary<string, object>
            {
                // DocFX metadata
                ["uid"] = name,
                ["title"] = title,

                // docs.ms metadata
                ["ms.date"] = DateTime.Today.ToString(),
                ["ms.topic"] = "managed-reference",

                // Q# metadata
                ["qsharp.kind"] = "namespace",
                ["qsharp.name"] = name,
                ["qsharp.summary"] = docComment.Summary,
            };
            var document = $@"
# {title}

{docComment.Summary}

"
                .MaybeWithSection("Description", docComment.Description)
                .WithSectionForEach("Example", docComment.Examples)
                .MaybeWithSection(
                    "See Also",
                    string.Join("\n", docComment.SeeAlso.Select(
                        seeAlso => AsSeeAlsoLink(seeAlso))))
                .WithYamlHeader(header);

            // Open a file to write the new doc to.
            await this.WriteAllTextAsync($"{name}.md", document);
        }

        /// <summary>
        ///     Given a documentation comment describing a Q# user-defined type
        ///     declaration, writes a Markdown file documenting that UDT
        ///     declaration to <see cref="OutputPath" />.
        /// </summary>
        /// <param name="type">The Q# UDT being documented.</param>
        /// <param name="docComment">
        ///     The API documentation comment describing <paramref name="type"/>.
        /// </param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task WriteOutput(QsCustomType type, DocComment docComment)
        {
            var namedItemDeclarations = type.TypeItems.ToDictionaryOfDeclarations();

            // Make a new Markdown document for the type declaration.
            var title = $"{type.FullName.Name} user defined type";
            var header = new Dictionary<string, object>
            {
                // DocFX metadata
                ["uid"] = type.FullName.ToString(),
                ["title"] = title,

                // docs.ms metadata
                ["ms.date"] = DateTime.Today.ToString(),
                ["ms.topic"] = "managed-reference",

                // Q# metadata
                ["qsharp.kind"] = "udt",
                ["qsharp.namespace"] = type.FullName.Namespace,
                ["qsharp.name"] = type.FullName.Name,
                ["qsharp.summary"] = docComment.Summary,
            };
            var document = $@"
# {title}

Namespace: [{type.FullName.Namespace}](xref:{type.FullName.Namespace})
{this.packageLink}

{docComment.Summary}

```{this.LanguageMode}
{type.WithoutDocumentationAndComments().ToSyntax()}
```

"
            .MaybeWithSection(
                "Named Items",
                string.Join("\n", type.TypeItems.TypeDeclarations().Select(
                    item =>
                    {
                        (var itemName, var resolvedType) = item;
                        var documentation =
                            docComment.NamedItems.TryGetValue(itemName, out var comment)
                            ? comment
                            : string.Empty;
                        return $"### {itemName} : {resolvedType.ToMarkdownLink()}\n\n{documentation}";
                    })))
            .MaybeWithSection("Description", docComment.Description)
            .WithSectionForEach("Example", docComment.Examples)
            .MaybeWithSection("Remarks", docComment.Remarks)
            .MaybeWithSection("References", docComment.References)
            .MaybeWithSection(
                "See Also",
                string.Join("\n", docComment.SeeAlso.Select(
                    seeAlso => AsSeeAlsoLink(seeAlso, type.FullName.Namespace))))
            .WithYamlHeader(header);

            // Open a file to write the new doc to.
            await this.WriteAllTextAsync(
                $"{type.FullName.Namespace}.{type.FullName.Name}.md",
                document);
        }

        /// <summary>
        ///     Given a documentation comment describing a Q# function or operation
        ///     declaration, writes a Markdown file documenting that callable
        ///     declaration to <see cref="OutputPath" />.
        /// </summary>
        /// <param name="callable">The Q# callable being documented.</param>
        /// <param name="docComment">
        ///     The API documentation comment describing <paramref name="callable"/>.
        /// </param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task WriteOutput(QsCallable callable, DocComment docComment)
        {
            // Make a new Markdown document for the type declaration.
            var kind = callable.Kind.Tag switch
            {
                QsCallableKind.Tags.Function => "function",
                QsCallableKind.Tags.Operation => "operation",
                QsCallableKind.Tags.TypeConstructor => "type constructor",
                _ => "<unknown>",
            };
            var title = $@"{callable.FullName.Name} {kind}";
            var header = new Dictionary<string, object>
            {
                // DocFX metadata
                ["uid"] = callable.FullName.ToString(),
                ["title"] = title,

                // docs.ms metadata
                ["ms.date"] = DateTime.Today.ToString(),
                ["ms.topic"] = "managed-reference",

                // Q# metadata
                ["qsharp.kind"] = kind,
                ["qsharp.namespace"] = callable.FullName.Namespace,
                ["qsharp.name"] = callable.FullName.Name,
                ["qsharp.summary"] = docComment.Summary,
            };
            var keyword = callable.Kind.Tag switch
            {
                QsCallableKind.Tags.Function => "function ",
                QsCallableKind.Tags.Operation => "operation ",
                QsCallableKind.Tags.TypeConstructor => "newtype ",
                _ => ""
            };
            var document = $@"
# {title}

Namespace: [{callable.FullName.Namespace}](xref:{callable.FullName.Namespace})
{this.packageLink}

{docComment.Summary}

```{this.LanguageMode}
{keyword}{callable.ToSyntax()}
```
"
            .MaybeWithSection("Description", docComment.Description)
            .MaybeWithSection(
                "Input",
                string.Join("\n", callable.ArgumentTuple.InputDeclarations().Select(
                    (item) =>
                    {
                        (var inputName, var resolvedType) = item;
                        var documentation = docComment.Input.TryGetValue(inputName, out var inputComment)
                            ? inputComment
                            : string.Empty;
                        return $"### {inputName} : {resolvedType.ToMarkdownLink()}\n\n{documentation}\n\n";
                    })))
            .WithSection($"Output : {callable.Signature.ReturnType.ToMarkdownLink()}", docComment.Output)
            .MaybeWithSection(
                "Type Parameters",
                string.Join("\n", callable.Signature.TypeParameters.Select(
                    typeParam =>
                        typeParam is QsLocalSymbol.ValidName name
                        ? $@"### '{name.Item}{"\n\n"}{(
                            docComment.TypeParameters.TryGetValue($"'{name.Item}", out var comment)
                            ? comment
                            : string.Empty)}"
                        : string.Empty)))
            .WithSectionForEach("Example", docComment.Examples)
            .MaybeWithSection("Remarks", docComment.Remarks)
            .MaybeWithSection("References", docComment.References)
            .MaybeWithSection(
                "See Also",
                string.Join("\n", docComment.SeeAlso.Select(
                    seeAlso => AsSeeAlsoLink(seeAlso, callable.FullName.Namespace))))
            .WithYamlHeader(header);

            // Open a file to write the new doc to.
            await this.WriteAllTextAsync(
                $"{callable.FullName.Namespace}.{callable.FullName.Name}.md",
                document);
        }
    }
}
