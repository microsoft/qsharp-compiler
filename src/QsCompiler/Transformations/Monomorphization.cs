// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;


namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
{
    using Concretion = Dictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;
    using GetConcreteIdentifierFunc = Func<Identifier.GlobalCallable, /*ImmutableConcretion*/ ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>, Identifier>;
    using ImmutableConcretion = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

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
        private struct Request
        {
            public QsQualifiedName originalName;
            public ImmutableConcretion typeResolutions;
            public QsQualifiedName concreteName;
        }

        private struct Response
        {
            public QsQualifiedName originalName;
            public ImmutableConcretion typeResolutions;
            public QsCallable concreteCallable;
        }

        public static QsCompilation Apply(QsCompilation compilation)
        {
            if (compilation == null || compilation.Namespaces.Contains(null)) throw new ArgumentNullException(nameof(compilation));

            var globals = compilation.Namespaces.GlobalCallableResolutions();

            var intrinsicCallableSet = globals
                .Where(kvp => kvp.Value.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                .Select(kvp => kvp.Key)
                .ToImmutableHashSet();

            var entryPoints = compilation.EntryPoints
                .Select(call => new Request
                {
                    originalName = call,
                    typeResolutions = ImmutableConcretion.Empty,
                    concreteName = call
                });

            var requests = new Stack<Request>(entryPoints);
            var responses = new List<Response>();

            while (requests.Any())
            {
                Request currentRequest = requests.Pop();

                // If there is a call to an unknown callable, throw exception
                if (!globals.TryGetValue(currentRequest.originalName, out QsCallable originalGlobal))
                    throw new ArgumentException($"Couldn't find definition for callable: {currentRequest.originalName}");

                var currentResponse = new Response
                {
                    originalName = currentRequest.originalName,
                    typeResolutions = currentRequest.typeResolutions,
                    concreteCallable = originalGlobal.WithFullName(name => currentRequest.concreteName)
                };

                GetConcreteIdentifierFunc getConcreteIdentifier = (globalCallable, types) =>
                    GetConcreteIdentifier(currentResponse, requests, responses, globalCallable, types);

                // Rewrite implementation
                currentResponse = ReplaceTypeParamImplementations.Apply(currentResponse);

                // Rewrite calls
                currentResponse = ReplaceTypeParamCalls.Apply(currentResponse, getConcreteIdentifier, intrinsicCallableSet);

                responses.Add(currentResponse);
            }

            return ResolveGenerics.Apply(compilation, responses, intrinsicCallableSet);
        }

        private static Identifier GetConcreteIdentifier(
                Response currentResponse,
                Stack<Request> requests,
                List<Response> responses,
                Identifier.GlobalCallable globalCallable,
                ImmutableConcretion types)
        {
            QsQualifiedName concreteName = globalCallable.Item;

            var typesHashSet = ImmutableHashSet<KeyValuePair<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>>.Empty;
            if (types != null && !types.IsEmpty)
            {
                typesHashSet = types.ToImmutableHashSet();
            }

            string name = null;

            // Check for recursive call
            if (currentResponse.originalName.Equals(globalCallable.Item) &&
                typesHashSet.SetEquals(currentResponse.typeResolutions))
            {
                name = currentResponse.concreteCallable.FullName.Name.Value;
            }

            // Search requests for identifier
            if (name == null)
            {
                name = requests
                    .Where(req =>
                        req.originalName.Equals(globalCallable.Item) &&
                        typesHashSet.SetEquals(req.typeResolutions))
                    .Select(req => req.concreteName.Name.Value)
                    .FirstOrDefault();
            }

            // Search responses for identifier
            if (name == null)
            {
                name = responses
                    .Where(res =>
                        res.originalName.Equals(globalCallable.Item) &&
                        typesHashSet.SetEquals(res.typeResolutions))
                    .Select(res => res.concreteCallable.FullName.Name.Value)
                    .FirstOrDefault();
            }

            // If identifier can't be found, make a new request
            if (name == null)
            {
                // If this is not a generic, do not change the name
                if (!typesHashSet.IsEmpty)
                {
                    // Create new name
                    concreteName = UniqueVariableNames.PrependGuid(globalCallable.Item);
                }

                requests.Push(new Request()
                {
                    originalName = globalCallable.Item,
                    typeResolutions = types,
                    concreteName = concreteName
                });
            }
            else // If the identifier was found, update with the name
            {
                concreteName = new QsQualifiedName(globalCallable.Item.Namespace, NonNullable<string>.New(name));
            }

            return Identifier.NewGlobalCallable(concreteName);
        }

        #region ResolveGenerics

        private class ResolveGenerics : SyntaxTreeTransformation<ResolveGenerics.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation, List<Response> responses, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
            {
                var filter = new ResolveGenerics(responses.ToLookup(res => res.concreteCallable.FullName.Namespace, res => res.concreteCallable), intrinsicCallableSet);

                return new QsCompilation(compilation.Namespaces.Select(ns => filter.Namespaces.OnNamespace(ns)).ToImmutableArray(), compilation.EntryPoints);
            }

            public class TransformationState
            {
                public readonly ILookup<NonNullable<string>, QsCallable> NamespaceCallables;
                public readonly ImmutableHashSet<QsQualifiedName> IntrinsicCallableSet;

                public TransformationState(ILookup<NonNullable<string>, QsCallable> namespaceCallables, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
                {
                    this.NamespaceCallables = namespaceCallables;
                    this.IntrinsicCallableSet = intrinsicCallableSet;
                }
            }

            /// <summary>
            /// Constructor for the ResolveGenericsSyntax class. Its transform function replaces global callables in the namespace.
            /// </summary>
            /// <param name="namespaceCallables">Maps namespace names to an enumerable of all global callables in that namespace.</param>
            private ResolveGenerics(ILookup<NonNullable<string>, QsCallable> namespaceCallables, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
                : base(new TransformationState(namespaceCallables, intrinsicCallableSet))
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                private bool NamespaceElementFilter(QsNamespaceElement elem)
                {
                    if (elem is QsNamespaceElement.QsCallable call)
                    {

                        return BuiltIn.RewriteStepDependencies.Contains(call.Item.FullName) || SharedState.IntrinsicCallableSet.Contains(call.Item.FullName);
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
                        .Where(NamespaceElementFilter)
                        .Concat(SharedState.NamespaceCallables[ns.Name].Select(QsNamespaceElement.NewQsCallable))
                        .ToImmutableArray());
                }
            }
        }

        #endregion

        #region RewriteImplementations

        private class ReplaceTypeParamImplementations :
            SyntaxTreeTransformation<ReplaceTypeParamImplementations.TransformationState>
        {
            public static Response Apply(Response current)
            {
                // Nothing to change if the current callable is already concrete
                if (current.typeResolutions == ImmutableConcretion.Empty) return current;

                var filter = new ReplaceTypeParamImplementations(current.typeResolutions);

                // Create a new response with the transformed callable
                return new Response
                {
                    originalName = current.originalName,
                    typeResolutions = current.typeResolutions,
                    concreteCallable = filter.Namespaces.OnCallableDeclaration(current.concreteCallable)
                };
            }

            public class TransformationState
            {
                public readonly ImmutableConcretion TypeParams;

                public TransformationState(ImmutableConcretion typeParams)
                {
                    this.TypeParams = typeParams;
                }
            }

            private ReplaceTypeParamImplementations(ImmutableConcretion typeParams) : base(new TransformationState(typeParams))
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Types = new TypeTransformation(this);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override ResolvedSignature OnSignature(ResolvedSignature s)
                {
                    // Remove the type parameters from the signature
                    s = new ResolvedSignature(
                        ImmutableArray<QsLocalSymbol>.Empty,
                        s.ArgumentType,
                        s.ReturnType,
                        s.Information
                        );
                    return base.OnSignature(s);
                }
            }

            private class TypeTransformation : TypeTransformation<TransformationState>
            {
                public TypeTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> OnTypeParameter(QsTypeParameter tp)
                {
                    if (SharedState.TypeParams.TryGetValue(Tuple.Create(tp.Origin, tp.TypeName), out var typeParam))
                    {
                        return typeParam.Resolution;
                    }
                    return QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>.NewTypeParameter(tp);
                }
            }
        }

        #endregion

        #region RewriteCalls

        private class ReplaceTypeParamCalls :
            SyntaxTreeTransformation<ReplaceTypeParamCalls.TransformationState>
        {
            public static Response Apply(Response current, GetConcreteIdentifierFunc getConcreteIdentifier, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet)
            {
                var filter = new ReplaceTypeParamCalls(getConcreteIdentifier, intrinsicCallableSet);

                // Create a new response with the transformed callable
                return new Response
                {
                    originalName = current.originalName,
                    typeResolutions = current.typeResolutions,
                    concreteCallable = filter.Namespaces.OnCallableDeclaration(current.concreteCallable)
                };
            }

            public class TransformationState
            {
                public readonly Concretion CurrentParamTypes = new Concretion();
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
                public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

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

                public override ImmutableConcretion OnTypeParamResolutions(ImmutableConcretion typeParams)
                {
                    // Merge the type params into the current dictionary
                   
                    foreach (var kvp in typeParams.Where(kv => !SharedState.IntrinsicCallableSet.Contains(kv.Key.Item1)))
                    {
                        SharedState.CurrentParamTypes.Add(kvp.Key, kvp.Value);
                    }

                    return typeParams.Where(kv => SharedState.IntrinsicCallableSet.Contains(kv.Key.Item1)).ToImmutableDictionary();
                }
            }

            private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    if (sym is Identifier.GlobalCallable global)
                    {
                        ImmutableConcretion applicableParams = SharedState.CurrentParamTypes
                            .Where(kvp => kvp.Key.Item1.Equals(global.Item))
                            .ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        // We want to skip over intrinsic callables. They will not be monomorphized.
                        if (!SharedState.IntrinsicCallableSet.Contains(global.Item))
                        {
                            // Create a new identifier
                            sym = SharedState.GetConcreteIdentifier(global, applicableParams);
                            tArgs = QsNullable<ImmutableArray<ResolvedType>>.Null;
                        }

                        // Remove Type Params used from the CurrentParamTypes
                        foreach (var key in applicableParams.Keys)
                        {
                            SharedState.CurrentParamTypes.Remove(key);
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
                public TypeTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent) { }

                public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> OnTypeParameter(QsTypeParameter tp)
                {
                    if (SharedState.CurrentParamTypes.TryGetValue(Tuple.Create(tp.Origin, tp.TypeName), out var typeParam))
                    {
                        return typeParam.Resolution;
                    }
                    return QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>.NewTypeParameter(tp);
                }
            }
        }

        #endregion
    }
}
