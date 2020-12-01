using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    public class QirTransformation : SyntaxTreeTransformation<GenerationContext>
    {
        public QirTransformation(QsCompilation compilation, Configuration config)
        : base(new GenerationContext(compilation, config), TransformationOptions.NoRebuild)
        {
            this.SharedState._Transformation = this;
            this.Namespaces = new QirNamespaceTransformation(this, TransformationOptions.NoRebuild);
            this.StatementKinds = new QirStatementKindTransformation(this, TransformationOptions.NoRebuild);
            this.Expressions = new QirExpressionTransformation(this, TransformationOptions.NoRebuild);
            this.ExpressionKinds = new QirExpressionKindTransformation(this, TransformationOptions.NoRebuild);
            this.Types = new QirTypeTransformation(this, TransformationOptions.NoRebuild);
        }

        public void Apply()
        {
            foreach (var ns in this.SharedState.Compilation.Namespaces)
            {
                this.Namespaces.OnNamespace(ns);
            }

            foreach (var epName in this.SharedState.Compilation.EntryPoints)
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
