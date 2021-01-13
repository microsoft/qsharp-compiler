// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Quantum.QsCompiler.Documentation
{
    /// <summary>
    /// Class to represent the documentation for a namespace and its constituent items.
    /// </summary>
    internal class DocNamespace
    {
        private readonly string name;
        private readonly string uid;
        private readonly string summary;
        private readonly List<DocItem> items = new List<DocItem>();

        public string Name => this.name;

        public bool IsNotEmpty => this.items.Count > 0;

        /// <summary>
        /// Constructs an instance from a compiled namespace and list of source files.
        /// </summary>
        /// <param name="ns">The namespace to be represented</param>
        /// <param name="sourceFiles">If specified, only the items in the specified source files are included.</param>
        internal DocNamespace(QsNamespace ns, IEnumerable<string>? sourceFiles = null)
        {
            var sourceFileSet = sourceFiles == null ? null : new HashSet<string>(sourceFiles);
            bool IsVisible(string source, AccessModifier access, string name)
            {
                var includeInDocs = sourceFileSet == null || sourceFileSet.Contains(source);
                return includeInDocs && access.IsDefaultAccess && !(name.StartsWith("_") || name.EndsWith("_")
                        || name.EndsWith("Impl", StringComparison.InvariantCultureIgnoreCase)
                        || name.EndsWith("ImplA", StringComparison.InvariantCultureIgnoreCase)
                        || name.EndsWith("ImplC", StringComparison.InvariantCultureIgnoreCase)
                        || name.EndsWith("ImplCA", StringComparison.InvariantCultureIgnoreCase));
            }

            this.name = ns.Name;
            this.uid = this.name.ToLowerInvariant();

            this.summary = "";
            foreach (var commentGroup in ns.Documentation)
            {
                foreach (var comment in commentGroup)
                {
                    if (!comment.IsDefaultOrEmpty)
                    {
                        this.summary = string.Join(Environment.NewLine, comment);
                    }
                }
            }

            foreach (var item in ns.Elements)
            {
                if (item is QsNamespaceElement.QsCallable c)
                {
                    var callable = c.Item;
                    if (IsVisible(callable.Source.AssemblyOrCodeFile, callable.Modifiers.Access, callable.FullName.Name) &&
                        (callable.Kind != QsCallableKind.TypeConstructor))
                    {
                        this.items.Add(new DocCallable(this.name, callable));
                    }
                }
                else if (item is QsNamespaceElement.QsCustomType u)
                {
                    var udt = u.Item;
                    if (IsVisible(udt.Source.AssemblyOrCodeFile, udt.Modifiers.Access, udt.FullName.Name))
                    {
                        this.items.Add(new DocUdt(this.name, udt));
                    }
                }
                // ignore anything else
            }

            // Sometimes we need the items in alphabetical order by UID, so let's do that here
            this.items.Sort((x, y) => x.Uid.CompareTo(y.Uid));
        }

        /// <summary>
        /// Merges a namespace into a TOC node.
        /// If there is an existing node with the same uid, then it is updated to reflect the data
        /// in this namespace.
        /// Otherwise, a new node is added to the sequence.
        /// </summary>
        /// <param name="toc">The TOC node to merge into.</param>
        internal void MergeNamespaceIntoToc(YamlSequenceNode toc)
        {
            bool MatchByUid(YamlNode n, string key)
            {
                var uid = new YamlScalarNode(this.uid);
                if (n is YamlMappingNode map)
                {
                    if (map.Children != null && map.Children.TryGetValue(new YamlScalarNode(Utils.UidKey), out YamlNode node))
                    {
                        if (node is YamlScalarNode valueNode)
                        {
                            return valueNode.Equals(uid);
                        }
                    }
                }
                return false;
            }

            string? TryGetUid(YamlNode node)
            {
                if (node is YamlMappingNode mappingNode)
                {
                    mappingNode.Children.TryGetValue(Utils.UidKey, out var uidNode);
                    return (uidNode as YamlScalarNode)?.Value;
                }
                else
                {
                    return null;
                }
            }

            string? TryGetName(YamlNode node)
            {
                if (node is YamlMappingNode mappingNode)
                {
                    mappingNode.Children.TryGetValue(Utils.NameKey, out var nameNode);
                    return (nameNode as YamlScalarNode)?.Value;
                }
                else
                {
                    return null;
                }
            }

            int CompareUids(YamlNode node1, YamlNode node2) =>
                string.Compare(
                    TryGetUid(node1),
                    TryGetUid(node2));

            var namespaceNode = toc.Children.SingleOrDefault(c => MatchByUid(c, this.uid)) as YamlMappingNode;
            YamlSequenceNode? itemListNode = null;
            if (namespaceNode == null)
            {
                namespaceNode = new YamlMappingNode();
                namespaceNode.AddStringMapping(Utils.UidKey, this.uid);
                namespaceNode.AddStringMapping(Utils.NameKey, this.name);
                toc.Add(namespaceNode);
                toc.Children.Sort((node1, node2) => CompareUids(node1, node2));
            }
            else
            {
                YamlNode itemsNode;
                if (namespaceNode.Children.TryGetValue(new YamlScalarNode(Utils.ItemsKey), out itemsNode))
                {
                    itemListNode = itemsNode as YamlSequenceNode;
                }
            }
            if (itemListNode == null)
            {
                itemListNode = new YamlSequenceNode();
                namespaceNode.Add(Utils.ItemsKey, itemListNode);
            }

            var itemsByUid = this.items
                .GroupBy(item => item.Uid)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(item => item.Name)
                        .Single());

            // Update itemsByUid with any items that may already exist.
            foreach (var existingChild in itemListNode.Children)
            {
                var uid = TryGetUid(existingChild);
                if (uid != null)
                {
                    itemsByUid[uid] = TryGetName(existingChild) ?? "";
                }
            }

            itemListNode.Children.Clear();
            foreach (var (uid, name) in itemsByUid.OrderBy(item => item.Key))
            {
                itemListNode.Add(Utils.BuildMappingNode(
                    Utils.NameKey, name, Utils.UidKey, uid));
            }
        }

        /// <summary>
        /// Writes the YAML files for all of the items in this namespace to the
        /// specified directory.
        /// </summary>
        /// <param name="directoryPath">The directory to write the files to</param>
        [Obsolete("Writing YAML documentation is no longer supported.")]
        internal void WriteItemsToDirectory(string directoryPath, List<Exception> errors)
        {
            void WriteItem(DocItem i)
            {
                var itemFileName = Path.Combine(directoryPath, i.Uid + Utils.YamlExtension);
                using (var text = new StreamWriter(File.Open(itemFileName, FileMode.Create)))
                {
                    i.WriteToFile(text);
                }
            }
            foreach (var item in this.items)
            {
                Utils.DoTrackingExceptions(() => WriteItem(item), errors);
            }
        }

        /// <summary>
        /// Writes the YAML file for this namespace to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="rootNode">
        /// The mapping node representing the preexisting contents of this namespace's YAML file.
        /// </param>
        [Obsolete("Writing YAML documentation is no longer supported.")]
        internal void WriteToStream(Stream stream, YamlMappingNode? rootNode = null)
        {
            string ToSequenceKey(string itemTypeName)
            {
                return itemTypeName + "s";
            }

            string? GetItemUid(YamlNode item)
            {
                if (item is YamlMappingNode map)
                {
                    YamlNode name;
                    if (map.Children.TryGetValue(new YamlScalarNode(Utils.UidKey), out name))
                    {
                        if (name is YamlScalarNode nameValue)
                        {
                            return nameValue.Value;
                        }
                    }
                }
                return "";
            }

            rootNode ??= new YamlMappingNode();
            rootNode.AddStringMapping(Utils.UidKey, this.uid);
            rootNode.AddStringMapping(Utils.NameKey, this.name);
            if (!string.IsNullOrEmpty(this.summary))
            {
                rootNode.AddStringMapping(Utils.SummaryKey, this.summary);
            }

            var itemTypeNodes = new Dictionary<string, SortedDictionary<string, YamlNode>>();

            // Collect existing items
            foreach (var itemType in new[] { Utils.FunctionKind, Utils.OperationKind, Utils.UdtKind })
            {
                var seqKey = ToSequenceKey(itemType);
                var thisList = new SortedDictionary<string, YamlNode>();
                itemTypeNodes[seqKey] = thisList;

                if (rootNode.Children.TryGetValue(seqKey, out var typeNode))
                {
                    var typeRoot = typeNode as YamlSequenceNode;
                    // We can safely assert here, since we know that the type
                    // node is always a mapping node. That said, it's better to
                    // be explicit and throw an exception instead if the cast
                    // fails.
                    if (typeRoot == null)
                    {
                        throw new Exception($"Expected {itemType} to be a mapping node, was actually a {typeNode.GetType()}.");
                    }
                    foreach (var item in typeRoot)
                    {
                        var uid = GetItemUid(item);
                        if (uid != null)
                        {
                            thisList.Add(uid, item);
                        }
                    }
                }
            }

            // Now add our new items, overwriting if they already exist
            foreach (var item in this.items)
            {
                var typeKey = ToSequenceKey(item.ItemType);
                if (itemTypeNodes.TryGetValue(typeKey, out var typeList))
                {
                    if (typeList.ContainsKey(item.Uid))
                    {
                        // TODO: Emit a warning log / diagnostic. What is the accepted way to do that here?
                        // $"Documentation for {item.Uid} already exists in this folder and will be overwritten.
                        // It's recommended to compile docs to a new folder to avoid deleted files lingering."
                    }

                    typeList[item.Uid] = item.ToNamespaceItem();
                }
            }

            // Now set the merged list back onto the root node
            foreach (var kvp in itemTypeNodes)
            {
                if (kvp.Value.Values.Count > 0)
                {
                    var sortedItems = new YamlSequenceNode(kvp.Value.Values);
                    rootNode.Children[new YamlScalarNode(kvp.Key)] = sortedItems;
                }
            }

            var doc = new YamlDocument(rootNode);
            var yamlStream = new YamlStream(doc);

            using var output = new StreamWriter(stream);
            output.WriteLine("### " + Utils.QsNamespaceYamlMime + Utils.AutogenerationWarning);
            yamlStream.Save(output, false);
        }

        /// <summary>
        /// Writes the YAML file for this namespace.
        /// </summary>
        /// <param name="directoryPath">The directory to write the file to</param>
        [Obsolete("Writing YAML documentation is no longer supported.")]
        internal void WriteToFile(string directoryPath)
        {
            var rootNode = Utils.ReadYamlFile(directoryPath, this.name) as YamlMappingNode;
            var tocFileName = Path.Combine(directoryPath, this.name + Utils.YamlExtension);
            this.WriteToStream(File.Open(tocFileName, FileMode.Create), rootNode);
        }
    }
}
