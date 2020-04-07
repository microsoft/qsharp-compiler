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


namespace Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
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
    internal class JohnsonCycleFind
    {
        private Stack<(HashSet<int> SCC, int MinNode)> SccStack = new Stack<(HashSet<int> SCC, int MinNode)>();

        /// <summary>
        /// Johnson's algorithm for finding all cycles in a graph.
        /// This returns a list of cycles, each represented as a list of nodes. Nodes
        /// for this algorithm are integers. The cycles are guaranteed to not contain
        /// any duplicates and the last node in the list is assumed to be connected
        /// to the first.
        /// </summary>
        public List<List<int>> GetAllCycles(Dictionary<int, List<int>> graph)
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

    /// Class used to track call graph of a compilation
    public class CallGraph
    {
        public struct CallGraphEdge
        {
            public ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType> ParamResolutions;
        }

        public struct CallGraphNode
        {
            public QsQualifiedName CallableName;
            public QsSpecializationKind Kind;
            public QsNullable<ImmutableArray<ResolvedType>> TypeArgs;
        }

        /// <summary>
        /// This is a dictionary mapping source nodes to information about target nodes. This information is represented
        /// by a dictionary mapping target node to the edges pointing from the source node to the target node.
        /// </summary>
        private Dictionary<CallGraphNode, Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>> _Dependencies =
            new Dictionary<CallGraphNode, Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>>();

        private QsNullable<ImmutableArray<ResolvedType>> RemovePositionFromTypeArgs(QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
            tArgs.IsValue
            ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(tArgs.Item.Select(x => StripPositionInfo.Apply(x)).ToImmutableArray())
            : tArgs;

        private void RecordDependency(CallGraphNode callerKey, CallGraphNode calledKey, CallGraphEdge edge)
        {
            if (_Dependencies.TryGetValue(callerKey, out var deps))
            {
                if (deps.TryGetValue(calledKey, out var edges))
                {
                    deps[calledKey] = edges.Add(edge);
                }
                else
                {
                    deps[calledKey] = ImmutableArray.Create(edge);
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
            // ToDo: Setting TypeArgs to Null because the type specialization is not implemented yet
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
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetDirectDependencies(CallGraphNode callerSpec)
        {
            if (_Dependencies.TryGetValue(callerSpec, out var deps))
            {
                return deps;
            }
            else
            {
                return new Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>();
            }
        }

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
            return new Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>();

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

            var cycles = new JohnsonCycleFind().GetAllCycles(graph);
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

        private class BuildGraph : SyntaxTreeTransformation<BuildGraph.TransformationState>
        {
            public class TransformationState
            {
                internal QsSpecialization spec;

                internal bool inCall = false;
                internal bool hasAdjointDependency = false;
                internal bool hasControlledDependency = false;

                internal CallGraph graph = new CallGraph();
            }

            public BuildGraph() : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
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

                private ExpressionKind HandleCall(TypedExpression method, TypedExpression arg)
                {
                    var contextInCall = SharedState.inCall;
                    SharedState.inCall = true;
                    this.Expressions.OnTypedExpression(method);
                    SharedState.inCall = contextInCall;
                    this.Expressions.OnTypedExpression(arg);
                    return ExpressionKind.InvalidExpr;
                }

                public override ExpressionKind OnOperationCall(TypedExpression method, TypedExpression arg) => HandleCall(method, arg);

                public override ExpressionKind OnFunctionCall(TypedExpression method, TypedExpression arg) => HandleCall(method, arg);

                public override ExpressionKind OnAdjointApplication(TypedExpression ex)
                {
                    SharedState.hasAdjointDependency = !SharedState.hasAdjointDependency;
                    var rtrn = base.OnAdjointApplication(ex);
                    SharedState.hasAdjointDependency = !SharedState.hasAdjointDependency;
                    return rtrn;
                }

                public override ExpressionKind OnControlledApplication(TypedExpression ex)
                {
                    var contextControlled = SharedState.hasControlledDependency;
                    SharedState.hasControlledDependency = true;
                    var rtrn = base.OnControlledApplication(ex);
                    SharedState.hasControlledDependency = contextControlled;
                    return rtrn;
                }

                public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    if (sym is Identifier.GlobalCallable global)
                    {
                        // ToDo: Type arguments need to be resolved for the whole expression to be accurate, though this will not be needed until type specialization is implemented
                        var typeArgs = tArgs;

                        // ToDo: Type argument dictionaries need to be resolved and set here
                        var edge = new CallGraph.CallGraphEdge { };

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
}
