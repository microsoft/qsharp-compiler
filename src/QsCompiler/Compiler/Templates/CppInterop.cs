// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Quantum.QsCompiler.BondSchemas.Execution;

namespace Microsoft.Quantum.QsCompiler.Templates
{
    internal static class ParameterCppExtensions
    {
        public static string CppType(this Parameter @this)
        {
            return @this.Type switch
            {
                DataType.BoolType => "char",
                DataType.IntegerType => "int64_t",
                DataType.DoubleType => "double",
                DataType.PauliType => "char",
                DataType.RangeType => "InteropRange*",
                DataType.ResultType => "char",
                DataType.StringType => "const char*",
                DataType.ArrayType => "InteropArray*",
                _ => throw new NotSupportedException($"Unsupported argument type {@this.Type}")
            };
        }

        public static string CliOptionString(this Parameter @this)
        {
            if (@this.Name.Length == 1)
            {
                return $"-{@this.Name}";
            }
            else
            {
                return $"--{@this.Name}";
            }
        }

        public static string CliTypeDescription(this Parameter @this)
        {
            return @this.Type switch
            {
                DataType.BoolType => $"bool",
                DataType.IntegerType => $"integer",
                DataType.DoubleType => $"double",
                DataType.PauliType => $"Pauli",
                DataType.RangeType => $"Range (start, step, end)",
                DataType.ResultType => $"Result",
                DataType.StringType => $"String",
                DataType.ArrayType => @this.ArrayType switch
                {
                    DataType.BoolType => $"bool array",
                    DataType.IntegerType => $"integer array",
                    DataType.DoubleType => $"double array",
                    DataType.PauliType => $"Pauli array",
                    DataType.RangeType => $"Range array",
                    DataType.ResultType => $"Result array",
                    DataType.StringType => $"String array",
                    _ => throw new NotSupportedException($"Unsupported array type {@this.Type}")
                },
                _ => throw new NotSupportedException($"Unsupported argument type {@this.Type}")
            };
        }

        public static string CliDescription(this Parameter @this) => $"A {@this.CliTypeDescription()} value for the {@this.Name} argument";

        public static string CppCliValueType(this Parameter @this)
        {
            return @this.Type switch
            {
                DataType.BoolType => "char",
                DataType.IntegerType => "int64_t",
                DataType.DoubleType => "double_t",
                DataType.PauliType => "PauliId",
                DataType.RangeType => "RangeTuple",
                DataType.ResultType => "char",
                DataType.StringType => "string",
                DataType.ArrayType => @this.ArrayType switch
                {
                    DataType.BoolType => "vector<char>",
                    DataType.IntegerType => "vector<int64_t>",
                    DataType.DoubleType => "vector<double_t>",
                    DataType.PauliType => "std::vector<PauliId>",
                    DataType.RangeType => "vector<RangeTuple>",
                    DataType.ResultType => "vector<char>",
                    DataType.StringType => "vector<string>",
                    _ => throw new NotSupportedException($"Unsupported array type {@this.Type}")
                },
                _ => throw new NotSupportedException($"Unsupported argument type {@this.Type}")
            };
        }

        public static string? CppCliVariableInitialValue(this Parameter @this)
        {
            return @this.Type switch
            {
                DataType.BoolType => "InteropFalseAsChar",
                DataType.IntegerType => "0",
                DataType.DoubleType => "0.0",
                DataType.PauliType => "PauliId::PauliId_I",
                DataType.RangeType => null,
                DataType.ResultType => "InteropResultZeroAsChar",
                DataType.StringType => null,
                DataType.ArrayType => @this.ArrayType switch
                {
                    DataType.BoolType => null,
                    DataType.IntegerType => null,
                    DataType.DoubleType => null,
                    DataType.PauliType => null,
                    DataType.RangeType => null,
                    DataType.ResultType => null,
                    DataType.StringType => null,
                    _ => throw new NotSupportedException($"Unsupported array type {@this.Type}")
                },
                _ => throw new NotSupportedException($"Unsupported argument type {@this.Type}")
            };
        }

        public static string? DataTypeTransformerMapName(DataType? type)
        {
            return type switch
            {
                DataType.BoolType => "BoolAsCharMap",
                DataType.IntegerType => null,
                DataType.DoubleType => null,
                DataType.PauliType => "PauliMap",
                DataType.RangeType => null,
                DataType.ResultType => "ResultAsCharMap",
                DataType.StringType => null,
                DataType.ArrayType => null,
                _ => throw new NotSupportedException($"Unsupported argument type {type}")
            };
        }

        public static string? TransformerMapName(this Parameter @this) =>
            @this.Type switch {
                DataType.ArrayType => DataTypeTransformerMapName(@this.ArrayType),
                _ => DataTypeTransformerMapName(@this.Type)
            };

        public static string CliValueVariableName(this Parameter @this)
        {
            return @this.Name + "CliValue";
        }

        public static string InteropVariableName(this Parameter @this)
        {
            return @this.Name + "InteropValue";
        }

        public static string IntermediateVariableName(this Parameter @this)
        {
            return @this.Name + "IntermediateValue";
        }
    }

    internal static class EntryPointCppExtensions
    {
        public static bool ContainsParameterType(this EntryPointOperation @this, DataType type)
        {
            foreach (Parameter arg in @this.Parameters)
            {
                if (arg.Type == type)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ContainsArrayType(this EntryPointOperation @this, DataType type)
        {
            foreach (Parameter arg in @this.Parameters)
            {
                if (arg.ArrayType == type)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
