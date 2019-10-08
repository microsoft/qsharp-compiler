// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SymbolManagement;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
{
    using LocalTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using GenericsCrate = Dictionary<QsQualifiedName, Dictionary<Concretion, NonNullable<string>>>;

    internal class Concretion : HashSet<(NonNullable<string>, LocalTypeKind)>
    {
        public override int GetHashCode()
        {
            int final = this.Select(x =>
               {
                   int first = x.Item1.Value.GetHashCode();
                   int second = NamespaceManager.TypeHash(ResolvedType.New(x.Item2));
                   string combined = (first, second).ToString();
                   int result = combined.GetHashCode();
                   return result;
               }).Aggregate((sum, i) => unchecked(sum + i)); // TODO: Use better hashing over enumerable
            return final;
        }

        public override bool Equals(object obj)
        {
            return this.GetHashCode() == obj.GetHashCode();
        }
    }

    public class ResolveGenericsSyntax :
        SyntaxTreeTransformation<ScopeTransformation<ResolveGenericsExpression>>
    {
        public static IEnumerable<QsNamespace> Apply(IEnumerable<QsNamespace> namespaces)
        {
            if (namespaces == null || namespaces.Contains(null)) throw new ArgumentNullException(nameof(namespaces));

            // Get list of generics
            GenericsCrate generics = new GenericsCrate();
            var Callables = namespaces.GlobalCallableResolutions();
            foreach (var callable in Callables)
            {
                if (callable.Value.Signature.TypeParameters.Any())
                {
                    //if(callable.Value.Specializations.Any(x => x.Implementation.IsProvided))
                    //{
                        generics.Add(callable.Key, new Dictionary<Concretion, NonNullable<string>>());
                    //}
                }
            }

            var filter = new ResolveGenericsSyntax(new ScopeTransformation<ResolveGenericsExpression>(new ResolveGenericsExpression(generics)));
            return namespaces.Select(ns => filter.Transform(ns));
        }

        public static IEnumerable<QsNamespace> Apply(params QsNamespace[] namespaces)
        {
            return Apply((IEnumerable<QsNamespace>)namespaces);
        }

        public ResolveGenericsSyntax(ScopeTransformation<ResolveGenericsExpression> scope) : base(scope) { }
    }

    public class ResolveGenericsExpression :
        ExpressionTransformation<ExpressionKindTransformation<ResolveGenericsExpression>>
    {
        private GenericsCrate Generics;

        internal ResolveGenericsExpression(GenericsCrate generics) : base(ex => new ExpressionKindTransformation<ResolveGenericsExpression>(ex as ResolveGenericsExpression)) { Generics = generics; }

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
                                    GetConcreteIdentifier(globalCallable, CreateConcretion(types)),
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
                result.Add((kvp.Key.TypeName, kvp.Value.Resolution));
            }
            return result;
        }

        private Identifier GetConcreteIdentifier(Identifier.GlobalCallable globalCallable, Concretion target)
        {
            // TODO: handle if globalCallable is not in Generics

            var concretions = Generics[globalCallable.Item];

            NonNullable<string> name;

            if (concretions.ContainsKey(target))
            {
                name = concretions[target];
            }
            else
            {
                // Create new name
                string nameString = Guid.NewGuid().ToString() + "_" + globalCallable.Item.Name.Value;

                // TODO: Check for identifier name collisions (not likely)
                // - check in global namespace
                // - check in concretion dict for current generic

                name = NonNullable<string>.New(nameString);
                concretions.Add(target, name);

                // TODO: Write a concrete definition for the generic, and use new name

            }

            return Identifier.NewGlobalCallable(new QsQualifiedName(globalCallable.Item.Namespace, name));
        }
    }
}