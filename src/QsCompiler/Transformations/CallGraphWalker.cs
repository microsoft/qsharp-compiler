// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    /// <summary>
    /// This transformation walks through the compilation without changing it, populating a given call graph as it does.
    /// </summary>
    internal static class BuildCallGraph
    {
        /// <summary>
        /// Populates the given graph based on the given callables.
        /// </summary>
        public static void PopulateSimpleGraph(SimpleCallGraph graph, IEnumerable<QsCallable> callables) => SimpleCallGraphWalker.PopulateSimpleGraph(graph, callables);

        /// <summary>
        /// Populates the given graph based on the given compilation. This will produce a call graph that
        /// contains all relationships amongst all callables in the compilation.
        /// </summary>
        public static void PopulateSimpleGraph(SimpleCallGraph graph, QsCompilation compilation) => SimpleCallGraphWalker.PopulateSimpleGraph(graph, compilation);

        /// <summary>
        /// Populates the given graph based on the given compilation. Only the compilation's entry points and
        /// those callables that the entry points depend on will be included in the graph.
        /// </summary>
        public static void PopulateTrimmedGraph(SimpleCallGraph graph, QsCompilation compilation) => SimpleCallGraphWalker.PopulateTrimmedGraph(graph, compilation);

        /// <summary>
        /// Populates the given graph based on the given compilation. Only the compilation's entry points and
        /// those callables that the entry points depend on will be included in the graph.
        /// </summary>
        public static void PopulateConcreteGraph(ConcreteCallGraph graph, QsCompilation compilation) => ConcreteCallGraphWalker.PopulateConcreteGraph(graph, compilation);

        private static class BaseCallGraphWalker<TGraph, TNode, TEdge>
            where TGraph : CallGraphBase<TNode, TEdge>
            where TNode : CallGraphNodeBase
            where TEdge : CallGraphEdgeBase
        {
            public abstract class TransformationState
            {
                internal TNode CurrentCallable;
                internal readonly TGraph Graph;

                // The type parameter resolutions of the current expression.
                internal IEnumerable<TypeParameterResolutions> ExpTypeParamResolutions = new List<TypeParameterResolutions>();
                internal QsNullable<Position> CurrentStatementOffset;
                internal QsNullable<DataTypes.Range> CurrentExpressionRange;
                internal readonly Stack<TNode> RequestStack = new Stack<TNode>(); // Used to keep track of the nodes that still need to be walked by the walker.
                internal readonly HashSet<TNode> ResolvedNodeSet = new HashSet<TNode>(); // Used to keep track of the nodes that have already been walked by the walker.

                internal TransformationState(TGraph graph)
                {
                    this.Graph = graph;
                }

                /// <summary>
                /// Get the array of type parameter resolutions for AddDependency to process.
                /// </summary>
                protected abstract TypeParameterResolutions[] GetTypeParamResolutions();

                /// <summary>
                /// Adds an edge from the current caller to the called node to the call graph.
                /// </summary>
                protected abstract void PushEdge(QsQualifiedName calledName, TypeParameterResolutions typeParamRes, DataTypes.Range referenceRange);

                /// <summary>
                /// Adds dependency to the graph from the current callable to the callable referenced by the given identifier.
                /// </summary>
                internal void AddDependency(QsQualifiedName identifier)
                {
                    var combination = new TypeResolutionCombination(this.GetTypeParamResolutions());
                    var typeRes = combination.CombinedResolutionDictionary.FilterByOrigin(identifier);
                    this.ExpTypeParamResolutions = new List<TypeParameterResolutions>();

                    var referenceRange = DataTypes.Range.Zero;
                    if (this.CurrentStatementOffset.IsValue
                        && this.CurrentExpressionRange.IsValue)
                    {
                        referenceRange = this.CurrentStatementOffset.Item + this.CurrentExpressionRange.Item;
                    }

                    this.PushEdge(identifier, typeRes, referenceRange);
                }
            }

            public class StatementWalker<TState> : StatementTransformation<TState>
                where TState : TransformationState
            {
                public StatementWalker(SyntaxTreeTransformation<TState> parent) : base(parent, TransformationOptions.NoRebuild)
                {
                }

                public override QsStatement OnStatement(QsStatement stm)
                {
                    this.SharedState.CurrentStatementOffset = stm.Location.IsValue
                        ? QsNullable<Position>.NewValue(stm.Location.Item.Offset)
                        : QsNullable<Position>.Null;
                    return base.OnStatement(stm);
                }
            }

            public class ExpressionWalker<TState> : ExpressionTransformation<TState>
                where TState : TransformationState
            {
                public ExpressionWalker(SyntaxTreeTransformation<TState> parent) : base(parent, TransformationOptions.NoRebuild)
                {
                }

                public override TypedExpression OnTypedExpression(TypedExpression ex)
                {
                    var contextRange = this.SharedState.CurrentExpressionRange;
                    this.SharedState.CurrentExpressionRange = ex.Range;

                    if (ex.TypeParameterResolutions.Any())
                    {
                        this.SharedState.ExpTypeParamResolutions = this.SharedState.ExpTypeParamResolutions.Prepend(ex.TypeParameterResolutions);
                    }
                    var rtrn = base.OnTypedExpression(ex);

                    this.SharedState.CurrentExpressionRange = contextRange;

                    return rtrn;
                }
            }

            public class ExpressionKindWalker<TState> : ExpressionKindTransformation<TState>
                where TState : TransformationState
            {
                public ExpressionKindWalker(SyntaxTreeTransformation<TState> parent) : base(parent, TransformationOptions.NoRebuild)
                {
                }

                public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    if (sym is Identifier.GlobalCallable global)
                    {
                        this.SharedState.AddDependency(global.Item);
                    }

                    return ExpressionKind.InvalidExpr;
                }
            }
        }

        private static class SimpleCallGraphWalker
        {
            /// <summary>
            /// Populates the given graph based on the given callables.
            /// </summary>
            public static void PopulateSimpleGraph(SimpleCallGraph graph, IEnumerable<QsCallable> callables)
            {
                var walker = new BuildGraph(graph);
                foreach (var callable in callables)
                {
                    walker.Namespaces.OnCallableDeclaration(callable);
                }
            }

            /// <summary>
            /// Populates the given graph based on the given compilation. This will produce a call graph that
            /// contains all relationships amongst all callables in the compilation.
            /// </summary>
            public static void PopulateSimpleGraph(SimpleCallGraph graph, QsCompilation compilation)
            {
                var walker = new BuildGraph(graph);
                walker.OnCompilation(compilation);
            }

            /// <summary>
            /// Populates the given graph based on the given compilation. Only the compilation's entry points and
            /// those callables that the entry points depend on will be included in the graph.
            /// </summary>
            public static void PopulateTrimmedGraph(SimpleCallGraph graph, QsCompilation compilation)
            {
                var walker = new BuildGraph(graph);
                var entryPointNodes = compilation.EntryPoints.Select(name => new SimpleCallGraphNode(name));
                walker.SharedState.WithTrimming = true;
                foreach (var entryPoint in entryPointNodes)
                {
                    // Make sure all the entry points are added to the graph
                    walker.SharedState.Graph.AddNode(entryPoint);
                    walker.SharedState.RequestStack.Push(entryPoint);
                }

                var globals = compilation.Namespaces.GlobalCallableResolutions();
                while (walker.SharedState.RequestStack.TryPop(out var currentRequest))
                {
                    // If there is a call to an unknown callable, throw exception
                    if (!globals.TryGetValue(currentRequest.CallableName, out QsCallable currentCallable))
                    {
                        throw new ArgumentException($"Couldn't find definition for callable: {currentRequest.CallableName}");
                    }

                    // The current request must be added before it is processed to prevent
                    // self-references from duplicating on the stack.
                    walker.SharedState.ResolvedNodeSet.Add(currentRequest);
                    walker.SharedState.CurrentCallable = currentRequest;
                    walker.Namespaces.OnCallableDeclaration(currentCallable);
                }
            }

            private class BuildGraph : SyntaxTreeTransformation<TransformationState>
            {
                public BuildGraph(SimpleCallGraph graph) : base(new TransformationState(graph))
                {
                    this.Namespaces = new NamespaceWalker(this);
                    this.Statements = new BaseCallGraphWalker<SimpleCallGraph, SimpleCallGraphNode, SimpleCallGraphEdge>.StatementWalker<TransformationState>(this);
                    this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                    this.Expressions = new BaseCallGraphWalker<SimpleCallGraph, SimpleCallGraphNode, SimpleCallGraphEdge>.ExpressionWalker<TransformationState>(this);
                    this.ExpressionKinds = new BaseCallGraphWalker<SimpleCallGraph, SimpleCallGraphNode, SimpleCallGraphEdge>.ExpressionKindWalker<TransformationState>(this);
                    this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
                }
            }

            private class TransformationState : BaseCallGraphWalker<SimpleCallGraph, SimpleCallGraphNode, SimpleCallGraphEdge>.TransformationState
            {
                // Flag indicating if the call graph is being limited to only include callables that are related to entry points.
                internal bool WithTrimming = false;

                internal TransformationState(SimpleCallGraph graph) : base(graph)
                {
                }

                /// <inheritdoc/>
                protected override TypeParameterResolutions[] GetTypeParamResolutions() => this.ExpTypeParamResolutions.ToArray();

                /// <inheritdoc/>
                protected override void PushEdge(QsQualifiedName calledName, TypeParameterResolutions edgeTypeParamRes, DataTypes.Range referenceRange)
                {
                    var called = new SimpleCallGraphNode(calledName);
                    this.Graph.AddDependency(this.CurrentCallable, called, edgeTypeParamRes, referenceRange);
                    // If we are not processing all elements, then we need to keep track of what elements
                    // have been processed, and which elements still need to be processed.
                    if (this.WithTrimming
                        && !this.RequestStack.Contains(called)
                        && !this.ResolvedNodeSet.Contains(called))
                    {
                        this.RequestStack.Push(called);
                    }
                }
            }

            private class NamespaceWalker : NamespaceTransformation<TransformationState>
            {
                public NamespaceWalker(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild)
                {
                }

                public override QsCallable OnCallableDeclaration(QsCallable c)
                {
                    if (!this.SharedState.WithTrimming)
                    {
                        var node = new SimpleCallGraphNode(c.FullName);
                        this.SharedState.CurrentCallable = node;
                        this.SharedState.Graph.AddNode(node);
                    }
                    return base.OnCallableDeclaration(c);
                }
            }
        }

        private static class ConcreteCallGraphWalker
        {
            /// <summary>
            /// Populates the given graph based on the given compilation. Only the compilation's entry points and
            /// those callables that the entry points depend on will be included in the graph.
            /// </summary>
            public static void PopulateConcreteGraph(ConcreteCallGraph graph, QsCompilation compilation)
            {
                var walker = new BuildGraph(graph);
                var entryPointNodes = compilation.EntryPoints.Select(name => new ConcreteCallGraphNode(name, TypeParameterResolutions.Empty));
                foreach (var entryPoint in entryPointNodes)
                {
                    // Make sure all the entry points are added to the graph
                    walker.SharedState.Graph.AddNode(entryPoint);
                    walker.SharedState.RequestStack.Push(entryPoint);
                }

                var globals = compilation.Namespaces.GlobalCallableResolutions();
                while (walker.SharedState.RequestStack.TryPop(out var currentRequest))
                {
                    // If there is a call to an unknown callable, throw exception
                    if (!globals.TryGetValue(currentRequest.CallableName, out QsCallable currentCallable))
                    {
                        throw new ArgumentException($"Couldn't find definition for callable: {currentRequest.CallableName}");
                    }

                    // The current request must be added before it is processed to prevent
                    // self-references from duplicating on the stack.
                    walker.SharedState.ResolvedNodeSet.Add(currentRequest);
                    walker.SharedState.CurrentCallable = currentRequest;
                    walker.Namespaces.OnCallableDeclaration(currentCallable);
                }
            }

            private class BuildGraph : SyntaxTreeTransformation<TransformationState>
            {
                public BuildGraph(ConcreteCallGraph graph) : base(new TransformationState(graph))
                {
                    this.Namespaces = new NamespaceTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                    this.Statements = new BaseCallGraphWalker<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.StatementWalker<TransformationState>(this);
                    this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                    this.Expressions = new BaseCallGraphWalker<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.ExpressionWalker<TransformationState>(this);
                    this.ExpressionKinds = new BaseCallGraphWalker<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.ExpressionKindWalker<TransformationState>(this);
                    this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
                }
            }

            private class TransformationState : BaseCallGraphWalker<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.TransformationState
            {
                internal TransformationState(ConcreteCallGraph graph) : base(graph)
                {
                }

                /// <inheritdoc/>
                protected override TypeParameterResolutions[] GetTypeParamResolutions() =>
                    this.ExpTypeParamResolutions.Append(this.CurrentCallable.ParamResolutions).ToArray();

                /// <inheritdoc/>
                protected override void PushEdge(QsQualifiedName calledName, TypeParameterResolutions nodeTypeParamRes, DataTypes.Range referenceRange)
                {
                    var called = new ConcreteCallGraphNode(calledName, nodeTypeParamRes);
                    this.Graph.AddDependency(this.CurrentCallable, called, referenceRange);
                    if (!this.RequestStack.Contains(called) && !this.ResolvedNodeSet.Contains(called))
                    {
                        this.RequestStack.Push(called);
                    }
                }
            }
        }
    }
}
