// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler
{

    public class AssemblyReader : IDisposable
    {
        public bool IsQuantumAssembly => isQuantumAssembly.Value;

        private FileStream fileStream;
        private PEReader peReader;
        private MetadataReader metadataReader;
        private readonly Lazy<bool> isQuantumAssembly;

        public AssemblyReader(Uri asm)
        {
            if (asm == null) throw new ArgumentNullException(nameof(asm));
            if (!File.Exists(asm.LocalPath))
            { throw new FileNotFoundException($"the file '{asm}' given to the attribute reader does not exist"); }

            fileStream = File.OpenRead(asm.LocalPath);
            peReader = new PEReader(fileStream);
            metadataReader = peReader.GetMetadataReader();

            isQuantumAssembly = new Lazy<bool>(() =>
                GetResourceNames()
                    .Any(
                        name => name == WellKnown.AST_RESOURCE_NAME
                    )
            );
        }

        private ImmutableDictionary<string, ManifestResource> GetResources() =>
            metadataReader
                .ManifestResources
                .Select(
                    handle => metadataReader.GetManifestResource(handle)
                )
                .ToImmutableDictionary(
                    resource => metadataReader.GetString(resource.Name),
                    resource => resource
                );

        private IEnumerable<string> GetResourceNames() =>
            GetResources().Keys;

        private ManifestResource GetAstResource()
        {
            if (!IsQuantumAssembly)
            {
                throw new IOException("Assembly is not a quantum assembly, or is a legacy quantum assembly.");
            }

            return GetResources()[WellKnown.AST_RESOURCE_NAME];
        }

        public IEnumerable<QsNamespace> GetAst()
        {
            var resource = GetAstResource();
            if (!resource.Implementation.IsNil)
            {
                throw new IOException("AST data missing from assembly.");
            }

            // We know the offset of the resource we want relative to the
            // resources directory. Thus, if we find the resources directory
            // we know where in the image the thing we want is.
            if (!peReader.PEHeaders.TryGetDirectoryOffset(
                    peReader.PEHeaders.CorHeader.ResourcesDirectory,
                    out var directoryOffset
            ))
            {
                throw new IOException("Could not find resources directory.");
            }

            // NOTE: This is going to be very slow, as it loads the entire
            //       assembly into a managed array, byte by byte. On the other
            //       hand, it at least avoids needing an unsafe block.
            //       The alternative would be to contribute a fix to PEMemoryBlock
            //       to expose a ReadOnlySpan.
            var image = peReader
                .GetEntireImage();
            var resourceLength = BitConverter.ToInt32(image
                // NOTE: The following cast will fail for large Q# ASTs (> 4GB).
                //       On the other hand, given current performance problems
                //       due to avoiding unsafety, we will likely have much larger
                //       problems first.
                .GetContent((int)resource.Offset + directoryOffset, sizeof(Int32))
                .ToArray(),
                0
            );
            var resourceData = image
                .GetContent((int)resource.Offset + sizeof(Int32) + directoryOffset, resourceLength)
                .ToArray();

            var memoryStream = new MemoryStream(resourceData);
            return CompilationLoader.ReadBinary(memoryStream);
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
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }

}
