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

            // Function for determining if callable is a generic
            Func<QsQualifiedName, bool> isGeneric = callable =>
                generics.Any(generic => generic.Name.Value == callable.Name.Value && generic.Namespace.Value == callable.Namespace.Value);

            var filter = new ResolveGenericsSyntax(new ResolveGenericsScope(isGeneric));
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
        public ResolveGenericsScope(Func<QsQualifiedName, bool> isGeneric) : base(new ResolveGenericsExpression(isGeneric)) { }

        //public override QsStatementKind onExpressionStatement(TypedExpression te)
        //{
        //    return QsStatementKind.NewQsExpressionStatement(te);
        //}
    }

    public class ResolveGenericsExpression :
        ExpressionTransformation<ResolvedGenericsExpressionKind>
    {
        public ResolveGenericsExpression(Func<QsQualifiedName, bool> isGeneric) :
            base(ex => new ResolvedGenericsExpressionKind(ex as ResolveGenericsExpression, isGeneric)) { }
    }

    public class ResolvedGenericsExpressionKind :
        ExpressionKindTransformation<ResolveGenericsExpression>
    {
        public ResolvedGenericsExpressionKind(ResolveGenericsExpression expr, Func<QsQualifiedName, bool> isGeneric) : base(expr)
        {
            this.isGeneric = isGeneric;
        }

        private Func<QsQualifiedName, bool> isGeneric;

        public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
        {
            if (sym is Identifier.GlobalCallable temp && this.isGeneric(temp.Item))
            {
                sym = Identifier.NewGlobalCallable(new QsQualifiedName(temp.Item.Namespace, NonNullable<string>.New("REWRITE_" + temp.Item.Name.Value)));
            }
            return base.onIdentifier(sym, tArgs);
        }
    }
}