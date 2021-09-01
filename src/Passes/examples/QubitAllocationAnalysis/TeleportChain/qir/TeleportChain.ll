
%Range = type { i64, i64, i64 }
%Qubit = type opaque
%Array = type opaque
%String = type opaque

@PauliI = internal constant i2 0
@PauliX = internal constant i2 1
@PauliY = internal constant i2 -1
@PauliZ = internal constant i2 -2
@EmptyRange = internal constant %Range { i64 0, i64 1, i64 -1 }

define internal i64 @TeleportChain__Calculate__body(i64 %n) {
entry:
  %q = call %Qubit* @__quantum__rt__qubit_allocate()
  %ret = alloca i64, align 8
  store i64 2, i64* %ret, align 4
  %0 = icmp ne i64 %n, 0
  br i1 %0, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  %1 = sub i64 %n, 1
  %2 = call i64 @TeleportChain__Calculate__body(i64 %1)
  %3 = add i64 %2, 2
  store i64 %3, i64* %ret, align 4
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %entry
  %4 = load i64, i64* %ret, align 4
  call void @__quantum__rt__qubit_release(%Qubit* %q)
  ret i64 %4
}

declare %Qubit* @__quantum__rt__qubit_allocate()

declare %Array* @__quantum__rt__qubit_allocate_array(i64)

declare void @__quantum__rt__qubit_release(%Qubit*)

define internal i64 @TeleportChain__Main__body() {
entry:
  %ret = alloca i64, align 8
  store i64 1, i64* %ret, align 4
  br label %header__1

header__1:                                        ; preds = %exiting__1, %entry
  %i = phi i64 [ 0, %entry ], [ %4, %exiting__1 ]
  %0 = icmp sle i64 %i, 3
  br i1 %0, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %1 = load i64, i64* %ret, align 4
  %2 = call i64 @TeleportChain__Calculate__body(i64 4)
  %3 = add i64 %1, %2
  store i64 %3, i64* %ret, align 4
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %4 = add i64 %i, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  %5 = load i64, i64* %ret, align 4
  ret i64 %5
}

define i64 @TeleportChain__Main__Interop() #0 {
entry:
  %0 = call i64 @TeleportChain__Main__body()
  ret i64 %0
}

define void @TeleportChain__Main() #1 {
entry:
  %0 = call i64 @TeleportChain__Main__body()
  %1 = call %String* @__quantum__rt__int_to_string(i64 %0)
  call void @__quantum__rt__message(%String* %1)
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*)

declare %String* @__quantum__rt__int_to_string(i64)

declare void @__quantum__rt__string_update_reference_count(%String*, i32)

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
