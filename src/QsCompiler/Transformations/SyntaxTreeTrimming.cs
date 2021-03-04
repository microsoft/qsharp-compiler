// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Microsoft.Quantum.QsCompiler.Transformations.SyntaxTreeTrimming
{
    public static class SyntaxTreeTrimming
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            return compilation;
        }

        private class ResolveGenerics : SyntaxTreeTransformation<ResolveGenerics.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation, List<QsCallable> callables, bool keepAllIntrinsics)
            {
                var intrinsicCallableSet = compilation.Namespaces.GlobalCallableResolutions()
                    .Where(kvp => kvp.Value.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                    .Select(kvp => kvp.Key)
                    .ToImmutableHashSet();

                var filter = new ResolveGenerics(
                    callables
                        .Where(call => !keepAllIntrinsics || !intrinsicCallableSet.Contains(call.FullName))
                        .ToLookup(res => res.FullName.Namespace),
                    intrinsicCallableSet,
                    keepAllIntrinsics);

                var transformed = filter.OnCompilation(compilation);
                return new QsCompilation(transformed.Namespaces.Where(ns => ns.Elements.Any()).ToImmutableArray(), transformed.EntryPoints);
            }

            public class TransformationState
            {
                public readonly ILookup<string, QsCallable> NamespaceCallables;
                public readonly ImmutableHashSet<QsQualifiedName> IntrinsicCallableSet;
                public readonly bool KeepAllIntrinsics;

                public TransformationState(ILookup<string, QsCallable> namespaceCallables, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet, bool keepAllIntrinsics)
                {
                    this.NamespaceCallables = namespaceCallables;
                    this.IntrinsicCallableSet = intrinsicCallableSet;
                    this.KeepAllIntrinsics = keepAllIntrinsics;
                }
            }

            /// <summary>
            /// Constructor for the ResolveGenericsSyntax class. Its transform function replaces global callables in the namespace.
            /// </summary>
            /// <param name="namespaceCallables">Maps namespace names to an enumerable of all global callables in that namespace.</param>
            private ResolveGenerics(ILookup<string, QsCallable> namespaceCallables, ImmutableHashSet<QsQualifiedName> intrinsicCallableSet, bool keepAllIntrinsics)
                : base(new TransformationState(namespaceCallables, intrinsicCallableSet, keepAllIntrinsics))
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

                private bool NamespaceElementFilter(QsNamespaceElement elem)
                {
                    if (elem is QsNamespaceElement.QsCallable call)
                    {
                        return BuiltIn.RewriteStepDependencies.Contains(call.Item.FullName) ||
                            (this.SharedState.KeepAllIntrinsics && this.SharedState.IntrinsicCallableSet.Contains(call.Item.FullName));
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
                        .Where(this.NamespaceElementFilter)
                        .Concat(this.SharedState.NamespaceCallables[ns.Name].Select(QsNamespaceElement.NewQsCallable))
                        .ToImmutableArray());
                }
            }
        }

        private class TrimTree : SyntaxTreeTransformation<double>
        {
            public static QsCompilation Apply(QsCompilation compilation, List<QsCallable> callables, bool keepAllIntrinsics)
            {
                var intrinsicCallableSet = compilation.Namespaces.GlobalCallableResolutions()
                    .Where(kvp => kvp.Value.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                    .Select(kvp => kvp.Key)
                    .ToImmutableHashSet();

                var filter = new ResolveGenerics(
                    callables
                        .Where(call => !keepAllIntrinsics || !intrinsicCallableSet.Contains(call.FullName))
                        .ToLookup(res => res.FullName.Namespace),
                    intrinsicCallableSet,
                    keepAllIntrinsics);

                var transformed = filter.OnCompilation(compilation);
                return new QsCompilation(transformed.Namespaces.Where(ns => ns.Elements.Any()).ToImmutableArray(), transformed.EntryPoints);
            }

            public static QsCompilation Apply(QsCompilation compilation, bool keepAllIntrinsics)
            {
                var nodes = new CallGraph(compilation, true).Nodes;

                var intrinsicCallableSet = compilation.Namespaces.GlobalCallableResolutions()
                    .Where(kvp => kvp.Value.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                    .Select(kvp => kvp.Key)
                    .ToImmutableHashSet();

                var nameSpaces = compilation.Namespaces
                    .Select(ns => ns.WithElements(elems => elems
                        .Where(elem => this.NamespaceElementFilter(elem))));

                return new TrimTree(graph).OnCompilation(compilation);
            }

            private bool NamespaceElementFilter(QsNamespaceElement elem)
            {
                if (elem is QsNamespaceElement.QsCallable call)
                {
                    return BuiltIn.RewriteStepDependencies.Contains(call.Item.FullName) ||
                        (this.SharedState.KeepAllIntrinsics && this.SharedState.IntrinsicCallableSet.Contains(call.Item.FullName));
                }
                else
                {
                    return true;
                }
            }

            public class TransformationState
            {

            }

            private CallGraph graph;

            private TrimTree(CallGraph graph)
                : base(0.0)
            {
                this.graph = graph;

                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation<double>(this, TransformationOptions.Disabled);
                this.Expressions = new ExpressionTransformation<double>(this, TransformationOptions.Disabled);
                this.Types = new TypeTransformation<double>(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : NamespaceTransformation<double>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<double> parent)
                    : base(parent)
                {
                }
            }
        }
    }
}
