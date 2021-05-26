// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.Documentation.Linting
{
    internal static class LintingExtensions
    {
        internal static void InvokeRules(
            this IDictionary<string, (DiagnosticSeverity, IDocumentationLintingRule)> rules,
            Func<IDocumentationLintingRule, IEnumerable<LintingMessage>> invokeRule,
            Action<IRewriteStep.Diagnostic> onDiagnostic)
        {
            foreach ((var lintName, (var severity, var lintRule)) in rules)
            {
                foreach (var raisedDiagnostic in invokeRule(lintRule))
                {
                    onDiagnostic(
                        raisedDiagnostic.AsDiagnostic(severity, lintName));
                }
            }
        }
    }

    public class LintingMessage
    {
        public string? Message { get; set; }

        public Range? Range { get; set; }

        public string? Source { get; set; }

        public IRewriteStep.Diagnostic AsDiagnostic(
            DiagnosticSeverity severity = DiagnosticSeverity.Warning,
            string? ruleName = null)
        => new IRewriteStep.Diagnostic
            {
                Message = $"{(ruleName == null ? "" : $"({ruleName}) ")}{this.Message}",
                Range = this.Range,
                Severity = severity,
                Stage = IRewriteStep.Stage.Transformation,
                Source = this.Source,
            };
    }

    public interface IDocumentationLintingRule
    {
        IEnumerable<LintingMessage> OnTypeDeclaration(QsCustomType type, DocComment comment)
        {
            yield break;
        }

        IEnumerable<LintingMessage> OnCallableDeclaration(QsCallable callable, DocComment comment)
        {
            yield break;
        }
    }

    public class RequireExamplesOnPublicDeclarations : IDocumentationLintingRule
    {
        public IEnumerable<LintingMessage> OnCallableDeclaration(QsCallable callable, DocComment comment)
        {
            if (!callable.Access.IsPublic)
            {
                yield break;
            }

            if (comment.Examples.IsEmpty)
            {
                yield return new LintingMessage
                {
                    Message = $"Public callable {callable.FullName} does not have any examples.",
                    Source = callable.Source.AssemblyOrCodeFile,
                    Range = null, // TODO: provide more exact locations once supported by DocParser.
                };
            }
        }

        public IEnumerable<LintingMessage> OnTypeDeclaration(QsCustomType type, DocComment comment)
        {
            if (!type.Access.IsPublic)
            {
                yield break;
            }

            if (comment.Examples.IsEmpty)
            {
                yield return new LintingMessage
                {
                    Message = $"Public user-defined type {type.FullName} does not have any examples.",
                    Source = type.Source.AssemblyOrCodeFile,
                    Range = null, // TODO: provide more exact locations once supported by DocParser.
                };
            }
        }
    }

    public class NoMathInSummary : IDocumentationLintingRule
    {
        private IEnumerable<LintingMessage> OnComment(DocComment comment, string name, string? source)
        {
            if (comment.Summary.Contains("$"))
            {
                yield return new LintingMessage
                {
                    Message = $"Summary for {name} should not contain LaTeX notation.",
                    Source = source,
                    Range = null, // TODO: provide more exact locations once supported by DocParser.
                };
            }
        }

        public IEnumerable<LintingMessage> OnCallableDeclaration(QsCallable callable, DocComment comment) =>
            this.OnComment(comment, $"{callable.FullName.Namespace}.{callable.FullName.Name}", callable.Source.AssemblyOrCodeFile);

        public IEnumerable<LintingMessage> OnTypeDeclaration(QsCustomType type, DocComment comment) =>
            this.OnComment(comment, $"{type.FullName.Namespace}.{type.FullName.Name}", type.Source.AssemblyOrCodeFile);
    }

    public class RequireCorrectInputNames : IDocumentationLintingRule
    {
        public IEnumerable<LintingMessage> OnCallableDeclaration(QsCallable callable, DocComment comment)
        {
            var callableName =
                $"{callable.FullName.Namespace}.{callable.FullName.Name}";

            // Validate input and type parameter names.
            var inputDeclarations = callable.ArgumentTuple.ToDictionaryOfDeclarations();
            var inputMessages = this.ValidateNames(
                callableName,
                "input",
                name => inputDeclarations.ContainsKey(name),
                comment.Input.Keys,
                range: null, // TODO: provide more exact locations once supported by DocParser.
                source: callable.Source.AssemblyOrCodeFile);
            var typeParamMessages = this.ValidateNames(
                callableName,
                "type parameter",
                name => callable.Signature.TypeParameters.Any(
                    typeParam =>
                        typeParam is QsLocalSymbol.ValidName validName &&
                        validName.Item == name.TrimStart('\'')),
                comment.TypeParameters.Keys,
                range: null, // TODO: provide more exact locations once supported by DocParser.
                source: callable.Source.AssemblyOrCodeFile);

            return inputMessages.Concat(typeParamMessages);
        }

        public IEnumerable<LintingMessage> OnTypeDeclaration(QsCustomType type, DocComment comment)
        {
            // Validate named item names.
            var inputDeclarations = type.TypeItems.ToDictionaryOfDeclarations();
            return this.ValidateNames(
                $"{type.FullName.Namespace}.{type.FullName.Name}",
                "named item",
                name => inputDeclarations.ContainsKey(name),
                comment.Input.Keys,
                range: null, // TODO: provide more exact locations once supported by DocParser.
                source: type.Source.AssemblyOrCodeFile);
        }

        private IEnumerable<LintingMessage> ValidateNames(
            string symbolName,
            string nameKind,
            Func<string, bool> isNameValid,
            IEnumerable<string> actualNames,
            Range? range = null,
            string? source = null)
        {
            foreach (var name in actualNames)
            {
                if (!isNameValid(name))
                {
                    yield return new LintingMessage
                    {
                        Message = $"When documenting {symbolName}, found documentation for {nameKind} {name}, but no such {nameKind} exists.",
                        Range = range,
                        Source = source,
                    };
                }
            }
        }
    }
}
