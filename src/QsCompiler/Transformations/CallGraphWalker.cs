// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;


namespace Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
{
    using Concretion = Dictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;
    using GetConcreteIdentifierFunc = Func<Identifier.GlobalCallable, /*ImmutableConcretion*/ ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>, Identifier>;
    using ImmutableConcretion = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using TypeArgsResolution = ImmutableArray<Tuple<QsQualifiedName, NonNullable<string>, ResolvedType>>;

    public class CallGraph
    {

    }

    public static class CallGraphWalker
    {
        private class BuildGraph : SyntaxTreeTransformation<BuildGraph.TransformationState>
        {
            public class TransformationState
            {
                internal bool inCall = false;
                internal bool adjoint = false;
                internal bool controlled = false;
            }

            private BuildGraph(ImmutableDictionary<NonNullable<string>, IEnumerable<QsCallable>> namespaceCallables) : base(new TransformationState()) 
            { 
                this.Namespaces = new NamespaceTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }

            private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
            {
                public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild) { }

                private ExpressionKind HandleCall(TypedExpression method, TypedExpression arg)
                {
                    var contextInCall = SharedState.inCall;
                    SharedState.inCall = true;
                    this.Expressions.OnTypedExpression(method);
                    SharedState.inCall = contextInCall;
                    this.Expressions.OnTypedExpression(arg);
                    return ExpressionKind.InvalidExpr;
                }

                public override ExpressionKind OnOperationCall(TypedExpression method, TypedExpression arg) => HandleCall(method, arg);

                public override ExpressionKind OnFunctionCall(TypedExpression method, TypedExpression arg) => HandleCall(method, arg);

                public override ExpressionKind OnAdjointApplication(TypedExpression ex)
                {
                    SharedState.adjoint = !SharedState.adjoint;
                    var rtrn = base.OnAdjointApplication(ex);
                    SharedState.adjoint = !SharedState.adjoint;
                    return rtrn;
                }

                public override ExpressionKind OnControlledApplication(TypedExpression ex)
                {
                    var contextControlled = SharedState.controlled;
                    SharedState.controlled = true;
                    var rtrn = base.OnControlledApplication(ex);
                    SharedState.controlled = contextControlled;
                    return rtrn;
                }

                public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
                {
                    return base.OnIdentifier(sym, tArgs);
                }
            }
        }
    }
}
