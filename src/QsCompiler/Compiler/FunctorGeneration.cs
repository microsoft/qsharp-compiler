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
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler
{
    public static class CodeGeneration
    {
        /// <summary>
        /// Given the argument tuple of a specialization, returns the argument tuple for its controlled version.
        /// Returns null if the given argument tuple is null.
        /// </summary>
        private static QsTuple<LocalVariableDeclaration<QsLocalSymbol>>? ControlledArg(QsTuple<LocalVariableDeclaration<QsLocalSymbol>> arg) =>
            arg != null
            ? SyntaxGenerator.WithControlQubits(
                arg,
                QsNullable<Position>.Null,
                QsLocalSymbol.NewValidName(InternalUse.ControlQubitsName),
                QsNullable<Range>.Null)
            : null;

        /// <summary>
        /// Given a sequence of specializations, returns the implementation of the given kind, or null if no such specialization exists.
        /// </summary>
        /// <exception cref="ArgumentException">More than one specialization exists for <paramref name="kind"/>.</exception>
        private static QsSpecialization? GetSpecialization(this IEnumerable<QsSpecialization> specs, QsSpecializationKind kind)
        {
            specs = specs.Where(spec => spec.Kind == kind);
            if (specs.Count() > 1)
            {
                throw new ArgumentException("several specializations of the given kind exist");
            }
            return specs.Any() ? specs.Single() : null;
        }

        /// <summary>
        /// Return the argument tuple as well as the QsScope if the implementation is provided.
        /// Returns null if the given implementation is null or not provided.
        /// </summary>
        private static (QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope)? GetContent(this SpecializationImplementation? impl) =>
            impl is SpecializationImplementation.Provided content
                ? (content.Item1, content.Item2)
                : ((QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope)?)null;

        /// <summary>
        /// If an implementation for the body of the given callable is provided,
        /// returns the provided implementation as well as its argument tuple as value.
        /// Returns null if the body specialization is intrinsic or external, or if the given callable is null.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// More than one body specialization exists, or <paramref name="callable"/> is not intrinsic or external and the implementation for the body is not provided.
        /// </exception>
        private static (QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope)? BodyImplementation(this QsCallable callable)
        {
            if (callable == null || callable.Kind.IsTypeConstructor)
            {
                return null;
            }
            var noBodyException = new ArgumentException("no implementation provided for body");
            var bodyDecl = callable.Specializations.GetSpecialization(QsSpecializationKind.QsBody)?.Implementation ?? throw noBodyException;
            if (bodyDecl.IsGenerated)
            {
                throw new ArgumentException("functor generator directive on body specialization");
            }
            if (bodyDecl.IsExternal || bodyDecl.IsIntrinsic)
            {
                return null;
            }
            return GetContent(bodyDecl) ?? throw noBodyException;
        }

        /// <summary>
        /// Given a Q# callable, evaluates any functor generator directive given for its adjoint specialization.
        /// If the body specialization is either intrinsic or external, return the given callable unchanged.
        /// Otherwise returns a new QsCallable with the adjoint specialization set to the generated implementation if it was generated,
        /// or set to the original specialization if the specialization was not requested to be auto-generated.
        /// Only valid functor generator directives are evaluated, anything else remains unmodified.
        /// The directives 'invert' and 'self' are considered to be valid functor generator directives.
        /// Assumes that operation calls may only ever occur within expression statements.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="callable"/> is not intrinsic or external and the implementation for the body is not provided.</exception>
        /// <exception cref="ArgumentException">More than one body or adjoint specialization exists.</exception>
        private static QsCallable BuildAdjoint(this QsCallable callable)
        {
            var bodyDecl = BodyImplementation(callable);
            if (bodyDecl == null)
            {
                return callable;
            }

            var adj = callable.Specializations.GetSpecialization(QsSpecializationKind.QsAdjoint);
            if (adj != null && adj.Implementation is SpecializationImplementation.Generated gen)
            {
                var (bodyArg, bodyImpl) = bodyDecl.Value;
                void SetImplementation(QsScope impl) => adj = adj.WithImplementation(SpecializationImplementation.NewProvided(bodyArg, impl));

                // if (gen.Item.IsSelfInverse) SetImplementation(bodyImpl); -> nothing to do here, we want to keep this information
                if (gen.Item.IsInvert)
                {
                    SetImplementation(bodyImpl.GenerateAdjoint());
                }
            }
            return callable.WithSpecializations(specs => specs.Select(s => s.Kind == QsSpecializationKind.QsAdjoint ? adj : s).ToImmutableArray());
        }

        /// <summary>
        /// Given a Q# callable, evaluates any functor generator directive given for its controlled specialization.
        /// If the body specialization is either intrinsic or external, return the given callable unchanged.
        /// Otherwise returns a new QsCallable with the controlled specialization set to the generated implementation if it was generated,
        /// or set to the original specialization if the specialization was not requested to be auto-generated.
        /// Only valid functor generator directives are evaluated, anything else remains unmodified.
        /// The directive 'distributed' is the only directive considered to be valid.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="callable"/> is not intrinsic or external and the implementation for the body is not provided.</exception>
        /// <exception cref="ArgumentException">More than one body or controlled specialization exists.</exception>
        private static QsCallable BuildControlled(this QsCallable callable)
        {
            var bodyDecl = BodyImplementation(callable);
            if (bodyDecl == null)
            {
                return callable;
            }

            var ctl = callable.Specializations.GetSpecialization(QsSpecializationKind.QsControlled);
            if (ctl != null && ctl.Implementation is SpecializationImplementation.Generated gen)
            {
                var (bodyArg, bodyImpl) = bodyDecl.Value;
                void SetImplementation(QsScope impl) => ctl = ctl.WithImplementation(SpecializationImplementation.NewProvided(ControlledArg(bodyArg), impl));
                if (gen.Item.IsDistribute)
                {
                    SetImplementation(bodyImpl.GenerateControlled());
                }
            }
            return callable.WithSpecializations(specs => specs.Select(s => s.Kind == QsSpecializationKind.QsControlled ? ctl : s).ToImmutableArray());
        }

        /// <summary>
        /// Given a Q# callable, evaluates any functor generator directive given for its controlled adjoint specialization.
        /// If the body specialization is either intrinsic or external, return the given callable unchanged.
        /// Otherwise returns a new QsCallable with the controlled adjoint specialization set to the generated implementation if it was generated,
        /// or set to the original specialization if the specialization was not requested to be auto-generated.
        /// Only valid functor generator directives are evaluated, anything else remains unmodified.
        /// The directives 'invert', 'self', and 'distributed' are considered to be valid functor generator directives.
        /// Assumes that if the controlled adjoint version is to be generated based on the controlled version,
        /// operation calls may only ever occur within expression statements.
        /// </summary>
        /// <exception cref="ArgumentException">More than one body, adjoint or controlled specialization (depending on the generator directive) exists.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="callable"/> is not intrinsic or external and the implementation for the body is not provided, or the implementation for the adjoint
        /// or controlled specialization (depending on the generator directive) is not provided.
        /// </exception>
        private static QsCallable BuildControlledAdjoint(this QsCallable callable)
        {
            var bodyDecl = BodyImplementation(callable);
            if (bodyDecl == null)
            {
                return callable;
            }

            var ctlAdj = callable.Specializations.GetSpecialization(QsSpecializationKind.QsControlledAdjoint);
            if (ctlAdj != null && ctlAdj.Implementation is SpecializationImplementation.Generated gen)
            {
                if (gen.Item.IsDistribute)
                {
                    var adj = callable.Specializations.GetSpecialization(QsSpecializationKind.QsAdjoint);
                    var (adjArg, adjImpl) = GetContent(adj?.Implementation) ?? throw new ArgumentException("no implementation provided for adjoint specialization");
                    ctlAdj = ctlAdj.WithImplementation(SpecializationImplementation.NewProvided(ControlledArg(adjArg), adjImpl.GenerateControlled()));
                }
                else
                {
                    var ctl = callable.Specializations.GetSpecialization(QsSpecializationKind.QsControlled);
                    var (ctlArg, ctlImpl) = GetContent(ctl?.Implementation) ?? throw new ArgumentException("no implementation provided for controlled specialization");
                    if (gen.Item.IsInvert)
                    {
                        ctlAdj = ctlAdj.WithImplementation(SpecializationImplementation.NewProvided(ctlArg, ctlImpl.GenerateAdjoint()));
                    }
                }
            }
            return callable.WithSpecializations(specs => specs.Select(s => s.Kind == QsSpecializationKind.QsControlledAdjoint ? ctlAdj : s).ToImmutableArray());
        }

        /// <summary>
        /// Given a Q# compilation, evaluates all functor generator directives and
        /// builds a new compilation where the corresponding elements in the syntax tree are replaced by the generated implementations.
        /// If the evaluation fails, the corresponding callable is left unchanged in the new tree.
        /// This is the case e.g. if more than one specialization of the same kind exists for a callable which causes an ArgumentException to be thrown.
        /// Any thrown exception is logged using the given onException action and are silently ignored if onException is not specified or null.
        /// Returns a boolean indicating if the evaluation of all directives was successful.
        /// </summary>
        public static bool GenerateFunctorSpecializations(QsCompilation compilation, out QsCompilation built, Action<Exception>? onException = null)
        {
            var success = true;
            var namespaces = compilation.Namespaces.Where(ns => ns != null).Select(ns =>
            {
                var elements = ns.Elements.Select(element =>
                {
                    if (element is QsNamespaceElement.QsCallable callableDecl)
                    {
                        var callable = callableDecl.Item;
                        try
                        {
                            callable = callable.BuildControlled();
                            callable = callable.BuildAdjoint();
                            callable = callable.BuildControlledAdjoint();
                            return QsNamespaceElement.NewQsCallable(callable);
                        }
                        catch (Exception ex)
                        {
                            success = false;
                            onException?.Invoke(ex);
                            return QsNamespaceElement.NewQsCallable(callableDecl.Item);
                        }
                    }
                    else
                    {
                        return element;
                    }
                });
                return new QsNamespace(ns.Name, elements.ToImmutableArray(), ns.Documentation);
            });
            built = new QsCompilation(namespaces.ToImmutableArray(), compilation.EntryPoints);
            return success;
        }
    }
}
