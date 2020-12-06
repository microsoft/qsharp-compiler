// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.TargetInstructionSeparation
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using TypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// Syntax tree transformation that replaces all intrinsic callables with ...
    /// </summary>
    public class SeparateTargetInstructions
    : SyntaxTreeTransformation
    {
        public SeparateTargetInstructions()
        {
            this.Namespaces = new NamespaceTransformation(this);
            this.Statements = new StatementTransformation(this, TransformationOptions.Disabled);
            this.StatementKinds = new StatementKindTransformation(this, TransformationOptions.Disabled);
            this.Expressions = new ExpressionTransformation(this, TransformationOptions.Disabled);
            this.ExpressionKinds = new ExpressionKindTransformation(this, TransformationOptions.Disabled);
            this.Types = new TypeTransformation(this, TransformationOptions.Disabled);
        }

        private class NamespaceTransformation
        : Core.NamespaceTransformation
        {
            private readonly ImmutableArray<QsNamespaceElement>.Builder elements;

            internal NamespaceTransformation(SeparateTargetInstructions parent)
            : base(parent)
            {
                this.elements = ImmutableArray.CreateBuilder<QsNamespaceElement>();
            }

            public override QsNamespaceElement OnNamespaceElement(QsNamespaceElement element)
            {
                if (element is QsNamespaceElement.QsCallable c)
                {
                    if (!c.Item.Signature.Information.InferredInformation.IsIntrinsic)
                    {
                        var callable = c.Item;
                        if (callable.Signature.Information.Characteristics.SupportedFunctors.IsNull)
                        {
                            throw new ArgumentException("supported functors could not be determined");
                        }
                        else if (!callable.Signature.Information.Characteristics.SupportedFunctors.Item.Any())
                        {
                            // TODO: no specializations, just add the @TargetInstruction attribute and be done with it
                            this.elements.Add(element);
                        }
                        else
                        {
                            // FIXME: redirect callable is not longer intrinsic!
                            var redirect = callable.WithSpecializations(specs => specs.Select(spec =>
                            {
                                if (!spec.Implementation.IsIntrinsic)
                                {
                                    throw new ArgumentException("non-intrinsic specialization for intrinsic callable");
                                }

                                // Get the correct argument tuple both for the added intrinsic callable
                                // and the generated provided specialization that replaces the intrinsic one.
                                var argTuple = callable.ArgumentTuple;
                                if (spec.Kind.IsQsControlled || spec.Kind.IsQsControlledAdjoint)
                                {
                                    argTuple = SyntaxGenerator.WithControlQubits(
                                        callable.ArgumentTuple,
                                        QsNullable<Position>.Null,
                                        QsLocalSymbol.NewValidName(InternalUse.ControlQubitsName),
                                        QsNullable<DataTypes.Range>.Null);
                                }

                                // one new operation for each specialization
                                var genCallableName = new QsQualifiedName(callable.FullName.Namespace, $"{callable.FullName.Name}__{spec.Kind}");
                                var genCallableBody = new QsSpecialization(
                                    QsSpecializationKind.QsBody,
                                    genCallableName,
                                    spec.Attributes,
                                    spec.SourceFile,
                                    QsNullable<QsLocation>.Null,
                                    spec.TypeArguments,
                                    spec.Signature, 
                                    spec.Implementation,
                                    spec.Documentation,
                                    spec.Comments);
                                var genCallable = new QsCallable(
                                    callable.Kind,
                                    genCallableName,
                                    callable.Attributes.AddRange(spec.Attributes), // TODO: add @TargetInstruction attribute
                                    callable.Modifiers,
                                    spec.SourceFile,
                                    spec.Location,
                                    spec.Signature,
                                    argTuple,
                                    ImmutableArray.Create(genCallableBody),
                                    spec.Documentation,
                                    QsComments.Empty);
                                this.elements.Add(QsNamespaceElement.NewQsCallable(genCallable));

                                // one operation to redirect everything
                                var sigTuple = Tuple.Create(spec.Signature.ArgumentType, spec.Signature.ReturnType);
                                var genCallableType =
                                    callable.Kind == QsCallableKind.Operation ? TypeKind.NewOperation(sigTuple, spec.Signature.Information) :
                                    callable.Kind == QsCallableKind.Function ? TypeKind.NewFunction(sigTuple.Item1, sigTuple.Item2) :
                                    callable.Kind == QsCallableKind.TypeConstructor ? TypeKind.NewFunction(sigTuple.Item1, sigTuple.Item2) :
                                    throw new NotImplementedException("unknown callable kind in target instruction separation");
                                var call = SyntaxGenerator.CallNonGeneric(
                                    SyntaxGenerator.AutoGeneratedExpression(
                                        ExpressionKind.NewIdentifier(Identifier.NewGlobalCallable(genCallableName), QsNullable<ImmutableArray<ResolvedType>>.Null),
                                        genCallableType,
                                        false),
                                    SyntaxGenerator.ArgumentTupleAsExpression(argTuple));
                                var statement = new QsStatement(
                                    QsStatementKind.NewQsExpressionStatement(call),
                                    LocalDeclarations.Empty,
                                    QsNullable<QsLocation>.Null,
                                    QsComments.Empty);
                                var localDeclarations = new LocalDeclarations(
                                    SyntaxGenerator.ExtractItems(argTuple).ValidDeclarations());

                                var scope = new QsScope(
                                    ImmutableArray.Create(statement),
                                    localDeclarations);
                                return spec.WithImplementation(SpecializationImplementation.NewProvided(
                                    argTuple,
                                    scope)); // call to the added callable
                            }).ToImmutableArray());
                            this.elements.Add(QsNamespaceElement.NewQsCallable(redirect));
                        }
                    }
                    else if (c.Item.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                    {
                        throw new ArgumentException("intrinsic specialization for non-intrinsic callable");
                    }
                }
                else
                {
                    this.elements.Add(element);
                }
                return element;
            }

            public override QsNamespace OnNamespace(QsNamespace ns)
            {
                this.elements.Clear();
                foreach (var element in ns.Elements)
                {
                    this.OnNamespaceElement(element);
                }
                return new QsNamespace(ns.Name, this.elements.ToImmutable(), ns.Documentation);
            }
        }
    }
}
