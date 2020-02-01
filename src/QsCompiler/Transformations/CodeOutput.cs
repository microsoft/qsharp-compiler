// Copyright (c) Microsoft Corporation. All rights reserved.
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


namespace Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
{
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using QsExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;


    // Q# type to Code

    /// <summary>
    /// Class used to generate Q# code for Q# types. 
    /// Adds an Output string property to ExpressionTypeTransformation, 
    /// that upon calling Transform on a Q# type is set to the Q# code corresponding to that type. 
    /// </summary>
    public class ExpressionTypeToQs :
        ExpressionTypeTransformation<ExpressionToQs>
    {
        public string Output;
        public const string InvalidType = "__UnknownType__";
        public const string InvalidSet = "__UnknownSet__"; 

        public Action beforeInvalidType;
        public Action beforeInvalidSet;

        public string Apply(ResolvedType t)
        {
            this.Transform(t);
            return this.Output;
        }

        public ExpressionTypeToQs(ExpressionToQs expression) :
            base(expression)
        { }
        

        public override QsTypeKind onArrayType(ResolvedType b)
        {
            this.Output = $"{this.Apply(b)}[]";
            return QsTypeKind.NewArrayType(b);
        }

        public override QsTypeKind onBool()
        {
            this.Output = Keywords.qsBool.id;
            return QsTypeKind.Bool;
        }

        public override QsTypeKind onDouble()
        {
            this.Output = Keywords.qsDouble.id;
            return QsTypeKind.Double;
        }

        public override QsTypeKind onFunction(ResolvedType it, ResolvedType ot)
        {
            this.Output = $"({this.Apply(it)} -> {this.Apply(ot)})";
            return QsTypeKind.NewFunction(it, ot);
        }

        public override QsTypeKind onInt()
        {
            this.Output = Keywords.qsInt.id;
            return QsTypeKind.Int;
        }

        public override QsTypeKind onBigInt()
        {
            this.Output = Keywords.qsBigInt.id;
            return QsTypeKind.BigInt;
        }

        public override QsTypeKind onInvalidType()
        {
            this.beforeInvalidType?.Invoke();
            this.Output = InvalidType;
            return QsTypeKind.InvalidType;
        }

        public override QsTypeKind onMissingType()
        {
            this.Output = "_"; // needs to be underscore, since this is valid as type argument specifier
            return QsTypeKind.MissingType;
        }

        public override ResolvedCharacteristics onCharacteristicsExpression(ResolvedCharacteristics fs)
        {
            int CurrentPrecedence = 0;
            string SetPrecedenceAndReturn(int prec, string str)
            {
                CurrentPrecedence = prec;
                return str;
            }

            string Recur(int prec, ResolvedCharacteristics ex)
            {
                var output = SetAnnotation(ex);
                return prec < CurrentPrecedence || CurrentPrecedence == int.MaxValue ? output : $"({output})";
            }

            string BinaryOperator(Keywords.QsOperator op, ResolvedCharacteristics lhs, ResolvedCharacteristics rhs) =>
                SetPrecedenceAndReturn(op.prec, $"{Recur(op.prec, lhs)} {op.op} {Recur(op.prec, rhs)}");

            string SetAnnotation(ResolvedCharacteristics characteristics)
            {
                if (characteristics.Expression is CharacteristicsKind<ResolvedCharacteristics>.SimpleSet set)
                {
                    string setName = null;
                    if (set.Item.IsAdjointable) setName = Keywords.qsAdjSet.id;
                    else if (set.Item.IsControllable) setName = Keywords.qsCtlSet.id;
                    else throw new NotImplementedException("unknown set name");
                    return SetPrecedenceAndReturn(int.MaxValue, setName);
                }
                else if (characteristics.Expression is CharacteristicsKind<ResolvedCharacteristics>.Union u)
                { return BinaryOperator(Keywords.qsSetUnion, u.Item1, u.Item2); }
                else if (characteristics.Expression is CharacteristicsKind<ResolvedCharacteristics>.Intersection i)
                { return BinaryOperator(Keywords.qsSetIntersection, i.Item1, i.Item2); }
                else if (characteristics.Expression.IsInvalidSetExpr)
                {
                    this.beforeInvalidSet?.Invoke();
                    return SetPrecedenceAndReturn(int.MaxValue, InvalidSet);
                }
                else throw new NotImplementedException("unknown set expression");
            }

            this.Output = fs.Expression.IsEmptySet ? null : SetAnnotation(fs); 
            return fs;
        }

        public override QsTypeKind onOperation(Tuple<ResolvedType, ResolvedType> sign, CallableInformation info)
        {
            info = base.onCallableInformation(info); 
            var characteristics = String.IsNullOrWhiteSpace(this.Output) ? "" : $" {Keywords.qsCharacteristics.id} {this.Output}"; 
            this.Output = $"({this.Apply(sign.Item1)} => {this.Apply(sign.Item2)}{characteristics})";
            return QsTypeKind.NewOperation(sign, info);
        }

        public override QsTypeKind onPauli()
        {
            this.Output = Keywords.qsPauli.id;
            return QsTypeKind.Pauli;
        }

        public override QsTypeKind onQubit()
        {
            this.Output = Keywords.qsQubit.id;
            return QsTypeKind.Qubit;
        }

        public override QsTypeKind onRange()
        {
            this.Output = Keywords.qsRange.id;
            return QsTypeKind.Range;
        }

        public override QsTypeKind onResult()
        {
            this.Output = Keywords.qsResult.id;
            return QsTypeKind.Result;
        }

        public override QsTypeKind onString()
        {
            this.Output = Keywords.qsString.id;
            return QsTypeKind.String;
        }

        public override QsTypeKind onTupleType(ImmutableArray<ResolvedType> ts)
        {
            this.Output = $"({String.Join(", ", ts.Select(this.Apply))})";
            return QsTypeKind.NewTupleType(ts);
        }

        public override QsTypeKind onTypeParameter(QsTypeParameter tp)
        {
            this.Output = $"'{tp.TypeName.Value}";
            return QsTypeKind.NewTypeParameter(tp);
        }

        public override QsTypeKind onUnitType()
        {
            this.Output = Keywords.qsUnit.id;
            return QsTypeKind.UnitType;
        }

        public override QsTypeKind onUserDefinedType(UserDefinedType udt)
        {
            var isInCurrentNamespace = udt.Namespace.Value == this._Expression.Context.CurrentNamespace;
            var isInOpenNamespace = this._Expression.Context.OpenedNamespaces.Contains(udt.Namespace) && !this._Expression.Context.SymbolsInCurrentNamespace.Contains(udt.Name);
            var hasShortName = this._Expression.Context.NamespaceShortNames.TryGetValue(udt.Namespace, out var shortName);
            this.Output = isInCurrentNamespace || (isInOpenNamespace && !this._Expression.Context.AmbiguousNames.Contains(udt.Name))
                ? udt.Name.Value
                : $"{(hasShortName ? shortName.Value : udt.Namespace.Value)}.{udt.Name.Value}";
            return QsTypeKind.NewUserDefinedType(udt);
        }
    }


