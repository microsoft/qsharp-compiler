// Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// Static class used to build a call graph. 
    /// </summary>
    public static class BuildCallGraph
    {
        /// <summary>
        /// Walks through the compilation without changing it, building up a call graph as it does.
        /// Returns the built call graph.
        /// May throw an ArgumentExeception or an ArgumentNullException if the given compilation contains invalid or inconsistent items. 
        /// </summary>
        public static CallGraph Apply(QsCompilation compilation)
        {
            var globals = compilation.Namespaces.GlobalCallableResolutions();
            var entryPointNodes = compilation.EntryPoints.Select(name =>
                new CallGraphNode(name, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null));
            var walker = new BuildGraph(globals, entryPoints: entryPointNodes); 

            if (compilation.EntryPoints.Any())
            {
                while (walker.SharedState.RequestStack.TryPop(out var currentRequest))
                {
                    if (!walker.SharedState.Callables.TryGetValue(currentRequest.CallableName, out var decl))
                        throw new ArgumentException($"Couldn't find definition for callable {currentRequest.CallableName}", nameof(currentRequest));

                    var relevantSpecs = decl.Specializations.Where(s => s.Kind == currentRequest.Kind);
                    var typeArgsId = SpecializationBundleProperties.BundleId(currentRequest.TypeArgs);

                    if (typeArgsId.IsNull)
                    {
                        if (decl.Signature.TypeParameters.Any()) // this would indicate an invalid syntax tree
                            throw new ArgumentException($"The types in the given compilation have not been properly resolved");

                        if (relevantSpecs.Count() != 1)
                            throw new ArgumentException($"Missing specialization {currentRequest.Kind} for {currentRequest.CallableName}");
                    }
                    else
                    {
                        // Finds the correct type specialization for the type arguments of the currentRequest.
                        // The assumption is that upon resolution, these type arguments have been cast to 
                        // the type of any explicitly defined ones in the closest matching specialization.
                        var specArgMatches = relevantSpecs.Select(spec =>
                        {
                            if (spec.TypeArguments.IsNull) return (0, spec);
                            if (spec.TypeArguments.Item.Count() != typeArgsId.Item.Count())
                                throw new ArgumentException($"Incorrect number of type arguments in request for {currentRequest.CallableName}");

                            var specTypeArgs = spec.TypeArguments.Item.Select(StripPositionInfo.Apply).ToImmutableArray();
                            var mismatch = specTypeArgs.Where((tArg, idx) => !tArg.Resolution.IsMissingType && !tArg.Resolution.Equals(typeArgsId.Item[idx])).Any();
                            if (mismatch) return (-1, spec);

                            var matches = specTypeArgs.Where((tArg, idx) => !tArg.Resolution.IsMissingType && tArg.Resolution.Equals(typeArgsId.Item[idx])).Count();
                            return (matches, spec);
                        });

                        var matches = specArgMatches.OrderByDescending(match => match.Item1).Append((-1, null));
                        if (matches.First().Item1 < 0)
                            throw new ArgumentException($"Could not find a suitable {currentRequest.Kind} specialization for {currentRequest.CallableName}");

                        relevantSpecs = matches.Select(match => match.spec);
                    }

                    // The current request must be added before it is processed to prevent
                    // self-references from duplicating on the stack.
                    walker.SharedState.ResolvedCallableSet.Add(currentRequest);

                    var spec = relevantSpecs.First();
                    var typeParamNames = GetTypeParameterNames(spec.Signature);
                    walker.SharedState.SetCurrentCaller(currentRequest, typeParamNames);
                    walker.Namespaces.OnSpecializationImplementation(spec.Implementation);
                }
            }
            else
            {
                // ToDo: can be replaced by walker.Apply(compilation) once master is merged in
                foreach (var ns in compilation.Namespaces)
                {
                    walker.Namespaces.OnNamespace(ns);
                }
            }

            return walker.SharedState.Graph;
        }

        private class BuildGraph : SyntaxTreeTransformation<TransformationState>
        {
            public BuildGraph(ImmutableDictionary<QsQualifiedName, QsCallable> callables, IEnumerable<CallGraphNode> entryPoints = null) 
            : base(new TransformationState(callables, entryPoints), TransformationOptions.NoRebuild)
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }
        }

        /// <summary>
        /// Gets the type parameter names for the given callable. 
        /// Throws an ArgumentNullExeception if the given signature is null. 
        /// Throws an ArgumentException if any of its type parameter names is invalid. 
        /// </summary>
        private static ImmutableArray<NonNullable<string>> GetTypeParameterNames(ResolvedSignature signature)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var typeParams = signature.TypeParameters.Select(p =>
                p is QsLocalSymbol.ValidName name ? name.Item
                : throw new ArgumentException($"invalid type parameter name"));
            return typeParams.ToImmutableArray();
        }

        /// <summary>
        /// Returns the type arguments for the given callable according to the given type parameter resolutions.
        /// Throws an ArgumentNullExeception if any of the given arguments is null. 
        /// Throws an ArgumentException if any of its type parameter names is invalid or
        /// if the resolution is missing for any of the type parameters of the callable. 
        /// </summary>
        private static ImmutableArray<ResolvedType> ConcreteTypeArguments(QsQualifiedName callable, ResolvedSignature signature, TypeParameterResolutions typeParamRes)
        {
            if (callable == null) throw new ArgumentNullException(nameof(callable));
            if (typeParamRes == null) throw new ArgumentNullException(nameof(typeParamRes));

            var typeArgs = GetTypeParameterNames(signature).Select(p =>
                typeParamRes.TryGetValue(Tuple.Create(callable, p), out var res) ? res
                : throw new ArgumentException($"unresolved type parameter {p.Value} for {callable}"));
            return typeArgs.ToImmutableArray();
        }

        private class TransformationState
        {
            internal bool IsInCall = false;
            internal bool HasAdjointDependency = false;
            internal bool HasControlledDependency = false;

            internal readonly CallGraph Graph;
            private readonly bool EnableFullConcretization;
            internal readonly Stack<CallGraphNode> RequestStack;
            internal readonly HashSet<CallGraphNode> ResolvedCallableSet;
            internal readonly ImmutableDictionary<QsQualifiedName, QsCallable> Callables;

            internal IEnumerable<TypeParameterResolutions> MyTypeParameterResolutions;
            internal TypeParameterResolutions CallerTypeParameterResolutions;

            internal CallGraphNode CurrentCaller { get; private set; }
            internal void SetCurrentCaller(CallGraphNode value, ImmutableArray<NonNullable<string>> typeParamNames)
            {
                CurrentCaller = value;
                this.CallerTypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>.Empty;
                if (value != null && value.TypeArgs.IsValue)
                {
                    if (value.TypeArgs.Item.Length != typeParamNames.Length)
                        throw new ArgumentException("The number of type parameter names does not match the number or type arguments");

                    this.CallerTypeParameterResolutions = value.TypeArgs.Item
                        .Where(res => !res.Resolution.IsMissingType)
                        .Select((res, idx) => (Tuple.Create(value.CallableName, typeParamNames[idx]), res))
                        .ToImmutableDictionary(kv => kv.Item1, kv => kv.Item2);
                }
            }

            internal TransformationState(ImmutableDictionary<QsQualifiedName, QsCallable> callables,
                IEnumerable<CallGraphNode> entryPoints = null, IEnumerable<CallGraphNode> resolved = null)
            {
                this.Callables = callables ?? throw new ArgumentNullException(nameof(callables));
                this.RequestStack = new Stack<CallGraphNode>(entryPoints ?? Array.Empty<CallGraphNode>());
                this.ResolvedCallableSet = new HashSet<CallGraphNode>(resolved ?? Array.Empty<CallGraphNode>());

                this.EnableFullConcretization = this.RequestStack.Any();
                this.MyTypeParameterResolutions = new List<TypeParameterResolutions>();
                this.CallerTypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>.Empty;
                this.Graph = new CallGraph();
            }

            // methods to update the call graph

            /// <summary>
            /// Adds an edge from the current caller to the called node to the call graph. 
            /// If full concretization is enables, adds the called node to the request stack if necessary.
            /// Throws an ArgumentNullException if the current caller or any of the given arguments is null. 
            /// </summary>
            private void PushEdge(CallGraphNode called, TypeParameterResolutions typeParamRes)
            {
                this.Graph.AddDependency(this.CurrentCaller, called, typeParamRes);
                if (this.EnableFullConcretization
                    && !this.RequestStack.Contains(called)
                    && !this.ResolvedCallableSet.Contains(called))
                {
                    // If we are not processing all elements, then we need to keep track of what elements
                    // have been processed, and which elements still need to be processed.
                    this.RequestStack.Push(called);
                }
            }

            /// <summary>
            /// Adds an edge from the current caller to a specialization of the callable with the given name to the call graph. 
            /// Clears the list of TypeParameterResolutions in the process. The called specialization is determined based on the
            /// current transformation state. If full concretization is enables, adds the called node to the request stack if necessary.
            /// Throws an ArgumentNullException if the given name is null. 
            /// May throw an ArgumentException if no callable with the given name exists in the dictionary of callables.
            /// </summary>
            internal void BuildEdge(QsQualifiedName called)
            {
                //TypeParamUtils.TryCombineTypeResolutionsForTarget(
                //    called, out var typeParamRes,
                //    MyTypeParameterResolutions.Append(CallerTypeParameterResolutions).ToArray());

                var combinations = new TypeResolutionCombination(MyTypeParameterResolutions.Append(CallerTypeParameterResolutions).ToArray());
                var typeParamRes = combinations.CombinedResolutionDictionary.FilterByOrigin(called);
                MyTypeParameterResolutions = new List<TypeParameterResolutions>();

                var typeArgsCalled = QsNullable<ImmutableArray<ResolvedType>>.Null;
                if (this.EnableFullConcretization)
                {
                    if (!Callables.TryGetValue(called, out var decl))
                        throw new ArgumentException($"Couldn't find definition for callable {called}");

                    var resTypeArgsCalled = ConcreteTypeArguments(decl.FullName, decl.Signature, typeParamRes);
                    typeArgsCalled = resTypeArgsCalled.Length != 0
                        ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(resTypeArgsCalled.ToImmutableArray())
                        : QsNullable<ImmutableArray<ResolvedType>>.Null;
                }

                if (IsInCall)
                {
                    var kind = QsSpecializationKind.QsBody;
                    if (HasAdjointDependency && HasControlledDependency)
                    {
                        kind = QsSpecializationKind.QsControlledAdjoint;
                    }
                    else if (HasAdjointDependency)
                    {
                        kind = QsSpecializationKind.QsAdjoint;
                    }
                    else if (HasControlledDependency)
                    {
                        kind = QsSpecializationKind.QsControlled;
                    }

                    PushEdge(new CallGraphNode(called, kind, typeArgsCalled), typeParamRes);
                }
                else
                {
                    // The callable is being used in a non-call context, such as being
                    // assigned to a variable or passed as an argument to another callable,
                    // which means it could get a functor applied at some later time.
                    // We're conservative and add all 4 possible kinds.
                    PushEdge(new CallGraphNode(called, QsSpecializationKind.QsBody, typeArgsCalled), typeParamRes);
                    PushEdge(new CallGraphNode(called, QsSpecializationKind.QsControlled, typeArgsCalled), typeParamRes);
                    PushEdge(new CallGraphNode(called, QsSpecializationKind.QsAdjoint, typeArgsCalled), typeParamRes);
                    PushEdge(new CallGraphNode(called, QsSpecializationKind.QsControlledAdjoint, typeArgsCalled), typeParamRes);
                }
            }
        }

        private class NamespaceTransformation : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec)
            {
                if (spec == null) throw new ArgumentNullException(nameof(spec));
                var typeParamNames = GetTypeParameterNames(spec.Signature);
                var node = new CallGraphNode(spec.Parent, spec.Kind, spec.TypeArguments);
                SharedState.SetCurrentCaller(node, typeParamNames);
                return base.OnSpecializationDeclaration(spec);
            }
        }

        private class ExpressionTransformation : ExpressionTransformation<TransformationState>
        {
            public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override TypedExpression OnTypedExpression(TypedExpression ex)
            {
                if (ex.TypeParameterResolutions.Any())
                {
                    SharedState.MyTypeParameterResolutions = SharedState.MyTypeParameterResolutions.Prepend(ex.TypeParameterResolutions);
                }
                return base.OnTypedExpression(ex);
            }
        }

        private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
        {
            public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override ExpressionKind OnCallLikeExpression(TypedExpression method, TypedExpression arg)
            {
                var contextInCall = SharedState.IsInCall;
                SharedState.IsInCall = true;
                this.Expressions.OnTypedExpression(method);
                SharedState.IsInCall = contextInCall;
                this.Expressions.OnTypedExpression(arg);
                return ExpressionKind.InvalidExpr;
            }

            public override ExpressionKind OnAdjointApplication(TypedExpression ex)
            {
                SharedState.HasAdjointDependency = !SharedState.HasAdjointDependency;
                base.OnAdjointApplication(ex);
                SharedState.HasAdjointDependency = !SharedState.HasAdjointDependency;
                return ExpressionKind.InvalidExpr;
            }

            public override ExpressionKind OnControlledApplication(TypedExpression ex)
            {
                var contextControlled = SharedState.HasControlledDependency;
                SharedState.HasControlledDependency = true;
                base.OnControlledApplication(ex);
                SharedState.HasControlledDependency = contextControlled;
                return ExpressionKind.InvalidExpr;
            }

            public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.GlobalCallable called)
                {
                    SharedState.BuildEdge(called.Item);
                }
                return ExpressionKind.InvalidExpr;
            }
        }
    }
}
