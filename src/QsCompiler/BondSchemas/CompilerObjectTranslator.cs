// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;

namespace Microsoft.Quantum.QsCompiler.BondSchemas
{
    public static class CompilerObjectTranslator
    {
        public static SyntaxTree.QsCompilation CreateQsCompilation(QsCompilation bondCompilation) =>
            new SyntaxTree.QsCompilation(
                namespaces: bondCompilation.Namespaces.Select(n => n.ToCompilerObject()).ToImmutableArray(),
                // TODO: Implement EntryPoints.
                entryPoints: Array.Empty<SyntaxTree.QsQualifiedName>().ToImmutableArray());

        private static DataTypes.Position ToCompilerObject(this Position position) =>
            DataTypes.Position.Create(
                line: position.Line,
                column: position.Column);

        private static DataTypes.Range ToCompilerObject(this Range range) =>
            DataTypes.Range.Create(
                start: range.Start.ToCompilerObject(),
                end: range.End.ToCompilerObject());

        private static SyntaxTree.CallableInformation ToCompilerObject(CallableInformation bondCallableInformation) =>
            new SyntaxTree.CallableInformation(
                // TODO: Implement Characteristics.
                characteristics: default,
                // TODO: Implement InferredInformation.
                inferredInformation: default);

        private static SyntaxTree.QsCallable ToCompilerObject(this QsCallable bondQsCallable) =>
            new SyntaxTree.QsCallable(
                kind: bondQsCallable.Kind.ToCompilerObject(),
                fullName: bondQsCallable.FullName.ToCompilerObject(),
                attributes: bondQsCallable.Attributes.Select(a => a.ToCompilerObject()).ToImmutableArray(),
                modifiers: bondQsCallable.Modifiers.ToCompilerObject(),
                sourceFile: bondQsCallable.SourceFile.ToNonNullable(),
                location: bondQsCallable.Location != null ?
                    bondQsCallable.Location.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.QsLocation>.Null,
                signature: bondQsCallable.Signature.ToCompilerObject(),
                // TODO: Implement ArgumentTuple.
                argumentTuple: default,
                specializations: bondQsCallable.Specializations.Select(s => s.ToCompilerObject()).ToImmutableArray(),
                documentation: bondQsCallable.Documentation.ToImmutableArray(),
                comments: bondQsCallable.Comments.ToCompilerObject());

        private static SyntaxTree.QsCallableKind ToCompilerObject(this QsCallableKind bondQsCallableKind) =>
            bondQsCallableKind switch
            {
                QsCallableKind.Operation => SyntaxTree.QsCallableKind.Operation,
                QsCallableKind.Function => SyntaxTree.QsCallableKind.Function,
                QsCallableKind.TypeConstructor => SyntaxTree.QsCallableKind.TypeConstructor,
                _ => throw new ArgumentException($"Unsupported Bond QsCallableKind: {bondQsCallableKind}")
            };

        private static SyntaxTree.QsComments ToCompilerObject(this QsComments bondQsComments) =>
            new SyntaxTree.QsComments(
                openingComments: bondQsComments.OpeningComments.ToImmutableArray(),
                closingComments: bondQsComments.ClosingComments.ToImmutableArray());

        private static SyntaxTree.QsCustomType ToCompilerObject(this QsCustomType bondQsCustomType) =>
            new SyntaxTree.QsCustomType(
                fullName: bondQsCustomType.FullName.ToCompilerObject(),
                // TODO: Implement Attributes.
                attributes: Array.Empty<SyntaxTree.QsDeclarationAttribute>().ToImmutableArray(),
                modifiers: bondQsCustomType.Modifiers.ToCompilerObject(),
                sourceFile: bondQsCustomType.SourceFile.ToNonNullable(),
                location: bondQsCustomType.Location != null ?
                    bondQsCustomType.Location.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.QsLocation>.Null,
                // TODO: Implement Type.
                type: default,
                // TODO: Implement TypeItems.
                typeItems: default,
                documentation: bondQsCustomType.Documentation.ToImmutableArray(),
                comments: bondQsCustomType.Comments.ToCompilerObject());

        private static SyntaxTree.QsDeclarationAttribute ToCompilerObject(this QsDeclarationAttribute bondQsDeclarationAttribute) =>
            new SyntaxTree.QsDeclarationAttribute(
                typeId: bondQsDeclarationAttribute.TypeId != null ?
                    bondQsDeclarationAttribute.TypeId.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.UserDefinedType>.Null,
                // TODO: Implement Argument.
                argument: default,
                offset: bondQsDeclarationAttribute.Offset.ToCompilerObject(),
                comments: bondQsDeclarationAttribute.Comments.ToCompilerObject());

