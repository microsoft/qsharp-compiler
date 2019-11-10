// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.SyntaxTree;
using System.Collections.Immutable;
using System.Linq;


namespace Microsoft.Quantum.QsCompiler.Transformations.IntrinsicMapping
{
    public class IntrinsicMapping
    {
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
