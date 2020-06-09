// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


// ToDo: Review access modifiers

namespace Microsoft.Quantum.QsCompiler.DependencyAnalysis
{
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    public static class TypeParamUtils
    {
        // TODO:
        // This is the method that should be invoked to verify cycles of interest,
        // i.e. where each callable in the cycle is type parametrized.
        // It should probably generate diagnostics; I'll add the doc comment once its use is fully defined.
        internal static bool VerifyCycle(CallGraphNode rootNode, params CallGraphEdge[] edges)
        {
            var parent = rootNode.CallableName;
            var validResolution = TryCombineTypeResolutionsForTarget(parent, out var combined, edges.Select(edge => edge.ParamResolutions).ToArray());
            var resolvedToConcrete = combined.Values.All(res => !(res.Resolution is ResolvedTypeKind.TypeParameter tp) || tp.Item.Origin.Equals(parent));
            return validResolution && resolvedToConcrete;
            //var isClosedCycle = validCycle && combined.Values.Any(res => res.Resolution is ResolvedTypeKind.TypeParameter tp && EqualsParent(tp.Item.Origin));
            // TODO: check that monomorphization correctly processes closed cycles - meaning add a test...
        }

        /// <summary>
        /// Combines subsequent concretions as part of a nested expression, or concretions as part of a cycle in the call graph,
        /// into a single dictionary containing the resolution for the type parameters of the specified target callable.
        /// The given resolutions are expected to be ordered starting with the dictionary containing the initial mapping for the
        /// type parameters of the specified target callable (the "innermost resolutions"). This mapping may potentially be to
        /// type parameters of other callables, which are then further concretized by subsequent resolutions.
        /// Returns the constructed dictionary as out parameter. Returns true if the combination of the given resolutions is valid,
        /// i.e. if there are no conflicting resolutions and type parameters of the target callable are uniquely resolved
        /// to either a concrete type, a type parameter of another callable, or themselves.
        /// Throws an ArgumentNullException if the given target is null.
        /// NOTE: This routine prioritizes the verifications to ensure the correctness of the resolution over performance.
        /// </summary>
        internal static bool TryCombineTypeResolutionsForTarget(QsQualifiedName target, out TypeParameterResolutions combined, params TypeParameterResolutions[] resolutions)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var success = TryCombineTypeResolutions(out combined, resolutions);
            combined = combined.Where(kvp => kvp.Key.Item1.Equals(target)).ToImmutableDictionary();
            return success;
        }

