// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Optimizations;
using Microsoft.Quantum.QsCompiler.SymbolManagement;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
{
    using Concretion = HashSet<(QsTypeParameter, ResolvedType)>;
    using GetConcreteIdentifierFunc = Func<Identifier.GlobalCallable, ImmutableDictionary<QsTypeParameter, ResolvedType>, Identifier>;

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
                .Where(call => // TODO: get list of entry points
                    call.Key.Namespace.Value == entryPointName.Namespace.Value &&
                    call.Key.Name.Value == entryPointName.Name.Value)
                .Select(call => new Request
                {
                    originalName = call.Key,
                    typeResolutions = null,
                    concreteName = call.Key
                });

            var requests = new Stack<Request>(entryPoints);
            var responses = new List<Response>();

            while(requests.Count() > 0)
            {
                Request currentRequest = requests.Pop();

                // If there is a call to an unknown callable, throw exception
                if (!globals.ContainsKey(currentRequest.originalName)) throw new ArgumentNullException(); // TODO: need to throw a more valid exception

                var currentResponse = new Response
                {
                    originalName = currentRequest.originalName,
                    typeResolutions = currentRequest.typeResolutions,
                    concreteCallable = globals[currentRequest.originalName].WithFullName(name => currentRequest.concreteName)
                };

                GetConcreteIdentifierFunc getConcreteIdentifier = (globalCallable, types) =>
                {
                    return GetConcreteIdentifier(currentResponse, requests, responses, globalCallable, types);
                };

                // Rewrite implementation
                currentResponse = ReplaceTypeParamImplementationsSyntax.Apply(currentResponse);

                // Rewrite calls
                currentResponse = ReplaceTypeParamCallsSyntax.Apply(currentResponse, getConcreteIdentifier);

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

        private static Concretion CreateConcretion(ImmutableDictionary<QsTypeParameter, ResolvedType> typeDict)
        {
            Concretion result = new Concretion();
            foreach (var kvp in typeDict)
            {
                result.Add((kvp.Key, kvp.Value));
            }
            return result;
        }

        private static Identifier GetConcreteIdentifier(
            Response currentResponse,
            Stack<Request> requests,
            List<Response> responses,
            Identifier.GlobalCallable globalCallable,
            ImmutableDictionary<QsTypeParameter, ResolvedType> types)
        {
            Concretion target = null;
            if (types != null && !types.IsEmpty)
            {
                target = CreateConcretion(types);
            }

            string name = null;

            // Check for recursive call
            if (currentResponse.originalName.Namespace.Value == globalCallable.Item.Namespace.Value &&
                currentResponse.originalName.Name.Value == globalCallable.Item.Name.Value &&
                currentResponse.typeResolutions.SetEquals(target))
            {
                name = currentResponse.concreteCallable.FullName.Name.Value;
            }

            // Search Requests for identifier
            if (name == null)
            {
                name = requests
                    .Where(req =>
                        req.originalName.Namespace.Value == globalCallable.Item.Namespace.Value &&
                        req.originalName.Name.Value == globalCallable.Item.Name.Value &&
                        req.typeResolutions.SetEquals(target))
                    .Select(req => req.concreteName.Name.Value)
                    .FirstOrDefault();
            }

            // Search Responses for identifier
            if (name == null)
            {
                name = responses
                    .Where(res =>
                        res.originalName.Namespace.Value == globalCallable.Item.Namespace.Value &&
                        res.originalName.Name.Value == globalCallable.Item.Name.Value &&
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

                requests.Push(new Request()
                {
                    originalName = globalCallable.Item,
                    typeResolutions = target,
                    concreteName = new QsQualifiedName(globalCallable.Item.Namespace, NonNullable<string>.New(name))
                });
            }

            return Identifier.NewGlobalCallable(new QsQualifiedName(globalCallable.Item.Namespace, NonNullable<string>.New(name)));
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
        public static Response Apply(Response current, GetConcreteIdentifierFunc getConcreteIdentifier)
        {
            var filter = new ReplaceTypeParamCallsSyntax(new ScopeTransformation<ReplaceTypeParamCallsExpression>(new ReplaceTypeParamCallsExpression(new Dictionary<QsTypeParameter, ResolvedType>(), getConcreteIdentifier)));

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
        private Dictionary<QsTypeParameter, ResolvedType> CurrentParamTypes;

        public ReplaceTypeParamCallsExpression(Dictionary<QsTypeParameter, ResolvedType> currentParamTypes, GetConcreteIdentifierFunc getConcreteIdentifier) :
            base(ex => new ReplaceTypeParamCallsExpressionKind(ex as ReplaceTypeParamCallsExpression, currentParamTypes, getConcreteIdentifier))
        {
            CurrentParamTypes = currentParamTypes;
        }

        public override ImmutableDictionary<QsTypeParameter, ResolvedType> onTypeParamResolutions(ImmutableDictionary<QsTypeParameter, ResolvedType> typeParams)
        {
            // Merge the type params into the current dictionary
            foreach (var kvp in typeParams)
            {
                CurrentParamTypes.Add(kvp.Key, kvp.Value);
            }

            // TODO: empty the dictionary
            //return ImmutableDictionary<QsTypeParameter, ResolvedType>.Empty;

            return base.onTypeParamResolutions(typeParams);
        }
    }

    internal class ReplaceTypeParamCallsExpressionKind : ExpressionKindTransformation<ReplaceTypeParamCallsExpression>
    {
        private Dictionary<QsTypeParameter, ResolvedType> CurrentParamTypes;

        private GetConcreteIdentifierFunc GetConcreteIdentifier;

        public ReplaceTypeParamCallsExpressionKind(ReplaceTypeParamCallsExpression expr,
            Dictionary<QsTypeParameter, ResolvedType> currentParamTypes,
            GetConcreteIdentifierFunc getConcreteIdentifier) : base(expr)
        {
            GetConcreteIdentifier = getConcreteIdentifier;
            CurrentParamTypes = currentParamTypes;
        }

        public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
        {
            if (sym is Identifier.GlobalCallable global)
            {
                var applicableParams = CurrentParamTypes
                    .Where(kvp =>
                        kvp.Key.Origin.Namespace.Value == global.Item.Namespace.Value &&
                        kvp.Key.Origin.Name.Value == global.Item.Name.Value)
                    .ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (applicableParams.Any())
                {
                    // Create a new identifier
                    sym = GetConcreteIdentifier(global, applicableParams);
                    tArgs = QsNullable<ImmutableArray<ResolvedType>>.Null; // TODO: Check that this is correct

                    // Remove Type Params used from the CurrentParamTypes
                    foreach (var key in applicableParams.Keys)
                    {
                        CurrentParamTypes.Remove(key);
                    }
                }
            }
            return base.onIdentifier(sym, tArgs);
        }
    }

    #endregion
}