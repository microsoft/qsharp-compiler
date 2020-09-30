// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

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
        public readonly string? Message;

        public ProtocolError(int code, string? message = null)
        {
            this.Code = code;
            this.Message = message;
        }

        public static ProtocolError AwaitingInitialization =>
            new ProtocolError(Codes.AwaitingInitialization);
    }

    // If the workaround for ignoring CodeActionKind is no longer needed,
    // please also remove the modification in the server's Initialize method
    // that sets capabilities.textDocument.codeAction to null.
    public static class Workarounds
    {
        /// <summary>
        /// This is the exact version as used by earlier versions of the package.
        /// We will use this one for the sake of avoiding a bug in the VS Code client
        /// that will cause an issue for deserializing the CodeActionKind array.
        /// </summary>
        [DataContract]
        public class CodeActionParams
        {
            [DataMember(Name = "textDocument")]
            public TextDocumentIdentifier? TextDocument { get; set; }

            [DataMember(Name = "range")]
            public VisualStudio.LanguageServer.Protocol.Range? Range { get; set; }

            [DataMember(Name = "context")]
            public CodeActionContext? Context { get; set; }

            public VisualStudio.LanguageServer.Protocol.CodeActionParams ToCodeActionParams() =>
                new VisualStudio.LanguageServer.Protocol.CodeActionParams
                {
                    TextDocument = this.TextDocument,
                    Range = this.Range,
                    Context = this.Context?.ToCodeActionContext()
                };
        }

        /// <summary>
        /// This is the exact version as used by earlier versions of the package.
        /// We will use this one for the sake of avoiding a bug in the VS Code client
        /// that will cause an issue for deserializing the CodeActionKind array.
        /// </summary>
        [DataContract]
        public class CodeActionContext
        {
            [DataMember(Name = "diagnostics")]
            public Diagnostic[]? Diagnostics { get; set; }

            public VisualStudio.LanguageServer.Protocol.CodeActionContext ToCodeActionContext() =>
                new VisualStudio.LanguageServer.Protocol.CodeActionContext
                {
                    Diagnostics = this.Diagnostics,
                    Only = null
                };
        }
    }
}
