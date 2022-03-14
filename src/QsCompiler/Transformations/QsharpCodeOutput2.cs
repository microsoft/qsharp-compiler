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
        public string? CurrentNamespace { get; set; }

        public ImmutableHashSet<string> OpenedNamespaces { get; set; }

        public ImmutableDictionary<string, string> NamespaceShortNames { get; set; } // mapping namespace names to their short names

        public ImmutableHashSet<string> SymbolsInCurrentNamespace { get; set; }

        public ImmutableHashSet<string> AmbiguousNames { get; set; }

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
    : MonoTransformation
    {
        public const string InvalidType = "__UnknownType__";
        public const string InvalidSet = "__UnknownSet__";
        public const string InvalidIdentifier = "__UnknownId__";
        public const string InvalidExpression = "__InvalidEx__";
        public const string InvalidSymbol = "__InvalidName__";
        public const string InvalidInitializer = "__InvalidInitializer__";
        public const string ExternalImplementation = "__external__";
        public const string InvalidFunctorGenerator = "__UnknownGenerator__";

        private Action? BeforeInvalidType { get; set; } = null;

        private Action? BeforeInvalidSet { get; set; } = null;

        private Action? BeforeInvalidIdentifier { get; set; } = null;

        private Action? BeforeInvalidExpression { get; set; } = null;

        private Action? BeforeInvalidSymbol { get; set; } = null;

        private Action? BeforeInvalidInitializer { get; set; } = null;

        private Action? BeforeExternalImplementation { get; set; } = null;

        private Action? BeforeInvalidFunctorGenerator { get; set; } = null;

        public string? TypeOutputHandle { get; set; } = null;

        public string? ExpressionOutputHandle { get; set; } = null;

        public List<string> StatementOutputHandle { get; } = new List<string>();

        public List<string> NamespaceOutputHandle { get; } = new List<string>();

        private QsComments StatementComments { get; set; } = QsComments.Empty;

        private TransformationContext Context { get; set; }

        private IEnumerable<string>? NamespaceDocumentation { get; set; } = null;

        private static bool PrecededByCode(IEnumerable<string> output) =>
            output == null ? false : output.Any() && !string.IsNullOrWhiteSpace(output.Last().Replace("{", ""));

        private static bool PrecededByBlock(IEnumerable<string> output) =>
            output == null ? false : output.Any() && output.Last().Trim() == "}";

        private void InvokeOnInvalid(Action action)
        {
            this.BeforeExternalImplementation = action;
            this.BeforeInvalidInitializer = action;
            this.BeforeInvalidSymbol = action;
            this.BeforeInvalidIdentifier = action;
            this.BeforeInvalidExpression = action;
            this.BeforeInvalidType = action;
            this.BeforeInvalidSet = action;
        }

        public SyntaxTreeToQsharp(TransformationContext? context = null)
        : base(TransformationOptions.NoRebuild)
        {
            this.Context = context ?? new TransformationContext();
        }

        /* public methods for convenience */

        public static SyntaxTreeToQsharp Default =>
            new SyntaxTreeToQsharp();

        public string? ToCode(ResolvedType t)
        {
            this.OnType(t);
            return this.TypeOutputHandle;
        }

        public string? ToCode(QsExpressionKind k)
        {
            this.OnExpressionKind(k);
            return this.ExpressionOutputHandle;
        }

        public string? ToCode(TypedExpression ex) =>
            this.ToCode(ex.Expression);

        public string ToCode(QsCustomType type)
        {
            var nrPreexistingLines = this.NamespaceOutputHandle.Count;
            this.OnTypeDeclaration(type);
            return string.Join(Environment.NewLine, this.NamespaceOutputHandle.Skip(nrPreexistingLines));
        }

        public string ToCode(QsStatementKind stmKind)
        {
            var nrPreexistingLines = this.StatementOutputHandle.Count;
            this.OnStatementKind(stmKind);
            return string.Join(Environment.NewLine, this.StatementOutputHandle.Skip(nrPreexistingLines));
        }

        public string ToCode(QsStatement stm) =>
            this.ToCode(stm);

        public string ToCode(QsNamespace ns)
        {
            var nrPreexistingLines = this.NamespaceOutputHandle.Count;
            this.OnNamespace(ns);
            return string.Join(Environment.NewLine, this.NamespaceOutputHandle.Skip(nrPreexistingLines));
        }

        public static string? CharacteristicsExpression(ResolvedCharacteristics characteristics, Action? onInvalidSet = null)
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

        public static string ArgumentTuple(
                QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>> arg,
                Func<ResolvedType, string?> typeTransformation,
                Action? onInvalidName = null,
                bool symbolsOnly = false) =>
            NamespaceArgumentTuple(arg, item => (SymbolName(item.VariableName, onInvalidName), item.Type), typeTransformation, symbolsOnly);

        public static string DeclarationSignature(QsCallable c, Func<ResolvedType, string?> typeTransformation, Action? onInvalidName = null)
        {
            var argTuple = ArgumentTuple(c.ArgumentTuple, typeTransformation, onInvalidName);
            return $"{c.FullName.Name}{TypeParameters(c.Signature, onInvalidName)} {argTuple} : {typeTransformation(c.Signature.ReturnType)}";
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
                        AmbiguousNames = ambiguousSymbols,
                    };

                    var totNrInvalid = 0;
                    var docComments = ns.Documentation[sourceFile];
                    var generator = new SyntaxTreeToQsharp(context);
                    generator.InvokeOnInvalid(() => ++totNrInvalid);
                    generator.NamespaceDocumentation = docComments.Count() == 1 ? docComments.Single() : ImmutableArray<string>.Empty; // let's drop the doc if it is ambiguous
                    generator.OnNamespace(tree);

                    if (totNrInvalid > 0)
                    {
                        success = false;
                    }

                    nsInFile.Add(ns.Name, string.Join(Environment.NewLine, generator.NamespaceOutputHandle));
                }

                generatedCode.Add(nsInFile.ToImmutableDictionary());
            }

            return success;
        }

        // allows to omit unnecessary parentheses
        private int currentPrecedence = 0;

        // used to replace interpolated pieces in string literals
        private static readonly Regex InterpolationArg =
            new Regex(@"(?<!\\)\{[0-9]+\}");

        /* overrides */

        public override QsTypeKind OnArrayType(ResolvedType b)
        {
            this.TypeOutputHandle = $"{this.ToCode(b)}[]";
            return QsTypeKind.NewArrayType(b);
        }

        /// <inheritdoc/>
        public override QsTypeKind OnBool()
        {
            this.TypeOutputHandle = Keywords.qsBool.id;
            return QsTypeKind.Bool;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnDouble()
        {
            this.TypeOutputHandle = Keywords.qsDouble.id;
            return QsTypeKind.Double;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnFunction(ResolvedType it, ResolvedType ot)
        {
            this.TypeOutputHandle = $"({this.ToCode(it)} -> {this.ToCode(ot)})";
            return QsTypeKind.NewFunction(it, ot);
        }

        /// <inheritdoc/>
        public override QsTypeKind OnInt()
        {
            this.TypeOutputHandle = Keywords.qsInt.id;
            return QsTypeKind.Int;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnBigInt()
        {
            this.TypeOutputHandle = Keywords.qsBigInt.id;
            return QsTypeKind.BigInt;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnInvalidType()
        {
            this.BeforeInvalidType?.Invoke();
            this.TypeOutputHandle = InvalidType;
            return QsTypeKind.InvalidType;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnMissingType()
        {
            this.TypeOutputHandle = "_"; // needs to be underscore, since this is valid as type argument specifier
            return QsTypeKind.MissingType;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnPauli()
        {
            this.TypeOutputHandle = Keywords.qsPauli.id;
            return QsTypeKind.Pauli;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnQubit()
        {
            this.TypeOutputHandle = Keywords.qsQubit.id;
            return QsTypeKind.Qubit;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnRange()
        {
            this.TypeOutputHandle = Keywords.qsRange.id;
            return QsTypeKind.Range;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnResult()
        {
            this.TypeOutputHandle = Keywords.qsResult.id;
            return QsTypeKind.Result;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnString()
        {
            this.TypeOutputHandle = Keywords.qsString.id;
            return QsTypeKind.String;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnTupleType(ImmutableArray<ResolvedType> ts)
        {
            this.TypeOutputHandle = $"({string.Join(", ", ts.Select(this.ToCode))})";
            return QsTypeKind.NewTupleType(ts);
        }

        /// <inheritdoc/>
        public override QsTypeKind OnTypeParameter(QsTypeParameter tp)
        {
            this.TypeOutputHandle = $"'{tp.TypeName}";
            return QsTypeKind.NewTypeParameter(tp);
        }

        /// <inheritdoc/>
        public override QsTypeKind OnUnitType()
        {
            this.TypeOutputHandle = Keywords.qsUnit.id;
            return QsTypeKind.UnitType;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnUserDefinedType(UserDefinedType udt)
        {
            var isInCurrentNamespace = udt.Namespace == this.Context.CurrentNamespace;
            var isInOpenNamespace = this.Context.OpenedNamespaces.Contains(udt.Namespace) && !this.Context.SymbolsInCurrentNamespace.Contains(udt.Name);
            var hasShortName = this.Context.NamespaceShortNames.TryGetValue(udt.Namespace, out var shortName);
            this.TypeOutputHandle = isInCurrentNamespace || (isInOpenNamespace && !this.Context.AmbiguousNames.Contains(udt.Name))
                ? udt.Name
                : $"{(hasShortName ? shortName : udt.Namespace)}.{udt.Name}";
            return QsTypeKind.NewUserDefinedType(udt);
        }

        /// <inheritdoc/>
        public override ResolvedCharacteristics OnCharacteristicsExpression(ResolvedCharacteristics set)
        {
            this.TypeOutputHandle = CharacteristicsExpression(set, onInvalidSet: this.BeforeInvalidSet);
            return set;
        }

        /// <inheritdoc/>
        public override QsTypeKind OnOperation(Tuple<ResolvedType, ResolvedType> sign, CallableInformation info)
        {
            info = this.OnCallableInformation(info);
            var characteristics = string.IsNullOrWhiteSpace(this.TypeOutputHandle) ? "" : $" {Keywords.qsCharacteristics.id} {this.TypeOutputHandle}";
            this.TypeOutputHandle = $"({this.ToCode(sign.Item1)} => {this.ToCode(sign.Item2)}{characteristics})";
            return QsTypeKind.NewOperation(sign, info);
        }

        /* private helper functions */

        private static string ReplaceInterpolatedArgs(string text, Func<int, string> replace)
        {
            var itemNr = 0;
            string ReplaceMatch(Match m) => replace.Invoke(itemNr++);
            return InterpolationArg.Replace(text, ReplaceMatch);
        }

        private string? Recur(int prec, TypedExpression ex)
        {
            this.OnTypedExpression(ex);
            return prec < this.currentPrecedence || this.currentPrecedence == int.MaxValue // need to cover the case where prec = currentPrec = MaxValue
                ? this.ExpressionOutputHandle
                : $"({this.ExpressionOutputHandle})";
        }

        private void UnaryOperator(Keywords.QsOperator op, TypedExpression ex)
        {
            this.ExpressionOutputHandle = Keywords.ReservedKeywords.Contains(op.op)
                ? $"{op.op} {this.Recur(op.prec, ex)}"
                : $"{op.op}{this.Recur(op.prec, ex)}";
            this.currentPrecedence = op.prec;
        }

        private void BinaryOperator(Keywords.QsOperator op, TypedExpression lhs, TypedExpression rhs)
        {
            this.ExpressionOutputHandle = $"{this.Recur(op.prec, lhs)} {op.op} {this.Recur(op.prec, rhs)}";
            this.currentPrecedence = op.prec;
        }

        private void TernaryOperator(Keywords.QsOperator op, TypedExpression fst, TypedExpression snd, TypedExpression trd)
        {
            this.ExpressionOutputHandle = $"{this.Recur(op.prec, fst)} {op.op} {this.Recur(op.prec, snd)} {op.cont} {this.Recur(op.prec, trd)}";
            this.currentPrecedence = op.prec;
        }

        private QsExpressionKind CallLike(TypedExpression method, TypedExpression arg)
        {
            var prec = Keywords.qsCallCombinator.prec;
            var argStr = arg.Expression.IsValueTuple || arg.Expression.IsUnitValue ? this.Recur(int.MinValue, arg) : $"({this.Recur(int.MinValue, arg)})";
            this.ExpressionOutputHandle = $"{this.Recur(prec, method)}{argStr}";
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
        public override QsExpressionKind OnLambda(Lambda<TypedExpression, ResolvedType> lambda)
        {
            var op = lambda.Kind.IsFunction ? "->" : "=>";
            var paramStr = ArgumentTuple(lambda.ArgumentTuple, this.ToCode);
            this.ExpressionOutputHandle = $"{paramStr} {op} {this.Recur(int.MinValue, lambda.Body)}";
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewLambda(lambda);
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
            this.ExpressionOutputHandle = $"{op.op} {this.Recur(op.prec, ex)}";
            this.currentPrecedence = op.prec;
            return QsExpressionKind.NewAdjointApplication(ex);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnControlledApplication(TypedExpression ex)
        {
            var op = Keywords.qsControlledModifier;
            this.ExpressionOutputHandle = $"{op.op} {this.Recur(op.prec, ex)}";
            this.currentPrecedence = op.prec;
            return QsExpressionKind.NewControlledApplication(ex);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnUnwrapApplication(TypedExpression ex)
        {
            var op = Keywords.qsUnwrapModifier;
            this.ExpressionOutputHandle = $"{this.Recur(op.prec, ex)}{op.op}";
            this.currentPrecedence = op.prec;
            return QsExpressionKind.NewUnwrapApplication(ex);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnUnitValue()
        {
            this.ExpressionOutputHandle = "()";
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.UnitValue;
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnMissingExpression()
        {
            this.ExpressionOutputHandle = "_";
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.MissingExpr;
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnInvalidExpression()
        {
            this.BeforeInvalidExpression?.Invoke();
            this.ExpressionOutputHandle = InvalidExpression;
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.InvalidExpr;
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnValueTuple(ImmutableArray<TypedExpression> vs)
        {
            this.ExpressionOutputHandle = $"({string.Join(", ", vs.Select(v => this.Recur(int.MinValue, v)))})";
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewValueTuple(vs);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnValueArray(ImmutableArray<TypedExpression> vs)
        {
            this.ExpressionOutputHandle = $"[{string.Join(", ", vs.Select(v => this.Recur(int.MinValue, v)))}]";
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewValueArray(vs);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnSizedArray(TypedExpression value, TypedExpression size)
        {
            var valueOutput = this.Recur(int.MinValue, value);
            var sizeOutput = this.Recur(int.MinValue, size);
            this.ExpressionOutputHandle = $"[{valueOutput}, size = {sizeOutput}]";

            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewSizedArray(value, size);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnNewArray(ResolvedType bt, TypedExpression idx)
        {
            this.ExpressionOutputHandle = $"{Keywords.arrayDecl.id} {this.ToCode(bt)}[{this.Recur(int.MinValue, idx)}]";
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewNewArray(bt, idx);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnArrayItemAccess(TypedExpression arr, TypedExpression idx)
        {
            var prec = Keywords.qsArrayAccessCombinator.prec;
            this.ExpressionOutputHandle = $"{this.Recur(prec, arr)}[{this.Recur(int.MinValue, idx)}]"; // Todo: generate contextual open range expression when appropriate
            this.currentPrecedence = prec;
            return QsExpressionKind.NewArrayItem(arr, idx);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnNamedItemAccess(TypedExpression ex, Identifier acc)
        {
            this.OnIdentifier(acc, QsNullable<ImmutableArray<ResolvedType>>.Null);
            var (op, itemName) = (Keywords.qsNamedItemCombinator, this.ExpressionOutputHandle);
            this.ExpressionOutputHandle = $"{this.Recur(op.prec, ex)}{op.op}{itemName}";
            return QsExpressionKind.NewNamedItem(ex, acc);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnIntLiteral(long i)
        {
            this.ExpressionOutputHandle = i.ToString(CultureInfo.InvariantCulture);
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewIntLiteral(i);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnBigIntLiteral(BigInteger b)
        {
            this.ExpressionOutputHandle = b.ToString("R", CultureInfo.InvariantCulture) + "L";
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewBigIntLiteral(b);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnDoubleLiteral(double d)
        {
            this.ExpressionOutputHandle = d.ToString("R", CultureInfo.InvariantCulture);
            if ((int)d == d)
            {
                this.ExpressionOutputHandle = $"{this.ExpressionOutputHandle}.0";
            }

            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewDoubleLiteral(d);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnBoolLiteral(bool b)
        {
            if (b)
            {
                this.ExpressionOutputHandle = Keywords.qsTrue.id;
            }
            else
            {
                this.ExpressionOutputHandle = Keywords.qsFalse.id;
            }

            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewBoolLiteral(b);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnStringLiteral(string s, ImmutableArray<TypedExpression> exs)
        {
            string InterpolatedArg(int index) => $"{{{this.Recur(int.MinValue, exs[index])}}}";
            this.ExpressionOutputHandle = exs.Length == 0 ? $"\"{s}\"" : $"$\"{ReplaceInterpolatedArgs(s, InterpolatedArg)}\"";
            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewStringLiteral(s, exs);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnRangeLiteral(TypedExpression lhs, TypedExpression rhs)
        {
            var op = Keywords.qsRangeOp;
            var lhsStr = lhs.Expression.IsRangeLiteral ? this.Recur(int.MinValue, lhs) : this.Recur(op.prec, lhs);
            this.ExpressionOutputHandle = $"{lhsStr} {op.op} {this.Recur(op.prec, rhs)}";
            this.currentPrecedence = op.prec;
            return QsExpressionKind.NewRangeLiteral(lhs, rhs);
        }

        /// <inheritdoc/>
        public override QsExpressionKind OnResultLiteral(QsResult r)
        {
            if (r.IsZero)
            {
                this.ExpressionOutputHandle = Keywords.qsZero.id;
            }
            else if (r.IsOne)
            {
                this.ExpressionOutputHandle = Keywords.qsOne.id;
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
                this.ExpressionOutputHandle = Keywords.qsPauliI.id;
            }
            else if (p.IsPauliX)
            {
                this.ExpressionOutputHandle = Keywords.qsPauliX.id;
            }
            else if (p.IsPauliY)
            {
                this.ExpressionOutputHandle = Keywords.qsPauliY.id;
            }
            else if (p.IsPauliZ)
            {
                this.ExpressionOutputHandle = Keywords.qsPauliZ.id;
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
                this.ExpressionOutputHandle = loc.Item;
            }
            else if (sym.IsInvalidIdentifier)
            {
                this.BeforeInvalidIdentifier?.Invoke();
                this.ExpressionOutputHandle = InvalidIdentifier;
            }
            else if (sym is Identifier.GlobalCallable global)
            {
                var isInCurrentNamespace = global.Item.Namespace == this.Context.CurrentNamespace;
                var isInOpenNamespace = this.Context.OpenedNamespaces.Contains(global.Item.Namespace) && !this.Context.SymbolsInCurrentNamespace.Contains(global.Item.Name);
                var hasShortName = this.Context.NamespaceShortNames.TryGetValue(global.Item.Namespace, out var shortName);
                this.ExpressionOutputHandle = isInCurrentNamespace || (isInOpenNamespace && !this.Context.AmbiguousNames.Contains(global.Item.Name))
                    ? global.Item.Name
                    : $"{(hasShortName ? shortName : global.Item.Namespace)}.{global.Item.Name}";
            }
            else
            {
                throw new NotImplementedException("unknown identifier kind");
            }

            if (tArgs.IsValue)
            {
                this.ExpressionOutputHandle = $"{this.ExpressionOutputHandle}<{string.Join(", ", tArgs.Item.Select(this.ToCode))}>";
            }

            this.currentPrecedence = int.MaxValue;
            return QsExpressionKind.NewIdentifier(sym, tArgs);
        }

        /* private helper functions */

        private int statementIndentation = 0;

        private void AddToStatementOutput(string line)
        {
            for (var i = 0; i < this.statementIndentation; ++i)
            {
                line = $"    {line}";
            }

            this.StatementOutputHandle.Add(line);
        }

        private void AddStatementComments(IEnumerable<string> comments)
        {
            foreach (var comment in comments)
            {
                this.AddToStatementOutput(string.IsNullOrWhiteSpace(comment) ? "" : $"//{comment}");
            }
        }

        private void AddStatement(string? stm)
        {
            var comments = this.StatementComments;
            if (PrecededByBlock(this.StatementOutputHandle) || (PrecededByCode(this.StatementOutputHandle) && comments.OpeningComments.Length != 0))
            {
                this.AddToStatementOutput("");
            }

            this.AddStatementComments(comments.OpeningComments);
            this.AddToStatementOutput($"{stm};");
            this.AddStatementComments(comments.ClosingComments);
            if (comments.ClosingComments.Length != 0)
            {
                this.AddToStatementOutput("");
            }
        }

        private void AddBlockStatement(string intro, QsScope statements, bool withWhiteSpace = true)
        {
            var comments = this.StatementComments;
            if (PrecededByCode(this.StatementOutputHandle) && withWhiteSpace)
            {
                this.AddToStatementOutput("");
            }

            this.AddStatementComments(comments.OpeningComments);
            this.AddToStatementOutput($"{intro} {"{"}");
            ++this.statementIndentation;
            this.OnScope(statements);
            this.AddStatementComments(comments.ClosingComments);
            --this.statementIndentation;
            this.AddToStatementOutput("}");
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
                this.BeforeInvalidSymbol?.Invoke();
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
                return $"{Keywords.qsQubit.id}[{this.ToCode(reg.Item)}]";
            }
            else if (init.Resolution is QsInitializerKind<ResolvedInitializer, TypedExpression>.QubitTupleAllocation tuple)
            {
                return $"({string.Join(", ", tuple.Item.Select(this.InitializerTuple))})";
            }
            else if (init.Resolution.IsInvalidInitializer)
            {
                this.BeforeInvalidInitializer?.Invoke();
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
            var header =
                stm.Kind.IsBorrow ? Keywords.qsBorrow.id
                : stm.Kind.IsAllocate ? Keywords.qsUse.id
                : throw new NotImplementedException("unknown qubit scope");

            var intro = $"{header} {symbols} = {initializers}";
            this.AddBlockStatement(intro, stm.Body);
            return QsStatementKind.NewQsQubitScope(stm);
        }

        /// <inheritdoc/>
        public override QsStatementKind OnForStatement(QsForStatement stm)
        {
            var symbols = this.SymbolTuple(stm.LoopItem.Item1);
            var intro = $"{Keywords.qsFor.id} {symbols} {Keywords.qsRangeIter.id} {this.ToCode(stm.IterationValues)}";
            this.AddBlockStatement(intro, stm.Body);
            return QsStatementKind.NewQsForStatement(stm);
        }

        /// <inheritdoc/>
        public override QsStatementKind OnWhileStatement(QsWhileStatement stm)
        {
            var intro = $"{Keywords.qsWhile.id} {this.ToCode(stm.Condition)}";
            this.AddBlockStatement(intro, stm.Body);
            return QsStatementKind.NewQsWhileStatement(stm);
        }

        /// <inheritdoc/>
        public override QsStatementKind OnRepeatStatement(QsRepeatStatement stm)
        {
            this.StatementComments = stm.RepeatBlock.Comments;
            this.AddBlockStatement(Keywords.qsRepeat.id, stm.RepeatBlock.Body);
            this.StatementComments = stm.FixupBlock.Comments;
            this.AddToStatementOutput($"{Keywords.qsUntil.id} {this.ToCode(stm.SuccessCondition)}");
            this.AddBlockStatement(Keywords.qsRUSfixup.id, stm.FixupBlock.Body, false);
            return QsStatementKind.NewQsRepeatStatement(stm);
        }

        /// <inheritdoc/>
        public override QsStatementKind OnConditionalStatement(QsConditionalStatement stm)
        {
            var header = Keywords.qsIf.id;
            if (PrecededByCode(this.StatementOutputHandle))
            {
                this.AddToStatementOutput("");
            }

            foreach (var clause in stm.ConditionalBlocks)
            {
                this.StatementComments = clause.Item2.Comments;
                var intro = $"{header} {this.ToCode(clause.Item1)}";
                this.AddBlockStatement(intro, clause.Item2.Body, false);
                header = Keywords.qsElif.id;
            }

            if (stm.Default.IsValue)
            {
                this.StatementComments = stm.Default.Item.Comments;
                this.AddBlockStatement(Keywords.qsElse.id, stm.Default.Item.Body, false);
            }

            return QsStatementKind.NewQsConditionalStatement(stm);
        }

        /// <inheritdoc/>
        public override QsStatementKind OnConjugation(QsConjugation stm)
        {
            this.StatementComments = stm.OuterTransformation.Comments;
            this.AddBlockStatement(Keywords.qsWithin.id, stm.OuterTransformation.Body, true);
            this.StatementComments = stm.InnerTransformation.Comments;
            this.AddBlockStatement(Keywords.qsApply.id, stm.InnerTransformation.Body, false);
            return QsStatementKind.NewQsConjugation(stm);
        }

        /// <inheritdoc/>
        public override QsStatementKind OnExpressionStatement(TypedExpression ex)
        {
            this.AddStatement(this.ToCode(ex));
            return QsStatementKind.NewQsExpressionStatement(ex);
        }

        /// <inheritdoc/>
        public override QsStatementKind OnFailStatement(TypedExpression ex)
        {
            this.AddStatement($"{Keywords.qsFail.id} {this.ToCode(ex)}");
            return QsStatementKind.NewQsFailStatement(ex);
        }

        /// <inheritdoc/>
        public override QsStatementKind OnReturnStatement(TypedExpression ex)
        {
            this.AddStatement($"{Keywords.qsReturn.id} {this.ToCode(ex)}");
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

            this.AddStatement($"{header} {this.SymbolTuple(stm.Lhs)} = {this.ToCode(stm.Rhs)}");
            return QsStatementKind.NewQsVariableDeclaration(stm);
        }

        /// <inheritdoc/>
        public override QsStatementKind OnValueUpdate(QsValueUpdate stm)
        {
            this.AddStatement($"{Keywords.qsValueUpdate.id} {this.ToCode(stm.Lhs)} = {this.ToCode(stm.Rhs)}");
            return QsStatementKind.NewQsValueUpdate(stm);
        }

        // overrides

        /// <inheritdoc/>
        public override QsStatement OnStatement(QsStatement stm)
        {
            this.StatementComments = stm.Comments;
            return base.OnStatement(stm);
        }

        /* private helper functions */

        private int namespaceIndentation = 0;
        private string? currentSpecialization = null;
        private int nrSpecialzations = 0;
        private QsComments declarationComments = QsComments.Empty;

        private void AddToNamespaceOutput(string line)
        {
            for (var i = 0; i < this.namespaceIndentation; ++i)
            {
                line = $"    {line}";
            }

            this.NamespaceOutputHandle.Add(line);
        }

        private void AddNamespaceComments(IEnumerable<string> comments)
        {
            foreach (var comment in comments)
            {
                this.AddToNamespaceOutput(string.IsNullOrWhiteSpace(comment) ? "" : $"//{comment}");
            }
        }

        private void AddDirective(string str)
        {
            this.AddToNamespaceOutput($"{str};");
        }

        private void AddDocumentation(IEnumerable<string>? doc)
        {
            if (doc == null)
            {
                return;
            }

            foreach (var line in doc)
            {
                this.AddToNamespaceOutput($"///{line}");
            }
        }

        private void AddBlock(Action processBlock)
        {
            var comments = this.declarationComments;
            var opening = "{";
            if (!this.NamespaceOutputHandle.Any())
            {
                this.AddToNamespaceOutput(opening);
            }
            else
            {
                this.NamespaceOutputHandle[this.NamespaceOutputHandle.Count - 1] += $" {opening}";
            }

            ++this.namespaceIndentation;
            processBlock();
            this.AddNamespaceComments(comments.ClosingComments);
            --this.namespaceIndentation;
            this.AddToNamespaceOutput("}");
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
                this.AddToNamespaceOutput("");
            }

            foreach (var c in callables)
            {
                this.OnNamespaceElement(c);
            }
        }

        /* internal static methods */

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

        internal static string NamespaceArgumentTuple<T>(
            QsTuple<T> arg,
            Func<T, (string?, ResolvedType)> getItemNameAndType,
            Func<ResolvedType, string?> typeTransformation,
            bool symbolsOnly = false)
        {
            if (arg is QsTuple<T>.QsTuple t)
            {
                return $"({string.Join(", ", t.Item.Select(a => NamespaceArgumentTuple(a, getItemNameAndType, typeTransformation, symbolsOnly)))})";
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
        public override Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>, QsScope> OnProvidedImplementation(
            QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>> argTuple, QsScope body)
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
                this.StatementOutputHandle.Clear();
                this.OnScope(body);
                foreach (var line in this.StatementOutputHandle)
                {
                    this.AddToNamespaceOutput(line);
                }
            }

            if (this.nrSpecialzations != 1)
            {
                // todo: needs to be adapted once we support type specializations
                this.AddToNamespaceOutput($"{this.currentSpecialization} {functorArg}");
                this.AddBlock(ProcessContent);
            }
            else
            {
                var comments = this.declarationComments;
                ProcessContent();
                this.AddNamespaceComments(comments.ClosingComments);
            }

            return new Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol, ResolvedType>>, QsScope>(argTuple, body);
        }

        /// <inheritdoc/>
        public override void OnInvalidGeneratorDirective()
        {
            this.BeforeInvalidFunctorGenerator?.Invoke();
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
            this.BeforeExternalImplementation?.Invoke();
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
            var precededByCode = PrecededByCode(this.NamespaceOutputHandle);
            var precededByBlock = PrecededByBlock(this.NamespaceOutputHandle);
            if (precededByCode && (precededByBlock || spec.Implementation.IsProvided || spec.Documentation.Any()))
            {
                this.AddToNamespaceOutput("");
            }

            this.declarationComments = spec.Comments;
            this.AddNamespaceComments(spec.Comments.OpeningComments);
            if (spec.Comments.OpeningComments.Any() && spec.Documentation.Any())
            {
                this.AddToNamespaceOutput("");
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

            this.AddToNamespaceOutput("");
            this.declarationComments = c.Comments;
            this.AddNamespaceComments(c.Comments.OpeningComments);
            if (c.Comments.OpeningComments.Any() && c.Documentation.Any())
            {
                this.AddToNamespaceOutput("");
            }

            this.AddDocumentation(c.Documentation);
            foreach (var attribute in c.Attributes)
            {
                this.OnAttribute(attribute);
            }

            var signature = DeclarationSignature(c, this.ToCode, this.BeforeInvalidSymbol);
            this.OnCharacteristicsExpression(c.Signature.Information.Characteristics);
            var characteristics = this.TypeOutputHandle;

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

            this.AddToNamespaceOutput($"{declHeader} {signature}");
            if (!string.IsNullOrWhiteSpace(characteristics))
            {
                this.AddToNamespaceOutput($"{Keywords.qsCharacteristics.id} {characteristics}");
            }

            this.AddBlock(() => c.Specializations.Select(this.OnSpecializationDeclaration).ToImmutableArray());
            this.AddToNamespaceOutput("");
            return c;
        }

        /// <inheritdoc/>
        public override QsCustomType OnTypeDeclaration(QsCustomType t)
        {
            this.AddToNamespaceOutput("");
            this.declarationComments = t.Comments; // no need to deal with closing comments (can't exist), but need to make sure DeclarationComments is up to date
            this.AddNamespaceComments(t.Comments.OpeningComments);
            if (t.Comments.OpeningComments.Any() && t.Documentation.Any())
            {
                this.AddToNamespaceOutput("");
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

            var udtTuple = NamespaceArgumentTuple(t.TypeItems, GetItemNameAndType, this.ToCode);
            this.AddDirective($"{Keywords.typeDeclHeader.id} {t.FullName.Name} = {udtTuple}");
            return t;
        }

        /// <inheritdoc/>
        public override QsDeclarationAttribute OnAttribute(QsDeclarationAttribute att)
        {
            // do *not* set DeclarationComments!
            this.OnTypedExpression(att.Argument);
            var arg = this.ExpressionOutputHandle;
            var argStr = att.Argument.Expression.IsValueTuple || att.Argument.Expression.IsUnitValue ? arg : $"({arg})";
            var id = att.TypeId.IsValue
                ? Identifier.NewGlobalCallable(new QsQualifiedName(att.TypeId.Item.Namespace, att.TypeId.Item.Name))
                : Identifier.InvalidIdentifier;
            this.OnIdentifier(id, QsNullable<ImmutableArray<ResolvedType>>.Null);
            this.AddNamespaceComments(att.Comments.OpeningComments);
            this.AddToNamespaceOutput($"@ {this.ExpressionOutputHandle}{argStr}");
            return att;
        }

        /// <inheritdoc/>
        public override QsNamespace OnNamespace(QsNamespace ns)
        {
            if (this.Context.CurrentNamespace != ns.Name)
            {
                this.Context =
                    new TransformationContext { CurrentNamespace = ns.Name };
                this.NamespaceDocumentation = null;
            }

            this.AddDocumentation(this.NamespaceDocumentation);
            this.AddToNamespaceOutput($"{Keywords.namespaceDeclHeader.id} {ns.Name}");
            this.AddBlock(() =>
            {
                var context = this.Context;
                var explicitImports = context.OpenedNamespaces.Where(opened => !BuiltIn.NamespacesToAutoOpen.Contains(opened));
                if (explicitImports.Any() || context.NamespaceShortNames.Any())
                {
                    this.AddToNamespaceOutput("");
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
                    this.AddToNamespaceOutput("");
                }

                this.ProcessNamespaceElements(ns.Elements);
            });

            return ns;
        }
    }
}
