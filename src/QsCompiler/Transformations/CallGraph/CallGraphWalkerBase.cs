// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
{
    using Range = DataTypes.Range;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, string>, ResolvedType>;

    internal static partial class BuildCallGraph
    {
        private static class CallGraphWalkerBase<TGraph, TNode, TEdge>
            where TGraph : CallGraphBuilder<TNode, TEdge>
            where TNode : CallGraphNodeBase
            where TEdge : CallGraphEdgeBase
        {
            public abstract class TransformationState
            {
                internal TNode? CurrentNode;
                internal readonly TGraph Graph;

                // The type parameter resolutions of the current expression.
                internal Stack<TypeParameterResolutions> ExprTypeParamResolutions = new Stack<TypeParameterResolutions>();
                internal QsNullable<Position> CurrentStatementOffset;
                internal QsNullable<Range> CurrentExpressionRange;
                internal readonly Stack<TNode> RequestStack = new Stack<TNode>(); // Used to keep track of the nodes that still need to be walked by the walker.
                internal readonly HashSet<TNode> ResolvedNodeSet = new HashSet<TNode>(); // Used to keep track of the nodes that have already been walked by the walker.

                internal TransformationState(TGraph graph)
                {
                    this.Graph = graph;
                }

                /// <summary>
                /// Adds dependency to the graph from the current callable to the callable referenced by the given identifier.
                /// </summary>
                internal abstract void AddDependency(QsQualifiedName identifier);
            }

            public class StatementWalker<TState> : StatementTransformation<TState>
                where TState : TransformationState
            {
                public StatementWalker(SyntaxTreeTransformation<TState> parent)
                    : base(parent, TransformationOptions.NoRebuild)
                {
                }

                public override QsStatement OnStatement(QsStatement stm)
                {
                    this.SharedState.ExprTypeParamResolutions.Clear();
                    this.SharedState.CurrentStatementOffset = stm.Location.IsValue
                        ? QsNullable<Position>.NewValue(stm.Location.Item.Offset)
                        : QsNullable<Position>.Null;
                    return base.OnStatement(stm);
                }
            }

            public class ExpressionWalker<TState> : ExpressionTransformation<TState>
                where TState : TransformationState
            {
                public ExpressionWalker(SyntaxTreeTransformation<TState> parent)
                    : base(parent, TransformationOptions.NoRebuild)
                {
                }

                public override TypedExpression OnTypedExpression(TypedExpression ex)
                {
                    var contextRange = this.SharedState.CurrentExpressionRange;
                    this.SharedState.CurrentExpressionRange = ex.Range;

                    if (ex.TypeParameterResolutions.Any())
                    {
                        this.SharedState.ExprTypeParamResolutions.Push(ex.TypeParameterResolutions);
                    }
                    var result = base.OnTypedExpression(ex);
                    this.SharedState.CurrentExpressionRange = contextRange;
                    return result;
                }
            }
        }
    }
}
