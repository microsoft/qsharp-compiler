// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using static Microsoft.Quantum.QsCompiler.Transformations.AddOutputRecording;

namespace Microsoft.Quantum.QsCompiler.Transformations
{
    using ParameterTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>;

    internal class ReplaceMessageCalls
    {
        internal static QsCompilation Apply(QsCompilation compilation)
        {
            return new Transformation().OnCompilation(new QsCompilation(
                CreateMessageRecorder(compilation.Namespaces),
                compilation.EntryPoints));
        }

        private static ImmutableArray<QsNamespace> CreateMessageRecorder(ImmutableArray<QsNamespace> namespaces)
        {
            var recorder = OutputRecorderDefinition.RecordMessage;

            var apiNamespace =
                namespaces.FirstOrDefault(x => x.Name == OutputRecorderDefinition.NamespaceName)
                ?? new QsNamespace(
                    OutputRecorderDefinition.NamespaceName,
                    ImmutableArray<QsNamespaceElement>.Empty,
                    Enumerable.Empty<ImmutableArray<string>>().ToLookup(x => default(string)));

            var definedElements = apiNamespace.Elements.Partition(e => e.IsQsCallable);
            var definedCallables = definedElements.Item1.ToDictionary(
                e => e.GetFullName().Name,
                e => ((QsNamespaceElement.QsCallable)e).Item);

            var apiSource = new Source(Path.GetTempFileName(), QsNullable<string>.Null);
            var recorderDeclaration = definedCallables.TryGetValue(recorder.QSharpName.Name, out var decl) ? decl : CreateMessageRecorderDecl(apiSource);
            if (SymbolResolution.TryGetTargetInstructionName(recorderDeclaration.Attributes).IsNull)
            {
                recorderDeclaration = recorderDeclaration.AddAttribute(
                    AttributeUtils.BuildAttribute(
                        BuiltIn.TargetInstruction.FullName,
                        AttributeUtils.StringArgument(recorder.TargetInstructionName)));
            }

            definedCallables[recorder.QSharpName.Name] = recorderDeclaration;
            if (!(recorderDeclaration.Kind.IsFunction
                && recorderDeclaration.Signature.ArgumentType.Equals(recorder.QSharpSignature.ArgumentType)
                && recorderDeclaration.Signature.ReturnType.Resolution.IsUnitType
                && SymbolResolution.TryGetTargetInstructionName(recorderDeclaration.Attributes) is var instr
                && instr.IsValue && instr.Item == recorder.TargetInstructionName))
            {
                throw new InvalidOperationException("callable with the expected recorder name is already defined but doesn't have the expected signature");
            }

            apiNamespace = apiNamespace.WithElements(_ =>
                definedElements.Item2.Concat(definedCallables.Values.Select(QsNamespaceElement.NewQsCallable))
                .ToImmutableArray());
            return namespaces.Where(x => x.Name != apiNamespace.Name).Append(apiNamespace).ToImmutableArray();
        }

        private static QsCallable CreateMessageRecorderDecl(Source source)
        {
            var recorder = OutputRecorderDefinition.RecordMessage;
            var parameterTuple = ParameterTuple.NewQsTuple(ImmutableArray.Create(
                ParameterTuple.NewQsTupleItem(
                    new LocalVariableDeclaration<QsLocalSymbol, ResolvedType>(
                        QsLocalSymbol.NewValidName("input"),
                        recorder.QSharpSignature.ArgumentType,
                        InferredExpressionInformation.ParameterDeclaration,
                        QsNullable<Position>.Null,
                        DataTypes.Range.Zero))));

            return new QsCallable(
                QsCallableKind.Function,
                recorder.QSharpName,
                ImmutableArray<QsDeclarationAttribute>.Empty,
                Access.Internal,
                source,
                QsNullable<QsLocation>.Null,
                recorder.QSharpSignature,
                parameterTuple,
                ImmutableArray.Create(
                    new QsSpecialization(
                        QsSpecializationKind.QsBody,
                        recorder.QSharpName,
                        ImmutableArray<QsDeclarationAttribute>.Empty,
                        source,
                        QsNullable<QsLocation>.Null,
                        QsNullable<ImmutableArray<ResolvedType>>.Null,
                        recorder.QSharpSignature,
                        SpecializationImplementation.Intrinsic,
                        ImmutableArray<string>.Empty,
                        QsComments.Empty)),
                ImmutableArray<string>.Empty,
                QsComments.Empty);
        }

        private class Transformation : SyntaxTreeTransformation
        {
            public Transformation()
                : base()
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation(this);
                this.StatementKinds = new StatementKindTransformation(this);
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ReplaceExpressionTransformation(this);
                this.Types = new TypeTransformation(this, TransformationOptions.Disabled);
            }

            private class ReplaceExpressionTransformation : ExpressionKindTransformation
            {
                public ReplaceExpressionTransformation(SyntaxTreeTransformation parent)
                    : base(parent)
                {
                }

                public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    if (sym is Identifier.GlobalCallable global)
                    {
                        var name = global.Item;
                        if (name.Equals(BuiltIn.Message.FullName))
                        {
                            sym = Identifier.NewGlobalCallable(OutputRecorderDefinition.RecordMessage.QSharpName);
                        }
                    }

                    return base.OnIdentifier(sym, tArgs);
                }
            }
        }
    }
}
