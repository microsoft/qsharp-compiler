// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Microsoft.Quantum.QsCompiler.DataTypes;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    /// <summary>
    /// This class translates compiler objects to Bond schema objects.
    /// </summary>
    public static class BondSchemaTranslator
    {
        /// <summary>
        /// Creates a Bond schema QsCompilation object from a QsCompilation compiler object.
        /// </summary>
        public static QsCompilation CreateBondCompilation(SyntaxTree.QsCompilation qsCompilation) =>
            new QsCompilation
            {
                Namespaces = qsCompilation.Namespaces.Select(n => n.ToBondSchema()).ToList(),
                EntryPoints = qsCompilation.EntryPoints.Select(e => e.ToBondSchema()).ToList()
            };

        private static AccessModifier ToBondSchema(this SyntaxTokens.AccessModifier accessModifier) =>
            accessModifier.Tag switch
            {
                SyntaxTokens.AccessModifier.Tags.DefaultAccess => AccessModifier.DefaultAccess,
                SyntaxTokens.AccessModifier.Tags.Internal => AccessModifier.Internal,
                _ => throw new ArgumentException($"Unsupported AccessModifier: {accessModifier}")
            };

        private static CallableInformation ToBondSchema(this SyntaxTree.CallableInformation callableInformation) =>
            new CallableInformation
            {
                Characteristics = callableInformation.Characteristics.ToBondSchema(),
                InferredInformation = callableInformation.InferredInformation.ToBondSchema()
            };

        private static Identifier ToBondSchema(this SyntaxTree.Identifier identifier)
        {
            string? bondLocalVariable = null;
            QsQualifiedName? bondGlobalCallable = null;
            NonNullable<string> compilerLocalVariable = default;
            SyntaxTree.QsQualifiedName? compilerGlobalCallable = null;
            IdentifierKind kind;
            if (identifier.TryGetLocalVariable(ref compilerLocalVariable))
            {
                kind = IdentifierKind.LocalVariable;
                bondLocalVariable = compilerLocalVariable.Value;
            }
            else if (identifier.TryGetGlobalCallable(ref compilerGlobalCallable))
            {
                kind = IdentifierKind.GlobalCallable;
                bondGlobalCallable = compilerGlobalCallable.ToBondSchema();
            }
            else if (identifier.IsInvalidIdentifier)
            {
                kind = IdentifierKind.InvalidIdentifier;
            }
            else
            {
                throw new ArgumentException($"Unsupported Identifier {identifier}");
            }

            return new Identifier
            {
                Kind = kind,
                LocalVariable = bondLocalVariable,
                GlobalCallable = bondGlobalCallable
            };
        }
            

        private static InferredCallableInformation ToBondSchema(this SyntaxTree.InferredCallableInformation inferredCallableInformation) =>
            new InferredCallableInformation
            {
                IsSelfAdjoint = inferredCallableInformation.IsSelfAdjoint,
                IsIntrinsic = inferredCallableInformation.IsIntrinsic
            };

        private static InferredExpressionInformation ToBondSchema(this SyntaxTree.InferredExpressionInformation inferredExpressionInformation) =>
            new InferredExpressionInformation
            {
                IsMutable = inferredExpressionInformation.IsMutable,
                HasLocalQuantumDependency = inferredExpressionInformation.HasLocalQuantumDependency
            };

        private static LocalDeclarations ToBondSchema(this SyntaxTree.LocalDeclarations localDeclarations) =>
            new LocalDeclarations
            {
                Variables = localDeclarations.Variables.Select(v => v.ToBondSchemaGeneric(typeTranslator: ToBondSchema)).ToList()
            };

        private static Modifiers ToBondSchema(this SyntaxTokens.Modifiers modifiers) =>
            new Modifiers
            {
                Access = modifiers.Access.ToBondSchema()
            };

        private static OpProperty ToBondSchema(this SyntaxTokens.OpProperty opProperty) =>
            opProperty.Tag switch
            {
                SyntaxTokens.OpProperty.Tags.Adjointable => OpProperty.Adjointable,
                SyntaxTokens.OpProperty.Tags.Controllable => OpProperty.Controllable,
                _ => throw new ArgumentException($"Unsupported OpProperty {opProperty}")
            };

        private static Position ToBondSchema(this DataTypes.Position position) =>
            new Position
            {
                Line = position.Line,
                Column = position.Column
            };

        private static QsBindingKind ToBondSchema(this SyntaxTree.QsBindingKind qsBindingKind) =>
            qsBindingKind.Tag switch
            {
                SyntaxTree.QsBindingKind.Tags.ImmutableBinding => QsBindingKind.ImmutableBinding,
                SyntaxTree.QsBindingKind.Tags.MutableBinding => QsBindingKind.MutableBinding,
                _ => throw new ArgumentException($"Unsupported QsBindingKind {qsBindingKind}")
            };

        private static QsCallable ToBondSchema(this SyntaxTree.QsCallable qsCallable) =>
            new QsCallable
            {
                Kind = qsCallable.Kind.ToBondSchema(),
                FullName = qsCallable.FullName.ToBondSchema(),
                Attributes = qsCallable.Attributes.Select(a => a.ToBondSchema()).ToList(),
                Modifiers = qsCallable.Modifiers.ToBondSchema(),
                SourceFile = qsCallable.SourceFile.Value,
                Location = qsCallable.Location.IsNull ?
                    null :
                    qsCallable.Location.Item.ToBondSchema(),
                Signature = qsCallable.Signature.ToBondSchema(),
                ArgumentTuple = qsCallable.ArgumentTuple.ToBondSchema(),
                Specializations = qsCallable.Specializations.Select(s => s.ToBondSchema()).ToList(),
                Documentation = qsCallable.Documentation.ToList(),
                Comments = qsCallable.Comments.ToBondSchema()
            };

        private static QsCallableKind ToBondSchema(this SyntaxTree.QsCallableKind qsCallableKind) =>
            qsCallableKind.Tag switch
            {
                SyntaxTree.QsCallableKind.Tags.Function => QsCallableKind.Function,
                SyntaxTree.QsCallableKind.Tags.Operation => QsCallableKind.Operation,
                SyntaxTree.QsCallableKind.Tags.TypeConstructor => QsCallableKind.TypeConstructor,
                _ => throw new ArgumentException($"Unsupported QsCallableKind {qsCallableKind}")
            };

        private static QsComments ToBondSchema(this SyntaxTree.QsComments qsComments) =>
            new QsComments
            {
                OpeningComments = qsComments.OpeningComments.ToList(),
                ClosingComments = qsComments.ClosingComments.ToList()
            };

        private static QsConditionalStatement ToBondSchema(this SyntaxTree.QsConditionalStatement qsConditionalStatement) =>
            new QsConditionalStatement
            {
                ConditionalBlocks = qsConditionalStatement.ConditionalBlocks.Select(c => c.ToQsConditionalBlock()).ToList(),
                Default = qsConditionalStatement.Default.IsNull ?
                    null :
                    qsConditionalStatement.Default.Item.ToBondSchema()
            };

        private static QsConjugation ToBondSchema(this SyntaxTree.QsConjugation qsConjugation) =>
            new QsConjugation
            {
                OuterTransformation = qsConjugation.OuterTransformation.ToBondSchema(),
                InnerTransformation = qsConjugation.InnerTransformation.ToBondSchema()
            };

        private static QsCustomType ToBondSchema(this SyntaxTree.QsCustomType qsCustomType) =>
            new QsCustomType
            {
                FullName = qsCustomType.FullName.ToBondSchema(),
                Attributes = qsCustomType.Attributes.Select(a => a.ToBondSchema()).ToList(),
                Modifiers = qsCustomType.Modifiers.ToBondSchema(),
                SourceFile = qsCustomType.SourceFile.Value,
                Location = qsCustomType.Location.IsNull ?
                    null :
                    qsCustomType.Location.Item.ToBondSchema(),
                Type = qsCustomType.Type.ToBondSchema(),
                TypeItems = qsCustomType.TypeItems.ToBondSchema(),
                Documentation = qsCustomType.Documentation.ToList(),
                Comments = qsCustomType.Comments.ToBondSchema()
            };

        private static QsDeclarationAttribute ToBondSchema(this SyntaxTree.QsDeclarationAttribute qsDeclarationAttribute) =>
            new QsDeclarationAttribute
            {
                TypeId = qsDeclarationAttribute.TypeId.IsNull ?
                    null :
                    qsDeclarationAttribute.TypeId.Item.ToBondSchema(),
                Argument = qsDeclarationAttribute.Argument.ToBondSchema(),
                Offset = qsDeclarationAttribute.Offset.ToBondSchema(),
                Comments = qsDeclarationAttribute.Comments.ToBondSchema()
            };

        private static QsExpressionKindComposition<TypedExpression, Identifier, ResolvedType> ToBondSchema(
            this SyntaxTokens.QsExpressionKind<SyntaxTree.TypedExpression, SyntaxTree.Identifier, SyntaxTree.ResolvedType> qsExpressionKind) =>
            qsExpressionKind.ToBondSchemaGeneric<
                TypedExpression,
                Identifier,
                ResolvedType,
                SyntaxTree.TypedExpression,
                SyntaxTree.Identifier,
                SyntaxTree.ResolvedType>(
            expressionTranslator: ToBondSchema,
            symbolTranslator: ToBondSchema,
            typeTranslator: ToBondSchema);

        private static QsForStatement ToBondSchema(this SyntaxTree.QsForStatement qsForStatement) =>
            new QsForStatement
            {
                LoopItem = qsForStatement.LoopItem.ToQsLoopItem(),
                IterationValues = qsForStatement.IterationValues.ToBondSchema(),
                Body = qsForStatement.Body.ToBondSchema()
            };

        private static QsGeneratorDirective ToBondSchema(this SyntaxTokens.QsGeneratorDirective qsGeneratorDirective) =>
            qsGeneratorDirective.Tag switch
            {
                SyntaxTokens.QsGeneratorDirective.Tags.Distribute => QsGeneratorDirective.Distribute,
                SyntaxTokens.QsGeneratorDirective.Tags.InvalidGenerator => QsGeneratorDirective.InvalidGenerator,
                SyntaxTokens.QsGeneratorDirective.Tags.Invert => QsGeneratorDirective.Invert,
                SyntaxTokens.QsGeneratorDirective.Tags.SelfInverse => QsGeneratorDirective.SelfInverse,
                _ => throw new ArgumentException($"Unsupported QsGeneratorDirective {qsGeneratorDirective}")
            };

        private static QsQualifiedName ToBondSchema(this SyntaxTree.QsQualifiedName qsQualifiedName) =>
            new QsQualifiedName
            {
                Namespace = qsQualifiedName.Namespace.Value,
                Name = qsQualifiedName.Name.Value
            };

        private static QsLocalSymbol ToBondSchema(this SyntaxTree.QsLocalSymbol qsLocalSymbol)
        {
            string? bondValidName = null;
            NonNullable<string> compilerValidName = default;
            QsLocalSymbolKind kind;
            if (qsLocalSymbol.TryGetValidName(ref compilerValidName))
            {
                kind = QsLocalSymbolKind.ValidName;
                bondValidName = compilerValidName.Value;
            }
            else if (qsLocalSymbol.IsInvalidName)
            {
                kind = QsLocalSymbolKind.InvalidName;
            }
            else
            {
                throw new ArgumentException($"Unsupported QsLocalSymbol {qsLocalSymbol}");
            }

            return new QsLocalSymbol
            {
                Kind = kind,
                Name = bondValidName
            };
        }

        private static LocalVariableDeclaration<QsLocalSymbol> ToBondSchema(
            this SyntaxTree.LocalVariableDeclaration<SyntaxTree.QsLocalSymbol> localVariableDeclaration) =>
            localVariableDeclaration.ToBondSchemaGeneric(typeTranslator: ToBondSchema);

        private static QsLocation ToBondSchema(this SyntaxTree.QsLocation qsLocation) =>
            new QsLocation
            {
                Offset = qsLocation.Offset.ToBondSchema(),
                Range = qsLocation.Range.ToBondSchema()
            };

        private static QsNamespace ToBondSchema(this SyntaxTree.QsNamespace qsNamespace) =>
            new QsNamespace
            {
                Name = qsNamespace.Name.Value,
                Elements = qsNamespace.Elements.Select(e => e.ToBondSchema()).ToList(),
                Documentation = qsNamespace.Documentation.ToQsSourceFileDocumentationList()
            };

        private static QsNamespaceElement ToBondSchema(this SyntaxTree.QsNamespaceElement qsNamespaceElement)
        {
            QsNamespaceElementKind kind;
            SyntaxTree.QsCallable? qsCallable = null;
            SyntaxTree.QsCustomType? qsCustomType = null;
            if (qsNamespaceElement.TryGetCallable(ref qsCallable))
            {
                kind = QsNamespaceElementKind.QsCallable;
            }
            else if (qsNamespaceElement.TryGetCustomType(ref qsCustomType))
            {
                kind = QsNamespaceElementKind.QsCustomType;
            }
            else
            {
                throw new ArgumentException($"Unsupported {typeof(SyntaxTree.QsNamespaceElement)} kind");
            }

            var bondQsNamespaceElement = new QsNamespaceElement
            {
                Kind = kind,
                Callable = qsCallable?.ToBondSchema(),
                CustomType = qsCustomType?.ToBondSchema()
            };

            return bondQsNamespaceElement;
        }

        private static QsPauli ToBondSchema(this SyntaxTokens.QsPauli qsPauli) =>
            qsPauli.Tag switch
            {
                SyntaxTokens.QsPauli.Tags.PauliI => QsPauli.PauliI,
                SyntaxTokens.QsPauli.Tags.PauliX => QsPauli.PauliX,
                SyntaxTokens.QsPauli.Tags.PauliY => QsPauli.PauliY,
                SyntaxTokens.QsPauli.Tags.PauliZ => QsPauli.PauliZ,
                _ => throw new ArgumentException($"Unsupported ")
            };

        private static QsPositionedBlock ToBondSchema(this SyntaxTree.QsPositionedBlock qsPositionedBlock) =>
            new QsPositionedBlock
            {
                Body = qsPositionedBlock.Body.ToBondSchema(),
                Location = qsPositionedBlock.Location.IsNull ?
                    null :
                    qsPositionedBlock.Location.Item.ToBondSchema(),
                Comments = qsPositionedBlock.Comments.ToBondSchema()
            };

        private static QsQubitScope ToBondSchema(this SyntaxTree.QsQubitScope qsQubitScope) =>
            new QsQubitScope
            {
                Kind = qsQubitScope.Kind.ToBondSchema(),
                Binding = qsQubitScope.Binding.ToBondSchemaGeneric(typeTranslator: ToBondSchema),
                Body = qsQubitScope.Body.ToBondSchema()
            };

        private static QsQubitScopeKind ToBondSchema(this SyntaxTree.QsQubitScopeKind qsQubitScopeKind) =>
            qsQubitScopeKind.Tag switch
            {
                SyntaxTree.QsQubitScopeKind.Tags.Allocate => QsQubitScopeKind.Allocate,
                SyntaxTree.QsQubitScopeKind.Tags.Borrow => QsQubitScopeKind.Borrow,
                _ => throw new ArgumentException($"Unsupported QsQubitScopeKind {qsQubitScopeKind}")
            };

        private static QsRepeatStatement ToBondSchema(this SyntaxTree.QsRepeatStatement qsRepeatStatement) =>
            new QsRepeatStatement
            {
                RepeatBlock = qsRepeatStatement.RepeatBlock.ToBondSchema(),
                SuccessCondition = qsRepeatStatement.SuccessCondition.ToBondSchema(),
                FixupBlock = qsRepeatStatement.FixupBlock.ToBondSchema()
            };

        private static QsResult ToBondSchema(this SyntaxTokens.QsResult qsResult) =>
            qsResult.Tag switch
            {
                SyntaxTokens.QsResult.Tags.Zero => QsResult.Zero,
                SyntaxTokens.QsResult.Tags.One => QsResult.One,
                _ => throw new ArgumentException($"Unsupported QsResult {qsResult}")
            };

        private static QsScope ToBondSchema(this SyntaxTree.QsScope qsScope) =>
            new QsScope
            {
                Statements = qsScope.Statements.Select(s => s.ToBondSchema()).ToList(),
                KnownSymbols = qsScope.KnownSymbols.ToBondSchema()
            };

        private static QsSpecialization ToBondSchema(this SyntaxTree.QsSpecialization qsSpecialization) =>
            new QsSpecialization
            {
                Kind = qsSpecialization.Kind.ToBondSchema(),
                Parent = qsSpecialization.Parent.ToBondSchema(),
                Attributes = qsSpecialization.Attributes.Select(a => a.ToBondSchema()).ToList(),
                SourceFile = qsSpecialization.SourceFile.Value,
                Location = qsSpecialization.Location.IsNull ?
                    null :
                    qsSpecialization.Location.Item.ToBondSchema(),
                TypeArguments = qsSpecialization.TypeArguments.IsNull ?
                    null :
                    qsSpecialization.TypeArguments.Item.Select(t => t.ToBondSchema()).ToList(),
                Signature = qsSpecialization.Signature.ToBondSchema(),
                Implementation = qsSpecialization.Implementation.ToBondSchema(),
                Documentation = qsSpecialization.Documentation.ToList(),
                Comments = qsSpecialization.Comments.ToBondSchema()
            };

        private static QsSpecializationKind ToBondSchema(this SyntaxTree.QsSpecializationKind qsSpecializationKind) =>
            qsSpecializationKind.Tag switch
            {
                SyntaxTree.QsSpecializationKind.Tags.QsAdjoint => QsSpecializationKind.QsAdjoint,
                SyntaxTree.QsSpecializationKind.Tags.QsBody => QsSpecializationKind.QsBody,
                SyntaxTree.QsSpecializationKind.Tags.QsControlled => QsSpecializationKind.QsControlled,
                SyntaxTree.QsSpecializationKind.Tags.QsControlledAdjoint => QsSpecializationKind.QsControlledAdjoint,
                _ => throw new ArgumentException($"Unsupported QsSpecializationKind {qsSpecializationKind}")
            };

        private static QsStatement ToBondSchema(this SyntaxTree.QsStatement qsStatement) =>
            new QsStatement
            {
                Statement = qsStatement.Statement.ToBondSchema(),
                SymbolDeclarations = qsStatement.SymbolDeclarations.ToBondSchema(),
                Location = qsStatement.Location.IsNull ?
                    null :
                    qsStatement.Location.Item.ToBondSchema(),
                Comments = qsStatement.Comments.ToBondSchema()
            };

        private static QsStatementKindComposition ToBondSchema(this SyntaxTree.QsStatementKind qsStatementKind)
        {
            SyntaxTree.TypedExpression? compilerTypedExpression = null;
            SyntaxTree.QsBinding<SyntaxTree.TypedExpression>? compilerVariableDeclaration = null;
            SyntaxTree.QsValueUpdate? compilerValueUpdate = null;
            SyntaxTree.QsConditionalStatement? compilerConditionalStatement = null;
            SyntaxTree.QsForStatement? compilerForStatement = null;
            SyntaxTree.QsWhileStatement? compilerWhileStatement = null;
            SyntaxTree.QsRepeatStatement? compilerRepeatStatement = null;
            SyntaxTree.QsConjugation? compilerConjugation = null;
            SyntaxTree.QsQubitScope? compilerQubitScope = null;
            QsStatementKind kind;
            if (qsStatementKind.TryGetExpressionStatement(ref compilerTypedExpression))
            {
                kind = QsStatementKind.QsExpressionStatement;
            }
            else if (qsStatementKind.TryGetReturnStatement(ref compilerTypedExpression))
            {
                kind = QsStatementKind.QsReturnStatement;
            }
            else if (qsStatementKind.TryGetFailStatement(ref compilerTypedExpression))
            {
                kind = QsStatementKind.QsFailStatement;
            }
            else if (qsStatementKind.TryGetVariableDeclaration(ref compilerVariableDeclaration))
            {
                kind = QsStatementKind.QsVariableDeclaration;
            }
            else if (qsStatementKind.TryGetValueUpdate(ref compilerValueUpdate))
            {
                kind = QsStatementKind.QsValueUpdate;
            }
            else if (qsStatementKind.TryGetConditionalStatement(ref compilerConditionalStatement))
            {
                kind = QsStatementKind.QsConditionalStatement;
            }
            else if (qsStatementKind.TryGetForStatement(ref compilerForStatement))
            {
                kind = QsStatementKind.QsForStatement;
            }
            else if (qsStatementKind.TryGetWhileStatement(ref compilerWhileStatement))
            {
                kind = QsStatementKind.QsWhileStatement;
            }
            else if (qsStatementKind.TryGetRepeatStatement(ref compilerRepeatStatement))
            {
                kind = QsStatementKind.QsRepeatStatement;
            }
            else if (qsStatementKind.TryGetConjugation(ref compilerConjugation))
            {
                kind = QsStatementKind.QsConjugation;
            }
            else if (qsStatementKind.TryGetQubitScope(ref compilerQubitScope))
            {
                kind = QsStatementKind.QsQubitScope;
            }
            else if (qsStatementKind.IsEmptyStatement)
            {
                kind = QsStatementKind.EmptyStatement;
            }
            else
            {
                throw new ArgumentException($"Unsupported QsStatementKind {qsStatementKind}");
            }

            return new QsStatementKindComposition
            {
                Kind = kind,
                TypedExpression = compilerTypedExpression?.ToBondSchema(),
                VariableDeclaration = compilerVariableDeclaration?.ToBondSchemaGeneric(typeTranslator: ToBondSchema),
                ValueUpdate = compilerValueUpdate?.ToBondSchema(),
                ConditionalStatement = compilerConditionalStatement?.ToBondSchema(),
                ForStatement = compilerForStatement?.ToBondSchema(),
                WhileStatement = compilerWhileStatement?.ToBondSchema(),
                RepeatStatement = compilerRepeatStatement?.ToBondSchema(),
                Conjugation = compilerConjugation?.ToBondSchema(),
                QubitScope = compilerQubitScope?.ToBondSchema()
            };
        }

        private static QsTuple<LocalVariableDeclaration<QsLocalSymbol>> ToBondSchema(
            this SyntaxTokens.QsTuple<SyntaxTree.LocalVariableDeclaration<SyntaxTree.QsLocalSymbol>> localVariableDeclaration) =>
            localVariableDeclaration.ToBondSchemaGeneric(typeTranslator: ToBondSchema);

        private static QsTuple<QsTypeItem> ToBondSchema(
            this SyntaxTokens.QsTuple<SyntaxTree.QsTypeItem> qsTypeItem) =>
            qsTypeItem.ToBondSchemaGeneric(typeTranslator: ToBondSchema);

        private static QsTypeKindComposition<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> ToBondSchema(
            this SyntaxTokens.QsTypeKind<SyntaxTree.ResolvedType, SyntaxTree.UserDefinedType, SyntaxTree.QsTypeParameter, SyntaxTree.CallableInformation> qsTypeKind) =>
            qsTypeKind.ToBondSchemaGeneric
                <ResolvedType,
                 UserDefinedType,
                 QsTypeParameter,
                 CallableInformation,
                 SyntaxTree.ResolvedType,
                 SyntaxTree.UserDefinedType,
                 SyntaxTree.QsTypeParameter,
                 SyntaxTree.CallableInformation>(
            dataTranslator: ToBondSchema,
            udtTranslator: ToBondSchema,
            paramTranslator: ToBondSchema,
            characteristicsTranslator: ToBondSchema);

        private static QsTypeItem ToBondSchema(this SyntaxTree.QsTypeItem qsTypeItem)
        {
            SyntaxTree.ResolvedType? compilerAnonymous = null;
            SyntaxTree.LocalVariableDeclaration<NonNullable<string>>? compilerNamed = null;
            QsTypeItemKind kind;
            if (qsTypeItem.TryGetAnonymous(ref compilerAnonymous))
            {
                kind = QsTypeItemKind.Anonymous;
            }
            else if (qsTypeItem.TryGetNamed(ref compilerNamed))
            {
                kind = QsTypeItemKind.Named;
            }
            else
            {
                throw new ArgumentException($"Unsupported QsTypeItem {qsTypeItem}");
            }

            return new QsTypeItem
            {
                Kind = kind,
                Anonymous = compilerAnonymous?.ToBondSchema(),
                Named = compilerNamed?.ToBondSchemaGeneric(typeTranslator: ToBondSchema)
            };
        }

        private static QsTypeParameter ToBondSchema(this SyntaxTree.QsTypeParameter qsTypeParameter) =>
            new QsTypeParameter
            {
                Origin = qsTypeParameter.Origin.ToBondSchema(),
                TypeName = qsTypeParameter.TypeName.Value,
                Range = qsTypeParameter.Range.IsNull ?
                    null :
                    qsTypeParameter.Range.Item.ToBondSchema()
            };

        private static QsValueUpdate ToBondSchema(this SyntaxTree.QsValueUpdate valueUpdate) =>
            new QsValueUpdate
            {
                Lhs = valueUpdate.Lhs.ToBondSchema(),
                Rhs = valueUpdate.Rhs.ToBondSchema()
            };

        private static QsWhileStatement ToBondSchema(this SyntaxTree.QsWhileStatement qsWhileStatement) =>
            new QsWhileStatement
            {
                Condition = qsWhileStatement.Condition.ToBondSchema(),
                Body = qsWhileStatement.Body.ToBondSchema()
            };

        private static Range ToBondSchema(this DataTypes.Range range) =>
            new Range
            {
                Start = range.Start.ToBondSchema(),
                End = range.End.ToBondSchema()
            };

        private static ResolvedCharacteristics ToBondSchema(this SyntaxTree.ResolvedCharacteristics resolvedCharacteristics) =>
            new ResolvedCharacteristics
            {
                Expression = resolvedCharacteristics.Expression.ToBondSchemaGeneric(typeTranslator: ToBondSchema)
            };

        private static ResolvedInitializer ToBondSchema(this SyntaxTree.ResolvedInitializer resolvedInitializer) =>
            new ResolvedInitializer
            {
                Initializer = resolvedInitializer.Resolution.ToBondSchemaGeneric(
                    initializerTranslator: ToBondSchema,
                    expressionTranslator: ToBondSchema),
                ResolvedType = resolvedInitializer.Type.ToBondSchema()
            };

        private static ResolvedSignature ToBondSchema(this SyntaxTree.ResolvedSignature resolvedSignature) =>
            new ResolvedSignature
            {
                TypeParameters = resolvedSignature.TypeParameters.Select(tp => tp.ToBondSchema()).ToList(),
                ArgumentType = resolvedSignature.ArgumentType.ToBondSchema(),
                ReturnType = resolvedSignature.ReturnType.ToBondSchema(),
                Information = resolvedSignature.Information.ToBondSchema()
            };

        private static ResolvedType ToBondSchema(this SyntaxTree.ResolvedType resolvedType) =>
            new ResolvedType
            {
                TypeKind = resolvedType.Resolution.ToBondSchema()
            };

        private static SpecializationImplementation ToBondSchema(this SyntaxTree.SpecializationImplementation specializationImplementation)
        {
            SyntaxTokens.QsGeneratorDirective? compilerGenerated = null;
            Tuple<SyntaxTokens.QsTuple<SyntaxTree.LocalVariableDeclaration<SyntaxTree.QsLocalSymbol>>, SyntaxTree.QsScope>? compilerProvided = null;
            SpecializationImplementationKind kind;
            if (specializationImplementation.TryGetGenerated(ref compilerGenerated))
            {
                kind = SpecializationImplementationKind.Generated;
            }
            else if (specializationImplementation.TryGetProvided(ref compilerProvided))
            {
                kind = SpecializationImplementationKind.Provided;
            }
            else
            {
                kind = specializationImplementation.Tag switch
                {
                    SyntaxTree.SpecializationImplementation.Tags.External => SpecializationImplementationKind.External,
                    SyntaxTree.SpecializationImplementation.Tags.Intrinsic => SpecializationImplementationKind.Intrinsic,
                    _ => throw new ArgumentException($"Unsupported SpecializationImplementation {specializationImplementation}")
                };
            }
            
            return new SpecializationImplementation
            {
                Kind = kind,
                Provided = compilerProvided?.ToSpecializationImplementationKindProvided(),
                Generated = compilerGenerated?.ToBondSchema()
            };
        }

        private static string ToBondSchema(this NonNullable<string> s) => s.Value;

        private static SymbolTuple ToBondSchema(this SyntaxTree.SymbolTuple symbolTuple)
        {
            string? bondVariableName = null;
            List<SymbolTuple>? bondVariableNameTuple = null;
            NonNullable<string> compilerVariableName = default;
            ImmutableArray<SyntaxTree.SymbolTuple> compilerVariableNameTuple = default;
            SymbolTupleKind kind;
            if (symbolTuple.TryGetVariableName(ref compilerVariableName))
            {
                kind = SymbolTupleKind.VariableName;
                bondVariableName = compilerVariableName.Value;
            }
            else if (symbolTuple.TryGetVariableNameTuple(ref compilerVariableNameTuple))
            {
                kind = SymbolTupleKind.VariableNameTuple;
                bondVariableNameTuple = compilerVariableNameTuple.Select(v => v.ToBondSchema()).ToList();
            }
            else
            {
                kind = symbolTuple.Tag switch
                {
                    SyntaxTree.SymbolTuple.Tags.InvalidItem => SymbolTupleKind.InvalidItem,
                    SyntaxTree.SymbolTuple.Tags.DiscardedItem => SymbolTupleKind.DiscardedItem,
                    _ => throw new ArgumentException($"Unsupported SymbolTuple {symbolTuple}")
                };
            }

            return new SymbolTuple
            {
                Kind = kind,
                VariableName = bondVariableName,
                VariableNameTuple = bondVariableNameTuple
            };
        }

        private static TypedExpression ToBondSchema(this SyntaxTree.TypedExpression typedExpression) =>
            new TypedExpression
            {
                Expression = typedExpression.Expression.ToBondSchema(),
                TypedArguments = typedExpression.TypeArguments.Select(t => t.ToTypedArgument()).ToList(),
                ResolvedType = typedExpression.ResolvedType.ToBondSchema(),
                Range = typedExpression.Range.IsNull ?
                    null :
                    typedExpression.Range.Item.ToBondSchema()
            };

        private static UserDefinedType ToBondSchema(this SyntaxTree.UserDefinedType userDefinedType) =>
            new UserDefinedType
            {
                Namespace = userDefinedType.Namespace.Value,
                Name = userDefinedType.Name.Value,
                Range = userDefinedType.Range.IsNull ?
                    null :
                    userDefinedType.Range.Item.ToBondSchema()
            };

        private static CharacteristicsKindComposition<TBond> ToBondSchemaGeneric<TBond, TCompiler>(
            this SyntaxTokens.CharacteristicsKind<TCompiler> characteristicsKind,
            Func<TCompiler, TBond> typeTranslator)
            where TBond : class
            where TCompiler : class
        {
            SyntaxTokens.OpProperty? compilerSimpleSet = null;
            Tuple<TCompiler, TCompiler>? compilerSetOperation = null;
            CharacteristicsKind kind;
            if (characteristicsKind.TryGetSimpleSet(ref compilerSimpleSet))
            {
                kind = CharacteristicsKind.SimpleSet;
            }
            else if (characteristicsKind.TryGetIntersection(ref compilerSetOperation))
            {
                kind = CharacteristicsKind.Intersection;
            }
            else if (characteristicsKind.TryGetUnion(ref compilerSetOperation))
            {
                kind = CharacteristicsKind.Union;
            }
            else
            {
                kind = characteristicsKind.Tag switch
                {
                    SyntaxTokens.CharacteristicsKind<TCompiler>.Tags.EmptySet => CharacteristicsKind.EmptySet,
                    SyntaxTokens.CharacteristicsKind<TCompiler>.Tags.InvalidSetExpr => CharacteristicsKind.InvalidSetExpr,
                    _ => throw new ArgumentException($"Unsupported CharacteristicsKind {characteristicsKind}")
                };
            }

            return new CharacteristicsKindComposition<TBond>
            {
                Kind = kind,
                SimpleSet = compilerSimpleSet?.ToBondSchema(),
                SetOperation = compilerSetOperation?.ToCharacteristicsKindSetOperationGeneric(typeTranslator: typeTranslator)
            };
        }

        private static LocalVariableDeclaration<TBond> ToBondSchemaGeneric<TBond, TCompiler>(
            this SyntaxTree.LocalVariableDeclaration<TCompiler> localVariableDeclaration,
            Func<TCompiler, TBond> typeTranslator) =>
            new LocalVariableDeclaration<TBond>
            {
                VariableName = typeTranslator(localVariableDeclaration.VariableName),
                Type = localVariableDeclaration.Type.ToBondSchema(),
                InferredInformation = localVariableDeclaration.InferredInformation.ToBondSchema(),
                Position = localVariableDeclaration.Position.IsNull ?
                    null :
                    localVariableDeclaration.Position.Item.ToBondSchema(),
                Range = localVariableDeclaration.Range.ToBondSchema()
            };

        private static QsBinding<TBond> ToBondSchemaGeneric<TBond, TCompiler>(
            this SyntaxTree.QsBinding<TCompiler> qsBinding,
            Func<TCompiler, TBond> typeTranslator) =>
            new QsBinding<TBond>
            {
                Kind = qsBinding.Kind.ToBondSchema(),
                Lhs = qsBinding.Lhs.ToBondSchema(),
                Rhs = typeTranslator(qsBinding.Rhs)
            };

        private static QsExpressionKindComposition<TBondExpression, TBondSymbol, TBondType> ToBondSchemaGeneric<
            TBondExpression,
            TBondSymbol,
            TBondType,
            TCompilerExpression,
            TCompilerSymbol,
            TCompilerType>(
                this SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType> qsExpressionKind,
                Func<TCompilerExpression, TBondExpression> expressionTranslator,
                Func<TCompilerSymbol, TBondSymbol> symbolTranslator,
                Func<TCompilerType, TBondType> typeTranslator)
            where TBondExpression : class
            where TBondSymbol : class
            where TBondType : class
            where TCompilerExpression : class
            where TCompilerSymbol : class
            where TCompilerType : class
        {
            Int64? bondIntLiteral = null;
            ArraySegment<byte> bondBigIntLiteral = null;
            double? bondDoubleLiteral = null;
            bool? bondBoolLiteral = null;
            List<TBondExpression>? bondExpressionArray = null;
            Tuple<TCompilerSymbol, QsNullable<ImmutableArray<TCompilerType>>>? compilerIdentifier = null;
            Int64 compilerIntLiteral = default;
            BigInteger compilerBigIntLiteral = default;
            double compilerDoubleLiteral = default;
            bool compilerBoolLiteral = default;
            Tuple<NonNullable<string>, ImmutableArray<TCompilerExpression>>? compilerStringLiteral = null;
            SyntaxTokens.QsResult? compilerResultLiteral = null;
            SyntaxTokens.QsPauli? compilerPauliLiteral = null;
            Tuple<TCompilerType, TCompilerExpression>? compilerNewArray = null;
            Tuple<TCompilerExpression, TCompilerSymbol>? compilerNamedItem = null;
            TCompilerExpression? compilerExpression = null;
            Tuple<TCompilerExpression, TCompilerExpression>? compilerExpressionDouble = null;
            Tuple<TCompilerExpression, TCompilerExpression, TCompilerExpression>? compilerExpressionTriple = null;
            ImmutableArray<TCompilerExpression> compilerExpressionArray = default;
            QsExpressionKind kind;
            if (qsExpressionKind.TryGetIdentifier(ref compilerIdentifier))
            {
                kind = QsExpressionKind.Identifier;
            }
            else if (qsExpressionKind.TryGetValueTuple(ref compilerExpressionArray))
            {
                kind = QsExpressionKind.ValueTuple;
                bondExpressionArray = compilerExpressionArray.Select(v => expressionTranslator(v)).ToList();
            }
            else if (qsExpressionKind.TryGetIntLiteral(ref compilerIntLiteral))
            {
                kind = QsExpressionKind.IntLiteral;
                bondIntLiteral = compilerIntLiteral;
            }
            else if (qsExpressionKind.TryGetBigIntLiteral(ref compilerBigIntLiteral))
            {
                kind = QsExpressionKind.BigIntLiteral;
                bondBigIntLiteral = compilerBigIntLiteral.ToByteArray();
            }
            else if (qsExpressionKind.TryGetDoubleLiteral(ref compilerDoubleLiteral))
            {
                kind = QsExpressionKind.DoubleLiteral;
                bondDoubleLiteral = compilerDoubleLiteral;
            }
            else if (qsExpressionKind.TryGetBoolLiteral(ref compilerBoolLiteral))
            {
                kind = QsExpressionKind.BoolLiteral;
                bondBoolLiteral = compilerBoolLiteral;
            }
            else if (qsExpressionKind.TryGetStringLiteral(ref compilerStringLiteral))
            {
                kind = QsExpressionKind.StringLiteral;
            }
            else if (qsExpressionKind.TryGetResultLiteral(ref compilerResultLiteral))
            {
                kind = QsExpressionKind.ResultLiteral;
            }
            else if (qsExpressionKind.TryGetPauliLiteral(ref compilerPauliLiteral))
            {
                kind = QsExpressionKind.PauliLiteral;
            }
            else if (qsExpressionKind.TryGetRangeLiteral(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.RangeLiteral;
            }
            else if (qsExpressionKind.TryGetNewArray(ref compilerNewArray))
            {
                kind = QsExpressionKind.NewArray;
            }
            else if (qsExpressionKind.TryGetValueArray(ref compilerExpressionArray))
            {
                kind = QsExpressionKind.ValueArray;
                bondExpressionArray = compilerExpressionArray.Select(e => expressionTranslator(e)).ToList();
            }
            else if (qsExpressionKind.TryGetArrayItem(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.ArrayItem;
            }
            else if (qsExpressionKind.TryGetNamedItem(ref compilerNamedItem))
            {
                kind = QsExpressionKind.NamedItem;
            }
            else if (qsExpressionKind.TryGetNEG(ref compilerExpression!))
            {
                kind = QsExpressionKind.NEG;
            }
            else if (qsExpressionKind.TryGetNOT(ref compilerExpression))
            {
                kind = QsExpressionKind.NOT;
            }
            else if (qsExpressionKind.TryGetBNOT(ref compilerExpression))
            {
                kind = QsExpressionKind.BNOT;
            }
            else if (qsExpressionKind.TryGetADD(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.ADD;
            }
            else if (qsExpressionKind.TryGetSUB(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.SUB;
            }
            else if (qsExpressionKind.TryGetMUL(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.MUL;
            }
            else if (qsExpressionKind.TryGetDIV(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.DIV;
            }
            else if (qsExpressionKind.TryGetMOD(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.MOD;
            }
            else if (qsExpressionKind.TryGetPOW(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.POW;
            }
            else if (qsExpressionKind.TryGetEQ(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.EQ;
            }
            else if (qsExpressionKind.TryGetNEQ(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.NEQ;
            }
            else if (qsExpressionKind.TryGetLT(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.LT;
            }
            else if (qsExpressionKind.TryGetLTE(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.LTE;
            }
            else if (qsExpressionKind.TryGetGT(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.GT;
            }
            else if (qsExpressionKind.TryGetGTE(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.GTE;
            }
            else if (qsExpressionKind.TryGetAND(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.AND;
            }
            else if (qsExpressionKind.TryGetOR(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.OR;
            }
            else if (qsExpressionKind.TryGetBOR(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.BOR;
            }
            else if (qsExpressionKind.TryGetBAND(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.BAND;
            }
            else if (qsExpressionKind.TryGetBXOR(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.BXOR;
            }
            else if (qsExpressionKind.TryGetLSHIFT(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.LSHIFT;
            }
            else if (qsExpressionKind.TryGetRSHIFT(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.RSHIFT;
            }
            else if (qsExpressionKind.TryGetCONDITIONAL(ref compilerExpressionTriple))
            {
                kind = QsExpressionKind.CONDITIONAL;
            }
            else if (qsExpressionKind.TryGetCopyAndUpdate(ref compilerExpressionTriple))
            {
                kind = QsExpressionKind.CopyAndUpdate;
            }
            else if (qsExpressionKind.TryGetUnwrapApplication(ref compilerExpression))
            {
                kind = QsExpressionKind.UnwrapApplication;
            }
            else if (qsExpressionKind.TryGetAdjointApplication(ref compilerExpression))
            {
                kind = QsExpressionKind.AdjointApplication;
            }
            else if (qsExpressionKind.TryGetControlledApplication(ref compilerExpression))
            {
                kind = QsExpressionKind.ControlledApplication;
            }
            else if (qsExpressionKind.TryGetCallLikeExpression(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.CallLikeExpression;
            }
            else
            {
                kind = qsExpressionKind.Tag switch
                {
                    SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.Tags.InvalidExpr => QsExpressionKind.InvalidExpr,
                    SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.Tags.MissingExpr => QsExpressionKind.MissingExpr,
                    SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.Tags.UnitValue => QsExpressionKind.UnitValue,
                    _ => throw new ArgumentException($"Unsupported QsExpressionKind {qsExpressionKind}")
                };
            }

            return new QsExpressionKindComposition<TBondExpression, TBondSymbol, TBondType>
            {
                Kind = kind,
                Identifier = compilerIdentifier?.ToQsExpressionKindIdentifierGeneric(
                    symbolTranslator: symbolTranslator,
                    typeTranslator: typeTranslator),
                IntLiteral = bondIntLiteral,
                BigIntLiteral = bondBigIntLiteral,
                DoubleLiteral = bondDoubleLiteral,
                StringLiteral = compilerStringLiteral?.ToQsExpressionKindStringLiteralGeneric(
                    typeTranslator: expressionTranslator),
                ResultLiteral = compilerResultLiteral?.ToBondSchema(),
                PauliLiteral = compilerPauliLiteral?.ToBondSchema(),
                NewArray = compilerNewArray?.ToQsExpressionKindNewArrayGeneric(
                    expressionTranslator: expressionTranslator,
                    typeTranslator: typeTranslator),
                NamedItem = compilerNamedItem?.ToQsExpressionKindNamedItemGeneric(
                    expressionTranslator: expressionTranslator,
                    symbolTranslator: symbolTranslator),
                Expression = compilerExpression != null ?
                    expressionTranslator(compilerExpression!) :
                    null!,
                ExpressionDouble = compilerExpressionDouble?.ToQsExpressionKindExpressionDoubleGeneric(
                    typeTranslator: expressionTranslator),
                ExpressionTriple = compilerExpressionTriple?.ToQsExpressionKindExpressionTripleGeneric(
                    typeTranslator: expressionTranslator),
                ExpressionArray = bondExpressionArray
            };
        }

        private static QsInitializerKindComposition<TBondInitializer, TBondExpression> ToBondSchemaGeneric<
            TBondInitializer,
            TBondExpression,
            TCompilerInitializer,
            TCompilerExpression>(
                this SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression> qsInitializerKind,
                Func<TCompilerInitializer, TBondInitializer> initializerTranslator,
                Func<TCompilerExpression, TBondExpression> expressionTranslator)
            where TBondExpression : class
            where TBondInitializer : class
            where TCompilerExpression : class
            where TCompilerInitializer : class
        {
            TBondExpression? bondQubitRegisterAllocation = null;
            List<TBondInitializer>? bondQubitTupleAllocation = null;
            TCompilerExpression? compilerQubitRegisterAllocation = null;
            ImmutableArray<TCompilerInitializer> compilerQubitTupleAllocation = default;
            QsInitializerKind kind;
            if (qsInitializerKind.TryGetQubitRegisterAllocation(ref compilerQubitRegisterAllocation!))
            {
                kind = QsInitializerKind.QubitRegisterAllocation;
                bondQubitRegisterAllocation = expressionTranslator(compilerQubitRegisterAllocation);
            }
            else if (qsInitializerKind.TryGetQubitTupleAllocation(ref compilerQubitTupleAllocation))
            {
                kind = QsInitializerKind.QubitTupleAllocation;
                bondQubitTupleAllocation = compilerQubitTupleAllocation.Select(q => initializerTranslator(q)).ToList();
            }
            else
            {
                kind = qsInitializerKind.Tag switch
                {
                    SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression>.Tags.InvalidInitializer => QsInitializerKind.InvalidInitializer,
                    SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression>.Tags.SingleQubitAllocation => QsInitializerKind.SingleQubitAllocation,
                    _ => throw new ArgumentException($"Unsupported QsInitializerKind {qsInitializerKind}")
                };
            }

            return new QsInitializerKindComposition<TBondInitializer, TBondExpression>
            {
                Kind = kind,
                QubitRegisterAllocation = bondQubitRegisterAllocation!,
                QubitTupleAllocation = bondQubitTupleAllocation
            };
        }

        private static QsTuple<TBond> ToBondSchemaGeneric<TBond, TCompiler>(
            this SyntaxTokens.QsTuple<TCompiler> qsTuple,
            Func<TCompiler, TBond> typeTranslator)
            where TBond : class
            where TCompiler : class
        {
            TBond? bondItem = null;
            List<QsTuple<TBond>>? bondItems = null;
            TCompiler? compilerItem = null;
            ImmutableArray<SyntaxTokens.QsTuple<TCompiler>> compilerItems = default;
            QsTupleKind kind;
            if (qsTuple.TryGetQsTupleItem(ref compilerItem!))
            {
                kind = QsTupleKind.QsTupleItem;
                bondItem = typeTranslator(compilerItem);
            }
            else if (qsTuple.TryGetQsTuple(ref compilerItems))
            {
                kind = QsTupleKind.QsTuple;
                bondItems = compilerItems.Select(i => i.ToBondSchemaGeneric(typeTranslator)).ToList();
            }
            else
            {
                throw new ArgumentException($"Unsupported QsTuple kind {qsTuple}");
            }

            return new QsTuple<TBond>
            {
                Kind = kind,
                Item = bondItem!,
                Items = bondItems
            };
        }

        private static QsTypeKindComposition<TBondType, TBondUdt, TBondParam, TBondCharacteristics> ToBondSchemaGeneric
            <TBondType,
             TBondUdt,
             TBondParam,
             TBondCharacteristics,
             TCompilerType,
             TCompilerUdt,
             TCompilerParam,
             TCompilerCharacteristics>(
                this SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics> qsTypeKind,
                Func<TCompilerType, TBondType> dataTranslator,
                Func<TCompilerUdt, TBondUdt> udtTranslator,
                Func<TCompilerParam, TBondParam> paramTranslator,
                Func<TCompilerCharacteristics, TBondCharacteristics> characteristicsTranslator)
            where TBondType : class
            where TBondUdt : class
            where TBondParam : class
            where TBondCharacteristics : class
            where TCompilerType : class
            where TCompilerUdt : class
            where TCompilerParam : class
            where TCompilerCharacteristics : class
        {
            TBondType? bondArrayType = null;
            List<TBondType>? bondTupleType = null;
            TBondParam? bondTypeParameter = null;
            TBondUdt? bondUserDefinedType = null;
            TCompilerType? compilerArrayType = null;
            Tuple<TCompilerType, TCompilerType>? compilerFunction = null;
            Tuple<Tuple<TCompilerType, TCompilerType>, TCompilerCharacteristics>? compilerOperation = null;
            ImmutableArray<TCompilerType> compilerTupleType = default;
            TCompilerParam? compilerTyperParameter = null;
            TCompilerUdt? compilerUdtType = null;
            QsTypeKind kind;
            if (qsTypeKind.TryGetArrayType(ref compilerArrayType!))
            {
                kind = QsTypeKind.ArrayType;
                bondArrayType = dataTranslator(compilerArrayType);
            }
            else if (qsTypeKind.TryGetFunction(ref compilerFunction))
            {
                kind = QsTypeKind.Function;
            }
            else if (qsTypeKind.TryGetOperation(ref compilerOperation))
            {
                kind = QsTypeKind.Operation;
            }
            else if (qsTypeKind.TryGetTupleType(ref compilerTupleType))
            {
                kind = QsTypeKind.TupleType;
                bondTupleType = compilerTupleType.Select(t => dataTranslator(t)).ToList();
            }
            else if (qsTypeKind.TryGetTypeParameter(ref compilerTyperParameter!))
            {
                kind = QsTypeKind.TypeParameter;
                bondTypeParameter = paramTranslator(compilerTyperParameter);
            }
            else if (qsTypeKind.TryGetUserDefinedType(ref compilerUdtType!))
            {
                kind = QsTypeKind.UserDefinedType;
                bondUserDefinedType = udtTranslator(compilerUdtType);
            }
            else
            {
                kind = qsTypeKind.Tag switch
                {
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.BigInt => QsTypeKind.BigInt,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.Bool => QsTypeKind.Bool,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.Double => QsTypeKind.Double,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.Int => QsTypeKind.Int,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.InvalidType => QsTypeKind.InvalidType,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.MissingType => QsTypeKind.MissingType,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.Pauli => QsTypeKind.Pauli,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.Qubit => QsTypeKind.Qubit,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.Range => QsTypeKind.Range,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.Result => QsTypeKind.Result,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.String => QsTypeKind.String,
                    SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Tags.UnitType => QsTypeKind.UnitType,
                    _ => throw new ArgumentException($"Unsupported QsTypeKind: {qsTypeKind.Tag}")
                };
            }

            return new QsTypeKindComposition<TBondType, TBondUdt, TBondParam, TBondCharacteristics>
            {
                Kind = kind,
                ArrayType = bondArrayType!,
                Function = compilerFunction?.ToQsTypeKindFunctionGeneric(typeTranslator: dataTranslator),
                Operation = compilerOperation?.ToQsTypeKindOperationGeneric(
                    dataTranslator: dataTranslator,
                    characteristicsTranslator: characteristicsTranslator),
                TupleType = bondTupleType,
                TypeParameter = bondTypeParameter!,
                UserDefinedType = bondUserDefinedType!
            };
        }

        private static CharacteristicsKindSetOperation<TBond> ToCharacteristicsKindSetOperationGeneric<TBond, TCompiler>(
            this Tuple<TCompiler, TCompiler> compilerSetOperation,
            Func<TCompiler, TBond> typeTranslator) =>
            new CharacteristicsKindSetOperation<TBond>
            {
                Set1 = typeTranslator(compilerSetOperation.Item1),
                Set2 = typeTranslator(compilerSetOperation.Item2)
            };

        private static QsConditionalBlock ToQsConditionalBlock(this Tuple<SyntaxTree.TypedExpression, SyntaxTree.QsPositionedBlock> qsConditionalBlock) =>
            new QsConditionalBlock
            {
                Expression = qsConditionalBlock.Item1.ToBondSchema(),
                Block = qsConditionalBlock.Item2.ToBondSchema()
            };

        private static QsExpressionKindExpressionDouble<TBond> ToQsExpressionKindExpressionDoubleGeneric<TBond, TCompiler>(
            this Tuple<TCompiler, TCompiler> expressionDouble,
            Func<TCompiler, TBond> typeTranslator) =>
            new QsExpressionKindExpressionDouble<TBond>
            {
                Expression1 = typeTranslator(expressionDouble.Item1),
                Expression2 = typeTranslator(expressionDouble.Item2)
            };

        private static QsExpressionKindExpressionTriple<TBond> ToQsExpressionKindExpressionTripleGeneric<TBond, TCompiler>(
            this Tuple<TCompiler, TCompiler, TCompiler> compilerExpressionTriple,
            Func<TCompiler, TBond> typeTranslator) =>
            new QsExpressionKindExpressionTriple<TBond>
            {
                Expression1 = typeTranslator(compilerExpressionTriple.Item1),
                Expression2 = typeTranslator(compilerExpressionTriple.Item2),
                Expression3 = typeTranslator(compilerExpressionTriple.Item3)
            };

        private static QsExpressionKindIdentifier<TBondSymbol, TBondType> ToQsExpressionKindIdentifierGeneric<
            TBondSymbol,
            TBondType,
            TCompilerSymbol,
            TCompilerType>(
                this Tuple<TCompilerSymbol, QsNullable<ImmutableArray<TCompilerType>>> compilerIdentifier,
                Func<TCompilerSymbol, TBondSymbol> symbolTranslator,
                Func<TCompilerType, TBondType> typeTranslator) =>
            new QsExpressionKindIdentifier<TBondSymbol, TBondType>
            {
                Symbol = symbolTranslator(compilerIdentifier.Item1),
                Types = compilerIdentifier.Item2.IsNull ?
                        null :
                        compilerIdentifier.Item2.Item.Select(t => typeTranslator(t)).ToList()
            };

        private static QsExpressionKindNamedItem<TBondExpression, TBondSymbol> ToQsExpressionKindNamedItemGeneric<
            TBondExpression,
            TBondSymbol,
            TCompilerExpression,
            TCompilerSymbol>(
                this Tuple<TCompilerExpression, TCompilerSymbol> compilerNamedItem,
                Func<TCompilerExpression, TBondExpression> expressionTranslator,
                Func<TCompilerSymbol, TBondSymbol> symbolTranslator) =>
            new QsExpressionKindNamedItem<TBondExpression, TBondSymbol>
            {
                Expression = expressionTranslator(compilerNamedItem.Item1),
                Symbol = symbolTranslator(compilerNamedItem.Item2)
            };

        private static QsExpressionKindNewArray<TBondExpression, TBondType> ToQsExpressionKindNewArrayGeneric<
            TBondExpression,
            TBondType,
            TCompilerExpression,
            TCompilerType>(
                this Tuple<TCompilerType, TCompilerExpression> compilerNewArray,
                Func<TCompilerExpression, TBondExpression> expressionTranslator,
                Func<TCompilerType, TBondType> typeTranslator) =>
            new QsExpressionKindNewArray<TBondExpression, TBondType>
            {
                Type = typeTranslator(compilerNewArray.Item1),
                Expression = expressionTranslator(compilerNewArray.Item2)
            };

        private static QsExpressionKindStringLiteral<TBond> ToQsExpressionKindStringLiteralGeneric<TBond, TCompiler>(
            this Tuple<NonNullable<string>, ImmutableArray<TCompiler>> compilerStringLiteral,
            Func<TCompiler, TBond> typeTranslator) =>
            new QsExpressionKindStringLiteral<TBond>
            {
                StringLiteral = compilerStringLiteral.Item1.ToBondSchema(),
                Expressions = compilerStringLiteral.Item2.Select(e => typeTranslator(e)).ToList()
            };

        private static QsLoopItem ToQsLoopItem(this Tuple<SyntaxTree.SymbolTuple, SyntaxTree.ResolvedType> loopItem) =>
            new QsLoopItem
            {
                SymbolTuple = loopItem.Item1.ToBondSchema(),
                ResolvedType = loopItem.Item2.ToBondSchema()
            };

        private static LinkedList<QsSourceFileDocumentation> ToQsSourceFileDocumentationList(
            this ILookup<NonNullable<string>, ImmutableArray<string>> qsDocumentation)
        {
            var documentationList = new LinkedList<QsSourceFileDocumentation>();
            foreach (var qsSourceFileDocumentation in qsDocumentation)
            {
                foreach (var items in qsSourceFileDocumentation)
                {
                    var qsDocumentationItem = new QsSourceFileDocumentation
                    {
                        FileName = qsSourceFileDocumentation.Key.Value,
                        DocumentationItems = items.ToList()
                    };

                    documentationList.AddLast(qsDocumentationItem);
                }
            }

            return documentationList;
        }

        private static QsTypeKindFunction<TBond> ToQsTypeKindFunctionGeneric<TBond, TCompiler>(
            this Tuple<TCompiler, TCompiler> compilerFunction,
            Func<TCompiler, TBond> typeTranslator) =>
            new QsTypeKindFunction<TBond>
            {
                Type1 = typeTranslator(compilerFunction.Item1),
                Type2 = typeTranslator(compilerFunction.Item2)
            };

        private static QsTypeKindOperation<TBondType, TBondCharacteristics> ToQsTypeKindOperationGeneric<
            TBondType,
            TBondCharacteristics,
            TCompilerData,
            TCompilerCharacteristics>(
                this Tuple<Tuple<TCompilerData, TCompilerData>, TCompilerCharacteristics> compilerOperation,
                Func<TCompilerData, TBondType> dataTranslator,
                Func<TCompilerCharacteristics, TBondCharacteristics> characteristicsTranslator) =>
            new QsTypeKindOperation<TBondType, TBondCharacteristics>
            {
                Type1 = dataTranslator(compilerOperation.Item1.Item1),
                Type2 = dataTranslator(compilerOperation.Item1.Item2),
                Characteristics = characteristicsTranslator(compilerOperation.Item2)
            };

        private static SpecializationImplementationKindProvided ToSpecializationImplementationKindProvided(
            this Tuple<SyntaxTokens.QsTuple<SyntaxTree.LocalVariableDeclaration<SyntaxTree.QsLocalSymbol>>, SyntaxTree.QsScope> provided) =>
            new SpecializationImplementationKindProvided
            {
                Tuple = provided.Item1.ToBondSchema(),
                Implementation = provided.Item2.ToBondSchema()
            };

        private static TypedArgument ToTypedArgument(
            this Tuple<SyntaxTree.QsQualifiedName, NonNullable<string>, SyntaxTree.ResolvedType> typedArgumet) =>
            new TypedArgument
            {
                Callable = typedArgumet.Item1.ToBondSchema(),
                Name = typedArgumet.Item2.Value,
                Resolution = typedArgumet.Item3.ToBondSchema()
            };
    }
}
