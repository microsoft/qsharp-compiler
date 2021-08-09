# Optimisation Using Opt

In this document, we give a brief introduction on how to perform IR optimisations
using `opt`.

## Stripping dead code

We start out by considering a simple case of a program that just returns 0:

```qsharp
namespace Example {
    @EntryPoint()
    operation OurAwesomeQuantumProgram(nQubits : Int) : Int {

        return 0;
    }
}
```

You find the code for this in the folder `SimpleExample`. To generate a QIR for this code, go to the folder and run

```sh
cd SimpleExample/
dotnet clean SimpleExample.csproj
(...)
dotnet build SimpleExample.csproj -c Debug
```

If everything went well, you should now have a subdirectory called `qir` and inside `qir`, you will find `SimpleExample.ll`. Depending on your compiler,
the generated QIR will vary, but in general, it will be relatively long. Looking at this file, you will see
that the total length is a little above 2000 lines of code. That is pretty extensive for a program which essentially
does nothing so obviously, most of the generated QIR must be dead code. We can now use `opt` to get rid of the dead code and we do this by invoking:

```sh
opt -S qir/SimpleExample.ll -O3  > qir/SimpleExample-O3.ll
```

All going well, this should reduce your QIR to

```language
; Function Attrs: norecurse nounwind readnone willreturn
define i64 @Example__QuantumFunction__Interop(i64 %nQubits) local_unnamed_addr #0 {
entry:
  ret i64 0
}

define void @Example__QuantumFunction(i64 %nQubits) local_unnamed_addr #1 {
entry:
  %0 = tail call %String* @__quantum__rt__int_to_string(i64 0)
  tail call void @__quantum__rt__message(%String* %0)
  tail call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}
```

with a few additional declarations.
