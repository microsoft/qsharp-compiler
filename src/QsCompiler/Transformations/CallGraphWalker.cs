// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;


namespace Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;

    /// Class used to track call graph of a compilation.
    /// This class is *not* threadsafe.
    public class CallGraph
    {
        public struct CallGraphDependency
        {
            public QsQualifiedName CallableName;
            public QsSpecializationKind Kind;
            public QsNullable<ImmutableArray<ResolvedType>> TypeArgs;
        }

        private Dictionary<CallGraphDependency, HashSet<CallGraphDependency>> _Dependencies = new Dictionary<CallGraphDependency, HashSet<CallGraphDependency>>();

        private void RecordDependency(CallGraphDependency callerKey, CallGraphDependency calledKey)
        {
            if (_Dependencies.TryGetValue(callerKey, out var deps))
            {
                deps.Add(calledKey);
            }
            else
            {
                var newDeps = new HashSet<CallGraphDependency>();
                newDeps.Add(calledKey);
                _Dependencies[callerKey] = newDeps;
            }
        }

        public void AddDependency(QsSpecialization callerSpec, QsQualifiedName calledName, QsSpecializationKind calledKind, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs)
        {
            var callerKey = new CallGraphDependency { CallableName = callerSpec.Parent, Kind = callerSpec.Kind, TypeArgs = callerSpec.TypeArguments };
            var calledKey = new CallGraphDependency { CallableName = calledName, Kind = calledKind, TypeArgs = calledTypeArgs };
            RecordDependency(callerKey, calledKey);
        }

        public void AddDependency(
            QsQualifiedName callerName, QsSpecializationKind callerKind, QsNullable<ImmutableArray<ResolvedType>> callerTypeArgs,
            QsQualifiedName calledName, QsSpecializationKind calledKind, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs)
        {
            var callerKey = new CallGraphDependency { CallableName = callerName, Kind = callerKind, TypeArgs = callerTypeArgs };
            var calledKey = new CallGraphDependency { CallableName = calledName, Kind = calledKind, TypeArgs = calledTypeArgs };
            RecordDependency(callerKey, calledKey);
        }

        /// Returns all specializations that are used directly within the given caller,
        /// whether they are called, partially applied, or assigned.
        /// The returned specializations are identified by the full name of the callable,
        /// the specialization kind, as well as the resolved type arguments.
        /// The returned type arguments are the exact type arguments of the expression,
        /// and may thus be incomplete or correspond to subtypes of a defined specialization bundle.
        public ImmutableArray<CallGraphDependency> GetDirectDependencies(QsSpecialization callerSpec)
        {
            var callerKey = new CallGraphDependency { CallableName = callerSpec.Parent, Kind = callerSpec.Kind, TypeArgs = callerSpec.TypeArguments };
            if (_Dependencies.TryGetValue(callerKey, out var deps))
            {
                return deps.ToImmutableArray();
            }
            else
            {
                return ImmutableArray<CallGraphDependency>.Empty;
            }
        }

        /// Returns all specializations directly or indirectly used within the given caller,
        /// whether they are called, partially applied, or assigned.
        /// The returned specializations are identified by the full name of the callable,
        /// the specialization kind, as well as the resolved type arguments.
        /// The returned type arguments are the exact type arguments of the expression,
        /// and may thus be incomplete or correspond to subtypes of a defined specialization bundle.
        public ImmutableArray<CallGraphDependency> GetAllDependencies(QsSpecialization callerSpec)
        {
            HashSet<CallGraphDependency> WalkDependencyTree(CallGraphDependency root, HashSet<CallGraphDependency> accum)
            {
                if (_Dependencies.TryGetValue(root, out var next))
                {
                    foreach (var k in next)
                    {
                        if (accum.Add(k))
                        {
                            WalkDependencyTree(k, accum);
                        }
                    }
                }

                return accum;
            }

            var callerKey = new CallGraphDependency { CallableName = callerSpec.Parent, Kind = callerSpec.Kind, TypeArgs = callerSpec.TypeArguments };
            return WalkDependencyTree(callerKey, new HashSet<CallGraphDependency>()).ToImmutableArray();
        }
    }

    public static class CallGraphWalker
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

        private class BuildGraph : SyntaxTreeTransformation<BuildGraph.TransformationState>
        {
            public class TransformationState
            {
                internal QsSpecialization spec;

                internal bool inCall = false;
                internal bool adjoint = false;
                internal bool controlled = false;

                internal CallGraph graph = new CallGraph();
            }

            public BuildGraph() : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
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

            private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

                private ExpressionKind HandleCall(TypedExpression method, TypedExpression arg)
                {
                    var contextInCall = SharedState.inCall;
                    SharedState.inCall = true;
                    this.Expressions.OnTypedExpression(method);
                    SharedState.inCall = contextInCall;
                    this.Expressions.OnTypedExpression(arg);
                    return ExpressionKind.InvalidExpr;
                }

                public override ExpressionKind OnOperationCall(TypedExpression method, TypedExpression arg) => HandleCall(method, arg);

                public override ExpressionKind OnFunctionCall(TypedExpression method, TypedExpression arg) => HandleCall(method, arg);

                public override ExpressionKind OnAdjointApplication(TypedExpression ex)
                {
                    SharedState.adjoint = !SharedState.adjoint;
                    var rtrn = base.OnAdjointApplication(ex);
                    SharedState.adjoint = !SharedState.adjoint;
                    return rtrn;
                }

                public override ExpressionKind OnControlledApplication(TypedExpression ex)
                {
                    var contextControlled = SharedState.controlled;
                    SharedState.controlled = true;
                    var rtrn = base.OnControlledApplication(ex);
                    SharedState.controlled = contextControlled;
                    return rtrn;
                }

                public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    if (sym is Identifier.GlobalCallable global)
                    {
                        var typeArgs = tArgs; // ToDo: THIS IS NOT ACCURATE
                        if (SharedState.inCall)
                        {
                            var kind = QsSpecializationKind.QsBody;
                            if (SharedState.adjoint && SharedState.controlled)
                            {
                                kind = QsSpecializationKind.QsControlledAdjoint;
                            }
                            else if (SharedState.adjoint)
                            {
                                kind = QsSpecializationKind.QsAdjoint;
                            }
                            else if (SharedState.controlled)
                            {
                                kind = QsSpecializationKind.QsControlled;
                            }

                            SharedState.graph.AddDependency(SharedState.spec, global.Item, kind, typeArgs);
                        }
                        else
                        {
                            // The callable is being used in a non-call context, such as being
                            // assigned to a variable or passed as an argument to another callable,
                            // which means it could get a functor applied at some later time.
                            // We're conservative and add all 4 possible kinds.
                            SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsBody, typeArgs);
                            SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsControlled, typeArgs);
                            SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsAdjoint, typeArgs);
                            SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsControlledAdjoint, typeArgs);
                        }
                    }

                    return ExpressionKind.InvalidExpr;
                }
            }
        }
    }
}
