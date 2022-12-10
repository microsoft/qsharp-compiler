# JIT Compile QIR with Python

Just-In-Time (JIT) compilation enables to combine certain advantages from interpreters and ahead-of-time compilers.
Essentially, the JIT compiler can decide to compile a program or sections of a program at run time, as well as how to compile them.
This means that the program can run faster compared to pure interpretation, while allowing for lazy compilation and adaptive optimization levels which can cut down on lengthy compilation times.

This example demonstrates how to use the [LLVM](https://llvm.org/) infrastructure to JIT compile QIR programs with a few lines in Python.

## Prerequisite

The sample requires a working [Python3](https://www.python.org/downloads/) installation, as well as the [llvmlite](https://llvmlite.readthedocs.io/en/latest/index.html) Python bindings.
If you don't have Python yet, you can get it from the official website or by installing [Miniconda](https://docs.conda.io/en/latest/miniconda.html), which also includes the Conda package manager.
Then install the llvmlite package with Conda or PIP:

```shell
conda install -c numba llvmlite
```

```shell
pip install llvmlite
```

The llvmlite package may also be available via the system package manager, just make sure to at least get version 0.37 for LLVM 11 support.

The sample also makes use of the [Clang compiler](https://clang.llvm.org/).
Refer to the [Optimization example](../Optimization#installing-clang) for instructions on installing Clang and LLVM.

## Calling into LLVM's JIT library

The LLVM core is a collection of libraries that facilitate building compilers, including components to JIT compile LLVM assembly or bytecode.
Since QIR is a specification on top of LLVM, the libraries can also be used to easily JIT compile QIR code.
To interface with LLVM, it comes with both a C++ and C API.
While the latter can be called into from Python using [ctypes](https://docs.python.org/3/library/ctypes.html), it is more convenient to use the native Python bindings provided by the [llvmlite](https://llvmlite.readthedocs.io/en/latest/index.html) project.

The function below is all that is needed to JIT compile a QIR program, and consists of these few short steps:

- initialize LLVM
- load QIR/simulator libraries
- parse the QIR program
- load the JIT compiler
- initialize and attach a simulator (see [next section](#building-the-project) for more info)
- run a QIR function

```py
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
```

Here, `simulator_lib` is the platform specific path to the simulator library.
The name of the QIR source file `qir_file` is provided as a command-line argument, as well the name of the entry point function `entry_point`.

The way external functions with C-style linkage are invoked from Python is by using the ctypes module.
Once a dynamic library has been loaded via `load_library_permanently`, its exported symbols are now available and can be searched for the desired function via `address_of_symbol`.
The returned value is a raw pointer that can be converted to a function pointer with the correct type information, and subsequently invoked.
This is done via the function `CFUNCTYPE(<return type>, <argument types>..)(<ptr_name>)`, which also specifies the usage of the standard C calling convention.
Refer to the [ctypes documentation](https://docs.python.org/3/library/ctypes.html) for more information on calling external functions from Python.

The full Python script can be found in `qir-jit.py`.

## Building the project

Before being able run QIR via LLVM's JIT compiler, we need to download the necessary simulator library from the [Quantum Simulators](https://www.nuget.org/packages/Microsoft.Quantum.Simulators/) NuGet packages:

- **Linux** (install mono for the NuGet CLI):

    ```shell
    mkdir build
    sudo apt update && sudo apt install -y mono-complete
    curl https://dist.nuget.org/win-x86-commandline/latest/nuget.exe --output build/nuget
    mono build/nuget sources add -name nuget.org -source https://api.nuget.org/v3/index.json
    mono build/nuget install Microsoft.Quantum.Simulators -Version 0.27.244707 -DirectDownload -DependencyVersion Ignore -OutputDirectory tmp
    cp tmp/Microsoft.Quantum.Simulators.0.27.244707/runtimes/linux-x64/native/libMicrosoft.Quantum.Simulator.Runtime.so build
    cp tmp/Microsoft.Quantum.Simulators.0.27.244707/runtimes/linux-x64/native/libomp.so build
    rm -r tmp
    ```

- **Windows**:

    ```shell
    mkdir build
    curl https://dist.nuget.org/win-x86-commandline/latest/nuget.exe --output build/nuget.exe
    build/nuget install Microsoft.Quantum.Simulators -Version 0.27.244707 -DirectDownload -DependencyVersion Ignore -OutputDirectory tmp
    cp tmp/Microsoft.Quantum.Simulators.0.27.244707/runtimes/win-x64/native/Microsoft.Quantum.Simulator.Runtime.dll build
    cp tmp/Microsoft.Quantum.Simulators.0.27.244707/runtimes/win-x64/native/Microsoft.Quantum.Simulator.Runtime.lib build
    cp tmp/Microsoft.Quantum.Simulators.0.27.244707/runtimes/win-x64/native/libomp140.x86_64.dll build
    rm -r tmp
    ```

## Running a QIR program with JIT

To run any QIR program through the JIT, simply run the Python script and provide the QIR source file name and entry point as command line arguments, for example:

```shell
python qir-jit.py Hello.ll Hello__HelloQ
```

QIR code can also be invoked by using the script as a module or using the code in a Jupyter Notebook.
