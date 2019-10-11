// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Optimizations;
using Microsoft.Quantum.QsCompiler.SymbolManagement;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
//using Microsoft.Quantum.QsCompiler.Optimizations;

namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
{
    using LocalTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using GenericsCrate = Dictionary<QsQualifiedName, (QsCallable, Dictionary<Concretion, QsCallable>)>;

    internal class Concretion : HashSet<(NonNullable<string>, LocalTypeKind)>
    {
        public override int GetHashCode()
        {
            return this.Select(x => (x.Item1.Value.GetHashCode(), NamespaceManager.TypeHash(ResolvedType.New(x.Item2))).ToString().GetHashCode())
                .Aggregate((sum, i) => unchecked(sum + i)); // TODO: Use better hashing over enumerable
        }

        public override bool Equals(object obj)
        {
            return this.GetHashCode() == obj.GetHashCode();
        }
    }

    public class ResolveGenericsSyntax :
        SyntaxTreeTransformation<ScopeTransformation<ResolveGenericsExpression>>
    {
        // DUMMY TEST FUNCTION - should be deleted
        private static void TestHash()
        {
            var temp = new Dictionary<Concretion, int>();
            Concretion conA = new Concretion();
            Concretion conB = new Concretion();
            
            conA.Add((
                NonNullable<string>.New("TypeVarA"),
                LocalTypeKind.Int
                ));
            
            conA.Add((
                NonNullable<string>.New("TypeVarB"),
                LocalTypeKind.NewTupleType(ImmutableArray.Create(ResolvedType.New(LocalTypeKind.Int), ResolvedType.New(LocalTypeKind.Double)))
                ));
            
            conB.Add((
                NonNullable<string>.New("TypeVarA"),
                LocalTypeKind.Int
                ));
            
            temp.Add(conA, 12);
            temp.Add(conB, 4);
            
            conB.Add((
                NonNullable<string>.New("TypeVarB"),
                LocalTypeKind.NewTupleType(ImmutableArray.Create(ResolvedType.New(LocalTypeKind.Int), ResolvedType.New(LocalTypeKind.Double)))
                ));
            
            var result = temp[conB];

            return;
        }

        private GenericsCrate Generics;

        public static IEnumerable<QsNamespace> Apply(IEnumerable<QsNamespace> namespaces)
        {
            //TestHash();

            if (namespaces == null || namespaces.Contains(null)) throw new ArgumentNullException(nameof(namespaces));

            // Get list of generics
            // TODO: this is specific to Provided implementations, but should be general to all implementation kinds
            GenericsCrate generics = namespaces.GlobalCallableResolutions()
                .Where(call => call.Value.Signature.TypeParameters.Any() && call.Value.Specializations.Any(x => x.Implementation.IsProvided))
                .ToDictionary(call => call.Key, call => (call.Value, new Dictionary<Concretion, QsCallable>()));

            var filter = new ResolveGenericsSyntax(new ScopeTransformation<ResolveGenericsExpression>(new ResolveGenericsExpression(generics)), generics);
            return namespaces.Select(ns => filter.Transform(ns));
        }

        public static IEnumerable<QsNamespace> Apply(params QsNamespace[] namespaces)
        {
            return Apply((IEnumerable<QsNamespace>)namespaces);
        }
        
        private ResolveGenericsSyntax(ScopeTransformation<ResolveGenericsExpression> scope, GenericsCrate generics) : base(scope) { this.Generics = generics; }

        public override QsNamespace Transform(QsNamespace ns)
        {
            base.Transform(ns);

            // Add concrete definitions for callables found in the generics data structure back into the syntax tree
            // Remove generic definitions
            return ns.WithElements(elems => elems.SelectMany(elem =>
            {
                if (elem is QsNamespaceElement.QsCallable callable && Generics.ContainsKey(callable.Item.FullName))
                {
                    return Generics[callable.Item.FullName].Item2.Values
                        .Where(call => call != null)
                        .Select(call => QsNamespaceElement.NewQsCallable(call));
                }
                else
                {
                    return ImmutableArray.Create(elem);
                }
            }).ToImmutableArray());
        }
    }

    public class ResolveGenericsExpression :
        ExpressionTransformation<ResolveGenericsExpressionKind>
    {
        private GenericsCrate Generics;

        internal ResolveGenericsExpression(GenericsCrate generics) : base(ex => new ResolveGenericsExpressionKind(ex as ResolveGenericsExpression)) { Generics = generics; }

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
                result.Add((kvp.Key.TypeName, kvp.Value.Resolution));
            }
            return result;
        }

        private Identifier GetConcreteIdentifier(Identifier.GlobalCallable globalCallable, ImmutableDictionary<QsTypeParameter, ResolvedType> types)
        {
            Concretion target = CreateConcretion(types);

            // TODO: handle if globalCallable is not in Generics

            var (original, concretions) = Generics[globalCallable.Item];

            NonNullable<string> name;

            if (concretions.ContainsKey(target))
            {
                name = concretions[target].FullName.Name;
            }
            else
            {
                // Create new name
                string nameString = Guid.NewGuid().ToString() + "_" + globalCallable.Item.Name.Value;

                // TODO: Check for identifier name collisions (not likely)
                // - check in global namespace
                // - check in concretion dict for current generic

                name = NonNullable<string>.New(nameString);

                var replaceParamsTransform = new MinorTransformations.ReplaceTypeParams(types);
                QsCallable newCallable = original
                    // TODO: this is currently specific to Provided, but should be generalized
                    .WithSpecializations(specs => specs.Select(specialization =>
                    {
                        if (specialization.Implementation is SpecializationImplementation.Provided prov)
                        {
                            return specialization.WithImplementation(SpecializationImplementation.NewProvided(prov.Item1, replaceParamsTransform.Transform(prov.Item2)));
                        }
                        return specialization;
                    }).ToImmutableArray())
                    .WithFullName(fullname => new QsQualifiedName(globalCallable.Item.Namespace, name));

                concretions.Add(target, newCallable);
            }

            return Identifier.NewGlobalCallable(new QsQualifiedName(globalCallable.Item.Namespace, name));
        }
    }

    public class ResolveGenericsExpressionKind : ExpressionKindTransformation<ResolveGenericsExpression>
    {
        public ResolveGenericsExpressionKind(ResolveGenericsExpression expr) : base(expr) { }

        public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
        {
            return base.onIdentifier(sym, tArgs);
        }
    }
}