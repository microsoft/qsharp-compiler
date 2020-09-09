// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker;

#nullable enable

namespace Microsoft.Quantum.QsCompiler.DependencyAnalysis
{
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    /// <summary>
    /// Base class for call graph edge types.
    /// </summary>
    public abstract class BaseCallGraphEdge : IEquatable<BaseCallGraphEdge>
    {
        /// <summary>
        /// Name of the callable where the reference was made.
        /// </summary>
        public QsQualifiedName FromCallableName { get; }

        /// <summary>
        /// Name of the callable being referenced.
        /// </summary>
        public QsQualifiedName ToCallableName { get; }

        /// <summary>
        /// The range of the reference represented by the edge.
        /// </summary>
        public DataTypes.Range ReferenceRange { get; }

        /// <summary>
        /// Base constructor for call graph edges. Initializes BaseCallGraphEdge properties.
        /// Throws an ArgumentNullException if any of the arguments are null.
        /// </summary>
        protected BaseCallGraphEdge(QsQualifiedName fromCallableName, QsQualifiedName toCallableName, DataTypes.Range referenceRange)
        {
            if (fromCallableName is null)
            {
                throw new ArgumentNullException(nameof(fromCallableName));
            }

            if (toCallableName is null)
            {
                throw new ArgumentNullException(nameof(toCallableName));
            }

            if (referenceRange is null)
            {
                throw new ArgumentNullException(nameof(referenceRange));
            }

            this.FromCallableName = fromCallableName;
            this.ToCallableName = toCallableName;
            this.ReferenceRange = referenceRange;
        }

        /// <summary>
        /// Determines if the object is the same as the given edge, ignoring the
        /// ordering of key-value pairs in the type parameter dictionaries.
        /// </summary>
        public bool Equals(BaseCallGraphEdge edge) =>
            this.FromCallableName.Equals(edge.FromCallableName)
            && this.ToCallableName.Equals(edge.ToCallableName)
            && this.ReferenceRange.Equals(edge.ReferenceRange);
    }

    /// <summary>
    /// Edge type for Simple Call Graphs.
    /// </summary>
    public class SimpleCallGraphEdge : BaseCallGraphEdge, IEquatable<SimpleCallGraphEdge>
    {
        /// <summary>
        /// Contains the type parameter resolutions associated with this edge.
        /// </summary>
        public TypeParameterResolutions ParamResolutions { get; }

        /// <summary>
        /// Constructor for SimpleCallGraphEdge objects.
        /// Strips position info from the given type parameter resolutions.
        /// Throws an ArgumentNullException if any of the arguments are null.
        /// </summary>
        internal SimpleCallGraphEdge(TypeParameterResolutions paramResolutions, QsQualifiedName fromCallableName, QsQualifiedName toCallableName, DataTypes.Range referenceRange)
            : base(fromCallableName, toCallableName, referenceRange)
        {
            if (paramResolutions is null)
            {
                throw new ArgumentNullException(nameof(paramResolutions));
            }

            // Remove position info from type parameter resolutions
            this.ParamResolutions = paramResolutions.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => StripPositionInfo.Apply(kvp.Value));
        }

