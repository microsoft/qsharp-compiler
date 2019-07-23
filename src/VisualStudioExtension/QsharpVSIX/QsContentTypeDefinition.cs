// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Microsoft.Quantum.QsLanguageExtensionVS
{
    public static class QsContentDefinition
    {
        [Export]
        [Name("Q#")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition QsContentTypeDefinition;


        [Export]
        [FileExtension(".qs")]
        [ContentType("Q#")]
        internal static FileExtensionToContentTypeDefinition QsFileExtensionDefinition;
    }
}
