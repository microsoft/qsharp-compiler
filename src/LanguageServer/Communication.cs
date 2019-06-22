// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsLanguageServer
{
    public static class CommandIds
    {
        public const string ApplyEdit = "qsLanguageServer/applyEdit";

        // commands for diagnostic purposes

        internal const string FileContentInMemory = "qsLanguageServer/fileContentInMemory";
        internal const string FileDiagnostics = "qsLanguageServer/fileDiagnostics";
    }


    public class ProtocolError
    {
        public static class Codes
        {
            // specified by the LSP

            public const int AwaitingInitialization = -32002;

            // behavior unspecified by LSP
            // -> according to JsonRpc 2.0, the error code range -32768 to -32000 is reserverd
            // -> using the range -32900 to -32999 for anything not specified by the LSP should be fine
        }

        public readonly int Code;
        public readonly string Message;

        public ProtocolError(int code, string message = null)
        {
            this.Code = code;
            this.Message = message;
        }

        public static ProtocolError AwaitingInitialization =>
            new ProtocolError(Codes.AwaitingInitialization);
    }
}