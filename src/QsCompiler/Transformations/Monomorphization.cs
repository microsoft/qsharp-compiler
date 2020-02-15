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


namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
{
    using Concretion = Dictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;
    using GetConcreteIdentifierFunc = Func<Identifier.GlobalCallable, /*ImmutableConcretion*/ ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>, Identifier>;
    using ImmutableConcretion = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    public static class MonomorphizationTransformation
    {
        public static QsCompilation Apply(QsCompilation compilation) => ResolveGenericsSyntax.Apply(compilation);

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

        #region ResolveGenerics

        private class ResolveGenericsSyntax :
            SyntaxTreeTransformation<NoScopeTransformations>
        {
            ImmutableDictionary<NonNullable<string>, IEnumerable<QsCallable>> NamespaceCallables;

            public static QsCompilation Apply(QsCompilation compilation)
            {
                if (compilation == null || compilation.Namespaces.Contains(null)) throw new ArgumentNullException(nameof(compilation));

                var globals = compilation.Namespaces.GlobalCallableResolutions();

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
                        throw new ArgumentException($"Couldn't find definition for callable: {currentRequest.originalName.Namespace.Value + "." + currentRequest.originalName.Name.Value}");

                    var currentResponse = new Response
                    {
                        originalName = currentRequest.originalName,
                        typeResolutions = currentRequest.typeResolutions,
                        concreteCallable = originalGlobal.WithFullName(name => currentRequest.concreteName)
                    };

                    GetConcreteIdentifierFunc getConcreteIdentifier = (globalCallable, types) =>
                        GetConcreteIdentifier(currentResponse, requests, responses, globalCallable, types);

                    // Rewrite implementation
                    currentResponse = ReplaceTypeParamImplementationsSyntax.Apply(currentResponse);

                    // Rewrite calls
                    currentResponse = ReplaceTypeParamCallsSyntax.Apply(currentResponse, getConcreteIdentifier);

                    responses.Add(currentResponse);
                }

                var filter = new ResolveGenericsSyntax(responses
                    .GroupBy(res => res.concreteCallable.FullName.Namespace)
                    .ToImmutableDictionary(group => group.Key, group => group.Select(res => res.concreteCallable)));
                return new QsCompilation(compilation.Namespaces.Select(ns => filter.Transform(ns)).ToImmutableArray(), compilation.EntryPoints);
            }

            /// <summary>
            /// Constructor for the ResolveGenericsSyntax class. Its transform function replaces global callables in the namespace.
            /// </summary>
            /// <param name="namespaceCallables">Maps namespace names to an enumerable of all global callables in that namespace.</param>
            public ResolveGenericsSyntax(ImmutableDictionary<NonNullable<string>, IEnumerable<QsCallable>> namespaceCallables) : base(new NoScopeTransformations())
            {
                NamespaceCallables = namespaceCallables;
            }

            public override QsNamespace Transform(QsNamespace ns)
            {
                NamespaceCallables.TryGetValue(ns.Name, out IEnumerable<QsCallable> concretesInNs);

                // Removes unused or generic callables from the namespace
                // Adds in the used concrete callables
                return ns.WithElements(elems => elems
                    .Where(elem => elem is QsNamespaceElement.QsCustomType)
                    .Concat(concretesInNs?.Select(call => QsNamespaceElement.NewQsCallable(call)) ?? Enumerable.Empty<QsNamespaceElement>())
                    .ToImmutableArray());
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
                        name = "_" + Guid.NewGuid().ToString("N") + "_" + globalCallable.Item.Name.Value;
                        concreteName = new QsQualifiedName(globalCallable.Item.Namespace, NonNullable<string>.New(name));
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
        }

        #endregion

        #region RewriteImplementations

        private class ReplaceTypeParamImplementationsSyntax :
            SyntaxTreeTransformation<ScopeTransformation<ExpressionTransformation<Core.ExpressionKindTransformationBase, ReplaceTypeParamImplementationsExpressionType>>>
        {
            public static Response Apply(Response current)
            {
                // Nothing to change if the current callable is already concrete
                if (current.typeResolutions == ImmutableConcretion.Empty) return current;

                var filter = new ReplaceTypeParamImplementationsSyntax(current.typeResolutions);

                // Create a new response with the transformed callable
                return new Response
                {
                    originalName = current.originalName,
                    typeResolutions = current.typeResolutions,
                    concreteCallable = filter.onCallableImplementation(current.concreteCallable)
                };
            }

            public ReplaceTypeParamImplementationsSyntax(ImmutableConcretion typeParams) : base(
                new ScopeTransformation<ExpressionTransformation<Core.ExpressionKindTransformationBase, ReplaceTypeParamImplementationsExpressionType>>(
                    new ExpressionTransformation<Core.ExpressionKindTransformationBase, ReplaceTypeParamImplementationsExpressionType>(
                        ex => new ExpressionKindTransformation<ExpressionTransformation<Core.ExpressionKindTransformationBase, ReplaceTypeParamImplementationsExpressionType>>(ex as ExpressionTransformation<Core.ExpressionKindTransformationBase, ReplaceTypeParamImplementationsExpressionType>),
                        ex => new ReplaceTypeParamImplementationsExpressionType(typeParams, ex as ExpressionTransformation<Core.ExpressionKindTransformationBase, ReplaceTypeParamImplementationsExpressionType>)
                        )))
            { }

            public override ResolvedSignature onSignature(ResolvedSignature s)
            {
                // Remove the type parameters from the signature
                s = new ResolvedSignature(
                    ImmutableArray<QsLocalSymbol>.Empty,
                    s.ArgumentType,
                    s.ReturnType,
                    s.Information
                    );
                return base.onSignature(s);
            }
        }

        private class ReplaceTypeParamImplementationsExpressionType :
                ExpressionTypeTransformation<ExpressionTransformation<Core.ExpressionKindTransformationBase, ReplaceTypeParamImplementationsExpressionType>>
        {
            ImmutableConcretion TypeParams;

            public ReplaceTypeParamImplementationsExpressionType(ImmutableConcretion typeParams, ExpressionTransformation<Core.ExpressionKindTransformationBase, ReplaceTypeParamImplementationsExpressionType> expr) : base(expr)
            {
                TypeParams = typeParams;
            }

            public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> onTypeParameter(QsTypeParameter tp)
            {
                if (TypeParams.TryGetValue(Tuple.Create(tp.Origin, tp.TypeName), out var typeParam))
                {
                    return typeParam.Resolution;
                }
                return QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>.NewTypeParameter(tp);
            }
        }

        #endregion

        #region RewriteCalls

        private class ReplaceTypeParamCallsSyntax :
            SyntaxTreeTransformation<ScopeTransformation<ReplaceTypeParamCallsExpression>>
        {
            public static Response Apply(Response current, GetConcreteIdentifierFunc getConcreteIdentifier)
            {
                var filter = new ReplaceTypeParamCallsSyntax(new ScopeTransformation<ReplaceTypeParamCallsExpression>(
                    new ReplaceTypeParamCallsExpression(new Concretion(), getConcreteIdentifier)));

                // Create a new response with the transformed callable
                return new Response
                {
                    originalName = current.originalName,
                    typeResolutions = current.typeResolutions,
                    concreteCallable = filter.onCallableImplementation(current.concreteCallable)
                };
            }

            public ReplaceTypeParamCallsSyntax(ScopeTransformation<ReplaceTypeParamCallsExpression> scope) : base(scope) { }

        }

        private class ReplaceTypeParamCallsExpression :
            ExpressionTransformation<ReplaceTypeParamCallsExpressionKind, ReplaceTypeParamCallsExpressionType>
        {
            private readonly Concretion CurrentParamTypes;

            public ReplaceTypeParamCallsExpression(Concretion currentParamTypes, GetConcreteIdentifierFunc getConcreteIdentifier) :
                base(ex => new ReplaceTypeParamCallsExpressionKind(ex as ReplaceTypeParamCallsExpression, currentParamTypes, getConcreteIdentifier),
                    ex => new ReplaceTypeParamCallsExpressionType(ex as ReplaceTypeParamCallsExpression, currentParamTypes))
            {
                CurrentParamTypes = currentParamTypes;
            }

            public override TypedExpression Transform(TypedExpression ex)
            {
                var range = this.onRangeInformation(ex.Range);
                var typeParamResolutions = this.onTypeParamResolutions(ex.TypeParameterResolutions)
                    .Select(kv => new Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>(kv.Key.Item1, kv.Key.Item2, kv.Value))
                    .ToImmutableArray();
                var exType = this.Types.Transform(ex.ResolvedType);
                var inferredInfo = this.onExpressionInformation(ex.InferredInformation);
                // Change the order so that Kind is transformed last.
                // This matters because the onTypeParamResolutions method builds up type param mappings in
                // the CurrentParamTypes dictionary that are then used, and removed from the
                // dictionary, in the next global callable identifier found under the Kind transformations.
                var kind = this.ExpressionKinds.Transform(ex.Expression);
                return new TypedExpression(kind, typeParamResolutions, exType, inferredInfo, range);
            }

            public override ImmutableConcretion onTypeParamResolutions(ImmutableConcretion typeParams)
            {
                // Merge the type params into the current dictionary
                foreach (var kvp in typeParams)
                {
                    CurrentParamTypes.Add(kvp.Key, kvp.Value);
                }

                return ImmutableConcretion.Empty;
            }
        }

        private class ReplaceTypeParamCallsExpressionKind : ExpressionKindTransformation<ReplaceTypeParamCallsExpression>
        {
            private readonly GetConcreteIdentifierFunc GetConcreteIdentifier;
            private Concretion CurrentParamTypes;

            public ReplaceTypeParamCallsExpressionKind(ReplaceTypeParamCallsExpression expr,
                Concretion currentParamTypes,
                GetConcreteIdentifierFunc getConcreteIdentifier) : base(expr)
            {
                GetConcreteIdentifier = getConcreteIdentifier;
                CurrentParamTypes = currentParamTypes;
            }

            public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.GlobalCallable global)
                {
                    ImmutableConcretion applicableParams = CurrentParamTypes
                        .Where(kvp => kvp.Key.Item1.Equals(global.Item))
                        .ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // Create a new identifier
                    sym = GetConcreteIdentifier(global, applicableParams);
                    tArgs = QsNullable<ImmutableArray<ResolvedType>>.Null;

                    // Remove Type Params used from the CurrentParamTypes
                    foreach (var key in applicableParams.Keys)
                    {
                        CurrentParamTypes.Remove(key);
                    }
                }
                else if (sym is Identifier.LocalVariable && tArgs.IsValue && tArgs.Item.Any())
                {
                    throw new ArgumentException($"Local variables cannot have type arguments.");
                }

                return base.onIdentifier(sym, tArgs);
            }
        }

        private class ReplaceTypeParamCallsExpressionType : ExpressionTypeTransformation<ReplaceTypeParamCallsExpression>
        {
            private Concretion CurrentParamTypes;

            public ReplaceTypeParamCallsExpressionType(ReplaceTypeParamCallsExpression expr, Concretion currentParamTypes) : base(expr)
            {
                CurrentParamTypes = currentParamTypes;
            }

            public override QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> onTypeParameter(QsTypeParameter tp)
            {
                if (CurrentParamTypes.TryGetValue(Tuple.Create(tp.Origin, tp.TypeName), out var typeParam))
                {
                    return typeParam.Resolution;
                }
                return QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>.NewTypeParameter(tp);
            }
        }

        #endregion
    }
}
