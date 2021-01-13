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
                var globals = compilation.Namespaces.GlobalCallableResolutions();
                var walker = new BuildGraph(graph);
                var entryPointNodes = compilation.EntryPoints.SelectMany(name =>
                    GetSpecializationKinds(globals, name).Select(kind =>
                        new ConcreteCallGraphNode(name, kind, TypeParameterResolutions.Empty)));
                foreach (var entryPoint in entryPointNodes)
                {
                    // Make sure all the entry points are added to the graph
                    walker.SharedState.Graph.AddNode(entryPoint);
                    walker.SharedState.RequestStack.Push(entryPoint);
                }

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
                    walker.SharedState.GetSpecializationKinds = (callableName) => GetSpecializationKinds(globals, callableName);
                    walker.Namespaces.OnSpecializationImplementation(spec.Implementation);
                }
            }

            private static IEnumerable<QsSpecializationKind> GetSpecializationKinds(ImmutableDictionary<QsQualifiedName, QsCallable> globals, QsQualifiedName callableName)
            {
                // If there is a call to an unknown callable, throw exception
                if (!globals.TryGetValue(callableName, out QsCallable currentCallable))
                {
                    throw new ArgumentException($"Couldn't find definition for callable: {callableName}");
                }

                return currentCallable.Specializations.Select(x => x.Kind).Distinct();
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
                public BuildGraph(ConcreteGraphBuilder graph)
                    : base(new TransformationState(graph))
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
                public bool IsInCall = false;
                public bool HasAdjointDependency = false;
                public bool HasControlledDependency = false;
                public Func<QsQualifiedName, IEnumerable<QsSpecializationKind>> GetSpecializationKinds = _ => Enumerable.Empty<QsSpecializationKind>();
                private Range lastReferenceRange = Range.Zero; // This is used if a self-inverse generator directive is encountered.

                internal TransformationState(ConcreteGraphBuilder graph)
                    : base(graph)
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
                    this.lastReferenceRange = referenceRange;

                    void AddEdge(QsSpecializationKind kind) => this.AddEdge(identifier, kind, typeRes, referenceRange);

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
                        // We're conservative and add all possible kinds defined for the callable.
                        foreach (var kind in this.GetSpecializationKinds(identifier))
                        {
                            AddEdge(kind);
                        }
                    }
                }

                /// <summary>
                /// Handles adding the dependencies for specializations marked as self-inverse.
                /// </summary>
                internal void AddSelfInverseDependency(QsQualifiedName identifier, QsSpecializationKind targetSpec)
                {
                    if (this.CurrentNode is null)
                    {
                        throw new ArgumentException("AddDependency requires CurrentNode to be non-null.");
                    }

                    var combination = new TypeResolutionCombination(new[] { this.CurrentNode.ParamResolutions });
                    var typeRes = combination.CombinedResolutionDictionary.FilterByOrigin(identifier);

                    this.AddEdge(identifier, targetSpec, typeRes, this.lastReferenceRange);
                }

                private void AddEdge(QsQualifiedName identifier, QsSpecializationKind kind, TypeParameterResolutions typeRes, Range referenceRange)
                {
                    if (this.CurrentNode is null)
                    {
                        throw new ArgumentException("AddEdge requires CurrentNode to be non-null.");
                    }

                    // Add an edge to the specific specialization kind referenced
                    var called = new ConcreteCallGraphNode(identifier, kind, typeRes);
                    var edge = new ConcreteCallGraphEdge(referenceRange);
                    this.Graph.AddDependency(this.CurrentNode, called, edge);

                    // Add all the specializations of the referenced callable to the graph
                    var newNodes = this.GetSpecializationKinds(identifier)
                        .Select(specKind => new ConcreteCallGraphNode(identifier, specKind, typeRes));
                    foreach (var node in newNodes)
                    {
                        if (!this.RequestStack.Contains(node) && !this.ResolvedNodeSet.Contains(node))
                        {
                            this.Graph.AddNode(node);
                            this.RequestStack.Push(node);
                        }
                    }
                }
            }

            private class NamespaceWalker : NamespaceTransformation<TransformationState>
            {
                public NamespaceWalker(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent, TransformationOptions.NoRebuild)
                {
                }

                public override QsGeneratorDirective OnGeneratedImplementation(QsGeneratorDirective directive)
                {
                    if (directive.IsSelfInverse)
                    {
                        if (this.SharedState.CurrentNode is null)
                        {
                            throw new ArgumentException("CurrentNode is expected to be non-null when processing self-adjoint specializations.");
                        }

                        if (this.SharedState.CurrentNode.Kind.IsQsAdjoint)
                        {
                            this.SharedState.AddSelfInverseDependency(this.SharedState.CurrentNode.CallableName, QsSpecializationKind.QsBody);
                        }
                        else if (this.SharedState.CurrentNode.Kind.IsQsControlledAdjoint)
                        {
                            this.SharedState.AddSelfInverseDependency(this.SharedState.CurrentNode.CallableName, QsSpecializationKind.QsControlled);
                        }
                        else
                        {
                            throw new ArgumentException("\"self\" can only be used on adjoint and controlled adjoint specializations.");
                        }

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
                public ExpressionKindWalker(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent, TransformationOptions.NoRebuild)
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