        private static SyntaxTree.QsLocalSymbol ToCompilerObject(this QsLocalSymbol bondQsLocalSymbol) =>
            bondQsLocalSymbol.Kind switch
            {
                QsLocalSymbolKind.ValidName => SyntaxTree.QsLocalSymbol.NewValidName(bondQsLocalSymbol.Name.ToNonNullable()),
                QsLocalSymbolKind.InvalidName => SyntaxTree.QsLocalSymbol.InvalidName,
                _ => throw new ArgumentException($"Unsupported Bond QsLocalSymbolKind: {bondQsLocalSymbol.Kind}")
            };

        private static SyntaxTree.QsLocation ToCompilerObject(this QsLocation bondQsLocation) =>
            new SyntaxTree.QsLocation(
                offset: bondQsLocation.Offset.ToCompilerObject(),
                range: bondQsLocation.Range.ToCompilerObject());

        private static SyntaxTree.QsNamespace ToCompilerObject(this QsNamespace bondQsNamespace) =>
            new SyntaxTree.QsNamespace(
                name: bondQsNamespace.Name.ToNonNullable(),
                elements: bondQsNamespace.Elements.Select(e => e.ToCompilerObject()).ToImmutableArray(),
                documentation: bondQsNamespace.Documentation.ToLookup(
                    p => p.FileName.ToNonNullable(),
                    p => p.DocumentationItems.ToImmutableArray()));

        private static SyntaxTree.QsNamespaceElement ToCompilerObject(this QsNamespaceElement bondQsNamespaceElement)
        {
            if (bondQsNamespaceElement.Kind == QsNamespaceElementKind.QsCallable)
            {
                return SyntaxTree.QsNamespaceElement.NewQsCallable(bondQsNamespaceElement.Callable.ToCompilerObject());
            }
            else if (bondQsNamespaceElement.Kind == QsNamespaceElementKind.QsCustomType)
            {
                return SyntaxTree.QsNamespaceElement.NewQsCustomType(bondQsNamespaceElement.CustomType.ToCompilerObject());
            }
            else
            {
                throw new ArgumentException($"Unsupported Bond QsNamespaceElementKind: {bondQsNamespaceElement.Kind}");
            }
        }

        private static SyntaxTree.QsQualifiedName ToCompilerObject(this QsQualifiedName bondQsQualifiedName) =>
            new SyntaxTree.QsQualifiedName(
                @namespace: bondQsQualifiedName.Namespace.ToNonNullable(),
                name: bondQsQualifiedName.Name.ToNonNullable());

        private static SyntaxTree.QsSpecialization ToCompilerObject(this QsSpecialization bondQsSpecialization) =>
            new SyntaxTree.QsSpecialization(
                kind: bondQsSpecialization.Kind.ToCompilerObject(),
                parent: bondQsSpecialization.Parent.ToCompilerObject(),
                attributes: bondQsSpecialization.Attributes.Select(a => a.ToCompilerObject()).ToImmutableArray(),
                sourceFile: bondQsSpecialization.SourceFile.ToNonNullable(),
                location: bondQsSpecialization.Location != null ?
                    bondQsSpecialization.Location.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<SyntaxTree.QsLocation>.Null,
                typeArguments: bondQsSpecialization.TypeArguments != null ?
                    bondQsSpecialization.TypeArguments.Select(t => t.ToCompilerObject()).ToImmutableArray().ToQsNullableGeneric() :
                    QsNullable<ImmutableArray<SyntaxTree.ResolvedType>>.Null,
                // TODO: Implement Signature.
                signature: default,
                // TODO: Implement Implementation.
                implementation: default,
                // TODO: Implement Documentation.
                documentation: Array.Empty<string>().ToImmutableArray(),
                // TODO: Implement Comments.
                comments: default);

        private static SyntaxTree.QsSpecializationKind ToCompilerObject(this QsSpecializationKind bondQsSpecializationKind) =>
            bondQsSpecializationKind switch
            {
                QsSpecializationKind.QsAdjoint => SyntaxTree.QsSpecializationKind.QsAdjoint,
                QsSpecializationKind.QsBody => SyntaxTree.QsSpecializationKind.QsBody,
                QsSpecializationKind.QsControlled => SyntaxTree.QsSpecializationKind.QsControlled,
                QsSpecializationKind.QsControlledAdjoint => SyntaxTree.QsSpecializationKind.QsControlledAdjoint,
                _ => throw new ArgumentException($"Unsupported Bond QsSpecializationKind {bondQsSpecializationKind}")
            };

        private static SyntaxTree.QsTypeParameter ToCompilerObject(this QsTypeParameter bondQsTypeParameter) =>
            new SyntaxTree.QsTypeParameter(
                origin: bondQsTypeParameter.Origin.ToCompilerObject(),
                typeName: bondQsTypeParameter.TypeName.ToNonNullable(),
                range: bondQsTypeParameter.Range != null ?
                    bondQsTypeParameter.Range.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<DataTypes.Range>.Null);

