// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    public class QirTransformation : SyntaxTreeTransformation<GenerationContext>
    {
        /// <summary>
        /// The compilation unit for which QIR is generated.
        /// </summary>
        public readonly QsCompilation Compilation;

        public QirTransformation(QsCompilation compilation, Configuration config)
        : base(new GenerationContext(compilation.Namespaces, config), TransformationOptions.NoRebuild)
        {
            this.SharedState.SetTransformation(this);
            this.Compilation = compilation;

            this.Namespaces = new QirNamespaceTransformation(this, TransformationOptions.NoRebuild);
            this.StatementKinds = new QirStatementKindTransformation(this, TransformationOptions.NoRebuild);
            this.Expressions = new QirExpressionTransformation(this, TransformationOptions.NoRebuild);
            this.ExpressionKinds = new QirExpressionKindTransformation(this, TransformationOptions.NoRebuild);
            this.Types = new QirTypeTransformation(this, TransformationOptions.NoRebuild);
        }

        public void Apply()
        {
            this.SharedState.InitializeRuntimeLibrary();
            this.SharedState.RegisterQuantumInstructions();

            foreach (var ns in this.Compilation.Namespaces)
            {
                this.Namespaces.OnNamespace(ns);
            }

            foreach (var epName in this.Compilation.EntryPoints)
            {
                this.SharedState.GenerateEntryPoint(epName);
            }
        }

        public void Emit()
        {
            this.SharedState.Emit();
        }
    }
}
