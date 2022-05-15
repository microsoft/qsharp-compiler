// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !__WRAPPER_API__
#define __WRAPPER_API__
#endif

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;

namespace Microsoft.Quantum.QsCompiler.Transformations
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
        private static readonly string ReturnValueVariableLabel = "rtrnVal";

        private static readonly string OutputRecordersNamespaceName = "Microsoft.Quantum.Core";

        private static readonly string BooleanRecordOutputName = "BooleanRecordOutput";

        private static readonly string IntegerRecordOutputName = "IntegerRecordOutput";

        private static readonly string DoubleRecordOutputName = "DoubleRecordOutput";

        private static readonly string ResultRecordOutputName = "ResultRecordOutput";

        private static readonly string TupleStartRecordOutputName = "TupleStartRecordOutput";

        private static readonly string TupleEndRecordOutputName = "TupleEndRecordOutput";

        private static readonly string ArrayStartRecordOutputName = "ArrayStartRecordOutput";

        private static readonly string ArrayEndRecordOutputName = "ArrayEndRecordOutput";

        // FIXME: CREATE ONLY IF IT DOESN'T EXIST
        private static QsCallable CreateOutputRecorder(string name, ResolvedTypeKind parameterTypeKind, Source source)
        {
            var qualifiedName = new QsQualifiedName(OutputRecordersNamespaceName, name);
            var sig = new ResolvedSignature(
                ImmutableArray<QsLocalSymbol>.Empty,
                ResolvedType.New(parameterTypeKind),
                ResolvedType.New(ResolvedTypeKind.UnitType),
                CallableInformation.NoInformation);
            var parameterTuple = parameterTypeKind.IsUnitType
                ? ParameterTuple.NewQsTuple(ImmutableArray.Create(
                    ParameterTuple.NewQsTupleItem(
                        new LocalVariableDeclaration<QsLocalSymbol, ResolvedType>(
                            QsLocalSymbol.NewValidName("input"),
                            ResolvedType.New(parameterTypeKind),
                            InferredExpressionInformation.ParameterDeclaration,
                            QsNullable<Position>.Null,
                            DataTypes.Range.Zero))))
                : ParameterTuple.NewQsTuple(ImmutableArray<ParameterTuple>.Empty);
            return new QsCallable(
                QsCallableKind.Function,
                qualifiedName,
                ImmutableArray<QsDeclarationAttribute>.Empty,
                Access.Internal,
                source,
                QsNullable<QsLocation>.Null,
                sig,
                parameterTuple,
                ImmutableArray.Create(
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
                        QsComments.Empty)),
                ImmutableArray<string>.Empty,
                QsComments.Empty);
        }

        private static QsCallable CreateOutputRecorder(string name, Source source) =>
            CreateOutputRecorder(name, ResolvedTypeKind.UnitType, source);

        private static IEnumerable<QsCallable> CreateOutputRecorderAPI(Source source) => new[]
            {
                CreateOutputRecorder(BooleanRecordOutputName, ResolvedTypeKind.Bool, source),
                CreateOutputRecorder(IntegerRecordOutputName, ResolvedTypeKind.Int, source),
                CreateOutputRecorder(DoubleRecordOutputName, ResolvedTypeKind.Double, source),
                CreateOutputRecorder(ResultRecordOutputName, ResolvedTypeKind.Result, source),
                CreateOutputRecorder(TupleStartRecordOutputName, source),
                CreateOutputRecorder(TupleEndRecordOutputName, source),
                CreateOutputRecorder(ArrayStartRecordOutputName, source),
                CreateOutputRecorder(ArrayEndRecordOutputName, source),
            };

        public static QsCompilation Apply(QsCompilation compilation)
        {
            if (compilation.EntryPoints.Length == 0)
            {
                return compilation;
            }

            var apiNamespace =
                compilation.Namespaces.FirstOrDefault(x => x.Name == OutputRecordersNamespaceName)
                ?? new QsNamespace(
                    OutputRecordersNamespaceName,
                    ImmutableArray<QsNamespaceElement>.Empty,
                    Enumerable.Empty<ImmutableArray<string>>().ToLookup(x => default(string)));

            var apiSource = new Source(Path.GetTempFileName(), QsNullable<string>.Null);
            apiNamespace = apiNamespace.WithElements(elems =>
                elems.AddRange(CreateOutputRecorderAPI(apiSource).Select(QsNamespaceElement.NewQsCallable)).ToImmutableArray());

            compilation = new QsCompilation(
                compilation.Namespaces
                    .Where(x => x.Name != apiNamespace.Name)
                    .Append(apiNamespace)
                    .ToImmutableArray(),
                compilation.EntryPoints);

            return new WrapEntryPoints().OnCompilation(compilation);
        }

        private static IEnumerable<QsStatement> RecordOutput(ParameterTuple argTuple, SymbolTuple outputTuple, Dictionary<string, ResolvedType> variableTypes)
        {
            var statements = new List<QsStatement>();

            if (outputTuple is SymbolTuple.VariableNameTuple tup)
            {
                statements.Add(RecordStartTuple());

                foreach (var i in tup.Item)
                {
                    statements.AddRange(RecordOutput(argTuple, i, variableTypes));
                }

                statements.Add(RecordEndTuple());
            }
            else if (outputTuple is SymbolTuple.VariableName name)
            {
                var variableType = variableTypes[name.Item];
                if (variableType.Resolution.IsBool)
                {
                    statements.Add(RecordBool(name.Item));
                }
                else if (variableType.Resolution.IsInt)
                {
                    statements.Add(RecordInt(name.Item));
                }
                else if (variableType.Resolution.IsDouble)
                {
                    statements.Add(RecordDouble(name.Item));
                }
                else if (variableType.Resolution.IsResult)
                {
                    statements.Add(RecordResult(name.Item));
                }
                else if (variableType.Resolution is ResolvedTypeKind.ArrayType arr)
                {
                    statements.Add(RecordStartArray());

                    var (iterVars, iterVarTypes) = CreateDeconstruction(arr.Item, variableTypes.Count());

                    // Merged dictionary of types for all known variables up to this point.
                    var merged = new[] { variableTypes, iterVarTypes }
                        .SelectMany(dict => dict)
                        .ToDictionary(pair => pair.Key, pair => pair.Value);

                    var forBodyStatements = RecordOutput(argTuple, iterVars, merged);

                    // FIXME: NOT SURE IF THIS IS CORRECT...
                    var knownSymbols = merged
                        .Select(kvp => new LocalVariableDeclaration<string, ResolvedType>(
                            kvp.Key,
                            kvp.Value,
                            InferredExpressionInformation.ParameterDeclaration,
                            QsNullable<Position>.Null,
                            DataTypes.Range.Zero))
                        .Concat(SyntaxGenerator.ExtractItems(argTuple).ValidDeclarations())
                        .ToImmutableArray();

                    var forStatement = new QsStatement(
                        QsStatementKind.NewQsForStatement(new QsForStatement(
                            Tuple.Create(iterVars, arr.Item),
                            SyntaxGenerator.AutoGeneratedExpression(
                                ExpressionKind.NewIdentifier(
                                    Identifier.NewLocalVariable(name.Item),
                                    QsNullable<ImmutableArray<ResolvedType>>.Null),
                                variableType.Resolution,
                                true), // assume a quantum dependency
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
                    throw new ArgumentException($"Invalid return type for Entry Point Wrapping: {variableType.Resolution}");
                }
            }

            return statements;
        }

        private static (SymbolTuple, Dictionary<string, ResolvedType>) CreateDeconstruction(ResolvedType returnType, int enumerationStart = 0)
        {
            var newVars = new Dictionary<string, ResolvedType>();
            static string MakeVariableName(int enumeration) =>
                $"__{ReturnValueVariableLabel}{enumeration}__";

            SymbolTuple CreateSymbolTuple(ResolvedType t)
            {
                if (t.Resolution is ResolvedTypeKind.TupleType tup)
                {
                    return SymbolTuple.NewVariableNameTuple(tup.Item.Select(CreateSymbolTuple).ToImmutableArray());
                }
                else
                {
                    var newName = MakeVariableName(enumerationStart++);
                    newVars.Add(newName, t);
                    return SymbolTuple.NewVariableName(newName);
                }
            }

            return (CreateSymbolTuple(returnType), newVars);
        }

        private static ImmutableArray<QsStatement> MakeStatements(QsCallable original)
        {
            var (captureVars, captureVarsDict) = CreateDeconstruction(original.Signature.ReturnType);
            var localDeclarations = // FIXME: REALLY?
                new LocalDeclarations(captureVarsDict
                    .Select(kvp => new LocalVariableDeclaration<string, ResolvedType>(
                        kvp.Key,
                        kvp.Value,
                        InferredExpressionInformation.ParameterDeclaration,
                        QsNullable<Position>.Null,
                        DataTypes.Range.Zero))
                    .ToImmutableArray());

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
            var callArgs = SyntaxGenerator.ArgumentTupleAsExpression(original.ArgumentTuple);
            var call = new TypedExpression(
                ExpressionKind.NewCallLikeExpression(callId, callArgs),
                TypeParameterResolution.Empty,
                original.Signature.ReturnType,
                new InferredExpressionInformation(false, false),
                QsNullable<DataTypes.Range>.Null);

            var outputDeconstruction = new QsStatement(
                QsStatementKind.NewQsVariableDeclaration(new QsBinding<TypedExpression>(
                    QsBindingKind.ImmutableBinding,
                    captureVars,
                    call)),
                localDeclarations,
                QsNullable<QsLocation>.Null,
                QsComments.Empty);

            return RecordOutput(original.ArgumentTuple, captureVars, captureVarsDict).Prepend(outputDeconstruction).ToImmutableArray();
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
                                Identifier.NewGlobalCallable(new QsQualifiedName(OutputRecordersNamespaceName, callName)),
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
                public ImmutableArray<QsQualifiedName>.Builder EntryPointNames { get; } = ImmutableArray.CreateBuilder<QsQualifiedName>();

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

            public override QsCompilation OnCompilation(QsCompilation compilation)
            {
                compilation = base.OnCompilation(compilation);
                return new QsCompilation(compilation.Namespaces, this.SharedState.EntryPointNames.ToImmutable());
            }

            private class NamespaceTransformation : NamespaceTransformation<TransformationState>
            {
                public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent)
                    : base(parent)
                {
                }

                private static QsCallable CreateEntryPointWrapper(QsCallable c)
                {
                    var wrapperName = new QsQualifiedName(c.FullName.Namespace, "__" + c.FullName.Name + "__");  // FIXME
                    var wrapperSignature = new ResolvedSignature(
                        ImmutableArray<QsLocalSymbol>.Empty,
                        c.Signature.ArgumentType,
                        ResolvedType.New(ResolvedTypeKind.UnitType),
                        CallableInformation.NoInformation);

                    var bodySpec = new QsSpecialization(
                        QsSpecializationKind.QsBody,
                        wrapperName,
                        ImmutableArray<QsDeclarationAttribute>.Empty,
                        c.Source,
                        c.Location,
                        QsNullable<ImmutableArray<ResolvedType>>.Null,
                        wrapperSignature,
                        SpecializationImplementation.NewProvided(
                            c.ArgumentTuple,
                            new QsScope(
                                MakeStatements(c),
                                new LocalDeclarations(SyntaxGenerator.ExtractItems(c.ArgumentTuple).ValidDeclarations()))),
                        ImmutableArray<string>.Empty,
                        QsComments.Empty);

                    return new QsCallable(
                        c.Kind,
                        wrapperName,
                        c.Attributes.Where(BuiltIn.MarksEntryPoint).ToImmutableArray(),
                        Access.Public,
                        c.Source,
                        c.Location,
                        wrapperSignature,
                        c.ArgumentTuple,
                        ImmutableArray.Create(bodySpec),
                        ImmutableArray<string>.Empty,
                        QsComments.Empty);
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
                            var wrapper = CreateEntryPointWrapper(c);
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
