// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker;

#nullable enable

namespace Microsoft.Quantum.QsCompiler.DependencyAnalysis
{
    using Range = DataTypes.Range;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    /// <summary>
    /// Edge type for Concrete Call Graphs.
    /// </summary>
    public sealed class ConcreteCallGraphEdge : CallGraphEdgeBase
    {
        /// <summary>
        /// Constructor for ConcreteCallGraphEdge objects.
        /// Throws an ArgumentNullException if any of the arguments are null.
        /// </summary>
        internal ConcreteCallGraphEdge(QsQualifiedName fromCallableName, QsQualifiedName toCallableName, Range referenceRange)
            : base(fromCallableName, toCallableName, referenceRange)
        {
        }
    }

    /// <summary>
    /// Node type that represents concrete instances of Q# callables.
    /// </summary>
    public sealed class ConcreteCallGraphNode : CallGraphNodeBase, IEquatable<ConcreteCallGraphNode>
    {
        /// <summary>
        /// The specific functor specialization represented.
        /// </summary>
        public QsSpecializationKind Kind { get; }

        /// <summary>
        /// The concrete type mappings for the type parameters for the callable.
        /// </summary>
        public TypeParameterResolutions ParamResolutions { get; }

        /// <summary>
        /// Constructor for ConcreteCallGraphNode objects.
        /// Strips position info from the given type parameter resolutions.
        /// Throws an ArgumentNullException if any of the arguments are null.
        /// </summary>
        public ConcreteCallGraphNode(QsQualifiedName callableName, QsSpecializationKind kind, TypeParameterResolutions paramResolutions) : base(callableName)
        {
            if (paramResolutions is null)
            {
                throw new ArgumentException(nameof(paramResolutions));
            }

            this.Kind = kind;

            // Remove position info from type parameter resolutions
            this.ParamResolutions = paramResolutions.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => StripPositionInfo.Apply(kvp.Value));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) =>
            obj is ConcreteCallGraphNode && this.Equals((ConcreteCallGraphNode)obj);

        /// <summary>
        /// Determines if the object is the same as the given node, ignoring the
        /// ordering of key-value pairs in the type parameter dictionaries.
        /// </summary>
        public bool Equals(ConcreteCallGraphNode other) =>
            base.Equals(other)
            && this.Kind.Equals(other.Kind)
            && (this.ParamResolutions == other.ParamResolutions
                || this.ParamResolutions
                       .OrderBy(kvp => kvp.Key)
                       .SequenceEqual(other.ParamResolutions.OrderBy(kvp => kvp.Key)));

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = default;
            hash.Add(this.CallableName);
            hash.Add(this.Kind);
            foreach (var kvp in this.ParamResolutions)
            {
                hash.Add(kvp);
            }
            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// A kind of call graph whose nodes represent concrete instances of Q# callables.
    /// </summary>
    public sealed class ConcreteCallGraph
    {
        private CallGraphBuilder<ConcreteCallGraphNode, ConcreteCallGraphEdge> graphBuilder = new CallGraphBuilder<ConcreteCallGraphNode, ConcreteCallGraphEdge>();

        // Properties

        /// <summary>
        /// A hash set of the nodes in the call graph.
        /// </summary>
        public ImmutableHashSet<ConcreteCallGraphNode> Nodes => this.graphBuilder.Nodes;

        /// <summary>
        /// Constructs a call graph with concretizations of callables that is trimmed to only
        /// include callables that entry points are dependent on.
        /// Throws ArgumentNullException if argument is null.
        /// </summary>
        public ConcreteCallGraph(QsCompilation compilation)
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            BuildCallGraph.PopulateConcreteGraph(this.graphBuilder, compilation);
        }

        /// <summary>
        /// Returns the children nodes of a given node. Each key in the returned lookup is a child
        /// node of the given node. Each value in the lookup is an edge connecting the given node to
        /// the child node represented by the associated key.
        /// Returns an empty ILookup if the node was found with no dependencies or was not found in
        /// the graph.
        /// Throws ArgumentNullException if argument is null.
        /// </summary>
        public ILookup<ConcreteCallGraphNode, ConcreteCallGraphEdge> GetDirectDependencies(ConcreteCallGraphNode node) => this.graphBuilder.GetDirectDependencies(node);
    }
}
