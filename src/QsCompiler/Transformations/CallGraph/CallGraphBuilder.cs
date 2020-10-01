// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.DependencyAnalysis
{
    using Range = DataTypes.Range;

    /// <summary>
    /// Base class for call graph edge types.
    /// </summary>
    public abstract class CallGraphEdgeBase : IEquatable<CallGraphEdgeBase>
    {
        /// <summary>
        /// The range of the reference represented by the edge.
        /// </summary>
        public Range ReferenceRange { get; }

        /// <summary>
        /// Base constructor for call graph edges. Initializes CallGraphEdgeBase properties.
        /// </summary>
        protected CallGraphEdgeBase(Range referenceRange) => this.ReferenceRange = referenceRange;

        /// <inheritdoc/>
        public bool Equals(CallGraphEdgeBase edge) => this.ReferenceRange.Equals(edge.ReferenceRange);
    }

    /// <summary>
    /// Base class for call graph node types.
    /// </summary>
    public abstract class CallGraphNodeBase : IEquatable<CallGraphNodeBase>
    {
        /// <summary>
        /// The name of the represented callable.
        /// </summary>
        public QsQualifiedName CallableName { get; }

        /// <summary>
        /// Base constructor for call graph nodes. Initializes CallableName.
        /// </summary>
        protected CallGraphNodeBase(QsQualifiedName callableName) => this.CallableName = callableName;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is CallGraphNodeBase node && this.Equals(node);

        /// <inheritdoc/>
        public bool Equals(CallGraphNodeBase other) => this.CallableName.Equals(other.CallableName);

        /// <inheritdoc/>
        public override int GetHashCode() => this.CallableName.GetHashCode();
    }

    /// <summary>
    /// Base class for call graph types.
    /// </summary>
    internal class CallGraphBuilder<TNode, TEdge>
        where TNode : CallGraphNodeBase
        where TEdge : CallGraphEdgeBase
    {
        // Static Elements

        /// <summary>
        /// Returns an empty dependency for a node.
        /// </summary>
        private static ILookup<TNode, TEdge> EmptyDependency() =>
            ImmutableArray<KeyValuePair<TNode, TEdge>>.Empty
            .ToLookup(kvp => kvp.Key, kvp => kvp.Value);

        // Member Fields

        /// <summary>
        /// This is a dictionary mapping source nodes to information about target nodes. This information is represented
        /// by a dictionary mapping target node to the edges pointing from the source node to the target node.
        /// </summary>
        private readonly Dictionary<TNode, Dictionary<TNode, ImmutableArray<TEdge>>> dependencies =
            new Dictionary<TNode, Dictionary<TNode, ImmutableArray<TEdge>>>();

        // Properties

        /// <summary>
        /// A hash set of the nodes in the call graph.
        /// </summary>
        public ImmutableHashSet<TNode> Nodes => this.dependencies.Keys.ToImmutableHashSet();

        // Common Member Methods

        /// <summary>
        /// Returns the children nodes of a given node. Each key in the returned lookup is a child
        /// node of the given node. Each value in the lookup is an edge connecting the given node to
        /// the child node represented by the associated key.
        /// Returns an empty ILookup if the node was found with no dependencies or was not found in
        /// the graph.
        /// </summary>
        public ILookup<TNode, TEdge> GetDirectDependencies(TNode node)
        {
            if (this.dependencies.TryGetValue(node, out var dep))
            {
                return dep
                    .SelectMany(kvp => kvp.Value, Tuple.Create)
                    .ToLookup(tup => tup.Item1.Key, tup => tup.Item2);
            }
            else
            {
                return EmptyDependency();
            }
        }

        // Mutator Methods

        /// <summary>
        /// Adds a dependency to the call graph using the two nodes and the edge between them.
        /// The nodes are added to the graph if they are not already there. The edge is always added.
        /// </summary>
        public void AddDependency(TNode fromNode, TNode toNode, TEdge edge)
        {
            if (this.dependencies.TryGetValue(fromNode, out var deps))
            {
                if (!deps.TryGetValue(toNode, out var edges))
                {
                    deps[toNode] = ImmutableArray.Create(edge);
                }
                else
                {
                    deps[toNode] = edges.Add(edge);
                }
            }
            else
            {
                var newDeps = new Dictionary<TNode, ImmutableArray<TEdge>>();
                newDeps[toNode] = ImmutableArray.Create(edge);
                this.dependencies[fromNode] = newDeps;
            }

            // Need to make sure the each dependencies has an entry for each node
            // in the graph, even if node has no dependencies.
            this.AddNode(toNode);
        }

        /// <summary>
        /// Adds the given node to the call graph, if it is not already in the graph.
        /// </summary>
        public void AddNode(TNode node)
        {
            if (!this.dependencies.ContainsKey(node))
            {
                this.dependencies[node] = new Dictionary<TNode, ImmutableArray<TEdge>>();
            }
        }
    }
}
