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
    /// This transformation walks through the compilation without changing it, building up a call graph as it does.
    /// This call graph is then returned to the user.
    /// </summary>
    public static class BuildCallGraph
    {
        /// <summary>
        /// Builds and returns the call graph for the given callables.
        /// </summary>
        public static SimpleCallGraph CreateSimpleGraph(IEnumerable<QsCallable> callables) => SimpleStuff.CreateSimpleGraph(callables);

        /// <summary>
        /// Runs the transformation on the a compilation without any entry points. This
        /// will produce a call graph that contains all relationships amongst all callables
        /// in the compilation.
        /// </summary>
        public static SimpleCallGraph CreateSimpleGraph(QsCompilation compilation) => SimpleStuff.CreateSimpleGraph(compilation);

        public static SimpleCallGraph CreateTrimmedGraph(QsCompilation compilation) => SimpleStuff.CreateTrimmedGraph(compilation);

        public static ConcreteCallGraph CreateConcreteGraph(QsCompilation compilation) => ConcreteStuff.CreateConcreteGraph(compilation);

        private static class GenericStuff<TGraph, TNode, TEdge>
            where TGraph : BaseCallGraph<TNode, TEdge>, new()
            where TNode : BaseCallGraphNode
            where TEdge : BaseCallGraphEdge
        {
            public abstract class TransformationState
            {
                internal TNode CurrentCallable;
                internal readonly TGraph Graph = new TGraph();
                internal IEnumerable<TypeParameterResolutions> ExpTypeParamResolutions = new List<TypeParameterResolutions>();
                internal QsNullable<Position> CurrentStatementOffset;
                internal QsNullable<DataTypes.Range> CurrentExpressionRange;
                internal readonly Stack<TNode> RequestStack = new Stack<TNode>(); // Used to keep track of the nodes that still need to be walked by the walker.
                internal readonly HashSet<TNode> ResolvedNodeSet = new HashSet<TNode>(); // Used to keep track of the nodes that have already been walked by the walker.

                protected abstract TypeParameterResolutions[] GetTypeParamResolutions();

                protected abstract void PushEdge(QsQualifiedName calledName, TypeParameterResolutions typeParamRes, DataTypes.Range referenceRange);

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

        private static class SimpleStuff
        {
            /// <summary>
            /// Builds and returns the call graph for the given callables.
            /// </summary>
            public static SimpleCallGraph CreateSimpleGraph(IEnumerable<QsCallable> callables)
            {
                var walker = new BuildGraph();
                foreach (var callable in callables)
                {
                    walker.Namespaces.OnCallableDeclaration(callable);
                }
                return walker.SharedState.Graph;
            }

            /// <summary>
            /// Runs the transformation on the a compilation without any entry points. This
            /// will produce a call graph that contains all relationships amongst all callables
            /// in the compilation.
            /// </summary>
            public static SimpleCallGraph CreateSimpleGraph(QsCompilation compilation)
            {
                var walker = new BuildGraph();
                walker.OnCompilation(compilation);
                return walker.SharedState.Graph;
            }

            public static SimpleCallGraph CreateTrimmedGraph(QsCompilation compilation)
            {
                var walker = new BuildGraph();
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

                return walker.SharedState.Graph;
            }

            private class BuildGraph : SyntaxTreeTransformation<TransformationState>
            {
                public BuildGraph() : base(new TransformationState())
                {
                    this.Namespaces = new NamespaceWalker(this);
                    this.Statements = new GenericStuff<SimpleCallGraph, SimpleCallGraphNode, SimpleCallGraphEdge>.StatementWalker<TransformationState>(this);
                    this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                    this.Expressions = new GenericStuff<SimpleCallGraph, SimpleCallGraphNode, SimpleCallGraphEdge>.ExpressionWalker<TransformationState>(this);
                    this.ExpressionKinds = new GenericStuff<SimpleCallGraph, SimpleCallGraphNode, SimpleCallGraphEdge>.ExpressionKindWalker<TransformationState>(this);
                    this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
                }
            }

            private class TransformationState : GenericStuff<SimpleCallGraph, SimpleCallGraphNode, SimpleCallGraphEdge>.TransformationState
            {
                // Flag indicating if the call graph is being limited to only include callables that are related to entry points.
                internal bool WithTrimming = false;

                protected override TypeParameterResolutions[] GetTypeParamResolutions() => this.ExpTypeParamResolutions.ToArray();

                /// <summary>
                /// Adds an edge from the current caller to the called node to the call graph.
                /// </summary>
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

        private static class ConcreteStuff
        {
            public static ConcreteCallGraph CreateConcreteGraph(QsCompilation compilation)
            {
                var walker = new BuildGraph();
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

                return walker.SharedState.Graph;
            }

            private class BuildGraph : SyntaxTreeTransformation<TransformationState>
            {
                public BuildGraph() : base(new TransformationState())
                {
                    this.Namespaces = new NamespaceTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                    this.Statements = new GenericStuff<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.StatementWalker<TransformationState>(this);
                    this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                    this.Expressions = new GenericStuff<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.ExpressionWalker<TransformationState>(this);
                    this.ExpressionKinds = new GenericStuff<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.ExpressionKindWalker<TransformationState>(this);
                    this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
                }
            }

            private class TransformationState : GenericStuff<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.TransformationState
            {
                protected override TypeParameterResolutions[] GetTypeParamResolutions() =>
                    this.ExpTypeParamResolutions.Append(this.CurrentCallable.ParamResolutions).ToArray();

                /// <summary>
                /// Adds an edge from the current caller to the called node to the call graph.
                /// </summary>
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
