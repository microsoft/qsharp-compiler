// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.DependencyAnalysis;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

// ToDo: Review access modifiers

namespace Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
{
    using ExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using TypeParameterResolutions = ImmutableDictionary<Tuple<QsQualifiedName, NonNullable<string>>, ResolvedType>;

    /// <summary>
    /// This transformation walks through the compilation without changing it, building up a call graph as it does.
    /// This call graph is then returned to the user.
    /// </summary>
    public static class BuildCallGraph
    {
        /// <summary>
        /// Builds and returns the call graph for the given callables.
        /// </summary>
        public static CallGraph Apply(IEnumerable<QsCallable> callables)
        {
            var walker = new BuildGraph();

            foreach (var callable in callables)
            {
                walker.Namespaces.OnCallableDeclaration(callable);
            }

            return walker.SharedState.Graph;
        }

        /// <summary>
        /// Builds and returns the call graph for the given compilation.
        /// </summary>
        public static CallGraph Apply(QsCompilation compilation) =>
            compilation.EntryPoints.Any()
            ? ApplyWithEntryPoints(compilation)
            : ApplyWithoutEntryPoints(compilation);

        /// <summary>
        /// Runs the transformation on the a compilation with entry points. This will trim
        /// the resulting call graph to only include those callables that are related
        /// to an entry point.
        /// </summary>
        private static CallGraph ApplyWithEntryPoints(QsCompilation compilation)
        {
            var walker = new BuildGraph();

            var entryPointNodes = compilation.EntryPoints.Select(name =>
                new CallGraphNode(name, QsSpecializationKind.QsBody, QsNullable<ImmutableArray<ResolvedType>>.Null));

            walker.SharedState.IsLimitedToEntryPoints = true;
            walker.SharedState.RequestStack = new Stack<CallGraphNode>(entryPointNodes);
            walker.SharedState.ResolvedNodeSet = new HashSet<CallGraphNode>();

            var globals = compilation.Namespaces.GlobalCallableResolutions();
            while (walker.SharedState.RequestStack.TryPop(out var currentRequest))
            {
                // If there is a call to an unknown callable, throw exception
                if (!globals.TryGetValue(currentRequest.CallableName, out QsCallable currentCallable))
                {
                    throw new ArgumentException($"Couldn't find definition for callable: {currentRequest.CallableName}");
                }

                var relevantSpecs = currentCallable.Specializations.Where(s => s.Kind == currentRequest.Kind);

                // The current request must be added before it is processed to prevent
                // self-references from duplicating on the stack.
                walker.SharedState.ResolvedNodeSet.Add(currentRequest);

                var spec = relevantSpecs.First();
                walker.Namespaces.OnSpecializationDeclaration(spec);
            }

            return walker.SharedState.Graph;
        }

        /// <summary>
        /// Runs the transformation on the a compilation without any entry points. This
        /// will produce a call graph that contains all relationships amongst all callables
        /// in the compilation.
        /// </summary>
        private static CallGraph ApplyWithoutEntryPoints(QsCompilation compilation)
        {
            var walker = new BuildGraph();
            walker.OnCompilation(compilation);
            return walker.SharedState.Graph;
        }

        private class BuildGraph : SyntaxTreeTransformation<TransformationState>
        {
            public BuildGraph() : base(new TransformationState())
            {
                this.Namespaces = new NamespaceTransformation(this);
                this.Statements = new StatementTransformation(this);
                this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
                this.Expressions = new ExpressionTransformation(this);
                this.ExpressionKinds = new ExpressionKindTransformation(this);
                this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
            }
        }

        private class TransformationState
        {
            internal bool IsInCall = false;
            internal bool HasAdjointDependency = false;
            internal bool HasControlledDependency = false;
            internal QsSpecialization CurrentSpecialization;
            internal CallGraph Graph = new CallGraph();
            internal IEnumerable<TypeParameterResolutions> TypeParameterResolutions = new List<TypeParameterResolutions>();
            internal QsNullable<Position> CurrentStatementOffset;
            internal QsNullable<DataTypes.Range> CurrentExpressionRange;

            // Flag indicating if the call graph is being limited to only include callables that are related to entry points.
            internal bool IsLimitedToEntryPoints = false;
            // RequestStack and ResolvedCallableSet are not used if IsLimitedToEntryPoints is false.
            internal Stack<CallGraphNode> RequestStack = null; // Used to keep track of the nodes that still need to be walked by the walker.
            internal HashSet<CallGraphNode> ResolvedNodeSet = null; // Used to keep track of the nodes that have already been walked by the walker.
        }

