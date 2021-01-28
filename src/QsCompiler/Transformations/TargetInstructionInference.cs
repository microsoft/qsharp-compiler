// Copyright (c) Microsoft Corporation.
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
    using ArgumentTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol>>;
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using TypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// Static class used to generated callables as needed
    /// such that each intrinsic specialization is its own callables,
    /// and label intrinsic callables with target instruction names.
    /// </summary>
    public static class InferTargetInstructions
    {
        /// <summary>
        /// Returns the suffix used to distinguish the generated callables for each functor specialization.
        /// </summary>
        public static string SpecializationSuffix(QsSpecializationKind kind) =>
            kind.IsQsBody ? "__Body" :
            kind.IsQsAdjoint ? "__Adj" :
            kind.IsQsControlled ? "__Ctl" :
            kind.IsQsControlledAdjoint ? "__CtlAdj" :
            $"__{kind}";

        /// <inheritdoc cref="ReplaceSelfAdjointSpecializations(QsNamespace)" />
        public static QsCompilation ReplaceSelfAdjointSpecializations(QsCompilation compilation)
        {
            var namespaces = compilation.Namespaces.Select(ReplaceSelfAdjointSpecializations).ToImmutableArray();
            return new QsCompilation(namespaces, compilation.EntryPoints);
        }

        /// <inheritdoc cref="LiftIntrinsicSpecializations(QsNamespace)" />
        public static QsCompilation LiftIntrinsicSpecializations(QsCompilation compilation)
        {
            var namespaces = compilation.Namespaces.Select(LiftIntrinsicSpecializations).ToImmutableArray();
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

        // private methods

        private static ArgumentTuple BuildSpecArgTuple(ArgumentTuple callableArg, QsSpecializationKind specKind) =>
            specKind.IsQsControlled || specKind.IsQsControlledAdjoint
                ? SyntaxGenerator.WithControlQubits(
                    callableArg,
                    QsNullable<Position>.Null,
                    QsLocalSymbol.NewValidName(InternalUse.ControlQubitsName),
                    QsNullable<DataTypes.Range>.Null)
                : callableArg;

        private static TypeKind OperationTypeFromSignature(ResolvedSignature signature) =>
            TypeKind.NewOperation(
                Tuple.Create(signature.ArgumentType, signature.ReturnType),
                signature.Information);

        private static TypedExpression IdentifierForCallable(QsQualifiedName cName, TypeKind cType) =>
            SyntaxGenerator.AutoGeneratedExpression(
                ExpressionKind.NewIdentifier(
                    Identifier.NewGlobalCallable(cName),
                    QsNullable<ImmutableArray<ResolvedType>>.Null),
                cType,
                false);

        /// <summary>
        /// Replaces self adjoint generation directives in non-intrinsic callables with a provided implementation
        /// that calls the appropriate specialization of the callable.
        /// Intrinsic callables are left unchanged.
        /// </summary>
        private static QsNamespace ReplaceSelfAdjointSpecializations(QsNamespace ns)
        {
            var elements = ImmutableArray.CreateBuilder<QsNamespaceElement>();
            foreach (var element in ns.Elements)
            {
                if (element is QsNamespaceElement.QsCallable c)
                {
                    if (c.Item.IsSelfAdjoint && !c.Item.IsIntrinsic)
                    {
                        var callableId = IdentifierForCallable(
                            c.Item.FullName,
                            OperationTypeFromSignature(c.Item.Signature));

                        var callable = c.Item.WithSpecializations(specs =>
                            ImmutableArray.CreateRange(specs.Select(spec =>
                            {
                                if (spec.Kind.IsQsBody || spec.Kind.IsQsControlled)
                                {
                                    return spec;
                                }
                                else
                                {
                                    var argTuple = BuildSpecArgTuple(c.Item.ArgumentTuple, spec.Kind);
                                    var callee = spec.Kind.IsQsControlledAdjoint
                                        ? SyntaxGenerator.AutoGeneratedExpression(
                                            ExpressionKind.NewControlledApplication(callableId),
                                            OperationTypeFromSignature(spec.Signature),
                                            false)
                                        : callableId;

                                    var call = SyntaxGenerator.CallNonGeneric(
                                        callee,
                                        SyntaxGenerator.ArgumentTupleAsExpression(argTuple));
                                    var statement = new QsStatement(
                                        QsStatementKind.NewQsReturnStatement(call),
                                        LocalDeclarations.Empty,
                                        QsNullable<QsLocation>.Null,
                                        QsComments.Empty);
                                    var localDeclarations = new LocalDeclarations(
                                        SyntaxGenerator.ValidDeclarations(SyntaxGenerator.ExtractItems(argTuple)));

                                    return spec.WithImplementation(SpecializationImplementation.NewProvided(
                                        argTuple,
                                        new QsScope(ImmutableArray.Create(statement), localDeclarations)));
                                }
                            })));
                        elements.Add(QsNamespaceElement.NewQsCallable(callable));
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

        /// <summary>
        /// Creates a separate callable for each intrinsic specialization,
        /// and replaces the specialization implementations of the original callable with a call to these.
        /// Self adjoint generation directives in intrinsic callables are replaced by a provided implementation.
        /// Type constructors and generic callables or callables that already define a target instruction name are left unchanged.
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
        private static QsNamespace LiftIntrinsicSpecializations(QsNamespace ns)
        {
            var elements = ImmutableArray.CreateBuilder<QsNamespaceElement>();
            foreach (var element in ns.Elements)
            {
                if (element is QsNamespaceElement.QsCallable c
                    && c.Item.Signature.TypeParameters.Length == 0
                    && !c.Item.Kind.IsTypeConstructor)
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
                        else if (callable.Specializations.Length == 1 && callable.Attributes.Any(BuiltIn.DefinesTargetInstruction))
                        {
                            elements.Add(element);
                        }
                        else
                        {
                            QsQualifiedName GeneratedName(QsSpecializationKind kind) =>
                                new QsQualifiedName(callable.FullName.Namespace, $"{callable.FullName.Name}{SpecializationSuffix(kind)}");

                            var specializations = ImmutableArray.CreateRange(callable.Specializations.Select(spec =>
                            {
                                var inferredInfo = spec.Signature.Information.InferredInformation;
                                if (!inferredInfo.IsIntrinsic && !inferredInfo.IsSelfAdjoint)
                                {
                                    throw new ArgumentException("non-intrinsic specialization for intrinsic callable");
                                }

                                // Get the correct argument tuple both for the added intrinsic callable
                                // and the generated provided specialization that replaces the intrinsic one.
                                var argTuple = BuildSpecArgTuple(callable.ArgumentTuple, spec.Kind);

                                // Create a separate callable for that specialization,
                                // unless the specialization is not needed for a self-adjoint callable.

                                var genCallableSignature = new ResolvedSignature(
                                    ImmutableArray<QsLocalSymbol>.Empty,
                                    spec.Signature.ArgumentType,
                                    spec.Signature.ReturnType,
                                    new CallableInformation(
                                        ResolvedCharacteristics.Empty,
                                        new InferredCallableInformation(isIntrinsic: true, isSelfAdjoint: false)));
                                var genCallableName = GeneratedName(
                                    inferredInfo.IsSelfAdjoint && spec.Kind.IsQsAdjoint ? QsSpecializationKind.QsBody :
                                    inferredInfo.IsSelfAdjoint && spec.Kind.IsQsControlledAdjoint ? QsSpecializationKind.QsControlled :
                                    spec.Kind);

                                if (!inferredInfo.IsSelfAdjoint || spec.Kind.IsQsBody || spec.Kind.IsQsControlled)
                                {
                                    var genCallableBody = new QsSpecialization(
                                        QsSpecializationKind.QsBody,
                                        genCallableName,
                                        spec.Attributes,
                                        spec.Source,
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
                                        spec.Source,
                                        spec.Location,
                                        genCallableSignature,
                                        argTuple,
                                        ImmutableArray.Create(genCallableBody),
                                        ImmutableArray<string>.Empty,
                                        QsComments.Empty);
                                    elements.Add(QsNamespaceElement.NewQsCallable(genCallable));
                                }

                                // Create a specialization that calls into the generated callable,
                                // or the corresponding callable no callable for the specialization
                                // has been added due to hte operation being self-adjoint.

                                var genCallableType =
                                    callable.Kind == QsCallableKind.Operation
                                    ? OperationTypeFromSignature(genCallableSignature)
                                    : TypeKind.NewFunction(genCallableSignature.ArgumentType, genCallableSignature.ReturnType);
                                var call = SyntaxGenerator.CallNonGeneric(
                                    IdentifierForCallable(genCallableName, genCallableType),
                                    SyntaxGenerator.ArgumentTupleAsExpression(argTuple));
                                var statement = new QsStatement(
                                    QsStatementKind.NewQsReturnStatement(call),
                                    LocalDeclarations.Empty,
                                    QsNullable<QsLocation>.Null,
                                    QsComments.Empty);
                                var localDeclarations = new LocalDeclarations(
                                    SyntaxGenerator.ValidDeclarations(SyntaxGenerator.ExtractItems(argTuple)));

                                return spec.WithImplementation(SpecializationImplementation.NewProvided(
                                    argTuple,
                                    new QsScope(ImmutableArray.Create(statement), localDeclarations)));
                            }));

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
                                callable.Source,
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
