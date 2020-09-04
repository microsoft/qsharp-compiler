// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

#nullable enable

namespace Microsoft.Quantum.QsCompiler.DependencyAnalysis
{
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    // Type Parameters are frequently referenced by the callable of the type parameter followed by the name of the specific type parameter.
    using TypeParameterName = Tuple<QsQualifiedName, NonNullable<string>>;
    using TypeParameterResolutions = ImmutableDictionary</*TypeParameterName*/ Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

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
        /// Constructor for CallGraphEdge objects.
        /// Strips position info from given resolutions before assigning them to ParamResoluitons
        /// to ensure that the same type parameters will compare as equal.
        /// Throws an ArgumentNullException if paramResolutions is null.
        /// </summary>
        internal BaseCallGraphEdge(QsQualifiedName fromCallableName, QsQualifiedName toCallableName, DataTypes.Range referenceRange)
        {
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

    public class ConcreteCallGraphEdge : BaseCallGraphEdge
    {
        internal ConcreteCallGraphEdge(QsQualifiedName fromCallableName, QsQualifiedName toCallableName, DataTypes.Range referenceRange)
            : base(fromCallableName, toCallableName, referenceRange)
        {
        }

        /// <summary>
        /// Creates a call graph edge that represents the combination of several edges that lead to a target node.
        /// The edges should be ordered to represent a path from a source node to the given target node so that
        /// the final edge points to the target node.
        /// Throws an ArgumentNullException if any of the arguments is null.
        /// </summary>
        public static ConcreteCallGraphEdge CombinePathIntoSingleEdge(IEnumerable<ConcreteCallGraphEdge> edges, BaseCallGraphNode targetNode)
        {
            if (targetNode == null)
            {
                throw new ArgumentNullException(nameof(targetNode));
            }

            if (edges == null || edges.Any(e => e == null))
            {
                throw new ArgumentNullException(nameof(edges));
            }

            var first = edges.First();
            var last = edges.Last();
            return new ConcreteCallGraphEdge(first.FromCallableName, last.ToCallableName, first.ReferenceRange);
        }
    }

    /// <summary>
    /// Contains the information that exists on edges in a call graph.
    /// The ParamResolutions are non-null and have all of their position information removed.
    /// The order of the elements of the ParamResolutions will not matter for comparison/hashing.
    /// </summary>
    public class SimpleCallGraphEdge : BaseCallGraphEdge, IEquatable<SimpleCallGraphEdge>
    {
        /// <summary>
        /// Contains the type parameter resolutions associated with this edge.
        /// </summary>
        public TypeParameterResolutions ParamResolutions { get; }

        /// <summary>
        /// Constructor for CallGraphEdge objects.
        /// Strips position info from given resolutions before assigning them to ParamResoluitons
        /// to ensure that the same type parameters will compare as equal.
        /// Throws an ArgumentNullException if paramResolutions is null.
        /// </summary>
        internal SimpleCallGraphEdge(TypeParameterResolutions paramResolutions, QsQualifiedName fromCallableName, QsQualifiedName toCallableName, DataTypes.Range referenceRange)
            : base(fromCallableName, toCallableName, referenceRange)
        {
            if (paramResolutions == null)
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

        /// <summary>
        /// Creates a call graph edge that represents the combination of several edges that lead to a target node.
        /// The edges should be ordered to represent a path from a source node to the given target node so that
        /// the final edge points to the target node.
        /// Throws an ArgumentNullException if any of the arguments is null.
        /// </summary>
        public static SimpleCallGraphEdge CombinePathIntoSingleEdge(IEnumerable<SimpleCallGraphEdge> edges, BaseCallGraphNode targetNode)
        {
            if (targetNode == null)
            {
                throw new ArgumentNullException(nameof(targetNode));
            }

            if (edges == null || edges.Any(e => e == null))
            {
                throw new ArgumentNullException(nameof(edges));
            }

            var combination = new TypeResolutionCombination(edges.Select(e => e.ParamResolutions).Reverse().ToArray());
            var first = edges.First();
            var last = edges.Last();
            return new SimpleCallGraphEdge(combination.CombinedResolutionDictionary.FilterByOrigin(targetNode.CallableName), first.FromCallableName, last.ToCallableName, first.ReferenceRange);
        }
    }

    public abstract class BaseCallGraphNode : IEquatable<BaseCallGraphNode>
    {
        /// <summary>
        /// The name of the represented callable.
        /// </summary>
        public QsQualifiedName CallableName { get; }

        public BaseCallGraphNode(QsQualifiedName callableName)
        {
            if (callableName is null)
            {
                throw new ArgumentException(nameof(callableName));
            }

            this.CallableName = callableName;
        }

        /// <summary>
        /// Constructor for CallGraphNode objects.
        /// Throws an ArgumentNullException if callable is null.
        /// </summary>
        public BaseCallGraphNode(QsCallable callable) : this(
            callable == null ? throw new ArgumentNullException(nameof(callable)) :
            callable.FullName)
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is BaseCallGraphNode && this.Equals((BaseCallGraphNode)obj);
        }

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
    /// Contains the information that exists on nodes in a call graph.
    /// The CallableName and Kind are expected to be non-null.
    /// </summary>
    public class ConcreteCallGraphNode : BaseCallGraphNode, IEquatable<ConcreteCallGraphNode>
    {
        public TypeParameterResolutions ParamResolutions { get; }

        /// <summary>
        /// Constructor for CallGraphNode objects.
        /// Strips position info from given type arguments before assigning them to TypeArgs.
        /// Throws an ArgumentNullException if callableName or kind is null.
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

    public class SimpleCallGraphNode : BaseCallGraphNode
    {
        public SimpleCallGraphNode(QsQualifiedName callableName)
            : base(callableName)
        {
        }

        /// <summary>
        /// Constructor for CallGraphNode objects.
        /// Throws an ArgumentNullException if callable is null.
        /// </summary>
        public SimpleCallGraphNode(QsCallable callable) : base(
            callable == null ? throw new ArgumentNullException(nameof(callable)) :
            callable.FullName)
        {
        }
    }

    /// <summary>
    /// Class used to track call graph of a compilation.
    /// </summary>
    public abstract class BaseCallGraph<TNode, TEdge>
        where TNode : BaseCallGraphNode
        where TEdge : BaseCallGraphEdge
    {
        // Static Elements

        /// <summary>
        /// Represents an empty dependency for a node.
        /// </summary>
        protected static readonly ILookup<TNode, TEdge> EmptyDependency =
            ImmutableArray<KeyValuePair<TNode, TEdge>>.Empty
            .ToLookup(kvp => kvp.Key, kvp => kvp.Value);

        // Member Fields

        /// <summary>
        /// This is a dictionary mapping source nodes to information about target nodes. This information is represented
        /// by a dictionary mapping target node to the edges pointing from the source node to the target node.
        /// </summary>
        protected readonly Dictionary<TNode, Dictionary<TNode, ImmutableArray<TEdge>>> dependencies =
            new Dictionary<TNode, Dictionary<TNode, ImmutableArray<TEdge>>>();

        // Constructors

        /// <summary>
        /// This default constructor is set to internal so that only the walker can create call graphs.
        /// </summary>
        internal BaseCallGraph()
        {
        }

        // Properties

        /// <summary>
        /// The number of nodes in the call graph.
        /// </summary>
        public int Count => this.dependencies.Count;

        /// <summary>
        /// A collection of the nodes in the call graph.
        /// </summary>
        public ImmutableHashSet<TNode> Nodes => this.dependencies.Keys.ToImmutableHashSet();

        // Member Methods

        /// <summary>
        /// Returns true if the given node is found in the call graph, false otherwise.
        /// Throws ArgumentNullException if argument is null.
        /// </summary>
        public bool ContainsNode(TNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return this.dependencies.ContainsKey(node);
        }

        /// <summary>
        /// Returns all specializations that are used directly within the given caller, whether they are
        /// called, partially applied, or assigned. Each key in the returned lookup represents a
        /// specialization that is used by the caller. Each value in the lookup is an IEnumerable of edges
        /// representing all the different ways the given caller specialization took a dependency on the
        /// specialization represented by the associated key.
        /// Throws ArgumentNullException if argument is null.
        /// Returns an empty ILookup if the node was found with no dependencies or was not found in
        /// the graph.
        /// </summary>
        public ILookup<TNode, TEdge> GetDirectDependencies(TNode callerNode)
        {
            if (callerNode == null)
            {
                throw new ArgumentNullException(nameof(callerNode));
            }

            if (this.dependencies.TryGetValue(callerNode, out var dep))
            {
                return dep
                    .SelectMany(kvp => kvp.Value, Tuple.Create)
                    .ToLookup(tup => tup.Item1.Key, tup => tup.Item2);
            }
            else
            {
                return EmptyDependency;
            }
        }

        /// <summary>
        /// Returns all specializations that are used directly within the given caller, whether they are
        /// called, partially applied, or assigned. Each key in the returned lookup represents a
        /// specialization that is used by the caller. Each value in the lookup is an IEnumerable of edges
        /// representing all the different ways the given caller specialization took a dependency on the
        /// specialization represented by the associated key.
        /// Throws ArgumentNullException if argument is null.
        /// Returns an empty ILookup if the node was found with no dependencies or was not found in
        /// the graph.
        /// </summary>
        //public ILookup<CallGraphNode, TEdge> GetDirectDependencies(QsCallable caller)
        //{
        //    if (caller == null)
        //    {
        //        throw new ArgumentNullException(nameof(caller));
        //    }
        //
        //    return this.GetDirectDependencies(new CallGraphNode(caller.Parent, callerSpec.Kind, callerSpec.TypeArguments));
        //}

        /// <summary>
        /// Adds a dependency to the call graph using the two nodes and the edge between them.
        /// Throws ArgumentNullException if any of the arguments are null.
        /// </summary>
        protected void AddDependency(TNode callerKey, TNode calledKey, TEdge edge)
        {
            if (callerKey is null)
            {
                throw new ArgumentNullException(nameof(callerKey));
            }

            if (calledKey is null)
            {
                throw new ArgumentNullException(nameof(calledKey));
            }

            if (edge is null)
            {
                throw new ArgumentNullException(nameof(edge));
            }

            if (this.dependencies.TryGetValue(callerKey, out var deps))
            {
                if (!deps.TryGetValue(calledKey, out var edges))
                {
                    deps[calledKey] = ImmutableArray.Create(edge);
                }
                else
                {
                    deps[calledKey] = edges.Add(edge);
                }
            }
            else
            {
                var newDeps = new Dictionary<TNode, ImmutableArray<TEdge>>();
                newDeps[calledKey] = ImmutableArray.Create(edge);
                this.dependencies[callerKey] = newDeps;
            }

            // Need to make sure the each dependencies has an entry for each node in the graph, even if node has no dependencies
            this.AddNode(calledKey);
        }

        /// <summary>
        /// Adds the given node to the call graph, if it is not already in the graph.
        /// </summary>
        internal void AddNode(TNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (!this.dependencies.ContainsKey(node))
            {
                this.dependencies[node] = new Dictionary<TNode, ImmutableArray<TEdge>>();
            }
        }

        /// <summary>
        /// Adds the given specialization to the call graph as a node, if it is not already in the graph.
        /// </summary>
        //internal void AddNode(QsCallable spec) => this.AddNode(new TNode(spec));
    }

    public class SimpleCallGraph : BaseCallGraph<SimpleCallGraphNode, SimpleCallGraphEdge>
    {
        private static IEnumerable<IEnumerable<SimpleCallGraphEdge>> CartesianProduct(IEnumerable<IEnumerable<SimpleCallGraphEdge>> sequences)
        {
            IEnumerable<IEnumerable<SimpleCallGraphEdge>> result = new[] { Enumerable.Empty<SimpleCallGraphEdge>() };
            foreach (var sequence in sequences)
            {
                result = sequence.SelectMany(item => result, (item, seq) => seq.Concat(new[] { item })).ToList();
            }
            return result;
        }

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

        internal void AddDependency(SimpleCallGraphNode callerKey, SimpleCallGraphNode calledKey, TypeParameterResolutions typeParamRes, DataTypes.Range referenceRange)
        {
            if (typeParamRes is null)
            {
                throw new ArgumentNullException(nameof(typeParamRes));
            }

            if (referenceRange is null)
            {
                throw new ArgumentNullException(nameof(referenceRange));
            }

            var edge = new SimpleCallGraphEdge(typeParamRes, callerKey.CallableName, calledKey.CallableName, referenceRange);

            this.AddDependency(callerKey, calledKey, edge);
        }

        /// <summary>
        /// Finds and returns a list of all cycles in the call graph, each one being represented by an array of nodes.
        /// To get the edges between the nodes of a given cycle, use the GetDirectDependencies method.
        /// </summary>
        internal ImmutableArray<ImmutableArray<SimpleCallGraphNode>> GetCallCycles()
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

    public class ConcreteCallGraph : BaseCallGraph<ConcreteCallGraphNode, ConcreteCallGraphEdge>
    {
        /// <summary>
        /// Returns all specializations that are used directly or indirectly within the given caller,
        /// whether they are called, partially applied, or assigned. Each key in the returned lookup
        /// represents a specialization that is used by the caller. Each value in the lookup is an
        /// IEnumerable of edges representing all the different ways the given caller specialization took a
        /// dependency on the specialization represented by the associated key.
        /// Throws ArgumentNullException if argument is null.
        /// Returns an empty ILookup if the node was found with no dependencies or was not found in
        /// the graph.
        /// </summary>
        public ILookup<ConcreteCallGraphNode, ConcreteCallGraphEdge> GetAllDependencies(ConcreteCallGraphNode callerSpec)
        {
            if (callerSpec == null)
            {
                throw new ArgumentNullException(nameof(callerSpec));
            }

            if (!this.dependencies.ContainsKey(callerSpec))
            {
                return EmptyDependency;
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
                            : edges.Select(e => ConcreteCallGraphEdge.CombinePathIntoSingleEdge(new[] { edgeFromRoot, e }, dependent));

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

            WalkDependencyTree(callerSpec, null);

            return accum
                .SelectMany(kvp => kvp.Value, Tuple.Create)
                .ToLookup(tup => tup.Item1.Key, tup => tup.Item2);
        }

        /// <summary>
        /// Returns all specializations that are used directly or indirectly within the given caller,
        /// whether they are called, partially applied, or assigned. Each key in the returned lookup
        /// represents a specialization that is used by the caller. Each value in the lookup is an
        /// IEnumerable of edges representing all the different ways the given caller specialization took a
        /// dependency on the specialization represented by the associated key.
        /// Throws ArgumentNullException if argument is null.
        /// Returns an empty ILookup if the node was found with no dependencies or was not found in
        /// the graph.
        /// </summary>
        //public ILookup<CallGraphNode, CallGraphEdge> GetAllDependencies(QsSpecialization callerSpec)
        //{
        //    if (callerSpec == null)
        //    {
        //        throw new ArgumentNullException(nameof(callerSpec));
        //    }
        //
        //    return this.GetAllDependencies(new CallGraphNode(callerSpec.Parent, callerSpec.Kind, callerSpec.TypeArguments));
        //}

        internal void AddDependency(ConcreteCallGraphNode callerKey, ConcreteCallGraphNode calledKey, DataTypes.Range referenceRange)
        {
            if (referenceRange is null)
            {
                throw new ArgumentNullException(nameof(referenceRange));
            }

            var edge = new ConcreteCallGraphEdge(callerKey.CallableName, calledKey.CallableName, referenceRange);

            this.AddDependency(callerKey, calledKey, edge);
        }
    }
}
