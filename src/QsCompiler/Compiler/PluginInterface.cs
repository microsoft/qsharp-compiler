// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler
{
    public interface IRewriteStep
    {
        /// <summary>
        /// User facing name identifying the rewrite step used for logging and in diagnostics. 
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The priority of the transformation relative to other transformations within the same dll. 
        /// Steps with higher priority will be executed first. 
        /// </summary>
        public int Priority { get; }
        /// <summary>
        /// Dictionary that will be populated when the rewrite step is loaded. 
        /// It contains the assembly constants for the Q# compilation unit on which the rewrite step is acting.
        /// </summary>
        public IDictionary<string, string> AssemblyConstants { get; }

        /// <summary>
        /// Indicates whether or not the rewrite step intends to modify the compilation in any form. 
        /// If a transformation is implemented, then that transformation will be executed only if either 
        /// no precondition verification is implemented, or the implemented precondition verification succeeds. 
        /// </summary>
        public bool ImplementsTransformation { get; }
        /// <summary>
        /// If a precondition verification is implemented, that verification is executed prior to executing anything else. 
        /// If the verification fails, nothing further is executed and the rewrite step is terminated. 
        /// A precondition verification should *not* throw an exception if the precondition is not satisfied, 
        /// but indicate this via the returned value. More detailed information can be provided via loggging. 
        /// </summary>
        public bool ImplementsPreconditionVerification { get; }
        /// <summary>
        /// A postcondition verification provides the means for detailed checks and guarantees for debugging purposes.
        /// If a postcondition verification is implemented, then that verification may be executed after a transformation is applied. 
        /// If the transformation is not applied, e.g. because the precondition verification failed, 
        /// then the postcondition verification is not executed either. 
        /// </summary>
        public bool ImplementsPostconditionVerification { get; }

        /// <summary>
        /// Implements a rewrite step transforming a Q# compilation. 
        /// <see cref="ImplementsTransformation"/> indicates whether or not this method is implemented. 
        /// </summary>
        /// <param name="compilation">Q# compilation that satisfies the implemented precondition, if any.</param>
        /// <param name="transformed">Q# compilation after transformation.</param>
        /// <returns>Whether or not the transformation succeeded.</returns>
        public bool Transformation(QsCompilation compilation, out QsCompilation transformed);
        /// <summary>
        /// Verifies whether a given compilation satisfies the precondition for executing this rewrite step. 
        /// <see cref="ImplementsPreconditionVerification"/> indicates whether or not this method is implemented. 
        /// </summary>
        /// <param name="compilation">Q# compilation for which to verify the precondition.</param>
        /// <returns>Whether or not the given compilation satisfies the precondition.</returns>
        public bool PreconditionVerification(QsCompilation compilation);
        /// <summary>
        /// Verifies whether a given compilation satisfies the postcondition after executing this rewrite step. 
        /// The verification is executed only if the transformation is implemented and executed, and may be skipped for performance reasons. 
        /// <see cref="ImplementsPostconditionVerification"/> indicates whether or not this method is implemented. 
        /// </summary>
        /// <param name="compilation">Q# compilation after performing the implemented transformation.</param>
        /// <returns>Whether or not the given compilation satisfies the postcondition of the transformation.</returns>
        public bool PostconditionVerification(QsCompilation compilation);
    }

}
