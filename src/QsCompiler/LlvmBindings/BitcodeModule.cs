// -----------------------------------------------------------------------
// <copyright file="BitcodeModule.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Enumeration to indicate the behavior of module level flags metadata sharing the same name in a <see cref="BitcodeModule"/>.</summary>
    [SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "It isn't a flags enum")]
    public enum ModuleFlagBehavior
    {
        /// <summary>Invalid value (default value for this enumeration).</summary>
        Invalid = 0,

        /// <summary>Emits an error if two values disagree, otherwise the resulting value is that of the operands.</summary>
        Error = LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorError,

        /// <summary>Emits a warning if two values disagree. The result will be the operand for the flag from the first module being linked.</summary>
        Warning = LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning,

        /// <summary>Adds a requirement that another module flag be present and have a specified value after linking is performed.</summary>
        /// <remarks>
        /// The value must be a metadata pair, where the first element of the pair is the ID of the module flag to be restricted, and the
        /// second element of the pair is the value the module flag should be restricted to. This behavior can be used to restrict the
        /// allowable results (via triggering of an error) of linking IDs with the <see cref="Override"/> behavior.
        /// </remarks>
        Require = LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorRequire,

        /// <summary>Uses the specified value, regardless of the behavior or value of the other module.</summary>
        /// <remarks>If both modules specify Override, but the values differ, and error will be emitted.</remarks>
        Override = LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorOverride,

        /// <summary>Appends the two values, which are required to be metadata nodes.</summary>
        Append = LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorAppend,

        /// <summary>Appends the two values, which are required to be metadata nodes dropping duplicate entries in the second list.</summary>
        AppendUnique = LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorAppendUnique,
    }

    /// <summary>LLVM Bit-code module.</summary>
    /// <remarks>
    /// A module is the basic unit for containing code in LLVM. Modules are an in memory
    /// representation of the LLVM Intermediate Representation (IR) bit-code. Each.
    /// </remarks>
    public sealed class BitcodeModule
        : IDisposable
    {
        private LLVMModuleRef moduleHandle;

        private BitcodeModule(LLVMModuleRef handle)
        {
            this.moduleHandle = handle;
            this.Context = ContextCache.GetContextFor(handle.Context);
        }

        /// <summary>Gets a value indicating whether the module is disposed or not.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>Gets the <see cref="Context"/> this module belongs to.</summary>
        public Context Context { get; }

        /// <summary>Gets or sets the Target Triple describing the target, ABI and OS.</summary>
        public string TargetTriple
        {
            get
            {
                this.ThrowIfDisposed();
                return this.moduleHandle.Target;
            }

            set
            {
                this.moduleHandle.Target = value;
            }
        }

        /// <summary>Gets the <see cref="GlobalVariable"/>s contained by this module.</summary>
        public IEnumerable<GlobalVariable> Globals
        {
            get
            {
                this.ThrowIfDisposed();
                var current = this.moduleHandle.FirstGlobal;
                while (current != default)
                {
                    yield return Value.FromHandle<GlobalVariable>(current)!;
                    current = current.NextGlobal;
                }
            }
        }

        /// <summary>Gets the functions contained in this module.</summary>
        public IEnumerable<IrFunction> Functions
        {
            get
            {
                this.ThrowIfDisposed();
                var current = this.moduleHandle.FirstFunction;
                while (current != default)
                {
                    yield return Value.FromHandle<IrFunction>(current)!;
                    current = current.NextFunction;
                }
            }
        }

        /// <summary>Gets the global aliases in this module.</summary>
        public IEnumerable<GlobalAlias> Aliases
        {
            get
            {
                this.ThrowIfDisposed();
                var current = this.moduleHandle.FirstGlobalAlias();
                while (current != default)
                {
                    yield return Value.FromHandle<GlobalAlias>(current)!;
                    current = current.NextGlobalAlias();
                }
            }
        }

        /// <summary>Gets the name of the module.</summary>
        public string Name
        {
            get
            {
                this.ThrowIfDisposed();
                return this.moduleHandle.GetModuleIdentifier();
            }
        }

        internal ref LLVMModuleRef ModuleHandle => ref this.moduleHandle;

        /// <summary>Load a bit-code module from a given file.</summary>
        /// <param name="path">path of the file to load.</param>
        /// <param name="context">Context to use for creating the module.</param>
        /// <returns>Loaded <see cref="BitcodeModule"/>.</returns>
        public static BitcodeModule LoadFrom(string path, Context context)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            var buffer = new MemoryBuffer(path);
            return LoadFrom(buffer, context);
        }

        /// <summary>Load bit code from a memory buffer.</summary>
        /// <param name="buffer">Buffer to load from.</param>
        /// <param name="context">Context to load the module into.</param>
        /// <returns>Loaded <see cref="BitcodeModule"/>.</returns>
        /// <remarks>
        /// This along with <see cref="WriteToBuffer"/> are useful for "cloning"
        /// a module from one context to another. This allows creation of multiple
        /// modules on different threads and contexts and later moving them to a
        /// single context in order to link them into a single final module for
        /// optimization.
        /// </remarks>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Module created here is owned, and disposed of via the projected BitcodeModule")]
        public static BitcodeModule LoadFrom(MemoryBuffer buffer, Context context)
        {
            return context.ContextHandle.TryParseBitcode(buffer.BufferHandle, out LLVMModuleRef modRef, out string message)
                ? throw new InternalCodeGeneratorException(message)
                : context.GetModuleFor(modRef);
        }

        /// <summary>Disposes the <see cref="BitcodeModule"/>, releasing resources associated with the module in native code.</summary>
        public void Dispose()
        {
            // if not already disposed, dispose the module. Do this only on dispose. The containing context
            // will clean up the module when it is disposed or finalized. Since finalization order isn't
            // deterministic it is possible that the module is finalized after the context has already run its
            // finalizer, which would cause an access violation in the native LLVM layer.
            if (!this.IsDisposed)
            {
                // remove the module handle from the module cache.
                this.moduleHandle.Dispose();
                this.IsDisposed = true;
            }
        }

        /// <summary>Link another module into this one.</summary>
        /// <param name="otherModule">module to link into this one.</param>
        /// <remarks>
        /// <note type="warning">
        /// <paramref name="otherModule"/> is destroyed by this process and no longer usable
        /// when this method returns.
        /// </note>
        /// </remarks>
        public void Link(BitcodeModule otherModule)
        {
            this.ThrowIfDisposed();

            if (otherModule.Context != this.Context)
            {
                throw new ArgumentException();
            }

            if (this.moduleHandle.Link(otherModule.moduleHandle))
            {
                throw new InternalCodeGeneratorException();
            }

            this.Context.RemoveModule(otherModule);
            otherModule.Detach().Dispose();
        }

        /// <summary>Verifies a bit-code module.</summary>
        /// <param name="errorMessage">Error messages describing any issues found in the bit-code.</param>
        /// <returns>true if the verification succeeded and false if not.</returns>
        public bool Verify(out string errorMessage)
        {
            this.ThrowIfDisposed();
            return this.moduleHandle.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out errorMessage);
        }

        /// <summary>Gets a function by name from this module.</summary>
        /// <param name="name">Name of the function to get.</param>
        /// <returns>The function or default if not found.</returns>
        [Obsolete("Use TryGetFunction instead")]
        public IrFunction? GetFunction(string name)
        {
            this.ThrowIfDisposed();

            var funcRef = this.moduleHandle.GetNamedFunction(name);
            return funcRef == default ? default : Value.FromHandle<IrFunction>(funcRef);
        }

        /// <summary>Looks up a function in the module by name.</summary>
        /// <param name="name">Name of the function.</param>
        /// <param name="function">The function or <see langword="default"/> if not found.</param>
        /// <returns><see langword="true"/> if the function was found or <see langword="false"/> if not.</returns>
        public bool TryGetFunction(string name, [MaybeNullWhen(false)] out IrFunction function)
        {
            this.ThrowIfDisposed();

            var funcRef = this.moduleHandle.GetNamedFunction(name);
            if (funcRef == default)
            {
                function = default;
                return false;
            }

            function = Value.FromHandle<IrFunction>(funcRef)!;
            return true;
        }

        /// <summary>Add a function with the specified signature to the module.</summary>
        /// <param name="name">Name of the function to add.</param>
        /// <param name="signature">Signature of the function.</param>
        /// <returns><see cref="IrFunction"/>matching the specified signature and name.</returns>
        /// <remarks>
        /// If a matching function already exists it is returned, and therefore the returned
        /// <see cref="IrFunction"/> may have a body and additional attributes. If a function of
        /// the same name exists with a different signature an exception is thrown as LLVM does
        /// not perform any function overloading.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        [Obsolete("Use CreateFunction( string name, IFunctionType signature ) instead")]
        public IrFunction AddFunction(string name, IFunctionType signature)
        {
            return this.CreateFunction(name, signature);
        }

        /// <summary>Gets an existing function with the specified signature to the module or creates a new one if it doesn't exist.</summary>
        /// <param name="name">Name of the function to add.</param>
        /// <param name="signature">Signature of the function.</param>
        /// <returns><see cref="IrFunction"/>matching the specified signature and name.</returns>
        /// <remarks>
        /// If a matching function already exists it is returned, and therefore the returned
        /// <see cref="IrFunction"/> may have a body and additional attributes. If a function of
        /// the same name exists with a different signature an exception is thrown as LLVM does
        /// not perform any function overloading.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Specific type required by interop call")]
        public IrFunction CreateFunction(string name, IFunctionType signature)
        {
            this.ThrowIfDisposed();

            LLVMValueRef valueRef = this.moduleHandle.GetNamedFunction(name);
            if (valueRef == default)
            {
                valueRef = this.moduleHandle.AddFunction(name, signature.GetTypeRef());
            }

            return Value.FromHandle<IrFunction>(valueRef)!;
        }

        /// <summary>Writes a bit-code module to a file.</summary>
        /// <param name="path">Path to write the bit-code into.</param>
        /// <remarks>
        /// This is a blind write. (e.g. no verification is performed)
        /// So if an invalid module is saved it might not work with any
        /// later stage processing tools.
        /// </remarks>
        public void WriteToFile(string path)
        {
            this.ThrowIfDisposed();

            var status = this.moduleHandle.WriteBitcodeToFile(path);
            if (status != 0)
            {
                throw new IOException();
            }
        }

        /// <summary>Writes this module as LLVM IR source to a file.</summary>
        /// <param name="path">File to write the LLVM IR source to.</param>
        /// <param name="errMsg">Error messages encountered, if any.</param>
        /// <returns><see langword="true"/> if successful or <see langword="false"/> if not.</returns>
        public bool WriteToTextFile(string path, out string errMsg)
        {
            this.ThrowIfDisposed();

            return this.moduleHandle.TryPrintToFile(path, out errMsg);
        }

        /// <summary>Creates a string representation of the module.</summary>
        /// <returns>LLVM textual representation of the module.</returns>
        /// <remarks>
        /// This is intentionally NOT an override of ToString() as that is
        /// used by debuggers to show the value of a type and this can take
        /// an extremely long time (up to many seconds depending on complexity
        /// of the module) which is bad for the debugger.
        /// </remarks>
        public string WriteToString()
        {
            this.ThrowIfDisposed();
            return this.moduleHandle.PrintToString();
        }

        /// <summary>Writes the LLVM IR bit code into a memory buffer.</summary>
        /// <returns><see cref="MemoryBuffer"/> containing the bit code module.</returns>
        public MemoryBuffer WriteToBuffer()
        {
            this.ThrowIfDisposed();
            return new MemoryBuffer(memoryBufferRef: this.moduleHandle.WriteBitcodeToMemoryBuffer());
        }

        /// <summary>Add an alias to the module.</summary>
        /// <param name="aliasee">Value being aliased.</param>
        /// <param name="aliasName">Name of the alias.</param>
        /// <returns><see cref="GlobalAlias"/> for the alias.</returns>
        public GlobalAlias AddAlias(Value aliasee, string aliasName)
        {
            this.ThrowIfDisposed();

            var handle = this.moduleHandle.AddAlias(aliasee.NativeType.GetTypeRef(), aliasee.ValueHandle, aliasName);
            return Value.FromHandle<GlobalAlias>(handle)!;
        }

        /// <summary>Get an alias by name.</summary>
        /// <param name="name">name of the alias to get.</param>
        /// <returns>Alias matching <paramref name="name"/> or default if no such alias exists.</returns>
        public GlobalAlias GetAlias(string name)
        {
            this.ThrowIfDisposed();

            var handle = this.moduleHandle.GetNamedGlobalAlias(name);
            return Value.FromHandle<GlobalAlias>(handle);
        }

        /// <summary>Adds a global to this module with a specific address space.</summary>
        /// <param name="addressSpace">Address space to add the global to.</param>
        /// <param name="typeRef">Type of the value.</param>
        /// <param name="name">Name of the global.</param>
        /// <returns>The new <see cref="GlobalVariable"/>.</returns>
        /// <openissues>
        /// - What does LLVM do if creating a second Global with the same name (return null, throw, crash??,...)
        /// </openissues>
        public GlobalVariable AddGlobalInAddressSpace(uint addressSpace, ITypeRef typeRef, string name)
        {
            this.ThrowIfDisposed();

            var handle = this.moduleHandle.AddGlobalInAddressSpace(typeRef.GetTypeRef(), name, addressSpace);
            return Value.FromHandle<GlobalVariable>(handle)!;
        }

        /// <summary>Adds a global to this module.</summary>
        /// <param name="addressSpace">Address space to add the global to.</param>
        /// <param name="typeRef">Type of the value.</param>
        /// <param name="isConst">Flag to indicate if this global is a constant.</param>
        /// <param name="linkage">Linkage type for this global.</param>
        /// <param name="constVal">Initial value for the global.</param>
        /// <returns>New global variable.</returns>
        public GlobalVariable AddGlobalInAddressSpace(uint addressSpace, ITypeRef typeRef, bool isConst, Linkage linkage, Constant constVal)
        {
            this.ThrowIfDisposed();

            return this.AddGlobalInAddressSpace(addressSpace, typeRef, isConst, linkage, constVal, string.Empty);
        }

        /// <summary>Adds a global to this module.</summary>
        /// <param name="addressSpace">Address space to add the global to.</param>
        /// <param name="typeRef">Type of the value.</param>
        /// <param name="isConst">Flag to indicate if this global is a constant.</param>
        /// <param name="linkage">Linkage type for this global.</param>
        /// <param name="constVal">Initial value for the global.</param>
        /// <param name="name">Name of the variable.</param>
        /// <returns>New global variable.</returns>
        public GlobalVariable AddGlobalInAddressSpace(uint addressSpace, ITypeRef typeRef, bool isConst, Linkage linkage, Constant constVal, string name)
        {
            this.ThrowIfDisposed();

            var retVal = this.AddGlobalInAddressSpace(addressSpace, typeRef, name);
            retVal.IsConstant = isConst;
            retVal.Linkage = linkage;
            retVal.Initializer = constVal;
            return retVal;
        }

        /// <summary>Adds a global to this module.</summary>
        /// <param name="typeRef">Type of the value.</param>
        /// <param name="name">Name of the global.</param>
        /// <returns>The new <see cref="GlobalVariable"/>.</returns>
        /// <openissues>
        /// - What does LLVM do if creating a second Global with the same name (return null, throw, crash??,...)
        /// </openissues>
        public GlobalVariable AddGlobal(ITypeRef typeRef, string name)
        {
            this.ThrowIfDisposed();

            var handle = this.moduleHandle.AddGlobal(typeRef.GetTypeRef(), name);
            return Value.FromHandle<GlobalVariable>(handle)!;
        }

        /// <summary>Adds a global to this module.</summary>
        /// <param name="typeRef">Type of the value.</param>
        /// <param name="isConst">Flag to indicate if this global is a constant.</param>
        /// <param name="linkage">Linkage type for this global.</param>
        /// <param name="constVal">Initial value for the global.</param>
        /// <returns>New global variable.</returns>
        public GlobalVariable AddGlobal(ITypeRef typeRef, bool isConst, Linkage linkage, Constant constVal)
        {
            this.ThrowIfDisposed();

            return this.AddGlobal(typeRef, isConst, linkage, constVal, string.Empty);
        }

        /// <summary>Adds a global to this module.</summary>
        /// <param name="typeRef">Type of the value.</param>
        /// <param name="isConst">Flag to indicate if this global is a constant.</param>
        /// <param name="linkage">Linkage type for this global.</param>
        /// <param name="constVal">Initial value for the global.</param>
        /// <param name="name">Name of the variable.</param>
        /// <returns>New global variable.</returns>
        public GlobalVariable AddGlobal(ITypeRef typeRef, bool isConst, Linkage linkage, Constant constVal, string name)
        {
            this.ThrowIfDisposed();

            var retVal = this.AddGlobal(typeRef, name);
            retVal.IsConstant = isConst;
            retVal.Linkage = linkage;
            retVal.Initializer = constVal;
            return retVal;
        }

        /// <summary>Retrieves a <see cref="ITypeRef"/> by name from the module.</summary>
        /// <param name="name">Name of the type.</param>
        /// <returns>The type or default if no type with the specified name exists in the module.</returns>
        public ITypeRef? GetTypeByName(string name)
        {
            this.ThrowIfDisposed();

            var hType = this.moduleHandle.GetTypeByName(name);
            return hType == default ? default : TypeRef.FromHandle(hType);
        }

        /// <summary>Retrieves a named global from the module.</summary>
        /// <param name="name">Name of the global.</param>
        /// <returns><see cref="GlobalVariable"/> or <see langword="default"/> if not found.</returns>
        public GlobalVariable? GetNamedGlobal(string name)
        {
            this.ThrowIfDisposed();

            var hGlobal = this.moduleHandle.GetNamedGlobal(name);
            return hGlobal == default ? default : Value.FromHandle<GlobalVariable>(hGlobal);
        }

        /// <summary>Gets a declaration for an LLVM intrinsic function.</summary>
        /// <param name="name">Name of the intrinsic.</param>
        /// <param name="args">Args for the intrinsic.</param>
        /// <returns>Function declaration.</returns>
        /// <remarks>
        /// This method will match overloaded intrinsics based on the parameter types. If an intrinsic
        /// has no overloads then an exact match is required. If the intrinsic has overloads than a prefix
        /// match is used.
        /// <note type="important">
        /// It is important to note that the prefix match requires the name provided to have a length greater
        /// than that of the name of the intrinsic and that the name starts with a matching overloaded intrinsic.
        /// for example: 'llvm.memset' would not match the overloaded memset intrinsic but 'llvm.memset.p.i' does.
        /// Thus, it is generally a good idea to use the signature from the LLVM documentation without the address
        /// space, or bit widths. That is instead of 'llvm.memset.p0i8.i32' use 'llvm.memset.p.i'.
        /// </note>
        /// </remarks>
        public IrFunction GetIntrinsicDeclaration(string name, params ITypeRef[] args)
        {
            uint id = Intrinsic.LookupId(name);
            return this.GetIntrinsicDeclaration(id, args);
        }

        /// <summary>Gets a declaration for an LLVM intrinsic function.</summary>
        /// <param name="id">id of the intrinsic.</param>
        /// <param name="args">Arguments for the intrinsic.</param>
        /// <returns>Function declaration.</returns>
        public IrFunction GetIntrinsicDeclaration(uint id, params ITypeRef[] args)
        {
            if (LLVM.IntrinsicIsOverloaded(id) == 0 && args.Length > 0)
            {
                throw new ArgumentException();
            }

            LLVMTypeRef[] llvmArgs = args.Select(a => a.GetTypeRef()).ToArray();
            LLVMValueRef valueRef = this.moduleHandle.GetIntrinsicDeclaration(id, llvmArgs);
            return (IrFunction)Value.FromHandle(valueRef)!;
        }

        /// <summary>Clones the current module.</summary>
        /// <returns>Cloned module.</returns>
        public BitcodeModule Clone()
        {
            this.ThrowIfDisposed();
            return FromHandle(this.moduleHandle.Clone())!;
        }

        /// <summary>Clones the module into a new <see cref="Context"/>.</summary>
        /// <param name="targetContext"><see cref="Context"/> to clone the module into.</param>
        /// <returns>Cloned copy of the module.</returns>
        public BitcodeModule Clone(Context targetContext)
        {
            this.ThrowIfDisposed();

            if (targetContext == this.Context)
            {
                return this.Clone();
            }

            var buffer = this.WriteToBuffer();
            var retVal = LoadFrom(buffer, targetContext);
            return retVal;
        }

        internal static BitcodeModule FromHandle(LLVMModuleRef nativeHandle)
        {
            var contextRef = nativeHandle.Context;
            Context context = ContextCache.GetContextFor(contextRef);
            return context.GetModuleFor(nativeHandle);
        }

        internal LLVMModuleRef Detach()
        {
            this.ThrowIfDisposed();
            this.Context.RemoveModule(this);
            var retVal = this.moduleHandle;
            this.moduleHandle = default;
            return retVal;
        }

        private void ThrowIfDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().ToString());
            }
        }

        internal class InterningFactory
            : HandleInterningMap<LLVMModuleRef, BitcodeModule>,
            IBitcodeModuleFactory
        {
            internal InterningFactory(Context context)
                : base(context)
            {
            }

            public BitcodeModule CreateBitcodeModule() => this.CreateBitcodeModule(string.Empty);

            public BitcodeModule CreateBitcodeModule(string moduleId)
            {
                var hContext = this.Context.ContextHandle.CreateModuleWithName(moduleId);
                return this.GetOrCreateItem(hContext);
            }

            private protected override BitcodeModule ItemFactory(LLVMModuleRef handle)
            {
                var contextRef = handle.Context;
                return this.Context.ContextHandle != contextRef
                    ? throw new ArgumentException()
                    : new BitcodeModule(handle);
            }
        }
    }
}
