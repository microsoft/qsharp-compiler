// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Newtonsoft.Json.Linq;

using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

#nullable enable

namespace Microsoft.Quantum.Documentation
{
    public class ProcessDocComments
    : SyntaxTreeTransformation<ProcessDocComments.TransformationState>
    {
        public class TransformationState
        { }

        private string? OutputPath;

        public ProcessDocComments(
            string? outputPath = null
        )
        : base(new TransformationState())
        {
            OutputPath = outputPath;
            // If the output path is not null, make sure the directory exists.
            if (outputPath != null && !Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // We provide our own custom namespace transformation, and expression kind transformation.
            this.Namespaces = new ProcessDocComments.NamespaceTransformation(this, outputPath);
        }

        public QsCompilation OnCompilation(QsCompilation compilation) =>
            new QsCompilation(
                compilation.Namespaces
                    .Select(this.Namespaces.OnNamespace)
                    .ToImmutableArray(),
                compilation.EntryPoints
            );

        private class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            private string? outputPath;
            internal NamespaceTransformation(ProcessDocComments parent, string? outputPath)
            : base(parent)
            { this.outputPath = outputPath; }
            
            private async Task MaybeWriteOutput(QsCustomType type, DocComment docComment)
            {
                if (outputPath == null) return;

                // Make a new Markdown document for the type declaration.
                var title = $"{type.FullName.Name.Value} user defined type";
                var header = new Dictionary<string, object>
                {
                    ["uid"] = type.FullName.ToString(),
                    ["title"] = title,
                    ["ms.date"] = DateTime.Today.ToString(),
                    ["ms.topic"] = "article"
                };
                var document = $@"
Namespace: [{type.FullName.Namespace.Value}](xref:{type.FullName.Namespace.Value})

# {title}

{docComment.Summary}

```Q#
{type.ToSyntax()}
```

"
                .MaybeWithSection("Description", docComment.Description)
                .MaybeWithSection("Remarks", docComment.Remarks)
                .MaybeWithSection("References", docComment.References)
                .MaybeWithSection(
                    "See Also",
                    String.Join("\n", docComment.SeeAlso.Select(
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

            private async Task MaybeWriteOutput(QsCallable callable, DocComment docComment)
            {
                if (outputPath == null) return;

                // Make a new Markdown document for the type declaration.
                var title = $@"{callable.FullName.Name.Value} {
                    callable.Kind.Tag switch
                    {
                        QsCallableKind.Tags.Function => "function",
                        QsCallableKind.Tags.Operation => "operation",
                        QsCallableKind.Tags.TypeConstructor => "type constructor"
                    }
                }";
                var header = new Dictionary<string, object>
                {
                    ["uid"] = callable.FullName.ToString(),
                    ["title"] = title,
                    ["ms.date"] = DateTime.Today.ToString(),
                    ["ms.topic"] = "article"
                };
                var document = $@"
Namespace: [{callable.FullName.Namespace.Value}](xref:{callable.FullName.Namespace.Value})

# {title}

{docComment.Summary}

```Q#
{callable.ToSyntax()}
```
"
                .MaybeWithSection("Description", docComment.Description)
                .MaybeWithSection("Remarks", docComment.Remarks)
                .MaybeWithSection("References", docComment.References)
                .MaybeWithSection(
                    "See Also",
                    String.Join("\n", docComment.SeeAlso.Select(
                        seeAlso => $"- {seeAlso}"
                    ))
                )
                .MaybeWithSection(
                    "Input",
                    String.Join("\n", docComment.Input.Select(
                        item => $"### {item.Key}\n\n{item.Value}\n\n"
                    ))
                )
                .MaybeWithSection("Output", docComment.Output)
                .MaybeWithSection("Type Parameters",
                    String.Join("\n", docComment.TypeParameters.Select(
                        item => $"### {item.Key}\n\n{item.Value}\n\n"
                    ))
                )
                .WithYamlHeader(header);

                // Open a file to write the new doc to.
                await File.WriteAllTextAsync(
                    Path.Join(outputPath, $"{callable.FullName.Namespace.Value.ToLowerInvariant()}.{callable.FullName.Name.Value.ToLowerInvariant()}.md"),
                    document
                );
            }

            public override QsCustomType OnTypeDeclaration(QsCustomType type)
            {
                type = base.OnTypeDeclaration(type);
                // If the UDT didn't come from a Q# source file, then it
                // came in from a reference, and shouldn't be documented in this
                // project.
                if (!type.SourceFile.Value.EndsWith(".qs")) return type;

                var isDeprecated = type.IsDeprecated(out var replacement);
                var docComment = new DocComment(
                    type.Documentation, type.FullName.Name.Value,
                    deprecated: isDeprecated,
                    replacement: replacement
                );

                MaybeWriteOutput(type, docComment).Wait();

                return type
                    .AttributeBuilder()
                        .MaybeWithSimpleDocumentationAttribute("Summary", docComment.Summary)
                        .MaybeWithSimpleDocumentationAttribute("Description", docComment.Description)
                        .MaybeWithSimpleDocumentationAttribute("Remarks", docComment.Remarks)
                        .MaybeWithSimpleDocumentationAttribute("References", docComment.References)
                        .WithListOfDocumentationAttributes("See Also", docComment.SeeAlso)
                    .Build();
            }

            public override QsCallable OnCallableDeclaration(QsCallable callable)
            {
                callable = base.OnCallableDeclaration(callable);
                // If the callable didn't come from a Q# source file, then it
                // came in from a reference, and shouldn't be documented in this
                // project.
                if (!callable.SourceFile.Value.EndsWith(".qs")) return callable;

                var isDeprecated = callable.IsDeprecated(out var replacement);
                var docComment = new DocComment(
                    callable.Documentation, callable.FullName.Name.Value,
                    deprecated: isDeprecated,
                    replacement: replacement
                );

                MaybeWriteOutput(callable, docComment).Wait();

                return callable
                    .AttributeBuilder()
                        .MaybeWithSimpleDocumentationAttribute("Summary", docComment.Summary)
                        .MaybeWithSimpleDocumentationAttribute("Description", docComment.Description)
                        .MaybeWithSimpleDocumentationAttribute("Remarks", docComment.Remarks)
                        .MaybeWithSimpleDocumentationAttribute("References", docComment.References)
                        .WithListOfDocumentationAttributes("See Also", docComment.SeeAlso)
                        .MaybeWithSimpleDocumentationAttribute("Output", docComment.Output)
                        .WithDocumentationAttributesFromDictionary("Input", docComment.Input)
                        .WithDocumentationAttributesFromDictionary("TypeParameter", docComment.TypeParameters)
                    .Build();
            }
        }
    }
}
