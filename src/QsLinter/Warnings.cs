// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Quantum.Compiler.Linter
{
    public enum WarningCategory
    {
        // Identifier warnings
        WrongIdentifierFormat,

        // Documentation warnings
        MissingDocumentation,
        MissingInputDocumentation,
        MissingSummary,
        MathInSummary
    }

    public struct SourceRange<T>
    {
        public T Start { get; set; }
        public T End { get; set; }
    }

    public struct SourceLocation
    {
        public string SourceFile { get; set; }
        public (int, int)? Offset { get; set; }
        public SourceRange<SourcePosition>? Range { get; set; }
    }

    public struct SourcePosition
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public struct Warning
    {
        // We ignore namespaces and categories, since those are only
        // used for grouping messages.
        [JsonIgnore()]
        public string? Namespace { get; set; }
        [JsonIgnore()]
        public WarningCategory Category { get; set; }
        public string Message { get; set; }
        public SourceLocation? Location { get; set; }

        public string Format() =>
            Namespace == null
            ? $"W {Category}: {Message}"
            : $"W {Category} ({Namespace}): {Message}";
    }

    public class ContextManager : IDisposable
    {

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private readonly Action OnExit;

        public ContextManager(Action OnExit)
        {
            this.OnExit = OnExit;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    OnExit();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }
}
