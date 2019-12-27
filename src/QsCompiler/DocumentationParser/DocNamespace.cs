// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
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
        public string Name => name;
        public bool IsNotEmpty => items.Count > 0;

        /// <summary>
        /// Constructs an instance from a compiled namespace and list of source files.
        /// </summary>
        /// <param name="ns">The namespace to be represented</param>
        /// <param name="sourceFiles">If specified, only the items in the specified source files are included.</param>
        internal DocNamespace(QsNamespace ns, IEnumerable<string>? sourceFiles = null)
        {
            var sourceFileSet = sourceFiles == null ? null : new HashSet<string>(sourceFiles);
            bool IsVisible(NonNullable<string> qualifiedName, NonNullable<string> source)
            {
                var name = qualifiedName.Value;
                var includeInDocs = sourceFileSet == null || sourceFileSet.Contains(source.Value);
                return includeInDocs && !(name.StartsWith("_") || name.EndsWith("_") 
                        || name.EndsWith("Impl", StringComparison.InvariantCultureIgnoreCase)
                        || name.EndsWith("ImplA", StringComparison.InvariantCultureIgnoreCase)
                        || name.EndsWith("ImplC", StringComparison.InvariantCultureIgnoreCase)
                        || name.EndsWith("ImplCA", StringComparison.InvariantCultureIgnoreCase));
            }

            this.name = ns.Name.Value;
            uid = this.name.ToLowerInvariant();

            this.summary = "";
            foreach (var commentGroup in ns.Documentation)
            {
                foreach (var comment in commentGroup)
                {
                    if (!comment.IsDefaultOrEmpty)
                    {
                        this.summary = String.Join(Environment.NewLine, comment);
                    }
                }
            }

            foreach (var item in ns.Elements)
            {
                if (item is QsNamespaceElement.QsCallable c)
                {
                    var callable = c.Item;
                    if (IsVisible(callable.FullName.Name, callable.SourceFile) &&
                        (callable.Kind != QsCallableKind.TypeConstructor))
                    {
                        items.Add(new DocCallable(name, callable));
                    }
                }
                else if (item is QsNamespaceElement.QsCustomType u)
                {
                    var udt = u.Item;
                    if (IsVisible(udt.FullName.Name, udt.SourceFile))
                    {
                        items.Add(new DocUdt(name, udt));
                    }
                }
                // ignore anything else
            }

            // Sometimes we need the items in alphabetical order by UID, so let's do that here
            items.Sort((x, y) => x.Uid.CompareTo(y.Uid));
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
                else { return null; }
            }

            int CompareUids(YamlNode node1, YamlNode node2) =>
                String.Compare(
                    TryGetUid(node1),
                    TryGetUid(node2)
                );

            var namespaceNode = toc.Children?.SingleOrDefault(c => MatchByUid(c, this.uid)) as YamlMappingNode;
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

            foreach (var item in items)
            {
                if (!itemListNode.Children.Any(c => MatchByUid(c, item.Uid)))
                {
                    itemListNode.Add(Utils.BuildMappingNode(Utils.NameKey, item.Name, Utils.UidKey, item.Uid));
                }
            }

            itemListNode.Children.Sort((node1, node2) => CompareUids(node1, node2));
        }

        /// <summary>
        /// Writes the YAML files for all of the items in this namespace to the
        /// specified directory.
        /// </summary>
        /// <param name="directoryPath">The directory to write the files to</param>
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
            foreach (var item in items)
            {
                Utils.DoTrackingExceptions(() => WriteItem(item), errors);
            }
        }

        /// <summary>
        /// Writes the YAML file for this namespace.
        /// </summary>
        /// <param name="directoryPath">The directory to write the file to</param>
        internal void WriteToFile(string directoryPath)
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

            var rootNode = Utils.ReadYamlFile(directoryPath, name) as YamlMappingNode ?? new YamlMappingNode();

            rootNode.AddStringMapping(Utils.UidKey, uid);
            rootNode.AddStringMapping(Utils.NameKey, name);
            if (!String.IsNullOrEmpty(summary))
            {
                rootNode.AddStringMapping(Utils.SummaryKey, summary);
            }

            var itemTypeNodes = new Dictionary<string, SortedDictionary<string, YamlNode>>();

            // Collect existing items
            foreach (var itemType in new []{Utils.FunctionKind, Utils.OperationKind, Utils.UdtKind})
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
            foreach (var item in items)
            {
                var typeKey = ToSequenceKey(item.ItemType);
                SortedDictionary<string, YamlNode> typeList;
                if (itemTypeNodes.TryGetValue(typeKey, out typeList))
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
            var stream = new YamlStream(doc);

            var tocFileName = Path.Combine(directoryPath, name + Utils.YamlExtension);
            using (var text = new StreamWriter(File.Open(tocFileName, FileMode.Create)))
            {
                text.WriteLine("### " + Utils.QsNamespaceYamlMime);
                stream.Save(text, false);
            }
        }
    }
}
