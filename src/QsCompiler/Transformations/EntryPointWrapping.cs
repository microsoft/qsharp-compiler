// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !__WRAPPER_API__
#define __WRAPPER_API__
#endif

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;

namespace Microsoft.Quantum.QsCompiler.Transformations.EntryPointWrapping
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ParameterTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeParameterResolution = ImmutableArray<Tuple<QsQualifiedName, string, ResolvedType>>;

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
        private static readonly string CaptureVariableLabel = "rtrnVal";

        private static readonly string WrapperAPINamespaceName = "Microsoft.Quantum.Core";

        private static readonly string BooleanRecordOutputName = "BooleanRecordOutput";

        private static readonly string IntegerRecordOutputName = "IntegerRecordOutput";

        private static readonly string DoubleRecordOutputName = "DoubleRecordOutput";

        private static readonly string ResultRecordOutputName = "ResultRecordOutput";

        private static readonly string TupleStartRecordOutputName = "TupleStartRecordOutput";

        private static readonly string TupleEndRecordOutputName = "TupleEndRecordOutput";

        private static readonly string ArrayStartRecordOutputName = "ArrayStartRecordOutput";

        private static readonly string ArrayEndRecordOutputName = "ArrayEndRecordOutput";

        private static QsCallable CreateTypeRecorder(string name, ResolvedTypeKind parameterTypeKind, Source source, bool hasParameter = true)
        {
            var qualifiedName = new QsQualifiedName(WrapperAPINamespaceName, name);
            var sig = new ResolvedSignature(
                ImmutableArray<QsLocalSymbol>.Empty,
                ResolvedType.New(parameterTypeKind),
                ResolvedType.New(ResolvedTypeKind.UnitType),
                CallableInformation.NoInformation);
            var parameterTuple = hasParameter
                ? ParameterTuple.NewQsTuple(new[]
                {
                    ParameterTuple.NewQsTupleItem(
                        new LocalVariableDeclaration<QsLocalSymbol, ResolvedType>(
                            QsLocalSymbol.NewValidName("input"),
                            ResolvedType.New(parameterTypeKind),
                            InferredExpressionInformation.ParameterDeclaration,
                            QsNullable<Position>.Null,
                            DataTypes.Range.Zero)),
                }.ToImmutableArray())
                : ParameterTuple.NewQsTuple(ImmutableArray<ParameterTuple>.Empty);
            return new QsCallable(
                QsCallableKind.Function,
                qualifiedName,
                ImmutableArray<QsDeclarationAttribute>.Empty,
                Access.Public,
                source,
                QsNullable<QsLocation>.Null,
                sig,
                parameterTuple,
                new[]
                {
                    new QsSpecialization(
                        QsSpecializationKind.QsBody,
                        qualifiedName,
                        ImmutableArray<QsDeclarationAttribute>.Empty,
                        source,
                        QsNullable<QsLocation>.Null,
                        QsNullable<ImmutableArray<ResolvedType>>.Null,
                        sig,
                        SpecializationImplementation.Intrinsic,
                        ImmutableArray<string>.Empty,
                        QsComments.Empty),
                }.ToImmutableArray(),
                ImmutableArray<string>.Empty,
                QsComments.Empty);
        }

        private static QsCallable CreateNoParamTypeRecorder(string name, Source source) =>
            CreateTypeRecorder(name, ResolvedTypeKind.UnitType, source, false);

        private static IEnumerable<QsNamespaceElement> CreateWrapperAPI(Source source)
        {
            return new[]
            {
                CreateTypeRecorder(BooleanRecordOutputName, ResolvedTypeKind.Bool, source),
                CreateTypeRecorder(IntegerRecordOutputName, ResolvedTypeKind.Int, source),
                CreateTypeRecorder(DoubleRecordOutputName, ResolvedTypeKind.Double, source),
                CreateTypeRecorder(ResultRecordOutputName, ResolvedTypeKind.Result, source),
                CreateNoParamTypeRecorder(TupleStartRecordOutputName, source),
                CreateNoParamTypeRecorder(TupleEndRecordOutputName, source),
                CreateNoParamTypeRecorder(ArrayStartRecordOutputName, source),
                CreateNoParamTypeRecorder(ArrayEndRecordOutputName, source),
            }
            .Select(x => QsNamespaceElement.NewQsCallable(x));
        }

        private static (QsNamespace, Source) GetNamespaceAndSource(QsCompilation compilation)
        {
            var modifiedCore =
                compilation.Namespaces.
                FirstOrDefault(x => x.Name == WrapperAPINamespaceName) ??
                    new QsNamespace(
                        WrapperAPINamespaceName,
                        ImmutableArray<QsNamespaceElement>.Empty,
                        Enumerable.Empty<ImmutableArray<string>>().ToLookup(x => default(string)));

            var elementInSource =
                modifiedCore.Elements.FirstOrDefault() ?? // Get the first element in the existing API namespace, if it exists
                compilation.Namespaces.First(x => x.Elements.Any()).Elements.First(); // Otherwise get the first element in the first namespace that has elements

            var source = elementInSource is QsNamespaceElement.QsCallable c
                ? c.Item.Source
                : ((QsNamespaceElement.QsCustomType)elementInSource).Item.Source;

            return (modifiedCore, source);
        }

        public static QsCompilation Apply(QsCompilation compilation)
        {
            var (apiNamespace, apiSource) = GetNamespaceAndSource(compilation);

            apiNamespace = apiNamespace.WithElements(elems => elems.AddRange(CreateWrapperAPI(apiSource)).ToImmutableArray());

            compilation = new QsCompilation(
                compilation.Namespaces
                    .Where(x => x.Name != apiNamespace.Name)
                    .Append(apiNamespace)
                    .ToImmutableArray(),
                compilation.EntryPoints);

            var filter = new WrapEntryPoints();
            compilation = filter.OnCompilation(compilation);
            return new QsCompilation(compilation.Namespaces, filter.SharedState.EntryPointNames.ToImmutableArray());
        }

        private static QsCallable MakeWrapper(QsCallable c)
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

            return wrapper.WithSpecializations(_ => new[] { MakeWrapperBody(wrapper, c) }.ToImmutableArray());
        }

        private static QsSpecialization MakeWrapperBody(QsCallable wrapper, QsCallable original)
        {
            return new QsSpecialization(
                QsSpecializationKind.QsBody,
                wrapper.FullName,
                ImmutableArray<QsDeclarationAttribute>.Empty,
                wrapper.Source,
                wrapper.Location,
                QsNullable<ImmutableArray<ResolvedType>>.Null,
                wrapper.Signature,
                SpecializationImplementation.NewProvided(
                    wrapper.ArgumentTuple,
                    new QsScope(
                        MakeStatements(original),
                        new LocalDeclarations(wrapper.ArgumentTuple.FlattenTuple()
                            .Select(decl => new LocalVariableDeclaration<string, ResolvedType>(
                                ((QsLocalSymbol.ValidName)decl.VariableName).Item,
                                decl.Type,
                                decl.InferredInformation,
                                decl.Position,
                                decl.Range))
                            .ToImmutableArray()))),
                wrapper.Documentation,
                wrapper.Comments);
        }

        private static TypedExpression ParameterTupleToValueTuple(ParameterTuple parameters)
        {
            if (parameters is ParameterTuple.QsTuple tuple)
            {
                if (tuple.Item.Count() == 1)
                {
                    return ParameterTupleToValueTuple(tuple.Item.First());
                }
                else if (tuple.Item.Count() > 1)
                {
                    var items = tuple.Item.Select(ParameterTupleToValueTuple).ToImmutableArray();
                    return new TypedExpression(
                        ExpressionKind.NewValueTuple(items),
                        TypeParameterResolution.Empty,
                        ResolvedType.New(ResolvedTypeKind.NewTupleType(items.Select(i => i.ResolvedType).ToImmutableArray())),
                        InferredExpressionInformation.ParameterDeclaration,
                        QsNullable<DataTypes.Range>.Null);
                }
                else
                {
                    return new TypedExpression(
                        ExpressionKind.UnitValue,
                        TypeParameterResolution.Empty,
                        ResolvedType.New(ResolvedTypeKind.UnitType),
                        InferredExpressionInformation.ParameterDeclaration,
                        QsNullable<DataTypes.Range>.Null);
                }
            }
            else if (parameters is ParameterTuple.QsTupleItem item && item.Item.VariableName is QsLocalSymbol.ValidName name)
            {
                return new TypedExpression(
                    ExpressionKind.NewIdentifier(Identifier.NewLocalVariable(name.Item), QsNullable<ImmutableArray<ResolvedType>>.Null),
                    TypeParameterResolution.Empty,
                    item.Item.Type,
                    InferredExpressionInformation.ParameterDeclaration,
                    QsNullable<DataTypes.Range>.Null);
            }

            throw new ArgumentException("Encountered invalid variable name during Entry Point Wrapping transformation.");
        }

        private static string MakeVariableName(int enumeration) =>
            $"__{CaptureVariableLabel}{enumeration}__";

        private static (SymbolTuple, Dictionary<string, ResolvedType>) MakeCaptureVariables(ResolvedType returnType, int enumerationStart = 0)
        {
            var newVars = new Dictionary<string, ResolvedType>();

            SymbolTuple MakeCapture(ResolvedType t)
            {
                if (t.Resolution is ResolvedTypeKind.TupleType tup)
                {
                    return SymbolTuple.NewVariableNameTuple(tup.Item.Select(MakeCapture).ToImmutableArray());
                }
                else
                {
                    var newName = MakeVariableName(enumerationStart);
                    enumerationStart++;
                    newVars.Add(newName, t);
                    return SymbolTuple.NewVariableName(newName);
                }
            }

            return (MakeCapture(returnType), newVars);
        }

        private static QsStatement MakeCaptureStatement(QsCallable original, SymbolTuple captureVars, LocalDeclarations localDeclarations)
        {
            var callId = new TypedExpression(
                ExpressionKind.NewIdentifier(
                    Identifier.NewGlobalCallable(original.FullName),
                    QsNullable<ImmutableArray<ResolvedType>>.Null),
                TypeParameterResolution.Empty,
                ResolvedType.New(
                    ResolvedTypeKind.NewOperation(
                        Tuple.Create(original.Signature.ArgumentType, original.Signature.ReturnType),
                        original.Signature.Information)),
                new InferredExpressionInformation(false, false),
                QsNullable<DataTypes.Range>.Null);
            var callArgs = ParameterTupleToValueTuple(original.ArgumentTuple);
            var call = new TypedExpression(
                ExpressionKind.NewCallLikeExpression(callId, callArgs),
                TypeParameterResolution.Empty,
                original.Signature.ReturnType,
                new InferredExpressionInformation(false, false),
                QsNullable<DataTypes.Range>.Null);

            return new QsStatement(
                QsStatementKind.NewQsVariableDeclaration(new QsBinding<TypedExpression>(
                    QsBindingKind.ImmutableBinding,
                    captureVars,
                    call)),
                localDeclarations,
                QsNullable<QsLocation>.Null,
                QsComments.Empty);
        }

        private static ImmutableArray<QsStatement> MakeStatements(QsCallable original)
        {
            IEnumerable<QsStatement> ProcessCapture(SymbolTuple captureTuple, Dictionary<string, ResolvedType> captureTypes)
            {
                var statements = new List<QsStatement>();

                if (captureTuple is SymbolTuple.VariableNameTuple tup)
                {
                    statements.Add(RecordStartTuple());

                    foreach (var i in tup.Item)
                    {
                        statements.AddRange(ProcessCapture(i, captureTypes));
                    }

                    statements.Add(RecordEndTuple());
                }
                else if (captureTuple is SymbolTuple.VariableName name)
                {
                    var captureType = captureTypes[name.Item];
                    if (captureType.Resolution.IsBool)
                    {
                        statements.Add(RecordBool(name.Item));
                    }
                    else if (captureType.Resolution.IsInt)
                    {
                        statements.Add(RecordInt(name.Item));
                    }
                    else if (captureType.Resolution.IsDouble)
                    {
                        statements.Add(RecordDouble(name.Item));
                    }
                    else if (captureType.Resolution.IsResult)
                    {
                        statements.Add(RecordResult(name.Item));
                    }
                    else if (captureType.Resolution is ResolvedTypeKind.ArrayType arr)
                    {
                        statements.Add(RecordStartArray());

                        var (forCaptureVars, forCaptureTypes) = MakeCaptureVariables(arr.Item, captureTypes.Count());

                        // Merged dictionary of types for all known variables up to this point.
                        var merged = new[] { captureTypes, forCaptureTypes }
                            .SelectMany(dict => dict)
                            .ToDictionary(pair => pair.Key, pair => pair.Value);

                        var forBodyStatements = ProcessCapture(forCaptureVars, merged);

                        var knownSymbols = merged
                            .Select(kvp => new LocalVariableDeclaration<string, ResolvedType>(
                                kvp.Key,
                                kvp.Value,
                                InferredExpressionInformation.ParameterDeclaration,
                                QsNullable<Position>.Null,
                                DataTypes.Range.Zero))
                            .Concat(original.ArgumentTuple.FlattenTuple()
                                .Select(decl => new LocalVariableDeclaration<string, ResolvedType>(
                                    ((QsLocalSymbol.ValidName)decl.VariableName).Item,
                                    decl.Type,
                                    decl.InferredInformation,
                                    decl.Position,
                                    decl.Range)))
                            .ToImmutableArray();

                        var forStatement = new QsStatement(
                            QsStatementKind.NewQsForStatement(new QsForStatement(
                                Tuple.Create(forCaptureVars, arr.Item),
                                new TypedExpression(
                                    ExpressionKind.NewIdentifier(
                                        Identifier.NewLocalVariable(name.Item),
                                        QsNullable<ImmutableArray<ResolvedType>>.Null),
                                    TypeParameterResolution.Empty,
                                    captureType,
                                    InferredExpressionInformation.ParameterDeclaration,
                                    QsNullable<DataTypes.Range>.Null),
                                new QsScope(
                                    forBodyStatements.ToImmutableArray(),
                                    new LocalDeclarations(knownSymbols.ToImmutableArray())))),
                            LocalDeclarations.Empty,
                            QsNullable<QsLocation>.Null,
                            QsComments.Empty);

                        statements.Add(forStatement);
                        statements.Add(RecordEndArray());
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid return type for Entry Point Wrapping: {captureType.Resolution}");
                    }
                }

                return statements;
            }

            var (captureVars, captureVarsDict) = MakeCaptureVariables(original.Signature.ReturnType);
            var captureStatement = MakeCaptureStatement(
                original,
                captureVars,
                new LocalDeclarations(captureVarsDict
                    .Select(kvp => new LocalVariableDeclaration<string, ResolvedType>(
                        kvp.Key,
                        kvp.Value,
                        InferredExpressionInformation.ParameterDeclaration,
                        QsNullable<Position>.Null,
                        DataTypes.Range.Zero))
                    .ToImmutableArray()));
            var statements = new List<QsStatement>() { captureStatement };

            statements.AddRange(ProcessCapture(captureVars, captureVarsDict));

            return statements.ToImmutableArray();
        }

