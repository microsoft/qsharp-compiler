using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations
{
    using QsRangeInfo = QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>;
    using QsExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;

    public static class FindExpressions
    {
        public static IList<TypedExpression> Find(Func<TypedExpression, bool> predicate, TypedExpression expression)
        {
            var transformation = new Transformation(new State(predicate));
            transformation.OnTypedExpression(expression);
            return transformation.SharedState.Expressions;
        }

        public static bool Contains(Func<TypedExpression, bool> predicate, TypedExpression expression) =>
            Find(predicate, expression).Any();

        private sealed class State
        {
            internal Func<TypedExpression, bool> Predicate { get; }

            internal IList<TypedExpression> Expressions { get; } = new List<TypedExpression>();

            internal State(Func<TypedExpression, bool> predicate) => Predicate = predicate;
        }

        private sealed class Transformation : ExpressionTransformation<State>
        {
            internal Transformation(State state) : base(state, TransformationOptions.NoRebuild)
            {
            }

            public override TypedExpression OnTypedExpression(TypedExpression expression)
            {
                if (SharedState.Predicate(expression))
                {
                    SharedState.Expressions.Add(expression);
                }
                return base.OnTypedExpression(expression);
            }
        }
    }

    public static class FindStatements
    {
        public static IList<QsStatement> Apply(Func<QsStatement, bool> predicate, QsScope scope)
        {
            var transformation = new Transformation(new State(predicate));
            transformation.OnScope(scope);
            return transformation.SharedState.Statements;
        }

        private sealed class State
        {
            internal Func<QsStatement, bool> Predicate { get; }

            internal IList<QsStatement> Statements { get; } = new List<QsStatement>();

            internal State(Func<QsStatement, bool> predicate) => Predicate = predicate;
        }

        private sealed class Transformation : StatementTransformation<State>
        {
            internal Transformation(State state) : base(state, TransformationOptions.NoRebuild)
            {
            }

            public override QsStatement OnStatement(QsStatement statement)
            {
                if (SharedState.Predicate(statement))
                {
                    SharedState.Statements.Add(statement);
                }
                return base.OnStatement(statement);
            }
        }
    }

    public static class UpdatedOutsideVariables
    {
        public static List<TypedExpression> Apply(QsScope scope)
        {
            var transformation = new SyntaxTreeTransformation<State>(new State(), TransformationOptions.NoRebuild);
            transformation.StatementKinds = new StatementKindTransformation(transformation);
            transformation.Statements.OnScope(scope);
            return transformation.SharedState.UpdatedOutsideVariables;
        }

        private static IEnumerable<string> Symbols(SymbolTuple tuple) => tuple switch
        {
            SymbolTuple.VariableName name => Enumerable.Repeat(name.Item.Value, 1),
            SymbolTuple.VariableNameTuple names => names.Item.SelectMany(Symbols),
            _ => Enumerable.Empty<string>()
        };

        private sealed class State
        {
            internal List<string> DeclaredVariables { get; } = new List<string>();

            internal List<TypedExpression> UpdatedOutsideVariables = new List<TypedExpression>();
        }

        private sealed class StatementKindTransformation : StatementKindTransformation<State>
        {
            internal StatementKindTransformation(SyntaxTreeTransformation<State> parent) : base(parent)
            {
            }

            public override QsStatementKind OnVariableDeclaration(QsBinding<TypedExpression> binding)
            {
                SharedState.DeclaredVariables.AddRange(Symbols(binding.Lhs));
                return base.OnVariableDeclaration(binding);
            }

            public override QsStatementKind OnValueUpdate(QsValueUpdate update)
            {
                var variables = FindExpressions.Find(
                    expr =>
                        expr.Expression is QsExpressionKind.Identifier id &&
                        id.Item1 is Identifier.LocalVariable local &&
                        !SharedState.DeclaredVariables.Contains(local.Item.Value),
                    update.Lhs);
                SharedState.UpdatedOutsideVariables.AddRange(variables);
                return base.OnValueUpdate(update);
            }
        }
    }
}
