// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System
open System.Diagnostics.CodeAnalysis
open System.Runtime.CompilerServices


/// Exception raised for compiler internal errors (i.e. failures of the Q# compiler).
type private QsCompilerException(message, innerException: Exception) =
    inherit Exception(message, innerException)
    new(message) = QsCompilerException(message, null)
    new() = QsCompilerException(null, null)


/// used much like a static class in C# for handling all internal errors in the Q# compiler
type QsCompilerError = QsCompilerError
    with

        /// Method that wraps all handling of inner errors (i.e. a failure of the Q# compiler)
        /// that allows to easily modify the behavior for such errors.
        [<DoesNotReturn>]
        static member Raise(message, innerException: Exception) : unit =
            if isNull innerException then
                QsCompilerException(message) |> raise
            else
                QsCompilerException(message, innerException) |> raise

        /// Method that wraps all handling of inner errors (i.e. a failure of the Q# compiler)
        /// that allows to easily modify the behavior for such errors.
        [<DoesNotReturn>]
        static member Raise message = QsCompilerError.Raise(message, null)

        /// If the given condition is not satisfied, calls QsCompilerError.Raise with the given message.
        static member Verify([<DoesNotReturnIf false>] condition, message) =
            if not condition then QsCompilerError.Raise(message)

        /// If the given condition is not satisfied, calls QsCompilerError.Raise with the given message.
        static member Verify([<DoesNotReturnIf false>] condition) = QsCompilerError.Verify(condition, null)

        /// Generates a string with full information about the given exception, prepending the given header.
        static member private Log(ex: Exception, header) =
            let outer = sprintf "%s: %s\n" (ex.GetType().Name) ex.Message

            let inner =
                if isNull ex.InnerException then
                    ""
                else
                    sprintf "Inner Exception: %s - %s\n" (ex.InnerException.GetType().Name) ex.InnerException.Message

            let stackTrace = sprintf "Stack trace: \n%s\n" ex.StackTrace
            sprintf "%s\n%s%s%s" header outer inner stackTrace

        /// Wraps the given action in a try-catch block.
        /// Calls QsCompilerError.Raise with a message detailing the any caught exception.
        static member RaiseOnFailure(action: Action, header) =
            try
                action.Invoke()
            with
            | :? RuntimeWrappedException as ex -> QsCompilerError.Log(ex, header) |> QsCompilerError.Raise
            | ex -> QsCompilerError.Log(ex, header) |> QsCompilerError.Raise

        /// Wraps the given function in a try-catch block.
        /// Calls QsCompilerError.Raise with a message detailing the any caught exception,
        /// and returns null if an exception occurred.
        static member RaiseOnFailure<'T when 'T: null>(func: Func<'T>, header) =
            let raiseAndReturnNull msg =
                QsCompilerError.Raise msg
                null

            try
                func.Invoke()
            with
            | :? RuntimeWrappedException as ex -> QsCompilerError.Log(ex, header) |> raiseAndReturnNull
            | ex -> QsCompilerError.Log(ex, header) |> raiseAndReturnNull
