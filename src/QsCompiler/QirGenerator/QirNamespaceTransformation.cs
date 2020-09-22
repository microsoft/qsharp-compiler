using Llvm.NET.Types;
using Llvm.NET.Values;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    using QsArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;

    internal class QirNamespaceTransformation : NamespaceTransformation<GenerationContext>
    {
        private QsCallable currentCallable = null;
        private QsSpecialization currentSpecialization = null;

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

        //public override QsArgumentTuple OnArgumentTuple(QsArgumentTuple arg)
        //{
        //}

        //public override QsDeclarationAttribute OnAttribute(QsDeclarationAttribute att)
        //{
        //}

        public override QsCallable OnCallableDeclaration(QsCallable c)
        {
            if (c.Kind == QsCallableKind.TypeConstructor)
            {
                // Type constructors are created in OnTypeDeclaration
                return c;
            }

            this.currentCallable = c;

            QsCallable res = base.OnCallableDeclaration(c);

            return res;
        }

        //public override void OnDistributeDirective()
        //{
        //}

        //public override ImmutableArray<string> OnDocumentation(ImmutableArray<string> doc)
        //{
        //}

        public override void OnExternalImplementation()
        {
            this.SharedState.RegisterFunction(this.currentSpecialization, this.currentCallable.ArgumentTuple);
        }

        //public override QsCallable OnFunction(QsCallable c)
        //{
        //}

        //public override QsGeneratorDirective OnGeneratedImplementation(QsGeneratorDirective directive)
        //{
        //    // TODO: This should fail, the specializations should have been generated before we got here.
        //    return base.OnGeneratedImplementation(directive);
        //}

        public override void OnIntrinsicImplementation()
        {
            this.SharedState.RegisterFunction(this.currentSpecialization, this.currentCallable.ArgumentTuple);
        }

        //public override void OnInvalidGeneratorDirective()
        //{
        //}

        //public override void OnInvertDirective()
        //{
        //}

        //public override QsNullable<QsLocation> OnLocation(QsNullable<QsLocation> l)
        //{
        //}

        //public override QsNamespace OnNamespace(QsNamespace ns)
        //{
        //}

        //public override QsNamespaceElement OnNamespaceElement(QsNamespaceElement element)
        //{
        //}

        //public override QsCallable OnOperation(QsCallable c)
        //{
        //}

        public override Tuple<QsArgumentTuple, QsScope> OnProvidedImplementation(QsArgumentTuple argTuple, QsScope body)
        {
            this.SharedState.GenerateFunctionHeader(this.currentSpecialization, argTuple);

            //// If the current callable is an Operation, attribute this LLVM function as "quantum"
            //if (this.currentCallable.Kind.IsOperation)
            //{
            //    this.SharedState.CurrentFunction.AddAttribute<string>()
            //}

            this.Transformation.Statements.OnScope(body);

            this.SharedState.EndSpecialization();
            this.currentSpecialization = null;

            return new Tuple<QsArgumentTuple, QsScope>(argTuple, body);
        }

        //public override void OnSelfInverseDirective()
        //{
        //}

        //public override ResolvedSignature OnSignature(ResolvedSignature s)
        //{
        //}

        //public override NonNullable<string> OnSourceFile(NonNullable<string> f)
        //{
        //}

        public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec)
        {
            this.SharedState.StartNewSpecialization();
            this.currentSpecialization = spec;
            return base.OnSpecializationDeclaration(spec);
        }

        //public override QsSpecialization OnBodySpecialization(QsSpecialization spec)
        //{
        //    return base.OnBodySpecialization(spec);
        //}

        //public override QsSpecialization OnAdjointSpecialization(QsSpecialization spec)
        //{
        //    return base.OnAdjointSpecialization(spec);
        //}

        //public override QsSpecialization OnControlledSpecialization(QsSpecialization spec)
        //{
        //    return base.OnControlledSpecialization(spec);
        //}

        //public override QsSpecialization OnControlledAdjointSpecialization(QsSpecialization spec)
        //{
        //    return base.OnControlledAdjointSpecialization(spec);
        //}

        //public override SpecializationImplementation OnSpecializationImplementation(SpecializationImplementation implementation)
        //{
        //}

        // This next method is not currently called
        //public override QsCallable OnTypeConstructor(QsCallable c)
        //{
        //}

        public override QsCustomType OnTypeDeclaration(QsCustomType t)
        {
            // Generate the type constructor
            this.SharedState.GenerateConstructor(t);

            return base.OnTypeDeclaration(t);
        }

        //public override QsTuple<QsTypeItem> OnTypeItems(QsTuple<QsTypeItem> tItem)
        //{
        //}
    }
}
