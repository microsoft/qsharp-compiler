// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.TextProcessing;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
{
    using QsExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using SpecializationBundle = Dictionary<
        QsNullable<ImmutableArray<ResolvedType>>,
        ImmutableDictionary<QsSpecializationKind, QsSpecialization>>;

    /// <summary>
    /// Class used to represent contextual information for expression transformations.
    /// </summary>
    public class TransformationContext
    {
        public string? CurrentNamespace;
        public ImmutableHashSet<string> OpenedNamespaces;
        public ImmutableDictionary<string, string> NamespaceShortNames; // mapping namespace names to their short names
        public ImmutableHashSet<string> SymbolsInCurrentNamespace;
        public ImmutableHashSet<string> AmbiguousNames;

        public TransformationContext()
        {
            this.CurrentNamespace = null;
            this.OpenedNamespaces = ImmutableHashSet<string>.Empty;
            this.NamespaceShortNames = ImmutableDictionary<string, string>.Empty;
            this.SymbolsInCurrentNamespace = ImmutableHashSet<string>.Empty;
            this.AmbiguousNames = ImmutableHashSet<string>.Empty;
        }
    }

    /// <summary>
    /// Class used to generate Q# code for compiled Q# namespaces.
    /// Upon calling Transform, the Output property is set to the Q# code corresponding to the given namespace.
    /// </summary>
    public class SyntaxTreeToQsharp
    : SyntaxTreeTransformation<SyntaxTreeToQsharp.TransformationState>
    {
        public const string InvalidType = "__UnknownType__";
        public const string InvalidSet = "__UnknownSet__";
        public const string InvalidIdentifier = "__UnknownId__";
        public const string InvalidExpression = "__InvalidEx__";
        public const string InvalidSymbol = "__InvalidName__";
        public const string InvalidInitializer = "__InvalidInitializer__";
        public const string ExternalImplementation = "__external__";
        public const string InvalidFunctorGenerator = "__UnknownGenerator__";

        public class TransformationState
        {
            public Action? BeforeInvalidType = null;
            public Action? BeforeInvalidSet = null;
            public Action? BeforeInvalidIdentifier = null;
            public Action? BeforeInvalidExpression = null;
            public Action? BeforeInvalidSymbol = null;
            public Action? BeforeInvalidInitializer = null;
            public Action? BeforeExternalImplementation = null;
            public Action? BeforeInvalidFunctorGenerator = null;

            internal string? TypeOutputHandle = null;
            internal string? ExpressionOutputHandle = null;
            internal readonly List<string> StatementOutputHandle = new List<string>();
            internal readonly List<string> NamespaceOutputHandle = new List<string>();

            internal QsComments StatementComments = QsComments.Empty;
            internal TransformationContext Context;
            internal IEnumerable<string>? NamespaceDocumentation = null;

            public TransformationState(TransformationContext? context = null) =>
                this.Context = context ?? new TransformationContext();

            internal static bool PrecededByCode(IEnumerable<string> output) =>
                output == null ? false : output.Any() && !string.IsNullOrWhiteSpace(output.Last().Replace("{", ""));

            internal static bool PrecededByBlock(IEnumerable<string> output) =>
                output == null ? false : output.Any() && output.Last().Trim() == "}";

            internal void InvokeOnInvalid(Action action)
            {
                this.BeforeExternalImplementation = action;
                this.BeforeInvalidInitializer = action;
                this.BeforeInvalidSymbol = action;
                this.BeforeInvalidIdentifier = action;
                this.BeforeInvalidExpression = action;
                this.BeforeInvalidType = action;
                this.BeforeInvalidSet = action;
            }
        }

        public SyntaxTreeToQsharp(TransformationContext? context = null)
        : base(new TransformationState(context), TransformationOptions.NoRebuild)
        {
            this.Types = new TypeTransformation(this);
            this.ExpressionKinds = new ExpressionKindTransformation(this);
            this.StatementKinds = new StatementKindTransformation(this);
            this.Statements = new StatementTransformation(this);
            this.Namespaces = new NamespaceTransformation(this);
        }

        // public methods for convenience

        public static SyntaxTreeToQsharp Default =>
            new SyntaxTreeToQsharp();

        public string? ToCode(ResolvedType t)
        {
            this.Types.OnType(t);
            return this.SharedState.TypeOutputHandle;
        }

        public string? ToCode(QsExpressionKind k)
        {
            this.ExpressionKinds.OnExpressionKind(k);
            return this.SharedState.ExpressionOutputHandle;
        }

        public string? ToCode(TypedExpression ex) =>
            this.ToCode(ex.Expression);

        public string ToCode(QsCustomType type)
        {
            var nrPreexistingLines = this.SharedState.NamespaceOutputHandle.Count;
            this.Namespaces.OnTypeDeclaration(type);
            return string.Join(Environment.NewLine, this.SharedState.NamespaceOutputHandle.Skip(nrPreexistingLines));
        }

        public string ToCode(QsStatementKind stmKind)
        {
            var nrPreexistingLines = this.SharedState.StatementOutputHandle.Count;
            this.StatementKinds.OnStatementKind(stmKind);
            return string.Join(Environment.NewLine, this.SharedState.StatementOutputHandle.Skip(nrPreexistingLines));
        }

        public string ToCode(QsStatement stm) =>
            this.ToCode(stm);

        public string ToCode(QsNamespace ns)
        {
            var nrPreexistingLines = this.SharedState.NamespaceOutputHandle.Count;
            this.Namespaces.OnNamespace(ns);
            return string.Join(Environment.NewLine, this.SharedState.NamespaceOutputHandle.Skip(nrPreexistingLines));
        }

        public static string? CharacteristicsExpression(ResolvedCharacteristics characteristics) =>
            TypeTransformation.CharacteristicsExpression(characteristics);

        public static string ArgumentTuple(
                QsTuple<LocalVariableDeclaration<QsLocalSymbol>> arg,
                Func<ResolvedType, string?> typeTransformation,
                Action? onInvalidName = null,
                bool symbolsOnly = false) =>
            NamespaceTransformation.ArgumentTuple(arg, item => (NamespaceTransformation.SymbolName(item.VariableName, onInvalidName), item.Type), typeTransformation, symbolsOnly);

        public static string DeclarationSignature(QsCallable c, Func<ResolvedType, string?> typeTransformation, Action? onInvalidName = null)
        {
            var argTuple = ArgumentTuple(c.ArgumentTuple, typeTransformation, onInvalidName);
            return $"{c.FullName.Name}{NamespaceTransformation.TypeParameters(c.Signature, onInvalidName)} {argTuple} : {typeTransformation(c.Signature.ReturnType)}";
        }

        /// <summary>
        /// For each file in the given parameter array of open directives,
        /// generates a dictionary that maps (the name of) each partial namespace contained in the file
        /// to a string containing the formatted Q# code for the part of the namespace.
        /// Qualified or unqualified names for types and identifiers are generated based on the given namespace and open directives.
        /// -> IMPORTANT: The given namespace is expected to contain *all* elements in that namespace for the *entire* compilation unit!
        /// </summary>
        public static bool Apply(
            out List<ImmutableDictionary<string, string>> generatedCode,
            IEnumerable<QsNamespace> namespaces,
            params (string, ImmutableDictionary<string, ImmutableArray<(string, string?)>>)[] openDirectives)
        {
            generatedCode = new List<ImmutableDictionary<string, string>>();
            var symbolsInNS = namespaces.ToImmutableDictionary(ns => ns.Name, ns => ns.Elements
                .SelectNotNull(element => (element as QsNamespaceElement.QsCallable)?.Item.FullName.Name)
                .ToImmutableHashSet());

            var success = true;
            foreach (var (sourceFile, imports) in openDirectives)
            {
                var nsInFile = new Dictionary<string, string>();
                foreach (var ns in namespaces)
                {
                    var tree = FilterBySourceFile.Apply(ns, sourceFile);
                    if (!tree.Elements.Any())
                    {
                        continue;
                    }

                    // determine all symbols that occur in multiple open namespaces
                    var ambiguousSymbols = symbolsInNS.Where(entry => imports[ns.Name].Contains((entry.Key, null)))
                        .SelectMany(entry => entry.Value)
                        .GroupBy(name => name)
                        .Where(group => group.Count() > 1)
                        .Select(group => group.Key).ToImmutableHashSet();

                    var openedNS = imports[ns.Name].Where(o => o.Item2 == null).Select(o => o.Item1).ToImmutableHashSet();
                    var nsShortNames = imports[ns.Name]
                        .SelectNotNull(o => o.Item2?.Apply(item2 => (o.Item1, item2)))
                        .ToImmutableDictionary(o => o.Item1, o => o.item2);
                    var context = new TransformationContext
                    {
                        CurrentNamespace = ns.Name,
                        OpenedNamespaces = openedNS,
                        NamespaceShortNames = nsShortNames,
                        SymbolsInCurrentNamespace = symbolsInNS[ns.Name],
                        AmbiguousNames = ambiguousSymbols
                    };

                    var totNrInvalid = 0;
                    var docComments = ns.Documentation[sourceFile];
                    var generator = new SyntaxTreeToQsharp(context);
                    generator.SharedState.InvokeOnInvalid(() => ++totNrInvalid);
                    generator.SharedState.NamespaceDocumentation = docComments.Count() == 1 ? docComments.Single() : ImmutableArray<string>.Empty; // let's drop the doc if it is ambiguous
                    generator.Namespaces.OnNamespace(tree);

                    if (totNrInvalid > 0)
                    {
                        success = false;
                    }
                    nsInFile.Add(ns.Name, string.Join(Environment.NewLine, generator.SharedState.NamespaceOutputHandle));
                }
                generatedCode.Add(nsInFile.ToImmutableDictionary());
            }
            return success;
        }

        // helper classes

        /// <summary>
        /// Class used to generate Q# code for Q# types.
        /// Adds an Output string property to ExpressionTypeTransformation,
        /// that upon calling Transform on a Q# type is set to the Q# code corresponding to that type.
        /// </summary>
        public class TypeTransformation
        : TypeTransformation<TransformationState>
        {
            private readonly Func<ResolvedType, string?> typeToQs;

            protected string? Output // the sole purpose of this is a shorter name ...
            {
                get => this.SharedState.TypeOutputHandle;
                set => this.SharedState.TypeOutputHandle = value;
            }

            public TypeTransformation(SyntaxTreeToQsharp parent)
            : base(parent, TransformationOptions.NoRebuild) =>
                this.typeToQs = parent.ToCode;

            public TypeTransformation()
            : base(new TransformationState(), TransformationOptions.NoRebuild) =>
                this.typeToQs = t =>
                {
                    this.Transformation.Types.OnType(t);
                    return this.SharedState.TypeOutputHandle;
                };

            // internal static methods

            internal static string? CharacteristicsExpression(ResolvedCharacteristics characteristics, Action? onInvalidSet = null)
            {
                int currentPrecedence = 0;
                string SetPrecedenceAndReturn(int prec, string str)
                {
                    currentPrecedence = prec;
                    return str;
                }

                string Recur(int prec, ResolvedCharacteristics ex)
                {
                    var output = SetAnnotation(ex);
                    return prec < currentPrecedence || currentPrecedence == int.MaxValue ? output : $"({output})";
                }

                string BinaryOperator(Keywords.QsOperator op, ResolvedCharacteristics lhs, ResolvedCharacteristics rhs) =>
                    SetPrecedenceAndReturn(op.prec, $"{Recur(op.prec, lhs)} {op.op} {Recur(op.prec, rhs)}");

                string SetAnnotation(ResolvedCharacteristics charEx)
                {
                    if (charEx.Expression is CharacteristicsKind<ResolvedCharacteristics>.SimpleSet set)
                    {
                        string setName;
                        if (set.Item.IsAdjointable)
                        {
                            setName = Keywords.qsAdjSet.id;
                        }
                        else if (set.Item.IsControllable)
                        {
                            setName = Keywords.qsCtlSet.id;
                        }
                        else
                        {
                            throw new NotImplementedException("unknown set name");
                        }
                        return SetPrecedenceAndReturn(int.MaxValue, setName);
                    }
                    else if (charEx.Expression is CharacteristicsKind<ResolvedCharacteristics>.Union u)
                    {
                        return BinaryOperator(Keywords.qsSetUnion, u.Item1, u.Item2);
                    }
                    else if (charEx.Expression is CharacteristicsKind<ResolvedCharacteristics>.Intersection i)
                    {
                        return BinaryOperator(Keywords.qsSetIntersection, i.Item1, i.Item2);
                    }
                    else if (charEx.Expression.IsInvalidSetExpr)
                    {
                        onInvalidSet?.Invoke();
                        return SetPrecedenceAndReturn(int.MaxValue, InvalidSet);
                    }
                    else
                    {
                        throw new NotImplementedException("unknown set expression");
                    }
                }

                return characteristics.Expression.IsEmptySet ? null : SetAnnotation(characteristics);
            }

            // overrides

            /// <inheritdoc/>
            public override QsTypeKind OnArrayType(ResolvedType b)
            {
                this.Output = $"{this.typeToQs(b)}[]";
                return QsTypeKind.NewArrayType(b);
            }

            /// <inheritdoc/>
            public override QsTypeKind OnBool()
            {
                this.Output = Keywords.qsBool.id;
                return QsTypeKind.Bool;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnDouble()
            {
                this.Output = Keywords.qsDouble.id;
                return QsTypeKind.Double;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnFunction(ResolvedType it, ResolvedType ot)
            {
                this.Output = $"({this.typeToQs(it)} -> {this.typeToQs(ot)})";
                return QsTypeKind.NewFunction(it, ot);
            }

            /// <inheritdoc/>
            public override QsTypeKind OnInt()
            {
                this.Output = Keywords.qsInt.id;
                return QsTypeKind.Int;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnBigInt()
            {
                this.Output = Keywords.qsBigInt.id;
                return QsTypeKind.BigInt;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnInvalidType()
            {
                this.SharedState.BeforeInvalidType?.Invoke();
                this.Output = InvalidType;
                return QsTypeKind.InvalidType;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnMissingType()
            {
                this.Output = "_"; // needs to be underscore, since this is valid as type argument specifier
                return QsTypeKind.MissingType;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnPauli()
            {
                this.Output = Keywords.qsPauli.id;
                return QsTypeKind.Pauli;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnQubit()
            {
                this.Output = Keywords.qsQubit.id;
                return QsTypeKind.Qubit;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnRange()
            {
                this.Output = Keywords.qsRange.id;
                return QsTypeKind.Range;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnResult()
            {
                this.Output = Keywords.qsResult.id;
                return QsTypeKind.Result;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnString()
            {
                this.Output = Keywords.qsString.id;
                return QsTypeKind.String;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnTupleType(ImmutableArray<ResolvedType> ts)
            {
                this.Output = $"({string.Join(", ", ts.Select(this.typeToQs))})";
                return QsTypeKind.NewTupleType(ts);
            }

            /// <inheritdoc/>
            public override QsTypeKind OnTypeParameter(QsTypeParameter tp)
            {
                this.Output = $"'{tp.TypeName}";
                return QsTypeKind.NewTypeParameter(tp);
            }

            /// <inheritdoc/>
            public override QsTypeKind OnUnitType()
            {
                this.Output = Keywords.qsUnit.id;
                return QsTypeKind.UnitType;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnUserDefinedType(UserDefinedType udt)
            {
                var isInCurrentNamespace = udt.Namespace == this.SharedState.Context.CurrentNamespace;
                var isInOpenNamespace = this.SharedState.Context.OpenedNamespaces.Contains(udt.Namespace) && !this.SharedState.Context.SymbolsInCurrentNamespace.Contains(udt.Name);
                var hasShortName = this.SharedState.Context.NamespaceShortNames.TryGetValue(udt.Namespace, out var shortName);
                this.Output = isInCurrentNamespace || (isInOpenNamespace && !this.SharedState.Context.AmbiguousNames.Contains(udt.Name))
                    ? udt.Name
                    : $"{(hasShortName ? shortName : udt.Namespace)}.{udt.Name}";
                return QsTypeKind.NewUserDefinedType(udt);
            }

            /// <inheritdoc/>
            public override ResolvedCharacteristics OnCharacteristicsExpression(ResolvedCharacteristics set)
            {
                this.Output = CharacteristicsExpression(set, onInvalidSet: this.SharedState.BeforeInvalidSet);
                return set;
            }

            /// <inheritdoc/>
            public override QsTypeKind OnOperation(Tuple<ResolvedType, ResolvedType> sign, CallableInformation info)
            {
                info = this.OnCallableInformation(info);
                var characteristics = string.IsNullOrWhiteSpace(this.Output) ? "" : $" {Keywords.qsCharacteristics.id} {this.Output}";
                this.Output = $"({this.typeToQs(sign.Item1)} => {this.typeToQs(sign.Item2)}{characteristics})";
                return QsTypeKind.NewOperation(sign, info);
            }
        }

        /// <summary>
        /// Class used to generate Q# code for Q# expressions.
        /// Upon calling Transform, the Output property is set to the Q# code corresponding to an expression of the given kind.
        /// </summary>
        public class ExpressionKindTransformation
        : ExpressionKindTransformation<TransformationState>
        {
            // allows to omit unnecessary parentheses
            private int currentPrecedence = 0;

            // used to replace interpolated pieces in string literals
            private static readonly Regex InterpolationArg =
                new Regex(@"(?<!\\)\{[0-9]+\}");

            private readonly Func<ResolvedType, string?> typeToQs;

            protected string? Output // the sole purpose of this is a shorter name ...
            {
                get => this.SharedState.ExpressionOutputHandle;
                set => this.SharedState.ExpressionOutputHandle = value;
            }

            public ExpressionKindTransformation(SyntaxTreeToQsharp parent)
            : base(parent, TransformationOptions.NoRebuild) =>
                this.typeToQs = parent.ToCode;

            // private helper functions

            private static string ReplaceInterpolatedArgs(string text, Func<int, string> replace)
            {
                var itemNr = 0;
                string ReplaceMatch(Match m) => replace.Invoke(itemNr++);
                return InterpolationArg.Replace(text, ReplaceMatch);
            }

            private string? Recur(int prec, TypedExpression ex)
            {
                this.Transformation.Expressions.OnTypedExpression(ex);
                return prec < this.currentPrecedence || this.currentPrecedence == int.MaxValue // need to cover the case where prec = currentPrec = MaxValue
                    ? this.Output
                    : $"({this.Output})";
            }

            private void UnaryOperator(Keywords.QsOperator op, TypedExpression ex)
            {
                this.Output = Keywords.ReservedKeywords.Contains(op.op)
                    ? $"{op.op} {this.Recur(op.prec, ex)}"
                    : $"{op.op}{this.Recur(op.prec, ex)}";
                this.currentPrecedence = op.prec;
            }

            private void BinaryOperator(Keywords.QsOperator op, TypedExpression lhs, TypedExpression rhs)
            {
                this.Output = $"{this.Recur(op.prec, lhs)} {op.op} {this.Recur(op.prec, rhs)}";
                this.currentPrecedence = op.prec;
            }

            private void TernaryOperator(Keywords.QsOperator op, TypedExpression fst, TypedExpression snd, TypedExpression trd)
            {
                this.Output = $"{this.Recur(op.prec, fst)} {op.op} {this.Recur(op.prec, snd)} {op.cont} {this.Recur(op.prec, trd)}";
                this.currentPrecedence = op.prec;
            }

            private QsExpressionKind CallLike(TypedExpression method, TypedExpression arg)
            {
                var prec = Keywords.qsCallCombinator.prec;
                var argStr = arg.Expression.IsValueTuple || arg.Expression.IsUnitValue ? this.Recur(int.MinValue, arg) : $"({this.Recur(int.MinValue, arg)})";
                this.Output = $"{this.Recur(prec, method)}{argStr}";
                this.currentPrecedence = prec;
                return QsExpressionKind.NewCallLikeExpression(method, arg);
            }

            // overrides

            /// <inheritdoc/>
            public override QsExpressionKind OnCopyAndUpdateExpression(TypedExpression lhs, TypedExpression acc, TypedExpression rhs)
            {
                this.TernaryOperator(Keywords.qsCopyAndUpdateOp, lhs, acc, rhs);
                return QsExpressionKind.NewCopyAndUpdate(lhs, acc, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnConditionalExpression(TypedExpression cond, TypedExpression ifTrue, TypedExpression ifFalse)
            {
                this.TernaryOperator(Keywords.qsConditionalOp, cond, ifTrue, ifFalse);
                return QsExpressionKind.NewCONDITIONAL(cond, ifTrue, ifFalse);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnAddition(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsADDop, lhs, rhs);
                return QsExpressionKind.NewADD(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnBitwiseAnd(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsBANDop, lhs, rhs);
                return QsExpressionKind.NewBAND(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnBitwiseExclusiveOr(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsBXORop, lhs, rhs);
                return QsExpressionKind.NewBXOR(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnBitwiseOr(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsBORop, lhs, rhs);
                return QsExpressionKind.NewBOR(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnDivision(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsDIVop, lhs, rhs);
                return QsExpressionKind.NewDIV(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnEquality(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsEQop, lhs, rhs);
                return QsExpressionKind.NewEQ(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnExponentiate(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsPOWop, lhs, rhs);
                return QsExpressionKind.NewPOW(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnGreaterThan(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsGTop, lhs, rhs);
                return QsExpressionKind.NewGT(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnGreaterThanOrEqual(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsGTEop, lhs, rhs);
                return QsExpressionKind.NewGTE(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnInequality(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsNEQop, lhs, rhs);
                return QsExpressionKind.NewNEQ(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnLeftShift(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsLSHIFTop, lhs, rhs);
                return QsExpressionKind.NewLSHIFT(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnLessThan(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsLTop, lhs, rhs);
                return QsExpressionKind.NewLT(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnLessThanOrEqual(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsLTEop, lhs, rhs);
                return QsExpressionKind.NewLTE(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnLogicalAnd(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsANDop, lhs, rhs);
                return QsExpressionKind.NewAND(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnLogicalOr(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsORop, lhs, rhs);
                return QsExpressionKind.NewOR(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnModulo(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsMODop, lhs, rhs);
                return QsExpressionKind.NewMOD(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnMultiplication(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsMULop, lhs, rhs);
                return QsExpressionKind.NewMUL(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnRightShift(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsRSHIFTop, lhs, rhs);
                return QsExpressionKind.NewRSHIFT(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnSubtraction(TypedExpression lhs, TypedExpression rhs)
            {
                this.BinaryOperator(Keywords.qsSUBop, lhs, rhs);
                return QsExpressionKind.NewSUB(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnNegative(TypedExpression ex)
            {
                this.UnaryOperator(Keywords.qsNEGop, ex);
                return QsExpressionKind.NewNEG(ex);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnLogicalNot(TypedExpression ex)
            {
                this.UnaryOperator(Keywords.qsNOTop, ex);
                return QsExpressionKind.NewNOT(ex);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnBitwiseNot(TypedExpression ex)
            {
                this.UnaryOperator(Keywords.qsBNOTop, ex);
                return QsExpressionKind.NewBNOT(ex);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnOperationCall(TypedExpression method, TypedExpression arg)
            {
                return this.CallLike(method, arg);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnFunctionCall(TypedExpression method, TypedExpression arg)
            {
                return this.CallLike(method, arg);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnPartialApplication(TypedExpression method, TypedExpression arg)
            {
                return this.CallLike(method, arg);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnAdjointApplication(TypedExpression ex)
            {
                var op = Keywords.qsAdjointModifier;
                this.Output = $"{op.op} {this.Recur(op.prec, ex)}";
                this.currentPrecedence = op.prec;
                return QsExpressionKind.NewAdjointApplication(ex);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnControlledApplication(TypedExpression ex)
            {
                var op = Keywords.qsControlledModifier;
                this.Output = $"{op.op} {this.Recur(op.prec, ex)}";
                this.currentPrecedence = op.prec;
                return QsExpressionKind.NewControlledApplication(ex);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnUnwrapApplication(TypedExpression ex)
            {
                var op = Keywords.qsUnwrapModifier;
                this.Output = $"{this.Recur(op.prec, ex)}{op.op}";
                this.currentPrecedence = op.prec;
                return QsExpressionKind.NewUnwrapApplication(ex);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnUnitValue()
            {
                this.Output = "()";
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.UnitValue;
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnMissingExpression()
            {
                this.Output = "_";
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.MissingExpr;
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnInvalidExpression()
            {
                this.SharedState.BeforeInvalidExpression?.Invoke();
                this.Output = InvalidExpression;
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.InvalidExpr;
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnValueTuple(ImmutableArray<TypedExpression> vs)
            {
                this.Output = $"({string.Join(", ", vs.Select(v => this.Recur(int.MinValue, v)))})";
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewValueTuple(vs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnValueArray(ImmutableArray<TypedExpression> vs)
            {
                this.Output = $"[{string.Join(", ", vs.Select(v => this.Recur(int.MinValue, v)))}]";
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewValueArray(vs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnNewArray(ResolvedType bt, TypedExpression idx)
            {
                this.Output = $"{Keywords.arrayDecl.id} {this.typeToQs(bt)}[{this.Recur(int.MinValue, idx)}]";
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewNewArray(bt, idx);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnArrayItem(TypedExpression arr, TypedExpression idx)
            {
                var prec = Keywords.qsArrayAccessCombinator.prec;
                this.Output = $"{this.Recur(prec, arr)}[{this.Recur(int.MinValue, idx)}]"; // Todo: generate contextual open range expression when appropriate
                this.currentPrecedence = prec;
                return QsExpressionKind.NewArrayItem(arr, idx);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnNamedItem(TypedExpression ex, Identifier acc)
            {
                this.OnIdentifier(acc, QsNullable<ImmutableArray<ResolvedType>>.Null);
                var (op, itemName) = (Keywords.qsNamedItemCombinator, this.Output);
                this.Output = $"{this.Recur(op.prec, ex)}{op.op}{itemName}";
                return QsExpressionKind.NewNamedItem(ex, acc);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnIntLiteral(long i)
            {
                this.Output = i.ToString(CultureInfo.InvariantCulture);
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewIntLiteral(i);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnBigIntLiteral(BigInteger b)
            {
                this.Output = b.ToString("R", CultureInfo.InvariantCulture) + "L";
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewBigIntLiteral(b);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnDoubleLiteral(double d)
            {
                this.Output = d.ToString("R", CultureInfo.InvariantCulture);
                if ((int)d == d)
                {
                    this.Output = $"{this.Output}.0";
                }
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewDoubleLiteral(d);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnBoolLiteral(bool b)
            {
                if (b)
                {
                    this.Output = Keywords.qsTrue.id;
                }
                else
                {
                    this.Output = Keywords.qsFalse.id;
                }
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewBoolLiteral(b);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnStringLiteral(string s, ImmutableArray<TypedExpression> exs)
            {
                string InterpolatedArg(int index) => $"{{{this.Recur(int.MinValue, exs[index])}}}";
                this.Output = exs.Length == 0 ? $"\"{s}\"" : $"$\"{ReplaceInterpolatedArgs(s, InterpolatedArg)}\"";
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewStringLiteral(s, exs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnRangeLiteral(TypedExpression lhs, TypedExpression rhs)
            {
                var op = Keywords.qsRangeOp;
                var lhsStr = lhs.Expression.IsRangeLiteral ? this.Recur(int.MinValue, lhs) : this.Recur(op.prec, lhs);
                this.Output = $"{lhsStr} {op.op} {this.Recur(op.prec, rhs)}";
                this.currentPrecedence = op.prec;
                return QsExpressionKind.NewRangeLiteral(lhs, rhs);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnResultLiteral(QsResult r)
            {
                if (r.IsZero)
                {
                    this.Output = Keywords.qsZero.id;
                }
                else if (r.IsOne)
                {
                    this.Output = Keywords.qsOne.id;
                }
                else
                {
                    throw new NotImplementedException("unknown Result literal");
                }
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewResultLiteral(r);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnPauliLiteral(QsPauli p)
            {
                if (p.IsPauliI)
                {
                    this.Output = Keywords.qsPauliI.id;
                }
                else if (p.IsPauliX)
                {
                    this.Output = Keywords.qsPauliX.id;
                }
                else if (p.IsPauliY)
                {
                    this.Output = Keywords.qsPauliY.id;
                }
                else if (p.IsPauliZ)
                {
                    this.Output = Keywords.qsPauliZ.id;
                }
                else
                {
                    throw new NotImplementedException("unknown Pauli literal");
                }
                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewPauliLiteral(p);
            }

            /// <inheritdoc/>
            public override QsExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.LocalVariable loc)
                {
                    this.Output = loc.Item;
                }
                else if (sym.IsInvalidIdentifier)
                {
                    this.SharedState.BeforeInvalidIdentifier?.Invoke();
                    this.Output = InvalidIdentifier;
                }
                else if (sym is Identifier.GlobalCallable global)
                {
                    var isInCurrentNamespace = global.Item.Namespace == this.SharedState.Context.CurrentNamespace;
                    var isInOpenNamespace = this.SharedState.Context.OpenedNamespaces.Contains(global.Item.Namespace) && !this.SharedState.Context.SymbolsInCurrentNamespace.Contains(global.Item.Name);
                    var hasShortName = this.SharedState.Context.NamespaceShortNames.TryGetValue(global.Item.Namespace, out var shortName);
                    this.Output = isInCurrentNamespace || (isInOpenNamespace && !this.SharedState.Context.AmbiguousNames.Contains(global.Item.Name))
                        ? global.Item.Name
                        : $"{(hasShortName ? shortName : global.Item.Namespace)}.{global.Item.Name}";
                }
                else
                {
                    throw new NotImplementedException("unknown identifier kind");
                }

                if (tArgs.IsValue)
                {
                    this.Output = $"{this.Output}<{string.Join(", ", tArgs.Item.Select(this.typeToQs))}>";
                }

                this.currentPrecedence = int.MaxValue;
                return QsExpressionKind.NewIdentifier(sym, tArgs);
            }
        }

        /// <summary>
        /// Class used to generate Q# code for Q# statements.
        /// Upon calling Transform, the _Output property of the scope transformation given on initialization
        /// is set to the Q# code corresponding to a statement of the given kind.
        /// </summary>
        public class StatementKindTransformation
        : StatementKindTransformation<TransformationState>
        {
            private int currentIndendation = 0;

            private readonly Func<TypedExpression, string?> expressionToQs;

            private bool PrecededByCode =>
                TransformationState.PrecededByCode(this.SharedState.StatementOutputHandle);

            private bool PrecededByBlock =>
                TransformationState.PrecededByBlock(this.SharedState.StatementOutputHandle);

            public StatementKindTransformation(SyntaxTreeToQsharp parent)
            : base(parent, TransformationOptions.NoRebuild) =>
                this.expressionToQs = parent.ToCode;

            // private helper functions

            private void AddToOutput(string line)
            {
                for (var i = 0; i < this.currentIndendation; ++i)
                {
                    line = $"    {line}";
                }
                this.SharedState.StatementOutputHandle.Add(line);
            }

            private void AddComments(IEnumerable<string> comments)
            {
                foreach (var comment in comments)
                {
                    this.AddToOutput(string.IsNullOrWhiteSpace(comment) ? "" : $"//{comment}");
                }
            }

            private void AddStatement(string? stm)
            {
                var comments = this.SharedState.StatementComments;
                if (this.PrecededByBlock || (this.PrecededByCode && comments.OpeningComments.Length != 0))
                {
                    this.AddToOutput("");
                }
                this.AddComments(comments.OpeningComments);
                this.AddToOutput($"{stm};");
                this.AddComments(comments.ClosingComments);
                if (comments.ClosingComments.Length != 0)
                {
                    this.AddToOutput("");
                }
            }

            private void AddBlockStatement(string intro, QsScope statements, bool withWhiteSpace = true)
            {
                var comments = this.SharedState.StatementComments;
                if (this.PrecededByCode && withWhiteSpace)
                {
                    this.AddToOutput("");
                }
                this.AddComments(comments.OpeningComments);
                this.AddToOutput($"{intro} {"{"}");
                ++this.currentIndendation;
                this.Transformation.Statements.OnScope(statements);
                this.AddComments(comments.ClosingComments);
                --this.currentIndendation;
                this.AddToOutput("}");
            }

            private string SymbolTuple(SymbolTuple sym)
            {
                if (sym.IsDiscardedItem)
                {
                    return "_";
                }
                else if (sym is SymbolTuple.VariableName name)
                {
                    return name.Item;
                }
                else if (sym is SymbolTuple.VariableNameTuple tuple)
                {
                    return $"({string.Join(", ", tuple.Item.Select(this.SymbolTuple))})";
                }
                else if (sym.IsInvalidItem)
                {
                    this.SharedState.BeforeInvalidSymbol?.Invoke();
                    return InvalidSymbol;
                }
                else
                {
                    throw new NotImplementedException("unknown item in symbol tuple");
                }
            }

            private string InitializerTuple(ResolvedInitializer init)
            {
                if (init.Resolution.IsSingleQubitAllocation)
                {
                    return $"{Keywords.qsQubit.id}()";
                }
                else if (init.Resolution is QsInitializerKind<ResolvedInitializer, TypedExpression>.QubitRegisterAllocation reg)
                {
                    return $"{Keywords.qsQubit.id}[{this.expressionToQs(reg.Item)}]";
                }
                else if (init.Resolution is QsInitializerKind<ResolvedInitializer, TypedExpression>.QubitTupleAllocation tuple)
                {
                    return $"({string.Join(", ", tuple.Item.Select(this.InitializerTuple))})";
                }
                else if (init.Resolution.IsInvalidInitializer)
                {
                    this.SharedState.BeforeInvalidInitializer?.Invoke();
                    return InvalidInitializer;
                }
                else
                {
                    throw new NotImplementedException("unknown qubit initializer");
                }
            }

            // overrides

            /// <inheritdoc/>
            public override QsStatementKind OnQubitScope(QsQubitScope stm)
            {
                var symbols = this.SymbolTuple(stm.Binding.Lhs);
                var initializers = this.InitializerTuple(stm.Binding.Rhs);
                string header;
                if (stm.Kind.IsBorrow)
                {
                    header = Keywords.qsBorrowing.id;
                }
                else if (stm.Kind.IsAllocate)
                {
                    header = Keywords.qsUsing.id;
                }
                else
                {
                    throw new NotImplementedException("unknown qubit scope");
                }

                var intro = $"{header} ({symbols} = {initializers})";
                this.AddBlockStatement(intro, stm.Body);
                return QsStatementKind.NewQsQubitScope(stm);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnForStatement(QsForStatement stm)
            {
                var symbols = this.SymbolTuple(stm.LoopItem.Item1);
                var intro = $"{Keywords.qsFor.id} ({symbols} {Keywords.qsRangeIter.id} {this.expressionToQs(stm.IterationValues)})";
                this.AddBlockStatement(intro, stm.Body);
                return QsStatementKind.NewQsForStatement(stm);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnWhileStatement(QsWhileStatement stm)
            {
                var intro = $"{Keywords.qsWhile.id} ({this.expressionToQs(stm.Condition)})";
                this.AddBlockStatement(intro, stm.Body);
                return QsStatementKind.NewQsWhileStatement(stm);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnRepeatStatement(QsRepeatStatement stm)
            {
                this.SharedState.StatementComments = stm.RepeatBlock.Comments;
                this.AddBlockStatement(Keywords.qsRepeat.id, stm.RepeatBlock.Body);
                this.SharedState.StatementComments = stm.FixupBlock.Comments;
                this.AddToOutput($"{Keywords.qsUntil.id} ({this.expressionToQs(stm.SuccessCondition)})");
                this.AddBlockStatement(Keywords.qsRUSfixup.id, stm.FixupBlock.Body, false);
                return QsStatementKind.NewQsRepeatStatement(stm);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnConditionalStatement(QsConditionalStatement stm)
            {
                var header = Keywords.qsIf.id;
                if (this.PrecededByCode)
                {
                    this.AddToOutput("");
                }
                foreach (var clause in stm.ConditionalBlocks)
                {
                    this.SharedState.StatementComments = clause.Item2.Comments;
                    var intro = $"{header} ({this.expressionToQs(clause.Item1)})";
                    this.AddBlockStatement(intro, clause.Item2.Body, false);
                    header = Keywords.qsElif.id;
                }
                if (stm.Default.IsValue)
                {
                    this.SharedState.StatementComments = stm.Default.Item.Comments;
                    this.AddBlockStatement(Keywords.qsElse.id, stm.Default.Item.Body, false);
                }
                return QsStatementKind.NewQsConditionalStatement(stm);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnConjugation(QsConjugation stm)
            {
                this.SharedState.StatementComments = stm.OuterTransformation.Comments;
                this.AddBlockStatement(Keywords.qsWithin.id, stm.OuterTransformation.Body, true);
                this.SharedState.StatementComments = stm.InnerTransformation.Comments;
                this.AddBlockStatement(Keywords.qsApply.id, stm.InnerTransformation.Body, false);
                return QsStatementKind.NewQsConjugation(stm);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnExpressionStatement(TypedExpression ex)
            {
                this.AddStatement(this.expressionToQs(ex));
                return QsStatementKind.NewQsExpressionStatement(ex);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnFailStatement(TypedExpression ex)
            {
                this.AddStatement($"{Keywords.qsFail.id} {this.expressionToQs(ex)}");
                return QsStatementKind.NewQsFailStatement(ex);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnReturnStatement(TypedExpression ex)
            {
                this.AddStatement($"{Keywords.qsReturn.id} {this.expressionToQs(ex)}");
                return QsStatementKind.NewQsReturnStatement(ex);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnVariableDeclaration(QsBinding<TypedExpression> stm)
            {
                string header;
                if (stm.Kind.IsImmutableBinding)
                {
                    header = Keywords.qsImmutableBinding.id;
                }
                else if (stm.Kind.IsMutableBinding)
                {
                    header = Keywords.qsMutableBinding.id;
                }
                else
                {
                    throw new NotImplementedException("unknown binding kind");
                }

                this.AddStatement($"{header} {this.SymbolTuple(stm.Lhs)} = {this.expressionToQs(stm.Rhs)}");
                return QsStatementKind.NewQsVariableDeclaration(stm);
            }

            /// <inheritdoc/>
            public override QsStatementKind OnValueUpdate(QsValueUpdate stm)
            {
                this.AddStatement($"{Keywords.qsValueUpdate.id} {this.expressionToQs(stm.Lhs)} = {this.expressionToQs(stm.Rhs)}");
                return QsStatementKind.NewQsValueUpdate(stm);
            }
        }

        /// <summary>
        /// Class used to generate Q# code for Q# statements.
        /// Upon calling Transform, the Output property is set to the Q# code corresponding to the given statement block.
        /// </summary>
        public class StatementTransformation
        : StatementTransformation<TransformationState>
        {
            public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent, TransformationOptions.NoRebuild)
            {
            }

            // overrides

            /// <inheritdoc/>
            public override QsStatement OnStatement(QsStatement stm)
            {
                this.SharedState.StatementComments = stm.Comments;
                return base.OnStatement(stm);
            }
        }

        public class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            private int currentIndendation = 0;
            private string? currentSpecialization = null;
            private int nrSpecialzations = 0;

            private QsComments declarationComments = QsComments.Empty;

            private readonly Func<ResolvedType, string?> typeToQs;

            private List<string> Output => // the sole purpose of this is a shorter name ...
                this.SharedState.NamespaceOutputHandle;

            public NamespaceTransformation(SyntaxTreeToQsharp parent)
            : base(parent, TransformationOptions.NoRebuild) =>
                this.typeToQs = parent.ToCode;

            // private helper functions

            private void AddToOutput(string line)
            {
                for (var i = 0; i < this.currentIndendation; ++i)
                {
                    line = $"    {line}";
                }
                this.Output.Add(line);
            }

            private void AddComments(IEnumerable<string> comments)
            {
                foreach (var comment in comments)
                {
                    this.AddToOutput(string.IsNullOrWhiteSpace(comment) ? "" : $"//{comment}");
                }
            }

            private void AddDirective(string str)
            {
                this.AddToOutput($"{str};");
            }

            private void AddDocumentation(IEnumerable<string>? doc)
            {
                if (doc == null)
                {
                    return;
                }
                foreach (var line in doc)
                {
                    this.AddToOutput($"///{line}");
                }
            }

            private void AddBlock(Action processBlock)
            {
                var comments = this.declarationComments;
                var opening = "{";
                if (!this.Output.Any())
                {
                    this.AddToOutput(opening);
                }
                else
                {
                    this.Output[this.Output.Count - 1] += $" {opening}";
                }
                ++this.currentIndendation;
                processBlock();
                this.AddComments(comments.ClosingComments);
                --this.currentIndendation;
                this.AddToOutput("}");
            }

            private void ProcessNamespaceElements(IEnumerable<QsNamespaceElement> elements)
            {
                var types = elements.Where(e => e.IsQsCustomType);
                var callables = elements.Where(e => e.IsQsCallable);

                foreach (var t in types)
                {
                    this.OnNamespaceElement(t);
                }
                if (types.Any())
                {
                    this.AddToOutput("");
                }

                foreach (var c in callables)
                {
                    this.OnNamespaceElement(c);
                }
            }

            // internal static methods

            internal static string SymbolName(QsLocalSymbol sym, Action? onInvalidName)
            {
                if (sym is QsLocalSymbol.ValidName n)
                {
                    return n.Item;
                }
                else if (sym.IsInvalidName)
                {
                    onInvalidName?.Invoke();
                    return InvalidSymbol;
                }
                else
                {
                    throw new NotImplementedException("unknown case for local symbol");
                }
            }

            internal static string TypeParameters(ResolvedSignature sign, Action? onInvalidName)
            {
                if (sign.TypeParameters.IsEmpty)
                {
                    return string.Empty;
                }
                return $"<{string.Join(", ", sign.TypeParameters.Select(tp => $"'{SymbolName(tp, onInvalidName)}"))}>";
            }

            internal static string ArgumentTuple<T>(
                QsTuple<T> arg,
                Func<T, (string?, ResolvedType)> getItemNameAndType,
                Func<ResolvedType, string?> typeTransformation,
                bool symbolsOnly = false)
            {
                if (arg is QsTuple<T>.QsTuple t)
                {
                    return $"({string.Join(", ", t.Item.Select(a => ArgumentTuple(a, getItemNameAndType, typeTransformation, symbolsOnly)))})";
                }
                else if (arg is QsTuple<T>.QsTupleItem i)
                {
                    var (itemName, itemType) = getItemNameAndType(i.Item);
                    return itemName == null
                        ? $"{(symbolsOnly ? "_" : $"{typeTransformation(itemType)}")}"
                        : $"{itemName}{(symbolsOnly ? "" : $" : {typeTransformation(itemType)}")}";
                }
                else
                {
                    throw new NotImplementedException("unknown case for argument tuple item");
                }
            }

            // overrides

            /// <inheritdoc/>
            public override Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope> OnProvidedImplementation(
                QsTuple<LocalVariableDeclaration<QsLocalSymbol>> argTuple, QsScope body)
            {
                var functorArg = "(...)";
                if (this.currentSpecialization == Keywords.ctrlDeclHeader.id || this.currentSpecialization == Keywords.ctrlAdjDeclHeader.id)
                {
                    var ctlQubitsName = SyntaxGenerator.ControlledFunctorArgument(argTuple);
                    if (ctlQubitsName != null)
                    {
                        functorArg = $"({ctlQubitsName}, ...)";
                    }
                }
                else if (this.currentSpecialization != Keywords.bodyDeclHeader.id && this.currentSpecialization != Keywords.adjDeclHeader.id)
                {
                    throw new NotImplementedException("the current specialization could not be determined");
                }

                void ProcessContent()
                {
                    this.SharedState.StatementOutputHandle.Clear();
                    this.Transformation.Statements.OnScope(body);
                    foreach (var line in this.SharedState.StatementOutputHandle)
                    {
                        this.AddToOutput(line);
                    }
                }
                if (this.nrSpecialzations != 1)
                {
                    // todo: needs to be adapted once we support type specializations
                    this.AddToOutput($"{this.currentSpecialization} {functorArg}");
                    this.AddBlock(ProcessContent);
                }
                else
                {
                    var comments = this.declarationComments;
                    ProcessContent();
                    this.AddComments(comments.ClosingComments);
                }
                return new Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope>(argTuple, body);
            }

            /// <inheritdoc/>
            public override void OnInvalidGeneratorDirective()
            {
                this.SharedState.BeforeInvalidFunctorGenerator?.Invoke();
                this.AddDirective($"{this.currentSpecialization} {InvalidFunctorGenerator}");
            }

            /// <inheritdoc/>
            public override void OnDistributeDirective() =>
                this.AddDirective($"{this.currentSpecialization} {Keywords.distributeFunctorGenDirective.id}");

            /// <inheritdoc/>
            public override void OnInvertDirective() =>
                this.AddDirective($"{this.currentSpecialization} {Keywords.invertFunctorGenDirective.id}");

            /// <inheritdoc/>
            public override void OnSelfInverseDirective() =>
                this.AddDirective($"{this.currentSpecialization} {Keywords.selfFunctorGenDirective.id}");

            /// <inheritdoc/>
            public override void OnIntrinsicImplementation() =>
                this.AddDirective($"{this.currentSpecialization} {Keywords.intrinsicFunctorGenDirective.id}");

            /// <inheritdoc/>
            public override void OnExternalImplementation()
            {
                this.SharedState.BeforeExternalImplementation?.Invoke();
                this.AddDirective($"{this.currentSpecialization} {ExternalImplementation}");
            }

            /// <inheritdoc/>
            public override QsSpecialization OnBodySpecialization(QsSpecialization spec)
            {
                this.currentSpecialization = Keywords.bodyDeclHeader.id;
                return base.OnBodySpecialization(spec);
            }

            /// <inheritdoc/>
            public override QsSpecialization OnAdjointSpecialization(QsSpecialization spec)
            {
                this.currentSpecialization = Keywords.adjDeclHeader.id;
                return base.OnAdjointSpecialization(spec);
            }

            /// <inheritdoc/>
            public override QsSpecialization OnControlledSpecialization(QsSpecialization spec)
            {
                this.currentSpecialization = Keywords.ctrlDeclHeader.id;
                return base.OnControlledSpecialization(spec);
            }

            /// <inheritdoc/>
            public override QsSpecialization OnControlledAdjointSpecialization(QsSpecialization spec)
            {
                this.currentSpecialization = Keywords.ctrlAdjDeclHeader.id;
                return base.OnControlledAdjointSpecialization(spec);
            }

            /// <inheritdoc/>
            public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec)
            {
                var precededByCode = TransformationState.PrecededByCode(this.Output);
                var precededByBlock = TransformationState.PrecededByBlock(this.Output);
                if (precededByCode && (precededByBlock || spec.Implementation.IsProvided || spec.Documentation.Any()))
                {
                    this.AddToOutput("");
                }
                this.declarationComments = spec.Comments;
                this.AddComments(spec.Comments.OpeningComments);
                if (spec.Comments.OpeningComments.Any() && spec.Documentation.Any())
                {
                    this.AddToOutput("");
                }
                this.AddDocumentation(spec.Documentation);
                return base.OnSpecializationDeclaration(spec);
            }

            /// <inheritdoc/>
            public override QsCallable OnCallableDeclaration(QsCallable c)
            {
                if (c.Kind.IsTypeConstructor)
                {
                    return c; // no code for these
                }

                this.AddToOutput("");
                this.declarationComments = c.Comments;
                this.AddComments(c.Comments.OpeningComments);
                if (c.Comments.OpeningComments.Any() && c.Documentation.Any())
                {
                    this.AddToOutput("");
                }
                this.AddDocumentation(c.Documentation);
                foreach (var attribute in c.Attributes)
                {
                    this.OnAttribute(attribute);
                }

                var signature = DeclarationSignature(c, this.typeToQs, this.SharedState.BeforeInvalidSymbol);
                this.Transformation.Types.OnCharacteristicsExpression(c.Signature.Information.Characteristics);
                var characteristics = this.SharedState.TypeOutputHandle;

                var userDefinedSpecs = c.Specializations.Where(spec => spec.Implementation.IsProvided);
                SpecializationBundle specBundles = SpecializationBundleProperties.Bundle(spec => spec.TypeArguments, spec => spec.Kind, userDefinedSpecs);
                bool NeedsToBeExplicit(QsSpecialization s)
                {
                    if (s.Kind.IsQsBody)
                    {
                        return true;
                    }
                    else if (s.Implementation is SpecializationImplementation.Generated gen)
                    {
                        if (gen.Item.IsSelfInverse)
                        {
                            return s.Kind.IsQsAdjoint;
                        }
                        if (s.Kind.IsQsControlled || s.Kind.IsQsAdjoint)
                        {
                            return false;
                        }

                        var relevantUserDefinedSpecs = specBundles.TryGetValue(SpecializationBundleProperties.BundleId(s.TypeArguments), out var dict)
                            ? dict // there may be no user defined implementations for a certain set of type arguments, in which case there is no such entry in the dictionary
                            : ImmutableDictionary<QsSpecializationKind, QsSpecialization>.Empty;
                        var userDefAdj = relevantUserDefinedSpecs.ContainsKey(QsSpecializationKind.QsAdjoint);
                        var userDefCtl = relevantUserDefinedSpecs.ContainsKey(QsSpecializationKind.QsControlled);
                        if (gen.Item.IsInvert)
                        {
                            return userDefAdj || !userDefCtl;
                        }
                        if (gen.Item.IsDistribute)
                        {
                            return userDefCtl && !userDefAdj;
                        }
                        return false;
                    }
                    else
                    {
                        return !s.Implementation.IsIntrinsic;
                    }
                }
                c = c.WithSpecializations(specs => specs.Where(NeedsToBeExplicit).ToImmutableArray());
                this.nrSpecialzations = c.Specializations.Length;

                var declHeader =
                    c.Kind.IsOperation ? Keywords.opDeclHeader.id :
                    c.Kind.IsFunction ? Keywords.fctDeclHeader.id :
                    throw new NotImplementedException("unknown callable kind");

                this.AddToOutput($"{declHeader} {signature}");
                if (!string.IsNullOrWhiteSpace(characteristics))
                {
                    this.AddToOutput($"{Keywords.qsCharacteristics.id} {characteristics}");
                }
                this.AddBlock(() => c.Specializations.Select(this.OnSpecializationDeclaration).ToImmutableArray());
                this.AddToOutput("");
                return c;
            }

            /// <inheritdoc/>
            public override QsCustomType OnTypeDeclaration(QsCustomType t)
            {
                this.AddToOutput("");
                this.declarationComments = t.Comments; // no need to deal with closing comments (can't exist), but need to make sure DeclarationComments is up to date
                this.AddComments(t.Comments.OpeningComments);
                if (t.Comments.OpeningComments.Any() && t.Documentation.Any())
                {
                    this.AddToOutput("");
                }
                this.AddDocumentation(t.Documentation);
                foreach (var attribute in t.Attributes)
                {
                    this.OnAttribute(attribute);
                }

                (string?, ResolvedType) GetItemNameAndType(QsTypeItem item)
                {
                    if (item is QsTypeItem.Named named)
                    {
                        return (named.Item.VariableName, named.Item.Type);
                    }
                    else if (item is QsTypeItem.Anonymous type)
                    {
                        return (null, type.Item);
                    }
                    else
                    {
                        throw new NotImplementedException("unknown case for type item");
                    }
                }
                var udtTuple = ArgumentTuple(t.TypeItems, GetItemNameAndType, this.typeToQs);
                this.AddDirective($"{Keywords.typeDeclHeader.id} {t.FullName.Name} = {udtTuple}");
                return t;
            }

            /// <inheritdoc/>
            public override QsDeclarationAttribute OnAttribute(QsDeclarationAttribute att)
            {
                // do *not* set DeclarationComments!
                this.Transformation.Expressions.OnTypedExpression(att.Argument);
                var arg = this.SharedState.ExpressionOutputHandle;
                var argStr = att.Argument.Expression.IsValueTuple || att.Argument.Expression.IsUnitValue ? arg : $"({arg})";
                var id = att.TypeId.IsValue
                    ? Identifier.NewGlobalCallable(new QsQualifiedName(att.TypeId.Item.Namespace, att.TypeId.Item.Name))
                    : Identifier.InvalidIdentifier;
                this.Transformation.ExpressionKinds.OnIdentifier(id, QsNullable<ImmutableArray<ResolvedType>>.Null);
                this.AddComments(att.Comments.OpeningComments);
                this.AddToOutput($"@ {this.SharedState.ExpressionOutputHandle}{argStr}");
                return att;
            }

            /// <inheritdoc/>
            public override QsNamespace OnNamespace(QsNamespace ns)
            {
                if (this.SharedState.Context.CurrentNamespace != ns.Name)
                {
                    this.SharedState.Context =
                        new TransformationContext { CurrentNamespace = ns.Name };
                    this.SharedState.NamespaceDocumentation = null;
                }

                this.AddDocumentation(this.SharedState.NamespaceDocumentation);
                this.AddToOutput($"{Keywords.namespaceDeclHeader.id} {ns.Name}");
                this.AddBlock(() =>
                {
                    var context = this.SharedState.Context;
                    var explicitImports = context.OpenedNamespaces.Where(opened => !BuiltIn.NamespacesToAutoOpen.Contains(opened));
                    if (explicitImports.Any() || context.NamespaceShortNames.Any())
                    {
                        this.AddToOutput("");
                    }
                    foreach (var nsName in explicitImports.OrderBy(name => name))
                    {
                        this.AddDirective($"{Keywords.importDirectiveHeader.id} {nsName}");
                    }
                    foreach (var kv in context.NamespaceShortNames.OrderBy(pair => pair.Key))
                    {
                        this.AddDirective($"{Keywords.importDirectiveHeader.id} {kv.Key} {Keywords.importedAs.id} {kv.Value}");
                    }
                    if (explicitImports.Any() || context.NamespaceShortNames.Any())
                    {
                        this.AddToOutput("");
                    }
                    this.ProcessNamespaceElements(ns.Elements);
                });

                return ns;
            }
        }
    }
}
