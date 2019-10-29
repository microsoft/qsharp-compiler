// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
            : base(ns, udt.FullName.Name.Value, Utils.UdtKind, udt.Documentation, udt.Attributes)
        {
            syntax = Utils.CustomTypeToSyntax(udt);
            customType = udt;
        }

        /// <summary>
        /// Writes the full representation of this UDT to a text stream.
        /// </summary>
        /// <param name="text">The text stream to output to</param>
        internal override void WriteToFile(TextWriter text)
        {
            var rootNode = new YamlMappingNode();
            rootNode.AddStringMapping(Utils.UidKey, uid);
            rootNode.AddStringMapping(Utils.NameKey, name);
            rootNode.AddStringMapping(Utils.TypeKey, itemType);
            rootNode.AddStringMapping(Utils.NamespaceKey, namespaceName);
            if (!string.IsNullOrEmpty(comments.Documentation))
            {
                rootNode.AddStringMapping(Utils.SummaryKey, comments.Documentation);
            }

            // UDTs get fancy treatment of examples
            if (!string.IsNullOrEmpty(comments.Remarks) || !string.IsNullOrEmpty(comments.Example))
            {
                var rems = comments.Remarks;
                if (!string.IsNullOrEmpty(comments.Example))
                {
                    // \r instead of \n because the YAML.Net serialization doubles \n.
                    // In the file the newline is correct; YAML.Net serializes \r as \n.
                    rems += "\r\r### Examples\r" + comments.Example;
                }
                rootNode.AddStringMapping(Utils.RemarksKey, rems);
            }

            rootNode.Add(Utils.SyntaxKey, syntax);
            if (!string.IsNullOrEmpty(comments.References))
            {
                rootNode.AddStringMapping(Utils.ReferencesKey, comments.References);
            }
            if (comments.SeeAlso.Count > 0)
            {
                rootNode.Add(Utils.SeeAlsoKey, Utils.BuildSequenceNode(comments.SeeAlso));
            }

            var doc = new YamlDocument(rootNode);
            var stream = new YamlStream(doc);

            text.WriteLine("### " + Utils.QsYamlMime);
            stream.Save(text, false);
        }
    }
}
