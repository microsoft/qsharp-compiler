using System;
using System.Collections;
using System.Collections.Generic;
using Ubiquity.NET.Llvm;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QirGenerator
{
    /// <summary>
    /// A simple class to manage a library of runtime functions.
    /// This class tries to avoid clutter by only generating declarations for functions that are actually
    /// called.
    /// Functions that are defined but never used will not have an declaration generated.
    /// </summary>
    internal class FunctionLibrary : IEnumerable<KeyValuePair<string, IrFunction>>
    {
        private readonly BitcodeModule module;
        private readonly Dictionary<string, IFunctionType> runtimeFunctions = new Dictionary<string, IFunctionType>();
        private readonly Dictionary<string, IrFunction> usedRuntimeFunctions =
            new Dictionary<string, IrFunction>();
        private readonly Func<string, string> nameMapper;

        /// <summary>
        /// Constructs a new function library.
        /// </summary>
        /// <param name="mod">The LLVM module in which the functions will be declared and/or defined</param>
        /// <param name="mapper">A function that maps the short name of the function into a mangled name</param>
        public FunctionLibrary(BitcodeModule mod, Func<string, string> mapper)
        {
            this.module = mod;
            this.nameMapper = mapper;
        }

        /// <summary>
        /// Adds a function to the library.
        /// This doesn't actually create a declaration for the function; instead, it records all of the
        /// required information so that the declaration can be created on demand.
        /// </summary>
        /// <param name="name">The simple, unmangled name of the function</param>
        /// <param name="returnType">The return type of the function</param>
        /// <param name="argTypes">The types of the function's arguments, as an array</param>
        public void AddFunction(string name, ITypeRef returnType, params ITypeRef[] argTypes)
        {
            this.runtimeFunctions[name] = this.module.Context.GetFunctionType(returnType, argTypes);
        }

        /// <summary>
        /// Adds a function with a variable argument list to the library.
        /// This doesn't actually create a declaration for the function; instead, it records all of the
        /// required information so that the declaration can be created on demand.
        /// </summary>
        /// <param name="name">The simple, unmangled name of the function</param>
        /// <param name="returnType">The return type of the function</param>
        /// <param name="argTypes">The types of the function's fixed arguments, as an array</param>
        public void AddVarargsFunction(string name, ITypeRef returnType, params ITypeRef[] argTypes)
        {
            this.runtimeFunctions[name] = this.module.Context.GetFunctionType(returnType, argTypes, true);
        }

        /// <summary>
        /// Gets a reference to a function.
        /// If the function has not already been declared, a new declaration is generated for it.
        /// </summary>
        /// <param name="name">The simple, unmangled name of the function</param>
        /// <returns>The object that represents the function</returns>
        public IrFunction GetFunction(string name)
        {
            var mappedName = this.nameMapper(name);
            var func = this.module.TryGetFunction(mappedName, out var fct) ?
                fct : this.module.CreateFunction(mappedName, this.runtimeFunctions[name]);
            this.usedRuntimeFunctions[name] = func;
            return func;
        }

        private class LibEnumerator : IEnumerator<KeyValuePair<string, IrFunction>>, IEnumerator
        {
            private bool disposedValue;
            private Dictionary<string, IrFunction>.Enumerator innerEnum;

            public LibEnumerator(Dictionary<string, IrFunction> dict)
            {
                this.innerEnum = dict.GetEnumerator();
            }

            public KeyValuePair<string, IrFunction> Current => this.innerEnum.Current;

            object IEnumerator.Current => this.innerEnum.Current;

            public bool MoveNext() => this.innerEnum.MoveNext();

            public void Reset() => throw new NotImplementedException();

            protected virtual void Dispose(bool disposing)
            {
                if (!this.disposedValue)
                {
                    if (disposing)
                    {
                        this.innerEnum.Dispose();
                    }

                    this.disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                this.Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Gets an enumerator through the runtime functions in this library that have
        /// actually been used.
        /// The enumerator returns KeyValuePairs with the base name of the function as 
        /// key and the actual LLVM function object as the value.
        /// </summary>
        /// <returns>The enumerator</returns>
        public IEnumerator<KeyValuePair<string, IrFunction>> GetEnumerator() => 
            new LibEnumerator(this.usedRuntimeFunctions);

        /// <summary>
        /// Gets an enumerator through the runtime functions in this library that have
        /// actually been used.
        /// The enumerator returns KeyValuePairs with the base name of the function as 
        /// key and the actual LLVM function object as the value.
        /// </summary>
        /// <returns>The enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator() => new LibEnumerator(this.usedRuntimeFunctions);
    }
}
