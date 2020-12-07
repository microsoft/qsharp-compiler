// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.Transformations.Targeting
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using TypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// Static class used to generated callables as needed
    /// such that each intrinsic specialization is its own callables,
    /// and label intrinsic callables with target instruction names.
    /// </summary>
    public static class InferTargetInstructions
    {
        /// <inheritdoc cref="OnNamespace"/>
        public static QsCompilation LiftIntrinsicSpecializations(QsCompilation compilation)
        {
            var namespaces = compilation.Namespaces.Select(OnNamespace).ToImmutableArray();
            return new QsCompilation(namespaces, compilation.EntryPoints);
        }

        /// <summary>
        /// Adds a TargetInstruction attribute to each intrinsic callable that doesn't have one,
        /// unless the automatically determined target instruction name conflicts with another target instruction name.
        /// The automatically determined name of the target instruction is the lower case version of the unqualified callable name.
        /// Type constructors and generic callables are left unchanged.
        /// </summary>
        /// <returns>Returns true if all missing attributes have been successfully added and false if some attributes could not be added.</returns>
        /// <exception cref="InvalidOperationException">All intrinsic callables need to have exactly one body specialization.</exception>
        public static bool TryAddMissingTargetInstructionAttributes(QsCompilation compilation, out QsCompilation transformed)
        {
            var success = true;
            List<string> instructionNames = new List<string>();
            // populate the intruction names with all manually specified ones first
            foreach (var callable in compilation.Namespaces.Callables())
            {
                var manuallySpecified = SymbolResolution.TryGetTargetInstructionName(callable.Attributes);
                if (manuallySpecified.IsValue)
                {
                    instructionNames.Add(manuallySpecified.Item);
                }
            }

            QsCallable AddAttribute(QsCallable callable)
            {
                if (callable.Specializations.Length != 1)
                {
                    throw new InvalidOperationException("intrinsic callable needs to have exactly one body specialization");
                }

                var instructionName = callable.FullName.Name.ToLowerInvariant();
                if (instructionNames.Contains(instructionName))
                {
                    success = false;
                    return callable;
                }
                return callable.AddAttribute(
                    AttributeUtils.BuildAttribute(
                        BuiltIn.TargetInstruction.FullName,
                        AttributeUtils.StringArgument(instructionName)));
            }

            QsNamespace AddAttributes(QsNamespace ns)
            {
                var elements = ns.Elements.Select(element =>
                    element is QsNamespaceElement.QsCallable callable
                    && callable.Item.IsIntrinsic
                    && callable.Item.Signature.TypeParameters.Length == 0
                    && !callable.Item.Kind.IsTypeConstructor
                    && !callable.Item.Attributes.Any(BuiltIn.DefinesTargetInstruction)
                    ? QsNamespaceElement.NewQsCallable(AddAttribute(callable.Item))
                    : element);
                return new QsNamespace(ns.Name, elements.ToImmutableArray(), ns.Documentation);
            }

            var namespaces = compilation.Namespaces.Select(AddAttributes).ToImmutableArray();
            transformed = new QsCompilation(namespaces, compilation.EntryPoints);
            return success;
        }

        /// <summary>
        /// Creates a separate callable for each intrinsic specialization.
        /// Leaves any type parameterized callables unmodified.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// An intrinsic callable contains non-intrinsic specializations
        /// or a non-intrinsic callable contains intrinsic specializations,
        /// or the a callable doesn't have a body specialization.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// A specialization has explicit type arguments;
        /// Monomorphization needs to run before separating target instructions.
        /// </exception>
        private static QsNamespace OnNamespace(QsNamespace ns)
        {
            var elements = ImmutableArray.CreateBuilder<QsNamespaceElement>();
            foreach (var element in ns.Elements)
            {
                if (element is QsNamespaceElement.QsCallable c && c.Item.Signature.TypeParameters.Length == 0)
                {
                    if (c.Item.IsIntrinsic)
                    {
                        QsCallable callable = c.Item;
                        if (!callable.Specializations.Any(spec => spec.Kind.IsQsBody))
                        {
                            throw new ArgumentException("missing body specialization");
                        }
                        else if (callable.Specializations.Any(spec => spec.TypeArguments.IsValue))
                        {
                            throw new InvalidOperationException("specialization with type arguments");
                        }
                        else if (callable.Specializations.Length == 1)
                        {
                            // No need to generate a separate callable for the specialization since there is only one.
                            elements.Add(element);
                        }
                        else
                        {
                            QsQualifiedName GeneratedName(QsSpecializationKind kind)
                            {
                                var suffix =
                                    kind.IsQsBody ? "Body" :
                                    kind.IsQsAdjoint ? "Adj" :
                                    kind.IsQsControlled ? "Ctl" :
                                    kind.IsQsControlledAdjoint ? "CtlAdj" :
                                    kind.ToString();
                                return new QsQualifiedName(callable.FullName.Namespace, $"{callable.FullName.Name}__{suffix}");
                            }

                            var specializations = callable.Specializations.Select(spec =>
                            {
                                if (spec.Implementation is SpecializationImplementation.Generated gen && gen.Item.IsSelfInverse)
                                {
                                    return spec;
                                }

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

                                // Create a separate callable for that specialization
                                var genCallableSignature = new ResolvedSignature(
                                    ImmutableArray<QsLocalSymbol>.Empty,
                                    spec.Signature.ArgumentType,
                                    spec.Signature.ReturnType,
                                    new CallableInformation(
                                        ResolvedCharacteristics.Empty,
                                        new InferredCallableInformation(isIntrinsic: true, isSelfAdjoint: false)));
                                var genCallableName = GeneratedName(spec.Kind);
                                var genCallableBody = new QsSpecialization(
                                    QsSpecializationKind.QsBody,
                                    genCallableName,
                                    spec.Attributes,
                                    spec.SourceFile,
                                    QsNullable<QsLocation>.Null,
                                    spec.TypeArguments,
                                    genCallableSignature,
                                    SpecializationImplementation.Intrinsic,
                                    spec.Documentation,
                                    spec.Comments);
                                var genCallable = new QsCallable(
                                    callable.Kind,
                                    genCallableName,
                                    callable.Attributes,
                                    callable.Modifiers,
                                    spec.SourceFile,
                                    spec.Location,
                                    genCallableSignature,
                                    argTuple,
                                    ImmutableArray.Create(genCallableBody),
                                    ImmutableArray<string>.Empty,
                                    QsComments.Empty);
                                elements.Add(QsNamespaceElement.NewQsCallable(genCallable));

                                // Create a specialization that calls into the generated callable.
                                var genCallableType =
                                    callable.Kind == QsCallableKind.Operation
                                    ? TypeKind.NewOperation(Tuple.Create(genCallableSignature.ArgumentType, genCallableSignature.ReturnType), genCallableSignature.Information)
                                    : TypeKind.NewFunction(genCallableSignature.ArgumentType, genCallableSignature.ReturnType);
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
                                    SyntaxGenerator.ValidDeclarations(SyntaxGenerator.ExtractItems(argTuple)));
                                return spec.WithImplementation(SpecializationImplementation.NewProvided(
                                    argTuple,
                                    new QsScope(ImmutableArray.Create(statement), localDeclarations)));
                            }).ToImmutableArray();

                            // Create a callable that contains all specializations that
                            // call into the generated callables for each specialization.
                            var inlineAttribute = AttributeUtils.BuildAttribute(BuiltIn.Inline.FullName, SyntaxGenerator.UnitValue);
                            var signature = new ResolvedSignature(
                                ImmutableArray<QsLocalSymbol>.Empty,
                                callable.Signature.ArgumentType,
                                callable.Signature.ReturnType,
                                new CallableInformation(
                                    callable.Signature.Information.Characteristics,
                                    new InferredCallableInformation(isSelfAdjoint: callable.IsSelfAdjoint, isIntrinsic: false)));
                            var redirect = new QsCallable(
                                callable.Kind,
                                callable.FullName,
                                ImmutableArray.Create(inlineAttribute),
                                callable.Modifiers,
                                callable.SourceFile,
                                callable.Location,
                                signature,
                                callable.ArgumentTuple,
                                specializations,
                                callable.Documentation,
                                callable.Comments);
                            elements.Add(QsNamespaceElement.NewQsCallable(redirect));
                        }
                    }
                    else if (c.Item.Specializations.Any(spec => spec.Implementation.IsIntrinsic))
                    {
                        throw new ArgumentException("intrinsic specialization for non-intrinsic callable");
                    }
                    else
                    {
                        elements.Add(element);
                    }
                }
                else
                {
                    elements.Add(element);
                }
            }
            return new QsNamespace(ns.Name, elements.ToImmutable(), ns.Documentation);
        }
    }
}
