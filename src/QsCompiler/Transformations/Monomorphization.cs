// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
{
    public class ResolveGenerics :
            SyntaxTreeTransformation<NoScopeTransformations>
    {
        public static QsNamespace Apply(QsNamespace ns)
        {
            if (ns == null) throw new ArgumentNullException(nameof(ns));
            var filter = new ResolveGenerics();
            return filter.Transform(ns);
        }

        //private readonly Dictionary<thing1, thing2> Callables;
        private ResolveGenerics() : base(new NoScopeTransformations()) { }

        public override QsSpecialization onSpecializationImplementation(QsSpecialization spec) // short cut to avoid further evaluation
        {
            this.onSourceFile(spec.SourceFile);
            return spec;
        }

        public override QsNamespace Transform(QsNamespace ns)
        {
            // Get global callables
            // this.Callables = GetCallables(ns);
            var newNs = base.Transform(ns);
            return newNs;
        }
    }

}