// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>;

    internal class QirNamespaceTransformation : NamespaceTransformation<GenerationContext>
    {
        private class TransformationContext
        {
            private QsCallable? currentCallable = null;
            private QsSpecialization? currentSpecialization = null;

            internal QsCallable GetCurrentCallable() =>
                this.currentCallable ?? throw new InvalidOperationException("current callable not specified");

            internal void SetCurrentCallable(QsCallable? value) =>
                this.currentCallable = value;

            internal QsSpecialization GetCurrentSpecialization() =>
                this.currentSpecialization ?? throw new InvalidOperationException("current specialization not specified");

            internal void SetCurrentSpecialization(QsSpecialization? value) =>
                this.currentSpecialization = value;
        }

        private readonly TransformationContext context = new TransformationContext();

        public QirNamespaceTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options)
            : base(parentTransformation, options)
        {
        }

        public QirNamespaceTransformation(GenerationContext sharedState, TransformationOptions options)
            : base(sharedState, options)
        {
        }

        public QirNamespaceTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation)
            : base(parentTransformation)
        {
        }

        public QirNamespaceTransformation(GenerationContext sharedState)
            : base(sharedState)
        {
        }

        /* public overrides */

        public override void OnIntrinsicImplementation()
        {
            var currentCallable = this.context.GetCurrentCallable();
            var currentSpec = this.context.GetCurrentSpecialization();
            if (currentCallable.Kind.IsTypeConstructor && currentSpec.Kind.IsQsBody)
            {
                this.SharedState.StartFunction();
                this.SharedState.GenerateConstructor(currentSpec, currentCallable.ArgumentTuple);
                this.SharedState.EndFunction(generatePending: true);
            }
        }

        public override Tuple<QsArgumentTuple, QsScope> OnProvidedImplementation(QsArgumentTuple argTuple, QsScope body)
        {
            this.SharedState.StartFunction();

            // If this compilation is for a Library project, we should treat every public callable from the target compilation unit
            // that isn't auto-generated (such as by monomorphization) as extern.
            var shouldBeExtern = this.SharedState.IsLibrary &&
                !NameGenerator.IsGeneratedName(this.context.GetCurrentCallable().FullName) &&
                this.context.GetCurrentCallable().Source.AssemblyFile.IsNull &&
                this.context.GetCurrentCallable().Access.IsPublic;
            this.SharedState.GenerateFunctionHeader(this.context.GetCurrentSpecialization(), argTuple, deconstuctArgument: true, shouldBeExtern);
            this.Transformation.Statements.OnScope(body);
            this.SharedState.EndFunction(generatePending: true);
            return Tuple.Create(argTuple, body);
        }

        public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec)
        {
            this.context.SetCurrentSpecialization(spec);
            spec = base.OnSpecializationDeclaration(spec);
            this.context.SetCurrentSpecialization(null);
            return spec;
        }

        public override QsCallable OnCallableDeclaration(QsCallable c)
        {
            if (this.SharedState.Functions.IsBuiltIn(c.FullName))
            {
                return c;
            }

            this.context.SetCurrentCallable(c);
            c = base.OnCallableDeclaration(c);
            this.context.SetCurrentCallable(null);
            return c;
        }

        public override QsCustomType OnTypeDeclaration(QsCustomType t) => t;
    }
}
