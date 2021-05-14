// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.Qir.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    public static class EntryPointOperationLoader
    {
        /// <summary>
        /// Loads the entry point operations found in the syntax tree included as a resource from <paramref name="assemblyFileInfo"/>.
        /// </summary>
        /// <param name="assemblyFileInfo">The file info of a .NET DLL from which to load entry point operations from.</param>
        /// <returns>
        /// A list of entry point operation objects representing the QIR entry point operations.
        /// </returns>
        /// <exception cref="FileNotFoundException"><paramref name="assemblyFileInfo"/> does not exist.</exception>
        /// <exception cref="ArgumentException"><paramref name="assemblyFileInfo"/> does not contain a Q# syntax tree.</exception>
        /// <exception cref="ArgumentException">Encounters invalid parameters for an entry point.</exception>
        public static IList<EntryPointOperation> LoadEntryPointOperations(FileInfo assemblyFileInfo)
        {
            if (!AssemblyLoader.LoadReferencedAssembly(assemblyFileInfo.FullName, out var compilation))
            {
                throw new ArgumentException("Unable to read the Q# syntax tree from the given DLL.");
            }
            return GenerateEntryPointOperations(compilation);
        }

        public static IList<EntryPointOperation> LoadEntryPointOperations(string qirBitCode)
        {
            var module = BitcodeModule.LoadFrom(qirBitCode, new Context());
            var entryPoints = module.Functions.Where(f =>
                f.Attributes.ContainsKey(FunctionAttributeIndex.Function) // TryGetValue for some reason does not seem to work properly
                && f.GetAttributesAtIndex(FunctionAttributeIndex.Function).Any(a => a.Name == AttributeNames.EntryPoint));
            Console.WriteLine($"entry points: {string.Join(", ", entryPoints.Where(f => f.Name.StartsWith("Microsoft")).Select(f => f.Name))}");

            return new List<EntryPointOperation>(entryPoints.Select(ep => new EntryPointOperation()
            {
                Name = ep.Name,
                Parameters = new List<Parameter>(ep.Parameters.Select(param => new Parameter()
                {
                    Name = param.Name,
                    Type = MapResolvedTypeToDataType(param.NativeType),
                    ArrayType = null // FIXME: we need to perserve the array item type in the signature to populate this
                }))
            }));
        }

        private static IList<EntryPointOperation> GenerateEntryPointOperations(QsCompilation compilation)
        {
            var globals = compilation.Namespaces.GlobalCallableResolutions();

            return compilation.EntryPoints.Select(ep => new EntryPointOperation()
            {
                Name = NameGeneration.InteropFriendlyWrapperName(ep),
                Parameters = GetParams(globals[ep])
            }).ToList();
        }

        private static List<Parameter> GetParams(QsCallable callable)
        {
            return SyntaxGenerator.ExtractItems(callable.ArgumentTuple)
                .Where(sym => !sym.Type.Resolution.IsUnitType)
                .Select(sym => MakeParameter(sym))
                .ToList();
        }

        private static Parameter MakeParameter(LocalVariableDeclaration<QsLocalSymbol> parameter)
        {
            var type = MapResolvedTypeToDataType(parameter.Type);
            var arrayType = parameter.Type.Resolution is QsTypeKind.ArrayType innerType
                ? (DataType?)MapResolvedTypeToDataType(innerType.Item)
                : null;

            if (arrayType == DataType.ArrayType)
            {
                throw new ArgumentException("Multi-dimensional arrays are not supported types of entry point parameters.");
            }

            return new Parameter()
            {
                Name = parameter.VariableName is QsLocalSymbol.ValidName name
                    ? name.Item
                    : throw new ArgumentException("Encountered invalid name for parameter."),
                Type = type,
                ArrayType = arrayType
            };
        }

        private static DataType MapResolvedTypeToDataType(ITypeRef rt) => rt switch
        {
            QsTypeKind.Tags.Bool => DataType.BoolType,
            QsTypeKind.Tags.Int => DataType.IntegerType,
            QsTypeKind.Tags.Double => DataType.DoubleType,
            QsTypeKind.Tags.Pauli => DataType.PauliType,
            QsTypeKind.Tags.Range => DataType.RangeType,
            QsTypeKind.Tags.Result => DataType.ResultType,
            QsTypeKind.Tags.String => DataType.StringType,
            QsTypeKind.Tags.ArrayType => DataType.ArrayType,
            _ => throw new ArgumentException($"Invalid type ({rt.Resolution.Tag}) for entry point parameter")
        };

        private static DataType MapResolvedTypeToDataType(ResolvedType rt) => rt.Resolution.Tag switch
        {
            QsTypeKind.Tags.Bool => DataType.BoolType,
            QsTypeKind.Tags.Int => DataType.IntegerType,
            QsTypeKind.Tags.Double => DataType.DoubleType,
            QsTypeKind.Tags.Pauli => DataType.PauliType,
            QsTypeKind.Tags.Range => DataType.RangeType,
            QsTypeKind.Tags.Result => DataType.ResultType,
            QsTypeKind.Tags.String => DataType.StringType,
            QsTypeKind.Tags.ArrayType => DataType.ArrayType,
            _ => throw new ArgumentException($"Invalid type ({rt.Resolution.Tag}) for entry point parameter")
        };
    }
}
