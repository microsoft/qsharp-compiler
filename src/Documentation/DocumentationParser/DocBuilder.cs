// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Quantum.QsCompiler.Documentation
{
    /// <summary>
    /// This class contains the functionality for writing all of the documentation
    /// YAML files for an entire compilation.
    /// </summary>
    [Obsolete("The YAML-based documentation builder has been replaced by the Markdown-based DocumentationGenerator.")]
    public class DocBuilder
    {
        private readonly string rootDocPath;
        private readonly List<DocNamespace> namespaces;

        /// <summary>
        /// Constructs a new documentation builder instance.
        /// </summary>
        /// <param name="rootPath">The root directory in which documentation files should be generated</param>
        /// <param name="tree">The compiled namespaces to generate documentation for</param>
        /// <param name="sources">If specified, documentation is only generated for the specified source files.</param>
        public DocBuilder(string rootPath, IEnumerable<QsNamespace> tree, IEnumerable<string>? sources = null)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path must be provided and may not be empty", nameof(rootPath));
            }
            this.rootDocPath = rootPath;
            // Sort namespaces in alphabetical order for the TOC
            this.namespaces = tree.Select(qns => new DocNamespace(qns, sources)).Where(ns => ns.IsNotEmpty).OrderBy(ns => ns.Name).ToList();
        }

        /// <summary>
        /// Generates all of the documentation files for a build.
        /// This includes the top-level table of contents (toc.yml), a table of contents
        /// for each namespace, and a detailed file for each callable and type.
        /// If the root directory does not exist, it will be created and populated.
        /// </summary>
        public void BuildDocs()
        {
            var errors = new List<Exception>();
            Directory.CreateDirectory(this.rootDocPath);

            var rootNode = Utils.ReadYamlFile(this.rootDocPath, Utils.TableOfContents) as YamlSequenceNode ?? new YamlSequenceNode();

            foreach (var ns in this.namespaces)
            {
                try
                {
                    ns.WriteItemsToDirectory(this.rootDocPath, errors);
                }
                catch (AggregateException ex)
                {
                    errors.AddRange(ex.InnerExceptions);
                }
                Utils.DoTrackingExceptions(() => ns.WriteToFile(this.rootDocPath), errors);
                Utils.DoTrackingExceptions(() => ns.MergeNamespaceIntoToc(rootNode), errors);
            }

            Utils.DoTrackingExceptions(() => Utils.WriteYamlFile(rootNode, this.rootDocPath, Utils.TableOfContents), errors);

            if (errors.Count > 0)
            {
                throw new AggregateException(errors);
            }
        }

        /// <summary>
        /// Generates all of the documentation files for a build.
        /// Returns true if no exception occurred during generation, and false otherwise.
        /// </summary>
        /// <param name="rootPath">The root directory in which documentation files should be generated.
        /// If this directory does not exist, it will be created and populated.</param>
        /// <param name="tree">The compiled namespaces to generate documentation for.</param>
        /// <param name="sources">If specified, documentation is only generated for the specified source files.</param>
        /// <param name="onException">Called on caught exceptions before ignoring them.</param>
        public static bool Run(
            string rootPath,
            IEnumerable<QsNamespace> tree,
            IEnumerable<string>? sources = null,
            Action<Exception>? onException = null)
        {
            try
            {
                var db = new DocBuilder(rootPath, tree, sources);
                db.BuildDocs();
                return true;
            }
            catch (Exception ex)
            {
                var exceptions = ex is AggregateException agg ? (IEnumerable<Exception>)agg.InnerExceptions : new[] { ex };
                foreach (var inner in exceptions)
                {
                    onException?.Invoke(inner);
                }
                return false;
            }
        }
    }
}
