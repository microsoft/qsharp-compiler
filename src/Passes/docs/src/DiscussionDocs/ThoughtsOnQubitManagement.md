# Referenced vs lock/unlock qubit allocation

## Motivation

As part of the QIR analysis module, we will need to make analysis of the qubit allocation in order to transform the QIR to certain profiles. To this end, we need to understand whether the current approach of "allocation" (locking) and "deallocation" (unlocking) could cause issues for if we later would want to migrate to a referenced counted scheme.

## Summary

A shallow first investigation suggest that we can proceed using `qubit_allocate` and `qubit_release` with without causing any issues to a future implementation of reference counted qubit. We've found that it is likely that proceeding with `qubit_allocate` and `qubit_release` for the first implementation is compatible with a later implementation of referenced qubits. We therefore propose to build the transformation MVP using `qubit_allocate` and `qubit_release`.

In some ways, `qubit_allocate` and `qubit_release` are a better choice than a referenced implementation as it allows direct compatibility existing languages such as C++. In appendix we demonstrate that we build the necessary resource management for a C++ runtime using the two functions `qubit_allocate` and `qubit_release`, meaning that it is directly possible to build a "Q++" version of C++ (if you disregard the other opaque types).

Notably, `Array` and other opaque QIR types, does not seem to be directly supportable C or C++ as they add qualifiers `struct` in front of the opaque typename:

```language
%struct.QubitId = type opaque
```

# Allocation and destruction

In this section, we will consider some frontend aspects around how qubits are currently allocated in a couple of different languages. As a starting point we will consider Q# and C++ as potential frontends. We will first discuss potential ways people will want to manage their qubits lifetime, then look at how qubits are allocated in C# today and finally discuss how qubits could be managed in C++.

## Possible lifetime management of qubits

We can think of a few different life-time management scenarios:

1. Scoped lifetime management
2. Lifetime limited to that of a child scope
3. Lifetime limited to the lifetime of a containing object
4. Fully reference counted.

So far we have only considered 1 and 4. Using allocators and deallocators (in a languge that support move semantics) allows us to implement all four scenarios.

Forcing reference counting will not allow us to do 2 and only to some degree 3. Further, we would only be able to map back to 1 if the count adds up at compile time. While 4 provides the most secure scenario there are many cases where 1-3 are desirable at least in ordinary resource management scenarios.

## Qubit allocation in Q#

Currently Q# implements 1 and it is appropriate to briefly consider how it currently uses `aloocate` and `release` to do so: Acquiring qubit access in Q# is done through a `use` statement which assigns the qubit to the current scope:

```
    operation Main(): Unit
    {
        use q1 = Qubit();

        /// ... The underlying qubit ID is reserved through
        /// the body of this function ...


        /// ... and released right before the exit of the scope.
    }
```

The use statement creates a allocate instruction and release instructions are inserted at the exit of the scope. However, one issue that is encountered in the Q# language is that it becomes hard for the frontend to provide garantuees that the qubit does not escape the scope. A good example of a hard case is

```
  operation Test() : Unit
  {
    use (q1, q2) = (Qubit(), Qubit());
    mutable arr = [q1, q2];

    for(i in 0..10)
    {
      use n = Qubit();
      arr /w= 0 <- q[1];
      arr /w= 1 <- n;
    }

    H(arr[0]);

  }
```

This case makes it hard to analyze which qubits stays in scope and which are escaping. We note that we have two scopes where qubits can escape: The `Test` operation and the `for` loop.

Analysing and providing garantuees on the whether a qubit is in use is the core problem that either needs to be solved in the QIR or the frontend.

## C++ frontend solution

This notation in the previous section is essentially a case of a RAII that requires two steps at construction:

1. Find the next free qubit id
2. Lock the qubit id

and one step at descruction

1. Unlock the qubit.

In a language like C++ the control of qubit locking and unlocking would be solved using non-copyable classes to manage the lifetime of the lock. We provide an example implementation and demonstrate that this compiles to (almost) valid QIR. Within the defined C++ runtime, the loop specified in the previous section, would look like:

