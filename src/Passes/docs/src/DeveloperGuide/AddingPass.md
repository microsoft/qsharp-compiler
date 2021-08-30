# Implementing a new pass

To make it easy to create a new pass, we have created a few templates to get you started quickly:

```sh
./manage create-pass HelloWorld
Available templates:

1. Function Pass

Select a template:1
```

At the moment you only have one choice which is a function pass. Over time we will add additional templates. Once you have instantiated your template, you are ready to build it:

```sh
mkdir Debug
cd Debug
cmake ..
-- The C compiler identification is AppleClang 12.0.5.12050022
-- The CXX compiler identification is AppleClang 12.0.5.12050022
(...)
-- Configuring done
-- Generating done
-- Build files have been written to: ./qsharp-compiler/src/Passes/Debug

make

[ 25%] Building CXX object libs/CMakeFiles/OpsCounter.dir/OpsCounter/OpsCounter.cpp.o
[ 50%] Linking CXX shared library libOpsCounter.dylib
[ 50%] Built target OpsCounter
[ 75%] Building CXX object libs/CMakeFiles/HelloWorld.dir/HelloWorld/HelloWorld.cpp.o
[100%] Linking CXX shared library libHelloWorld.dylib
[100%] Built target HelloWorld
```

Your new pass is ready to be implemented. Open `libs/HelloWorld/HelloWorld.cpp` to implement the details of the pass. At the moment, the
template will not do much except for print the function names of your code. To test your new pass go to the directory `examples/ClassicalIrCommandline`,
build an IR and run the pass:

```sh
cd ../examples/ClassicalIrCommandline
make
opt -load-pass-plugin ../../Debug/libs/libHelloWorld.{dylib,so} --passes="hello-world" -disable-output classical-program.ll
```

If everything worked, you should see output like this:

```sh
Implement your pass here: foo
Implement your pass here: bar
Implement your pass here: main
```

## Running a pass

You can run a pass using [opt](https://llvm.org/docs/CommandGuide/opt.html) as follows:

```sh
cd examples/ClassicalIrCommandline
make emit-llvm-bc
opt -load-pass-plugin ../../{Debug,Release}/libOpsCounter.{dylib,so} --passes="print<operation-counter>" -disable-output classical-program.bc
```

For a detailed tutorial, see examples.
