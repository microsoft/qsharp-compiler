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
                internal TNode CurrentNode;
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
                /// Adds dependency to the graph from the current callable to the callable referenced by the given identifier.
                /// </summary>
                internal abstract void AddDependency(QsQualifiedName identifier);
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
                    walker.SharedState.CurrentNode = currentRequest;
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
                    this.ExpressionKinds = new ExpressionKindWalker(this);
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

                internal override void AddDependency(QsQualifiedName identifier)
                {
                    var combination = new TypeResolutionCombination(this.ExpTypeParamResolutions.ToArray());
                    var typeRes = combination.CombinedResolutionDictionary.FilterByOrigin(identifier);
                    this.ExpTypeParamResolutions = new List<TypeParameterResolutions>();

                    var referenceRange = DataTypes.Range.Zero;
                    if (this.CurrentStatementOffset.IsValue
                        && this.CurrentExpressionRange.IsValue)
                    {
                        referenceRange = this.CurrentStatementOffset.Item + this.CurrentExpressionRange.Item;
                    }

                    var called = new SimpleCallGraphNode(identifier);
                    this.Graph.AddDependency(this.CurrentNode, called, typeRes, referenceRange);
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

        private static class ConcreteCallGraphWalker
        {
            /// <summary>
            /// Populates the given graph based on the given compilation. Only the compilation's entry points and
            /// those callables that the entry points depend on will be included in the graph.
            /// </summary>
            public static void PopulateConcreteGraph(ConcreteCallGraph graph, QsCompilation compilation)
            {
                var walker = new BuildGraph(graph);
                var entryPointNodes = compilation.EntryPoints.Select(name => new ConcreteCallGraphNode(name, QsSpecializationKind.QsBody, TypeParameterResolutions.Empty));
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

                    var spec = GetSpecializationFromRequest(currentRequest, currentCallable);

                    // The current request must be added before it is processed to prevent
                    // self-references from duplicating on the stack.
                    walker.SharedState.ResolvedNodeSet.Add(currentRequest);
                    walker.SharedState.CurrentNode = currentRequest;
                    walker.Namespaces.OnSpecializationImplementation(spec.Implementation);
                }
            }

            /// <summary>
            /// Returns the specialization that the request is referring to.
            /// </summary>
            private static QsSpecialization GetSpecializationFromRequest(ConcreteCallGraphNode request, QsCallable callable)
            {
                var relevantSpecs = callable.Specializations.Where(s => s.Kind == request.Kind);
                var typeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null;
                if (callable.Signature.TypeParameters.Any())
                {
                    // Convert ParamResolutions from a dictionary to a type argument list
                    typeArgs = QsNullable<ImmutableArray<ResolvedType>>.NewValue(callable.Signature.TypeParameters.Select(typeParam =>
                    {
                        if (typeParam is QsLocalSymbol.ValidName name)
                        {
                            if (request.ParamResolutions.TryGetValue(Tuple.Create(callable.FullName, name.Item), out var res))
                            {
                                return res;
                            }
                            else
                            {
                                throw new ArgumentException($"Couldn't resolve all type parameters for {callable.FullName}");
                            }
                        }
                        else
                        {
                            throw new ArgumentException("Encountered invalid type parameter name during call graph construction");
                        }
                    }).ToImmutableArray());
                    typeArgs = SpecializationBundleProperties.BundleId(typeArgs);
                }
                else if (relevantSpecs.Count() != 1)
                {
                    throw new ArgumentException($"Missing specialization {request.Kind} for {request.CallableName}");
                }

                if (!typeArgs.IsNull)
                {
                    // Finds the correct type specialization for the type arguments of the currentRequest.
                    // The assumption is that upon resolution, these type arguments have been cast to
                    // the type of any explicitly defined ones in the closest matching specialization.
                    var specArgMatches = relevantSpecs.Select(spec =>
                    {
                        if (spec.TypeArguments.IsNull)
                        {
                            return (0, spec);
                        }
                        if (spec.TypeArguments.Item.Count() != typeArgs.Item.Count())
                        {
                            throw new ArgumentException($"Incorrect number of type arguments in request for {request.CallableName}");
                        }
                        var specTypeArgs = spec.TypeArguments.Item.Select(StripPositionInfo.Apply).ToImmutableArray();
                        var mismatch = specTypeArgs.Where((tArg, idx) => !tArg.Resolution.IsMissingType && !tArg.Resolution.Equals(typeArgs.Item[idx])).Any();
                        if (mismatch)
                        {
                            return (-1, spec);
                        }
                        var matches = specTypeArgs.Where((tArg, idx) => !tArg.Resolution.IsMissingType && tArg.Resolution.Equals(typeArgs.Item[idx])).Count();
                        return (matches, spec);
                    });

                    if (!specArgMatches.Any(m => m.Item1 >= 0))
                    {
                        throw new ArgumentException($"Could not find a suitable {request.Kind} specialization for {request.CallableName}");
                    }

                    relevantSpecs = specArgMatches
                        .OrderByDescending(match => match.Item1)
                        .Select(match => match.spec);
                }

                return relevantSpecs.First();
            }

            private class BuildGraph : SyntaxTreeTransformation<TransformationState>
            {
                public BuildGraph(ConcreteCallGraph graph) : base(new TransformationState(graph))
                {
                    this.Namespaces = new NamespaceWalker(this);
                    this.Statements = new BaseCallGraphWalker<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.StatementWalker<TransformationState>(this);
                    this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                    this.Expressions = new BaseCallGraphWalker<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.ExpressionWalker<TransformationState>(this);
                    this.ExpressionKinds = new ExpressionKindWalker(this);
                    this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
                }
            }

            private class TransformationState : BaseCallGraphWalker<ConcreteCallGraph, ConcreteCallGraphNode, ConcreteCallGraphEdge>.TransformationState
            {
                public bool IsInCall = false;
                public bool HasAdjointDependency = false;
                public bool HasControlledDependency = false;

                internal TransformationState(ConcreteCallGraph graph) : base(graph)
                {
                }

                internal override void AddDependency(QsQualifiedName identifier)
                {
                    var combination = new TypeResolutionCombination(this.ExpTypeParamResolutions.Append(this.CurrentNode.ParamResolutions).ToArray());
                    var typeRes = combination.CombinedResolutionDictionary.FilterByOrigin(identifier);
                    this.ExpTypeParamResolutions = new List<TypeParameterResolutions>();

                    var referenceRange = DataTypes.Range.Zero;
                    if (this.CurrentStatementOffset.IsValue
                        && this.CurrentExpressionRange.IsValue)
                    {
                        referenceRange = this.CurrentStatementOffset.Item + this.CurrentExpressionRange.Item;
                    }

                    void AddEdge(QsSpecializationKind kind)
                    {
                        var called = new ConcreteCallGraphNode(identifier, kind, typeRes);
                        this.Graph.AddDependency(this.CurrentNode, called, referenceRange);
                        if (!this.RequestStack.Contains(called) && !this.ResolvedNodeSet.Contains(called))
                        {
                            this.RequestStack.Push(called);
                        }
                    }

                    if (this.IsInCall)
                    {
                        if (this.HasAdjointDependency && this.HasControlledDependency)
                        {
                            AddEdge(QsSpecializationKind.QsControlledAdjoint);
                        }
                        else if (this.HasAdjointDependency)
                        {
                            AddEdge(QsSpecializationKind.QsAdjoint);
                        }
                        else if (this.HasControlledDependency)
                        {
                            AddEdge(QsSpecializationKind.QsControlled);
                        }
                        else
                        {
                            AddEdge(QsSpecializationKind.QsBody);
                        }
                    }
                    else
                    {
                        // The callable is being used in a non-call context, such as being
                        // assigned to a variable or passed as an argument to another callable,
                        // which means it could get a functor applied at some later time.
                        // We're conservative and add all 4 possible kinds.
                        AddEdge(QsSpecializationKind.QsBody);
                        AddEdge(QsSpecializationKind.QsAdjoint);
                        AddEdge(QsSpecializationKind.QsControlled);
                        AddEdge(QsSpecializationKind.QsControlledAdjoint);
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
                    throw new ArgumentException("Encountered unresolved generated specialization while constructing concrete call graph.");
                }
            }

            private class ExpressionKindWalker : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindWalker(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild)
                {
                }

                public override ExpressionKind OnCallLikeExpression(TypedExpression method, TypedExpression arg)
                {
                    var contextInCall = this.SharedState.IsInCall;
                    this.SharedState.IsInCall = true;
                    this.Expressions.OnTypedExpression(method);
                    this.SharedState.IsInCall = contextInCall;
                    this.Expressions.OnTypedExpression(arg);
                    return ExpressionKind.InvalidExpr;
                }

                public override ExpressionKind OnAdjointApplication(TypedExpression ex)
                {
                    this.SharedState.HasAdjointDependency = !this.SharedState.HasAdjointDependency;
                    var result = base.OnAdjointApplication(ex);
                    this.SharedState.HasAdjointDependency = !this.SharedState.HasAdjointDependency;
                    return result;
                }

                public override ExpressionKind OnControlledApplication(TypedExpression ex)
                {
                    var contextControlled = this.SharedState.HasControlledDependency;
                    this.SharedState.HasControlledDependency = true;
                    var result = base.OnControlledApplication(ex);
                    this.SharedState.HasControlledDependency = contextControlled;
                    return result;
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
