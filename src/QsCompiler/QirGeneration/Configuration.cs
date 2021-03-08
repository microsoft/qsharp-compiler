// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QIR;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Types;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    /// <summary>
    /// Class that contains all configurable settings for the QIR emission.
    /// </summary>
    public class Configuration
    {
        private static readonly ImmutableDictionary<string, string> QSharpRuntimeStructNames =
            ImmutableDictionary.CreateRange(new Dictionary<string, string>
            {
                [TypeNames.Result] = "class.RESULT",
                [TypeNames.Array] = "struct.quantum::Array",
                [TypeNames.Tuple] = "struct.quantum::Tuple",
                [TypeNames.Callable] = "struct.quantum::Callable",
                [TypeNames.Qubit] = "class.QUBIT"
            });

        private readonly ImmutableDictionary<string, string> interopTypeMapping;
        private ImmutableDictionary<string, IStructType>? opaqueStructs;
        private Context? context;

        /// <summary>
        /// Constructs a class instance storing the configurable settings for QIR emission.
        /// </summary>
        /// <param name="interopTypeMapping">
        /// Optional parameter that maps the name of a QIR type to the name of the corresponding interop type.
        /// The mapping specifies with which type names the QIR types are replaced with
        /// when generating the interop wrappers and entry point(s).
        /// </param>
        public Configuration(Dictionary<string, string>? interopTypeMapping = null)
        {
            this.interopTypeMapping = interopTypeMapping != null
                ? interopTypeMapping.ToImmutableDictionary()
                : QSharpRuntimeStructNames;

            if (this.interopTypeMapping.ContainsKey(TypeNames.Int) ||
                this.interopTypeMapping.ContainsKey(TypeNames.Double) ||
                this.interopTypeMapping.ContainsKey(TypeNames.Bool) ||
                this.interopTypeMapping.ContainsKey(TypeNames.Pauli))
            {
                throw new NotSupportedException(
                    "Custom type names for integer and floating point types are currently not supported. " +
                    "This includes the types Int, Double, Bool, and Pauli.");
            }
        }

        /// <summary>
        /// Sets the context within which types are created by <see cref="MapToInteropType(ITypeRef)"./>
        /// </summary>
        internal void SetContext(Context context)
        {
            this.context = context;
            this.opaqueStructs = this.interopTypeMapping.ToImmutableDictionary(
                kv => kv.Key,
                kv => context.CreateStructType(kv.Value));
        }

        /// <summary>
        /// Maps a QIR type to a more interop-friendly type using the specified type mapping for interoperability.
        /// </summary>
        /// <exception cref="ArgumentException">The given type is a pointer to a non-struct type, or is void.</exception>
        /// <exception cref="InvalidOperationException">No context has been set by calling <see cref="SetContext(Context)"/>.</exception>
        internal ITypeRef MapToInteropType(ITypeRef t)
        {
            if (this.context == null || this.opaqueStructs == null)
            {
                throw new InvalidOperationException("no context specified");
            }

            // Range, Tuple (typed and untyped), Array, Result, String, BigInt, Callable, and Qubit
            // are all structs or struct pointers.
            t = t.IsPointer ? Types.StructFromPointer(t) : t;
            var typeName = (t as IStructType)?.Name;

            if (typeName != null && this.opaqueStructs.TryGetValue(typeName, out IStructType replacement))
            {
                // covers all structs or struct pointers for which a type name has been defined in the interop mapping
                return replacement.CreatePointerType();
            }
            else if (typeName == TypeNames.String || typeName == TypeNames.BigInt)
            {
                // pass strings and big ints as data arrays, unless a type name is specified for them
                return this.context.Int8Type.CreatePointerType();
            }
            else if (t is IStructType st)
            {
                // pass all structs or struct pointers for which no type name has been defined in the interop mapping
                // as pointers to the corresponding LLVM struct type - this in particular applies to typed tuples
                var itemTypes = st.Members.Select(this.MapToInteropType).ToArray();
                return this.context.CreateStructType(packed: false, itemTypes).CreatePointerType();
            }
            if (t.IsInteger)
            {
                // covers Int, Bool, Pauli
                var nrBytes = 1 + ((t.IntegerBitWidth - 1) / 8);
                return this.context.GetIntType(8 * nrBytes);
            }
            else if (t.IsFloatingPoint)
            {
                return this.context.DoubleType;
            }
            else
            {
                // Unit is covered as long as a struct name for Tuple is defined
                throw new ArgumentException("Unrecognized type");
            }
        }
    }
}
