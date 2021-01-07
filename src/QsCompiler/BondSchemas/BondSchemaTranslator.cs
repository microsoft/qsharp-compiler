// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    /// <summary>
    /// This class translates compiler objects to Bond schema objects.
    /// </summary>
    internal static class BondSchemaTranslator
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
            IdentifierKind kind;
            if (identifier is SyntaxTree.Identifier.LocalVariable compilerLocalVariable)
            {
                kind = IdentifierKind.LocalVariable;
                bondLocalVariable = compilerLocalVariable.Item;
            }
            else if (identifier is SyntaxTree.Identifier.GlobalCallable compilerGlobalCallable)
            {
                kind = IdentifierKind.GlobalCallable;
                bondGlobalCallable = compilerGlobalCallable.Item.ToBondSchema();
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
                Variables = localDeclarations.Variables.Select(v => v.ToBondSchemaGeneric(typeTranslator: s => s)).ToList()
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
                SourceFile = qsCallable.SourceFile,
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
                SourceFile = qsCustomType.SourceFile,
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
            qsExpressionKind.ToBondSchemaGeneric(
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
                Namespace = qsQualifiedName.Namespace,
                Name = qsQualifiedName.Name
            };

        private static QsLocalSymbol ToBondSchema(this SyntaxTree.QsLocalSymbol qsLocalSymbol)
        {
            string? bondValidName = null;
            QsLocalSymbolKind kind;
            if (qsLocalSymbol is SyntaxTree.QsLocalSymbol.ValidName compilerValidName)
            {
                kind = QsLocalSymbolKind.ValidName;
                bondValidName = compilerValidName.Item;
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
                Name = qsNamespace.Name,
                Elements = qsNamespace.Elements.Select(e => e.ToBondSchema()).ToList(),
                Documentation = qsNamespace.Documentation.ToQsSourceFileDocumentationList()
            };

        private static QsNamespaceElement ToBondSchema(this SyntaxTree.QsNamespaceElement qsNamespaceElement)
        {
            QsCallable? bondQsCallable = null;
            QsCustomType? bondQsCustomType = null;
            QsNamespaceElementKind kind;
            if (qsNamespaceElement is SyntaxTree.QsNamespaceElement.QsCallable compilerQsCallable)
            {
                kind = QsNamespaceElementKind.QsCallable;
                bondQsCallable = compilerQsCallable.Item.ToBondSchema();
            }
            else if (qsNamespaceElement is SyntaxTree.QsNamespaceElement.QsCustomType compilerQsCustomType)
            {
                kind = QsNamespaceElementKind.QsCustomType;
                bondQsCustomType = compilerQsCustomType.Item.ToBondSchema();
            }
            else
            {
                throw new ArgumentException($"Unsupported {typeof(SyntaxTree.QsNamespaceElement)} kind");
            }

            var bondQsNamespaceElement = new QsNamespaceElement
            {
                Kind = kind,
                Callable = bondQsCallable,
                CustomType = bondQsCustomType
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
                SourceFile = qsSpecialization.SourceFile,
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
            TypedExpression? bondTypedExpression = null;
            QsBinding<TypedExpression>? bondVariableDeclaration = null;
            QsValueUpdate? bondValueUpdate = null;
            QsConditionalStatement? bondConditionalStatement = null;
            QsForStatement? bondForStatement = null;
            QsWhileStatement? bondWhileStatement = null;
            QsRepeatStatement? bondRepeatStatement = null;
            QsConjugation? bondConjugation = null;
            QsQubitScope? bondQubitScope = null;
            QsStatementKind kind;
            if (qsStatementKind is SyntaxTree.QsStatementKind.QsExpressionStatement compilerExpressionStatement)
            {
                kind = QsStatementKind.QsExpressionStatement;
                bondTypedExpression = compilerExpressionStatement.Item.ToBondSchema();
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsReturnStatement compilerReturnStatement)
            {
                kind = QsStatementKind.QsReturnStatement;
                bondTypedExpression = compilerReturnStatement.Item.ToBondSchema();
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsFailStatement compilerFailStatement)
            {
                kind = QsStatementKind.QsFailStatement;
                bondTypedExpression = compilerFailStatement.Item.ToBondSchema();
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsVariableDeclaration compilerVariableDeclaration)
            {
                kind = QsStatementKind.QsVariableDeclaration;
                bondVariableDeclaration = compilerVariableDeclaration.Item.ToBondSchemaGeneric(typeTranslator: ToBondSchema);
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsValueUpdate compilerValueUpdate)
            {
                kind = QsStatementKind.QsValueUpdate;
                bondValueUpdate = compilerValueUpdate.Item.ToBondSchema();
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsConditionalStatement compilerConditionalStatement)
            {
                kind = QsStatementKind.QsConditionalStatement;
                bondConditionalStatement = compilerConditionalStatement.Item.ToBondSchema();
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsForStatement compilerForStatement)
            {
                kind = QsStatementKind.QsForStatement;
                bondForStatement = compilerForStatement.Item.ToBondSchema();
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsWhileStatement compilerWhileStatement)
            {
                kind = QsStatementKind.QsWhileStatement;
                bondWhileStatement = compilerWhileStatement.Item.ToBondSchema();
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsRepeatStatement compilerRepeatStatement)
            {
                kind = QsStatementKind.QsRepeatStatement;
                bondRepeatStatement = compilerRepeatStatement.Item.ToBondSchema();
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsConjugation compilerConjugation)
            {
                kind = QsStatementKind.QsConjugation;
                bondConjugation = compilerConjugation.Item.ToBondSchema();
            }
            else if (qsStatementKind is SyntaxTree.QsStatementKind.QsQubitScope compilerQubitScope)
            {
                kind = QsStatementKind.QsQubitScope;
                bondQubitScope = compilerQubitScope.Item.ToBondSchema();
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
                TypedExpression = bondTypedExpression,
                VariableDeclaration = bondVariableDeclaration,
                ValueUpdate = bondValueUpdate,
                ConditionalStatement = bondConditionalStatement,
                ForStatement = bondForStatement,
                WhileStatement = bondWhileStatement,
                RepeatStatement = bondRepeatStatement,
                Conjugation = bondConjugation,
                QubitScope = bondQubitScope
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
            qsTypeKind.ToBondSchemaGeneric(
                dataTranslator: ToBondSchema,
                udtTranslator: ToBondSchema,
                paramTranslator: ToBondSchema,
                characteristicsTranslator: ToBondSchema);

        private static QsTypeItem ToBondSchema(this SyntaxTree.QsTypeItem qsTypeItem)
        {
            ResolvedType? bondAnonymous = null;
            LocalVariableDeclaration<string>? bondNamed = null;
            QsTypeItemKind kind;
            if (qsTypeItem is SyntaxTree.QsTypeItem.Anonymous compilerAnonymous)
            {
                kind = QsTypeItemKind.Anonymous;
                bondAnonymous = compilerAnonymous.Item.ToBondSchema();
            }
            else if (qsTypeItem is SyntaxTree.QsTypeItem.Named compilerNamed)
            {
                kind = QsTypeItemKind.Named;
                bondNamed = compilerNamed.Item.ToBondSchemaGeneric(typeTranslator: s => s);
            }
            else
            {
                throw new ArgumentException($"Unsupported QsTypeItem {qsTypeItem}");
            }

            return new QsTypeItem
            {
                Kind = kind,
                Anonymous = bondAnonymous,
                Named = bondNamed
            };
        }

        private static QsTypeParameter ToBondSchema(this SyntaxTree.QsTypeParameter qsTypeParameter) =>
            new QsTypeParameter
            {
                Origin = qsTypeParameter.Origin.ToBondSchema(),
                TypeName = qsTypeParameter.TypeName,
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
            QsGeneratorDirective? bondGenerated = null;
            SpecializationImplementationKindProvided? bondProvided = null;
            SpecializationImplementationKind kind;
            if (specializationImplementation is SyntaxTree.SpecializationImplementation.Generated compilerGenerated)
            {
                kind = SpecializationImplementationKind.Generated;
                bondGenerated = compilerGenerated.Item.ToBondSchema();
            }
            else if (specializationImplementation is SyntaxTree.SpecializationImplementation.Provided compilerProvided)
            {
                kind = SpecializationImplementationKind.Provided;
                bondProvided = ToSpecializationImplementationKindProvided(compilerProvided.Item1, compilerProvided.Item2);
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
                Provided = bondProvided,
                Generated = bondGenerated
            };
        }

        private static SymbolTuple ToBondSchema(this SyntaxTree.SymbolTuple symbolTuple)
        {
            string? bondVariableName = null;
            List<SymbolTuple>? bondVariableNameTuple = null;
            SymbolTupleKind kind;
            if (symbolTuple is SyntaxTree.SymbolTuple.VariableName compilerVariableName)
            {
                kind = SymbolTupleKind.VariableName;
                bondVariableName = compilerVariableName.Item;
            }
            else if (symbolTuple is SyntaxTree.SymbolTuple.VariableNameTuple compilerVariableNameTuple)
            {
                kind = SymbolTupleKind.VariableNameTuple;
                bondVariableNameTuple = compilerVariableNameTuple.Item.Select(v => v.ToBondSchema()).ToList();
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
                InferredInformation = typedExpression.InferredInformation.ToBondSchema(),
                Range = typedExpression.Range.IsNull ?
                    null :
                    typedExpression.Range.Item.ToBondSchema()
            };

        private static UserDefinedType ToBondSchema(this SyntaxTree.UserDefinedType userDefinedType) =>
            new UserDefinedType
            {
                Namespace = userDefinedType.Namespace,
                Name = userDefinedType.Name,
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
            OpProperty? bondSimpleSet = null;
            CharacteristicsKindSetOperation<TBond>? bondSetOperation = null;
            CharacteristicsKind kind;
            if (characteristicsKind is SyntaxTokens.CharacteristicsKind<TCompiler>.SimpleSet compilerSimpleSet)
            {
                kind = CharacteristicsKind.SimpleSet;
                bondSimpleSet = compilerSimpleSet.Item.ToBondSchema();
            }
            else if (characteristicsKind is SyntaxTokens.CharacteristicsKind<TCompiler>.Intersection compilerIntersection)
            {
                kind = CharacteristicsKind.Intersection;
                bondSetOperation = ToCharacteristicsKindSetOperationGeneric(
                    set1: compilerIntersection.Item1,
                    set2: compilerIntersection.Item2,
                    typeTranslator: typeTranslator);
            }
            else if (characteristicsKind is SyntaxTokens.CharacteristicsKind<TCompiler>.Union compilerUnion)
            {
                kind = CharacteristicsKind.Union;
                bondSetOperation = ToCharacteristicsKindSetOperationGeneric(
                    set1: compilerUnion.Item1,
                    set2: compilerUnion.Item2,
                    typeTranslator: typeTranslator);
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
                SimpleSet = bondSimpleSet,
                SetOperation = bondSetOperation
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
            long? bondIntLiteral = null;
            ArraySegment<byte> bondBigIntLiteral = null;
            double? bondDoubleLiteral = null;
            bool? bondBoolLiteral = null;
            QsExpressionKindStringLiteral<TBondExpression>? bondStringLiteral = null;
            QsResult? bondResultLiteral = null;
            QsPauli? bondPauliLiteral = null;
            QsExpressionKindNamedItem<TBondExpression, TBondSymbol>? bondNamedItem = null;
            QsExpressionKindNewArray<TBondExpression, TBondType>? bondNewArray = null;
            TBondExpression? bondExpression = null;
            QsExpressionKindExpressionDouble<TBondExpression>? bondExpressionDouble = null;
            QsExpressionKindExpressionTriple<TBondExpression>? bondExpressionTriple = null;
            List<TBondExpression>? bondExpressionArray = null;
            QsExpressionKindIdentifier<TBondSymbol, TBondType>? bondIdentifier = null;
            QsExpressionKind kind;
            if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.Identifier compilerIdentifier)
            {
                kind = QsExpressionKind.Identifier;
                bondIdentifier = ToQsExpressionKindIdentifierGeneric(
                    symbol: compilerIdentifier.Item1,
                    types: compilerIdentifier.Item2,
                    symbolTranslator: symbolTranslator,
                    typeTranslator: typeTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.ValueTuple compilerValueTuple)
            {
                kind = QsExpressionKind.ValueTuple;
                bondExpressionArray = compilerValueTuple.Item.Select(v => expressionTranslator(v)).ToList();
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.IntLiteral compilerIntLiteral)
            {
                kind = QsExpressionKind.IntLiteral;
                bondIntLiteral = compilerIntLiteral.Item;
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.BigIntLiteral compilerBigIntLiteral)
            {
                kind = QsExpressionKind.BigIntLiteral;
                bondBigIntLiteral = compilerBigIntLiteral.Item.ToByteArray();
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.DoubleLiteral compilerDoubleLiteral)
            {
                kind = QsExpressionKind.DoubleLiteral;
                bondDoubleLiteral = compilerDoubleLiteral.Item;
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.BoolLiteral compilerBoolLiteral)
            {
                kind = QsExpressionKind.BoolLiteral;
                bondBoolLiteral = compilerBoolLiteral.Item;
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.StringLiteral compilerStringLiteral)
            {
                kind = QsExpressionKind.StringLiteral;
                bondStringLiteral = ToQsExpressionKindStringLiteralGeneric(
                    stringLiteral: compilerStringLiteral.Item1,
                    expressions: compilerStringLiteral.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.ResultLiteral compilerResultLiteral)
            {
                kind = QsExpressionKind.ResultLiteral;
                bondResultLiteral = compilerResultLiteral.Item.ToBondSchema();
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.PauliLiteral compilerPauliLiteral)
            {
                kind = QsExpressionKind.PauliLiteral;
                bondPauliLiteral = compilerPauliLiteral.Item.ToBondSchema();
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.RangeLiteral compilerRangeLiteral)
            {
                kind = QsExpressionKind.RangeLiteral;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerRangeLiteral.Item1,
                    expression2: compilerRangeLiteral.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.NewArray compilerNewArray)
            {
                kind = QsExpressionKind.NewArray;
                bondNewArray = ToQsExpressionKindNewArrayGeneric(
                    type: compilerNewArray.Item1,
                    expression: compilerNewArray.Item2,
                    expressionTranslator: expressionTranslator,
                    typeTranslator: typeTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.ValueArray compilerValueArray)
            {
                kind = QsExpressionKind.ValueArray;
                bondExpressionArray = compilerValueArray.Item.Select(e => expressionTranslator(e)).ToList();
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.ArrayItem compilerArrayItem)
            {
                kind = QsExpressionKind.ArrayItem;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerArrayItem.Item1,
                    expression2: compilerArrayItem.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.NamedItem compilerNamedItem)
            {
                kind = QsExpressionKind.NamedItem;
                bondNamedItem = ToQsExpressionKindNamedItemGeneric(
                    expression: compilerNamedItem.Item1,
                    symbol: compilerNamedItem.Item2,
                    expressionTranslator: expressionTranslator,
                    symbolTranslator: symbolTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.NEG compilerNEG)
            {
                kind = QsExpressionKind.NEG;
                bondExpression = expressionTranslator(compilerNEG.Item);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.NOT compilerNOT)
            {
                kind = QsExpressionKind.NOT;
                bondExpression = expressionTranslator(compilerNOT.Item);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.BNOT compilerBNOT)
            {
                kind = QsExpressionKind.BNOT;
                bondExpression = expressionTranslator(compilerBNOT.Item);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.ADD compilerADD)
            {
                kind = QsExpressionKind.ADD;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerADD.Item1,
                    expression2: compilerADD.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.SUB compilerSUB)
            {
                kind = QsExpressionKind.SUB;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerSUB.Item1,
                    expression2: compilerSUB.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.MUL compilerMUL)
            {
                kind = QsExpressionKind.MUL;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerMUL.Item1,
                    expression2: compilerMUL.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.DIV compilerDIV)
            {
                kind = QsExpressionKind.DIV;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerDIV.Item1,
                    expression2: compilerDIV.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.MOD compilerMOD)
            {
                kind = QsExpressionKind.MOD;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerMOD.Item1,
                    expression2: compilerMOD.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.POW compilerPOW)
            {
                kind = QsExpressionKind.POW;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerPOW.Item1,
                    expression2: compilerPOW.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.EQ compilerEQ)
            {
                kind = QsExpressionKind.EQ;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerEQ.Item1,
                    expression2: compilerEQ.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.NEQ compilerNEQ)
            {
                kind = QsExpressionKind.NEQ;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerNEQ.Item1,
                    expression2: compilerNEQ.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.LT compilerLT)
            {
                kind = QsExpressionKind.LT;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerLT.Item1,
                    expression2: compilerLT.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.LTE compilerLTE)
            {
                kind = QsExpressionKind.LTE;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerLTE.Item1,
                    expression2: compilerLTE.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.GT compilerGT)
            {
                kind = QsExpressionKind.GT;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerGT.Item1,
                    expression2: compilerGT.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.GTE compilerGTE)
            {
                kind = QsExpressionKind.GTE;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerGTE.Item1,
                    expression2: compilerGTE.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.AND compilerAND)
            {
                kind = QsExpressionKind.AND;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerAND.Item1,
                    expression2: compilerAND.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.OR compilerOR)
            {
                kind = QsExpressionKind.OR;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerOR.Item1,
                    expression2: compilerOR.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.BOR compilerBOR)
            {
                kind = QsExpressionKind.BOR;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerBOR.Item1,
                    expression2: compilerBOR.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.BAND compilerBAND)
            {
                kind = QsExpressionKind.BAND;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerBAND.Item1,
                    expression2: compilerBAND.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.BXOR compilerBXOR)
            {
                kind = QsExpressionKind.BXOR;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerBXOR.Item1,
                    expression2: compilerBXOR.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.LSHIFT compilerLSHIFT)
            {
                kind = QsExpressionKind.LSHIFT;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerLSHIFT.Item1,
                    expression2: compilerLSHIFT.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.RSHIFT compilerRSHIFT)
            {
                kind = QsExpressionKind.RSHIFT;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerRSHIFT.Item1,
                    expression2: compilerRSHIFT.Item2,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.CONDITIONAL compilerConditional)
            {
                kind = QsExpressionKind.CONDITIONAL;
                bondExpressionTriple = ToQsExpressionKindExpressionTripleGeneric(
                    expression1: compilerConditional.Item1,
                    expression2: compilerConditional.Item2,
                    expression3: compilerConditional.Item3,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.CopyAndUpdate compilerCopyAndUpdate)
            {
                kind = QsExpressionKind.CopyAndUpdate;
                bondExpressionTriple = ToQsExpressionKindExpressionTripleGeneric(
                    expression1: compilerCopyAndUpdate.Item1,
                    expression2: compilerCopyAndUpdate.Item2,
                    expression3: compilerCopyAndUpdate.Item3,
                    typeTranslator: expressionTranslator);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.UnwrapApplication compilerUnwrapApplication)
            {
                kind = QsExpressionKind.UnwrapApplication;
                bondExpression = expressionTranslator(compilerUnwrapApplication.Item);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.AdjointApplication compilerAdjointApplication)
            {
                kind = QsExpressionKind.AdjointApplication;
                bondExpression = expressionTranslator(compilerAdjointApplication.Item);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.ControlledApplication compilerControlledApplication)
            {
                kind = QsExpressionKind.ControlledApplication;
                bondExpression = expressionTranslator(compilerControlledApplication.Item);
            }
            else if (qsExpressionKind is SyntaxTokens.QsExpressionKind<TCompilerExpression, TCompilerSymbol, TCompilerType>.CallLikeExpression compilerCallLikeExpression)
            {
                kind = QsExpressionKind.CallLikeExpression;
                bondExpressionDouble = ToQsExpressionKindExpressionDoubleGeneric(
                    expression1: compilerCallLikeExpression.Item1,
                    expression2: compilerCallLikeExpression.Item2,
                    typeTranslator: expressionTranslator);
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
                Identifier = bondIdentifier,
                IntLiteral = bondIntLiteral,
                BigIntLiteral = bondBigIntLiteral,
                DoubleLiteral = bondDoubleLiteral,
                BoolLiteral = bondBoolLiteral,
                StringLiteral = bondStringLiteral,
                ResultLiteral = bondResultLiteral,
                PauliLiteral = bondPauliLiteral,
                NewArray = bondNewArray,
                NamedItem = bondNamedItem,
                Expression = bondExpression!,
                ExpressionDouble = bondExpressionDouble,
                ExpressionTriple = bondExpressionTriple,
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
            QsInitializerKind kind;
            if (qsInitializerKind is SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression>.QubitRegisterAllocation compilerQubitRegisterAllocation)
            {
                kind = QsInitializerKind.QubitRegisterAllocation;
                bondQubitRegisterAllocation = expressionTranslator(compilerQubitRegisterAllocation.Item);
            }
            else if (qsInitializerKind is SyntaxTokens.QsInitializerKind<TCompilerInitializer, TCompilerExpression>.QubitTupleAllocation compilerQubitTupleAllocation)
            {
                kind = QsInitializerKind.QubitTupleAllocation;
                bondQubitTupleAllocation = compilerQubitTupleAllocation.Item.Select(q => initializerTranslator(q)).ToList();
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
            QsTupleKind kind;
            if (qsTuple is SyntaxTokens.QsTuple<TCompiler>.QsTupleItem compilerTupleItem)
            {
                kind = QsTupleKind.QsTupleItem;
                bondItem = typeTranslator(compilerTupleItem.Item);
            }
            else if (qsTuple is SyntaxTokens.QsTuple<TCompiler>.QsTuple compilerTuple)
            {
                kind = QsTupleKind.QsTuple;
                bondItems = compilerTuple.Item.Select(i => i.ToBondSchemaGeneric(typeTranslator)).ToList();
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
            QsTypeKindFunction<TBondType>? bondFunction = null;
            QsTypeKindOperation<TBondType, TBondCharacteristics>? bondOperation = null;
            List<TBondType>? bondTupleType = null;
            TBondParam? bondTypeParameter = null;
            TBondUdt? bondUserDefinedType = null;
            QsTypeKind kind;
            if (qsTypeKind is SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.ArrayType compilerArrayType)
            {
                kind = QsTypeKind.ArrayType;
                bondArrayType = dataTranslator(compilerArrayType.Item);
            }
            else if (qsTypeKind is SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Function compilerFunction)
            {
                kind = QsTypeKind.Function;
                bondFunction = ToQsTypeKindFunctionGeneric(
                    type1: compilerFunction.Item1,
                    type2: compilerFunction.Item2,
                    typeTranslator: dataTranslator);
            }
            else if (qsTypeKind is SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.Operation compilerOperation)
            {
                kind = QsTypeKind.Operation;
                bondOperation = ToQsTypeKindOperationGeneric(
                    dataTuple: compilerOperation.Item1,
                    charateristics: compilerOperation.Item2,
                    dataTranslator: dataTranslator,
                    characteristicsTranslator: characteristicsTranslator);
            }
            else if (qsTypeKind is SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.TupleType compilerTupleType)
            {
                kind = QsTypeKind.TupleType;
                bondTupleType = compilerTupleType.Item.Select(t => dataTranslator(t)).ToList();
            }
            else if (qsTypeKind is SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.TypeParameter compilerTypeParameter)
            {
                kind = QsTypeKind.TypeParameter;
                bondTypeParameter = paramTranslator(compilerTypeParameter.Item);
            }
            else if (qsTypeKind is SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.UserDefinedType compilerUdt)
            {
                kind = QsTypeKind.UserDefinedType;
                bondUserDefinedType = udtTranslator(compilerUdt.Item);
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
                Function = bondFunction,
                Operation = bondOperation,
                TupleType = bondTupleType,
                TypeParameter = bondTypeParameter!,
                UserDefinedType = bondUserDefinedType!
            };
        }

        private static CharacteristicsKindSetOperation<TBond> ToCharacteristicsKindSetOperationGeneric<TBond, TCompiler>(
            TCompiler set1,
            TCompiler set2,
            Func<TCompiler, TBond> typeTranslator) =>
            new CharacteristicsKindSetOperation<TBond>
            {
                Set1 = typeTranslator(set1),
                Set2 = typeTranslator(set2)
            };

        private static QsConditionalBlock ToQsConditionalBlock(this Tuple<SyntaxTree.TypedExpression, SyntaxTree.QsPositionedBlock> qsConditionalBlock) =>
            new QsConditionalBlock
            {
                Expression = qsConditionalBlock.Item1.ToBondSchema(),
                Block = qsConditionalBlock.Item2.ToBondSchema()
            };

        private static QsExpressionKindExpressionDouble<TBond> ToQsExpressionKindExpressionDoubleGeneric<TBond, TCompiler>(
            TCompiler expression1,
            TCompiler expression2,
            Func<TCompiler, TBond> typeTranslator) =>
            new QsExpressionKindExpressionDouble<TBond>
            {
                Expression1 = typeTranslator(expression1),
                Expression2 = typeTranslator(expression2)
            };

        private static QsExpressionKindExpressionTriple<TBond> ToQsExpressionKindExpressionTripleGeneric<TBond, TCompiler>(
            TCompiler expression1,
            TCompiler expression2,
            TCompiler expression3,
            Func<TCompiler, TBond> typeTranslator) =>
            new QsExpressionKindExpressionTriple<TBond>
            {
                Expression1 = typeTranslator(expression1),
                Expression2 = typeTranslator(expression2),
                Expression3 = typeTranslator(expression3)
            };

        private static QsExpressionKindIdentifier<TBondSymbol, TBondType> ToQsExpressionKindIdentifierGeneric<
            TBondSymbol,
            TBondType,
            TCompilerSymbol,
            TCompilerType>(
                TCompilerSymbol symbol,
                QsNullable<ImmutableArray<TCompilerType>> types,
                Func<TCompilerSymbol, TBondSymbol> symbolTranslator,
                Func<TCompilerType, TBondType> typeTranslator) =>
            new QsExpressionKindIdentifier<TBondSymbol, TBondType>
            {
                Symbol = symbolTranslator(symbol),
                Types = types.IsNull ? null : types.Item.Select(t => typeTranslator(t)).ToList()
            };

        private static QsExpressionKindNamedItem<TBondExpression, TBondSymbol> ToQsExpressionKindNamedItemGeneric<
            TBondExpression,
            TBondSymbol,
            TCompilerExpression,
            TCompilerSymbol>(
                TCompilerExpression expression,
                TCompilerSymbol symbol,
                Func<TCompilerExpression, TBondExpression> expressionTranslator,
                Func<TCompilerSymbol, TBondSymbol> symbolTranslator) =>
            new QsExpressionKindNamedItem<TBondExpression, TBondSymbol>
            {
                Expression = expressionTranslator(expression),
                Symbol = symbolTranslator(symbol)
            };

        private static QsExpressionKindNewArray<TBondExpression, TBondType> ToQsExpressionKindNewArrayGeneric<
            TBondExpression,
            TBondType,
            TCompilerExpression,
            TCompilerType>(
                TCompilerType type,
                TCompilerExpression expression,
                Func<TCompilerExpression, TBondExpression> expressionTranslator,
                Func<TCompilerType, TBondType> typeTranslator) =>
            new QsExpressionKindNewArray<TBondExpression, TBondType>
            {
                Type = typeTranslator(type),
                Expression = expressionTranslator(expression)
            };

        private static QsExpressionKindStringLiteral<TBond> ToQsExpressionKindStringLiteralGeneric<TBond, TCompiler>(
            string stringLiteral,
            ImmutableArray<TCompiler> expressions,
            Func<TCompiler, TBond> typeTranslator) =>
            new QsExpressionKindStringLiteral<TBond>
            {
                StringLiteral = stringLiteral,
                Expressions = expressions.Select(e => typeTranslator(e)).ToList()
            };

        private static QsLoopItem ToQsLoopItem(this Tuple<SyntaxTree.SymbolTuple, SyntaxTree.ResolvedType> loopItem) =>
            new QsLoopItem
            {
                SymbolTuple = loopItem.Item1.ToBondSchema(),
                ResolvedType = loopItem.Item2.ToBondSchema()
            };

        private static LinkedList<QsSourceFileDocumentation> ToQsSourceFileDocumentationList(
            this ILookup<string, ImmutableArray<string>> qsDocumentation)
        {
            var documentationList = new LinkedList<QsSourceFileDocumentation>();
            foreach (var qsSourceFileDocumentation in qsDocumentation)
            {
                foreach (var items in qsSourceFileDocumentation)
                {
                    var qsDocumentationItem = new QsSourceFileDocumentation
                    {
                        FileName = qsSourceFileDocumentation.Key,
                        DocumentationItems = items.ToList()
                    };

                    documentationList.AddLast(qsDocumentationItem);
                }
            }

            return documentationList;
        }

        private static QsTypeKindFunction<TBond> ToQsTypeKindFunctionGeneric<TBond, TCompiler>(
            TCompiler type1,
            TCompiler type2,
            Func<TCompiler, TBond> typeTranslator) =>
            new QsTypeKindFunction<TBond>
            {
                Type1 = typeTranslator(type1),
                Type2 = typeTranslator(type2)
            };

        private static QsTypeKindOperation<TBondType, TBondCharacteristics> ToQsTypeKindOperationGeneric<
            TBondType,
            TBondCharacteristics,
            TCompilerData,
            TCompilerCharacteristics>(
                Tuple<TCompilerData, TCompilerData> dataTuple,
                TCompilerCharacteristics charateristics,
                Func<TCompilerData, TBondType> dataTranslator,
                Func<TCompilerCharacteristics, TBondCharacteristics> characteristicsTranslator) =>
            new QsTypeKindOperation<TBondType, TBondCharacteristics>
            {
                Type1 = dataTranslator(dataTuple.Item1),
                Type2 = dataTranslator(dataTuple.Item2),
                Characteristics = characteristicsTranslator(charateristics)
            };

        private static SpecializationImplementationKindProvided ToSpecializationImplementationKindProvided(
            SyntaxTokens.QsTuple<SyntaxTree.LocalVariableDeclaration<SyntaxTree.QsLocalSymbol>> tuple,
            SyntaxTree.QsScope implementation) =>
            new SpecializationImplementationKindProvided
            {
                Tuple = tuple.ToBondSchema(),
                Implementation = implementation.ToBondSchema()
            };

        private static TypedArgument ToTypedArgument(
            this Tuple<SyntaxTree.QsQualifiedName, string, SyntaxTree.ResolvedType> typedArgumet) =>
            new TypedArgument
            {
                Callable = typedArgumet.Item1.ToBondSchema(),
                Name = typedArgumet.Item2,
                Resolution = typedArgumet.Item3.ToBondSchema()
            };
    }
}