```c++
int main()
{
  auto  q1     = Qubit::use();
  auto  q2     = Qubit::use();
  Qubit arr[2] = {std::move(q1), std::move(q2)};

  for (uint64_t i = 0; i < 10; ++i)
  {
    auto n = Qubit::use();
    arr[0] = std::move(arr[1]);
    arr[1] = std::move(n);
  }

  H(arr[0]);

  return 0;
}
```

Compiling this to LLVM IR, produces

```
  %1 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !3
  %2 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !6
  %3 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %3) #2
  %4 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %4) #2
  %5 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %5) #2
  %6 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %6) #2
  %7 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %7) #2
  %8 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %8) #2
  %9 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %9) #2
  %10 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %10) #2
  %11 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %11) #2
  %12 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %12) #2

  ;; Note that this is correctly placed right before the last
  ;; __four__ dealloactions (two in the array + the two allocated in the beginning).
  tail call void @__quantum__qis__h__body(%struct.QubitId* %11) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %12) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %11) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %2) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %1) #2
  ret i32 0
```

which is exactly the output we would want.

## Proposal to resolve the allocation issue

Continue to use `allocate` and `release` and let it be up to the frontend to implement the needed functionality to manage the lifetime of the qubits.

# Appendix

## C and C++ as the classical runtime

We note that with the current definition of a Qubit being an opaque type, it would be difficult to make the QIR compatible with C++. However, changing the type into a `i64` or `struct.Qubit { i64 }` would make the QIR directly compatible with C and C++ as runtime languages.

Example of a classical C++ runtime that compiles into QIR compatible IR (except for the opaque class type):

```c++
#include <cstdint>
struct QubitId
{
  int64_t value;
};

extern "C" QubitId __quantum__rt__qubit_allocate();
extern "C" void    __quantum__rt__qubit_release(QubitId id);
extern "C" void    __quantum__qis__h__body(QubitId id);

int32_t main()
{
  auto qubit = __quantum__rt__qubit_allocate();

  __quantum__qis__h__body(qubit);
  __quantum__rt__qubit_release(qubit);

  return 0;
}
```

## C++ implemenation of qubit

```c++
#include <cstdint>
struct QubitId;

extern "C" QubitId *__quantum__rt__qubit_allocate() noexcept;
extern "C" void     __quantum__rt__qubit_release(QubitId *id) noexcept;
extern "C" void     __quantum__rt__h__body(QubitId *id) noexcept;

class Qubit
{
public:
  Qubit()              = delete;
  Qubit(Qubit const &) = delete;
  Qubit(Qubit &&)      = default;
  Qubit &operator=(Qubit const &) = delete;
  Qubit &operator=(Qubit &&) = default;

  static Qubit use() noexcept
  {
    QubitId *id = __quantum__rt__qubit_allocate();

    return Qubit(id);
  }

  ~Qubit() noexcept
  {
    __quantum__rt__qubit_release(id_);
  }

  QubitId *id() const
  {
    return id_;
  }

private:
  Qubit(QubitId *id)
    : id_{id}
  {}

  QubitId *id_{};
};

inline void H(Qubit &qubit) noexcept
{
  __quantum__rt__h__body(qubit.id());
}
```

With the above runtime, we will attempt to implement

```
  operation Main()
  {
    use (q1, q2, q3, q4) = (Qubit(), Qubit(), Qubit(), Qubit());
    H(q2);
  }
```

which would translate into

```c++
int main()
{
  auto q1 = Qubit::use();
  auto q2 = Qubit::use();
  auto q3 = Qubit::use();
  auto q4 = Qubit::use();
  H(q2);
  return 0;
}
```

Compiling this and emitting the IR with optimisation `O2`, we get

```
define i32 @main() local_unnamed_addr #0 personality i32 (...)* @__gxx_personality_v0 {
  %1 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !3
  %2 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !6
  %3 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  %4 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !12
  tail call void @__quantum__rt__h__body(%struct.QubitId* %2) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %4) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %3) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %2) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %1) #2
  ret i32 0
}
```

which is QIR compatible if you disregard the use of `struct.QubitId*` instead of `Qubit*`.

The different paradigms for using a qubit could be as follows:

