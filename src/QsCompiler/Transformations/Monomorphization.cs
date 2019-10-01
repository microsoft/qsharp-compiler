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
            var filter = new ResolveGenericsSyntax(new ResolveGenericsScope());

            // Get list of generics
            var Callables = namespaces.GlobalCallableResolutions();
            foreach (var callable in Callables)
            {
                if (callable.Value.Signature.TypeParameters.Any())
                {
                    filter.Generics.Add(callable.Value);
                }
            }

            return namespaces.Select(ns => filter.Transform(ns));
        }

        public static IEnumerable<QsNamespace> Apply(params QsNamespace[] namespaces)
        {
            return Apply(namespaces);
        }

        List<QsCallable> Generics;

        public ResolveGenericsSyntax(ResolveGenericsScope scope) : base(scope)
        {
            this.Generics = new List<QsCallable>();
        }

        //public override QsNamespace Transform(QsNamespace ns)
        //{
        //    // Get global callables
        //    
        //    // this.Callables = GetCallables(ns);
        //    var newNs = base.Transform(ns);
        //    return newNs;
        //}
    }

    public class ResolveGenericsScope :
            ScopeTransformation<ResolveGenericsExpression>
    {
        public ResolveGenericsScope() : base(new ResolveGenericsExpression()) { }

        //public override QsStatementKind onExpressionStatement(TypedExpression te)
        //{
        //    return QsStatementKind.NewQsExpressionStatement(te);
        //}
    }

    public class ResolveGenericsExpression :
        ExpressionTransformation<ResolvedGenericsExpressionKind>
    {
        public ResolveGenericsExpression() : base(ex => new ResolvedGenericsExpressionKind(ex as ResolveGenericsExpression))
        {
            
        }

        //public override TypedExpression Transform(TypedExpression ex)
        //{
        //    if (ex.Expression is QsExpressionKind<TypedExpression, Identifier, ResolvedType>.Identifier temp)
        //    {
        //        temp.Item1.IsGlobalCallable
        //        var kind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>.NewIdentifier(
        //            Identifier.NewGlobalCallable(
        //                new QsQualifiedName(NonNullable<string>.New(""), NonNullable<string>.New(""))),
        //            new QsNullable<ImmutableArray<ResolvedType>>());
        //        ex = new TypedExpression(kind, ex.TypeParameterResolutions, ex.ResolvedType, ex.InferredInformation, ex.Range);
        //    }
        //    return ex;
        //}
    }

    public class ResolvedGenericsExpressionKind :
        ExpressionKindTransformation<ResolveGenericsExpression>
    {
        public ResolvedGenericsExpressionKind(ResolveGenericsExpression expr) : base(expr)
        {
            
        }

        public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
        {
            if (sym is Identifier.GlobalCallable temp)
            {
                sym = Identifier.NewGlobalCallable(new QsQualifiedName(temp.Item.Namespace, NonNullable<string>.New("REWRITE_" + temp.Item.Name.Value)));
            }
            return base.onIdentifier(sym, tArgs);
        }
    }
}