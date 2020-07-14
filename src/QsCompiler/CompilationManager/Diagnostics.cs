﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder.DataStructures;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    public static class Diagnostics
    {
        internal static char ExpectedEnding(ErrorCode invalidFragmentEnding)
        {
            if (invalidFragmentEnding == ErrorCode.ExpectingOpeningBracket)
            {
                return '{';
            }
            else if (invalidFragmentEnding == ErrorCode.ExpectingSemicolon)
            {
                return ';';
            }
            else if (invalidFragmentEnding == ErrorCode.UnexpectedFragmentDelimiter)
            {
                return CodeFragment.MissingDelimiter;
            }
            else
            {
                throw new NotImplementedException("unrecognized fragment ending");
            }
        }

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
        /// Generates a suitable Diagnostic from the given CompilerDiagnostic returned by the Q# compiler.
        /// The message range contained in the given CompilerDiagnostic is first converted to a Position object,
        /// and then added to the given positionOffset if the latter is not null.
        /// Throws an ArgumentNullException if the Range of the given CompilerDiagnostic is null.
        /// Throws an ArgumentOutOfRangeException if the contained range contains zero or negative entries, or if its Start is bigger than its End.
        /// </summary>
        internal static Diagnostic Generate(string filename, QsCompilerDiagnostic msg, Position positionOffset = null)
        {
            if (msg.Range == null)
            {
                throw new ArgumentNullException(nameof(msg.Range));
            }
            return new Diagnostic
            {
                Severity = Severity(msg),
                Code = Code(msg),
                Source = filename,
                Message = msg.Message,
                Range = DiagnosticTools.GetAbsoluteRange(positionOffset, msg.Range)
            };
        }

        internal const string QsCodePrefix = "QS";

        public static bool TryGetCode(string str, out int code)
        {
            code = -1;
            str = str?.Trim();
            return !string.IsNullOrWhiteSpace(str) &&
                str.StartsWith(QsCodePrefix, StringComparison.InvariantCultureIgnoreCase) &&
                int.TryParse(str.Substring(2), out code);
        }
    }

    public static class Informations
    {
        public static string Code(this InformationCode code) =>
            Information((int)code);

        internal static string Information(int code) =>
            $"{Diagnostics.QsCodePrefix}{code}";
    }

    public static class Warnings
    {
        public static string Code(this WarningCode code) =>
            Warning((int)code);

        internal static string Warning(int code) =>
            $"{Diagnostics.QsCodePrefix}{code}";

        // warnings 70**

        public static Diagnostic LoadWarning(WarningCode code, IEnumerable<string> args, string source) =>
            new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Code = Code(code),
                Source = source,
                Message = DiagnosticItem.Message(code, args ?? Enumerable.Empty<string>()),
                Range = null
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
                Range = pos == null ? null : new LSP.Range { Start = pos, End = pos }
            };
        }
    }

    public static class Errors
    {
        public static string Code(this ErrorCode code) =>
            Error((int)code);

        internal static string Error(int code) =>
            $"{Diagnostics.QsCodePrefix}{code}";

        // errors 70**

        public static Diagnostic LoadError(ErrorCode code, IEnumerable<string> args, string source) =>
            new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = Code(code),
                Source = source,
                Message = DiagnosticItem.Message(code, args ?? Enumerable.Empty<string>()),
                Range = null
            };

        // errors 20**

        internal static Diagnostic InvalidFragmentEnding(string filename, ErrorCode code, Position pos)
        {
            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = Code(code),
                Source = filename,
                Message = DiagnosticItem.Message(code, Enumerable.Empty<string>()),
                Range = pos == null ? null : new LSP.Range { Start = pos, End = pos }
            };
        }

        internal static Diagnostic MisplacedOpeningBracketError(string filename, Position pos)
        {
            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = ErrorCode.MisplacedOpeningBracket.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(ErrorCode.MisplacedOpeningBracket, Enumerable.Empty<string>()),
                Range = pos == null ? null : new LSP.Range { Start = pos, End = pos }
            };
        }

        // errors 10**

        internal static Diagnostic ExcessBracketError(string filename, Position pos)
        {
            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = ErrorCode.ExcessBracketError.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(ErrorCode.ExcessBracketError, Enumerable.Empty<string>()),
                Range = pos == null ? null : new LSP.Range { Start = pos, End = pos }
            };
        }

        internal static Diagnostic MissingClosingBracketError(string filename, Position pos)
        {
            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = ErrorCode.MissingBracketError.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(ErrorCode.MissingBracketError, Enumerable.Empty<string>()),
                Range = pos == null ? null : new LSP.Range { Start = pos, End = pos }
            };
        }

        internal static Diagnostic MissingStringDelimiterError(string filename, Position pos)
        {
            return new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = ErrorCode.MissingStringDelimiterError.Code(),
                Source = filename,
                Message = DiagnosticItem.Message(ErrorCode.MissingStringDelimiterError, Enumerable.Empty<string>()),
                Range = pos == null ? null : new LSP.Range { Start = pos, End = pos }
            };
        }
    }
}
