// -----------------------------------------------------------------------
// <copyright file="ILibLlvm.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Ubiquity.NET.Llvm.Interop
{
    /// <summary>Code gen target to register/initialize</summary>
    public enum CodeGenTarget
    {
        /// <summary>Native target of the host system, generally used for JIT execution</summary>
        Native,

        /// <summary>ARM AArch64 target</summary>
        AArch64,

        /// <summary>AMD GPU target</summary>
        AMDGPU,

        /// <summary>ARM 32 bit targets</summary>
        ARM,

        /// <summary>BPF target</summary>
        BPF,

        /// <summary>Hexagon target</summary>
        Hexagon,

        /// <summary>Lanai target</summary>
        Lanai,

        /// <summary>MIPS target</summary>
        MIPS,

        /// <summary>MSP430 target</summary>
        MSP430,

        /// <summary>NVIDIA PTX target</summary>
        NvidiaPTX,

        /// <summary>PowerPV target</summary>
        PowerPC,

        /// <summary>Sparc target</summary>
        Sparc,

        /// <summary>SystemZ target</summary>
        SystemZ,

        /// <summary>WebAssembly target</summary>
        WebAssembly,

        /// <summary>X86 target</summary>
        X86,

        /// <summary>XCore target</summary>
        XCore,

        /// <summary>RISC-V target</summary>
        RISCV,

        /// <summary>All available targets</summary>
        All = int.MaxValue
    }

    /// <summary>Target tools to register/enable</summary>
    [Flags]
    public enum TargetRegistrations
    {
        /// <summary>Register nothing</summary>
        None = 0x00,

        /// <summary>Register the Target class</summary>
        Target = 0x01,

        /// <summary>Register the Target info for the target</summary>
        TargetInfo = 0x02,

        /// <summary>Register the target machine(s) for a target</summary>
        TargetMachine = 0x04,

        /// <summary>Registers the assembly source code generator for a target</summary>
        AsmPrinter = 0x08,

        /// <summary>Registers the Disassembler for a target</summary>
        Disassembler = 0x10,

        /// <summary>Registers the assembly source parser for a target</summary>
        AsmParser = 0x20,

        /// <summary>Registers all the code generation components</summary>
        CodeGen = Target | TargetInfo | TargetMachine,

        /// <summary>Registers all components</summary>
        All = CodeGen | AsmPrinter | Disassembler | AsmParser
    }

    /// <summary>Interface to the core LLVM library itself</summary>
    /// <remarks>
    /// When this instance is disposed the LLVM libraries are no longer usable in the process
    /// <note type="important">
    /// It is important to note that the LLVM library does NOT currently support re-initialization in
    /// the same process. Therefore, it is recommended that initialization is done once at process startup
    /// and then the resulting interface disposed just before the process exits.
    /// </note>
    /// </remarks>
    public interface ILibLlvm
        : IDisposable
    {
        /// <summary>Registers components for ARM AArch64 target(s)</summary>
        /// <param name="target">Target architecture to register/initialize</param>
        /// <param name="registrations">Flags indicating which components to register/enable</param>
        void RegisterTarget( CodeGenTarget target, TargetRegistrations registrations = TargetRegistrations.All );
    }
}
