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


namespace Microsoft.Quantum.QsCompiler
{
    /// This class relies on the ECMA-335 standard to extract information contained in compiled binaries. 
    /// The standard can be found here: https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf,
    /// and the section on custom attributes starts on page 267. 
    public static class AttributeReader
    {
        /// There are two possible handle kinds in use for the constructor of a custom attribute, 
        /// one pointing to the MethodDef table and one to the MemberRef table, see p.216 in the ECMA standard linked above and 
        /// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/src/System/Reflection/Metadata/TypeSystem/CustomAttribute.cs#L42
        /// This routine extracts the namespace and type name of the given attribute and returns the corresponding string handles. 
        /// Returns null if the constructor handle is not a MethodDefinition or a MemberDefinition. 
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
            else return null;
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
                var _ = attrReader.ReadUInt16(); // All custom attributes start with 0x0001, so read that now and discard it.
                try
                {
                    var serialization = attrReader.ReadSerializedString(); // FIXME: this needs to be made more robust!
                    return (metadataReader.GetString(name), serialization);
                }
                catch { return null; }
            }
            return null;
        }

        /// Given a stream with the bytes of an assembly, read its custom attributes and
        /// returns a tuple containing the name of the attribute and the constructor argument 
        /// for all attributes defined in a Microsoft.Quantum* namespace.  
        /// Throws an ArgumentNullException if the given stream is null. 
        /// Throws the corresponding exceptions if the information cannot be extracted.
        public static IEnumerable<(string, string)> GetQsCompilerAttributes(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using (var assemblyFile = new PEReader(stream))
            {
                var metadataReader = assemblyFile.GetMetadataReader();
                return metadataReader.GetAssemblyDefinition().GetCustomAttributes()
                    .Select(metadataReader.GetCustomAttribute)
                    .Select(attribute => GetAttribute(metadataReader, attribute))
                    .Where(ctorItems => ctorItems.HasValue)
                    .Select(ctorItems => ctorItems.Value).ToImmutableArray();
            }
        }

        /// Given the full name of an assembly, opens the file and reads its custom attributes and
        /// returns a tuple containing the name of the attribute and the constructor argument 
        /// for all attributes defined in the Microsoft.Quantum* namespace.  
        /// Throws an ArgumentNullException if the given uri is null. 
        /// Throws a FileNotFoundException if no file with the given name exists. 
        /// Throws the corresponding exceptions if the information cannot be extracted.
        public static IEnumerable<(string, string)> GetQsCompilerAttributes(Uri asm)
        {
            if (asm == null) throw new ArgumentNullException(nameof(asm));
            if (!File.Exists(asm.LocalPath))
            { throw new FileNotFoundException($"the file '{asm}' given to the attribute reader does not exist"); }

            using (var stream = File.OpenRead(asm.LocalPath))
            { return GetQsCompilerAttributes(stream); }
        }
    }
}
