using System;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    using QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;

    internal class QirNamespaceTransformation : NamespaceTransformation<GenerationContext>
    {
        internal class TransformationContext
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

        public QirNamespaceTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options) : base(parentTransformation, options)
        {
        }

        public QirNamespaceTransformation(GenerationContext sharedState, TransformationOptions options) : base(sharedState, options)
        {
        }

        public QirNamespaceTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation) : base(parentTransformation)
        {
        }

        public QirNamespaceTransformation(GenerationContext sharedState) : base(sharedState)
        {
        }

        public override QsCallable OnCallableDeclaration(QsCallable c)
        {
            if (c.Kind == QsCallableKind.TypeConstructor)
            {
                return c;
            }

            this.context.SetCurrentCallable(c);
            return base.OnCallableDeclaration(c);
        }

        public override void OnExternalImplementation()
        {
            this.SharedState.RegisterFunction(this.context.GetCurrentSpecialization(), this.context.GetCurrentCallable().ArgumentTuple);
        }

        public override void OnIntrinsicImplementation()
        {
            this.SharedState.RegisterFunction(this.context.GetCurrentSpecialization(), this.context.GetCurrentCallable().ArgumentTuple);
        }

        public override Tuple<QsArgumentTuple, QsScope> OnProvidedImplementation(QsArgumentTuple argTuple, QsScope body)
        {
            this.SharedState.StartSpecialization();
            this.SharedState.GenerateFunctionHeader(this.context.GetCurrentSpecialization(), argTuple);
            this.Transformation.Statements.OnScope(body);
            this.SharedState.EndSpecialization();
            this.context.SetCurrentSpecialization(null);
            return new Tuple<QsArgumentTuple, QsScope>(argTuple, body);
        }

        public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec)
        {
            this.context.SetCurrentSpecialization(spec);
            return base.OnSpecializationDeclaration(spec);
        }

        public override QsCustomType OnTypeDeclaration(QsCustomType t)
        {
            // Generate the type constructor
            this.SharedState.GenerateConstructor(t);

            return base.OnTypeDeclaration(t);
        }
    }
}
