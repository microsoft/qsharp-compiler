// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations
{
    using ParameterTuple = QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    /// <summary>
    /// This class generates a new entry point callable that takes the same arguments and returns void.
    /// Output recording functions that need to be provided by the runtime are instead used to log the
    /// original return value. Removes the entry point attribute from the original entry point and
    /// instead marks the newly generated callable as entry point.
    /// </summary>
    public class AddOutputRecording
    {
        internal class OutputRecorderDefinition
        {
            internal static string NamespaceName => BuiltIn.Message.FullName.Namespace; // don't change this without editing Message

            internal QsQualifiedName QSharpName { get; }

            internal ResolvedType QSharpType { get; }

            internal QsCallableKind CallableKind => QsCallableKind.Function;

            internal string TargetInstructionName { get; }

            internal ResolvedSignature QSharpSignature { get; }

            internal OutputRecorderDefinition(string qsharpName, string instructionName, ResolvedTypeKind parameterType)
            {
                this.QSharpName = new QsQualifiedName(NamespaceName, qsharpName);
                this.TargetInstructionName = instructionName;
                this.QSharpSignature = new ResolvedSignature(
                    ImmutableArray<QsLocalSymbol>.Empty,
                    ResolvedType.New(parameterType),
                    ResolvedType.New(ResolvedTypeKind.UnitType),
                    CallableInformation.NoInformation);
                this.QSharpType = ResolvedType.New(
                    this.CallableKind.IsFunction
                    ? ResolvedTypeKind.NewFunction(this.QSharpSignature.ArgumentType, this.QSharpSignature.ReturnType)
                    : throw new NotImplementedException("expecting output recorders to be functions"));
            }

            internal static readonly OutputRecorderDefinition Message =
                new OutputRecorderDefinition(BuiltIn.Message.FullName.Name, RuntimeLibrary.Message, ResolvedTypeKind.String);

            internal static readonly OutputRecorderDefinition RecordMessage =
                new OutputRecorderDefinition("RecordMessage", "__quantum__rt__message_record_output", ResolvedTypeKind.String);

            internal static readonly OutputRecorderDefinition Boolean =
                new OutputRecorderDefinition("BooleanRecordOutput", "__quantum__rt__bool_record_output", ResolvedTypeKind.Bool);

            internal static readonly OutputRecorderDefinition Integer =
                new OutputRecorderDefinition("IntegerRecordOutput", "__quantum__rt__int_record_output", ResolvedTypeKind.Int);

            internal static readonly OutputRecorderDefinition Double =
                new OutputRecorderDefinition("DoubleRecordOutput", "__quantum__rt__double_record_output", ResolvedTypeKind.Double);

            internal static readonly OutputRecorderDefinition Result =
                new OutputRecorderDefinition("ResultRecordOutput", "__quantum__rt__result_record_output", ResolvedTypeKind.Result);

            internal static readonly OutputRecorderDefinition TupleStart =
                new OutputRecorderDefinition("TupleStartRecordOutput", "__quantum__rt__tuple_start_record_output", ResolvedTypeKind.UnitType);

            internal static readonly OutputRecorderDefinition TupleEnd =
                new OutputRecorderDefinition("TupleEndRecordOutput", "__quantum__rt__tuple_end_record_output", ResolvedTypeKind.UnitType);

            internal static readonly OutputRecorderDefinition ArrayStart =
                new OutputRecorderDefinition("ArrayStartRecordOutput", "__quantum__rt__array_start_record_output", ResolvedTypeKind.UnitType);

            internal static readonly OutputRecorderDefinition ArrayEnd =
                new OutputRecorderDefinition("ArrayEndRecordOutput", "__quantum__rt__array_end_record_output", ResolvedTypeKind.UnitType);
        }

        internal string MainSuffix { get; }

        internal delegate IEnumerable<QsStatement> OutputRecorder(SymbolTuple outputTuple, ImmutableDictionary<string, LocalVariableDeclaration<string, ResolvedType>> variableDeclarations);

        public interface IOutputRecorder
        {
            public QsStatement RecordStartTuple();

            public QsStatement RecordEndTuple();

            public QsStatement RecordStartArray();

            public QsStatement RecordEndArray();

            public QsStatement RecordBool(string name);

            public QsStatement RecordInt(string name);

            public QsStatement RecordDouble(string name);

            public QsStatement RecordResult(string name);

            internal IEnumerable<OutputRecorderDefinition> RecorderAPI { get; }
        }

        private IOutputRecorder Recorder { get; }

        internal AddOutputRecording(bool useRuntimeAPI, string? mainSuffix = null)
        {
            this.Recorder = useRuntimeAPI ? new RuntimeAPI() : new MessageAPI();
            this.MainSuffix = mainSuffix ?? "__Main";
        }

        public static QsCompilation Apply(QsCompilation compilation, bool useRuntimeAPI = false, string? mainSuffix = null, bool alwaysCreateWrapper = false)
        {
            var recording = new AddOutputRecording(useRuntimeAPI, mainSuffix);
            var transformation = new WrapEntryPoints(recording.RecordOutput, recording.MainSuffix, alwaysCreateWrapper);

            if (useRuntimeAPI)
            {
                compilation = ReplaceMessageCalls.Apply(compilation);
            }

            return compilation.EntryPoints.Length > 0
            ? transformation.OnCompilation(new QsCompilation(
                CreateOutputRecorderAPI(recording.Recorder.RecorderAPI, compilation.Namespaces),
                compilation.EntryPoints))
            : compilation;
        }

        private class RuntimeAPI : IOutputRecorder
        {
            private static readonly IEnumerable<OutputRecorderDefinition> UsedRecorders = ImmutableArray.Create(
                OutputRecorderDefinition.Boolean,
                OutputRecorderDefinition.Integer,
                OutputRecorderDefinition.Double,
                OutputRecorderDefinition.Result,
                OutputRecorderDefinition.TupleStart,
                OutputRecorderDefinition.TupleEnd,
                OutputRecorderDefinition.ArrayStart,
                OutputRecorderDefinition.ArrayEnd);

            public IEnumerable<OutputRecorderDefinition> RecorderAPI => UsedRecorders;

            /* private helpers */

            private static QsStatement MakeAPICall(OutputRecorderDefinition recorder, string? parameterName = null)
            {
                var parameter = recorder.QSharpSignature.ArgumentType.Resolution.IsUnitType
                    ? SyntaxGenerator.UnitValue
                    : SyntaxGenerator.LocalVariable(parameterName, recorder.QSharpSignature.ArgumentType.Resolution, true); // assume local quantum dependency

                return new QsStatement(
                    QsStatementKind.NewQsExpressionStatement(
                        SyntaxGenerator.CallNonGeneric(
                            SyntaxGenerator.GlobalCallable(recorder.QSharpName, recorder.QSharpType.Resolution, QsNullable<ImmutableArray<ResolvedType>>.Null),
                            parameter)),
                    LocalDeclarations.Empty,
                    QsNullable<QsLocation>.Null,
                    QsComments.Empty);
            }

            /* interface methods */

            public QsStatement RecordStartTuple() =>
                MakeAPICall(OutputRecorderDefinition.TupleStart);

            public QsStatement RecordEndTuple() =>
                MakeAPICall(OutputRecorderDefinition.TupleEnd);

            public QsStatement RecordStartArray() =>
                MakeAPICall(OutputRecorderDefinition.ArrayStart);

            public QsStatement RecordEndArray() =>
                MakeAPICall(OutputRecorderDefinition.ArrayEnd);

            public QsStatement RecordBool(string name) =>
                MakeAPICall(OutputRecorderDefinition.Boolean, name);

            public QsStatement RecordInt(string name) =>
                MakeAPICall(OutputRecorderDefinition.Integer, name);

            public QsStatement RecordDouble(string name) =>
                MakeAPICall(OutputRecorderDefinition.Double, name);

            public QsStatement RecordResult(string name) =>
                MakeAPICall(OutputRecorderDefinition.Result, name);
        }

        private class MessageAPI : IOutputRecorder
        {
            private static readonly IEnumerable<OutputRecorderDefinition> UsedRecorders = ImmutableArray.Create(
                OutputRecorderDefinition.Message);

            public IEnumerable<OutputRecorderDefinition> RecorderAPI => UsedRecorders;

            private static QsStatement MakeMessageCall(string message, ImmutableArray<TypedExpression> interpolatedExpressions)
            {
                var messageFunctionType = ResolvedTypeKind.NewFunction(
                    ResolvedType.New(ResolvedTypeKind.String),
                    ResolvedType.New(ResolvedTypeKind.UnitType));

                return new QsStatement(
                    QsStatementKind.NewQsExpressionStatement(
                        SyntaxGenerator.CallNonGeneric(
                            SyntaxGenerator.GlobalCallable(BuiltIn.Message.FullName, messageFunctionType, QsNullable<ImmutableArray<ResolvedType>>.Null),
                            SyntaxGenerator.StringLiteral(message, interpolatedExpressions))),
                    LocalDeclarations.Empty,
                    QsNullable<QsLocation>.Null,
                    QsComments.Empty);
            }

            public QsStatement RecordStartTuple() =>
                MakeMessageCall("Tuple Start", ImmutableArray<TypedExpression>.Empty);

            public QsStatement RecordEndTuple() =>
                MakeMessageCall("Tuple End", ImmutableArray<TypedExpression>.Empty);

            public QsStatement RecordStartArray() =>
                MakeMessageCall("Array Start", ImmutableArray<TypedExpression>.Empty);

            public QsStatement RecordEndArray() =>
                MakeMessageCall("Array End", ImmutableArray<TypedExpression>.Empty);

            public QsStatement RecordBool(string name)
            {
                var id = SyntaxGenerator.LocalVariable(name, ResolvedTypeKind.Bool, true); // assume local quantum dependency
                return MakeMessageCall("Bool: {0}", ImmutableArray.Create(id));
            }

            public QsStatement RecordInt(string name)
            {
                var id = SyntaxGenerator.LocalVariable(name, ResolvedTypeKind.Int, true); // assume local quantum dependency
                return MakeMessageCall("Int: {0}", ImmutableArray.Create(id));
            }

            public QsStatement RecordDouble(string name)
            {
                var id = SyntaxGenerator.LocalVariable(name, ResolvedTypeKind.Double, true); // assume local quantum dependency
                return MakeMessageCall("Double: {0}", ImmutableArray.Create(id));
            }

            public QsStatement RecordResult(string name)
            {
                var id = SyntaxGenerator.LocalVariable(name, ResolvedTypeKind.Result, true); // assume local quantum dependency
                return MakeMessageCall("Result: {0}", ImmutableArray.Create(id));
            }
        }

        private static string MakeVariableName(int enumeration) =>
            $"__rtrnVal{enumeration}__";

        private static QsCallable CreateOutputRecorder(OutputRecorderDefinition recorder, Source source)
        {
            var parameterTuple = !recorder.QSharpSignature.ArgumentType.Resolution.IsUnitType
                ? ParameterTuple.NewQsTuple(ImmutableArray<ParameterTuple>.Empty)
                : ParameterTuple.NewQsTuple(ImmutableArray.Create(
                    ParameterTuple.NewQsTupleItem(
                        new LocalVariableDeclaration<QsLocalSymbol, ResolvedType>(
                            QsLocalSymbol.NewValidName("input"),
                            recorder.QSharpSignature.ArgumentType,
                            InferredExpressionInformation.ParameterDeclaration,
                            QsNullable<Position>.Null,
                            DataTypes.Range.Zero))));
            return new QsCallable(
                recorder.CallableKind,
                recorder.QSharpName,
                ImmutableArray<QsDeclarationAttribute>.Empty,
                Access.Internal,
                source,
                QsNullable<QsLocation>.Null,
                recorder.QSharpSignature,
                parameterTuple,
                ImmutableArray.Create(
                    new QsSpecialization(
                        QsSpecializationKind.QsBody,
                        recorder.QSharpName,
                        ImmutableArray<QsDeclarationAttribute>.Empty,
                        source,
                        QsNullable<QsLocation>.Null,
                        QsNullable<ImmutableArray<ResolvedType>>.Null,
                        recorder.QSharpSignature,
                        SpecializationImplementation.Intrinsic,
                        ImmutableArray<string>.Empty,
                        QsComments.Empty)),
                ImmutableArray<string>.Empty,
                QsComments.Empty);
        }

        private static ImmutableArray<QsNamespace> CreateOutputRecorderAPI(IEnumerable<OutputRecorderDefinition> recorderAPI, ImmutableArray<QsNamespace> namespaces)
        {
            var apiNamespace =
                namespaces.FirstOrDefault(x => x.Name == OutputRecorderDefinition.NamespaceName)
                ?? new QsNamespace(
                    OutputRecorderDefinition.NamespaceName,
                    ImmutableArray<QsNamespaceElement>.Empty,
                    Enumerable.Empty<ImmutableArray<string>>().ToLookup(x => default(string)));

            var definedElements = apiNamespace.Elements.Partition(e => e.IsQsCallable);
            var definedCallables = definedElements.Item1.ToDictionary(
                e => e.GetFullName().Name,
                e => ((QsNamespaceElement.QsCallable)e).Item);

            var apiSource = new Source(Path.GetTempFileName(), QsNullable<string>.Null);
            foreach (var recorder in recorderAPI)
            {
                var recorderDeclaration = definedCallables.TryGetValue(recorder.QSharpName.Name, out var decl) ? decl : CreateOutputRecorder(recorder, apiSource);
                if (SymbolResolution.TryGetTargetInstructionName(recorderDeclaration.Attributes).IsNull)
                {
                    recorderDeclaration = recorderDeclaration.AddAttribute(
                        AttributeUtils.BuildAttribute(
                            BuiltIn.TargetInstruction.FullName,
                            AttributeUtils.StringArgument(recorder.TargetInstructionName)));
                }

                definedCallables[recorder.QSharpName.Name] = recorderDeclaration;
                if (!(recorderDeclaration.Kind.IsFunction
                    && recorderDeclaration.Signature.ArgumentType.Equals(recorder.QSharpSignature.ArgumentType)
                    && recorderDeclaration.Signature.ReturnType.Resolution.IsUnitType
                    && SymbolResolution.TryGetTargetInstructionName(recorderDeclaration.Attributes) is var instr
                    && instr.IsValue && instr.Item == recorder.TargetInstructionName))
                {
                    throw new InvalidOperationException("callable with the expected recorder name is already defined but doesn't have the expected signature");
                }
            }

            apiNamespace = apiNamespace.WithElements(_ =>
                definedElements.Item2.Concat(definedCallables.Values.Select(QsNamespaceElement.NewQsCallable))
                .ToImmutableArray());
            return namespaces.Where(x => x.Name != apiNamespace.Name).Append(apiNamespace).ToImmutableArray();
        }

        private static (SymbolTuple, ImmutableDictionary<string, LocalVariableDeclaration<string, ResolvedType>>) CreateDeconstruction(ResolvedType returnType, int enumerationStart = 0)
        {
            var newVars = ImmutableDictionary.CreateBuilder<string, LocalVariableDeclaration<string, ResolvedType>>();

            SymbolTuple CreateSymbolTuple(ResolvedType t)
            {
                if (t.Resolution is ResolvedTypeKind.TupleType tup)
                {
                    return SymbolTuple.NewVariableNameTuple(tup.Item.Select(CreateSymbolTuple).ToImmutableArray());
                }
                else
                {
                    var newName = MakeVariableName(enumerationStart++);
                    var inferredInfo = new InferredExpressionInformation(isMutable: false, hasLocalQuantumDependency: true); // assume a quantum dependency
                    var newDecl = new LocalVariableDeclaration<string, ResolvedType>(
                        newName, t, inferredInfo, QsNullable<Position>.Null, DataTypes.Range.Zero);
                    newVars.Add(newName, newDecl);
                    return SymbolTuple.NewVariableName(newName);
                }
            }

            return (CreateSymbolTuple(returnType), newVars.ToImmutable());
        }

        private IEnumerable<QsStatement> RecordOutput(SymbolTuple outputTuple, ImmutableDictionary<string, LocalVariableDeclaration<string, ResolvedType>> variableDeclarations)
        {
            var statements = new List<QsStatement>();

            if (outputTuple is SymbolTuple.VariableNameTuple tup)
            {
                statements.Add(this.Recorder.RecordStartTuple());

                foreach (var sym in tup.Item)
                {
                    statements.AddRange(this.RecordOutput(sym, variableDeclarations));
                }

                statements.Add(this.Recorder.RecordEndTuple());
            }
            else if (outputTuple is SymbolTuple.VariableName name)
            {
                var variableDecl = variableDeclarations[name.Item];
                if (variableDecl.Type.Resolution.IsBool)
                {
                    statements.Add(this.Recorder.RecordBool(name.Item));
                }
                else if (variableDecl.Type.Resolution.IsInt)
                {
                    statements.Add(this.Recorder.RecordInt(name.Item));
                }
                else if (variableDecl.Type.Resolution.IsDouble)
                {
                    statements.Add(this.Recorder.RecordDouble(name.Item));
                }
                else if (variableDecl.Type.Resolution.IsResult)
                {
                    statements.Add(this.Recorder.RecordResult(name.Item));
                }
                else if (variableDecl.Type.Resolution is ResolvedTypeKind.ArrayType arr)
                {
                    statements.Add(this.Recorder.RecordStartArray());

                    var (iterVars, iterVarTypes) = CreateDeconstruction(arr.Item, variableDeclarations.Count());
                    var definedInsideFor = variableDeclarations.Concat(iterVarTypes)
                        .ToImmutableDictionary(pair => pair.Key, pair => pair.Value);

                    var forStatement = new QsStatement(
                        QsStatementKind.NewQsForStatement(new QsForStatement(
                            Tuple.Create(iterVars, arr.Item),
                            SyntaxGenerator.LocalVariable(
                                name.Item,
                                variableDecl.Type.Resolution,
                                variableDecl.InferredInformation.HasLocalQuantumDependency),
                            new QsScope(
                                this.RecordOutput(iterVars, definedInsideFor).ToImmutableArray(),
                                new LocalDeclarations(definedInsideFor.Values.ToImmutableArray())))),
                        LocalDeclarations.Empty,
                        QsNullable<QsLocation>.Null,
                        QsComments.Empty);
                    statements.Add(forStatement);

                    statements.Add(this.Recorder.RecordEndArray());
                }
                else if (!variableDecl.Type.Resolution.IsUnitType)
                {
                    // we choose to ignore unit for output recording
                    throw new ArgumentException($"Invalid type for in output recording: {variableDecl.Type.Resolution}");
                }
            }

            return statements;
        }

        private class WrapEntryPoints :
            SyntaxTreeTransformation<WrapEntryPoints.TransformationState>
        {
            public class TransformationState
            {
                public ImmutableArray<QsQualifiedName>.Builder EntryPointNames { get; } = ImmutableArray.CreateBuilder<QsQualifiedName>();

                public List<QsCallable> NewEntryPointWrappers { get; } = new List<QsCallable>();
            }

            public WrapEntryPoints(OutputRecorder recorder, string mainSuffix, bool alwaysCreateWrapper)
                : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this, recorder, mainSuffix, alwaysCreateWrapper);
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
                private readonly OutputRecorder record;
                private readonly bool alwaysCreateWrapper;
                private readonly string mainSuffix;

                public NamespaceTransformation(
                    SyntaxTreeTransformation<TransformationState> parent, OutputRecorder recorder, string mainSuffix, bool alwaysCreateWrapper)
                    : base(parent)
                {
                    this.record = recorder;
                    this.mainSuffix = mainSuffix;
                    this.alwaysCreateWrapper = alwaysCreateWrapper;
                }

                private QsCallable CreateEntryPointWrapper(QsCallable c)
                {
                    QsScope CreateWrapperBody()
                    {
                        var (argType, returnType) = (c.Signature.ArgumentType, c.Signature.ReturnType);
                        var callableType = c.Kind.IsOperation
                            ? ResolvedTypeKind.NewOperation(Tuple.Create(argType, returnType), c.Signature.Information)
                            : ResolvedTypeKind.NewFunction(argType, returnType);
                        var callee = SyntaxGenerator.GlobalCallable(c.FullName, callableType, QsNullable<ImmutableArray<ResolvedType>>.Null);
                        var callArgs = SyntaxGenerator.ArgumentTupleAsExpression(c.ArgumentTuple);

                        var (outputTuple, variableDeclarations) = CreateDeconstruction(c.Signature.ReturnType);
                        var outputDeconstruction = new QsStatement(
                            QsStatementKind.NewQsVariableDeclaration(new QsBinding<TypedExpression>(
                                QsBindingKind.ImmutableBinding,
                                outputTuple,
                                SyntaxGenerator.CallNonGeneric(callee, callArgs))),
                            new LocalDeclarations(variableDeclarations.Values.ToImmutableArray()),
                            QsNullable<QsLocation>.Null,
                            QsComments.Empty);

                        var paramDecl = SyntaxGenerator.ExtractItems(c.ArgumentTuple).ValidDeclarations();
                        var localDeclarations = variableDeclarations.Values.Concat(paramDecl)
                            .ToImmutableDictionary(decl => decl.VariableName);
                        return new QsScope(
                            this.record(outputTuple, localDeclarations).Prepend(outputDeconstruction).ToImmutableArray(),
                            new LocalDeclarations(paramDecl));
                    }

                    var wrapperName = new QsQualifiedName(c.FullName.Namespace, $"{c.FullName.Name}{this.mainSuffix}");
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
                        SpecializationImplementation.NewProvided(c.ArgumentTuple, CreateWrapperBody()),
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
                        if (c.Signature.ReturnType.Resolution.IsUnitType && !this.alwaysCreateWrapper)
                        {
                            this.SharedState.EntryPointNames.Add(c.FullName);
                            return c;
                        }
                        else
                        {
                            var wrapper = this.CreateEntryPointWrapper(c);
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
