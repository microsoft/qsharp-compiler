// -----------------------------------------------------------------------
// <copyright file="InternalCodeGeneratorException.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Exception generated when the internal state of the code generation cannot proceed due to an internal error</summary>
    [Serializable]
    public class InternalCodeGeneratorException
        : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="InternalCodeGeneratorException"/> class.</summary>
        public InternalCodeGeneratorException( )
        {
        }

        /// <summary>Initializes a new instance of the <see cref="InternalCodeGeneratorException"/> class.</summary>
        /// <param name="message">Message for the exception</param>
        public InternalCodeGeneratorException( string message )
            : base( message )
        {
        }

        /// <summary>Initializes a new instance of the <see cref="InternalCodeGeneratorException"/> class.</summary>
        /// <param name="message">Message for the exception</param>
        /// <param name="inner">Inner exception</param>
        public InternalCodeGeneratorException( string message, Exception inner )
            : base( message, inner )
        {
        }

        /// <summary>Initializes a new instance of the <see cref="InternalCodeGeneratorException"/> class from serialization</summary>
        /// <param name="serializationInfo">Serialization Info for construction</param>
        /// <param name="streamingContext">Context for construction</param>
        protected InternalCodeGeneratorException( System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext )
            : base( serializationInfo, streamingContext )
        {
        }
    }
}
