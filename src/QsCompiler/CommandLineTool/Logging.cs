﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler.Diagnostics 
{
    public class ConsoleLogger : LogTracker
    {
        private readonly Func<Diagnostic, string> ApplyFormatting;

        protected internal virtual string Format(Diagnostic msg) =>
            this.ApplyFormatting(msg); 

        protected sealed override void Print(Diagnostic msg) =>
            PrintToConsole(msg.Severity, this.Format(msg));

        public ConsoleLogger(
            Func<Diagnostic, string> format = null,
            DiagnosticSeverity verbosity = DiagnosticSeverity.Warning,
            IEnumerable<int> noWarn = null, int lineNrOffset = 0)
        : base(verbosity, noWarn, lineNrOffset) =>
            this.ApplyFormatting = format ?? Formatting.HumanReadableFormat;

        /// <summary>
        /// Prints the given message to the Console. 
        /// Errors and Warnings are printed to the error stream.
        /// Throws an ArgumentNullException if the given message is null. 
        /// </summary>
        private static void PrintToConsole(DiagnosticSeverity severity, string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            var (stream, color) =
                severity == DiagnosticSeverity.Error ? (Console.Error, ConsoleColor.Red) :
                severity == DiagnosticSeverity.Warning ? (Console.Error, ConsoleColor.Yellow) :
                (Console.Out, Console.ForegroundColor);

            var consoleColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            try
            {
                var output = message;
                stream.WriteLine(output);
            }
            finally { Console.ForegroundColor = consoleColor; }
        }

        /// <summary>
        /// Prints a summary containing the currently counted number of errors, warnings and exceptions.
        /// Indicates a compilation failure if the given status does not correspond to the ReturnCode indicating a success. 
        /// </summary>
        public virtual void ReportSummary(int status = CommandLineCompiler.ReturnCode.SUCCESS)
        {
            string ItemString(int nr, string name) => $"{nr} {name}{(nr == 1 ? "" : "s")}";
            var errors = ItemString(this.NrErrorsLogged, "error");
            var warnings = ItemString(this.NrWarningsLogged, "warning");
            var exceptions = this.NrExceptionsLogged > 0 
                ? $"\n{ItemString(this.NrExceptionsLogged, "logged exception")}"
                : "";

            Console.WriteLine("\n____________________________________________\n");
            if (status == CommandLineCompiler.ReturnCode.SUCCESS)
            {
                Console.WriteLine($"Q#: Success! ({errors}, {warnings}) {exceptions}\n");
                return;
            }
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            try { Console.WriteLine($"Q# compilation failed: {errors}, {warnings} {exceptions}\n"); }
            finally { Console.ForegroundColor = color; }
        }
    }
}
