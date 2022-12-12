import sys, platform
import llvmlite.binding as llvm
from ctypes import CFUNCTYPE

linux_simulator_lib = "build/libMicrosoft.Quantum.Simulator.Runtime.so"

windows_simulator_lib = "build/Microsoft.Quantum.Simulator.Runtime.dll"

if platform.system() == "Linux":
    simulator_lib = linux_simulator_lib
elif platform.system() == "Windows":
    simulator_lib = windows_simulator_lib
else:
    raise Exception("unsupported platform")

def main(qir_file, entry_point):
    # Initialize LLVM
    llvm.initialize()
    llvm.initialize_native_target()
    llvm.initialize_native_asmprinter()

    # Load the simulator library
    llvm.load_library_permanently(simulator_lib)

    # Parse the provided QIR module
    file = open(qir_file, 'r')
    module = llvm.parse_assembly(file.read())

    # Create a jit execution engine
    target = llvm.Target.from_default_triple().create_target_machine()
    jit_engine = llvm.create_mcjit_compiler(module, target)

    # Run the entry point of the QIR module
    fun_ptr = jit_engine.get_function_address(entry_point)
    CFUNCTYPE(None)(fun_ptr)()

if __name__ == "__main__":
    assert len(sys.argv) == 3, "need to supply qir file and entry point arguments"
    main(sys.argv[1], sys.argv[2])