#if __WRAPPER_API__

        private static QsStatement MakeAPICall(string callName, ResolvedTypeKind parameterTypeKind, string parameterName, bool hasParameter = true)
        {
            var parameter = new TypedExpression(
                hasParameter
                ? ExpressionKind.NewIdentifier(Identifier.NewLocalVariable(parameterName), QsNullable<ImmutableArray<ResolvedType>>.Null)
                : ExpressionKind.UnitValue,
                TypeParameterResolution.Empty,
                ResolvedType.New(parameterTypeKind),
                new InferredExpressionInformation(false, false),
                QsNullable<DataTypes.Range>.Null);

            return new QsStatement(
                QsStatementKind.NewQsExpressionStatement(new TypedExpression(
                    ExpressionKind.NewCallLikeExpression(
                        new TypedExpression(
                            ExpressionKind.NewIdentifier(
                                Identifier.NewGlobalCallable(new QsQualifiedName(WrapperAPINamespaceName, callName)),
                                QsNullable<ImmutableArray<ResolvedType>>.Null),
                            TypeParameterResolution.Empty,
                            ResolvedType.New(ResolvedTypeKind.NewFunction(
                                ResolvedType.New(parameterTypeKind),
                                ResolvedType.New(ResolvedTypeKind.UnitType))),
                            new InferredExpressionInformation(false, false),
                            QsNullable<DataTypes.Range>.Null),
                        parameter),
                    TypeParameterResolution.Empty,
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    new InferredExpressionInformation(false, false),
                    QsNullable<DataTypes.Range>.Null)),
                LocalDeclarations.Empty,
                QsNullable<QsLocation>.Null,
                QsComments.Empty);
        }

        private static QsStatement MakeNoArgAPICall(string callName) =>
            MakeAPICall(callName, ResolvedTypeKind.UnitType, "", false);

        private static QsStatement RecordStartTuple() =>
            MakeNoArgAPICall(TupleStartRecordOutputName);

        private static QsStatement RecordEndTuple() =>
            MakeNoArgAPICall(TupleEndRecordOutputName);

        private static QsStatement RecordStartArray() =>
            MakeNoArgAPICall(ArrayStartRecordOutputName);

        private static QsStatement RecordEndArray() =>
            MakeNoArgAPICall(ArrayEndRecordOutputName);

        private static QsStatement RecordBool(string name) =>
            MakeAPICall(BooleanRecordOutputName, ResolvedTypeKind.Bool, name);

        private static QsStatement RecordInt(string name) =>
            MakeAPICall(IntegerRecordOutputName, ResolvedTypeKind.Int, name);

        private static QsStatement RecordDouble(string name) =>
            MakeAPICall(DoubleRecordOutputName, ResolvedTypeKind.Double, name);

        private static QsStatement RecordResult(string name) =>
            MakeAPICall(ResultRecordOutputName, ResolvedTypeKind.Result, name);

