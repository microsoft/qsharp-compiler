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


// ToDo: Review access modifiers

namespace Microsoft.Quantum.QsCompiler.Transformations
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

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
    public class JohnsonCycleFind
    {
        private Stack<(HashSet<int> SCC, int MinNode)> SccStack = new Stack<(HashSet<int> SCC, int MinNode)>();

        /// <summary>
        /// Johnson's algorithm for finding all cycles in a graph.
        /// This returns a list of cycles, each represented as a list of nodes. Nodes
        /// for this algorithm are integers. The cycles are guaranteed to not contain
        /// any duplicates and the last node in the list is assumed to be connected
        /// to the first.
        /// </summary>
        public List<List<int>> GetAllCyclesStack(Dictionary<int, List<int>> graph)
        {
            // Possible Optimization: Each SCC could be processed in parallel.

            // Possible Optimization: Getting the subgraph for an SCC only needs
            // to consider the parent graph, not the full graph. A recursive strategy
            // may be better than using a stack object.

            var cycles = new List<List<int>>();

            PushSCCsFromGraph(graph);
            while (SccStack.Any())
            {
                var (scc, startNode) = SccStack.Pop();
                var subGraph = GetSubGraphLimitedToNodes(graph, scc);
                cycles.AddRange(GetSccCycles(subGraph, startNode));

                subGraph.Remove(startNode);
                foreach (var (_, children) in subGraph)
                {
                    children.Remove(startNode);
                }

                PushSCCsFromGraph(subGraph);
            }

            return cycles;
        }

        public List<List<int>> GetAllCyclesRecursive(Dictionary<int, List<int>> graph)
        {
            // Possible Optimization: Each SCC could be processed in parallel.

            var cycles = new List<List<int>>();

            var sccs = TarjanSCC(graph).OrderByDescending(x => x.MinNode);

            foreach (var (scc, startNode) in sccs)
            {
                var subGraph = GetSubGraphLimitedToNodes(graph, scc);
                cycles.AddRange(GetSccCycles(subGraph, startNode));

                subGraph.Remove(startNode);
                foreach (var (_, children) in subGraph)
                {
                    children.Remove(startNode);
                }

                cycles.AddRange(GetAllCyclesRecursive(subGraph));
            }

            return cycles;
        }

        public List<List<int>> GetAllCyclesLINQ(Dictionary<int, List<int>> graph)
        {
            // Possible Optimization: Each SCC could be processed in parallel.

            var cycles = new List<List<int>>();

            var sccs = TarjanSCC(graph).OrderByDescending(x => x.MinNode);

            return sccs
                .SelectMany(item =>
                {
                    var cycles = new List<List<int>>();
                    var (scc, startNode) = item;
                    var subGraph = GetSubGraphLimitedToNodes(graph, scc);
                    cycles.AddRange(GetSccCycles(subGraph, startNode));

                    subGraph.Remove(startNode);
                    foreach (var (_, children) in subGraph)
                    {
                        children.Remove(startNode);
                    }

                    cycles.AddRange(GetAllCyclesLINQ(subGraph));
                    return cycles;
                })
                .ToList();
        }

        public List<List<int>> GetAllCyclesParallel(Dictionary<int, List<int>> graph)
        {
            var cycles = new List<List<int>>();

            var sccs = TarjanSCC(graph).OrderByDescending(x => x.MinNode);

            return sccs
                .AsParallel()
                .SelectMany(item =>
                {
                    var cycles = new List<List<int>>();
                    var (scc, startNode) = item;
                    var subGraph = GetSubGraphLimitedToNodes(graph, scc);
                    cycles.AddRange(GetSccCycles(subGraph, startNode));

                    subGraph.Remove(startNode);
                    foreach (var (_, children) in subGraph)
                    {
                        children.Remove(startNode);
                    }

                    cycles.AddRange(GetAllCyclesParallel(subGraph));
                    return cycles;
                })
                .ToList();
        }

        /// <summary>
        /// Uses Tarjan's algorithm for finding strongly-connected components, or SCCs of a
        /// graph to get all SCC, orders them by their node with the smallest id, and pushes
        /// them to the SCC stack.
        /// </summary>
        private void PushSCCsFromGraph(Dictionary<int, List<int>> graph)
        {
            var sccs = TarjanSCC(graph).OrderByDescending(x => x.MinNode);
            foreach (var scc in sccs)
            {
                SccStack.Push(scc);
            }
        }

        /// <summary>
        /// Gets a subgraph of the input graph where each node in the subgraph is found in the given hash set of nodes.
        /// </summary>
        private Dictionary<int, List<int>> GetSubGraphLimitedToNodes(Dictionary<int, List<int>> inputGraph, HashSet<int> subGraphNodes) =>
            inputGraph
                .Where(kvp => subGraphNodes.Contains(kvp.Key)) // filter the keys
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Where(dep => subGraphNodes.Contains(dep)).ToList()); // filter the adjacency lists

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
        private List<(HashSet<int> SCC, int MinNode)> TarjanSCC(Dictionary<int, List<int>> inputGraph)
        {
            var index = 0; // The index represents the order in which the nodes are discovered by Tarjan's algorithm.
                           // This is necessarily separate from the node's id value.
            var nodeStack = new Stack<int>();
            var nodeInfo = new Dictionary<int, (int Index, int LowLink, bool OnStack)>();
            var SCCs = new List<(HashSet<int> SCC, int MinNode)>();

            void setMinLowLink(int node, int potentialMin)
            {
                var vInfo = nodeInfo[node];
                if (vInfo.LowLink > potentialMin)
                {
                    vInfo.LowLink = potentialMin;
                    nodeInfo[node] = vInfo;
                }
            }

            void strongconnect(int node)
            {
                // Set the depth index for node to the smallest unused index
                nodeStack.Push(node);
                nodeInfo[node] = (index, index, true);
                index += 1;

                // Consider children of node
                foreach (var child in inputGraph[node])
                {
                    if (!nodeInfo.ContainsKey(child))
                    {
                        // Child has not yet been visited; recurse on it
                        strongconnect(child);
                        setMinLowLink(node, nodeInfo[child].LowLink);
                    }
                    else if (nodeInfo[child].OnStack)
                    {
                        // Child is in stack and hence in the current SCC
                        // If child is not in stack, then (node, child) is an edge pointing to an SCC already found and must be ignored
                        // Note: The next line may look odd - but is correct.
                        // It says child.index not child.lowlink; that is deliberate and from the original paper
                        setMinLowLink(node, nodeInfo[child].Index);
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

                    } while (node != nodeInScc);
                    SCCs.Add((scc, minNode));
                }
            }

            foreach (var node in inputGraph.Keys)
            {
                if (!nodeInfo.ContainsKey(node))
                {
                    strongconnect(node);
                }
            }

            return SCCs;
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
        private List<List<int>> GetSccCycles(Dictionary<int, List<int>> intputSCC, int startNode)
        {
            var cycles = new List<List<int>>();
            var blockedSet = new HashSet<int>();
            var blockedMap = new Dictionary<int, HashSet<int>>();
            var nodeStack = new Stack<int>();

            void unblock(int node)
            {
                if (blockedSet.Remove(node) && blockedMap.TryGetValue(node, out var nodesToUnblock))
                {
                    blockedMap.Remove(node);
                    foreach (var n in nodesToUnblock)
                    {
                        unblock(n);
                    }
                }
            }

            bool populateCycles(int currNode)
            {
                var foundCycle = false;
                nodeStack.Push(currNode);
                blockedSet.Add(currNode);

                foreach (var child in intputSCC[currNode])
                {
                    if (child == startNode)
                    {
                        foundCycle = true;
                        cycles.Add(nodeStack.Reverse().ToList());
                    }
                    else if (!blockedSet.Contains(child))
                    {
                        foundCycle |= populateCycles(child);
                    }
                }

                nodeStack.Pop();

                if (foundCycle)
                {
                    unblock(currNode);
                }
                else
                {
                    // Mark currNode as being blocked on each of its children
                    // If any of currNode's children unblock, currNode will unblock
                    foreach (var child in intputSCC[currNode])
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

                return foundCycle;
            }

            populateCycles(startNode);

            return cycles;
        }
    }

    /// <summary>
    /// Struct containing information that exists on edges in a call graph.
    /// </summary>
    public struct CallGraphEdge
    {
        public ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> ParamResolutions;
    }

    /// <summary>
    /// Struct containing information that exists on nodes in a call graph.
    /// </summary>
    public struct CallGraphNode
    {
        public QsQualifiedName CallableName;
        public QsSpecializationKind Kind;
        public QsNullable<ImmutableArray<ResolvedType>> TypeArgs;
    }

    /// Class used to track call graph of a compilation
    public class CallGraph
    {
        // TODO:
        // This is the method that should be invoked to verify cycles of interest,
        // i.e. where each callable in the cycle is type parametrized.
        // It should probably generate diagnostics; I'll add the doc comment once its use is fully defined.
        internal static bool VerifyCycle(CallGraphNode rootNode, params CallGraphEdge[] edges)
        {
            var parent = rootNode.CallableName;
            var validResolution = TryCombineTypeResolutions(parent, out var combined, edges.Select(edge => edge.ParamResolutions).ToArray());
            var resolvedToConcrete = combined.Values.All(res => !(res.Resolution is ResolvedTypeKind.TypeParameter tp) || tp.Item.Origin.Equals(parent));
            return validResolution && resolvedToConcrete;
            //var isClosedCycle = validCycle && combined.Values.Any(res => res.Resolution is ResolvedTypeKind.TypeParameter tp && EqualsParent(tp.Item.Origin));
            // TODO: check that monomorphization correctly processes closed cycles - meaning add a test...
        }

        /// <summary>
        /// Combines subsequent concretions as part of a nested expression, or concretions as part of a cycle in the call graph,
        /// into a single dictionary containing the resolution for the type parameters of the specified parent callable.
        /// The given resolutions are expected to be ordered starting with the dictionary containing the initial mapping for the
        /// type parameters of the specified parent callable (the "innermost resolutions"). This mapping may potentially be to
        /// type parameters of other callables, which are then further concretized by subsequent resolutions.
        /// Returns the constructed dictionary as out parameter. Returns true if the combination of the given resolutions is valid,
        /// i.e. if there are no conflicting resolutions and type parameters of the parent callables are uniquely resolved
        /// to either a concrete type, a type parameter of another callable, or themselves.
        /// Throws an ArgumentNullException if the given parent is null.
        /// Throws an ArgumentException if the given resolutions imply that type parameters from multiple callables are
        /// simultaneously treated as concrete types.
        /// NOTE: This routine prioritizes the verifications to ensure the correctness of the resolution over performance.
        /// </summary>
        public static bool TryCombineTypeResolutions
            (QsQualifiedName parent, out ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> combined,
            params ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>[] resolutions)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            var combinedBuilder = ImmutableDictionary.CreateBuilder<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>();
            var success = true;

            static Tuple<QsQualifiedName, NonNullable<string>> AsTypeResolutionKey(QsTypeParameter tp) => Tuple.Create(tp.Origin, tp.TypeName);
            static bool ResolutionToTypeParameter(Tuple<QsQualifiedName, NonNullable<string>> typeParam, ResolvedType res) =>
                res.Resolution is ResolvedTypeKind.TypeParameter tp && tp.Item.Origin.Equals(typeParam.Item1) && tp.Item.TypeName.Equals(typeParam.Item2);

            // Returns true if the given resolution for the given key constrains the type parameter
            // by mapping it to a different type parameter belonging to the same callable.
            bool InconsistentResolutionToNative(Tuple<QsQualifiedName, NonNullable<string>> key, ResolvedType resolution)
            {
                var resolutionToTypeParam = resolution.Resolution as ResolvedTypeKind.TypeParameter;
                var isResolutionToNative = resolutionToTypeParam != null && resolutionToTypeParam.Item.Origin.Equals(parent);
                return isResolutionToNative
                    // We can omit this check as long as combinedBuilder only ever contains native type parameters:
                    // && key.Item1.Equals(parent)
                    && key.Item2.Value != resolutionToTypeParam.Item.TypeName.Value;
            }

            foreach (var resolution in resolutions)
            {
                // Contains a lookup of all the keys in the combined resolutions whose value needs to be updated
                // if a certain type parameter is resolved by the currently processed dictionary.
                var mayBeReplaced = combinedBuilder
                    .Where(kv => kv.Value.Resolution.IsTypeParameter)
                    .ToLookup(
                        kv => AsTypeResolutionKey(((ResolvedTypeKind.TypeParameter)kv.Value.Resolution).Item),
                        entry => entry.Key);

                // We need to ensure that the mappings for external type parameters are processed first,
                // to cover an edge case that would otherwise be indicated as a conflicting resolution.
                foreach (var entry in resolution.Where(entry => mayBeReplaced.Contains(entry.Key)))
                {
                    // resolution of an external type parameter that is currently listed as value in the combined type resolution dictionary
                    foreach (var keyInCombined in mayBeReplaced[entry.Key])
                    {
                        // If one of the values is a type parameter from the parent callable,
                        // but it isn't mapped to itself then the combined resolution is invalid.
                        success = success && !InconsistentResolutionToNative(keyInCombined, entry.Value);
                        combinedBuilder[keyInCombined] = entry.Value;
                    }
                }

                // resolution of a type parameter that belongs to the parent callable
                foreach (var entry in resolution.Where(entry => entry.Key.Item1.Equals(parent)))
                {
                    // A native type parameter cannot be resolved to another native type parameter, since this would constrain them.
                    success = success && !InconsistentResolutionToNative(entry.Key, entry.Value);
                    // Check that there is no conflicting resolution already defined.
                    var conflictingResolutionExists = combinedBuilder.TryGetValue(entry.Key, out var current)
                        && !current.Equals(entry.Value) && !ResolutionToTypeParameter(entry.Key, current);
                    success = success && !conflictingResolutionExists;
                    combinedBuilder[entry.Key] = entry.Value;
                }

                if (resolution.Any(entry => !mayBeReplaced.Contains(entry.Key) && !entry.Key.Item1.Equals(parent)))
                {
                    // It does not make sense to support this case, since there is no valid context in which type parameters
                    // belonging to multiple callables can/should be treated as concrete types simultaneously.
                    throw new ArgumentException("attempting to define resolution for type parameter that does not belong to parent callable");
                }
            }

            combined = combinedBuilder.ToImmutable();
            QsCompilerError.Verify(combined.Keys.All(key => key.Item1.Equals(parent)), "for type parameter that does not belong to parent callable");
            return success;
        }

        /// <summary>
        /// This is a dictionary mapping source nodes to information about target nodes. This information is represented
        /// by a dictionary mapping target node to the edges pointing from the source node to the target node.
        /// </summary>
        private readonly Dictionary<CallGraphNode, Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>> _Dependencies =
            new Dictionary<CallGraphNode, Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>>();

        private QsNullable<ImmutableArray<ResolvedType>> RemovePositionFromTypeArgs(QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
            tArgs.IsValue
            ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(tArgs.Item.Select(StripPositionInfo.Apply).ToImmutableArray())
            : tArgs;

        private void RecordDependency(CallGraphNode callerKey, CallGraphNode calledKey, CallGraphEdge edge)
        {
            if (_Dependencies.TryGetValue(callerKey, out var deps))
            {
                deps[calledKey] = deps.TryGetValue(calledKey, out var edges)
                    ? edges.Add(edge)
                    : ImmutableArray.Create(edge);
            }
            else
            {
                var newDeps = new Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>();
                newDeps[calledKey] = ImmutableArray.Create(edge);
                _Dependencies[callerKey] = newDeps;
            }

            // Need to make sure the each dependencies has an entry for each node in the graph, even if node has no dependencies
            if (!_Dependencies.ContainsKey(calledKey))
            {
                _Dependencies[calledKey] = new Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>();
            }
        }

        /// <summary>
        /// Adds a dependency to the call graph using the caller's specialization and the called specialization's information.
        /// </summary>
        public void AddDependency(QsSpecialization callerSpec, QsQualifiedName calledName, QsSpecializationKind calledKind, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs, CallGraphEdge edge) =>
            AddDependency(
                callerSpec.Parent, callerSpec.Kind, callerSpec.TypeArguments,
                calledName, calledKind, calledTypeArgs,
                edge);

        /// <summary>
        /// Adds a dependency to the call graph using the relevant information from the caller's specialization and the called specialization.
        /// </summary>
        public void AddDependency(
            QsQualifiedName callerName, QsSpecializationKind callerKind, QsNullable<ImmutableArray<ResolvedType>> callerTypeArgs,
            QsQualifiedName calledName, QsSpecializationKind calledKind, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs,
            CallGraphEdge edge)
        {
            // Setting TypeArgs to Null because the type specialization is not implemented yet
            var callerKey = new CallGraphNode { CallableName = callerName, Kind = callerKind, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null };
            var calledKey = new CallGraphNode { CallableName = calledName, Kind = calledKind, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null };
            RecordDependency(callerKey, calledKey, edge);
        }

        /// <summary>
        /// Returns all specializations that are used directly within the given caller, whether they are
        /// called, partially applied, or assigned. Each key in the returned dictionary represents a
        /// specialization that is used by the caller. Each value in the dictionary is an array of edges
        /// representing all the different ways the given caller specialization took a dependency on the
        /// specialization represented by the associated key.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetDirectDependencies(CallGraphNode callerSpec) =>
            _Dependencies.GetValueOrDefault(callerSpec, new Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>());

        /// <summary>
        /// Returns all specializations that are used directly within the given caller, whether they are
        /// called, partially applied, or assigned. Each key in the returned dictionary represents a
        /// specialization that is used by the caller. Each value in the dictionary is an array of edges
        /// representing all the different ways the given caller specialization took a dependency on the
        /// specialization represented by the associated key.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetDirectDependencies(QsSpecialization callerSpec) =>
            GetDirectDependencies(new CallGraphNode { CallableName = callerSpec.Parent, Kind = callerSpec.Kind, TypeArgs = RemovePositionFromTypeArgs(callerSpec.TypeArguments) });

        // ToDo: this method needs a way of resolving type parameters before it can be completed
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetAllDependencies(CallGraphNode callerSpec)
        {
            throw new NotImplementedException();

            //HashSet<(CallGraphNode, CallGraphEdge)> WalkDependencyTree(CallGraphNode root, HashSet<(CallGraphNode, CallGraphEdge)> accum, DependencyType parentDepType)
            //{
            //    if (_Dependencies.TryGetValue(root, out var next))
            //    {
            //        foreach (var k in next)
            //        {
            //            // Get the maximum type of dependency between the parent dependency type and the current dependency type
            //            var maxDepType = k.Item2.CompareTo(parentDepType) > 0 ? k.Item2 : parentDepType;
            //            if (accum.Add((k.Item1, maxDepType)))
            //            {
            //                // ToDo: this won't work once Type specialization are implemented
            //                var noTypeParams = new CallGraphNode { CallableName = k.Item1.CallableName, Kind = k.Item1.Kind, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null };
            //                WalkDependencyTree(noTypeParams, accum, maxDepType);
            //            }
            //        }
            //    }
            //
            //    return accum;
            //}
            //
            //return WalkDependencyTree(callerSpec, new HashSet<(CallGraphNode, DependencyType)>(), DependencyType.NoTypeParameters).ToImmutableArray();
        }

        /// <summary>
        /// Returns all specializations that are used directly or indirectly within the given caller,
        /// whether they are called, partially applied, or assigned. Each key in the returned dictionary
        /// represents a specialization that is used by the caller. Each value in the dictionary is an
        /// array of edges representing all the different ways the given caller specialization took a
        /// dependency on the specialization represented by the associated key.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetAllDependencies(QsSpecialization callerSpec) =>
            GetAllDependencies(new CallGraphNode { CallableName = callerSpec.Parent, Kind = callerSpec.Kind, TypeArgs = RemovePositionFromTypeArgs(callerSpec.TypeArguments) });

        /// <summary>
        /// Finds and returns a list of all cycles in the call graph, each one being represented by an array of nodes.
        /// To get the edges between the nodes of a given cycle, use the GetDirectDependencies method.
        /// </summary>
        public List<ImmutableArray<CallGraphNode>> GetCallCycles()
        {
            var indexToNode = _Dependencies.Keys.ToImmutableArray();
            var nodeToIndex = indexToNode.Select((v, i) => (v, i)).ToImmutableDictionary(kvp => kvp.v, kvp => kvp.i);
            var graph = indexToNode
                .Select((v, i) => (v, i))
                .ToDictionary(kvp => kvp.i,
                    kvp => _Dependencies[kvp.v].Keys
                        .Select(dep => nodeToIndex[dep])
                        .ToList());

            var cycles = new JohnsonCycleFind().GetAllCyclesLINQ(graph);
            return cycles.Select(cycle => cycle.Select(index => indexToNode[index]).ToImmutableArray()).ToList();
        }
    }

    /// <summary>
    /// This transformation walks through the compilation without changing it, building up a call graph as it does.
    /// This call graph is then returned to the user.
    /// </summary>
    public static class BuildCallGraph
    {
        public static CallGraph Apply(QsCompilation compilation)
        {
            var walker = new BuildGraph();
            foreach (var ns in compilation.Namespaces)
            {
                walker.Namespaces.OnNamespace(ns);
            }
            return walker.SharedState.graph;
        }

        private class BuildGraph : SyntaxTreeTransformation<TransformationState>
        {
            public BuildGraph() : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }
        }

        private class TransformationState
        {
            internal QsSpecialization spec;

            internal bool inCall = false;
            internal bool hasAdjointDependency = false;
            internal bool hasControlledDependency = false;

            internal CallGraph graph = new CallGraph();
        }

        private class NamespaceTransformation : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec)
            {
                SharedState.spec = spec;
                return base.OnSpecializationDeclaration(spec);
            }
        }

        private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
        {
            public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override ExpressionKind OnCallLikeExpression(TypedExpression method, TypedExpression arg)
            {
                var contextInCall = SharedState.inCall;
                SharedState.inCall = true;
                this.Expressions.OnTypedExpression(method);
                SharedState.inCall = contextInCall;
                this.Expressions.OnTypedExpression(arg);
                return ExpressionKind.InvalidExpr;
            }

            public override ExpressionKind OnAdjointApplication(TypedExpression ex)
            {
                SharedState.hasAdjointDependency = !SharedState.hasAdjointDependency;
                var result = base.OnAdjointApplication(ex);
                SharedState.hasAdjointDependency = !SharedState.hasAdjointDependency;
                return result;
            }

            public override ExpressionKind OnControlledApplication(TypedExpression ex)
            {
                var contextControlled = SharedState.hasControlledDependency;
                SharedState.hasControlledDependency = true;
                var result = base.OnControlledApplication(ex);
                SharedState.hasControlledDependency = contextControlled;
                return result;
            }

            public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.GlobalCallable global)
                {
                    // Type arguments need to be resolved for the whole expression to be accurate
                    // ToDo: this needs adaption if we want to support type specializations
                    var typeArgs = tArgs;

                    // ToDo: Type argument dictionaries need to be resolved and set here
                    var edge = new CallGraphEdge { };

                    if (SharedState.inCall)
                    {
                        var kind = QsSpecializationKind.QsBody;
                        if (SharedState.hasAdjointDependency && SharedState.hasControlledDependency)
                        {
                            kind = QsSpecializationKind.QsControlledAdjoint;
                        }
                        else if (SharedState.hasAdjointDependency)
                        {
                            kind = QsSpecializationKind.QsAdjoint;
                        }
                        else if (SharedState.hasControlledDependency)
                        {
                            kind = QsSpecializationKind.QsControlled;
                        }

                        SharedState.graph.AddDependency(SharedState.spec, global.Item, kind, typeArgs, edge);
                    }
                    else
                    {
                        // The callable is being used in a non-call context, such as being
                        // assigned to a variable or passed as an argument to another callable,
                        // which means it could get a functor applied at some later time.
                        // We're conservative and add all 4 possible kinds.
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsBody, typeArgs, edge);
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsControlled, typeArgs, edge);
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsAdjoint, typeArgs, edge);
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsControlledAdjoint, typeArgs, edge);
                    }
                }

                return ExpressionKind.InvalidExpr;
            }
        }
    }
}
