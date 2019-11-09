using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Sources;


namespace Microsoft.Quantum.QsCompiler.Transformations.IntrinsicMapping
{
    public class IntrinsicMapping
    {
        public static QsCompilation Apply(QsCompilation environment, QsCompilation target)
        {
            var envNames = environment.Namespaces.ToImmutableDictionary(ns => ns.Name);
            var targetNames = target.Namespaces.Select(ns => ns.Name);

            var outer = environment.Namespaces
                .Where(ns => !targetNames.Contains(ns.Name))
                .Concat(target.Namespaces.Where(ns => !envNames.ContainsKey(ns.Name)));
            var inner = target.Namespaces.Where(ns => envNames.ContainsKey(ns.Name)).Select(ns => MergeNamespaces(envNames[ns.Name], ns));

            return new QsCompilation(outer.Union(inner).ToImmutableArray(), target.EntryPoints);
        }

        private static QsNamespace MergeNamespaces(QsNamespace overriding, QsNamespace accepting)
        {
            var overridingNames = HoistElemNames(overriding.Elements).Select(x => x.Name);
            var acceptingElems = HoistElemNames(accepting.Elements);

            return accepting.WithElements(_ => overriding.Elements
                .Concat(acceptingElems
                    .Where(elem => !overridingNames.Contains(elem.Name))
                    .Select(elem => elem.Element))
                .ToImmutableArray());
        }

        private static IEnumerable<(QsQualifiedName Name, QsNamespaceElement Element)> HoistElemNames(IEnumerable<QsNamespaceElement> elements)
        {
            return elements
                .Where(elem => elem is QsNamespaceElement.QsCallable)
                .Select(elem => (Name : ((QsNamespaceElement.QsCallable)elem).Item.FullName, Element : elem))
                .Concat(elements
                    .Where(elem => elem is QsNamespaceElement.QsCustomType)
                    .Select(elem => (Name : ((QsNamespaceElement.QsCustomType)elem).Item.FullName, Element : elem)));
        }
    }
}
