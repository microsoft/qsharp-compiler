// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// An exception that is thrown when an attempt is made to access content that is not part of a file.
    /// </summary>
    public class FileContentException : Exception
    {
        /// <summary>
        /// Creates a <see cref="FileContentException"/> with the given message.
        /// </summary>
        public FileContentException(string message)
            : base(message)
        {
        }
    }
}
