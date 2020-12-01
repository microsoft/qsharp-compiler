// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler.Diagnostics
{
    public interface ILogger
    {
        void Log(ErrorCode item, IEnumerable<string> args, string? source = null, LSP.Range? range = null);

        void Log(WarningCode item, IEnumerable<string> args, string? source = null, LSP.Range? range = null);

        void Log(InformationCode item, IEnumerable<string> args, string? source = null, LSP.Range? range = null, params string[] messageParam);

        void Log(params Diagnostic[] messages);

        void Log(Exception ex);
    }

    public abstract class LogTracker : ILogger
    {
        public DiagnosticSeverity Verbosity { get; set; }

        public int NrErrorsLogged { get; private set; }

        public int NrWarningsLogged { get; private set; }

        public int NrExceptionsLogged { get; private set; }

        private readonly int lineNrOffset;
        private readonly ImmutableArray<int> noWarn;

        public LogTracker(
            DiagnosticSeverity verbosity = DiagnosticSeverity.Warning,
            IEnumerable<int>? noWarn = null,
            int lineNrOffset = 0)
        {
            this.Verbosity = verbosity;
            this.NrErrorsLogged = 0;
            this.NrWarningsLogged = 0;
            this.NrExceptionsLogged = 0;

            this.lineNrOffset = lineNrOffset;
            this.noWarn = noWarn?.ToImmutableArray() ?? ImmutableArray<int>.Empty;
        }

        // methods that need to or can be specified by a deriving class

        /// <summary>
        /// Called whenever a diagnostic is logged after the diagnostic has been properly processed.
        /// </summary>
        protected internal abstract void Print(Diagnostic msg);

        /// <summary>
        /// Called whenever an exception is logged after the exception has been properly tracked.
        /// Prints the given exception as Hint if the logger verbosity is sufficiently high.
        /// </summary>
        protected internal virtual void OnException(Exception ex) =>
            this.Output(ex == null ? null : new Diagnostic
            {
                Severity = DiagnosticSeverity.Hint,
                Message = $"{Environment.NewLine}{ex}{Environment.NewLine}"
            });

        // routines for convenience

        /// <summary>
        /// Logs a diagnostic message based on the given error code,
        /// with the given source as the file for which the error occurred.
        /// </summary>
        public void Log(ErrorCode code, IEnumerable<string> args, string? source = null, LSP.Range? range = null) =>
            this.Log(new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = Errors.Code(code),
                Source = source,
                Message = DiagnosticItem.Message(code, args ?? Enumerable.Empty<string>()),
                Range = range
            });

        /// <summary>
        /// Logs a a diagnostic message based on the given warning code,
        /// with the given source as the file for which the error occurred.
        /// </summary>
        public void Log(WarningCode code, IEnumerable<string> args, string? source = null, LSP.Range? range = null) =>
            this.Log(new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Code = Warnings.Code(code),
                Source = source,
                Message = DiagnosticItem.Message(code, args ?? Enumerable.Empty<string>()),
                Range = range
            });

        /// <summary>
        /// Generates a Diagnostic message based on the given information code,
        /// with any message parameters appended on a new line to the message defined by the information code.
        /// The given source is listed as the file for which the error occurred.
        /// </summary>
        public void Log(InformationCode code, IEnumerable<string> args, string? source = null, LSP.Range? range = null, params string[] messageParam) =>
            this.Log(new Diagnostic
            {
                Severity = DiagnosticSeverity.Information,
                Code = null, // do not show a code for infos
                Source = source,
                Message = $"{DiagnosticItem.Message(code, args ?? Enumerable.Empty<string>())}{Environment.NewLine}{string.Join(Environment.NewLine, messageParam)}",
                Range = range
            });

        /// <summary>
        /// Logs the given diagnostic messages.
        /// Ignores any parameter that is null.
        /// </summary>
        public void Log(params Diagnostic[] messages)
        {
            if (messages == null)
            {
                return;
            }
            foreach (var m in messages)
            {
                if (m == null)
                {
                    continue;
                }
                this.Log(m);
            }
        }

        // core routines which track logged diagnostics

        /// <summary>
        /// Calls Print on the given diagnostic if the verbosity is sufficiently high.
        /// Does nothing if the given diagnostic is null.
        /// </summary>
        private void Output(Diagnostic? msg)
        {
            if (msg?.Severity <= this.Verbosity)
            {
                this.Print(msg);
            }
        }

        /// <summary>
        /// Increases the error or warning counter if appropriate, and
        /// prints the given diagnostic ff the logger verbosity is sufficiently high.
        /// Before printing, the line numbers are shifted by the offset specified upon initialization.
        /// Returns without doing anything if the given diagnostic is a warning that is to be ignored.
        /// </summary>
        public void Log(Diagnostic m)
        {
            if (m.Severity == DiagnosticSeverity.Warning &&
                CompilationBuilder.Diagnostics.TryGetCode(m.Code, out int code)
                && this.noWarn.Contains(code))
            {
                return;
            }

            if (m.Severity == DiagnosticSeverity.Error)
            {
                ++this.NrErrorsLogged;
            }
            if (m.Severity == DiagnosticSeverity.Warning)
            {
                ++this.NrWarningsLogged;
            }

            var msg = m.Range == null ? m : m.WithLineNumOffset(this.lineNrOffset);
            this.Output(msg);
        }

        /// <summary>
        /// Increases the exception counter and calls OnException with the given exception.
        /// </summary>
        public void Log(Exception ex)
        {
            ++this.NrExceptionsLogged;
            this.OnException(ex);
        }
    }

    public static class Formatting
    {
        public static IEnumerable<string>? Indent(params string[] items) =>
            items?.Select(msg => $"    {msg}");

        /// <summary>
        /// Returns a string that contains all information about the given diagnostic in human readable format.
        /// The string contains one-based position information if the range information is not null,
        /// assuming the given position information is zero-based.
        /// </summary>
        public static string HumanReadableFormat(Diagnostic msg)
        {
            var codeStr = msg.Code == null ? string.Empty : $" {msg.Code}";
            var (startLine, startChar) = (msg.Range?.Start?.Line + 1 ?? 0, msg.Range?.Start?.Character + 1 ?? 0);

            var source = msg.Source == null ? string.Empty : $"File: {msg.Source} \n";
            var position = msg.Range == null ? string.Empty : $"Position: [ln {startLine}, cn {startChar}] \n";
            var message = $"{source}{position}{msg.Message ?? "no details are available"}";
            return
                msg.Severity == DiagnosticSeverity.Error ? $"\nError{codeStr}: \n{message}" :
                msg.Severity == DiagnosticSeverity.Warning ? $"\nWarning{codeStr}: \n{message}" :
                msg.Severity == DiagnosticSeverity.Information ? $"\nInformation{codeStr}: \n{message}" :
                string.IsNullOrWhiteSpace(codeStr) ? $"\n{message}" : $"\n[{codeStr.Trim()}] {message}";
        }

        /// <summary>
        /// Returns a string that contains all information about the given diagnostic
        /// in a format that is detected and processed as a diagnostic by VS and VS Code.
        /// The string contains one-based position information if the range information is not null,
        /// assuming the given position information is zero-based.
        /// </summary>
        public static string MsBuildFormat(Diagnostic msg)
        {
            var codeStr = msg.Code == null ? string.Empty : $" {msg.Code}";
            var (startLine, startChar) = (msg.Range?.Start?.Line + 1 ?? 0, msg.Range?.Start?.Character + 1 ?? 0);

            var level =
                msg.Severity == DiagnosticSeverity.Error ? "error" :
                msg.Severity == DiagnosticSeverity.Warning ? "warning" :
                "info";
            var source = msg.Source ?? string.Empty;
            var position = msg.Range == null ? string.Empty : $"({startLine},{startChar})";
            return $"{source}{position}: {level}{codeStr}: {msg.Message}";
        }
    }
}
