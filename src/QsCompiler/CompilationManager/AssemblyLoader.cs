﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations;
using Newtonsoft.Json.Bson;

namespace Microsoft.Quantum.QsCompiler
{
    /// <summary>
    /// This class relies on the ECMA-335 standard to extract information contained in compiled binaries.
    /// The standard can be found here: https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf,
    /// and the section on custom attributes starts on page 267.
    /// </summary>
    public static class AssemblyLoader
    {
        /// <summary>
        /// Loads the Q# data structures in a referenced assembly with the URI <paramref name="asm"/>,
        /// and returns the loaded content via <paramref name="headers"/>.
        /// </summary>
        /// <param name="asm">The uri of the referenced assembly.</param>
        /// <param name="onDeserializationException">Called if an exception is thrown during deserialization.</param>
        /// <returns>
        /// False if some of the content could not be loaded successfully,
        /// possibly because the referenced assembly has been compiled with an older compiler version.
        /// </returns>
        /// <exception cref="FileNotFoundException"><paramref name="asm"/> does not exist.</exception>
        /// <exception cref="ArgumentException"><paramref name="asm"/> is not an absolute file URI.</exception>
        /// <remarks>
        /// Throws the corresponding exceptions if the information cannot be extracted.
        /// </remarks>
        public static bool LoadReferencedAssembly(Uri asm, out References.Headers headers, bool ignoreDllResources = false, Action<Exception>? onDeserializationException = null)
        {
            var id = CompilationUnitManager.GetFileId(asm);
            if (!File.Exists(asm.LocalPath))
            {
                throw new FileNotFoundException($"The file '{asm.LocalPath}' given to the assembly loader does not exist.");
            }

            using var stream = File.OpenRead(asm.LocalPath);
            using var assemblyFile = new PEReader(stream);
            if (ignoreDllResources || !FromResource(assemblyFile, out var compilation, onDeserializationException))
            {
                PerformanceTracking.TaskStart(PerformanceTracking.Task.HeaderAttributesLoading);
                var attributes = LoadHeaderAttributes(assemblyFile);
                PerformanceTracking.TaskEnd(PerformanceTracking.Task.HeaderAttributesLoading);
                PerformanceTracking.TaskStart(PerformanceTracking.Task.ReferenceHeadersCreation);
                headers = new References.Headers(id, attributes);
                PerformanceTracking.TaskEnd(PerformanceTracking.Task.ReferenceHeadersCreation);
                return ignoreDllResources || !attributes.Any(); // just means we have no references
            }

            PerformanceTracking.TaskStart(PerformanceTracking.Task.ReferenceHeadersCreation);
            headers = new References.Headers(id, compilation?.Namespaces ?? ImmutableArray<QsNamespace>.Empty);
            PerformanceTracking.TaskEnd(PerformanceTracking.Task.ReferenceHeadersCreation);
            return true;
        }

        /// <summary>
        /// Loads the Q# data structures in a referenced assembly with the path <paramref name="asmPath"/>,
        /// and returns the loaded content as <paramref name="compilation"/>.
        /// </summary>
        /// <returns>
        /// False if some of the content could not be loaded successfully,
        /// possibly because the referenced assembly has been compiled with an older compiler version.
        /// </returns>
        /// <exception cref="FileNotFoundException"><paramref name="asmPath"/> does not exist.</exception>
        /// <remarks>
        /// Catches any exception throw upon loading the compilation, and invokes <paramref name="onException"/> with it if specified.
        /// <para/>
        /// Sets <paramref name="compilation"/> to null if an exception occurred during loading.
        /// </remarks>
        public static bool LoadReferencedAssembly(
            string asmPath,
            [NotNullWhen(true)] out QsCompilation? compilation,
            Action<Exception>? onException = null)
        {
            if (!File.Exists(asmPath))
            {
                throw new FileNotFoundException($"The file '{asmPath}' does not exist.");
            }

            using var stream = File.OpenRead(asmPath);
            using var assemblyFile = new PEReader(stream);
            try
            {
                return FromResource(assemblyFile, out compilation, onException);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                compilation = null;
                return false;
            }
        }

        // tools for loading the compiled syntax tree from the dll resource (later setup for shipping Q# libraries)

        /// <summary>
        /// Given a binary representation of compiled Q# code, returns the corresponding Q# compilation.
        /// </summary>
        /// <param name="byteArray">A binary representation of compiled Q# code.</param>
        /// <param name="compilation">The compilation deserialized from <paramref name="byteArray"/>.</param>
        /// <param name="onDeserializationException">Called if an exception is thrown during deserialization.</param>
        /// <returns>
        /// True if the compilation could be deserialized without throwing an exception, and it is properly instantiated. False otherwise.
        /// </returns>
        public static bool LoadSyntaxTree(
            byte[] byteArray,
            [NotNullWhen(true)] out QsCompilation? compilation,
            Action<Exception>? onDeserializationException = null)
        {
            compilation = null;
            try
            {
                PerformanceTracking.TaskStart(PerformanceTracking.Task.SyntaxTreeDeserialization);
                compilation = BondSchemas.Protocols.DeserializeQsCompilationFromSimpleBinary(byteArray);
                PerformanceTracking.TaskEnd(PerformanceTracking.Task.SyntaxTreeDeserialization);
            }
            catch (Exception ex)
            {
                onDeserializationException?.Invoke(ex);
                return false;
            }

            return compilation != null && IsValidCompilation(compilation);
        }

