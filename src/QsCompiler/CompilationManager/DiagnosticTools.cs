// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    public static class DiagnosticTools
    {
        /// <summary>
        /// Returns the line and character of the given position as tuple without verifying them.
        /// Throws an ArgumentNullException if the given position is null.
        /// </summary>
        internal static Tuple<int, int> AsTuple(Position position) => 
            position != null 
            ? new Tuple<int, int>(position.Line, position.Character)
            : throw new ArgumentNullException(nameof(position));

        /// <summary>
        /// Returns a Position with the line and character given as tuple (inverse function for AsTuple).
        /// Throws an ArgumentNullException if the given tuple is null.
        /// </summary>
        internal static Position AsPosition(Tuple<int, int> position) =>
            position != null
            ? new Position(position.Item1, position.Item2)
            : throw new ArgumentNullException(nameof(position));

        /// <summary>
        /// Given the starting position, convertes the relative Position w.r.t. that starting position returned by the Q# compiler into an absolute position as expected by VS.
        /// IMPORTANT: The position returned by the Q# Compiler is (assumed to be) one-based, whereas the given and returned absolute positions are (assumed to be) zero-based!
        /// If the starting position is null, then this routine simply converts the PositionInfo given by the Q# compiler to the Position object that VS expects.
        /// Throws an ArgumentOutOfRangeException if the Line or Column of the given relative position are smaller than one.
        /// Throws an ArgumentException if the Line or Column of the given offset are smaller than zero.
        /// </summary>
        internal static Position GetAbsolutePosition(Position offset, QsPositionInfo relativePosition)
        {
            if (relativePosition.Line < 1 || relativePosition.Column < 1) throw new ArgumentOutOfRangeException(nameof(relativePosition));

            if (offset == null) return new Position(relativePosition.Line - 1, relativePosition.Column - 1); // fparsec position info is one based
            if (!Utils.IsValidPosition(offset)) throw new ArgumentException(nameof(offset));

            var absPos = offset.Copy();
            absPos.Line += relativePosition.Line - 1; // fparsec position info is one based
            absPos.Character = relativePosition.Line > 1 ? relativePosition.Column - 1 : absPos.Character + relativePosition.Column - 1; // VS expects zero based positions
            return absPos;
        }

        /// <summary>
        /// Given the starting position, convertes the relative range returned by the Q# compiler w.r.t. that starting position into an absolute range as expected by VS.
        /// IMPORTANT: The position returned by the Q# Compiler is (assumed to be) one-based, whereas the given and returned absolute positions are (assumed to be) zero-based!
        /// If the starting position is null, then this routine simply converts the PositionInfo given by the Q# compiler to the Position object that VS expects.
        /// Throws an ArgumentNullException if the given relative range is null. 
        /// Throws an ArgumentException if the Line or Column of the given offset are smaller than zero, 
        /// or if the end position (second item) of the given relative range is larger than the start position (first item). 
        /// Throws an ArgumentOutOfRangeException if the Line or Column of the given relative position are smaller than one.
        /// </summary>
        internal static Range GetAbsoluteRange(Position offset, Tuple<QsPositionInfo, QsPositionInfo> relativeRange)
        {
            bool LargerThan(QsPositionInfo lhs, QsPositionInfo rhs) =>
                lhs.Line > rhs.Line || (lhs.Line == rhs.Line && lhs.Column > rhs.Column); 
            if (relativeRange == null) throw new ArgumentNullException(nameof(relativeRange));
            if (LargerThan(relativeRange.Item1, relativeRange.Item2)) throw new ArgumentException("invalid range", nameof(relativeRange)); 
            return new Range { Start = GetAbsolutePosition(offset, relativeRange.Item1), End = GetAbsolutePosition(offset, relativeRange.Item2) };
        }

        /// <summary>
        /// Given the location information for a declared symbol,
        /// as well as the position of the declaration within which the symbol is declared, 
        /// returns the zero-based line and character index indicating the position of the symbol in the file.
        /// Returns null if the given object is not compatible with the position information generated by this CompilationBuilder.
        /// </summary>
        public static Tuple<int, int> SymbolPosition(QsLocation rootLocation, QsNullable<Tuple<int,int>> symbolPosition, Tuple<QsPositionInfo, QsPositionInfo> symbolRange)
        {
            var offset = symbolPosition.IsNull // the position offset is set to null (only) for variables defined in the declaration
                ? DeclarationPosition(rootLocation)
                : StatementPosition(rootLocation.Offset, symbolPosition.Item);
            return DeclarationPosition(GetAbsolutePosition(new Position(offset.Item1, offset.Item2), symbolRange.Item1));
        }

        /// <summary>
        /// Given the position of the specialization declaration to whose implementation the statement belongs, 
        /// and the position of the statement within the implementation, 
        /// returns the zero-based line and character index indicating the position of the statement in the file.
        /// Returns null if either one of the given objects is not compatible with the position information generated by this CompilationBuilder.
        /// </summary>
        public static Tuple<int, int> StatementPosition(QsLocation rootLocation, QsLocation statementLocation) =>
            StatementPosition(rootLocation.Offset, statementLocation.Offset);
        internal static Tuple<int, int> StatementPosition(Tuple<int,int> rootOffset, Tuple<int, int> statementPos) =>
            DeclarationPosition(AsPosition(rootOffset).Add(AsPosition(statementPos)));

        /// <summary>
        /// Given the location of a callable, type or specialization declaration, 
        /// returns the zero-based line and character index indicating the position of the declaration in the file.
        /// </summary>
        public static Tuple<int, int> DeclarationPosition(QsLocation location) => DeclarationPosition(AsPosition(location.Offset));
        private static Tuple<int, int> DeclarationPosition(Position position) => new Tuple<int, int>(position.Line, position.Character);

        /// <summary>
        /// Returns a new Position with the line number and character of the given Position
        /// or null in case the given Position is null.
        /// </summary>
        public static Position Copy(this Position pos)
        {
            return pos == null 
                ? null 
                : new Position(pos.Line, pos.Character);
        }

        /// <summary>
        /// Verifies the given Position, and returns a *new* Position with updated line number.
        /// Throws an ArgumentNullException if the given Position is null.
        /// Throws an ArgumentException if the given Position is invalid.
        /// Throws and ArgumentOutOfRangeException if the updated line number is negative.
        /// </summary>
        public static Position WithUpdatedLineNumber(this Position pos, int lineNrChange)
        {
            if (!Utils.IsValidPosition(pos)) throw new ArgumentException($"invalid Position in {nameof(WithUpdatedLineNumber)}");
            if (pos.Line + lineNrChange < 0) throw new ArgumentOutOfRangeException(nameof(lineNrChange));
            var updated = pos.Copy();
            updated.Line += lineNrChange;
            return updated;
        }

        /// <summary>
        /// For a given Range, returns a new Range with its starting and ending position a copy of the start and end of the given Range 
        /// (i.e. does a deep copy) or null in case the given Range is null.
        /// </summary>
        public static Range Copy(this Range r)
        {
            return r == null 
                ? null 
                : new Range { Start = r.Start.Copy(), End = r.End.Copy() };
        }

        /// <summary>
        /// Verifies the given Range, and returns a *new* Range with updated line numbers.
        /// Throws an ArgumentNullException if the given Range is null.
        /// Throws an ArgumentException if the given Range is invalid.
        /// Throws and ArgumentOutOfRangeException if the updated line number is negative.
        /// </summary>
        public static Range WithUpdatedLineNumber(this Range range, int lineNrChange)
        {
            if (lineNrChange == 0) return range ?? throw new ArgumentNullException(nameof(range));
            if (!Utils.IsValidRange(range)) throw new ArgumentException($"invalid Range in {nameof(WithUpdatedLineNumber)}"); // range can be empty
            if (range.Start.Line + lineNrChange < 0) throw new ArgumentOutOfRangeException(nameof(lineNrChange));
            var updated = range.Copy();
            updated.Start.Line += lineNrChange;
            updated.End.Line += lineNrChange;
            return updated;
        }

        /// <summary>
        /// Returns a new Diagnostic, making a deep copy of the given one (in particular a deep copy of it's Range)
        /// or null if the given Diagnostic is null.
        /// </summary>
        public static Diagnostic Copy(this Diagnostic message)
        {
            if (message == null) return null;
            return new Diagnostic()
            {
                Range = message.Range.Copy(),
                Severity = message.Severity,
                Code = message.Code,
                Source = message.Source,
                Message = message.Message
            };
        }

        /// <summary>
        /// For a given Diagnostic, verifies its range and returns a *new* Diagnostic with updated line numbers.
        /// Throws an ArgumentNullException if the given Diagnostic is null.
        /// Throws an ArgumentException if the Range of the given Diagnostic is invalid.
        /// Throws and ArgumentOutOfRangeException if the updated line number is negative.
        /// </summary>
        public static Diagnostic WithUpdatedLineNumber(this Diagnostic diagnostic, int lineNrChange)
        {
            if (lineNrChange == 0) return diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
            var updatedRange = diagnostic.Range.WithUpdatedLineNumber(lineNrChange); // throws if the given diagnostic is null
            var updated = diagnostic.Copy();
            updated.Range = updatedRange;
            return updated;
        }

        /// <summary>
        /// Returns a function that returns true if the ErrorType of the given Diagnostic is one of the given types.
        /// </summary>
        internal static Func<Diagnostic, bool> ErrorType(params ErrorCode[] types)
        {
            var codes = types.Select(err => err.Code());
            return m => m.IsError() && codes.Contains(m.Code);
        }

        /// <summary>
        /// Returns a function that returns true if the WarningType of the given Diagnostic is one of the given types.
        /// </summary>
        internal static Func<Diagnostic, bool> WarningType(params WarningCode[] types)
        {
            var codes = types.Select(warn => warn.Code());
            return m => m.IsWarning() && codes.Contains(m.Code);
        }

        /// <summary>
        /// Returns true if the given diagnostics is an error.
        /// </summary>
        public static bool IsError(this Diagnostic m) =>
            m.Severity == DiagnosticSeverity.Error;

        /// <summary>
        /// Returns true if the given diagnostics is a warning.
        /// </summary>
        public static bool IsWarning(this Diagnostic m) =>
            m.Severity == DiagnosticSeverity.Warning;

        /// <summary>
        /// Returns true if the given diagnostics is an information.
        /// </summary>
        public static bool IsInformation(this Diagnostic m) =>
            m.Severity == DiagnosticSeverity.Information;

        /// <summary>
        /// Extracts all elements satisfying the given condition and which start at a line that is larger or equal to lowerBound.
        /// Throws an ArgumentNullException if the given condition is null.
        /// </summary>
        public static IEnumerable<Diagnostic> Filter(this IEnumerable<Diagnostic> orig, Func<Diagnostic, bool> condition, int lowerBound)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return orig?.Where(m => condition(m) && lowerBound <= m.Range.Start.Line);
        }

        /// <summary>
        /// Extracts all elements satisfying the given condition and which start at a line that is larger or equal to lowerBound and smaller than upperBound.
        /// Throws an ArgumentNullException if the given condition is null.
        /// </summary>
        public static IEnumerable<Diagnostic> Filter(this IEnumerable<Diagnostic> orig, Func<Diagnostic, bool> condition, int lowerBound, int upperBound)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            return orig?.Where(m => condition(m) && lowerBound <= m.Range.Start.Line && m.Range.End.Line < upperBound);
        }

        /// <summary>
        /// Extracts all elements which start at a line that is larger or equal to lowerBound.
        /// </summary>
        public static IEnumerable<Diagnostic> Filter(this IEnumerable<Diagnostic> orig, int lowerBound)
        { return orig?.Filter(m => true, lowerBound); }

        /// <summary>
        /// Extracts all elements which start at a line that is larger or equal to lowerBound and smaller than upperBound.
        /// </summary>
        public static IEnumerable<Diagnostic> Filter(this IEnumerable<Diagnostic> orig, int lowerBound, int upperBound)
        { return orig?.Filter(m => true, lowerBound, upperBound); }


        /// <summary>
        /// Returns true if the start line of the given diagnostic is larger or equal to lowerBound.
        /// </summary>
        internal static bool SelectByStartLine(this Diagnostic m, int lowerBound)
        { return m?.Range?.Start?.Line == null ? false : lowerBound <= m.Range.Start.Line; }

        /// <summary>
        /// Returns true if the start line of the given diagnostic is larger or equal to lowerBound, and smaller than upperBound.
        /// </summary>
        internal static bool SelectByStartLine(this Diagnostic m, int lowerBound, int upperBound)
        { return m?.Range?.Start?.Line == null ? false : lowerBound <= m.Range.Start.Line && m.Range.Start.Line < upperBound; }

        /// <summary>
        /// Returns true if the end line of the given diagnostic is larger or equal to lowerBound.
        /// </summary>
        internal static bool SelectByEndLine(this Diagnostic m, int lowerBound)
        { return m?.Range?.End?.Line == null ? false : lowerBound <= m.Range.End.Line; }

        /// <summary>
        /// Returns true if the end line of the given diagnostic is larger or equal to lowerBound, and smaller than upperBound.
        /// </summary>
        internal static bool SelectByEndLine(this Diagnostic m, int lowerBound, int upperBound)
        { return m?.Range?.End?.Line == null ? false : lowerBound <= m.Range.End.Line && m.Range.End.Line < upperBound; }


        /// <summary>
        /// Returns true if the start position of the given diagnostic is larger or equal to lowerBound.
        /// </summary>
        internal static bool SelectByStart(this Diagnostic m, Position lowerBound)
        { return m?.Range?.Start?.Line == null ? false : lowerBound.IsSmallerThanOrEqualTo(m.Range.Start); }

        /// <summary>
        /// Returns true if the start position of the given diagnostic is larger or equal to lowerBound, and smaller than upperBound.
        /// </summary>
        internal static bool SelectByStart(this Diagnostic m, Position lowerBound, Position upperBound)
        { return m?.Range?.Start?.Line == null ? false : lowerBound.IsSmallerThanOrEqualTo(m.Range.Start) && m.Range.Start.IsSmallerThan(upperBound); }

        /// <summary>
        /// Returns true if the end position of the given diagnostic is larger or equal to lowerBound.
        /// </summary>
        internal static bool SelectByEnd(this Diagnostic m, Position lowerBound)
        { return m?.Range?.End?.Line == null ? false : lowerBound.IsSmallerThanOrEqualTo(m.Range.End); }

        /// <summary>
        /// Returns true if the end position of the given diagnostic is larger or equal to lowerBound, and smaller than upperBound.
        /// </summary>
        internal static bool SelectByEnd(this Diagnostic m, Position lowerBound, Position upperBound)
        { return m?.Range?.End?.Line == null ? false : lowerBound.IsSmallerThanOrEqualTo(m.Range.End) && m.Range.End.IsSmallerThan(upperBound); }
    }
}
