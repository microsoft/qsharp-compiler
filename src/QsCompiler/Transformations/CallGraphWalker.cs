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
        public static CallGraph Apply(QsCompilation compilation) =>
            compilation.EntryPoints.Any()
            ? ApplyWithEntryPoints(compilation)
            : ApplyWithoutEntryPoints(compilation);

        private static CallGraph ApplyWithEntryPoints(QsCompilation compilation)
        {
            var walker = new BuildGraph();

            walker.SharedState.IsLimitedToEntryPoints = true;
            walker.SharedState.RequestStack = new Stack<QsQualifiedName>(compilation.EntryPoints);
            walker.SharedState.ResolvedCallableSet = new HashSet<QsQualifiedName>();
            var globals = compilation.Namespaces.GlobalCallableResolutions();
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

            return walker.SharedState.Graph;
        }

        private static CallGraph ApplyWithoutEntryPoints(QsCompilation compilation)
        {
            var walker = new BuildGraph();

            // ToDo: This can be simplified once the OnCompilation method is merged in
            foreach (var ns in compilation.Namespaces)
            {
                walker.Namespaces.OnNamespace(ns);
            }

            return walker.SharedState.Graph;
        }

        private class BuildGraph : SyntaxTreeTransformation<TransformationState>
        {
            public BuildGraph() : base(new TransformationState())
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
            internal QsSpecialization CurrentSpecialization;
            internal CallGraph Graph = new CallGraph();
            internal IEnumerable<TypeParameterResolutions> TypeParameterResolutions = new List<TypeParameterResolutions>();
 
            internal bool IsLimitedToEntryPoints = false;
            internal Stack<QsQualifiedName> RequestStack = null;
            internal HashSet<QsQualifiedName> ResolvedCallableSet = null;
        }

        private class NamespaceTransformation : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec)
            {
                SharedState.CurrentSpecialization = spec;
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

            public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.GlobalCallable global)
                {
                    // Type arguments need to be resolved for the whole expression to be accurate
                    // ToDo: this needs adaption if we want to support type specializations
                    var typeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null;

                    TypeParamUtils.TryCombineTypeResolutionsForTarget(global.Item, out var typeParamRes, SharedState.TypeParameterResolutions.ToArray());
                    SharedState.TypeParameterResolutions = new List<TypeParameterResolutions>();

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
                            SharedState.CurrentSpecialization,
                            global.Item, kind, typeArgs,
                            typeParamRes);
                    }
                    else
                    {
                        // The callable is being used in a non-call context, such as being
                        // assigned to a variable or passed as an argument to another callable,
                        // which means it could get a functor applied at some later time.
                        // We're conservative and add all 4 possible kinds.
                        SharedState.Graph.AddDependency(SharedState.CurrentSpecialization, global.Item, QsSpecializationKind.QsBody, typeArgs, typeParamRes);
                        SharedState.Graph.AddDependency(SharedState.CurrentSpecialization, global.Item, QsSpecializationKind.QsControlled, typeArgs, typeParamRes);
                        SharedState.Graph.AddDependency(SharedState.CurrentSpecialization, global.Item, QsSpecializationKind.QsAdjoint, typeArgs, typeParamRes);
                        SharedState.Graph.AddDependency(SharedState.CurrentSpecialization, global.Item, QsSpecializationKind.QsControlledAdjoint, typeArgs, typeParamRes);
                    }

                    // If we are not processing all elements, then we need to keep track of what elements
                    // have been processed, and which elements still need to be processed.
                    if (SharedState.IsLimitedToEntryPoints
                        && !SharedState.RequestStack.Contains(global.Item)
                        && !SharedState.ResolvedCallableSet.Contains(global.Item))
                    {
                        SharedState.RequestStack.Push(global.Item);
                    }
                }

                return ExpressionKind.InvalidExpr;
            }
        }
    }
}