```c++
int main()
{
  auto q1 = Qubit::use();
  auto q2 = Qubit::use();
  auto q3 = Qubit::use();
  auto q4 = Qubit::use();

  // q1 is released at the end of f
  f(std::move(q1));

  // q2 is released at the end of main, use in g
  g(q1);

  // q3 is released when arr is destroyed
  std::vector<Qubit> arr;
  arr.emplace_back(std::move(q3));

  // q4 is a reference counted qubit that can be freely moved around
  auto q4ptr = std::make_shared<Qubit>(std::move(q4));

  // Release q4
  q4ptr.reset();

  return 0;
}
```

## Teleportation example

To test the limits of a C++ implementation, we here implement the teleportation example:

```c++
inline void prepareEntangledPair(Qubit &left, Qubit &right) noexcept
{
  hAdj(left);
  cnotAdj(left, right);
}

inline void applyCorrection(Qubit &src, Qubit &intermediate, Qubit &dest) noexcept
{
  if (mResetZ(src) == one())
  {
    z(dest);
  }
  if (mResetZ(intermediate) == one())
  {
    x(dest);
  }
}

int main()
{
  constexpr auto nPairs = 2;

  auto                 leftMessage  = Qubit::use();
  auto                 rightMessage = Qubit::use();
  Array<Qubit, nPairs> leftPreshared;
  for (auto i = 0; i < nPairs; ++i)
  {
    leftPreshared[i] = Qubit::use();
  }

  Array<Qubit, nPairs> rightPreshared;
  for (auto i = 0; i < nPairs; ++i)
  {
    rightPreshared[i] = Qubit::use();
  }

  prepareEntangledPair(leftMessage, rightMessage);

  for (auto i = 0; i < nPairs; ++i)
  {
    prepareEntangledPair(leftPreshared[i], rightPreshared[i]);
  }

  prepareEntangledPair(rightMessage, leftPreshared[0]);
  applyCorrection(rightMessage, leftPreshared[0], rightPreshared[0]);

  for (auto i = 0; i < nPairs; ++i)
  {
    prepareEntangledPair(rightPreshared[i - 1], leftPreshared[i]);
    applyCorrection(rightPreshared[i - 1], leftPreshared[i], rightPreshared[i]);
  }
  return 0;
}

```

which produces