#else

        private static QsStatement MakeMessageCall(string message, ImmutableArray<TypedExpression> interpolatedExpressions)
        {
            return new QsStatement(
                QsStatementKind.NewQsExpressionStatement(new TypedExpression(
                    ExpressionKind.NewCallLikeExpression(
                        new TypedExpression(
                            ExpressionKind.NewIdentifier(
                                Identifier.NewGlobalCallable(BuiltIn.Message.FullName),
                                QsNullable<ImmutableArray<ResolvedType>>.Null),
                            TypeParameterResolution.Empty,
                            ResolvedType.New(ResolvedTypeKind.NewFunction(
                                ResolvedType.New(ResolvedTypeKind.String),
                                ResolvedType.New(ResolvedTypeKind.UnitType))),
                            new InferredExpressionInformation(false, false),
                            QsNullable<DataTypes.Range>.Null),
                        new TypedExpression(
                            ExpressionKind.NewStringLiteral(message, interpolatedExpressions),
                            TypeParameterResolution.Empty,
                            ResolvedType.New(ResolvedTypeKind.String),
                            new InferredExpressionInformation(false, false),
                            QsNullable<DataTypes.Range>.Null)),
                    TypeParameterResolution.Empty,
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    new InferredExpressionInformation(false, false),
                    QsNullable<DataTypes.Range>.Null)),
                LocalDeclarations.Empty,
                QsNullable<QsLocation>.Null,
                QsComments.Empty);
        }

        private static QsStatement RecordStartTuple() =>
            MakeMessageCall("Tuple Start", ImmutableArray<TypedExpression>.Empty);

        private static QsStatement RecordEndTuple() =>
            MakeMessageCall("Tuple End", ImmutableArray<TypedExpression>.Empty);

        private static QsStatement RecordStartArray() =>
            MakeMessageCall("Array Start", ImmutableArray<TypedExpression>.Empty);

        private static QsStatement RecordEndArray() =>
            MakeMessageCall("Array End", ImmutableArray<TypedExpression>.Empty);

        private static QsStatement RecordBool(string name)
        {
            var id = new TypedExpression(
                ExpressionKind.NewIdentifier(Identifier.NewLocalVariable(name), QsNullable<ImmutableArray<ResolvedType>>.Null),
                TypeParameterResolution.Empty,
                ResolvedType.New(ResolvedTypeKind.Bool),
                new InferredExpressionInformation(false, false),
                QsNullable<DataTypes.Range>.Null);

            return MakeMessageCall("Bool: {0}", new[] { id }.ToImmutableArray());
        }

        private static QsStatement RecordInt(string name)
        {
            var id = new TypedExpression(
                ExpressionKind.NewIdentifier(Identifier.NewLocalVariable(name), QsNullable<ImmutableArray<ResolvedType>>.Null),
                TypeParameterResolution.Empty,
                ResolvedType.New(ResolvedTypeKind.Int),
                new InferredExpressionInformation(false, false),
                QsNullable<DataTypes.Range>.Null);

            return MakeMessageCall("Int: {0}", new[] { id }.ToImmutableArray());
        }

        private static QsStatement RecordDouble(string name)
        {
            var id = new TypedExpression(
                ExpressionKind.NewIdentifier(Identifier.NewLocalVariable(name), QsNullable<ImmutableArray<ResolvedType>>.Null),
                TypeParameterResolution.Empty,
                ResolvedType.New(ResolvedTypeKind.Double),
                new InferredExpressionInformation(false, false),
                QsNullable<DataTypes.Range>.Null);

            return MakeMessageCall("Double: {0}", new[] { id }.ToImmutableArray());
        }

        private static QsStatement RecordResult(string name)
        {
            var id = new TypedExpression(
                ExpressionKind.NewIdentifier(Identifier.NewLocalVariable(name), QsNullable<ImmutableArray<ResolvedType>>.Null),
                TypeParameterResolution.Empty,
                ResolvedType.New(ResolvedTypeKind.Result),
                new InferredExpressionInformation(false, false),
                QsNullable<DataTypes.Range>.Null);

            return MakeMessageCall("Result: {0}", new[] { id }.ToImmutableArray());
        }

#endif

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
                            var wrapper = MakeWrapper(c);
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

                public override QsNamespace OnNamespace(QsNamespace ns)
                {
                    this.SharedState.NewEntryPointWrappers.Clear();
                    ns = base.OnNamespace(ns);
                    var newElements = this.SharedState.NewEntryPointWrappers.Select(e => QsNamespaceElement.NewQsCallable(e));
                    return ns.WithElements(elements => elements.AddRange(newElements));
                }
            }
        }
    }
}
