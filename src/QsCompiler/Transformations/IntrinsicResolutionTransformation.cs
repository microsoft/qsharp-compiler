// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
            var overridingNames = overriding.Elements.ToImmutableDictionary(x => x.FullName);

            // Check that overriding elems and accepting elems with the same names are
            // the same kind of elems and have the same signatures if they are callables
            foreach (var elem in accepting.Elements)
            {
                if (overridingNames.TryGetValue(elem.FullName, out var overrideElem))
                {
                    if (elem is QsNamespaceElement.QsCallable call)
                    {
                        if (overrideElem is QsNamespaceElement.QsCallable overrideCall)
                        {
                            if (!CompareSigs(call.Item.Signature, overrideCall.Item.Signature))
                            {
                                throw new Exception($"Callable {overrideCall.FullName.FullName} in environment compilation does not have the same signature as callable {call.FullName.FullName} in target compilation");
                            }
                        }
                        else
                        {
                            throw new Exception($"Custom type {overrideElem.FullName.FullName} in environment compilation has the same name as callable {call.FullName.FullName} in target compilation");
                        }
                    }

                    if (elem is QsNamespaceElement.QsCustomType && !(overrideElem is QsNamespaceElement.QsCustomType))
                    {
                        throw new Exception($"Callable {overrideElem.FullName.FullName} in environment compilation has the same name as custom type {elem.FullName.FullName} in target compilation");
                    }
                }
            }

            return accepting.WithElements(_ => accepting.Elements
                .Where(elem => !overridingNames.ContainsKey(elem.FullName))
                .Concat(overriding.Elements)
                .ToImmutableArray());
        }

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
