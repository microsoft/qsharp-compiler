// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;

namespace Microsoft.Quantum.QsCompiler.Transformations.EntryPointWrapping
{
    using GetConcreteIdentifierFunc = Func<Identifier.GlobalCallable, /*TypeParameterResolutions*/ ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType>, Identifier>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType>;

    /// <summary>
    /// This transformation replaces callables with type parameters with concrete
    /// instances of the same callables. The concrete values for the type parameters
    /// are found from uses of the callables.
    /// This transformation also removes all callables that are not used directly or
    /// indirectly from any of the marked entry point.
    /// Monomorphizing intrinsic callables is optional and intrinsics can be prevented
    /// from being monomorphized if the monomorphizeIntrinsics parameter is set to false.
    /// There are also some built-in callables that are also exempt from
    /// being removed from non-use, as they are needed for later rewrite steps.
    /// </summary>
    public static class EntryPointWrapping
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            var filter = new WrapEntryPoints();
            return filter.OnCompilation(compilation);
        }

        private class WrapEntryPoints :
            SyntaxTreeTransformation<WrapEntryPoints.TransformationState>
        {
            public class TransformationState
            {
                public List<QsQualifiedName> EntryPointNames { get; } = new List<QsQualifiedName>();

                public List<QsCallable> NewEntryPointWrappers { get; } = new List<QsCallable>();
            }

            public WrapEntryPoints()
                : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.ExpressionKinds = new ExpressionKindTransformation<TransformationState>(this, TransformationOptions.Disabled);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                private QsCallable MakeWrapper(QsCallable c)
                {
                    var wrapperSignature = new ResolvedSignature(
                        ImmutableArray<QsLocalSymbol>.Empty,
                        c.Signature.ArgumentType,
                        ResolvedType.New(ResolvedTypeKind.UnitType),
                        new CallableInformation(ResolvedCharacteristics.Empty, InferredCallableInformation.NoInformation));

                    var wrapper = new QsCallable(
                        c.Kind,
                        NameDecorator.PrependGuid(c.FullName),
                        c.Attributes.Where(BuiltIn.MarksEntryPoint).ToImmutableArray(),
                        c.Access,
                        c.Source,
                        c.Location,
                        wrapperSignature,
                        c.ArgumentTuple,
                        ImmutableArray<QsSpecialization>.Empty, // we will make the body later
                        c.Documentation,
                        c.Comments);

                    return wrapper.WithSpecializations(_ => new[] { this.MakeWrapperBody(wrapper) }.ToImmutableArray());
                }

                // ToDo
                private QsSpecialization MakeWrapperBody(QsCallable parent)
                {
                    return new QsSpecialization(
                        QsSpecializationKind.QsBody,
                        parent.FullName,
                        ImmutableArray<QsDeclarationAttribute>.Empty,
                        parent.Source,
                        parent.Location,
                        QsNullable<ImmutableArray<ResolvedType>>.Null,
                        parent.Signature,
                        SpecializationImplementation.NewProvided(
                            parent.ArgumentTuple,
                            new QsScope(
                                ImmutableArray<QsStatement>.Empty, // ToDo
                                new LocalDeclarations(parent.ArgumentTuple.FlattenTuple()
                                    .Select(decl => new LocalVariableDeclaration<string, ResolvedType>(
                                        ((QsLocalSymbol.ValidName)decl.VariableName).Item,
                                        decl.Type,
                                        decl.InferredInformation,
                                        decl.Position,
                                        decl.Range))
                                    .ToImmutableArray()))),
                        parent.Documentation,
                        parent.Comments);
                }

                public override QsCallable OnCallableDeclaration(QsCallable c)
                {
                    if (c.Attributes.Any(BuiltIn.MarksEntryPoint))
                    {
                        if (c.Signature.ReturnType.Resolution.IsUnitType)
                        {
                            this.SharedState.EntryPointNames.Add(c.FullName);
                            return c;
                        }
                        else
                        {
                            var wrapper = this.MakeWrapper(c);
                            this.SharedState.EntryPointNames.Add(wrapper.FullName);
                            this.SharedState.NewEntryPointWrappers.Add(wrapper);
                            return new QsCallable(
                                c.Kind,
                                c.FullName,
                                c.Attributes.Where(a => !BuiltIn.MarksEntryPoint(a)).ToImmutableArray(), // remove EntryPoint attribute
                                c.Access,
                                c.Source,
                                c.Location,
                                c.Signature,
                                c.ArgumentTuple,
                                c.Specializations,
                                c.Documentation,
                                c.Comments);
                        }
                    }
                    else
                    {
                        return c;
                    }
                }
            }
        }
    }
}
