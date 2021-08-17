import llvmlite.binding as llvm
from ctypes import CFUNCTYPE

qir_file = "qir/hello.ll"
entry_point = "Hello__SayHello"
runtime_libs = ["lib/libMicrosoft.Quantum.Qir.Runtime.so", "lib/libMicrosoft.Quantum.Qir.QSharp.Core.so", "lib/libMicrosoft.Quantum.Qir.QSharp.Foundation.so", "lib/libMicrosoft.Quantum.Simulator.Runtime.so"]

def main():
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
    llvm.load_library_permanently("lib/libQIRWrapper.so")
    fun_ptr = llvm.address_of_symbol("InitQIRSim")
    CFUNCTYPE(None)(fun_ptr)()

    # Run the entry point of the QIR module
    fun_ptr = jit_engine.get_function_address(entry_point)
    CFUNCTYPE(None)(fun_ptr)()

if __name__ == "__main__":
    main()