        /// <summary>
        /// Combines subsequent type parameter resolutions dictionaries into a single dictionary containing the resolution for all
        /// the type parameters found.
        ///
        /// The given resolutions are expected to be ordered such that dictionaries containing type parameters that take a
        /// dependency on other type parameters in other dictionaries appear before those dictionaries they depend on.
        /// I.e., dictionary A depends on dictionary B, so A should come before B. When using this method to resolve
        /// the resolutions of a nested expression, this means that the innermost resolutions should come first, followed by
        /// the next innermost, and so on until the outermost expression is given last.
        ///
        /// Returns the constructed dictionary as out parameter. Returns true if the combination of the given resolutions is valid,
        /// i.e. if there are no conflicting resolutions and type parameters are uniquely resolved to either a concrete type, a
        /// type parameter belonging to a different callable, or themselves.
        /// </summary>
        internal static bool TryCombineTypeResolutions(out TypeParameterResolutions combined, params TypeParameterResolutions[] resolutionDictionaries)
        {
            if (!resolutionDictionaries.Any())
            {
                combined = TypeParameterResolutions.Empty;
                return true;
            }

            var combinedBuilder = ImmutableDictionary.CreateBuilder<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>();
            var success = true;

            static Tuple<QsQualifiedName, NonNullable<string>> AsTypeResolutionKey(QsTypeParameter tp) => Tuple.Create(tp.Origin, tp.TypeName);
            static bool IsSelfResolution(Tuple<QsQualifiedName, NonNullable<string>> typeParam, ResolvedType res) =>
                res.Resolution is ResolvedTypeKind.TypeParameter tp && tp.Item.Origin.Equals(typeParam.Item1) && tp.Item.TypeName.Equals(typeParam.Item2);
            static bool IsConstrictiveResolution(Tuple<QsQualifiedName, NonNullable<string>> typeParam, ResolvedType res) =>
                res.Resolution is ResolvedTypeKind.TypeParameter tp && typeParam.Item1.Equals(tp.Item.Origin) && typeParam.Item2.Value != tp.Item.TypeName.Value;

            foreach (var resolutionDictionary in resolutionDictionaries)
            {
                // Contains a lookup of all the keys in the combined resolutions whose value needs to be updated
                // if a certain type parameter is resolved by the currently processed dictionary.
                var mayBeReplaced = combinedBuilder
                    .Where(kv => kv.Value.Resolution.IsTypeParameter)
                    .ToLookup(
                        // ToDo: Check for composite types that contain type parameter types.
                        kv => AsTypeResolutionKey(((ResolvedTypeKind.TypeParameter)kv.Value.Resolution).Item),
                        entry => entry.Key);

                // Do any replacements for type parameters that may be replaced with values in the current dictionary.
                // This needs to be done first to cover an edge case.
                foreach (var (typeParam, paramRes) in resolutionDictionary.Where(entry => mayBeReplaced.Contains(entry.Key)))
                {
                    // Get all the parameters whose value is dependent on the current resolution's type parameter,
                    // and update their values with this resolution's value.
                    foreach (var keyInCombined in mayBeReplaced[typeParam])
                    {
                        // Check that we are not constricting a type parameter to another type parameter of the same callable.
                        success = success && !IsConstrictiveResolution(keyInCombined, paramRes);
                        combinedBuilder[keyInCombined] = paramRes;
                    }
                }

                // Add all resolutions to the current dictionary.
                foreach (var (typeParam, paramRes) in resolutionDictionary)
                {
                    // Check that we are not constricting a type parameter to another type parameter of the same callable.
                    success = success && !IsConstrictiveResolution(typeParam, paramRes);
                    // Check that there is no conflicting resolution already defined.
                    var conflictingResolutionExists = combinedBuilder.TryGetValue(typeParam, out var current)
                        && !current.Equals(paramRes) && !IsSelfResolution(typeParam, current);
                    success = success && !conflictingResolutionExists;
                    combinedBuilder[typeParam] = paramRes;
                }
            }

            combined = combinedBuilder.ToImmutable();
            return success;
        }
    }

    /// <summary>
    /// Contains the information that exists on edges in a call graph.
    /// The ParamResolutions are expected to be non-null and have all of their position information removed.
    /// The order of the elements of the ParamResolutions will not matter for comparison/hashing.
    /// </summary>
    public class CallGraphEdge
    {
        public TypeParameterResolutions ParamResolutions;

        /// <summary>
        /// Constructor for CallGraphEdge objects.
        /// Strips position info from given resolutions before assigning them to ParamResoluitons
        /// to ensure that the same type parameters will have the same hash.
        /// Throws an ArgumentNullException if paramResolutions is null.
        /// </summary>
        public CallGraphEdge(TypeParameterResolutions paramResolutions)
        {
            if (paramResolutions == null) throw new ArgumentNullException(nameof(paramResolutions));

            // Remove position info from type parameter resolutions
            ParamResolutions = paramResolutions.ToImmutableDictionary(kvp => kvp.Key,
                kvp => StripPositionInfo.Apply(kvp.Value));
        }

        /// <summary>
        /// Determines if the object is the same as the given edge, ignoring the
        /// ordering of key-value pairs in the type parameter dictionaries.
        /// </summary>
        public bool Equals(CallGraphEdge edge)
        {
            return this.ParamResolutions.OrderBy(kvp => kvp.Key).SequenceEqual(edge.ParamResolutions.OrderBy(kvp => kvp.Key));
        }

        /// <summary>
        /// Inserts the edge into the given array of edges if the edge is not already in the array.
        /// Ignores order of key-value pairs in the type parameter dictionaries.
        /// </summary>
        public ImmutableArray<CallGraphEdge> InsertEdge(ImmutableArray<CallGraphEdge> edges)
        {
            var ordered = this.ParamResolutions.OrderBy(kvp => kvp.Key).ToList();

            if (edges == null || edges.Length == 0)
            {
                return ImmutableArray.Create(this);
            }
            else if (edges.Any(e => ordered.SequenceEqual(e.ParamResolutions.OrderBy(kvp => kvp.Key))))
            {
                return edges;
            }
            else
            {
                return edges.Add(this);
            }
        }

        /// <summary>
        /// Creates a call graph edge that represents the combination of several edges that lead to a target node.
        /// The edges should be ordered by their distance to the target node. So the first edge should point directly
        /// to the target node, the next edge should point to the node that has the first edge, the third edge should
        /// point to the node that has the second edge, and so on.
        /// Throws an ArgumentNullException if any of the arguments is null.
        /// </summary>
        public static CallGraphEdge CombineEdges(CallGraphNode targetNode, params CallGraphEdge[] edges)
        {
            if (targetNode == null) throw new ArgumentNullException(nameof(targetNode));
            if (edges == null || edges.Any(e => e == null)) throw new ArgumentNullException(nameof(edges));

            if (TypeParamUtils.TryCombineTypeResolutionsForTarget(targetNode.CallableName, out var combinedEdge, edges.Select(e => e.ParamResolutions).ToArray()))
            {
                return new CallGraphEdge(combinedEdge);
            }
            else
            {
                return new CallGraphEdge(TypeParameterResolutions.Empty);
            }
        }
    }

    /// <summary>
    /// Contains the information that exists on nodes in a call graph.
    /// The CallableName and Kind are expected to be non-null.
    /// </summary>
    public class CallGraphNode : IEquatable<CallGraphNode>
    {
        public readonly QsQualifiedName CallableName;
        public readonly QsSpecializationKind Kind;
        public readonly QsNullable<ImmutableArray<ResolvedType>> TypeArgs;

        /// <summary>
        /// Constructor for CallGraphNode objects.
        /// Strips position info from given type arguments before assigning them to TypeArgs.
        /// Throws an ArgumentNullException if callableName or kind is null.
        /// </summary>
        public CallGraphNode(QsQualifiedName callableName, QsSpecializationKind kind, QsNullable<ImmutableArray<ResolvedType>> typeArgs)
        {
            CallableName = callableName ?? throw new ArgumentNullException(nameof(callableName));
            Kind = kind ?? throw new ArgumentNullException(nameof(kind));
            TypeArgs = typeArgs.IsValue
                ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(typeArgs.Item.Select(StripPositionInfo.Apply).ToImmutableArray())
                : typeArgs;
        }

        public override bool Equals(object obj)
        {
            return obj is CallGraphNode && Equals((CallGraphNode)obj);
        }

        public bool Equals(CallGraphNode other)
        {
            return (CallableName, Kind, TypeArgs).Equals((other.CallableName, other.Kind, other.TypeArgs));
        }

        public override int GetHashCode()
        {
            return (CallableName, Kind, TypeArgs).GetHashCode();
        }
    }

    /// <summary>
    /// Class used to track call graph of a compilation.
    /// </summary>
    public class CallGraph
    {
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
            private Stack<(HashSet<int> SCC, int MinNode)> SccStack = new Stack<(HashSet<int> SCC, int MinNode)>();

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

                PushSCCsFromGraph(graph, graph.Keys.ToHashSet());
                while (SccStack.Any())
                {
                    var (scc, startNode) = SccStack.Pop();
                    cycles.AddRange(GetSccCycles(graph, scc, startNode));
                    scc.Remove(startNode);
                    PushSCCsFromGraph(graph, scc);
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
                var sccs = TarjanSCC(graph, filter).OrderByDescending(x => x.MinNode);
                foreach (var scc in sccs)
                {
                    SccStack.Push(scc);
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
                        if (filter.Contains(child))
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

                foreach (var node in filter)
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
            private List<List<int>> GetSccCycles(Dictionary<int, List<int>> intputSCC, HashSet<int> filter, int startNode)
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
                        if (filter.Contains(child))
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

                populateCycles(startNode);

                return cycles;
            }
        }

        /// <summary>
        /// This is a dictionary mapping source nodes to information about target nodes. This information is represented
        /// by a dictionary mapping target node to the edges pointing from the source node to the target node.
        /// </summary>
        private readonly Dictionary<CallGraphNode, Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>> _Dependencies =
            new Dictionary<CallGraphNode, Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>>();

        private void RecordDependency(CallGraphNode callerKey, CallGraphNode calledKey, CallGraphEdge edge)
        {
            if (_Dependencies.TryGetValue(callerKey, out var deps))
            {
                if (!deps.TryGetValue(calledKey, out var edges))
                {
                    deps[calledKey] = ImmutableArray.Create(edge);
                }
                else
                {
                    deps[calledKey] = edge.InsertEdge(edges);
                }
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
        /// All parameters are expected to be non-null; the QsNullable parameter may take on their associated null value.
        /// Throws ArgumentNullException.
        /// </summary>
        public void AddDependency(QsSpecialization callerSpec, QsQualifiedName calledName, QsSpecializationKind calledKind,
            QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs, TypeParameterResolutions typeParamRes)
        {
            if (callerSpec == null) throw new ArgumentNullException(nameof(callerSpec));

            AddDependency(
                callerSpec.Parent, callerSpec.Kind, callerSpec.TypeArguments,
                calledName, calledKind, calledTypeArgs,
                typeParamRes);
        }

        /// <summary>
        /// Adds a dependency to the call graph using the relevant information from the caller's specialization and the called specialization.
        /// All parameters are expected to be non-null; the QsNullable parameters may take on their associated null value.
        /// Throws ArgumentNullException.
        /// </summary>
        public void AddDependency(
            QsQualifiedName callerName, QsSpecializationKind callerKind, QsNullable<ImmutableArray<ResolvedType>> callerTypeArgs,
            QsQualifiedName calledName, QsSpecializationKind calledKind, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs,
            TypeParameterResolutions typeParamRes)
        {
            if (typeParamRes == null) throw new ArgumentNullException(nameof(typeParamRes));

            // Setting TypeArgs to Null because the type specialization is not implemented yet
            var callerKey = new CallGraphNode(callerName, callerKind, QsNullable<ImmutableArray<ResolvedType>>.Null);
            var calledKey = new CallGraphNode(calledName, calledKind, QsNullable<ImmutableArray<ResolvedType>>.Null);

            var edge = new CallGraphEdge(typeParamRes);
            RecordDependency(callerKey, calledKey, edge);
        }

        /// <summary>
        /// Returns all specializations that are used directly within the given caller, whether they are
        /// called, partially applied, or assigned. Each key in the returned dictionary represents a
        /// specialization that is used by the caller. Each value in the dictionary is an array of edges
        /// representing all the different ways the given caller specialization took a dependency on the
        /// specialization represented by the associated key.
        /// Throws ArgumentNullException if argument is null.
        /// Returns an empty dictionary if the node was found with no dependencies.
        /// Returns null if the node was not found to be in the graph.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetDirectDependencies(CallGraphNode callerSpec)
        {
            if (callerSpec == null) throw new ArgumentNullException(nameof(callerSpec));
            return _Dependencies.GetValueOrDefault(callerSpec, null);
        }

        /// <summary>
        /// Returns all specializations that are used directly within the given caller, whether they are
        /// called, partially applied, or assigned. Each key in the returned dictionary represents a
        /// specialization that is used by the caller. Each value in the dictionary is an array of edges
        /// representing all the different ways the given caller specialization took a dependency on the
        /// specialization represented by the associated key.
        /// Throws ArgumentNullException if argument is null.
        /// Returns an empty dictionary if the node was found with no dependencies.
        /// Returns null if the node was not found to be in the graph.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetDirectDependencies(QsSpecialization callerSpec)
        {
            if (callerSpec == null) throw new ArgumentNullException(nameof(callerSpec));
            return GetDirectDependencies(new CallGraphNode(callerSpec.Parent, callerSpec.Kind, callerSpec.TypeArguments));
        }

        /// <summary>
        /// Returns all specializations that are used directly or indirectly within the given caller,
        /// whether they are called, partially applied, or assigned. Each key in the returned dictionary
        /// represents a specialization that is used by the caller. Each value in the dictionary is an
        /// array of edges representing all the different ways the given caller specialization took a
        /// dependency on the specialization represented by the associated key.
        /// Throws ArgumentNullException if argument is null.
        /// Returns an empty dictionary if the node was found with no dependencies.
        /// Returns null if the node was not found to be in the graph.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetAllDependencies(CallGraphNode callerSpec)
        {
            if (callerSpec == null) throw new ArgumentNullException(nameof(callerSpec));

            if (!_Dependencies.ContainsKey(callerSpec)) return null;

            var accum = new Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>();

            void WalkDependencyTree(CallGraphNode root, CallGraphEdge edgeFromRoot)
            {
                if (_Dependencies.TryGetValue(root, out var next))
                {
                    foreach (var (dependent, edges) in next)
                    {
                        var combinedEdges = edges.Select(e => CallGraphEdge.CombineEdges(dependent, e, edgeFromRoot));

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

            WalkDependencyTree(callerSpec, new CallGraphEdge(TypeParameterResolutions.Empty));

            return accum;
        }

        /// <summary>
        /// Returns all specializations that are used directly or indirectly within the given caller,
        /// whether they are called, partially applied, or assigned. Each key in the returned dictionary
        /// represents a specialization that is used by the caller. Each value in the dictionary is an
        /// array of edges representing all the different ways the given caller specialization took a
        /// dependency on the specialization represented by the associated key.
        /// Throws ArgumentNullException if argument is null.
        /// Returns an empty dictionary if the node was found with no dependencies.
        /// Returns null if the node was not found to be in the graph.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetAllDependencies(QsSpecialization callerSpec)
        {
            if (callerSpec == null) throw new ArgumentNullException(nameof(callerSpec));
            return GetAllDependencies(new CallGraphNode(callerSpec.Parent, callerSpec.Kind, callerSpec.TypeArguments));
        }

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

            var cycles = new JohnsonCycleFind().GetAllCycles(graph);
            return cycles.Select(cycle => cycle.Select(index => indexToNode[index]).ToImmutableArray()).ToList();
        }
    }

}
