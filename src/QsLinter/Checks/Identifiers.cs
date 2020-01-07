// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.Compiler.Linter
{
    internal enum IdentifierKind
    {
        Global,
        Local
    }

    public partial class LintingStep
    {

        private void CheckIdentifier(string identifier, IdentifierKind kind = IdentifierKind.Global, SourceLocation? location = null)
        {
            switch (kind)
            {
                case IdentifierKind.Global:
                    if (!identifier.IsPascalCase())
                    {
                        Warn(WarningCategory.WrongIdentifierFormat, $"Identifier {identifier} was expected to be formatted as PascalCase.", location);
                    }
                    break;

                case IdentifierKind.Local:
                    if (!identifier.IsCamelCase())
                    {
                        Warn(WarningCategory.WrongIdentifierFormat, $"Identifier {identifier} was expected to be formatted as camelCase.", location);
                    }
                    break;

            }
        }
    }
}