        private static SyntaxTree.ResolvedSignature ToCompilerObject(this ResolvedSignature bondResolvedSignature) =>
            new SyntaxTree.ResolvedSignature(
                typeParameters: bondResolvedSignature.TypeParameters.Select(tp => tp.ToCompilerObject()).ToImmutableArray(),
                // Implement ArgumentType
                argumentType: default,
                // Implement ReturnType
                returnType: default,
                // Implement Information
                information: default);

        private static SyntaxTree.ResolvedType ToCompilerObject(this ResolvedType bondResolvedType) =>
            SyntaxTree.ResolvedType.New(bondResolvedType.TypeKind.ToCompilerObject());

        private static SyntaxTree.UserDefinedType ToCompilerObject(this UserDefinedType bondUserDefinedType) =>
            new SyntaxTree.UserDefinedType(
                @namespace: bondUserDefinedType.Namespace.ToNonNullable(),
                name: bondUserDefinedType.Name.ToNonNullable(),
                range: bondUserDefinedType.Range != null ?
                    bondUserDefinedType.Range.ToCompilerObject().ToQsNullableGeneric() :
                    QsNullable<DataTypes.Range>.Null);

        private static SyntaxTokens.AccessModifier ToCompilerObject(this AccessModifier accessModifier) =>
            accessModifier switch
            {
                AccessModifier.DefaultAccess => SyntaxTokens.AccessModifier.DefaultAccess,
                AccessModifier.Internal => SyntaxTokens.AccessModifier.Internal,
                _ => throw new ArgumentException($"Unsupported Bond AccessModifier: {accessModifier}")
            };

        private static SyntaxTokens.Modifiers ToCompilerObject(this Modifiers modifiers) =>
            new SyntaxTokens.Modifiers(
                access: modifiers.Access.ToCompilerObject());

        private static SyntaxTokens.QsTypeKind<SyntaxTree.ResolvedType, SyntaxTree.UserDefinedType, SyntaxTree.QsTypeParameter, SyntaxTree.CallableInformation> ToCompilerObject(
            this QsTypeKindComposition<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation> bondQsTypeKindComposition) =>
            bondQsTypeKindComposition.ToCompilerObjectGeneric(
                typeTranslator: ToCompilerObject,
                udtTranslator: ToCompilerObject,
                paramTranslator: ToCompilerObject,
                characteristicsTranslator: ToCompilerObject);

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
            if (bondQsTypeKindComposition.Kind == QsTypeKind.ArrayType)
            {
                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.NewArrayType(
                    item: typeTranslator(bondQsTypeKindComposition.ArrayType));
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.TupleType)
            {
                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.NewTupleType(
                    item: bondQsTypeKindComposition.TupleType.Select(t => typeTranslator(t)).ToImmutableArray());
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.UserDefinedType)
            {
                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.NewUserDefinedType(
                    item: udtTranslator(bondQsTypeKindComposition.UserDefinedType));
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.TypeParameter)
            {
                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.NewTypeParameter(
                    item: paramTranslator(bondQsTypeKindComposition.TypeParameter));
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.Operation)
            {
                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.NewOperation(
                    item1: Tuple.Create(
                        typeTranslator(bondQsTypeKindComposition.Operation.Type1),
                        typeTranslator(bondQsTypeKindComposition.Operation.Type2)),
                    item2: characteristicsTranslator(bondQsTypeKindComposition.Operation.Characteristics));
            }
            else if (bondQsTypeKindComposition.Kind == QsTypeKind.Function)
            {
                return SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.NewFunction(
                    item1: typeTranslator(bondQsTypeKindComposition.Function.Type1),
                    item2: typeTranslator(bondQsTypeKindComposition.Function.Type2));
            }
            else
            {
                var simpleQsTypeKind = bondQsTypeKindComposition.Kind switch
                {
                    QsTypeKind.UnitType => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateUnitType<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.Int => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateInt<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.BigInt => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateBigInt<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.Double => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateDouble<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.Bool => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateBool<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.String => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateString<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.Qubit => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateQubit<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.Result => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateResult<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.Pauli => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreatePauli<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.Range => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateRange<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.MissingType => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateMissingType<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    QsTypeKind.InvalidType => SyntaxTokens.QsTypeKind<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>.
                        CreateInvalidType<TCompilerType, TCompilerUdt, TCompilerParam, TCompilerCharacteristics>(),
                    _ => throw new ArgumentException($"Unsupported Bond QsTypeKind {bondQsTypeKindComposition.Kind}")
                };

                return simpleQsTypeKind;
            }
        }

        private static NonNullable<string> ToNonNullable(this string str) =>
            NonNullable<string>.New(str);

        private static QsNullable<T> ToQsNullableGeneric<T>(this T obj) =>
                QsNullable<T>.NewValue(obj);
    }
}
