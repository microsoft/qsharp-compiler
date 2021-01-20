// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.Transformations.IntrinsicResolution
{
    public static class ReplaceWithTargetIntrinsics
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

            return new QsCompilation(
                environment.Namespaces
                    .Where(ns => !targetNames.Contains(ns.Name))
                    .Concat(target.Namespaces.Select(ns =>
                        envNames.TryGetValue(ns.Name, out var envNs)
                        ? MergeNamespaces(envNs, ns)
                        : ns))
                    .ToImmutableArray(),
                target.EntryPoints);
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
                MergeElements(overriding.Elements.Where(x => x.IsQsCallable), accepting.Elements.Where(x => x.IsQsCallable))
                .Concat(MergeElements(overriding.Elements.Where(x => x.IsQsCustomType), accepting.Elements.Where(x => x.IsQsCustomType)))
                .ToImmutableArray());
        }

        private static IEnumerable<QsNamespaceElement> MergeElements(IEnumerable<QsNamespaceElement> overriding, IEnumerable<QsNamespaceElement> accepting)
        {
            var overridingNames = overriding.ToImmutableDictionary(x => x.GetFullName());

            // Check that overriding elems and accepting elems with the same names have the same signatures/type definitions
            foreach (var elem in accepting)
            {
                if (overridingNames.TryGetValue(elem.GetFullName(), out var overrideElem))
                {
                    if (elem is QsNamespaceElement.QsCallable elemCall &&
                    overrideElem is QsNamespaceElement.QsCallable overrideCall &&
                    !CompareSignatures(elemCall.Item.Signature, overrideCall.Item.Signature))
                    {
                        throw new Exception($"Callable {overrideCall.GetFullName()} in environment compilation does not have the same signature as callable {elemCall.GetFullName()} in target compilation");
                    }
                    else if (elem is QsNamespaceElement.QsCustomType elemTyp &&
                        overrideElem is QsNamespaceElement.QsCustomType overrideTyp &&
                        !CompareUserDefinedTypes(elemTyp, overrideTyp))
                    {
                        throw new Exception($"Custom type {overrideElem.GetFullName()} in environment compilation does not match custom type {elem.GetFullName()} in target compilation");
                    }
                }
            }

            return accepting
                .Where(elem => !overridingNames.ContainsKey(elem.GetFullName()))
                .Concat(overriding);
        }

        private static bool CompareSignatures(ResolvedSignature first, ResolvedSignature second)
        {
            return
                StripPositionInfo.Apply(first.ArgumentType).Equals(StripPositionInfo.Apply(second.ArgumentType)) &&
                StripPositionInfo.Apply(first.ReturnType).Equals(StripPositionInfo.Apply(second.ReturnType)) &&
                first.TypeParameters.Select((val, i) => val.Equals(second.TypeParameters[i])).All(x => x) && // ToDo: this should be a more robust check
                first.Information.Characteristics.GetProperties().SetEquals(second.Information.Characteristics.GetProperties());
        }

        private static bool CompareUserDefinedTypes(QsNamespaceElement.QsCustomType first, QsNamespaceElement.QsCustomType second)
        {
            var tempNs = StripPositionInfo.Apply(
                new QsNamespace(
                    "tempNs",
                    ImmutableArray.Create<QsNamespaceElement>(first, second),
                    Array.Empty<string>().ToLookup(ns => ns, _ => ImmutableArray<string>.Empty)));
            var firstUDT = ((QsNamespaceElement.QsCustomType)tempNs.Elements[0]).Item;
            var secondUDT = ((QsNamespaceElement.QsCustomType)tempNs.Elements[1]).Item;
            return firstUDT.TypeItems.Equals(secondUDT.TypeItems);
        }
    }
}
