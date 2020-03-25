// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;


namespace Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// Class used to track call graph of a compilation.
    /// This class is *not* threadsafe. // ToDo: is this still not threadsafe?
    public class CallGraph
    {
        public struct CallGraphEdge
        {
            // ToDo: Fill out edge information
        }

        //public enum DependencyType
        //{
        //    NoTypeParameters,
        //    NoForwardingTypeParameters,
        //    ForwardingTypeParameters,
        //    AugmentingTypeParameters
        //}

        public struct CallGraphNode
        {
            public QsQualifiedName CallableName;
            public QsSpecializationKind Kind;
            public QsNullable<ImmutableArray<ResolvedType>> TypeArgs;
        }

        private Dictionary<CallGraphNode, HashSet<(CallGraphNode, CallGraphEdge)>> _Dependencies = new Dictionary<CallGraphNode, HashSet<(CallGraphNode, CallGraphEdge)>>();

        private QsNullable<ImmutableArray<ResolvedType>> RemovePositionFromTypeArgs(QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
            tArgs.IsValue
            ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(tArgs.Item.Select(x => x.RemovePositionInfo()).ToImmutableArray())
            : tArgs;

        // ToDo: it might be cleaner to have an F# member function on the ResolvedType class for doing this
        private bool IsAugmentingTypeParameters(ResolvedType type, QsQualifiedName callerName)
        {
            bool isAugmenting(ResolvedType type)
            {
                if (type.Resolution is ResolvedTypeKind.ArrayType ary)
                {
                    return isAugmenting(ary.Item);
                }
                else if (type.Resolution is ResolvedTypeKind.TupleType tup)
                {
                    return tup.Item.Any(x => isAugmenting(x));
                }
                else if (type.Resolution is ResolvedTypeKind.Operation op)
                {
                    return isAugmenting(op.Item1.Item1) || isAugmenting(op.Item1.Item2);
                }
                else if (type.Resolution is ResolvedTypeKind.Function func)
                {
                    return isAugmenting(func.Item1) || isAugmenting(func.Item2);
                }
                else if (type.Resolution is ResolvedTypeKind.TypeParameter tParam)
                {
                    return tParam.Item.Origin.Equals(callerName);
                }

                return false;
            }

            if (type.Resolution is ResolvedTypeKind.TypeParameter tParam)
            {
                return false;
            }

            return isAugmenting(type);
        }

        private void RecordDependency(CallGraphNode callerKey, CallGraphNode calledKey, CallGraphEdge edge)
        {
            if (_Dependencies.TryGetValue(callerKey, out var deps))
            {
                deps.Add((calledKey, edge));
            }
            else
            {
                var newDeps = new HashSet<(CallGraphNode, CallGraphEdge)>();
                newDeps.Add((calledKey, edge));
                _Dependencies[callerKey] = newDeps;
            }
        }

        public void AddDependency(QsSpecialization callerSpec, QsQualifiedName calledName, QsSpecializationKind calledKind, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs) =>
            AddDependency(
                callerSpec.Parent, callerSpec.Kind, callerSpec.TypeArguments,
                calledName, calledKind, calledTypeArgs);

        public void AddDependency(
            QsQualifiedName callerName, QsSpecializationKind callerKind, QsNullable<ImmutableArray<ResolvedType>> callerTypeArgs,
            QsQualifiedName calledName, QsSpecializationKind calledKind, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs)
        {
            //callerTypeArgs = RemovePositionFromTypeArgs(callerTypeArgs);
            //calledTypeArgs = RemovePositionFromTypeArgs(calledTypeArgs);

            // ToDo: Setting TypeArgs to Null is temporary
            var callerKey = new CallGraphNode { CallableName = callerName, Kind = callerKind, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null };
            var calledKey = new CallGraphNode { CallableName = calledName, Kind = calledKind, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null };

            // ToDo: there is probably a better way to write this if-else structure
            //var dependencyType = DependencyType.NoTypeParameters;
            //if (calledTypeArgs.IsValue && calledTypeArgs.Item.Any())
            //{
            //    if (calledTypeArgs.Item.Any(x => IsAugmentingTypeParameters(x, callerName)))
            //    {
            //        dependencyType = DependencyType.AugmentingTypeParameters;
            //    }
            //    else if (calledTypeArgs.Item.Any(x => x.Resolution is ResolvedTypeKind.TypeParameter tParam && tParam.Item.Origin.Equals(callerName)))
            //    {
            //        dependencyType = DependencyType.ForwardingTypeParameters;
            //    }
            //    else
            //    {
            //        dependencyType = DependencyType.NoForwardingTypeParameters;
            //    }
            //}

            RecordDependency(callerKey, calledKey, new CallGraphEdge { });
        }

        public ImmutableArray<(CallGraphNode, CallGraphEdge)> GetDirectDependencies(CallGraphNode callerSpec)
        {
            if (_Dependencies.TryGetValue(callerSpec, out var deps))
            {
                return deps.ToImmutableArray();
            }
            else
            {
                return ImmutableArray<(CallGraphNode, CallGraphEdge)>.Empty;
            }
        }

        /// Returns all specializations that are used directly within the given caller,
        /// whether they are called, partially applied, or assigned.
        /// The returned specializations are identified by the full name of the callable,
        /// the specialization kind, as well as the resolved type arguments.
        /// The returned type arguments are the exact type arguments of the expression,
        /// and may thus be incomplete or correspond to subtypes of a defined specialization bundle.
        public ImmutableArray<(CallGraphNode, CallGraphEdge)> GetDirectDependencies(QsSpecialization callerSpec) =>
            GetDirectDependencies(new CallGraphNode { CallableName = callerSpec.Parent, Kind = callerSpec.Kind, TypeArgs = RemovePositionFromTypeArgs(callerSpec.TypeArguments) });

        // ToDo
        public ImmutableArray<(CallGraphNode, CallGraphEdge)> GetAllDependencies(CallGraphNode callerSpec)
        {
            return ImmutableArray<(CallGraphNode, CallGraphEdge)>.Empty;

            //HashSet<(CallGraphNode, CallGraphEdge)> WalkDependencyTree(CallGraphNode root, HashSet<(CallGraphNode, CallGraphEdge)> accum, DependencyType parentDepType)
            //{
            //    if (_Dependencies.TryGetValue(root, out var next))
            //    {
            //        foreach (var k in next)
            //        {
            //            // Get the maximum type of dependency between the parent dependency type and the current dependency type
            //            var maxDepType = k.Item2.CompareTo(parentDepType) > 0 ? k.Item2 : parentDepType;
            //            if (accum.Add((k.Item1, maxDepType)))
            //            {
            //                // ToDo: this won't work once Type specialization are implemented
            //                var noTypeParams = new CallGraphNode { CallableName = k.Item1.CallableName, Kind = k.Item1.Kind, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null };
            //                WalkDependencyTree(noTypeParams, accum, maxDepType);
            //            }
            //        }
            //    }
            //
            //    return accum;
            //}
            //
            //return WalkDependencyTree(callerSpec, new HashSet<(CallGraphNode, DependencyType)>(), DependencyType.NoTypeParameters).ToImmutableArray();
        }

        /// Returns all specializations directly or indirectly used within the given caller,
        /// whether they are called, partially applied, or assigned.
        /// The returned specializations are identified by the full name of the callable,
        /// the specialization kind, as well as the resolved type arguments.
        /// The returned type arguments are the exact type arguments of the expression,
        /// and may thus be incomplete or correspond to subtypes of a defined specialization bundle.
        public ImmutableArray<(CallGraphNode, CallGraphEdge)> GetAllDependencies(QsSpecialization callerSpec) =>
            GetAllDependencies(new CallGraphNode { CallableName = callerSpec.Parent, Kind = callerSpec.Kind, TypeArgs = RemovePositionFromTypeArgs(callerSpec.TypeArguments) });

        public HashSet<ImmutableArray<(CallGraphNode, CallGraphEdge)>> GetCallCycles()
        {
            var active = new Stack<CallGraphNode>();
            var activeHash = new Dictionary<CallGraphNode, int>();
            var finished = new HashSet<CallGraphNode>();
            var cycles = new HashSet<ImmutableArray<(CallGraphNode, CallGraphEdge)>>();

            void processDependencies(CallGraphNode depKey)
            {
                active.Push(depKey);
                activeHash.Add(depKey, active.Count() - 1);

                foreach (var (dep, depType) in _Dependencies[depKey])
                {
                    if (!finished.Contains(dep))
                    {
                        if (activeHash.TryGetValue(dep, out var position))
                        {
                            // Cycle detected
                            
                        }
                        else
                        {
                            processDependencies(dep);
                        }
                    }
                }

                activeHash.Remove(depKey);
                finished.Add(active.Pop());
            }

            foreach (var key in _Dependencies.Keys)
            {
                if (!finished.Contains(key))
                {
                    processDependencies(key);
                }
            }

            return cycles;
        }
    }

    public static class BuildCallGraph
    {
        public static CallGraph Apply(QsCompilation compilation)
        {
            var walker = new BuildGraph();

            walker.Namespaces.OnNamespace(compilation.Namespaces.First(x => x.Name.Value == "Input"));

            //foreach (var ns in compilation.Namespaces)
            //{
            //    walker.Namespaces.OnNamespace(ns);
            //}

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
                this.Namespaces = new NamespaceTransformation(this);
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
