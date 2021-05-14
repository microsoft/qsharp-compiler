// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Quantum.QsCompiler.Documentation
{
    /// <summary>
    /// Class to represent the documentation for a user-defined type.
    /// </summary>
    internal class DocUdt : DocItem
    {
        private readonly string syntax;
        private readonly QsCustomType customType;

        /// <summary>
        /// Constructs an instance from the name of the namespace this UDT is defined in
        /// and the compiled UDT itself.
        /// </summary>
        /// <param name="ns">The name of the defining namespace</param>
        /// <param name="udt">The compiled UDT</param>
        internal DocUdt(string ns, QsCustomType udt)
            : base(ns, udt.FullName.Name, Utils.UdtKind, udt.Documentation, udt.Attributes)
        {
            this.syntax = Utils.CustomTypeToSyntax(udt);
            this.customType = udt;
        }

        /// <summary>
        /// Writes the full representation of this UDT to a text stream.
        /// </summary>
        /// <param name="text">The text stream to output to</param>
        [Obsolete("Writing YAML documentation is no longer supported.")]
        internal override void WriteToFile(TextWriter text)
        {
            var rootNode = new YamlMappingNode();
            rootNode.AddStringMapping(Utils.UidKey, this.Uid);
            rootNode.AddStringMapping(Utils.NameKey, this.Name);
            rootNode.AddStringMapping(Utils.TypeKey, this.ItemType);
            rootNode.AddStringMapping(Utils.NamespaceKey, this.NamespaceName.AsObsoleteUid());
            if (!string.IsNullOrEmpty(this.Comments.Documentation))
            {
                rootNode.AddStringMapping(Utils.SummaryKey, this.Comments.Documentation);
            }

            // UDTs get fancy treatment of examples
            if (!string.IsNullOrEmpty(this.Comments.Remarks) || !string.IsNullOrEmpty(this.Comments.Example))
            {
                var rems = this.Comments.Remarks;
                if (!string.IsNullOrEmpty(this.Comments.Example))
                {
                    // \r instead of \n because the YAML.Net serialization doubles \n.
                    // In the file the newline is correct; YAML.Net serializes \r as \n.
                    rems += "\r\r### Examples\r" + this.Comments.Example;
                }

                rootNode.AddStringMapping(Utils.RemarksKey, rems);
            }

            rootNode.Add(Utils.SyntaxKey, this.syntax);
            if (!string.IsNullOrEmpty(this.Comments.References))
            {
                rootNode.AddStringMapping(Utils.ReferencesKey, this.Comments.References);
            }

            if (this.Comments.SeeAlso.Count > 0)
            {
                rootNode.Add(Utils.SeeAlsoKey, Utils.BuildSequenceNode(this.Comments.SeeAlso));
            }

            var doc = new YamlDocument(rootNode);
            var stream = new YamlStream(doc);

            text.WriteLine("### " + Utils.QsYamlMime + Utils.AutogenerationWarning);
            stream.Save(text, false);
        }
    }
}
