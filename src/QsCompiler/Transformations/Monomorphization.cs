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
    using Concretization = Dictionary<Tuple<QsQualifiedName, string>, ResolvedType>;
    using GetConcreteIdentifierFunc = Func<Identifier.GlobalCallable, /*ImmutableConcretization*/ ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType>, Identifier>;
    using ImmutableConcretization = ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

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
        public static QsCompilation Apply(QsCompilation compilation)
        {
            var globals = compilation.Namespaces.GlobalCallableResolutions();
            var concretizations = new List<QsCallable>();
            var concreteNames = new Dictionary<ConcreteCallGraphNode, QsQualifiedName>();

            var nodes = new ConcreteCallGraph(compilation).Nodes
                // Remove specialization information so that we only deal with the full callables.
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
                    concretizations.Add(concrete.WithFullName(oldName => concreteName));
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

            return ResolveGenerics.Apply(compilation, final, intrinsicCallableSet);
        }

        private static Identifier GetConcreteIdentifier(
            Dictionary<ConcreteCallGraphNode, QsQualifiedName> concreteNames,
            Identifier.GlobalCallable globalCallable,
            ImmutableConcretization types)
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

        private static AccessModifier GetAccessModifier(ImmutableDictionary<QsQualifiedName, QsCustomType> userDefinedTypes, QsQualifiedName typeName)
        {
            // If there is a reference to an unknown type, throw exception
            if (!userDefinedTypes.TryGetValue(typeName, out var type))
            {
                throw new ArgumentException($"Couldn't find definition for user defined type: {typeName}");
            }
            return type.Modifiers.Access;
        }

        #region ResolveGenerics

        private class ResolveGenerics : SyntaxTreeTransformation<ResolveGenerics.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation, List<QsCallable> callables, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
            {
                var filter = new ResolveGenerics(callables.ToLookup(res => res.FullName.Namespace), intrinsicCallableSet);

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
            private ResolveGenerics(ILookup<string, QsCallable> namespaceCallables, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
                : base(new TransformationState(namespaceCallables, intrinsicCallableSet))
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent)
                {
                }

                private bool NamespaceElementFilter(QsNamespaceElement elem)
                {
                    if (elem is QsNamespaceElement.QsCallable call)
                    {
                        return BuiltIn.RewriteStepDependencies.Contains(call.Item.FullName) || this.SharedState.IntrinsicCallableSet.Contains(call.Item.FullName);
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

        private class ReplaceTypeParamImplementations :
            SyntaxTreeTransformation<ReplaceTypeParamImplementations.TransformationState>
        {
            public static QsCallable Apply(QsCallable callable, ImmutableConcretization typeParams, GetAccessModifiers getAccessModifiers)
            {
                var filter = new ReplaceTypeParamImplementations(typeParams, getAccessModifiers);
                return filter.Namespaces.OnCallableDeclaration(callable);
            }

            public class TransformationState
            {
                public readonly ImmutableConcretization TypeParams;
                public readonly GetAccessModifiers GetAccessModifiers;

                public TransformationState(ImmutableConcretization typeParams, GetAccessModifiers getAccessModifiers)
                {
                    this.TypeParams = typeParams;
                    this.GetAccessModifiers = getAccessModifiers;
                }
            }

            private ReplaceTypeParamImplementations(ImmutableConcretization typeParams, GetAccessModifiers getAccessModifiers)
                : base(new TransformationState(typeParams, getAccessModifiers))
            {
                this.Namespaces = new NamespaceTransformation(this);
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
                public readonly Concretization CurrentParamTypes = new Concretization();
                public readonly GetConcreteIdentifierFunc GetConcreteIdentifier;
                public readonly ImmutableHashSet<QsQualifiedName> IntrinsicCallableSet;

                public TransformationState(GetConcreteIdentifierFunc getConcreteIdentifier, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
                {
                    this.GetConcreteIdentifier = getConcreteIdentifier;
                    this.IntrinsicCallableSet = intrinsicCallableSet;
                }
            }

            private ReplaceTypeParamCalls(GetConcreteIdentifierFunc getConcreteIdentifier, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
                : base(new TransformationState(getConcreteIdentifier, intrinsicCallableSet))
            {
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation(this);
            }

            private class ExpressionTransformation : ExpressionTransformation<TransformationState>
            {
                public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent)
                {
                }

                public override TypedExpression OnTypedExpression(TypedExpression ex)
                {
                    var range = this.OnRangeInformation(ex.Range);
                    var typeParamResolutions = this.OnTypeParamResolutions(ex.TypeParameterResolutions)
                        .Select(kv => Tuple.Create(kv.Key.Item1, kv.Key.Item2, kv.Value))
                        .ToImmutableArray();
                    var exType = this.Types.OnType(ex.ResolvedType);
                    var inferredInfo = this.OnExpressionInformation(ex.InferredInformation);
                    // Change the order so that Kind is transformed last.
                    // This matters because the onTypeParamResolutions method builds up type param mappings in
                    // the CurrentParamTypes dictionary that are then used, and removed from the
                    // dictionary, in the next global callable identifier found under the Kind transformations.
                    var kind = this.ExpressionKinds.OnExpressionKind(ex.Expression);
                    return new TypedExpression(kind, typeParamResolutions, exType, inferredInfo, range);
                }

                public override ImmutableConcretization OnTypeParamResolutions(ImmutableConcretization typeParams)
                {
                    // Merge the type params into the current dictionary

                    foreach (var kvp in typeParams.Where(kv => !this.SharedState.IntrinsicCallableSet.Contains(kv.Key.Item1)))
                    {
                        this.SharedState.CurrentParamTypes.Add(kvp.Key, kvp.Value);
                    }

                    return typeParams.Where(kv => this.SharedState.IntrinsicCallableSet.Contains(kv.Key.Item1)).ToImmutableDictionary();
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
                        ImmutableConcretization applicableParams = this.SharedState.CurrentParamTypes
                            .Where(kvp => kvp.Key.Item1.Equals(global.Item))
                            .ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        // We want to skip over intrinsic callables. They will not be monomorphized.
                        if (!this.SharedState.IntrinsicCallableSet.Contains(global.Item))
                        {
                            // Create a new identifier
                            sym = this.SharedState.GetConcreteIdentifier(global, applicableParams);
                            tArgs = QsNullable<ImmutableArray<ResolvedType>>.Null;
                        }

                        // Remove Type Params used from the CurrentParamTypes
                        foreach (var key in applicableParams.Keys)
                        {
                            this.SharedState.CurrentParamTypes.Remove(key);
                        }
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

                public override ResolvedTypeKind OnTypeParameter(QsTypeParameter tp)
                {
                    if (this.SharedState.CurrentParamTypes.TryGetValue(Tuple.Create(tp.Origin, tp.TypeName), out var typeParam))
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
