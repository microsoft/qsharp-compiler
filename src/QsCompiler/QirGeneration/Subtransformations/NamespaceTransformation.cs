// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QIR;
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

        private readonly IImmutableSet<QsQualifiedName> publicApi;
        private readonly TransformationContext context = new TransformationContext();

        public QirNamespaceTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options, IImmutableSet<QsQualifiedName>? publicApi = null)
            : base(parentTransformation, options) =>
            this.publicApi = publicApi ?? ImmutableHashSet<QsQualifiedName>.Empty;

        public QirNamespaceTransformation(GenerationContext sharedState, TransformationOptions options, IImmutableSet<QsQualifiedName>? publicApi = null)
            : base(sharedState, options) =>
            this.publicApi = publicApi ?? ImmutableHashSet<QsQualifiedName>.Empty;

        public QirNamespaceTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, IImmutableSet<QsQualifiedName>? publicApi = null)
            : base(parentTransformation) =>
            this.publicApi = publicApi ?? ImmutableHashSet<QsQualifiedName>.Empty;

        public QirNamespaceTransformation(GenerationContext sharedState, IImmutableSet<QsQualifiedName>? publicApi = null)
            : base(sharedState) =>
            this.publicApi = publicApi ?? ImmutableHashSet<QsQualifiedName>.Empty;

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

            var shouldBeExtern = this.publicApi.Contains(this.context.GetCurrentCallable().FullName);
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
            if (this.SharedState.Functions.IsBuiltIn(c.FullName) ||
                (this.SharedState.TargetQirProfile && !this.publicApi.Contains(c.FullName)))
            {
                return c;
            }

            this.context.SetCurrentCallable(c);
            c = base.OnCallableDeclaration(c);
            this.context.SetCurrentCallable(null);

            // TODO: The check for whether we are targeting a QIR profile can be removed
            // once we get rid of entry point and interop wrappers.
            if (this.SharedState.TargetQirProfile && c.Attributes.Any(BuiltIn.MarksEntryPoint))
            {
                this.SharedState.AttachAttributes(c.FullName, QsSpecializationKind.QsBody, AttributeNames.EntryPoint);
            }

            return c;
        }

        public override QsCustomType OnTypeDeclaration(QsCustomType t) => t;
    }
}
