// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.SyntaxTreeTrimming
{
    public static class TrimSyntaxTree
    {
        public static QsCompilation Apply(QsCompilation compilation, bool keepAllIntrinsics)
        {
            return TrimTree.Apply(compilation, keepAllIntrinsics);
        }

        private class TrimTree : SyntaxTreeTransformation<TrimTree.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation, bool keepAllIntrinsics)
            {
                var callablesToKeep = new CallGraph(compilation, true).Nodes.Select(node => node.CallableName).ToImmutableHashSet();

                // ToDo: convert to using ternary operator, when target-type
                // conditional expressions are supported in C#
                Func<QsNamespaceElement, bool> filter = elem => Filter(elem, callablesToKeep);
                if (keepAllIntrinsics)
                {
                    filter = elem => FilterWithIntrinsics(elem, callablesToKeep);
                }

                var transformed = new TrimTree(filter).OnCompilation(compilation);
                return new QsCompilation(transformed.Namespaces.Where(ns => ns.Elements.Any()).ToImmutableArray(), transformed.EntryPoints);
            }

            private static bool FilterWithIntrinsics(QsNamespaceElement elem, ImmutableHashSet<QsQualifiedName> graphNodes)
            {
                if (elem is QsNamespaceElement.QsCallable call)
                {
                    return call.Item.Specializations.Any(spec => spec.Implementation.IsIntrinsic)
                        || BuiltIn.RewriteStepDependencies.Contains(call.Item.FullName)
                        || graphNodes.Contains(call.Item.FullName);
                }
                else
                {
                    return true;
                }
            }

            private static bool Filter(QsNamespaceElement elem, ImmutableHashSet<QsQualifiedName> graphNodes)
            {
                if (elem is QsNamespaceElement.QsCallable call)
                {
                    return BuiltIn.RewriteStepDependencies.Contains(call.Item.FullName)
                        || graphNodes.Contains(call.Item.FullName);
                }
                else
                {
                    return true;
                }
            }

            public class TransformationState
            {
                public readonly Func<QsNamespaceElement, bool> NamespaceElementFilter;

                public TransformationState(Func<QsNamespaceElement, bool> namespaceElementFilter)
                {
                    this.NamespaceElementFilter = namespaceElementFilter;
                }
            }

            private TrimTree(Func<QsNamespaceElement, bool> namespaceElementFilter)
                : base(new TransformationState(namespaceElementFilter))
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                public override QsNamespace OnNamespace(QsNamespace ns)
                {
                    return ns.WithElements(elements => elements
                        .Where(this.SharedState.NamespaceElementFilter)
                        .ToImmutableArray());
                }
            }
        }
    }
}
