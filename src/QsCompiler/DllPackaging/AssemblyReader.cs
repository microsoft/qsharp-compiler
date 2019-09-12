// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


namespace Microsoft.Quantum.QsCompiler
{

    public class AssemblyReader : IDisposable
    {
        private FileStream fileStream;
        private PEReader peReader;
        private MetadataReader metadataReader;

        public AssemblyReader(Uri asm)
        {
            if (asm == null) throw new ArgumentNullException(nameof(asm));
            if (!File.Exists(asm.LocalPath))
            { throw new FileNotFoundException($"the file '{asm}' given to the attribute reader does not exist"); }

            fileStream = File.OpenRead(asm.LocalPath);
            peReader = new PEReader(fileStream);
            metadataReader = peReader.GetMetadataReader();
        }

        private ImmutableDictionary<string, ManifestResource> Resources =>
            metadataReader.ManifestResources
                .Select(metadataReader.GetManifestResource)
                .ToImmutableDictionary(
                    resource => metadataReader.GetString(resource.Name),
                    resource => resource
                );

        public bool TryGetManifestResource(out ManifestResource qResource) =>
            Resources.TryGetValue(WellKnown.AST_RESOURCE_NAME, out qResource);

        public IEnumerable<QsNamespace> GetAst()
        {
            if (!TryGetManifestResource(out var resource) || resource.Implementation.IsNil)
            { return ImmutableArray<QsNamespace>.Empty; }

            // The offset of the resource is relative to the resources directory. 
            // We hence need to know the offset of the directory itself in order to load it. 
            var resourceDir = peReader.PEHeaders.CorHeader.ResourcesDirectory;
            if (!peReader.PEHeaders.TryGetDirectoryOffset(resourceDir, out var directoryOffset))
            { return ImmutableArray<QsNamespace>.Empty; }

            // This is going to be very slow, as it loads the entire assembly into a managed array, byte by byte.
            // Due to the finite size of the managed array, that imposes a memory limitation of around 4GB. 
            // The other alternative would be to have an unsafe block, or to contribute a fix to PEMemoryBlock to expose a ReadOnlySpan.
            var image = peReader.GetEntireImage(); // uses int to denote the length and access parameters
            var absResourceOffset = (int)resource.Offset + directoryOffset;

            // the first four bytes of the resource denote how long the resource is, and are followed by the actual resource data
            var resourceLength = BitConverter.ToInt32(image.GetContent(absResourceOffset, sizeof(Int32)).ToArray(), 0);
            var resourceData = image.GetContent(absResourceOffset + sizeof(Int32), resourceLength).ToArray();
            return CompilationLoader.ReadBinary(new MemoryStream(resourceData));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    peReader.Dispose();
                    peReader = null;
                    fileStream.Dispose();
                    peReader = null;
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        public void Dispose() => Dispose(true);
        #endregion

    }

}
