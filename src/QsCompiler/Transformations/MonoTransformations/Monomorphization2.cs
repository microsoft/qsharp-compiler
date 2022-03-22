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
    /// Monomorphizing intrinsic callables is optional and intrinsics can be prevented
    /// from being monomorphized if the monomorphizeIntrinsics parameter is set to false.
    /// There are also some built-in callables that are also exempt from
    /// being removed from non-use, as they are needed for later rewrite steps.
    /// </summary>
    public static class Monomorphize
    {
        private static bool IsGeneric(QsCallable callable) =>
            callable.Signature.TypeParameters.Any() || callable.Specializations.Any(spec => spec.Signature.TypeParameters.Any());

        /// <summary>
        /// Performs Monomorphization on the given compilation. If the monomorphizeIntrinsics parameter
        /// is set to false, then intrinsics will not be monomorphized.
        /// </summary>
        public static QsCompilation Apply(QsCompilation compilation, bool monomorphizeIntrinsics = false)
        {
            var globals = compilation.Namespaces.GlobalCallableResolutions();
            var concretizations = new List<QsCallable>();
            var concreteNamesMap = new Dictionary<ConcreteCallGraphNode, QsQualifiedName>();

            var nodesWithResolutions = new ConcreteCallGraph(compilation).Nodes

                // Remove specialization information so that we only deal with the full callables.
                // Note: this only works fine if for all nodes in the call graph,
                // all existing functor specializations and their dependencies are also in the call graph.
                .Select(n => new ConcreteCallGraphNode(n.CallableName, QsSpecializationKind.QsBody, n.ParamResolutions))
                .Where(n => n.ParamResolutions.Any())
                .ToImmutableHashSet();

            var getAccessModifiers = new GetAccessModifiers((typeName) => GetAccessModifier(compilation.Namespaces.GlobalTypeResolutions(), typeName));

            // Loop through the nodes, getting a list of concrete callables
            foreach (var node in nodesWithResolutions)
            {
                // If there is a call to an unknown callable, throw exception
                if (!globals.TryGetValue(node.CallableName, out var originalGlobal))
                {
                    throw new ArgumentException($"Couldn't find definition for callable: {node.CallableName}");
                }

                if (monomorphizeIntrinsics || !originalGlobal.IsIntrinsic)
                {
                    // Get concrete name
                    var concreteName = NameDecorator.PrependGuid(node.CallableName);

                    // Add to concrete name mapping
                    concreteNamesMap[node] = concreteName;

                    // Generate the concrete version of the callable
                    var concrete =
                        ReplaceTypeParamImplementations.Apply(originalGlobal, node.ParamResolutions, getAccessModifiers)
                        .WithFullName(oldName => concreteName)
                        .WithSpecializations(specs => specs.Select(spec => spec.WithParent(_ => concreteName)).ToImmutableArray());
                    concretizations.Add(concrete);
                }
            }

            var callablesByNamespace = concretizations.ToLookup(x => x.FullName.Namespace);
            var namespacesWithImpls = compilation.Namespaces.Select(ns =>
            {
                var elemsToAdd = callablesByNamespace[ns.Name].Select(call => QsNamespaceElement.NewQsCallable(call));

                return ns.WithElements(elems =>
                    elems
                    .Where(elem =>
                        !(elem is QsNamespaceElement.QsCallable call)
                        || !IsGeneric(call.Item)
                        || (call.Item.IsIntrinsic && !monomorphizeIntrinsics))
                    .Concat(elemsToAdd)
                    .ToImmutableArray());
            }).ToImmutableArray();

            var compWithImpls = new QsCompilation(namespacesWithImpls, compilation.EntryPoints);

            GetConcreteIdentifierFunc getConcreteIdentifier = (globalCallable, types) =>
                    GetConcreteIdentifier(concreteNamesMap, globalCallable, types);

            var intrinsicsToKeep = monomorphizeIntrinsics
                ? ImmutableHashSet<QsQualifiedName>.Empty
                : globals
                    .Where(kvp => kvp.Value.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                    .Select(kvp => kvp.Key)
                    .ToImmutableHashSet();

            return ReplaceTypeParamCalls.Apply(compWithImpls, getConcreteIdentifier, intrinsicsToKeep);
        }

        /* Rewrite Implementations */

        private static Access GetAccessModifier(ImmutableDictionary<QsQualifiedName, QsCustomType> userDefinedTypes, QsQualifiedName typeName)
        {
            // If there is a reference to an unknown type, throw exception
            if (!userDefinedTypes.TryGetValue(typeName, out var type))
            {
                throw new ArgumentException($"Couldn't find definition for user defined type: {typeName}");
            }

            return type.Access;
        }

        private class ReplaceTypeParamImplementations : MonoTransformation
        {
            public static QsCallable Apply(QsCallable callable, TypeParameterResolutions typeParams, GetAccessModifiers getAccessModifiers)
            {
                var filter = new ReplaceTypeParamImplementations(typeParams, getAccessModifiers);
                return filter.OnCallableDeclaration(callable);
            }

            private TypeParameterResolutions TypeParams { get; }

            private GetAccessModifiers GetAccessModifiers { get; }

            private ReplaceTypeParamImplementations(TypeParameterResolutions typeParams, GetAccessModifiers getAccessModifiers)
                : base()
            {
                this.TypeParams = typeParams;
                this.GetAccessModifiers = getAccessModifiers;
            }

            public override QsCallable OnCallableDeclaration(QsCallable c)
            {
                var relevantAccessModifiers = this.GetAccessModifiers.Apply(this.TypeParams.Values)
                    .Append(c.Access);

                c = new QsCallable(
                    c.Kind,
                    c.FullName,
                    c.Attributes,
                    relevantAccessModifiers.Min(),
                    c.Source,
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

            public override TypeParameterResolutions OnTypeParamResolutions(TypeParameterResolutions typeParams)
            {
                // We don't want to process the keys of type parameter resolutions
                return typeParams.ToImmutableDictionary(kvp => kvp.Key, kvp => this.OnType(kvp.Value));
            }

            public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
            {
                if (this.TypeParams.TryGetValue(Tuple.Create(tp.Origin, tp.TypeName), out var typeParam))
                {
                    return typeParam.Resolution;
                }

                return ResolvedTypeKind.NewTypeParameter(tp);
            }
        }

        private class GetAccessModifiers : MonoTransformation
        {
            private HashSet<Access> AccessModifiers { get; } = new HashSet<Access>();

            private Func<QsQualifiedName, Access> GetAccessModifier { get; }

            public GetAccessModifiers(Func<QsQualifiedName, Access> getAccessModifier)
                : base(TransformationOptions.NoRebuild)
            {
                this.GetAccessModifier = getAccessModifier;
            }

            public IEnumerable<Access> Apply(IEnumerable<ResolvedType> types)
            {
                this.AccessModifiers.Clear();
                foreach (var res in types)
                {
                    this.OnType(res);
                }

                return this.AccessModifiers.ToImmutableArray();
            }

            public override ResolvedTypeKind OnUserDefinedType(UserDefinedType udt)
            {
                this.AccessModifiers.Add(this.GetAccessModifier(new QsQualifiedName(udt.Namespace, udt.Name)));
                return base.OnUserDefinedType(udt);
            }
        }

        /* Rewrite Calls */

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

        private class ReplaceTypeParamCalls : MonoTransformation
        {
            public static QsCallable Apply(QsCallable current, GetConcreteIdentifierFunc getConcreteIdentifier, ImmutableHashSet<QsQualifiedName> intrinsicsToKeep)
            {
                var filter = new ReplaceTypeParamCalls(getConcreteIdentifier, intrinsicsToKeep);
                return filter.OnCallableDeclaration(current);
            }

            public static QsCompilation Apply(QsCompilation compilation, GetConcreteIdentifierFunc getConcreteIdentifier, ImmutableHashSet<QsQualifiedName> intrinsicsToKeep)
            {
                var filter = new ReplaceTypeParamCalls(getConcreteIdentifier, intrinsicsToKeep);
                return filter.OnCompilation(compilation);
            }

            private Stack<TypeParameterResolutions> CurrentTypeParamResolutions { get; } = new Stack<TypeParameterResolutions>();

            private GetConcreteIdentifierFunc GetConcreteIdentifier { get; }

            private ImmutableHashSet<QsQualifiedName> IntrinsicsToKeep { get; }

            private TypeParameterResolutions? LastCalculatedTypeResolutions { get; set; } = null;

            private ReplaceTypeParamCalls(GetConcreteIdentifierFunc getConcreteIdentifier, ImmutableHashSet<QsQualifiedName> intrinsicsToKeep)
                : base()
            {
                this.GetConcreteIdentifier = getConcreteIdentifier;
                this.IntrinsicsToKeep = intrinsicsToKeep;
            }

            public override QsStatement OnStatement(QsStatement stm)
            {
                this.CurrentTypeParamResolutions.Clear();
                this.LastCalculatedTypeResolutions = null;
                return base.OnStatement(stm);
            }

            public override TypeParameterResolutions OnTypeParamResolutions(TypeParameterResolutions typeParams)
            {
                if (typeParams.Any())
                {
                    var noIntrinsicRes = typeParams.Where(kvp => !this.IntrinsicsToKeep.Contains(kvp.Key.Item1)).ToImmutableDictionary();
                    var intrinsicRes = typeParams.Where(kvp => this.IntrinsicsToKeep.Contains(kvp.Key.Item1)).ToImmutableDictionary();

                    this.CurrentTypeParamResolutions.Push(noIntrinsicRes);

                    return intrinsicRes;
                }
                else
                {
                    return typeParams;
                }
            }

            public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.GlobalCallable global)
                {
                    // We want to skip over callables listed in IntrinsicsToKeep; they will not be monomorphized.
                    if (!this.IntrinsicsToKeep.Contains(global.Item))
                    {
                        var combination = new TypeResolutionCombination(this.CurrentTypeParamResolutions);
                        this.LastCalculatedTypeResolutions = combination.CombinedResolutionDictionary;
                        var typeRes = combination.CombinedResolutionDictionary.FilterByOrigin(global.Item);

                        // Create a new identifier
                        sym = this.GetConcreteIdentifier(global, typeRes);
                        tArgs = QsNullable<ImmutableArray<ResolvedType>>.Null;
                    }

                    this.CurrentTypeParamResolutions.Clear();
                }
                else if (sym is Identifier.LocalVariable && tArgs.IsValue && tArgs.Item.Any())
                {
                    throw new ArgumentException($"Local variables cannot have type arguments.");
                }

                return base.OnIdentifier(sym, tArgs);
            }

            // The purpose of overriding OnTypeParameter here is because we need to rewrite the type
            // of the global identifier expressions to resolve their containing the type parameter
            // references. These references are not with respect to the calling callable, which is why
            // they were not and could not be addressed in the ReplaceTypeParamImplementations transformation.
            public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
            {
                if (this.LastCalculatedTypeResolutions != null
                    && this.LastCalculatedTypeResolutions.TryGetValue(Tuple.Create(tp.Origin, tp.TypeName), out var typeParam))
                {
                    return typeParam.Resolution;
                }

                return ResolvedTypeKind.NewTypeParameter(tp);
            }
        }
    }
}
