// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Quantum.QsCompiler.Documentation
{
    /// <summary>
    /// Base class for documented items (callables and user-defined types)
    /// </summary>
    internal abstract class DocItem
    {
        protected string NamespaceName { get; }

        protected DocComment Comments { get; }

        protected bool Deprecated { get; }

        protected string Replacement { get; }

        /// <summary>
        /// The item's kind, as a string (Utils.OperationKind, .FunctionKind, or .UdtKind)
        /// </summary>
        internal string ItemType { get; }

        /// <summary>
        /// The unique internal ID of the item
        /// </summary>
        internal string Uid { get; }

        /// <summary>
        /// The name of the item
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Constructs a documented item.
        /// </summary>
        /// <param name="nsName">The name of the namespace that defines this item</param>
        /// <param name="itemName">The name of the item itself</param>
        /// <param name="kind">The item's kind: operation, function, or UDT</param>
        /// <param name="documentation">The source documentation for the item</param>
        internal DocItem(
            string nsName,
            string itemName,
            string kind,
            ImmutableArray<string> documentation,
            IEnumerable<QsDeclarationAttribute> attributes)
        {
            this.NamespaceName = nsName;
            this.Name = itemName;
            this.Uid = (this.NamespaceName + "." + this.Name).ToLowerInvariant();
            this.ItemType = kind;
            var res = SymbolResolution.TryFindRedirect(attributes);
            this.Deprecated = res.IsValue;
            this.Replacement = res.ValueOr("");
            this.Comments = new DocComment(documentation, this.Name, this.Deprecated, this.Replacement);
        }

        /// <summary>
        /// Returns a YAML node describing this item suitable for inclusion in a namespace description.
        /// </summary>
        /// <returns>A new YAML mapping node that describes this item</returns>
        internal YamlMappingNode ToNamespaceItem() =>
            Utils.BuildMappingNode(Utils.UidKey, this.Uid, Utils.SummaryKey, this.Comments.ShortSummary);

        /// <summary>
        /// Returns a YAML node describing this item suitable for inclusion in a table of contents.
        /// </summary>
        /// <returns>A new YAML mapping node that describes this item</returns>
        internal YamlMappingNode ToTocItem() => Utils.BuildMappingNode(Utils.UidKey, this.Uid, Utils.NameKey, this.Name);

        /// <summary>
        /// Writes a full YAML representation of this item to the given text stream.
        /// </summary>
        /// <param name="text">The text stream to output to</param>
        [Obsolete("Writing YAML documentation is no longer supported.")]
        internal abstract void WriteToFile(TextWriter text);
    }
}
