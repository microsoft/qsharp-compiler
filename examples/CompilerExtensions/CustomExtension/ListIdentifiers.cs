// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;


namespace Microsoft.Quantum.Demos.CompilerExtensions.Demo
{
    /// <summary>
    /// Implements a transformation of a Q# compilation.
    /// The transformation adds a comment to each callable listing all identifiers used within the callable.
    /// </summary>
    public class ListIdentifiers
    : SyntaxTreeTransformation<ListIdentifiers.TransformationState>
    {
        // The constructor defines what transformations are executed, and whether the syntax tree nodes are rebuilt in the process.
        // Since all data structures are immutable, modifying the compilation requires rebuilding the nodes.
        public ListIdentifiers()
        : base(new TransformationState())
        {
            // We provide our own custom namespace transformation, and expression kind transformation.
            this.Namespaces = new ListIdentifiers.NamespaceTransformation(this);
            this.ExpressionKinds = new ListIdentifiers.ExpressionKindTransformation(this);
            // Since we only want to modify the QsCallable nodes, there is no need to rebuild statements and expressions.
            this.Statements = new StatementTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
            this.StatementKinds = new StatementKindTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
            this.Expressions = new ExpressionTransformation<TransformationState>(this, TransformationOptions.NoRebuild);
            // Since we are only interested in all occurring identifiers, we can omit walking type nodes entirely.
            this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
        }

        /// <summary>
        /// Adds a comment to each callable listing all identifiers used within the callable.
        /// </summary>
        public QsCompilation OnCompilation(QsCompilation compilation)
        {
            var namespaces = compilation.Namespaces
                .Select(this.Namespaces.OnNamespace)
                .ToImmutableArray();
            return new QsCompilation(namespaces, compilation.EntryPoints);
        }

        /// <summary>
        /// Class used to track the internal state of the transformation, as well as access any information based on it.
        /// These properties and methods are usually used by multiple subtransformations.
        /// </summary>
        public class TransformationState
        {
            private QsQualifiedName CurrentCallable;
            private readonly Dictionary<QsQualifiedName, ImmutableHashSet<string>.Builder> Identifiers =
                new Dictionary<QsQualifiedName, ImmutableHashSet<string>.Builder>();

            public ImmutableHashSet<string> IdentifiersInCallable(QsQualifiedName name) =>
                this.Identifiers.TryGetValue(name, out var ids) ? ids.ToImmutable() : null;

            internal bool SetCurrentCallable(QsQualifiedName name)
            {
                this.CurrentCallable = name;
                return name != null && this.Identifiers.TryAdd(name, ImmutableHashSet.CreateBuilder<string>());
            }

            internal bool AddIdentifier(string id)
            {
                var cannotSet = this.CurrentCallable == null || id == null;
                var exists = this.Identifiers.TryGetValue(this.CurrentCallable, out var ids);
                if (cannotSet || !exists) return false;
                else ids.Add(id);
                return true;
            }
        }

        /// <summary>
        /// Class that defines namespace transformations for ListIdentifiers.
        /// It adds a comment to each callable listing all identifiers used within the callable
        /// according to the information accumulated in the shared transformation state.
        /// </summary>
        private class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            internal NamespaceTransformation(ListIdentifiers parent)
            : base(parent)
            { }

            private static QsCallable AddComments(QsCallable c, params string[] comments) =>
                new QsCallable(
                    c.Kind, c.FullName, c.Attributes, c.Modifiers,
                    c.SourceFile, c.Location,
                    c.Signature, c.ArgumentTuple, c.Specializations,
                    c.Documentation, new QsComments(c.Comments.OpeningComments.AddRange(comments), c.Comments.ClosingComments));

            public override QsCallable OnCallableDeclaration(QsCallable c)
            {
                this.SharedState.SetCurrentCallable(c.FullName);
                c = base.OnCallableDeclaration(c);
                this.SharedState.SetCurrentCallable(null);
                var comment = $" {String.Join(", ", this.SharedState.IdentifiersInCallable(c.FullName))}";
                return AddComments(c, " Contained identifiers:", comment, "");
            }
        }

        /// <summary>
        /// Class that defines expression kind transformations for ListIdentifiers.
        /// It adds any identifier that occurs within an expression to the shared transformation state.
        /// The transformation does not modify the syntax tree nodes.
        /// </summary>
        private class ExpressionKindTransformation
        : ExpressionKindTransformation<TransformationState>
        {
            internal ExpressionKindTransformation(ListIdentifiers parent)
            : base(parent, TransformationOptions.NoRebuild)
            { }

            public override QsExpressionKind<TypedExpression, Identifier, ResolvedType> OnIdentifier
                (Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs)
            {
                var name =
                    sym is Identifier.LocalVariable var ? var.Item :
                    sym is Identifier.GlobalCallable global ? global.Item.ToString() :
                    null;

                if (name != null) this.SharedState.AddIdentifier(name);
                return base.OnIdentifier(sym, tArgs);
            }
        }
    }
}
