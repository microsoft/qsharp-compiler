// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.Documentation
{

    public class DocumentationWriter
    {
        private readonly string outputPath;
        public string OutputPath => outputPath;

        private readonly string? packageName;
        public string? PackageName => packageName;
        private readonly string PackageLink;

        public DocumentationWriter(string outputPath, string? packageName)
        {
            this.outputPath = outputPath;
            this.packageName = packageName;

            // If the output path is not null, make sure the directory exists.
            if (outputPath != null && !Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            PackageLink = PackageName == null
                ? ""
                : $"Package: [{PackageName}](https://nuget.org/packages/{PackageName})\n";
        }

        public async Task WriteOutput(QsNamespace ns, DocComment docComment)
        {
            var name = ns.Name.Value;
            var uid = name;
            var title = $"{name} namespace";
            var header = new Dictionary<string, object>
            {
                // DocFX metadata
                ["uid"] = name,
                ["title"] = title,

                // docs.ms metadata
                ["ms.date"] = DateTime.Today.ToString(),
                ["ms.topic"] = "article",

                // Q# metadata
                ["qsharp.kind"] = "udt",
                ["qsharp.name"] = name,
                ["qsharp.summary"] = docComment.Summary
            };
            var document = $@"
# {title}

{docComment.Summary}

"
                .MaybeWithSection("Description", docComment.Description)
                .WithYamlHeader(header);

            // Open a file to write the new doc to.
            await File.WriteAllTextAsync(
                Path.Join(outputPath, $"{name.ToLowerInvariant()}.md"),
                document
            );
        }
   
        public async Task WriteOutput(QsCustomType type, DocComment docComment)
        {
            var namedItemDeclarations = type.TypeItems.ToDictionaryOfDeclarations();
            // Make a new Markdown document for the type declaration.
            var title = $"{type.FullName.Name.Value} user defined type";
            var header = new Dictionary<string, object>
            {
                // DocFX metadata
                ["uid"] = type.FullName.ToString(),
                ["title"] = title,

                // docs.ms metadata
                ["ms.date"] = DateTime.Today.ToString(),
                ["ms.topic"] = "article",

                // Q# metadata
                ["qsharp.kind"] = "udt",
                ["qsharp.namespace"] = type.FullName.Namespace.Value,
                ["qsharp.name"] = type.FullName.Name.Value,
                ["qsharp.summary"] = docComment.Summary
            };
            var document = $@"
Namespace: [{type.FullName.Namespace.Value}](xref:{type.FullName.Namespace.Value})
{PackageLink}

# {title}

{docComment.Summary}

```Q#
{type.ToSyntax()}
```

"
            .MaybeWithSection(
                "Named Items",
                string.Join("\n", docComment.NamedItems.Select(
                    item =>
                    {
                        var hasName = namedItemDeclarations.TryGetValue(item.Key, out var itemType);
                        return $"### {item.Key}{(hasName ? $" : {itemType.ToMarkdownLink()}" : "")}\n\n{item.Value}\n\n";;
                    }
                ))
            )
            .MaybeWithSection("Description", docComment.Description)
            .MaybeWithSection("Remarks", docComment.Remarks)
            .MaybeWithSection("References", docComment.References)
            .MaybeWithSection(
                "See Also",
                string.Join("\n", docComment.SeeAlso.Select(
                    seeAlso => $"- {seeAlso}"
                ))
            )
            .WithYamlHeader(header);

            // Open a file to write the new doc to.
            await File.WriteAllTextAsync(
                Path.Join(outputPath, $"{type.FullName.Namespace.Value.ToLowerInvariant()}.{type.FullName.Name.Value.ToLowerInvariant()}.md"),
                document
            );
        }

        

        public async Task WriteOutput(QsCallable callable, DocComment docComment)
        {
            var inputDeclarations = callable.ArgumentTuple.ToDictionaryOfDeclarations();

            // Make a new Markdown document for the type declaration.
            var kind = callable.Kind.Tag switch
            {
                QsCallableKind.Tags.Function => "function",
                QsCallableKind.Tags.Operation => "operation",
                QsCallableKind.Tags.TypeConstructor => "type constructor"
            };
            var title = $@"{callable.FullName.Name.Value} {kind}";
            var header = new Dictionary<string, object>
            {
                // DocFX metadata
                ["uid"] = callable.FullName.ToString(),
                ["title"] = title,

                // docs.ms metadata
                ["ms.date"] = DateTime.Today.ToString(),
                ["ms.topic"] = "article",

                // Q# metadata
                ["qsharp.kind"] = kind,
                ["qsharp.namespace"] = callable.FullName.Namespace.Value,
                ["qsharp.name"] = callable.FullName.Name.Value,
                ["qsharp.summary"] = docComment.Summary
            };
            var document = $@"
Namespace: [{callable.FullName.Namespace.Value}](xref:{callable.FullName.Namespace.Value})
{PackageLink}

# {title}

{docComment.Summary}

```Q#
{callable.ToSyntax()}
```
"
            .MaybeWithSection("Description", docComment.Description)
            .MaybeWithSection(
                "Input",
                String.Join("\n", docComment.Input.Select(
                    item =>
                    {
                        var hasInput = inputDeclarations.TryGetValue(item.Key, out var inputType);
                        return $"### {item.Key}{(hasInput ? $" : {inputType.ToMarkdownLink()}" : "")}\n\n{item.Value}\n\n";
                    }
                ))
            )
            .MaybeWithSection("Output", docComment.Output)
            .MaybeWithSection("Type Parameters",
                String.Join("\n", docComment.TypeParameters.Select(
                    item => $"### {item.Key}\n\n{item.Value}\n\n"
                ))
            )
            .MaybeWithSection("Remarks", docComment.Remarks)
            .MaybeWithSection("References", docComment.References)
            .MaybeWithSection(
                "See Also",
                String.Join("\n", docComment.SeeAlso.Select(
                    seeAlso => $"- [{seeAlso}](xref:{seeAlso})"
                ))
            )
            .WithYamlHeader(header);

            // Open a file to write the new doc to.
            await File.WriteAllTextAsync(
                Path.Join(outputPath, $"{callable.FullName.Namespace.Value.ToLowerInvariant()}.{callable.FullName.Name.Value.ToLowerInvariant()}.md"),
                document
            );
        }

    }

}