    /// <summary>
    /// Class used to generate Q# code for Q# expressions. 
    /// Upon calling Transform, the Output property is set to the Q# code corresponding to an expression of the given kind. 
    /// </summary>
    public class ExpressionKindToQs :
        ExpressionKindTransformation<ExpressionToQs>
    {
        public string Output;

        public const string InvalidIdentifier = "__UnknownId__";
        public const string InvalidExpression = "__InvalidEx__";

        public Action beforeInvalidIdentifier;
        public Action beforeInvalidExpression;

        /// <summary>
        /// allows to omit unnecessary parentheses
        /// </summary>
        private int CurrentPrecedence = 0;

        public string Apply(QsExpressionKind k)
        {
            this.Transform(k);
            return this.Output;
        }

        public ExpressionKindToQs(ExpressionToQs expression) :
            base(expression)
        { }


        private string Type(ResolvedType t) =>
            this._Expression._Type.Apply(t);

        private string Recur(int prec, TypedExpression ex)
        {
            this._Expression.Transform(ex);
            return prec < this.CurrentPrecedence || this.CurrentPrecedence == int.MaxValue // need to cover the case where prec = currentPrec = MaxValue
                ? this.Output 
                : $"({this.Output})";
        }

        private void UnaryOperator(Keywords.QsOperator op, TypedExpression ex)
        {
            this.Output = Keywords.ReservedKeywords.Contains(op.op)
                ? $"{op.op} {this.Recur(op.prec, ex)}"
                : $"{op.op}{this.Recur(op.prec, ex)}";
            this.CurrentPrecedence = op.prec;
        }

        private void BinaryOperator(Keywords.QsOperator op, TypedExpression lhs, TypedExpression rhs)
        {
            this.Output = $"{this.Recur(op.prec, lhs)} {op.op} {this.Recur(op.prec, rhs)}";
            this.CurrentPrecedence = op.prec;
        }

        private void TernaryOperator(Keywords.QsOperator op, TypedExpression fst, TypedExpression snd, TypedExpression trd)
        {
            this.Output = $"{this.Recur(op.prec, fst)} {op.op} {this.Recur(op.prec, snd)} {op.cont} {this.Recur(op.prec, trd)}";
            this.CurrentPrecedence = op.prec;
        }

        private QsExpressionKind CallLike(TypedExpression method, TypedExpression arg)
        {
            var prec = Keywords.qsCallCombinator.prec;
            var argStr = arg.Expression.IsValueTuple || arg.Expression.IsUnitValue ? this.Recur(int.MinValue, arg) : $"({this.Recur(int.MinValue, arg)})";
            this.Output = $"{this.Recur(prec, method)}{argStr}";
            this.CurrentPrecedence = prec;
            return QsExpressionKind.NewCallLikeExpression(method, arg);
        }


        public override QsExpressionKind onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
        {
            if (sym is Identifier.LocalVariable loc)
            { this.Output = loc.Item.Value; }
            else if (sym.IsInvalidIdentifier)
            {
                this.beforeInvalidIdentifier?.Invoke();
                this.Output = InvalidIdentifier;
            }
            else if (sym is Identifier.GlobalCallable global)
            {
                var isInCurrentNamespace = global.Item.Namespace.Value == this._Expression.Context.CurrentNamespace;
                var isInOpenNamespace = this._Expression.Context.OpenedNamespaces.Contains(global.Item.Namespace) && !this._Expression.Context.SymbolsInCurrentNamespace.Contains(global.Item.Name);
                var hasShortName = this._Expression.Context.NamespaceShortNames.TryGetValue(global.Item.Namespace, out var shortName);
                this.Output = isInCurrentNamespace || (isInOpenNamespace && !this._Expression.Context.AmbiguousNames.Contains(global.Item.Name))
                    ? global.Item.Name.Value
                    : $"{(hasShortName ? shortName.Value : global.Item.Namespace.Value)}.{global.Item.Name.Value}";
            }
            else throw new NotImplementedException("unknown identifier kind");

            if (tArgs.IsValue)
            { this.Output = $"{this.Output}<{ String.Join(", ", tArgs.Item.Select(this.Type))}>"; }
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewIdentifier(sym, tArgs);
        }

        public override QsExpressionKind onOperationCall(TypedExpression method, TypedExpression arg) =>
            this.CallLike(method, arg);

        public override QsExpressionKind onFunctionCall(TypedExpression method, TypedExpression arg) =>
            this.CallLike(method, arg);

        public override QsExpressionKind onPartialApplication(TypedExpression method, TypedExpression arg) =>
            this.CallLike(method, arg);

        public override QsExpressionKind onAdjointApplication(TypedExpression ex)
        {
            var op = Keywords.qsAdjointModifier;
            this.Output = $"{op.op} {this.Recur(op.prec, ex)}";
            this.CurrentPrecedence = op.prec;
            return QsExpressionKind.NewAdjointApplication(ex);
        }

        public override QsExpressionKind onControlledApplication(TypedExpression ex)
        {
            var op = Keywords.qsControlledModifier;
            this.Output = $"{op.op} {this.Recur(op.prec, ex)}";
            this.CurrentPrecedence = op.prec;
            return QsExpressionKind.NewControlledApplication(ex);
        }

        public override QsExpressionKind onUnwrapApplication(TypedExpression ex)
        {
            var op = Keywords.qsUnwrapModifier;
            this.Output = $"{this.Recur(op.prec, ex)}{op.op}";
            this.CurrentPrecedence = op.prec;
            return QsExpressionKind.NewUnwrapApplication(ex);
        }

        public override QsExpressionKind onUnitValue()
        {
            this.Output = "()";
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.UnitValue;
        }

        public override QsExpressionKind onMissingExpression()
        {
            this.Output = "_";
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.MissingExpr;
        }

        public override QsExpressionKind onInvalidExpression()
        {
            this.beforeInvalidExpression?.Invoke();
            this.Output = InvalidExpression;
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.InvalidExpr;
        }

        public override QsExpressionKind onValueTuple(ImmutableArray<TypedExpression> vs)
        {
            this.Output = $"({String.Join(", ", vs.Select(v => this.Recur(int.MinValue, v)))})";
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewValueTuple(vs);
        }

        public override QsExpressionKind onValueArray(ImmutableArray<TypedExpression> vs)
        {
            this.Output = $"[{String.Join(", ", vs.Select(v => this.Recur(int.MinValue, v)))}]";
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewValueArray(vs);
        }

        public override QsExpressionKind onNewArray(ResolvedType bt, TypedExpression idx)
        {
            this.Output = $"{Keywords.arrayDecl.id} {this.Type(bt)}[{this.Recur(int.MinValue, idx)}]";
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewNewArray(bt, idx);
        }

        public override QsExpressionKind onArrayItem(TypedExpression arr, TypedExpression idx)
        {
            var prec = Keywords.qsArrayAccessCombinator.prec;
            this.Output = $"{this.Recur(prec,arr)}[{this.Recur(int.MinValue, idx)}]"; // Todo: generate contextual open range expression when appropriate
            this.CurrentPrecedence = prec;
            return QsExpressionKind.NewArrayItem(arr, idx);
        }

        public override QsExpressionKind onNamedItem(TypedExpression ex, Identifier acc)
        {
            this.onIdentifier(acc, QsNullable<ImmutableArray<ResolvedType>>.Null);
            var (op, itemName) = (Keywords.qsNamedItemCombinator, this.Output);
            this.Output = $"{this.Recur(op.prec,ex)}{op.op}{itemName}";
            return base.onNamedItem(ex, acc);
        }

        public override QsExpressionKind onIntLiteral(long i)
        {
            this.Output = i.ToString(CultureInfo.InvariantCulture);
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewIntLiteral(i);
        }

        public override QsExpressionKind onBigIntLiteral(BigInteger b)
        {
            this.Output = b.ToString("R", CultureInfo.InvariantCulture) + "L";
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewBigIntLiteral(b);
        }

        public override QsExpressionKind onDoubleLiteral(double d)
        {
            this.Output = d.ToString("R", CultureInfo.InvariantCulture);
            if ((int)d == d) this.Output = $"{this.Output}.0";
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewDoubleLiteral(d);
        }

        public override QsExpressionKind onBoolLiteral(bool b)
        {
            if (b) this.Output = Keywords.qsTrue.id;
            else this.Output = Keywords.qsFalse.id;
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewBoolLiteral(b);
        }

        private static readonly Regex InterpolationArg = new Regex(@"(?<!\\)\{[0-9]+\}");
        private static string ReplaceInterpolatedArgs(string text, Func<int, string> replace)
        {
            var itemNr = 0;
            string ReplaceMatch(Match m) => replace?.Invoke(itemNr++);
            return InterpolationArg.Replace(text, ReplaceMatch);
        }

        public override QsExpressionKind onStringLiteral(NonNullable<string> s, ImmutableArray<TypedExpression> exs)
        {
            string InterpolatedArg(int index) => $"{{{this.Recur(int.MinValue, exs[index])}}}";
            this.Output = exs.Length == 0 ? $"\"{s.Value}\"" : $"$\"{ReplaceInterpolatedArgs(s.Value, InterpolatedArg)}\""; 
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewStringLiteral(s, exs);
        }

        public override QsExpressionKind onRangeLiteral(TypedExpression lhs, TypedExpression rhs)
        {
            var op = Keywords.qsRangeOp;
            var lhsStr = lhs.Expression.IsRangeLiteral ? this.Recur(int.MinValue, lhs) : this.Recur(op.prec, lhs);
            this.Output = $"{lhsStr} {op.op} {this.Recur(op.prec, rhs)}";
            this.CurrentPrecedence = op.prec;            
            return QsExpressionKind.NewRangeLiteral(lhs, rhs);
        }

        public override QsExpressionKind onResultLiteral(QsResult r)
        {
            if (r.IsZero) this.Output = Keywords.qsZero.id;
            else if (r.IsOne) this.Output = Keywords.qsOne.id;
            else throw new NotImplementedException("unknown Result literal");
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewResultLiteral(r);
        }

        public override QsExpressionKind onPauliLiteral(QsPauli p)
        {
            if (p.IsPauliI) this.Output = Keywords.qsPauliI.id;
            else if (p.IsPauliX) this.Output = Keywords.qsPauliX.id;
            else if (p.IsPauliY) this.Output = Keywords.qsPauliY.id;
            else if (p.IsPauliZ) this.Output = Keywords.qsPauliZ.id;
            else throw new NotImplementedException("unknown Pauli literal");
            this.CurrentPrecedence = int.MaxValue;
            return QsExpressionKind.NewPauliLiteral(p);
        }


        public override QsExpressionKind onCopyAndUpdateExpression(TypedExpression lhs, TypedExpression acc, TypedExpression rhs)
        {
            TernaryOperator(Keywords.qsCopyAndUpdateOp, lhs, acc, rhs);
            return QsExpressionKind.NewCopyAndUpdate(lhs, acc, rhs);
        }

        public override QsExpressionKind onConditionalExpression(TypedExpression cond, TypedExpression ifTrue, TypedExpression ifFalse)
        {
            TernaryOperator(Keywords.qsConditionalOp, cond, ifTrue, ifFalse);
            return QsExpressionKind.NewCONDITIONAL(cond, ifTrue, ifFalse);
        }

        public override QsExpressionKind onAddition(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsADDop, lhs, rhs);
            return QsExpressionKind.NewADD(lhs, rhs);
        }

