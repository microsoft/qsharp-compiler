// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.SyntaxTreeTrimming
{
    /// <summary>
    /// Removes unused callables from the syntax tree.
    /// </summary>
    public static class TrimSyntaxTree
    {
        /// <summary>
        /// Applies the transformation that removes from the syntax tree all callables that
        /// are unused, meaning they are not a descendant of at least one entry point in
        /// the call graph. If keepAllIntrinsics is true, callables with an intrinsic body
        /// will not be trimmed, regardless of usage. Any callables that later
        /// transformations will depend on should be passed in and will not be trimmed,
        /// regardless of usage. Note that unused type constructors will be subject to
        /// trimming as any other callable.
        /// </summary>
        public static QsCompilation Apply(QsCompilation compilation, bool keepAllIntrinsics, IEnumerable<QsQualifiedName>? dependencies = null, bool isLibrary = false)
        {
            return TrimTree.Apply(compilation, keepAllIntrinsics, dependencies, isLibrary);
        }

        private class TrimTree : SyntaxTreeTransformation<TrimTree.TransformationState>
        {
            public static QsCompilation Apply(QsCompilation compilation, bool keepAllIntrinsics, IEnumerable<QsQualifiedName>? dependencies, bool isLibrary)
            {
                var globals = compilation.Namespaces.GlobalCallableResolutions();
                var dependenciesToKeep = dependencies?.Where(globals.ContainsKey) ?? ImmutableArray<QsQualifiedName>.Empty;
                var augmentedEntryPoints = dependenciesToKeep
                    .Concat(compilation.EntryPoints)
                    .Distinct();

                // If this compilation is for a Library project, treat each public, non-generic callable as an entry point
                // for the purpose of constructing the call graph and pruning the syntax tree.
                if (isLibrary)
                {
                    var externals = globals.Where(g => g.Value.Source.AssemblyFile.IsNull && g.Value.Signature.TypeParameters.IsEmpty && g.Value.Access.IsPublic);
                    augmentedEntryPoints = augmentedEntryPoints.Concat(externals.Select(e => e.Key)).Distinct();
                }

                var compilationWithBuiltIns = new QsCompilation(compilation.Namespaces, augmentedEntryPoints.ToImmutableArray());
                var callablesToKeep = new CallGraph(compilationWithBuiltIns, true).Nodes.Select(node => node.CallableName).ToImmutableHashSet();

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
                    return graphNodes.Contains(call.Item.FullName);
                }
                else
                {
                    return true;
                }
            }

            /// <summary>
            /// Class representing the state of the transformation.
            /// </summary>
            public class TransformationState
            {
                public Func<QsNamespaceElement, bool> NamespaceElementFilter { get; }

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
