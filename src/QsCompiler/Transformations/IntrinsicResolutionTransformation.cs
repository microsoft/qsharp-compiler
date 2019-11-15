// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
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
        /// Custom type elements are treated separately from callable elements.
        /// </summary>
        private static QsNamespace MergeNamespaces(QsNamespace overriding, QsNamespace accepting)
        {
            return accepting.WithElements(_ =>
                MergeElements(overriding.Elements.Where(x => x.IsQsCallable), accepting.Elements.Where(x => x.IsQsCallable), true)
                .Concat(MergeElements(overriding.Elements.Where(x => x.IsQsCustomType), accepting.Elements.Where(x => x.IsQsCustomType), false))
                .ToImmutableArray());
        }

        private static IEnumerable<QsNamespaceElement> MergeElements(IEnumerable<QsNamespaceElement> overriding, IEnumerable<QsNamespaceElement> accepting, bool checkSignature = false)
        {
            var overridingNames = overriding.ToImmutableDictionary(x => x.FullName);

            if (checkSignature) // Check that overriding elems and accepting elems with the same names have the same signatures if they are callables
            {
                foreach (var elem in accepting)
                {
                    if (overridingNames.TryGetValue(elem.FullName, out var overrideElem))
                    {
                        if (elem is QsNamespaceElement.QsCallable elemCall && overrideElem is QsNamespaceElement.QsCallable overrideCall)
                        {
                            if (!CompareSigs(elemCall.Item.Signature, overrideCall.Item.Signature))
                            {
                                throw new Exception($"Callable {overrideCall.FullName.FullName} in environment compilation does not have the same signature as callable {elemCall.FullName.FullName} in target compilation");
                            }
                        }

                    }
                }
            }

            return accepting
                .Where(elem => !overridingNames.ContainsKey(elem.FullName))
                .Concat(overriding);
        }

        // ToDo: There should be a standardized way of checking if two signatures are the same
        private static bool CompareSigs(ResolvedSignature first, ResolvedSignature second)
        {
            return
                first.ArgumentType.Resolution == second.ArgumentType.Resolution &&
                first.ReturnType.Resolution == second.ReturnType.Resolution &&
                first.TypeParameters == second.TypeParameters;
                // ToDo: check that they implement the same specializations
        }
    }
}
