﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;

using Lsp = Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = Microsoft.Quantum.QsCompiler.DataTypes.Position;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    public static class Diagnostics
    {
        internal static ImmutableList<char> ExpectedEndings(ErrorCode invalidFragmentEnding) =>
            invalidFragmentEnding switch
            {
                ErrorCode.ExpectingOpeningBracket => ImmutableList.Create('{'),
                ErrorCode.ExpectingSemicolon => ImmutableList.Create(';'),
                ErrorCode.ExpectingOpeningBracketOrSemicolon => ImmutableList.Create('{', ';'),
                ErrorCode.UnexpectedFragmentDelimiter => ImmutableList.Create(CodeFragment.MissingDelimiter),
                _ => throw new ArgumentException("Unrecognized fragment ending.")
            };

        private static DiagnosticSeverity Severity(QsCompilerDiagnostic msg)
        {
            if (msg.Diagnostic.IsError)
            {
                return DiagnosticSeverity.Error;
            }
            else if (msg.Diagnostic.IsWarning)
            {
                return DiagnosticSeverity.Warning;
            }
            else if (msg.Diagnostic.IsInformation)
            {
                return DiagnosticSeverity.Information;
            }
            else
            {
                throw new NotImplementedException("Hints are currently not supported - they need to be added to Diagnostics.fs in the QsLanguageProcessor, and here.");
            }
        }

        private static string Code(QsCompilerDiagnostic msg)
        {
            if (msg.Diagnostic.IsError)
            {
                return Errors.Error(msg.Code);
            }
            else if (msg.Diagnostic.IsWarning)
            {
                return Warnings.Warning(msg.Code);
            }
            else if (msg.Diagnostic.IsInformation)
            {
                return Informations.Information(msg.Code);
            }
            else
            {
                throw new NotImplementedException("Hints are currently not supported - they need to be added to Diagnostics.fs in the QsLanguageProcessor, and here.");
            }
        }

        /// <summary>
        /// Generates a suitable <see cref="Diagnostic"/> from <paramref name="msg"/> returned by the Q# compiler.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The contained range contains zero or negative entries, or its Start is bigger than its End.</exception>
        /// <remarks>
        /// The message range contained in <paramref name="msg"/> is first converted to a <see cref="Position"/> object,
        /// and then added to <paramref name="positionOffset"/> if the latter is not null.
        /// </remarks>
        internal static Diagnostic Generate(string filename, QsCompilerDiagnostic msg, Position? positionOffset = null) =>
            new Diagnostic
            {
                Severity = Severity(msg),
                Code = Code(msg),
                Source = filename,
                Message = msg.Message,
                Range = ((positionOffset ?? Position.Zero) + msg.Range).ToLsp()
            };

        private const string CodePrefix = "QS";

        internal static string CodeString(int code) => CodePrefix + code.ToString("D4");

        public static bool TryGetCode(string str, out int code)
        {
            code = -1;
            str = str?.Trim() ?? "";
            return !string.IsNullOrWhiteSpace(str) &&
                str.StartsWith(CodePrefix, StringComparison.InvariantCultureIgnoreCase) &&
                int.TryParse(str.Substring(2), out code);
        }
    }

    public static class Informations
    {
        public static string Code(this InformationCode code) => Information((int)code);

        internal static string Information(int code) => Diagnostics.CodeString(code);
    }

    public static class Warnings
    {
        public static string Code(this WarningCode code) => Warning((int)code);

        internal static string Warning(int code) => Diagnostics.CodeString(code);

        // warnings 70**

        public static Diagnostic LoadWarning(WarningCode code, IEnumerable<string> args, string? source) =>
            new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Code = Code(code),
                Source = source,
                Message = DiagnosticItem.Message(code, args ?? Enumerable.Empty<string>()),
                Range = new Lsp.Range { Start = new Lsp.Position(0, 0), End = new Lsp.Position(0, 0) }
            };

        // warnings 20**

        internal static Diagnostic EmptyStatementWarning(string filename, Position pos)
        {
            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Code = WarningCode.ExcessSemicolon.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(WarningCode.ExcessSemicolon, Enumerable.Empty<string>()),
                Range = new Lsp.Range { Start = pos.ToLsp(), End = pos.ToLsp() }
            };
        }
    }

    public static class Errors
    {
        public static string Code(this ErrorCode code) => Error((int)code);

        internal static string Error(int code) => Diagnostics.CodeString(code);

        // errors 70**

        public static Diagnostic LoadError(ErrorCode code, IEnumerable<string> args, string? source) =>
            new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = Code(code),
                Source = source,
                Message = DiagnosticItem.Message(code, args ?? Enumerable.Empty<string>()),
                Range = new Lsp.Range { Start = new Lsp.Position(0, 0), End = new Lsp.Position(0, 0) }
            };

        // errors 20**

        internal static Diagnostic InvalidFragmentEnding(string filename, ErrorCode code, Position pos)
        {
            if (pos == null)
            {
                throw new ArgumentNullException(nameof(pos));
            }

            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = Code(code),
                Source = filename,
                Message = DiagnosticItem.Message(code, Enumerable.Empty<string>()),
                Range = new Lsp.Range { Start = pos.ToLsp(), End = pos.ToLsp() }
            };
        }

        internal static Diagnostic MisplacedOpeningBracketError(string filename, Position pos)
        {
            if (pos == null)
            {
                throw new ArgumentNullException(nameof(pos));
            }

            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = ErrorCode.MisplacedOpeningBracket.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(ErrorCode.MisplacedOpeningBracket, Enumerable.Empty<string>()),
                Range = new Lsp.Range { Start = pos.ToLsp(), End = pos.ToLsp() }
            };
        }

        // errors 10**

        internal static Diagnostic ExcessBracketError(string filename, Position pos)
        {
            if (pos == null)
            {
                throw new ArgumentNullException(nameof(pos));
            }

            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = ErrorCode.ExcessBracketError.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(ErrorCode.ExcessBracketError, Enumerable.Empty<string>()),
                Range = new Lsp.Range { Start = pos.ToLsp(), End = pos.ToLsp() }
            };
        }

        internal static Diagnostic MissingClosingBracketError(string filename, Position pos)
        {
            if (pos == null)
            {
                throw new ArgumentNullException(nameof(pos));
            }

            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = ErrorCode.MissingBracketError.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(ErrorCode.MissingBracketError, Enumerable.Empty<string>()),
                Range = new Lsp.Range { Start = pos.ToLsp(), End = pos.ToLsp() }
            };
        }

        internal static Diagnostic MissingStringDelimiterError(string filename, Position pos)
        {
            if (pos == null)
            {
                throw new ArgumentNullException(nameof(pos));
            }

            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = ErrorCode.MissingStringDelimiterError.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(ErrorCode.MissingStringDelimiterError, Enumerable.Empty<string>()),
                Range = new Lsp.Range { Start = pos.ToLsp(), End = pos.ToLsp() }
            };
        }

        internal static Diagnostic InvalidCharacterInInterpolatedArgument(string filename, Position pos, char invalidCharacter)
        {
            if (pos == null)
            {
                throw new ArgumentNullException(nameof(pos));
            }

            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = ErrorCode.InvalidCharacterInInterpolatedArgument.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(ErrorCode.InvalidCharacterInInterpolatedArgument, new[] { invalidCharacter.ToString() }),
                Range = new Lsp.Range { Start = pos.ToLsp(), End = pos.ToLsp() }
            };
        }
    }
}
