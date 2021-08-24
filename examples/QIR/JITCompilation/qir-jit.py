import sys, platform
import llvmlite.binding as llvm
from ctypes import CFUNCTYPE

linux_runtime_libs = ["build/libMicrosoft.Quantum.Qir.Runtime.so",
                      "build/libMicrosoft.Quantum.Qir.QSharp.Core.so",
                      "build/libMicrosoft.Quantum.Qir.QSharp.Foundation.so",
                      "build/Microsoft.Quantum.Simulator.Runtime.dll",
                      "build/libQIRinit.so"]

windows_runtime_libs = ["build/Microsoft.Quantum.Qir.Runtime.dll",
                        "build/Microsoft.Quantum.Qir.QSharp.Core.dll",
                        "build/Microsoft.Quantum.Qir.QSharp.Foundation.dll",
                        "build/Microsoft.Quantum.Simulator.Runtime.dll",
                        "build/QIRinit.dll"]

if platform.system() == "Linux":
    runtime_libs = linux_runtime_libs
elif platform.system() == "Windows":
    runtime_libs = windows_runtime_libs
else:
    raise Exception("unsupported platform")

def main(qir_file, entry_point):
    # Initialize LLVM
    llvm.initialize()
    llvm.initialize_native_target()
    llvm.initialize_native_asmprinter()

    # Load the QIR Runtime libraries
    for lib in runtime_libs:
        llvm.load_library_permanently(lib)

    # Parse the provided QIR module
    file = open(qir_file, 'r')
    module = llvm.parse_assembly(file.read())

    # Create a jit execution engine
    target = llvm.Target.from_default_triple().create_target_machine()
    jit_engine = llvm.create_mcjit_compiler(module, target)

    # Initialize the QIR Runtime and simulator via exposed C wrapper
    fun_ptr = llvm.address_of_symbol("InitQIRSim")
    CFUNCTYPE(None)(fun_ptr)()

    # Run the entry point of the QIR module
    fun_ptr = jit_engine.get_function_address(entry_point)
    CFUNCTYPE(None)(fun_ptr)()

if __name__ == "__main__":
    assert len(sys.argv) == 3, "need to supply qir file and entry point arguments"
    main(sys.argv[1], sys.argv[2])
