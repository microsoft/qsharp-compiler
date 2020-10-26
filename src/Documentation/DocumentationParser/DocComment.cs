// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Markdig;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Quantum.QsCompiler.Diagnostics;

namespace Microsoft.Quantum.QsCompiler.Documentation
{
    /// <summary>
    /// This class represents the parsed documentation comments for a Q# item (operation, function, type, etc.).
    /// </summary>
    public class DocComment
    {
        /// <summary>
        /// The summary description of the item.
        /// This should be one paragraph of plain text.
        /// </summary>
        public string Summary { get; } = "";

        /// <summary>
        /// The (rest of the) full description of the item.
        /// This should not duplicate the summary, but rather follow it.
        /// </summary>
        public string Description { get; } = "";

        /// <summary>
        /// The short hover information for the item.
        /// This should be one paragraph of plain text.
        /// Currently this is the first paragraph of the summary field.
        /// </summary>
        public string ShortSummary { get; } = "";

        /// <summary>
        /// The full markdown-formatted hover information for the item.
        /// Currently this is the same as the short hover.
        /// </summary>
        public string FullSummary => this.ShortSummary;

        /// <summary>
        /// The full markdown-formatted on-line documentation for the item.
        /// Currently this consists of the summary field followed by the description field.
        /// </summary>
        public string Documentation { get; } = "";

        /// <summary>
        /// The inputs to the item, as a list of symbol/description pairs.
        /// This is only populated for functions and operations.
        /// </summary>
        public Dictionary<string, string> Input { get; }
            = new Dictionary<string, string>();

        /// <summary>
        /// The output from the item.
        /// This is only populated for functions and operations.
        /// </summary>
        public string Output { get; } = "";

        /// <summary>
        /// The type parameters for the item, as a list of symbol/description pairs.
        /// This is only populated for functions and operations.
        /// </summary>
        public Dictionary<string, string> TypeParameters { get; } =
            new Dictionary<string, string>();

        /// <summary>
        /// Descriptions of each named item for the item being documented,
        /// as a dictionary from identifiers for each named item to the
        /// corresponding description.
        /// </summary>
        /// <remarks>
        ///     Only applicable when the item being documented is a UDT.
        /// </remarks>
        public Dictionary<string, string> NamedItems { get; } =
            new Dictionary<string, string>();

        /// <summary>
        /// All examples of using the named item, concatenated as a single Markdown
        /// document.
        /// </summary>
        [Obsolete("Please use Examples instead.")]
        public string Example => string.Join(
            "\n\n",
            this.Examples
        );

        /// <summary>
        /// A list of examples of using the item.
        /// </summary>
        public ImmutableList<string> Examples { get; } = ImmutableList<string>.Empty;

        /// <summary>
        /// Additional commentary about the item.
        /// </summary>
        public string Remarks { get; } = "";

        /// <summary>
        /// A list of links to other documentation related to this item.
        /// </summary>
        public List<string> SeeAlso { get; } =
            new List<string>();