        /// <summary>
        /// Determines if the object is the same as the given edge, ignoring the
        /// ordering of key-value pairs in the type parameter dictionaries.
        /// </summary>
        public bool Equals(SimpleCallGraphEdge edge) =>
            base.Equals(edge)
            && (this.ParamResolutions == edge.ParamResolutions
                || this.ParamResolutions
                       .OrderBy(kvp => kvp.Key)
                       .SequenceEqual(edge.ParamResolutions.OrderBy(kvp => kvp.Key)));
    }

    /// <summary>
    /// Edge type for Concrete Call Graphs.
    /// </summary>
    public class ConcreteCallGraphEdge : BaseCallGraphEdge
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
    /// Base class for call graph node types.
    /// </summary>
    public abstract class BaseCallGraphNode : IEquatable<BaseCallGraphNode>
    {
        /// <summary>
        /// The name of the represented callable.
        /// </summary>
        public QsQualifiedName CallableName { get; }

        /// <summary>
        /// Base constructor for call graph nodes. Initializes CallableName.
        /// Throws an ArgumentNullException if argument is null.
        /// </summary>
        protected BaseCallGraphNode(QsQualifiedName callableName)
        {
            if (callableName is null)
            {
                throw new ArgumentException(nameof(callableName));
            }

            this.CallableName = callableName;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is BaseCallGraphNode && this.Equals((BaseCallGraphNode)obj);
        }

        /// <inheritdoc/>
        public bool Equals(BaseCallGraphNode other) =>
            this.CallableName.Equals(other.CallableName);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = default;
            hash.Add(this.CallableName);
            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// Node type that represents Q# callables.
    /// </summary>
    public class SimpleCallGraphNode : BaseCallGraphNode
    {
        /// <summary>
        /// Constructor for SimpleCallGraphNode objects.
        /// Throws an ArgumentNullException if the argument is null.
        /// </summary>
        public SimpleCallGraphNode(QsQualifiedName callableName)
            : base(callableName)
        {
        }
    }

    /// <summary>
    /// Node type that represents concrete instances of Q# callables.
    /// </summary>
    public class ConcreteCallGraphNode : BaseCallGraphNode, IEquatable<ConcreteCallGraphNode>
    {
        /// <summary>
        /// The concrete type mappings for the type parameters for the callable.
        /// </summary>
        public TypeParameterResolutions ParamResolutions { get; }

        /// <summary>
        /// Constructor for ConcreteCallGraphNode objects.
        /// Strips position info from the given type parameter resolutions.
        /// Throws an ArgumentNullException if any of the arguments are null.
        /// </summary>
        public ConcreteCallGraphNode(QsQualifiedName callableName, TypeParameterResolutions paramResolutions) : base(callableName)
        {
            if (paramResolutions is null)
            {
                throw new ArgumentException(nameof(paramResolutions));
            }

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
            && (this.ParamResolutions == other.ParamResolutions
                || this.ParamResolutions
                       .OrderBy(kvp => kvp.Key)
                       .SequenceEqual(other.ParamResolutions.OrderBy(kvp => kvp.Key)));

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = default;
            hash.Add(this.CallableName);
            foreach (var kvp in this.ParamResolutions)
            {
                hash.Add(kvp);
            }
            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// Base class for call graph types.
    /// </summary>
    public abstract class BaseCallGraph<TNode, TEdge>
        where TNode : BaseCallGraphNode
        where TEdge : BaseCallGraphEdge
    {
        // Static Elements

        /// <summary>
        /// Returns an empty dependency for a node.
        /// </summary>
        protected static ILookup<TNode, TEdge> EmptyDependency() =>
            ImmutableArray<KeyValuePair<TNode, TEdge>>.Empty
            .ToLookup(kvp => kvp.Key, kvp => kvp.Value);

        // Member Fields

        /// <summary>
        /// This is a dictionary mapping source nodes to information about target nodes. This information is represented
        /// by a dictionary mapping target node to the edges pointing from the source node to the target node.
        /// </summary>
        protected readonly Dictionary<TNode, Dictionary<TNode, ImmutableArray<TEdge>>> dependencies =
            new Dictionary<TNode, Dictionary<TNode, ImmutableArray<TEdge>>>();

        // Properties

        /// <summary>
        /// The number of nodes in the call graph.
        /// </summary>
        public int Count => this.dependencies.Count;

        /// <summary>
        /// A hash set of the nodes in the call graph.
        /// </summary>
        public ImmutableHashSet<TNode> Nodes => this.dependencies.Keys.ToImmutableHashSet();

        // Member Methods

        /// <summary>
        /// Returns true if the given node is found in the call graph, false otherwise.
        /// Throws ArgumentNullException if argument is null.
        /// </summary>
        public bool ContainsNode(TNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return this.dependencies.ContainsKey(node);
        }

        /// <summary>
        /// Returns the children nodes of a given node. Each key in the returned lookup is a child
        /// node of the given node. Each value in the lookup is an edge connecting the given node to
        /// the child node represented by the associated key.
        /// Returns an empty ILookup if the node was found with no dependencies or was not found in
        /// the graph.
        /// Throws ArgumentNullException if argument is null.
        /// </summary>
        public ILookup<TNode, TEdge> GetDirectDependencies(TNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

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

        /// <summary>
        /// Adds a dependency to the call graph using the two nodes and the edge between them.
        /// The nodes are added to the graph if they are not already there. The edge is always added.
        /// Throws ArgumentNullException if any of the arguments are null.
        /// </summary>
        protected void AddDependency(TNode fromNode, TNode toNode, TEdge edge)
        {
            if (fromNode is null)
            {
                throw new ArgumentNullException(nameof(fromNode));
            }

            if (toNode is null)
            {
                throw new ArgumentNullException(nameof(toNode));
            }

            if (edge is null)
            {
                throw new ArgumentNullException(nameof(edge));
            }

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
        /// Throws ArgumentNullException if the argument is null.
        /// </summary>
        internal void AddNode(TNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (!this.dependencies.ContainsKey(node))
            {
                this.dependencies[node] = new Dictionary<TNode, ImmutableArray<TEdge>>();
            }
        }
    }

    /// <summary>
    /// A kind of call graph whose nodes represent Q# callables.
    /// </summary>
    public class SimpleCallGraph : BaseCallGraph<SimpleCallGraphNode, SimpleCallGraphEdge>
    {
        // Static Elements

        private static IEnumerable<IEnumerable<SimpleCallGraphEdge>> CartesianProduct(IEnumerable<IEnumerable<SimpleCallGraphEdge>> sequences)
        {
            IEnumerable<IEnumerable<SimpleCallGraphEdge>> result = new[] { Enumerable.Empty<SimpleCallGraphEdge>() };
            foreach (var sequence in sequences)
            {
                result = sequence.SelectMany(item => result, (item, seq) => seq.Concat(new[] { item })).ToList();
            }
            return result;
        }

        // Constructors

        /// <summary>
        /// Constructs a call graph from a compilation. The optional trim argument may be
        /// used to specify the call graph to be trimmed to only include callables that
        /// entry points are dependent on.
        /// Throws ArgumentNullException if compilation argument is null.
        /// </summary>
        public SimpleCallGraph(QsCompilation compilation, bool trim = false)
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (trim)
            {
                BuildCallGraph.PopulateTrimmedGraph(this, compilation);
            }
            else
            {
                BuildCallGraph.PopulateSimpleGraph(this, compilation);
            }
        }

        /// <summary>
        /// Constructs a call graph from callables.
        /// Throws an ArgumentNullException if the argument is null or any of the callables are null.
        /// </summary>
        public SimpleCallGraph(IEnumerable<QsCallable> callables)
        {
            if (callables is null || callables.Any(x => x is null))
            {
                throw new ArgumentNullException(nameof(callables));
            }

            BuildCallGraph.PopulateSimpleGraph(this, callables);
        }

        // Member Methods

        /// <summary>
        /// Given a call graph edges, finds all cycles and determines if each is valid.
        /// Invalid cycles are those that cause type parameters to be mapped to
        /// other type parameters of the same callable (constricting resolutions)
        /// or to a type containing a nested reference to the same type parameter,
        /// i.e Foo.A -> Foo.A[].
        /// Returns an enumerable of tuples for each edge of each invalid cycle found,
        /// each tuple containing a diagnostic and the callable name where the diagnostic
        /// should be placed.
        /// </summary>
        public IEnumerable<Tuple<QsCompilerDiagnostic, QsQualifiedName>> VerifyAllCycles()
        {
            var diagnostics = new List<Tuple<QsCompilerDiagnostic, QsQualifiedName>>();

            if (this.Nodes.Any())
            {
                var cycles = this.GetCallCycles().SelectMany(x => CartesianProduct(this.GetEdges(x).Reverse()));
                foreach (var cycle in cycles)
                {
                    var combination = new TypeResolutionCombination(cycle.Select(edge => edge.ParamResolutions).ToArray());
                    if (!combination.IsValid)
                    {
                        foreach (var edge in cycle)
                        {
                            diagnostics.Add(Tuple.Create(
                                QsCompilerDiagnostic.Error(
                                    Diagnostics.ErrorCode.InvalidCyclicTypeParameterResolution,
                                    Enumerable.Empty<string>(),
                                    edge.ReferenceRange),
                                edge.FromCallableName));
                        }
                    }
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Adds a dependency to the call graph from the fromNode to the toNode, creating an edge in between them.
        /// Throws an ArgumentNullException if any argument is null.
        /// </summary>
        internal void AddDependency(SimpleCallGraphNode fromNode, SimpleCallGraphNode toNode, TypeParameterResolutions typeParamRes, DataTypes.Range referenceRange)
        {
            if (typeParamRes is null)
            {
                throw new ArgumentNullException(nameof(typeParamRes));
            }

            if (referenceRange is null)
            {
                throw new ArgumentNullException(nameof(referenceRange));
            }

            if (fromNode is null)
            {
                throw new ArgumentNullException(nameof(fromNode));
            }

            if (toNode is null)
            {
                throw new ArgumentNullException(nameof(toNode));
            }

            var edge = new SimpleCallGraphEdge(typeParamRes, fromNode.CallableName, toNode.CallableName, referenceRange);

            this.AddDependency(fromNode, toNode, edge);
        }

        /// <summary>
        /// Finds and returns a list of all cycles in the call graph, each one being represented by an array of nodes.
        /// To get the edges between the nodes of a given cycle, use the GetDirectDependencies method.
        /// </summary>
        private ImmutableArray<ImmutableArray<SimpleCallGraphNode>> GetCallCycles()
        {
            var indexToNode = this.dependencies.Keys.ToImmutableArray();
            var nodeToIndex = indexToNode.Select((v, i) => (v, i)).ToImmutableDictionary(kvp => kvp.v, kvp => kvp.i);
            var graph = indexToNode
                .Select((v, i) => (v, i))
                .ToDictionary(
                    kvp => kvp.i,
                    kvp => this.dependencies[kvp.v].Keys
                        .Select(dep => nodeToIndex[dep])
                        .ToList());

            var cycles = new JohnsonCycleFind().GetAllCycles(graph);
            return cycles.Select(cycle => cycle.Select(index => indexToNode[index]).ToImmutableArray()).ToImmutableArray();
        }

        private IEnumerable<IEnumerable<SimpleCallGraphEdge>> GetEdges(ImmutableArray<SimpleCallGraphNode> cycle)
            => cycle.Select((curr, i) => this.GetDirectDependencies(curr)[cycle[(i + 1) % cycle.Length]]);

        // Inner Classes

        /// <summary>
        /// Implementation of Johnson's algorithm for finding all simple cycles in a graph.
        /// A simple cycle is one where no node repeats in the cycle until getting back
        /// to the beginning of the cycle.
        ///
        /// The algorithm uses Tarjan's algorithm for finding all strongly-connected
        /// components in a graph. A strongly-connected component, or SCC, is a subgraph
        /// in which all nodes can reach all other nodes in the subgraph.
        ///
        /// The algorithm begins by using Tarjan's algorithm to find the SCCs of the graph.
        /// Each SCC is processed for cycles, using the smallest-id node of the SCC as the
        /// starting node. Then the starting node is removed from the graph, and Tarjan is used
        /// again to find SCCs of the new graph, and the next SCC is processed. This repeats
        /// until there are no more SCCs (and no more nodes) left in the graph.
        ///
        /// The algorithm has time complexity of O((n + e)(c + 1)) with n nodes, e edges,
        /// and c elementary cycles.
        ///
        /// The starting point for this algorithm in this class is the GetAllCycles method.
        /// </summary>
        private class JohnsonCycleFind
        {
            private readonly Stack<(HashSet<int> SCC, int MinNode)> sccStack = new Stack<(HashSet<int> SCC, int MinNode)>();

            /// <summary>
            /// Johnson's algorithm for finding all cycles in a graph.
            /// This returns a list of cycles, each represented as a list of nodes. Nodes
            /// for this algorithm are integers. The cycles are guaranteed to not contain
            /// any duplicates and the last node in the list is assumed to be connected
            /// to the first.
            ///
            /// This implementation passes the full graph along with a hash set of nodes
            /// to represent subgraphs because it is more performant to check if a node
            /// is in the hash set than to build a dictionary for the subgraph.
            /// </summary>
            public List<List<int>> GetAllCycles(Dictionary<int, List<int>> graph)
            {
                var cycles = new List<List<int>>();

                this.PushSCCsFromGraph(graph, graph.Keys.ToHashSet());
                while (this.sccStack.Any())
                {
                    var (scc, startNode) = this.sccStack.Pop();
                    cycles.AddRange(this.GetSccCycles(graph, scc, startNode));
                    scc.Remove(startNode);
                    this.PushSCCsFromGraph(graph, scc);
                }

                return cycles;
            }

            /// <summary>
            /// Uses Tarjan's algorithm for finding strongly-connected components, or SCCs of a
            /// graph to get all SCC, orders them by their node with the smallest id, and pushes
            /// them to the SCC stack.
            /// </summary>
            private void PushSCCsFromGraph(Dictionary<int, List<int>> graph, HashSet<int> filter)
            {
                var sccs = this.TarjanSCC(graph, filter).OrderByDescending(x => x.MinNode);
                foreach (var scc in sccs)
                {
                    this.sccStack.Push(scc);
                }
            }

            /// <summary>
            /// Tarjan's algorithm for finding all strongly-connected components in a graph.
            /// A strongly-connected component, or SCC, is a subgraph in which all nodes can reach
            /// all other nodes in the subgraph.
            ///
            /// This implementation was based on the pseudo-code found here:
            /// https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm
            ///
            /// This returns a list of SCCs represented by sets of nodes, each paired with the
            /// value of their lowest valued-node. The list is sorted such that each SCC comes
            /// before any of its children (reverse topological ordering).
            /// </summary>
            private List<(HashSet<int> SCC, int MinNode)> TarjanSCC(Dictionary<int, List<int>> inputGraph, HashSet<int> filter)
            {
                var index = 0; // The index represents the order in which the nodes are discovered by Tarjan's algorithm.
                               // This is necessarily separate from the node's id value.
                var nodeStack = new Stack<int>();
                var nodeInfo = new Dictionary<int, (int Index, int LowLink, bool OnStack)>();
                var stronglyConnectedComponents = new List<(HashSet<int> SCC, int MinNode)>();

                void SetMinLowLink(int node, int potentialMin)
                {
                    var vInfo = nodeInfo[node];
                    if (vInfo.LowLink > potentialMin)
                    {
                        vInfo.LowLink = potentialMin;
                        nodeInfo[node] = vInfo;
                    }
                }

                void StrongConnect(int node)
                {
                    // Set the depth index for node to the smallest unused index
                    nodeStack.Push(node);
                    nodeInfo[node] = (index, index, true);
                    index += 1;

                    // Consider children of node
                    foreach (var child in inputGraph[node])
                    {
                        if (filter.Contains(child))
                        {
                            if (!nodeInfo.ContainsKey(child))
                            {
                                // Child has not yet been visited; recurse on it
                                StrongConnect(child);
                                SetMinLowLink(node, nodeInfo[child].LowLink);
                            }
                            else if (nodeInfo[child].OnStack)
                            {
                                // Child is in stack and hence in the current SCC
                                // If child is not in stack, then (node, child) is an edge pointing to an SCC already found and must be ignored
                                // Note: The next line may look odd - but is correct.
                                // It says child.index not child.lowlink; that is deliberate and from the original paper
                                SetMinLowLink(node, nodeInfo[child].Index);
                            }
                        }
                    }

                    // If node is a root node, pop the stack and generate an SCC
                    if (nodeInfo[node].LowLink == nodeInfo[node].Index)
                    {
                        var scc = new HashSet<int>();

                        var minNode = node;
                        int nodeInScc;
                        do
                        {
                            nodeInScc = nodeStack.Pop();
                            var wInfo = nodeInfo[nodeInScc];
                            wInfo.OnStack = false;
                            nodeInfo[nodeInScc] = wInfo;
                            scc.Add(nodeInScc);

                            // Keep track of minimum node id in scc
                            if (minNode > nodeInScc)
                            {
                                minNode = nodeInScc;
                            }
                        }
                        while (node != nodeInScc);
                        stronglyConnectedComponents.Add((scc, minNode));
                    }
                }

                foreach (var node in filter)
                {
                    if (!nodeInfo.ContainsKey(node))
                    {
                        StrongConnect(node);
                    }
                }

                return stronglyConnectedComponents;
            }

            /// <summary>
            /// Johnson's algorithm for finding all cycles in an SCC, or strongly-connected component.
            ///
            /// It will process an individual SCC for cycles by looking at each of their nodes
            /// starting with the smallest-id node of the SCC as the current node. Children of
            /// the current node are explored, and if any of them are the starting node, a cycle is
            /// found. Nodes are marked as 'blocked' when visited, and if no cycle is found after
            /// exploring a node's children, the node is marked as being blocked on its children.
            /// Only when a cycle is found is a node unblocked and any node blocked on that node is
            /// also unblocked, recursively.
            /// </summary>
            private List<List<int>> GetSccCycles(Dictionary<int, List<int>> intputSCC, HashSet<int> filter, int startNode)
            {
                var cycles = new List<List<int>>();
                var blockedSet = new HashSet<int>();
                var blockedMap = new Dictionary<int, HashSet<int>>();
                var nodeStack = new Stack<int>();

                void Unblock(int node)
                {
                    if (blockedSet.Remove(node) && blockedMap.TryGetValue(node, out var nodesToUnblock))
                    {
                        blockedMap.Remove(node);
                        foreach (var n in nodesToUnblock)
                        {
                            Unblock(n);
                        }
                    }
                }

                bool PopulateCycles(int currNode)
                {
                    var foundCycle = false;
                    nodeStack.Push(currNode);
                    blockedSet.Add(currNode);

                    foreach (var child in intputSCC[currNode])
                    {
                        if (filter.Contains(child))
                        {
                            if (child == startNode)
                            {
                                foundCycle = true;
                                cycles.Add(nodeStack.Reverse().ToList());
                            }
                            else if (!blockedSet.Contains(child))
                            {
                                foundCycle |= PopulateCycles(child);
                            }
                        }
                    }

                    nodeStack.Pop();

                    if (foundCycle)
                    {
                        Unblock(currNode);
                    }
                    else
                    {
                        // Mark currNode as being blocked on each of its children
                        // If any of currNode's children unblock, currNode will unblock
                        foreach (var child in intputSCC[currNode])
                        {
                            if (filter.Contains(child))
                            {
                                if (!blockedMap.ContainsKey(child))
                                {
                                    blockedMap[child] = new HashSet<int>() { currNode };
                                }
                                else
                                {
                                    blockedMap[child].Add(currNode);
                                }
                            }
                        }
                    }

                    return foundCycle;
                }

                PopulateCycles(startNode);

                return cycles;
            }
        }
    }

    /// <summary>
    /// A kind of call graph whose nodes represent concrete instances of Q# callables.
    /// </summary>
    public class ConcreteCallGraph : BaseCallGraph<ConcreteCallGraphNode, ConcreteCallGraphEdge>
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
        /// Returns all callables that are used directly or indirectly within the given caller,
        /// whether they are called, partially applied, or assigned. Each key in the returned lookup
        /// represents a callable that is used by the caller. Each value in the lookup is an
        /// IEnumerable of edges representing all the different ways the given caller took a
        /// dependency on the callable represented by the associated key.
        /// Throws ArgumentNullException if argument is null.
        /// Returns an empty ILookup if the node was found with no dependencies or was not found in
        /// the graph.
        /// </summary>
        public ILookup<ConcreteCallGraphNode, ConcreteCallGraphEdge> GetAllDependencies(ConcreteCallGraphNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (!this.dependencies.ContainsKey(node))
            {
                return EmptyDependency();
            }

            var accum = new Dictionary<ConcreteCallGraphNode, ImmutableArray<ConcreteCallGraphEdge>>();

            void WalkDependencyTree(ConcreteCallGraphNode root, ConcreteCallGraphEdge? edgeFromRoot)
            {
                if (this.dependencies.TryGetValue(root, out var next))
                {
                    foreach (var (dependent, edges) in next)
                    {
                        var combinedEdges = edgeFromRoot is null
                            ? edges
                            : edges.Select(e => new ConcreteCallGraphEdge(edgeFromRoot.FromCallableName, e.ToCallableName, edgeFromRoot.ReferenceRange));

                        if (accum.TryGetValue(dependent, out var existingEdges))
                        {
                            combinedEdges = combinedEdges.Where(edge => !existingEdges.Any(existing => edge.Equals(existing)));
                            accum[dependent] = existingEdges.AddRange(combinedEdges);
                        }
                        else
                        {
                            accum[dependent] = combinedEdges.ToImmutableArray();
                        }

                        foreach (var edge in combinedEdges)
                        {
                            WalkDependencyTree(dependent, edge);
                        }
                    }
                }
            }

            WalkDependencyTree(node, null);

            return accum
                .SelectMany(kvp => kvp.Value, Tuple.Create)
                .ToLookup(tup => tup.Item1.Key, tup => tup.Item2);
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