        /// <summary>
        /// Given a stream containing a binary representation of compiled Q# code, returns the corresponding Q# compilation.
        /// </summary>
        /// <param name="stream">A stream containing the binary representation of compiled Q# code.</param>
        /// <param name="compilation">The compilation deserialized from <paramref name="stream"/>.</param>
        /// <param name="onDeserializationException">Called if an exception is thrown during deserialization.</param>
        /// <returns>
        /// True if the compilation could be deserialized without throwing an exception, and it is properly instantiated. False otherwise.
        /// </returns>
        [Obsolete("Only loads binary representations generated by compiler versions up to 0.13.20102604.")]
        public static bool LoadSyntaxTree(
            Stream stream,
            [NotNullWhen(true)] out QsCompilation? compilation,
            Action<Exception>? onDeserializationException = null)
        {
            PerformanceTracking.TaskStart(PerformanceTracking.Task.DeserializerInit);
            using var reader = new BsonDataReader(stream);
            PerformanceTracking.TaskEnd(PerformanceTracking.Task.DeserializerInit);
            (compilation, reader.ReadRootValueAsArray) = (null, false);
            try
            {
                PerformanceTracking.TaskStart(PerformanceTracking.Task.SyntaxTreeDeserialization);
                compilation = Json.Serializer.Deserialize<QsCompilation>(reader);
                PerformanceTracking.TaskEnd(PerformanceTracking.Task.SyntaxTreeDeserialization);
            }
            catch (Exception ex)
            {
                onDeserializationException?.Invoke(ex);
                return false;
            }

            return compilation != null && IsValidCompilation(compilation);
        }

        private static bool IsValidCompilation(QsCompilation compilation) =>
            !compilation.Namespaces.IsDefault && !compilation.EntryPoints.IsDefault;

        /// <summary>
        /// Creates a dictionary of all manifest resources in <paramref name="reader"/>.
        /// </summary>
        private static ImmutableDictionary<string, ManifestResource> Resources(this MetadataReader reader) =>
            reader.ManifestResources
                .Select(reader.GetManifestResource)
                .ToImmutableDictionary(
                    resource => reader.GetString(resource.Name),
                    resource => resource);

        /// <summary>
        /// Loads any Q# compilation included as a resource from <paramref name="assemblyFile"/>.
        /// </summary>
        /// <param name="assemblyFile">The reader for the byte stream of a dotnet DLL from which to load the compilation.</param>
        /// <param name="compilation">The Q# compilation included as a resource of <paramref name="assemblyFile"/>.</param>
        /// <param name="onDeserializationException">Called if an exception is thrown during deserialization.</param>
        /// <returns>
        /// True if <paramref name="assemblyFile"/> includes a suitable resource, false otherwise.
        /// </returns>
        /// <remarks>
        /// May throw an exception if <paramref name="assemblyFile"/> has been compiled with a different compiler version.
        /// </remarks>
        private static bool FromResource(
            PEReader assemblyFile,
            [NotNullWhen(true)] out QsCompilation? compilation,
            Action<Exception>? onDeserializationException = null)
        {
            compilation = null;
            var metadataReader = assemblyFile.GetMetadataReader();
            bool isBondV1ResourcePresent = false;
            bool isNewtonSoftResourcePresent = false;
            ManifestResource resource;
            if (metadataReader.Resources().TryGetValue(DotnetCoreDll.ResourceNameQsDataBondV1, out resource))
            {
                isBondV1ResourcePresent = true;
            }
#pragma warning disable 618 // ResourceName is obsolete.
            else if (metadataReader.Resources().TryGetValue(DotnetCoreDll.ResourceName, out resource))
#pragma warning restore 618
            {
                isNewtonSoftResourcePresent = true;
            }

            // The offset of resources is relative to the resources directory.
            // It is possible that there is no offset given because a valid dll allows for extenal resources.
            // In all Q# dlls there will be a resource with the specific name chosen by the compiler.
            var isResourcePresent = isBondV1ResourcePresent || isNewtonSoftResourcePresent;
            var resourceDir = assemblyFile.PEHeaders.CorHeader.ResourcesDirectory;
            if (!assemblyFile.PEHeaders.TryGetDirectoryOffset(resourceDir, out var directoryOffset) ||
                !isResourcePresent ||
                !resource.Implementation.IsNil)
            {
                return false;
            }

            // This is going to be very slow, as it loads the entire assembly into a managed array, byte by byte.
            // Due to the finite size of the managed array, that imposes a memory limitation of around 4GB.
            // The other alternative would be to have an unsafe block, or to contribute a fix to PEMemoryBlock to expose a ReadOnlySpan.
            PerformanceTracking.TaskStart(PerformanceTracking.Task.LoadDataFromReferenceToStream);
            var image = assemblyFile.GetEntireImage(); // uses int to denote the length and access parameters
            var absResourceOffset = (int)resource.Offset + directoryOffset;

            // the first four bytes of the resource denote how long the resource is, and are followed by the actual resource data
            var resourceLength = BitConverter.ToInt32(image.GetContent(absResourceOffset, sizeof(int)).ToArray(), 0);
            var resourceData = image.GetContent(absResourceOffset + sizeof(int), resourceLength).ToArray();
            PerformanceTracking.TaskEnd(PerformanceTracking.Task.LoadDataFromReferenceToStream);

            // Use the correct method depending on the resource.
            if (isBondV1ResourcePresent)
            {
                return LoadSyntaxTree(resourceData, out compilation, onDeserializationException);
            }
            else if (isNewtonSoftResourcePresent)
            {
#pragma warning disable 618 // LoadSyntaxTree is obsolete.
                return LoadSyntaxTree(new MemoryStream(resourceData), out compilation, onDeserializationException);
#pragma warning restore 618
            }

            return false;
        }