        private class NamespaceTransformation : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec)
            {
                this.SharedState.CurrentSpecialization = spec;
                this.SharedState.Graph.AddNode(spec);
                return base.OnSpecializationDeclaration(spec);
            }
        }

        private class StatementTransformation : StatementTransformation<TransformationState>
        {
            public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override QsStatement OnStatement(QsStatement stm)
            {
                this.SharedState.CurrentStatementOffset = stm.Location.IsValue
                    ? QsNullable<Position>.NewValue(stm.Location.Item.Offset)
                    : QsNullable<Position>.Null;
                return base.OnStatement(stm);
            }
        }

        private class ExpressionTransformation : ExpressionTransformation<TransformationState>
        {
            public ExpressionTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override TypedExpression OnTypedExpression(TypedExpression ex)
            {
                var contextRange = this.SharedState.CurrentExpressionRange;
                this.SharedState.CurrentExpressionRange = ex.Range;

                if (ex.TypeParameterResolutions.Any())
                {
                    this.SharedState.TypeParameterResolutions = this.SharedState.TypeParameterResolutions.Prepend(ex.TypeParameterResolutions);
                }
                var rtrn = base.OnTypedExpression(ex);

                this.SharedState.CurrentExpressionRange = contextRange;

                return rtrn;
            }
        }

        private class ExpressionKindTransformation : ExpressionKindTransformation<TransformationState>
        {
            /// <summary>
            /// Adds an edge from the current caller to the called node to the call graph.
            /// </summary>
            private void PushEdge(CallGraphNode called, TypeParameterResolutions typeParamRes, DataTypes.Range referenceRange)
            {
                this.SharedState.Graph.AddDependency(new CallGraphNode(this.SharedState.CurrentSpecialization), called, typeParamRes, referenceRange);
                // If we are not processing all elements, then we need to keep track of what elements
                // have been processed, and which elements still need to be processed.
                if (this.SharedState.IsLimitedToEntryPoints
                    && !this.SharedState.RequestStack.Contains(called)
                    && !this.SharedState.ResolvedNodeSet.Contains(called))
                {
                    this.SharedState.RequestStack.Push(called);
                }
            }

            public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent) : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override ExpressionKind OnCallLikeExpression(TypedExpression method, TypedExpression arg)
            {
                var contextInCall = this.SharedState.IsInCall;
                this.SharedState.IsInCall = true;
                this.Expressions.OnTypedExpression(method);
                this.SharedState.IsInCall = contextInCall;
                this.Expressions.OnTypedExpression(arg);
                return ExpressionKind.InvalidExpr;
            }

            public override ExpressionKind OnAdjointApplication(TypedExpression ex)
            {
                this.SharedState.HasAdjointDependency = !this.SharedState.HasAdjointDependency;
                var result = base.OnAdjointApplication(ex);
                this.SharedState.HasAdjointDependency = !this.SharedState.HasAdjointDependency;
                return result;
            }

            public override ExpressionKind OnControlledApplication(TypedExpression ex)
            {
                var contextControlled = this.SharedState.HasControlledDependency;
                this.SharedState.HasControlledDependency = true;
                var result = base.OnControlledApplication(ex);
                this.SharedState.HasControlledDependency = contextControlled;
                return result;
            }

            public override ExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                if (sym is Identifier.GlobalCallable global)
                {
                    // Type arguments need to be resolved for the whole expression to be accurate
                    // ToDo: this needs adaption if we want to support type specializations
                    var typeArgs = QsNullable<ImmutableArray<ResolvedType>>.Null;

                    var combination = new TypeResolutionCombination(this.SharedState.TypeParameterResolutions.ToArray());
                    var typeParamRes = combination.CombinedResolutionDictionary.FilterByOrigin(global.Item);
                    this.SharedState.TypeParameterResolutions = new List<TypeParameterResolutions>();

                    var referenceRange = DataTypes.Range.Zero;
                    if (this.SharedState.CurrentStatementOffset.IsValue
                        && this.SharedState.CurrentExpressionRange.IsValue)
                    {
                        referenceRange = this.SharedState.CurrentStatementOffset.Item
                            + this.SharedState.CurrentExpressionRange.Item;
                    }

                    if (this.SharedState.IsInCall)
                    {
                        var kind = QsSpecializationKind.QsBody;
                        if (this.SharedState.HasAdjointDependency && this.SharedState.HasControlledDependency)
                        {
                            kind = QsSpecializationKind.QsControlledAdjoint;
                        }
                        else if (this.SharedState.HasAdjointDependency)
                        {
                            kind = QsSpecializationKind.QsAdjoint;
                        }
                        else if (this.SharedState.HasControlledDependency)
                        {
                            kind = QsSpecializationKind.QsControlled;
                        }

                        this.PushEdge(new CallGraphNode(global.Item, kind, typeArgs), typeParamRes, referenceRange);
                    }
                    else
                    {
                        // The callable is being used in a non-call context, such as being
                        // assigned to a variable or passed as an argument to another callable,
                        // which means it could get a functor applied at some later time.
                        // We're conservative and add all 4 possible kinds.
                        this.PushEdge(new CallGraphNode(global.Item, QsSpecializationKind.QsBody, typeArgs), typeParamRes, referenceRange);
                        this.PushEdge(new CallGraphNode(global.Item, QsSpecializationKind.QsControlled, typeArgs), typeParamRes, referenceRange);
                        this.PushEdge(new CallGraphNode(global.Item, QsSpecializationKind.QsAdjoint, typeArgs), typeParamRes, referenceRange);
                        this.PushEdge(new CallGraphNode(global.Item, QsSpecializationKind.QsControlledAdjoint, typeArgs), typeParamRes, referenceRange);
                    }
                }

                return ExpressionKind.InvalidExpr;
            }
        }
    }
}