        /// <summary>
        /// Reference material about the item.
        /// </summary>
        public string References { get; } = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocComment"/> class
        /// from the documentation comments
        /// associated with a source code element.
        /// </summary>
        /// <param name="docComments">The doc comments from the source code</param>
        /// <param name="name">The name of the element</param>
        /// <param name="deprecated">Flag indicating whether or not the element had a Deprecated attribute</param>
        /// <param name="replacement">The name of the replacement element for deprecated elements, if given</param>
        public DocComment(IEnumerable<string> docComments, string name, bool deprecated, string? replacement)
        {
            static string GetHeadingText(HeadingBlock heading)
            {
                var sb = new StringBuilder();
                foreach (var item in heading.Inline)
                {
                    sb.Append(item);
                }
                return sb.ToString();
            }

            static string GetParagraphText(LeafBlock leaf)
            {
                var sb = new StringBuilder();
                foreach (var item in leaf.Inline)
                {
                    sb.Append(item);
                }
                return sb.ToString();
            }

            static string ToMarkdown(IEnumerable<Block> blocks)
            {
                var writer = new StringWriter();
                var renderer = new NormalizeRenderer(writer);
                var pipeline = new MarkdownPipelineBuilder().Build();
                pipeline.Setup(renderer);
                foreach (var block in blocks)
                {
                    renderer.Render(block);
                }
                // We convert \n to \r because the YAML serialization will eventually
                // output \n\n for \n, but \r\n for \r.
                return writer.ToString().TrimEnd().Replace('\n', '\r');
            }

            static List<ValueTuple<string, List<Block>>> BreakIntoSections(IEnumerable<Block> blocks, int level)
            {
                var key = "";
                var accum = new List<Block>();
                var result = new List<(string, List<Block>)>();

                foreach (var block in blocks)
                {
                    if (block is HeadingBlock heading)
                    {
                        if (heading.Level == level)
                        {
                            if (accum.Count > 0)
                            {
                                result.Add((key, accum));
                                accum = new List<Block>();
                            }
                            key = GetHeadingText(heading);
                        }
                        else
                        {
                            accum.Add(block);
                        }
                    }
                    else
                    {
                        accum.Add(block);
                    }
                }

                if (accum.Count > 0)
                {
                    result.Add((key, accum));
                }

                return result;
            }

            static void ParseListSection(IEnumerable<Block> blocks, List<string> accum, bool lowerCase)
            {
                foreach (var block in blocks)
                {
                    if (block is ListBlock)
                    {
                        foreach (var sub in block.Descendants())
                        {
                            if (sub is ListItemBlock item)
                            {
                                // Some special treatment for funky doc comments in some of the Canon\
                                if (item.Count == 1 && item.LastChild is LeafBlock leaf && leaf.Inline != null &&
                                    leaf.Inline.FirstChild is LiteralInline literal)
                                {
                                    var itemText = lowerCase ? GetParagraphText(leaf).ToLowerInvariant() : GetParagraphText(leaf);
                                    literal.Content = new Markdig.Helpers.StringSlice(itemText);
                                }
                                accum.Add(ToMarkdown(new Block[] { item }));
                            }
                        }
                    }
                }
            }

            static void ParseMapSection(IEnumerable<Block> blocks, Dictionary<string, string> accum)
            {
                var subsections = BreakIntoSections(blocks, 2);
                foreach ((var key, var subs) in subsections)
                {
                    // TODO: when we add the ability to flag warnings from the doc comment builder,
                    // we should check here for duplicate keys and generate a warning if appropriate.
                    accum[key] = ToMarkdown(subs);
                }
            }

            // First element is not matching, second is matching
            static (List<Block>, List<Block>) PartitionNestedSection(IEnumerable<Block> blocks, int level, string name)
            {
                var inMatch = false;
                var result = (new List<Block>(), new List<Block>());

                foreach (var block in blocks)
                {
                    var skip = false;
                    if (block is HeadingBlock heading && heading.Level == level)
                    {
                        inMatch = GetHeadingText(heading).Equals(name);
                        skip = true;
                    }
                    if (inMatch)
                    {
                        if (!skip)
                        {
                            result.Item2.Add(block);
                        }
                    }
                    else
                    {
                        result.Item1.Add(block);
                    }
                }

                return result;
            }

            // Initialize to safe empty values
            var deprecationSummary = string.IsNullOrWhiteSpace(replacement)
                ? DiagnosticItem.Message(WarningCode.DeprecationWithoutRedirect, new[] { name })
                : DiagnosticItem.Message(
                    WarningCode.DeprecationWithRedirect,
                    new string[] { name, $"<xref:{replacement.AsUid()}>" });
            var deprecationDetails = "";

            var text = string.Join("\n", docComments);

            // Only parse if there are comments to parse
            if (!string.IsNullOrWhiteSpace(text))
            {
                var doc = Markdown.Parse(text);
                var sections = BreakIntoSections(doc, 1);
                List<Block> summarySection = new List<Block>();
                List<Block> descriptionSection = new List<Block>();

                foreach ((var tag, var section) in sections)
                {
                    switch (tag)
                    {
                        case "Summary":
                            this.Summary = ToMarkdown(section);
                            summarySection.AddRange(section);
                            // For now, the short hover information gets the first paragraph of the summary.
                            this.ShortSummary = ToMarkdown(section.GetRange(0, 1));
                            break;
                        case "Deprecated":
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                deprecationSummary = ToMarkdown(section.GetRange(0, 1));
                                if (section.Count > 1)
                                {
                                    deprecationDetails = ToMarkdown(section.GetRange(1, section.Count - 1));
                                }
                            }
                            else
                            {
                                deprecationDetails = ToMarkdown(section);
                            }
                            deprecated = true;
                            break;
                        case "Description":
                            this.Description = ToMarkdown(section);
                            descriptionSection = section;
                            break;
                        case "Input":
                            ParseMapSection(section, this.Input);
                            break;
                        case "Output":
                            this.Output = ToMarkdown(section);
                            break;
                        case "Type Parameters":
                            ParseMapSection(section, this.TypeParameters);
                            break;
                        case "Named Items":
                            ParseMapSection(section, this.NamedItems);
                            break;
                        case "Example":
                            this.Examples = this.Examples.Add(ToMarkdown(section));
                            break;
                        case "Remarks":
                            (var remarks, var examples) = PartitionNestedSection(section, 2, "Example");
                            if (examples.Count > 0 && this.Examples.IsEmpty)
                            {
                                this.Examples = this.Examples.Add(ToMarkdown(examples));
                            }
                            this.Remarks = ToMarkdown(remarks);
                            break;
                        case "See Also":
                            // seeAlso is a list of UIDs, which are all lower case,
                            // so pass true to lowercase all strings found in this section
                            ParseListSection(section, this.SeeAlso, false);
                            break;
                        case "References":
                            this.References = ToMarkdown(section);
                            break;
                        default:
                            // TODO: add diagnostic warning about unknown tag
                            break;
                    }
                }

                this.Documentation = ToMarkdown(summarySection.Concat(descriptionSection));
            }

            if (deprecated)
            {
                var deprecationWarning = Utils.Warning(string.Join(
                    "\n\n",
                    new[] { deprecationSummary, deprecationDetails }.Where(s => !string.IsNullOrWhiteSpace(s))));
                this.ShortSummary = deprecationSummary;
                this.Summary = deprecationWarning + "\n\n" + this.Summary;
                this.Documentation = deprecationWarning + "\n\n" + this.Documentation;
            }
        }

        /// <summary>
        /// Constructs a DocComment instance from the documentation comments
        /// associated with a source code element.
        /// </summary>
        /// <param name="docComments">The doc comments from the source code</param>
        public DocComment(IEnumerable<string> docComments)
            : this(docComments, "", false, "")
        {
        }
    }
}
