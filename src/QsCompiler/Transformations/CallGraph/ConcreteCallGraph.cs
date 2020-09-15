// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker;

#nullable enable

namespace Microsoft.Quantum.QsCompiler.DependencyAnalysis
{
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    /// <summary>
    /// Edge type for Concrete Call Graphs.
    /// </summary>
    public class ConcreteCallGraphEdge : CallGraphEdgeBase
    {
        /// <summary>
        /// Constructor for ConcreteCallGraphEdge objects.
        /// Throws an ArgumentNullException if any of the arguments are null.
        /// </summary>
        internal ConcreteCallGraphEdge(QsQualifiedName fromCallableName, QsQualifiedName toCallableName, DataTypes.Range referenceRange)
            : base(fromCallableName, toCallableName, referenceRange)
        {
        }
    }

    /// <summary>
    /// Node type that represents concrete instances of Q# callables.
    /// </summary>
    public class ConcreteCallGraphNode : CallGraphNodeBase, IEquatable<ConcreteCallGraphNode>
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
        public override bool Equals(object obj)
        {
            return obj is ConcreteCallGraphNode && this.Equals((ConcreteCallGraphNode)obj);
        }

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
    public class ConcreteCallGraph : CallGraphBase<ConcreteCallGraphNode, ConcreteCallGraphEdge>
    {
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

            BuildCallGraph.PopulateConcreteGraph(this, compilation);
        }

        /// <summary>
        /// Adds a dependency to the call graph from the fromNode to the toNode, creating an edge in between them.
        /// Throws an ArgumentNullException if any argument is null.
        /// </summary>
        internal void AddDependency(ConcreteCallGraphNode fromNode, ConcreteCallGraphNode toNode, DataTypes.Range referenceRange)
        {
            if (fromNode is null)
            {
                throw new ArgumentNullException(nameof(fromNode));
            }

            if (toNode is null)
            {
                throw new ArgumentNullException(nameof(toNode));
            }

            if (referenceRange is null)
            {
                throw new ArgumentNullException(nameof(referenceRange));
            }

            var edge = new ConcreteCallGraphEdge(fromNode.CallableName, toNode.CallableName, referenceRange);

            this.AddDependency(fromNode, toNode, edge);
        }
    }
}