        public override QsExpressionKind onBitwiseAnd(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsBANDop, lhs, rhs);
            return QsExpressionKind.NewBAND(lhs, rhs);
        }

        public override QsExpressionKind onBitwiseExclusiveOr(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsBXORop, lhs, rhs);
            return QsExpressionKind.NewBXOR(lhs, rhs);
        }

        public override QsExpressionKind onBitwiseOr(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsBORop, lhs, rhs);
            return QsExpressionKind.NewBOR(lhs, rhs);
        }

        public override QsExpressionKind onDivision(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsDIVop, lhs, rhs);
            return QsExpressionKind.NewDIV(lhs, rhs);
        }

        public override QsExpressionKind onEquality(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsEQop, lhs, rhs);
            return QsExpressionKind.NewEQ(lhs, rhs);
        }

        public override QsExpressionKind onExponentiate(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsPOWop, lhs, rhs);
            return QsExpressionKind.NewPOW(lhs, rhs);
        }

        public override QsExpressionKind onGreaterThan(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsGTop, lhs, rhs);
            return QsExpressionKind.NewGT(lhs, rhs);
        }

        public override QsExpressionKind onGreaterThanOrEqual(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsGTEop, lhs, rhs);
            return QsExpressionKind.NewGTE(lhs, rhs);
        }

        public override QsExpressionKind onInequality(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsNEQop, lhs, rhs);
            return QsExpressionKind.NewNEQ(lhs, rhs);
        }

        public override QsExpressionKind onLeftShift(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsLSHIFTop, lhs, rhs);
            return QsExpressionKind.NewLSHIFT(lhs, rhs);
        }

        public override QsExpressionKind onLessThan(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsLTop, lhs, rhs);
            return QsExpressionKind.NewLT(lhs, rhs);
        }

        public override QsExpressionKind onLessThanOrEqual(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsLTEop, lhs, rhs);
            return QsExpressionKind.NewLTE(lhs, rhs);
        }

        public override QsExpressionKind onLogicalAnd(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsANDop, lhs, rhs);
            return QsExpressionKind.NewAND(lhs, rhs);
        }

        public override QsExpressionKind onLogicalOr(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsORop, lhs, rhs);
            return QsExpressionKind.NewOR(lhs, rhs);
        }

        public override QsExpressionKind onModulo(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsMODop, lhs, rhs);
            return QsExpressionKind.NewMOD(lhs, rhs);
        }

        public override QsExpressionKind onMultiplication(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsMULop, lhs, rhs);
            return QsExpressionKind.NewMUL(lhs, rhs);
        }

        public override QsExpressionKind onRightShift(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsRSHIFTop, lhs, rhs);
            return QsExpressionKind.NewRSHIFT(lhs, rhs);
        }

        public override QsExpressionKind onSubtraction(TypedExpression lhs, TypedExpression rhs)
        {
            BinaryOperator(Keywords.qsSUBop, lhs, rhs);
            return QsExpressionKind.NewSUB(lhs, rhs);
        }

        public override QsExpressionKind onNegative(TypedExpression ex)
        {
            UnaryOperator(Keywords.qsNEGop, ex);
            return QsExpressionKind.NewNEG(ex);
        }

        public override QsExpressionKind onLogicalNot(TypedExpression ex)
        {
            UnaryOperator(Keywords.qsNOTop, ex);
            return QsExpressionKind.NewNOT(ex);
        }

        public override QsExpressionKind onBitwiseNot(TypedExpression ex)
        {
            UnaryOperator(Keywords.qsBNOTop, ex);
            return QsExpressionKind.NewBNOT(ex);
        }
    }


    /// <summary>
    /// used to pass contextual information for expression transformations
    /// </summary>
    public class TransformationContext
    {
        public string CurrentNamespace;
        public ImmutableHashSet<NonNullable<string>> OpenedNamespaces;
        public ImmutableDictionary<NonNullable<string>, NonNullable<string>> NamespaceShortNames; // mapping namespace names to their short names
        public ImmutableHashSet<NonNullable<string>> SymbolsInCurrentNamespace;
        public ImmutableHashSet<NonNullable<string>> AmbiguousNames;

        public TransformationContext()
        {
            this.CurrentNamespace = null;
            this.OpenedNamespaces = ImmutableHashSet<NonNullable<string>>.Empty;
            this.NamespaceShortNames = ImmutableDictionary<NonNullable<string>, NonNullable<string>>.Empty;
            this.SymbolsInCurrentNamespace = ImmutableHashSet<NonNullable<string>>.Empty;
            this.AmbiguousNames = ImmutableHashSet<NonNullable<string>>.Empty;
        }
    }


    /// <summary>
    /// Class used to generate Q# code for Q# expressions. 
    /// Upon calling Transform, the Output property is set to the Q# code corresponding to the given expression. 
    /// </summary>
    public class ExpressionToQs :
        ExpressionTransformation<ExpressionKindToQs, ExpressionTypeToQs>
    {
        internal readonly TransformationContext Context;

        public ExpressionToQs(TransformationContext context = null) :
            base(e => new ExpressionKindToQs(e as ExpressionToQs), e => new ExpressionTypeToQs(e as ExpressionToQs)) =>
            this.Context = context ?? new TransformationContext();
    }


    /// <summary>
    /// Class used to generate Q# code for Q# statements. 
    /// Upon calling Transform, the _Output property of the scope transformation given on initialization
    /// is set to the Q# code corresponding to a statement of the given kind. 
    /// </summary>
    public class StatementKindToQs :
        StatementKindTransformation<ScopeToQs>
    {
        private int currentIndendation = 0;

        public const string InvalidSymbol = "__InvalidName__";
        public const string InvalidInitializer = "__InvalidInitializer__";

        public Action beforeInvalidSymbol;
        public Action beforeInvalidInitializer;

        internal StatementKindToQs(ScopeToQs scope) :
            base(scope)
        { }

        private void AddToOutput(string line)
        {
            for (var i = 0; i < currentIndendation; ++i) line = $"    {line}";
            this._Scope._Output.Add(line);
        }

        private void AddComments(IEnumerable<string> comments)
        {
            foreach (var comment in comments)
            { this.AddToOutput(String.IsNullOrWhiteSpace(comment) ? "" : $"//{comment}"); }
        }

        private bool PrecededByCode =>
            SyntaxTreeToQs.PrecededByCode(this._Scope._Output);

        private void AddStatement(string stm)
        {
            var comments = this._Scope.CurrentComments;
            var precededByBlockStatement = SyntaxTreeToQs.PrecededByBlock(this._Scope._Output);

            if (precededByBlockStatement || (PrecededByCode && comments.OpeningComments.Length != 0)) this.AddToOutput("");
            this.AddComments(comments.OpeningComments);
            this.AddToOutput($"{stm};");
            this.AddComments(comments.ClosingComments);
            if (comments.ClosingComments.Length != 0) this.AddToOutput("");
        }

        private void AddBlockStatement(string intro, QsScope statements, bool withWhiteSpace = true)
        {
            var comments = this._Scope.CurrentComments;
            if (PrecededByCode && withWhiteSpace) this.AddToOutput("");
            this.AddComments(comments.OpeningComments);
            this.AddToOutput($"{intro} {"{"}");
            ++currentIndendation;
            this._Scope.Transform(statements);
            this.AddComments(comments.ClosingComments);
            --currentIndendation;
            this.AddToOutput("}");
        }

        private string Expression(TypedExpression ex) =>
            this._Scope._Expression._Kind.Apply(ex.Expression);

        private string SymbolTuple(SymbolTuple sym)
        {
            if (sym.IsDiscardedItem) return "_";
            else if (sym is SymbolTuple.VariableName name) return name.Item.Value;
            else if (sym is SymbolTuple.VariableNameTuple tuple) return $"({String.Join(", ", tuple.Item.Select(SymbolTuple))})";
            else if (sym.IsInvalidItem)
            {
                this.beforeInvalidSymbol?.Invoke();
                return InvalidSymbol;
            }
            else throw new NotImplementedException("unknown item in symbol tuple");
        }

        private string InitializerTuple(ResolvedInitializer init)
        {
            if (init.Resolution.IsSingleQubitAllocation) return $"{Keywords.qsQubit.id}()";
            else if (init.Resolution is QsInitializerKind<ResolvedInitializer, TypedExpression>.QubitRegisterAllocation reg)
            { return $"{Keywords.qsQubit.id}[{Expression(reg.Item)}]"; }
            else if (init.Resolution is QsInitializerKind<ResolvedInitializer, TypedExpression>.QubitTupleAllocation tuple)
            { return $"({String.Join(", ", tuple.Item.Select(InitializerTuple))})"; }
            else if (init.Resolution.IsInvalidInitializer)
            {
                this.beforeInvalidInitializer?.Invoke();
                return InvalidInitializer;
            }
            else throw new NotImplementedException("unknown qubit initializer");
        }


        private QsStatementKind QubitScope(QsQubitScope stm)
        {
            var symbols = SymbolTuple(stm.Binding.Lhs);
            var initializers = InitializerTuple(stm.Binding.Rhs);
            string header;
            if (stm.Kind.IsBorrow) header = Keywords.qsBorrowing.id;
            else if (stm.Kind.IsAllocate) header = Keywords.qsUsing.id;
            else throw new NotImplementedException("unknown qubit scope");

            var intro = $"{header} ({symbols} = {initializers})";
            this.AddBlockStatement(intro, stm.Body);
            return QsStatementKind.NewQsQubitScope(stm);
        }

        public override QsStatementKind onAllocateQubits(QsQubitScope stm) =>
            this.QubitScope(stm);

        public override QsStatementKind onBorrowQubits(QsQubitScope stm) =>
            this.QubitScope(stm);

        public override QsStatementKind onForStatement(QsForStatement stm)
        {
            var symbols = SymbolTuple(stm.LoopItem.Item1);
            var intro = $"{Keywords.qsFor.id} ({symbols} {Keywords.qsRangeIter.id} {Expression(stm.IterationValues)})";
            this.AddBlockStatement(intro, stm.Body);
            return QsStatementKind.NewQsForStatement(stm);
        }

        public override QsStatementKind onWhileStatement(QsWhileStatement stm)
        {
            var intro = $"{Keywords.qsWhile.id} ({Expression(stm.Condition)})";
            this.AddBlockStatement(intro, stm.Body);
            return QsStatementKind.NewQsWhileStatement(stm);
        }

        public override QsStatementKind onRepeatStatement(QsRepeatStatement stm)
        {
            this._Scope.CurrentComments = stm.RepeatBlock.Comments;
            this.AddBlockStatement(Keywords.qsRepeat.id, stm.RepeatBlock.Body);
            this._Scope.CurrentComments = stm.FixupBlock.Comments;
            this.AddToOutput($"{Keywords.qsUntil.id} ({Expression(stm.SuccessCondition)})");
            this.AddBlockStatement(Keywords.qsRUSfixup.id, stm.FixupBlock.Body, false);
            return QsStatementKind.NewQsRepeatStatement(stm);
        }

        public override QsStatementKind onConditionalStatement(QsConditionalStatement stm)
        {
            var header = Keywords.qsIf.id;
            if (PrecededByCode) this.AddToOutput("");
            foreach (var clause in stm.ConditionalBlocks)
            {
                this._Scope.CurrentComments = clause.Item2.Comments;
                var intro = $"{header} ({Expression(clause.Item1)})";
                this.AddBlockStatement(intro, clause.Item2.Body, false);
                header = Keywords.qsElif.id;
            }
            if (stm.Default.IsValue)
            {
                this._Scope.CurrentComments = stm.Default.Item.Comments;
                this.AddBlockStatement(Keywords.qsElse.id, stm.Default.Item.Body, false);
            }
            return QsStatementKind.NewQsConditionalStatement(stm);
        }

        public override QsStatementKind onConjugation(QsConjugation stm)
        {
            this._Scope.CurrentComments = stm.OuterTransformation.Comments;
            this.AddBlockStatement(Keywords.qsWithin.id, stm.OuterTransformation.Body, false);
            this._Scope.CurrentComments = stm.InnerTransformation.Comments;
            this.AddBlockStatement(Keywords.qsApply.id, stm.InnerTransformation.Body, true);
            return QsStatementKind.NewQsConjugation(stm);
        }


        public override QsStatementKind onExpressionStatement(TypedExpression ex)
        {
            this.AddStatement(Expression(ex));
            return QsStatementKind.NewQsExpressionStatement(ex);
        }

        public override QsStatementKind onFailStatement(TypedExpression ex)
        {
            this.AddStatement($"{Keywords.qsFail.id} {Expression(ex)}");
            return QsStatementKind.NewQsFailStatement(ex);
        }

        public override QsStatementKind onReturnStatement(TypedExpression ex)
        {
            this.AddStatement($"{Keywords.qsReturn.id} {Expression(ex)}");
            return QsStatementKind.NewQsReturnStatement(ex);
        }

        public override QsStatementKind onVariableDeclaration(QsBinding<TypedExpression> stm)
        {
            string header;
            if (stm.Kind.IsImmutableBinding) header = Keywords.qsImmutableBinding.id;
            else if (stm.Kind.IsMutableBinding) header = Keywords.qsMutableBinding.id;
            else throw new NotImplementedException("unknown binding kind");

            this.AddStatement($"{header} {SymbolTuple(stm.Lhs)} = {Expression(stm.Rhs)}");
            return QsStatementKind.NewQsVariableDeclaration(stm);
        }

        public override QsStatementKind onValueUpdate(QsValueUpdate stm)
        {
            this.AddStatement($"{Keywords.qsValueUpdate.id} {Expression(stm.Lhs)} = {Expression(stm.Rhs)}");
            return QsStatementKind.NewQsValueUpdate(stm);
        }
    }


    /// <summary>
    /// Class used to generate Q# code for Q# statements. 
    /// Upon calling Transform, the Output property is set to the Q# code corresponding to the given statement block.
    /// </summary>
    public class ScopeToQs :
        ScopeTransformation<StatementKindToQs, ExpressionToQs>
    {
        internal readonly List<string> _Output;
        public string Output => String.Join(Environment.NewLine, this._Output);
        internal QsComments CurrentComments;

        public ScopeToQs(TransformationContext context = null) :
            base(s => new StatementKindToQs(s as ScopeToQs), new ExpressionToQs(context))
        {
            this.CurrentComments = QsComments.Empty;
            this._Output = new List<string>();
        }

        public override QsStatement onStatement(QsStatement stm)
        {
            this.CurrentComments = stm.Comments;
            return base.onStatement(stm);
        }
    }


    /// <summary>
    /// Class used to generate Q# code for compiled Q# namespaces. 
    /// Upon calling Transform, the Output property is set to the Q# code corresponding to the given namespace.
    /// </summary>
    public class SyntaxTreeToQs :
        SyntaxTreeTransformation<ScopeToQs>
    {
        private QsComments CurrentComments;
        private int currentIndendation = 0;
        private string currentSpec;
        private int nrSpecialzations;

        private readonly List<string> _Output;
        public string Output => String.Join(Environment.NewLine, this._Output);

        internal static bool PrecededByCode(IEnumerable<string> output) =>
            output == null ? false : output.Any() && !String.IsNullOrWhiteSpace(output.Last().Replace("{", ""));

        internal static bool PrecededByBlock(IEnumerable<string> output) =>
            output == null ? false : output.Any() && output.Last().Trim() == "}";

        public const string ExternalImplementation = "__external__";
        public const string InvalidFunctorGenerator = "__UnknownGenerator__";

        public Action beforeExternalImplementation;
        public Action beforeInvalidFunctorGenerator;

        private void SetAllInvalid(Action action)
        {
            this.beforeExternalImplementation = action;
            this._Scope._StatementKind.beforeInvalidInitializer = action;
            this._Scope._StatementKind.beforeInvalidSymbol = action;
            this._Scope._Expression._Kind.beforeInvalidIdentifier = action;
            this._Scope._Expression._Kind.beforeInvalidExpression = action;
            this._Scope._Expression._Type.beforeInvalidType = action;
            this._Scope._Expression._Type.beforeInvalidSet = action;
        }

        private void SetAllInvalid(SyntaxTreeToQs other)
        {
            this.beforeExternalImplementation = other.beforeExternalImplementation;
            this._Scope._StatementKind.beforeInvalidInitializer = other._Scope._StatementKind.beforeInvalidInitializer;
            this._Scope._StatementKind.beforeInvalidSymbol = other._Scope._StatementKind.beforeInvalidSymbol;
            this._Scope._Expression._Kind.beforeInvalidIdentifier = other._Scope._Expression._Kind.beforeInvalidIdentifier;
            this._Scope._Expression._Kind.beforeInvalidExpression = other._Scope._Expression._Kind.beforeInvalidExpression;
            this._Scope._Expression._Type.beforeInvalidType = other._Scope._Expression._Type.beforeInvalidType;
            this._Scope._Expression._Type.beforeInvalidSet = other._Scope._Expression._Type.beforeInvalidSet; 
        }

        /// <summary>
        /// For each file in the given parameter array of open directives, 
        /// generates a dictionary that maps (the name of) each partial namespace contained in the file 
        /// to a string containing the formatted Q# code for the part of the namespace. 
        /// Qualified or unqualified names for types and identifiers are generated based on the given namespace and open directives. 
        /// Throws an ArgumentNullException if the given namespace is null. 
        /// -> IMPORTANT: The given namespace is expected to contain *all* elements in that namespace for the *entire* compilation unit!
        /// </summary>
        public static bool Apply(out List<ImmutableDictionary<NonNullable<string>, string>> generatedCode,
            IEnumerable<QsNamespace> namespaces, 
            params (NonNullable<string>, ImmutableDictionary<NonNullable<string>, ImmutableArray<(NonNullable<string>, string)>>)[] openDirectives)
        {
            if (namespaces == null) throw new ArgumentNullException(nameof(namespaces));

            generatedCode = new List<ImmutableDictionary<NonNullable<string>, string>>();
            var symbolsInNS = namespaces.ToImmutableDictionary(ns => ns.Name, ns => ns.Elements
                .Select(element => (element is QsNamespaceElement.QsCallable c) ? c.Item.FullName.Name.Value : null)
                .Where(name => name != null).Select(name => NonNullable<string>.New(name)).ToImmutableHashSet());

            var success = true;
            foreach (var (sourceFile, imports) in openDirectives)
            {
                var nsInFile = new Dictionary<NonNullable<string>, string>();
                foreach (var ns in namespaces)
                {
                    var tree = FilterBySourceFile.Apply(ns, sourceFile);
                    if (!tree.Elements.Any()) continue;

                    // determine all symbols that occur in multiple open namespaces
                    var ambiguousSymbols = symbolsInNS.Where(entry => imports[ns.Name].Contains((entry.Key, null)))
                        .SelectMany(entry => entry.Value)
                        .GroupBy(name => name)
                        .Where(group => group.Count() > 1)
                        .Select(group => group.Key).ToImmutableHashSet();

                    var openedNS = imports[ns.Name].Where(o => o.Item2 == null).Select(o => o.Item1).ToImmutableHashSet();
                    var nsShortNames = imports[ns.Name].Where(o => o.Item2 != null).ToImmutableDictionary(o => o.Item1, o => NonNullable<string>.New(o.Item2));
                    var context = new TransformationContext
                    {
                        CurrentNamespace = ns.Name.Value,
                        OpenedNamespaces = openedNS,
                        NamespaceShortNames = nsShortNames,
                        SymbolsInCurrentNamespace = symbolsInNS[ns.Name],
                        AmbiguousNames = ambiguousSymbols
                    };

                    var generator = new SyntaxTreeToQs(new ScopeToQs(context));
                    var totNrInvalid = 0;
                    generator.SetAllInvalid(() => ++totNrInvalid);

                    var docComments = ns.Documentation[sourceFile];
                    generator.AddDocumentation(docComments.Count() == 1 ? docComments.Single() : ImmutableArray<string>.Empty); // let's drop the doc if it is ambiguous

                    generator.AddToOutput($"{Keywords.namespaceDeclHeader.id} {ns.Name.Value}");
                    generator.AddBlock(() =>
                    {
                        var explicitImports = openedNS.Where(opened => !BuiltIn.NamespacesToAutoOpen.Contains(opened));
                        if (explicitImports.Any() || nsShortNames.Any()) generator.AddToOutput("");
                        foreach (var nsName in explicitImports.OrderBy(name => name))
                        { generator.AddDirective($"{Keywords.importDirectiveHeader.id} {nsName.Value}"); }
                        foreach (var kv in nsShortNames.OrderBy(pair => pair.Key))
                        { generator.AddDirective($"{Keywords.importDirectiveHeader.id} {kv.Key.Value} {Keywords.importedAs.id} {kv.Value.Value}"); }
                        if (explicitImports.Any() || nsShortNames.Any()) generator.AddToOutput("");
                        generator.ProcessNamespaceElements(tree.Elements);
                    });
                    if (totNrInvalid > 0) success = false;
                    nsInFile.Add(ns.Name, generator.Output);
                }
                generatedCode.Add(nsInFile.ToImmutableDictionary());
            }
            return success;
        }

        public SyntaxTreeToQs(ScopeToQs scope = null) :
            base(scope ?? new ScopeToQs())
        {
            this.CurrentComments = QsComments.Empty;
            this._Output = new List<string>();
        }

        private void AddToOutput(string line)
        {
            for (var i = 0; i < currentIndendation; ++i) line = $"    {line}";
            this._Output.Add(line);
        }

        private void AddComments(IEnumerable<string> comments)
        {
            foreach (var comment in comments)
            { this.AddToOutput(String.IsNullOrWhiteSpace(comment) ? "" : $"//{comment}"); }
        }

        private void AddDirective(string str) =>
            this.AddToOutput($"{str};");

        private void AddBlock(Action processBlock)
        {
            var comments = this.CurrentComments;
            var opening = "{";
            if (!this._Output.Any()) this.AddToOutput(opening);
            else this._Output[this._Output.Count - 1] += $" {opening}";
            ++currentIndendation;
            processBlock();
            this.AddComments(comments.ClosingComments);
            --currentIndendation;
            this.AddToOutput("}");
        }

        private string Type(ResolvedType t) =>
            this._Scope._Expression._Type.Apply(t);

        private static string SymbolName(QsLocalSymbol sym, Action onInvalidName)
        {
            if (sym is QsLocalSymbol.ValidName n) return n.Item.Value;
            else if (sym.IsInvalidName)
            {
                onInvalidName?.Invoke();
                return StatementKindToQs.InvalidSymbol;
            }
            else throw new NotImplementedException("unknown case for local symbol");
        }

        private static string TypeParameters(ResolvedSignature sign, Action onInvalidName)
        {
            if (sign.TypeParameters.IsEmpty) return String.Empty;
            return $"<{String.Join(", ", sign.TypeParameters.Select(tp => $"'{SyntaxTreeToQs.SymbolName(tp, onInvalidName)}"))}>";
        }

        private static string ArgumentTuple<T>(QsTuple<T> arg, 
            Func<T, (string, ResolvedType)> getItemNameAndType, Func<ResolvedType, string> typeTransformation, bool symbolsOnly = false)
        {
            if (arg is QsTuple<T>.QsTuple t)
            { return $"({String.Join(", ", t.Item.Select(a => ArgumentTuple(a, getItemNameAndType, typeTransformation, symbolsOnly)))})"; }
            else if (arg is QsTuple<T>.QsTupleItem i)
            {
                var (itemName, itemType) = getItemNameAndType(i.Item);
                return itemName == null 
                    ? $"{(symbolsOnly ? "_" : $"{typeTransformation(itemType)}")}"
                    : $"{itemName}{(symbolsOnly ? "" : $" : {typeTransformation(itemType)}")}";
            }
            else throw new NotImplementedException("unknown case for argument tuple item");
        }

        public static string ArgumentTuple(QsTuple<LocalVariableDeclaration<QsLocalSymbol>> arg,
            Func<ResolvedType, string> typeTransformation, Action onInvalidName = null, bool symbolsOnly = false) =>
            ArgumentTuple(arg, item => (SymbolName(item.VariableName, onInvalidName), item.Type), typeTransformation, symbolsOnly);

        public static string DeclarationSignature(QsCallable c, Func<ResolvedType, string> typeTransformation, Action onInvalidName = null)
        {
            var argTuple = SyntaxTreeToQs.ArgumentTuple(c.ArgumentTuple, typeTransformation, onInvalidName);
            return $"{c.FullName.Name.Value}{TypeParameters(c.Signature, onInvalidName)} {argTuple} : {typeTransformation(c.Signature.ReturnType)}";
        }

        private void AddDocumentation(ImmutableArray<string> doc)
        {
            foreach (var line in doc)
            { this.AddToOutput($"///{line}"); }
        }

        private void ProcessNamespaceElements(IEnumerable<QsNamespaceElement> elements)
        {
            var types = elements.Where(e => e.IsQsCustomType);
            var callables = elements.Where(e => e.IsQsCallable);

            foreach (var t in types)
            { this.dispatchNamespaceElement(t); }
            if (types.Any()) this.AddToOutput("");

            foreach (var c in callables)
            { this.dispatchNamespaceElement(c); }
        }


        public override Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope> onProvidedImplementation
            (QsTuple<LocalVariableDeclaration<QsLocalSymbol>> argTuple, QsScope body)
        {
            var functorArg = "(...)";
            if (this.currentSpec == Keywords.ctrlDeclHeader.id || this.currentSpec == Keywords.ctrlAdjDeclHeader.id)
            {
                var ctlQubitsName = SyntaxGenerator.ControlledFunctorArgument(argTuple);
                if (ctlQubitsName != null) functorArg = $"({ctlQubitsName}, ...)";
            }
            else if (this.currentSpec != Keywords.bodyDeclHeader.id && this.currentSpec != Keywords.adjDeclHeader.id)
            { throw new NotImplementedException("the current specialization could not be determined"); }

            void ProcessContent()
            {
                this._Scope._Output.Clear();
                this._Scope.Transform(body);
                foreach (var line in this._Scope._Output)
                { this.AddToOutput(line); }
            }
            if (this.nrSpecialzations != 1) // todo: needs to be adapted once we support type specializations
            {
                this.AddToOutput($"{this.currentSpec} {functorArg}");
                this.AddBlock(ProcessContent);
            }
            else
            {
                var comments = this.CurrentComments;
                ProcessContent();
                this.AddComments(comments.ClosingComments);
            }
            return new Tuple<QsTuple<LocalVariableDeclaration<QsLocalSymbol>>, QsScope>(argTuple, body);
        }

        public override void onInvalidGeneratorDirective()
        {
            this.beforeInvalidFunctorGenerator?.Invoke();
            this.AddDirective($"{this.currentSpec} {InvalidFunctorGenerator}");
        }

        public override void onDistributeDirective() =>
            this.AddDirective($"{this.currentSpec} {Keywords.distributeFunctorGenDirective.id}");

        public override void onInvertDirective() =>
            this.AddDirective($"{this.currentSpec} {Keywords.invertFunctorGenDirective.id}");

        public override void onSelfInverseDirective() =>
            this.AddDirective($"{this.currentSpec} {Keywords.selfFunctorGenDirective.id}");

        public override void onIntrinsicImplementation() =>
            this.AddDirective($"{this.currentSpec} {Keywords.intrinsicFunctorGenDirective.id}");

        public override void onExternalImplementation()
        {
            this.beforeExternalImplementation?.Invoke();
            this.AddDirective($"{this.currentSpec} {ExternalImplementation}");
        }

        public override QsSpecialization beforeSpecialization(QsSpecialization spec)
        {
            var precededByCode = PrecededByCode(this._Output);
            var precededByBlock = PrecededByBlock(this._Output);
            if (precededByCode && (precededByBlock || spec.Implementation.IsProvided || spec.Documentation.Any())) this.AddToOutput("");
            this.CurrentComments = spec.Comments;
            this.AddComments(spec.Comments.OpeningComments);
            if (spec.Comments.OpeningComments.Any() && spec.Documentation.Any()) this.AddToOutput("");
            this.AddDocumentation(spec.Documentation);
            return spec;
        }

        public override QsSpecialization onBodySpecialization(QsSpecialization spec)
        {
            this.currentSpec = Keywords.bodyDeclHeader.id;
            return base.onBodySpecialization(spec);
        }

        public override QsSpecialization onAdjointSpecialization(QsSpecialization spec)
        {
            this.currentSpec = Keywords.adjDeclHeader.id;
            return base.onAdjointSpecialization(spec); 
        }

        public override QsSpecialization onControlledSpecialization(QsSpecialization spec)
        {
            this.currentSpec = Keywords.ctrlDeclHeader.id;
            return base.onControlledSpecialization(spec); 
        }

        public override QsSpecialization onControlledAdjointSpecialization(QsSpecialization spec)
        {
            this.currentSpec = Keywords.ctrlAdjDeclHeader.id;
            return base.onControlledAdjointSpecialization(spec); 
        }

        private QsCallable onCallable(QsCallable c, string declHeader)
        {
            if (!c.Kind.IsTypeConstructor)
            {
                this.AddToOutput("");
                this.CurrentComments = c.Comments;
                this.AddComments(c.Comments.OpeningComments);
                if (c.Comments.OpeningComments.Any() && c.Documentation.Any()) this.AddToOutput("");
                this.AddDocumentation(c.Documentation);
                foreach (var attribute in c.Attributes)
                { this.onAttribute(attribute); }
            }

            var signature = SyntaxTreeToQs.DeclarationSignature(c, this.Type, this._Scope._StatementKind.beforeInvalidSymbol);
            this._Scope._Expression._Type.onCharacteristicsExpression(c.Signature.Information.Characteristics);
            var characteristics = this._Scope._Expression._Type.Output;

            var userDefinedSpecs = c.Specializations.Where(spec => spec.Implementation.IsProvided);
            var specBundles = SpecializationBundleProperties.Bundle<QsSpecialization>(spec => spec.TypeArguments, spec => spec.Kind, userDefinedSpecs); 
            bool NeedsToBeExplicit (QsSpecialization s)
            {
                if (s.Kind.IsQsBody) return true;
                else if (s.Implementation is SpecializationImplementation.Generated gen)
                {
                    if (gen.Item.IsSelfInverse) return s.Kind.IsQsAdjoint;
                    if (s.Kind.IsQsControlled || s.Kind.IsQsAdjoint) return false;

                    var relevantUserDefinedSpecs = specBundles.TryGetValue(SpecializationBundleProperties.BundleId(s.TypeArguments), out var dict)
                        ? dict // there may be no user defined implementations for a certain set of type arguments, in which case there is no such entry in the dictionary
                        : ImmutableDictionary<QsSpecializationKind, QsSpecialization>.Empty;
                    var userDefAdj = relevantUserDefinedSpecs.ContainsKey(QsSpecializationKind.QsAdjoint);
                    var userDefCtl = relevantUserDefinedSpecs.ContainsKey(QsSpecializationKind.QsControlled);
                    if (gen.Item.IsInvert) return userDefAdj || !userDefCtl;
                    if (gen.Item.IsDistribute) return userDefCtl && !userDefAdj;
                    return false; 
                }
                else return !s.Implementation.IsIntrinsic;
            }
            c = c.WithSpecializations(specs => specs.Where(NeedsToBeExplicit).ToImmutableArray());
            this.nrSpecialzations = c.Specializations.Length;

            this.AddToOutput($"{declHeader} {signature}");
            if (!String.IsNullOrWhiteSpace(characteristics)) this.AddToOutput($"{Keywords.qsCharacteristics.id} {characteristics}");
            this.AddBlock(() => c.Specializations.Select(dispatchSpecialization).ToImmutableArray());
            this.AddToOutput("");
            return c;
        }

        public override QsCallable onFunction(QsCallable c) =>
            this.onCallable(c, Keywords.fctDeclHeader.id);

        public override QsCallable onOperation(QsCallable c) =>
            this.onCallable(c, Keywords.opDeclHeader.id);

        public override QsCallable onTypeConstructor(QsCallable c) => c; // no code for these
        public override QsCustomType onType(QsCustomType t)
        {
            this.AddToOutput("");
            this.CurrentComments = t.Comments; // no need to deal with closing comments (can't exist), but need to make sure CurrentComments is up to date
            this.AddComments(t.Comments.OpeningComments);
            if (t.Comments.OpeningComments.Any() && t.Documentation.Any()) this.AddToOutput("");
            this.AddDocumentation(t.Documentation);
            foreach (var attribute in t.Attributes)
            { this.onAttribute(attribute); } 

            (string, ResolvedType) GetItemNameAndType (QsTypeItem item)
            {
                if (item is QsTypeItem.Named named) return (named.Item.VariableName.Value, named.Item.Type);
                else if (item is QsTypeItem.Anonymous type) return (null, type.Item);
                else throw new NotImplementedException("unknown case for type item");
            }
            var udtTuple = ArgumentTuple<QsTypeItem>(t.TypeItems, GetItemNameAndType, this.Type); 
            this.AddDirective($"{Keywords.typeDeclHeader.id} {t.FullName.Name.Value} = {udtTuple}");
            return t;
        }

        public override QsDeclarationAttribute onAttribute(QsDeclarationAttribute att)
        {
            // do *not* set CurrentComments!
            this._Scope._Expression.Transform(att.Argument);
            var arg = this._Scope._Expression._Kind.Output;
            var argStr = att.Argument.Expression.IsValueTuple || att.Argument.Expression.IsUnitValue ? arg : $"({arg})";
            var id = att.TypeId.IsValue  
                ? Identifier.NewGlobalCallable(new QsQualifiedName(att.TypeId.Item.Namespace, att.TypeId.Item.Name))
                : Identifier.InvalidIdentifier;
            this._Scope._Expression._Kind.onIdentifier(id, QsNullable<ImmutableArray<ResolvedType>>.Null);
            this.AddComments(att.Comments.OpeningComments);
            this.AddToOutput($"@ {this._Scope._Expression._Kind.Output}{argStr}");
            return att;
        }

        public override QsNamespace Transform(QsNamespace ns)
        {
            var scope = new ScopeToQs(new TransformationContext { CurrentNamespace = ns.Name.Value });
            var generator = new SyntaxTreeToQs(scope);
            generator.SetAllInvalid(this);

            generator.AddToOutput($"{Keywords.namespaceDeclHeader.id} {ns.Name.Value}");
            generator.AddBlock(() => generator.ProcessNamespaceElements(ns.Elements));
            this._Output.AddRange(generator._Output);
            return ns;
        }
    }
}

