// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

#nullable enable

// ToDo: Review access modifiers

namespace Microsoft.Quantum.QsCompiler.DependencyAnalysis
{
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    // Type Parameters are frequently referenced by the callable of the type parameter followed by the name of the specific type parameter.
    using TypeParameterName = Tuple<QsQualifiedName, NonNullable<string>>;
    using TypeParameterResolutions = ImmutableDictionary</*TypeParameterName*/ Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    /// <summary>
    /// Contains the information that exists on edges in a call graph.
    /// The ParamResolutions are non-null and have all of their position information removed.
    /// The order of the elements of the ParamResolutions will not matter for comparison/hashing.
    /// </summary>
    public sealed class CallGraphEdge : ICallGraphEdge
    {
        /// <summary>
        /// Contains the type parameter resolutions associated with this edge.
        /// </summary>
        public TypeParameterResolutions ParamResolutions { get; private set; }

        public NonNullable<string> FileName { get; set; }

        public QsPositionInfo Start { get; set; }

        public QsPositionInfo End { get; set; }

        /// <summary>
        /// Constructor for CallGraphEdge objects.
        /// Strips position info from given resolutions before assigning them to ParamResoluitons
        /// to ensure that the same type parameters will compare as equal.
        /// Throws an ArgumentNullException if paramResolutions is null.
        /// </summary>
        public CallGraphEdge(TypeParameterResolutions paramResolutions, NonNullable<string> fileName, QsPositionInfo positionStart, QsPositionInfo positionEnd)
        {
            if (paramResolutions == null)
            {
                throw new ArgumentNullException(nameof(paramResolutions));
            }

            // Remove position info from type parameter resolutions
            this.ParamResolutions = paramResolutions.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => StripPositionInfo.Apply(kvp.Value));

            this.FileName = fileName;
            this.Start = positionStart;
            this.End = positionEnd;
        }

        /// <summary>
        /// Determines if the object is the same as the given edge, ignoring the
        /// ordering of key-value pairs in the type parameter dictionaries.
        /// </summary>
        public bool Equals(ICallGraphEdge edge) =>
            this.FileName.Equals(edge.FileName)
            && this.Start.Equals(edge.Start)
            && this.End.Equals(edge.End)
            && (this.ParamResolutions == edge.ParamResolutions
                || this.ParamResolutions
                       .OrderBy(kvp => kvp.Key)
                       .SequenceEqual(edge.ParamResolutions.OrderBy(kvp => kvp.Key)));

        /// <summary>
        /// Inserts the edge into the given array of edges if the edge is not already in the array.
        /// Ignores order of key-value pairs in the type parameter dictionaries.
        /// </summary>
        //public static ImmutableArray<ICallGraphEdge> InsertEdge(ICallGraphEdge edge, ImmutableArray<ICallGraphEdge> edges)
        //{
        //    var ordered = edge.ParamResolutions.OrderBy(kvp => kvp.Key).ToList();
        //
        //    if (edges == null || edges.Length == 0)
        //    {
        //        return ImmutableArray.Create(edge);
        //    }
        //    else if (edges.Any(e => ordered.SequenceEqual(e.ParamResolutions.OrderBy(kvp => kvp.Key))))
        //    {
        //        return edges;
        //    }
        //    else
        //    {
        //        return edges.Add(edge);
        //    }
        //}