        // tools for loading headers based on attributes in compiled C# code (early setup for shipping Q# libraries)

        /// <summary>
        /// This routine extracts the namespace and type name of <paramref name="attribute"/>.
        /// </summary>
        /// <returns>
        /// The tuple (namespace, name).
        /// </returns>
        /// <remarks>
        /// There are two possible handle kinds in use for the constructor of a custom attribute,
        /// one pointing to the MethodDef table and one to the MemberRef table, see p.216 in the ECMA standard linked above and
        /// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/src/System/Reflection/Metadata/TypeSystem/CustomAttribute.cs#L42
        /// <para/>
        /// Returns null if the constructor handle of <paramref name="attribute"/> is not a <see cref="HandleKind.MethodDefinition"/> or a <see cref="HandleKind.MemberReference"/>.
        /// </remarks>
        private static (StringHandle, StringHandle)? GetAttributeType(MetadataReader metadataReader, CustomAttribute attribute)
        {
            if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
            {
                var ctor = metadataReader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                var type = metadataReader.GetTypeDefinition(ctor.GetDeclaringType());
                return (type.Namespace, type.Name);
            }
            else if (attribute.Constructor.Kind == HandleKind.MemberReference)
            {
                var ctor = metadataReader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                var type = metadataReader.GetTypeReference((TypeReferenceHandle)ctor.Parent);
                return (type.Namespace, type.Name);
            }
            else
            {
                return null;
            }
        }

        // TODO: this needs to be made more robust.
        // We currently rely on the fact that all attributes defined by the Q# compiler
        // have a single constructor taking a single string argument.
        private static (string, string)? GetAttribute(MetadataReader metadataReader, CustomAttribute attribute)
        {
            var attrType = GetAttributeType(metadataReader, attribute);
            QsCompilerError.Verify(attrType.HasValue, "the type of the custom attribute could not be determined");
            var (ns, name) = attrType.Value;

            var attrNS = metadataReader.GetString(ns);
            if (attrNS.StartsWith("Microsoft.Quantum", StringComparison.InvariantCulture))
            {
                var attrReader = metadataReader.GetBlobReader(attribute.Value);
                _ = attrReader.ReadUInt16(); // All custom attributes start with 0x0001, so read that now and discard it.
                try
                {
                    var serialization = attrReader.ReadSerializedString(); // FIXME: this needs to be made more robust
                    return (metadataReader.GetString(name), serialization);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Reads the custom attributes of <paramref name="assemblyFile"/>.
        /// </summary>
        /// <param name="assemblyFile">A reader for the byte stream of the dotnet DLL.</param>
        /// <returns>
        /// Tuples containing the name of the attribute and the constructor argument
        /// for all attributes defined in a Microsoft.Quantum* namespace.
        /// </returns>
        /// <remarks>
        /// Throws the corresponding exceptions if the information cannot be extracted.
        /// </remarks>
        private static IEnumerable<(string, string)> LoadHeaderAttributes(PEReader assemblyFile)
        {
            var metadataReader = assemblyFile.GetMetadataReader();
            return metadataReader.GetAssemblyDefinition().GetCustomAttributes()
                .Select(metadataReader.GetCustomAttribute)
                .SelectNotNull(attribute => GetAttribute(metadataReader, attribute))
                .ToImmutableArray();
        }
    }
}
