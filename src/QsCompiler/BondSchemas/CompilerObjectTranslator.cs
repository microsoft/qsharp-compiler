// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Microsoft.Quantum.QsCompiler.DataTypes;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    /// <summary>
    /// This class translates Bond schema objects to C# compiler objects.
    /// </summary>
    internal static class CompilerObjectTranslator
    {
        /// <summary>
        /// Creates a C# QsCompilation compiler object from a Bond schema QsCompilation object.
        /// </summary>
        public static SyntaxTree.QsCompilation CreateQsCompilation(QsCompilation bondCompilation) =>
            new SyntaxTree.QsCompilation(
                namespaces: bondCompilation.Namespaces.Select(n => n.ToCompilerObject()).ToImmutableArray(),
                entryPoints: bondCompilation.EntryPoints.Select(e => e.ToCompilerObject()).ToImmutableArray());

        private static BigInteger ToBigInteger(this ArraySegment<byte> blob) =>
            new BigInteger(blob);

        private static DataTypes.Position ToCompilerObject(this Position position) =>
            DataTypes.Position.Create(
                line: position.Line,
                column: position.Column);

        private static DataTypes.Range ToCompilerObject(this Range range) =>
            DataTypes.Range.Create(
                start: range.Start.ToCompilerObject(),
                end: range.End.ToCompilerObject());

        private static SyntaxTokens.AccessModifier ToCompilerObject(this AccessModifier bondAccessModifier) =>
            bondAccessModifier switch
            {
                AccessModifier.DefaultAccess => SyntaxTokens.AccessModifier.DefaultAccess,
                AccessModifier.Internal => SyntaxTokens.AccessModifier.Internal,
                _ => throw new ArgumentException($"Unsupported Bond AccessModifier '{bondAccessModifier}'")
            };

        private static SyntaxTokens.CharacteristicsKind<SyntaxTree.ResolvedCharacteristics> ToCompilerObject(
            this CharacteristicsKindComposition<ResolvedCharacteristics> bondCharacteristicsKindComposition) =>
            bondCharacteristicsKindComposition.ToCompilerObjectGeneric(typeTranslator: ToCompilerObject);

        private static SyntaxTokens.QsExpressionKind<SyntaxTree.TypedExpression, SyntaxTree.Identifier, SyntaxTree.ResolvedType> ToCompilerObject(
            this QsExpressionKindComposition<TypedExpression, Identifier, ResolvedType> bondQsExpressionKindComposition) =>
            bondQsExpressionKindComposition.ToCompilerObjectGeneric<
                SyntaxTree.TypedExpression,
                SyntaxTree.Identifier,
                SyntaxTree.ResolvedType,
                TypedExpression,
                Identifier,
                ResolvedType>(
                    expressionTranslator: ToCompilerObject,
                    symbolTranslator: ToCompilerObject,
                    typeTranslator: ToCompilerObject);

        private static SyntaxTokens.QsGeneratorDirective ToCompilerObject(this QsGeneratorDirective bondQsGeneratorDirective) =>
            bondQsGeneratorDirective switch
            {
                QsGeneratorDirective.Distribute => SyntaxTokens.QsGeneratorDirective.Distribute,
                QsGeneratorDirective.InvalidGenerator => SyntaxTokens.QsGeneratorDirective.InvalidGenerator,
                QsGeneratorDirective.Invert => SyntaxTokens.QsGeneratorDirective.Invert,
                QsGeneratorDirective.SelfInverse => SyntaxTokens.QsGeneratorDirective.SelfInverse,
                _ => throw new ArgumentException($"Unsupported Bond QsGeneratorDirective '{bondQsGeneratorDirective}'")
            };

        private static SyntaxTokens.QsInitializerKind<SyntaxTree.ResolvedInitializer, SyntaxTree.TypedExpression> ToCompilerObject(
            this QsInitializerKindComposition<ResolvedInitializer, TypedExpression> bondQsInitializerKindComposition) =>
            bondQsInitializerKindComposition.ToCompilerObjectGeneric(
                initializerTranslator: ToCompilerObject,
                expressionTranslator: ToCompilerObject);

        private static SyntaxTokens.Modifiers ToCompilerObject(this Modifiers bondModifiers) =>
            new SyntaxTokens.Modifiers(
                access: bondModifiers.Access.ToCompilerObject());

        private static SyntaxTokens.OpProperty ToCompilerObject(this OpProperty bondOpProperty) =>
            bondOpProperty switch
            {
                OpProperty.Adjointable => SyntaxTokens.OpProperty.Adjointable,
                OpProperty.Controllable => SyntaxTokens.OpProperty.Controllable,
                _ => throw new ArgumentException($"Unsupported Bond OpProperty '{bondOpProperty}'")
            };

        private static SyntaxTokens.QsPauli ToCompilerObject(this QsPauli bondQsPauli) =>
            bondQsPauli switch
            {
                QsPauli.PauliI => SyntaxTokens.QsPauli.PauliI,
                QsPauli.PauliX => SyntaxTokens.QsPauli.PauliX,
                QsPauli.PauliY => SyntaxTokens.QsPauli.PauliY,
                QsPauli.PauliZ => SyntaxTokens.QsPauli.PauliZ,
                _ => throw new ArgumentException($"Unsupported Bond QsPauli '{bondQsPauli}'")
            };

        private static SyntaxTokens.QsResult ToCompilerObject(this QsResult bondQsResult) =>
            bondQsResult switch
            {
                QsResult.Zero => SyntaxTokens.QsResult.Zero,
                QsResult.One => SyntaxTokens.QsResult.One,
                _ => throw new ArgumentException($"Unsupported Bond QsResult '{bondQsResult}'")
            };

        private static SyntaxTokens.QsTuple<SyntaxTree.LocalVariableDeclaration<SyntaxTree.QsLocalSymbol>> ToCompilerObject(
            this QsTuple<LocalVariableDeclaration<QsLocalSymbol>> bondQsTuple) =>
            bondQsTuple.ToCompilerObjectGeneric(typeTranslator: ToCompilerObject);

        private static SyntaxTokens.QsTuple<SyntaxTree.QsTypeItem> ToCompilerObject(
            this QsTuple<QsTypeItem> bondQsTuple) =>
            bondQsTuple.ToCompilerObjectGeneric(typeTranslator: ToCompilerObject);

        private static SyntaxTokens.QsTypeKind<SyntaxTree.ResolvedType, SyntaxTree.UserDefinedType, SyntaxTree.QsTypeParameter, SyntaxTree.CallableInformation> ToCompilerObject(
            this QsTypeKindComposition<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> bondQsTypeKindComposition) =>
            bondQsTypeKindComposition.ToCompilerObjectGeneric(
                typeTranslator: ToCompilerObject,
                udtTranslator: ToCompilerObject,
                paramTranslator: ToCompilerObject,
                characteristicsTranslator: ToCompilerObject);

        private static SyntaxTree.CallableInformation ToCompilerObject(this CallableInformation bondCallableInformation) =>
            new SyntaxTree.CallableInformation(
                characteristics: bondCallableInformation.Characteristics.ToCompilerObject(),
                inferredInformation: bondCallableInformation.InferredInformation.ToCompilerObject());

        private static SyntaxTree.Identifier ToCompilerObject(Identifier bondIdentifier)
        {
            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond Identifier '{fieldName}' field is null when Kind is '{bondIdentifier.Kind}'";

            if (bondIdentifier.Kind == IdentifierKind.LocalVariable)
            {
                var localVariable =
                    bondIdentifier.LocalVariable ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("LocalVariable"));

                return SyntaxTree.Identifier.NewLocalVariable(item: localVariable);
            }
            else if (bondIdentifier.Kind == IdentifierKind.GlobalCallable)
            {
                var globalCallable =
                    bondIdentifier.GlobalCallable ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("GlobalCallable"));

                return SyntaxTree.Identifier.NewGlobalCallable(item: globalCallable.ToCompilerObject());
            }
            else
            {
                throw new ArgumentException($"Unsupported Bond IdentifierKind '{bondIdentifier.Kind}'");
            }
        }

        private static SyntaxTree.InferredCallableInformation ToCompilerObject(
            this InferredCallableInformation bondInferredCallableInformation) =>
            new SyntaxTree.InferredCallableInformation(
                isSelfAdjoint: bondInferredCallableInformation.IsSelfAdjoint,
                isIntrinsic: bondInferredCallableInformation.IsIntrinsic);

        private static SyntaxTree.InferredExpressionInformation ToCompilerObject(
            this InferredExpressionInformation bondInferredExpressionInformation) =>
            new SyntaxTree.InferredExpressionInformation(
                isMutable: bondInferredExpressionInformation.IsMutable,
                hasLocalQuantumDependency: bondInferredExpressionInformation.HasLocalQuantumDependency);

        private static SyntaxTree.LocalDeclarations ToCompilerObject(
            this LocalDeclarations bondLocalDeclarations) =>
            new SyntaxTree.LocalDeclarations(
                variables: bondLocalDeclarations.Variables.Select(v => v.ToCompilerObject()).ToImmutableArray());

        private static SyntaxTree.LocalVariableDeclaration<string> ToCompilerObject(
            this LocalVariableDeclaration<string> bondLocalVariableDeclaration) =>
            bondLocalVariableDeclaration.ToCompilerObjectGeneric(typeTranslator: s => s);

        private static SyntaxTree.LocalVariableDeclaration<SyntaxTree.QsLocalSymbol> ToCompilerObject(
            this LocalVariableDeclaration<QsLocalSymbol> bondLocalVariableDeclaration) =>
            bondLocalVariableDeclaration.ToCompilerObjectGeneric(typeTranslator: ToCompilerObject);

        public static SyntaxTree.QsBinding<SyntaxTree.ResolvedInitializer> ToCompilerObject(
            this QsBinding<ResolvedInitializer> bondQsBinding) =>
            bondQsBinding.ToCompilerObjectGeneric(typeTranslator: ToCompilerObject);

        private static SyntaxTree.QsBinding<SyntaxTree.TypedExpression> ToCompilerObject(
            this QsBinding<TypedExpression> bondQsBinding) =>
            bondQsBinding.ToCompilerObjectGeneric(typeTranslator: ToCompilerObject);

        private static SyntaxTree.QsBindingKind ToCompilerObject(this QsBindingKind bondQsBindingKind) =>
            bondQsBindingKind switch
            {
                QsBindingKind.ImmutableBinding => SyntaxTree.QsBindingKind.ImmutableBinding,
                QsBindingKind.MutableBinding => SyntaxTree.QsBindingKind.MutableBinding,
                _ => throw new ArgumentException($"Unsupported Bond QsBindingKind '{bondQsBindingKind}'")
            };

        private static SyntaxTree.QsCallable ToCompilerObject(this QsCallable bondQsCallable) =>
            new SyntaxTree.QsCallable(
                kind: bondQsCallable.Kind.ToCompilerObject(),
                fullName: bondQsCallable.FullName.ToCompilerObject(),
                attributes: bondQsCallable.Attributes.Select(a => a.ToCompilerObject()).ToImmutableArray(),
                modifiers: bondQsCallable.Modifiers.ToCompilerObject(),
                sourceFile: bondQsCallable.SourceFile,
                location: bondQsCallable.Location != null ?
                    bondQsCallable.Location.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.QsLocation>.Null,
                signature: bondQsCallable.Signature.ToCompilerObject(),
                argumentTuple: bondQsCallable.ArgumentTuple.ToCompilerObject(),
                specializations: bondQsCallable.Specializations.Select(s => s.ToCompilerObject()).ToImmutableArray(),
                documentation: bondQsCallable.Documentation.ToImmutableArray(),
                comments: bondQsCallable.Comments.ToCompilerObject());

        private static SyntaxTree.QsCallableKind ToCompilerObject(this QsCallableKind bondQsCallableKind) =>
            bondQsCallableKind switch
            {
                QsCallableKind.Operation => SyntaxTree.QsCallableKind.Operation,
                QsCallableKind.Function => SyntaxTree.QsCallableKind.Function,
                QsCallableKind.TypeConstructor => SyntaxTree.QsCallableKind.TypeConstructor,
                _ => throw new ArgumentException($"Unsupported Bond QsCallableKind '{bondQsCallableKind}'")
            };

        private static SyntaxTree.QsComments ToCompilerObject(this QsComments bondQsComments) =>
            new SyntaxTree.QsComments(
                openingComments: bondQsComments.OpeningComments.ToImmutableArray(),
                closingComments: bondQsComments.ClosingComments.ToImmutableArray());

        private static SyntaxTree.QsConditionalStatement ToCompilerObject(this QsConditionalStatement bondQsConditionalStatement) =>
            new SyntaxTree.QsConditionalStatement(
                conditionalBlocks: bondQsConditionalStatement.ConditionalBlocks.Select(c => c.ToCompilerObject()).ToImmutableArray(),
                @default: bondQsConditionalStatement.Default != null ?
                    bondQsConditionalStatement.Default.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.QsPositionedBlock>.Null);

        private static SyntaxTree.QsConjugation ToCompilerObject(this QsConjugation bondQsConjugation) =>
            new SyntaxTree.QsConjugation(
                outerTransformation: bondQsConjugation.OuterTransformation.ToCompilerObject(),
                innerTransformation: bondQsConjugation.InnerTransformation.ToCompilerObject());

        private static SyntaxTree.QsCustomType ToCompilerObject(this QsCustomType bondQsCustomType) =>
            new SyntaxTree.QsCustomType(
                fullName: bondQsCustomType.FullName.ToCompilerObject(),
                attributes: bondQsCustomType.Attributes.Select(a => a.ToCompilerObject()).ToImmutableArray(),
                modifiers: bondQsCustomType.Modifiers.ToCompilerObject(),
                sourceFile: bondQsCustomType.SourceFile,
                location: bondQsCustomType.Location != null ?
                    bondQsCustomType.Location.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.QsLocation>.Null,
                type: bondQsCustomType.Type.ToCompilerObject(),
                typeItems: bondQsCustomType.TypeItems.ToCompilerObject(),
                documentation: bondQsCustomType.Documentation.ToImmutableArray(),
                comments: bondQsCustomType.Comments.ToCompilerObject());

        private static SyntaxTree.QsDeclarationAttribute ToCompilerObject(this QsDeclarationAttribute bondQsDeclarationAttribute) =>
            new SyntaxTree.QsDeclarationAttribute(
                typeId: bondQsDeclarationAttribute.TypeId != null ?
                    bondQsDeclarationAttribute.TypeId.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.UserDefinedType>.Null,
                argument: bondQsDeclarationAttribute.Argument.ToCompilerObject(),
                offset: bondQsDeclarationAttribute.Offset.ToCompilerObject(),
                comments: bondQsDeclarationAttribute.Comments.ToCompilerObject());

        private static SyntaxTree.QsForStatement ToCompilerObject(this QsForStatement bondQsForStatement) =>
            new SyntaxTree.QsForStatement(
                loopItem: bondQsForStatement.LoopItem.ToCompilerObject(),
                iterationValues: bondQsForStatement.IterationValues.ToCompilerObject(),
                body: bondQsForStatement.Body.ToCompilerObject());

        private static SyntaxTree.QsLocalSymbol ToCompilerObject(this QsLocalSymbol bondQsLocalSymbol)
        {
            if (bondQsLocalSymbol.Kind == QsLocalSymbolKind.ValidName)
            {
                var validName =
                    bondQsLocalSymbol.Name ??
                    throw new ArgumentNullException($"Bond QsLocalSymbol 'Name' field is null when Kind is '{bondQsLocalSymbol.Kind}'");

                return SyntaxTree.QsLocalSymbol.NewValidName(item: validName);
            }
            else
            {
                return bondQsLocalSymbol.Kind switch
                {
                    QsLocalSymbolKind.InvalidName => SyntaxTree.QsLocalSymbol.InvalidName,
                    _ => throw new ArgumentException($"Unsupported Bond QsLocalSymbolKind '{bondQsLocalSymbol.Kind}'")
                };
            }
        }

        private static SyntaxTree.QsLocation ToCompilerObject(this QsLocation bondQsLocation) =>
            new SyntaxTree.QsLocation(
                offset: bondQsLocation.Offset.ToCompilerObject(),
                range: bondQsLocation.Range.ToCompilerObject());

        private static SyntaxTree.QsNamespace ToCompilerObject(this QsNamespace bondQsNamespace) =>
            new SyntaxTree.QsNamespace(
                name: bondQsNamespace.Name,
                elements: bondQsNamespace.Elements.Select(e => e.ToCompilerObject()).ToImmutableArray(),
                documentation: bondQsNamespace.Documentation.ToLookup(
                    p => p.FileName,
                    p => p.DocumentationItems.ToImmutableArray()));

        private static SyntaxTree.QsNamespaceElement ToCompilerObject(this QsNamespaceElement bondQsNamespaceElement)
        {
            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond QsNamespaceElement '{fieldName}' field is null when Kind is '{bondQsNamespaceElement.Kind}'";

            if (bondQsNamespaceElement.Kind == QsNamespaceElementKind.QsCallable)
            {
                var callable =
                    bondQsNamespaceElement.Callable ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Callable"));

                return SyntaxTree.QsNamespaceElement.NewQsCallable(item: callable.ToCompilerObject());
            }
            else if (bondQsNamespaceElement.Kind == QsNamespaceElementKind.QsCustomType)
            {
                var customType =
                    bondQsNamespaceElement.CustomType ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("CustomType"));

                return SyntaxTree.QsNamespaceElement.NewQsCustomType(item: customType.ToCompilerObject());
            }
            else
            {
                throw new ArgumentException($"Unsupported Bond QsNamespaceElementKind '{bondQsNamespaceElement.Kind}'");
            }
        }

        private static SyntaxTree.QsPositionedBlock ToCompilerObject(this QsPositionedBlock bondQsPositionedBlock) =>
            new SyntaxTree.QsPositionedBlock(
                body: bondQsPositionedBlock.Body.ToCompilerObject(),
                location: bondQsPositionedBlock.Location != null ?
                    bondQsPositionedBlock.Location.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.QsLocation>.Null,
                comments: bondQsPositionedBlock.Comments.ToCompilerObject());

        private static SyntaxTree.QsQualifiedName ToCompilerObject(this QsQualifiedName bondQsQualifiedName) =>
            new SyntaxTree.QsQualifiedName(
                @namespace: bondQsQualifiedName.Namespace,
                name: bondQsQualifiedName.Name);

        private static SyntaxTree.QsQubitScope ToCompilerObject(this QsQubitScope bondQsQubitScope) =>
            new SyntaxTree.QsQubitScope(
                kind: bondQsQubitScope.Kind.ToCompilerObject(),
                binding: bondQsQubitScope.Binding.ToCompilerObject(),
                body: bondQsQubitScope.Body.ToCompilerObject());

        private static SyntaxTree.QsQubitScopeKind ToCompilerObject(this QsQubitScopeKind bondQsQubitScopeKind) =>
            bondQsQubitScopeKind switch
            {
                QsQubitScopeKind.Allocate => SyntaxTree.QsQubitScopeKind.Allocate,
                QsQubitScopeKind.Borrow => SyntaxTree.QsQubitScopeKind.Borrow,
                _ => throw new ArgumentException($"Unsupported Bond QsQubitScopeKind '{bondQsQubitScopeKind}'")
            };

        private static SyntaxTree.QsRepeatStatement ToCompilerObject(this QsRepeatStatement bondQsRepeatStatement) =>
            new SyntaxTree.QsRepeatStatement(
                repeatBlock: bondQsRepeatStatement.RepeatBlock.ToCompilerObject(),
                successCondition: bondQsRepeatStatement.SuccessCondition.ToCompilerObject(),
                fixupBlock: bondQsRepeatStatement.FixupBlock.ToCompilerObject());

        private static SyntaxTree.QsScope ToCompilerObject(this QsScope bondQsScope) =>
            new SyntaxTree.QsScope(
                statements: bondQsScope.Statements.Select(s => s.ToCompilerObject()).ToImmutableArray(),
                knownSymbols: bondQsScope.KnownSymbols.ToCompilerObject());

        private static SyntaxTree.QsSpecialization ToCompilerObject(this QsSpecialization bondQsSpecialization) =>
            new SyntaxTree.QsSpecialization(
                kind: bondQsSpecialization.Kind.ToCompilerObject(),
                parent: bondQsSpecialization.Parent.ToCompilerObject(),
                attributes: bondQsSpecialization.Attributes.Select(a => a.ToCompilerObject()).ToImmutableArray(),
                sourceFile: bondQsSpecialization.SourceFile,
                location: bondQsSpecialization.Location != null ?
                    bondQsSpecialization.Location.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.QsLocation>.Null,
                typeArguments: bondQsSpecialization.TypeArguments != null ?
                    bondQsSpecialization.TypeArguments.Select(t => t.ToCompilerObject()).ToImmutableArray().ToQsNullableGeneric() :
                    QsNullable<ImmutableArray<SyntaxTree.ResolvedType>>.Null,
                signature: bondQsSpecialization.Signature.ToCompilerObject(),
                implementation: bondQsSpecialization.Implementation.ToCompilerObject(),
                documentation: bondQsSpecialization.Documentation.ToImmutableArray(),
                comments: bondQsSpecialization.Comments.ToCompilerObject());

        private static SyntaxTree.QsSpecializationKind ToCompilerObject(this QsSpecializationKind bondQsSpecializationKind) =>
            bondQsSpecializationKind switch
            {
                QsSpecializationKind.QsAdjoint => SyntaxTree.QsSpecializationKind.QsAdjoint,
                QsSpecializationKind.QsBody => SyntaxTree.QsSpecializationKind.QsBody,
                QsSpecializationKind.QsControlled => SyntaxTree.QsSpecializationKind.QsControlled,
                QsSpecializationKind.QsControlledAdjoint => SyntaxTree.QsSpecializationKind.QsControlledAdjoint,
                _ => throw new ArgumentException($"Unsupported Bond QsSpecializationKind '{bondQsSpecializationKind}'")
            };

        private static SyntaxTree.QsStatement ToCompilerObject(this QsStatement bondQsStatement) =>
            new SyntaxTree.QsStatement(
                statement: bondQsStatement.Statement.ToCompilerObject(),
                symbolDeclarations: bondQsStatement.SymbolDeclarations.ToCompilerObject(),
                location: bondQsStatement.Location != null ?
                    bondQsStatement.Location.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.QsLocation>.Null,
                comments: bondQsStatement.Comments.ToCompilerObject());

        private static SyntaxTree.QsStatementKind ToCompilerObject(
            this QsStatementKindComposition bondQsStatementKindComposition)
        {
            string InvalidKindForFieldMessage(string fieldName) =>
                $"Bond QsStatementKindComposition '{bondQsStatementKindComposition.Kind}' is not related to '{fieldName}' field";

            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond QsStatementKindComposition '{fieldName}' field is null when Kind is '{bondQsStatementKindComposition.Kind}'";

            if ((bondQsStatementKindComposition.Kind == QsStatementKind.QsExpressionStatement) ||
                (bondQsStatementKindComposition.Kind == QsStatementKind.QsReturnStatement) ||
                (bondQsStatementKindComposition.Kind == QsStatementKind.QsFailStatement))
            {
                var bondTypedExpression =
                    bondQsStatementKindComposition.TypedExpression ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("TypedExpression"));

                var compilerTypedExpression = bondTypedExpression.ToCompilerObject();
                return bondQsStatementKindComposition.Kind switch
                {
                    QsStatementKind.QsExpressionStatement => SyntaxTree.QsStatementKind.NewQsExpressionStatement(
                        item: compilerTypedExpression),
                    QsStatementKind.QsReturnStatement => SyntaxTree.QsStatementKind.NewQsReturnStatement(
                        item: compilerTypedExpression),
                    QsStatementKind.QsFailStatement => SyntaxTree.QsStatementKind.NewQsFailStatement(
                        item: compilerTypedExpression),
                    _ => throw new InvalidOperationException(InvalidKindForFieldMessage("TypedExpression"))
                };
            }
            else if (bondQsStatementKindComposition.Kind == QsStatementKind.QsVariableDeclaration)
            {
                var variableDeclaration =
                    bondQsStatementKindComposition.VariableDeclaration ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("VariableDeclaration"));

                return SyntaxTree.QsStatementKind.NewQsVariableDeclaration(
                    item: variableDeclaration.ToCompilerObject());
            }
            else if (bondQsStatementKindComposition.Kind == QsStatementKind.QsValueUpdate)
            {
                var valueUpdate =
                    bondQsStatementKindComposition.ValueUpdate ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("ValueUpdate"));

                return SyntaxTree.QsStatementKind.NewQsValueUpdate(
                    item: valueUpdate.ToCompilerObject());
            }
            else if (bondQsStatementKindComposition.Kind == QsStatementKind.QsConditionalStatement)
            {
                var conditionalStatement =
                    bondQsStatementKindComposition.ConditionalStatement ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("ConditionalStatement"));

                return SyntaxTree.QsStatementKind.NewQsConditionalStatement(
                    item: conditionalStatement.ToCompilerObject());
            }
            else if (bondQsStatementKindComposition.Kind == QsStatementKind.QsForStatement)
            {
                var forStatement =
                    bondQsStatementKindComposition.ForStatement ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("ForStatement"));

                return SyntaxTree.QsStatementKind.NewQsForStatement(
                    item: forStatement.ToCompilerObject());
            }
            else if (bondQsStatementKindComposition.Kind == QsStatementKind.QsWhileStatement)
            {
                var whileStatement =
                    bondQsStatementKindComposition.WhileStatement ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("WhileStatement"));

                return SyntaxTree.QsStatementKind.NewQsWhileStatement(
                    item: whileStatement.ToCompilerObject());
            }
            else if (bondQsStatementKindComposition.Kind == QsStatementKind.QsRepeatStatement)
            {
                var repeatStatement =
                    bondQsStatementKindComposition.RepeatStatement ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("RepeatStatement"));

                return SyntaxTree.QsStatementKind.NewQsRepeatStatement(
                    item: repeatStatement.ToCompilerObject());
            }
            else if (bondQsStatementKindComposition.Kind == QsStatementKind.QsConjugation)
            {
                var conjugation =
                    bondQsStatementKindComposition.Conjugation ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Conjugation"));

                return SyntaxTree.QsStatementKind.NewQsConjugation(
                    item: conjugation.ToCompilerObject());
            }
            else if (bondQsStatementKindComposition.Kind == QsStatementKind.QsQubitScope)
            {
                var qubitScope =
                    bondQsStatementKindComposition.QubitScope ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("QubitScope"));

                return SyntaxTree.QsStatementKind.NewQsQubitScope(
                    item: qubitScope.ToCompilerObject());
            }
            else if (bondQsStatementKindComposition.Kind == QsStatementKind.EmptyStatement)
            {
                return SyntaxTree.QsStatementKind.EmptyStatement;
            }
            else
            {
                throw new ArgumentException($"Unsupported Bond QsStatementKind '{bondQsStatementKindComposition.Kind}'");
            }
        }

        private static SyntaxTree.QsTypeItem ToCompilerObject(this QsTypeItem bondQsTypeItem)
        {
            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond QsTypeItem '{fieldName}' field is null when Kind is '{bondQsTypeItem.Kind}'";

            if (bondQsTypeItem.Kind == QsTypeItemKind.Named)
            {
                var named =
                    bondQsTypeItem.Named ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Named"));

                return SyntaxTree.QsTypeItem.NewNamed(
                    item: named.ToCompilerObjectGeneric(typeTranslator: item => item));
            }
            else if(bondQsTypeItem.Kind == QsTypeItemKind.Anonymous)
            {
                var anonymous =
                    bondQsTypeItem.Anonymous ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Anonymous"));

                return SyntaxTree.QsTypeItem.NewAnonymous(item: anonymous.ToCompilerObject());
            }
            else
            {
                throw new ArgumentException($"Unsupported Bond QsTypeItemKind '{bondQsTypeItem.Kind}'");
            }
        }

        private static SyntaxTree.QsTypeParameter ToCompilerObject(this QsTypeParameter bondQsTypeParameter) =>
            new SyntaxTree.QsTypeParameter(
                origin: bondQsTypeParameter.Origin.ToCompilerObject(),
                typeName: bondQsTypeParameter.TypeName,
                range: bondQsTypeParameter.Range != null ?
                    bondQsTypeParameter.Range.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<DataTypes.Range>.Null);

        private static SyntaxTree.QsWhileStatement ToCompilerObject(this QsWhileStatement bondQsWhileStatement) =>
            new SyntaxTree.QsWhileStatement(
                condition: bondQsWhileStatement.Condition.ToCompilerObject(),
                body: bondQsWhileStatement.Body.ToCompilerObject());

        private static SyntaxTree.QsValueUpdate ToCompilerObject(this QsValueUpdate bondQsValueUpdate) =>
            new SyntaxTree.QsValueUpdate(
                lhs: bondQsValueUpdate.Lhs.ToCompilerObject(),
                rhs: bondQsValueUpdate.Rhs.ToCompilerObject());

        private static SyntaxTree.ResolvedCharacteristics ToCompilerObject(this ResolvedCharacteristics bondResolvedCharacteristics) =>
            SyntaxTree.ResolvedCharacteristics.New(kind: bondResolvedCharacteristics.Expression.ToCompilerObject());

        // TODO: Check whether this translation is correct in a round-trip.
        private static SyntaxTree.ResolvedInitializer ToCompilerObject(this ResolvedInitializer bondResolvedInitializer) =>
            SyntaxTree.ResolvedInitializer.New(kind: bondResolvedInitializer.Initializer.ToCompilerObject());

        private static SyntaxTree.ResolvedSignature ToCompilerObject(this ResolvedSignature bondResolvedSignature) =>
            new SyntaxTree.ResolvedSignature(
                typeParameters: bondResolvedSignature.TypeParameters.Select(tp => tp.ToCompilerObject()).ToImmutableArray(),
                argumentType: bondResolvedSignature.ArgumentType.ToCompilerObject(),
                returnType: bondResolvedSignature.ReturnType.ToCompilerObject(),
                information: bondResolvedSignature.Information.ToCompilerObject());

        private static SyntaxTree.ResolvedType ToCompilerObject(this ResolvedType bondResolvedType) =>
            SyntaxTree.ResolvedType.New(bondResolvedType.TypeKind.ToCompilerObject());

        private static SyntaxTree.SpecializationImplementation ToCompilerObject(
            this SpecializationImplementation bondSpecializationImplementation)
        {
            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond SpecializationImplementation '{fieldName}' field is null when Kind is '{bondSpecializationImplementation.Kind}'";

            if (bondSpecializationImplementation.Kind == SpecializationImplementationKind.Provided)
            {
                var provided =
                    bondSpecializationImplementation.Provided ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Provided"));

                return SyntaxTree.SpecializationImplementation.NewProvided(
                    item1: provided.Tuple.ToCompilerObject(),
                    item2: provided.Implementation.ToCompilerObject());
            }
            else if (bondSpecializationImplementation.Kind == SpecializationImplementationKind.Generated)
            {
                var generated =
                    bondSpecializationImplementation.Generated ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Generated"));

                return SyntaxTree.SpecializationImplementation.NewGenerated(
                    item: generated.ToCompilerObject());
            }
            else
            {
                return bondSpecializationImplementation.Kind switch
                {
                    SpecializationImplementationKind.External => SyntaxTree.SpecializationImplementation.External,
                    SpecializationImplementationKind.Intrinsic => SyntaxTree.SpecializationImplementation.Intrinsic,
                    _ => throw new ArgumentException($"Unsupported Bond SpecializationImplementationKind '{bondSpecializationImplementation.Kind}'")
                };
            }
        }

        private static SyntaxTree.SymbolTuple ToCompilerObject(this SymbolTuple bondSymbolTuple)
        {
            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond SymbolTuple '{fieldName}' field is null when Kind is '{bondSymbolTuple.Kind}'";

            if (bondSymbolTuple.Kind == SymbolTupleKind.VariableName)
            {
                var variableName =
                    bondSymbolTuple.VariableName ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("VariableName"));

                return SyntaxTree.SymbolTuple.NewVariableName(item: variableName);
            }
            else if (bondSymbolTuple.Kind == SymbolTupleKind.VariableNameTuple)
            {
                var variableNameTuple =
                    bondSymbolTuple.VariableNameTuple ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("VariableNameTuple"));

                return SyntaxTree.SymbolTuple.NewVariableNameTuple(
                    item: variableNameTuple.Select(v => v.ToCompilerObject()).ToImmutableArray());
            }
            else
            {
                return bondSymbolTuple.Kind switch
                {
                    SymbolTupleKind.DiscardedItem => SyntaxTree.SymbolTuple.DiscardedItem,
                    SymbolTupleKind.InvalidItem => SyntaxTree.SymbolTuple.InvalidItem,
                    _ => throw new ArgumentException($"Unsupported Bond SymbolTupleKind '{bondSymbolTuple.Kind}'")
                };
            }
        }

        private static SyntaxTree.TypedExpression ToCompilerObject(this TypedExpression bondTypedExpression) =>
            new SyntaxTree.TypedExpression(
                expression: bondTypedExpression.Expression.ToCompilerObject(),
                typeArguments: bondTypedExpression.TypedArguments.
                    Select(t => Tuple.Create(t.Callable.ToCompilerObject(), t.Name, t.Resolution.ToCompilerObject())).
                    ToImmutableArray(),
                resolvedType: bondTypedExpression.ResolvedType.ToCompilerObject(),
                inferredInformation: bondTypedExpression.InferredInformation.ToCompilerObject(),
                range: bondTypedExpression.Range != null ?
                    bondTypedExpression.Range.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<DataTypes.Range>.Null);

        private static SyntaxTree.UserDefinedType ToCompilerObject(this UserDefinedType bondUserDefinedType) =>
            new SyntaxTree.UserDefinedType(
                @namespace: bondUserDefinedType.Namespace,
                name: bondUserDefinedType.Name,
                range: bondUserDefinedType.Range != null ?
                    bondUserDefinedType.Range.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<DataTypes.Range>.Null);

        private static Tuple<SyntaxTree.SymbolTuple, SyntaxTree.ResolvedType> ToCompilerObject(this QsLoopItem bondQsLoopItem) =>
            new Tuple<SyntaxTree.SymbolTuple, SyntaxTree.ResolvedType>(
                item1: bondQsLoopItem.SymbolTuple.ToCompilerObject(),
                item2: bondQsLoopItem.ResolvedType.ToCompilerObject());

        private static Tuple<SyntaxTree.TypedExpression, SyntaxTree.QsPositionedBlock> ToCompilerObject(
            this QsConditionalBlock bondQsConditionalBlock) =>
            new Tuple<SyntaxTree.TypedExpression, SyntaxTree.QsPositionedBlock>(
                item1: bondQsConditionalBlock.Expression.ToCompilerObject(),
                item2: bondQsConditionalBlock.Block.ToCompilerObject());

        private static SyntaxTokens.CharacteristicsKind<TCompiler> ToCompilerObjectGeneric<TCompiler, TBond>(
            this CharacteristicsKindComposition<TBond> bondCharacteristicsKindComposition,
            Func<TBond, TCompiler> typeTranslator)
        {
            string InvalidKindForFieldMessage(string fieldName) =>
                $"Bond CharacteristicsKind '{bondCharacteristicsKindComposition.Kind}' is not related to '{fieldName}' field";

            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond CharacteristicsKindComposition '{fieldName}' field is null when Kind is '{bondCharacteristicsKindComposition.Kind}'";

            if (bondCharacteristicsKindComposition.Kind == CharacteristicsKind.SimpleSet)
            {
                var simpleSet =
                    bondCharacteristicsKindComposition.SimpleSet ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("SimpleSet"));

                return SyntaxTokens.CharacteristicsKind<TCompiler>.NewSimpleSet(
                    item: simpleSet.ToCompilerObject());
            }
            else if((bondCharacteristicsKindComposition.Kind == CharacteristicsKind.Union) ||
                    (bondCharacteristicsKindComposition.Kind == CharacteristicsKind.Intersection))
            {
                var bondSetOperation =
                    bondCharacteristicsKindComposition.SetOperation ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("SetOperation"));

                var compilerSet1 = typeTranslator(bondSetOperation.Set1);
                var compilerSet2 = typeTranslator(bondSetOperation.Set2);
                return bondCharacteristicsKindComposition.Kind switch
                {
                    CharacteristicsKind.Union => SyntaxTokens.CharacteristicsKind<TCompiler>.NewUnion(
                        item1: compilerSet1,
                        item2: compilerSet2),
                    CharacteristicsKind.Intersection => SyntaxTokens.CharacteristicsKind<TCompiler>.NewIntersection(
                        item1: compilerSet1,
                        item2: compilerSet2),
                    _ => throw new InvalidOperationException(InvalidKindForFieldMessage("SetOperation"))
                };
            }
            else
            {
                return bondCharacteristicsKindComposition.Kind switch
                {
                    CharacteristicsKind.EmptySet => SyntaxTokens.CharacteristicsKind<TCompiler>.EmptySet,
                    CharacteristicsKind.InvalidSetExpr => SyntaxTokens.CharacteristicsKind<TCompiler>.InvalidSetExpr,
                    _ => throw new ArgumentException($"Unsupported Bond CharacteristicsKind '{bondCharacteristicsKindComposition.Kind}'")
                };
            }
        }

        private static SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType> ToCompilerObjectGeneric<
            TCompilerExpression,
            TCompilerSymbol,
            TCompilerType,
            TBondExpression,
            TBondSymbol,
            TBondType>(
                this QsExpressionKindComposition<TBondExpression, TBondSymbol, TBondType> bondQsExpressionKindComposition,
                Func<TBondExpression, TCompilerExpression> expressionTranslator,
                Func<TBondSymbol, TCompilerSymbol> symbolTranslator,
                Func<TBondType, TCompilerType> typeTranslator)
        {
            string InvalidKindForFieldMessage(string fieldName) =>
                $"Bond QsExpressionKind '{bondQsExpressionKindComposition.Kind}' is not related to '{fieldName}' field";

            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond QsExpressionKindComposition '{fieldName}' field is null when Kind is '{bondQsExpressionKindComposition.Kind}'";

            if (bondQsExpressionKindComposition.Kind == QsExpressionKind.Identifier)
            {
                var identifier =
                    bondQsExpressionKindComposition.Identifier ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Identifier"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewIdentifier(
                        item1: symbolTranslator(identifier.Symbol),
                        item2: identifier.Types != null ?
                            identifier.Types.Select(t => typeTranslator(t)).ToImmutableArray().ToQsNullableGeneric() :
                            QsNullable<ImmutableArray<TCompilerType>>.Null);
            }
            else if (bondQsExpressionKindComposition.Kind == QsExpressionKind.IntLiteral)
            {
                var intLiteral =
                    bondQsExpressionKindComposition.IntLiteral ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("IntLiteral"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewIntLiteral(item: intLiteral);
            }
            else if (bondQsExpressionKindComposition.Kind == QsExpressionKind.BigIntLiteral)
            {
                var bigIntLiteral =
                    bondQsExpressionKindComposition.BigIntLiteral != null ?
                        bondQsExpressionKindComposition.BigIntLiteral :
                        throw new ArgumentNullException(UnexpectedNullFieldMessage("BigIntLiteral"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewBigIntLiteral(item: bigIntLiteral.ToBigInteger());
            }
            else if (bondQsExpressionKindComposition.Kind == QsExpressionKind.DoubleLiteral)
            {
                var doubleLiteral =
                    bondQsExpressionKindComposition.DoubleLiteral ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("DoubleLiteral"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewDoubleLiteral(item: doubleLiteral);
            }
            else if (bondQsExpressionKindComposition.Kind == QsExpressionKind.BoolLiteral)
            {
                var boolLiteral =
                    bondQsExpressionKindComposition.BoolLiteral ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("BoolLiteral"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewBoolLiteral(item: boolLiteral);
            }
            else if (bondQsExpressionKindComposition.Kind == QsExpressionKind.StringLiteral)
            {
                var stringLiteral =
                    bondQsExpressionKindComposition.StringLiteral ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("StringLiteral"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewStringLiteral(
                    item1: stringLiteral.StringLiteral,
                    item2: stringLiteral.Expressions.Select(e => expressionTranslator(e)).ToImmutableArray());
            }
            else if (bondQsExpressionKindComposition.Kind == QsExpressionKind.ResultLiteral)
            {
                var resultLiteral =
                    bondQsExpressionKindComposition.ResultLiteral ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("ResultLiteral"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewResultLiteral(item: resultLiteral.ToCompilerObject());
            }
            else if (bondQsExpressionKindComposition.Kind == QsExpressionKind.PauliLiteral)
            {
                var pauliLiteral =
                    bondQsExpressionKindComposition.PauliLiteral ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("PauliLiteral"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewPauliLiteral(item: pauliLiteral.ToCompilerObject());
            }
            else if (bondQsExpressionKindComposition.Kind == QsExpressionKind.NewArray)
            {
                var newArray =
                    bondQsExpressionKindComposition.NewArray ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("NewArray"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewNewArray(
                        item1: typeTranslator(newArray.Type),
                        item2: expressionTranslator(newArray.Expression));
            }
            else if (bondQsExpressionKindComposition.Kind == QsExpressionKind.NamedItem)
            {
                var namedItem =
                    bondQsExpressionKindComposition.NamedItem ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("NamedItem"));

                return SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                    NewNamedItem(
                        item1: expressionTranslator(namedItem.Expression),
                        item2: symbolTranslator(namedItem.Symbol));
            }
            else if ((bondQsExpressionKindComposition.Kind == QsExpressionKind.NEG) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.NOT) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.BNOT) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.UnwrapApplication) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.AdjointApplication) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.ControlledApplication))
            {
                var bondExpression =
                    bondQsExpressionKindComposition.Expression ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Expression"));

                var compilerExpression = expressionTranslator(bondExpression);
                return bondQsExpressionKindComposition.Kind switch
                {
                    QsExpressionKind.NEG => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewNEG(item: compilerExpression),
                    QsExpressionKind.NOT => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewNOT(item: compilerExpression),
                    QsExpressionKind.BNOT => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewBNOT(item: compilerExpression),
                    QsExpressionKind.UnwrapApplication => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewUnwrapApplication(item: compilerExpression),
                    QsExpressionKind.AdjointApplication => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewAdjointApplication(item: compilerExpression),
                    QsExpressionKind.ControlledApplication => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewControlledApplication(item: compilerExpression),
                    _ => throw new InvalidOperationException(InvalidKindForFieldMessage("Expression"))
                };
            }
            else if ((bondQsExpressionKindComposition.Kind == QsExpressionKind.RangeLiteral) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.ArrayItem) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.ADD) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.SUB) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.MUL) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.DIV) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.MOD) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.POW) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.EQ) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.NEQ) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.LT) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.LTE) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.GT) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.GTE) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.AND) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.OR) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.BOR) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.BAND) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.BXOR) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.LSHIFT) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.RSHIFT) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.CallLikeExpression))
            {
                var bondExpressionDouble =
                    bondQsExpressionKindComposition.ExpressionDouble ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("ExpressionDouble"));

                var compilerExpression1 = expressionTranslator(bondExpressionDouble.Expression1);
                var compilerExpression2 = expressionTranslator(bondExpressionDouble.Expression2);
                return bondQsExpressionKindComposition.Kind switch
                {
                    QsExpressionKind.RangeLiteral => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewRangeLiteral(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.ArrayItem => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewArrayItem(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.ADD => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewADD(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.SUB => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewSUB(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.MUL => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewMUL(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.DIV => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewDIV(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.MOD => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewMOD(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.POW => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewPOW(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.EQ => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewEQ(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.NEQ => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewNEQ(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.LT => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewLT(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.LTE => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewLTE(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.GT => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewGT(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.GTE => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewGTE(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.AND => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewAND(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.OR => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewOR(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.BOR => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewBOR(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.BAND => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewBAND(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.BXOR => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewBXOR(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.LSHIFT => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewLSHIFT(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.RSHIFT => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewRSHIFT(item1: compilerExpression1, item2: compilerExpression2),
                    QsExpressionKind.CallLikeExpression => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewCallLikeExpression(item1: compilerExpression1, item2: compilerExpression2),
                    _ => throw new InvalidOperationException(InvalidKindForFieldMessage("ExpressionDouble"))
                };
            }
            else if ((bondQsExpressionKindComposition.Kind == QsExpressionKind.CONDITIONAL) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.CopyAndUpdate))
            {
                var bondExpressionTriple =
                    bondQsExpressionKindComposition.ExpressionTriple ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("ExpressionTriple"));

                var compilerExpression1 = expressionTranslator(bondExpressionTriple.Expression1);
                var compilerExpression2 = expressionTranslator(bondExpressionTriple.Expression2);
                var compilerExpression3 = expressionTranslator(bondExpressionTriple.Expression3);
                return bondQsExpressionKindComposition.Kind switch
                {
                    QsExpressionKind.CONDITIONAL => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewCONDITIONAL(item1: compilerExpression1, item2: compilerExpression2, item3: compilerExpression3),
                    QsExpressionKind.CopyAndUpdate => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewCopyAndUpdate(item1: compilerExpression1, item2: compilerExpression2, item3: compilerExpression3),
                    _ => throw new InvalidOperationException(InvalidKindForFieldMessage("ExpressionTriple"))
                };
            }
            else if ((bondQsExpressionKindComposition.Kind == QsExpressionKind.ValueTuple) ||
                     (bondQsExpressionKindComposition.Kind == QsExpressionKind.ValueArray))
            {
                var bondExpressionArray =
                    bondQsExpressionKindComposition.ExpressionArray ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("ExpressionArray"));

                var compilerExpressionArray = bondExpressionArray.Select(e => expressionTranslator(e)).ToImmutableArray();
                return bondQsExpressionKindComposition.Kind switch
                {
                    QsExpressionKind.ValueTuple => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewValueTuple(item: compilerExpressionArray),
                    QsExpressionKind.ValueArray => SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.
                        NewValueArray(item: compilerExpressionArray),
                    _ => throw new InvalidOperationException(InvalidKindForFieldMessage("ExpressionArray"))
                };
            }
            else
            {
                return bondQsExpressionKindComposition.Kind switch
                {
                    QsExpressionKind.UnitValue =>
                        SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.UnitValue,
                    QsExpressionKind.MissingExpr =>
                        SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.MissingExpr,
                    QsExpressionKind.InvalidExpr =>
                        SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.InvalidExpr,
                    _ => throw new ArgumentException($"Unsupported Bond QsExpressionKind '{bondQsExpressionKindComposition.Kind}'")
                };
            }
        }

        private static SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression> ToCompilerObjectGeneric<
            TCompilerInitializer,
            TCompilerExpression,
            TBondInitializer,
            TBondExpression>(
                this QsInitializerKindComposition<TBondInitializer, TBondExpression> bondQsInitializerKindComposition,
                Func<TBondInitializer, TCompilerInitializer> initializerTranslator,
                Func<TBondExpression, TCompilerExpression> expressionTranslator)
        {
            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond QsInitializerKindComposition '{fieldName}' field is null when Kind is '{bondQsInitializerKindComposition.Kind}'";

            if (bondQsInitializerKindComposition.Kind == QsInitializerKind.QubitRegisterAllocation)
            {
                var expression =
                    bondQsInitializerKindComposition.QubitRegisterAllocation ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("QubitRegisterAllocation"));

                return SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression>.
                    NewQubitRegisterAllocation(item: expressionTranslator(expression));
            }
            else if (bondQsInitializerKindComposition.Kind == QsInitializerKind.QubitTupleAllocation)
            {
                var initializer =
                    bondQsInitializerKindComposition.QubitTupleAllocation ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("QubitTupleAllocation"));

                return SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression>.
                    NewQubitTupleAllocation(item: initializer.Select(i => initializerTranslator(i)).ToImmutableArray());
            }
            else
            {
                return bondQsInitializerKindComposition.Kind switch
                {
                    QsInitializerKind.InvalidInitializer =>
                        SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression>.InvalidInitializer,
                    QsInitializerKind.SingleQubitAllocation =>
                        SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression>.SingleQubitAllocation,
                    _ => throw new ArgumentException($"Unsupported Bond QsInitializer kind '{bondQsInitializerKindComposition.Kind}'")
                };
            }
        }

        private static SyntaxTokens.QsTuple<TCompiler> ToCompilerObjectGeneric<TCompiler, TBond>(
            this QsTuple<TBond> bondQsTuple,
            Func<TBond, TCompiler> typeTranslator)
        {
            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond QsTuple '{fieldName}' field is null when Kind is '{bondQsTuple.Kind}'";

            if (bondQsTuple.Kind == QsTupleKind.QsTuple)
            {
                var bondTupleItems =
                    bondQsTuple.Items ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Items"));

                return SyntaxTokens.QsTuple<TCompiler>.NewQsTuple(
                    item: bondTupleItems.Select(t => t.ToCompilerObjectGeneric(typeTranslator)).ToImmutableArray());
            }
            else if (bondQsTuple.Kind == QsTupleKind.QsTupleItem)
            {
                var bondTupleItem =
                    bondQsTuple.Item ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Item"));

                return SyntaxTokens.QsTuple<TCompiler>.NewQsTupleItem(item: typeTranslator(bondTupleItem));
            }
            else
            {
                throw new ArgumentException($"Unsupported Bond QsTupleKind '{bondQsTuple.Kind}'");
            }
        }

        private static SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics> ToCompilerObjectGeneric<
            TCompilerType,
            TCompilerUdt,
            TCompilerParam,
            TCompilerCharacteristics,
            TBondType,
            TBondUdt,
            TBondParam,
            TBondCharacteristics>(
                this QsTypeKindComposition<TBondType, TBondUdt, TBondParam, TBondCharacteristics> bondQsTypeKindComposition,
                Func<TBondType, TCompilerType> typeTranslator,
                Func<TBondUdt, TCompilerUdt> udtTranslator,
                Func<TBondParam, TCompilerParam> paramTranslator,
                Func<TBondCharacteristics, TCompilerCharacteristics> characteristicsTranslator)
        {
            string UnexpectedNullFieldMessage(string fieldName) =>
                $"Bond QsTypeKindComposition '{fieldName}' field is null when Kind is '{bondQsTypeKindComposition.Kind}'";

            if (bondQsTypeKindComposition.Kind == QsTypeKind.ArrayType)
            {
                var arrayType =
                    bondQsTypeKindComposition.ArrayType ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("ArrayType"));

                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                    NewArrayType(item: typeTranslator(arrayType));
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.TupleType)
            {
                var tupleType =
                    bondQsTypeKindComposition.TupleType ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("TupleType"));

                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                    NewTupleType(item: tupleType.Select(t => typeTranslator(t)).ToImmutableArray());
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.UserDefinedType)
            {
                var userDefinedType =
                    bondQsTypeKindComposition.UserDefinedType ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("UserDefinedType"));

                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                    NewUserDefinedType(item: udtTranslator(userDefinedType));
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.TypeParameter)
            {
                var typeParameter =
                    bondQsTypeKindComposition.TypeParameter ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("TypeParameter"));

                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                    NewTypeParameter(item: paramTranslator(typeParameter));
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.Operation)
            {
                var operation =
                    bondQsTypeKindComposition.Operation ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Operation"));

                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                    NewOperation(
                        item1: Tuple.Create(typeTranslator(operation.Type1), typeTranslator(operation.Type2)),
                        item2: characteristicsTranslator(operation.Characteristics));
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.Function)
            {
                var function =
                    bondQsTypeKindComposition.Function ??
                    throw new ArgumentNullException(UnexpectedNullFieldMessage("Function"));

                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                    NewFunction(
                        item1: typeTranslator(function.Type1),
                        item2: typeTranslator(function.Type2));
            }
            else
            {
                var simpleQsTypeKind = bondQsTypeKindComposition.Kind switch
                {
                    QsTypeKind.UnitType =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.UnitType,
                    QsTypeKind.Int =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Int,
                    QsTypeKind.BigInt =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.BigInt,
                    QsTypeKind.Double =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Double,
                    QsTypeKind.Bool =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Bool,
                    QsTypeKind.String =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.String,
                    QsTypeKind.Qubit =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Qubit,
                    QsTypeKind.Result =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Result,
                    QsTypeKind.Pauli =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Pauli,
                    QsTypeKind.Range =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Range,
                    QsTypeKind.MissingType =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.MissingType,
                    QsTypeKind.InvalidType =>
                        SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.InvalidType,
                    _ => throw new ArgumentException($"Unsupported Bond QsTypeKind {bondQsTypeKindComposition.Kind}")
                };

                return simpleQsTypeKind;
            }
        }

        private static SyntaxTree.LocalVariableDeclaration<TCompiler> ToCompilerObjectGeneric<TCompiler, TBond>(
            this LocalVariableDeclaration<TBond> bondLocalVariableDeclaration,
            Func<TBond, TCompiler> typeTranslator) =>
            new SyntaxTree.LocalVariableDeclaration<TCompiler>(
                variableName: typeTranslator(bondLocalVariableDeclaration.VariableName),
                type: bondLocalVariableDeclaration.Type.ToCompilerObject(),
                inferredInformation: bondLocalVariableDeclaration.InferredInformation.ToCompilerObject(),
                position: bondLocalVariableDeclaration.Position != null ?
                    bondLocalVariableDeclaration.Position.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<DataTypes.Position>.Null,
                range: bondLocalVariableDeclaration.Range.ToCompilerObject());

        private static SyntaxTree.QsBinding<TCompiler> ToCompilerObjectGeneric<TCompiler, TBond>(
            this QsBinding<TBond> bondQsBinding,
            Func<TBond, TCompiler> typeTranslator) =>
            new SyntaxTree.QsBinding<TCompiler>(
                kind: bondQsBinding.Kind.ToCompilerObject(),
                lhs: bondQsBinding.Lhs.ToCompilerObject(),
                rhs: typeTranslator(bondQsBinding.Rhs));

        private static QsNullable<T> ToQsNullableGeneric<T>(this T obj) =>
                QsNullable<T>.NewValue(obj);
    }
}
