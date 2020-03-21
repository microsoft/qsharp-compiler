// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;


namespace Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
{
    using Concretion = Dictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;
    using GetConcreteIdentifierFunc = Func<Identifier.GlobalCallable, /*ImmutableConcretion*/ ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>, Identifier>;
    using ImmutableConcretion = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>;

    /// Class used to track call graph of a compilation.
    /// This class is *not* threadsafe.
    public class CallGraph
    {
        public struct SpecializationKey
        {
            public QsQualifiedName CallableName;
            public QsSpecializationKind Kind;
            public QsNullable<List<int>> TypeArgHash;
        }

        private Dictionary<SpecializationKey, HashSet<SpecializationKey>> dependencies = new Dictionary<SpecializationKey, HashSet<SpecializationKey>>();
        private Dictionary<int, ResolvedType> typeHashes = new Dictionary<int, ResolvedType>();

        private SpecializationKey SpecInfoToKey(QsSpecializationKind kind, QsQualifiedName parent, QsNullable<ImmutableArray<ResolvedType>> typeArgs)
        {
            List<int> getTypeArgHash(ImmutableArray<ResolvedType> tArgs)
            {
                return tArgs
                    .Select(t =>
                    {
                        var tHash = t.GetHashCode();
                        typeHashes[tHash] = t;
                        return tHash;
                    })
                    .ToList();
            }

            var typeArgHash = QsNullable<List<int>>.Null;
            if (typeArgs.IsValue)
            {
                typeArgHash = QsNullable<List<int>>.NewValue(getTypeArgHash(typeArgs.Item));
            }

            return new SpecializationKey() { Kind = kind, CallableName = parent, TypeArgHash = typeArgHash };
        }

        private SpecializationKey SpecToKey(QsSpecialization spec) =>
            SpecInfoToKey(spec.Kind, spec.Parent, spec.TypeArguments);

        private QsNullable<List<ResolvedType>> HashToTypeArgs(QsNullable<List<int>> tArgHash)
        {
            if (tArgHash.IsNull)
            {
                return QsNullable<List<ResolvedType>>.Null;
            }
            else
            {
                return QsNullable<List<ResolvedType>>.NewValue(tArgHash.Item
                    .Select(hash =>
                    {
                        if (typeHashes.TryGetValue(hash, out var t))
                        {
                            return t;
                        }
                        else
                        {
                            throw new ArgumentException("no type with the given hash has been listed");
                        }
                    })
                    .ToList());
            }
        }

        private void RecordDependency(SpecializationKey callerKey, SpecializationKey calledKey)
        {
            if (dependencies.TryGetValue(callerKey, out var deps))
            {
                deps.Add(calledKey);
            }
            else
            {
                var newDeps = new HashSet<SpecializationKey>();
                newDeps.Add(calledKey);
                dependencies[callerKey] = newDeps;
            }
        }

        public void AddDependency(QsSpecialization callerSpec, QsSpecializationKind calledKind, QsQualifiedName calledName, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs)
        {
            var callerKey = SpecToKey(callerSpec);
            var calledKey = SpecInfoToKey(calledKind, calledName, calledTypeArgs);
            RecordDependency(callerKey, calledKey);
        }

        public void AddDependency(
            QsSpecializationKind callerKind, QsQualifiedName callerName, QsNullable<ImmutableArray<ResolvedType>> callerTypeArgs,
            QsSpecializationKind calledKind, QsQualifiedName calledName, QsNullable<ImmutableArray<ResolvedType>> calledTypeArgs)
        {
            var callerKey = SpecInfoToKey(callerKind, callerName, callerTypeArgs);
            var calledKey = SpecInfoToKey(calledKind, calledName, calledTypeArgs);
            RecordDependency(callerKey, calledKey);
        }

        /// Returns all specializations that are used directly within the given caller,
        /// whether they are called, partially applied, or assigned.
        /// The returned specializations are identified by the full name of the callable,
        /// the specialization kind, as well as the resolved type arguments.
        /// The returned type arguments are the exact type arguments of the expression,
        /// and may thus be incomplete or correspond to subtypes of a defined specialization bundle.
        public ImmutableArray<(QsQualifiedName, QsSpecializationKind, QsNullable<List<ResolvedType>>)> GetDirectDependencies(QsSpecialization callerSpec)
        {
            var key = SpecToKey(callerSpec);
            if (dependencies.TryGetValue(key, out var deps))
            {
                 return deps
                    .Select(key => (key.CallableName, key.Kind, HashToTypeArgs(key.TypeArgHash)))
                    .ToImmutableArray();
            }
            else
            {
                return ImmutableArray<(QsQualifiedName, QsSpecializationKind, QsNullable<List<ResolvedType>>)>.Empty;
            }
        }

        /// Returns all specializations directly or indirectly used within the given caller,
        /// whether they are called, partially applied, or assigned.
        /// The returned specializations are identified by the full name of the callable,
        /// the specialization kind, as well as the resolved type arguments.
        /// The returned type arguments are the exact type arguments of the expression,
        /// and may thus be incomplete or correspond to subtypes of a defined specialization bundle.
        public ImmutableArray<(QsQualifiedName, QsSpecializationKind, QsNullable<List<ResolvedType>>)> GetAllDependencies(QsSpecialization callerSpec)
        {
            HashSet<SpecializationKey> WalkDependencyTree(SpecializationKey root, HashSet<SpecializationKey> accum)
            {
                if (dependencies.TryGetValue(root, out var next))
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

            var key = SpecToKey(callerSpec);
            return WalkDependencyTree(key, new HashSet<SpecializationKey>())
                .Select(key => (key.CallableName, key.Kind, HashToTypeArgs(key.TypeArgHash)))
                .ToImmutableArray();
        }
    }

    public static class CallGraphWalker
    {
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

            private BuildGraph(ImmutableDictionary<NonNullable<string>, IEnumerable<QsCallable>> namespaceCallables) : base(new TransformationState())
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

                            SharedState.graph.AddDependency(SharedState.spec, kind, global.Item, typeArgs);
                        }
                        else
                        {
                            // The callable is being used in a non-call context, such as being
                            // assigned to a variable or passed as an argument to another callable,
                            // which means it could get a functor applied at some later time.
                            // We're conservative and add all 4 possible kinds.
                            SharedState.graph.AddDependency(SharedState.spec, QsSpecializationKind.QsBody, global.Item, typeArgs);
                            SharedState.graph.AddDependency(SharedState.spec, QsSpecializationKind.QsControlled, global.Item, typeArgs);
                            SharedState.graph.AddDependency(SharedState.spec, QsSpecializationKind.QsAdjoint, global.Item, typeArgs);
                            SharedState.graph.AddDependency(SharedState.spec, QsSpecializationKind.QsControlledAdjoint, global.Item, typeArgs);
                        }
                    }

                    return ExpressionKind.InvalidExpr;
                }
            }
        }
    }
}
