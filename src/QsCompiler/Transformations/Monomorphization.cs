// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;

namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
{
    using GetConcreteIdentifierFunc = Func<Identifier.GlobalCallable, /*TypeParameterResolutions*/ ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType>, Identifier>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType>;

    /// <summary>
    /// This transformation replaces callables with type parameters with concrete
    /// instances of the same callables. The concrete values for the type parameters
    /// are found from uses of the callables.
    /// This transformation also removes all callables that are not used directly or
    /// indirectly from any of the marked entry point.
    /// Intrinsic callables are not monomorphized or removed from the compilation.
    /// There are also some built-in callables that are also exempt from
    /// being removed from non-use, as they are needed for later rewrite steps.
    /// </summary>
    public static class Monomorphize
    {
        /// <summary>
        /// Performs Monomorphization on the given compilation.
        /// </summary>
        public static QsCompilation Apply(QsCompilation compilation, bool keepAllIntrinsics = true)
        {
            var globals = compilation.Namespaces.GlobalCallableResolutions();
            var concretizations = new List<QsCallable>();
            var concreteNames = new Dictionary<ConcreteCallGraphNode, QsQualifiedName>();

            var nodes = new ConcreteCallGraph(compilation).Nodes
                // Remove specialization information so that we only deal with the full callables.
                // Note: this only works fine if for all nodes in the call graph,
                // all existing functor specializations and their dependencies are also in the call graph.
                .Select(n => new ConcreteCallGraphNode(n.CallableName, QsSpecializationKind.QsBody, n.ParamResolutions))
                .ToImmutableHashSet();

            var getAccessModifiers = new GetAccessModifiers((typeName) => GetAccessModifier(compilation.Namespaces.GlobalTypeResolutions(), typeName));

            // Loop through the nodes, getting a list of concrete callables
            foreach (var node in nodes)
            {
                // If there is a call to an unknown callable, throw exception
                if (!globals.TryGetValue(node.CallableName, out QsCallable originalGlobal))
                {
                    throw new ArgumentException($"Couldn't find definition for callable: {node.CallableName}");
                }

                if (node.ParamResolutions.Any())
                {
                    // Get concrete name
                    var concreteName = UniqueVariableNames.PrependGuid(node.CallableName);

                    // Add to concrete name mapping
                    concreteNames[node] = concreteName;

                    // Generate the concrete version of the callable
                    var concrete = ReplaceTypeParamImplementations.Apply(originalGlobal, node.ParamResolutions, getAccessModifiers);
                    concretizations.Add(
                        concrete.WithFullName(oldName => concreteName)
                        .WithSpecializations(specs => specs.Select(spec => spec.WithParent(_ => concreteName)).ToImmutableArray()));
                }
                else
                {
                    concretizations.Add(originalGlobal);
                }
            }

            GetConcreteIdentifierFunc getConcreteIdentifier = (globalCallable, types) =>
                    GetConcreteIdentifier(concreteNames, globalCallable, types);

            var intrinsicCallableSet = globals
                .Where(kvp => kvp.Value.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                .Select(kvp => kvp.Key)
                .ToImmutableHashSet();

            var final = new List<QsCallable>();
            // Loop through concretizations, replacing all references to generics with their concrete counterparts
            foreach (var callable in concretizations)
            {
                final.Add(ReplaceTypeParamCalls.Apply(callable, getConcreteIdentifier, intrinsicCallableSet));
            }

            return ResolveGenerics.Apply(compilation, final, intrinsicCallableSet, keepAllIntrinsics);
        }

        #region ResolveGenerics

        private class ResolveGenerics : SyntaxTreeTransformation<ResolveGenerics.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation, List<QsCallable> callables, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet, bool keepIntrinsics)
            {
                var filter = new ResolveGenerics(callables.ToLookup(res => res.FullName.Namespace), intrinsicCallableSet, keepIntrinsics);
                return filter.OnCompilation(compilation);
            }

            public class TransformationState
            {
                public readonly ILookup<string, QsCallable> NamespaceCallables;
                public readonly ImmutableHashSet<QsQualifiedName> IntrinsicCallableSet;

                public TransformationState(ILookup<string, QsCallable> namespaceCallables, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
                {
                    this.NamespaceCallables = namespaceCallables;
                    this.IntrinsicCallableSet = intrinsicCallableSet;
                }
            }

            /// <summary>
            /// Constructor for the ResolveGenericsSyntax class. Its transform function replaces global callables in the namespace.
            /// </summary>
            /// <param name="namespaceCallables">Maps namespace names to an enumerable of all global callables in that namespace.</param>
            private ResolveGenerics(ILookup<string, QsCallable> namespaceCallables, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet, bool keepIntrinsics)
                : base(new TransformationState(namespaceCallables, intrinsicCallableSet))
            {
                this.Namespaces = new NamespaceTransformation(this, keepIntrinsics);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                private readonly bool keepIntrinsics;

                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent, bool keepIntrinsics) : base(parent)
                {
                    this.keepIntrinsics = keepIntrinsics;
                }

                private bool NamespaceElementFilter(QsNamespaceElement elem)
                {
                    if (elem is QsNamespaceElement.QsCallable call)
                    {
                        return BuiltIn.RewriteStepDependencies.Contains(call.Item.FullName) ||
                            (this.keepIntrinsics && this.SharedState.IntrinsicCallableSet.Contains(call.Item.FullName));
                    }
                    else
                    {
                        return true;
                    }
                }

                public override QsNamespace OnNamespace(QsNamespace ns)
                {
                    // Removes unused or generic callables from the namespace
                    // Adds in the used concrete callables
                    return ns.WithElements(elems => elems
                        .Where(this.NamespaceElementFilter)
                        .Concat(this.SharedState.NamespaceCallables[ns.Name].Select(QsNamespaceElement.NewQsCallable))
                        .ToImmutableArray());
                }
            }
        }

        #endregion

        #region RewriteImplementations

        private static AccessModifier GetAccessModifier(ImmutableDictionary<QsQualifiedName, QsCustomType> userDefinedTypes, QsQualifiedName typeName)
        {
            // If there is a reference to an unknown type, throw exception
            if (!userDefinedTypes.TryGetValue(typeName, out var type))
            {
                throw new ArgumentException($"Couldn't find definition for user defined type: {typeName}");
            }
            return type.Modifiers.Access;
        }

        private class ReplaceTypeParamImplementations :
            SyntaxTreeTransformation<ReplaceTypeParamImplementations.TransformationState>
        {
            public static QsCallable Apply(QsCallable callable, TypeParameterResolutions typeParams, GetAccessModifiers getAccessModifiers)
            {
                var filter = new ReplaceTypeParamImplementations(typeParams, getAccessModifiers);
                return filter.Namespaces.OnCallableDeclaration(callable);
            }

            public class TransformationState
            {
                public readonly TypeParameterResolutions TypeParams;
                public readonly GetAccessModifiers GetAccessModifiers;

                public TransformationState(TypeParameterResolutions typeParams, GetAccessModifiers getAccessModifiers)
                {
                    this.TypeParams = typeParams;
                    this.GetAccessModifiers = getAccessModifiers;
                }
            }

            private ReplaceTypeParamImplementations(TypeParameterResolutions typeParams, GetAccessModifiers getAccessModifiers)
                : base(new TransformationState(typeParams, getAccessModifiers))
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation<TransformationState>(this);
                this.StatementKinds = new StatementKindTransformation<TransformationState>(this);
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation<TransformationState>(this);
                this.Types = new TypeTransformation(this);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent)
                {
                }

                public override QsCallable OnCallableDeclaration(QsCallable c)
                {
                    var relaventAccessModifiers = this.SharedState.GetAccessModifiers.Apply(this.SharedState.TypeParams.Values)
                        .Append(c.Modifiers.Access);

                    c = new QsCallable(
                        c.Kind,
                        c.FullName,
                        c.Attributes,
                        new Modifiers(GetAccessModifiers.GetLeastAccess(relaventAccessModifiers)),
                        c.SourceFile,
                        c.Location,
                        c.Signature,
                        c.ArgumentTuple,
                        c.Specializations,
                        c.Documentation,
                        c.Comments);

                    return base.OnCallableDeclaration(c);
                }

                public override ResolvedSignature OnSignature(ResolvedSignature s)
                {
                    // Remove the type parameters from the signature
                    s = new ResolvedSignature(
                        ImmutableArray<QsLocalSymbol>.Empty,
                        s.ArgumentType,
                        s.ReturnType,
                        s.Information);
                    return base.OnSignature(s);
                }
            }

            private class ExpressionTransformation : ExpressionTransformation<TransformationState>
            {
                public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent)
                {
                }

                public override TypeParameterResolutions OnTypeParamResolutions(TypeParameterResolutions typeParams)
                {
                    // We don't want to process the keys of type parameter resolutions
                    return typeParams.ToImmutableDictionary(kvp => kvp.Key, kvp => this.Types.OnType(kvp.Value));
                }
            }

            private class TypeTransformation : TypeTransformation<TransformationState>
            {
                public TypeTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent)
                {
                }

                public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
                {
                    if (this.SharedState.TypeParams.TryGetValue(Tuple.Create(tp.Origin, tp.TypeName), out var typeParam))
                    {
                        return typeParam.Resolution;
                    }
                    return ResolvedTypeKind.NewTypeParameter(tp);
                }
            }
        }

        private class GetAccessModifiers : TypeTransformation<GetAccessModifiers.TransformationState>
        {
            public IEnumerable<AccessModifier> Apply(IEnumerable<ResolvedType> types)
            {
                this.SharedState.AccessModifiers.Clear();
                foreach (var res in types)
                {
                    this.OnType(res);
                }
                return this.SharedState.AccessModifiers.ToImmutableArray();
            }

            public static AccessModifier GetLeastAccess(IEnumerable<AccessModifier> modifiers)
            {
                // ToDo: this needs to be made more robust if access modifiers are changed.
                return modifiers.Any(ac => ac.IsInternal) ? AccessModifier.Internal : AccessModifier.DefaultAccess;
            }

            internal class TransformationState
            {
                public readonly HashSet<AccessModifier> AccessModifiers = new HashSet<AccessModifier>();
                public readonly Func<QsQualifiedName, AccessModifier> GetAccessModifier;

                public TransformationState(Func<QsQualifiedName, AccessModifier> getAccessModifier)
                {
                    this.GetAccessModifier = getAccessModifier;
                }
            }

            public GetAccessModifiers(Func<QsQualifiedName, AccessModifier> getAccessModifier)
                : base(new TransformationState(getAccessModifier), TransformationOptions.NoRebuild)
            {
            }

            public override ResolvedTypeKind OnUserDefinedType(UserDefinedType udt)
            {
                this.SharedState.AccessModifiers.Add(this.SharedState.GetAccessModifier(new QsQualifiedName(udt.Namespace, udt.Name)));
                return base.OnUserDefinedType(udt);
            }
        }

        #endregion

        #region RewriteCalls

        private static Identifier GetConcreteIdentifier(
            Dictionary<ConcreteCallGraphNode, QsQualifiedName> concreteNames,
            Identifier.GlobalCallable globalCallable,
            TypeParameterResolutions types)
        {
            if (types.IsEmpty)
            {
                return globalCallable;
            }

            var node = new ConcreteCallGraphNode(globalCallable.Item, QsSpecializationKind.QsBody, types);

            if (concreteNames.TryGetValue(node, out var name))
            {
                return Identifier.NewGlobalCallable(name);
            }
            else
            {
                return globalCallable;
            }
        }

        private class ReplaceTypeParamCalls :
            SyntaxTreeTransformation<ReplaceTypeParamCalls.TransformationState>
        {
            public static QsCallable Apply(QsCallable current, GetConcreteIdentifierFunc getConcreteIdentifier, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
            {
                var filter = new ReplaceTypeParamCalls(getConcreteIdentifier, intrinsicCallableSet);
                return filter.Namespaces.OnCallableDeclaration(current);
            }

            public class TransformationState
            {
                public readonly Stack<TypeParameterResolutions> CurrentTypeParamResolutions = new Stack<TypeParameterResolutions>();
                public readonly GetConcreteIdentifierFunc GetConcreteIdentifier;
                public readonly ImmutableHashSet<QsQualifiedName> IntrinsicCallableSet;
                public TypeParameterResolutions? LastCalculatedTypeResolutions = null;

                public TransformationState(GetConcreteIdentifierFunc getConcreteIdentifier, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
                {
                    this.GetConcreteIdentifier = getConcreteIdentifier;
                    this.IntrinsicCallableSet = intrinsicCallableSet;
                }
            }

            private ReplaceTypeParamCalls(GetConcreteIdentifierFunc getConcreteIdentifier, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
                : base(new TransformationState(getConcreteIdentifier, intrinsicCallableSet))
            {
                this.Namespaces = new NamespaceTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Statements = new StatementTransformation(this);
                this.StatementKinds = new StatementKindTransformation<TransformationState>(this);
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation(this);
            }

            private class StatementTransformation : StatementTransformation<TransformationState>
            {
                public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent)
                {
                }

                public override QsStatement OnStatement(QsStatement stm)
                {
                    this.SharedState.CurrentTypeParamResolutions.Clear();
                    this.SharedState.LastCalculatedTypeResolutions = null;
                    return base.OnStatement(stm);
                }
            }

            private class ExpressionTransformation : ExpressionTransformation<TransformationState>
            {
                public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent)
                {
                }

                public override TypeParameterResolutions OnTypeParamResolutions(TypeParameterResolutions typeParams)
                {
                    if (typeParams.Any())
                    {
                        var noIntrinsicRes = typeParams.Where(kvp => !this.SharedState.IntrinsicCallableSet.Contains(kvp.Key.Item1)).ToImmutableDictionary();
                        var intrinsicRes = typeParams.Where(kvp => this.SharedState.IntrinsicCallableSet.Contains(kvp.Key.Item1)).ToImmutableDictionary();

                        this.SharedState.CurrentTypeParamResolutions.Push(noIntrinsicRes);

                        return intrinsicRes;
                    }
                    else
                    {
                        return typeParams;
                    }
                }
            }

            private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent)
                {
                }

                public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    if (sym is Identifier.GlobalCallable global)
                    {
                        // We want to skip over intrinsic callables. They will not be monomorphized.
                        if (!this.SharedState.IntrinsicCallableSet.Contains(global.Item))
                        {
                            var combination = new TypeResolutionCombination(this.SharedState.CurrentTypeParamResolutions);
                            this.SharedState.LastCalculatedTypeResolutions = combination.CombinedResolutionDictionary;
                            var typeRes = combination.CombinedResolutionDictionary.FilterByOrigin(global.Item);

                            // Create a new identifier
                            sym = this.SharedState.GetConcreteIdentifier(global, typeRes);
                            tArgs = QsNullable<ImmutableArray<ResolvedType>>.Null;
                        }
                        this.SharedState.CurrentTypeParamResolutions.Clear();
                    }
                    else if (sym is Identifier.LocalVariable && tArgs.IsValue && tArgs.Item.Any())
                    {
                        throw new ArgumentException($"Local variables cannot have type arguments.");
                    }

                    return base.OnIdentifier(sym, tArgs);
                }
            }

            private class TypeTransformation : TypeTransformation<TransformationState>
            {
                public TypeTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent)
                {
                }

                // The purpose of overriding OnTypeParameter here is because we need to rewrite the type
                // of the global identifier expressions to resolve their containing the type parameter
                // references. These references are not with respect to the calling callable, which is why
                // they were not and could not be addressed in the ReplaceTypeParamImplementations transformation.
                public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
                {
                    if (this.SharedState.LastCalculatedTypeResolutions != null
                        && this.SharedState.LastCalculatedTypeResolutions.TryGetValue(Tuple.Create(tp.Origin, tp.TypeName), out var typeParam))
                    {
                        return typeParam.Resolution;
                    }
                    return ResolvedTypeKind.NewTypeParameter(tp);
                }
            }
        }

        #endregion
    }
}
