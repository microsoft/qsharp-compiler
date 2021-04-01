// <copyright file="StaticState.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Interop
{
    /// <summary>Provides support for various LLVM static state initialization and manipulation.</summary>
    public sealed class Library
        : DisposableObject,
        ILibLlvm
    {
        private static int currentInitializationState;

        // lazy initialized singleton unmanaged delegate so it is never collected
        private static Lazy<LLVMFatalErrorHandler>? fatalErrorHandlerDelegate;

        private Library()
        {
        }

        private enum InitializationState
        {
            Uninitialized,
            Initializing,
            Initialized,
            ShuttingDown,
            ShutDown, // NOTE: This is a terminal state, it doesn't return to uninitialized
        }

        /// <summary>Initializes the native LLVM library support.</summary>
        /// <returns>
        /// <see cref="IDisposable"/> implementation for the library.
        /// </returns>
        /// <remarks>
        /// This can only be called once per application to initialize the
        /// LLVM library. <see cref="IDisposable.Dispose()"/> will release
        /// any resources allocated by the library. The current LLVM library does
        /// *NOT* support re-initialization within the same process. Thus, this
        /// is best used at the top level of the application and released at or
        /// near process exit.
        /// </remarks>
        public static ILibLlvm InitializeLLVM()
        {
            var previousState = (InitializationState)Interlocked.CompareExchange(
                ref currentInitializationState,
                (int)InitializationState.Initializing,
                (int)InitializationState.Uninitialized);
            if (previousState != InitializationState.Uninitialized)
            {
                throw new InvalidOperationException();
            }

            // initialize the static fields
            unsafe
            {
                fatalErrorHandlerDelegate = new Lazy<LLVMFatalErrorHandler>(() => FatalErrorHandler, LazyThreadSafetyMode.PublicationOnly);
            }

            LLVM.InstallFatalErrorHandler(Marshal.GetFunctionPointerForDelegate(fatalErrorHandlerDelegate.Value));
            Interlocked.Exchange(ref currentInitializationState, (int)InitializationState.Initialized);
            return new Library();
        }

        /// <inheritdoc/>
        public void RegisterTarget(CodeGenTarget target, TargetRegistrations registrations = TargetRegistrations.All)
        {
            switch (target)
            {
                case CodeGenTarget.Native:
                    RegisterNative(registrations);
                    break;
                case CodeGenTarget.AArch64:
                    RegisterAArch64(registrations);
                    break;
                case CodeGenTarget.AMDGPU:
                    RegisterAMDGPU(registrations);
                    break;
                case CodeGenTarget.ARM:
                    RegisterARM(registrations);
                    break;
                case CodeGenTarget.BPF:
                    RegisterBPF(registrations);
                    break;
                case CodeGenTarget.Hexagon:
                    RegisterHexagon(registrations);
                    break;
                case CodeGenTarget.Lanai:
                    RegisterLanai(registrations);
                    break;
                case CodeGenTarget.MIPS:
                    RegisterMips(registrations);
                    break;
                case CodeGenTarget.MSP430:
                    RegisterMSP430(registrations);
                    break;
                case CodeGenTarget.NvidiaPTX:
                    RegisterNVPTX(registrations);
                    break;
                case CodeGenTarget.PowerPC:
                    RegisterPowerPC(registrations);
                    break;
                case CodeGenTarget.Sparc:
                    RegisterSparc(registrations);
                    break;
                case CodeGenTarget.SystemZ:
                    RegisterSystemZ(registrations);
                    break;
                case CodeGenTarget.WebAssembly:
                    RegisterWebAssembly(registrations);
                    break;
                case CodeGenTarget.X86:
                    RegisterX86(registrations);
                    break;
                case CodeGenTarget.XCore:
                    RegisterXCore(registrations);
                    break;
                case CodeGenTarget.RISCV:
                    RegisterRISCV(registrations);
                    break;
                case CodeGenTarget.All:
                    RegisterAll(registrations);
                    break;
            }
        }

        // TODO: Figure out how to read targets.def to get the full set of target architectures
        // and generate all of the registration (including skipping of any init calls that don't exist).

        // basic pattern to follow for any new targets in the future
        /*
        /// <summary>Registers components for the XXX target</summary>
        /// <param name="registrations">Flags indicating which components to register/enable</param>
        public static void RegisterXXX( TargetRegistrations registrations = TargetRegistrations.All )
        {
            if( registrations.HasFlag( TargetRegistrations.Target ) )
            {
                LLVM.InitializeXXXTarget( );
            }

            if( registrations.HasFlag( TargetRegistrations.TargetInfo ) )
            {
                LLVM.InitializeXXXTargetInfo( );
            }

            if( registrations.HasFlag( TargetRegistrations.TargetMachine ) )
            {
                LLVM.InitializeXXXTargetMC( );
            }

            if( registrations.HasFlag( TargetRegistrations.AsmPrinter ) )
            {
                LLVM.InitializeXXXAsmPrinter( );
            }

            if( registrations.HasFlag( TargetRegistrations.Disassembler ) )
            {
                LLVM.InitializeXXXDisassembler( );
            }

            if( registrations.HasFlag( TargetRegistrations.AsmParser ) )
            {
                LLVM.InitializeXXXAsmParser( );
            }
        }
        */

        /// <summary>Registers components for all available targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterAll(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeAllTargets();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeAllTargetInfos();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeAllTargetMCs();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeAllAsmPrinters();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeAllDisassemblers();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeAllAsmParsers();
            }
        }

        /// <summary>Registers components for the target representing the system the calling process is running on.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterNative(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeNativeTarget();
            }

            /* Not supported on this platform
            //if( registrations.HasFlag( TargetRegistration.TargetInfo ) )
            //    LLVM.InitializeNativeTargetInfo( );

            //if( registrations.HasFlag( TargetRegistration.TargetMachine ) )
            //    LLVM.InitializeNativeTargetMC( );
            */

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeNativeAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeNativeDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeNativeAsmParser();
            }
        }

        /// <summary>Registers components for ARM AArch64 target(s).</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterAArch64(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeAArch64Target();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeAArch64TargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeAArch64TargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeAArch64AsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeAArch64Disassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeAArch64AsmParser();
            }
        }

        /// <summary>Registers components for AMDGPU targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterAMDGPU(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeAMDGPUTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeAMDGPUTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeAMDGPUTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeAMDGPUAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeAMDGPUDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeAMDGPUAsmParser();
            }
        }

        /// <summary>Registers components for ARM 32bit and 16bit thumb targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterARM(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeARMTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeARMTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeARMTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeARMAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeARMDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeARMAsmParser();
            }
        }

        /// <summary>Registers components for the Berkeley Packet Filter (BPF) target.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterBPF(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeBPFTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeBPFTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeBPFTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeBPFAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeBPFDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeBPFAsmParser();
            }
        }

        /// <summary>Registers components for the Hexagon CPU.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterHexagon(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeHexagonTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeHexagonTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeHexagonTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeHexagonAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeHexagonDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeHexagonAsmParser();
            }
        }

        /// <summary>Registers components for the Lanai target.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterLanai(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeLanaiTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeLanaiTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeLanaiTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeLanaiAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeLanaiDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeLanaiAsmParser();
            }
        }

        /// <summary>Registers components for MIPS targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterMips(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeMipsTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeMipsTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeMipsTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeMipsAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeMipsDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeMipsAsmParser();
            }
        }

        /// <summary>Registers components for MSP430 targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterMSP430(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeMSP430Target();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeMSP430TargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeMSP430TargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeMSP430AsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeMSP430Disassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeMSP430AsmParser();
            }
        }

        /// <summary>Registers components for the NVPTX targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterNVPTX(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeNVPTXTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeNVPTXTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeNVPTXTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeNVPTXAsmPrinter();
            }

            /*
            if( registrations.HasFlag( TargetRegistrations.Disassembler ) )
            {
                LLVM.InitializeNVPTXDisassembler( );
            }

            if( registrations.HasFlag( TargetRegistrations.AsmParser ) )
            {
                LLVM.InitializeNVPTXAsmParser( );
            }
            */
        }

        /// <summary>Registers components for the PowerPC targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterPowerPC(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializePowerPCTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializePowerPCTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializePowerPCTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializePowerPCAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializePowerPCDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializePowerPCAsmParser();
            }
        }

        /// <summary>Registers components for SPARC targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterSparc(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeSparcTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeSparcTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeSparcTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeSparcAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeSparcDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeSparcAsmParser();
            }
        }

        /// <summary>Registers components for SystemZ targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterSystemZ(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeSystemZTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeSystemZTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeSystemZTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeSystemZAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeSystemZDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeSystemZAsmParser();
            }
        }

        /// <summary>Registers components for the WebAssembly target.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterWebAssembly(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeWebAssemblyTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeWebAssemblyTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeWebAssemblyTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeWebAssemblyAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeWebAssemblyDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeWebAssemblyAsmParser();
            }
        }

        /// <summary>Registers components for X86 targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterX86(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeX86Target();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeX86TargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeX86TargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeX86AsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeX86Disassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeX86AsmParser();
            }
        }

        /// <summary>Registers components for XCore targets.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterXCore(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeXCoreTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeXCoreTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeXCoreTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeXCoreAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeXCoreDisassembler();
            }

            /*
            if( registrations.HasFlag( TargetRegistrations.AsmParser ) )
            {
                LLVM.InitializeXCoreAsmParser( );
            }
            */
        }

        /// <summary>Registers components for the RISCV target.</summary>
        /// <param name="registrations">Flags indicating which components to register/enable.</param>
        internal static void RegisterRISCV(TargetRegistrations registrations = TargetRegistrations.All)
        {
            if (registrations.HasFlag(TargetRegistrations.Target))
            {
                LLVM.InitializeRISCVTarget();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetInfo))
            {
                LLVM.InitializeRISCVTargetInfo();
            }

            if (registrations.HasFlag(TargetRegistrations.TargetMachine))
            {
                LLVM.InitializeRISCVTargetMC();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmPrinter))
            {
                LLVM.InitializeRISCVAsmPrinter();
            }

            if (registrations.HasFlag(TargetRegistrations.Disassembler))
            {
                LLVM.InitializeRISCVDisassembler();
            }

            if (registrations.HasFlag(TargetRegistrations.AsmParser))
            {
                LLVM.InitializeRISCVAsmParser();
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            InternalShutdownLLVM();
        }

        private static unsafe void FatalErrorHandler(sbyte* reason)
        {
            // NOTE: LLVM will call exit() upon return from this function and there's no way to stop it
            Trace.TraceError("LLVM Fatal Error: '{0}'; Application will exit.", new string((char*)reason));
        }

        private static void InternalShutdownLLVM()
        {
            var previousState = (InitializationState)Interlocked.CompareExchange(
                ref currentInitializationState,
                (int)InitializationState.ShuttingDown,
                (int)InitializationState.Initialized);
            if (previousState != InitializationState.Initialized)
            {
                throw new InvalidOperationException();
            }

            LLVM.Shutdown();

            Interlocked.Exchange(ref currentInitializationState, (int)InitializationState.ShutDown);
        }
    }
}
