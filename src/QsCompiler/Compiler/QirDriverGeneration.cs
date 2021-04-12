// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint;
using Microsoft.Quantum.QsCompiler.Templates;

namespace Microsoft.Quantum.QsCompiler
{
    public static class QirDriverGeneration
    {
        private delegate string StringConversion(object? value);

        public static void GenerateQirDriverCpp(EntryPointOperation entryPointOperation, Stream stream)
        {
            QirDriverCpp qirDriverCpp = new QirDriverCpp(entryPointOperation);
            var cppSource = qirDriverCpp.TransformText();
            stream.Write(Encoding.UTF8.GetBytes(cppSource));
            stream.Flush();
            stream.Position = 0;
        }

        public static string GenerateCommandLineArguments(IList<Argument> arguments)
        {
            // Sort arguments by position.
            var sortedArguments = arguments.OrderBy(arg => arg.Position);
            var argumentBuilder = new StringBuilder();
            foreach (var arg in sortedArguments)
            {
                if (argumentBuilder.Length != 0)
                {
                    argumentBuilder.Append(' ');
                }

                argumentBuilder.Append($"--{arg.Name}").Append(' ').Append(GetArgumentValueString(arg));
            }

            return argumentBuilder.ToString();
        }

        private static string GetArgumentValueString(Argument argument)
        {
            // Today, only the first argument value in the array will be used.
            var value = argument.Values[0];
            return argument.Type switch
            {
                DataType.BoolType => GetBoolValueString(value.Bool),
                DataType.DoubleType => GetDoubleValueString(value.Double),
                DataType.IntegerType => GetIntegerValueString(value.Integer),
                DataType.PauliType => GetPauliValueString(value.Pauli),
                DataType.RangeType => GetRangeValueString(value.Range),
                DataType.ResultType => GetResultValueString(value.Result),
                DataType.StringType => GetStringValueString(value.String),
                DataType.ArrayType => GetArrayValueString(argument.ArrayType, value.Array),
                _ => throw new ArgumentException($"Unsupported data type {argument.Type}")
            };
        }

        private static string GetArrayValueString(DataType? arrayType, ArrayValue arrayValue)
        {
            static string ConvertArray<T>(IList<T> list, StringConversion conversion)
            {
                return list.Aggregate<T, string>(string.Empty, (aggregation, val) =>
                {
                    if (aggregation != string.Empty)
                    {
                        aggregation += ' ';
                    }
                    return aggregation + conversion.Invoke(val);
                });
            }

            return arrayType switch
            {
                DataType.BoolType => ConvertArray(arrayValue.Bool, GetBoolValueString),
                DataType.DoubleType => ConvertArray(arrayValue.Double, GetDoubleValueString),
                DataType.IntegerType => ConvertArray(arrayValue.Integer, GetIntegerValueString),
                DataType.PauliType => ConvertArray(arrayValue.Pauli, GetPauliValueString),
                DataType.RangeType => ConvertArray(arrayValue.Range, GetRangeValueString),
                DataType.ResultType => ConvertArray(arrayValue.Result, GetResultValueString),
                DataType.StringType => ConvertArray(arrayValue.String, GetStringValueString),
                _ => throw new ArgumentException($"Unsupported array data type {arrayType}")
            };
        }

        private static string GetResultValueString(object? value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Cannot convert null result value to string.");
            }
            return (ResultValue)value == ResultValue.One ? "1" : "0";
        }

        private static string GetRangeValueString(object? value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Cannot convert null range value to string.");
            }
            var rangeValue = (RangeValue)value;
            return $"{rangeValue.Start} {rangeValue.Step} {rangeValue.End}";
        }

        private static string GetStringValueString(object? value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Cannot convert null string value to string.");
            }

            return $"\"{value}\"";
        }

        private static string GetBoolValueString(object? value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Cannot convert null bool value to string.");
            }

            return value.ToString().ToLower();
        }

        private static string GetPauliValueString(object? value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Cannot convert null pauli value to string.");
            }

            return value.ToString().ToLower();
        }

        private static string GetDoubleValueString(object? value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Cannot convert null double value to string.");
            }

            return value.ToString().ToLower();
        }

        private static string GetIntegerValueString(object? value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Cannot convert null integer value to string.");
            }

            return value.ToString().ToLower();
        }
    }
}
