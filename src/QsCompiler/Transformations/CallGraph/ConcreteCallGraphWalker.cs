// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
{
    using ConcreteGraphBuilder = CallGraphBuilder<ConcreteCallGraphNode, ConcreteCallGraphEdge>;
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using Range = DataTypes.Range;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType>;

    internal static partial class BuildCallGraph
    {
        /// <summary>
        /// Populates the given graph based on the given compilation. Only the compilation's entry points and
        /// those callables that the entry points depend on will be included in the graph. All Generated
        /// Implementations for specializations should be resolved before calling this. This will throw an
        /// error if a Generated Implementation is encountered.
        /// </summary>
        public static void PopulateConcreteGraph(ConcreteGraphBuilder graph, QsCompilation compilation) => ConcreteCallGraphWalker.PopulateConcreteGraph(graph, compilation);

        private static class ConcreteCallGraphWalker
        {
            /// <summary>
            /// Populates the given graph based on the given compilation. Only the compilation's entry points and
            /// those callables that the entry points depend on will be included in the graph. All Generated
            /// Implementations for specializations should be resolved before calling this, except Self-Inverse,
            /// which is handled by creating a dependency to the appropriate specialization of the same callable.
            /// This will throw an error if a Generated Implementation other than a Self-Inverse is encountered.
            /// </summary>
            public static void PopulateConcreteGraph(ConcreteGraphBuilder graph, QsCompilation compilation)
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
                    walker.SharedState.CurrentNode = currentRequest;
                    walker.Namespaces.OnCallableDeclaration(currentCallable);
                }
            }

            private class BuildGraph : SyntaxTreeTransformation<TransformationState>
            {
                public BuildGraph(ConcreteGraphBuilder graph) : base(new TransformationState(graph))
                {
                    this.Namespaces = new NamespaceWalker(this);
                    this.Statements = new CallGraphWalkerBase<ConcreteGraphBuilder, ConcreteCallGraphNode, ConcreteCallGraphEdge>.StatementWalker<TransformationState>(this);
                    this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                    this.Expressions = new CallGraphWalkerBase<ConcreteGraphBuilder, ConcreteCallGraphNode, ConcreteCallGraphEdge>.ExpressionWalker<TransformationState>(this);
                    this.ExpressionKinds = new ExpressionKindWalker(this);
                    this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
                }
            }

            private class TransformationState : CallGraphWalkerBase<ConcreteGraphBuilder, ConcreteCallGraphNode, ConcreteCallGraphEdge>.TransformationState
            {
                internal TransformationState(ConcreteGraphBuilder graph) : base(graph)
                {
                }

                internal override void AddDependency(QsQualifiedName identifier)
                {
                    if (this.CurrentNode is null)
                    {
                        throw new ArgumentException("AddDependency requires CurrentNode to be non-null.");
                    }

                    var combination = new TypeResolutionCombination(this.ExprTypeParamResolutions.Append(this.CurrentNode.ParamResolutions));
                    var typeRes = combination.CombinedResolutionDictionary.FilterByOrigin(identifier);
                    this.ExprTypeParamResolutions.Clear();

                    var referenceRange = Range.Zero;
                    if (this.CurrentStatementOffset.IsValue
                        && this.CurrentExpressionRange.IsValue)
                    {
                        referenceRange = this.CurrentStatementOffset.Item + this.CurrentExpressionRange.Item;
                    }

                    // Add an edge to the specific specialization kind referenced
                    var called = new ConcreteCallGraphNode(identifier, typeRes);
                    var edge = new ConcreteCallGraphEdge(referenceRange);
                    this.Graph.AddDependency(this.CurrentNode, called, edge);

                    // Keep track of what elements have been processed,
                    // and which elements still need to be processed.
                    if (!this.RequestStack.Contains(called) && !this.ResolvedNodeSet.Contains(called))
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

                public override QsGeneratorDirective OnGeneratedImplementation(QsGeneratorDirective directive)
                {
                    if (directive.IsSelfInverse)
                    {
                        return directive;
                    }
                    else
                    {
                        throw new ArgumentException("Encountered unresolved generated specialization while constructing concrete call graph.");
                    }
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
