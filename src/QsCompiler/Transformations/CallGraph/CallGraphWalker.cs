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
    using GraphBuilder = CallGraphBuilder<CallGraphNode, CallGraphEdge>;
    using Range = DataTypes.Range;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType>;

    internal static partial class BuildCallGraph
    {
        /// <summary>
        /// Populates the given graph based on the given callables.
        /// </summary>
        public static void PopulateGraph(GraphBuilder graph, IEnumerable<QsCallable> callables) => CallGraphWalker.PopulateGraph(graph, callables);

        /// <summary>
        /// Populates the given graph based on the given compilation. This will produce a call graph that
        /// contains all relationships amongst all callables in the compilation.
        /// </summary>
        public static void PopulateGraph(GraphBuilder graph, QsCompilation compilation) => CallGraphWalker.PopulateGraph(graph, compilation);

        /// <summary>
        /// Populates the given graph based on the given compilation. Only the compilation's entry points and
        /// those callables that the entry points depend on will be included in the graph.
        /// </summary>
        public static void PopulateTrimmedGraph(GraphBuilder graph, QsCompilation compilation) => CallGraphWalker.PopulateTrimmedGraph(graph, compilation);

        private static class CallGraphWalker
        {
            /// <summary>
            /// Populates the given graph based on the given callables.
            /// </summary>
            public static void PopulateGraph(GraphBuilder graph, IEnumerable<QsCallable> callables)
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
            public static void PopulateGraph(GraphBuilder graph, QsCompilation compilation)
            {
                var walker = new BuildGraph(graph);
                walker.OnCompilation(compilation);
            }

            /// <summary>
            /// Populates the given graph based on the given compilation. Only the compilation's entry points and
            /// those callables that the entry points depend on will be included in the graph.
            /// </summary>
            public static void PopulateTrimmedGraph(GraphBuilder graph, QsCompilation compilation)
            {
                var walker = new BuildGraph(graph);
                var entryPointNodes = compilation.EntryPoints.Select(name => new CallGraphNode(name));
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
                    walker.SharedState.CurrentNode = currentRequest;
                    walker.Namespaces.OnCallableDeclaration(currentCallable);
                }
            }

            private class BuildGraph : SyntaxTreeTransformation<TransformationState>
            {
                public BuildGraph(GraphBuilder graph) : base(new TransformationState(graph))
                {
                    this.Namespaces = new NamespaceWalker(this);
                    this.Statements = new CallGraphWalkerBase<GraphBuilder, CallGraphNode, CallGraphEdge>.StatementWalker<TransformationState>(this);
                    this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                    this.Expressions = new CallGraphWalkerBase<GraphBuilder, CallGraphNode, CallGraphEdge>.ExpressionWalker<TransformationState>(this);
                    this.ExpressionKinds = new ExpressionKindWalker(this);
                    this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
                }
            }

            private class TransformationState : CallGraphWalkerBase<GraphBuilder, CallGraphNode, CallGraphEdge>.TransformationState
            {
                // Flag indicating if the call graph is being limited to only include callables that are related to entry points.
                internal bool WithTrimming = false;

                internal TransformationState(GraphBuilder graph) : base(graph)
                {
                }

                internal override void AddDependency(QsQualifiedName identifier)
                {
                    if (this.CurrentNode is null)
                    {
                        throw new ArgumentException("AddDependency requires CurrentNode to be non-null.");
                    }

                    var combination = new TypeResolutionCombination(this.ExprTypeParamResolutions);
                    var typeRes = combination.CombinedResolutionDictionary.FilterByOrigin(identifier);
                    this.ExprTypeParamResolutions.Clear();

                    var referenceRange = Range.Zero;
                    if (this.CurrentStatementOffset.IsValue
                        && this.CurrentExpressionRange.IsValue)
                    {
                        referenceRange = this.CurrentStatementOffset.Item + this.CurrentExpressionRange.Item;
                    }

                    var called = new CallGraphNode(identifier);
                    var edge = new CallGraphEdge(typeRes, referenceRange);
                    this.Graph.AddDependency(this.CurrentNode, called, edge);
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
                        var node = new CallGraphNode(c.FullName);
                        this.SharedState.CurrentNode = node;
                        this.SharedState.Graph.AddNode(node);
                    }
                    return base.OnCallableDeclaration(c);
                }
            }

            private class ExpressionKindWalker : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindWalker(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild)
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
    }
}
