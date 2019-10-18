// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Optimizations;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
{
    using Concretion = HashSet<(QsTypeParameter, ResolvedType)>;

    internal struct Request
    {
        public QsQualifiedName originalName;
        public Concretion typeResolutions;
        public QsQualifiedName concreteName;
    }

    internal struct Response
    {
        public QsQualifiedName originalName;
        public Concretion typeResolutions;
        public QsCallable concreteCallable;
    }

    #region ResolveGenerics

    public class ResolveGenericsSyntax :
        SyntaxTreeTransformation<NoScopeTransformations>
    {
        ImmutableDictionary<NonNullable<string>, IEnumerable<QsCallable>> NamespaceCallables;

        public static IEnumerable<QsNamespace> Apply(IEnumerable<QsNamespace> namespaces, QsQualifiedName entryPointName)
        {
            if (namespaces == null || namespaces.Contains(null)) throw new ArgumentNullException(nameof(namespaces));

            var globals = namespaces.GlobalCallableResolutions();

            var entryPoints = globals
                .Where(call => call.Key.IsEqual(entryPointName)) // TODO: get list of entry points
                .Select(call => new Request
                {
                    originalName = call.Key,
                    typeResolutions = null,
                    concreteName = call.Key
                });

            var requests = new Stack<Request>(entryPoints);
            var responses = new List<Response>();

            while(requests.Any())
            {
                Request currentRequest = requests.Pop();

                // If there is a call to an unknown callable, throw exception
                if (!globals.ContainsKey(currentRequest.originalName))
                    throw new ArgumentException($"Couldn't find definition for callable: {currentRequest.originalName.Namespace.Value + "." + currentRequest.originalName.Name.Value}");

                var currentResponse = new Response
                {
                    originalName = currentRequest.originalName,
                    typeResolutions = currentRequest.typeResolutions,
                    concreteCallable = globals[currentRequest.originalName].WithFullName(name => currentRequest.concreteName)
                };

                // Rewrite implementation
                currentResponse = ReplaceTypeParamImplementationsSyntax.Apply(currentResponse);

                // Rewrite calls
                currentResponse = ReplaceTypeParamCallsSyntax.Apply(currentResponse, requests, responses);

                responses.Add(currentResponse);
            }

            var filter = new ResolveGenericsSyntax(responses
                .GroupBy(res => res.concreteCallable.FullName.Namespace)
                .ToImmutableDictionary(group => group.Key, group => group.Select(res => res.concreteCallable)));
            return namespaces.Select(ns => filter.Transform(ns));
        }

        internal ResolveGenericsSyntax(ImmutableDictionary<NonNullable<string>, IEnumerable<QsCallable>> namespaceCallables) : base(new NoScopeTransformations())
        {
            NamespaceCallables = namespaceCallables;
        }

        public override QsNamespace Transform(QsNamespace ns)
        {
            NamespaceCallables.TryGetValue(ns.Name, out IEnumerable<QsCallable> concretesInNs);

            // Add concrete definitions for callables back into the syntax tree
            // Remove generic (or unused) definitions
            return ns.WithElements(elems => elems
                .Where(elem => elem is QsNamespaceElement.QsCustomType)
                .Concat(concretesInNs?.Select(call => QsNamespaceElement.NewQsCallable(call)) ?? Enumerable.Empty<QsNamespaceElement>())
                .ToImmutableArray());
        }
    }

    #endregion

    #region RewriteImplementations

    internal class ReplaceTypeParamImplementationsSyntax :
        SyntaxTreeTransformation<MinorTransformations.ReplaceTypeParams>
    {
        public static Response Apply(Response current)
        {
            // Nothing to change if the current callable is already concrete
            if (current.typeResolutions == null) return current;

            var filter = new ReplaceTypeParamImplementationsSyntax(
                new MinorTransformations.ReplaceTypeParams(current.typeResolutions.ToImmutableDictionary(param => param.Item1, param => param.Item2)));

            // Create a new response with the transformed callable
            return new Response
            {
                originalName = current.originalName,
                typeResolutions = current.typeResolutions,
                concreteCallable = filter.onCallableImplementation(current.concreteCallable)
            };
        }

        public ReplaceTypeParamImplementationsSyntax(MinorTransformations.ReplaceTypeParams scope) : base(scope) { }

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

    #endregion

    #region RewriteCalls

    internal class ReplaceTypeParamCallsSyntax :
        SyntaxTreeTransformation<ScopeTransformation<ReplaceTypeParamCallsExpression>>
    {
        public static Response Apply(Response current, Stack<Request> requests, List<Response> responses)
        {
            var filter = new ReplaceTypeParamCallsSyntax(new ScopeTransformation<ReplaceTypeParamCallsExpression>(new ReplaceTypeParamCallsExpression(current, requests, responses)));

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

    internal class ReplaceTypeParamCallsExpression :
        ExpressionTransformation<ReplaceTypeParamCallsExpressionKind>
    {
        private Response CurrentResponse;
        private Stack<Request> Requests;
        private List<Response> Responses;

        public ReplaceTypeParamCallsExpression(Response currentResponse, Stack<Request> requests, List<Response> responses) :
            base(ex => new ReplaceTypeParamCallsExpressionKind(ex as ReplaceTypeParamCallsExpression))
        {
            CurrentResponse = currentResponse;
            Requests = requests;
            Responses = responses;
        }

        public override TypedExpression Transform(TypedExpression ex)
        {
            // TODO: This should be recursive and use a more robust data structure to handle the type information for generics
            ImmutableDictionary<QsTypeParameter, ResolvedType> types = ex.TypeParameterResolutions;

            if (types.Any() && ex.Expression is QsExpressionKind<TypedExpression, Identifier, ResolvedType>.CallLikeExpression call)
            {
                // For now, only resolve identifiers of global callables
                if (call.Item1.Expression is QsExpressionKind<TypedExpression, Identifier, ResolvedType>.Identifier id &&
                    id.Item1 is Identifier.GlobalCallable globalCallable)
                {
                    // Rebuild the method with the updated identifier
                    ex = new TypedExpression(
                        QsExpressionKind<TypedExpression, Identifier, ResolvedType>.NewCallLikeExpression(
                            new TypedExpression(
                                QsExpressionKind<TypedExpression, Identifier, ResolvedType>.NewIdentifier(
                                    GetConcreteIdentifier(globalCallable, types),
                                    id.Item2
                                    ),
                                call.Item1.TypeParameterResolutions,
                                call.Item1.ResolvedType,
                                call.Item1.InferredInformation,
                                call.Item1.Range
                                ),
                            call.Item2
                            ),
                        ex.TypeParameterResolutions,
                        ex.ResolvedType,
                        ex.InferredInformation,
                        ex.Range
                        );
                }
            }

            return base.Transform(ex);
        }

        private Concretion CreateConcretion(ImmutableDictionary<QsTypeParameter, ResolvedType> typeDict)
        {
            Concretion result = new Concretion();
            foreach (var kvp in typeDict)
            {
                result.Add((kvp.Key, kvp.Value));
            }
            return result;
        }

        private Identifier GetConcreteIdentifier(Identifier.GlobalCallable globalCallable, ImmutableDictionary<QsTypeParameter, ResolvedType> types)
        {
            Concretion target = null;
            if (types != null && !types.IsEmpty)
            {
                target = CreateConcretion(types);
            }

            string name = null;

            // Check for recursive call
            if (CurrentResponse.originalName.IsEqual(globalCallable.Item) &&
                CurrentResponse.typeResolutions.SetEquals(target))
            {
                name = CurrentResponse.concreteCallable.FullName.Name.Value;
            }

            // Search Requests for identifier
            if (name == null)
            {
                name = Requests
                    .Where(req =>
                        req.originalName.IsEqual(globalCallable.Item) &&
                        req.typeResolutions.SetEquals(target))
                    .Select(req => req.concreteName.Name.Value)
                    .FirstOrDefault();
            }

            // Search Responses for identifier
            if (name == null)
            {
                name = Responses
                    .Where(res =>
                        res.originalName.IsEqual(globalCallable.Item) &&
                        res.typeResolutions.SetEquals(target))
                    .Select(res => res.concreteCallable.FullName.Name.Value)
                    .FirstOrDefault();
            }

            // If identifier can't be found, make a new request
            if (name == null)
            {
                if (target != null)
                {
                    // Create new name
                    name = Guid.NewGuid().ToString() + "_" + globalCallable.Item.Name.Value;

                    // TODO: Check for identifier name collisions (not likely)
                    // - check in global namespace
                    // - check in concretion dict for current generic
                }
                else
                {
                    // If this is not a generic, do not change the name
                    name = globalCallable.Item.Name.Value;
                }

                Requests.Push(new Request()
                {
                    originalName = globalCallable.Item,
                    typeResolutions = target,
                    concreteName = new QsQualifiedName(globalCallable.Item.Namespace, NonNullable<string>.New(name))
                });
            }

            return Identifier.NewGlobalCallable(new QsQualifiedName(globalCallable.Item.Namespace, NonNullable<string>.New(name)));
        }
    }

    internal class ReplaceTypeParamCallsExpressionKind : ExpressionKindTransformation<ReplaceTypeParamCallsExpression>
    {
        public ReplaceTypeParamCallsExpressionKind(ReplaceTypeParamCallsExpression expr) : base(expr) { }
    }
    #endregion
}
