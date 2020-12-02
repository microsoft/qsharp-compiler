// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.Transformations.LoopLifting
{
    /// <summary>
    /// Performs the transformation steps needed to lift any repeat-until loops
    /// into generated recursive operations.
    /// </summary>
    public static class LiftLoops
    {
        public static QsCompilation Apply(QsCompilation compilation)
        {
            return new LiftRepeatBodies().OnCompilation(compilation);
        }

        private class LiftRepeatBodies : ContentLifting.LiftContent<ContentLifting.LiftContent.TransformationState>
        {
            public LiftRepeatBodies() : base(new ContentLifting.LiftContent.TransformationState())
            { 

            }

            private new class StatementKindTransformation
                : ContentLifting.LiftContent<ContentLifting.LiftContent.TransformationState>.StatementKindTransformation
            {
                public StatementKindTransformation(SyntaxTreeTransformation<ContentLifting.LiftContent.TransformationState> parent)
                    : base(parent)
                {

                }

                public override QsStatementKind OnRepeatStatement(QsRepeatStatement statement)
                {
                    if (this.SharedState.LiftBody(statement.RepeatBlock.Body, out var callable, out var call))
                    {
                        this.SharedState.GeneratedOperations?.Add(callable);
                        var block = new QsPositionedBlock(
                            new QsScope(ImmutableArray.Create(call), statement.RepeatBlock.Body.KnownSymbols),
                            statement.RepeatBlock.Location,
                            statement.RepeatBlock.Comments);
                        return QsStatementKind.NewQsRepeatStatement(new QsRepeatStatement(
                            block,
                            statement.SuccessCondition,
                            statement.FixupBlock));
                    }

                    return QsStatementKind.NewQsRepeatStatement(statement);
                }
            }
        }
    }
}