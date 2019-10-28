// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;


namespace Microsoft.Quantum.QsCompiler.Documentation
{
    /// <summary>
    /// Base class for documented items (callables and user-defined types)
    /// </summary>
    internal abstract class DocItem
    {
        protected readonly string namespaceName;
        protected readonly string name;
        protected readonly string uid;
        protected readonly string itemType;
        protected readonly DocComment comments;
        protected readonly bool deprecated;
        protected readonly string replacement;

        /// <summary>
        /// The item's kind, as a string (Utilities.OperationKind, .FunctionKind, or .UdtKind)
        /// </summary>
        internal string ItemType => this.itemType;
        /// <summary>
        /// The unique internal ID of the item
        /// </summary>
        internal string Uid => this.uid;
        /// <summary>
        /// The name of the item
        /// </summary>
        internal string Name => this.name;

        /// <summary>
        /// Constructs a documented item.
        /// </summary>
        /// <param name="nsName">The name of the namespace that defines this item</param>
        /// <param name="itemName">The name of the item itself</param>
        /// <param name="kind">The item's kind: operation, function, or UDT</param>
        /// <param name="documentation">The source documentation for the item</param>
        internal DocItem(string nsName, string itemName, string kind, ImmutableArray<string> documentation,
            IEnumerable<QsDeclarationAttribute> attributes)
        {
            namespaceName = nsName;
            name = itemName;
            uid = (namespaceName + "." + name).ToLowerInvariant();
            itemType = kind;
            var res = SymbolResolution.TryFindRedirect(attributes);
            deprecated = res.IsValue;
            replacement = res.ValueOr("");
            comments = new DocComment(documentation, name, deprecated, replacement);
        }

        /// <summary>
        /// Returns a YAML node describing this item suitable for inclusion in a namespace description.
        /// </summary>
        /// <returns>A new YAML mapping node that describes this item</returns>
        internal YamlMappingNode ToNamespaceItem() => Utils.BuildMappingNode(Utils.UidKey, uid, Utils.SummaryKey, comments.Summary);

        /// <summary>
        /// Returns a YAML node describing this item suitable for inclusion in a table of contents.
        /// </summary>
        /// <returns>A new YAML mapping node that describes this item</returns>
        internal YamlMappingNode ToTocItem() => Utils.BuildMappingNode(Utils.UidKey, uid, Utils.NameKey, name);

        /// <summary>
        /// Writes a full YAML representation of this item to the given text stream.
        /// </summary>
        /// <param name="text">The text stream to output to</param>
        internal abstract void WriteToFile(TextWriter text);
    }
}
