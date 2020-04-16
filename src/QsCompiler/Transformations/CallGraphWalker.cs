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


// ToDo: Review access modifiers

namespace Microsoft.Quantum.QsCompiler.Transformations
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    /// <summary>
    /// Struct containing information that exists on edges in a call graph.
    /// </summary>
    public struct CallGraphEdge
    {
        public TypeParameterResolutions ParamResolutions;
    }

    /// <summary>
    /// Struct containing information that exists on nodes in a call graph.
    /// </summary>
    public struct CallGraphNode
    {
        public QsQualifiedName CallableName;
        public QsSpecializationKind Kind;
        public QsNullable<ImmutableArray<ResolvedType>> TypeArgs;
    }

    /// Class used to track call graph of a compilation
    public class CallGraph
    {
        // TODO:
        // This is the method that should be invoked to verify cycles of interest,
        // i.e. where each callable in the cycle is type parametrized.
        // It should probably generate diagnostics; I'll add the doc comment once its use is fully defined.
        internal static bool VerifyCycle(CallGraphNode rootNode, params CallGraphEdge[] edges)
        {
            var parent = rootNode.CallableName;
            var validResolution = TryCombineTypeResolutions(parent, out var combined, edges.Select(edge => edge.ParamResolutions).ToArray());
            var resolvedToConcrete = combined.Values.All(res => !(res.Resolution is ResolvedTypeKind.TypeParameter tp) || tp.Item.Origin.Equals(parent));
            return validResolution && resolvedToConcrete;
            //var isClosedCycle = validCycle && combined.Values.Any(res => res.Resolution is ResolvedTypeKind.TypeParameter tp && EqualsParent(tp.Item.Origin));
            // TODO: check that monomorphization correctly processes closed cycles - meaning add a test...
        }

        /// <summary>
        /// Combines subsequent concretions as part of a nested expression, or concretions as part of a cycle in the call graph,
        /// into a single dictionary containing the resolution for the type parameters of the specified target callable.
        /// The given resolutions are expected to be ordered starting with the dictionary containing the initial mapping for the
        /// type parameters of the specified target callable (the "innermost resolutions"). This mapping may potentially be to
        /// type parameters of other callables, which are then further concretized by subsequent resolutions.
        /// Returns the constructed dictionary as out parameter. Returns true if the combination of the given resolutions is valid,
        /// i.e. if there are no conflicting resolutions and type parameters of the target callable are uniquely resolved
        /// to either a concrete type, a type parameter of another callable, or themselves.
        /// Throws an ArgumentNullException if the given target is null.
        /// Throws an ArgumentException if the given resolutions imply that type parameters from multiple callables are
        /// simultaneously treated as concrete types.
        /// NOTE: This routine prioritizes the verifications to ensure the correctness of the resolution over performance.
        /// </summary>
        public static bool TryCombineTypeResolutions
            (QsQualifiedName target, out TypeParameterResolutions combined,
            params TypeParameterResolutions[] resolutions)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            // Short-cut some edge cases
            if (!resolutions.Any())
            {
                combined = TypeParameterResolutions.Empty;
                return true;
            }

            if (resolutions.Length == 1)
            {
                combined = resolutions[0];
                return true;
            }

            var combinedBuilder = ImmutableDictionary.CreateBuilder<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>();
            var success = true;

            static Tuple<QsQualifiedName, NonNullable<string>> AsTypeResolutionKey(QsTypeParameter tp) => Tuple.Create(tp.Origin, tp.TypeName);
            static bool ResolutionToTypeParameter(Tuple<QsQualifiedName, NonNullable<string>> typeParam, ResolvedType res) =>
                res.Resolution is ResolvedTypeKind.TypeParameter tp && tp.Item.Origin.Equals(typeParam.Item1) && tp.Item.TypeName.Equals(typeParam.Item2);

            // Returns true if the given resolution for the given key constrains the type parameter
            // by mapping it to a different type parameter belonging to the same callable.
            bool InconsistentResolutionToNative(Tuple<QsQualifiedName, NonNullable<string>> key, ResolvedType resolution)
            {
                var resolutionToTypeParam = resolution.Resolution as ResolvedTypeKind.TypeParameter;
                var isResolutionToNative = resolutionToTypeParam != null && resolutionToTypeParam.Item.Origin.Equals(target);
                return isResolutionToNative
                    // We can omit this check as long as combinedBuilder only ever contains native type parameters:
                    // && key.Item1.Equals(target)
                    && key.Item2.Value != resolutionToTypeParam.Item.TypeName.Value;
            }

            foreach (var resolution in resolutions)
            {
                // Contains a lookup of all the keys in the combined resolutions whose value needs to be updated
                // if a certain type parameter is resolved by the currently processed dictionary.
                var mayBeReplaced = combinedBuilder
                    .Where(kv => kv.Value.Resolution.IsTypeParameter)
                    .ToLookup(
                        kv => AsTypeResolutionKey(((ResolvedTypeKind.TypeParameter)kv.Value.Resolution).Item),
                        entry => entry.Key);

                // We need to ensure that the mappings for external type parameters are processed first,
                // to cover an edge case that would otherwise be indicated as a conflicting resolution.
                foreach (var (typeParam, paramRes) in resolution.Where(entry => mayBeReplaced.Contains(entry.Key)))
                {
                    // resolution of an external type parameter that is currently listed as value in the combined type resolution dictionary
                    foreach (var keyInCombined in mayBeReplaced[typeParam])
                    {
                        // If one of the values is a type parameter from the target callable,
                        // but it isn't mapped to itself then the combined resolution is invalid.
                        success = success && !InconsistentResolutionToNative(keyInCombined, paramRes);
                        combinedBuilder[keyInCombined] = paramRes;
                    }
                }

                // resolution of a type parameter that belongs to the target callable
                foreach (var (typeParam, paramRes) in resolution.Where(entry => entry.Key.Item1.Equals(target)))
                {
                    // A native type parameter cannot be resolved to another native type parameter, since this would constrain them.
                    success = success && !InconsistentResolutionToNative(typeParam, paramRes);
                    // Check that there is no conflicting resolution already defined.
                    var conflictingResolutionExists = combinedBuilder.TryGetValue(typeParam, out var current)
                        && !current.Equals(paramRes) && !ResolutionToTypeParameter(typeParam, current);
                    success = success && !conflictingResolutionExists;
                    combinedBuilder[typeParam] = paramRes;
                }

                if (resolution.Any(entry => !mayBeReplaced.Contains(entry.Key) && !entry.Key.Item1.Equals(target)))
                {
                    // It does not make sense to support this case, since there is no valid context in which type parameters
                    // belonging to multiple callables can/should be treated as concrete types simultaneously.
                    throw new ArgumentException("attempting to define resolution for type parameter that does not belong to target callable");
                }
            }

            combined = combinedBuilder.ToImmutable();
            QsCompilerError.Verify(combined.Keys.All(key => key.Item1.Equals(target)), "for type parameter that does not belong to target callable");
            return success;
        }

        /// <summary>
        /// This is a dictionary mapping source nodes to information about target nodes. This information is represented
        /// by a dictionary mapping target node to the edges pointing from the source node to the target node.
        /// </summary>
        private readonly Dictionary<CallGraphNode, Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>> _Dependencies =
            new Dictionary<CallGraphNode, Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>>();

        private QsNullable<ImmutableArray<ResolvedType>> RemovePositionFromTypeArgs(QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
            tArgs.IsValue
            ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(tArgs.Item.Select(StripPositionInfo.Apply).ToImmutableArray())
            : tArgs;

        private void RecordDependency(CallGraphNode callerKey, CallGraphNode calledKey, CallGraphEdge edge)
        {
            if (_Dependencies.TryGetValue(callerKey, out var deps))
            {
                deps[calledKey] = deps.TryGetValue(calledKey, out var edges)
                    ? edges.Add(edge)
                    : ImmutableArray.Create(edge);
            }
            else
            {
                var newDeps = new Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>();
                newDeps[calledKey] = ImmutableArray.Create(edge);
                _Dependencies[callerKey] = newDeps;
            }
        }

        /// <summary>
        /// Adds a dependency to the call graph using the caller's specialization and the called specialization's information.
        /// </summary>
        public void AddDependency(QsSpecialization callerSpec, QsQualifiedName calledName, QsSpecializationKind calledKind, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs, CallGraphEdge edge) =>
            AddDependency(
                callerSpec.Parent, callerSpec.Kind, callerSpec.TypeArguments,
                calledName, calledKind, calledTypeArgs,
                edge);

        /// <summary>
        /// Adds a dependency to the call graph using the relevant information from the caller's specialization and the called specialization.
        /// </summary>
        public void AddDependency(
            QsQualifiedName callerName, QsSpecializationKind callerKind, QsNullable<ImmutableArray<ResolvedType>> callerTypeArgs,
            QsQualifiedName calledName, QsSpecializationKind calledKind, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs,
            CallGraphEdge edge)
        {
            // Setting TypeArgs to Null because the type specialization is not implemented yet
            var callerKey = new CallGraphNode { CallableName = callerName, Kind = callerKind, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null };
            var calledKey = new CallGraphNode { CallableName = calledName, Kind = calledKind, TypeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null };
            RecordDependency(callerKey, calledKey, edge);
        }

        /// <summary>
        /// Returns all specializations that are used directly within the given caller, whether they are
        /// called, partially applied, or assigned. Each key in the returned dictionary represents a
        /// specialization that is used by the caller. Each value in the dictionary is an array of edges
        /// representing all the different ways the given caller specialization took a dependency on the
        /// specialization represented by the associated key.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetDirectDependencies(CallGraphNode callerSpec) =>
            _Dependencies.GetValueOrDefault(callerSpec, new Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>>());

        /// <summary>
        /// Returns all specializations that are used directly within the given caller, whether they are
        /// called, partially applied, or assigned. Each key in the returned dictionary represents a
        /// specialization that is used by the caller. Each value in the dictionary is an array of edges
        /// representing all the different ways the given caller specialization took a dependency on the
        /// specialization represented by the associated key.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetDirectDependencies(QsSpecialization callerSpec) =>
            GetDirectDependencies(new CallGraphNode { CallableName = callerSpec.Parent, Kind = callerSpec.Kind, TypeArgs = RemovePositionFromTypeArgs(callerSpec.TypeArguments) });

        // ToDo: this method needs a way of resolving type parameters before it can be completed
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetAllDependencies(CallGraphNode callerSpec)
        {
            throw new NotImplementedException();

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

        /// <summary>
        /// Returns all specializations that are used directly or indirectly within the given caller,
        /// whether they are called, partially applied, or assigned. Each key in the returned dictionary
        /// represents a specialization that is used by the caller. Each value in the dictionary is an
        /// array of edges representing all the different ways the given caller specialization took a
        /// dependency on the specialization represented by the associated key.
        /// </summary>
        public Dictionary<CallGraphNode, ImmutableArray<CallGraphEdge>> GetAllDependencies(QsSpecialization callerSpec) =>
            GetAllDependencies(new CallGraphNode { CallableName = callerSpec.Parent, Kind = callerSpec.Kind, TypeArgs = RemovePositionFromTypeArgs(callerSpec.TypeArguments) });

        /// <summary>
        /// Finds and returns a list of all cycles in the call graph, each one being represented by an array of nodes.
        /// To get the edges between the nodes of a given cycle, use the GetDirectDependencies method.
        /// </summary>
        public List<ImmutableArray<CallGraphNode>> GetCallCycles()
        {
            // ToDo: need to implement a robust algorithm for finding cycles
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This transformation walks through the compilation without changing it, building up a call graph as it does.
    /// This call graph is then returned to the user.
    /// </summary>
    public static class BuildCallGraph
    {
        public static CallGraph Apply(QsCompilation compilation)
        {
            var walker = new BuildGraph();

            var targetNs = NonNullable<string>.New("Microsoft.Quantum.Testing.TypeParameterResolution");
            walker.Namespaces.OnNamespace(compilation.Namespaces.First(ns => ns.Name.Equals(targetNs)));

            //foreach (var ns in compilation.Namespaces)
            //{
            //    walker.Namespaces.OnNamespace(ns);
            //}
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

                    CallGraph.TryCombineTypeResolutions(global.Item, out var combined, SharedState.typeParameterResolutions.ToArray());
                    SharedState.typeParameterResolutions = new List<TypeParameterResolutions>();
                    var edge = new CallGraphEdge { ParamResolutions = combined };

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

                        SharedState.graph.AddDependency(SharedState.spec, global.Item, kind, typeArgs, edge);
                    }
                    else
                    {
                        // The callable is being used in a non-call context, such as being
                        // assigned to a variable or passed as an argument to another callable,
                        // which means it could get a functor applied at some later time.
                        // We're conservative and add all 4 possible kinds.
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsBody, typeArgs, edge);
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsControlled, typeArgs, edge);
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsAdjoint, typeArgs, edge);
                        SharedState.graph.AddDependency(SharedState.spec, global.Item, QsSpecializationKind.QsControlledAdjoint, typeArgs, edge);
                    }
                }

                return ExpressionKind.InvalidExpr;
            }
        }
    }
}
