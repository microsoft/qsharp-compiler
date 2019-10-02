// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
{
    public class ResolveGenericsSyntax :
        SyntaxTreeTransformation<ResolveGenericsScope>
    {
        public static IEnumerable<QsNamespace> Apply(IEnumerable<QsNamespace> namespaces)
        {
            if (namespaces == null || namespaces.Contains(null)) throw new ArgumentNullException(nameof(namespaces));

            // Get list of generics
            List<QsQualifiedName> generics = new List<QsQualifiedName>();
            var Callables = namespaces.GlobalCallableResolutions();
            foreach (var callable in Callables)
            {
                if (callable.Value.Signature.TypeParameters.Any())
                {
                    generics.Add(callable.Key);
                }
            }

            var filter = new ResolveGenericsSyntax(new ResolveGenericsScope());
            return namespaces.Select(ns => filter.Transform(ns));
        }

        public static IEnumerable<QsNamespace> Apply(params QsNamespace[] namespaces)
        {
            return Apply(namespaces);
        }

        public ResolveGenericsSyntax(ResolveGenericsScope scope) : base(scope) { }
    }

    public class ResolveGenericsScope :
            ScopeTransformation<ResolveGenericsExpression>
    {
        public ResolveGenericsScope() : base(new ResolveGenericsExpression()) { }
    }

    public class ResolveGenericsExpression :
        ExpressionTransformation<ResolvedGenericsExpressionKind>
    {
        public ResolveGenericsExpression() : base(ex => new ResolvedGenericsExpressionKind(ex as ResolveGenericsExpression)) { }

        public override TypedExpression Transform(TypedExpression ex)
        {
            ImmutableDictionary<QsTypeParameter, ResolvedType> types = ex.TypeParameterResolutions;

            if (types.Any() && ex.Expression is QsExpressionKind<TypedExpression, Identifier, ResolvedType>.CallLikeExpression call)
            {
                string prefix = CreateTypePrefix(types);

                // For now, only resolve identifiers of global callables
                if (call.Item1.Expression is QsExpressionKind<TypedExpression, Identifier, ResolvedType>.Identifier id &&
                    id.Item1 is Identifier.GlobalCallable globalCallable)
                {
                    // Rebuild the method with the updated identifier
                    ex = new TypedExpression(
                        QsExpressionKind<TypedExpression, Identifier, ResolvedType>.NewCallLikeExpression(
                            new TypedExpression(
                                QsExpressionKind<TypedExpression, Identifier, ResolvedType>.NewIdentifier(
                                    Identifier.NewGlobalCallable(new QsQualifiedName(globalCallable.Item.Namespace, NonNullable<string>.New(prefix + globalCallable.Item.Name.Value))),
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

        private string CreateTypePrefix(ImmutableDictionary<QsTypeParameter, ResolvedType> typeDict)
        {
            return "_" + String.Join("", typeDict
                .ToList()
                .OrderBy(kvp => kvp.Key.TypeName)
                .Select(kvp => kvp.Key.TypeName.Value + "_" + kvp.Value.Resolution.ToString() + "_")
                );
        }
    }

    public class ResolvedGenericsExpressionKind :
        ExpressionKindTransformation<ResolveGenericsExpression>
    {
        public ResolvedGenericsExpressionKind(ResolveGenericsExpression expr) : base(expr) { }
    }
}