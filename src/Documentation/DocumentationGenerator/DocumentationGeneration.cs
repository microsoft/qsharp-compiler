// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.Documentation.Linting;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using QsAssemblyConstants = Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants;

namespace Microsoft.Quantum.Documentation
{
    /// <summary>
    ///     Rewrite step that generates API documentation from documentation
    ///     comments in the Q# source files being compiled.
    /// </summary>
    public class DocumentationGeneration : IRewriteStep
    {
        private static readonly ImmutableDictionary<string, (bool EnableByDefault, IDocumentationLintingRule Rule)> LintingRules;

        static DocumentationGeneration()
        {
            var rules = ImmutableDictionary.CreateBuilder<string, (bool EnableByDefault, IDocumentationLintingRule Rule)>();
            rules.Add("require-correct-input-names", (true, new RequireCorrectInputNames()));
            rules.Add("require-examples", (EnableByDefault: false, new RequireExamplesOnPublicDeclarations()));
            rules.Add("no-math-in-summary", (true, new NoMathInSummary()));
            LintingRules = rules.ToImmutableDictionary();
        }

        private string docsOutputPath = "";
        private readonly List<IRewriteStep.Diagnostic> diagnostics;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DocumentationGeneration"/> class.
        /// </summary>
        public DocumentationGeneration()
        {
            this.diagnostics = new List<IRewriteStep.Diagnostic>(); // collects diagnostics that will be displayed to the user
        }

        /// <inheritdoc/>
        public string Name => "DocumentationGeneration";

        /// <inheritdoc/>
        public int Priority => 0; // only compared within this dll

        /// <inheritdoc/>
        public IDictionary<string, string?> AssemblyConstants { get; } = new Dictionary<string, string?>();

        /// <inheritdoc/>
        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics => this.diagnostics;

        /// <inheritdoc/>
        public bool ImplementsPreconditionVerification => true;

        /// <inheritdoc/>
        public bool ImplementsTransformation => true;

        /// <inheritdoc/>
        public bool ImplementsPostconditionVerification => false;

        /// <inheritdoc/>
        public bool PreconditionVerification(QsCompilation compilation)
        {
            if (!this.AssemblyConstants.TryGetValue(QsAssemblyConstants.DocsOutputPath, out var path))
            {
                this.diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = $"Documentation generation was enabled, but precondition for {this.Name} was not satisfied: Missing assembly property {QsAssemblyConstants.DocsOutputPath}.",
                    Stage = IRewriteStep.Stage.PreconditionVerification,
                });
                return false;
            }

            if (string.IsNullOrEmpty(path))
            {
                this.diagnostics.Add(new IRewriteStep.Diagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = $"Documentation generation was enabled, but precondition for {this.Name} was not satisfied: Assembly property {QsAssemblyConstants.DocsOutputPath} was found, but was empty.",
                    Stage = IRewriteStep.Stage.PreconditionVerification,
                });
                return false;
            }

            this.docsOutputPath = path;

            // Diagnostics with severity Info or lower usually won't be displayed to the user.
            // If the severity is Error or Warning the diagnostic is shown to the user like any other compiler diagnostic,
            // and if the Source property is set to the absolute path of an existing file,
            // the user will be directed to the file when double clicking the diagnostics.
            this.diagnostics.Add(new IRewriteStep.Diagnostic
            {
                Severity = DiagnosticSeverity.Info,
                Message = $"Documentation generation was enabled, and precondition for {this.Name} was satisfied, writing docs to {this.docsOutputPath}.",
                Stage = IRewriteStep.Stage.PreconditionVerification,
            });

            return true;
        }

        /// <inheritdoc/>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
        {
            // Find a list of linting rules to be enabled and disabled by
            // by looking at the relevant assembly constant.
            // We expect linting rule configurations to be formatted as a comma-separated
            // list of rules, each one prefaced with either "ignore:", "warn:"
            // or "error:", indicating the level of severity for each.
            var lintingRulesConfig = (
                this.AssemblyConstants
                    .TryGetValue(QsAssemblyConstants.DocsLintingRules, out var config)
                        ? config ?? ""
                        : "")
                .Split(",")
                .Where(rule => !string.IsNullOrWhiteSpace(rule))
                .Select(rule =>
                    {
                        var ruleParts = rule.Split(":", 2);
                        if (ruleParts.Length != 2)
                        {
                            throw new Exception($"Error parsing documentation linting rule specification \"{rule}\"; expected a specification of the form \"severity:rule-name\".");
                        }

                        return (severity: ruleParts[0], ruleName: ruleParts[1]);
                    })
                .ToDictionary(
                    config => config.ruleName,
                    config => config.severity);

            // If any rules were specified that aren't present, warn about that
            // now.
            foreach ((var ruleName, var severity) in lintingRulesConfig)
            {
                if (!LintingRules.ContainsKey(ruleName))
                {
                    this.diagnostics.Add(new IRewriteStep.Diagnostic
                    {
                        Severity = DiagnosticSeverity.Info,
                        Message = $"Documentation linting rule {ruleName} was set to {severity}, but no such linting rule exists.",
                        Stage = IRewriteStep.Stage.Transformation,
                    });
                }
            }

            // Actually populate the rules now.
            var lintingRules = LintingRules
                .Select(
                    rule => (
                        Name: rule.Key,
                        Severity:
                            (
                                rule.Value.EnableByDefault,
                                lintingRulesConfig.TryGetValue(rule.Key, out var severity) ? severity : null)
                            switch
                            {
                                // We handle should happen when the user didn't
                                // override here.
                                (true, null) => DiagnosticSeverity.Warning,
                                (false, null) => DiagnosticSeverity.Hidden,

                                // If the user did override, we can ignore
                                // EnableByDefault.
                                (_, "ignore") => DiagnosticSeverity.Hidden,
                                (_, "warning") => DiagnosticSeverity.Warning,
                                (_, "error") => DiagnosticSeverity.Error,

                                // If we get down to this point, something went
                                // wrong; the given severity wasn't recognized.
                                (_, var unknown) => throw new Exception(
                                    $"Documentation linting severity for rule {rule.Key} was set to {unknown}, but expected one of \"error\", \"warning\", or \"ignore\""),
                            },
                        Rule: rule.Value.Rule))
                .Where(rule => rule.Severity != DiagnosticSeverity.Hidden)
                .ToDictionary(
                    rule => rule.Name,
                    rule => (rule.Severity, rule.Rule));

            var docProcessor = new ProcessDocComments(
                this.docsOutputPath,
                this.AssemblyConstants.TryGetValue(QsAssemblyConstants.DocsPackageId, out var packageName)
                    ? packageName
                    : null,
                lintingRules);

            docProcessor.OnDiagnostic += diagnostic =>
            {
                this.diagnostics.Add(diagnostic);
            };

            transformed = docProcessor.OnCompilation(compilation);
            return true;
        }

        /// <inheritdoc/>
        public bool PostconditionVerification(QsCompilation compilation) =>
            throw new NotImplementedException();
    }
}
