# Introduction to passes

Amongst other things, this library defines [LLVM passes](https://llvm.org/docs/Passes.html) used for analysing, optimising and transforming the IR. The QIR pass library is a dynamic library that can be compiled and ran separately from the
rest of the project code.

## What do LLVM passes do?

Before getting started, we here provide a few examples of classical use cases for [LLVM passes](https://llvm.org/docs/Passes.html). You find additional [instructive examples here][1].

**Example 1: Transformation**. As a first example of what [LLVM passes](https://llvm.org/docs/Passes.html) can do, we look at optimisation. Consider a compiler which
compiles

```c
double test(double x) {
    return (1+2+x)*(x+(1+2));
}
```

into following IR:

```
define double @test(double %x) {
entry:
        %addtmp = fadd double 3.000000e+00, %x
        %addtmp1 = fadd double %x, 3.000000e+00
        %multmp = fmul double %addtmp, %addtmp1
        ret double %multmp
}
```

This code is obviously inefficient as we could get rid of one operation by rewritting the code to:

```c
double test(double x) {
    double y = 3+x;
    return y * y;
}
```

One purpose of [LLVM passes](https://llvm.org/docs/Passes.html) is to allow automatic transformation from the above IR to the IR:

```
define double @test(double %x) {
entry:
        %addtmp = fadd double %x, 3.000000e+00
        %multmp = fmul double %addtmp, %addtmp
        ret double %multmp
}
```

**Example 2: Analytics**. Another example of useful passes are those generating and collecting statistics about the program. For instance, one analytics program
makes sense for classical programs is to count instructions used to implement functions. Take the C program:

```c
int foo(int x)
{
  return x;
}

void bar(int x, int y)
{
  foo(x + y);
}

int main()
{
  foo(2);
  bar(3, 2);

  return 0;
}
```

which produces follow IR (without optimisation):

```language
define dso_local i32 @foo(i32 %0) #0 {
  %2 = alloca i32, align 4
  store i32 %0, i32* %2, align 4
  %3 = load i32, i32* %2, align 4
  ret i32 %3
}

define dso_local void @bar(i32 %0, i32 %1) #0 {
  %3 = alloca i32, align 4
  %4 = alloca i32, align 4
  store i32 %0, i32* %3, align 4
  store i32 %1, i32* %4, align 4
  %5 = load i32, i32* %3, align 4
  %6 = load i32, i32* %4, align 4
  %7 = add nsw i32 %5, %6
  %8 = call i32 @foo(i32 %7)
  ret void
}

define dso_local i32 @main() #0 {
  %1 = alloca i32, align 4
  store i32 0, i32* %1, align 4
  %2 = call i32 @foo(i32 2)
  call void @bar(i32 3, i32 2)
  ret i32 0
}
```

A stat pass for this code, would collect following statisics:

```text
Stats for 'foo'
===========================
Opcode          # Used
---------------------------
load            1
ret             1
alloca          1
store           1
---------------------------

Stats for 'bar'
===========================
Opcode          # Used
---------------------------
load            2
add             1
ret             1
alloca          2
store           2
call            1
---------------------------

Stats for 'main'
===========================
Opcode          # Used
---------------------------
ret             1
alloca          1
store           1
call            2
---------------------------
```

**Example 3: Code validation**. A third use case is code validation. For example, one could write a pass to check whether bounds are exceeded on [static arrays][2].
Note that this is a non-standard usecase as such analysis is usually made using the AST rather than at the IR level.

**References**

- [1] https://github.com/banach-space/llvm-tutor#analysis-vs-transformation-pass
- [2] https://github.com/victor-fdez/llvm-array-check-pass

## Out-of-source Pass

This library is build as set of out-of-source-passes. All this means is that we will not be downloading the LLVM repository and modifying this repository directly. You can read more [here](https://llvm.org/docs/CMake.html#cmake-out-of-source-pass).

# Library structure for passes

An important part of this PR is that it proposes a structure for passes: It is suggested that each pass has their own subcode base. The reason for this proposal is that it makes it very easy to add and remove passes as well as decide which passes to link against. Each pass is kept in its own subdirectory under `libs`:

```
Passes
├── CMakeLists.txt
└── OpsCounter
    ├── OpsCounter.cpp
    └── OpsCounter.hpp
```

Adding a new pass is easy using the `manage` tool developed in this PR:

```
./manage create-pass HelloWorld
Available templates:

1. Function Pass

Select a template:1
```

which results in a new pass code in the `libs`:

```
Passes
├── CMakeLists.txt
├── HelloWorld
│   ├── HelloWorld.cpp
│   ├── HelloWorld.hpp
│   └── SPECIFICATION.md
└── OpsCounter
    ├── OpsCounter.cpp
    └── OpsCounter.hpp
```

A full example of how to create a basic function pass is included in the README.md file for anyone interested.