        /// <summary>
        /// Creates a call graph edge that represents the combination of several edges that lead to a target node.
        /// The edges should be ordered by their distance to the target node. So the first edge should point directly
        /// to the target node, the next edge should point to the node that has the first edge, the third edge should
        /// point to the node that has the second edge, and so on.
        /// Throws an ArgumentNullException if any of the arguments is null.
        /// </summary>
        public static ICallGraphEdge CombinePathIntoSingleEdge(ICallGraphNode targetNode, params ICallGraphEdge[] edges)
        {
            if (targetNode == null)
            {
                throw new ArgumentNullException(nameof(targetNode));
            }

            if (edges == null || edges.Any(e => e == null))
            {
                throw new ArgumentNullException(nameof(edges));
            }

            var combination = new TypeResolutionCombination(edges.Select(e => e.ParamResolutions).ToArray());
            var last = edges.Last();
            return new CallGraphEdge(combination.CombinedResolutionDictionary.FilterByOrigin(targetNode.CallableName), last.FileName, last.Start, last.End);
        }
    }

    /// <summary>
    /// Contains the information that exists on nodes in a call graph.
    /// The CallableName and Kind are expected to be non-null.
    /// </summary>
    public sealed class CallGraphNode : ICallGraphNode
    {
        /// <summary>
        /// The name of the represented callable.
        /// </summary>
        public QsQualifiedName CallableName { get; private set; }

        /// <summary>
        /// The specialization represented.
        /// </summary>
        public QsSpecializationKind Kind { get; private set; }

        /// <summary>
        /// The type arguments associated with this specialization.
        /// </summary>
        public QsNullable<ImmutableArray<ResolvedType>> TypeArgs { get; private set; }

        /// <summary>
        /// Constructor for CallGraphNode objects.
        /// Strips position info from given type arguments before assigning them to TypeArgs.
        /// Throws an ArgumentNullException if callableName or kind is null.
        /// </summary>
        public CallGraphNode(QsQualifiedName callableName, QsSpecializationKind kind, QsNullable<ImmutableArray<ResolvedType>> typeArgs)
        {
            this.CallableName = callableName ?? throw new ArgumentNullException(nameof(callableName));
            this.Kind = kind ?? throw new ArgumentNullException(nameof(kind));
            this.TypeArgs = typeArgs.IsValue
                ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(typeArgs.Item.Select(StripPositionInfo.Apply).ToImmutableArray())
                : typeArgs;
        }

        /// <summary>
        /// Constructor for CallGraphNode objects.
        /// Strips position info from given type arguments before assigning them to TypeArgs.
        /// Throws an ArgumentNullException if specialization is null.
        /// </summary>
        public CallGraphNode(QsSpecialization specialization) : this(
            specialization == null ? throw new ArgumentNullException(nameof(specialization)) :
            specialization.Parent,
            specialization.Kind,
            specialization.TypeArguments)
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ICallGraphNode && this.Equals((ICallGraphNode)obj);
        }

        /// <inheritdoc/>
        public bool Equals(ICallGraphNode other)
        {
            return (this.CallableName, this.Kind, this.TypeArgs).Equals((other.CallableName, other.Kind, other.TypeArgs));
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (this.CallableName, this.Kind, this.TypeArgs).GetHashCode();
        }
    }

    /// <summary>
    /// Class used to track call graph of a compilation.
    /// </summary>
    public class CallGraph : ICallGraph
    {
        /// <summary>
        /// Walker that checks a given type parameter resolution to see if it constricts
        /// the type parameter to another type parameter of the same callable, or contains
        /// a nested self-reference.
        /// </summary>
        private class CheckTypeParameterResolutions : TypeTransformation<CheckTypeParameterResolutions.TransformationState>
        {
            /// <summary>
            /// Determines if the given ResolvedType contains a reference to a different type
            /// parameter of the same callable as the given type parameter, typeParam, or if
            /// it contains a nested self-reference. Direct self-references are allowed.
            /// Returns false if a conflicting reference or a nested self-reference is found.
            /// Returns true otherwise.
            /// </summary>
            public static bool ContainsNestedSelfReference(TypeParameterName typeParam, ResolvedType typeParamRes)
            {
                if (typeParamRes.Resolution is ResolvedTypeKind.TypeParameter tp
                    && tp.Item.Origin.Equals(typeParam.Item1))
                {
                    // If given a type parameter whose origin matches the callable,
                    // the only valid resolution is a direct self-resolution
                    return !tp.Item.TypeName.Equals(typeParam.Item2);
                }

                // If not dealing with a top-level type parameter, then check the type for nested self-references
                var walker = new CheckTypeParameterResolutions(typeParam.Item1);
                walker.OnType(typeParamRes);

                return walker.SharedState.IsNestedSelfReference;
            }

            internal class TransformationState
            {
                public readonly QsQualifiedName Origin;
                public bool IsNestedSelfReference = false;

                public TransformationState(QsQualifiedName origin)
                {
                    this.Origin = origin;
                }
            }

            private CheckTypeParameterResolutions(QsQualifiedName origin)
                : base(new TransformationState(origin), TransformationOptions.NoRebuild)
            {
            }

            public new ResolvedType OnType(ResolvedType t)
            {
                // Short-circuit if we already know the type is constrictive.
                if (!this.SharedState.IsNestedSelfReference)
                {
                    base.OnType(t);
                }

                // It doesn't matter what we return because this is a walker.
                return t;
            }

            public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
            {
                if (tp.Origin.Equals(this.SharedState.Origin))
                {
                    this.SharedState.IsNestedSelfReference = true;
                }

                return base.OnTypeParameter(tp);
            }
        }

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

        /// <summary>
        /// This is a dictionary mapping source nodes to information about target nodes. This information is represented
        /// by a dictionary mapping target node to the edges pointing from the source node to the target node.
        /// </summary>
        private readonly Dictionary<ICallGraphNode, Dictionary<ICallGraphNode, ImmutableArray<ICallGraphEdge>>> dependencies =
            new Dictionary<ICallGraphNode, Dictionary<ICallGraphNode, ImmutableArray<ICallGraphEdge>>>();

        /// <summary>
        /// Represents an empty dependency for a node.
        /// </summary>
        private static readonly ILookup<ICallGraphNode, ICallGraphEdge> EmptyDependency =
            ImmutableArray<KeyValuePair<ICallGraphNode, ICallGraphEdge>>.Empty
            .ToLookup(kvp => kvp.Key, kvp => kvp.Value);

        /// <summary>
        /// Given a cycle of call graph edges, determines if the cycle is valid.
        /// Invalid cycles are those that cause type parameters to be mapped to
        /// other type parameters of the same callable (constricting resolutions)
        /// or to a type containing a nested reference to the same type parameter,
        /// i.e Foo.A -> Foo.A[].
        /// Returns true if the cycle is valid, false if invalid.
        /// </summary>
        internal static bool VerifyCycle(params ICallGraphEdge[] edges)
        {
            var combination = new TypeResolutionCombination(edges.Select(edge => edge.ParamResolutions).ToArray());
            if (!combination.IsValid)
            {
                return false;
            }
            // Check for if there is a nested self-reference in the resolutions, i.e. Foo.A -> Foo.A[]
            // Valid cycles do not contain nested self-references in their type parameter resolutions,
            // although this may be valid in other circumstances.
            return combination.CombinedResolutionDictionary
                .All(kvp => !CheckTypeParameterResolutions.ContainsNestedSelfReference(kvp.Key, kvp.Value));
        }

        private IEnumerable<IEnumerable<ICallGraphEdge>> GetEdges(ImmutableArray<ICallGraphNode> cycle)
            => cycle.Select((curr, i) => this.GetDirectDependencies(curr)[cycle[(i + 1) % cycle.Length]]);

        private static IEnumerable<IEnumerable<ICallGraphEdge>> CartesianProduct(IEnumerable<IEnumerable<ICallGraphEdge>> sequences)
        {
            IEnumerable<IEnumerable<ICallGraphEdge>> result = new[] { Enumerable.Empty<ICallGraphEdge>() };
            foreach (var sequence in sequences)
            {
                result = sequence.SelectMany(item => result, (item, seq) => seq.Concat(new[] { item })).ToList();
            }
            return result;
        }

        public IEnumerable<Tuple<NonNullable<string>, QsCompilerDiagnostic>> VerifyAllCycles()
        {
            var diagnostics = new List<Tuple<NonNullable<string>, QsCompilerDiagnostic>>();

            if (this.Nodes.Any())
            {
                var cycles = this.GetCallCycles().SelectMany(x => CartesianProduct(this.GetEdges(x).Reverse()));
                foreach (var cycle in cycles)
                {
                    if (!VerifyCycle(cycle.ToArray()))
                    {
                        foreach (var edge in cycle)
                        {
                            diagnostics.Add(Tuple.Create(
                                edge.FileName,
                                QsCompilerDiagnostic.Error(
                                    Diagnostics.ErrorCode.InvalidCyclicTypeParameterResolution,
                                    Enumerable.Empty<string>(),
                                    edge.Start,
                                    edge.End)));
                        }
                    }
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// A collection of the nodes in the call graph.
        /// </summary>
        public ImmutableHashSet<ICallGraphNode> Nodes => this.dependencies.Keys.ToImmutableHashSet();

        /// <summary>
        /// The number of nodes in the call graph.
        /// </summary>
        public int Count => this.dependencies.Count;

        private void RecordDependency(ICallGraphNode callerKey, ICallGraphNode calledKey, ICallGraphEdge edge)
        {
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
                var newDeps = new Dictionary<ICallGraphNode, ImmutableArray<ICallGraphEdge>>();
                newDeps[calledKey] = ImmutableArray.Create(edge);
                this.dependencies[callerKey] = newDeps;
            }

            // Need to make sure the each dependencies has an entry for each node in the graph, even if node has no dependencies
            if (!this.dependencies.ContainsKey(calledKey))
            {
                this.dependencies[calledKey] = new Dictionary<ICallGraphNode, ImmutableArray<ICallGraphEdge>>();
            }
        }

        // Make the default constructor internal so that only the walker can create call graphs.
        internal CallGraph()
        {
        }

        /// <summary>
        /// Adds a dependency to the call graph using the caller's specialization and
        /// the called specialization's information.
        /// Throws ArgumentNullException if any of the arguments are null, though calledTypeArgs
        /// may have the QsNullable.Null value.
        /// </summary>
        internal void AddDependency(
            QsSpecialization callerSpec,
            QsQualifiedName calledName,
            QsSpecializationKind calledKind,
            QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs,
            TypeParameterResolutions typeParamRes,
            QsPositionInfo positionStart,
            QsPositionInfo positionEnd)
        {
            if (callerSpec == null)
            {
                throw new ArgumentNullException(nameof(callerSpec));
            }

            this.AddDependency(
                callerSpec.Parent,
                callerSpec.Kind,
                callerSpec.TypeArguments,
                calledName,
                calledKind,
                calledTypeArgs,
                typeParamRes,
                callerSpec.SourceFile,
                positionStart,
                positionEnd);
        }

        /// <summary>
        /// Adds a dependency to the call graph using the relevant information from the
        /// caller's specialization and the called specialization.
        /// Throws ArgumentNullException if any of the arguments are null, though callerTypeArgs
        /// and calledTypeArgs may have the QsNullable.Null value.
        /// </summary>
        internal void AddDependency(
            QsQualifiedName callerName,
            QsSpecializationKind callerKind,
            QsNullable<ImmutableArray<ResolvedType>> callerTypeArgs,
            QsQualifiedName calledName,
            QsSpecializationKind calledKind,
            QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs,
            TypeParameterResolutions typeParamRes,
            NonNullable<string> fileName,
            QsPositionInfo positionStart,
            QsPositionInfo positionEnd)
        {
            // Setting TypeArgs to Null because the type specialization is not implemented yet
            var callerKey = new CallGraphNode(callerName, callerKind, QsNullable<ImmutableArray<ResolvedType>>.Null);
            var calledKey = new CallGraphNode(calledName, calledKind, QsNullable<ImmutableArray<ResolvedType>>.Null);

            var edge = new CallGraphEdge(typeParamRes, fileName, positionStart, positionEnd);
            this.RecordDependency(callerKey, calledKey, edge);
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
        public ILookup<ICallGraphNode, ICallGraphEdge> GetDirectDependencies(ICallGraphNode callerNode)
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
        public ILookup<ICallGraphNode, ICallGraphEdge> GetDirectDependencies(QsSpecialization callerSpec)
        {
            if (callerSpec == null)
            {
                throw new ArgumentNullException(nameof(callerSpec));
            }

            return this.GetDirectDependencies(new CallGraphNode(callerSpec.Parent, callerSpec.Kind, callerSpec.TypeArguments));
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
        public ILookup<ICallGraphNode, ICallGraphEdge> GetAllDependencies(ICallGraphNode callerSpec)
        {
            if (callerSpec == null)
            {
                throw new ArgumentNullException(nameof(callerSpec));
            }

            if (!this.dependencies.ContainsKey(callerSpec))
            {
                return EmptyDependency;
            }

            var accum = new Dictionary<ICallGraphNode, ImmutableArray<ICallGraphEdge>>();

            void WalkDependencyTree(ICallGraphNode root, ICallGraphEdge? edgeFromRoot)
            {
                if (this.dependencies.TryGetValue(root, out var next))
                {
                    foreach (var (dependent, edges) in next)
                    {
                        var combinedEdges = edgeFromRoot is null
                            ? edges
                            : edges.Select(e => CallGraphEdge.CombinePathIntoSingleEdge(dependent, e, edgeFromRoot));

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
        public ILookup<ICallGraphNode, ICallGraphEdge> GetAllDependencies(QsSpecialization callerSpec)
        {
            if (callerSpec == null)
            {
                throw new ArgumentNullException(nameof(callerSpec));
            }

            return this.GetAllDependencies(new CallGraphNode(callerSpec.Parent, callerSpec.Kind, callerSpec.TypeArguments));
        }

        /// <summary>
        /// Finds and returns a list of all cycles in the call graph, each one being represented by an array of nodes.
        /// To get the edges between the nodes of a given cycle, use the GetDirectDependencies method.
        /// </summary>
        internal ImmutableArray<ImmutableArray<ICallGraphNode>> GetCallCycles()
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

        /// <summary>
        /// Returns true if the given node is found in the call graph, false otherwise.
        /// Throws ArgumentNullException if argument is null.
        /// </summary>
        public bool ContainsNode(ICallGraphNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return this.dependencies.ContainsKey(node);
        }
    }
}
