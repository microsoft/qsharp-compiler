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


// ToDo: Review access modifiers

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
        public static CallGraph Apply(QsCompilation compilation)
        {
            var globals = compilation.Namespaces.GlobalCallableResolutions();
            var walker = new BuildGraph(globals);

            if (compilation.EntryPoints.Any())
            {
                walker.SharedState.IsLimitedToEntryPoints = true;
                walker.SharedState.RequestStack = new Stack<QsQualifiedName>(compilation.EntryPoints);
                walker.SharedState.ResolvedCallableSet = new HashSet<QsQualifiedName>();
                while (walker.SharedState.RequestStack.Any())
                {
                    var currentRequest = walker.SharedState.RequestStack.Pop();

                    // If there is a call to an unknown callable, throw exception
                    if (!globals.TryGetValue(currentRequest, out QsCallable currentCallable))
                        throw new ArgumentException($"Couldn't find definition for callable: {currentRequest}");

                    // The current request must be added before it is processed to prevent
                    // self-references from duplicating on the stack.
                    walker.SharedState.ResolvedCallableSet.Add(currentRequest);

                    walker.Namespaces.OnCallableDeclaration(currentCallable);
                }
            }
            else
            {
                foreach (var ns in compilation.Namespaces)
                {
                    walker.Namespaces.OnNamespace(ns);
                }
            }

            return walker.SharedState.Graph;
        }

        private class BuildGraph : SyntaxTreeTransformation<TransformationState>
        {
            public BuildGraph(ImmutableDictionary<QsQualifiedName, QsCallable> callables) 
            : base(new TransformationState(callables))
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }
        }

        private class TransformationState
        {
            internal bool IsInCall = false;
            internal bool HasAdjointDependency = false;
            internal bool HasControlledDependency = false;
            internal QsQualifiedName CurrentCaller;
            internal QsSpecializationKind CallerKind;
            internal QsNullable<ImmutableArray<ResolvedType>> CallerTypeArguments;
            internal CallGraph Graph = new CallGraph();
            internal IEnumerable<TypeParameterResolutions> TypeParameterResolutions = new List<TypeParameterResolutions>();
            
            internal bool IsLimitedToEntryPoints = false;
            internal Stack<CallGraphNode> RequestStack = null; 
            internal HashSet<CallGraphNode> ResolvedCallableSet = null;
            internal readonly ImmutableDictionary<QsQualifiedName, QsCallable> Callables;

            internal TransformationState(ImmutableDictionary<QsQualifiedName, QsCallable> callables) =>
                this.Callables = callables ?? throw new ArgumentNullException(nameof(callables));
        }

        private class NamespaceTransformation : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override QsCallable OnCallableDeclaration(QsCallable callable)
            {
                if (callable == null) throw new ArgumentNullException(nameof(callable));
                if (callable.Specializations.Any(s => s.TypeArguments.IsValue))
                {
                    // This exception can be removed once the todo below has been implemented.  
                    throw new NotSupportedException("type specializations are not supported by the call graph walker");
                }

                SharedState.CurrentCaller = callable.FullName;
                var relevantBundle = callable.WithSpecializations(specs =>
                    // Todo: We need to find the closest match of all the specializations, and filter all functor specializations for those. 
                    specs.Where(s => s.TypeArguments.IsNull).ToImmutableArray()
                );
                return base.OnCallableDeclaration(relevantBundle);
            }

            public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec)
            {
                if (spec == null) throw new ArgumentNullException(nameof(spec));
                SharedState.CallerKind = spec.Kind;
                if (SharedState.CallerTypeArguments.IsNull)
                {
                    SharedState.CallerTypeArguments = spec.TypeArguments;
                }
                else if (spec.TypeArguments.IsValue)
                {
                    var callerArgs = spec.TypeArguments.Item.Select((type, idx) =>
                        type.Resolution.IsMissingType ? SharedState.CallerTypeArguments.Item[idx] : type);
                    SharedState.CallerTypeArguments = QsNullable<ImmutableArray<ResolvedType>>.NewValue(callerArgs.ToImmutableArray());

                    // This exception can be removed once the todo below has been implemented.  
                    // Todo: check if the spec type args are compatible with the ones set for the caller, 
                    // and throw an ArgumentException if they are not. 
                    throw new NotSupportedException("type specializations are not supported by the call graph walker");
                }
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
                    SharedState.TypeParameterResolutions = SharedState.TypeParameterResolutions.Prepend(ex.TypeParameterResolutions);
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
                var result = base.OnAdjointApplication(ex);
                SharedState.HasAdjointDependency = !SharedState.HasAdjointDependency;
                return result;
            }

            public override ExpressionKind OnControlledApplication(TypedExpression ex)
            {
                var contextControlled = SharedState.HasControlledDependency;
                SharedState.HasControlledDependency = true;
                var result = base.OnControlledApplication(ex);
                SharedState.HasControlledDependency = contextControlled;
                return result;
            }

            /// <summary>
            /// Gets the type parameter names for the given callable. 
            /// Throws an ArgumentException if the ShareState does not contain a callable with the given name, 
            /// or if any of its type parameter names is invalid. 
            /// </summary>
            private ImmutableArray<NonNullable<string>> GetTypeParameterNames(QsQualifiedName callable)
            {
                if (!SharedState.Callables.TryGetValue(callable, out var decl))
                    throw new ArgumentException($"Couldn't find definition for callable {callable}");

                var typeParams = decl.Signature.TypeParameters.Select(p =>
                    p is QsLocalSymbol.ValidName name ? name.Item
                    : throw new ArgumentException($"invalid type parameter name for callable {callable}"));

                return typeParams.ToImmutableArray();
            }

            /// <summary>
            /// Returns the type arguments for the given callable according to the given type parameter resolutions.
            /// Throws an ArgumentException if the resolution is missing for any of the type parameters of the callable. 
            /// </summary>
            private ImmutableArray<ResolvedType> ConcreteTypeArguments(QsQualifiedName callable, TypeParameterResolutions typeParamRes)
            {
                var typeArgs = GetTypeParameterNames(callable).Select(p =>
                    typeParamRes.TryGetValue(Tuple.Create(callable, p), out var res) ? res
                    // FIXME: SET TO NULL IF THERE ARE RESOLUTIONS THAT ARE NOT FULLY CONCRETE
                    : throw new ArgumentException($"unresolved type parameter {p.Value} for {callable}"));

                return typeArgs.ToImmutableArray();

            }

            public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.GlobalCallable called)
                {
                    TypeParamUtils.TryCombineTypeResolutionsForTarget(
                        called.Item, out var typeParamRes, 
                        SharedState.TypeParameterResolutions.Append(SharedState.CallerTypeArguments).ToArray());
                    SharedState.TypeParameterResolutions = new List<TypeParameterResolutions>();

                    var resTypeArgsCalled = ConcreteTypeArguments(called.Item, typeParamRes);
                    var typeArgsCalled = resTypeArgsCalled.Length != 0
                        ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(resTypeArgsCalled.ToImmutableArray())
                        : QsNullable<ImmutableArray<ResolvedType>>.Null;

                    if (SharedState.IsInCall)
                    {
                        var kind = QsSpecializationKind.QsBody;
                        if (SharedState.HasAdjointDependency && SharedState.HasControlledDependency)
                        {
                            kind = QsSpecializationKind.QsControlledAdjoint;
                        }
                        else if (SharedState.HasAdjointDependency)
                        {
                            kind = QsSpecializationKind.QsAdjoint;
                        }
                        else if (SharedState.HasControlledDependency)
                        {
                            kind = QsSpecializationKind.QsControlled;
                        }

                        SharedState.Graph.AddDependency(
                            SharedState.CurrentCaller, SharedState.CallerKind, SharedState.CallerTypeArguments,
                            called.Item, kind, typeArgsCalled,
                            typeParamRes);
                    }
                    else
                    {
                        // The callable is being used in a non-call context, such as being
                        // assigned to a variable or passed as an argument to another callable,
                        // which means it could get a functor applied at some later time.
                        // We're conservative and add all 4 possible kinds.
                        SharedState.Graph.AddDependency(
                            SharedState.CurrentCaller, SharedState.CallerKind, SharedState.CallerTypeArguments, 
                            called.Item, QsSpecializationKind.QsBody, typeArgsCalled, typeParamRes);
                        SharedState.Graph.AddDependency(
                            SharedState.CurrentCaller, SharedState.CallerKind, SharedState.CallerTypeArguments, 
                            called.Item, QsSpecializationKind.QsControlled, typeArgsCalled, typeParamRes);
                        SharedState.Graph.AddDependency(
                            SharedState.CurrentCaller, SharedState.CallerKind, SharedState.CallerTypeArguments, 
                            called.Item, QsSpecializationKind.QsAdjoint, typeArgsCalled, typeParamRes);
                        SharedState.Graph.AddDependency(
                            SharedState.CurrentCaller, SharedState.CallerKind, SharedState.CallerTypeArguments, 
                            called.Item, QsSpecializationKind.QsControlledAdjoint, typeArgsCalled, typeParamRes);
                    }

                    // If we are not processing all elements, then we need to keep track of what elements
                    // have been processed, and which elements still need to be processed.
                    var callerKey = new CallGraphNode(SharedState.CurrentCaller, SharedState.CallerKind, SharedState.CallerTypeArguments);
                    var calledKey = new CallGraphNode(called.Item, NULL, typeArgsCalled);
                    if (SharedState.IsLimitedToEntryPoints
                        && !SharedState.RequestStack.Contains(calledKey)
                        && !SharedState.ResolvedCallableSet.Contains(calledKey))
                    {
                        SharedState.RequestStack.Push(calledKey); 
                    }
                }

                return ExpressionKind.InvalidExpr;
            }
        }
    }
}