```
define i32 @main() local_unnamed_addr #0 personality i32 (...)* @__gxx_personality_v0 {
  %1 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !3
  %2 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !6
  %3 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %3) #2
  %4 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !9
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %4) #2
  %5 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !12
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %5) #2
  %6 = tail call %struct.QubitId* @__quantum__rt__qubit_allocate() #2, !noalias !12
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %6) #2
  tail call void @__quantum__rt__h_adj(%struct.QubitId* %1) #2
  tail call void @__quantum__rt__cnot_adj(%struct.QubitId* %1, %struct.QubitId* %2) #2
  tail call void @__quantum__rt__h_adj(%struct.QubitId* %3) #2
  tail call void @__quantum__rt__cnot_adj(%struct.QubitId* %3, %struct.QubitId* %5) #2
  tail call void @__quantum__rt__h_adj(%struct.QubitId* %4) #2
  tail call void @__quantum__rt__cnot_adj(%struct.QubitId* %4, %struct.QubitId* %6) #2
  tail call void @__quantum__rt__h_adj(%struct.QubitId* %2) #2
  tail call void @__quantum__rt__cnot_adj(%struct.QubitId* %2, %struct.QubitId* %3) #2
  %7 = tail call %struct.Result* @__quantum__qis__m__body(%struct.QubitId* %2) #2
  tail call void @__quantum__qis__reset__body(%struct.QubitId* %2) #2
  %8 = tail call %struct.Result* @__quantum__rt__result_get_one() #2
  %9 = icmp eq %struct.Result* %7, %8
  br i1 %9, label %10, label %11

10:                                               ; preds = %0
  tail call void @__quantum__rt__z_body(%struct.QubitId* %5) #2
  br label %11

11:                                               ; preds = %10, %0
  %12 = tail call %struct.Result* @__quantum__qis__m__body(%struct.QubitId* %3) #2
  tail call void @__quantum__qis__reset__body(%struct.QubitId* %3) #2
  %13 = tail call %struct.Result* @__quantum__rt__result_get_one() #2
  %14 = icmp eq %struct.Result* %12, %13
  br i1 %14, label %15, label %16

15:                                               ; preds = %11
  tail call void @__quantum__rt__x_body(%struct.QubitId* %5) #2
  br label %16

16:                                               ; preds = %11, %15
  tail call void @__quantum__rt__h_adj(%struct.QubitId* undef) #2
  tail call void @__quantum__rt__cnot_adj(%struct.QubitId* undef, %struct.QubitId* %3) #2
  %17 = tail call %struct.Result* @__quantum__qis__m__body(%struct.QubitId* undef) #2
  tail call void @__quantum__qis__reset__body(%struct.QubitId* undef) #2
  %18 = tail call %struct.Result* @__quantum__rt__result_get_one() #2
  %19 = icmp eq %struct.Result* %17, %18
  br i1 %19, label %20, label %21

20:                                               ; preds = %16
  tail call void @__quantum__rt__z_body(%struct.QubitId* %5) #2
  br label %21

21:                                               ; preds = %20, %16
  %22 = tail call %struct.Result* @__quantum__qis__m__body(%struct.QubitId* %3) #2
  tail call void @__quantum__qis__reset__body(%struct.QubitId* %3) #2
  %23 = tail call %struct.Result* @__quantum__rt__result_get_one() #2
  %24 = icmp eq %struct.Result* %22, %23
  br i1 %24, label %25, label %26

25:                                               ; preds = %21
  tail call void @__quantum__rt__x_body(%struct.QubitId* %5) #2
  br label %26

26:                                               ; preds = %21, %25
  tail call void @__quantum__rt__h_adj(%struct.QubitId* %5) #2
  tail call void @__quantum__rt__cnot_adj(%struct.QubitId* %5, %struct.QubitId* %4) #2
  %27 = tail call %struct.Result* @__quantum__qis__m__body(%struct.QubitId* %5) #2
  tail call void @__quantum__qis__reset__body(%struct.QubitId* %5) #2
  %28 = tail call %struct.Result* @__quantum__rt__result_get_one() #2
  %29 = icmp eq %struct.Result* %27, %28
  br i1 %29, label %30, label %31

30:                                               ; preds = %26
  tail call void @__quantum__rt__z_body(%struct.QubitId* %6) #2
  br label %31

31:                                               ; preds = %30, %26
  %32 = tail call %struct.Result* @__quantum__qis__m__body(%struct.QubitId* %4) #2
  tail call void @__quantum__qis__reset__body(%struct.QubitId* %4) #2
  %33 = tail call %struct.Result* @__quantum__rt__result_get_one() #2
  %34 = icmp eq %struct.Result* %32, %33
  br i1 %34, label %35, label %36

35:                                               ; preds = %31
  tail call void @__quantum__rt__x_body(%struct.QubitId* %6) #2
  br label %36

36:                                               ; preds = %35, %31
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %6) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %5) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %4) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %3) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %2) #2
  tail call void @__quantum__rt__qubit_release(%struct.QubitId* %1) #2
  ret i32 0
}
```

## C++ example of firmware

```c++

/// firmware.hpp
#pragma once
#include "target.hpp"

extern "C" QubitId __quantum__rt__qubit_allocate();
extern "C" void __quantum__rt__qubit_release(QubitId id);
extern "C" void firmware_init();


/// firmware.cpp
#include "firmware.hpp"
#include "target.hpp"

int64_t free_qubit_id[QUBIT_COUNT];
int64_t next_free_qubit;

extern "C" QubitId __quantum__rt__qubit_allocate()
{
  int64_t ret = free_qubit_id[next_free_qubit];
  ++next_free_qubit;
  return ret;
}

extern "C" void __quantum__rt__qubit_release(QubitId id)
{
  --next_free_qubit;
  free_qubit_id[next_free_qubit] = id;
}

extern "C" QubitId __quantum__rt__qubit_count()
{
  return QUBIT_COUNT;
}

extern "C" void firmware_init()
{
  for (int64_t i = 0; i < QUBIT_COUNT; ++i)
  {
    free_qubit_id[i] = i;
  }
}
```

## Target definition

```c++
// target.hpp
#pragma once
#include <cstdint>

/// Target configuration
#define QUBIT_COUNT 16

typedef int64_t QubitId;

```
