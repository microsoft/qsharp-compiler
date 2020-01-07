// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.Compiler.Linter
{
    public partial class LintingStep
    {

        private void CheckUdtDocumentation(QsCustomType udt)
        {
            if (udt.Documentation.IsEmpty)
            {
                Warn(
                    WarningCategory.MissingDocumentation,
                    $"No documentation on public UDT {udt.FullName.ToDottedName()}.",
                    location: udt.GetLocation()
                );
                return;
            }

            // TODO: make suggestions when documentation is present.
        }


        private void CheckCallableDocumentation(QsCallable callable)
        {
            if (callable.Documentation.IsEmpty)
            {
                Warn(
                    WarningCategory.MissingDocumentation,
                    $"No documentation on public {callable.Kind} {callable.FullName.ToDottedName()}.",
                    location: callable.GetLocation()
                );
                return;
            }

            var parsedComment = new DocComment(
                callable.Documentation,
                callable.FullName.Name.GetValue(),
                // We only check documentation on non-deprecated elements,
                // so we can safely tell the documentation parser that this
                // element is non-deprecated.
                false, ""
            );

            // Is there a summary?
            if (parsedComment.Summary.IsNullOrEmpty())
            {
                Warn(
                    WarningCategory.MissingSummary,
                    $"No summary on public {callable.Kind} {callable.FullName.ToDottedName()}.",
                    location: callable.GetLocation()
                );
            }

            // Is there LaTeX notation in the summary?
            if (parsedComment.Summary.Contains("$"))
            {
                Warn(
                    WarningCategory.MathInSummary,
                    $"LaTeX notation found in summary of {callable.Kind} {callable.FullName.ToDottedName()}.",
                    location: callable.GetLocation()
                );
            }

            if (parsedComment.Summary.ContainsMathSymbols())
            {
                Warn(
                    WarningCategory.MathInSummary,
                    $"Unicode math notation found in summary of {callable.Kind} {callable.FullName.ToDottedName()}.",
                    location: callable.GetLocation()
                );
            }

            // Check that each input has a corresponding section.
            foreach (var input in callable.ArgumentTuple.GetItems())
            {
                if (input.VariableName.TryGetName(out var name))
                {
                    if (!parsedComment.Input.ContainsKey(name))
                    {
                        Warn(
                            WarningCategory.MissingInputDocumentation,
                            $"Input {name} of {callable.Kind} {callable.FullName.ToDottedName()} is not documented.",
                            location: callable.GetLocation()
                        );
                    }
                }
            }
        }

        private void CheckNamespaceDocumentation(QsNamespace ns)
        {
            // TODO
        }
    }
}
