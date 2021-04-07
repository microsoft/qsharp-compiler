using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Quantum.QsCompiler.BondSchemas.EntryPoint;

namespace Microsoft.Quantum.QsCompiler.Templates
{
    public class ArgumentCpp : Argument
    {
        public ArgumentCpp(Argument argument)
        {
            this.Name = argument.Name;
            this.Position = argument.Position;
            this.Type = argument.Type;
            this.ArrayType = argument.ArrayType;
            this.Values = argument.Values.ToList();
        }

        public string CppType()
        {
            return this.Type switch
            {
                DataType.BoolType => "char",
                DataType.IntegerType => "int64_t",
                DataType.DoubleType => "double",
                DataType.PauliType => "char",
                DataType.RangeType => "InteropRange*",
                DataType.ResultType => "char",
                DataType.StringType => "const char*",
                DataType.ArrayType => "InteropArray *",
            };
        }

        public string CliOptionString()
        {
            if (this.Name.Length == 1)
            {
                return $"-{this.Name}";
            }
            else
            {
                return $"--{this.Name}";
            }
        }

        public string? CliDescription()
        {
            return this.Type switch
            {
                DataType.BoolType => "A bool value",
                DataType.IntegerType => "An integer value",
                DataType.DoubleType => "A double value",
                DataType.PauliType => "A Pauli value",
                DataType.RangeType => "A Range value (start, step, end)",
                DataType.ResultType => "A Result value",
                DataType.StringType => "A String value",
                DataType.ArrayType => this.ArrayType switch
                {
                    DataType.BoolType => "A bool array",
                    DataType.IntegerType => "An integer array",
                    DataType.DoubleType => "A double array",
                    DataType.PauliType => "A Pauli array",
                    DataType.RangeType => "A Range array",
                    DataType.ResultType => "A Result array",
                    DataType.StringType => "A String array",
                },
            };
        }

        public string? CppVarType()
        {
            return this.Type switch
            {
                DataType.BoolType => "char",
                DataType.IntegerType => "int64_t",
                DataType.DoubleType => "double_t",
                DataType.PauliType => "PauliId",
                DataType.RangeType => "RangeTuple",
                DataType.ResultType => "char",
                DataType.StringType => "string",
                DataType.ArrayType => this.ArrayType switch
                {
                    DataType.BoolType => "vector<char>",
                    DataType.IntegerType => "vector<int64_t>",
                    DataType.DoubleType => "vector<double_t>",
                    DataType.PauliType => "std::vector<PauliId>",
                    DataType.RangeType => "vector<RangeTuple>",
                    DataType.ResultType => "vector<char>",
                    DataType.StringType => "vector<string>",
                },
            };
        }

        public string? CppVarInitialValue()
        {
            return this.Type switch
            {
                DataType.BoolType => "InteropFalseAsChar",
                DataType.IntegerType => "0",
                DataType.DoubleType => "0.0",
                DataType.PauliType => "PauliId::PauliId_I",
                DataType.RangeType => null,
                DataType.ResultType => "InteropResultZeroAsChar",
                DataType.StringType => null,
                DataType.ArrayType => this.ArrayType switch
                {
                    DataType.BoolType => null,
                    DataType.IntegerType => null,
                    DataType.DoubleType => null,
                    DataType.PauliType => null,
                    DataType.RangeType => null,
                    DataType.ResultType => null,
                    DataType.StringType => null,
                },
            };
        }

        public string? TransformationType()
        {
            return this.Type switch
            {
                DataType.BoolType => "BoolAsCharMap",
                DataType.IntegerType => null,
                DataType.DoubleType => null,
                DataType.PauliType => "PauliMap",
                DataType.RangeType => null,
                DataType.ResultType => "ResultAsCharMap",
                DataType.StringType => null,
                DataType.ArrayType => this.ArrayType switch
                {
                    DataType.BoolType => "BoolAsCharMap",
                    DataType.IntegerType => null,
                    DataType.DoubleType => null,
                    DataType.PauliType => "PauliMap",
                    DataType.RangeType => null,
                    DataType.ResultType => "ResultAsCharMap",
                    DataType.StringType => null,
                },
            };
        }
    }

    public class EntryPointOperationCpp : EntryPointOperation
    {
        public List<ArgumentCpp> InteropArguments;

        public EntryPointOperationCpp(EntryPointOperation entryPointOperation)
        {
            this.Name = entryPointOperation.Name;
            this.Arguments = entryPointOperation.Arguments.ToList();
            this.Arguments.Sort((a, b) => a.Position.CompareTo(b.Position));
            this.InteropArguments = entryPointOperation.Arguments.Select(arg => new ArgumentCpp(arg)).ToList();
        }

        public bool ContainsArgumentType(DataType type)
        {
            foreach (Argument arg in this.Arguments)
            {
                if (arg.Type == type)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
