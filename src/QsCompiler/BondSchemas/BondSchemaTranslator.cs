// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Microsoft.Quantum.QsCompiler.DataTypes;

using QsDocumentation = System.Linq.ILookup<Microsoft.Quantum.QsCompiler.DataTypes.NonNullable<string>, System.Collections.Immutable.ImmutableArray<string>>;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    public static class BondSchemaTranslator
    {
        public static QsCompilation CreateBondCompilation(SyntaxTree.QsCompilation qsCompilation) =>
            new QsCompilation
            {
                Namespaces = qsCompilation.Namespaces.Select(n => n.ToBondSchema()).ToList(),
                EntryPoints = qsCompilation.EntryPoints.Select(e => e.ToBondSchema()).ToList()
            };

        private static AccessModifier ToBondSchema(this SyntaxTokens.AccessModifier accessModifier)
        {
            if (accessModifier.IsDefaultAccess)
            {
                return AccessModifier.DefaultAccess;
            }
            else if (accessModifier.IsInternal)
            {
                return AccessModifier.Internal;
            }
            else
            {
                throw new ArgumentException($"Unsupported access modifier: {accessModifier}");
            }
        }

        private static CallableInformation ToBondSchema(this SyntaxTree.CallableInformation callableInformation) =>
            new CallableInformation
            {
                Characteristics = callableInformation.Characteristics.ToBondSchema(),
                InferredInformation = callableInformation.InferredInformation.ToBondSchema()
            };

        private static Identifier ToBondSchema(this SyntaxTree.Identifier identifier)
        {
            string? bondLocalVariable = null;
            QsQualifiedName bondGlobalCallable = null;
            NonNullable<string> compilerLocalVariable = default;
            SyntaxTree.QsQualifiedName compilerGlobalCallable = default;
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
                Location = qsCallable.Location.IsNull ? null : qsCallable.Location.Item.ToBondSchema(),
                Signature = qsCallable.Signature.ToBondSchema(),
                ArgumentTuple = qsCallable.ArgumentTuple.ToBondSchema(),
                Specializations = qsCallable.Specializations.Select(s => s.ToBondSchema()).ToList(),
                Documentation = qsCallable.Documentation.ToList(),
                Comments = qsCallable.Comments.ToBondSchema()
            };

        private static QsCallableKind ToBondSchema(this SyntaxTree.QsCallableKind qsCallableKind)
        {
            if (qsCallableKind.IsOperation)
            {
                return QsCallableKind.Operation;
            }
            else if (qsCallableKind.IsFunction)
            {
                return QsCallableKind.Function;
            }
            else if (qsCallableKind.IsTypeConstructor)
            {
                return QsCallableKind.TypeConstructor;
            }

            throw new ArgumentException($"Unsupported QsCallableKind {qsCallableKind}");
        }

        private static QsComments ToBondSchema(this SyntaxTree.QsComments qsComments) =>
            new QsComments
            {
                OpeningComments = qsComments.OpeningComments.ToList(),
                ClosingComments = qsComments.ClosingComments.ToList()
            };

        private static QsConditionalBlock ToBondSchema(this Tuple<SyntaxTree.TypedExpression, SyntaxTree.QsPositionedBlock> qsConditionalBlock) =>
            new QsConditionalBlock
            {
                Expression = qsConditionalBlock.Item1.ToBondSchema(),
                Block = qsConditionalBlock.Item2.ToBondSchema()
            };

        private static QsConditionalStatement ToBondSchema(this SyntaxTree.QsConditionalStatement qsConditionalStatement) =>
            new QsConditionalStatement
            {
                ConditionalBlocks = qsConditionalStatement.ConditionalBlocks.Select(c => c.ToBondSchema()).ToList(),
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
                TypeId = qsDeclarationAttribute.TypeId.IsNull ? null : qsDeclarationAttribute.TypeId.Item.ToBondSchema(),
                Argument = qsDeclarationAttribute.Argument.ToBondSchema(),
                Offset = qsDeclarationAttribute.Offset.ToBondSchema(),
                Comments = qsDeclarationAttribute.Comments.ToBondSchema()
            };

        private static QsExpressionKindDetail<TypedExpression, Identifier, ResolvedType> ToBondSchema(
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
                LoopItem = qsForStatement.LoopItem.ToBondSchema(),
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
            var validName = NonNullable<string>.New(string.Empty);
            if (qsLocalSymbol.TryGetValidName(ref validName))
            {
                return new QsLocalSymbol
                {
                    Kind = QsLocalSymbolKind.ValidName,
                    Name = validName.Value
                };
            }
            else if (qsLocalSymbol.IsInvalidName)
            {
                return new QsLocalSymbol
                {
                    Kind = QsLocalSymbolKind.InvalidName
                };
            }
            else
            {
                throw new ArgumentException($"Unsupported QsLocalSymbol {qsLocalSymbol}");
            }
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

        private static QsLoopItem ToBondSchema(this Tuple<SyntaxTree.SymbolTuple, SyntaxTree.ResolvedType> loopItem) =>
            new QsLoopItem
            {
                SymbolTuple = loopItem.Item1.ToBondSchema(),
                ResolvedType = loopItem.Item2.ToBondSchema()
            };

        private static QsNamespace ToBondSchema(this SyntaxTree.QsNamespace qsNamespace) =>
            new QsNamespace
            {
                Name = qsNamespace.Name.Value,
                Elements = qsNamespace.Elements.Select(e => e.ToBondSchema()).ToList(),
                Documentation = qsNamespace.Documentation.ToBondSchema()
            };

        private static QsNamespaceElement ToBondSchema(this SyntaxTree.QsNamespaceElement qsNamespaceElement)
        {
            QsNamespaceElementKind kind;
            SyntaxTree.QsCallable qsCallable = null;
            SyntaxTree.QsCustomType qsCustomType = null;
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
                Statements = qsScope.Statements.Select(s => s.ToBondSchema()).ToList()
                // TODO: Implement LocalDeclarations.
            };

        private static LinkedList<QsSourceFileDocumentation> ToBondSchema(this QsDocumentation qsDocumentation)
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
                Location = qsStatement.Location.IsNull ? null : qsStatement.Location.Item.ToBondSchema(),
                Comments = qsStatement.Comments.ToBondSchema()
            };

        private static QsStatementKindDetail ToBondSchema(this SyntaxTree.QsStatementKind qsStatementKind)
        {
            TypedExpression bondTypedExpression = null;
            QsBinding<TypedExpression> bondVariableDeclaration = null;
            QsValueUpdate bondValueUpdate = null;
            QsConditionalStatement bondConditionalStatement = null;
            QsForStatement bondForStatement = null;
            QsWhileStatement bondWhileStatement = null;
            QsRepeatStatement bondRepeatStatement = null;
            QsConjugation bondConjugation = null;
            QsQubitScope bondQubitScope = null;
            SyntaxTree.TypedExpression compilerTypedExpression = default;
            SyntaxTree.QsBinding<SyntaxTree.TypedExpression> compilerVariableDeclaration = default;
            SyntaxTree.QsValueUpdate compilerValueUpdate = default;
            SyntaxTree.QsConditionalStatement compilerConditionalStatement = default;
            SyntaxTree.QsForStatement compilerForStatement = default;
            SyntaxTree.QsWhileStatement compilerWhileStatement = default;
            SyntaxTree.QsRepeatStatement compilerRepeatStatement = default;
            SyntaxTree.QsConjugation compilerConjugation = default;
            SyntaxTree.QsQubitScope compilerQubitScope = default;
            QsStatementKind kind;
            if (qsStatementKind.TryGetExpressionStatement(ref compilerTypedExpression))
            {
                kind = QsStatementKind.QsExpressionStatement;
                bondTypedExpression = compilerTypedExpression.ToBondSchema();
            }
            else if (qsStatementKind.TryGetReturnStatement(ref compilerTypedExpression))
            {
                kind = QsStatementKind.QsReturnStatement;
                bondTypedExpression = compilerTypedExpression.ToBondSchema();
            }
            else if (qsStatementKind.TryGetFailStatement(ref compilerTypedExpression))
            {
                kind = QsStatementKind.QsFailStatement;
                bondTypedExpression = compilerTypedExpression.ToBondSchema();
            }
            else if (qsStatementKind.TryGetVariableDeclaration(ref compilerVariableDeclaration))
            {
                kind = QsStatementKind.QsVariableDeclaration;
                bondVariableDeclaration = compilerVariableDeclaration.ToBondSchemaGeneric(typeTranslator: ToBondSchema);
            }
            else if (qsStatementKind.TryGetValueUpdate(ref compilerValueUpdate))
            {
                kind = QsStatementKind.QsValueUpdate;
                bondValueUpdate = compilerValueUpdate.ToBondSchema();
            }
            else if (qsStatementKind.TryGetConditionalStatement(ref compilerConditionalStatement))
            {
                kind = QsStatementKind.QsConditionalStatement;
                bondConditionalStatement = compilerConditionalStatement.ToBondSchema();
            }
            else if (qsStatementKind.TryGetForStatement(ref compilerForStatement))
            {
                kind = QsStatementKind.QsForStatement;
                bondForStatement = compilerForStatement.ToBondSchema();
            }
            else if (qsStatementKind.TryGetWhileStatement(ref compilerWhileStatement))
            {
                kind = QsStatementKind.QsWhileStatement;
                bondWhileStatement = compilerWhileStatement.ToBondSchema();
            }
            else if (qsStatementKind.TryGetRepeatStatement(ref compilerRepeatStatement))
            {
                kind = QsStatementKind.QsRepeatStatement;
                bondRepeatStatement = compilerRepeatStatement.ToBondSchema();
            }
            else if (qsStatementKind.TryGetConjugation(ref compilerConjugation))
            {
                kind = QsStatementKind.QsConjugation;
                bondConjugation = compilerConjugation.ToBondSchema();
            }
            else if (qsStatementKind.TryGetQubitScope(ref compilerQubitScope))
            {
                kind = QsStatementKind.QsQubitScope;
                bondQubitScope = compilerQubitScope.ToBondSchema();
            }
            else if (qsStatementKind.IsEmptyStatement)
            {
                kind = QsStatementKind.EmptyStatement;
            }
            else
            {
                throw new ArgumentException($"Unsupported QsStatementKind {qsStatementKind}");
            }

            return new QsStatementKindDetail
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

        private static QsTypeKindDetails<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> ToBondSchema(
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
            dataTypeTranslator: ToBondSchema,
            udtTypeTranslator: ToBondSchema,
            tParamTypeTranslator: ToBondSchema,
            characteristicsTypeTranslator: ToBondSchema);

        private static QsTypeItem ToBondSchema(this SyntaxTree.QsTypeItem qsTypeItem)
        {
            ResolvedType bondAnonymous = null;
            LocalVariableDeclaration<string> bondNamed = null;
            SyntaxTree.ResolvedType compilerAnonymous = null;
            SyntaxTree.LocalVariableDeclaration<NonNullable<string>> compilerNamed = null;
            QsTypeItemKind kind;
            if (qsTypeItem.TryGetAnonymous(ref compilerAnonymous))
            {
                kind = QsTypeItemKind.Anonymous;
                bondAnonymous = compilerAnonymous.ToBondSchema();
            }
            else if (qsTypeItem.TryGetNamed(ref compilerNamed))
            {
                kind = QsTypeItemKind.Named;
                bondNamed = compilerNamed.ToBondSchemaGeneric(typeTranslator: ToBondSchema);
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
                TypeName = qsTypeParameter.TypeName.Value,
                Range = qsTypeParameter.Range.IsNull ? null : qsTypeParameter.Range.Item.ToBondSchema()
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
            var kind = specializationImplementation.Tag switch
            {
                SyntaxTree.SpecializationImplementation.Tags.External => SpecializationImplementationKind.External,
                SyntaxTree.SpecializationImplementation.Tags.Generated => SpecializationImplementationKind.Generated,
                SyntaxTree.SpecializationImplementation.Tags.Intrinsic => SpecializationImplementationKind.Intrinsic,
                SyntaxTree.SpecializationImplementation.Tags.Provided => SpecializationImplementationKind.Provided,
                _ => throw new ArgumentException($"Unsupported SpecializationImplementation {specializationImplementation}")
            };

            QsGeneratorDirective? bondGenerated = null;
            SyntaxTokens.QsGeneratorDirective compilerGenerated = null;
            SpecializationImplementationKindProvided bondProvided = null;
            Tuple<SyntaxTokens.QsTuple<SyntaxTree.LocalVariableDeclaration<SyntaxTree.QsLocalSymbol>>, SyntaxTree.QsScope> compilerProvided = null;
            if (specializationImplementation.TryGetGenerated(ref compilerGenerated))
            {
                bondGenerated = compilerGenerated.ToBondSchema();
            }
            else if (specializationImplementation.TryGetProvided(ref compilerProvided))
            {
                bondProvided = new SpecializationImplementationKindProvided
                {
                    Tuple = compilerProvided.Item1.ToBondSchema(),
                    Implementation = compilerProvided.Item2.ToBondSchema()
                };
            }
            
            return new SpecializationImplementation
            {
                Kind = kind,
                Provided = bondProvided,
                Generated = bondGenerated
            };
        }

        private static string ToBondSchema(this NonNullable<string> s) => s.Value;

        private static SymbolTuple ToBondSchema(this SyntaxTree.SymbolTuple symbolTuple)
        {
            string? bondVariableName = null;
            List<SymbolTuple> bondVariableNameTuple = null;
            DataTypes.NonNullable<string> compilerVariableName = default;
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

        private static TypedArgument ToBondSchema(this Tuple<SyntaxTree.QsQualifiedName, NonNullable<string>, SyntaxTree.ResolvedType> typedArgumet) =>
            new TypedArgument
            {
                Callable = typedArgumet.Item1.ToBondSchema(),
                Name = typedArgumet.Item2.Value,
                Resolution = typedArgumet.Item3.ToBondSchema()
            };

        private static TypedExpression ToBondSchema(this SyntaxTree.TypedExpression typedExpression) =>
            new TypedExpression
            {
                Expression = typedExpression.Expression.ToBondSchema(),
                TypedArguments = typedExpression.TypeArguments.Select(t => t.ToBondSchema()).ToList(),
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
                Range = userDefinedType.Range.IsNull ? null : userDefinedType.Range.Item.ToBondSchema()
            };

        private static CharacteristicsKind ToBondSchemaGeneric<CompilerType>(
            this SyntaxTokens.CharacteristicsKind<CompilerType> characteristicsKind) =>
            characteristicsKind.Tag switch
            {
                SyntaxTokens.CharacteristicsKind<CompilerType>.Tags.EmptySet => CharacteristicsKind.EmptySet,
                SyntaxTokens.CharacteristicsKind<CompilerType>.Tags.Intersection => CharacteristicsKind.Intersection,
                SyntaxTokens.CharacteristicsKind<CompilerType>.Tags.InvalidSetExpr => CharacteristicsKind.InvalidSetExpr,
                SyntaxTokens.CharacteristicsKind<CompilerType>.Tags.SimpleSet => CharacteristicsKind.SimpleSet,
                SyntaxTokens.CharacteristicsKind<CompilerType>.Tags.Union => CharacteristicsKind.Union,
                _ => throw new ArgumentException($"Unsupported CharacteristicsKind {characteristicsKind}")
            };

        private static CharacteristicsKindDetail<BondType> ToBondSchemaGeneric<BondType, CompilerType>(
            this SyntaxTokens.CharacteristicsKind<CompilerType> characteristicsKind,
            Func<CompilerType, BondType> typeTranslator)
            where BondType : class
            where CompilerType : class
        {
            OpProperty? bondSimpleSet = null;
            CharacteristicsKindSetOperation<BondType> bondSetOperation = null;
            SyntaxTokens.OpProperty compilerSimpleSet = null;
            Tuple<CompilerType, CompilerType> compilerIntersection = null;
            Tuple<CompilerType, CompilerType> compilerUnion = null;
            var kind = characteristicsKind.ToBondSchemaGeneric();
            if (characteristicsKind.TryGetSimpleSet(ref compilerSimpleSet))
            {
                bondSimpleSet = compilerSimpleSet.ToBondSchema();
            }
            else if (characteristicsKind.TryGetIntersection(ref compilerIntersection))
            {
                bondSetOperation = new CharacteristicsKindSetOperation<BondType>
                {
                    SetA = typeTranslator(compilerIntersection.Item1),
                    SetB = typeTranslator(compilerIntersection.Item2)
                };
            }
            else if (characteristicsKind.TryGetUnion(ref compilerUnion))
            {
                bondSetOperation = new CharacteristicsKindSetOperation<BondType>
                {
                    SetA = typeTranslator(compilerUnion.Item1),
                    SetB = typeTranslator(compilerUnion.Item2)
                };
            }

            return new CharacteristicsKindDetail<BondType>
            {
                Kind = kind,
                SimpleSet = bondSimpleSet,
                SetOperation = bondSetOperation
            };
        }

        private static LocalVariableDeclaration<BondType> ToBondSchemaGeneric<BondType, CompilerType>(
            this SyntaxTree.LocalVariableDeclaration<CompilerType> localVariableDeclaration,
            Func<CompilerType, BondType> typeTranslator) =>
            new LocalVariableDeclaration<BondType>
            {
                VariableName = typeTranslator(localVariableDeclaration.VariableName),
                Type = localVariableDeclaration.Type.ToBondSchema(),
                InferredInformation = localVariableDeclaration.InferredInformation.ToBondSchema(),
                Position = localVariableDeclaration.Position.IsNull ?
                    null :
                    localVariableDeclaration.Position.Item.ToBondSchema(),
                Range = localVariableDeclaration.Range.ToBondSchema()
            };

        private static QsBinding<BondType> ToBondSchemaGeneric<BondType, CompilerType>(
            this SyntaxTree.QsBinding<CompilerType> qsBinding,
            Func<CompilerType, BondType> typeTranslator) =>
            new QsBinding<BondType>
            {
                Kind = qsBinding.Kind.ToBondSchema(),
                Lhs = qsBinding.Lhs.ToBondSchema(),
                Rhs = typeTranslator(qsBinding.Rhs)
            };

        private static QsExpressionKindDetail<TBondExpression, TBondSymbol, TBondType> ToBondSchemaGeneric<
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
            QsExpressionKindIdentifier<TBondSymbol, TBondType> bondIdentifier = null;
            Int64? bondIntLiteral = null;
            ArraySegment<byte> bondBigIntLiteral = null;
            double? bondDoubleLiteral = null;
            bool? bondBoolLiteral = null;
            QsExpressionKindStringLiteral<TBondExpression> bondStringLiteral = null;
            QsResult? bondResultLiteral = null;
            QsPauli? bondPauliLiteral = null;
            QsExpressionKindNewArray<TBondExpression, TBondType> bondNewArray = null;
            QsExpressionKindNamedItem<TBondExpression, TBondSymbol> bondNamedItem = null;
            TBondExpression bondExpression = null;
            QsExpressionKindExpressionDouble<TBondExpression> bondExpressionDouble = null;
            QsExpressionKindExpressionTriple<TBondExpression> bondExpressionTriple = null;
            List<TBondExpression> bondExpressionArray = null;
            Tuple<TCompilerSymbol, QsNullable<ImmutableArray<TCompilerType>>> compilerIdentifier = null;
            Int64 compilerIntLiteral = default;
            BigInteger compilerBigIntLiteral = default;
            double compilerDoubleLiteral = default;
            bool compilerBoolLiteral = default;
            Tuple<NonNullable<string>, ImmutableArray<TCompilerExpression>> compilerStringLiteral = default;
            SyntaxTokens.QsResult compilerResultLiteral = default;
            SyntaxTokens.QsPauli compilerPauliLiteral = default;
            Tuple<TCompilerType, TCompilerExpression> compilerNewArray = default;
            Tuple<TCompilerExpression, TCompilerSymbol> compilerNamedItem = default;
            TCompilerExpression compilerExpression = default;
            Tuple<TCompilerExpression, TCompilerExpression> compilerExpressionDouble = default;
            Tuple<TCompilerExpression, TCompilerExpression, TCompilerExpression> compilerExpressionTriple = default;
            ImmutableArray<TCompilerExpression> compilerExpressionArray = default;
            QsExpressionKind kind;
            if (qsExpressionKind.TryGetIdentifier(ref compilerIdentifier))
            {
                kind = QsExpressionKind.Identifier;
                bondIdentifier = new QsExpressionKindIdentifier<TBondSymbol, TBondType>
                {
                    Symbol = symbolTranslator(compilerIdentifier.Item1),
                    Types = compilerIdentifier.Item2.IsNull ?
                        null :
                        compilerIdentifier.Item2.Item.Select(t => typeTranslator(t)).ToList()
                };
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
                bondStringLiteral = new QsExpressionKindStringLiteral<TBondExpression>
                {
                    StringLiteral = compilerStringLiteral.Item1.ToBondSchema(),
                    Expressions = compilerStringLiteral.Item2.Select(e => expressionTranslator(e)).ToList()
                };
            }
            else if (qsExpressionKind.TryGetResultLiteral(ref compilerResultLiteral))
            {
                kind = QsExpressionKind.ResultLiteral;
                bondResultLiteral = compilerResultLiteral.ToBondSchema();
            }
            else if (qsExpressionKind.TryGetPauliLiteral(ref compilerPauliLiteral))
            {
                kind = QsExpressionKind.PauliLiteral;
                bondPauliLiteral = compilerPauliLiteral.ToBondSchema();
            }
            else if (qsExpressionKind.TryGetRangeLiteral(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.RangeLiteral;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetNewArray(ref compilerNewArray))
            {
                kind = QsExpressionKind.NewArray;
                bondNewArray = new QsExpressionKindNewArray<TBondExpression, TBondType>
                {
                    Type = typeTranslator(compilerNewArray.Item1),
                    Expression = expressionTranslator(compilerNewArray.Item2)
                };
            }
            else if (qsExpressionKind.TryGetValueArray(ref compilerExpressionArray))
            {
                kind = QsExpressionKind.ValueArray;
                bondExpressionArray = compilerExpressionArray.Select(e => expressionTranslator(e)).ToList();
            }
            else if (qsExpressionKind.TryGetArrayItem(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.ArrayItem;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetNamedItem(ref compilerNamedItem))
            {
                kind = QsExpressionKind.NamedItem;
                bondNamedItem = compilerNamedItem.ToBondSchemaGeneric(expressionTranslator, symbolTranslator);
            }
            else if (qsExpressionKind.TryGetNEG(ref compilerExpression))
            {
                kind = QsExpressionKind.NEG;
                bondExpression = expressionTranslator(compilerExpression);
            }
            else if (qsExpressionKind.TryGetNOT(ref compilerExpression))
            {
                kind = QsExpressionKind.NOT;
                bondExpression = expressionTranslator(compilerExpression);
            }
            else if (qsExpressionKind.TryGetBNOT(ref compilerExpression))
            {
                kind = QsExpressionKind.BNOT;
                bondExpression = expressionTranslator(compilerExpression);
            }
            else if (qsExpressionKind.TryGetADD(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.ADD;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetSUB(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.SUB;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetMUL(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.MUL;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetDIV(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.DIV;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetMOD(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.MOD;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetPOW(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.POW;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetEQ(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.EQ;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetNEQ(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.NEQ;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetLT(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.LT;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetLTE(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.LTE;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetGT(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.GT;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetGTE(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.GTE;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetAND(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.AND;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetOR(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.OR;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetBOR(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.BOR;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetBAND(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.BAND;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetBXOR(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.BXOR;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetLSHIFT(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.LSHIFT;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetRSHIFT(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.RSHIFT;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetCONDITIONAL(ref compilerExpressionTriple))
            {
                kind = QsExpressionKind.CONDITIONAL;
                bondExpressionTriple = compilerExpressionTriple.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetCopyAndUpdate(ref compilerExpressionTriple))
            {
                kind = QsExpressionKind.CopyAndUpdate;
                bondExpressionTriple = compilerExpressionTriple.ToBondSchemaGeneric(expressionTranslator);
            }
            else if (qsExpressionKind.TryGetUnwrapApplication(ref compilerExpression))
            {
                kind = QsExpressionKind.UnwrapApplication;
                bondExpression = expressionTranslator(compilerExpression);
            }
            else if (qsExpressionKind.TryGetAdjointApplication(ref compilerExpression))
            {
                kind = QsExpressionKind.AdjointApplication;
                bondExpression = expressionTranslator(compilerExpression);
            }
            else if (qsExpressionKind.TryGetControlledApplication(ref compilerExpression))
            {
                kind = QsExpressionKind.ControlledApplication;
                bondExpression = expressionTranslator(compilerExpression);
            }
            else if (qsExpressionKind.TryGetCallLikeExpression(ref compilerExpressionDouble))
            {
                kind = QsExpressionKind.CallLikeExpression;
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
                bondExpressionDouble = compilerExpressionDouble.ToBondSchemaGeneric(expressionTranslator);
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

            return new QsExpressionKindDetail<TBondExpression, TBondSymbol, TBondType>
            {
                Kind = kind,
                Identifier = bondIdentifier,
                IntLiteral = bondIntLiteral,
                BigIntLiteral = bondBigIntLiteral,
                DoubleLiteral = bondDoubleLiteral,
                StringLiteral = bondStringLiteral,
                ResultLiteral = bondResultLiteral,
                PauliLiteral = bondPauliLiteral,
                NewArray = bondNewArray,
                NamedItem = bondNamedItem,
                Expression = bondExpression,
                ExpressionDouble = bondExpressionDouble,
                ExpressionArray = bondExpressionArray
            };
        }

        private static QsExpressionKindExpressionDouble<TBondExpression> ToBondSchemaGeneric<TBondExpression, TCompilerExpression>(
            this Tuple<TCompilerExpression, TCompilerExpression> expressionDouble,
            Func<TCompilerExpression, TBondExpression> expressionTranslator) =>
            new QsExpressionKindExpressionDouble<TBondExpression>
            {
                Expression1 = expressionTranslator(expressionDouble.Item1),
                Expression2 = expressionTranslator(expressionDouble.Item2)
            };

        private static QsExpressionKindExpressionTriple<TBondExpression> ToBondSchemaGeneric<TBondExpression, TCompilerExpression>(
            this Tuple<TCompilerExpression, TCompilerExpression, TCompilerExpression> expressionTriple,
            Func<TCompilerExpression, TBondExpression> expressionTranslator) =>
            new QsExpressionKindExpressionTriple<TBondExpression>
            {
                Expression1 = expressionTranslator(expressionTriple.Item1),
                Expression2 = expressionTranslator(expressionTriple.Item2),
                Expression3 = expressionTranslator(expressionTriple.Item3)
            };

        private static QsExpressionKindNamedItem<TBondExpression, TBondSymbol> ToBondSchemaGeneric<
            TBondExpression,
            TBondSymbol,
            TCompilerExpression,
            TCompilerSymbol>(
                this Tuple<TCompilerExpression, TCompilerSymbol> namedItem,
                Func<TCompilerExpression, TBondExpression> expressionTranslator,
                Func<TCompilerSymbol, TBondSymbol> symbolTranslator) =>
            new QsExpressionKindNamedItem<TBondExpression, TBondSymbol>
            {
                Expression = expressionTranslator(namedItem.Item1),
                Symbol = symbolTranslator(namedItem.Item2)
            };

        private static QsInitializerKindDetail<TBondInitializer, TBondExpression> ToBondSchemaGeneric<
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
            TBondExpression bondQubitRegisterAllocation = null;
            List<TBondInitializer> bondQubitTupleAllocation = null;
            TCompilerExpression compilerQubitRegisterAllocation = default;
            ImmutableArray<TCompilerInitializer> compilerQubitTupleAllocation = default;
            QsInitializerKind kind;
            if (qsInitializerKind.TryGetQubitRegisterAllocation(ref compilerQubitRegisterAllocation))
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

            return new QsInitializerKindDetail<TBondInitializer, TBondExpression>
            {
                Kind = kind,
                QubitRegisterAllocation = bondQubitRegisterAllocation,
                QubitTupleAllocation = bondQubitTupleAllocation
            };
        }

        private static QsTuple<BondType> ToBondSchemaGeneric<BondType, CompilerType>(
            this SyntaxTokens.QsTuple<CompilerType> qsTuple,
            Func<CompilerType, BondType> typeTranslator)
        {
            CompilerType item = default;
            ImmutableArray<SyntaxTokens.QsTuple<CompilerType>> items;
            if (qsTuple.TryGetQsTupleItem(ref item))
            {
                return new QsTuple<BondType>
                {
                    Kind = QsTupleKind.QsTupleItem,
                    Item = typeTranslator(item)
                };
            }
            else if (qsTuple.TryGetQsTuple(ref items))
            {
                return new QsTuple<BondType>
                {
                    Kind = QsTupleKind.QsTuple,
                    Items = items.Select(i => i.ToBondSchemaGeneric(typeTranslator)).ToList()
                };
            }
            else
            {
                throw new ArgumentException($"Unsupported QsTuple kind {qsTuple}");
            }
        }

        private static QsTypeKind ToBondSchemaGeneric<
            CompilerDataType,
            CompilerUdtType,
            CompilerTParamType,
            CompilerCharacteristicsType>(
            this SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType> qsTypeKind) =>
            qsTypeKind.Tag switch
            {
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.ArrayType => QsTypeKind.ArrayType,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.BigInt => QsTypeKind.BigInt,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Bool => QsTypeKind.Bool,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Double => QsTypeKind.Double,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Function => QsTypeKind.Function,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Int => QsTypeKind.Int,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.InvalidType => QsTypeKind.InvalidType,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.MissingType => QsTypeKind.MissingType,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Operation => QsTypeKind.Operation,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Pauli => QsTypeKind.Pauli,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Qubit => QsTypeKind.Qubit,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Range => QsTypeKind.Range,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Result => QsTypeKind.Result,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.String => QsTypeKind.String,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.TupleType => QsTypeKind.TupleType,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.TypeParameter => QsTypeKind.TypeParameter,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.UnitType => QsTypeKind.UnitType,
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.UserDefinedType => QsTypeKind.UserDefinedType,
                _ => throw new ArgumentException($"Unsupported QsTypeKind: {qsTypeKind.Tag}")
            };

        private static QsTypeKindDetails<BondDataType, BondUdtType, BondTParamType, BondCharacteristicsType> ToBondSchemaGeneric
            <BondDataType,
             BondUdtType,
             BondTParamType,
             BondCharacteristicsType,
             CompilerDataType,
             CompilerUdtType,
             CompilerTParamType,
             CompilerCharacteristicsType>(
                this SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType> qsTypeKind,
                Func<CompilerDataType, BondDataType> dataTypeTranslator,
                Func<CompilerUdtType, BondUdtType> udtTypeTranslator,
                Func<CompilerTParamType, BondTParamType> tParamTypeTranslator,
                Func<CompilerCharacteristicsType, BondCharacteristicsType> characteristicsTypeTranslator)
            where BondDataType : class
            where BondUdtType : class
            where BondTParamType : class
            where BondCharacteristicsType : class
            where CompilerDataType : class
            where CompilerUdtType : class
            where CompilerTParamType : class
            where CompilerCharacteristicsType : class
        {

            BondDataType bondArrayType = null;
            QsTypeKindFunction<BondDataType> bondFunction = null;
            QsTypeKindOperation<BondDataType, BondCharacteristicsType> bondOperation = null;
            List<BondDataType> bondTupleType = null;
            BondTParamType bondTypeParameter = null;
            BondUdtType bondUserDefinedType = null;
            CompilerDataType compilerArrayType = null;
            Tuple<CompilerDataType, CompilerDataType> compilerFunction = null;
            Tuple<Tuple<CompilerDataType, CompilerDataType>, CompilerCharacteristicsType> compilerOperation = null;
            ImmutableArray<CompilerDataType> compilerTupleType;
            CompilerTParamType compilerTyperParameter = null;
            CompilerUdtType compilerUdtType = null;
            if (qsTypeKind.TryGetArrayType(ref compilerArrayType))
            {
                bondArrayType = dataTypeTranslator(compilerArrayType);
            }
            else if (qsTypeKind.TryGetFunction(ref compilerFunction))
            {
                bondFunction = new QsTypeKindFunction<BondDataType>
                {
                    DataA = dataTypeTranslator(compilerFunction.Item1),
                    DataB = dataTypeTranslator(compilerFunction.Item2)
                };
            }
            else if (qsTypeKind.TryGetOperation(ref compilerOperation))
            {
                bondOperation = new QsTypeKindOperation<BondDataType, BondCharacteristicsType>
                {
                    DataA = dataTypeTranslator(compilerOperation.Item1.Item1),
                    DataB = dataTypeTranslator(compilerOperation.Item1.Item2),
                    Characteristics = characteristicsTypeTranslator(compilerOperation.Item2)
                };
            }
            else if (qsTypeKind.TryGetTupleType(ref compilerTupleType))
            {
                bondTupleType = compilerTupleType.Select(t => dataTypeTranslator(t)).ToList();
            }
            else if (qsTypeKind.TryGetTypeParameter(ref compilerTyperParameter))
            {
                bondTypeParameter = tParamTypeTranslator(compilerTyperParameter);
            }
            else if (qsTypeKind.TryGetUserDefinedType(ref compilerUdtType))
            {
                bondUserDefinedType = udtTypeTranslator(compilerUdtType);
            }

            var bondQsTypeKindDetails = qsTypeKind.Tag switch
            {
                var tag when
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.BigInt ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Bool ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Double ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Int ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.InvalidType ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.MissingType ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Pauli ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Qubit ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Range ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Result ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.String ||
                    tag == SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.UnitType =>
                        new QsTypeKindDetails<BondDataType, BondUdtType, BondTParamType, BondCharacteristicsType>
                        {
                            Kind = qsTypeKind.ToBondSchemaGeneric()
                        },
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.ArrayType =>
                    new QsTypeKindDetails<BondDataType, BondUdtType, BondTParamType, BondCharacteristicsType>
                    {
                        Kind = QsTypeKind.ArrayType,
                        ArrayType = bondArrayType ?? throw new InvalidOperationException($"ArrayType cannot be null when Kind is {QsTypeKind.ArrayType}")
                    },
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Function =>
                    new QsTypeKindDetails<BondDataType, BondUdtType, BondTParamType, BondCharacteristicsType>
                    {
                        Kind = QsTypeKind.Function,
                        Function = bondFunction ?? throw new InvalidOperationException($"Function cannot be null when Kind is {QsTypeKind.Function}")
                    },
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.Operation =>
                    new QsTypeKindDetails<BondDataType, BondUdtType, BondTParamType, BondCharacteristicsType>
                    {
                        Kind = QsTypeKind.Operation,
                        Operation = bondOperation ?? throw new InvalidOperationException($"Operation cannot be null when Kind is {QsTypeKind.Operation}")
                    },
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.TupleType =>
                    new QsTypeKindDetails<BondDataType, BondUdtType, BondTParamType, BondCharacteristicsType>
                    {
                        Kind = QsTypeKind.TupleType,
                        TupleType = bondTupleType ?? throw new InvalidOperationException($"TupleType cannot be null when Kind is {QsTypeKind.TupleType}")
                    },
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.TypeParameter =>
                    new QsTypeKindDetails<BondDataType, BondUdtType, BondTParamType, BondCharacteristicsType>
                    {
                        Kind = QsTypeKind.TypeParameter,
                        TypeParameter = bondTypeParameter ?? throw new InvalidOperationException($"TypeParameter cannot be null when Kind is {QsTypeKind.TypeParameter}")
                    },
                SyntaxTokens.QsTypeKind<CompilerDataType, CompilerUdtType, CompilerTParamType, CompilerCharacteristicsType>.Tags.UserDefinedType =>
                    new QsTypeKindDetails<BondDataType, BondUdtType, BondTParamType, BondCharacteristicsType>
                    {
                        Kind = QsTypeKind.UserDefinedType,
                        UserDefinedType = bondUserDefinedType ?? throw new InvalidOperationException($"UserDefinedType cannot be null when Kind is {QsTypeKind.UserDefinedType}")
                    },
                _ => throw new ArgumentException($"Unsupported QsTypeKind: {qsTypeKind.Tag}")
            };

            return bondQsTypeKindDetails;
        }
    }
}
