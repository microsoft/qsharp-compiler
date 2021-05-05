// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Quantum.Qir.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    public static class EntryPointOperationLoader
    {
        public static IList<EntryPointOperation> LoadEntryPointOperations(FileInfo assemblyFileInfo)
        {
            if (!AssemblyLoader.LoadReferencedAssembly(assemblyFileInfo.FullName, out var compilation))
            {
                throw new ArgumentException("Unable to read the Q# syntax tree from the given DLL.");
            }
            return GenerateEntryPointOperations(compilation);
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
            return new Parameter()
            {
                Name = parameter.VariableName is QsLocalSymbol.ValidName name
                    ? name.Item
                    : throw new ArgumentException("Encountered invalid name for parameter."),
                Type = MapResolvedTypeToDataType(parameter.Type)
            };
        }

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
            _ => throw new NotImplementedException("invalid type for entry point parameter")
        };
    }
}
