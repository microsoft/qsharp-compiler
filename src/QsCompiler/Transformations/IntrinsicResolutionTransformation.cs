// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations.IntrinsicResolutionTransformation
{
    public class IntrinsicResolutionTransformation
    {
        /// <summary>
        /// Merge the environment-specific syntax tree with the target tree. The resulting tree will
        /// have the union of the namespaces found in both input trees. All namespaces in the intersection
        /// between the trees will be merged so that the environment's definitions of elements are preserved
        /// over the target's definitions.
        /// </summary>
        public static QsCompilation Apply(QsCompilation environment, QsCompilation target)
        {
            var envNames = environment.Namespaces.ToImmutableDictionary(ns => ns.Name);
            var targetNames = target.Namespaces.Select(ns => ns.Name).ToImmutableHashSet();

            return new QsCompilation(environment.Namespaces
                .Where(ns => !targetNames.Contains(ns.Name))
                .Concat(target.Namespaces.Select(ns =>
                    envNames.TryGetValue(ns.Name, out var envNs)
                    ? MergeNamespaces(envNs, ns)
                    : ns))
                .ToImmutableArray(), target.EntryPoints);
        }

        /// <summary>
        /// Returns a copy of the accepting namespace that includes all the elements from the overriding namespace.
        /// If there are elements found in both namespaces, the resulting ns takes the overriding ns's version of the elements
        /// The resulting namespace takes the overriding namespace's version of the elements found in both input namespaces.
        /// </summary>
        private static QsNamespace MergeNamespaces(QsNamespace overriding, QsNamespace accepting)
        {
            var overridingNames = overriding.Elements.Select(x => x.FullName).ToImmutableHashSet();

            return accepting.WithElements(_ => accepting.Elements
                .Where(elem => !overridingNames.Contains(elem.FullName))
                .Concat(overriding.Elements)
                .ToImmutableArray());
        }
    }
}
