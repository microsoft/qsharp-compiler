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
            var walker = new BuildGraph();
            foreach (var ns in compilation.Namespaces)
            {
                walker.Namespaces.OnNamespace(ns);
            }
            return walker.SharedState.graph;
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
            internal QsSpecialization spec;

            internal bool inCall = false;
            internal bool hasAdjointDependency = false;
            internal bool hasControlledDependency = false;

            internal CallGraph graph = new CallGraph();

            internal IEnumerable<TypeParameterResolutions> typeParameterResolutions = new List<TypeParameterResolutions>();
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

        private class ExpressionTransformation : ExpressionTransformation<TransformationState>
        {
            public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override TypedExpression OnTypedExpression(TypedExpression ex)
            {
                if (ex.TypeParameterResolutions.Any())
                {
                    SharedState.typeParameterResolutions = SharedState.typeParameterResolutions.Prepend(ex.TypeParameterResolutions);
                }
                return base.OnTypedExpression(ex);
            }
        }

        private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
        {
            public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

            public override ExpressionKind OnCallLikeExpression(TypedExpression method, TypedExpression arg)
            {
                var contextInCall = SharedState.inCall;
                SharedState.inCall = true;
                this.Expressions.OnTypedExpression(method);
                SharedState.inCall = contextInCall;
                this.Expressions.OnTypedExpression(arg);
                return ExpressionKind.InvalidExpr;
            }

            public override ExpressionKind OnAdjointApplication(TypedExpression ex)
            {
                SharedState.hasAdjointDependency = !SharedState.hasAdjointDependency;
                var result = base.OnAdjointApplication(ex);
                SharedState.hasAdjointDependency = !SharedState.hasAdjointDependency;
                return result;
            }

            public override ExpressionKind OnControlledApplication(TypedExpression ex)
            {
                var contextControlled = SharedState.hasControlledDependency;
                SharedState.hasControlledDependency = true;
                var result = base.OnControlledApplication(ex);
                SharedState.hasControlledDependency = contextControlled;
                return result;
            }

            public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.GlobalCallable global)
                {
                    // Type arguments need to be resolved for the whole expression to be accurate
                    // ToDo: this needs adaption if we want to support type specializations
                    var typeArgs = tArgs;

                    TypeParamUtils.TryCombineTypeResolutionsForTarget(global.Item, out var typeParamRes, SharedState.typeParameterResolutions.ToArray());
                    SharedState.typeParameterResolutions = new List<TypeParameterResolutions>();

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

                        SharedState.graph.AddDependency(SharedState.spec, global.Item, kind, typeArgs, typeParamRes);
                    }
                    else
                    {
                        // The callable is being used in a non-call context, such as being
                        // assigned to a variable or passed as an argument to another callable,
                        // which means it could get a functor applied at some later time.
                        // We're conservative and add all 4 possible kinds.
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsBody, typeArgs, typeParamRes);
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsControlled, typeArgs, typeParamRes);
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsAdjoint, typeArgs, typeParamRes);
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsControlledAdjoint, typeArgs, typeParamRes);
                    }
                }

                return ExpressionKind.InvalidExpr;
            }
        }
    }
}
